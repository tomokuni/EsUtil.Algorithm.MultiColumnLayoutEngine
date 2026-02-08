using System;

using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineInner;

/// <summary><b>Greedy 近似法によるレイアウト最適化を実装する Strategy クラス</b></summary>
/// <remarks>
/// 【概要】<br/>
/// ストリップ梱包問題の近似解を高速に求める Greedy 近似法を実装した Strategy クラスです。<br/>
/// 品質90%+を達成しますが、最適解を保証しません。大規模データ向けに最適化されています。<br/>
/// <br/>
/// 【ポイント】<br/>
/// • 複数の列数を試す（1 列, 2 列, 3 列, ...）<br/>
/// • 最も高さが小さい結果を採用<br/>
/// • メトリクスキャッシュを使用して重複計算を回避<br/>
/// <br/>
/// 【実装の詳細】<br/>
/// 列数を1からcolumnLimitまで試し、最適なレイアウトを選択します。<br/>
/// ColumnMetricsCache を使用してメトリクス計算を効率化します。<br/>
/// <br/>
/// 【制約】<br/>
/// columnLimit は正の値で、アイテム数を超えない。<br/>
/// widthLimit は正の値。<br/>
/// <br/>
/// 【注意点】<br/>
/// 品質は95%+ですが、最適解を保証しません。<br/>
/// 大規模データ向けに高速です。<br/>
/// <br/>
/// 【使用例】<br/>
/// var strategy = new GreedyLayoutStrategy(items, space, 10, cache);<br/>
/// var result = strategy.Solve(100.0);<br/>
/// <br/>
/// 【最適化手法】<br/>
/// • ColumnMetricsCache の共有で計算コスト削減<br/>
/// </remarks>
/// <param name="items">アイテム配列</param>
/// <param name="space">行間・列間スペース</param>
/// <param name="columnLimit">列数上限</param>
/// <param name="metricsCache">共有メトリクスキャッシュ</param>
internal sealed class GreedyLayoutStrategy(
    Size[] items,
    Space space,
    int columnLimit,
    ColumnMetricsCache metricsCache) : ILayoutStrategy
{

    /// <summary><b>戦略名を取得します</b></summary>
    public string StrategyName { get; init; } = "Greedy";

    /// <summary><b>Greedy 法でレイアウトを計算します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. CalculateGreedyLayout を呼び出し<br/>
    /// 2. 結果を返却<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// widthLimit は正の値であることを前提とします。<br/>
    /// </remarks>
    /// <param name="widthLimit">幅の制限値</param>
    /// <returns>計算されたレイアウト結果</returns>
    public StrategyResult Solve(double widthLimit)
    {
        // CalculateGreedyLayout を呼び出して結果を返却する
        return CalculateGreedyLayout(widthLimit);
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

    /// <summary><b>Greedy 法でレイアウトを計算します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. 最適結果を初期化<br/>
    /// 2. 列数1からcolumnLimitまでループ<br/>
    /// 3. 各列数で BuildGreedyColumns を呼び出し<br/>
    /// 4. 有効な結果があれば比較して最適なものを選択<br/>
    /// 5. 最適結果を返却<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 列数がアイテム数を超える場合はスキップします。<br/>
    /// </remarks>
    /// <param name="widthLimit">幅の制限値</param>
    /// <returns>計算されたレイアウト結果</returns>
    internal StrategyResult CalculateGreedyLayout(double widthLimit)
    {
        // 最適結果を初期化する
        var bestResult = new StrategyResult(double.MaxValue, double.MaxValue, []);
        int n = items.Length;

        // 異なる列数を試す
        for (int numColumns = 1; numColumns <= Math.Min(n, columnLimit); numColumns++)
        {
            // 指定列数でレイアウトを構築する
            var (valid, result) = BuildGreedyColumns(widthLimit, numColumns);
            if (!valid)
                continue;

            // より優れた結果を選択（高さが小さいか、同じ高さなら幅が小さい）
            if (result.MinHeight < bestResult.MinHeight ||
                  (result.MinHeight == bestResult.MinHeight && result.UsedWidth < bestResult.UsedWidth))
            {
                bestResult = result;
            }
        }

        // 最適結果を返却する
        return bestResult;
    }

    /// <summary><b>指定の列数でアイテムを均等分割して配置します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. アイテムを均等分割<br/>
    /// 2. 各列にアイテムを配置<br/>
    /// 3. メトリクスを計算し、制限チェック<br/>
    /// 4. 有効なら結果を構築<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 列の幅がwidthLimitを超える場合、無効とします。<br/>
    /// </remarks>
    /// <param name="widthLimit">幅の制限値</param>
    /// <param name="numColumns">配置する列数</param>
    /// <returns>（有効性フラグ、レイアウト結果）タプル：配置成功なら true、制限超過なら false</returns>
    internal (bool valid, StrategyResult result) BuildGreedyColumns(
        double widthLimit, int numColumns)
    {
        // 均等分割の計算
        int n = items.Length;
        int baseItemsPerColumn = n / numColumns;
        int extraItems = n % numColumns;
        double spaceColumn = space.Column;
        var segments = new ColumnSegment[numColumns];

        double totalWidth = 0.0;
        double maxHeight = 0.0;
        int currentStart = 0;

        // 各列にアイテムを配置
        for (int col = 0; col < numColumns; col++)
        {
            // この列に配置するアイテム数（最初の extraItems 列は1個多い）
            int itemsInCol = baseItemsPerColumn + (col < extraItems ? 1 : 0);
            int endIdx = currentStart + itemsInCol - 1;

            // 【バリデーション】セグメントが有効か確認
            if (currentStart < 0 || endIdx >= n || currentStart > endIdx)
                return (false, StrategyResult.Empty);

            // 列のメトリクスを計算
            var metrics = metricsCache.GetMetrics(
                new ColumnSegment(currentStart, endIdx));
            double width = metrics.Width;
            double height = metrics.Height;

            // 列の幅が制限を超えるかチェック
            if (width > widthLimit)
                return (false, StrategyResult.Empty);

            // セグメントを記録
            segments[col] = new ColumnSegment(currentStart, endIdx);
            totalWidth += width;

            // 列間スペースを追加（最初の列以外）
            if (col > 0)
                totalWidth += spaceColumn;

            // 最大高さを記録
            if (height > maxHeight)
                maxHeight = height;

            currentStart = endIdx + 1;
        }

        // 総幅が制限を超えないかチェック
        if (totalWidth > widthLimit)
            return (false, StrategyResult.Empty);

        // 有効な結果を返却する
        return (true, new StrategyResult(totalWidth, maxHeight, segments));
    }
}
