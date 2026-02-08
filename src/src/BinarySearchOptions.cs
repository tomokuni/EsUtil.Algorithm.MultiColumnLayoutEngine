using System;

using EsUtil.Algorithm.MultiColumnLayoutEngineInner;

namespace EsUtil.Algorithm;

public partial class MultiColumnLayoutEngine
{

    /// <summary><b>バイナリサーチアルゴリズムの動作制御オプション</b></summary>
    /// <remarks>
    /// 【概要】<br/>
    /// BinarySearchLayoutStrategy の動作を制御するオプションを定義します。<br/>
    /// 収束精度、反復回数、下限比率を設定できます。<br/>
    /// <br/>
    /// 【フィールド説明】<br/>
    /// • Epsilon：二分探索の収束判定の許容誤差<br/>
    ///   - 小さいほど精度が高い（計算時間が長くなる）<br/>
    ///   - デフォルト：1e-3（0.001）<br/>
    ///   - 制約：value > 0.0<br/>
    /// <br/>
    /// • MaxIterations：二分探索の最大反復回数<br/>
    ///   - 上限を設定して無限ループ防止<br/>
    ///   - デフォルト：100<br/>
    ///   - 制約：value > 0<br/>
    /// <br/>
    /// • LowerBoundRatio：二分探索の下限比率<br/>
    ///   - Greedy 結果を上限として、この値を掛けた値が下限となる<br/>
    ///   - デフォルト：0.95<br/>
    ///   - 制約：value > 0<br/>
    /// <br/>
    /// 【注意点】<br/>
    /// Epsilon が小さすぎると計算時間が長くなる。<br/>
    /// MaxIterations で反復回数を制限。<br/>
    /// </remarks>
    public readonly record struct BinarySearchOptions(
        double Epsilon = 1e-3,
        int MaxIterations = 100,
        double LowerBoundRatio = 0.95)
    {

        /// <summary><b>コンストラクタ（デフォルト値）</b></summary>
        public BinarySearchOptions() : this(1e-3, 100, 0.95) { }

        /// <summary><b>デフォルトのオプション値</b></summary>
        public static readonly BinarySearchOptions Default = new(1e-3, 100, 0.95);

        /// <summary><b>オプション設定の妥当性を検証します</b></summary>
        /// <remarks>
        /// 【処理フロー】<br/>
        /// 1. Epsilon が正の値かチェック<br/>
        /// 2. MaxIterations が正の値かチェック<br/>
        /// 3. LowerBoundRatio が正の値かチェック<br/>
        /// <br/>
        /// 【注意点】<br/>
        /// 無効な場合、ArgumentException をスロー。<br/>
        /// </remarks>
        /// <exception cref="ArgumentException">オプションが無効な場合</exception>
        public void IsValid()
        {
            // オプション設定の妥当性
            if (!Helper.IsPositiveFinite(Epsilon))
                throw new ArgumentException("CurrentBinarySearchOptions.Epsilon must be positive.");
            if (!Helper.IsPositiveFinite(MaxIterations))
                throw new ArgumentException("CurrentBinarySearchOptions.MaxIterations must be positive.");
            if (!Helper.IsPositiveFinite(LowerBoundRatio))
                throw new ArgumentException("CurrentBinarySearchOptions.LowerBoundRatio must be positive.");
        }

    }

}
