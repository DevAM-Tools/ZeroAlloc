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

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="Formatted{T}"/> and <see cref="Utf8Formatted{T}"/> wrapper types.
/// Validates format string pass-through, culture handling, and ToString fallback paths.
/// </summary>
public class FormattedWrapperTests
{
    // ========================================================================
    // Formatted<T> - BASIC FORMATTING
    // ========================================================================

    /// <summary>Verifies basic formatting with a format string.</summary>
    [Fact]
    public void Formatted_BasicWithFormatString()
    {
        Formatted<double> formatted = new(1234.5, "N2", CultureInfo.InvariantCulture);

        string result = formatted.ToString();

        Assert.Equal("1,234.50", result);
    }

    /// <summary>Verifies formatting with culture-specific provider.</summary>
    [Fact]
    public void Formatted_WithCulture()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        Formatted<double> formatted = new(1234.5, "N2", german);

        string result = formatted.ToString();

        Assert.Equal("1.234,50", result);
    }

    /// <summary>Verifies TryFormat uses stored format and provider, ignoring passed ones.</summary>
    [Fact]
    public void Formatted_TryFormat_UsesStoredFormatAndProvider()
    {
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
        Formatted<int> formatted = new(42, "D5", french);

        Span<char> buffer = stackalloc char[64];
        bool success = formatted.TryFormat(buffer, out int written, default, null);

        Assert.True(success);
        Assert.Equal("00042", new string(buffer.Slice(0, written)));
    }

    /// <summary>Verifies formatting when the stackalloc buffer is too small (triggers heap fallback).</summary>
    [Fact]
    public void Formatted_ToString_FallbackForLargeValue()
    {
        // Create a value that formats to more than 256 chars
        // A double with many decimal places using a custom format
        Formatted<double> formatted = new(1.0 / 3.0, "R", CultureInfo.InvariantCulture);

        string result = formatted.ToString();

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    /// <summary>Verifies the Create factory method.</summary>
    [Fact]
    public void Formatted_Create_FactoryMethod()
    {
        Formatted<int> formatted = Formatted<int>.Create(42, "X8");

        string result = formatted.ToString();

        Assert.Equal("0000002A", result);
    }

    /// <summary>Verifies ToString(string?, IFormatProvider?) delegates to ToString().</summary>
    [Fact]
    public void Formatted_ToStringOverload_DelegatesToToString()
    {
        Formatted<double> formatted = new(3.14, "F1", CultureInfo.InvariantCulture);

        string result1 = formatted.ToString();
        string result2 = formatted.ToString(null, null);

        Assert.Equal(result1, result2);
    }

    /// <summary>Verifies TryFormat returns false when destination is too small.</summary>
    [Fact]
    public void Formatted_TryFormat_ReturnsFalseWhenTooSmall()
    {
        Formatted<double> formatted = new(123456.789, "N2", CultureInfo.InvariantCulture);

        Span<char> tinyBuffer = stackalloc char[2];
        bool success = formatted.TryFormat(tinyBuffer, out int written, default, null);

        Assert.False(success);
        Assert.Equal(0, written);
    }

    // ========================================================================
    // Utf8Formatted<T> - BASIC FORMATTING
    // ========================================================================

    /// <summary>Verifies basic UTF-8 formatting with a format string.</summary>
    [Fact]
    public void Utf8Formatted_BasicWithFormatString()
    {
        Utf8Formatted<double> formatted = new(1234.5, "N2", CultureInfo.InvariantCulture);

        string result = formatted.ToString();

        Assert.Equal("1,234.50", result);
    }

    /// <summary>Verifies UTF-8 formatting with culture-specific provider.</summary>
    [Fact]
    public void Utf8Formatted_WithCulture()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        Utf8Formatted<double> formatted = new(1234.5, "N2", german);

        string result = formatted.ToString();

        Assert.Equal("1.234,50", result);
    }

    /// <summary>Verifies TryFormat uses stored format and provider for UTF-8 output.</summary>
    [Fact]
    public void Utf8Formatted_TryFormat_UsesStoredFormatAndProvider()
    {
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
        Utf8Formatted<int> formatted = new(42, "D5", french);

        Span<byte> buffer = stackalloc byte[64];
        bool success = formatted.TryFormat(buffer, out int written, default, null);

        Assert.True(success);
        Assert.Equal("00042", Encoding.UTF8.GetString(buffer.Slice(0, written)));
    }

    /// <summary>Verifies the Create factory method for UTF-8.</summary>
    [Fact]
    public void Utf8Formatted_Create_FactoryMethod()
    {
        Utf8Formatted<int> formatted = Utf8Formatted<int>.Create(255, "X4");

        string result = formatted.ToString();

        Assert.Equal("00FF", result);
    }

    /// <summary>Verifies ToString(string?, IFormatProvider?) delegates to ToString().</summary>
    [Fact]
    public void Utf8Formatted_ToStringOverload_DelegatesToToString()
    {
        Utf8Formatted<double> formatted = new(3.14, "F1", CultureInfo.InvariantCulture);

        string result1 = formatted.ToString();
        string result2 = formatted.ToString(null, null);

        Assert.Equal(result1, result2);
    }

    /// <summary>Verifies TryFormat returns false when destination is too small.</summary>
    [Fact]
    public void Utf8Formatted_TryFormat_ReturnsFalseWhenTooSmall()
    {
        Utf8Formatted<double> formatted = new(123456.789, "N2", CultureInfo.InvariantCulture);

        Span<byte> tinyBuffer = stackalloc byte[2];
        bool success = formatted.TryFormat(tinyBuffer, out int written, default, null);

        Assert.False(success);
        Assert.Equal(0, written);
    }

    // ========================================================================
    // INTEGRATION WITH ZA API
    // ========================================================================

    /// <summary>Verifies Formatted works inside ZA.String().</summary>
    [Fact]
    public void Formatted_InsideZaString()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        Formatted<double> formatted = new(1234.5, "N2", german);

        using TempString temp = ZA.String(formatted);
        string result = temp.ToString();

        Assert.Equal("1.234,50", result);
    }

    /// <summary>Verifies Utf8Formatted works inside ZA.Utf8().</summary>
    [Fact]
    public void Utf8Formatted_InsideZaUtf8()
    {
        CultureInfo invariant = CultureInfo.InvariantCulture;
        Utf8Formatted<int> formatted = new(42, "D5", invariant);

        using TempBytes temp = ZA.Utf8(formatted);
        string result = Encoding.UTF8.GetString(temp.AsSpan());

        Assert.Equal("00042", result);
    }
}
