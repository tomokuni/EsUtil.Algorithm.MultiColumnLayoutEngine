# VerticalMultiColumnLayout - ソースコード詳細仕様書

## 目次

1. 概要
2. 型定義（Enums・Records・Structs）
3. 主要クラス
4. パフォーマンス向上施策
5. アルゴリズムの詳細
6. メモリ管理戦略

---

## 概要

`MultiColumnLayoutEngine` は、複数のアイテムを複数の列に配置し、最大列高さを最小化する最適化問題を解く C# ライブラリです。

### クラス構成

```
MultiColumnLayoutEngine（パブリック）
├── Method（列挙型）: DynamicProgramming, Greedy, BinarySearch
├── BinarySearchOptions（レコード構造体）: アルゴリズム制御パラメータ
├── MetadataKey（列挙型）: メトリクス取得キー
├── Space（内部レコード構造体）: 行間・列間スペース
├── Size（内部レコード構造体）: 幅・高さ情報
├── ColumnSegment（内部レコード構造体）: 列の範囲指定
├── StrategyResult（内部レコード構造体）: 戦略実行結果
├── ColumnMetricsCache（内部シールドクラス）: メトリクスキャッシング
├── ILayoutStrategy（内部インターフェース）: 戦略パターン用抽象
├── LayoutStrategyBase（内部クラス）: 基底クラス
├── LayoutStrategyFactory（内部クラス）: Strategy 生成 Factory
├── DPLayoutStrategy（内部クラス）: DP 実装
├── GreedyLayoutStrategy（内部クラス）: Greedy 実装
├── BinarySearchLayoutStrategy（内部クラス）: BinarySearch 実装
└── Helper（内部静的クラス）: 検証・計算補助

MultiColumnLayoutEngineExtensions（パブリック静的クラス）
└── C# 14 の拡張メンバー（extension ブロック）: Solve(widthLimit, method) と状態参照プロパティ
```

### C# 14（.NET 10）の適用箇所

| 機能 | 適用箇所 | 内容 |
| --- | --- | --- |
| 拡張メンバー（extension ブロック） | MultiColumnLayoutEngineExtensions | 型を変更せずに Solve(widthLimit, method) と 3 つのプロパティを追加 |
| field キーワード | MultiColumnLayoutEngineBase.ItemsMaxWidth | バッキング フィールドを宣言せずに最大幅をキャッシュ（初期値はプロパティ初期化子で double.NaN） |
| null 条件付き代入 | MultiColumnLayoutEngine.GetBinarySearchIterationCount() | _strategyFactory が null の場合は代入自体をスキップ |
| 第一級 Span | ColumnMetricsCache.CalcMetrics() | 配列から Span を通常の型と同様に扱う（AsSpan） |

---

## 型定義

### BinarySearchOptions レコード構造体

```csharp
public readonly record struct BinarySearchOptions(
    double Epsilon = 1e-3,
    int MaxIterations = 100,
    double LowerBoundRatio = 0.95)
{
    public BinarySearchOptions() : this(1e-3, 100, 0.95) { }
    public static readonly BinarySearchOptions Default = new(1e-3, 100, 0.95);
    public void IsValid();
}
```

**フィールド説明：**

| フィールド | 型 | デフォルト | 制約 | 説明 |
|:---|:---|:---|:---|:---|
| Epsilon | double | 1e-3 | > 0.0 | 二分探索の収束判定許容誤差。小さいほど精度向上 |
| MaxIterations | int | 100 | > 0 | 二分探索の最大反復回数。無限ループ防止 |
| LowerBoundRatio | double | 0.95 | > 0.0 | 下限値の比率。初期下限値 = Greedy結果 × (1 - この値) |

---

### MetadataKey 列挙型

```csharp
public enum MetadataKey
{
    StrategyName,
    BinarySearchIterationCount
}
```

**用途：** 戦略固有のメトリクス情報を取得するためのキー。

---

### Method 列挙型

```csharp
public enum Method
{
    DynamicProgramming,  // 動的計画法：最適解保証（O(n² × m)）
    Greedy,              // Greedy 近似法：高速計算（O(n × m)、品質 95%+）
    BinarySearch         // バイナリサーチ法：バランス型（O(n × log(h))、品質 99%+）
}
```

