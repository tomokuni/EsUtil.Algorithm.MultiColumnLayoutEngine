using System;
using System.Buffers;
using System.Runtime.CompilerServices;

using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineInner;

/// <summary><b>動的計画法（DP）を実装する Strategy クラス</b></summary>
/// <remarks>
/// 【概要】<br/>
/// ストリップ梱包問題の最適解を保証する動的計画法（DP）を実装した Strategy クラスです。<br/>
/// 品質100%を達成しますが、計算量 O(n² × m) で速度が遅いため、小～中規模データ向けです。<br/>
/// <br/>
/// 【ポイント】<br/>
/// • コンストラクタで共有の ColumnMetricsCache を受け取る<br/>
/// • Solve() 内で _metricsCache.GetMetrics() を使用<br/>
/// • 前回実行時のメトリクスキャッシュがあれば自動的に再利用<br/>
/// <br/>
/// 【実装の詳細】<br/>
/// DPテーブルを使用して最適な列分割を計算します。<br/>
/// <br/>
/// 【制約】<br/>
/// n（アイテム数）とm（列数上限）の積が大きい場合、計算時間が長くなります。<br/>
/// メモリ使用量は O(n × m) です。<br/>
/// <br/>
/// 【注意点】<br/>
/// 小～中規模データ向けで、大規模データでは Greedy を推奨。<br/>
/// 品質は100%ですが、速度は遅いです。<br/>
/// <br/>
/// 【使用例】<br/>
/// var strategy = new DPLayoutStrategy(items, space, 10, cache);<br/>
/// var result = strategy.Solve(100.0);<br/>
/// <br/>
/// 【最適化手法】<br/>
/// • ColumnMetricsCache の共有で重複計算回避<br/>
/// • ArrayPool によるメモリプール化<br/>
/// • AggressiveInlining でメソッド最適化<br/>
/// </remarks>
/// <param name="items">アイテム配列</param>
/// <param name="space">行間・列間スペース</param>
/// <param name="columnLimit">列数上限</param>
/// <param name="metricsCache">共有メトリクスキャッシュ</param>
internal sealed class DPLayoutStrategy(
    Size[] items,
    Space space,
    int columnLimit,
    ColumnMetricsCache metricsCache) : ILayoutStrategy
{

    /// <summary><b>戦略名を取得します</b></summary>
    public string StrategyName { get; init; } = "DynamicProgramming";

    /// <summary><b>DP 法でレイアウトを計算します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. CalculateDPLayout を呼び出し<br/>
    /// 2. 結果を返却<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// widthLimit は正の値であることを前提とします。<br/>
    /// </remarks>
    /// <param name="widthLimit">幅の制限値</param>
    /// <returns>計算されたレイアウト結果</returns>
    public StrategyResult Solve(double widthLimit)
    {
        // CalculateDPLayout を呼び出して結果を返却する
        var (_, result) = CalculateDPLayout(widthLimit);
        return result;
    }


    /// <summary><b>戦略固有のメトリクス情報を返却します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. キーが StrategyName で型が string の場合、StrategyName を返却<br/>
    /// 2. それ以外は default を返却<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 主にデバッグやログ用に使用されます。<br/>
    /// </remarks>
    public T? GetMetadata<T>(MetadataKey key) where T : IEquatable<T>
    {
        // キーが StrategyName で型が string の場合、StrategyName を返却する
        if (key == MetadataKey.StrategyName && typeof(T) == typeof(string))
            return (T?)(object?)StrategyName;

        // それ以外はデフォルト値を返却する
        return default;
    }


    /// <summary><b>DP 法の具体的な計算ロジックを実装します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. DPテーブル確保<br/>
    /// 2. DPテーブル埋充<br/>
    /// 3. 最適解復元<br/>
    /// 4. メモリ解放<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// アイテム数が0の場合、空の結果を返します。<br/>
    /// </remarks>
    /// <param name="widthLimit">幅の制限値</param>
    /// <returns>（有効性フラグ、最適レイアウト結果）タプル</returns>
    internal (bool valid, StrategyResult result) CalculateDPLayout(double widthLimit)
    {
        // アイテム数を確認する
        int n = items.Length;
        if (n == 0)
            return (true, StrategyResult.Empty);

        // 【段階 1】DP テーブル確保
        int dpSize = (n + 1) * (columnLimit + 1);
        var dp = ArrayPool<DpState>.Shared.Rent(dpSize);

        try
        {
            // 【段階 2】DP テーブル埋充処理（キャッシュ経由でメトリクスを取得）
            FillDPTable(dp, n, widthLimit);

            // 【段階 3】最適解を復元
            return BuildColumnSegments(dp, n);
        }
        finally
        {
            // 【段階 4】メモリ解放
            ArrayPool<DpState>.Shared.Return(dp, clearArray: false);
        }
    }

    /// <summary><b>DP テーブルを埋充します（メトリクスキャッシュ経由で遅延計算）</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. テーブル初期化<br/>
    /// 2. ベースケース設定<br/>
    /// 3. DP埋充ループで各状態を計算<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 幅制限を超える場合は遷移をスキップします。<br/>
    /// </remarks>
    /// <param name="dp">DP テーブル</param>
    /// <param name="n">アイテム数</param>
    /// <param name="widthLimit">幅の制限値</param>
    internal void FillDPTable(DpState[] dp, int n, double widthLimit)
    {
        // ホットパス開始：タプルのローカルキャッシング化
        double spaceColumn = space.Column;
        int colLimit = columnLimit;

        // DP テーブル初期化：全て無効な状態に設定
        for (int i = 0; i <= n; i++)
        {
            for (int j = 0; j <= colLimit; j++)
            {
                int idx = i * (colLimit + 1) + j;
                dp[idx] = DpState.Create(double.MaxValue, 0, -1);
            }
        }

        // ベースケース：0 個のアイテムを 0 列で配置 → 高さ 0、幅 0
        dp[0] = DpState.Create(0.0, 0, -1);

        // DP 埋充ループ
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= Math.Min(i, colLimit); j++)
            {
                // 最後の j 番目の列の開始位置 k を全て試す
                for (int k = j - 1; k < i; k++)
                {
                    // キャッシュ経由でメトリクスを取得
                    var metrics = metricsCache.GetMetrics(new ColumnSegment(k, i - 1));
                    double Width = metrics.Width;
                    double Height = metrics.Height;

                    // 条件判定：幅チェック + 前の状態チェック
                    int prevIdx = k * (colLimit + 1) + (j - 1);
                    var prev = dp[prevIdx];

                    // 前の状態が無効、または幅制限超過なら遷移不可
                    if (!prev.IsValid || (Width > widthLimit))
                        continue;

                    // 新しい総幅と高さを計算
                    double newWidth = prev.Width + Width + (j > 1 ? spaceColumn : 0.0);
                    if (newWidth > widthLimit)
                        continue;

                    // 最大列高さ = max（前 j-1 列の最大高さ、この列の高さ）
                    double newHeight = Math.Max(prev.Height, Height);

                    // 更新判定と実行
                    // より優れている場合（高さ小、同じ高さなら幅小）に更新
                    int currentIdx = i * (colLimit + 1) + j;
                    ref var currentState = ref dp[currentIdx];

                    // 参照型で直接比較・更新
                    if (newHeight < currentState.Height ||
                        (newHeight == currentState.Height && newWidth < currentState.Width))
                    {
                        currentState = DpState.Create(newHeight, newWidth, k);
                    }
                }
            }
        }
    }

    /// <summary><b>DP テーブルから最適解を見つけます</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. 各列数で状態を確認<br/>
    /// 2. 高さと幅を比較して最適を選択<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 無効な状態はスキップします。<br/>
    /// </remarks>
    /// <param name="dp">DP テーブル</param>
    /// <param name="n">アイテム数</param>
    /// <returns>最適な列数（見つからない場合は -1）</returns>
    internal int FindBestLayout(DpState[] dp, int n)
    {
        // 最適な列数を初期化する
        int bestJ = -1;
        double bestHeight = double.MaxValue;
        double bestWidth = double.MaxValue;

        // 使用する列数 j = 1 から min(n, columnLimit) まで試す
        for (int j = 1; j <= Math.Min(n, columnLimit); j++)
        {
            int idx = n * (columnLimit + 1) + j;
            var state = dp[idx];

            if (!state.IsValid)
                continue;

            // 高さが小さいか、同じ高さなら幅が小さいほうを選択
            double stateWidth = state.Width;
            if (state.Height < bestHeight ||
                (state.Height == bestHeight && stateWidth < bestWidth))
            {
                bestHeight = state.Height;
                bestWidth = stateWidth;
                bestJ = j;
            }
        }

        // 最適な列数を返却する
        return bestJ;
    }

    /// <summary><b>DP テーブルから ColumnSegments を復元します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. 最適列数を見つける<br/>
    /// 2. バックトレースでセグメント復元<br/>
    /// 3. 結果構築<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// ArrayPool を使用してメモリ効率を高めます。<br/>
    /// </remarks>
    /// <param name="dp">DP テーブル</param>
    /// <param name="n">アイテム数</param>
    /// <returns>（有効性フラグ、最適レイアウト結果）タプル</returns>
    internal (bool valid, StrategyResult result) BuildColumnSegments(DpState[] dp, int n)
    {
        // 最適な列数を見つける
        int bestJ = FindBestLayout(dp, n);
        if (bestJ == -1)
            return (false, StrategyResult.Empty);

        ColumnSegment[] segments;
        bool useArrayPool = bestJ > 128;
        if (useArrayPool)
            segments = ArrayPool<ColumnSegment>.Shared.Rent(bestJ);
        else
            segments = new ColumnSegment[bestJ];

        try
        {
            // バックトレース：DP テーブルを遡ってセグメントを復元
            int segmentIdx = bestJ - 1;
            int currentI = n;
            int currentJ = bestJ;

            while (currentJ > 0)
            {
                int idx = currentI * (columnLimit + 1) + currentJ;
                int breakIdx = dp[idx].BreakIdx;

                // [breakIdx, currentI-1] が currentJ 番目の列
                segments[segmentIdx--] = new ColumnSegment(breakIdx, currentI - 1);

                currentI = breakIdx;
                currentJ--;
            }

            int finalIdx = n * (columnLimit + 1) + bestJ;
            return (true, new StrategyResult(
                     dp[finalIdx].Width,
                     dp[finalIdx].Height,
                     segments));
        }
        finally
        {
            // ArrayPool 使用時のみ返却
            if (useArrayPool)
                ArrayPool<ColumnSegment>.Shared.Return(segments, clearArray: false);
        }
    }

    /// <summary><b>DP テーブルの各状態を表すレコード構造体</b></summary>
    /// <remarks>
    /// 【概要】<br/>
    /// DP計算の状態を保持します。<br/>
    /// <br/>
    /// 【制約】<br/>
    /// Height が double.MaxValue の場合、無効状態。<br/>
    /// <br/>
    /// 【最適化手法】<br/>
    /// • AggressiveInlining でCreateメソッド最適化<br/>
    /// </remarks>
    internal readonly record struct DpState(double Height, double Width, int BreakIdx)
    {
        /// <summary><b>この DP 状態が有効かどうかを判定します</b></summary>
        public bool IsValid => !double.IsInfinity(Height);

        /// <summary><b>DpState インスタンスを作成します</b></summary>
        /// <remarks>
        /// 【処理フロー】<br/>
        /// 1. パラメータを渡してインスタンス生成<br/>
        /// <br/>
        /// 【注意点】<br/>
        /// AggressiveInlining でインライン化されます。<br/>
        /// </remarks>
        /// <param name="height">最大列高さ。</param>
        /// <param name="width">総幅。</param>
        /// <param name="breakIdx">分割位置。</param>
        /// <returns>新しい DpState インスタンス。</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DpState Create(double height, double width, int breakIdx)
            => new(height, width, breakIdx);
    }
}
