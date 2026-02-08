using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using static EsUtil.Algorithm.MultiColumnLayoutEngine;

namespace EsUtil.Algorithm.MultiColumnLayoutEngineTest;

/// <summary>
/// MultiColumnLayout 詳細ベンチマーク<br/>
/// 
/// 【計測項目】<br/>
/// - double型対応<br/>
/// - アイテム数: 10, 30, 60, 100, 500, 1000<br/>
/// - 列数制限: デフォルト (10), 5列限定, 10列限定<br/>
/// - DP vs Greedy 比較<br/>
/// - タイムアウト検出（2秒）<br/>
/// - 詳細統計出力<br/>
/// - CPU情報の記載<br/>
/// 
/// 【計測精度】<br/>
/// - ミリ秒単位で小数第3位まで表示<br/>
/// - 複数回計測による中央値<br/>
/// </summary>
public class Benchmark
{
    private const int MeasureIterations = 5;
    private static readonly int[] ItemCounts = [10, 30, 60, 100, 300, 600, 1000, 10000, 100000, 1000000, 10000000];
    //private static readonly int[] ItemCounts = [10000000];

    private const double WidthLimit = 500.0;
    private const double RowSpace = 5.0;
    private const double ColumnSpace = 10.0;
    private const int DefaultColumnLimit = 5;
    private const long TimeoutMs = 2000;

    /// <summary>main エントリポイント</summary>
    public static void Main()
    {
        // 【エンコーディング修正】PowerShell での文字化け対策
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("VerticalMultiColumnLayout - ベンチマーク");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine();

        Console.WriteLine($"【実行環境】.NET 10, C# 14.0");
        Console.WriteLine();
        Console.WriteLine($"※ {MeasureIterations}回の計測中央値を表示");
        Console.WriteLine("※ DB未計測の品質G(%)はBSearch基準で算出");

        // Double型をテスト
        RunBenchmark(ItemCounts, MeasureIterations);

        Console.WriteLine("\nベンチマーク完了");
    }

    private static void RunBenchmark(int[] itemCounts, int iteration)
    {
        // 完全ベンチマーク
        var results = PerformMeasurements(itemCounts, iteration);

        if (results.Count > 0)
        {
            GenerateReport(results);
        }
        else
        {
            Console.WriteLine("  ⚠️ 計測結果なし");
        }
    }

    private static List<BenchmarkData> PerformMeasurements(int[] itemCounts, int iteration)
    {
        var results = new List<BenchmarkData>();
        Warmup();

        Console.WriteLine(new string('-', 84));
        Console.WriteLine($"{"アイテム数",11} | {"ctor ms",7} | {"DP ms",7} | {"Gr ms",7} | {"BS ms (iter)",12} | {"品質G(%)",6} | {"品質B(%)",6}");
        Console.WriteLine(new string('-', 84));

        foreach (int count in itemCounts)
        {
            if (count == 1)
                Console.Write($"計測中:    初回 ");
            else
                Console.Write($"計測中: {count,8} ");
            Console.Out.Flush();

            try
            {
                Thread.Sleep(100);
                var data = MeasurePerformance(count, iteration);
                if (data == null)
                {
                    Console.WriteLine("❌ タイムアウト");
                    continue;
                }
                results.Add(data);

                string ctor = !double.IsNaN(data.ConstructorTimeMs) ? $"{data.ConstructorTimeMs,7:F3}" : "   N/A ";
                string dpSolve = !double.IsNaN(data.DPSolveTimeMs) ? $"{data.DPSolveTimeMs,7:F3}" : "   N/A ";
                string grSolve = $"{data.GreedySolveTimeMs,7:F3}";
                string bsSolve = !double.IsNaN(data.BSearchSolveTimeMs) ? $"{data.BSearchSolveTimeMs,7:F3} ({data.BSearchIterationCount,2})" : "   N/A      ";

                string grQuality = !double.IsNaN(data.GreedyAccuracy) ? $"{data.GreedyAccuracy:F1}%" : "N/A ";
                string bsQuality = !double.IsNaN(data.BSearchAccuracy) ? $"{data.BSearchAccuracy:F1}%" : "N/A ";
                string grMark = !double.IsNaN(data.GreedyAccuracy) ? GetAccuracyMark(data.GreedyAccuracy) : "  ";
                string bsMark = !double.IsNaN(data.BSearchAccuracy) ? GetAccuracyMark(data.BSearchAccuracy) : "  ";

                Console.WriteLine($"| {ctor} | {dpSolve} | {grSolve} | {bsSolve} | {grQuality,6}{grMark} | {bsQuality,6}{bsMark}");
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ エラー: {ex.Message}");
                Console.Out.Flush();
            }
        }

        return results;
    }

