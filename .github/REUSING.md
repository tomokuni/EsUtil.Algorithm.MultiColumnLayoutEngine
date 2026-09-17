# 他のリポジトリへの流用方法

本ドキュメントは、本リポジトリの**ビルド・テスト・リリースの仕組みを別のリポジトリへ展開する**開発者向けの情報です。
構成と使い方は [`RELEASE.md`](RELEASE.md) を参照してください。

## 何が流用できるか

リポジトリ固有の設定は `release-config.json` にあります。それ以外のファイルは汎用です。

| ファイル | 流用 | 備考 |
| --- | --- | --- |
| [`release-config.json`](release-config.json) | **コピーして編集** | リポジトリ固有設定 |
| [`scripts/version.ps1`](scripts/version.ps1) | **そのまま** | バージョンの規則 |
| [`scripts/set-version.ps1`](scripts/set-version.ps1) | **そのまま** | バージョンファイルのパスは引数・設定で渡す |
| [`scripts/verify-release-version.ps1`](scripts/verify-release-version.ps1) | **そのまま** | ブランチ規則も共通 |
| [`workflows/publish.yml`](workflows/publish.yml) | **そのまま** | 設定を読んで pack し、NuGet.org / GitHub Packages へ公開する |
| [`workflows/release.yml`](workflows/release.yml) | **そのまま** | 設定を読むため変更不要 |
| [`workflows/build.yml`](workflows/build.yml) | **コピーして編集** | ビルド・テストのコマンドのみリポジトリ依存 |
| 本ドキュメント・`RELEASE.md` | コピーして調整 | |

## 手順

### 1. ファイルをコピーする

```text
<新しいリポジトリ>/
├── Directory.Build.props     # リリースバージョン（新規作成。下記「2.」の versionFile が指す）
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
    <!-- リリースバージョン。各 .csproj では指定しない。 -->
    <!-- 自動インクリメントは行わない。リリース時に release.yml が入力値へ更新する。 -->
    <Version>0.0.1</Version>
  </PropertyGroup>

</Project>
```

既存の共通プロパティ（`Nullable` / `ImplicitUsings` / `LangVersion` など）をここへまとめてもかまいません。

### 2. `release-config.json` を編集する

| キー | 内容 | 例 |
| --- | --- | --- |
| `product` | リリース名とアセットのタイトルに使う表示名 | `"EsUtil.Algorithm.MultiColumnLayoutEngine"` |
| `versionFile` | バージョン（`<Version>`）を記載するファイル（リポジトリルートからの相対パス） | `"Directory.Build.props"` |
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

**パッケージを増やす場合はこの配列に要素を追加します。** ワークフローとスクリプトの変更は不要です。

> **注意**: バージョンはリポジトリルートの `Directory.Build.props` に `<Version>` として記載し、
> 各 `.csproj` には記載しないでください（全プロジェクトが同じ値を継承します）。複数プロジェクトで共有する場合も同じ構成にします。

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

初回のリリース前に、nuget.org へポリシーを登録します。手順は `RELEASE.md` の
「NuGet.org の Trusted Publishing ポリシーの登録手順」を参照してください。

- **Workflow File には `publish.yml` を指定します**（OIDC トークンを要求するワークフローのファイル名）。
- **Glob Patterns and Packages には、公開するパッケージ ID を指定します**（例: `EsUtil.Algorithm.MultiColumnLayoutEngine`）。
  1 行に 1 つ入力し、パッケージを増やしたら行を追記してください（`*` を使った glob も指定できます）。
- **Policy Name は任意**です（UI では必須入力・64 文字以内。照合には使われない識別用の名前）。
  未入力の場合は `publish.yml` から `publish` が自動設定されます。
- 登録前に `.github/workflows/publish.yml` をリポジトリへ push しておいてください（ポリシーはファイル名で検証されます）。
- **Repository / Workflow File にはワイルドカードが使えません**（`repository` / `job_workflow_ref` クレームと完全一致のため、
  ポリシーはリポジトリごと・ワークフロー ファイルごとに 1 つ必要）。ワークフロー ファイル名を `publish.yml` に揃えておくと、
  リポジトリを増やすときに大きく変わるのは Repository 名だけになります。
- **`publish.yml` は呼び出し元と同じリポジトリに置いてください。** nuget.org は `job_workflow_ref` の
  プレフィックスが `{owner}/{repo}/.github/workflows/` であることも検証するため、共通リポジトリに置いた
  再利用ワークフローを他リポジトリから呼ぶ方式は使えません（コピーして各リポジトリへ配置します）。
  詳細は `RELEASE.md` の「別リポジトリのパッケージを公開する場合」を参照してください。

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
| **配布物がアプリ（複数の UI など）の場合** | `packages[]` を `uis[]`（名前・スクリプト・出力・配布名）へ置き換え、pack ステップを配布用スクリプトの実行に変えます。`release.yml` は保管された成果物をそのまま添付するため変更不要です |
| **GitHub Packages へ公開しない** | `publish.yml` の `push` ジョブから該当ステップを削除し、`packages: write` 権限を外します（呼び出し元 `release.yml` の権限も合わせて外します） |

## 変更時の注意事項

- **パッケージの定義（`packages[]`）は `release-config.json` に置いてください。** ワークフローへ書き戻すと二重管理になり、追加時に漏れます。
- **バージョンの規則（形式・比較・系列）は `scripts/version.ps1` に置いてください。** 他のスクリプトで再実装すると判定がずれます。
- **バージョンを記載するファイルは 1 つにしてください**（本リポジトリは `Directory.Build.props`）。番号と成果物が不一致になるのを防ぎます。
- **取り消せない外部公開（NuGet.org / GitHub Packages）は、バージョンコミットとタグ作成より前に実行してください。**
  NuGet は同じバージョンを再利用できないため、公開に失敗したときにタグとバージョンを消費しないようにします。
- **タグは `gh release create --target` に作成させてください**（公開が成功した後にのみタグが作られます）。
- **外部公開は `--skip-duplicate` で冪等にしてください**（失敗後の再実行で未完了分だけが進みます）。
- **OIDC トークンの要求元は `publish.yml` に固定してください**（nuget.org の Trusted Publishing ポリシーがファイル名で検証します）。
