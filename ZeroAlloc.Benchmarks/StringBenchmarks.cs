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

    [BenchmarkCategory("Simple"), Benchmark(Baseline = true)]
    public string Simple_Concat()
    {
        return "User " + _UserId + " logged in";
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_Interpolation()
    {
        return $"User {_UserId} logged in";
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_StringBuilder()
    {
        StringBuilder sb = new();
        sb.Append("User ");
        sb.Append(_UserId);
        sb.Append(" logged in");
        return sb.ToString();
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_StringCreate()
    {
        // Optimal .NET approach using String.Create
        const int maxLen = 6 + 10 + 10; // "User " + int.MaxValue.ToString().Length + " logged in"
        return string.Create(maxLen, _UserId, static (span, userId) =>
        {
            "User ".CopyTo(span);
            int pos = 5;
            userId.TryFormat(span[pos..], out int written);
            pos += written;
            " logged in".CopyTo(span[pos..]);
        });
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_ZString()
    {
        return ZString.Concat("User ", _UserId, " logged in");
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_ZeroAlloc()
    {
        return Z.String("User ", _UserId, " logged in").ToString();
    }

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

    [BenchmarkCategory("Medium"), Benchmark(Baseline = true)]
    public string Medium_Concat()
    {
        return "User " + _UserName + " (ID: " + _UserId + ") logged in at " + _Timestamp + " session: " + _SessionId;
    }

    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_Interpolation()
    {
        return $"User {_UserName} (ID: {_UserId}) logged in at {_Timestamp} session: {_SessionId}";
    }

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

    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_ZeroAlloc()
    {
        return Z.String(
            "User ", _UserName, " (ID: ", _UserId, ") logged in at ",
            _Timestamp, " session: ", _SessionId).ToString();
    }

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

    [BenchmarkCategory("Complex"), Benchmark(Baseline = true)]
    public string Complex_Interpolation()
    {
        return $"User {_UserName} (ID: {_UserId}) at {_Timestamp:yyyy-MM-dd HH:mm:ss} balance ${_Balance:N2} session: {_SessionId}";
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public string Complex_StringBuilder()
    {
        StringBuilder sb = new StringBuilder(128);
        sb.Append("User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") at ");
        sb.Append(_Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.Append(" balance $");
        sb.Append(_Balance.ToString("N2"));
        sb.Append(" session: ");
        sb.Append(_SessionId);
        return sb.ToString();
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public string Complex_ZString()
    {
        using Utf16ValueStringBuilder sb = ZString.CreateStringBuilder();
        sb.Append("User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") at ");
        sb.Append(_Timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.Append(" balance $");
        sb.Append(_Balance.ToString("N2"));
        sb.Append(" session: ");
        sb.Append(_SessionId);
        return sb.ToString();
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public string Complex_ZeroAlloc()
    {
        return Z.String(
            "User ", _UserName, " (ID: ", _UserId, ") at ",
            new Formatted<DateTime>(_Timestamp, "yyyy-MM-dd HH:mm:ss"), " balance $",
            new Formatted<double>(_Balance, "N2"), " session: ", _SessionId).ToString();
    }

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

    [BenchmarkCategory("Culture"), Benchmark(Baseline = true)]
    public string Culture_StringFormat()
    {
        return string.Format(_GermanCulture,
            "Bestellung #{0}: {1} × Artikel zu {2:C2}, Rabatt {3:P2}, Gesamt: {4:N2} €",
            _OrderId, _Quantity, _Price, _Percentage, _Balance);
    }

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

    [BenchmarkCategory("Culture"), Benchmark]
    public string Culture_ZeroAlloc()
    {
        return Z.String(
            "Bestellung #", _OrderId, ": ", _Quantity, " × Artikel zu ",
            new Formatted<decimal>(_Price, "C2", _GermanCulture), ", Rabatt ",
            new Formatted<double>(_Percentage, "P2", _GermanCulture), ", Gesamt: ",
            new Formatted<double>(_Balance, "N2", _GermanCulture), " €").ToString();
    }

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

    [BenchmarkCategory("Stress"), Benchmark(Baseline = true)]
    public string Stress_Interpolation()
    {
        return $"[{_Timestamp:O}] User {_UserName} (ID: {_UserId}, Session: {_SessionId}) " +
               $"Order #{_OrderId}: {_Quantity}x @ {_Price:C2} (DE: {_Price.ToString("C2", _GermanCulture)}) " +
               $"Discount: {_Percentage:P2}, Balance: ${_Balance:N2}, Status: Active, Priority: High";
    }

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
        sb.Append(_Price.ToString("C2"));
        sb.Append(" (DE: ");
        sb.Append(_Price.ToString("C2", _GermanCulture));
        sb.Append(") Discount: ");
        sb.Append(_Percentage.ToString("P2"));
        sb.Append(", Balance: $");
        sb.Append(_Balance.ToString("N2"));
        sb.Append(", Status: Active, Priority: High");
        return sb.ToString();
    }

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
        sb.Append(_Price.ToString("C2"));
        sb.Append(" (DE: ");
        sb.Append(_Price.ToString("C2", _GermanCulture));
        sb.Append(") Discount: ");
        sb.Append(_Percentage.ToString("P2"));
        sb.Append(", Balance: $");
        sb.Append(_Balance.ToString("N2"));
        sb.Append(", Status: Active, Priority: High");
        return sb.ToString();
    }

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

    [BenchmarkCategory("TryString"), Benchmark(Baseline = true)]
    public int TryString_StringCreate()
    {
        Span<char> buffer = stackalloc char[64];
        "User ".CopyTo(buffer);
        int pos = 5;
        _UserId.TryFormat(buffer[pos..], out int written);
        pos += written;
        " logged in".CopyTo(buffer[pos..]);
        pos += 10;
        return pos;
    }

    [BenchmarkCategory("TryString"), Benchmark]
    public int TryString_ZeroAlloc()
    {
        Span<char> buffer = stackalloc char[64];
        Z.TryString(buffer, out int written, "User ", _UserId, " logged in");
        return written;
    }

    #endregion
}
