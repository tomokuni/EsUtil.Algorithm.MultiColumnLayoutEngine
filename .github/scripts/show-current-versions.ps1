<#
.SYNOPSIS
現在のバージョン状況（リポジトリのバージョン・タグ・GitHub Release・NuGet.org の公開状況）を表示する。
.DESCRIPTION
リリース実行時に「今どのバージョンがどこまで出ているか」を確認するための情報を出力する。
標準出力へ Markdown の表を書き出し、環境変数 GITHUB_STEP_SUMMARY が設定されている場合は
実行サマリー（Actions の実行ページ）にも同じ表を追記する。

入力フォーム（workflow_dispatch）の初期値には現在の値を表示できないため（GitHub Actions の仕様で
default に式を指定できない）、代わりに実行サマリーで確認できるようにするためのスクリプトである。

本スクリプトは情報の表示のみを行い、ファイルや外部の状態を変更しない。

全タグの最大（バックポートの判断に使う）と、系列ごとの最大（バックポート先の判断に使う）を表示し、
`version` に入力する値の案内と、リリースできないブランチで実行された場合の注意を表示する。
.PARAMETER RepoRoot
リポジトリのルート。既定は本スクリプトの 2 つ上の階層。
.PARAMETER Branch
実行ブランチ名（例: main）。省略時は git から取得する。GitHub Actions では
checkout が detached HEAD になるため、ワークフローからは `github.ref_name` を渡す。
.PARAMETER NoNetwork
NuGet.org と GitHub への問い合わせを行わない（オフラインで確認する場合）。
.OUTPUTS
System.String
表示した表（Markdown）。
.EXAMPLE
& ./.github/scripts/show-current-versions.ps1 -Branch main
.EXAMPLE
& ./.github/scripts/show-current-versions.ps1 -NoNetwork
#>
[CmdletBinding()]
param(
    [string]$RepoRoot,

    [string]$Branch,

    [switch]$NoNetwork
)

$ErrorActionPreference = 'Stop'

# バージョンの規則（形式・比較・最大値）は version.ps1 に委譲する
. "$PSScriptRoot/version.ps1"

if (-not $RepoRoot) {
    # 本スクリプトは <リポジトリルート>/.github/scripts に置かれている
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

<#
.SYNOPSIS
バージョンファイルに記載された <Version> の値を取得する。
#>
function Get-VersionFileValue {
    param([string]$VersionFile)

    $path = Join-Path $RepoRoot $VersionFile

    if (-not (Test-Path -LiteralPath $path)) {
        throw "バージョンファイルが見つかりません: $path"
    }

    $text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
    $match = [regex]::Match($text, '<Version>([^<]*)</Version>')

    if (-not $match.Success) {
        throw "バージョンファイルに <Version>...</Version> がありません: $path"
    }

    return $match.Groups[1].Value
}

<#
.SYNOPSIS
MSBuild でプロジェクトのプロパティ値を取得する。
.DESCRIPTION
値のみを標準出力へ出す dotnet msbuild -getProperty を使用する（MSBuild の式も展開される）。
#>
function Get-MSBuildProperty {
    param(
        [string]$Project,
        [string]$Name
    )

    $path = Join-Path $RepoRoot $Project

    if (-not (Test-Path -LiteralPath $path)) {
        throw "プロジェクトが見つかりません: $path"
    }

    $value = & dotnet msbuild $path "-getProperty:$Name" -nologo

    if ($LASTEXITCODE -ne 0) {
        throw "プロパティの取得に失敗しました: $Project / $Name（exit $LASTEXITCODE）"
    }

    return ($value | Out-String).Trim()
}

<#
.SYNOPSIS
実行ブランチ名を取得する（取得できない場合は空文字）。
.DESCRIPTION
GitHub Actions の checkout は detached HEAD になるため、その場合は 'HEAD' が返る。
呼び出し側は -Branch で明示的に渡すことを推奨する。
#>
function Get-CurrentBranch {
    $name = (& git -C $RepoRoot rev-parse --abbrev-ref HEAD 2>$null | Out-String).Trim()

    if ([string]::IsNullOrWhiteSpace($name) -or $name -eq 'HEAD') {
        return ''
    }

    return $name
}

<#
.SYNOPSIS
最新の GitHub Release のタグを取得する（取得できない場合は空文字）。
#>
function Get-LatestReleaseTag {
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        return ''
    }

    $json = (& gh release list --limit 1 --json tagName 2>$null | Out-String).Trim()

    if ([string]::IsNullOrWhiteSpace($json)) {
        return ''
    }

    $releases = @($json | ConvertFrom-Json)

    if ($releases.Count -eq 0) {
        return ''
    }

    return [string]$releases[0].tagName
}