**用途別選択：**
- DynamicProgramming: 小～中規模（～1000 件）、最高品質要求
- Greedy: 大規模（1000+ 件）、速度重視、初期値算出
- BinarySearch: 全般推奨、品質と速度のバランス最適

---

### Space レコード構造体

```csharp
internal readonly record struct Space(double Row, double Column);
```

**フィールド説明：**
- Row (≥ 0.0): アイテム間の垂直方向スペース（行間）
- Column (≥ 0.0): 列間の水平方向スペース（列間）

**計算への影響：**
```
列の高さ = Σ(アイテム高さ) + (アイテム数 - 1) × Row
```

---

### Size レコード構造体

```csharp
internal readonly record struct Size(double Width, double Height)
{
    public static Size[] ToSizeArray(IReadOnlyList<(double Width, double Height)> items);
}
```

**制約条件：**
- Width > 0.0
- Height > 0.0

**メソッド：**
- `ToSizeArray()`: タプル配列から Size 配列に変換

---

### ColumnSegment レコード構造体

```csharp
internal readonly record struct ColumnSegment(int StartIdx, int EndIdx);
```

**制約条件：**
- 0 ≤ StartIdx ≤ EndIdx < アイテム数

**意味：**
- StartIdx: 列に属する最初のアイテムのインデックス
- EndIdx: 列に属する最後のアイテムのインデックス（包含）

---

### StrategyResult レコード構造体

```csharp
internal readonly record struct StrategyResult(
    double UsedWidth,
    double MinHeight,
    ColumnSegment[] ColumnSegments)
{
    public static readonly StrategyResult Empty = new(0.0, 0.0, []);
}
```

**フィールド説明：**
- UsedWidth (≥ 0.0): 実際に使用した幅（列間スペース含む）
- MinHeight (≥ 0.0): 最大列高さ（最小化対象）
- ColumnSegments: 各列のアイテム範囲の配列

---

## 主要クラス

### 1. LayoutStrategyBase クラス（内部）

**責務：**
- マルチカラムレイアウト最適化アルゴリズムの基底クラス
- Template Method パターンによる共通処理の実装
- 入力検証、キャッシュ管理、結果検証

#### 定数

```csharp
internal const int STACKALLOC_THRESHOLD = 32;
```

スタックアロケーション（stackalloc）を使用する配列の要素数の上限。これ以下のサイズなら stackalloc、以上なら ArrayPool を使用。

#### フィールド

```csharp
internal readonly Size[] _items;                                    // アイテム配列（幅・高さ）
internal readonly Space _space;                                     // 行間・列間スペース
internal readonly int _columnLimit;                                 // 使用可能な列数の上限
internal double _itemsMaxWidthCached = double.NaN;                  // 最大アイテム幅のキャッシュ
internal StrategyResult _lastSolveResult = StrategyResult.Empty;    // 最後の Solve() 実行時の結果
internal readonly ColumnMetricsCache _metricsCache;                 // メトリクスキャッシュ
```

#### パブリック メソッド

##### Constructor

```csharp
public LayoutStrategyBase(
    IReadOnlyList<(double Width, double Height)> items,
    (double Row, double Column) space,
    int columnLimit = 10)
```

**機能：** インスタンスの初期化と入力パラメータの検証

**処理フロー：**
1. ValidateParameter() で入力値を検証
2. フィールド変数に値を格納
3. ColumnMetricsCache インスタンスを作成

**パラメータ検証内容：**
- items が null でないか
- columnLimit が正の値か
- space.Row、space.Column が非負か
- 各アイテムの幅・高さが正の値か

**例外：** ArgumentException（検証失敗時）

---

##### Solve() メソッド

```csharp
public (double UsedWidth, double MinHeight) Solve(double widthLimit)
```

**機能：** レイアウトを計算（Template Method パターン）

**処理フロー：**

