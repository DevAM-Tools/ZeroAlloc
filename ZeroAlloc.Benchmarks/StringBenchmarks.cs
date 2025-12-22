/*
MIT License
SPDX-License-Identifier: MIT

Copyright (c) 2025 ZeroAlloc Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

namespace ZeroAlloc.Benchmarks;

using System.Buffers;
using System.Globalization;
using System.Text;
using Cysharp.Text;

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
    private int _userId = 12345;
    private string _userName = "JohnDoe";
    private DateTime _timestamp = new(2025, 6, 15, 14, 30, 45);
    private double _balance = 1234567.89;
    private Guid _sessionId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    private decimal _price = 9876543.21m;
    private double _percentage = 0.15678;
    private int _quantity = 42;
    private long _orderId = 987654321012345;

    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 1: Simple (3 parts: string + int + string)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Simple"), Benchmark(Baseline = true)]
    public string Simple_Concat()
    {
        return "User " + _userId + " logged in";
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_Interpolation()
    {
        return $"User {_userId} logged in";
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_StringBuilder()
    {
        var sb = new StringBuilder();
        sb.Append("User ");
        sb.Append(_userId);
        sb.Append(" logged in");
        return sb.ToString();
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_StringCreate()
    {
        // Optimal .NET approach using String.Create
        const int maxLen = 6 + 10 + 10; // "User " + int.MaxValue.ToString().Length + " logged in"
        return string.Create(maxLen, _userId, static (span, userId) =>
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
        return ZString.Concat("User ", _userId, " logged in");
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public string Simple_ZeroAlloc()
    {
        return Z.String("User ", _userId, " logged in").ToString();
    }

    [BenchmarkCategory("Simple"), Benchmark]
    public int Simple_ZeroAlloc_NoAlloc()
    {
        // True zero-allocation: uses stack memory only
        using var temp = Z.String("User ", _userId, " logged in");
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 2: Medium (7 parts: mixed types, no formatting)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Medium"), Benchmark(Baseline = true)]
    public string Medium_Concat()
    {
        return "User " + _userName + " (ID: " + _userId + ") logged in at " + _timestamp + " session: " + _sessionId;
    }

    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_Interpolation()
    {
        return $"User {_userName} (ID: {_userId}) logged in at {_timestamp} session: {_sessionId}";
    }

    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_StringBuilder()
    {
        var sb = new StringBuilder();
        sb.Append("User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(") logged in at ");
        sb.Append(_timestamp);
        sb.Append(" session: ");
        sb.Append(_sessionId);
        return sb.ToString();
    }

    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_StringBuilder_Capacity()
    {
        var sb = new StringBuilder(128);
        sb.Append("User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(") logged in at ");
        sb.Append(_timestamp);
        sb.Append(" session: ");
        sb.Append(_sessionId);
        return sb.ToString();
    }

    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_ZString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append("User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(") logged in at ");
        sb.Append(_timestamp);
        sb.Append(" session: ");
        sb.Append(_sessionId);
        return sb.ToString();
    }

    [BenchmarkCategory("Medium"), Benchmark]
    public string Medium_ZeroAlloc()
    {
        return Z.String(
            "User ", _userName, " (ID: ", _userId, ") logged in at ",
            _timestamp, " session: ", _sessionId).ToString();
    }

    [BenchmarkCategory("Medium"), Benchmark]
    public int Medium_ZeroAlloc_NoAlloc()
    {
        using var temp = Z.String(
            "User ", _userName, " (ID: ", _userId, ") logged in at ",
            _timestamp, " session: ", _sessionId);
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 3: Complex (with custom formatting)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Complex"), Benchmark(Baseline = true)]
    public string Complex_Interpolation()
    {
        return $"User {_userName} (ID: {_userId}) at {_timestamp:yyyy-MM-dd HH:mm:ss} balance ${_balance:N2} session: {_sessionId}";
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public string Complex_StringBuilder()
    {
        var sb = new StringBuilder(128);
        sb.Append("User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(") at ");
        sb.Append(_timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.Append(" balance $");
        sb.Append(_balance.ToString("N2"));
        sb.Append(" session: ");
        sb.Append(_sessionId);
        return sb.ToString();
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public string Complex_ZString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append("User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(") at ");
        sb.Append(_timestamp.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.Append(" balance $");
        sb.Append(_balance.ToString("N2"));
        sb.Append(" session: ");
        sb.Append(_sessionId);
        return sb.ToString();
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public string Complex_ZeroAlloc()
    {
        return Z.String(
            "User ", _userName, " (ID: ", _userId, ") at ",
            new Formatted<DateTime>(_timestamp, "yyyy-MM-dd HH:mm:ss"), " balance $",
            new Formatted<double>(_balance, "N2"), " session: ", _sessionId).ToString();
    }

    [BenchmarkCategory("Complex"), Benchmark]
    public int Complex_ZeroAlloc_NoAlloc()
    {
        using var temp = Z.String(
            "User ", _userName, " (ID: ", _userId, ") at ",
            new Formatted<DateTime>(_timestamp, "yyyy-MM-dd HH:mm:ss"), " balance $",
            new Formatted<double>(_balance, "N2"), " session: ", _sessionId);
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 4: Culture-Specific (German formatting)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Culture"), Benchmark(Baseline = true)]
    public string Culture_StringFormat()
    {
        return string.Format(GermanCulture,
            "Bestellung #{0}: {1} × Artikel zu {2:C2}, Rabatt {3:P2}, Gesamt: {4:N2} €",
            _orderId, _quantity, _price, _percentage, _balance);
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public string Culture_StringBuilder()
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
        sb.Append(", Gesamt: ");
        sb.Append(_balance.ToString("N2", GermanCulture));
        sb.Append(" €");
        return sb.ToString();
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public string Culture_ZString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append("Bestellung #");
        sb.Append(_orderId);
        sb.Append(": ");
        sb.Append(_quantity);
        sb.Append(" × Artikel zu ");
        sb.Append(_price.ToString("C2", GermanCulture));
        sb.Append(", Rabatt ");
        sb.Append(_percentage.ToString("P2", GermanCulture));
        sb.Append(", Gesamt: ");
        sb.Append(_balance.ToString("N2", GermanCulture));
        sb.Append(" €");
        return sb.ToString();
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public string Culture_ZeroAlloc()
    {
        return Z.String(
            "Bestellung #", _orderId, ": ", _quantity, " × Artikel zu ",
            new Formatted<decimal>(_price, "C2", GermanCulture), ", Rabatt ",
            new Formatted<double>(_percentage, "P2", GermanCulture), ", Gesamt: ",
            new Formatted<double>(_balance, "N2", GermanCulture), " €").ToString();
    }

    [BenchmarkCategory("Culture"), Benchmark]
    public int Culture_ZeroAlloc_NoAlloc()
    {
        using var temp = Z.String(
            "Bestellung #", _orderId, ": ", _quantity, " × Artikel zu ",
            new Formatted<decimal>(_price, "C2", GermanCulture), ", Rabatt ",
            new Formatted<double>(_percentage, "P2", GermanCulture), ", Gesamt: ",
            new Formatted<double>(_balance, "N2", GermanCulture), " €");
        return temp.Length;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 5: Stress Test (Maximum complexity)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Stress"), Benchmark(Baseline = true)]
    public string Stress_Interpolation()
    {
        return $"[{_timestamp:O}] User {_userName} (ID: {_userId}, Session: {_sessionId}) " +
               $"Order #{_orderId}: {_quantity}x @ {_price:C2} (DE: {_price.ToString("C2", GermanCulture)}) " +
               $"Discount: {_percentage:P2}, Balance: ${_balance:N2}, Status: Active, Priority: High";
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public string Stress_StringBuilder()
    {
        var sb = new StringBuilder(512);
        sb.Append('[');
        sb.Append(_timestamp.ToString("O"));
        sb.Append("] User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(", Session: ");
        sb.Append(_sessionId);
        sb.Append(") Order #");
        sb.Append(_orderId);
        sb.Append(": ");
        sb.Append(_quantity);
        sb.Append("x @ ");
        sb.Append(_price.ToString("C2"));
        sb.Append(" (DE: ");
        sb.Append(_price.ToString("C2", GermanCulture));
        sb.Append(") Discount: ");
        sb.Append(_percentage.ToString("P2"));
        sb.Append(", Balance: $");
        sb.Append(_balance.ToString("N2"));
        sb.Append(", Status: Active, Priority: High");
        return sb.ToString();
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public string Stress_ZString()
    {
        using var sb = ZString.CreateStringBuilder();
        sb.Append('[');
        sb.Append(_timestamp.ToString("O"));
        sb.Append("] User ");
        sb.Append(_userName);
        sb.Append(" (ID: ");
        sb.Append(_userId);
        sb.Append(", Session: ");
        sb.Append(_sessionId);
        sb.Append(") Order #");
        sb.Append(_orderId);
        sb.Append(": ");
        sb.Append(_quantity);
        sb.Append("x @ ");
        sb.Append(_price.ToString("C2"));
        sb.Append(" (DE: ");
        sb.Append(_price.ToString("C2", GermanCulture));
        sb.Append(") Discount: ");
        sb.Append(_percentage.ToString("P2"));
        sb.Append(", Balance: $");
        sb.Append(_balance.ToString("N2"));
        sb.Append(", Status: Active, Priority: High");
        return sb.ToString();
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public string Stress_ZeroAlloc()
    {
        return Z.String(
            "[", new Formatted<DateTime>(_timestamp, "O"), "] User ", _userName,
            " (ID: ", _userId, ", Session: ", _sessionId, ") Order #", _orderId,
            ": ", _quantity, "x @ ", new Formatted<decimal>(_price, "C2"),
            " (DE: ", new Formatted<decimal>(_price, "C2", GermanCulture), ") Discount: ",
            new Formatted<double>(_percentage, "P2"), ", Balance: $",
            new Formatted<double>(_balance, "N2"),
            ", Status: Active, Priority: High").ToString();
    }

    [BenchmarkCategory("Stress"), Benchmark]
    public int Stress_ZeroAlloc_NoAlloc()
    {
        using var temp = Z.String(
            "[", new Formatted<DateTime>(_timestamp, "O"), "] User ", _userName,
            " (ID: ", _userId, ", Session: ", _sessionId, ") Order #", _orderId,
            ": ", _quantity, "x @ ", new Formatted<decimal>(_price, "C2"),
            " (DE: ", new Formatted<decimal>(_price, "C2", GermanCulture), ") Discount: ",
            new Formatted<double>(_percentage, "P2"), ", Balance: $",
            new Formatted<double>(_balance, "N2"),
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
        _userId.TryFormat(buffer[pos..], out int written);
        pos += written;
        " logged in".CopyTo(buffer[pos..]);
        pos += 10;
        return pos;
    }

    [BenchmarkCategory("TryString"), Benchmark]
    public int TryString_ZeroAlloc()
    {
        Span<char> buffer = stackalloc char[64];
        Z.TryString(buffer, out int written, "User ", _userId, " logged in");
        return written;
    }

    #endregion
}
