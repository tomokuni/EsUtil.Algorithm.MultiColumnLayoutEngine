using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>LayoutStrategyBase の単体テスト</summary>
public class LayoutStrategyBaseTests
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

    /// <summary>行間スペース反映テスト</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_行間スペース反映テスト(Method method)
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var space = (Row: 10.0, Column: 0.0);
        var layout = new MultiColumnLayoutEngine(items, space, columnLimit: 3);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = method;
        var (_, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.True(TestHelper.ValidateMinHeight(minHeight, columnSegments, items, space));
        // 行間スペースがあるため、スペースなしより高さが増加
        var layoutNoSpace = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3)
        {
            CurrentMethod = method
        };
        var (_, minHeightNoSpace) = layoutNoSpace.Solve(widthLimit);
        Assert.True(minHeight >= minHeightNoSpace);
    }

    /// <summary>列間スペース反映テスト</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_列間スペース反映テスト(Method method)
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var space = (Row: 0.0, Column: 20.0);
        var layout = new MultiColumnLayoutEngine(items, space, columnLimit: 3);
        const double widthLimit = 200.0;

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, _) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        // 複数列の場合、列間スペースが含まれる
        if (columnSegments.Length > 1)
        {
            Assert.True(TestHelper.ValidateUsedWidth(usedWidth, items, columnSegments, space));
        }
    }

    /// <summary>均一アイテムセットの計算</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_均一アイテムセットの計算(Method method)
    {
        // Arrange
        var items = TestHelper.GetUniformTestItems();  // 6個の (50, 100) アイテム
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 3);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        // 6個のアイテムを3列に分割 → 各列2個ずつ = 高さ 200
        Assert.Equal(3, columnSegments.Length);
        Assert.Equal(200.0, minHeight);
        Assert.Equal(150.0, usedWidth);
    }

    /// <summary>アイテム幅がlimit超過時フォールバック</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_アイテム幅がlimit超過時フォールバック(Method method)
    {
        // Arrange
        var items = new List<(double Width, double Height)>
        {
            (150.0, 100.0)  // 幅が widthLimit と同等
        };
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 100.0;  // アイテム幅が超過

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        // フォールバックして単列配置になる
        Assert.Single(columnSegments);
        Assert.Equal((0, 0), columnSegments[0]);
        Assert.Equal(100.0, minHeight);
        Assert.Equal(150.0, usedWidth);
    }

    /// <summary>極端な幅のアイテムセット</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_極端な幅のアイテムセット(Method method)
    {
        // Arrange
        var items = TestHelper.GetExtremeWidthItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 4);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, _) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.True(TestHelper.ValidateColumnSegment(columnSegments, items.Count));
        Assert.True(usedWidth <= widthLimit);
        Assert.True(TestHelper.ValidateSegmentOrder(columnSegments, items.Count));
    }

    /// <summary>極端な高さのアイテムセット</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_極端な高さのアイテムセット(Method method)
    {
        // Arrange
        var items = TestHelper.GetExtremeHeightItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 4);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = method;
        var (_, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.True(TestHelper.ValidateColumnSegment(columnSegments, items.Count));
        Assert.True(minHeight > 0.0);
        Assert.True(TestHelper.ValidateMinHeight(minHeight, columnSegments, items, (0.0, 0.0)));
    }

    /// <summary>大規模アイテムセット</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_大規模アイテムセット(Method method)
    {
        // Arrange
        var items = TestHelper.GetLargeTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 5);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(TestHelper.ValidateColumnSegment(columnSegments, items.Count));
        Assert.True(usedWidth <= widthLimit);
        Assert.True(TestHelper.ValidateLayoutComprehensive(minHeight, usedWidth, columnSegments, items, (0.0, 0.0), widthLimit));
    }

    /// <summary>列数上限1の強制単列配置</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_列数上限1の強制単列配置(Method method)
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 1);
        const double widthLimit = 100.0;

        // Act
        layout.CurrentMethod = method;
        layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.Single(columnSegments);
        Assert.Equal((0, items.Count - 1), columnSegments[0]);
    }

    /// <summary>列数上限が項目数以上</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_列数上限が項目数以上(Method method)
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();  // 3個
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);  // 上限は 10
        const double widthLimit = 200.0;

        // Act
        layout.CurrentMethod = method;
        layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        // 最大で3列まで使用可能
        Assert.True(columnSegments.Length <= items.Count);
    }

    /// <summary>幅制限が最小アイテム幅より小さい</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_幅制限が最小アイテム幅より小さい(Method method)
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();  // 幅: 40, 50, 60
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 35.0;  // 最小アイテム幅 40より小さい

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, _) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        // フォールバックして単列配置
        Assert.Single(columnSegments);
        // 使用幅は最大アイテム幅になる
        Assert.Equal(60.0, usedWidth);
    }

    /// <summary>浮動小数点精度テスト</summary>
    /// <param name="method"></param>
    [Theory]
    [InlineData(Method.DynamicProgramming)]
    [InlineData(Method.Greedy)]
    [InlineData(Method.BinarySearch)]
    public void Solve_浮動小数点精度テスト(Method method)
    {
        // Arrange
        var items = new List<(double Width, double Height)>
        {
            (33.33, 100.01),
            (33.34, 100.02),
            (33.33, 100.01)
        };
        var layout = new MultiColumnLayoutEngine(items, (1.5, 2.5), columnLimit: 3);
        const double widthLimit = 150.0;

        // Act
        layout.CurrentMethod = method;
        var (usedWidth, minHeight) = layout.Solve(widthLimit);
        var columnSegments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(columnSegments);
        Assert.True(usedWidth <= widthLimit);
        Assert.True(minHeight > 0.0);
    }

    /// <summary>IsValidWidth_有効_テスト</summary>
    [Fact]
    public void IsValidWidth_有効_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);

        // Act
        var isValid = layout.IsValidWidth(100.0);

        // Assert
        Assert.False(isValid);  // 最大幅 50 < 100 なので false
    }

    /// <summary>IsValidWidth_無効_テスト</summary>
    [Fact]
    public void IsValidWidth_無効_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);

        // Act
        var isValid = layout.IsValidWidth(30.0);

        // Assert
        Assert.True(isValid);  // 最大幅 50 > 30 なので true
    }

    /// <summary>VerifyLayoutResult_有効_テスト</summary>
    [Fact]
    public void VerifyLayoutResult_有効_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        layout.Solve(100.0);

        // Act
        try
        {
            layout.VerifyLayoutResult(100.0);
        }
        catch (Exception)
        {
            Assert.Fail("例外が発生しました");
        }
    }

    /// <summary>VerifyLayoutResult_無効_テスト</summary>
    [Fact]
    public void VerifyLayoutResult_無効_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        layout.Solve(100.0);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => layout.VerifyLayoutResult(30.0));
        Assert.Contains("width limit exceeded", ex.Message);
    }

    /// <summary>SolveSingleColumnLayout_テスト</summary>
    [Fact]
    public void SolveSingleColumnLayout_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var layout = new MultiColumnLayoutEngine(items, (5.0, 0.0), columnLimit: 10);

        // Act
        var (usedWidth, minHeight) = layout.SolveSingleColumnLayout();

        // Assert
        var segments = layout.GetLastColumnSegments();
        Assert.Single(segments);
        Assert.Equal((0, items.Count - 1), segments[0]);
        Assert.Equal(60.0, usedWidth);  // 最大幅
        Assert.True(minHeight > 0.0);
    }

    /// <summary>GetLastResult_テスト</summary>
    [Fact]
    public void GetLastResult_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        layout.Solve(100.0);

        // Act
        var (usedWidth, minHeight) = layout.GetLastResult();

        // Assert
        Assert.Equal(50.0, usedWidth);
        Assert.Equal(100.0, minHeight);
    }

    /// <summary>GetLastColumnSegments_テスト</summary>
    [Fact]
    public void GetLastColumnSegments_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        layout.Solve(100.0);

        // Act
        var segments = layout.GetLastColumnSegments();

        // Assert
        Assert.Single(segments);
        Assert.Equal((0, 0), segments[0]);
    }

    /// <summary>GetLastItemLayouts_テスト</summary>
    [Fact]
    public void GetLastItemLayouts_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        layout.Solve(100.0);

        // Act
        var itemLayouts = layout.GetLastItemLayouts();

        // Assert
        Assert.Single(itemLayouts);
        var (x, y, w, h) = itemLayouts[0];
        Assert.Equal(0.0, x);
        Assert.Equal(0.0, y);
        Assert.Equal(50.0, w);
        Assert.Equal(100.0, h);
    }

    /// <summary>GetLastItemLayouts_座標計算の連続性_同一列Y座標</summary>
    [Fact]
    public void GetLastItemLayouts_座標計算の連続性_同一列Y座標()
    {
        // Arrange
        var items = new List<(double Width, double Height)>
        {
            (50.0, 100.0),
            (50.0, 120.0),
            (50.0, 140.0)
        };
        var space = (Row: 10.0, Column: 0.0);
        var layout = new MultiColumnLayoutEngine(items, space, 1)
        {
            CurrentMethod = Method.DynamicProgramming
        };  // 1列に強制
        layout.Solve(100.0);

        // Act
        var layouts = layout.GetLastItemLayouts();

        // Assert: Y座標が連続しているか検証
        Assert.Equal(0.0, layouts[0].Y);
        Assert.Equal(100.0 + 10.0, layouts[1].Y);  // 前アイテム高 + 行間スペース
        Assert.Equal(100.0 + 10.0 + 120.0 + 10.0, layouts[2].Y);
    }

    /// <summary>GetLastItemLayouts_座標計算の連続性_複数列X座標</summary>
    [Fact]
    public void GetLastItemLayouts_座標計算の連続性_複数列X座標()
    {
        // Arrange
        var items = new List<(double Width, double Height)>
        {
            (40.0, 100.0),
            (50.0, 100.0),
            (60.0, 100.0)
        };
        var space = (Row: 0.0, Column: 10.0);
        var layout = new MultiColumnLayoutEngine(items, space, 3)
        {
            CurrentMethod = Method.DynamicProgramming
        };
        layout.Solve(200.0);

        // Act
        var layouts = layout.GetLastItemLayouts();

        // Assert: X座標が右に増加していることを確認
        double prevX = -1.0;
        foreach (var (X, _, _, _) in layouts)
        {
            Assert.True(X >= prevX);
            prevX = X;
        }
    }

    /// <summary>GetLastItemLayouts_座標計算値_厳密検証</summary>
    [Fact]
    public void GetLastItemLayouts_座標計算値_厳密検証()
    {
        // Arrange
        var items = TestHelper.GetUniformTestItems();  // 6個の (50, 100)
        var space = (Row: 0.0, Column: 0.0);
        var layout = new MultiColumnLayoutEngine(items, space, 3)
        {
            CurrentMethod = Method.DynamicProgramming
        };
        layout.Solve(150.0);

        // Act
        var itemLayouts = layout.GetLastItemLayouts();

        // Assert: 各レイアウト要素が正確か確認
        Assert.Equal(6, itemLayouts.Count);

        // 全アイテムが同じ幅
        foreach (var (_, _, Width, Height) in itemLayouts)
        {
            Assert.Equal(50.0, Width);
            Assert.Equal(100.0, Height);
        }

        // X座標が3つの異なる値
        var uniqueX = itemLayouts.Select(l => l.X).Distinct().ToList();
        Assert.Equal(3, uniqueX.Count);
    }

    /// <summary>GetLastItemLayouts_基本的な座標計算テスト</summary>
    [Fact]
    public void GetLastItemLayouts_基本的な座標計算テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        const double widthLimit = 100.0;
        layout.CurrentMethod = Method.DynamicProgramming;
        layout.Solve(widthLimit);

        // Act
        var itemLayouts = layout.GetLastItemLayouts();

        // Assert
        Assert.Single(itemLayouts);
        var (x, y, w, h) = itemLayouts[0];
        Assert.Equal(0.0, x);
        Assert.Equal(0.0, y);
        Assert.Equal(50.0, w);
        Assert.Equal(100.0, h);
    }

    /// <summary>GetLastItemLayouts_複数列配置の座標計算</summary>
    [Fact]
    public void GetLastItemLayouts_複数列配置の座標計算()
    {
        // Arrange
        var items = TestHelper.GetUniformTestItems();  // 6個の (50, 100)
        var space = (Row: 0.0, Column: 0.0);
        var layout = new MultiColumnLayoutEngine(items, space, columnLimit: 3);
        const double widthLimit = 150.0;
        layout.CurrentMethod = Method.DynamicProgramming;
        layout.Solve(widthLimit);

        // Act
        var itemLayouts = layout.GetLastItemLayouts();

        // Assert
        Assert.Equal(6, itemLayouts.Count);

        // 最初の列のアイテム（0, 1）
        Assert.Equal(0.0, itemLayouts[0].X);
        Assert.Equal(0.0, itemLayouts[0].Y);
        Assert.Equal(0.0, itemLayouts[1].X);
        Assert.Equal(100.0, itemLayouts[1].Y);

        // X座標が列ごとに増加
        Assert.True(itemLayouts[2].X >= itemLayouts[1].X);
    }

    /// <summary>ClearCache_テスト</summary>
    [Fact]
    public void ClearCache_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);
        layout.Solve(100.0);  // キャッシュを使用

        // Act
        layout.ClearCache();

        // Assert
        // キャッシュがクリアされたことを確認（間接的に）
        var (usedWidth, minHeight) = layout.Solve(100.0);
        Assert.Equal(50.0, usedWidth);
        Assert.Equal(100.0, minHeight);
    }

}
