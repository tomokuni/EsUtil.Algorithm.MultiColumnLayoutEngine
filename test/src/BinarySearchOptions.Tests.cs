using System;
using Xunit;

using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>BinarySearchOptions の単体テスト</summary>
public class BinarySearchOptionsTests
{

    /// <summary>BinarySearchOptions_Default_プロパティテスト</summary>
    [Fact]
    public void BinarySearchOptions_Default_プロパティテスト()
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

    /// <summary>BinarySearchOptions_デフォルトコンストラクタ_テスト</summary>
    [Fact]
    public void BinarySearchOptions_デフォルトコンストラクタ_テスト()
    {
        // Act
        var options = new BinarySearchOptions();

        // Assert
        Assert.Equal(1e-3, options.Epsilon);
        Assert.Equal(100, options.MaxIterations);
        Assert.Equal(0.95, options.LowerBoundRatio);
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

    /// <summary>BinarySearchOptions_IsValid_有効な値_テスト</summary>
    [Fact]
    public void BinarySearchOptions_IsValid_有効な値_テスト()
    {
        // Arrange
        var options = new BinarySearchOptions(1e-3, 100, 0.95);

        // Act
        try
        {
            options.IsValid();
        }
        catch (Exception)
        {
            Assert.Fail("例外が発生しました");
        }
    }

    /// <summary>BinarySearchOptions_IsValid_無効なEpsilon_例外</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void BinarySearchOptions_IsValid_無効なEpsilon_例外(double epsilon)
    {
        // Arrange
        var options = new BinarySearchOptions(epsilon, 100, 0.95);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.IsValid());
        Assert.Contains("Epsilon", ex.Message);
    }

    /// <summary>BinarySearchOptions_IsValid_無効なMaxIterations_例外</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BinarySearchOptions_IsValid_無効なMaxIterations_例外(int maxIterations)
    {
        // Arrange
        var options = new BinarySearchOptions(1e-3, maxIterations, 0.95);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.IsValid());
        Assert.Contains("MaxIterations", ex.Message);
    }

    /// <summary>BinarySearchOptions_IsValid_無効なLowerBoundRatio_例外</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void BinarySearchOptions_IsValid_無効なLowerBoundRatio_例外(double lowerBoundRatio)
    {
        // Arrange
        var options = new BinarySearchOptions(1e-3, 100, lowerBoundRatio);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => options.IsValid());
        Assert.Contains("LowerBoundRatio", ex.Message);
    }

    /// <summary>BinarySearchOptions_境界値_テスト</summary>
    [Fact]
    public void BinarySearchOptions_境界値_テスト()
    {
        // Arrange
        var options = new BinarySearchOptions(double.Epsilon, 1, double.Epsilon);

        // Act
        try
        {
            options.IsValid();
        }
        catch (Exception)
        {
            Assert.Fail("例外が発生しました");
        }
    }

}
