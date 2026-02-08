using System;
using System.Collections.Generic;
using Xunit;

using EsUtil.Algorithm.MultiColumnLayoutEngineInner;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>Common の単体テスト</summary>
public class CommonTests
{

    /// <summary>StrategyResult_Empty_プロパティテスト</summary>
    [Fact]
    public void StrategyResult_Empty_プロパティテスト()
    {
        // Act
        StrategyResult empty = StrategyResult.Empty;

        // Assert
        Assert.Equal(0.0, empty.MinHeight);
        Assert.Equal(0.0, empty.UsedWidth);
        Assert.Empty(empty.ColumnSegments);
    }

    /// <summary>StrategyResult_コンストラクタ_テスト</summary>
    [Fact]
    public void StrategyResult_コンストラクタ_テスト()
    {
        // Arrange
        var segments = new ColumnSegment[] { new(0, 1), new(2, 3) };

        // Act
        var result = new StrategyResult(100.0, 200.0, segments);

        // Assert
        Assert.Equal(100.0, result.UsedWidth);
        Assert.Equal(200.0, result.MinHeight);
        Assert.Equal(2, result.ColumnSegments.Length);
        Assert.Equal(0, result.ColumnSegments[0].StartIdx);
        Assert.Equal(1, result.ColumnSegments[0].EndIdx);
    }


    /// <summary>ColumnSegment_コンストラクタ_テスト</summary>
    [Fact]
    public void ColumnSegment_コンストラクタ_テスト()
    {
        // Act
        var segment = new ColumnSegment(5, 10);

        // Assert
        Assert.Equal(5, segment.StartIdx);
        Assert.Equal(10, segment.EndIdx);
    }

    /// <summary>ColumnSegment_等価性_テスト</summary>
    [Fact]
    public void ColumnSegment_等価性_テスト()
    {
        // Arrange
        var segment1 = new ColumnSegment(0, 5);
        var segment2 = new ColumnSegment(0, 5);
        var segment3 = new ColumnSegment(1, 5);

        // Assert
        Assert.Equal(segment1, segment2);
        Assert.NotEqual(segment1, segment3);
    }


    /// <summary>Space_コンストラクタ_テスト</summary>
    [Fact]
    public void Space_コンストラクタ_テスト()
    {
        // Act
        var space = new Space(10.0, 20.0);

        // Assert
        Assert.Equal(10.0, space.Row);
        Assert.Equal(20.0, space.Column);
    }

    /// <summary>Space_デフォルト値_テスト</summary>
    [Fact]
    public void Space_デフォルト値_テスト()
    {
        // Act
        var space = new Space(0.0, 0.0);

        // Assert
        Assert.Equal(0.0, space.Row);
        Assert.Equal(0.0, space.Column);
    }


    /// <summary>Size_コンストラクタ_テスト</summary>
    [Fact]
    public void Size_コンストラクタ_テスト()
    {
        // Act
        var size = new Size(50.0, 100.0);

        // Assert
        Assert.Equal(50.0, size.Width);
        Assert.Equal(100.0, size.Height);
    }

    /// <summary>Size_ToSizeArray_IReadOnlyList_テスト</summary>
    [Fact]
    public void Size_ToSizeArray_IReadOnlyList_テスト()
    {
        // Arrange
        var items = new List<(double Width, double Height)>
        {
            (40.0, 100.0),
            (50.0, 120.0),
            (60.0, 140.0)
        };

        // Act
        var sizes = Size.ToSizeArray(items);

        // Assert
        Assert.Equal(3, sizes.Length);
        Assert.Equal(40.0, sizes[0].Width);
        Assert.Equal(100.0, sizes[0].Height);
        Assert.Equal(50.0, sizes[1].Width);
        Assert.Equal(120.0, sizes[1].Height);
        Assert.Equal(60.0, sizes[2].Width);
        Assert.Equal(140.0, sizes[2].Height);
    }

    /// <summary>Size_ToSizeArray_ReadOnlySpan_テスト</summary>
    [Fact]
    public void Size_ToSizeArray_ReadOnlySpan_テスト()
    {
        // Arrange
        var items = new (double Width, double Height)[]
        {
            (40.0, 100.0),
            (50.0, 120.0)
        };
        var span = new ReadOnlySpan<(double Width, double Height)>(items);

        // Act
        var sizes = Size.ToSizeArray(span);

        // Assert
        Assert.Equal(2, sizes.Length);
        Assert.Equal(40.0, sizes[0].Width);
        Assert.Equal(100.0, sizes[0].Height);
        Assert.Equal(50.0, sizes[1].Width);
        Assert.Equal(120.0, sizes[1].Height);
    }


    // Common.cs の構造体は internal なので、ここで直接テスト可能
    // ただし、パブリックAPI経由でカバレッジ100%を目指すため、必要最小限

    /// <summary>StrategyResult_デコンストラクタ_テスト</summary>
    [Fact]
    public void StrategyResult_デコンストラクタ_テスト()
    {
        // Arrange
        var segments = new ColumnSegment[] { new(0, 1) };
        var result = new StrategyResult(150.0, 250.0, segments);

        // Act
        var (usedWidth, minHeight, columnSegments) = result;

        // Assert
        Assert.Equal(150.0, usedWidth);
        Assert.Equal(250.0, minHeight);
        Assert.Single(columnSegments);
    }

    /// <summary>ColumnSegment_デコンストラクタ_テスト</summary>
    [Fact]
    public void ColumnSegment_デコンストラクタ_テスト()
    {
        // Arrange
        var segment = new ColumnSegment(3, 7);

        // Act
        var (start, end) = segment;

        // Assert
        Assert.Equal(3, start);
        Assert.Equal(7, end);
    }

    /// <summary>Space_デコンストラクタ_テスト</summary>
    [Fact]
    public void Space_デコンストラクタ_テスト()
    {
        // Arrange
        var space = new Space(5.0, 15.0);

        // Act
        var (row, column) = space;

        // Assert
        Assert.Equal(5.0, row);
        Assert.Equal(15.0, column);
    }

    /// <summary>Size_デコンストラクタ_テスト</summary>
    [Fact]
    public void Size_デコンストラクタ_テスト()
    {
        // Arrange
        var size = new Size(80.0, 160.0);

        // Act
        var (width, height) = size;

        // Assert
        Assert.Equal(80.0, width);
        Assert.Equal(160.0, height);
    }

    /// <summary>Size_ToSizeArray_空リスト_テスト</summary>
    [Fact]
    public void Size_ToSizeArray_空リスト_テスト()
    {
        // Arrange
        var items = new List<(double Width, double Height)>();

        // Act
        var sizes = Size.ToSizeArray(items);

        // Assert
        Assert.Empty(sizes);
    }

    /// <summary>Size_ToSizeArray_空Span_テスト</summary>
    [Fact]
    public void Size_ToSizeArray_空Span_テスト()
    {
        // Arrange
        var items = Array.Empty<(double Width, double Height)>();
        var span = new ReadOnlySpan<(double Width, double Height)>(items);

        // Act
        var sizes = Size.ToSizeArray(span);

        // Assert
        Assert.Empty(sizes);
    }
}