```
1. パラメータ検証（ValidateParameter）
   └─ widthLimit > 0.0 かつ有限値かチェック

2. 最大アイテム幅チェック
   └─ IsValidWidth() で widthLimit 超過判定
   └─ 超過なら SolveSingleColumnLayout()（フォールバック）

3. 空リスト判定
   └─ items.Length == 0 なら (0.0, 0.0) を返却

4. レイアウト計算（メイン処理）
   └─ SolveCore() を呼び出し（サブクラスで実装）

5. 出力値検証（VerifyLayoutResult）
   ├─ UsedWidth ≤ widthLimit
   └─ MinHeight ≤ 単列配置高さ

6. 結果キャッシュと返却
   ├─ _lastSolveResult に保存
   └─ (UsedWidth, MinHeight) を返却
```

**戻り値：**
- UsedWidth: 実際に使用した幅
- MinHeight: 最大列高さ

**例外：**
- ArgumentException（パラメータ検証失敗）
- InvalidOperationException（出力検証失敗）

---

##### GetLastResult()

```csharp
public (double UsedWidth, double MinHeight) GetLastResult()
```

**機能：** 最後の Solve() 実行時の結果を返却

---

##### GetLastColumnSegments()

```csharp
public (int StartIdx, int EndIdx)[] GetLastColumnSegments()
```

**機能：** 最後の Solve() 実行時の各列のセグメント情報を返却

---

##### GetLastItemLayouts()

```csharp
public IReadOnlyList<(double X, double Y, double Width, double Height)> GetLastItemLayouts()
```

**機能：** 各アイテムの描画座標と寸法を計算

**処理フロー：**

```
【段階 1】各列の幅を計算
  └─ メトリクスキャッシュから取得（既計算なら O(1)）
  └─ stackalloc で GC 割り当て削減（STACKALLOC_THRESHOLD 以下）

【段階 2】各列の X 座標を計算
  ├─ 列の幅を累積
  └─ 列間スペースを加算

【段階 3】各アイテムの座標計算
  ├─ X: 所属列の左端位置
  ├─ Y: 列内での累積高さ
  ├─ Width: 列の幅（max(その列のアイテム幅)）
  └─ Height: そのアイテムの高さ
```

---

##### SolveSingleColumnLayout()

```csharp
public (double UsedWidth, double MinHeight) SolveSingleColumnLayout()
```

**機能：** フォールバック用の単列配置を計算

**処理内容：**
```
1. 全アイテムを 1 つの列として計算
2. メトリクスキャッシュから取得
3. セグメント配列を [(0, items.Length - 1)] に設定
4. 結果をキャッシュして返却
```

---

##### IsValidWidth()

```csharp
public bool IsValidWidth(double widthLimit)
```

**機能：** 最大アイテム幅が widthLimit を超えるか判定（true = 複数列配置不可）

**処理内容：**
```
1. 最大幅をキャッシュ化（複数回呼び出しで O(1)）
2. 最大幅 > widthLimit で true を返却
```

---

##### ClearCache()

```csharp
public void ClearCache()
```

**機能：** 列メトリクスキャッシュをクリアします

---

##### VerifyLayoutResult()

```csharp
public void VerifyLayoutResult(double widthLimit)
```

**機能：** レイアウト計算結果の正当性を検証

**検証項目：**

```
1. UsedWidth ≤ widthLimit
   └─ 使用幅が制限を超えていない（物理的制約）

2. MinHeight ≤ 単列配置高さ
   └─ 複数列が単列より劣化していない（品質保証）
```

**例外：** InvalidOperationException（詳細メッセージ付き）

---

### 2. MultiColumnLayoutEngine クラス（パブリック）

**責務：**
- マルチカラムレイアウト最適化のエントリーポイント
- アルゴリズム選択と実行
- Strategy パターンの実装

#### 継承

```csharp
public sealed class MultiColumnLayoutEngine : LayoutStrategyBase
```

LayoutStrategyBase を継承し、共通機能を活用。

#### フィールド

```csharp
internal LayoutStrategyFactory? _strategyFactory = null;
```

#### パブリック メソッド

##### Constructor

```csharp
public MultiColumnLayoutEngine(
    IReadOnlyList<(double Width, double Height)> items,
    (double Row, double Column) space,
    int columnLimit = 10,
    BinarySearchOptions? options = null)
```

**機能：** インスタンスの初期化（基底クラス + オプション設定）

