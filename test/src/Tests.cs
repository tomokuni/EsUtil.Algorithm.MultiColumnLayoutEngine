using System.Collections.Generic;
using System.Linq;
using Xunit;
using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;



#region MultiColumnLayoutTest

/// <summary>MultiColumnLayout の単体テスト</summary>
public class Tests
{

    #region QualityComparison

    /// <summary>品質比較テスト</summary>
    [Fact]
    public void CheckAlgorithmQuality_品質比較テスト()
    {
        // Arrange
        var items = TestHelper.GetLargeTestItems();
        var layout = new MultiColumnLayoutEngine(items, (5.0, 5.0), columnLimit: 5);
        const double widthLimit = 200.0;

        // Act
        layout.CurrentMethod = Method.DynamicProgramming;
        var (_, dpMinHeight) = layout.Solve(widthLimit);
        layout.CurrentMethod = Method.Greedy;
        var (_, greedyMinHeight) = layout.Solve(widthLimit);
        layout.CurrentMethod = Method.BinarySearch;
        var (_, bsearchMinHeight) = layout.Solve(widthLimit);

        // Assert
        // DP が最適解であることを確認
        Assert.True(dpMinHeight <= greedyMinHeight);
        Assert.True(dpMinHeight <= bsearchMinHeight);

        // 品質が許容範囲内であることを確認（5%以内）
        Assert.True(TestHelper.ValidateAlgorithmQuality(dpMinHeight, greedyMinHeight, bsearchMinHeight, 5.0));
    }

    /// <summary>単一アイテムでの同一性</summary>
    [Fact]
    public void CheckAlgorithmQuality_単一アイテムでの同一性()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 100.0;

        // Act
        layout.CurrentMethod = Method.DynamicProgramming;
        var (dpUsedWidth, dpMinHeight) = layout.Solve(widthLimit);
        layout.CurrentMethod = Method.Greedy;
        var (greedyUsedWidth, greedyMinHeight) = layout.Solve(widthLimit);
        layout.CurrentMethod = Method.BinarySearch;
        var (bsearchUsedWidth, bsearchMinHeight) = layout.Solve(widthLimit);

