using System;
using System.Collections.Generic;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineInner;

/// <summary><b>ヘルパーメソッドを提供するユーティリティクラス</b></summary>
/// <remarks>
/// 【概要】<br/>
/// レイアウトエンジンで使用する検証や計算補助メソッドを集めた静的クラスです。<br/>
/// </remarks>
internal static class Helper
{

    /// <summary><b>値が非負数の有限値かを判定します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. 値が負数かチェック<br/>
    /// 2. NaN または無限大かチェック<br/>
    /// 3. 全ての条件を満たせば true<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 非負数とは 0 以上を意味します。<br/>
    /// </remarks>
    /// <param name="value">判定対象の値</param>
    /// <returns>値が非負数の有限値の場合に true を返します。</returns>
    internal static bool IsNonNegativeFinite(double value)
    {
        // 値が負数、NaN、無限大でないかをチェックする
        return !(value < 0.0 || double.IsNaN(value) || double.IsInfinity(value));
    }

    /// <summary><b>値が正の有限値かを判定します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. 値が正でないかチェック<br/>
    /// 2. NaN または無限大かチェック<br/>
    /// 3. 全ての条件を満たせば true<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 正の値とは 0 より大きいことを意味します。<br/>
    /// </remarks>
    /// <param name="value">判定対象の値</param>
    /// <returns>値が正の有限値の場合に true を返します。</returns>
    internal static bool IsPositiveFinite(double value)
    {
        // 値が正でない、NaN、無限大でないかをチェックする
        return !(value <= 0.0 || double.IsNaN(value) || double.IsInfinity(value));
    }

    /// <summary><b>入力パラメータ（コンストラクタ用）の妥当性を検証します</b></summary>
    /// <remarks>
    /// 【検証順序】<br/>
    /// 1. items が null でないか<br/>
    /// 2. columnLimit が正の値か<br/>
    /// 3. space.Row が非負か<br/>
    /// 4. space.Column が非負か<br/>
    /// 5. 各アイテムの幅・高さが正の値か<br/>
    /// <br/>
    /// 【検証内容】<br/>
    /// NaN（非数）および無限大チェックを含む厳密な検証を実施<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 検証失敗時は ArgumentException をスローします。<br/>
    /// </remarks>
    /// <param name="items">アイテムリスト（null 不可）</param>
    /// <param name="space">スペース設定（非負値）</param>
    /// <param name="columnLimit">列数上限（正の値）</param>
    /// <exception cref="ArgumentException">検証失敗時に発生</exception>
    internal static void ValidateParameter(
        IReadOnlyList<(double Width, double Height)> items,
        (double Row, double Column) space,
        int columnLimit)
    {
        // 検証 1：items が null でないか
        if (items is null)
            throw new ArgumentException("items cannot be null.");

        // 検証 2：columnLimit の妥当性
        if (columnLimit <= 0)
            throw new ArgumentException("columnLimit must be positive.");

        // 検証 3：space.Row の妥当性
        if (!IsNonNegativeFinite(space.Row))
            throw new ArgumentException("space.Row must not be negative.");

        // 検証 4：space.Column の妥当性
        if (!IsNonNegativeFinite(space.Column))
            throw new ArgumentException("space.Column must not be negative.");

        // 検証 5：各アイテムの幅・高さ妥当性
        for (int i = 0; i < items.Count; i++)
        {
            var (Width, Height) = items[i];
            if (Width <= 0.0 || double.IsNaN(Width) || double.IsInfinity(Width)
                || Height <= 0.0 || double.IsNaN(Height) || double.IsInfinity(Height))
            {
                throw new ArgumentException($"all items must have positive width and height. (idx={i} is invalid).");
            }
        }
    }

    /// <summary><b>入力パラメータ（Solve メソッド用）の妥当性を検証します</b></summary>
    /// <remarks>
    /// 【検証内容】<br/>
    /// NaN（非数）および無限大チェックを含む厳密な検証を実施<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 検証失敗時は ArgumentException をスローします。<br/>
    /// </remarks>
    /// <param name="widthLimit">使用可能な幅の上限（必須、正の値）</param>
    /// <exception cref="ArgumentException">検証失敗時に発生</exception>
    internal static void ValidateParameter(double widthLimit)
    {
        // 検証 1：widthLimit の妥当性
        if (widthLimit <= 0.0 || double.IsNaN(widthLimit) || double.IsInfinity(widthLimit))
            throw new ArgumentException("widthLimit must be positive.");
    }

    /// <summary><b>レイアウト計算結果の正当性を検証します</b></summary>
    /// <remarks>
    /// 【検証項目】<br/>
    /// 1. UsedWidth ≤ widthLimit<br/>
    /// 2. MinHeight ≤ 単列レイアウト高さ<br/>
    /// <br/>
    /// 【検証の意図】<br/>
    /// • 検証 1：使用幅が制限を超えていない（物理的制約）<br/>
    /// • 検証 2：複数列レイアウトが単列より劣化していない（品質保証）<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 検証失敗時は InvalidOperationException をスローします。<br/>
    /// </remarks>
    /// <param name="lastSolveResult">最後のSolve結果</param>
    /// <param name="widthLimit">幅の制限値</param>
    /// <param name="metricsCache">列メトリクスキャッシュ</param>
    /// <param name="itemsLength">アイテム数</param>
    /// <exception cref="InvalidOperationException">
    /// 検証失敗時に発生（詳細メッセージ付き）<br/>
    /// • "width limit exceeded: {widthLimit} &lt; {UsedWidth}"<br/>
    /// • "minHeight exceeds single column height: {MinHeight} &gt; {singleColHeight}"
    /// </exception>
    internal static void VerifyLayoutResult(StrategyResult lastSolveResult, double widthLimit, ColumnMetricsCache metricsCache, int itemsLength)
    {
        // 検証 1：使用幅が制限を超えないか
        if (lastSolveResult.UsedWidth > widthLimit)
            throw new InvalidOperationException(
                $"width limit exceeded: {widthLimit} < {lastSolveResult.UsedWidth}");

        // 検証 2：最大列高さが単列配置より大きくないか
        var metrics = metricsCache.CalcMetrics(new(0, itemsLength - 1));
        if (lastSolveResult.MinHeight > metrics.Height)
            throw new InvalidOperationException(
                $"minHeight exceeds single column height: {lastSolveResult.MinHeight} > {metrics.Height}");
    }

}