**処理フロー：**
1. 基底クラスのコンストラクタを呼び出し
2. BinarySearchOptions を設定（BinarySearch 用）

---

##### Solve() メソッド

```csharp
public (double UsedWidth, double MinHeight) Solve(
    double widthLimit,
    Method method = Method.BinarySearch)
```

**機能：** 選択されたアルゴリズムでレイアウトを計算

**処理フロー：**
1. 基底クラスの Solve() を呼び出し
2. アルゴリズム選択は内部で StrategyFactory が処理

---

##### BinarySearchOptions

```csharp
public BinarySearchOptions BinarySearchOptions { get; set; }
```

**機能：** BinarySearch アルゴリズムのオプションを取得/設定

---

##### CurrentMethod

```csharp
public Method CurrentMethod { get; set; }
```

**機能：** 使用するアルゴリズムを取得/設定

---

##### GetCurrentStrategyName()

```csharp
public string GetCurrentStrategyName()
```

**機能：** 現在アクティブな戦略の名前を取得

---

##### GetBinarySearchIterationCount()

```csharp
public int? GetBinarySearchIterationCount()
```

**機能：** 最後の BinarySearch 実行時の反復回数を取得

**戻り値：**
- BinarySearch 以外: null
- BinarySearch: 反復回数

---

### 3. ColumnMetricsCache クラス（内部）

**責務：**
- 列メトリクス（幅と高さ）のキャッシュ機構
- 複数アルゴリズム間でキャッシュを共用
- 重複計算を回避

#### 定数

なし

#### フィールド

```csharp
internal readonly Dictionary<ColumnSegment, Size> _cache;
```

#### パブリック メソッド

##### Constructor

```csharp
public ColumnMetricsCache(Size[] items, Space space, int estimatedCapacity = 1000)
```

**機能：** キャッシュの初期化

---

##### GetMetrics()

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public Size GetMetrics(ColumnSegment key)
```

**機能：** キャッシュ付き遅延計算でメトリクスを取得

**処理フロー：**
1. キャッシュに該当キーが存在するかチェック
2. 存在する場合は O(1) で返却
3. 存在しない場合は CalcMetrics() で計算 → キャッシュに登録 → 返却

---

##### CalcMetrics()

```csharp
[MethodImpl(MethodImplOptions.AggressiveInlining)]
public Size CalcMetrics(ColumnSegment segment)
```

**機能：** 指定範囲のアイテムから列のメトリクスを計算

**計算内容：**
- 列の高さ = Σ(アイテム高さ) + (アイテム数 - 1) × space.Row
- 列の幅 = max(アイテム幅)

---

##### SetMetrics()

```csharp
public void SetMetrics(ColumnSegment key, Size metrics)
```

**機能：** メトリクスをキャッシュに登録（重複登録防止）

---

##### Clear()

```csharp
public void Clear()
```

**機能：** キャッシュをクリア

---

### 4. ILayoutStrategy インターフェース（内部）

**責務：** Strategy パターンの抽象型（DP、Greedy、BSearch の共通インターフェース）

#### メソッド

##### Solve()

```csharp
public StrategyResult Solve(double widthLimit)
```

**機能：** 指定条件でレイアウトを計算

---

##### GetMetadata()

```csharp
T? GetMetadata<T>(MetadataKey key) where T : IEquatable<T>
```

**機能：** 戦略固有のメトリクス情報を取得（型安全、値型のみ）

---

### 5. LayoutStrategyFactory クラス（内部）

**責務：**
- Strategy インスタンスの遅延初期化と管理
- ColumnMetricsCache の共用
- GetStrategy() で指定 Method に対応する Strategy を返却

#### フィールド

```csharp
internal DPLayoutStrategy? _dpStrategy;
internal GreedyLayoutStrategy? _greedyStrategy;
internal BinarySearchLayoutStrategy? _binarySearchStrategy;
internal BinarySearchOptions? _bSearchOptions;
public Method CurrentMethod;
```

#### パブリック メソッド

##### Constructor

```csharp
public LayoutStrategyFactory(
    Size[] items,
    Space space,
    int columnLimit,
    ColumnMetricsCache metricsCache)
