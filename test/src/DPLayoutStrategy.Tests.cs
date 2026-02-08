using System;
using System.Collections.Generic;
using Xunit;

using EsUtil.Algorithm.MultiColumnLayoutEngineInner;
using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>DPLayoutStrategy の単体テスト</summary>
public class DPLayoutStrategyTests
{

    /// <summary>CoreDP_最適な列分割_テスト</summary>
    [Fact]
    public void CoreDP_最適な列分割_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = Method.DynamicProgramming;
        var (_, dpMinHeight) = layout.Solve(widthLimit);

        // Assert
        // DP は最適解を保証
        var greedyLayout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3)
        {
            CurrentMethod = Method.Greedy
        };
        var (_, greedyMinHeight) = greedyLayout.Solve(widthLimit);
        Assert.True(dpMinHeight <= greedyMinHeight);
    }

    /// <summary>CoreDP_品質保証_テスト</summary>
    [Fact]
    public void CoreDP_品質保証_テスト()
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
    }

    /// <summary>CoreDP_空アイテムリスト_テスト</summary>
    [Fact]
    public void CoreDP_空アイテムリスト_テスト()
    {
        // Arrange
        var items = new List<(double Width, double Height)>();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 100.0;

        // Act
        layout.CurrentMethod = Method.DynamicProgramming;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.Empty(columnSegments);
        Assert.Equal(0.0, minHeight);
        Assert.Equal(0.0, usedWidth);
    }

    /// <summary>CoreDP_単一アイテム_テスト</summary>
    [Fact]
    public void CoreDP_単一アイテム_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 100.0;

        // Act
        layout.CurrentMethod = Method.DynamicProgramming;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.Single(columnSegments);
        Assert.Equal((0, 0), columnSegments[0]);
        Assert.Equal(100.0, minHeight);
        Assert.Equal(50.0, usedWidth);
    }

    /// <summary>CoreDP_複数列配置_テスト</summary>
    [Fact]
    public void CoreDP_複数列配置_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = Method.DynamicProgramming;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(TestHelper.ValidateColumnSegment(columnSegments, items.Count));
        Assert.True(usedWidth <= widthLimit);
        Assert.True(minHeight > 0.0);
    }


    /// <summary>DPLayoutStrategy_コンストラクタ_テスト</summary>
    [Fact]
    public void DPLayoutStrategy_コンストラクタ_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);

        // Act
        var strategy = new DPLayoutStrategy(items, space, 10, cache);

        // Assert
        Assert.Equal("DynamicProgramming", strategy.StrategyName);
    }

    /// <summary>DPLayoutStrategy_Solve_テスト</summary>
    [Fact]
    public void DPLayoutStrategy_Solve_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new DPLayoutStrategy(items, space, 3, cache);

        // Act
        var result = strategy.Solve(150.0);

        // Assert
        Assert.True(result.UsedWidth <= 150.0);
        Assert.True(result.MinHeight > 0.0);
        Assert.NotEmpty(result.ColumnSegments);
    }

    /// <summary>DPLayoutStrategy_GetMetadata_StrategyName_テスト</summary>
    [Fact]
    public void DPLayoutStrategy_GetMetadata_StrategyName_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new DPLayoutStrategy(items, space, 10, cache);

        // Act
        var name = strategy.GetMetadata<string>(MetadataKey.StrategyName);

        // Assert
        Assert.Equal("DynamicProgramming", name);
    }

    /// <summary>DPLayoutStrategy_GetMetadata_無効キー_テスト</summary>
    [Fact]
    public void DPLayoutStrategy_GetMetadata_無効キー_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new DPLayoutStrategy(items, space, 10, cache);

        // Act
        var result = strategy.GetMetadata<string>((MetadataKey)999);

        // Assert
        Assert.Null(result);
    }

    /// <summary>DPLayoutStrategy_CalculateDPLayout_テスト</summary>
    [Fact]
    public void DPLayoutStrategy_CalculateDPLayout_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new DPLayoutStrategy(items, space, 3, cache);

        // Act
        var (valid, result) = strategy.CalculateDPLayout(150.0);

        // Assert
        Assert.True(valid);
        Assert.True(result.UsedWidth <= 150.0);
        Assert.True(result.MinHeight > 0.0);
        Assert.NotEmpty(result.ColumnSegments);
    }

    /// <summary>DPLayoutStrategy_CalculateDPLayout_空アイテム_テスト</summary>
    [Fact]
    public void DPLayoutStrategy_CalculateDPLayout_空アイテム_テスト()
    {
        // Arrange
        var items = Array.Empty<Size>();
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new DPLayoutStrategy(items, space, 10, cache);

        // Act
        var (valid, result) = strategy.CalculateDPLayout(100.0);

        // Assert
        Assert.True(valid);
        Assert.Equal(StrategyResult.Empty, result);
    }

    /// <summary>DPLayoutStrategy_FillDPTable_テスト</summary>
    [Fact]
    public void DPLayoutStrategy_FillDPTable_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new DPLayoutStrategy(items, space, 3, cache);
        int n = items.Length;
        int dpSize = (n + 1) * (3 + 1);
        var dp = new DPLayoutStrategy.DpState[dpSize];

        // Act
        strategy.FillDPTable(dp, n, 150.0);

        // Assert
        // ベースケースが設定されている
        Assert.True(dp[0].IsValid);
        Assert.Equal(0.0, dp[0].Height);
        Assert.Equal(0.0, dp[0].Width);
    }

    /// <summary>DPLayoutStrategy_FindBestLayout_テスト</summary>
    [Fact]
    public void DPLayoutStrategy_FindBestLayout_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new DPLayoutStrategy(items, space, 3, cache);
        int n = items.Length;
        int dpSize = (n + 1) * (3 + 1);
        var dp = new DPLayoutStrategy.DpState[dpSize];
        strategy.FillDPTable(dp, n, 150.0);

        // Act
        var bestJ = strategy.FindBestLayout(dp, n);

        // Assert
        Assert.True(bestJ >= 1 && bestJ <= 3);
    }

    /// <summary>DPLayoutStrategy_BuildColumnSegments_テスト</summary>
    [Fact]
    public void DPLayoutStrategy_BuildColumnSegments_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var strategy = new DPLayoutStrategy(items, space, 3, cache);
        int n = items.Length;
        int dpSize = (n + 1) * (3 + 1);
        var dp = new DPLayoutStrategy.DpState[dpSize];
        strategy.FillDPTable(dp, n, 150.0);

        // Act
        var (valid, result) = strategy.BuildColumnSegments(dp, n);

        // Assert
        Assert.True(valid);
        Assert.True(result.UsedWidth <= 150.0);
        Assert.True(result.MinHeight > 0.0);
        Assert.NotEmpty(result.ColumnSegments);
    }

    /// <summary>DpState_IsValid_テスト</summary>
    [Fact]
    public void DpState_IsValid_テスト()
    {
        // Arrange
        var validState = DPLayoutStrategy.DpState.Create(100.0, 50.0, 0);
        var invalidState = DPLayoutStrategy.DpState.Create(double.PositiveInfinity, 0.0, -1);

        // Assert
        Assert.True(validState.IsValid);
        Assert.False(invalidState.IsValid);
    }

    /// <summary>DpState_Create_テスト</summary>
    [Fact]
    public void DpState_Create_テスト()
    {
        // Act
        var state = DPLayoutStrategy.DpState.Create(100.0, 50.0, 0);

        // Assert
        Assert.Equal(100.0, state.Height);
        Assert.Equal(50.0, state.Width);
        Assert.Equal(0, state.BreakIdx);
    }
}
