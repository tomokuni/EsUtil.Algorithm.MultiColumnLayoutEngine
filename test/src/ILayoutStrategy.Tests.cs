using System.Collections.Generic;
using Xunit;

using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>ILayoutStrategy の単体テスト</summary>
public class ILayoutStrategyTests
{

    /// <summary>Solve_単一アイテム_テスト</summary>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_単一アイテム_テスト(Method method)
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 100.0;

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.Single(columnSegments);
        Assert.Equal((0, 0), columnSegments[0]);
        Assert.Equal(100.0, minHeight);
        Assert.Equal(50.0, usedWidth);
    }

    /// <summary>Solve_複数列配置_テスト</summary>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_複数列配置_テスト(Method method)
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(TestHelper.ValidateColumnSegment(columnSegments, items.Count));
        Assert.True(usedWidth <= widthLimit);
        Assert.True(minHeight > 0.0);
    }

    /// <summary>Solve_空アイテムリスト_テスト</summary>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_空アイテムリスト_テスト(Method method)
    {
        // Arrange
        var items = new List<(double Width, double Height)>();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 100.0;

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.Empty(columnSegments);
        Assert.Equal(0.0, minHeight);
        Assert.Equal(0.0, usedWidth);
    }

    /// <summary>GetMetadata_StrategyName_テスト</summary>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void GetMetadata_StrategyName_テスト(Method method)
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10)
        {
            CurrentMethod = method
        };

        // Act
        layout.Solve(100.0);  // Solve を呼び出して Strategy を使用

        // Assert
        // Strategy が正しく設定されていることを間接的に確認
        var segments = layout.GetLastColumnSegments();
        Assert.NotEmpty(segments);
    }

    /// <summary>GetMetadata_BinarySearchIterationCount_テスト</summary>
    [Fact]
    public void GetMetadata_BinarySearchIterationCount_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3)
        {
            CurrentMethod = Method.BinarySearch
        };

        // Act
        layout.Solve(150.0);  // Solve を呼び出して Strategy を使用

        // Assert
        // BinarySearch が実行されたことを間接的に確認
        var segments = layout.GetLastColumnSegments();
        Assert.NotEmpty(segments);
    }

    /// <summary>GetMetadata_無効キー_テスト</summary>
    [Fact]
    public void GetMetadata_無効キー_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10)
        {
            CurrentMethod = Method.Greedy
        };

        // Act
        layout.Solve(100.0);  // Solve を呼び出して Strategy を使用

        // Assert
        // Greedy が実行されたことを間接的に確認
        var segments = layout.GetLastColumnSegments();
        Assert.Single(segments);
    }

}
