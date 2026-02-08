using Xunit;

using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>MetadataKey の単体テスト</summary>
public class MetadataKeyTests
{

    /// <summary>MetadataKey_値_テスト</summary>
    [Fact]
    public void MetadataKey_値_テスト()
    {
        // Assert
        Assert.Equal(0, (int)MetadataKey.StrategyName);
        Assert.Equal(1, (int)MetadataKey.BinarySearchIterationCount);
    }

    /// <summary>MetadataKey_名前_テスト</summary>
    [Fact]
    public void MetadataKey_名前_テスト()
    {
        // Assert
        Assert.Equal("StrategyName", MetadataKey.StrategyName.ToString());
        Assert.Equal("BinarySearchIterationCount", MetadataKey.BinarySearchIterationCount.ToString());
    }

}
