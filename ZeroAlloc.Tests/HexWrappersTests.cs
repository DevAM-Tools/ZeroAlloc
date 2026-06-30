// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Exit-point coverage for uppercase fixed-width hex formatting wrappers in <c>HexWrappers.cs</c>.
/// </summary>
public sealed class HexWrappersTests
{
    // ========================================================================
    // FORMAT / TOSTRING
    // ========================================================================

    /// <summary>Verifies each HexN wrapper formats to the expected uppercase fixed-width string.</summary>
    [Test]
    [Arguments(nameof(Hex1), (byte)0x0A, "A")]
    [Arguments(nameof(Hex2), (byte)0x0A, "0A")]
    [Arguments(nameof(Hex3), (ushort)0x0ABC, "ABC")]
    [Arguments(nameof(Hex4), (ushort)0x1234, "1234")]
    [Arguments(nameof(Hex5), 0x12345u, "12345")]
    [Arguments(nameof(Hex6), 0xABCDEFu, "ABCDEF")]
    [Arguments(nameof(Hex7), 0x0ABCDEFu, "0ABCDEF")]
    [Arguments(nameof(Hex8), 0xDEADBEEFu, "DEADBEEF")]
    [Arguments(nameof(Hex16), 0x0123456789ABCDEFUL, "0123456789ABCDEF")]
    public async Task HexWrapper_ToString_ReturnsUpperFixedWidth(string typeName, ulong rawValue, string expected)
    {
        string text = _ToStringHex(typeName, rawValue);

        await Assert.That(text).IsEqualTo(expected);
    }

    /// <summary>Verifies char TryFormat writes the expected uppercase hex into the destination span.</summary>
    [Test]
    [Arguments(nameof(Hex1), (byte)0x0F, 1)]
    [Arguments(nameof(Hex2), (byte)0xFF, 2)]
    [Arguments(nameof(Hex3), (ushort)0xFFF, 3)]
    [Arguments(nameof(Hex4), (ushort)0xFFFF, 4)]
    [Arguments(nameof(Hex5), 0xFFFFFu, 5)]
    [Arguments(nameof(Hex6), 0xFFFFFFu, 6)]
    [Arguments(nameof(Hex7), 0xFFFFFFFu, 7)]
    [Arguments(nameof(Hex8), 0xFFFFFFFFu, 8)]
    [Arguments(nameof(Hex16), 0xFFFFFFFFFFFFFFFFUL, 16)]
    public async Task HexWrapper_TryFormatChar_WritesExpectedHex(string typeName, ulong rawValue, int formattedLength)
    {
        bool success;
        int charsWritten;
        string written;
        {
            Span<char> buffer = stackalloc char[formattedLength];
            success = _TryFormatHexChar(typeName, rawValue, buffer, out charsWritten);
            written = new string(buffer.Slice(0, charsWritten));
        }

        await Assert.That(success).IsTrue();
        await Assert.That(charsWritten).IsEqualTo(formattedLength);
        await Assert.That(written).IsEqualTo(_ToStringHex(typeName, rawValue));
    }