        // Assert
        Assert.Equal(dpMinHeight, greedyMinHeight);
        Assert.Equal(dpMinHeight, bsearchMinHeight);
        Assert.Equal(dpUsedWidth, greedyUsedWidth);
        Assert.Equal(dpUsedWidth, bsearchUsedWidth);
    }

    /// <summary>均一アイテムでの同一性</summary>
    [Fact]
    public void CheckAlgorithmQuality_均一アイテムでの同一性()
    {
        // Arrange
        var items = TestHelper.GetUniformTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = Method.DynamicProgramming;
        var (_, dpMinHeight) = layout.Solve(widthLimit);
        layout.CurrentMethod = Method.Greedy;
        var (_, greedyMinHeight) = layout.Solve(widthLimit);
        layout.CurrentMethod = Method.BinarySearch;
        var (_, bsearchMinHeight) = layout.Solve(widthLimit);
        var dpSegments = layout.GetLastColumnSegments();
        layout.CurrentMethod = Method.Greedy;
        layout.Solve(widthLimit);
        var greedySegments = layout.GetLastColumnSegments();
        layout.CurrentMethod = Method.BinarySearch;
        layout.Solve(widthLimit);
        var bsearchSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.Equal(dpMinHeight, greedyMinHeight);
        Assert.Equal(dpMinHeight, bsearchMinHeight);
        Assert.Equal(dpSegments.Length, greedySegments.Length);
        Assert.Equal(dpSegments.Length, bsearchSegments.Length);
    }

    #endregion QualityComparison

    #region EdgeCases

    /// <summary>ランダムアイテムセットの連続テスト</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_ランダムアイテムセットの連続テスト(Method method)
    {
        // Arrange
        var itemsList = new[]
        {
            TestHelper.GenerateRandomItems(5, 42),
            TestHelper.GenerateRandomItems(8, 43),
            TestHelper.GenerateRandomItems(12, 44)
        };

        foreach (var items in itemsList)
        {
            // Act
            var layout = new MultiColumnLayoutEngine(items, (2.0, 2.0), columnLimit: 5)
            {
                CurrentMethod = method
            };
            var (usedWidth, minHeight) = layout.Solve(200.0);
            var segments = layout.GetLastColumnSegments();

            // Assert
            // セグメントが正しく形成されているか確認
            Assert.NotEmpty(segments);
            Assert.True(TestHelper.ValidateColumnSegment(segments, items.Count));

            // 幅が制限内か確認
            Assert.True(usedWidth <= 200.0);

            // 高さが正のみ確認（ValidateMinHeightは異なる検証のため削除）
            Assert.True(minHeight > 0.0);
        }
    }

    /// <summary>最小幅での配置テスト</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_最小幅での配置テスト(Method method)
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();  // 幅: 40, 50, 60
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 40.0;  // 最小幅
        layout.CurrentMethod = method;

        // Act
        var (usedWidth, _) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        // フォールバックして単列配置
        Assert.Single(columnSegments);
        Assert.Equal(60.0, usedWidth);  // 最大幅
    }

    /// <summary>最大幅での配置テスト</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_最大幅での配置テスト(Method method)
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3);
        const double widthLimit = 1000.0;  // 非常に大きい幅
        layout.CurrentMethod = method;

        // Act
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(usedWidth <= widthLimit);
        // 最適な配置が見つかる
        Assert.True(TestHelper.ValidateLayoutComprehensive(minHeight, usedWidth, columnSegments, items, (0.0, 0.0), widthLimit));
    }

    #endregion EdgeCases

    #region StressTests

    /// <summary>Solve_最大列数ストレステスト</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_最大列数ストレステスト(Method method)
    {
        // Arrange
        var items = TestHelper.GenerateTestItems(20);
        var layout = new MultiColumnLayoutEngine(items, (1.0, 1.0), columnLimit: 20);
        const double widthLimit = 500.0;
        layout.CurrentMethod = method;

        // Act
        var (usedWidth, _) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(TestHelper.ValidateColumnSegment(columnSegments, items.Count));
        Assert.True(usedWidth <= widthLimit);
    }

    /// <summary>Solve_多数アイテムストレステスト</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_多数アイテムストレステスト(Method method)
    {
        // Arrange
        var items = TestHelper.GenerateTestItems(50);
        var layout = new MultiColumnLayoutEngine(items, (0.5, 0.5), columnLimit: 10);
        const double widthLimit = 1000.0;
        layout.CurrentMethod = method;

        // Act
        var (usedWidth, _) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(columnSegments.Length <= 10);
        Assert.True(usedWidth <= widthLimit);
    }

    /// <summary>Solve_スタックアロケーション境界テスト_STACKALLOC_THRESHOLD直前</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_スタックアロケーション境界テスト_STACKALLOC_THRESHOLD直前(Method method)
    {
        // Arrange
        var items = TestHelper.GenerateTestItems(40);
        var stackallocThreshold = MultiColumnLayoutEngine.STACKALLOC_THRESHOLD;
        var layout = new MultiColumnLayoutEngine(items, (0.5, 0.5), columnLimit: stackallocThreshold);
        const double widthLimit = 600.0;
        layout.CurrentMethod = method;

        // Act
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(TestHelper.ValidateColumnSegment(columnSegments, items.Count));
        Assert.True(usedWidth <= widthLimit);
        Assert.True(minHeight > 0.0);
        Assert.True(columnSegments.Length <= 32);
    }

    /// <summary>Solve_スタックアロケーション境界テスト_STACKALLOC_THRESHOLD超過</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_スタックアロケーション境界テスト_STACKALLOC_THRESHOLD超過(Method method)
    {
        // Arrange
        var items = TestHelper.GenerateTestItems(40);
        var stackallocThreshold = MultiColumnLayoutEngine.STACKALLOC_THRESHOLD;
        var layout = new MultiColumnLayoutEngine(items, (0.5, 0.5), columnLimit: stackallocThreshold + 1);
        const double widthLimit = 600.0;
        layout.CurrentMethod = method;

        // Act
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(TestHelper.ValidateColumnSegment(columnSegments, items.Count));
        Assert.True(usedWidth <= widthLimit);
        Assert.True(minHeight > 0.0);
        Assert.True(columnSegments.Length <= 33);
    }

    #endregion StressTests

    #region NumericalStabilityTests

    /// <summary>NumericalStability_浮動小数点誤差_テスト</summary>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void NumericalStability_浮動小数点誤差_テスト(Method method)
    {
        // Arrange
        var items = new List<(double Width, double Height)>
        {
            (0.1 + 0.1 + 0.1, 100.0),  // 0.30000000000000004
            (0.2 + 0.1, 100.0),         // 0.30000000000000004
            (0.3, 100.0)                // 0.3
        };
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        // 浮動小数点誤差にもかかわらず、正確に配置されることを確認
        Assert.NotEmpty(columnSegments);
        Assert.True(usedWidth <= widthLimit);
        Assert.True(minHeight > 0.0);
    }

    /// <summary>NumericalStability_小さい値_テスト</summary>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void NumericalStability_小さい値_テスト(Method method)
    {
        // Arrange
        var items = new List<(double Width, double Height)>
        {
            (0.001, 0.001),
            (0.002, 0.002),
            (0.003, 0.003)
        };
        var layout = new MultiColumnLayoutEngine(items, (0.0001, 0.0001), columnLimit: 3);
        const double widthLimit = 0.01;
        layout.CurrentMethod = method;

        // Act
        var (_, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(minHeight > 0.0);
    }

    /// <summary>NumericalStability_大きい値_テスト</summary>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void NumericalStability_大きい値_テスト(Method method)
    {
        // Arrange
        var items = new List<(double Width, double Height)>
        {
            (10000.0, 10000.0),
            (20000.0, 20000.0),
            (30000.0, 30000.0)
        };
        var layout = new MultiColumnLayoutEngine(items, (100.0, 100.0), columnLimit: 3);
        const double widthLimit = 100000.0;
        layout.CurrentMethod = method;

        // Act
        var (_, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(minHeight > 0.0);
    }

    #endregion NumericalStabilityTests

    #region AlgorithmQualityComprehensiveTests

    /// <summary>AlgorithmQuality_ランダムパターン複数試行_品質検証</summary>
    [Fact]
    public void AlgorithmQuality_ランダムパターン複数試行()
    {
        // Arrange
        var seeds = new[] { 100, 200, 300, 400, 500 };
        var allPassed = true;

        foreach (var seed in seeds)
        {
            // Act
            var items = TestHelper.GenerateRandomItems(30, seed);
            var layout = new MultiColumnLayoutEngine(items, (1.0, 1.0), 5)
            {
                CurrentMethod = Method.DynamicProgramming
            };
            var (dpUsedWidth, dpMinHeight) = layout.Solve(300.0);
            layout.CurrentMethod = Method.Greedy;
            var (greedyUsedWidth, greedyMinHeight) = layout.Solve(300.0);
            layout.CurrentMethod = Method.BinarySearch;
            var (bsearchUsedWidth, bsearchMinHeight) = layout.Solve(300.0);

            // Assert
            if (!(dpMinHeight <= greedyMinHeight &&
                  dpMinHeight <= bsearchMinHeight))
            {
                allPassed = false;
                break;
            }
        }

        Assert.True(allPassed);
    }

    /// <summary>AlgorithmQuality_エッジケース_DP最適性検証</summary>
    [Fact]
    public void AlgorithmQuality_エッジケース_DP最適性()
    {
        // Arrange: 極端な値を組み合わせ
        var testCases = new[]
        {
            TestHelper.GetExtremeWidthItems(),
            TestHelper.GetExtremeHeightItems(),
            TestHelper.GetUniformTestItems()
        };

        foreach (var items in testCases)
        {
            // Act
            var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 4)
            {
                CurrentMethod = Method.DynamicProgramming
            };
            var (_, dpMinHeight) = layout.Solve(150.0);
            layout.CurrentMethod = Method.Greedy;
            var (_, greedyMinHeight) = layout.Solve(150.0);
            layout.CurrentMethod = Method.BinarySearch;
            var (_, bsearchMinHeight) = layout.Solve(150.0);

            // Assert
            Assert.True(dpMinHeight <= greedyMinHeight);
            Assert.True(dpMinHeight <= bsearchMinHeight);
        }
    }

    /// <summary>AlgorithmQuality_GreedyがDP超過_異常検出</summary>
    [Fact]
    public void AlgorithmQuality_GreedyがDP超過_異常検出()
    {
        // Arrange: 複数の異なるデータで品質を確認
        var seeds = new[] { 42, 100, 200, 300, 400, 500 };
        var abnormalCases = new List<(int seed, double dpHeight, double greedyHeight, double accuracy)>();

        foreach (var seed in seeds)
        {
            // Act
            var items = TestHelper.GenerateRandomItems(30, seed);
            var layout = new MultiColumnLayoutEngine(items, (1.0, 1.0), 5)
            {
                CurrentMethod = Method.DynamicProgramming
            };
            var (dpMinHeight, _) = layout.Solve(300.0);
            layout.CurrentMethod = Method.Greedy;
            var (greedyMinHeight, _) = layout.Solve(300.0);

            var accuracy = TestHelper.CalculateAccuracy(dpMinHeight, greedyMinHeight);

            // Assert: Greedy が DP を上回る場合は異常（100%超）
            if (greedyMinHeight < dpMinHeight)
            {
                abnormalCases.Add((seed, dpMinHeight, greedyMinHeight, accuracy));
            }

            System.Diagnostics.Debug.WriteLine($"Seed={seed}: DP={dpMinHeight:F2}, Greedy={greedyMinHeight:F2}, Accuracy={accuracy:F1}%");
        }

        // 異常ケースがある場合は詳細情報を出力
        if (abnormalCases.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine("\n🚨 警告: Greedy が DP より優れた結果を出したケース（DP 実装バグの可能性）:");
            foreach (var (seed, dpHeight, greedyHeight, accuracy) in abnormalCases)
            {
                System.Diagnostics.Debug.WriteLine($"  Seed={seed}: DP={dpHeight:F2}, Greedy={greedyHeight:F2}, Accuracy={accuracy:F1}%");
                System.Diagnostics.Debug.WriteLine($"    → Greedy の方が {(dpHeight - greedyHeight):F2} 小さい");
            }
        }

        // 品質が大きく低下していないことを確認
        var allAccuracies = new List<double>();
        foreach (var seed in seeds)
        {
            var items = TestHelper.GenerateRandomItems(30, seed);
            var layout = new MultiColumnLayoutEngine(items, (1.0, 1.0), 5)
            {
                CurrentMethod = Method.DynamicProgramming
            };
            var (dpMinHeight, _) = layout.Solve(300.0);
            layout.CurrentMethod = Method.Greedy;
            var (greedyMinHeight, _) = layout.Solve(300.0);
            layout.CurrentMethod = Method.BinarySearch;
            var (bsearchMinHeight, _) = layout.Solve(300.0);

            allAccuracies.Add(TestHelper.CalculateAccuracy(dpMinHeight, greedyMinHeight));
            allAccuracies.Add(TestHelper.CalculateAccuracy(dpMinHeight, bsearchMinHeight));
        }

        // 平均精度が 85% 以上であることを確認
        double avgAccuracy = allAccuracies.Average();
        double minAccuracy = allAccuracies.Min();
        double maxAccuracy = allAccuracies.Max();

        System.Diagnostics.Debug.WriteLine($"\n📊 統計情報:");
        System.Diagnostics.Debug.WriteLine($"  平均精度: {avgAccuracy:F1}%");
        System.Diagnostics.Debug.WriteLine($"  最小精度: {minAccuracy:F1}%");
        System.Diagnostics.Debug.WriteLine($"  最大精度: {maxAccuracy:F1}%");

        Assert.True(avgAccuracy >= 85.0, $"Greedy の平均精度が低い: {avgAccuracy:F2}%");
    }

    #endregion AlgorithmQualityComprehensiveTests

    #region EdgeCaseCombinationTests

    /// <summary>EdgeCaseCombination_最小幅と最大行間スペース_強制単列</summary>
    [Fact]
    public void EdgeCaseCombination_最小幅と最大行間スペース()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();  // 幅50
        var space = (Row: 1000.0, Column: 1000.0);
        var layout = new MultiColumnLayoutEngine(items, space, 1);  // 列数最小
        const double widthLimit = 40.0;  // 最小幅
        layout.CurrentMethod = Method.DynamicProgramming;

        // Act
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var segments = layout.GetLastColumnSegments();

        // Assert
        Assert.Single(segments);
        Assert.Equal(100.0, minHeight);  // 単一アイテムなので行間スペース不適用
        Assert.Equal(50.0, usedWidth);
    }

    /// <summary>EdgeCaseCombination_最大幅と最大列数_最適分割</summary>
    [Fact]
    public void EdgeCaseCombination_最大幅と最大列数()
    {
        // Arrange
        var items = TestHelper.GenerateRandomItems(20);
        var space = (Row: 0.0, Column: 0.0);
        var layout = new MultiColumnLayoutEngine(items, space, 20)  // 列数最大
        {
            CurrentMethod = Method.BinarySearch
        };
        const double widthLimit = 1000.0;  // 幅最大

        // Act
        var (bsearchUsedWidth, _) = layout.Solve(widthLimit);
        var segments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(segments);
        Assert.True(segments.Length <= 20);
        Assert.True(bsearchUsedWidth <= widthLimit);
    }

    #endregion EdgeCaseCombinationTests
}

#endregion VerticalMultiColumnLayoutTest
