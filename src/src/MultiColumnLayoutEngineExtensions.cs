using System;

using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm;

/// <summary><b>MultiColumnLayoutEngine の状態参照とアルゴリズム指定を拡張メンバーで提供します</b></summary>
/// <remarks>
/// 【概要】<br/>
/// C# 14 の拡張メンバー（extension ブロック）で実装しています。<br/>
/// MultiColumnLayoutEngine 本体の型を変更せずに、メソッドとプロパティを型へ追加できます。<br/>
/// <br/>
/// 【提供機能】<br/>
/// • Solve(widthLimit, method)：アルゴリズムを引数で指定して 1 回だけ解く（CurrentMethod は呼び出し前へ戻る）<br/>
/// • ItemCount：登録されているアイテム数<br/>
/// • ColumnCount：最後の Solve() で確定した列数<br/>
/// • IterationCount：最後の BinarySearch の反復回数（未実行・対象外は 0）<br/>
/// <br/>
/// 【特徴】<br/>
/// • 既存のインスタンス メソッド Solve(double) とは引数の個数が異なるため、既存の呼び出しはそのまま動作します。<br/>
/// • 拡張プロパティは C# 14 で追加された機能で、従来の拡張メソッド（メソッドのみ）では表現できません。<br/>
/// <br/>
/// 【注意点】<br/>
/// • using EsUtil.Algorithm; のみで利用できます（拡張メソッド用の追加の using は不要）。<br/>
/// • Solve(widthLimit, method) は内部で CurrentMethod を切り替えるため、スレッド セーフではありません。<br/>
/// <br/>
/// 【使用例】<br/>
/// var engine = new MultiColumnLayoutEngine(items, (1.0, 2.0), 10);<br/>
/// var (width, height) = engine.Solve(100.0, Method.Greedy); // 1 回だけ Greedy で解く<br/>
/// int columns = engine.ColumnCount;<br/>
/// <br/>
/// 【最適化手法】<br/>
/// • プロパティは既存の公開 API（GetLastColumnSegments 等）の薄いラッパーとし、追加の計算を行いません。<br/>
/// </remarks>
public static class MultiColumnLayoutEngineExtensions
{
    extension(MultiColumnLayoutEngine engine)
    {
        /// <summary><b>指定したアルゴリズムで 1 回だけレイアウトを計算します</b></summary>
        /// <remarks>
        /// 【処理フロー】<br/>
        /// 1. 呼び出し前の CurrentMethod を退避<br/>
        /// 2. CurrentMethod を引数の method へ変更<br/>
        /// 3. Solve(widthLimit) を呼び出す<br/>
        /// 4. CurrentMethod を退避した値へ戻す（例外時も finally で戻す）<br/>
        /// <br/>
        /// 【注意点】<br/>
        /// • 例外が発生した場合も CurrentMethod は元に戻ります。<br/>
        /// • 戻り値は Solve(double) と同一です。<br/>
        /// </remarks>
        /// <param name="widthLimit">使用可能な幅の上限（正の値）</param>
        /// <param name="method">使用するアルゴリズム</param>
        /// <returns>(UsedWidth, MinHeight) タプル</returns>
        /// <exception cref="ArgumentException">widthLimit が無効な場合</exception>
        /// <exception cref="InvalidOperationException">出力値の検証に失敗した場合</exception>
        public (double UsedWidth, double MinHeight) Solve(double widthLimit, Method method)
        {
            // 呼び出し前のアルゴリズムを退避する（呼び出し後に元へ戻すため）
            var previousMethod = engine.CurrentMethod;

            try
            {
                engine.CurrentMethod = method;
                return engine.Solve(widthLimit);
            }
            finally
            {
                // 例外時も含めて、元のアルゴリズムへ戻す
                engine.CurrentMethod = previousMethod;
            }
        }

        /// <summary><b>登録されているアイテム数を取得します</b></summary>
        /// <remarks>
        /// 【処理フロー】<br/>
        /// 1. 内部のアイテム配列の長さを返却<br/>
        /// <br/>
        /// 【注意点】<br/>
        /// コンストラクタで受け取った時点の件数で固定です。<br/>
        /// </remarks>
        public int ItemCount => engine._items.Length;

        /// <summary><b>最後の Solve() 実行時の列数を取得します</b></summary>
        /// <remarks>
        /// 【処理フロー】<br/>
        /// 1. GetLastColumnSegments() の要素数を返却<br/>
        /// <br/>
        /// 【注意点】<br/>
        /// Solve() 未実行の場合は 0 です。<br/>
        /// </remarks>
        public int ColumnCount => engine.GetLastColumnSegments().Length;

        /// <summary><b>最後の BinarySearch 実行時の反復回数を取得します</b></summary>
        /// <remarks>
        /// 【処理フロー】<br/>
        /// 1. GetBinarySearchIterationCount() の null を 0 へ変換して返却<br/>
        /// <br/>
        /// 【注意点】<br/>
        /// Strategy が未生成、または BinarySearch 以外の実行の場合は 0 です。<br/>
        /// </remarks>
        public int IterationCount => engine.GetBinarySearchIterationCount() ?? 0;
    }
}
