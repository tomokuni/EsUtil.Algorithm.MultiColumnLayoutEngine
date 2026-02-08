using System.Collections.Generic;
using Xunit;

using EsUtil.Algorithm.MultiColumnLayoutEngineInner;
using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>GreedyLayoutStrategy の単体テスト</summary>
public class GreedyLayoutStrategyTests
{

    /// <summary>CoreGreedy_均等分割_テスト</summary>
    [Fact]
    public void CoreGreedy_均等分割_テスト()
    {
        // Arrange
        var items = TestHelper.GetUniformTestItems();  // 6個
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = Method.Greedy;
        layout.Solve(widthLimit);
        var segments = layout.GetLastColumnSegments();

        // Assert
        // 6個を3列で均等分割 → 各列2個
        Assert.Equal(3, segments.Length);
    }

    /// <summary>CoreGreedy_品質保証_テスト</summary>
    [Fact]
    public void CoreGreedy_品質保証_テスト()
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

        // Assert
        // Greedy は DP より劣る可能性があるが、品質は許容範囲内
        double accuracy = TestHelper.CalculateAccuracy(dpMinHeight, greedyMinHeight);
        Assert.True(accuracy >= 85.0);  // 85% 以上の精度
    }

    /// <summary>CoreGreedy_空アイテムリスト_テスト</summary>
    [Fact]
    public void CoreGreedy_空アイテムリスト_テスト()
    {
        // Arrange
        var items = new List<(double Width, double Height)>();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 100.0;

        // Act
        layout.CurrentMethod = Method.Greedy;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.Empty(columnSegments);
        Assert.Equal(0.0, minHeight);
        Assert.Equal(0.0, usedWidth);
    }

    /// <summary>CoreGreedy_単一アイテム_テスト</summary>
    [Fact]
    public void CoreGreedy_単一アイテム_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 100.0;

        // Act
        layout.CurrentMethod = Method.Greedy;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.Single(columnSegments);
        Assert.Equal((0, 0), columnSegments[0]);
        Assert.Equal(100.0, minHeight);
        Assert.Equal(50.0, usedWidth);
    }

    /// <summary>CoreGreedy_複数列配置_テスト</summary>
    [Fact]
    public void CoreGreedy_複数列配置_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = Method.Greedy;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(TestHelper.ValidateColumnSegment(columnSegments, items.Count));
        Assert.True(usedWidth <= widthLimit);
        Assert.True(minHeight > 0.0);
    }


    /// <summary>GreedyLayoutStrategy_コンストラクタ_テスト</summary>
    [Fact]
    public void GreedyLayoutStrategy_コンストラクタ_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);

        // Act
        var strategy = new GreedyLayoutStrategy(items, space, 10, cache);

        // Assert
        Assert.Equal("Greedy", strategy.StrategyName);
    }

    /// <summary>GreedyLayoutStrategy_Solve_テスト</summary>
    [Fact]
    public void GreedyLayoutStrategy_Solve_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new GreedyLayoutStrategy(items, space, 3, cache);

        // Act
        var result = strategy.Solve(150.0);

        // Assert
        Assert.True(result.UsedWidth <= 150.0);
        Assert.True(result.MinHeight > 0.0);
        Assert.NotEmpty(result.ColumnSegments);
    }

    /// <summary>GreedyLayoutStrategy_GetMetadata_StrategyName_テスト</summary>
    [Fact]
    public void GreedyLayoutStrategy_GetMetadata_StrategyName_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new GreedyLayoutStrategy(items, space, 10, cache);

        // Act
        var name = strategy.GetMetadata<string>(MetadataKey.StrategyName);

        // Assert
        Assert.Equal("Greedy", name);
    }

    /// <summary>GreedyLayoutStrategy_GetMetadata_無効キー_テスト</summary>
    [Fact]
    public void GreedyLayoutStrategy_GetMetadata_無効キー_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new GreedyLayoutStrategy(items, space, 10, cache);

        // Act
        var result = strategy.GetMetadata<string>((MetadataKey)999);

        // Assert
        Assert.Null(result);
    }

    /// <summary>GreedyLayoutStrategy_CalculateGreedyLayout_テスト</summary>
    [Fact]
    public void GreedyLayoutStrategy_CalculateGreedyLayout_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new GreedyLayoutStrategy(items, space, 3, cache);

        // Act
        var result = strategy.CalculateGreedyLayout(150.0);

        // Assert
        Assert.True(result.UsedWidth <= 150.0);
        Assert.True(result.MinHeight > 0.0);
        Assert.NotEmpty(result.ColumnSegments);
    }

    /// <summary>GreedyLayoutStrategy_BuildGreedyColumns_有効_テスト</summary>
    [Fact]
    public void GreedyLayoutStrategy_BuildGreedyColumns_有効_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new GreedyLayoutStrategy(items, space, 3, cache);

        // Act
        var (valid, result) = strategy.BuildGreedyColumns(150.0, 3);

        // Assert
        Assert.True(valid);
        Assert.True(result.UsedWidth <= 150.0);
        Assert.True(result.MinHeight > 0.0);
        Assert.Equal(3, result.ColumnSegments.Length);
    }

    /// <summary>GreedyLayoutStrategy_BuildGreedyColumns_無効_テスト</summary>
    [Fact]
    public void GreedyLayoutStrategy_BuildGreedyColumns_無効_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new GreedyLayoutStrategy(items, space, 3, cache);

        // Act
        var (valid, result) = strategy.BuildGreedyColumns(30.0, 3); // 幅制限が厳しすぎる

        // Assert
        Assert.False(valid);
        Assert.Equal(StrategyResult.Empty, result);
    }

    /// <summary>GreedyLayoutStrategy_BuildGreedyColumns_境界値_テスト</summary>
    [Fact]
    public void GreedyLayoutStrategy_BuildGreedyColumns_境界値_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new GreedyLayoutStrategy(items, space, 1, cache);

        // Act
        var (valid, result) = strategy.BuildGreedyColumns(100.0, 1);

        // Assert
        Assert.True(valid);
        Assert.Equal(50.0, result.UsedWidth);
        Assert.Equal(100.0, result.MinHeight);
        Assert.Single(result.ColumnSegments);
    }
}
