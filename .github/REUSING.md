# 他のリポジトリへの流用方法

本ドキュメントは、本リポジトリの**ビルド・テスト・リリースの仕組みを別のリポジトリへ展開する**開発者向けの情報です。
仕組みの設計と使い方は [`RELEASE.md`](RELEASE.md) を参照してください。

## 何が流用できるか

**リポジトリ固有の知識を `release-config.json` に閉じ込めています。** それ以外は汎用です。

| ファイル | 流用 | 備考 |
| --- | --- | --- |
| [`release-config.json`](release-config.json) | **コピーして編集** | 唯一のリポジトリ固有設定 |
| [`scripts/version.ps1`](scripts/version.ps1) | **そのまま** | バージョンの規則（リポジトリ非依存） |
| [`scripts/set-version.ps1`](scripts/set-version.ps1) | **そのまま** | バージョンファイルのパスは引数・設定で渡す |
| [`scripts/verify-release-version.ps1`](scripts/verify-release-version.ps1) | **そのまま** | ブランチ規則も汎用 |
| [`workflows/publish.yml`](workflows/publish.yml) | **そのまま** | 設定を読んで pack し、NuGet.org / GitHub Packages へ公開する |
| [`workflows/release.yml`](workflows/release.yml) | **そのまま** | 設定を読むため変更不要 |
| [`workflows/build.yml`](workflows/build.yml) | **コピーして編集** | ビルド・テストのコマンドのみリポジトリ依存 |
| 本ドキュメント・`RELEASE.md` | コピーして調整 | |

## アプリ配布向けの仕組みとの対応

本リポジトリの仕組みは、アプリ（複数の UI を配布するリポジトリ）向けに作られた仕組みを
**NuGet ライブラリ向けに合わせた**ものです。元の仕組みとの対応は次のとおりです。

| 元の仕組み（アプリ） | 本リポジトリ（ライブラリ） |
| --- | --- |
| `release-config.json` の `uis[]`（UI の定義） | `release-config.json` の `packages[]`（パッケージの定義） |
| `buildScript/*_publish_*.bat` を実行して配布物を作る | `dotnet pack` で nupkg を作る |
| `publish.yml`（配布物の作成・保管） | `publish.yml`（pack・保管・**NuGet.org / GitHub Packages への公開**） |
| `release.yml` が UI の成果物を Release へ添付 | `release.yml` が nupkg を Release へ添付 |
| バージョン更新のみのコミットでは publish をスキップ | スキップしない（pack のコストが小さく、公開物と版の一致を常に検証できるため） |

## 手順

### 1. ファイルをコピーする

```text
<新しいリポジトリ>/
├── Directory.Build.props     # バージョンの単一所有元（新規作成。下記「2.」の versionFile が指す）
└── .github/
    ├── release-config.json
    ├── RELEASE.md
    ├── REUSING.md
    ├── scripts/
    │   ├── version.ps1
    │   ├── set-version.ps1
    │   └── verify-release-version.ps1
    └── workflows/
        ├── build.yml
        ├── publish.yml
        └── release.yml
```

`Directory.Build.props` はリポジトリルートに置く最小構成でかまいません。

```xml
<Project>

  <PropertyGroup>
    <!-- リリースバージョン。全プロジェクトで単一所有し、各 .csproj では指定しない。 -->
    <!-- 自動インクリメントは行わない。リリース時に release.yml が入力値へ更新する。 -->
    <Version>0.0.1</Version>
  </PropertyGroup>

</Project>
```

既存の共通プロパティ（`Nullable` / `ImplicitUsings` / `LangVersion` など）をここへ集約してもかまいません。

### 2. `release-config.json` を編集する

| キー | 内容 | 例 |
| --- | --- | --- |
| `product` | リリース名とアセットのタイトルに使う表示名 | `"EsUtil.Algorithm.MultiColumnLayoutEngine"` |
| `versionFile` | バージョンを所有するファイル（リポジトリルートからの相対パス） | `"Directory.Build.props"` |
| `gateWorkflow` | リリースの前提（ゲート）となるワークフローのファイル名 | `"build.yml"` |
| `artifactRetentionDays` | アーティファクトの保持日数 | `30` |
| `releaseNotes` | Release 本文の冒頭に付ける説明 | `"..."` |
| `nuget.user` | nuget.org のプロファイル名（メールアドレスではない） | `"SEKIYA.Tomokuni"` |
| `nuget.source` | NuGet の公開先 | `"https://api.nuget.org/v3/index.json"` |
| `packages[]` | 公開するパッケージの定義（下記） | — |

`packages[]` の各要素:

| キー | 内容 |
| --- | --- |
| `name` | アーティファクト名（ジョブの表示にも使う） |
| `project` | pack するプロジェクト（リポジトリルートからの相対パス） |

**パッケージを増やす場合はこの配列に要素を追加するだけです。** ワークフローとスクリプトの変更は不要です。

