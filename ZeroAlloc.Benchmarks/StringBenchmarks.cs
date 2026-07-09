// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Benchmarks;

/// <summary>
/// Comprehensive benchmarks comparing ZeroAlloc.String performance against:
/// - String concatenation (+ operator)
/// - String interpolation ($"...")
/// - StringBuilder (with and without capacity)
/// - String.Create (stackalloc-based)
/// - ZString (Cysharp's zero-allocation library)
/// 
/// Each category tests progressively more complex scenarios.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class StringBenchmarks
{
    // === Test Data ===
    private int _UserId = 12345;
    private string _UserName = "JohnDoe";
    private DateTime _Timestamp = new(2025, 6, 15, 14, 30, 45);
    private double _Balance = 1234567.89;
    private Guid _SessionId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    private decimal _Price = 9876543.21m;
    private double _Percentage = 0.15678;
    private int _Quantity = 42;
    private long _OrderId = 987654321012345;

    private static readonly CultureInfo _GermanCulture = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo _InvariantCulture = CultureInfo.InvariantCulture;

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 1: Simple (3 parts: string + int + string)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Simple 3-part string built with + concatenation; baseline for the Simple category.
    /// </summary>
    [BenchmarkCategory("Simple"), Benchmark(Baseline = true)]
    public string Simple_Concat()
    {
        return "User " + _UserId + " logged in";
    }

    /// <summary>
    /// Same simple string built with string interpolation ($"...").
    /// </summary>
    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_Interpolation()
    {
        return $"User {_UserId} logged in";
    }

    /// <summary>
    /// Same simple string built with StringBuilder append calls.
    /// </summary>
    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_StringBuilder()
    {
        StringBuilder sb = new();
        sb.Append("User ");
        sb.Append(_UserId);
        sb.Append(" logged in");
        return sb.ToString();
    }

    /// <summary>
    /// Same simple string built with String.Create and stack-allocated span formatting.
    /// </summary>
    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_StringCreate()
    {
        // Optimal .NET approach using String.Create
        const int maxLen = 6 + 10 + 10; // "User " + int.MaxValue.ToString().Length + " logged in"
        return string.Create(maxLen, _UserId, static (span, userId) =>
        {
            "User ".CopyTo(span);
            int pos = 5;
            userId.TryFormat(span[pos..], out int written, provider: CultureInfo.InvariantCulture);
            pos += written;
            " logged in".CopyTo(span[pos..]);
        });
    }

    /// <summary>
    /// Same simple string built with ZString.Concat.
    /// </summary>
    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_ZString()
    {
        return ZString.Concat("User ", _UserId, " logged in");
    }

    /// <summary>
    /// Same simple string built with Z.String and converted to a heap string.
    /// </summary>
    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_ZeroAlloc()
    {
        return Z.String("User ", _UserId, " logged in").ToString();
    }

    /// <summary>
    /// Same simple string built with Z.String on the stack only; no heap allocation.
    /// </summary>
    [BenchmarkCategory("Simple"), Benchmark]
    public int Simple_ZeroAlloc_NoAlloc()
    {
        // True zero-allocation: uses stack memory only
        using TempString temp = Z.String("User ", _UserId, " logged in");
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 2: Medium (7 parts: mixed types, no formatting)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Medium 7-part mixed-type string built with + concatenation; baseline for the Medium category.
    /// </summary>
    [BenchmarkCategory("Medium"), Benchmark(Baseline = true)]
    public string Medium_Concat()
    {
        return "User " + _UserName + " (ID: " + _UserId + ") logged in at " + _Timestamp + " session: " + _SessionId;
    }

    /// <summary>
    /// Same medium string built with string interpolation.
    /// </summary>
    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_Interpolation()
    {
        return $"User {_UserName} (ID: {_UserId}) logged in at {_Timestamp} session: {_SessionId}";
    }

    /// <summary>
    /// Same medium string built with StringBuilder append calls.
    /// </summary>
    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_StringBuilder()
    {
        StringBuilder sb = new();
        sb.Append("User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") logged in at ");
        sb.Append(_Timestamp);
        sb.Append(" session: ");
        sb.Append(_SessionId);
        return sb.ToString();
    }

    /// <summary>
    /// Same medium string built with a pre-sized StringBuilder (128-byte capacity).
    /// </summary>
    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_StringBuilder_Capacity()
    {
        StringBuilder sb = new StringBuilder(128);
        sb.Append("User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") logged in at ");
        sb.Append(_Timestamp);
        sb.Append(" session: ");
        sb.Append(_SessionId);
        return sb.ToString();
    }

    /// <summary>
    /// Same medium string built with ZString Utf16ValueStringBuilder.
    /// </summary>
    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_ZString()
    {
        using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
        sb.Append("User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") logged in at ");
        sb.Append(_Timestamp);
        sb.Append(" session: ");
        sb.Append(_SessionId);
        return sb.ToString();
    }

    /// <summary>
    /// Same medium string built with Z.String and converted to a heap string.
    /// </summary>
    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_ZeroAlloc()
    {
        return Z.String(
            "User ", _UserName, " (ID: ", _UserId, ") logged in at ",
            _Timestamp, " session: ", _SessionId).ToString();
    }

    /// <summary>
    /// Same medium string built with Z.String on the stack only; no heap allocation.
    /// </summary>
    [BenchmarkCategory("Medium"), Benchmark]
    public int Medium_ZeroAlloc_NoAlloc()
    {
        using TempString temp = Z.String(
            "User ", _UserName, " (ID: ", _UserId, ") logged in at ",
            _Timestamp, " session: ", _SessionId);
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 3: Complex (with custom formatting)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Complex formatted string with date and currency patterns via interpolation; baseline for Complex.
    /// </summary>
    [BenchmarkCategory("Complex"), Benchmark(Baseline = true)]
    public string Complex_Interpolation()
    {
        return $"User {_UserName} (ID: {_UserId}) at {_Timestamp:yyyy-MM-dd HH:mm:ss} balance ${_Balance:N2} session: {_SessionId}";
    }

    /// <summary>
    /// Same complex formatted string built with StringBuilder and explicit ToString formatting.
    /// </summary>
    [BenchmarkCategory("Complex"), Benchmark]
    public string Complex_StringBuilder()
    {
        StringBuilder sb = new StringBuilder(128);
        sb.Append("User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") at ");
        sb.Append(_Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        sb.Append(" balance $");
        sb.Append(_Balance.ToString("N2", CultureInfo.InvariantCulture));
        sb.Append(" session: ");
        sb.Append(_SessionId);
        return sb.ToString();
    }

    /// <summary>
    /// Same complex formatted string built with ZString Utf16ValueStringBuilder.
    /// </summary>
    [BenchmarkCategory("Complex"), Benchmark]
    public string Complex_ZString()
    {
        using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
        sb.Append("User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") at ");
        sb.Append(_Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        sb.Append(" balance $");
        sb.Append(_Balance.ToString("N2", CultureInfo.InvariantCulture));
        sb.Append(" session: ");
        sb.Append(_SessionId);
        return sb.ToString();
    }

    /// <summary>
    /// Same complex formatted string built with Z.String and Formatted wrappers.
    /// </summary>
    [BenchmarkCategory("Complex"), Benchmark]
    public string Complex_ZeroAlloc()
    {
        return Z.String(
            "User ", _UserName, " (ID: ", _UserId, ") at ",
            new Formatted<DateTime>(_Timestamp, "yyyy-MM-dd HH:mm:ss"), " balance $",
            new Formatted<double>(_Balance, "N2"), " session: ", _SessionId).ToString();
    }

    /// <summary>
    /// Same complex formatted string built with Z.String on the stack only; no heap allocation.
    /// </summary>
    [BenchmarkCategory("Complex"), Benchmark]
    public int Complex_ZeroAlloc_NoAlloc()
    {
        using TempString temp = Z.String(
            "User ", _UserName, " (ID: ", _UserId, ") at ",
            new Formatted<DateTime>(_Timestamp, "yyyy-MM-dd HH:mm:ss"), " balance $",
            new Formatted<double>(_Balance, "N2"), " session: ", _SessionId);
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 4: Culture-Specific (German formatting)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// German-locale order summary built with string.Format; baseline for the Culture category.
    /// </summary>
    [BenchmarkCategory("Culture"), Benchmark(Baseline = true)]
    public string Culture_StringFormat()
    {
        return string.Format(_GermanCulture,
            "Bestellung #{0}: {1} × Artikel zu {2:C2}, Rabatt {3:P2}, Gesamt: {4:N2} €",
            _OrderId, _Quantity, _Price, _Percentage, _Balance);
    }

    /// <summary>
    /// Same German-locale order summary built with StringBuilder and culture-specific ToString calls.
    /// </summary>
    [BenchmarkCategory("Culture"), Benchmark]
    public string Culture_StringBuilder()
    {
        StringBuilder sb = new StringBuilder(128);
        sb.Append("Bestellung #");
        sb.Append(_OrderId);
        sb.Append(": ");
        sb.Append(_Quantity);
        sb.Append(" × Artikel zu ");
        sb.Append(_Price.ToString("C2", _GermanCulture));
        sb.Append(", Rabatt ");
        sb.Append(_Percentage.ToString("P2", _GermanCulture));
        sb.Append(", Gesamt: ");
        sb.Append(_Balance.ToString("N2", _GermanCulture));
        sb.Append(" €");
        return sb.ToString();
    }

    /// <summary>
    /// Same German-locale order summary built with ZString Utf16ValueStringBuilder.
    /// </summary>
    [BenchmarkCategory("Culture"), Benchmark]
    public string Culture_ZString()
    {
        using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
        sb.Append("Bestellung #");
        sb.Append(_OrderId);
        sb.Append(": ");
        sb.Append(_Quantity);
        sb.Append(" × Artikel zu ");
        sb.Append(_Price.ToString("C2", _GermanCulture));
        sb.Append(", Rabatt ");
        sb.Append(_Percentage.ToString("P2", _GermanCulture));
        sb.Append(", Gesamt: ");
        sb.Append(_Balance.ToString("N2", _GermanCulture));
        sb.Append(" €");
        return sb.ToString();
    }

    /// <summary>
    /// Same German-locale order summary built with Z.String and culture-aware Formatted wrappers.
    /// </summary>
    [BenchmarkCategory("Culture"), Benchmark]
    public string Culture_ZeroAlloc()
    {
        return Z.String(
            "Bestellung #", _OrderId, ": ", _Quantity, " × Artikel zu ",
            new Formatted<decimal>(_Price, "C2", _GermanCulture), ", Rabatt ",
            new Formatted<double>(_Percentage, "P2", _GermanCulture), ", Gesamt: ",
            new Formatted<double>(_Balance, "N2", _GermanCulture), " €").ToString();
    }

    /// <summary>
    /// Same German-locale order summary built with Z.String on the stack only; no heap allocation.
    /// </summary>
    [BenchmarkCategory("Culture"), Benchmark]
    public int Culture_ZeroAlloc_NoAlloc()
    {
        using TempString temp = Z.String(
            "Bestellung #", _OrderId, ": ", _Quantity, " × Artikel zu ",
            new Formatted<decimal>(_Price, "C2", _GermanCulture), ", Rabatt ",
            new Formatted<double>(_Percentage, "P2", _GermanCulture), ", Gesamt: ",
            new Formatted<double>(_Balance, "N2", _GermanCulture), " €");
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 5: Stress Test (Maximum complexity)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maximum-complexity multi-field string with mixed formatting via interpolation; baseline for Stress.
    /// </summary>
    [BenchmarkCategory("Stress"), Benchmark(Baseline = true)]
    public string Stress_Interpolation()
    {
        return $"[{_Timestamp:O}] User {_UserName} (ID: {_UserId}, Session: {_SessionId}) " +
               $"Order #{_OrderId}: {_Quantity}x @ {_Price:C2} (DE: {_Price.ToString("C2", _GermanCulture)}) " +
               $"Discount: {_Percentage:P2}, Balance: ${_Balance:N2}, Status: Active, Priority: High";
    }

    /// <summary>
    /// Same stress-test string built with a pre-sized StringBuilder and explicit formatting.
    /// </summary>
    [BenchmarkCategory("Stress"), Benchmark]
    public string Stress_StringBuilder()
    {
        StringBuilder sb = new StringBuilder(512);
        sb.Append('[');
        sb.Append(_Timestamp.ToString("O"));
        sb.Append("] User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(", Session: ");
        sb.Append(_SessionId);
        sb.Append(") Order #");
        sb.Append(_OrderId);
        sb.Append(": ");
        sb.Append(_Quantity);
        sb.Append("x @ ");
        sb.Append(_Price.ToString("C2", CultureInfo.InvariantCulture));
        sb.Append(" (DE: ");
        sb.Append(_Price.ToString("C2", _GermanCulture));
        sb.Append(") Discount: ");
        sb.Append(_Percentage.ToString("P2", CultureInfo.InvariantCulture));
        sb.Append(", Balance: $");
        sb.Append(_Balance.ToString("N2", CultureInfo.InvariantCulture));
        sb.Append(", Status: Active, Priority: High");
        return sb.ToString();
    }

    /// <summary>
    /// Same stress-test string built with ZString Utf16ValueStringBuilder.
    /// </summary>
    [BenchmarkCategory("Stress"), Benchmark]
    public string Stress_ZString()
    {
        using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
        sb.Append('[');
        sb.Append(_Timestamp.ToString("O"));
        sb.Append("] User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(", Session: ");
        sb.Append(_SessionId);
        sb.Append(") Order #");
        sb.Append(_OrderId);
        sb.Append(": ");
        sb.Append(_Quantity);
        sb.Append("x @ ");
        sb.Append(_Price.ToString("C2", CultureInfo.InvariantCulture));
        sb.Append(" (DE: ");
        sb.Append(_Price.ToString("C2", _GermanCulture));
        sb.Append(") Discount: ");
        sb.Append(_Percentage.ToString("P2", CultureInfo.InvariantCulture));
        sb.Append(", Balance: $");
        sb.Append(_Balance.ToString("N2", CultureInfo.InvariantCulture));
        sb.Append(", Status: Active, Priority: High");
        return sb.ToString();
    }

    /// <summary>
    /// Same stress-test string built with Z.String and Formatted wrappers.
    /// </summary>
    [BenchmarkCategory("Stress"), Benchmark]
    public string Stress_ZeroAlloc()
    {
        return Z.String(
            "[", new Formatted<DateTime>(_Timestamp, "O"), "] User ", _UserName,
            " (ID: ", _UserId, ", Session: ", _SessionId, ") Order #", _OrderId,
            ": ", _Quantity, "x @ ", new Formatted<decimal>(_Price, "C2"),
            " (DE: ", new Formatted<decimal>(_Price, "C2", _GermanCulture), ") Discount: ",
            new Formatted<double>(_Percentage, "P2"), ", Balance: $",
            new Formatted<double>(_Balance, "N2"),
            ", Status: Active, Priority: High").ToString();
    }

    /// <summary>
    /// Same stress-test string built with Z.String on the stack only; no heap allocation.
    /// </summary>
    [BenchmarkCategory("Stress"), Benchmark]
    public int Stress_ZeroAlloc_NoAlloc()
    {
        using TempString temp = Z.String(
            "[", new Formatted<DateTime>(_Timestamp, "O"), "] User ", _UserName,
            " (ID: ", _UserId, ", Session: ", _SessionId, ") Order #", _OrderId,
            ": ", _Quantity, "x @ ", new Formatted<decimal>(_Price, "C2"),
            " (DE: ", new Formatted<decimal>(_Price, "C2", _GermanCulture), ") Discount: ",
            new Formatted<double>(_Percentage, "P2"), ", Balance: $",
            new Formatted<double>(_Balance, "N2"),
            ", Status: Active, Priority: High");
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 6: TryString (Stack-allocated destination)
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Writes a simple string into a stack buffer via manual span copy and TryFormat; baseline for TryString.
    /// </summary>
    [BenchmarkCategory("TryString"), Benchmark(Baseline = true)]
    public int TryString_StringCreate()
    {
        Span<char> buffer = stackalloc char[64];
        "User ".CopyTo(buffer);
        int pos = 5;
        _UserId.TryFormat(buffer[pos..], out int written, provider: CultureInfo.InvariantCulture);
        pos += written;
        " logged in".CopyTo(buffer[pos..]);
        pos += 10;
        return pos;
    }

    /// <summary>
    /// Same stack-buffer write using Z.TryString instead of manual span formatting.
    /// </summary>
    [BenchmarkCategory("TryString"), Benchmark]
    public int TryString_ZeroAlloc()
    {
        Span<char> buffer = stackalloc char[64];
        Z.TryString(buffer, out int written, "User ", _UserId, " logged in");
        return written;
    }

    #endregion
}
