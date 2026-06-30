// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Exit-point coverage for UTF-8/16/32, ASCII, and Latin1 string encoding wrappers in <c>StringWrappers.cs</c>.
/// </summary>
public sealed class StringWrappersTests
{
    private const string _SampleAscii = "Hello";
    private const string _SampleUtf8 = "café";
    private const string _SampleLatin1 = "naïve";
    private const string _SampleNonAscii = "über";

    // ========================================================================
    // UTF-8 WRAPPERS
    // ========================================================================

    /// <summary>Verifies plain Utf8 encodes UTF-8 bytes without a length prefix.</summary>
    [Test]
    public async Task Utf8_TryFormat_WritesUtf8BytesWithoutPrefix()
    {
        Utf8 wrapper = _SampleUtf8;
        byte[] expected = Encoding.UTF8.GetBytes(_SampleUtf8);

        bool success;
        int written;
        byte[] snapshot;
        {
            Span<byte> destination = stackalloc byte[expected.Length];
            success = wrapper.TryFormat(destination, out written, default, null);
            snapshot = destination.ToArray();
        }

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(expected.Length);
        await Assert.That(snapshot).IsEquivalentTo(expected);
    }

    /// <summary>Verifies Utf8 returns empty output for null without requiring buffer space.</summary>
    [Test]
    public async Task Utf8_TryFormat_NullValue_WritesZeroBytes()
    {
        Utf8 wrapper = new Utf8(null);

        bool success = wrapper.TryFormat(Span<byte>.Empty, out int written, default, null);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies parsable UTF-8 wrappers round-trip through TryFormat and TryParse.</summary>
    [Test]
    [Arguments(nameof(Utf8Var))]
    [Arguments(nameof(Utf8FixBE))]
    [Arguments(nameof(Utf8FixLE))]
    [Arguments(nameof(Utf8Fix16LE))]
    [Arguments(nameof(Utf8Z))]
    public async Task Utf8ParsableWrapper_FormatParse_RoundTrip_PreservesValue(string wrapperName)
    {
        byte[] buffer = new byte[64];
        int written = _FormatStringWrapper(wrapperName, _SampleUtf8, buffer);
        bool parsed = _TryParseStringWrapper(wrapperName, buffer.AsSpan(0, written), out string? text, out int consumed);

        await Assert.That(parsed).IsTrue();
        await Assert.That(consumed).IsEqualTo(written);
        await Assert.That(text).IsEqualTo(_SampleUtf8);
    }

    /// <summary>Verifies UTF-8 fixed-length wrappers fail TryParse when payload bytes are missing.</summary>
    [Test]
    [Arguments(nameof(Utf8FixBE), new byte[] { 0x00, 0x00, 0x00, 0x03, 0x61 })]
    [Arguments(nameof(Utf8FixLE), new byte[] { 0x03, 0x00, 0x00, 0x00, 0x61 })]
    [Arguments(nameof(Utf8Fix16LE), new byte[] { 0x03, 0x00, 0x61 })]
    [Arguments(nameof(Utf8Var), new byte[] { 0x03, 0x61 })]
    public async Task Utf8ParsableWrapper_TryParse_InsufficientPayload_ReturnsFalse(string wrapperName, byte[] source)
    {
        bool parsed = _TryParseStringWrapper(wrapperName, source, out _, out int consumed);

        await Assert.That(parsed).IsFalse();
        await Assert.That(consumed).IsEqualTo(0);
    }

    /// <summary>Verifies Utf8Z.TryParse fails when no null terminator is present.</summary>
    [Test]
    public async Task Utf8Z_TryParse_MissingNullTerminator_ReturnsFalse()
    {
        bool parsed = Utf8Z.TryParse("abc"u8, out Utf8Z value, out int consumed);

        await Assert.That(parsed).IsFalse();
        await Assert.That(consumed).IsEqualTo(0);
        await Assert.That(value.Value).IsEqualTo("");
    }

    // ========================================================================
    // UTF-16 WRAPPERS
    // ========================================================================

    /// <summary>Verifies UTF-16 BE/LE plain wrappers encode two bytes per char.</summary>
    [Test]
    [Arguments(nameof(Utf16BE), true)]
    [Arguments(nameof(Utf16LE), false)]
    public async Task Utf16PlainWrapper_TryFormat_WritesCodeUnits(string wrapperName, bool bigEndian)
    {
        Encoding encoding = bigEndian ? Encoding.BigEndianUnicode : Encoding.Unicode;
        byte[] expected = encoding.GetBytes(_SampleAscii);
        byte[] snapshot = new byte[expected.Length];

        bool success = _TryFormatStringWrapper(wrapperName, _SampleAscii, snapshot, out int written);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(expected.Length);
        await Assert.That(snapshot).IsEquivalentTo(expected);
    }

    /// <summary>Verifies UTF-16 prefixed wrappers include a length prefix before payload bytes.</summary>
    [Test]
    [Arguments(nameof(Utf16BEVar))]
    [Arguments(nameof(Utf16BEFixBE))]
    [Arguments(nameof(Utf16BEFixLE))]
    [Arguments(nameof(Utf16LEVar))]
    [Arguments(nameof(Utf16LEFixBE))]
    [Arguments(nameof(Utf16LEFixLE))]
    public async Task Utf16PrefixedWrapper_TryFormat_WritesPrefixAndPayload(string wrapperName)
    {
        byte[] buffer = new byte[64];
        int written = _FormatStringWrapper(wrapperName, _SampleAscii, buffer);

        await Assert.That(written).IsGreaterThan(_SampleAscii.Length);
        await Assert.That(written).IsGreaterThan(2);
    }

    // ========================================================================
    // UTF-32 WRAPPERS
    // ========================================================================

    /// <summary>Verifies UTF-32 plain wrappers encode using the configured endianness.</summary>
    [Test]
    [Arguments(nameof(Utf32BE))]
    [Arguments(nameof(Utf32LE))]
    public async Task Utf32PlainWrapper_TryFormat_WritesEncodedBytes(string wrapperName)
    {
        byte[] buffer = new byte[64];
        int written = _FormatStringWrapper(wrapperName, "A", buffer);

        await Assert.That(written).IsGreaterThan(0);
    }

    /// <summary>Verifies UTF-32 prefixed wrappers emit a length prefix for non-null strings.</summary>
    [Test]
    [Arguments(nameof(Utf32BEVar))]
    [Arguments(nameof(Utf32BEFixBE))]
    [Arguments(nameof(Utf32BEFixLE))]
    [Arguments(nameof(Utf32LEVar))]
    [Arguments(nameof(Utf32LEFixBE))]
    [Arguments(nameof(Utf32LEFixLE))]
    public async Task Utf32PrefixedWrapper_TryFormat_IncludesLengthPrefix(string wrapperName)
    {
        byte[] buffer = new byte[128];
        int written = _FormatStringWrapper(wrapperName, _SampleAscii, buffer);

        await Assert.That(written).IsGreaterThan(4);
    }

    // ========================================================================
    // ASCII WRAPPERS
    // ========================================================================

    /// <summary>Verifies Ascii replaces non-ASCII code points with question marks.</summary>
    [Test]
    public async Task Ascii_TryFormat_ReplacesNonAsciiWithQuestionMark()
    {
        Ascii wrapper = _SampleNonAscii;
        byte[] snapshot = new byte[_SampleNonAscii.Length];

        bool success = wrapper.TryFormat(snapshot, out int written, default, null);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(_SampleNonAscii.Length);
        await Assert.That(snapshot[0]).IsEqualTo((byte)'?');
        await Assert.That(snapshot[1]).IsEqualTo((byte)'b');
        await Assert.That(snapshot[2]).IsEqualTo((byte)'e');
        await Assert.That(snapshot[3]).IsEqualTo((byte)'r');
    }

    /// <summary>Verifies Ascii prefixed wrappers preserve replacement semantics in the payload.</summary>
    [Test]
    [Arguments(nameof(AsciiVar))]
    [Arguments(nameof(AsciiFixBE))]
    [Arguments(nameof(AsciiFixLE))]
    [Arguments(nameof(AsciiZ))]
    public async Task AsciiPrefixedWrapper_TryFormat_PreservesAsciiReplacement(string wrapperName)
    {
        byte[] buffer = new byte[64];
        int written = _FormatStringWrapper(wrapperName, _SampleNonAscii, buffer);
        int payloadOffset = _PayloadOffset(wrapperName, written, _SampleNonAscii.Length);
        byte firstPayloadByte = buffer[payloadOffset];

        await Assert.That(firstPayloadByte).IsEqualTo((byte)'?');
    }

    // ========================================================================
    // LATIN1 WRAPPERS
    // ========================================================================

    /// <summary>Verifies Latin1 encodes ISO-8859-1 bytes for extended characters.</summary>
    [Test]
    public async Task Latin1_TryFormat_WritesLatin1Bytes()
    {
        Latin1 wrapper = _SampleLatin1;
        byte[] expected = Encoding.Latin1.GetBytes(_SampleLatin1);
        byte[] snapshot = new byte[expected.Length];

        bool success = wrapper.TryFormat(snapshot, out int written, default, null);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(expected.Length);
        await Assert.That(snapshot).IsEquivalentTo(expected);
    }

    /// <summary>Verifies Latin1 prefixed wrappers include payload bytes after the prefix.</summary>
    [Test]
    [Arguments(nameof(Latin1Var))]
    [Arguments(nameof(Latin1FixBE))]
    [Arguments(nameof(Latin1FixLE))]
    [Arguments(nameof(Latin1Z))]
    public async Task Latin1PrefixedWrapper_TryFormat_WritesPrefixAndPayload(string wrapperName)
    {
        byte[] buffer = new byte[64];
        int written = _FormatStringWrapper(wrapperName, _SampleLatin1, buffer);

        await Assert.That(written).IsGreaterThan(_SampleLatin1.Length);
    }

    /// <summary>Verifies Utf8Var.TryParse fails when the VarInt length prefix cannot be parsed.</summary>
    [Test]
    public async Task Utf8Var_TryParse_InvalidVarIntPrefix_ReturnsFalse()
    {
        bool parsed = Utf8Var.TryParse([0x80], out Utf8Var value, out int consumed);

        await Assert.That(parsed).IsFalse();
        await Assert.That(consumed).IsEqualTo(0);
        await Assert.That(value.Value).IsEqualTo("");
    }

    /// <summary>Verifies fixed-length UTF-8 wrappers fail TryParse when the header is incomplete.</summary>
    [Test]
    [Arguments(nameof(Utf8FixBE), new byte[] { 0x00, 0x00, 0x01 })]
    [Arguments(nameof(Utf8FixLE), new byte[] { 0x01, 0x00 })]
    [Arguments(nameof(Utf8Fix16LE), new byte[] { 0x01 })]
    public async Task Utf8FixedWrapper_TryParse_IncompleteHeader_ReturnsFalse(string wrapperName, byte[] source)
    {
        bool parsed = _TryParseStringWrapper(wrapperName, source, out _, out int consumed);

        await Assert.That(parsed).IsFalse();
        await Assert.That(consumed).IsEqualTo(0);
    }

    /// <summary>Verifies null-backed fixed-prefix UTF-8 wrappers fail when the destination cannot hold the prefix.</summary>
    [Test]
    [Arguments(nameof(Utf8FixLE), 3)]
    [Arguments(nameof(Utf8FixBE), 3)]
    [Arguments(nameof(Utf8Fix16LE), 1)]
    public async Task Utf8FixedWrapper_TryFormat_NullValue_UndersizedPrefixBuffer_ReturnsFalse(string wrapperName, int bufferSize)
    {
        bool success = _TryFormatStringWrapper(wrapperName, null, stackalloc byte[bufferSize], out int written);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies reverse implicit conversion from parsable UTF-8 wrappers returns Value.</summary>
    [Test]
    [Arguments(nameof(Utf8FixBE))]
    [Arguments(nameof(Utf8FixLE))]
    [Arguments(nameof(Utf8Fix16LE))]
    [Arguments(nameof(Utf8Z))]
    public async Task Utf8ParsableWrapper_ImplicitConversion_ToString_ReturnsValue(string wrapperName)
    {
        string text = _ReverseImplicitToString(wrapperName, _SampleAscii);

        await Assert.That(text).IsEqualTo(_SampleAscii);
    }

    /// <summary>Verifies implicit string conversion constructs an equivalent UTF-8 fixed wrapper.</summary>
    [Test]
    [Arguments(nameof(Utf8FixBE))]
    [Arguments(nameof(Utf8FixLE))]
    [Arguments(nameof(Utf8Fix16LE))]
    [Arguments(nameof(Utf8Z))]
    public async Task Utf8FixedWrapper_ImplicitConversion_FromString_MatchesExplicitConstructor(string wrapperName)
    {
        string implicitText = _ToStringImplicit(wrapperName, _SampleAscii);
        string explicitText = _ToStringStringWrapper(wrapperName, _SampleAscii);

        await Assert.That(implicitText).IsEqualTo(explicitText);
    }

    /// <summary>Verifies UTF-8 fixed wrappers expose ToString overloads and default ToString for stored values.</summary>
    [Test]
    [Arguments(nameof(Utf8FixLE))]
    [Arguments(nameof(Utf8Fix16LE))]
    [Arguments(nameof(Utf8Z))]
    public async Task Utf8FixedWrapper_ToStringAndOverload_ReturnStoredValue(string wrapperName)
    {
        string plain = _ToStringStringWrapper(wrapperName, _SampleAscii);
        string overload = _ToStringStringWrapperWithFormat(wrapperName, _SampleAscii);

        await Assert.That(plain).IsEqualTo(_SampleAscii);
        await Assert.That(overload).IsEqualTo(_SampleAscii);
    }

    /// <summary>Verifies Utf8Var.ToString overload delegates to parameterless ToString.</summary>
    [Test]
    public async Task Utf8Var_ToStringOverload_DelegatesToParameterless()
    {
        Utf8Var wrapper = _SampleAscii;

        string plain = wrapper.ToString();
        string overload = wrapper.ToString(null, CultureInfo.InvariantCulture);

        await Assert.That(overload).IsEqualTo(plain);
    }

    /// <summary>Verifies Utf8FixBE implicit conversion from string constructs an equivalent wrapper.</summary>
    [Test]
    public async Task Utf8FixBE_ImplicitConversion_FromString_MatchesExplicitConstructor()
    {
        Utf8FixBE implicitWrapper = _SampleAscii;
        Utf8FixBE explicitWrapper = new(_SampleAscii);

        await Assert.That(implicitWrapper.ToString()).IsEqualTo(explicitWrapper.ToString());
    }

    /// <summary>Verifies null-backed Utf16BE returns success with zero bytes written.</summary>
    [Test]
    public async Task Utf16BE_TryFormat_NullValue_WritesZeroBytes()
    {
        Utf16BE wrapper = new Utf16BE(null);

        bool success = wrapper.TryFormat(Span<byte>.Empty, out int written, default, null);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies null-backed Utf16LE returns success with zero bytes written.</summary>
    [Test]
    public async Task Utf16LE_TryFormat_NullValue_WritesZeroBytes()
    {
        Utf16LE wrapper = new Utf16LE(null);

        bool success = wrapper.TryFormat(Span<byte>.Empty, out int written, default, null);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies null-backed Utf16LE fails when destination cannot hold encoded output.</summary>
    [Test]
    public async Task Utf16LE_TryFormat_NonNullValue_UndersizedBuffer_ReturnsFalse()
    {
        Utf16LE wrapper = _SampleAscii;

        bool success = wrapper.TryFormat(stackalloc byte[1], out int written, default, null);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies UTF-16 prefixed wrappers cover ToString, overload, and implicit conversion.</summary>
    [Test]
    [Arguments(nameof(Utf16BEVar))]
    [Arguments(nameof(Utf16BEFixBE))]
    [Arguments(nameof(Utf16BEFixLE))]
    [Arguments(nameof(Utf16LEVar))]
    [Arguments(nameof(Utf16LEFixBE))]
    [Arguments(nameof(Utf16LEFixLE))]
    public async Task Utf16PrefixedWrapper_SurfaceArea_IsCovered(string wrapperName)
    {
        string implicitText = _ToStringImplicit(wrapperName, _SampleAscii);
        string plain = _ToStringStringWrapper(wrapperName, _SampleAscii);
        string overload = _ToStringStringWrapperWithFormat(wrapperName, _SampleAscii);

        await Assert.That(implicitText).IsEqualTo(_SampleAscii);
        await Assert.That(plain).IsEqualTo(_SampleAscii);
        await Assert.That(overload).IsEqualTo(_SampleAscii);
    }

    /// <summary>Verifies null-backed UTF-16 fixed-prefix wrappers fail when prefix cannot be written.</summary>
    [Test]
    [Arguments(nameof(Utf16BEFixBE), 3)]
    [Arguments(nameof(Utf16BEFixLE), 3)]
    [Arguments(nameof(Utf16LEFixBE), 3)]
    [Arguments(nameof(Utf16LEFixLE), 3)]
    public async Task Utf16FixedWrapper_TryFormat_NullValue_UndersizedPrefixBuffer_ReturnsFalse(string wrapperName, int bufferSize)
    {
        bool success = _TryFormatStringWrapper(wrapperName, null, stackalloc byte[bufferSize], out int written);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies Utf16LE implicit conversion from string constructs an equivalent wrapper.</summary>
    [Test]
    public async Task Utf16LE_ImplicitConversion_FromString_MatchesExplicitConstructor()
    {
        Utf16LE implicitWrapper = _SampleAscii;
        Utf16LE explicitWrapper = new(_SampleAscii);

        await Assert.That(implicitWrapper.ToString()).IsEqualTo(explicitWrapper.ToString());
    }

    /// <summary>Verifies null-backed Utf32 plain wrappers return success with zero bytes written.</summary>
    [Test]
    [Arguments(nameof(Utf32BE))]
    [Arguments(nameof(Utf32LE))]
    public async Task Utf32PlainWrapper_TryFormat_NullValue_WritesZeroBytes(string wrapperName)
    {
        bool success = _TryFormatStringWrapper(wrapperName, null, Span<byte>.Empty, out int written);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies non-null Utf32 plain wrappers fail when destination is too small.</summary>
    [Test]
    [Arguments(nameof(Utf32BE))]
    [Arguments(nameof(Utf32LE))]
    public async Task Utf32PlainWrapper_TryFormat_NonNullValue_UndersizedBuffer_ReturnsFalse(string wrapperName)
    {
        bool success = _TryFormatStringWrapper(wrapperName, _SampleAscii, stackalloc byte[1], out int written);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies UTF-32 prefixed wrappers cover ToString, overload, and implicit conversion.</summary>
    [Test]
    [Arguments(nameof(Utf32BEVar))]
    [Arguments(nameof(Utf32BEFixBE))]
    [Arguments(nameof(Utf32BEFixLE))]
    [Arguments(nameof(Utf32LEVar))]
    [Arguments(nameof(Utf32LEFixBE))]
    [Arguments(nameof(Utf32LEFixLE))]
    public async Task Utf32PrefixedWrapper_SurfaceArea_IsCovered(string wrapperName)
    {
        string implicitText = _ToStringImplicit(wrapperName, _SampleAscii);
        string plain = _ToStringStringWrapper(wrapperName, _SampleAscii);
        string overload = _ToStringStringWrapperWithFormat(wrapperName, _SampleAscii);

        await Assert.That(implicitText).IsEqualTo(_SampleAscii);
        await Assert.That(plain).IsEqualTo(_SampleAscii);
        await Assert.That(overload).IsEqualTo(_SampleAscii);
    }

    /// <summary>Verifies null-backed UTF-32 fixed-prefix wrappers fail when prefix cannot be written.</summary>
    [Test]
    [Arguments(nameof(Utf32BEFixBE), 3)]
    [Arguments(nameof(Utf32BEFixLE), 3)]
    [Arguments(nameof(Utf32LEFixBE), 3)]
    [Arguments(nameof(Utf32LEFixLE), 3)]
    public async Task Utf32FixedWrapper_TryFormat_NullValue_UndersizedPrefixBuffer_ReturnsFalse(string wrapperName, int bufferSize)
    {
        bool success = _TryFormatStringWrapper(wrapperName, null, stackalloc byte[bufferSize], out int written);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies Utf32BE implicit conversion from string constructs an equivalent wrapper.</summary>
    [Test]
    public async Task Utf32BE_ImplicitConversion_FromString_MatchesExplicitConstructor()
    {
        Utf32BE implicitWrapper = _SampleAscii;
        Utf32BE explicitWrapper = new(_SampleAscii);

        await Assert.That(implicitWrapper.ToString()).IsEqualTo(explicitWrapper.ToString());
    }

    /// <summary>Verifies null-backed Ascii returns success with zero bytes written.</summary>
    [Test]
    public async Task Ascii_TryFormat_NullValue_WritesZeroBytes()
    {
        Ascii wrapper = new Ascii(null);

        bool success = wrapper.TryFormat(Span<byte>.Empty, out int written, default, null);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies ASCII prefixed wrappers cover ToString, overload, and implicit conversion.</summary>
    [Test]
    [Arguments(nameof(AsciiVar))]
    [Arguments(nameof(AsciiFixBE))]
    [Arguments(nameof(AsciiFixLE))]
    [Arguments(nameof(AsciiZ))]
    public async Task AsciiPrefixedWrapper_SurfaceArea_IsCovered(string wrapperName)
    {
        string implicitText = _ToStringImplicit(wrapperName, _SampleAscii);
        string plain = _ToStringStringWrapper(wrapperName, _SampleAscii);
        string overload = _ToStringStringWrapperWithFormat(wrapperName, _SampleAscii);

        await Assert.That(implicitText).IsEqualTo(_SampleAscii);
        await Assert.That(plain).IsEqualTo(_SampleAscii);
        await Assert.That(overload).IsEqualTo(_SampleAscii);
    }

    /// <summary>Verifies null-backed ASCII fixed-prefix wrappers fail when prefix cannot be written.</summary>
    [Test]
    [Arguments(nameof(AsciiFixBE), 3)]
    [Arguments(nameof(AsciiFixLE), 3)]
    public async Task AsciiFixedWrapper_TryFormat_NullValue_UndersizedPrefixBuffer_ReturnsFalse(string wrapperName, int bufferSize)
    {
        bool success = _TryFormatStringWrapper(wrapperName, null, stackalloc byte[bufferSize], out int written);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies null-backed AsciiZ fails when destination cannot hold the null terminator.</summary>
    [Test]
    public async Task AsciiZ_TryFormat_NullValue_UndersizedBuffer_ReturnsFalse()
    {
        AsciiZ wrapper = new AsciiZ(null);

        bool success = wrapper.TryFormat(Span<byte>.Empty, out int written, default, null);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies null-backed Latin1 returns success with zero bytes written.</summary>
    [Test]
    public async Task Latin1_TryFormat_NullValue_WritesZeroBytes()
    {
        Latin1 wrapper = new Latin1(null);

        bool success = wrapper.TryFormat(Span<byte>.Empty, out int written, default, null);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies Latin1 prefixed wrappers cover ToString, overload, and implicit conversion.</summary>
    [Test]
    [Arguments(nameof(Latin1Var))]
    [Arguments(nameof(Latin1FixBE))]
    [Arguments(nameof(Latin1FixLE))]
    [Arguments(nameof(Latin1Z))]
    public async Task Latin1PrefixedWrapper_SurfaceArea_IsCovered(string wrapperName)
    {
        string implicitText = _ToStringImplicit(wrapperName, _SampleLatin1);
        string plain = _ToStringStringWrapper(wrapperName, _SampleLatin1);
        string overload = _ToStringStringWrapperWithFormat(wrapperName, _SampleLatin1);

        await Assert.That(implicitText).IsEqualTo(_SampleLatin1);
        await Assert.That(plain).IsEqualTo(_SampleLatin1);
        await Assert.That(overload).IsEqualTo(_SampleLatin1);
    }

    /// <summary>Verifies null-backed Latin1 fixed-prefix wrappers fail when prefix cannot be written.</summary>
    [Test]
    [Arguments(nameof(Latin1FixBE), 3)]
    [Arguments(nameof(Latin1FixLE), 3)]
    public async Task Latin1FixedWrapper_TryFormat_NullValue_UndersizedPrefixBuffer_ReturnsFalse(string wrapperName, int bufferSize)
    {
        bool success = _TryFormatStringWrapper(wrapperName, null, stackalloc byte[bufferSize], out int written);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies null-backed Latin1Z fails when destination cannot hold the null terminator.</summary>
    [Test]
    public async Task Latin1Z_TryFormat_NullValue_UndersizedBuffer_ReturnsFalse()
    {
        Latin1Z wrapper = new Latin1Z(null);

        bool success = wrapper.TryFormat(Span<byte>.Empty, out int written, default, null);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies Utf8 returns empty ToString overload for null.</summary>
    [Test]
    public async Task Utf8_ToStringOverload_NullValue_ReturnsEmpty()
    {
        Utf8 wrapper = new Utf8(null);

        string overload = wrapper.ToString(null, CultureInfo.InvariantCulture);

        await Assert.That(overload).IsEqualTo("");
    }


    /// <summary>Verifies TryFormat returns false when the destination cannot hold the encoded output.</summary>
    [Test]
    [Arguments(nameof(Utf8))]
    [Arguments(nameof(Utf8Var))]
    [Arguments(nameof(Utf8FixBE))]
    [Arguments(nameof(Utf8FixLE))]
    [Arguments(nameof(Utf8Fix16LE))]
    [Arguments(nameof(Utf8Z))]
    [Arguments(nameof(Utf16BE))]
    [Arguments(nameof(Utf16BEVar))]
    [Arguments(nameof(Utf16BEFixBE))]
    [Arguments(nameof(Utf16BEFixLE))]
    [Arguments(nameof(Utf16LE))]
    [Arguments(nameof(Utf16LEVar))]
    [Arguments(nameof(Utf16LEFixBE))]
    [Arguments(nameof(Utf16LEFixLE))]
    [Arguments(nameof(Utf32BE))]
    [Arguments(nameof(Utf32BEVar))]
    [Arguments(nameof(Utf32BEFixBE))]
    [Arguments(nameof(Utf32BEFixLE))]
    [Arguments(nameof(Utf32LE))]
    [Arguments(nameof(Utf32LEVar))]
    [Arguments(nameof(Utf32LEFixBE))]
    [Arguments(nameof(Utf32LEFixLE))]
    [Arguments(nameof(Ascii))]
    [Arguments(nameof(AsciiVar))]
    [Arguments(nameof(AsciiFixBE))]
    [Arguments(nameof(AsciiFixLE))]
    [Arguments(nameof(AsciiZ))]
    [Arguments(nameof(Latin1))]
    [Arguments(nameof(Latin1Var))]
    [Arguments(nameof(Latin1FixBE))]
    [Arguments(nameof(Latin1FixLE))]
    [Arguments(nameof(Latin1Z))]
    public async Task StringWrapper_TryFormat_UndersizedBuffer_ReturnsFalse(string wrapperName)
    {
        bool success = _TryFormatStringWrapper(wrapperName, _SampleAscii, Span<byte>.Empty, out int written);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies ToString returns empty string for null-backed wrappers.</summary>
    [Test]
    [Arguments(nameof(Utf8))]
    [Arguments(nameof(Utf8Var))]
    [Arguments(nameof(Utf16LE))]
    [Arguments(nameof(Utf32BE))]
    [Arguments(nameof(Ascii))]
    [Arguments(nameof(Latin1))]
    public async Task StringWrapper_ToString_NullValue_ReturnsEmpty(string wrapperName)
    {
        string text = _ToStringStringWrapper(wrapperName, null);

        await Assert.That(text).IsEqualTo("");
    }

    /// <summary>Verifies ToString(string?, IFormatProvider?) delegates to parameterless ToString.</summary>
    [Test]
    [Arguments(nameof(Utf8))]
    [Arguments(nameof(Utf8Var))]
    [Arguments(nameof(Utf8FixBE))]
    [Arguments(nameof(Utf8FixLE))]
    [Arguments(nameof(Utf8Fix16LE))]
    [Arguments(nameof(Utf8Z))]
    [Arguments(nameof(Utf16BE))]
    [Arguments(nameof(Utf16BEVar))]
    [Arguments(nameof(Utf16BEFixBE))]
    [Arguments(nameof(Utf16BEFixLE))]
    [Arguments(nameof(Utf16LE))]
    [Arguments(nameof(Utf16LEVar))]
    [Arguments(nameof(Utf16LEFixBE))]
    [Arguments(nameof(Utf16LEFixLE))]
    [Arguments(nameof(Utf32BE))]
    [Arguments(nameof(Utf32BEVar))]
    [Arguments(nameof(Utf32BEFixBE))]
    [Arguments(nameof(Utf32BEFixLE))]
    [Arguments(nameof(Utf32LE))]
    [Arguments(nameof(Utf32LEVar))]
    [Arguments(nameof(Utf32LEFixBE))]
    [Arguments(nameof(Utf32LEFixLE))]
    [Arguments(nameof(Ascii))]
    [Arguments(nameof(AsciiVar))]
    [Arguments(nameof(AsciiFixBE))]
    [Arguments(nameof(AsciiFixLE))]
    [Arguments(nameof(AsciiZ))]
    [Arguments(nameof(Latin1))]
    [Arguments(nameof(Latin1Var))]
    [Arguments(nameof(Latin1FixBE))]
    [Arguments(nameof(Latin1FixLE))]
    [Arguments(nameof(Latin1Z))]
    public async Task StringWrapper_ToStringOverload_DelegatesToParameterless(string wrapperName)
    {
        string plain = _ToStringStringWrapper(wrapperName, _SampleAscii);
        string overload = _ToStringStringWrapperWithFormat(wrapperName, _SampleAscii);

        await Assert.That(overload).IsEqualTo(plain);
    }

    /// <summary>Verifies implicit string conversion constructs an equivalent wrapper.</summary>
    [Test]
    [Arguments(nameof(Utf8))]
    [Arguments(nameof(Utf8Var))]
    [Arguments(nameof(Utf8FixBE))]
    [Arguments(nameof(Utf8FixLE))]
    [Arguments(nameof(Utf8Fix16LE))]
    [Arguments(nameof(Utf8Z))]
    [Arguments(nameof(Utf16BE))]
    [Arguments(nameof(Utf16BEVar))]
    [Arguments(nameof(Utf16BEFixBE))]
    [Arguments(nameof(Utf16BEFixLE))]
    [Arguments(nameof(Utf16LE))]
    [Arguments(nameof(Utf16LEVar))]
    [Arguments(nameof(Utf16LEFixBE))]
    [Arguments(nameof(Utf16LEFixLE))]
    [Arguments(nameof(Utf32BE))]
    [Arguments(nameof(Utf32BEVar))]
    [Arguments(nameof(Utf32BEFixBE))]
    [Arguments(nameof(Utf32BEFixLE))]
    [Arguments(nameof(Utf32LE))]
    [Arguments(nameof(Utf32LEVar))]
    [Arguments(nameof(Utf32LEFixBE))]
    [Arguments(nameof(Utf32LEFixLE))]
    [Arguments(nameof(Ascii))]
    [Arguments(nameof(AsciiVar))]
    [Arguments(nameof(AsciiFixBE))]
    [Arguments(nameof(AsciiFixLE))]
    [Arguments(nameof(AsciiZ))]
    [Arguments(nameof(Latin1))]
    [Arguments(nameof(Latin1Var))]
    [Arguments(nameof(Latin1FixBE))]
    [Arguments(nameof(Latin1FixLE))]
    [Arguments(nameof(Latin1Z))]
    public async Task StringWrapper_ImplicitConversion_MatchesExplicitConstructor(string wrapperName)
    {
        string implicitText = _ToStringImplicit(wrapperName, _SampleAscii);
        string explicitText = _ToStringStringWrapper(wrapperName, _SampleAscii);

        await Assert.That(implicitText).IsEqualTo(explicitText);
    }

    /// <summary>Verifies reverse implicit conversion from Utf8Var to string returns Value.</summary>
    [Test]
    public async Task Utf8Var_ImplicitConversion_ToString_ReturnsValue()
    {
        Utf8Var wrapper = _SampleAscii;
        string text = wrapper;

        await Assert.That(text).IsEqualTo(_SampleAscii);
    }

    /// <summary>Verifies static TryGetSerializedSize is unsupported for variable-length UTF-8 wrappers.</summary>
    [Test]
    [Arguments(nameof(Utf8Var))]
    [Arguments(nameof(Utf8FixBE))]
    [Arguments(nameof(Utf8FixLE))]
    [Arguments(nameof(Utf8Fix16LE))]
    [Arguments(nameof(Utf8Z))]
    public async Task Utf8ParsableWrapper_TryGetSerializedSize_ReturnsFalse(string wrapperName)
    {
        bool success = _TryGetSerializedSize(wrapperName, out int size);

        await Assert.That(success).IsFalse();
        await Assert.That(size).IsEqualTo(0);
    }

    // ========================================================================
    // HELPERS
    // ========================================================================

    private static int _FormatStringWrapper(string wrapperName, string? value, Span<byte> destination)
    {
        if (!_TryFormatStringWrapper(wrapperName, value, destination, out int written))
        {
            throw new InvalidOperationException($"TryFormat failed for {wrapperName}.");
        }

        return written;
    }

    private static bool _TryFormatStringWrapper(string wrapperName, string? value, Span<byte> destination, out int bytesWritten) => wrapperName switch
    {
        nameof(Utf8) => new Utf8(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf8Var) => new Utf8Var(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf8FixBE) => new Utf8FixBE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf8FixLE) => new Utf8FixLE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf8Fix16LE) => new Utf8Fix16LE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf8Z) => new Utf8Z(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf16BE) => new Utf16BE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf16BEVar) => new Utf16BEVar(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf16BEFixBE) => new Utf16BEFixBE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf16BEFixLE) => new Utf16BEFixLE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf16LE) => new Utf16LE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf16LEVar) => new Utf16LEVar(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf16LEFixBE) => new Utf16LEFixBE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf16LEFixLE) => new Utf16LEFixLE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf32BE) => new Utf32BE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf32BEVar) => new Utf32BEVar(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf32BEFixBE) => new Utf32BEFixBE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf32BEFixLE) => new Utf32BEFixLE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf32LE) => new Utf32LE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf32LEVar) => new Utf32LEVar(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf32LEFixBE) => new Utf32LEFixBE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Utf32LEFixLE) => new Utf32LEFixLE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Ascii) => new Ascii(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(AsciiVar) => new AsciiVar(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(AsciiFixBE) => new AsciiFixBE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(AsciiFixLE) => new AsciiFixLE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(AsciiZ) => new AsciiZ(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Latin1) => new Latin1(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Latin1Var) => new Latin1Var(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Latin1FixBE) => new Latin1FixBE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Latin1FixLE) => new Latin1FixLE(value).TryFormat(destination, out bytesWritten, default, null),
        nameof(Latin1Z) => new Latin1Z(value).TryFormat(destination, out bytesWritten, default, null),
        _ => throw new ArgumentOutOfRangeException(nameof(wrapperName), wrapperName, "Unknown string wrapper type."),
    };

    private static bool _TryParseStringWrapper(string wrapperName, ReadOnlySpan<byte> source, out string? text, out int consumed)
    {
        switch (wrapperName)
        {
            case nameof(Utf8Var):
            {
                bool ok = Utf8Var.TryParse(source, out Utf8Var parsed, out consumed);
                text = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(Utf8FixBE):
            {
                bool ok = Utf8FixBE.TryParse(source, out Utf8FixBE parsed, out consumed);
                text = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(Utf8FixLE):
            {
                bool ok = Utf8FixLE.TryParse(source, out Utf8FixLE parsed, out consumed);
                text = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(Utf8Fix16LE):
            {
                bool ok = Utf8Fix16LE.TryParse(source, out Utf8Fix16LE parsed, out consumed);
                text = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(Utf8Z):
            {
                bool ok = Utf8Z.TryParse(source, out Utf8Z parsed, out consumed);
                text = ok ? parsed.Value : null;
                return ok;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(wrapperName), wrapperName, "Wrapper does not support TryParse.");
        }
    }

    private static bool _TryGetSerializedSize(string wrapperName, out int size) => wrapperName switch
    {
        nameof(Utf8Var) => Utf8Var.TryGetSerializedSize(out size),
        nameof(Utf8FixBE) => Utf8FixBE.TryGetSerializedSize(out size),
        nameof(Utf8FixLE) => Utf8FixLE.TryGetSerializedSize(out size),
        nameof(Utf8Fix16LE) => Utf8Fix16LE.TryGetSerializedSize(out size),
        nameof(Utf8Z) => Utf8Z.TryGetSerializedSize(out size),
        _ => throw new ArgumentOutOfRangeException(nameof(wrapperName), wrapperName, "Wrapper does not expose TryGetSerializedSize."),
    };

    private static string _ToStringStringWrapper(string wrapperName, string? value) => wrapperName switch
    {
        nameof(Utf8) => new Utf8(value).ToString(),
        nameof(Utf8Var) => new Utf8Var(value).ToString(),
        nameof(Utf8FixBE) => new Utf8FixBE(value).ToString(),
        nameof(Utf8FixLE) => new Utf8FixLE(value).ToString(),
        nameof(Utf8Fix16LE) => new Utf8Fix16LE(value).ToString(),
        nameof(Utf8Z) => new Utf8Z(value).ToString(),
        nameof(Utf16BE) => new Utf16BE(value).ToString(),
        nameof(Utf16BEVar) => new Utf16BEVar(value).ToString(),
        nameof(Utf16BEFixBE) => new Utf16BEFixBE(value).ToString(),
        nameof(Utf16BEFixLE) => new Utf16BEFixLE(value).ToString(),
        nameof(Utf16LE) => new Utf16LE(value).ToString(),
        nameof(Utf16LEVar) => new Utf16LEVar(value).ToString(),
        nameof(Utf16LEFixBE) => new Utf16LEFixBE(value).ToString(),
        nameof(Utf16LEFixLE) => new Utf16LEFixLE(value).ToString(),
        nameof(Utf32BE) => new Utf32BE(value).ToString(),
        nameof(Utf32BEVar) => new Utf32BEVar(value).ToString(),
        nameof(Utf32BEFixBE) => new Utf32BEFixBE(value).ToString(),
        nameof(Utf32BEFixLE) => new Utf32BEFixLE(value).ToString(),
        nameof(Utf32LE) => new Utf32LE(value).ToString(),
        nameof(Utf32LEVar) => new Utf32LEVar(value).ToString(),
        nameof(Utf32LEFixBE) => new Utf32LEFixBE(value).ToString(),
        nameof(Utf32LEFixLE) => new Utf32LEFixLE(value).ToString(),
        nameof(Ascii) => new Ascii(value).ToString(),
        nameof(AsciiVar) => new AsciiVar(value).ToString(),
        nameof(AsciiFixBE) => new AsciiFixBE(value).ToString(),
        nameof(AsciiFixLE) => new AsciiFixLE(value).ToString(),
        nameof(AsciiZ) => new AsciiZ(value).ToString(),
        nameof(Latin1) => new Latin1(value).ToString(),
        nameof(Latin1Var) => new Latin1Var(value).ToString(),
        nameof(Latin1FixBE) => new Latin1FixBE(value).ToString(),
        nameof(Latin1FixLE) => new Latin1FixLE(value).ToString(),
        nameof(Latin1Z) => new Latin1Z(value).ToString(),
        _ => throw new ArgumentOutOfRangeException(nameof(wrapperName), wrapperName, "Unknown string wrapper type."),
    };

    private static string _ToStringStringWrapperWithFormat(string wrapperName, string? value) => wrapperName switch
    {
        nameof(Utf8) => new Utf8(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf8Var) => new Utf8Var(value).ToString("ignored", CultureInfo.InvariantCulture),
        nameof(Utf8FixBE) => new Utf8FixBE(value).ToString("ignored", CultureInfo.InvariantCulture),
        nameof(Utf8FixLE) => new Utf8FixLE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf8Fix16LE) => new Utf8Fix16LE(value).ToString(null, null),
        nameof(Utf8Z) => new Utf8Z(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf16BE) => new Utf16BE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf16BEVar) => new Utf16BEVar(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf16BEFixBE) => new Utf16BEFixBE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf16BEFixLE) => new Utf16BEFixLE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf16LE) => new Utf16LE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf16LEVar) => new Utf16LEVar(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf16LEFixBE) => new Utf16LEFixBE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf16LEFixLE) => new Utf16LEFixLE(value).ToString(null, null),
        nameof(Utf32BE) => new Utf32BE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf32BEVar) => new Utf32BEVar(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf32BEFixBE) => new Utf32BEFixBE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf32BEFixLE) => new Utf32BEFixLE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf32LE) => new Utf32LE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf32LEVar) => new Utf32LEVar(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf32LEFixBE) => new Utf32LEFixBE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Utf32LEFixLE) => new Utf32LEFixLE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Ascii) => new Ascii(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(AsciiVar) => new AsciiVar(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(AsciiFixBE) => new AsciiFixBE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(AsciiFixLE) => new AsciiFixLE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(AsciiZ) => new AsciiZ(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Latin1) => new Latin1(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Latin1Var) => new Latin1Var(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Latin1FixBE) => new Latin1FixBE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Latin1FixLE) => new Latin1FixLE(value).ToString(null, CultureInfo.InvariantCulture),
        nameof(Latin1Z) => new Latin1Z(value).ToString(null, CultureInfo.InvariantCulture),
        _ => throw new ArgumentOutOfRangeException(nameof(wrapperName), wrapperName, "Unknown string wrapper type."),
    };

    private static string _ToStringImplicit(string wrapperName, string value) => wrapperName switch
    {
        nameof(Utf8) => ((Utf8)value).ToString(),
        nameof(Utf8Var) => ((Utf8Var)value).ToString(),
        nameof(Utf8FixBE) => ((Utf8FixBE)value).ToString(),
        nameof(Utf8FixLE) => ((Utf8FixLE)value).ToString(),
        nameof(Utf8Fix16LE) => ((Utf8Fix16LE)value).ToString(),
        nameof(Utf8Z) => ((Utf8Z)value).ToString(),
        nameof(Utf16BE) => ((Utf16BE)value).ToString(),
        nameof(Utf16BEVar) => ((Utf16BEVar)value).ToString(),
        nameof(Utf16BEFixBE) => ((Utf16BEFixBE)value).ToString(),
        nameof(Utf16BEFixLE) => ((Utf16BEFixLE)value).ToString(),
        nameof(Utf16LE) => ((Utf16LE)value).ToString(),
        nameof(Utf16LEVar) => ((Utf16LEVar)value).ToString(),
        nameof(Utf16LEFixBE) => ((Utf16LEFixBE)value).ToString(),
        nameof(Utf16LEFixLE) => ((Utf16LEFixLE)value).ToString(),
        nameof(Utf32BE) => ((Utf32BE)value).ToString(),
        nameof(Utf32BEVar) => ((Utf32BEVar)value).ToString(),
        nameof(Utf32BEFixBE) => ((Utf32BEFixBE)value).ToString(),
        nameof(Utf32BEFixLE) => ((Utf32BEFixLE)value).ToString(),
        nameof(Utf32LE) => ((Utf32LE)value).ToString(),
        nameof(Utf32LEVar) => ((Utf32LEVar)value).ToString(),
        nameof(Utf32LEFixBE) => ((Utf32LEFixBE)value).ToString(),
        nameof(Utf32LEFixLE) => ((Utf32LEFixLE)value).ToString(),
        nameof(Ascii) => ((Ascii)value).ToString(),
        nameof(AsciiVar) => ((AsciiVar)value).ToString(),
        nameof(AsciiFixBE) => ((AsciiFixBE)value).ToString(),
        nameof(AsciiFixLE) => ((AsciiFixLE)value).ToString(),
        nameof(AsciiZ) => ((AsciiZ)value).ToString(),
        nameof(Latin1) => ((Latin1)value).ToString(),
        nameof(Latin1Var) => ((Latin1Var)value).ToString(),
        nameof(Latin1FixBE) => ((Latin1FixBE)value).ToString(),
        nameof(Latin1FixLE) => ((Latin1FixLE)value).ToString(),
        nameof(Latin1Z) => ((Latin1Z)value).ToString(),
        _ => throw new ArgumentOutOfRangeException(nameof(wrapperName), wrapperName, "Unknown string wrapper type."),
    };

    private static string _ReverseImplicitToString(string wrapperName, string value) => wrapperName switch
    {
        nameof(Utf8FixBE) => (string)new Utf8FixBE(value),
        nameof(Utf8FixLE) => (string)new Utf8FixLE(value),
        nameof(Utf8Fix16LE) => (string)new Utf8Fix16LE(value),
        nameof(Utf8Z) => (string)new Utf8Z(value),
        _ => throw new ArgumentOutOfRangeException(nameof(wrapperName), wrapperName, "Wrapper does not support reverse implicit conversion."),
    };

    private static int _PayloadOffset(string wrapperName, int totalWritten, int asciiLength) => wrapperName switch
    {
        nameof(AsciiVar) => totalWritten - asciiLength,
        nameof(AsciiFixBE) or nameof(AsciiFixLE) => 4,
        nameof(AsciiZ) => 0,
        _ => throw new ArgumentOutOfRangeException(nameof(wrapperName), wrapperName, "Unsupported payload offset wrapper."),
    };
}
