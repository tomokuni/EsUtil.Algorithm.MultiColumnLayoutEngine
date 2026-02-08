using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineInner;

/// <summary><b>レイアウト計算の結果を格納するレコード構造体</b></summary>
/// <remarks>
/// 【制約】<br/>
/// UsedWidth と MinHeight は非負の有限値であることを想定。<br/>
/// ColumnSegments は null でない配列。<br/>
/// <br/>
/// Empty インスタンスはデフォルト値として使用可能。<br/>
/// </remarks>
internal readonly record struct StrategyResult(double UsedWidth, double MinHeight, ColumnSegment[] ColumnSegments)
{
    /// <summary><b>空のレイアウト結果</b></summary>
    public static readonly StrategyResult Empty = new(0.0, 0.0, []);
}

/// <summary><b>各列のアイテム範囲を表すレコード構造体</b></summary>
/// <remarks>
/// 【制約】<br/>
/// StartIdx ≤ EndIdx で有効な範囲を表す。<br/>
/// インデックスは 0-based。<br/>
/// </remarks>
internal readonly record struct ColumnSegment(int StartIdx, int EndIdx);

/// <summary><b>行間スペースと列間スペースを表すレコード構造体</b></summary>
/// <remarks>
/// 【制約】<br/>
/// Row と Column は非負値。<br/>
/// デフォルト値は (0.0, 0.0)。<br/>
/// </remarks>
internal readonly record struct Space(double Row, double Column);

/// <summary><b>幅と高さを表すサイズ情報を格納するレコード構造体</b></summary>
/// <remarks>
/// 【制約】<br/>
/// Width と Height は正の値。<br/>
/// 【最適化手法】<br/>
/// • AggressiveInlining でメソッド最適化<br/>
/// </remarks>
internal readonly record struct Size(double Width, double Height)
{
    /// <summary><b>IReadOnlyList から 配列に変換します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. Size 配列を新規作成<br/>
    /// 2. 各タプルを Size に変換して代入<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// 入力が null の場合、例外が発生します。<br/>
    /// </remarks>
    /// <param name="items">（幅, 高さ）タプルのリスト</param>
    /// <returns>Size 配列</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size[] ToSizeArray(IReadOnlyList<(double Width, double Height)> items)
    {
        // Size 配列を新規作成する
        var sizes = new Size[items.Count];
        // 各タプルを Size に変換して代入する
        for (int i = 0; i < items.Count; i++)
        {
            var (Width, Height) = items[i];
            sizes[i] = new(Width, Height);
        }
        return sizes;
    }

    /// <summary><b>ReadOnlySpan から 配列に変換します</b></summary>
    /// <remarks>
    /// 【処理フロー】<br/>
    /// 1. Size 配列を新規作成<br/>
    /// 2. 各タプルを Size に変換して代入<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// ReadOnlySpan を使用して範囲アクセス。<br/>
    /// </remarks>
    /// <param name="items">（幅, 高さ）タプルのリスト</param>
    /// <returns>Size 配列</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Size[] ToSizeArray(ReadOnlySpan<(double Width, double Height)> items)
    {
        // Size 配列を新規作成する
        var sizes = new Size[items.Length];
        // 各タプルを Size に変換して代入する
        for (int i = 0; i < items.Length; i++)
        {
            var (Width, Height) = items[i];
            sizes[i] = new(Width, Height);
        }
        return sizes;
    }
}
