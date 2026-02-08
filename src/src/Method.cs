namespace EsUtil.Algorithm;

public partial class MultiColumnLayoutEngine
{

    /// <summary><b>使用するレイアウトアルゴリズムを指定する列挙型</b></summary>
    public enum Method
    {
        /// <summary><b>動的計画法：最適解を保証（計算量 O(n² × m)）</b></summary>
        DynamicProgramming,

        /// <summary><b>Greedy 近似法：高速計算（計算量 O(n × m)、品質 90%+）</b></summary>
        Greedy,

        /// <summary><b>バイナリサーチ法：バランス型（計算量 O(n × log(h))、品質 99%+）</b></summary>
        BinarySearch
    }

}