```

**機能：** Factory の初期化

---

##### GetStrategy()

```csharp
public ILayoutStrategy GetStrategy()
```

**機能：** 指定メソッドに対応した Strategy インスタンスを取得（遅延初期化）

---

##### BinarySearchOptions

```csharp
public BinarySearchOptions BinarySearchOptions { get; set; }
```

**機能：** BinarySearch オプションを取得/設定

---

##### ClearStrategyCache()

```csharp
public void ClearStrategyCache()
```

**機能：** Strategy インスタンスの遅延初期化キャッシュをクリア

---

##### ClearMetricsCache()

```csharp
public void ClearMetricsCache()
```

**機能：** メトリクスキャッシュをクリア

---

### 6. DPLayoutStrategy クラス（内部）

**責務：** 動的計画法による最適解計算

#### フィールド

```csharp
public string StrategyName = "DynamicProgramming";
```

#### パブリック メソッド

##### Constructor

```csharp
public DPLayoutStrategy(
    Size[] items,
    Space space,
    int columnLimit,
    ColumnMetricsCache metricsCache)
```

**機能：** DP Strategy の初期化

---

##### Solve()

```csharp
public StrategyResult Solve(double widthLimit)
```

**機能：** DP 法でレイアウトを計算

---

##### GetMetadata()

```csharp
public T? GetMetadata<T>(MetadataKey key) where T : IEquatable<T>
```

**機能：** メタデータを返却（StrategyName）

---

##### CalculateDPLayout()

```csharp
internal (bool valid, StrategyResult result) CalculateDPLayout(double widthLimit)
```

**機能：** DP 法の具体的な計算ロジック

**処理フロー：**
1. DPテーブル確保（ArrayPool）
2. DPテーブル埋充（FillDPTable）
3. 最適解復元（BuildColumnSegments）
4. メモリ解放

---

##### FillDPTable()

```csharp
internal void FillDPTable(DpState[] dp, int n, double widthLimit)
```

**機能：** DP テーブルを埋充

---

##### FindBestLayout()

```csharp
internal int FindBestLayout(DpState[] dp, int n)
```

**機能：** DP テーブルから最適解を見つけ

---

##### BuildColumnSegments()

```csharp
internal (bool valid, StrategyResult result) BuildColumnSegments(DpState[] dp, int n)
```

**機能：** DP テーブルから ColumnSegments を復元

---

##### DpState レコード構造体

```csharp
internal readonly record struct DpState(double Height, double Width, int BreakIdx)
{
    public bool IsValid => !double.IsInfinity(Height);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DpState Create(double height, double width, int breakIdx) => new(height, width, breakIdx);
}
```

---

### 7. GreedyLayoutStrategy クラス（内部）

**責務：** Greedy 近似法による高速計算

#### フィールド

```csharp
public string StrategyName = "Greedy";
```

#### パブリック メソッド

##### Constructor

```csharp
public GreedyLayoutStrategy(
    Size[] items,
    Space space,
    int columnLimit,
    ColumnMetricsCache metricsCache)
```

**機能：** Greedy Strategy の初期化

---

##### Solve()

```csharp
public StrategyResult Solve(double widthLimit)
```

**機能：** Greedy 法でレイアウトを計算

---

##### GetMetadata()

```csharp
public T? GetMetadata<T>(MetadataKey key) where T : IEquatable<T>
```

**機能：** メタデータを返却（StrategyName）

---

##### CalculateGreedyLayout()

```csharp
internal StrategyResult CalculateGreedyLayout(double widthLimit)
```

**機能：** Greedy 法でレイアウトを計算

---

##### BuildGreedyColumns()

```csharp
internal (bool valid, StrategyResult result) BuildGreedyColumns(
    double widthLimit, int numColumns)
```

**機能：** 指定列数でアイテムを均等分割して配置

---

### 8. BinarySearchLayoutStrategy クラス（内部）

**責務：** バイナリサーチ法による高速最適化

#### フィールド

```csharp
public string StrategyName = "BinarySearch";
public int LastIterationCount;
```

#### パブリック メソッド

##### Constructor

```csharp
public BinarySearchLayoutStrategy(
    Size[] items,
    Space space,
    int columnLimit,
    BinarySearchOptions options,
    ColumnMetricsCache metricsCache)
