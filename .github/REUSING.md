# 他のリポジトリへの流用方法

本ドキュメントは、本リポジトリの**ビルド・テスト・リリースの仕組みを別のリポジトリへ展開する**開発者向けの情報です。
構成と使い方は [`RELEASE.md`](RELEASE.md) を参照してください。

## 何が流用できるか

リポジトリ固有の設定は `release-config.json` にあります。それ以外のファイルは汎用です
（`workflows/`・`scripts/`・`actions/` は**全リポジトリで同一のファイル**です）。

| ファイル | 流用 | 備考 |
| --- | --- | --- |
| [`release-config.json`](release-config.json) | **コピーして編集** | リポジトリ固有設定 |
| [`actions/read-config`](actions/read-config/action.yml) | **そのまま** | `release-config.json` を読んで各ワークフローへ渡す共通アクション |
| [`../global.json`](../global.json) | **コピーして編集** | .NET SDK のバージョン（対象フレームワークに合わせる） |
| [`scripts/version.ps1`](scripts/version.ps1) | **そのまま** | バージョンの規則 |
| [`scripts/set-version.ps1`](scripts/set-version.ps1) | **そのまま** | バージョンファイルのパスは引数・設定で渡す |
| [`scripts/verify-release-version.ps1`](scripts/verify-release-version.ps1) | **そのまま** | ブランチ規則も共通 |
| [`scripts/show-current-versions.ps1`](scripts/show-current-versions.ps1) | **そのまま** | 現在のバージョン状況を表示する（読み取りのみ） |
| [`dependabot.yml`](dependabot.yml) | **コピーして調整** | GitHub Actions / NuGet の更新 PR。`directories` を対象プロジェクトに合わせる |
| [`rulesets/tag-version.json`](rulesets/tag-version.json) | **コピー（編集不要）** | Tag ruleset の定義。リポジトリの Settings へインポートする |
| [`workflows/publish.yml`](workflows/publish.yml) | **そのまま** | 設定を読んで pack し、NuGet.org / GitHub Packages へ公開する |
| [`workflows/release.yml`](workflows/release.yml) | **そのまま** | 設定を読むため変更不要 |
| [`workflows/build.yml`](workflows/build.yml) | **そのまま** | ビルド・テスト・pack の対象は `solutionFile` が持つため変更不要 |
| 本ドキュメント・`RELEASE.md` | コピーして調整 | |

## 手順

### 1. ファイルをコピーする

```text
<新しいリポジトリ>/
├── Directory.Build.props     # リリースバージョン（新規作成。下記「2.」の versionFile が指す）
├── global.json               # .NET SDK のバージョン（新規作成。ワークフローでは指定しない）
└── .github/
    ├── release-config.json
    ├── actions/
    │   └── read-config/
    │       └── action.yml
    ├── dependabot.yml
    ├── RELEASE.md
    ├── REUSING.md
    ├── rulesets/
    │   └── tag-version.json
    ├── scripts/
    │   ├── version.ps1
    │   ├── set-version.ps1
    │   ├── verify-release-version.ps1
    │   └── show-current-versions.ps1
    └── workflows/
        ├── build.yml
        ├── publish.yml
        └── release.yml
```

`Directory.Build.props` はリポジトリルートに置く最小構成でかまいません。

```xml
<Project>

  <PropertyGroup>
    <!-- リリースバージョン。各 .csproj では指定しない。 -->
    <!-- 自動インクリメントは行わない。リリース時に release.yml が入力値へ更新する。 -->
    <Version>0.0.1</Version>
  </PropertyGroup>

</Project>
```

既存の共通プロパティ（`Nullable` / `ImplicitUsings` / `LangVersion` など）をここへまとめてもかまいません。

`global.json` には対象フレームワークに合う SDK を記載します（ワークフローには書きません）。

```json
{
  "sdk": {
    "version": "10.0.100",
    "rollForward": "latestFeature"
  }
}
```

### 2. `release-config.json` を編集する

