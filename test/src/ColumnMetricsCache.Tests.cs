using Xunit;

using EsUtil.Algorithm.MultiColumnLayoutEngineInner;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;


/// <summary>MultiColumnLayout の単体テスト</summary>
public class ColumnMetricsCacheTests
{

    /// <summary>GetColumnMetrics_メトリクス計算_テスト</summary>
    [Fact]
    public void GetColumnMetrics_メトリクス計算_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var layout = new MultiColumnLayoutEngine(items, (5.0, 0.0), columnLimit: 10);

        // Act
        var (Width, Height) = layout.GetColumnMetrics(new ColumnSegment(0, 1));  // 最初の2個のアイテム

        // Assert
        // 高さ = 100 + 120 + 5 = 225
        Assert.Equal(225.0, Height);
        // 幅 = max(40, 50) = 50
        Assert.Equal(50.0, Width);
    }

    /// <summary>GetColumnMetrics_単一アイテム_テスト</summary>
    [Fact]
    public void GetColumnMetrics_単一アイテム_テスト()
    {
        // Arrange
        var items = TestHelper.GetSingleItem();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);

        // Act
        var (Width, Height) = layout.GetColumnMetrics(new ColumnSegment(0, 0));

        // Assert
        Assert.Equal(100.0, Height);
        Assert.Equal(50.0, Width);
    }

    /// <summary>GetColumnMetrics_キャッシング効率_テスト</summary>
    [Fact]
    public void GetColumnMetrics_キャッシング効率_テスト()
    {
        // Arrange
        var items = TestHelper.GetLargeTestItems();
        var layout = new MultiColumnLayoutEngine(items, (2.0, 2.0), columnLimit: 5);

        // Act
        // 同じセグメントで複数回呼び出し
        var (Width, Height) = layout.GetColumnMetrics(new ColumnSegment(0, 2));
        var metrics2 = layout.GetColumnMetrics(new ColumnSegment(0, 2));
        var metrics3 = layout.GetColumnMetrics(new ColumnSegment(3, 5));

        // Assert
        // キャッシュされた値は同じ
        Assert.Equal(Height, metrics2.Height);
        Assert.Equal(Width, metrics2.Width);
        // 異なるセグメントは異なる値
        Assert.NotEqual(Height, metrics3.Height);
    }

    /// <summary>GetColumnMetrics_逆順インデックス_デフォルト返却</summary>
    [Fact]
    public void GetColumnMetrics_逆順インデックス_デフォルト返却()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var layout = new MultiColumnLayoutEngine(items, (0.0, 0.0), columnLimit: 10);

        // Act
        var (width, height) = layout.GetColumnMetrics(new(5, 2));  // 逆順

        // Assert
        Assert.Equal(0.0, width);
        Assert.Equal(0.0, height);
    }

    /// <summary>GetColumnMetrics_単一アイテム_行間スペース不適用</summary>
    [Fact]
    public void GetColumnMetrics_単一アイテム_行間スペース不適用()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();  // 最初のアイテム (40, 100)
        var space = (Row: 100.0, Column: 0.0);
        var layout = new MultiColumnLayoutEngine(items, space, 10);

        // Act
        var (width, height) = layout.GetColumnMetrics(new ColumnSegment(0, 0));

        // Assert
        // 単一アイテムなので行間スペースは加算されない
        Assert.Equal(40.0, width);
        Assert.Equal(100.0, height);
    }

    /// <summary>GetColumnMetrics_複数アイテム_行間スペース正確計算</summary>
    [Fact]
    public void GetColumnMetrics_複数アイテム_行間スペース正確計算()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();  // (40,100), (50,120), (60,140)
        var space = (Row: 5.0, Column: 0.0);
        var layout = new MultiColumnLayoutEngine(items, space, 10);

        // Act
        var (width, height) = layout.GetColumnMetrics(new(0, 2));

        // Assert
        // 高さ = 100 + 5 + 120 + 5 + 140 = 370
        // 幅 = max(40, 50, 60) = 60
        Assert.Equal(60.0, width);
        Assert.Equal(370.0, height);
    }

    /// <summary>GetColumnMetrics_同一セグメント_複数回呼び出し</summary>
    [Fact]
    public void GetColumnMetrics_同一セグメント_複数回呼び出し()
    {
        // Arrange
        var items = TestHelper.GetLargeTestItems();
        var layout = new MultiColumnLayoutEngine(items, (2.0, 2.0), columnLimit: 5);

        // Act
        var (Width, Height) = layout.GetColumnMetrics(new ColumnSegment(0, 2));
        var (Width2, Height2) = layout.GetColumnMetrics(new ColumnSegment(0, 2));
        var (Width3, _) = layout.GetColumnMetrics(new ColumnSegment(3, 5));

        // Assert
        Assert.Equal(Height, Height2);
        Assert.Equal(Width, Width2);
        Assert.NotEqual(Height, Width3);
    }

    /// <summary>ColumnMetricsCache_GetMetrics_キャッシュミス_テスト</summary>
    [Fact]
    public void ColumnMetricsCache_GetMetrics_キャッシュミス_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var cache = new ColumnMetricsCache(Size.ToSizeArray(items), new Space(Row: 0.0, Column: 0.0));

        // Act
        var metrics = cache.GetMetrics(new ColumnSegment(0, 1));

        // Assert
        Assert.Equal(50.0, metrics.Width);  // max(40,50)
        Assert.Equal(220.0, metrics.Height);  // 100 + 120
    }

    /// <summary>ColumnMetricsCache_GetMetrics_キャッシュヒット_テスト</summary>
    [Fact]
    public void ColumnMetricsCache_GetMetrics_キャッシュヒット_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var cache = new ColumnMetricsCache(Size.ToSizeArray(items), new Space(Row: 0.0, Column: 0.0));
        var segment = new ColumnSegment(0, 1);
        cache.GetMetrics(segment); // 初回

        // Act
        var metrics = cache.GetMetrics(segment); // 2回目

        // Assert
        // キャッシュから返却される
        Assert.Equal(50.0, metrics.Width);
        Assert.Equal(220.0, metrics.Height);
    }

    /// <summary>ColumnMetricsCache_Clear_テスト</summary>
    [Fact]
    public void ColumnMetricsCache_Clear_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var cache = new ColumnMetricsCache(Size.ToSizeArray(items), new Space(Row: 0.0, Column: 0.0));
        var segment = new ColumnSegment(0, 1);
        cache.GetMetrics(segment);
        cache.Clear();

        // Act
        var metrics = cache.GetMetrics(segment); // クリア後

        // Assert
        Assert.Equal(50.0, metrics.Width);
        Assert.Equal(220.0, metrics.Height);
    }

    /// <summary>ColumnMetricsCache_GetMetrics_異なるセグメント_テスト</summary>
    [Fact]
    public void ColumnMetricsCache_GetMetrics_異なるセグメント_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var cache = new ColumnMetricsCache(Size.ToSizeArray(items), new Space(Row: 0.0, Column: 0.0));
        var segment1 = new ColumnSegment(0, 1);
        var segment2 = new ColumnSegment(2, 2);

        // Act
        var metrics1 = cache.GetMetrics(segment1);
        var metrics2 = cache.GetMetrics(segment2);

        // Assert
        Assert.Equal(50.0, metrics1.Width);  // max(40,50)
        Assert.Equal(220.0, metrics1.Height);  // 100+120
        Assert.Equal(60.0, metrics2.Width);  // 60
        Assert.Equal(140.0, metrics2.Height);  // 140
    }

    /// <summary>ColumnMetricsCache_CalcMetrics_逆順セグメント_テスト</summary>
    [Fact]
    public void ColumnMetricsCache_CalcMetrics_逆順セグメント_テスト()
    {
        // Arrange
        var items = TestHelper.GetStandardTestItems();
        var cache = new ColumnMetricsCache(Size.ToSizeArray(items), new Space(Row: 0.0, Column: 0.0));

        // Act
        var metrics = cache.CalcMetrics(new ColumnSegment(2, 0));  // 逆順

        // Assert
        Assert.Equal(0.0, metrics.Width);
        Assert.Equal(0.0, metrics.Height);
    }
}


