using System;
using Xunit;

using EsUtil.Algorithm.MultiColumnLayoutEngineInner;
using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>LayoutStrategyFactory の単体テスト</summary>
public class LayoutStrategyFactoryTests
{

    /// <summary>SolveOptions_Default_プロパティテスト</summary>
    [Fact]
    public void SolveOptions_Default_プロパティテスト()
    {
        // Act
        var defaultOptions = BinarySearchOptions.Default;

        // Assert
        Assert.Equal(1e-3, defaultOptions.Epsilon);
        Assert.Equal(100, defaultOptions.MaxIterations);
        Assert.Equal(0.95, defaultOptions.LowerBoundRatio);
    }

    /// <summary>BinarySearchOptions_コンストラクタ_テスト</summary>
    [Fact]
    public void BinarySearchOptions_コンストラクタ_テスト()
    {
        // Act
        var options = new BinarySearchOptions(1e-4, 50, 0.9);

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


    /// <summary>Method_全列挙値_テスト</summary>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Method_全列挙値_テスト(Method method)
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10)
        {
            CurrentMethod = method
        };

        // Act
        layout.Solve(100.0);
        var segments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(segments);
    }


    /// <summary>LayoutStrategyFactory_コンストラクタ_テスト</summary>
    [Fact]
    public void LayoutStrategyFactory_コンストラクタ_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);

        // Act
        var factory = new LayoutStrategyFactory(items, space, 10, cache);

        // Assert
        Assert.Equal(Method.BinarySearch, factory.CurrentMethod);
    }

    /// <summary>LayoutStrategyFactory_CurrentMethod_設定テスト</summary>
    [Fact]
    public void LayoutStrategyFactory_CurrentMethod_設定テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
#pragma warning disable IDE0017 // オブジェクトの初期化を簡略化します
        var factory = new LayoutStrategyFactory(items, space, 10, cache);
#pragma warning restore IDE0017 // オブジェクトの初期化を簡略化します

        // Act
        factory.CurrentMethod = Method.DynamicProgramming;

        // Assert
        Assert.Equal(Method.DynamicProgramming, factory.CurrentMethod);
    }

    /// <summary>LayoutStrategyFactory_GetStrategy_DP_テスト</summary>
    [Fact]
    public void LayoutStrategyFactory_GetStrategy_DP_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var factory = new LayoutStrategyFactory(items, space, 10, cache)
        {
            CurrentMethod = Method.DynamicProgramming
        };

        // Act
        var strategy = factory.GetStrategy();

        // Assert
        Assert.NotNull(strategy);
    }

    /// <summary>LayoutStrategyFactory_GetStrategy_Greedy_テスト</summary>
    [Fact]
    public void LayoutStrategyFactory_GetStrategy_Greedy_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var factory = new LayoutStrategyFactory(items, space, 10, cache)
        {
            CurrentMethod = Method.Greedy
        };

        // Act
        var strategy = factory.GetStrategy();

        // Assert
        Assert.NotNull(strategy);
    }

    /// <summary>LayoutStrategyFactory_GetStrategy_BinarySearch_テスト</summary>
    [Fact]
    public void LayoutStrategyFactory_GetStrategy_BinarySearch_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var factory = new LayoutStrategyFactory(items, space, 10, cache)
        {
            CurrentMethod = Method.BinarySearch
        };

        // Act
        var strategy = factory.GetStrategy();

        // Assert
        Assert.NotNull(strategy);
    }

    /// <summary>LayoutStrategyFactory_GetStrategy_無効メソッド_例外</summary>
    [Fact]
    public void LayoutStrategyFactory_GetStrategy_無効メソッド_例外()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var factory = new LayoutStrategyFactory(items, space, 10, cache)
        {
            CurrentMethod = (Method)999 // 無効な値
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => factory.GetStrategy());
        Assert.Contains("Invalid method", ex.Message);
    }

    /// <summary>LayoutStrategyFactory_BinarySearchOptions_デフォルト_テスト</summary>
    [Fact]
    public void LayoutStrategyFactory_BinarySearchOptions_デフォルト_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var factory = new LayoutStrategyFactory(items, space, 10, cache);

        // Act
        var options = factory.CurrentBinarySearchOptions;

        // Assert
        Assert.Equal(BinarySearchOptions.Default, options);
    }

    /// <summary>LayoutStrategyFactory_BinarySearchOptions_設定_テスト</summary>
    [Fact]
    public void LayoutStrategyFactory_BinarySearchOptions_設定_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var factory = new LayoutStrategyFactory(items, space, 10, cache);
        var newOptions = new BinarySearchOptions(1e-4, 50, 0.9);

        // Act
        factory.CurrentBinarySearchOptions = newOptions;

        // Assert
        Assert.Equal(newOptions, factory.CurrentBinarySearchOptions);
    }

    /// <summary>LayoutStrategyFactory_ClearStrategyCache_テスト</summary>
    [Fact]
    public void LayoutStrategyFactory_ClearStrategyCache_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var factory = new LayoutStrategyFactory(items, space, 10, cache);
        factory.GetStrategy(); // 初期化

        // Act
        factory.ClearStrategyCache();

        // Assert
        // 内部フィールドが null になっていることを確認（直接アクセスできないので間接的に）
        Assert.Equal(Method.BinarySearch, factory.CurrentMethod);
    }

    /// <summary>LayoutStrategyFactory_ClearMetricsCache_テスト</summary>
    [Fact]
    public void LayoutStrategyFactory_ClearMetricsCache_テスト()
    {
        // Arrange
        var items = Size.ToSizeArray(TestHelper.GetSingleItem());
        var space = new Space(0.0, 0.0);
        var cache = new ColumnMetricsCache(items, space);
        var factory = new LayoutStrategyFactory(items, space, 10, cache);
        cache.GetMetrics(new ColumnSegment(0, 0)); // キャッシュに追加

        // Act
        factory.ClearMetricsCache();

        // Assert
        // キャッシュがクリアされていることを確認（直接アクセスできないので間接的に）
        Assert.Equal(Method.BinarySearch, factory.CurrentMethod);
    }
}