    private static string GetAccuracyMark(double accuracy)
    {
        if (accuracy > 100.00)
            return "❌";
        if (accuracy >= 99.95)
            return "✨";
        else if (accuracy >= 95.0)
            return "✔ ";
        else if (accuracy >= 90.0)
            return "⚠ ";
        else
            return "❌";
    }

    private static double GetMedian(List<double> values)
    {
        if (values.Count == 0)
            return double.NaN;

        var sorted = values.OrderBy(x => x).ToList();
        int count = sorted.Count;

        if (count % 2 == 0)
            return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
        else
            return sorted[count / 2];
    }

    private static BenchmarkData? MeasurePerformance(int itemCount, int iteration)
    {
        var items = TestHelper.GenerateRandomItems(itemCount, seed: 0);

        try
        {
            // iteration 回計測を実施（コンストラクタと Solve を分離）
            var ctorTimes = new List<double>();
            var dpSolveTimes = new List<double>();
            var grSolveTimes = new List<double>();
            var bsSolveTimes = new List<double>();
            var bsIterations = new List<int>();
            
            double dpMinHeight = 0.0;
            double dpUsedWidth = 0.0;
            double grMinHeight = 0.0;
            double grUsedWidth = 0.0;
            double bsMinHeight = 0.0;
            double bsUsedWidth = 0.0;

            for (int i = 0; i < iteration; i++)
            {
                // コンストラクタ計測
                var swCtor = Stopwatch.StartNew();
                var layout = new MultiColumnLayoutEngine(items, (RowSpace, ColumnSpace), DefaultColumnLimit);
                swCtor.Stop();
                ctorTimes.Add(swCtor.Elapsed.TotalMilliseconds);

                // DP計測（1000アイテム以下の場合のみ）
                if (itemCount <= 1000)
                {
                    // Solve計測
                    layout.ClearCache();
                    layout.CurrentMethod = Method.DynamicProgramming;
                    var swDpSolve = Stopwatch.StartNew();
                    (dpUsedWidth, dpMinHeight) = layout.Solve(WidthLimit);
                    swDpSolve.Stop();
                    dpSolveTimes.Add(swDpSolve.Elapsed.TotalMilliseconds);
                    if (swDpSolve.Elapsed.TotalMilliseconds > TimeoutMs)
                        return null;
                }

                // Greedy計測
                layout.ClearCache();
                layout.CurrentMethod = Method.Greedy;
                var swGrSolve = Stopwatch.StartNew();
                (grUsedWidth, grMinHeight) = layout.Solve(WidthLimit);
                swGrSolve.Stop();
                grSolveTimes.Add(swGrSolve.Elapsed.TotalMilliseconds);
                if (swGrSolve.Elapsed.TotalMilliseconds > TimeoutMs)
                    return null;

                // BSearch計測（1000000アイテム以下の場合のみ）
                if (itemCount <= 1000000)
                {
                    // Solve計測
                    layout.ClearCache();
                    layout.CurrentMethod = Method.BinarySearch;
                    var swBsSolve = Stopwatch.StartNew();
                    (bsUsedWidth, bsMinHeight) = layout.Solve(WidthLimit);
                    swBsSolve.Stop();
                    bsSolveTimes.Add(swBsSolve.Elapsed.TotalMilliseconds);

                    int bsearchIterationCount = (int)layout.GetBinarySearchIterationCount()!;
                    bsIterations.Add(bsearchIterationCount);
                    if (swBsSolve.Elapsed.TotalMilliseconds > TimeoutMs)
                        return null;
                }
            }

            // タイムアウト処理
            if (grSolveTimes.Any(t => t > TimeoutMs) || bsSolveTimes.Any(t => t > TimeoutMs))
                return null;

            // 中央値の計算（コンストラクタ / Solve を分離して中央値を算出）
            double ctorMedian = GetMedian(ctorTimes);
            double dpSolveMedian = (itemCount <= 1000) ? GetMedian(dpSolveTimes) : double.NaN;
            double greedySolveMedian = GetMedian(grSolveTimes);
            double bsearchSolveMedian = (itemCount <= 1000000) ? GetMedian(bsSolveTimes) : double.NaN;

            // BSearch反復回数
            int bsearchIteration = (itemCount <= 1000000) ? bsIterations.First() : 0;

            // 統計計算
            double itemWidth = items.First().Width;
            double itemHeight = items.First().Height;
            double greedyAccuracy = (itemCount <= 1000) ? TestHelper.CalculateAccuracy(dpMinHeight, grMinHeight)
                : (itemCount <= 1000000) ? TestHelper.CalculateAccuracy(bsMinHeight, grMinHeight) : double.NaN;
            double bsearchAccuracy = (itemCount <= 1000) ? TestHelper.CalculateAccuracy(dpMinHeight, bsMinHeight) : double.NaN;

            return new BenchmarkData
            {
                ItemCount = itemCount,
                ConstructorTimeMs = ctorMedian,
                DPSolveTimeMs = dpSolveMedian,
                GreedySolveTimeMs = greedySolveMedian,
                BSearchSolveTimeMs = bsearchSolveMedian,

                DPMinHeight = (itemCount <= 1000) ? dpMinHeight : double.NaN,
                DPUsedWidth = (itemCount <= 1000) ? dpUsedWidth : double.NaN,
                GreedyMinHeight = grMinHeight,
                GreedyUsedWidth = grUsedWidth,
                BSearchMinHeight = (itemCount <= 1000000) ? bsMinHeight : double.NaN,
                BSearchUsedWidth = (itemCount <= 1000000) ? bsUsedWidth : double.NaN,
                AvgItemWidth = itemWidth,
                AvgItemHeight = itemHeight,
                GreedyAccuracy = greedyAccuracy,
                BSearchAccuracy = bsearchAccuracy,
                BSearchIterationCount = bsearchIteration
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  エラー: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    private static void GenerateReport(List<BenchmarkData> results)
    {
        var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Report");
        Directory.CreateDirectory(dir);

        var typeName = "double";
        var path = Path.Combine(dir, $"benchmark_{typeName}_report.txt");

        using var writer = new StreamWriter(path, false, Encoding.UTF8);
        writer.WriteLine($"VerticalMultiColumnLayout - {typeName}型 詳細ベンチマークレポート");
        writer.WriteLine(new string('=', 160));
        writer.WriteLine();
        writer.WriteLine("【実行環境】");
        writer.WriteLine($"  .NET: 10.0");
        writer.WriteLine($"  言語: C# 14.0");
        writer.WriteLine();
        writer.WriteLine("【計測条件】");
        writer.WriteLine($"  widthLimit: {WidthLimit}, rowSpace: {RowSpace}, columnSpace: {ColumnSpace}, columnLimit: {DefaultColumnLimit}");
        writer.WriteLine($"  タイムアウト: {TimeoutMs}ms");
        writer.WriteLine($"  精度: ミリ秒単位小数第3位");
        writer.WriteLine($"  計測回数: {MeasureIterations}回（中央値を採用）");
        writer.WriteLine();
        writer.WriteLine("【詳細結果】");
        writer.WriteLine();
        writer.WriteLine("アイテム数 |  平均幅 | 平均高さ | ctor(ms) |   DP(ms) |   Gr(ms) | BS(ms) (Iter) | 品質G | 品質B");
        writer.WriteLine(new string('-', 115));
        foreach (var data in results)
        {
            var ctorTms = double.IsNaN(data.ConstructorTimeMs) ? "N/A " : $"{data.ConstructorTimeMs,8:F3}";
            var dpTms = double.IsNaN(data.DPSolveTimeMs) ? "N/A " : $"{data.DPSolveTimeMs,8:F3}";
            var grTms = double.IsNaN(data.GreedySolveTimeMs) ? "N/A " : $"{data.GreedySolveTimeMs,8:F3}";
            var bsTms = double.IsNaN(data.BSearchSolveTimeMs) ? "N/A " : $"{data.BSearchSolveTimeMs,8:F3}";
            var grAccuracy = double.IsNaN(data.GreedyAccuracy) ? "N/A " : $"{data.GreedyAccuracy,5:F1}";
            var bsAccuracy = double.IsNaN(data.BSearchAccuracy) ? "N/A " : $"{data.BSearchAccuracy,5:F1}";

            var bsIter = data.BSearchIterationCount > 0 ? $"{data.BSearchIterationCount,2}" : "  ";
            writer.WriteLine($"{data.ItemCount,8} | {data.AvgItemWidth,6:F1} | {data.AvgItemHeight,7:F1} | " +
                $"{ctorTms,8} | {dpTms,8} | {grTms,8} | {bsTms,8} ({bsIter}) | {grAccuracy,5} | {bsAccuracy,5}");
        }
        writer.WriteLine();
        writer.WriteLine("【統計分析】");

        writer.WriteLine($"    中央値(ctor): {BenchmarkData.GetMedianConstructorTime(results):F3}ms");
        writer.WriteLine($"  【DP法】");
        writer.WriteLine($"    DP中央値(solve): {BenchmarkData.GetMedianDPSolveTime(results):F3}ms");

        writer.WriteLine($"  【Greedy法】");
        writer.WriteLine($"    平均品質: {BenchmarkData.GetAvgGreedyAccuracy(results):F1}%");
        writer.WriteLine($"    最小品質: {BenchmarkData.GetMinGreedyAccuracy(results):F1}%");
        writer.WriteLine($"    最大品質: {BenchmarkData.GetMaxGreedyAccuracy(results):F1}%");
        writer.WriteLine($"    中央値(solve): {BenchmarkData.GetMedianGreedySolveTime(results):F3}ms");
        writer.WriteLine();
        writer.WriteLine($"  【BSearch法】");
        writer.WriteLine($"    平均品質: {BenchmarkData.GetAvgBSearchAccuracy(results):F1}%");
        writer.WriteLine($"    最小品質: {BenchmarkData.GetMinBSearchAccuracy(results):F1}%");
        writer.WriteLine($"    最大品質: {BenchmarkData.GetMaxBSearchAccuracy(results):F1}%");
        writer.WriteLine($"    中央値(solve): {BenchmarkData.GetMedianBSearchSolveTime(results):F3}ms");
        writer.WriteLine($"    平均反復回数: {BenchmarkData.GetAvgBSearchIterations(results):F1}");
        writer.WriteLine();
        writer.WriteLine("【結論】");
        writer.WriteLine($"  Greedy品質目標達成: {(BenchmarkData.GetMinGreedyAccuracy(results) >= 90.0 ? "✓ 達成" : "✗ 要検討")} (最小品質: {BenchmarkData.GetMinGreedyAccuracy(results):F1}%)");
        writer.WriteLine($"  BSearch品質評価: {(BenchmarkData.GetMinBSearchAccuracy(results) >= 95.0 ? "✓ 優秀" : BenchmarkData.GetMinBSearchAccuracy(results) >= 90.0 ? "✓ 良好" : "✗ 要検討")} (最小品質: {BenchmarkData.GetMinBSearchAccuracy(results):F1}%)");
        writer.WriteLine();
        writer.WriteLine($"レポート出力: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        writer.WriteLine($"ファイルパス: {path}");
    }

    private static void Warmup()
    {
        var warmupItems = TestHelper.GenerateRandomItems(10, seed: 42);

        // 各アルゴリズムを1回実行してJITコンパイルとメモリ割り当てを安定化
        var layout1 = new MultiColumnLayoutEngine(warmupItems, (RowSpace, ColumnSpace), DefaultColumnLimit)
        {
            CurrentMethod = Method.DynamicProgramming
        };
        _ = layout1.Solve(WidthLimit);

        var layout2 = new MultiColumnLayoutEngine(warmupItems, (RowSpace, ColumnSpace), DefaultColumnLimit)
        {
            CurrentMethod = Method.Greedy
        };
        _ = layout2.Solve(WidthLimit);

        var layout3 = new MultiColumnLayoutEngine(warmupItems, (RowSpace, ColumnSpace), DefaultColumnLimit)
        {
            CurrentMethod = Method.BinarySearch
        };
        _ = layout3.Solve(WidthLimit);
    }

    private class BenchmarkData
    {
        public int ItemCount { get; set; }

        // 追加: コンストラクタ / Solve を分離して保持
        public double ConstructorTimeMs { get; set; }

        public double DPSolveTimeMs { get; set; }
        public double GreedySolveTimeMs { get; set; }
        public double BSearchSolveTimeMs { get; set; }

        public double DPMinHeight { get; set; }
        public double DPUsedWidth { get; set; }

        public double GreedyMinHeight { get; set; }
        public double GreedyUsedWidth { get; set; }

        public double BSearchMinHeight { get; set; }
        public double BSearchUsedWidth { get; set; }

        public double AvgItemWidth { get; set; }
        public double AvgItemHeight { get; set; }

        public double GreedyAccuracy { get; set; }
        public double BSearchAccuracy { get; set; }

        public int BSearchIterationCount { get; set; }

        public static double GetAvgGreedyAccuracy(List<BenchmarkData> results) => results.Average(r => r.GreedyAccuracy);
        public static double GetMinGreedyAccuracy(List<BenchmarkData> results) => results.Min(r => r.GreedyAccuracy);
        public static double GetMaxGreedyAccuracy(List<BenchmarkData> results) => results.Max(r => r.GreedyAccuracy);

        public static double GetAvgBSearchAccuracy(List<BenchmarkData> results) => results.Where(w => !double.IsNaN(w.BSearchAccuracy)).Average(r => r.BSearchAccuracy);
        public static double GetMinBSearchAccuracy(List<BenchmarkData> results) => results.Where(w => !double.IsNaN(w.BSearchAccuracy)).Min(r => r.BSearchAccuracy);
        public static double GetMaxBSearchAccuracy(List<BenchmarkData> results) => results.Where(w => !double.IsNaN(w.BSearchAccuracy)).Max(r => r.BSearchAccuracy);

        // 追加: ctor / solve のメディアン取得
        public static double GetMedianConstructorTime(List<BenchmarkData> results) => GetMedian([.. results.Where(r => !double.IsNaN(r.ConstructorTimeMs)).Select(r => r.ConstructorTimeMs)]);
        public static double GetMedianDPSolveTime(List<BenchmarkData> results) => GetMedian([.. results.Where(r => !double.IsNaN(r.DPSolveTimeMs)).Select(r => r.DPSolveTimeMs)]);
        public static double GetMedianGreedySolveTime(List<BenchmarkData> results) => GetMedian([.. results.Select(r => r.GreedySolveTimeMs)]);
        public static double GetMedianBSearchSolveTime(List<BenchmarkData> results) => GetMedian([.. results.Select(r => r.BSearchSolveTimeMs)]);

        public static double GetAvgBSearchIterations(List<BenchmarkData> results) => 
            results.Where(r => r.BSearchIterationCount > 0).Average(r => r.BSearchIterationCount);
    }
}
