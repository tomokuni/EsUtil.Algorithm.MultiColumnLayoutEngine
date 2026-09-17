<#
バージョン（semver）に関する規則を単一所有するライブラリ。

本ファイルは関数のみを定義し、実行時に何も出力しない。
利用側はスクリプトの先頭で dot-source する（例: . "$PSScriptRoot/version.ps1"）。

バージョンの形式・比較・系列・プレリリースの判定規則は本ファイルだけが持つ。
バージョンを扱う新しいスクリプトを追加する場合も、規則を再実装せずここを参照する。
#>

<#
.SYNOPSIS
バージョン文字列を検証し、比較可能な型へ変換する。
.DESCRIPTION
semver 形式のみを許可する（数値部分の先頭 0 は不可。プレリリースは `-rc.1` のような識別子）。
比較には System.Management.Automation.SemanticVersion を使用する。
.NET の [version] 型はプレリリース（1.2.3-rc.1）を解析できず、文字列比較は 0.9.0 と 0.10.0 の大小を誤るため使用しない。
.PARAMETER Version
検証するバージョン（例: 1.2.3 / 1.2.3-rc.1）。
.OUTPUTS
System.Management.Automation.SemanticVersion
.EXAMPLE
$v = ConvertTo-SemanticVersion -Version '1.2.3-rc.1'
#>
function ConvertTo-SemanticVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Version
    )

    # 数値部分の先頭 0 を禁止し、プレリリース識別子を任意で許可する
    $pattern = '^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$'

    if ($Version -notmatch $pattern) {
        throw "バージョンの形式が不正です: '$Version'（例: 1.2.3 / 1.2.3-rc.1）"
    }

    return [System.Management.Automation.SemanticVersion]$Version
}

<#
.SYNOPSIS
バージョンがプレリリースかどうかを判定する。
.PARAMETER Version
判定するバージョン（例: 1.2.3 / 1.2.3-rc.1）。
.OUTPUTS
System.Boolean
プレリリース（`-` を含む）の場合は true。
.EXAMPLE
Test-PrereleaseVersion -Version '1.2.3-rc.1'   # -> True
#>
function Test-PrereleaseVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Version
    )

    # 形式検証を兼ねる（不正な形式はここで例外になる）
    $null = ConvertTo-SemanticVersion -Version $Version

    return $Version.Contains('-')
}

<#
.SYNOPSIS
バージョンの系列（major.minor）を取得する。
.DESCRIPTION
系列はリリースの互換性の単位であり、同じ系列の中でのみバージョンを比較する（バックポートの判定に使う）。
.PARAMETER Version
系列を取得するバージョン（例: 1.2.3）。
.OUTPUTS
System.String
`<major>.<minor>` 形式の系列（例: 1.2）。
.EXAMPLE
Get-VersionSeries -Version '1.2.3'   # -> 1.2
#>
function Get-VersionSeries {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Version
    )

    $parsed = ConvertTo-SemanticVersion -Version $Version

    return "$($parsed.Major).$($parsed.Minor)"
}

<#
.SYNOPSIS
バージョンの一覧から最大のものを取得する。
.DESCRIPTION
Git の並び替え（git tag --sort=-v:refname）は semver の優先順位に従わない
（v1.2.3-rc.1 を v1.2.3 より上位に置く）ため、最大値の算出は本関数で行う。
.PARAMETER Versions
比較するバージョンの一覧（`v` を除いた semver 文字列。null 可）。
.OUTPUTS
System.String
最大のバージョン。一覧が空の場合は $null。
.EXAMPLE
Get-MaxVersion -Versions @('1.0.0', '1.2.0', '1.1.5')   # -> 1.2.0
#>
function Get-MaxVersion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]$Versions
    )

    $max = $null

    foreach ($version in $Versions) {
        $parsed = ConvertTo-SemanticVersion -Version $version
        if ($null -eq $max -or $parsed -gt $max) {
            $max = $parsed
        }
    }

    if ($null -eq $max) {
        return $null
    }

    return $max.ToString()
}

<#
.SYNOPSIS
バージョンが比較対象より大きいかどうかを判定する。
.PARAMETER Version
判定するバージョン。
.PARAMETER Than
比較対象のバージョン。
.OUTPUTS
System.Boolean
Version が Than より大きい場合は true（同値は false）。
.EXAMPLE
Test-VersionGreater -Version '1.2.4' -Than '1.2.3'   # -> True
#>
function Test-VersionGreater {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Version,

        [Parameter(Mandatory)]
        [string]$Than
    )

    $left = ConvertTo-SemanticVersion -Version $Version
    $right = ConvertTo-SemanticVersion -Version $Than

    return $left -gt $right
}

<#
.SYNOPSIS
バージョンに対応するタグ名を取得する。
.PARAMETER Version
タグ名を取得するバージョン（例: 1.2.3）。
.OUTPUTS
System.String
`v<version>` 形式のタグ名（例: v1.2.3）。
.EXAMPLE
Get-TagName -Version '1.2.3'   # -> v1.2.3
#>
function Get-TagName {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Version
    )

    return "v$Version"
}

<#
.SYNOPSIS
リポジトリのタグのうち、リリース済みとみなせるバージョンを 1 件ずつ出力する。
.DESCRIPTION
`v` で始まるタグを対象とし、形式の判定はバージョンの規則（ConvertTo-SemanticVersion）に委譲する。
`v1.2` のような semver でないタグは対象外とする。

呼び出し側は `@(Get-ReleasedVersions -RepoRoot $RepoRoot)` として配列で受け取る
（0 件の場合は空の配列になる。パイプラインへ 1 件ずつ出力するため、複数件でも展開されない）。
.PARAMETER RepoRoot
リポジトリのルート。省略時はカレントディレクトリ。
.OUTPUTS
System.String
`v` を除いた semver 文字列（例: 1.2.3）。
.EXAMPLE
$released = @(Get-ReleasedVersions -RepoRoot $RepoRoot)
#>
function Get-ReleasedVersions {
    [CmdletBinding()]
    param(
        [string]$RepoRoot
    )

    $tags = @()

    if ($RepoRoot) {
        $tags = @(& git -C $RepoRoot tag -l 'v*')
    }
    else {
        $tags = @(& git tag -l 'v*')
    }

    foreach ($name in ($tags | Where-Object { $_ })) {
        try {
            # 形式が不正なタグ（v1.2 など）は対象外とする
            $null = ConvertTo-SemanticVersion -Version $name.Substring(1)
            Write-Output $name.Substring(1)
        }
        catch {
            continue
        }
    }
}
