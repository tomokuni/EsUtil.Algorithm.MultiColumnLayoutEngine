<!-- markdownlint-disable MD041 -- バッジを先頭に配置するため -->
[![release](https://img.shields.io/github/v/release/tomokuni/EsUtil.Algorithm.MultiColumnLayoutEngine?label=release)](https://github.com/tomokuni/EsUtil.Algorithm.MultiColumnLayoutEngine/releases)
[![nuget](https://img.shields.io/nuget/v/EsUtil.Algorithm.MultiColumnLayoutEngine?label=nuget)](https://www.nuget.org/packages/EsUtil.Algorithm.MultiColumnLayoutEngine)
[![build](https://github.com/tomokuni/EsUtil.Algorithm.MultiColumnLayoutEngine/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/tomokuni/EsUtil.Algorithm.MultiColumnLayoutEngine/actions/workflows/build.yml)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-0078D4)](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)

# VerticalMultiColumnLayout

## 概要

`MultiColumnLayoutEngine` は、複数のアイテムを複数列に配置し、**最大列高さ（MinHeight）を最小化**するレイアウト最適化ライブラリです。

Web UI、グリッドレイアウト、カード配置など、複雑なマルチカラムレイアウトが必要なアプリケーションに最適です。

---

## 対応環境

- **.NET 10 以上**（ライブラリ本体。C# 14 で実装）
- OS に依存しないため、Windows / Linux / macOS のいずれでも動作します。
- テストはライブラリと同じ `net10.0` を対象に実行しています。

---

## インストール

### NuGet.org から

```powershell
dotnet add package EsUtil.Algorithm.MultiColumnLayoutEngine
```

- NuGet ギャラリー: <https://www.nuget.org/packages/EsUtil.Algorithm.MultiColumnLayoutEngine>
- パッケージ名: `EsUtil.Algorithm.MultiColumnLayoutEngine`

### GitHub Packages から

GitHub Packages は認証が必要なため、事前にソースと資格情報を登録します。

```powershell
# USERNAME は GitHub のユーザー名、TOKEN は read:packages 権限を持つ PAT を指定します
dotnet nuget add source --username USERNAME --password TOKEN --store-password-in-clear-text --name github "https://nuget.pkg.github.com/tomokuni/index.json"

dotnet add package EsUtil.Algorithm.MultiColumnLayoutEngine
```

- GitHub Packages: <https://github.com/tomokuni/EsUtil.Algorithm.MultiColumnLayoutEngine/pkgs/nuget/EsUtil.Algorithm.MultiColumnLayoutEngine>

---

## 主要機能

### 3 つのアルゴリズム

| アルゴリズム | 計算量 | 品質 | 特徴 | 推奨用途 |
| :--- | :--- | :--- | :--- | :--- |
| DynamicProgramming（DP） | O(n² × m) | 最適解 100% | 最高品質 | 小～中規模（～1000 件）、品質最優先 |
| Greedy（貪欲法） | O(n × m) | 95%+ | 高速計算 | 大規模（1000+ 件）、速度重視 |
| BinarySearch | O(n × log(h)) | 99%+ | バランス型 | 全般推奨、品質と速度のバランス最適 |

### パフォーマンスベンチマーク（リリースビルド .NET 10）

```text
【アイテム数ごとの計算時間（Solve メソッド実行時）】

アイテム数    DP法(ms)    Greedy法(ms)  BinarySearch(ms)  反復回数  品質G(%)  品質B(%)
10 件        0.018       0.002         0.003             1 回      97.4%     100.0%
30 件        0.210       0.004         0.005             0 回      90.9%     100.0%
60 件        0.599       0.007         0.008             1 回      97.2%     100.0%
100 件       1.585       0.010         0.011             2 回      97.6%     100.0%
300 件       5.153       0.019         0.021             2 回      98.1%     100.0%
600 件       29.257      0.028         0.034             4 回      98.7%     100.0%
1,000 件     114.898     0.042         0.045             4 回      97.9%     100.0%
10,000 件    --          0.111         0.291             8 回      99.7%     --
100,000 件   --          0.568         3.065             11 回     99.8%     --
1,000,000 件 --          6.965         36.739            12 回     99.9%     --
10,000,000 件 --         70.625        --                --        --        --

※ "--" は計測対象外または計算不可
```

### 品質分析

**DP 法（DynamicProgramming）**:

- 精度：最適解保証（100%）
- 適用範囲：小～中規模（～1000 件）
- 限界：1000 件超えで計算時間が急増し非実用的
- メモリ効率：O(n × m)
- 推奨シナリオ：品質最優先の小規模データセット（～100 件）

**Greedy 法**:

- 精度：90%+
- スケール性：アイテム数が増加するほど品質向上傾向
- 大規模対応：1,000,000 件でも 6.215 ms で計算完了
- メモリ効率：O(n)
- 推奨シナリオ：
  - 速度重視の大規模データセット（1,000+ 件）
  - 実用品質（90%+）で十分な用途

**BinarySearch 法**:

- 精度：99%+
- 安定性：最高（品質の最小保証）
- 大規模対応：1,000,000 件でも 35.508 ms で計算完了
- メモリ効率：O(n)
- 推奨シナリオ：全般推奨、品質と速度のバランス最適
  - デフォルト選択肢（Solve の method デフォルト値）
  - 品質 100% が必須な用途
  - 全スケール対応（10 件～1,000,000 件+）

### 選択ガイドライン

| アイテム数 | 推奨アルゴリズム | 理由 |
| :--- | :--- | :--- |
| 10～100 件 | DP 法 | 最高品質、計算時間も許容範囲 |
| 100～500 件 | BinarySearch | DP と同等品質、更に高速 |
| 500～1,000 件 | BinarySearch | DP が非実用的、BS で 100% 品質 |
| 1,000～10,000 | BinarySearch | 高速（0.306 ms）、100% 品質 |
| 10,000～100,000 | BinarySearch | 実用的（2.987 ms）、100% 品質 |
| 100,000+ | Greedy or BS | BS：35.508 ms, 100% 品質 </br> Greedy：6.215 ms, 99.9% 品質 </br> (速度重視なら Greedy 推奨) |

### 最適化手法

- **ArrayPool** による GC メモリ割り当て削減（DP テーブル、セグメント配列）
- **stackalloc** による GC 圧力ゼロ化（32 要素以下の一時バッファ）
- **ReadOnlySpan** による参照型アクセス最適化
- **キャッシング機構** による複数アルゴリズム間の重複計算回避
- **MethodImpl.AggressiveInlining** による小規模メソッド最適化
- **MethodImpl.AggressiveOptimization** によるホットパス最適化
- **ホットパス変数キャッシング** による CPU メモリレイテンシ削減

---

## 使用方法

### 基本的な使用例

```csharp
using EsUtil.Algorithm;
using static EsUtil.Algorithm.MultiColumnLayoutEngine;  // Method（入れ子の列挙型）を使用するため

// 1. アイテムリストを作成
var items = new List<(double Width, double Height)>
{
    (50.0, 100.0),   // 幅 50、高さ 100
    (60.0, 120.0),   // 幅 60、高さ 120
    (70.0, 140.0)    // 幅 70、高さ 140
};

// 2. レイアウトインスタンスを作成
var layout = new MultiColumnLayoutEngine(
    items: items,
    space: (Row: 5.0, Column: 10.0),  // 行間 5、列間 10
    columnLimit: 5                    // 最大 5 列
);

// 3. レイアウトを計算（BinarySearch で高精度＆高速）
var (usedWidth, minHeight) = layout.Solve(
    widthLimit: 200.0,
    method: Method.BinarySearch
);

Console.WriteLine($"使用幅: {usedWidth}, 最大高さ: {minHeight}");

// 4. 各列のセグメント情報を取得
var columnSegments = layout.GetLastColumnSegments();
foreach (var (startIdx, endIdx) in columnSegments)
{
    Console.WriteLine($"列: アイテム {startIdx} ～ {endIdx}");
}

// 5. 各アイテムの描画座標を計算
var itemLayouts = layout.GetLastItemLayouts();
foreach (var (i, (x, y, w, h)) in itemLayouts.Select((item, i) => (i, item)))
{
    Console.WriteLine($"アイテム {i}: X={x}, Y={y}, Width={w}, Height={h}");
}
```

### アルゴリズム選択ガイド

`Solve(widthLimit, method)` は拡張メンバーです（呼び出し後に `CurrentMethod` は元へ戻ります）。

#### BinarySearch 法（推奨）

```csharp
var (usedWidth, minHeight) = layout.Solve(200.0, Method.BinarySearch);
// メリット: 99%+ 精度、全サイズで高速、デフォルト推奨
// デメリット: なし
```

#### Greedy 法（高速計算）

```csharp
var (usedWidth, minHeight) = layout.Solve(200.0, Method.Greedy);
// メリット: 高速、O(n × m) 計算量
// デメリット: 品質低下（95%+）
```

#### DP 法（最高品質）

```csharp
var (usedWidth, minHeight) = layout.Solve(200.0, Method.DynamicProgramming);
// メリット: 最適解保証（100% 品質）
// デメリット: 1000 件超えで計算不可、メモリ大量消費
```

---

## API リファレンス

### コンストラクタ

```csharp
public MultiColumnLayoutEngine(
    IReadOnlyList<(double Width, double Height)> items,
    (double Row, double Column) space,
    int columnLimit = 10
);
```

**パラメータ：**:

- `items`: 各アイテムの (幅, 高さ) リスト（必須、順序維持）
- `space`: 行間スペース (Row) と列間スペース (Column)（非負値）
- `columnLimit`: 使用可能な列数の上限（デフォルト: 10、正の値）

> BinarySearch 用のオプションは、プロパティ `CurrentBinarySearchOptions` で指定します（省略時は `BinarySearchOptions.Default`）。

### Solve メソッド

```csharp
// インスタンス メソッド（CurrentMethod で指定したアルゴリズムを使用）
public (double UsedWidth, double MinHeight) Solve(double widthLimit);

// 拡張メンバー（アルゴリズムを引数で指定。CurrentMethod は呼び出し前へ戻る）
public (double UsedWidth, double MinHeight) Solve(double widthLimit, Method method);
```

**戻り値：**:

- `UsedWidth`: 実際に使用した幅（widthLimit 以下）
- `MinHeight`: 最大列高さ（最小化された値）

**計算式：**:

```text
列の高さ = Σ(アイテム高さ) + (アイテム数 - 1) × space.Row
列の幅 = max(アイテム幅)
MinHeight = max(全列の高さ)
UsedWidth = Σ(列幅) + (列数 - 1) × space.Column
```

### 補助メソッド

```csharp
// 最後の Solve() 実行時の結果を取得
public (double UsedWidth, double MinHeight) GetLastResult();

// 最後の Solve() 実行時の各列のセグメント情報を取得
public (int StartIdx, int EndIdx)[] GetLastColumnSegments();

// 各アイテムの描画座標とサイズを計算
public IReadOnlyList<(double X, double Y, double Width, double Height)> GetLastItemLayouts();

// 単列配置（フォールバック）を計算
public (double UsedWidth, double MinHeight) SolveSingleColumnLayout();

// 最大アイテム幅が widthLimit を超えるか判定
public bool IsValidWidth(double widthLimit);

// 最後の BinarySearch 実行時の反復回数を取得（未実行・対象外は null）
public int? GetBinarySearchIterationCount();

// キャッシュをクリア（複数 Solve() 呼び出し時の最適化を無効化）
public void ClearCache();
```

### 拡張メンバー（C# 14 の extension ブロック）

`MultiColumnLayoutEngine` 本体の型を変更せずに追加したメンバーです。`using EsUtil.Algorithm;` のみで、インスタンス メンバーと同じ形で呼び出せます。

| 拡張メンバー | 内容 |
| --- | --- |
| `(double, double) Solve(double widthLimit, Method method)` | 指定したアルゴリズムで 1 回だけ解く（`CurrentMethod` は呼び出し前へ戻る） |
| `int ItemCount` | 登録されているアイテム数 |
| `int ColumnCount` | 最後の `Solve()` で確定した列数（未実行は 0） |
| `int IterationCount` | 最後の BinarySearch の反復回数（未実行・対象外は 0） |

```csharp
using EsUtil.Algorithm;
using static EsUtil.Algorithm.MultiColumnLayoutEngine;  // Method は入れ子の列挙型のため

var engine = new MultiColumnLayoutEngine(items, (1.0, 2.0), 10);

// 一度だけ Greedy で解く（CurrentMethod は呼び出し前の値へ戻る）
var (usedWidth, minHeight) = engine.Solve(200.0, Method.Greedy);

Console.WriteLine($"アイテム数: {engine.ItemCount} / 列数: {engine.ColumnCount} / 反復: {engine.IterationCount}");
```

---

## オプション設定（BinarySearch 用）

```csharp
var options = new BinarySearchOptions(
    Epsilon: 1e-3,          // 収束判定許容誤差（デフォルト: 1e-3）
    MaxIterations: 100,     // 最大反復回数（デフォルト: 100）
    LowerBoundRatio: 0.95   // 下限比率（デフォルト: 0.95）
);

var layout = new MultiColumnLayoutEngine(
    items: items,
    space: (5.0, 10.0),
    columnLimit: 10
);

// BinarySearch のオプションはプロパティで設定する（設定すると Strategy が再作成される）
layout.CurrentBinarySearchOptions = options;

var (usedWidth, minHeight) = layout.Solve(200.0);
```

**パラメータ説明：**:

- `Epsilon`：二分探索の収束条件。小さいほど精度向上（但し反復回数増加）
- `MaxIterations`：無限ループ防止の上限値。通常は 100 回で充分
- `LowerBoundRatio`：初期下限値の比率。Greedy 結果 × (1 - この値)

**レコード構造体の更新方法：**
BinarySearchOptions はレコード構造体なので、一部の値を更新するには `with` キーワードを使用します。

```csharp
// デフォルトオプションを取得
var defaultOptions = BinarySearchOptions.Default;

// Epsilon のみを変更
var customOptions = defaultOptions with { Epsilon = 1e-4 };

// 複数の値を変更
var advancedOptions = defaultOptions with { 
    Epsilon = 1e-4, 
    MaxIterations = 200 
};
```

---

## エラーハンドリング

### 入力値検証

不正な入力値は **ArgumentException** をスロー：

```csharp
try
{
    var layout = new MultiColumnLayoutEngine(
        items: new List<(double, double)> { (0.0, 100.0) },  // 幅が 0 は無効
        space: (-1.0, 0.0),                                  // 負のスペースは無効
        columnLimit: 0                                       // 列数は正の値必須
    );
}
catch (ArgumentException ex)
{
    Console.WriteLine($"エラー: {ex.Message}");
}
```

**検証項目：**:

- items が null でないか
- 各アイテムの幅・高さが正の値か（NaN、無限大は無効）
- space.Row、space.Column が非負か（NaN、無限大は無効）
- columnLimit が正の値か
- options が指定される場合、各パラメータが妥当か

### 出力値検証

計算結果の妥当性チェック（自動）：

```csharp
try
{
    var (usedWidth, minHeight) = layout.Solve(100.0);
    // 検証: UsedWidth ≤ widthLimit
    // 検証: MinHeight ≤ 単列配置高さ
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"検証エラー: {ex.Message}");
}
```

---

## パフォーマンス最適化

### 大規模データセット向け

```csharp
// 大規模データセット（100,000 件以上）
var largeItems = Enumerable.Range(0, 1000000)
    .Select(i => (Width: (i % 100) + 10.0, Height: (i % 200) + 20.0))
    .ToList();

var layout = new MultiColumnLayoutEngine(
    items: largeItems,
    space: (Row: 0.0, Column: 0.0),
    columnLimit: 20
);

// BinarySearch で高速計算（ArrayPool で GC 圧力ゼロ）
var (usedWidth, minHeight) = layout.Solve(500.0, Method.BinarySearch);
```