<#
.SYNOPSIS
NuGet.org に公開済みのバージョン一覧を取得する（未公開の場合は空の配列）。
#>
function Get-NuGetPublishedVersions {
    param([string]$PackageId)

    $url = "https://api.nuget.org/v3-flatcontainer/$($PackageId.ToLowerInvariant())/index.json"

    try {
        $response = Invoke-RestMethod -Uri $url -Headers @{ 'User-Agent' = 'EsUtil-release-summary' } -TimeoutSec 30
        return @($response.versions)
    }
    catch {
        # 未公開（404）などは「なし」として扱う
        return @()
    }
}

<#
.SYNOPSIS
値が空の場合に代替文字列を返す。
#>
function Format-Value {
    param(
        [string]$Value,
        [string]$Fallback = 'なし'
    )

    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $Fallback
    }

    return $Value
}

# ─── 1. 実行ブランチと比較対象の決定 ───
# 通常のリリースは全タグの最大、バックポート（旧系列）は対象系列の最大と比較される
$branch = $Branch

if ([string]::IsNullOrWhiteSpace($branch)) {
    $branch = Get-CurrentBranch
}

$branchLabel = '不明（git から取得できませんでした。`-Branch` で指定してください）'

if (-not [string]::IsNullOrWhiteSpace($branch)) {
    $branchLabel = $branch
}

<#
.SYNOPSIS
次のバージョンの候補（比較対象の最大の次）を求める。
#>
function Get-NextVersionExample {
    param([string]$Version)

    if ([string]::IsNullOrWhiteSpace($Version)) {
        return ''
    }

    if ($Version.Contains('-')) {
        # プレリリース（例: 1.0.1-rc.1）の次は、同じ番号の正式版（例: 1.0.1）が候補になる
        return $Version.Split('-')[0]
    }

    $parsed = ConvertTo-SemanticVersion -Version $Version

    return "$($parsed.Major).$($parsed.Minor).$($parsed.Patch + 1)"
}

# 形式判定と最大値の算出はバージョンの規則（version.ps1）に委譲する
$released = @(Get-ReleasedVersions -RepoRoot $RepoRoot)
$maxVersion = Get-MaxVersion -Versions $released
$maxTag = 'なし'

if ($maxVersion) {
    $maxTag = Get-TagName -Version $maxVersion
}

$nextExample = Get-NextVersionExample -Version $maxVersion

# 系列ごとの最大（バックポートの判断に使う。新しい系列の順に並べる）
$seriesMaxima = @()

foreach ($group in ($released | Group-Object { Get-VersionSeries -Version $_ })) {
    $seriesMax = Get-MaxVersion -Versions @($group.Group)

    $seriesMaxima += @{
        Series  = $group.Name
        Version = $seriesMax
        Tag     = Get-TagName -Version $seriesMax
    }
}

$seriesMaxima = @($seriesMaxima | Sort-Object { ConvertTo-SemanticVersion -Version $_.Version } -Descending)

# ─── 2. リポジトリの状態 ───
$configPath = Join-Path $RepoRoot '.github/release-config.json'

if (-not (Test-Path -LiteralPath $configPath)) {
    throw "設定ファイルが見つかりません: $configPath"
}

$config = Get-Content $configPath -Raw | ConvertFrom-Json

$versionFile = $config.versionFile
$versionInFile = Get-VersionFileValue -VersionFile $versionFile

$latestRelease = ''

if (-not $NoNetwork) {
    $latestRelease = Get-LatestReleaseTag
}

# リリースを許可するブランチ（release-config.json の releaseBranches。未指定は main のみ）
$allowedBranches = @('main')

