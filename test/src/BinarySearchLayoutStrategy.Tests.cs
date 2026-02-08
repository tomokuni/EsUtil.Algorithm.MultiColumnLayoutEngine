using Xunit;

using EsUtil.Algorithm.MultiColumnLayoutEngineInner;
using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>BinarySearchLayoutStrategy の単体テスト</summary>
public class BinarySearchLayoutStrategyTests
{

    /// <summary>BinarySearchOptions_コンストラクタ_テスト</summary>
    [Fact]
    public void BinarySearchOptions_コンストラクタ_テスト()
    {
        // Act
        var options = new Algorithm.MultiColumnLayoutEngine.BinarySearchOptions(1e-4, 50, 0.9);

        // Assert
        Assert.Equal(1e-4, options.Epsilon);
        Assert.Equal(50, options.MaxIterations);
        Assert.Equal(0.9, options.LowerBoundRatio);
    }

    /// <summary>BinarySearchOptions_等価性_テスト</summary>
    [Fact]
    public void BinarySearchOptions_等価性_テスト()
    {
        // Arrange
        var options1 = new BinarySearchOptions(1e-3, 100, 0.95);
        var options2 = new BinarySearchOptions(1e-3, 100, 0.95);
        var options3 = new BinarySearchOptions(1e-4, 100, 0.95);

        // Assert
        Assert.Equal(options1, options2);
        Assert.NotEqual(options1, options3);
    }


    /// <summary>CoreBSearch_収束テスト</summary>
    [Fact]
    public void CoreBSearch_収束テスト()
    {
        // Arrange
        var items = TestHelper.GetLargeTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 5);
        const double widthLimit = 200.0;

        // Act
        layout.CurrentMethod = Method.DynamicProgramming;
        var (_, dpMinHeight) = layout.Solve(widthLimit);
        layout.CurrentMethod = Method.BinarySearch;
        var (_, bsearchMinHeight) = layout.Solve(widthLimit);

