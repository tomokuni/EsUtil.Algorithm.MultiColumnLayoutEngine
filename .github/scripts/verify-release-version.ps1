<#
.SYNOPSIS
リリースしようとしているバージョンの妥当性を検証し、リリース情報を返す。
.DESCRIPTION
次の順で検証する。1 つでも満たさない場合は例外で失敗する。
  1. バージョンの形式（semver。規則は version.ps1 に委譲する）
  2. 実行ブランチ（main または release/<major>.<minor>）
  3. 系列と単調性（既存タグより大きいこと）
     - main         : 全タグの最大より大きいこと
     - release/X.Y  : 入力の系列が X.Y に一致し、vX.Y.* の最大より大きいこと
  4. タグ v<version> が未作成であること（同値のリリースを防ぐ）

既存タグの比較はバージョンの規則を単一所有する version.ps1 に委譲する
（git tag --sort は semver の優先順位に従わないため、最大値の算出はスクリプト側で行う）。
.PARAMETER Version
リリースするバージョン（例: 1.2.3 / 1.2.3-rc.1）。
.PARAMETER Branch
実行ブランチ名（例: main / release/1.2）。
.OUTPUTS
System.String
リリース情報を表す JSON（version, tag, prerelease, notesStartTag）。
.EXAMPLE
$info = & ./.github/scripts/verify-release-version.ps1 -Version 1.2.0 -Branch main | ConvertFrom-Json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [Parameter(Mandatory)]
    [string]$Branch
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/version.ps1"

# 形式の検証と系列の取得はバージョンの規則（version.ps1）に従う
$null = ConvertTo-SemanticVersion -Version $Version
$tag = Get-TagName -Version $Version
$series = Get-VersionSeries -Version $Version

# ─── 既存タグの収集（v で始まる semver 形式のタグのみを対象にする）───
$released = @()

foreach ($name in (git tag -l 'v*')) {
    try {
        # 形式が不正なタグ（v1.2 など）は対象外とする
        $null = ConvertTo-SemanticVersion -Version $name.Substring(1)
        $released += $name.Substring(1)
    }
    catch {
        continue
    }
}

# ─── 系列の決定と単調性の検証 ───
if ($Branch -eq 'main') {
    # 系列をまたぐ逆戻り（v2.0.0 があるのに 1.3.0 をリリースする等）を防ぐ
    $scope = $released
    $scopeLabel = '全タグ'
}
elseif ($Branch -match '^release/(?<series>\d+\.\d+)$') {
    $expected = $Matches['series']

    if ($series -ne $expected) {
        throw "ブランチ $Branch では $expected.x 系のバージョンのみリリースできます（入力: $Version）"
    }

    $scope = @($released | Where-Object { $_ -match "^$([regex]::Escape($expected))\.\d+(-.*)?$" })
    $scopeLabel = "ブランチ $Branch の系列（v$expected.*）"
}
else {
    throw "リリースは main または release/<major>.<minor> ブランチからのみ実行できます（現在: $Branch）"
}

$max = Get-MaxVersion -Versions $scope

if ($max -and -not (Test-VersionGreater -Version $Version -Than $max)) {
    throw "入力 $Version は $scopeLabel の最大 $max 以下です。より大きいバージョンを指定してください。"
}

# ─── タグの未作成確認 ───
if (git tag -l $tag) {
    throw "タグ $tag は既に存在します。"
}

# リリースノートは「その系列の前回リリース」からの差分で生成する
$notesStartTag = if ($max) { Get-TagName -Version $max } else { $null }

Write-Host "検証OK: version=$Version tag=$tag branch=$Branch"
Write-Host "  比較対象: $scopeLabel / 既存最大: $(if ($max) { $max } else { 'なし' })"

# 呼び出し側が取り出せるよう、結果のみを JSON で出力する
Write-Output (@{
        version       = $Version
        tag           = $tag
        prerelease    = (Test-PrereleaseVersion -Version $Version)
        notesStartTag = $notesStartTag
    } | ConvertTo-Json -Compress)