    /// <summary>Verifies UTF-8 TryFormat writes ASCII hex bytes matching the char formatter output.</summary>
    [Test]
    [Arguments(nameof(Hex1), (byte)0x0A)]
    [Arguments(nameof(Hex2), (byte)0x0A)]
    [Arguments(nameof(Hex3), (ushort)0x0ABC)]
    [Arguments(nameof(Hex4), (ushort)0x1234)]
    [Arguments(nameof(Hex5), 0x12345u)]
    [Arguments(nameof(Hex6), 0xABCDEFu)]
    [Arguments(nameof(Hex7), 0x0ABCDEFu)]
    [Arguments(nameof(Hex8), 0xDEADBEEFu)]
    [Arguments(nameof(Hex16), 0x0123456789ABCDEFUL)]
    public async Task HexWrapper_TryFormatUtf8_MatchesCharOutput(string typeName, ulong rawValue)
    {
        int length = _FormattedLength(typeName);
        bool charSuccess;
        bool utf8Success;
        int charWritten;
        int utf8Written;
        string charText;
        string utf8Text;
        {
            Span<char> charBuffer = stackalloc char[length];
            Span<byte> utf8Buffer = stackalloc byte[length];
            charSuccess = _TryFormatHexChar(typeName, rawValue, charBuffer, out charWritten);
            utf8Success = _TryFormatHexUtf8(typeName, rawValue, utf8Buffer, out utf8Written);
            charText = new string(charBuffer.Slice(0, charWritten));
            utf8Text = Encoding.ASCII.GetString(utf8Buffer.Slice(0, utf8Written));
        }

        await Assert.That(charSuccess).IsTrue();
        await Assert.That(utf8Success).IsTrue();
        await Assert.That(utf8Written).IsEqualTo(charWritten);
        await Assert.That(utf8Text).IsEqualTo(charText);
    }

    // ========================================================================
    // MASKING / BOUNDARY VALUES
    // ========================================================================

    /// <summary>Verifies nibble wrappers mask input to the lower 4 bits.</summary>
    [Test]
    public async Task Hex1_Constructor_MasksToLowerNibble()
    {
        Hex1 wrapper = 0x1A;

        await Assert.That(wrapper.ToString()).IsEqualTo("A");
    }

    /// <summary>Verifies Hex3 masks input to 12 bits.</summary>
    [Test]
    public async Task Hex3_Constructor_MasksToTwelveBits()
    {
        Hex3 wrapper = 0xFABC;

        await Assert.That(wrapper.ToString()).IsEqualTo("ABC");
    }

    /// <summary>Verifies Hex5 masks input to 20 bits.</summary>
    [Test]
    public async Task Hex5_Constructor_MasksToTwentyBits()
    {
        Hex5 wrapper = 0x10ABCDE;

        await Assert.That(wrapper.ToString()).IsEqualTo("ABCDE");
    }

    /// <summary>Verifies Hex6 masks input to 24 bits.</summary>
    [Test]
    public async Task Hex6_Constructor_MasksToTwentyFourBits()
    {
        Hex6 wrapper = 0x1FFFFFF;

        await Assert.That(wrapper.ToString()).IsEqualTo("FFFFFF");
    }

    /// <summary>Verifies Hex7 masks input to 28 bits.</summary>
    [Test]
    public async Task Hex7_Constructor_MasksToTwentyEightBits()
    {
        Hex7 wrapper = 0x1FFFFFFF;

        await Assert.That(wrapper.ToString()).IsEqualTo("FFFFFFF");
    }

    // ========================================================================
    // FAILURE PATHS
    // ========================================================================

    /// <summary>Verifies char TryFormat returns false when the destination is too small.</summary>
    [Test]
    [Arguments(nameof(Hex1))]
    [Arguments(nameof(Hex2))]
    [Arguments(nameof(Hex3))]
    [Arguments(nameof(Hex4))]
    [Arguments(nameof(Hex5))]
    [Arguments(nameof(Hex6))]
    [Arguments(nameof(Hex7))]
    [Arguments(nameof(Hex8))]
    [Arguments(nameof(Hex16))]
    public async Task HexWrapper_TryFormatChar_UndersizedBuffer_ReturnsFalse(string typeName)
    {
        bool success = _TryFormatHexChar(typeName, 0, Span<char>.Empty, out int charsWritten);

        await Assert.That(success).IsFalse();
        await Assert.That(charsWritten).IsEqualTo(0);
    }