> **バージョンは 1 ファイルが単一所有する構成にしてください。** 本リポジトリはリポジトリルートの
> `Directory.Build.props` に `<Version>` を置き、各 `.csproj` では指定しません（全プロジェクトが継承します）。
> 複数プロジェクトでバージョンを共有する場合もこの形が使えます。

### 3. `build.yml` を編集する

`build` ジョブのコマンドのみ、リポジトリに合わせて変更します。

```yaml
      - name: 復元
        run: dotnet restore <ソリューション>.slnx

      - name: ビルド（Release）
        run: dotnet build <ソリューション>.slnx -c Release --no-restore

      - name: テスト（Release）
        run: dotnet test <ソリューション>.slnx -c Release --no-build
```

- `.slnx` を読むには新しい SDK（9 以降）が必要です。`.sln` や個別の csproj を使う場合は `dotnet-version` も合わせて変更してください。
- テストが無い・不要な場合はテストのステップを削除します。
- **`GeneratePackageOnBuild` は使わないでください。** これを有効にすると、クリーンな状態の `dotnet pack` が
  `NU5026`（パックする dll が見つからない）で失敗します。`build` でビルドしてから
  `pack --no-build` を実行する形にしてください（本リポジトリの `build.yml` / `publish.yml` が参考になります）。

### 4. NuGet.org の Trusted Publishing ポリシーを登録する

`RELEASE.md` の「公開先とその設定」を参照してください。**Workflow File には `publish.yml` を指定します。**

### 5. 動作を確認する

```powershell
# スクリプトの単体確認（バージョン設定・検証。既定のバージョンファイルは Directory.Build.props）
Copy-Item Directory.Build.props "$env:TEMP/dbp.bak"
& ./.github/scripts/set-version.ps1 -Version 1.0.1
& ./.github/scripts/verify-release-version.ps1 -Version 1.0.1 -Branch main
Copy-Item "$env:TEMP/dbp.bak" Directory.Build.props

# パイプラインの確認（CI と同一条件）
git clean -xdf -- src test
dotnet restore MultiColumnLayoutEngine.slnx
dotnet build MultiColumnLayoutEngine.slnx -c Release --no-restore
dotnet test MultiColumnLayoutEngine.slnx -c Release --no-build
```

その後、`main` へ push して Build が成功することを確認し、`Actions` → `Release` を手動実行します。

## 流用時に必要になる可能性がある変更

| 状況 | 対応 |
| --- | --- |
| **リポジトリが private** | Actions の分数が有料になります。毎 push のビルドとテストは実行時間が長いため、`build.yml` の `on.push` に `paths` を追加して対象を限定することを検討してください |
| **NuGet ギャラリー未公開のパッケージを参照する** | クリーンな CI からは復元できません。リポジトリへ同梱し `NuGet.config` のソースに追加するか、公開してください |
| **複数系列の保守（バックポート）が不要** | `verify-release-version.ps1` のブランチ分岐（`release/X.Y`）はそのままでも害はありませんが、`release/**` のトリガーを `build.yml` から外しても構いません |
| **バージョンを自動で決めたい** | 本仕組みは「人が入力する」前提です。自動化（Conventional Commits からの算出など）を併用する場合は、`verify-release-version.ps1` の検証はそのまま活かせます |
| **アプリ（配布物が複数）へ展開する** | `packages[]` を `uis[]`（名前・スクリプト・出力・配布名）へ置き換え、pack ステップを `buildScript/*_publish_*.bat` の実行に変えます。`release.yml` は保管された成果物をそのまま添付するため変更不要です |
| **GitHub Packages へ公開しない** | `publish.yml` の `push` ジョブから該当ステップを削除し、`packages: write` 権限を外します（呼び出し元 `release.yml` の権限も合わせて外します） |

## 設計上の約束（変更時に守ること）

流用先でも同じ品質を保つため、次の関係を崩さないでください。

| # | 約束 | 理由 |
| --- | --- | --- |
| 1 | **パッケージの定義は `release-config.json` だけに置く** | ワークフローへ書き戻すと二重管理になり、追加時に漏れる |
| 2 | **バージョンの規則は `version.ps1` だけに置く** | 正規表現や比較を各所に書くと、判定がずれる |
| 3 | **バージョンの単一所有元は 1 ファイル** | 番号と成果物の不一致を防ぐ |
| 4 | **取り消せない外部公開を、バージョンコミットとタグ作成より前に実行する** | 失敗時にタグとバージョンを消費しないため（NuGet は同じバージョンを再利用できない） |
| 5 | **タグは `gh release create --target` に作成させる** | 公開が成功した後に初めてタグができる |
| 6 | **外部公開は `--skip-duplicate` で冪等にする** | 失敗後の再実行で、未完了分だけを進められる |
| 7 | **OIDC トークンの要求元は `publish.yml` に固定する** | nuget.org の Trusted Publishing ポリシーがファイル名で検証するため |
