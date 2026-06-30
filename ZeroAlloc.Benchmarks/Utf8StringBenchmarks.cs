// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Benchmarks;

/// <summary>
/// Comprehensive benchmarks comparing ZeroAlloc UTF-8 performance against:
/// - Encoding.UTF8.GetBytes with string interpolation
/// - Encoding.UTF8.GetBytes with StringBuilder
/// - ZString UTF-8 builder
/// 
/// UTF-8 encoding is critical for:
/// - HTTP headers and responses
/// - JSON serialization
/// - Protocol buffers
/// - Network packet payloads
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class Utf8StringBenchmarks
{
    // === Test Data ===
    private int _UserId = 12345;
    private string _UserName = "JohnDoe";
    private DateTime _Timestamp = new(2025, 6, 15, 14, 30, 45);
    private double _Balance = 1234567.89;
    private decimal _Price = 9876543.21m;
    private double _Percentage = 0.15678;
    private int _Quantity = 42;
    private long _OrderId = 987654321012345;

    private static readonly CultureInfo _GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 1: Simple UTF-8 (3 parts)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Simple"), Benchmark(Baseline = true)]
    public byte[] Simple_GetBytes_Concat()
    {
        string s = "User " + _UserId + " logged in";
        return Encoding.UTF8.GetBytes(s);
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public byte[] Simple_GetBytes_Interpolation()
    {
        return Encoding.UTF8.GetBytes($"User {_UserId} logged in");
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public byte[] Simple_ZString()
    {
        using Utf8ValueStringBuilder sb = ZString.CreateUtf8StringBuilder();
        sb.Append("User ");
        sb.Append(_UserId);
        sb.Append(" logged in");
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public byte[] Simple_ZeroAlloc()
    {
        return Z.Utf8("User ", _UserId, " logged in").ToArray();
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public int Simple_ZeroAlloc_NoAlloc()
    {
        // True zero-allocation: uses stack memory only
        using TempBytes temp = Z.Utf8("User ", _UserId, " logged in");
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 2: Complex UTF-8 (with formatting)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Complex"), Benchmark(Baseline = true)]
    public byte[] Complex_GetBytes_Interpolation()
    {
        return Encoding.UTF8.GetBytes(
            $"User {_UserName} (ID: {_UserId}) balance: ${_Balance:N2} at {_Timestamp:yyyy-MM-dd}");
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public byte[] Complex_StringBuilder()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") balance: $");
        sb.Append(_Balance.ToString("N2"));
        sb.Append(" at ");
        sb.Append(_Timestamp.ToString("yyyy-MM-dd"));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public byte[] Complex_ZString()
    {
        using Utf8ValueStringBuilder sb = ZString.CreateUtf8StringBuilder();
        sb.Append("User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") balance: $");
        sb.Append(_Balance.ToString("N2"));
        sb.Append(" at ");
        sb.Append(_Timestamp.ToString("yyyy-MM-dd"));
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public byte[] Complex_ZeroAlloc()
    {
        return Z.Utf8(
            "User ", _UserName, " (ID: ", _UserId, ") balance: $",
            new Utf8Formatted<double>(_Balance, "N2"), " at ",
            new Utf8Formatted<DateTime>(_Timestamp, "yyyy-MM-dd")).ToArray();
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public int Complex_ZeroAlloc_NoAlloc()
    {
        using TempBytes temp = Z.Utf8(
            "User ", _UserName, " (ID: ", _UserId, ") balance: $",
            new Utf8Formatted<double>(_Balance, "N2"), " at ",
            new Utf8Formatted<DateTime>(_Timestamp, "yyyy-MM-dd"));
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 3: JSON-like Output
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("JSON"), Benchmark(Baseline = true)]
    public byte[] Json_GetBytes()
    {
        return Encoding.UTF8.GetBytes(
            $"{{\"user\":\"{_UserName}\",\"id\":{_UserId},\"balance\":{_Balance},\"date\":\"{_Timestamp:yyyy-MM-dd}\"}}");
    }

    [BenchmarkCategory("JSON"), Benchmark]
    public byte[] Json_StringBuilder()
    {
        StringBuilder sb = new StringBuilder(128);
        sb.Append("{\"user\":\"");
        sb.Append(_UserName);
        sb.Append("\",\"id\":");
        sb.Append(_UserId);
        sb.Append(",\"balance\":");
        sb.Append(_Balance);
        sb.Append(",\"date\":\"");
        sb.Append(_Timestamp.ToString("yyyy-MM-dd"));
        sb.Append("\"}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [BenchmarkCategory("JSON"), Benchmark]
    public byte[] Json_ZString()
    {
        using Utf8ValueStringBuilder sb = ZString.CreateUtf8StringBuilder();
        sb.Append("{\"user\":\"");
        sb.Append(_UserName);
        sb.Append("\",\"id\":");
        sb.Append(_UserId);
        sb.Append(",\"balance\":");
        sb.Append(_Balance);
        sb.Append(",\"date\":\"");
        sb.Append(_Timestamp.ToString("yyyy-MM-dd"));
        sb.Append("\"}");
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("JSON"), Benchmark]
    public byte[] Json_ZeroAlloc()
    {
        return Z.Utf8(
            "{\"user\":\"", _UserName,
            "\",\"id\":", _UserId,
            ",\"balance\":", new Utf8Formatted<double>(_Balance, "G"),
            ",\"date\":\"", new Utf8Formatted<DateTime>(_Timestamp, "yyyy-MM-dd"), "\"}").ToArray();
    }

    [BenchmarkCategory("JSON"), Benchmark]
    public int Json_ZeroAlloc_NoAlloc()
    {
        using TempBytes temp = Z.Utf8(
            "{\"user\":\"", _UserName,
            "\",\"id\":", _UserId,
            ",\"balance\":", new Utf8Formatted<double>(_Balance, "G"),
            ",\"date\":\"", new Utf8Formatted<DateTime>(_Timestamp, "yyyy-MM-dd"), "\"}");
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 4: HTTP Headers
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("HTTP"), Benchmark(Baseline = true)]
    public byte[] Http_GetBytes()
    {
        return Encoding.UTF8.GetBytes(
            $"Content-Length: {_UserId}\r\nX-User-Name: {_UserName}\r\nX-Request-Time: {_Timestamp:O}\r\n");
    }

    [BenchmarkCategory("HTTP"), Benchmark]
    public byte[] Http_StringBuilder()
    {
        StringBuilder sb = new StringBuilder(128);
        sb.Append("Content-Length: ");
        sb.Append(_UserId);
        sb.Append("\r\nX-User-Name: ");
        sb.Append(_UserName);
        sb.Append("\r\nX-Request-Time: ");
        sb.Append(_Timestamp.ToString("O"));
        sb.Append("\r\n");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [BenchmarkCategory("HTTP"), Benchmark]
    public byte[] Http_ZString()
    {
        using Utf8ValueStringBuilder sb = ZString.CreateUtf8StringBuilder();
        sb.Append("Content-Length: ");
        sb.Append(_UserId);
        sb.Append("\r\nX-User-Name: ");
        sb.Append(_UserName);
        sb.Append("\r\nX-Request-Time: ");
        sb.Append(_Timestamp.ToString("O"));
        sb.Append("\r\n");
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("HTTP"), Benchmark]
    public byte[] Http_ZeroAlloc()
    {
        return Z.Utf8(
            "Content-Length: ", _UserId,
            "\r\nX-User-Name: ", _UserName,
            "\r\nX-Request-Time: ", new Utf8Formatted<DateTime>(_Timestamp, "O"),
            "\r\n").ToArray();
    }

    [BenchmarkCategory("HTTP"), Benchmark]
    public int Http_ZeroAlloc_NoAlloc()
    {
        using TempBytes temp = Z.Utf8(
            "Content-Length: ", _UserId,
            "\r\nX-User-Name: ", _UserName,
            "\r\nX-Request-Time: ", new Utf8Formatted<DateTime>(_Timestamp, "O"),
            "\r\n");
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 5: Culture-Specific UTF-8
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Culture"), Benchmark(Baseline = true)]
    public byte[] Culture_GetBytes()
    {
        string s = string.Format(_GermanCulture,
            "Bestellung #{0}: {1} × Artikel zu {2:C2}, Rabatt {3:P2}",
            _OrderId, _Quantity, _Price, _Percentage);
        return Encoding.UTF8.GetBytes(s);
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public byte[] Culture_StringBuilder()
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
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public byte[] Culture_ZString()
    {
        using Utf8ValueStringBuilder sb = ZString.CreateUtf8StringBuilder();
        sb.Append("Bestellung #");
        sb.Append(_OrderId);
        sb.Append(": ");
        sb.Append(_Quantity);
        sb.Append(" × Artikel zu ");
        sb.Append(_Price.ToString("C2", _GermanCulture));
        sb.Append(", Rabatt ");
        sb.Append(_Percentage.ToString("P2", _GermanCulture));
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public byte[] Culture_ZeroAlloc()
    {
        return Z.Utf8(
            "Bestellung #", _OrderId, ": ", _Quantity, " × Artikel zu ",
            new Utf8Formatted<decimal>(_Price, "C2", _GermanCulture), ", Rabatt ",
            new Utf8Formatted<double>(_Percentage, "P2", _GermanCulture)).ToArray();
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public int Culture_ZeroAlloc_NoAlloc()
    {
        using TempBytes temp = Z.Utf8(
            "Bestellung #", _OrderId, ": ", _Quantity, " × Artikel zu ",
            new Utf8Formatted<decimal>(_Price, "C2", _GermanCulture), ", Rabatt ",
            new Utf8Formatted<double>(_Percentage, "P2", _GermanCulture));
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 6: TryUtf8 (Stack-allocated destination)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("TryUtf8"), Benchmark(Baseline = true)]
    public int TryUtf8_GetBytes_StackAlloc()
    {
        Span<byte> buffer = stackalloc byte[128];
        string s = $"User {_UserId} logged in";
        return Encoding.UTF8.GetBytes(s, buffer);
    }

    [BenchmarkCategory("TryUtf8"), Benchmark]
    public int TryUtf8_ZString()
    {
        Span<byte> buffer = stackalloc byte[128];
        using Utf8ValueStringBuilder sb = ZString.CreateUtf8StringBuilder();
        sb.Append("User ");
        sb.Append(_UserId);
        sb.Append(" logged in");
        ReadOnlySpan<byte> span = sb.AsSpan();
        span.CopyTo(buffer);
        return span.Length;
    }

    [BenchmarkCategory("TryUtf8"), Benchmark]
    public int TryUtf8_ZeroAlloc()
    {
        Span<byte> buffer = stackalloc byte[128];
        Z.TryUtf8(buffer, out int written, "User ", _UserId, " logged in");
        return written;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 7: Stress Test (Maximum complexity)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Stress"), Benchmark(Baseline = true)]
    public byte[] Stress_GetBytes()
    {
        string s = $"[{_Timestamp:O}] User {_UserName} (ID: {_UserId}) " +
                   $"Order #{_OrderId}: {_Quantity}x @ {_Price:C2} " +
                   $"Discount: {_Percentage:P2}, Balance: ${_Balance:N2}";
        return Encoding.UTF8.GetBytes(s);
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public byte[] Stress_StringBuilder()
    {
        StringBuilder sb = new StringBuilder(256);
        sb.Append('[');
        sb.Append(_Timestamp.ToString("O"));
        sb.Append("] User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") Order #");
        sb.Append(_OrderId);
        sb.Append(": ");
        sb.Append(_Quantity);
        sb.Append("x @ ");
        sb.Append(_Price.ToString("C2"));
        sb.Append(" Discount: ");
        sb.Append(_Percentage.ToString("P2"));
        sb.Append(", Balance: $");
        sb.Append(_Balance.ToString("N2"));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public byte[] Stress_ZString()
    {
        using Utf8ValueStringBuilder sb = ZString.CreateUtf8StringBuilder();
        sb.Append('[');
        sb.Append(_Timestamp.ToString("O"));
        sb.Append("] User ");
        sb.Append(_UserName);
        sb.Append(" (ID: ");
        sb.Append(_UserId);
        sb.Append(") Order #");
        sb.Append(_OrderId);
        sb.Append(": ");
        sb.Append(_Quantity);
        sb.Append("x @ ");
        sb.Append(_Price.ToString("C2"));
        sb.Append(" Discount: ");
        sb.Append(_Percentage.ToString("P2"));
        sb.Append(", Balance: $");
        sb.Append(_Balance.ToString("N2"));
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public byte[] Stress_ZeroAlloc()
    {
        return Z.Utf8(
            "[", new Utf8Formatted<DateTime>(_Timestamp, "O"), "] User ", _UserName,
            " (ID: ", _UserId, ") Order #", _OrderId, ": ", _Quantity, "x @ ",
            new Utf8Formatted<decimal>(_Price, "C2"), " Discount: ",
            new Utf8Formatted<double>(_Percentage, "P2"), ", Balance: $",
            new Utf8Formatted<double>(_Balance, "N2")).ToArray();
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public int Stress_ZeroAlloc_NoAlloc()
    {
        using TempBytes temp = Z.Utf8(
            "[", new Utf8Formatted<DateTime>(_Timestamp, "O"), "] User ", _UserName,
            " (ID: ", _UserId, ") Order #", _OrderId, ": ", _Quantity, "x @ ",
            new Utf8Formatted<decimal>(_Price, "C2"), " Discount: ",
            new Utf8Formatted<double>(_Percentage, "P2"), ", Balance: $",
            new Utf8Formatted<double>(_Balance, "N2"));
        return temp.Length;
    }

    #endregion
}