```

**機能：** BinarySearch Strategy の初期化

---

##### Solve()

```csharp
public StrategyResult Solve(double widthLimit)
```

**機能：** BinarySearch 法でレイアウトを計算

---

##### GetMetadata()

```csharp
public T? GetMetadata<T>(MetadataKey key) where T : IEquatable<T>
```

**機能：** メタデータを返却（StrategyName, BinarySearchIterationCount）

---

##### CalculateBinarySearchLayout()

```csharp
internal StrategyResult CalculateBinarySearchLayout(double widthLimit, double initUpper)
```

**機能：** 変形バイナリサーチ法で最適高さを探索

---

##### TryFitColumns()

```csharp
[MethodImpl(MethodImplOptions.AggressiveOptimization)]
internal (bool canFit, double width, double height, int segmentCount) TryFitColumns(
     double widthLimit, double heightLimit, Span<ColumnSegment> outSegmentsBuffer)
```

**機能：** 指定高さで全アイテムが配置可能かを判定

---

### 9. Helper クラス（内部静的）

**責務：** 検証・計算補助メソッド

#### メソッド

##### IsNonNegativeFinite()

```csharp
internal static bool IsNonNegativeFinite(double value)
```

**機能：** 値が非負数の有限値かを判定

---

##### IsPositiveFinite()

```csharp
internal static bool IsPositiveFinite(double value)
```

**機能：** 値が正の有限値かを判定

---

##### ValidateParameter() (コンストラクタ用)

```csharp
internal static void ValidateParameter(
    IReadOnlyList<(double Width, double Height)> items,
    (double Row, double Column) space,
    int columnLimit)
```

**機能：** 入力パラメータの妥当性を検証

---

##### ValidateParameter() (Solve 用)

```csharp
internal static void ValidateParameter(double widthLimit)
```

**機能：** 幅制限の妥当性を検証

---

##### VerifyLayoutResult()

```csharp
internal static void VerifyLayoutResult(StrategyResult lastSolveResult, double widthLimit, ColumnMetricsCache metricsCache, int itemsLength)
```

**機能：** レイアウト計算結果の正当性を検証

---

### 10. MultiColumnLayoutEngineExtensions クラス（パブリック静的）

**責務：** C# 14 の拡張メンバー（extension ブロック）で MultiColumnLayoutEngine の操作性を補完

**特徴：**

- 型（MultiColumnLayoutEngine）を変更せずにメソッドとプロパティを追加できる<br/>
- 既存のインスタンス メソッド Solve(double) とは引数の個数が異なるため、既存の呼び出しの互換性を維持する<br/>
- 拡張プロパティは C# 14 で追加された機能（従来の拡張メソッドではメソッドしか追加できなかった）<br/>

**注意点：**

- Solve(widthLimit, method) は内部で CurrentMethod を切り替えるため、スレッド セーフではない<br/>

#### 拡張メソッド

##### Solve() (アルゴリズム指定)

```csharp
public (double UsedWidth, double MinHeight) Solve(double widthLimit, Method method)
```

**機能：** 指定したアルゴリズムで 1 回だけレイアウトを計算

**処理フロー：**

1. 呼び出し前の CurrentMethod を退避<br/>
2. CurrentMethod を引数の method へ変更<br/>
3. Solve(widthLimit) を呼び出す<br/>
4. finally で CurrentMethod を退避した値へ戻す（例外時も復元）<br/>

#### 拡張プロパティ

##### ItemCount

```csharp
public int ItemCount { get; }
```

**機能：** 登録されているアイテム数を取得（内部のアイテム配列の長さ。コンストラクタで受け取った時点の件数で固定）

##### ColumnCount

```csharp
public int ColumnCount { get; }
```

**機能：** 最後の Solve() 実行時の列数を取得（GetLastColumnSegments().Length。未実行は 0）

##### IterationCount

```csharp
public int IterationCount { get; }
```

**機能：** 最後の BinarySearch 実行時の反復回数を取得（GetBinarySearchIterationCount() の null を 0 へ変換。未生成・対象外は 0）
