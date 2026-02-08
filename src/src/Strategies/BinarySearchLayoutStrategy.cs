using System;
using System.Buffers;
using System.Runtime.CompilerServices;

using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineInner;

/// <summary><b>バイナリサーチ法によるレイアウト最適化を実装する Strategy クラス</b></summary>
/// <remarks>
/// 【概要】<br/>
/// ストリップ梱包問題の近似解をバイナリサーチで求める変形バイナリサーチ法を実装した Strategy クラスです。<br/>
/// Greedyの結果を初期値として使用し、品質99%+を達成しますが、最適解を保証しません。<br/>
/// <br/>
/// 【キャッシュ共有の活用】<br/>
/// • Greedy 実行時に計算したメトリクスがキャッシュに登録<br/>
/// • BinarySearch の各反復で TryFitColumns() 実行時に、
///   同じセグメントのメトリクスはキャッシュから O(1) で返却<br/>
/// • 結果：Greedy ⇒ BinarySearch で計算量削減<br/>
/// <br/>
/// 【実装の詳細】<br/>
/// 変形バイナリサーチで高さを探索し、TryFitColumnsで配置判定を行います。<br/>
/// ArrayPoolを使用してメモリ効率を高めます。<br/>
/// <br/>
/// 【制約】<br/>
/// options.MaxIterations で反復回数を制限。<br/>
/// widthLimit と heightLimit は正の値。<br/>
/// <br/>
/// 【注意点】<br/>
/// Greedyの結果を活用するため、単独使用より組み合わせ推奨。<br/>
/// 品質は99%+ですが、最適解を保証しません。<br/>
/// <br/>
/// 【使用例】<br/>
/// var strategy = new BinarySearchLayoutStrategy(items, space, 10, options, cache);<br/>
/// var result = strategy.Solve(100.0);<br/>
/// <br/>
/// 【最適化手法】<br/>
/// • ColumnMetricsCache の共有で計算コスト削減<br/>
/// • ArrayPool によるメモリプール化<br/>
/// • AggressiveOptimization でTryFitColumns最適化<br/>
/// </remarks>
/// <param name="items">アイテム配列</param>
/// <param name="space">行間・列間スペース</param>
/// <param name="columnLimit">列数上限</param>
/// <param name="options">アルゴリズムオプション</param>
/// <param name="metricsCache">共有メトリクスキャッシュ</param>
internal sealed class BinarySearchLayoutStrategy(
    Size[] items,
    Space space,
    int columnLimit,
    BinarySearchOptions options,
    ColumnMetricsCache metricsCache) : ILayoutStrategy
{

    /// <summary><b>戦略名を取得します</b></summary>
    public string StrategyName { get; init; } = "BinarySearch";

    /// <summary><b>最後の BinarySearch Solve() 実行時の反復回数</b></summary>
    public int LastIterationCount { get; private set; } = 0;

    /// <summary><b>BinarySearch 法でレイアウトを計算します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. Greedyで初期上限を取得<br/>
    /// 2. CalculateBinarySearchLayoutで探索<br/>
    /// 3. 結果を返却<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// widthLimit は正の値であることを前提とします。<br/>
    /// </remarks>
    /// <param name="widthLimit">幅の制限値。</param>
    /// <returns>計算されたレイアウト結果。</returns>
    public StrategyResult Solve(double widthLimit)
    {
        // 反復回数をリセットする
        LastIterationCount = 0;

        // Greedy で上限を取得
        var greedyStrategy = new GreedyLayoutStrategy(items, space, columnLimit, metricsCache);
        var greedyResult = greedyStrategy.Solve(widthLimit);
        double initUpper = greedyResult.MinHeight;

        // CalculateBinarySearchLayout で探索を実行する
        return CalculateBinarySearchLayout(widthLimit, initUpper);
    }


    /// <summary><b>戦略固有のメトリクス情報を返却します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. キーが StrategyName で型が string の場合、StrategyName を返却<br/>
    /// 2. キーが BinarySearchIterationCount で型が int の場合、LastIterationCount を返却<br/>
    /// 3. それ以外は default を返却<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 主にデバッグやログ用に使用されます。<br/>
    /// </remarks>
    public T? GetMetadata<T>(MetadataKey key) where T : IEquatable<T>
    {
        // キーが StrategyName で型が string の場合、StrategyName を返却する
        if (key == MetadataKey.StrategyName && typeof(T) == typeof(string))
            return (T?)(object?)StrategyName;

        // キーが BinarySearchIterationCount で型が int の場合、LastIterationCount を返却する
        if (key == MetadataKey.BinarySearchIterationCount && typeof(T) == typeof(int))
            return (T?)(object?)LastIterationCount;

        // それ以外はデフォルト値を返却する
        return default;
    }


    /// <summary><b>変形バイナリサーチ法で最適な高さを探索します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. Greedy結果で初期化<br/>
    /// 2. 下限探索<br/>
    /// 3. バイナリサーチで最適高さ探索<br/>
    /// 4. 結果返却<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// initUpper が不十分な場合、単列高さに調整します。<br/>
    /// </remarks>
    /// <param name="widthLimit">幅の制限値</param>
    /// <param name="initUpper">探索する高さの上限値</param>
    /// <returns>最適レイアウト結果。</returns>
    internal StrategyResult CalculateBinarySearchLayout(double widthLimit, double initUpper)
    {
        // ArrayPool でバッファを確保する
        var bestSegmentsBuffer = ArrayPool<ColumnSegment>.Shared.Rent(columnLimit);
        var maxIterations = options.MaxIterations;
        var epsilon = options.Epsilon;

        try
        {
            // 高さ上限を検証し、問題なければ最良結果として記録
            var (initFit, bestWidth, bestHeight, bestSegmentCount)
                = TryFitColumns(widthLimit, initUpper, bestSegmentsBuffer);
            if (!initFit)
            {
                // 単列配置のメトリクスを計算
                (_, initUpper) = metricsCache.CalcMetrics(
                    new ColumnSegment(0, items.Length - 1));

                (initFit, bestWidth, bestHeight, bestSegmentCount)
                    = TryFitColumns(widthLimit, initUpper, bestSegmentsBuffer);
                if (!initFit)
                {
                    throw new InvalidOperationException(
                       $"BinarySearch failed: cannot fit items within the Single Column Height {initUpper}.");
                }
            }

            Span<ColumnSegment> tempBuffer = columnLimit <= MultiColumnLayoutEngine.STACKALLOC_THRESHOLD
                ? stackalloc ColumnSegment[columnLimit]
                : new ColumnSegment[columnLimit];

            double upper = bestHeight;
            double difference = upper * (1 - options.LowerBoundRatio);
            double lower = upper - difference;

            // 【前処理】lower を段階的に下げながら TryFitColumns が失敗するまで試行して、妥当な下限値を見つける
            while (lower > 0.0)
            {
                // 下限を試す
                var (canFit, width, height, segmentCount) = TryFitColumns(widthLimit, lower, tempBuffer);
                if (!canFit)
                {
                    // 配置失敗 → lower はこれ以上下げられない（現在の lower の 1 つ手前が下限）
                    break;
                }

                // 配置成功 → より低い値を試す必要があるため、lower と upper をさらに下げる
                upper = height;
                lower = height - difference;

                bestHeight = height;
                bestWidth = width;
                bestSegmentCount = segmentCount;
                tempBuffer[..segmentCount].CopyTo(new Span<ColumnSegment>(bestSegmentsBuffer, 0, segmentCount));
            }

            // 【段階 1】バイナリサーチ法
            int iter = 0;
            for (; iter < maxIterations; iter++)
            {
                // 上限のちょっと下試す (最適値なら失敗して完了とみなせる)
                var (canFit, width, height, segmentCount) = TryFitColumns(widthLimit, upper - epsilon, tempBuffer);
                if (!canFit)
                {
                    // 配置失敗 → 完了とみなせる
                    break;
                }

                // 配置成功 → 上限を下げる
                upper = height;

                bestHeight = height;
                bestWidth = width;
                bestSegmentCount = segmentCount;
                tempBuffer[..segmentCount].CopyTo(new Span<ColumnSegment>(bestSegmentsBuffer, 0, segmentCount));

                // 中間値を候補高さとして試す
                double mid = (lower + upper) * 0.5;
                (canFit, width, height, segmentCount) = TryFitColumns(widthLimit, mid, tempBuffer);
                if (canFit)
                {
                    // 配置成功 → 上限を下げる
                    upper = height;

                    bestHeight = height;
                    bestWidth = width;
                    bestSegmentCount = segmentCount;
                    tempBuffer[..segmentCount].CopyTo(new Span<ColumnSegment>(bestSegmentsBuffer, 0, segmentCount));
                }
                else
                {
                    // 配置失敗 → 下限を上げる
                    lower = mid;
                }

                // 収束条件チェック
                if ((upper - lower) <= epsilon)
                    break;
            }

            // 反復回数を記録する
            LastIterationCount = iter;  // 反復回数を記録

            // 【段階 2】最良結果を返却
            var finalSegments = new ColumnSegment[bestSegmentCount];
            Array.Copy(bestSegmentsBuffer, finalSegments, bestSegmentCount);
            return new StrategyResult(bestWidth, bestHeight, finalSegments);
        }
        finally
        {
            // ArrayPool を返却する
            ArrayPool<ColumnSegment>.Shared.Return(bestSegmentsBuffer, clearArray: false);
        }
    }

    /// <summary><b>指定の高さで全アイテムが配置可能かどうかを判定します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. 各列を構築しながらアイテムを詰め込む<br/>
    /// 2. 高さ制限を超えたら列を終了<br/>
    /// 3. 全アイテムが配置可能かチェック<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// AggressiveOptimization で最適化されています。<br/>
    /// </remarks>
    /// <param name="widthLimit">幅の制限値。</param>
    /// <param name="heightLimit">各列の高さの制限値。</param>
    /// <param name="outSegmentsBuffer">構築した列セグメントを格納するバッファ。</param>
    /// <returns>（配置可能フラグ、総幅、総高さ、セグメント数）タプル。</returns>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal (bool canFit, double width, double height, int segmentCount) TryFitColumns(
         double widthLimit, double heightLimit, Span<ColumnSegment> outSegmentsBuffer)
    {
        // ホットパス開始：タプルのローカルキャッシング化（メモリレイテンシ削減）
        double spaceRow = space.Row;
        double spaceColumn = space.Column;

        int n = items.Length;
        int segmentCount = 0;
        double totalWidth = 0.0;
        double totalHeight = 0.0;
        int itemIdx = 0;

        // 各列を構築
        while (itemIdx < n && segmentCount < columnLimit)
        {
            // 列の開始インデックスを記録する
            int colStartIdx = itemIdx;
            double colHeight = 0.0;
            double colWidth = 0.0;
            int itemsInCol = 0;

            // この列にアイテムを詰め込む
            while (itemIdx < n)
            {
                // アイテムのサイズを取得する
                (double Width, double Height) = items[itemIdx];

                // 行間スペースを考慮
                double additionalSpace = itemsInCol > 0 ? spaceRow : 0.0;
                double newHeight = colHeight + Height + additionalSpace;

                // 列の高さが heightLimit を超えるなら、この列の終了
                if (newHeight > heightLimit)
                    break;

                // アイテムを列に追加
                colHeight = newHeight;
                if (Width > colWidth)
                    colWidth = Width;
                itemsInCol++;
                itemIdx++;
            }

            // この列にアイテムが配置されなかった場合は失敗（詰まらない）
            if (itemsInCol == 0)
                return (false, totalWidth, totalHeight, segmentCount);

            // メトリクスをキャッシュに登録
            metricsCache.SetMetrics(
                new ColumnSegment(colStartIdx, itemIdx - 1),
                new Size(colWidth, colHeight));

            // バッファに直接書き込み（GC なし）
            outSegmentsBuffer[segmentCount] = new ColumnSegment(colStartIdx, itemIdx - 1);
            segmentCount++;
            totalWidth += colWidth;

            // 列間スペースを追加（最初の列以外）
            if (segmentCount > 1)
                totalWidth += spaceColumn;

            if (colHeight > totalHeight)
                totalHeight = colHeight;

            // 総幅が制限を超えたら失敗
            if (totalWidth > widthLimit)
                return (false, totalWidth, totalHeight, segmentCount);
        }

        // 全アイテムが処理されたかチェック
        if (itemIdx < n)
            return (false, totalWidth, totalHeight, segmentCount);

        // 配置成功を返却する
        return (true, totalWidth, totalHeight, segmentCount);
    }

}
