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
    private int _userId = 12345;
    private string _userName = "JohnDoe";
    private DateTime _timestamp = new(2025, 6, 15, 14, 30, 45);
    private double _balance = 1234567.89;
    private decimal _price = 9876543.21m;
    private double _percentage = 0.15678;
    private int _quantity = 42;
    private long _orderId = 987654321012345;

    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 1: Simple UTF-8 (3 parts)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Simple"), Benchmark(Baseline = true)]
    public byte[] Simple_GetBytes_Concat()
    {
        string s = "User " + _userId + " logged in";
        return Encoding.UTF8.GetBytes(s);
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public byte[] Simple_GetBytes_Interpolation()
    {
        return Encoding.UTF8.GetBytes($"User {_userId} logged in");
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public byte[] Simple_ZString()
    {
        using var sb = ZString.CreateUtf8StringBuilder();
        sb.Append("User ");
        sb.Append(_userId);
        sb.Append(" logged in");
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public byte[] Simple_ZeroAlloc()
    {
        return Z.Utf8("User ", _userId, " logged in").ToArray();
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public int Simple_ZeroAlloc_NoAlloc()
    {
        // True zero-allocation: uses stack memory only
        using var temp = Z.Utf8("User ", _userId, " logged in");
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
            $"User {_userName} (ID: {_userId}) balance: ${_balance:N2} at {_timestamp:yyyy-MM-dd}");
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public byte[] Complex_StringBuilder()
    {
        var sb = new StringBuilder();
        sb.Append("User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(") balance: $");
        sb.Append(_balance.ToString("N2"));
        sb.Append(" at ");
        sb.Append(_timestamp.ToString("yyyy-MM-dd"));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public byte[] Complex_ZString()
    {
        using var sb = ZString.CreateUtf8StringBuilder();
        sb.Append("User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(") balance: $");
        sb.Append(_balance.ToString("N2"));
        sb.Append(" at ");
        sb.Append(_timestamp.ToString("yyyy-MM-dd"));
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public byte[] Complex_ZeroAlloc()
    {
        return Z.Utf8(
            "User ", _userName, " (ID: ", _userId, ") balance: $",
            new Utf8Formatted<double>(_balance, "N2"), " at ",
            new Utf8Formatted<DateTime>(_timestamp, "yyyy-MM-dd")).ToArray();
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public int Complex_ZeroAlloc_NoAlloc()
    {
        using var temp = Z.Utf8(
            "User ", _userName, " (ID: ", _userId, ") balance: $",
            new Utf8Formatted<double>(_balance, "N2"), " at ",
            new Utf8Formatted<DateTime>(_timestamp, "yyyy-MM-dd"));
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
            $"{{\"user\":\"{_userName}\",\"id\":{_userId},\"balance\":{_balance},\"date\":\"{_timestamp:yyyy-MM-dd}\"}}");
    }

    [BenchmarkCategory("JSON"), Benchmark]
    public byte[] Json_StringBuilder()
    {
        var sb = new StringBuilder(128);
        sb.Append("{\"user\":\"");
        sb.Append(_userName);
        sb.Append("\",\"id\":");
        sb.Append(_userId);
        sb.Append(",\"balance\":");
        sb.Append(_balance);
        sb.Append(",\"date\":\"");
        sb.Append(_timestamp.ToString("yyyy-MM-dd"));
        sb.Append("\"}");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [BenchmarkCategory("JSON"), Benchmark]
    public byte[] Json_ZString()
    {
        using var sb = ZString.CreateUtf8StringBuilder();
        sb.Append("{\"user\":\"");
        sb.Append(_userName);
        sb.Append("\",\"id\":");
        sb.Append(_userId);
        sb.Append(",\"balance\":");
        sb.Append(_balance);
        sb.Append(",\"date\":\"");
        sb.Append(_timestamp.ToString("yyyy-MM-dd"));
        sb.Append("\"}");
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("JSON"), Benchmark]
    public byte[] Json_ZeroAlloc()
    {
        return Z.Utf8(
            "{\"user\":\"", _userName,
            "\",\"id\":", _userId,
            ",\"balance\":", new Utf8Formatted<double>(_balance, "G"),
            ",\"date\":\"", new Utf8Formatted<DateTime>(_timestamp, "yyyy-MM-dd"), "\"}").ToArray();
    }

    [BenchmarkCategory("JSON"), Benchmark]
    public int Json_ZeroAlloc_NoAlloc()
    {
        using var temp = Z.Utf8(
            "{\"user\":\"", _userName,
            "\",\"id\":", _userId,
            ",\"balance\":", new Utf8Formatted<double>(_balance, "G"),
            ",\"date\":\"", new Utf8Formatted<DateTime>(_timestamp, "yyyy-MM-dd"), "\"}");
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
            $"Content-Length: {_userId}\r\nX-User-Name: {_userName}\r\nX-Request-Time: {_timestamp:O}\r\n");
    }

    [BenchmarkCategory("HTTP"), Benchmark]
    public byte[] Http_StringBuilder()
    {
        var sb = new StringBuilder(128);
        sb.Append("Content-Length: ");
        sb.Append(_userId);
        sb.Append("\r\nX-User-Name: ");
        sb.Append(_userName);
        sb.Append("\r\nX-Request-Time: ");
        sb.Append(_timestamp.ToString("O"));
        sb.Append("\r\n");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [BenchmarkCategory("HTTP"), Benchmark]
    public byte[] Http_ZString()
    {
        using var sb = ZString.CreateUtf8StringBuilder();
        sb.Append("Content-Length: ");
        sb.Append(_userId);
        sb.Append("\r\nX-User-Name: ");
        sb.Append(_userName);
        sb.Append("\r\nX-Request-Time: ");
        sb.Append(_timestamp.ToString("O"));
        sb.Append("\r\n");
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("HTTP"), Benchmark]
    public byte[] Http_ZeroAlloc()
    {
        return Z.Utf8(
            "Content-Length: ", _userId,
            "\r\nX-User-Name: ", _userName,
            "\r\nX-Request-Time: ", new Utf8Formatted<DateTime>(_timestamp, "O"),
            "\r\n").ToArray();
    }

    [BenchmarkCategory("HTTP"), Benchmark]
    public int Http_ZeroAlloc_NoAlloc()
    {
        using var temp = Z.Utf8(
            "Content-Length: ", _userId,
            "\r\nX-User-Name: ", _userName,
            "\r\nX-Request-Time: ", new Utf8Formatted<DateTime>(_timestamp, "O"),
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
        string s = string.Format(GermanCulture,
            "Bestellung #{0}: {1} × Artikel zu {2:C2}, Rabatt {3:P2}",
            _orderId, _quantity, _price, _percentage);
        return Encoding.UTF8.GetBytes(s);
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public byte[] Culture_StringBuilder()
    {
        var sb = new StringBuilder(128);
        sb.Append("Bestellung #");
        sb.Append(_orderId);
        sb.Append(": ");
        sb.Append(_quantity);
        sb.Append(" × Artikel zu ");
        sb.Append(_price.ToString("C2", GermanCulture));
        sb.Append(", Rabatt ");
        sb.Append(_percentage.ToString("P2", GermanCulture));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public byte[] Culture_ZString()
    {
        using var sb = ZString.CreateUtf8StringBuilder();
        sb.Append("Bestellung #");
        sb.Append(_orderId);
        sb.Append(": ");
        sb.Append(_quantity);
        sb.Append(" × Artikel zu ");
        sb.Append(_price.ToString("C2", GermanCulture));
        sb.Append(", Rabatt ");
        sb.Append(_percentage.ToString("P2", GermanCulture));
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public byte[] Culture_ZeroAlloc()
    {
        return Z.Utf8(
            "Bestellung #", _orderId, ": ", _quantity, " × Artikel zu ",
            new Utf8Formatted<decimal>(_price, "C2", GermanCulture), ", Rabatt ",
            new Utf8Formatted<double>(_percentage, "P2", GermanCulture)).ToArray();
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public int Culture_ZeroAlloc_NoAlloc()
    {
        using var temp = Z.Utf8(
            "Bestellung #", _orderId, ": ", _quantity, " × Artikel zu ",
            new Utf8Formatted<decimal>(_price, "C2", GermanCulture), ", Rabatt ",
            new Utf8Formatted<double>(_percentage, "P2", GermanCulture));
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
        string s = $"User {_userId} logged in";
        return Encoding.UTF8.GetBytes(s, buffer);
    }

    [BenchmarkCategory("TryUtf8"), Benchmark]
    public int TryUtf8_ZString()
    {
        Span<byte> buffer = stackalloc byte[128];
        using var sb = ZString.CreateUtf8StringBuilder();
        sb.Append("User ");
        sb.Append(_userId);
        sb.Append(" logged in");
        var span = sb.AsSpan();
        span.CopyTo(buffer);
        return span.Length;
    }

    [BenchmarkCategory("TryUtf8"), Benchmark]
    public int TryUtf8_ZeroAlloc()
    {
        Span<byte> buffer = stackalloc byte[128];
        Z.TryUtf8(buffer, out int written, "User ", _userId, " logged in");
        return written;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 7: Stress Test (Maximum complexity)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Stress"), Benchmark(Baseline = true)]
    public byte[] Stress_GetBytes()
    {
        string s = $"[{_timestamp:O}] User {_userName} (ID: {_userId}) " +
                   $"Order #{_orderId}: {_quantity}x @ {_price:C2} " +
                   $"Discount: {_percentage:P2}, Balance: ${_balance:N2}";
        return Encoding.UTF8.GetBytes(s);
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public byte[] Stress_StringBuilder()
    {
        var sb = new StringBuilder(256);
        sb.Append('[');
        sb.Append(_timestamp.ToString("O"));
        sb.Append("] User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(") Order #");
        sb.Append(_orderId);
        sb.Append(": ");
        sb.Append(_quantity);
        sb.Append("x @ ");
        sb.Append(_price.ToString("C2"));
        sb.Append(" Discount: ");
        sb.Append(_percentage.ToString("P2"));
        sb.Append(", Balance: $");
        sb.Append(_balance.ToString("N2"));
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public byte[] Stress_ZString()
    {
        using var sb = ZString.CreateUtf8StringBuilder();
        sb.Append('[');
        sb.Append(_timestamp.ToString("O"));
        sb.Append("] User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(") Order #");
        sb.Append(_orderId);
        sb.Append(": ");
        sb.Append(_quantity);
        sb.Append("x @ ");
        sb.Append(_price.ToString("C2"));
        sb.Append(" Discount: ");
        sb.Append(_percentage.ToString("P2"));
        sb.Append(", Balance: $");
        sb.Append(_balance.ToString("N2"));
        return sb.AsSpan().ToArray();
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public byte[] Stress_ZeroAlloc()
    {
        return Z.Utf8(
            "[", new Utf8Formatted<DateTime>(_timestamp, "O"), "] User ", _userName,
            " (ID: ", _userId, ") Order #", _orderId, ": ", _quantity, "x @ ",
            new Utf8Formatted<decimal>(_price, "C2"), " Discount: ",
            new Utf8Formatted<double>(_percentage, "P2"), ", Balance: $",
            new Utf8Formatted<double>(_balance, "N2")).ToArray();
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public int Stress_ZeroAlloc_NoAlloc()
    {
        using var temp = Z.Utf8(
            "[", new Utf8Formatted<DateTime>(_timestamp, "O"), "] User ", _userName,
            " (ID: ", _userId, ") Order #", _orderId, ": ", _quantity, "x @ ",
            new Utf8Formatted<decimal>(_price, "C2"), " Discount: ",
            new Utf8Formatted<double>(_percentage, "P2"), ", Balance: $",
            new Utf8Formatted<double>(_balance, "N2"));
        return temp.Length;
    }

    #endregion
}
