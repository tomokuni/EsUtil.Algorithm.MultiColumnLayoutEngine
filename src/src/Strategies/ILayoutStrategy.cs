using System;

using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineInner;

/// <summary><b>レイアウト計算戦略の共通インターフェース</b></summary>
/// <remarks>
/// 【概要】<br/>
/// Strategy パターンを使用して複数のアルゴリズムを統一的に扱うための基本インターフェースです。<br/>
/// <br/>
/// 【実装の詳細】<br/>
/// 各 Strategy クラスがこのインターフェースを実装し、
/// Factory パターンでインスタンス生成されます。<br/>
/// <br/>
/// 【制約】<br/>
/// 実装クラスは internal であり、外部公開されません。<br/>
/// <br/>
/// 【注意点】<br/>
/// GetMetadata は型安全にメタデータを取得します。<br/>
/// <br/>
/// 【使用例】<br/>
/// var strategy = factory.GetStrategy();<br/>
/// var result = strategy.Solve(widthLimit);<br/>
/// <br/>
/// 【最適化手法】<br/>
/// • ColumnMetricsCache の共有で計算コスト削減<br/>
/// </remarks>
internal interface ILayoutStrategy
{
    /// <summary><b>指定の条件でレイアウトを計算します</b></summary>
    /// <remarks>
    /// 【処理内容】<br/>
    /// このメソッド内で ColumnMetricsCache（コンストラクタで共有）を使用し、
    /// メトリクスの計算結果をキャッシュから取得します。<br/>
    /// 同じセグメントを複数 Strategy で計算する場合、
    /// キャッシュから O(1) で返却されます。<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// widthLimit は正の値であることを前提とします。<br/>
    /// </remarks>
    /// <param name="widthLimit">使用可能な幅の上限</param>
    /// <returns>計算されたレイアウト結果</returns>
    public StrategyResult Solve(double widthLimit);

    /// <summary><b>戦略固有のメトリクス情報を取得します（型安全、値型のみ）</b></summary>
    /// <remarks>
    /// 【用途】<br/>
    /// • BinarySearch の反復回数取得<br/>
    /// <br/>
    /// 【型制約】<br/>
    /// • 値型（struct）のみサポート<br/>
    /// • Nullable&lt;T&gt; で null 許容<br/>
    /// <br/>
    /// 【実装例】<br/>
    /// • BinarySearchLayoutStrategy：反復回数（int 型）を返却<br/>
    /// • その他：null を返却<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// サポートされていない型の場合、null を返します。<br/>
    /// </remarks>
    /// <typeparam name="T">取得するメタデータの型（struct のみ）</typeparam>
    /// <param name="key">メトリクスキー</param>
    /// <returns>メタデータ値、または null</returns>
    T? GetMetadata<T>(MetadataKey key) where T : IEquatable<T>;

}
