// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Benchmarks;

/// <summary>
/// Benchmarks comparing ZeroAlloc.LocalizedString vs standard interpolation.
/// 
/// LocalizedString produces IDENTICAL output to interpolated strings
/// when using CurrentCulture, while maintaining zero-allocation characteristics.
/// 
/// This is important for:
/// - Localized applications
/// - Currency formatting
/// - Date/time formatting
/// - Number formatting with culture-specific separators
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class LocalizedStringBenchmarks
{
    // === Test Data ===
    private int _userId = 12345;
    private string _userName = "JohnDoe";
    private DateTime _timestamp = new(2025, 6, 15, 14, 30, 45);
    private double _balance = 1234567.89;
    private decimal _price = 9876543.21m;
    private double _percentage = 0.15678;
    private long _orderId = 987654321012345;

    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    [GlobalSetup]
    public void Setup()
    {
        // Set German culture for culture-sensitive formatting tests
        CultureInfo.CurrentCulture = GermanCulture;
    }

    #region Output Verification (Debug helper)

    /// <summary>
    /// Verifies that LocalizedString produces identical output to interpolation.
    /// Call this to debug/verify before running benchmarks.
    /// </summary>
    public static void VerifyOutputEquality()
    {
        CultureInfo germanCulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = germanCulture;

        int userId = 12345;
        double balance = 1234567.89;
        DateTime timestamp = new(2025, 6, 15, 14, 30, 45);

        string interpolated = $"User {userId} has balance {balance} at {timestamp}";
        string localizedString = Z.LocalizedString(germanCulture, "User ", userId, " has balance ", balance, " at ", timestamp);

        Console.WriteLine($"Interpolated:      {interpolated}");
        Console.WriteLine($"LocalizedString:   {localizedString}");
        Console.WriteLine($"Are equal:         {interpolated == localizedString}");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 1: Simple Integer
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Simple"), Benchmark(Baseline = true)]
    public string Simple_Interpolation()
    {
        return $"User {_userId} logged in";
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_LocalizedString()
    {
        return Z.LocalizedString(GermanCulture, "User ", _userId, " logged in");
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 2: Numbers (Culture-sensitive formatting)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Numbers"), Benchmark(Baseline = true)]
    public string Numbers_Interpolation()
    {
        // German culture: 1234567,89 (comma as decimal separator)
        return $"Balance: {_balance}, Price: {_price}";
    }

    [BenchmarkCategory("Numbers"), Benchmark]
    public string Numbers_LocalizedString()
    {
        return Z.LocalizedString(GermanCulture, "Balance: ", _balance, ", Price: ", _price);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 3: DateTime (Culture-sensitive formatting)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("DateTime"), Benchmark(Baseline = true)]
    public string DateTime_Interpolation()
    {
        // German: 15.06.2025 14:30:45
        return $"Logged in at {_timestamp}";
    }

    [BenchmarkCategory("DateTime"), Benchmark]
    public string DateTime_LocalizedString()
    {
        return Z.LocalizedString(GermanCulture, "Logged in at ", _timestamp);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 4: Complex (Multiple types)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Complex"), Benchmark(Baseline = true)]
    public string Complex_Interpolation()
    {
        return $"Order #{_orderId}: User {_userName} spent {_balance} ({_percentage}%) at {_timestamp}";
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public string Complex_LocalizedString()
    {
        return Z.LocalizedString(GermanCulture,
            "Order #", _orderId, ": User ", _userName,
            " spent ", _balance, " (", _percentage, "%) at ", _timestamp);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 5: Mixed Strings and Numbers
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Mixed"), Benchmark(Baseline = true)]
    public string Mixed_Interpolation()
    {
        return $"User {_userName} has ID {_userId} with balance {_balance}";
    }

    [BenchmarkCategory("Mixed"), Benchmark]
    public string Mixed_LocalizedString()
    {
        return Z.LocalizedString(GermanCulture, "User ", _userName, " has ID ", _userId, " with balance ", _balance);
    }

    #endregion
}
