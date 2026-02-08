using Xunit;

using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>Method の単体テスト</summary>
public class MethodTests
{

    /// <summary>Method_値_テスト</summary>
    [Fact]
    public void Method_値_テスト()
    {
        // Assert
        Assert.Equal(0, (int)Method.DynamicProgramming);
        Assert.Equal(1, (int)Method.Greedy);
        Assert.Equal(2, (int)Method.BinarySearch);
    }

    /// <summary>Method_名前_テスト</summary>
    [Fact]
    public void Method_名前_テスト()
    {
        // Assert
        Assert.Equal("DynamicProgramming", Method.DynamicProgramming.ToString());
        Assert.Equal("Greedy", Method.Greedy.ToString());
        Assert.Equal("BinarySearch", Method.BinarySearch.ToString());
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
        var (usedWidth, minHeight) = layout.Solve(100.0);
        var segments = layout.GetLastColumnSegments();

        // Assert
        Assert.NotEmpty(segments);
        Assert.True(usedWidth > 0.0);
        Assert.True(minHeight > 0.0);
    }

}