| キー | 内容 | 例 |
| --- | --- | --- |
| `product` | リリース名とアセットのタイトルに使う表示名 | `"EsUtil.Algorithm.MultiColumnLayoutEngine"` |
| `versionFile` | バージョン（`<Version>`）を記載するファイル（リポジトリルートからの相対パス） | `"Directory.Build.props"` |
| `solutionFile` | ビルド・テスト・pack の対象となるソリューション ファイル（リポジトリルートからの相対パス） | `"EsUtil.Other.Library.slnx"` |
| `gateWorkflow` | リリースの前提（ゲート）となるワークフローのファイル名 | `"build.yml"` |
| `releaseBranches` | **リリースを許可するブランチ**（完全一致とワイルドカード。未指定は `main` のみ。`release/` 配下は `release/<major>.<minor>` の形式に限定される） | `["main"]` / `["main", "release/**"]` |
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

**パッケージを増やす場合はこの配列に要素を追加します。** ワークフローとスクリプトの変更は不要です。

設定の値は [`actions/read-config`](actions/read-config/action.yml) がまとめて読み取ります。
**キーを追加する場合は、同アクションの `outputs` にも追加してください**（追加しないとワークフローから参照できません）。
**必須のキー（`product` / `versionFile` / `solutionFile` / `gateWorkflow` / `packages`）が空の場合、読み取り時に失敗します**
（ワークフロー側で原因の分かりにくいエラーになるのを防ぐため）。

> **注意**: バージョンはリポジトリルートの `Directory.Build.props` に `<Version>` として記載し、
> 各 `.csproj` には記載しないでください（全プロジェクトが同じ値を継承します）。複数プロジェクトで共有する場合も同じ構成にします。

### 3. `release-config.json` の `solutionFile` を設定する

`release-config.json` の `solutionFile` に、ビルド・テスト・pack の対象となるソリューション ファイル
（リポジトリルートからの相対パス）を記載します。**ワークフローの編集は不要です。**

```json
  "solutionFile": "EsUtil.Other.Library.slnx",
```

`workflows/build.yml`・`workflows/publish.yml`・`workflows/release.yml` は**全リポジトリで同一のファイル**です
（リポジトリ名・ソリューション名を含まないため、そのままコピーして使用できます）。

- **SDK のバージョンは `global.json` に記載します**（ワークフローへ `dotnet-version` を書きません。`actions/setup-dotnet` が `global.json` を読むため、記載箇所を 1 つにできます）。
  `actions/setup-dotnet` の `cache: true` と `cache-dependency-path: '**/*.csproj'` で NuGet のグローバル パッケージ フォルダがキャッシュされます。
- `.slnx` を読むには新しい SDK（9 以降）が必要です。`.sln` や個別の csproj を使う場合は `global.json` のバージョンも合わせて変更してください。
- テストが無い・不要な場合はテストのステップを削除し、配布するパッケージが無い場合は pack のステップを削除します。
- **テストプロジェクトには `<IsPackable>false</IsPackable>` を設定してください。** 設定しないと、ソリューション単位の `dotnet pack` がテストの nupkg も作成します。
- **`GeneratePackageOnBuild` は使わないでください。** これを有効にすると、クリーンな状態の `dotnet pack` が
  `NU5026`（パックする dll が見つからない）で失敗します。`build` でビルドしてから
  `pack --no-build` を実行する形にしてください（本リポジトリの `build.yml` / `publish.yml` が参考になります）。

### 4. NuGet.org の Trusted Publishing ポリシーを登録する

初回のリリース前に、nuget.org へポリシーを登録します。手順は `RELEASE.md` の
「NuGet.org の Trusted Publishing ポリシーの登録手順」を参照してください。

流用時に変えるのは次の 2 つです。

| 入力項目 | 指定値 |
| --- | --- |
| Repository Owner / Repository | 新しいリポジトリの値（例: `tomokuni` / `EsUtil.Algorithm.MultiColumnLayoutEngine`） |
| Glob Patterns and Packages | 公開するパッケージ ID（1 行に 1 つ。例: `EsUtil.Algorithm.MultiColumnLayoutEngine`） |

**注意事項**:

