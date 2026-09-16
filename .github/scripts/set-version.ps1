<#
.SYNOPSIS
指定されたバージョン番号をバージョンファイルへ設定する。
.DESCRIPTION
バージョンの単一所有元（既定: Directory.Build.props の <Version>）の 1 行を書き換える。
改行コードと BOM の有無を変えないよう、テキストをそのまま読み書きする。
既に同じ値が設定されている場合は何もせず成功する（リトライ時にコミットが失敗しないようにするため）。
形式の検証はバージョンの規則を単一所有する version.ps1 に委譲する。
.PARAMETER Version
設定するバージョン（例: 1.2.3 / 1.2.3-rc.1）。
.PARAMETER VersionFile
バージョンを所有するファイル（リポジトリルートからの相対パス）。
.PARAMETER RepoRoot
リポジトリのルート。既定は本スクリプトの 2 つ上の階層。
.OUTPUTS
System.String
設定後のバージョン文字列。
.EXAMPLE
$version = & ./.github/scripts/set-version.ps1 -Version 1.2.3
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$VersionFile = 'Directory.Build.props',

    [string]$RepoRoot
)

$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/version.ps1"

# 形式はバージョンの規則（version.ps1）に従う
$null = ConvertTo-SemanticVersion -Version $Version

if (-not $RepoRoot) {
    # 本スクリプトは <リポジトリルート>/.github/scripts に置かれている
    $RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
}

$filePath = Join-Path $RepoRoot $VersionFile

if (-not (Test-Path -LiteralPath $filePath)) {
    throw "バージョンファイルが見つかりません: $filePath"
}

$text = [System.IO.File]::ReadAllText($filePath, [System.Text.Encoding]::UTF8)
$match = [regex]::Match($text, '<Version>[^<]*</Version>')

if (-not $match.Success) {
    throw "バージョンファイルに <Version>...</Version> がありません: $filePath"
}

$element = "<Version>$Version</Version>"

if ($match.Value -eq $element) {
    Write-Host "バージョンは既に設定済みです: $element"
    Write-Output $Version
    return
}

$updated = $text.Substring(0, $match.Index) + $element + $text.Substring($match.Index + $match.Length)

[System.IO.File]::WriteAllText($filePath, $updated, [System.Text.UTF8Encoding]::new($false))

Write-Host "バージョンを設定しました: $($match.Value) -> $element（$VersionFile）"

# 呼び出し側が取り出せるよう、バージョン文字列のみを出力する
Write-Output $Version