if ($config.PSObject.Properties['releaseBranches'] -and $config.releaseBranches) {
    $allowedBranches = @($config.releaseBranches)
}

# ─── 3. パッケージの公開状況 ───
$packageRows = @()

foreach ($package in @($config.packages)) {
    $packageId = Get-MSBuildProperty -Project $package.project -Name 'PackageId'
    $published = @()

    if (-not $NoNetwork) {
        $published = @(Get-NuGetPublishedVersions -PackageId $packageId)
    }

    $latest = '未公開'
    $all = 'なし'

    if ($published.Count -gt 0) {
        $latest = $published[$published.Count - 1]
        $all = $published -join ', '
    }

    $packageRows += @{
        Package = $packageId
        Latest  = $latest
        All     = $all
    }
}

# ─── 4. 表の作成 ───
$lines = @()
$lines += '## 現在のバージョン状況'
$lines += ''
$lines += '| 項目 | 値 |'
$lines += '| --- | --- |'
$lines += "| 実行ブランチ | $branchLabel |"
$lines += "| リリース可能なブランチ | $($allowedBranches -join ', ') |"
$lines += "| $versionFile の <Version> | $versionInFile |"
$lines += "| タグの最大 | $maxTag |"
$lines += "| 最新の GitHub Release | $(Format-Value $latestRelease) |"

# 複数の系列がある場合は、系列ごとの最大（バックポートの比較対象）を表示する
if ($seriesMaxima.Count -gt 1) {
    $lines += ''
    $lines += '| 系列 | タグの最大 |';
    $lines += '| --- | --- |'

    foreach ($row in $seriesMaxima) {
        $lines += "| $($row.Series) | $($row.Tag) |"
    }
}

$lines += ''
$lines += '| パッケージ | NuGet.org の最新公開 | NuGet.org の公開済みバージョン |'
$lines += '| --- | --- | --- |'

foreach ($row in $packageRows) {
    $lines += "| $($row.Package) | $($row.Latest) | $($row.All) |"
}

# ─── 5. version に入力する値の案内 ───
$exampleSuffix = ''

if ($nextExample) {
    $exampleSuffix = "（例: $nextExample。これより大きいバージョンなら可）"
}
else {
    # 比較対象のタグが 1 件も無い場合は、任意のバージョンを入力できる
    $exampleSuffix = '（比較対象のタグがないため、任意のバージョンを入力できます）'
}

$lines += ''
$lines += '### `version` に入力する値'
$lines += ''

if ($maxVersion) {
    $lines += "- **通常のリリース: 全タグの最大（$maxTag）より大きい**バージョン。$exampleSuffix"
}
else {
    $lines += '- **通常のリリース: 任意のバージョン**（比較対象のタグがまだありません）。'
}

$backportLine = '- **バックポート（旧系列へのリリース）: 対象系列のタグの最大より大きい**バージョン（系列も一致させる）。'

if ($seriesMaxima.Count -gt 1) {
    $backportLine += ' 系列ごとの最大は上の表を参照してください。'
}
else {
    $backportLine += ' 既存のタグと同じ系列の最大と比較されます。'
}

$lines += $backportLine
$lines += '  バックポートでは `Directory.Build.props` のバージョンは書き換えません（系列ブランチからの場合は書き換えます）。'
$lines += '- 同じ系列の中で既存以下のバージョンは指定できません。'

# 実行ブランチが許可されていない場合は注意を出す（許可ブランチは release-config.json の releaseBranches）
$branchAllowed = $false

foreach ($pattern in $allowedBranches) {
    if ($pattern -eq $branch -or ($pattern -match '[*?]' -and $branch -like $pattern)) {
        $branchAllowed = $true
        break
    }
}

if (-not $branchAllowed) {
    $lines += ''
    $lines += "> 注意: リリースは次のブランチからのみ実行できます: $($allowedBranches -join ', ')（現在のブランチ: $branchLabel）。"
}

$markdown = $lines -join "`n"

# ─── 6. 出力（標準出力と実行サマリー）───
Write-Output $markdown

if ($env:GITHUB_STEP_SUMMARY) {
    $markdown | Out-File -FilePath $env:GITHUB_STEP_SUMMARY -Append -Encoding utf8
}
