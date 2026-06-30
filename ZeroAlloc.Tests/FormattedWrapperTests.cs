// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="Formatted{T}"/> and <see cref="Utf8Formatted{T}"/> wrapper types.
/// Validates format string pass-through, culture handling, and ToString fallback paths.
/// </summary>
public sealed class FormattedWrapperTests
{
    // ========================================================================
    // Formatted<T> - BASIC FORMATTING
    // ========================================================================
    /// <summary>Verifies basic formatting with a format string.</summary>
    [Test]
    public async Task Formatted_BasicWithFormatString()
    {
        Formatted<double> formatted = new(1234.5, "N2", CultureInfo.InvariantCulture);

        string result = formatted.ToString();

        await Assert.That(result).IsEqualTo("1,234.50");
    }

    /// <summary>Verifies formatting with culture-specific provider.</summary>
    [Test]
    public async Task Formatted_WithCulture()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        Formatted<double> formatted = new(1234.5, "N2", german);

        string result = formatted.ToString();

        await Assert.That(result).IsEqualTo("1.234,50");
    }

    /// <summary>Verifies TryFormat uses stored format and provider, ignoring passed ones.</summary>
    [Test]
    public async Task Formatted_TryFormat_UsesStoredFormatAndProvider()
    {
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
        Formatted<int> formatted = new(42, "D5", french);

        bool success;
        int written;
        string writtenContent;
        {
            Span<char> buffer = stackalloc char[64];
            success = formatted.TryFormat(buffer, out written, default, null);
            writtenContent = new string(buffer.Slice(0, written));
        }

        await Assert.That(success).IsTrue();
        await Assert.That(writtenContent).IsEqualTo("00042");
    }

    /// <summary>Verifies formatting when the stackalloc buffer is too small (triggers heap fallback).</summary>
    [Test]
    public async Task Formatted_ToString_FallbackForLargeValue()
    {
        Formatted<LargeSpanFormattedValue> formatted = new(new LargeSpanFormattedValue("fallback-text"));

        string result = formatted.ToString();

        await Assert.That(result).IsEqualTo("fallback-text");
    }

    /// <summary>Verifies ToString fallback when TryFormat fails and ToString returns null.</summary>
    [Test]
    public async Task Formatted_ToString_FallbackWhenToStringReturnsNull_ReturnsEmpty()
    {
        Formatted<NullToStringSpanFormattedValue> formatted = new(new NullToStringSpanFormattedValue());

        string result = formatted.ToString();

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies the Create factory method.</summary>
    [Test]
    public async Task Formatted_Create_FactoryMethod()
    {
        Formatted<int> formatted = Formatted<int>.Create(42, "X8");

        string result = formatted.ToString();

        await Assert.That(result).IsEqualTo("0000002A");
    }

    /// <summary>Verifies ToString(string?, IFormatProvider?) delegates to ToString().</summary>
    [Test]
    public async Task Formatted_ToStringOverload_DelegatesToToString()
    {
        Formatted<double> formatted = new(3.14, "F1", CultureInfo.InvariantCulture);

        string result1 = formatted.ToString();
        string result2 = formatted.ToString(null, null);

        await Assert.That(result2).IsEqualTo(result1);
    }

    /// <summary>Verifies TryFormat falls back to the passed provider when none is stored.</summary>
    [Test]
    public async Task Formatted_TryFormat_NullStoredProvider_UsesPassedProvider()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        Formatted<double> formatted = new(1234.5, "N2", provider: null);

        bool success;
        int written;
        string writtenContent;
        {
            Span<char> buffer = stackalloc char[64];
            success = formatted.TryFormat(buffer, out written, default, german);
            writtenContent = new string(buffer.Slice(0, written));
        }

        await Assert.That(success).IsTrue();
        await Assert.That(writtenContent).IsEqualTo("1.234,50");
    }

    /// <summary>Verifies TryFormat returns false when destination is too small.</summary>
    [Test]
    public async Task Formatted_TryFormat_ReturnsFalseWhenTooSmall()
    {
        Formatted<double> formatted = new(123456.789, "N2", CultureInfo.InvariantCulture);

        bool success;
        int written;
        {
            Span<char> tinyBuffer = stackalloc char[2];
            success = formatted.TryFormat(tinyBuffer, out written, default, null);
        }

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    // ========================================================================
    // Utf8Formatted<T> - BASIC FORMATTING
    // ========================================================================

    /// <summary>Verifies basic UTF-8 formatting with a format string.</summary>
    [Test]
    public async Task Utf8Formatted_BasicWithFormatString()
    {
        Utf8Formatted<double> formatted = new(1234.5, "N2", CultureInfo.InvariantCulture);

        string result = formatted.ToString();

        await Assert.That(result).IsEqualTo("1,234.50");
    }

    /// <summary>Verifies UTF-8 formatting with culture-specific provider.</summary>
    [Test]
    public async Task Utf8Formatted_WithCulture()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        Utf8Formatted<double> formatted = new(1234.5, "N2", german);

        string result = formatted.ToString();

        await Assert.That(result).IsEqualTo("1.234,50");
    }

    /// <summary>Verifies TryFormat uses stored format and provider for UTF-8 output.</summary>
    [Test]
    public async Task Utf8Formatted_TryFormat_UsesStoredFormatAndProvider()
    {
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
        Utf8Formatted<int> formatted = new(42, "D5", french);

        bool success;
        int written;
        string writtenContent;
        {
            Span<byte> buffer = stackalloc byte[64];
            success = formatted.TryFormat(buffer, out written, default, null);
            writtenContent = Encoding.UTF8.GetString(buffer.Slice(0, written));
        }

        await Assert.That(success).IsTrue();
        await Assert.That(writtenContent).IsEqualTo("00042");
    }

    /// <summary>Verifies ToString fallback when stackalloc TryFormat fails for UTF-8 values.</summary>
    [Test]
    public async Task Utf8Formatted_ToString_FallbackForLargeValue()
    {
        Utf8Formatted<LargeUtf8FormattedValue> formatted = new(new LargeUtf8FormattedValue("utf8-fallback"));

        string result = formatted.ToString();

        await Assert.That(result).IsEqualTo("utf8-fallback");
    }

    /// <summary>Verifies UTF-8 ToString fallback when ToString returns null.</summary>
    [Test]
    public async Task Utf8Formatted_ToString_FallbackWhenToStringReturnsNull_ReturnsEmpty()
    {
        Utf8Formatted<NullToStringUtf8FormattedValue> formatted = new(new NullToStringUtf8FormattedValue());

        string result = formatted.ToString();

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    /// <summary>Verifies the Create factory method for UTF-8.</summary>
    [Test]
    public async Task Utf8Formatted_Create_FactoryMethod()
    {
        Utf8Formatted<int> formatted = Utf8Formatted<int>.Create(255, "X4");

        string result = formatted.ToString();

        await Assert.That(result).IsEqualTo("00FF");
    }

    /// <summary>Verifies ToString(string?, IFormatProvider?) delegates to ToString().</summary>
    [Test]
    public async Task Utf8Formatted_ToStringOverload_DelegatesToToString()
    {
        Utf8Formatted<double> formatted = new(3.14, "F1", CultureInfo.InvariantCulture);

        string result1 = formatted.ToString();
        string result2 = formatted.ToString(null, null);

        await Assert.That(result2).IsEqualTo(result1);
    }

    /// <summary>Verifies UTF-8 TryFormat falls back to the passed provider when none is stored.</summary>
    [Test]
    public async Task Utf8Formatted_TryFormat_NullStoredProvider_UsesPassedProvider()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        Utf8Formatted<double> formatted = new(1234.5, "N2", provider: null);

        bool success;
        int written;
        string writtenContent;
        {
            Span<byte> buffer = stackalloc byte[64];
            success = formatted.TryFormat(buffer, out written, default, german);
            writtenContent = Encoding.UTF8.GetString(buffer.Slice(0, written));
        }

        await Assert.That(success).IsTrue();
        await Assert.That(writtenContent).IsEqualTo("1.234,50");
    }

    /// <summary>Verifies TryFormat returns false when destination is too small.</summary>
    [Test]
    public async Task Utf8Formatted_TryFormat_ReturnsFalseWhenTooSmall()
    {
        Utf8Formatted<double> formatted = new(123456.789, "N2", CultureInfo.InvariantCulture);

        bool success;
        int written;
        {
            Span<byte> tinyBuffer = stackalloc byte[2];
            success = formatted.TryFormat(tinyBuffer, out written, default, null);
        }

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    // ========================================================================
    // INTEGRATION WITH ZA API
    // ========================================================================

    /// <summary>Verifies Formatted works inside ZA.String().</summary>
    [Test]
    public async Task Formatted_InsideZaString()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        Formatted<double> formatted = new(1234.5, "N2", german);

        string result;
        {
            using TempString temp = ZA.String(formatted);
            result = temp.ToString();
        }

        await Assert.That(result).IsEqualTo("1.234,50");
    }

    /// <summary>Verifies Utf8Formatted works inside ZA.Utf8().</summary>
    [Test]
    public async Task Utf8Formatted_InsideZaUtf8()
    {
        CultureInfo invariant = CultureInfo.InvariantCulture;
        Utf8Formatted<int> formatted = new(42, "D5", invariant);

        string result;
        {
            using TempBytes temp = ZA.Utf8(formatted);
            result = Encoding.UTF8.GetString(temp.AsSpan());
        }

        await Assert.That(result).IsEqualTo("00042");
    }

    // ========================================================================
    // STRING WRAPPER NULL + EMPTY DESTINATION TryFormat
    // Verifies that Try-pattern contract is respected: null value + empty
    // destination must return false without throwing IndexOutOfRangeException.
    // ========================================================================

    #region StringWrapper NullValue EmptyDestination

    /// <summary>Verifies Utf8Var.TryFormat returns false for null value and empty destination.</summary>
    [Test]
    public async Task Utf8Var_TryFormat_NullValue_EmptyDestination_ReturnsFalse()
    {
        Utf8Var wrapper = new(null);

        bool result = wrapper.TryFormat(Span<byte>.Empty, out int bytesWritten, default, null);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies Utf8Var.TryFormat writes 0x00 for null value when destination has capacity.</summary>
    [Test]
    public async Task Utf8Var_TryFormat_NullValue_OneByteDest_WritesNullTerminator()
    {
        Utf8Var wrapper = new(null);
        bool result;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> destination = stackalloc byte[1];
            result = wrapper.TryFormat(destination, out bytesWritten, default, null);
            byte0 = destination[0];
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0);
    }

    /// <summary>Verifies Utf8Z.TryFormat returns false for null value and empty destination.</summary>
    [Test]
    public async Task Utf8Z_TryFormat_NullValue_EmptyDestination_ReturnsFalse()
    {
        Utf8Z wrapper = new(null);

        bool result = wrapper.TryFormat(Span<byte>.Empty, out int bytesWritten, default, null);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies Utf8Z.TryFormat writes 0x00 for null value when destination has capacity.</summary>
    [Test]
    public async Task Utf8Z_TryFormat_NullValue_OneByteDest_WritesNullTerminator()
    {
        Utf8Z wrapper = new(null);
        bool result;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> destination = stackalloc byte[1];
            result = wrapper.TryFormat(destination, out bytesWritten, default, null);
            byte0 = destination[0];
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0);
    }

    /// <summary>Verifies Utf16BEVar.TryFormat returns false for null value and empty destination.</summary>
    [Test]
    public async Task Utf16BEVar_TryFormat_NullValue_EmptyDestination_ReturnsFalse()
    {
        Utf16BEVar wrapper = new(null);

        bool result = wrapper.TryFormat(Span<byte>.Empty, out int bytesWritten, default, null);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies Utf16BEVar.TryFormat writes 0x00 for null value when destination has capacity.</summary>
    [Test]
    public async Task Utf16BEVar_TryFormat_NullValue_OneByteDest_WritesNullTerminator()
    {
        Utf16BEVar wrapper = new(null);
        bool result;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> destination = stackalloc byte[1];
            result = wrapper.TryFormat(destination, out bytesWritten, default, null);
            byte0 = destination[0];
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0);
    }

    /// <summary>Verifies Utf16LEVar.TryFormat returns false for null value and empty destination.</summary>
    [Test]
    public async Task Utf16LEVar_TryFormat_NullValue_EmptyDestination_ReturnsFalse()
    {
        Utf16LEVar wrapper = new(null);

        bool result = wrapper.TryFormat(Span<byte>.Empty, out int bytesWritten, default, null);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies Utf16LEVar.TryFormat writes 0x00 for null value when destination has capacity.</summary>
    [Test]
    public async Task Utf16LEVar_TryFormat_NullValue_OneByteDest_WritesNullTerminator()
    {
        Utf16LEVar wrapper = new(null);
        bool result;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> destination = stackalloc byte[1];
            result = wrapper.TryFormat(destination, out bytesWritten, default, null);
            byte0 = destination[0];
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0);
    }

    /// <summary>Verifies Utf32BEVar.TryFormat returns false for null value and empty destination.</summary>
    [Test]
    public async Task Utf32BEVar_TryFormat_NullValue_EmptyDestination_ReturnsFalse()
    {
        Utf32BEVar wrapper = new(null);

        bool result = wrapper.TryFormat(Span<byte>.Empty, out int bytesWritten, default, null);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies Utf32BEVar.TryFormat writes 0x00 for null value when destination has capacity.</summary>
    [Test]
    public async Task Utf32BEVar_TryFormat_NullValue_OneByteDest_WritesNullTerminator()
    {
        Utf32BEVar wrapper = new(null);
        bool result;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> destination = stackalloc byte[1];
            result = wrapper.TryFormat(destination, out bytesWritten, default, null);
            byte0 = destination[0];
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0);
    }

    /// <summary>Verifies Utf32LEVar.TryFormat returns false for null value and empty destination.</summary>
    [Test]
    public async Task Utf32LEVar_TryFormat_NullValue_EmptyDestination_ReturnsFalse()
    {
        Utf32LEVar wrapper = new(null);

        bool result = wrapper.TryFormat(Span<byte>.Empty, out int bytesWritten, default, null);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies Utf32LEVar.TryFormat writes 0x00 for null value when destination has capacity.</summary>
    [Test]
    public async Task Utf32LEVar_TryFormat_NullValue_OneByteDest_WritesNullTerminator()
    {
        Utf32LEVar wrapper = new(null);
        bool result;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> destination = stackalloc byte[1];
            result = wrapper.TryFormat(destination, out bytesWritten, default, null);
            byte0 = destination[0];
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0);
    }

    /// <summary>Verifies AsciiVar.TryFormat returns false for null value and empty destination.</summary>
    [Test]
    public async Task AsciiVar_TryFormat_NullValue_EmptyDestination_ReturnsFalse()
    {
        AsciiVar wrapper = new(null);

        bool result = wrapper.TryFormat(Span<byte>.Empty, out int bytesWritten, default, null);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies AsciiVar.TryFormat writes 0x00 for null value when destination has capacity.</summary>
    [Test]
    public async Task AsciiVar_TryFormat_NullValue_OneByteDest_WritesNullTerminator()
    {
        AsciiVar wrapper = new(null);
        bool result;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> destination = stackalloc byte[1];
            result = wrapper.TryFormat(destination, out bytesWritten, default, null);
            byte0 = destination[0];
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0);
    }

    /// <summary>Verifies AsciiZ.TryFormat returns false for null value and empty destination.</summary>
    [Test]
    public async Task AsciiZ_TryFormat_NullValue_EmptyDestination_ReturnsFalse()
    {
        AsciiZ wrapper = new(null);

        bool result = wrapper.TryFormat(Span<byte>.Empty, out int bytesWritten, default, null);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies AsciiZ.TryFormat writes 0x00 for null value when destination has capacity.</summary>
    [Test]
    public async Task AsciiZ_TryFormat_NullValue_OneByteDest_WritesNullTerminator()
    {
        AsciiZ wrapper = new(null);
        bool result;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> destination = stackalloc byte[1];
            result = wrapper.TryFormat(destination, out bytesWritten, default, null);
            byte0 = destination[0];
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0);
    }

    /// <summary>Verifies Latin1Var.TryFormat returns false for null value and empty destination.</summary>
    [Test]
    public async Task Latin1Var_TryFormat_NullValue_EmptyDestination_ReturnsFalse()
    {
        Latin1Var wrapper = new(null);

        bool result = wrapper.TryFormat(Span<byte>.Empty, out int bytesWritten, default, null);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies Latin1Var.TryFormat writes 0x00 for null value when destination has capacity.</summary>
    [Test]
    public async Task Latin1Var_TryFormat_NullValue_OneByteDest_WritesNullTerminator()
    {
        Latin1Var wrapper = new(null);
        bool result;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> destination = stackalloc byte[1];
            result = wrapper.TryFormat(destination, out bytesWritten, default, null);
            byte0 = destination[0];
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0);
    }

    /// <summary>Verifies Latin1Z.TryFormat returns false for null value and empty destination.</summary>
    [Test]
    public async Task Latin1Z_TryFormat_NullValue_EmptyDestination_ReturnsFalse()
    {
        Latin1Z wrapper = new(null);

        bool result = wrapper.TryFormat(Span<byte>.Empty, out int bytesWritten, default, null);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies Latin1Z.TryFormat writes 0x00 for null value when destination has capacity.</summary>
    [Test]
    public async Task Latin1Z_TryFormat_NullValue_OneByteDest_WritesNullTerminator()
    {
        Latin1Z wrapper = new(null);
        bool result;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> destination = stackalloc byte[1];
            result = wrapper.TryFormat(destination, out bytesWritten, default, null);
            byte0 = destination[0];
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0);
    }

    /// <summary>Verifies fixed-length wrappers return false for null value and an empty destination.</summary>
    [Test]
    [Arguments(nameof(Utf8FixBE))]
    [Arguments(nameof(Utf8FixLE))]
    [Arguments(nameof(Utf8Fix16LE))]
    [Arguments(nameof(Utf16BEFixBE))]
    [Arguments(nameof(Utf16BEFixLE))]
    [Arguments(nameof(Utf16LEFixBE))]
    [Arguments(nameof(Utf16LEFixLE))]
    [Arguments(nameof(Utf32BEFixBE))]
    [Arguments(nameof(Utf32BEFixLE))]
    [Arguments(nameof(Utf32LEFixBE))]
    [Arguments(nameof(Utf32LEFixLE))]
    [Arguments(nameof(AsciiFixBE))]
    [Arguments(nameof(AsciiFixLE))]
    [Arguments(nameof(Latin1FixBE))]
    [Arguments(nameof(Latin1FixLE))]
    public async Task FixedLengthWrapper_TryFormat_NullValue_EmptyDestination_ReturnsFalse(string wrapperName)
    {
        bool result = _TryFormatNullFixedLengthWrapper(wrapperName, Span<byte>.Empty, out int bytesWritten);

        await Assert.That(result).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    /// <summary>Verifies fixed-length wrappers write an all-zero length prefix for null values when the destination exactly fits the prefix.</summary>
    [Test]
    [Arguments(nameof(Utf8FixBE), 4)]
    [Arguments(nameof(Utf8FixLE), 4)]
    [Arguments(nameof(Utf8Fix16LE), 2)]
    [Arguments(nameof(Utf16BEFixBE), 4)]
    [Arguments(nameof(Utf16BEFixLE), 4)]
    [Arguments(nameof(Utf16LEFixBE), 4)]
    [Arguments(nameof(Utf16LEFixLE), 4)]
    [Arguments(nameof(Utf32BEFixBE), 4)]
    [Arguments(nameof(Utf32BEFixLE), 4)]
    [Arguments(nameof(Utf32LEFixBE), 4)]
    [Arguments(nameof(Utf32LEFixLE), 4)]
    [Arguments(nameof(AsciiFixBE), 4)]
    [Arguments(nameof(AsciiFixLE), 4)]
    [Arguments(nameof(Latin1FixBE), 4)]
    [Arguments(nameof(Latin1FixLE), 4)]
    public async Task FixedLengthWrapper_TryFormat_NullValue_ExactPrefixDestination_WritesZeroPrefix(string wrapperName, int prefixLength)
    {
        bool result;
        int bytesWritten;
        byte[] snapshot;
        {
            Span<byte> destination = stackalloc byte[prefixLength];
            destination.Fill(0xCC);
            result = _TryFormatNullFixedLengthWrapper(wrapperName, destination, out bytesWritten);
            snapshot = destination.ToArray();
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(prefixLength);
        await Assert.That(snapshot).IsEquivalentTo(new byte[prefixLength]);
    }

    /// <summary>Formats a null fixed-length string wrapper chosen by name.</summary>
    private static bool _TryFormatNullFixedLengthWrapper(string wrapperName, Span<byte> destination, out int bytesWritten)
    {
        switch (wrapperName)
        {
            case nameof(Utf8FixBE):
                return new Utf8FixBE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Utf8FixLE):
                return new Utf8FixLE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Utf8Fix16LE):
                return new Utf8Fix16LE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Utf16BEFixBE):
                return new Utf16BEFixBE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Utf16BEFixLE):
                return new Utf16BEFixLE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Utf16LEFixBE):
                return new Utf16LEFixBE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Utf16LEFixLE):
                return new Utf16LEFixLE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Utf32BEFixBE):
                return new Utf32BEFixBE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Utf32BEFixLE):
                return new Utf32BEFixLE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Utf32LEFixBE):
                return new Utf32LEFixBE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Utf32LEFixLE):
                return new Utf32LEFixLE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(AsciiFixBE):
                return new AsciiFixBE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(AsciiFixLE):
                return new AsciiFixLE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Latin1FixBE):
                return new Latin1FixBE(null).TryFormat(destination, out bytesWritten, default, null);
            case nameof(Latin1FixLE):
                return new Latin1FixLE(null).TryFormat(destination, out bytesWritten, default, null);
            default:
                throw new ArgumentOutOfRangeException(nameof(wrapperName), $"Unknown fixed-length wrapper '{wrapperName}'.");
        }
    }

    #endregion

    private readonly struct LargeSpanFormattedValue(string text) : ISpanFormattable
    {
        private readonly string _Text = text;

        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = 0;
            return false;
        }

        public string ToString(string? format, IFormatProvider? formatProvider) => _Text;

        public override string ToString() => _Text;
    }

    private readonly struct NullToStringSpanFormattedValue : ISpanFormattable
    {
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            charsWritten = 0;
            return false;
        }

        public string ToString(string? format, IFormatProvider? formatProvider) => null!;

        public override string ToString() => null!;
    }

    private readonly struct LargeUtf8FormattedValue(string text) : IUtf8SpanFormattable
    {
        private readonly string _Text = text;

        public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            bytesWritten = 0;
            return false;
        }

        public override string ToString() => _Text;
    }

    private readonly struct NullToStringUtf8FormattedValue : IUtf8SpanFormattable
    {
        public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            bytesWritten = 0;
            return false;
        }

        public override string ToString() => null!;
    }
}