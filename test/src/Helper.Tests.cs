using System;
using System.Collections.Generic;
using Xunit;

using EsUtil.Algorithm.MultiColumnLayoutEngineInner;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>Helper の単体テスト</summary>
public class HelperTests
{

    /// <summary>ValidateParameter_NullItems_検証例外</summary>
    [Fact]
    public void ValidateParameter_NullItems_検証例外()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<ArgumentException>((Action)(() =>
        {
            Helper.ValidateParameter(null!, (0.0, 0.0), 10);
        }));
        Assert.Contains("null", ex.Message);
    }

    /// <summary>ValidateParameter_InvalidColumnLimit_検証例外</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateParameter_InvalidColumnLimit_検証例外(int columnLimit)
    {
        // Arrange
        var items = TestHelper.GetSingleItem();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>((Action)(() =>
        {
            Helper.ValidateParameter(items, (0.0, 0.0), columnLimit);
        }));
        Assert.Contains("positive", ex.Message);
    }

    /// <summary>ValidateParameter_NegativeSpaceRow_検証例外</summary>
    [Fact]
    public void ValidateParameter_NegativeSpaceRow_検証例外()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>((Action)(() =>
        {
            Helper.ValidateParameter(items, (-1.0, 0.0), 10);
        }));
        Assert.Contains("Row", ex.Message);
    }

    /// <summary>ValidateParameter_NegativeSpaceColumn_検証例外</summary>
    [Fact]
    public void ValidateParameter_NegativeSpaceColumn_検証例外()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>((Action)(() =>
        {
            Helper.ValidateParameter((IReadOnlyList<(double Width, double Height)>)TestHelper.GetSingleItem(), (0.0, -1.0), 10);
        }));
        Assert.Contains("Column", ex.Message);
    }

    /// <summary>ValidateParameter_InvalidItemValue_検証例外</summary>
    [Fact]
    public void ValidateParameter_InvalidItemValue_検証例外()
    {
        // Arrange
        var items = new List<(double Width, double Height)>
        {
            (-1.0, 100.0)  // 幅が負
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>((Action)(() =>
        {
            Helper.ValidateParameter(items, (0.0, 0.0), 10);
        }));
        Assert.Contains("positive", ex.Message);
    }

    /// <summary>ValidateParameterWidthLimit_InvalidValue_検証例外</summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    public void ValidateParameterWidthLimit_InvalidValue_検証例外(double widthLimit)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>((Action)(() =>
        {
            Helper.ValidateParameter(widthLimit);
        }));
        Assert.Contains("positive", ex.Message);
    }


    /// <summary>IsValidWidth_幅制限内_false</summary>
    [Fact]
    public void IsValidWidth_幅制限内_false()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();  // 幅 50
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), 10);
        const double widthLimit = 100.0;

        // Act
        bool result = layout.IsValidWidth(widthLimit);

        // Assert
        Assert.False(result);  // 50 < 100 → false
    }

    /// <summary>IsValidWidth_幅制限超過_true</summary>
    [Fact]
    public void IsValidWidth_幅制限超過_true()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();  // 幅 50
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), 10);
        const double widthLimit = 40.0;

        // Act
        bool result = layout.IsValidWidth(widthLimit);

        // Assert
        Assert.True(result);  // 50 > 40 → true
    }

    /// <summary>IsValidWidth_複数アイテムの最大値_チェック</summary>
    [Fact]
    public void IsValidWidth_複数アイテムの最大値_チェック()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();  // 幅: 40, 50, 60
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), 10);
        const double widthLimit = 59.0;

        // Act
        bool result = layout.IsValidWidth(widthLimit);

        // Assert
        Assert.True(result);  // max(40, 50, 60) = 60 > 59 → true
    }

    /// <summary>IsValidWidth_キャッシング動作テスト</summary>
    [Fact]
    public void IsValidWidth_キャッシング動作テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), 10);

        // Act
        bool result1 = layout.IsValidWidth(100.0);
        bool result2 = layout.IsValidWidth(100.0);  // 2回目はキャッシュから

        // Assert
        Assert.Equal(result1, result2);  // 結果は同じ
    }


    /// <summary>IsNonNegativeFinite_正の値_true</summary>
    [Fact]
    public void IsNonNegativeFinite_正の値_true()
    {
        // Act
        bool result = Helper.IsNonNegativeFinite(5.0);

        // Assert
        Assert.True(result);
    }

    /// <summary>IsNonNegativeFinite_ゼロ_true</summary>
    [Fact]
    public void IsNonNegativeFinite_ゼロ_true()
    {
        // Act
        bool result = Helper.IsNonNegativeFinite(0.0);

        // Assert
        Assert.True(result);
    }

    /// <summary>IsNonNegativeFinite_負の値_false</summary>
    [Fact]
    public void IsNonNegativeFinite_負の値_false()
    {
        // Act
        bool result = Helper.IsNonNegativeFinite(-1.0);

        // Assert
        Assert.False(result);
    }

    /// <summary>IsNonNegativeFinite_NaN_false</summary>
    [Fact]
    public void IsNonNegativeFinite_NaN_false()
    {
        // Act
        bool result = Helper.IsNonNegativeFinite(double.NaN);

        // Assert
        Assert.False(result);
    }

    /// <summary>IsNonNegativeFinite_無限大_false</summary>
    [Fact]
    public void IsNonNegativeFinite_無限大_false()
    {
        // Act
        bool result = Helper.IsNonNegativeFinite(double.PositiveInfinity);

        // Assert
        Assert.False(result);
    }

    /// <summary>IsPositiveFinite_正の値_true</summary>
    [Fact]
    public void IsPositiveFinite_正の値_true()
    {
        // Act
        bool result = Helper.IsPositiveFinite(5.0);

        // Assert
        Assert.True(result);
    }

    /// <summary>IsPositiveFinite_ゼロ_false</summary>
    [Fact]
    public void IsPositiveFinite_ゼロ_false()
    {
        // Act
        bool result = Helper.IsPositiveFinite(0.0);

        // Assert
        Assert.False(result);
    }

    /// <summary>IsPositiveFinite_負の値_false</summary>
    [Fact]
    public void IsPositiveFinite_負の値_false()
    {
        // Act
        bool result = Helper.IsPositiveFinite(-1.0);

        // Assert
        Assert.False(result);
    }

    /// <summary>IsPositiveFinite_NaN_false</summary>
    [Fact]
    public void IsPositiveFinite_NaN_false()
    {
        // Act
        bool result = Helper.IsPositiveFinite(double.NaN);

        // Assert
        Assert.False(result);
    }

    /// <summary>IsPositiveFinite_無限大_false</summary>
    [Fact]
    public void IsPositiveFinite_無限大_false()
    {
        // Act
        bool result = Helper.IsPositiveFinite(double.PositiveInfinity);

        // Assert
        Assert.False(result);
    }

    /// <summary>ValidateParameter_有効なパラメータ_例外なし</summary>
    [Fact]
    public void ValidateParameter_有効なパラメータ_例外なし()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var space = (Row: 0.0, Column: 0.0);
        int columnLimit = 10;

        // Act
        try
        {
            Helper.ValidateParameter(items, space, columnLimit);
        }
        catch (Exception)
        {
            Assert.Fail("例外が発生しました");
        }
    }

    /// <summary>ValidateParameterWidthLimit_有効な値_例外なし</summary>
    [Fact]
    public void ValidateParameterWidthLimit_有効な値_例外なし()
    {
        // Act
        try
        {
            Helper.ValidateParameter(100.0);
        }
        catch (Exception)
        {
            Assert.Fail("例外が発生しました");
        }
    }

    /// <summary>ValidateParameterWidthLimit_NaN_例外</summary>
    [Fact]
    public void ValidateParameterWidthLimit_NaN_例外()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Helper.ValidateParameter(double.NaN));
        Assert.Contains("positive", ex.Message);
    }

    /// <summary>ValidateParameterWidthLimit_無限大_例外</summary>
    [Fact]
    public void ValidateParameterWidthLimit_無限大_例外()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => Helper.ValidateParameter(double.PositiveInfinity));
        Assert.Contains("positive", ex.Message);
    }

}