    /// <summary>Verifies UTF-8 TryFormat returns false when the destination is too small.</summary>
    [Test]
    [Arguments(nameof(Hex1))]
    [Arguments(nameof(Hex2))]
    [Arguments(nameof(Hex3))]
    [Arguments(nameof(Hex4))]
    [Arguments(nameof(Hex5))]
    [Arguments(nameof(Hex6))]
    [Arguments(nameof(Hex7))]
    [Arguments(nameof(Hex8))]
    [Arguments(nameof(Hex16))]
    public async Task HexWrapper_TryFormatUtf8_UndersizedBuffer_ReturnsFalse(string typeName)
    {
        bool success = _TryFormatHexUtf8(typeName, 0, Span<byte>.Empty, out int bytesWritten);

        await Assert.That(success).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    // ========================================================================
    // IMPLICIT CONVERSION + IFORMATTABLE OVERLOAD
    // ========================================================================

    /// <summary>Verifies implicit primitive conversion matches explicit constructor formatting.</summary>
    [Test]
    [Arguments(nameof(Hex2), (byte)0x5A)]
    [Arguments(nameof(Hex4), (ushort)0x5A5A)]
    [Arguments(nameof(Hex8), 0x5A5A5A5Au)]
    [Arguments(nameof(Hex16), 0x5A5A5A5A5A5A5A5AUL)]
    public async Task HexWrapper_ImplicitConversion_MatchesExplicitConstructor(string typeName, ulong rawValue)
    {
        string implicitText = _ToStringHexImplicit(typeName, rawValue);
        string explicitText = _ToStringHex(typeName, rawValue);

        await Assert.That(implicitText).IsEqualTo(explicitText);
    }

    /// <summary>Verifies ToString(string?, IFormatProvider?) delegates to parameterless ToString.</summary>
    [Test]
    [Arguments(nameof(Hex1), (byte)0x0A)]
    [Arguments(nameof(Hex2), (byte)0xAB)]
    [Arguments(nameof(Hex3), (ushort)0x0ABC)]
    [Arguments(nameof(Hex4), (ushort)0x1234)]
    [Arguments(nameof(Hex5), 0x12345u)]
    [Arguments(nameof(Hex6), 0xABCDEFu)]
    [Arguments(nameof(Hex7), 0x0ABCDEFu)]
    [Arguments(nameof(Hex8), 0x12345678u)]
    [Arguments(nameof(Hex16), 0x0123456789ABCDEFUL)]
    public async Task HexWrapper_ToStringOverload_DelegatesToParameterless(string typeName, ulong rawValue)
    {
        string plain = _ToStringHex(typeName, rawValue);
        string overload = _ToStringHexWithFormat(typeName, rawValue);

        await Assert.That(overload).IsEqualTo(plain);
    }

    // ========================================================================
    // HELPERS
    // ========================================================================

    private static int _FormattedLength(string typeName) => typeName switch
    {
        nameof(Hex1) => Hex1.FormattedLength,
        nameof(Hex2) => Hex2.FormattedLength,
        nameof(Hex3) => Hex3.FormattedLength,
        nameof(Hex4) => Hex4.FormattedLength,
        nameof(Hex5) => Hex5.FormattedLength,
        nameof(Hex6) => Hex6.FormattedLength,
        nameof(Hex7) => Hex7.FormattedLength,
        nameof(Hex8) => Hex8.FormattedLength,
        nameof(Hex16) => Hex16.FormattedLength,
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown hex wrapper type."),
    };

    private static string _ToStringHex(string typeName, ulong rawValue) => typeName switch
    {
        nameof(Hex1) => new Hex1((byte)rawValue).ToString(),
        nameof(Hex2) => new Hex2((byte)rawValue).ToString(),
        nameof(Hex3) => new Hex3((ushort)rawValue).ToString(),
        nameof(Hex4) => new Hex4((ushort)rawValue).ToString(),
        nameof(Hex5) => new Hex5((uint)rawValue).ToString(),
        nameof(Hex6) => new Hex6((uint)rawValue).ToString(),
        nameof(Hex7) => new Hex7((uint)rawValue).ToString(),
        nameof(Hex8) => new Hex8((uint)rawValue).ToString(),
        nameof(Hex16) => new Hex16(rawValue).ToString(),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown hex wrapper type."),
    };

    private static string _ToStringHexImplicit(string typeName, ulong rawValue) => typeName switch
    {
        nameof(Hex2) => ((Hex2)(byte)rawValue).ToString(),
        nameof(Hex4) => ((Hex4)(ushort)rawValue).ToString(),
        nameof(Hex8) => ((Hex8)(uint)rawValue).ToString(),
        nameof(Hex16) => ((Hex16)rawValue).ToString(),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown hex wrapper type."),
    };

    private static string _ToStringHexWithFormat(string typeName, ulong rawValue) => typeName switch
    {
        nameof(Hex1) => new Hex1((byte)rawValue).ToString("X", CultureInfo.InvariantCulture),
        nameof(Hex2) => new Hex2((byte)rawValue).ToString("X", CultureInfo.InvariantCulture),
        nameof(Hex3) => new Hex3((ushort)rawValue).ToString(null, CultureInfo.InvariantCulture),
        nameof(Hex4) => new Hex4((ushort)rawValue).ToString("G", CultureInfo.InvariantCulture),
        nameof(Hex5) => new Hex5((uint)rawValue).ToString(null, null),
        nameof(Hex6) => new Hex6((uint)rawValue).ToString("X", CultureInfo.InvariantCulture),
        nameof(Hex7) => new Hex7((uint)rawValue).ToString(null, CultureInfo.InvariantCulture),
        nameof(Hex8) => new Hex8((uint)rawValue).ToString(null, CultureInfo.InvariantCulture),
        nameof(Hex16) => new Hex16(rawValue).ToString("X", CultureInfo.InvariantCulture),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown hex wrapper type."),
    };

    private static bool _TryFormatHexChar(string typeName, ulong rawValue, Span<char> destination, out int charsWritten) => typeName switch
    {
        nameof(Hex1) => new Hex1((byte)rawValue).TryFormat(destination, out charsWritten, default, null),
        nameof(Hex2) => new Hex2((byte)rawValue).TryFormat(destination, out charsWritten, default, null),
        nameof(Hex3) => new Hex3((ushort)rawValue).TryFormat(destination, out charsWritten, default, null),
        nameof(Hex4) => new Hex4((ushort)rawValue).TryFormat(destination, out charsWritten, default, null),
        nameof(Hex5) => new Hex5((uint)rawValue).TryFormat(destination, out charsWritten, default, null),
        nameof(Hex6) => new Hex6((uint)rawValue).TryFormat(destination, out charsWritten, default, null),
        nameof(Hex7) => new Hex7((uint)rawValue).TryFormat(destination, out charsWritten, default, null),
        nameof(Hex8) => new Hex8((uint)rawValue).TryFormat(destination, out charsWritten, default, null),
        nameof(Hex16) => new Hex16(rawValue).TryFormat(destination, out charsWritten, default, null),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown hex wrapper type."),
    };

    private static bool _TryFormatHexUtf8(string typeName, ulong rawValue, Span<byte> destination, out int bytesWritten) => typeName switch
    {
        nameof(Hex1) => new Hex1((byte)rawValue).TryFormat(destination, out bytesWritten, default, null),
        nameof(Hex2) => new Hex2((byte)rawValue).TryFormat(destination, out bytesWritten, default, null),
        nameof(Hex3) => new Hex3((ushort)rawValue).TryFormat(destination, out bytesWritten, default, null),
        nameof(Hex4) => new Hex4((ushort)rawValue).TryFormat(destination, out bytesWritten, default, null),
        nameof(Hex5) => new Hex5((uint)rawValue).TryFormat(destination, out bytesWritten, default, null),
        nameof(Hex6) => new Hex6((uint)rawValue).TryFormat(destination, out bytesWritten, default, null),
        nameof(Hex7) => new Hex7((uint)rawValue).TryFormat(destination, out bytesWritten, default, null),
        nameof(Hex8) => new Hex8((uint)rawValue).TryFormat(destination, out bytesWritten, default, null),
        nameof(Hex16) => new Hex16(rawValue).TryFormat(destination, out bytesWritten, default, null),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown hex wrapper type."),
    };
}
