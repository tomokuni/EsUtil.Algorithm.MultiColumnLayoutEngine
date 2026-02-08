using System;
using System.Collections.Generic;
using System.Linq;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>
/// テスト支援機能を提供するクラス<br/>
/// <br/>
/// 【責務】<br/>
/// • テスト用ランダムアイテム生成<br/>
/// • アルゴリズム品質評価（DP比較）<br/>
/// • レイアウト検証ユーティリティ<br/>
/// <br/>
/// 【用途】<br/>
/// • ユニットテストの準備（テストデータ生成）<br/>
/// • ベンチマークテストでの性能測定<br/>
/// • アルゴリズムの品質比較検証<br/>
/// • 結果の妥当性確認<br/>
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// テスト用のランダムアイテムを生成します<br/>
    /// <br/>
    /// 【生成パラメータ】<br/>
    /// • 幅：10～200 の整数（多様性を高めて Greedy が最適解に一致しにくくする）<br/>
    /// • 高さ：10～500 の整数（多様性を高めて Greedy が最適解に一致しにくくする）<br/>
    /// <br/>
    /// 【用途】<br/>
    /// • ベンチマークテスト<br/>
    /// • 性能検証<br/>
    /// • 再現可能な結果を得るため、シードを使用<br/>
    /// <br/>
    /// 【シードについて】<br/>
    /// デフォルトシード 42 を使用すると、常に同じアイテムが生成されます。<br/>
    /// これにより、テスト結果の再現性が確保されます。<br/>
    /// </summary>
    /// <param name="count">生成するアイテム数</param>
    /// <param name="seed">乱数シード（デフォルト: 42）</param>
    /// <returns>生成されたアイテムリスト</returns>
    public static List<(double Width, double Height)> GenerateRandomItems(int count, int seed = 42)
    {
        var random = new Random(seed);
        return [.. Enumerable.Range(0, count).Select(_ => ((double)random.Next(10, 201), (double)random.Next(10, 501)))];
    }

    /// <summary>
    /// DP法と比較アルゴリズムの品質を計算します<br/>
    /// <br/>
    /// 【計算式】<br/>
    /// Accuracy(%) = (DP値 / Algorithm値) × 100.0<br/>
    /// <br/>
    /// 【解釈】<br/>
    /// • 100%：最適解と同等<br/>
    /// • &lt;100%：Algorithm がより優れている（理論的に不可能 → 実装バグの可能性）<br/>
    /// • >100%：Algorithm がより悪い（DP が最適なので正常）<br/>
    /// <br/>
    /// 【重要】<br/>
    /// DP法は完全な最適化を保証するため、Greedy や BinarySearch の結果が DP より優ることは理論的にあり得ません。<br/>
    /// >100% の値が出現した場合は、実装に問題がある可能性があります。<br/>
    /// </summary>
    /// <param name="dpHeight">DP法の高さ（基準値・最適解）</param>
    /// <param name="algorithmHeight">比較アルゴリズムの高さ</param>
    /// <returns>精度（%）：100% = 同等、>100% = Algorithm がより悪い、&lt;100% = バグの可能性</returns>
    public static double CalculateAccuracy(double dpHeight, double algorithmHeight)
    {
        if (dpHeight == 0.0 || algorithmHeight == 0.0)
            return double.NaN;

        // DP値 / Algorithm値で品質を計算
        // dpHeight == algorithmHeight → 100%（最適解と同等）
        // dpHeight <  algorithmHeight → <100%（Algorithm がより優れた結果 = 理論的に不可能）
        // dpHeight >  algorithmHeight → >100%（Algorithm がより悪い = 正常）
        double accuracy = (dpHeight / algorithmHeight) * 100.0;

        return accuracy;
    }

    /// <summary>
    /// 列セグメントの構造を検証します<br/>
    /// <br/>
    /// 【検証項目】<br/>
    /// 1. 全列が連続している（gap がない）<br/>
    /// 2. 最初の列は index 0 から開始<br/>
    /// 3. 最後の列は最後のアイテムで終了<br/>
    /// <br/>
    /// 【例】<br/>
    /// 有効: [(0, 9), (10, 19), (20, 29)]<br/>
    /// 無効: [(0, 9), (10, 19)]  → 最後のアイテムで終了していない<br/>
    /// 無効: [(0, 5), (10, 19)]  → gap がある（6-9 が欠落）<br/>
    /// </summary>
    /// <param name="segment">列セグメント配列</param>
    /// <param name="itemCount">アイテム総数</param>
    /// <returns>検証成功なら true</returns>
    public static bool ValidateColumnSegment((int StartIdx, int EndIdx)[] segment, int itemCount)
    {
        if (segment.Length == 0)
            return itemCount == 0;

        // 最初の列が 0 から開始、最後の列がアイテム終了で終了
        if (segment[0].StartIdx != 0 || segment[^1].EndIdx != itemCount - 1)
            return false;

        // 全列が連続しているか確認
        for (int i = 1; i < segment.Length; i++)
        {
            if (segment[i].StartIdx != segment[i - 1].EndIdx + 1)
                return false;
        }

        return true;
    }

    /// <summary>
    /// usedWidth が正しく計算されているか検証します<br/>
    /// <br/>
    /// 【検証内容】<br/>
    /// ? 各列の幅を計算し、列間スペースを含めた総幅がusedWidthと一致するか確認<br/>
    /// <br/>
    /// 【計算プロセス】<br/>
    /// 1. ColumnSegments から各列を取得<br/>
    /// 2. 各列の幅（最大アイテム幅）を計算<br/>
    /// 3. 列間スペースを加算<br/>
    /// 4. 総幅を求める<br/>
    /// 5. 計算値と usedWidth が一致するか確認<br/>
    /// </summary>
    /// <param name="usedWidth">レイアウトの使用幅</param>
    /// <param name="items">アイテムリスト</param>
    /// <param name="segment">列セグメント配列</param>
    /// <param name="space">スペース設定</param>
    /// <returns>検証成功なら true</returns>
    public static bool ValidateUsedWidth(double usedWidth, IReadOnlyList<(double Width, double Height)> items, IReadOnlyList<(int, int)> segment, (double Row, double Column) space)
    {
        if (segment.Count == 0)
            return false;

        double totalWidth = 0.0;

        // 各列の幅を計算
        foreach (var (startIdx, endIdx) in segment)
        {
            double maxWidth = 0.0;
            for (int i = startIdx; i <= endIdx; i++)
            {
                if (items[i].Width > maxWidth)
                    maxWidth = items[i].Width;
            }
            totalWidth += maxWidth;
        }

        // 列間スペースを追加
        totalWidth += (segment.Count - 1) * space.Column;

        return totalWidth == usedWidth;
    }


    /// <summary>
    /// 標準的なテスト用アイテムセットを取得します（単列テスト用）<br/>
    /// <br/>
    /// 【構成】<br/>
    /// • アイテム数：3個<br/>
    /// • 幅：40, 50, 60<br/>
    /// • 高さ：100, 120, 140<br/>
    /// </summary>
    /// <returns>標準テスト用アイテムリスト</returns>
    public static List<(double Width, double Height)> GetStandardTestItems()
    {
        return
        [
            (40.0, 100.0),
            (50.0, 120.0),
            (60.0, 140.0)
        ];
    }

    /// <summary>
    /// 複数列配置用の大規模テストアイテムセットを取得します<br/>
    /// <br/>
    /// 【構成】<br/>
    /// • アイテム数：10個<br/>
    /// • 幅：30～50<br/>
    /// • 高さ：80～150<br/>
    /// </summary>
    /// <returns>複数列配置用テストアイテムリスト</returns>
    public static List<(double Width, double Height)> GetLargeTestItems()
    {
        return
        [
            (30.0, 80.0),
            (35.0, 90.0),
            (40.0, 100.0),
            (45.0, 110.0),
            (50.0, 120.0),
            (32.0, 95.0),
            (38.0, 105.0),
            (42.0, 115.0),
            (48.0, 125.0),
            (50.0, 150.0)
        ];
    }

    /// <summary>
    /// 均一なアイテムセット（テスト結果が予測可能）を取得します<br/>
    /// <br/>
    /// 【構成】<br/>
    /// • アイテム数：6個<br/>
    /// • 全アイテム共通：幅 50、高さ 100<br/>
    /// <br/>
    /// 【用途】<br/>
    /// • 出力値の計算が容易<br/>
    /// • アルゴリズムの動作検証<br/>
    /// </summary>
    /// <returns>均一テストアイテムリスト</returns>
    public static List<(double Width, double Height)> GetUniformTestItems()
    {
        return [.. Enumerable.Range(0, 6).Select(_ => (50.0, 100.0))];
    }

    /// <summary>
    /// 極端な幅のアイテムセット（エッジケーステスト用）を取得します<br/>
    /// <br/>
    /// 【構成】<br/>
    /// • アイテム数：4個<br/>
    /// • 幅：10（小）、100（大）を交互に配置<br/>
    /// • 高さ：50～150<br/>
    /// </summary>
    /// <returns>極端な幅のテストアイテムリスト</returns>
    public static List<(double Width, double Height)> GetExtremeWidthItems()
    {
        return
        [
            (10.0, 50.0),
            (100.0, 100.0),
            (10.0, 75.0),
            (100.0, 150.0)
        ];
    }

    /// <summary>
    /// 極端な高さのアイテムセット（エッジケーステスト用）を取得します<br/>
    /// <br/>
    /// 【構成】<br/>
    /// • アイテム数：4個<br/>
    /// • 幅：40～60<br/>
    /// • 高さ：10（小）、500（大）を交互に配置<br/>
    /// </summary>
    /// <returns>極端な高さのテストアイテムリスト</returns>
    public static List<(double Width, double Height)> GetExtremeHeightItems()
    {
        return
        [
            (40.0, 10.0),
            (50.0, 500.0),
            (55.0, 15.0),
            (60.0, 450.0)
        ];
    }

    /// <summary>
    /// 単一アイテムセット（最小ケース）を取得します<br/>
    /// <br/>
    /// 【構成】<br/>
    /// • アイテム数：1個<br/>
    /// • 幅：50、高さ：100<br/>
    /// </summary>
    /// <returns>単一アイテムのテストリスト</returns>
    public static List<(double Width, double Height)> GetSingleItem()
    {
        return [(50.0, 100.0)];
    }

    /// <summary>
    /// 指定本数のアイテムセットを生成します（カスタムテスト用）<br/>
    /// <br/>
    /// 【生成ルール】<br/>
    /// • 幅：40 ～ (40 + count × 2) までの値<br/>
    /// • 高さ：100 ～ (100 + count × 10) までの値<br/>
    /// </summary>
    /// <param name="count">生成するアイテム数</param>
    /// <returns>生成されたアイテムリスト</returns>
    public static List<(double Width, double Height)> GenerateTestItems(int count)
    {
        return [.. Enumerable.Range(0, count).Select(i => (40.0 + i * 2.0, 100.0 + i * 10.0))];
    }

    /// <summary>
    /// StrategyResult の最大列高さが、各列の実計算高さと一致するか検証します<br/>
    /// <br/>
    /// 【検証内容】<br/>
    /// • 各列の実高さを計算<br/>
    /// • 最大値が result.MinHeight と一致するか確認<br/>
    /// </summary>
    /// <param name="MinHeight">最小高さ</param>
    /// <param name="segments">レイアウト計算結果の ColumnSegments（(StartIdx, EndIdx) 配列）</param>
    /// <param name="items">アイテムリスト</param>
    /// <param name="space">スペース設定</param>
    /// <returns>検証成功なら true</returns>
    public static bool ValidateMinHeight(double MinHeight, (int StartIdx, int EndIdx)[] segments, IReadOnlyList<(double Width, double Height)> items, (double Row, double Column) space)
    {
        if (segments.Length == 0)
            return MinHeight == 0.0;

        double maxHeight = 0.0;

        foreach (var (startIdx, endIdx) in segments)
        {
            double colHeight = 0.0;

            for (int i = startIdx; i <= endIdx; i++)
            {
                colHeight += items[i].Height;
                if (i > startIdx)
                    colHeight += space.Row;
            }

            if (colHeight > maxHeight)
                maxHeight = colHeight;
        }

        return Math.Abs(MinHeight - maxHeight) < 1e-9;
    }

    /// <summary>
    /// 各列のアイテム配置順序が正しいか検証します<br/>
    /// <br/>
    /// 【検証内容】<br/>
    /// • セグメント内の startIdx &lt;= endIdx<br/>
    /// • セグメントの順序が昇順<br/>
    /// • 各セグメント内のアイテムインデックスが有効範囲内<br/>
    /// </summary>
    /// <param name="segments">レイアウト計算結果の ColumnSegments（(StartIdx, EndIdx) 配列）</param>
    /// <param name="itemCount">アイテム総数</param>
    /// <returns>検証成功なら true</returns>
    public static bool ValidateSegmentOrder((int StartIdx, int EndIdx)[] segments, int itemCount)
    {
        if (segments.Length == 0)
            return itemCount == 0;

        for (int i = 0; i < segments.Length; i++)
        {
            var (startIdx, endIdx) = segments[i];

            // 各セグメントの妥当性
            if (startIdx < 0 || endIdx >= itemCount || startIdx > endIdx)
                return false;

            // セグメントの順序チェック
            if (i > 0 && segments[i - 1].EndIdx >= startIdx)
                return false;
        }

        return true;
    }

    /// <summary>
    /// アルゴリズム間の結果品質を比較します<br/>
    /// <br/>
    /// 【比較方法】<br/>
    /// • DP法を基準として、Greedy と BinarySearch の精度を計算<br/>
    /// • 品質低下が許容範囲内か判定<br/>
    /// </summary>
    /// <param name="dpHeight">DP法の最大列高さ</param>
    /// <param name="greedyHeight">Greedy法の最大列高さ</param>
    /// <param name="bsearchHeight">BinarySearch法の最大列高さ</param>
    /// <param name="tolerancePercent">許容誤差率（デフォルト: 5%）</param>
    /// <returns>全て許容範囲内なら true</returns>
    public static bool ValidateAlgorithmQuality(double dpHeight, double greedyHeight, double bsearchHeight, double tolerancePercent = 5.0)
    {
        if (dpHeight == 0.0)
            return greedyHeight == 0.0 && bsearchHeight == 0.0;

        double maxTolerance = dpHeight * tolerancePercent / 100.0;

        bool greedyValid = (greedyHeight - dpHeight) <= maxTolerance;
        bool bsearchValid = (bsearchHeight - dpHeight) <= maxTolerance;

        return greedyValid && bsearchValid;
    }

    /// <summary>
    /// レイアウト計算結果が物理的に妥当か総合的に検証します<br/>
    /// <br/>
    /// 【検証項目】<br/>
    /// 1. ColumnSegments が連続し、全アイテムをカバー<br/>
    /// 2. MinHeight が各列の実計算高さの最大値と一致<br/>
    /// 3. UsedWidth が制約内で正しく計算<br/>
    /// 4. アイテム配置が重複していない<br/>
    /// </summary>
    /// <param name="MinHeight">最小高さ</param>
    /// <param name="UsedWidth">使用幅</param>
    /// <param name="segments">レイアウト計算結果の ColumnSegments（(StartIdx, EndIdx) 配列）</param>
    /// <param name="items">アイテムリスト</param>
    /// <param name="space">スペース設定</param>
    /// <param name="widthLimit">幅の上限</param>
    /// <returns>全検証が成功したら true</returns>
    public static bool ValidateLayoutComprehensive(double MinHeight, double UsedWidth, (int StartIdx, int EndIdx)[] segments, IReadOnlyList<(double Width, double Height)> items, (double Row, double Column) space, double widthLimit)
    {
        // 検証1：セグメントの連続性
        if (!ValidateColumnSegment(segments, items.Count))
            return false;

        // 検証2：MinHeight の正当性
        if (!ValidateMinHeight(MinHeight, segments, items, space))
            return false;

        // 検証3：セグメント順序
        if (!ValidateSegmentOrder(segments, items.Count))
            return false;

        // 検証4：幅チェック
        if (UsedWidth > widthLimit)
            return false;

        return true;
    }
}