        // Assert
        // BinarySearch が DP の結果に近い
        double accuracy = TestHelper.CalculateAccuracy(dpMinHeight, bsearchMinHeight);
        Assert.True(accuracy >= 95.0);  // 95% 以上の精度
    }

    /// <summary>CoreBSearch_カスタムオプション_テスト</summary>
    [Fact]
    public void CoreBSearch_カスタムオプション_テスト()
    {
        // Arrange
        var items = TestHelper.GetLargeTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 5)
        {
            CurrentBinarySearchOptions = new BinarySearchOptions(Epsilon: 1e-6, MaxIterations: 100, LowerBoundRatio: 0.75)
        };
        const double widthLimit = 200.0;

        // Act
        layout.CurrentMethod = Method.Greedy;
        var (greedyUsedWidth, greedyMinHeight) = layout.Solve(widthLimit);
        layout.CurrentMethod = Method.BinarySearch;
        var (bsearchUsedWidth, bsearchMinHeight) = layout.Solve(widthLimit);
        var segments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(segments);
        Assert.True(bsearchUsedWidth <= widthLimit);
        Assert.True(greedyUsedWidth <= widthLimit);
        Assert.True(bsearchMinHeight <= greedyMinHeight);
    }


    /// <summary>BinarySearchLayoutStrategy_コンストラクタ_テスト</summary>
    [Fact]
    public void BinarySearchLayoutStrategy_コンストラクタ_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var options = BinarySearchOptions.Default;

        // Act
        var strategy = new BinarySearchLayoutStrategy(items, space, 10, options, cache);

        // Assert
        Assert.Equal("BinarySearch", strategy.StrategyName);
        Assert.Equal(0, strategy.LastIterationCount);
    }

    /// <summary>BinarySearchLayoutStrategy_Solve_テスト</summary>
    [Fact]
    public void BinarySearchLayoutStrategy_Solve_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var options = BinarySearchOptions.Default;
        var strategy = new BinarySearchLayoutStrategy(items, space, 3, options, cache);

        // Act
        var result = strategy.Solve(150.0);

        // Assert
        Assert.True(result.UsedWidth <= 150.0);
        Assert.True(result.MinHeight > 0.0);
        Assert.NotEmpty(result.ColumnSegments);
        Assert.True(strategy.LastIterationCount >= 0);
    }

    /// <summary>BinarySearchLayoutStrategy_GetMetadata_StrategyName_テスト</summary>
    [Fact]
    public void BinarySearchLayoutStrategy_GetMetadata_StrategyName_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var options = BinarySearchOptions.Default;
        var strategy = new BinarySearchLayoutStrategy(items, space, 10, options, cache);

        // Act
        var name = strategy.GetMetadata<string>(MetadataKey.StrategyName);

        // Assert
        Assert.Equal("BinarySearch", name);
    }

    /// <summary>BinarySearchLayoutStrategy_GetMetadata_IterationCount_テスト</summary>
    [Fact]
    public void BinarySearchLayoutStrategy_GetMetadata_IterationCount_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var options = BinarySearchOptions.Default;
        var strategy = new BinarySearchLayoutStrategy(items, space, 3, options, cache);
        strategy.Solve(150.0); // 実行して反復回数を設定

        // Act
        var count = strategy.GetMetadata<int>(MetadataKey.BinarySearchIterationCount);

        // Assert
        Assert.True(count >= 0);
    }

    /// <summary>BinarySearchLayoutStrategy_GetMetadata_無効キー_テスト</summary>
    [Fact]
    public void BinarySearchLayoutStrategy_GetMetadata_無効キー_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var options = BinarySearchOptions.Default;
        var strategy = new BinarySearchLayoutStrategy(items, space, 10, options, cache);

        // Act
        var result = strategy.GetMetadata<string>((MetadataKey)999);

        // Assert
        Assert.Null(result);
    }

    /// <summary>BinarySearchLayoutStrategy_CalculateBinarySearchLayout_テスト</summary>
    [Fact]
    public void BinarySearchLayoutStrategy_CalculateBinarySearchLayout_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var options = BinarySearchOptions.Default;
        var strategy = new BinarySearchLayoutStrategy(items, space, 3, options, cache);

        // Act
        var result = strategy.CalculateBinarySearchLayout(150.0, 200.0);

        // Assert
        Assert.True(result.UsedWidth <= 150.0);
        Assert.True(result.MinHeight > 0.0);
        Assert.NotEmpty(result.ColumnSegments);
    }

    /// <summary>BinarySearchLayoutStrategy_TryFitColumns_配置可能_テスト</summary>
    [Fact]
    public void BinarySearchLayoutStrategy_TryFitColumns_配置可能_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var options = BinarySearchOptions.Default;
        var strategy = new BinarySearchLayoutStrategy(items, space, 3, options, cache);
        var buffer = new ColumnSegment[3];

        // Act
        var (canFit, width, height, segmentCount) = strategy.TryFitColumns(150.0, 200.0, buffer);

        // Assert
        Assert.True(canFit);
        Assert.True(width <= 150.0);
        Assert.True(height <= 200.0);
        Assert.True(segmentCount > 0);
    }

    /// <summary>BinarySearchLayoutStrategy_TryFitColumns_配置不可_テスト</summary>
    [Fact]
    public void BinarySearchLayoutStrategy_TryFitColumns_配置不可_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetStandardTestItems());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var options = BinarySearchOptions.Default;
        var strategy = new BinarySearchLayoutStrategy(items, space, 3, options, cache);
        var buffer = new ColumnSegment[3];

        // Act
        var (canFit, _, _, segmentCount) = strategy.TryFitColumns(50.0, 50.0, buffer); // 狭すぎる

        // Assert
        Assert.False(canFit);
        Assert.Equal(0, segmentCount);  // 配置できなかったのでセグメント数0
    }

    /// <summary>BinarySearchLayoutStrategy_TryFitColumns_境界値_テスト</summary>
    [Fact]
    public void BinarySearchLayoutStrategy_TryFitColumns_境界値_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var options = BinarySearchOptions.Default;
        var strategy = new BinarySearchLayoutStrategy(items, space, 1, options, cache);
        var buffer = new ColumnSegment[1];

        // Act
        var (canFit, width, height, segmentCount) = strategy.TryFitColumns(100.0, 100.0, buffer);

        // Assert
        Assert.True(canFit);
        Assert.Equal(50.0, width); // アイテム幅
        Assert.Equal(100.0, height); // アイテム高さ
        Assert.Equal(1, segmentCount);
    }
}