- **Workflow File は `publish.yml` のままにしてください**（全リポジトリでファイル名を揃えると、流用時の差分が Repository だけになります）。
- **`publish.yml` は呼び出し元と同じリポジトリに置いてください**（nuget.org が `job_workflow_ref` のプレフィックスも検証するため、共通リポジトリに置いた再利用ワークフローを他リポジトリから呼ぶ方式は使えません）。

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
| **旧系列のコードでリリースしたい（系列ブランチを使うバックポート）** | `release/<major>.<minor>` ブランチを作り、修正を cherry-pick して push します（`.github` と `Directory.Build.props` も含める）。`releaseBranches` に `"release/**"` があれば追加設定は不要です（本リポジトリは設定済み。未指定の場合は `releaseBranches` へ追加）。系列ブランチでは入力バージョンの系列とブランチ名が一致していることも検証されます |
| **バージョンを自動で決めたい** | 本仕組みは「人が入力する」前提です。自動化（Conventional Commits からの算出など）を併用する場合は、`verify-release-version.ps1` の検証はそのまま活かせます |
| **配布物がアプリ（複数の UI など）の場合** | `packages[]` を `uis[]`（名前・スクリプト・出力・配布名）へ置き換え、pack ステップを配布用スクリプトの実行に変えます。`release.yml` は保管された成果物をそのまま添付するため変更不要です |
| **GitHub Packages へ公開しない** | `publish.yml` の `push` ジョブから該当ステップを削除し、`packages: write` 権限を外します（呼び出し元 `release.yml` の権限も合わせて外します） |
| **タグの保護（Tag ruleset）を入れる** | `rulesets/tag-version.json` をそのまま使えます（`v*` の作成・更新・削除を禁止し、bypass に GitHub Actions を指定）。Settings → Rules → Rulesets → New ruleset → **Import a ruleset** で読み込んでください。手順は `RELEASE.md` の「リポジトリの設定（初回のみ）」を参照 |
| **ビルドをもっと速くしたい** | `.NET SDK` は**ランナー イメージの最新版**をそのまま使います（`actions/setup-dotnet` を使わない）。イメージの SDK は PATH が通っており、`global.json` は `rollForward: latestFeature` のためイメージの最新 SDK で要件を満たします。`setup-dotnet` を使うと「チャネルの最新」を取得しようとしてイメージに無い版を毎回ダウンロードします。キャッシュは NuGet の復元のみを対象にします |

## 変更時の注意事項

- **パッケージの定義（`packages[]`）は `release-config.json` に置いてください。** ワークフローへ書き戻すと二重管理になり、追加時に漏れます。
- **ビルド対象のソリューション ファイル（`solutionFile`）は `release-config.json` に置いてください。** ワークフローへソリューション名を書くと、リポジトリごとにワークフローが分かれます（`build.yml` / `publish.yml` / `release.yml` は全リポジトリで同一に保ちます）。
- **設定の読み取りは `actions/read-config` に置いてください。** ワークフローごとに `jq` などで読み直すと、キーを追加したときに読み取り漏れが起きます。
- **.NET SDK のバージョンは `global.json` に置いてください。** ワークフローへ `dotnet-version` を書くと二重管理になり、更新時にずれます。SDK はランナー イメージの最新版を使うため `actions/setup-dotnet` は使いません（イメージに無い版を毎回ダウンロードしてしまうため）。
- **バージョンの規則（形式・比較・系列・タグの列挙）は `scripts/version.ps1` に置いてください。** 他のスクリプトで再実装すると判定がずれます。
- **バージョンを記載するファイルは 1 つにしてください**（本リポジトリは `Directory.Build.props`）。番号と成果物が不一致になるのを防ぎます。
- **取り消せない外部公開（NuGet.org / GitHub Packages）は、バージョンコミットとタグ作成より前に実行してください。**
  NuGet は同じバージョンを再利用できないため、公開に失敗したときにタグとバージョンを消費しないようにします。
- **タグは `gh release create --target` に作成させてください**（公開が成功した後にのみタグが作られます）。
- **外部公開は `--skip-duplicate` で冪等にしてください**（失敗後の再実行で未完了分だけが進みます）。
- **OIDC トークンの要求元は `publish.yml` に固定してください**（nuget.org の Trusted Publishing ポリシーがファイル名で検証します）。
