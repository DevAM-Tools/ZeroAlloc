// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Exit-point coverage for fixed-endian, floating-point, raw, and variable-length binary wrappers
/// defined in <c>EndianWrapper.cs</c>.
/// </summary>
public sealed class EndianWrapperTests
{
    // ========================================================================
    // FIXED-SIZE WRAPPERS — ROUND-TRIP
    // ========================================================================

    /// <summary>Verifies format then parse round-trips preserve the wrapped value.</summary>
    [Test]
    [Arguments(nameof(U16BE), (ushort)0x1234)]
    [Arguments(nameof(U32BE), 0xDEADBEEFu)]
    [Arguments(nameof(U64BE), 0x0123456789ABCDEFUL)]
    [Arguments(nameof(I16BE), (short)-1234)]
    [Arguments(nameof(I32BE), -0x7EADBEEF)]
    [Arguments(nameof(I64BE), -0x0123456789ABCDEFL)]
    [Arguments(nameof(U16LE), (ushort)0x3412)]
    [Arguments(nameof(U32LE), 0xEFBEADDEu)]
    [Arguments(nameof(U64LE), 0xEFCDAB9078563412UL)]
    [Arguments(nameof(I16LE), (short)1234)]
    [Arguments(nameof(I32LE), 0x7EADBEEF)]
    [Arguments(nameof(I64LE), 0x0123456789ABCDEFL)]
    [Arguments(nameof(F32BE), 3.14159265f)]
    [Arguments(nameof(F32LE), -2.71828183f)]
    [Arguments(nameof(F64BE), 1.23456789)]
    [Arguments(nameof(F64LE), -9.87654321)]
    public async Task FixedWrapper_FormatParse_RoundTrip_PreservesValue(string typeName, object rawValue)
    {
        byte[] buffer = new byte[16];
        int written = _FormatFixedWrapper(typeName, rawValue, buffer);
        bool parsed = _TryParseFixedWrapper(typeName, buffer.AsSpan(0, written), out object? roundTripped);

        await Assert.That(parsed).IsTrue();
        await Assert.That(roundTripped).IsEqualTo(rawValue);
    }

    /// <summary>Verifies 128-bit unsigned wrappers round-trip through format and parse.</summary>
    [Test]
    public async Task U128BE_FormatParse_RoundTrip_PreservesValue()
    {
        UInt128 value = UInt128.Parse("0102030405060708090A0B0C0D0E0F10", NumberStyles.HexNumber);
        Span<byte> buffer = stackalloc byte[U128BE.Size];

        bool formatted = new U128BE(value).TryFormat(buffer, out int written, default, null);
        bool parsed = U128BE.TryParse(buffer.Slice(0, written), out U128BE roundTripped, out int consumed);

        await Assert.That(formatted).IsTrue();
        await Assert.That(parsed).IsTrue();
        await Assert.That(consumed).IsEqualTo(U128BE.Size);
        await Assert.That(roundTripped.Value).IsEqualTo(value);
    }

    /// <summary>Verifies 128-bit signed big-endian wrappers round-trip through format and parse.</summary>
    [Test]
    public async Task I128BE_FormatParse_RoundTrip_PreservesValue()
    {
        Int128 value = -Int128.Parse("0102030405060708090A0B0C0D0E0F10", NumberStyles.HexNumber);
        Span<byte> buffer = stackalloc byte[I128BE.Size];

        bool formatted = new I128BE(value).TryFormat(buffer, out int written, default, null);
        bool parsed = I128BE.TryParse(buffer.Slice(0, written), out I128BE roundTripped, out int consumed);

        await Assert.That(formatted).IsTrue();
        await Assert.That(parsed).IsTrue();
        await Assert.That(consumed).IsEqualTo(I128BE.Size);
        await Assert.That(roundTripped.Value).IsEqualTo(value);
    }

    /// <summary>Verifies 128-bit unsigned little-endian wrappers round-trip through format and parse.</summary>
    [Test]
    public async Task U128LE_FormatParse_RoundTrip_PreservesValue()
    {
        UInt128 value = UInt128.Parse("000000000000000000000000000000FF", NumberStyles.HexNumber);
        Span<byte> buffer = stackalloc byte[U128LE.Size];

        bool formatted = new U128LE(value).TryFormat(buffer, out int written, default, null);
        bool parsed = U128LE.TryParse(buffer.Slice(0, written), out U128LE roundTripped, out int consumed);

        await Assert.That(formatted).IsTrue();
        await Assert.That(parsed).IsTrue();
        await Assert.That(consumed).IsEqualTo(U128LE.Size);
        await Assert.That(roundTripped.Value).IsEqualTo(value);
    }

    /// <summary>Verifies 128-bit signed little-endian wrappers round-trip through format and parse.</summary>
    [Test]
    public async Task I128LE_FormatParse_RoundTrip_PreservesValue()
    {
        Int128 value = -Int128.Parse("0102030405060708090A0B0C0D0E0F10", NumberStyles.HexNumber);
        Span<byte> buffer = stackalloc byte[I128LE.Size];

        bool formatted = new I128LE(value).TryFormat(buffer, out int written, default, null);
        bool parsed = I128LE.TryParse(buffer.Slice(0, written), out I128LE roundTripped, out int consumed);

        await Assert.That(formatted).IsTrue();
        await Assert.That(parsed).IsTrue();
        await Assert.That(consumed).IsEqualTo(I128LE.Size);
        await Assert.That(roundTripped.Value).IsEqualTo(value);
    }

    // ========================================================================
    // FIXED-SIZE WRAPPERS — SERIALIZED SIZE
    // ========================================================================

    /// <summary>Verifies TryGetSerializedSize reports the compile-time Size constant.</summary>
    [Test]
    [Arguments(nameof(U16BE), 2)]
    [Arguments(nameof(U32BE), 4)]
    [Arguments(nameof(U64BE), 8)]
    [Arguments(nameof(U128BE), 16)]
    [Arguments(nameof(I16BE), 2)]
    [Arguments(nameof(I32BE), 4)]
    [Arguments(nameof(I64BE), 8)]
    [Arguments(nameof(I128BE), 16)]
    [Arguments(nameof(U16LE), 2)]
    [Arguments(nameof(U32LE), 4)]
    [Arguments(nameof(U64LE), 8)]
    [Arguments(nameof(U128LE), 16)]
    [Arguments(nameof(I16LE), 2)]
    [Arguments(nameof(I32LE), 4)]
    [Arguments(nameof(I64LE), 8)]
    [Arguments(nameof(I128LE), 16)]
    [Arguments(nameof(F32BE), 4)]
    [Arguments(nameof(F32LE), 4)]
    [Arguments(nameof(F64BE), 8)]
    [Arguments(nameof(F64LE), 8)]
    public async Task FixedWrapper_TryGetSerializedSize_ReturnsSizeConstant(string typeName, int expectedSize)
    {
        bool success = _TryGetFixedSerializedSize(typeName, out int size);

        await Assert.That(success).IsTrue();
        await Assert.That(size).IsEqualTo(expectedSize);
    }

    // ========================================================================
    // FIXED-SIZE WRAPPERS — TRYFORMAT FAILURE
    // ========================================================================

    /// <summary>Verifies TryFormat returns false when the destination is one byte too small.</summary>
    [Test]
    [Arguments(nameof(U16BE), 2)]
    [Arguments(nameof(U32BE), 4)]
    [Arguments(nameof(U64BE), 8)]
    [Arguments(nameof(U128BE), 16)]
    [Arguments(nameof(I16BE), 2)]
    [Arguments(nameof(I32BE), 4)]
    [Arguments(nameof(I64BE), 8)]
    [Arguments(nameof(I128BE), 16)]
    [Arguments(nameof(U16LE), 2)]
    [Arguments(nameof(U32LE), 4)]
    [Arguments(nameof(U64LE), 8)]
    [Arguments(nameof(U128LE), 16)]
    [Arguments(nameof(I16LE), 2)]
    [Arguments(nameof(I32LE), 4)]
    [Arguments(nameof(I64LE), 8)]
    [Arguments(nameof(I128LE), 16)]
    [Arguments(nameof(F32BE), 4)]
    [Arguments(nameof(F32LE), 4)]
    [Arguments(nameof(F64BE), 8)]
    [Arguments(nameof(F64LE), 8)]
    public async Task FixedWrapper_TryFormat_UndersizedBuffer_ReturnsFalse(string typeName, int requiredSize)
    {
        bool success;
        int bytesWritten;
        {
            Span<byte> tiny = stackalloc byte[Math.Max(0, requiredSize - 1)];
            success = _TryFormatFixedWrapper(typeName, tiny, out bytesWritten);
        }

        await Assert.That(success).IsFalse();
        await Assert.That(bytesWritten).IsEqualTo(0);
    }

    // ========================================================================
    // FIXED-SIZE WRAPPERS — TRYPARSE FAILURE
    // ========================================================================

    /// <summary>Verifies TryParse returns false when source data is shorter than Size.</summary>
    [Test]
    [Arguments(nameof(U16BE), 2)]
    [Arguments(nameof(U32BE), 4)]
    [Arguments(nameof(U64BE), 8)]
    [Arguments(nameof(U128BE), 16)]
    [Arguments(nameof(I16BE), 2)]
    [Arguments(nameof(I32BE), 4)]
    [Arguments(nameof(I64BE), 8)]
    [Arguments(nameof(I128BE), 16)]
    [Arguments(nameof(U16LE), 2)]
    [Arguments(nameof(U32LE), 4)]
    [Arguments(nameof(U64LE), 8)]
    [Arguments(nameof(U128LE), 16)]
    [Arguments(nameof(I16LE), 2)]
    [Arguments(nameof(I32LE), 4)]
    [Arguments(nameof(I64LE), 8)]
    [Arguments(nameof(I128LE), 16)]
    [Arguments(nameof(F32BE), 4)]
    [Arguments(nameof(F32LE), 4)]
    [Arguments(nameof(F64BE), 8)]
    [Arguments(nameof(F64LE), 8)]
    public async Task FixedWrapper_TryParse_InsufficientData_ReturnsFalse(string typeName, int requiredSize)
    {
        bool success = _TryParseFixedWrapper(typeName, new byte[Math.Max(0, requiredSize - 1)], out _);

        await Assert.That(success).IsFalse();
    }

    // ========================================================================
    // FIXED-SIZE WRAPPERS — TOSTRING
    // ========================================================================

    /// <summary>Verifies unsigned wrappers format default ToString as uppercase hex.</summary>
    [Test]
    [Arguments(nameof(U16BE), (ushort)0x00AB, "00AB")]
    [Arguments(nameof(U32BE), 0x000000CDu, "000000CD")]
    [Arguments(nameof(U64BE), 0x00000000000000EFUL, "00000000000000EF")]
    [Arguments(nameof(U16LE), (ushort)0x00AB, "00AB")]
    [Arguments(nameof(U32LE), 0x000000CDu, "000000CD")]
    [Arguments(nameof(U64LE), 0x00000000000000EFUL, "00000000000000EF")]
    public async Task UnsignedFixedWrapper_ToString_ReturnsUpperHex(string typeName, object value, string expected)
    {
        string text = _ToStringFixedWrapper(typeName, value);

        await Assert.That(text).IsEqualTo(expected);
    }

    /// <summary>Verifies signed wrappers delegate default ToString to decimal formatting.</summary>
    [Test]
    [Arguments(nameof(I16BE), (short)-42, "-42")]
    [Arguments(nameof(I32BE), 1000, "1000")]
    [Arguments(nameof(I64BE), long.MinValue, "-9223372036854775808")]
    [Arguments(nameof(I16LE), (short)7, "7")]
    public async Task SignedFixedWrapper_ToString_ReturnsDecimal(string typeName, object value, string expected)
    {
        string text = _ToStringFixedWrapper(typeName, value);

        await Assert.That(text).IsEqualTo(expected);
    }

    /// <summary>Verifies ToString(string?, IFormatProvider?) delegates to the underlying value formatter.</summary>
    [Test]
    [Arguments(nameof(U16BE), (ushort)255, "X4", "00FF")]
    [Arguments(nameof(U32BE), 255u, "X4", "00FF")]
    [Arguments(nameof(U64BE), 255UL, "X4", "00FF")]
    [Arguments(nameof(I16BE), (short)-255, "X4", "FF01")]
    [Arguments(nameof(I32BE), -255, "X8", "FFFFFF01")]
    [Arguments(nameof(I64BE), -255L, "X16", "FFFFFFFFFFFFFF01")]
    [Arguments(nameof(U16LE), (ushort)255, "X4", "00FF")]
    [Arguments(nameof(U32LE), 255u, "X4", "00FF")]
    [Arguments(nameof(U64LE), 255UL, "X4", "00FF")]
    [Arguments(nameof(I16LE), (short)-255, "X4", "FF01")]
    [Arguments(nameof(I32LE), -255, "X8", "FFFFFF01")]
    [Arguments(nameof(I64LE), -255L, "X16", "FFFFFFFFFFFFFF01")]
    [Arguments(nameof(F32BE), 3.5f, "F1", "3.5")]
    [Arguments(nameof(F32LE), -3.5f, "F1", "-3.5")]
    [Arguments(nameof(F64BE), 3.5, "F1", "3.5")]
    [Arguments(nameof(F64LE), -3.5, "F1", "-3.5")]
    public async Task FixedWrapper_ToStringWithFormat_UsesValueFormatter(string typeName, object value, string format, string expected)
    {
        string text = _ToStringFixedWrapperWithFormat(typeName, value, format, CultureInfo.InvariantCulture);

        await Assert.That(text).IsEqualTo(expected);
    }

    /// <summary>Verifies 128-bit unsigned ToString overload delegates to the underlying value formatter.</summary>
    [Test]
    [Arguments(nameof(U128BE), "0102030405060708090A0B0C0D0E0F10", "X", "102030405060708090A0B0C0D0E0F10")]
    [Arguments(nameof(U128LE), "000000000000000000000000000000FF", "X", "FF")]
    public async Task Unsigned128Wrapper_ToStringWithFormat_UsesValueFormatter(string typeName, object value, string format, string expected)
    {
        UInt128 parsedValue = value is string hexText
            ? UInt128.Parse(hexText, NumberStyles.HexNumber)
            : (UInt128)value;
        string text = _ToStringFixedWrapperWithFormat(typeName, parsedValue, format, CultureInfo.InvariantCulture);
        string expectedText = parsedValue.ToString(format, CultureInfo.InvariantCulture);

        await Assert.That(text).IsEqualTo(expectedText);
    }

    /// <summary>Verifies 128-bit signed ToString overload delegates to the underlying value formatter.</summary>
    [Test]
    public async Task I128Wrapper_ToStringWithFormat_UsesValueFormatter()
    {
        Int128 beValue = -255;
        Int128 leValue = -255;

        string beText = new I128BE(beValue).ToString("D", CultureInfo.InvariantCulture);
        string leText = new I128LE(leValue).ToString("D", CultureInfo.InvariantCulture);

        await Assert.That(beText).IsEqualTo("-255");
        await Assert.That(leText).IsEqualTo("-255");
    }

    // ========================================================================
    // FIXED-SIZE WRAPPERS — IMPLICIT CONVERSION
    // ========================================================================

    /// <summary>Verifies ushort and short both implicitly convert to U16BE with identical bit patterns.</summary>
    [Test]
    public async Task U16BE_ImplicitConversion_FromShortAndUshort_MatchBitPattern()
    {
        U16BE fromUshort = (ushort)0xFF00;
        U16BE fromShort = (short)-256;

        byte[] buffer = new byte[4];
        fromUshort.TryFormat(buffer.AsSpan(0, 2), out int writtenUshort, default, null);
        fromShort.TryFormat(buffer.AsSpan(2, 2), out int writtenShort, default, null);

        await Assert.That(writtenUshort).IsEqualTo(2);
        await Assert.That(writtenShort).IsEqualTo(2);
        await Assert.That(buffer[0]).IsEqualTo(buffer[2]);
        await Assert.That(buffer[1]).IsEqualTo(buffer[3]);
    }

    /// <summary>Verifies float and double implicit conversions construct equivalent wrappers.</summary>
    [Test]
    public async Task FloatWrapper_ImplicitConversion_MatchesExplicitConstructor()
    {
        F32BE explicitBe = new(1.5f);
        F32BE implicitBe = 1.5f;
        F32LE explicitLe = new(-2.5f);
        F32LE implicitLe = -2.5f;
        F64BE explicit64Be = new(1.5);
        F64BE implicit64Be = 1.5;
        F64LE explicit64Le = new(-2.5);
        F64LE implicit64Le = -2.5;

        await Assert.That(implicitBe.Value).IsEqualTo(explicitBe.Value);
        await Assert.That(implicitLe.Value).IsEqualTo(explicitLe.Value);
        await Assert.That(implicit64Be.Value).IsEqualTo(explicit64Be.Value);
        await Assert.That(implicit64Le.Value).IsEqualTo(explicit64Le.Value);
    }

    /// <summary>Verifies each fixed wrapper implicit conversion constructs a wrapper with the expected value.</summary>
    [Test]
    [Arguments(nameof(U32BE), 42u)]
    [Arguments(nameof(U32BE), -1)]
    [Arguments(nameof(U64BE), 42UL)]
    [Arguments(nameof(U64BE), -1L)]
    [Arguments(nameof(U128BE), "42")]
    [Arguments(nameof(I16BE), (short)-7)]
    [Arguments(nameof(I32BE), -99)]
    [Arguments(nameof(I64BE), -99L)]
    [Arguments(nameof(I128BE), "-99")]
    [Arguments(nameof(U16LE), (ushort)42)]
    [Arguments(nameof(U16LE), (short)-1)]
    [Arguments(nameof(U32LE), 42u)]
    [Arguments(nameof(U32LE), -1)]
    [Arguments(nameof(U64LE), 42UL)]
    [Arguments(nameof(U64LE), -1L)]
    [Arguments(nameof(U128LE), "42")]
    [Arguments(nameof(I16LE), (short)-7)]
    [Arguments(nameof(I32LE), -99)]
    [Arguments(nameof(I64LE), -99L)]
    [Arguments(nameof(I128LE), "-99")]
    public async Task FixedWrapper_ImplicitConversion_ConstructsWrapper(string typeName, object rawValue)
    {
        object wrapper = _ImplicitConvertFixedWrapper(typeName, rawValue);
        object value = _ReadFixedWrapperValue(typeName, wrapper);

        await Assert.That(value).IsEqualTo(_NormalizeExpectedValue(typeName, rawValue));
    }

    /// <summary>Verifies 128-bit unsigned default ToString returns uppercase hex.</summary>
    [Test]
    [Arguments(nameof(U128BE), "0102030405060708090A0B0C0D0E0F10", "0102030405060708090A0B0C0D0E0F10")]
    [Arguments(nameof(U128LE), "000000000000000000000000000000FF", "000000000000000000000000000000FF")]
    public async Task Unsigned128Wrapper_ToString_ReturnsUpperHex(string typeName, string hexValue, string expected)
    {
        UInt128 value = UInt128.Parse(hexValue, NumberStyles.HexNumber);
        string text = _ToStringFixedWrapper(typeName, value);

        await Assert.That(text).IsEqualTo(expected);
    }

    /// <summary>Verifies 128-bit signed wrappers delegate default ToString to decimal formatting.</summary>
    [Test]
    public async Task I128Wrapper_ToString_ReturnsDecimal()
    {
        Int128 beValue = -42;
        Int128 leValue = -7;

        await Assert.That(new I128BE(beValue).ToString()).IsEqualTo("-42");
        await Assert.That(new I128LE(leValue).ToString()).IsEqualTo("-7");
    }

    /// <summary>Verifies floating-point wrappers delegate default ToString to the underlying value formatter.</summary>
    [Test]
    public async Task FloatWrapper_ToString_ReturnsUnderlyingValueFormat()
    {
        F32BE be32 = 1.25f;
        F32LE le32 = -2.5f;
        F64BE be64 = 9.875;
        F64LE le64 = -1.5;

        await Assert.That(be32.ToString()).IsEqualTo(1.25f.ToString());
        await Assert.That(le32.ToString()).IsEqualTo((-2.5f).ToString());
        await Assert.That(be64.ToString()).IsEqualTo(9.875.ToString());
        await Assert.That(le64.ToString()).IsEqualTo((-1.5).ToString());
    }

    /// <summary>Verifies I32LE and I64LE delegate default ToString to decimal formatting.</summary>
    [Test]
    public async Task I32LE_And_I64LE_ToString_ReturnDecimal()
    {
        await Assert.That(new I32LE(1000).ToString()).IsEqualTo("1000");
        await Assert.That(new I64LE(long.MinValue).ToString()).IsEqualTo(long.MinValue.ToString());
    }

    /// <summary>Verifies 128-bit signed TryParse fails when source is shorter than Size.</summary>
    [Test]
    [Arguments(nameof(I128BE))]
    [Arguments(nameof(U128LE))]
    public async Task WideWrapper_TryParse_InsufficientData_ReturnsFalse(string typeName)
    {
        bool success = _TryParseFixedWrapper(typeName, new byte[15], out _);

        await Assert.That(success).IsFalse();
    }

    // ========================================================================
    // RAW WRAPPER
    // ========================================================================

    /// <summary>Verifies Raw copies bytes verbatim through TryFormat.</summary>
    [Test]
    public async Task Raw_TryFormat_CopiesBytesVerbatim()
    {
        byte[] source = [0x01, 0x02, 0x03, 0x04];
        Raw raw = source;

        bool success;
        int written;
        byte[] snapshot;
        {
            Span<byte> destination = stackalloc byte[4];
            success = raw.TryFormat(destination, out written, default, null);
            snapshot = destination.ToArray();
        }

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(4);
        await Assert.That(snapshot).IsEquivalentTo(source);
    }

    /// <summary>Verifies Raw.TryFormat returns false when destination is too small.</summary>
    [Test]
    public async Task Raw_TryFormat_UndersizedBuffer_ReturnsFalse()
    {
        Raw raw = new byte[] { 0xAA, 0xBB };

        bool success = raw.TryFormat(stackalloc byte[1], out int written, default, null);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies Raw.ToString returns uppercase hex without separators.</summary>
    [Test]
    public async Task Raw_ToString_ReturnsUpperHex()
    {
        Raw raw = new byte[] { 0x0A, 0xFF };

        string text = raw.ToString();

        await Assert.That(text).IsEqualTo("0AFF");
    }

    /// <summary>Verifies implicit conversion from ReadOnlySpan&lt;byte&gt; constructs Raw with matching Size.</summary>
    [Test]
    public async Task Raw_ImplicitConversion_FromReadOnlySpan_PreservesLength()
    {
        (int size, byte[] valueSnapshot) = _CreateRawFromSpan([4, 5, 6]);

        await Assert.That(size).IsEqualTo(3);
        await Assert.That(valueSnapshot).IsEquivalentTo((byte[])[4, 5, 6]);
    }

    /// <summary>Verifies Raw.Value exposes the wrapped bytes.</summary>
    [Test]
    public async Task Raw_Value_ReturnsWrappedBytes()
    {
        byte[] data = [0x10, 0x20];
        Raw raw = data;
        byte[] valueSnapshot = raw.Value.ToArray();

        await Assert.That(valueSnapshot).IsEquivalentTo(data);
    }

    // ========================================================================
    // VARINT
    // ========================================================================

    /// <summary>Verifies VarInt format/parse round-trip for representative encoded lengths.</summary>
    [Test]
    [Arguments(0UL, 1)]
    [Arguments(127UL, 1)]
    [Arguments(128UL, 2)]
    [Arguments(16_383UL, 2)]
    [Arguments(16_384UL, 3)]
    [Arguments(ulong.MaxValue, 10)]
    public async Task VarInt_FormatParse_RoundTrip_PreservesValue(ulong value, int expectedEncodedSize)
    {
        VarInt wrapper = value;
        Span<byte> buffer = stackalloc byte[VarInt.MaxSize];

        bool formatted = wrapper.TryFormat(buffer, out int written, default, null);
        bool parsed = VarInt.TryParse(buffer.Slice(0, written), out VarInt roundTripped, out int consumed);

        await Assert.That(formatted).IsTrue();
        await Assert.That(parsed).IsTrue();
        await Assert.That(consumed).IsEqualTo(written);
        await Assert.That(roundTripped.Value).IsEqualTo(value);
        await Assert.That(wrapper.EncodedSize).IsEqualTo(expectedEncodedSize);
    }

    /// <summary>Verifies VarInt.TryGetSerializedSize is unsupported for static sizing.</summary>
    [Test]
    public async Task VarInt_TryGetSerializedSize_ReturnsFalse()
    {
        bool success = VarInt.TryGetSerializedSize(out int size);

        await Assert.That(success).IsFalse();
        await Assert.That(size).IsEqualTo(0);
    }

    /// <summary>Verifies VarInt.TryGetWrittenSize reports EncodedSize.</summary>
    [Test]
    public async Task VarInt_TryGetWrittenSize_MatchesEncodedSize()
    {
        VarInt wrapper = 300UL;

        bool success = wrapper.TryGetWrittenSize(out int size);

        await Assert.That(success).IsTrue();
        await Assert.That(size).IsEqualTo(wrapper.EncodedSize);
    }

    /// <summary>Verifies VarInt.ToString(string?, IFormatProvider?) delegates to the underlying ulong formatter.</summary>
    [Test]
    public async Task VarInt_ToStringWithFormat_UsesValueFormatter()
    {
        VarInt wrapper = 42UL;

        string text = wrapper.ToString("D", CultureInfo.InvariantCulture);

        await Assert.That(text).IsEqualTo("42");
    }

    /// <summary>Verifies VarInt.TryWrite delegates to TryFormat.</summary>
    [Test]
    public async Task VarInt_TryWrite_MatchesTryFormat()
    {
        VarInt wrapper = 300UL;
        Span<byte> buffer = stackalloc byte[VarInt.MaxSize];

        bool writeSuccess = wrapper.TryWrite(buffer, out int writeBytes);
        bool formatSuccess = wrapper.TryFormat(buffer, out int formatBytes, default, null);

        await Assert.That(writeSuccess).IsTrue();
        await Assert.That(formatSuccess).IsTrue();
        await Assert.That(writeBytes).IsEqualTo(formatBytes);
    }

    /// <summary>Verifies VarInt.TryFormat fails when the buffer cannot hold continuation bytes.</summary>
    [Test]
    public async Task VarInt_TryFormat_UndersizedBuffer_ReturnsFalse()
    {
        VarInt wrapper = 16_384UL;
        Span<byte> buffer = stackalloc byte[1];

        bool success = wrapper.TryFormat(buffer, out int written, default, null);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies VarInt.TryParse fails on truncated continuation encoding.</summary>
    [Test]
    public async Task VarInt_TryParse_TruncatedContinuation_ReturnsFalse()
    {
        ReadOnlySpan<byte> truncated = [0x80];

        bool success = VarInt.TryParse(truncated, out VarInt value, out int consumed);

        await Assert.That(success).IsFalse();
        await Assert.That(consumed).IsEqualTo(0);
        await Assert.That(value.Value).IsEqualTo(0UL);
    }

    /// <summary>Verifies smaller integer types implicitly widen into VarInt.</summary>
    [Test]
    public async Task VarInt_ImplicitConversion_FromByte_PreservesValue()
    {
        VarInt wrapper = (byte)42;

        await Assert.That(wrapper.Value).IsEqualTo(42UL);
    }

    /// <summary>Verifies ushort values implicitly widen into VarInt.</summary>
    [Test]
    public async Task VarInt_ImplicitConversion_FromUshort_PreservesValue()
    {
        VarInt wrapper = (ushort)1000;

        await Assert.That(wrapper.Value).IsEqualTo(1000UL);
    }

    /// <summary>Verifies uint values implicitly widen into VarInt.</summary>
    [Test]
    public async Task VarInt_ImplicitConversion_FromUint_PreservesValue()
    {
        VarInt wrapper = 50_000u;

        await Assert.That(wrapper.Value).IsEqualTo(50_000UL);
    }

    // ========================================================================
    // VARINT ZIGZAG
    // ========================================================================

    /// <summary>Verifies VarIntZigZag round-trips signed values with compact encoding.</summary>
    [Test]
    [Arguments(0L, 1)]
    [Arguments(-1L, 1)]
    [Arguments(1L, 1)]
    [Arguments(-2L, 1)]
    [Arguments(2L, 1)]
    [Arguments(-64L, 1)]
    [Arguments(64L, 2)]
    [Arguments(long.MinValue, 10)]
    [Arguments(long.MaxValue, 10)]
    public async Task VarIntZigZag_FormatParse_RoundTrip_PreservesValue(long value, int expectedEncodedSize)
    {
        VarIntZigZag wrapper = value;
        Span<byte> buffer = stackalloc byte[VarIntZigZag.MaxSize];

        bool formatted = wrapper.TryFormat(buffer, out int written, default, null);
        bool parsed = VarIntZigZag.TryParse(buffer.Slice(0, written), out VarIntZigZag roundTripped, out int consumed);

        await Assert.That(formatted).IsTrue();
        await Assert.That(parsed).IsTrue();
        await Assert.That(consumed).IsEqualTo(written);
        await Assert.That(roundTripped.Value).IsEqualTo(value);
        await Assert.That(wrapper.EncodedSize).IsEqualTo(expectedEncodedSize);
    }

    /// <summary>Verifies DecodeZigZag inverts ZigZag encoding for known pairs.</summary>
    [Test]
    [Arguments(0UL, 0L)]
    [Arguments(1UL, -1L)]
    [Arguments(2UL, 1L)]
    [Arguments(3UL, -2L)]
    [Arguments(4UL, 2L)]
    public async Task VarIntZigZag_DecodeZigZag_ReturnsSignedValue(ulong encoded, long expected)
    {
        long decoded = VarIntZigZag.DecodeZigZag(encoded);

        await Assert.That(decoded).IsEqualTo(expected);
    }

    /// <summary>Verifies VarIntZigZag.TryGetSerializedSize is unsupported for static sizing.</summary>
    [Test]
    public async Task VarIntZigZag_TryGetSerializedSize_ReturnsFalse()
    {
        bool success = VarIntZigZag.TryGetSerializedSize(out int size);

        await Assert.That(success).IsFalse();
        await Assert.That(size).IsEqualTo(0);
    }

    /// <summary>Verifies VarIntZigZag.TryWrite delegates to TryFormat.</summary>
    [Test]
    public async Task VarIntZigZag_TryWrite_MatchesTryFormat()
    {
        VarIntZigZag wrapper = -42L;
        Span<byte> buffer = stackalloc byte[VarIntZigZag.MaxSize];

        bool writeSuccess = wrapper.TryWrite(buffer, out int writeBytes);
        bool formatSuccess = wrapper.TryFormat(buffer, out int formatBytes, default, null);

        await Assert.That(writeSuccess).IsTrue();
        await Assert.That(formatSuccess).IsTrue();
        await Assert.That(writeBytes).IsEqualTo(formatBytes);
    }

    /// <summary>Verifies VarIntZigZag.TryGetWrittenSize reports EncodedSize.</summary>
    [Test]
    public async Task VarIntZigZag_TryGetWrittenSize_MatchesEncodedSize()
    {
        VarIntZigZag wrapper = -42L;

        bool success = wrapper.TryGetWrittenSize(out int size);

        await Assert.That(success).IsTrue();
        await Assert.That(size).IsEqualTo(wrapper.EncodedSize);
    }

    /// <summary>Verifies VarIntZigZag.ToString(string?, IFormatProvider?) delegates to the signed value formatter.</summary>
    [Test]
    public async Task VarIntZigZag_ToStringWithFormat_UsesValueFormatter()
    {
        VarIntZigZag wrapper = -42L;

        string text = wrapper.ToString("D", CultureInfo.InvariantCulture);

        await Assert.That(text).IsEqualTo("-42");
    }

    /// <summary>Verifies VarIntZigZag.TryFormat fails when the buffer cannot hold continuation bytes.</summary>
    [Test]
    public async Task VarIntZigZag_TryFormat_UndersizedBuffer_ReturnsFalse()
    {
        VarIntZigZag wrapper = 16_384L;
        Span<byte> buffer = stackalloc byte[1];

        bool success = wrapper.TryFormat(buffer, out int written, default, null);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies VarIntZigZag.TryFormat fails when the buffer cannot hold the terminal byte.</summary>
    [Test]
    public async Task VarIntZigZag_TryFormat_MissingTerminalByte_ReturnsFalse()
    {
        VarIntZigZag wrapper = 16_384L;
        Span<byte> buffer = stackalloc byte[2];

        bool success = wrapper.TryFormat(buffer, out int written, default, null);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>Verifies smaller integer types implicitly widen into VarIntZigZag.</summary>
    [Test]
    public async Task VarIntZigZag_ImplicitConversion_FromSmallerTypes_PreservesValue()
    {
        VarIntZigZag fromInt = -7;
        VarIntZigZag fromShort = (short)-8;
        VarIntZigZag fromSbyte = (sbyte)-9;

        await Assert.That(fromInt.Value).IsEqualTo(-7L);
        await Assert.That(fromShort.Value).IsEqualTo(-8L);
        await Assert.That(fromSbyte.Value).IsEqualTo(-9L);
    }

    /// <summary>Verifies VarIntZigZag.TryParse fails when underlying VarInt bytes are incomplete.</summary>
    [Test]
    public async Task VarIntZigZag_TryParse_InsufficientData_ReturnsFalse()
    {
        bool success = VarIntZigZag.TryParse(ReadOnlySpan<byte>.Empty, out VarIntZigZag value, out int consumed);

        await Assert.That(success).IsFalse();
        await Assert.That(consumed).IsEqualTo(0);
        await Assert.That(value.Value).IsEqualTo(0L);
    }

    // ========================================================================
    // HELPERS
    // ========================================================================

    private static int _FormatFixedWrapper(string typeName, object rawValue, Span<byte> destination)
    {
        switch (typeName)
        {
            case nameof(U16BE):
                return _Write(new U16BE((ushort)rawValue), destination);
            case nameof(U32BE):
                return _Write(new U32BE((uint)rawValue), destination);
            case nameof(U64BE):
                return _Write(new U64BE((ulong)rawValue), destination);
            case nameof(U128BE):
                return _Write(new U128BE((UInt128)rawValue), destination);
            case nameof(I16BE):
                return _Write(new I16BE((short)rawValue), destination);
            case nameof(I32BE):
                return _Write(new I32BE((int)rawValue), destination);
            case nameof(I64BE):
                return _Write(new I64BE((long)rawValue), destination);
            case nameof(I128BE):
                return _Write(new I128BE((Int128)rawValue), destination);
            case nameof(U16LE):
                return _Write(new U16LE((ushort)rawValue), destination);
            case nameof(U32LE):
                return _Write(new U32LE((uint)rawValue), destination);
            case nameof(U64LE):
                return _Write(new U64LE((ulong)rawValue), destination);
            case nameof(U128LE):
                return _Write(new U128LE((UInt128)rawValue), destination);
            case nameof(I16LE):
                return _Write(new I16LE((short)rawValue), destination);
            case nameof(I32LE):
                return _Write(new I32LE((int)rawValue), destination);
            case nameof(I64LE):
                return _Write(new I64LE((long)rawValue), destination);
            case nameof(I128LE):
                return _Write(new I128LE((Int128)rawValue), destination);
            case nameof(F32BE):
                return _Write(new F32BE((float)rawValue), destination);
            case nameof(F32LE):
                return _Write(new F32LE((float)rawValue), destination);
            case nameof(F64BE):
                return _Write(new F64BE((double)rawValue), destination);
            case nameof(F64LE):
                return _Write(new F64LE((double)rawValue), destination);
            default:
                throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown fixed wrapper type.");
        }
    }

    private static bool _TryFormatFixedWrapper(string typeName, Span<byte> destination, out int bytesWritten)
    {
        object sample = _SampleValue(typeName);
        switch (typeName)
        {
            case nameof(U16BE):
                return new U16BE((ushort)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(U32BE):
                return new U32BE((uint)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(U64BE):
                return new U64BE((ulong)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(U128BE):
                return new U128BE((UInt128)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(I16BE):
                return new I16BE((short)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(I32BE):
                return new I32BE((int)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(I64BE):
                return new I64BE((long)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(I128BE):
                return new I128BE((Int128)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(U16LE):
                return new U16LE((ushort)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(U32LE):
                return new U32LE((uint)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(U64LE):
                return new U64LE((ulong)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(U128LE):
                return new U128LE((UInt128)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(I16LE):
                return new I16LE((short)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(I32LE):
                return new I32LE((int)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(I64LE):
                return new I64LE((long)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(I128LE):
                return new I128LE((Int128)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(F32BE):
                return new F32BE((float)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(F32LE):
                return new F32LE((float)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(F64BE):
                return new F64BE((double)sample).TryFormat(destination, out bytesWritten, default, null);
            case nameof(F64LE):
                return new F64LE((double)sample).TryFormat(destination, out bytesWritten, default, null);
            default:
                throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown fixed wrapper type.");
        }
    }

    private static bool _TryGetFixedSerializedSize(string typeName, out int size) => typeName switch
    {
        nameof(U16BE) => U16BE.TryGetSerializedSize(out size),
        nameof(U32BE) => U32BE.TryGetSerializedSize(out size),
        nameof(U64BE) => U64BE.TryGetSerializedSize(out size),
        nameof(U128BE) => U128BE.TryGetSerializedSize(out size),
        nameof(I16BE) => I16BE.TryGetSerializedSize(out size),
        nameof(I32BE) => I32BE.TryGetSerializedSize(out size),
        nameof(I64BE) => I64BE.TryGetSerializedSize(out size),
        nameof(I128BE) => I128BE.TryGetSerializedSize(out size),
        nameof(U16LE) => U16LE.TryGetSerializedSize(out size),
        nameof(U32LE) => U32LE.TryGetSerializedSize(out size),
        nameof(U64LE) => U64LE.TryGetSerializedSize(out size),
        nameof(U128LE) => U128LE.TryGetSerializedSize(out size),
        nameof(I16LE) => I16LE.TryGetSerializedSize(out size),
        nameof(I32LE) => I32LE.TryGetSerializedSize(out size),
        nameof(I64LE) => I64LE.TryGetSerializedSize(out size),
        nameof(I128LE) => I128LE.TryGetSerializedSize(out size),
        nameof(F32BE) => F32BE.TryGetSerializedSize(out size),
        nameof(F32LE) => F32LE.TryGetSerializedSize(out size),
        nameof(F64BE) => F64BE.TryGetSerializedSize(out size),
        nameof(F64LE) => F64LE.TryGetSerializedSize(out size),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown fixed wrapper type."),
    };

    private static bool _TryParseFixedWrapper(string typeName, ReadOnlySpan<byte> source, out object? value)
    {
        switch (typeName)
        {
            case nameof(U16BE):
            {
                bool ok = U16BE.TryParse(source, out U16BE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(U32BE):
            {
                bool ok = U32BE.TryParse(source, out U32BE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(U64BE):
            {
                bool ok = U64BE.TryParse(source, out U64BE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(U128BE):
            {
                bool ok = U128BE.TryParse(source, out U128BE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(I16BE):
            {
                bool ok = I16BE.TryParse(source, out I16BE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(I32BE):
            {
                bool ok = I32BE.TryParse(source, out I32BE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(I64BE):
            {
                bool ok = I64BE.TryParse(source, out I64BE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(I128BE):
            {
                bool ok = I128BE.TryParse(source, out I128BE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(U16LE):
            {
                bool ok = U16LE.TryParse(source, out U16LE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(U32LE):
            {
                bool ok = U32LE.TryParse(source, out U32LE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(U64LE):
            {
                bool ok = U64LE.TryParse(source, out U64LE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(U128LE):
            {
                bool ok = U128LE.TryParse(source, out U128LE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(I16LE):
            {
                bool ok = I16LE.TryParse(source, out I16LE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(I32LE):
            {
                bool ok = I32LE.TryParse(source, out I32LE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(I64LE):
            {
                bool ok = I64LE.TryParse(source, out I64LE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(I128LE):
            {
                bool ok = I128LE.TryParse(source, out I128LE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(F32BE):
            {
                bool ok = F32BE.TryParse(source, out F32BE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(F32LE):
            {
                bool ok = F32LE.TryParse(source, out F32LE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(F64BE):
            {
                bool ok = F64BE.TryParse(source, out F64BE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            case nameof(F64LE):
            {
                bool ok = F64LE.TryParse(source, out F64LE parsed, out _);
                value = ok ? parsed.Value : null;
                return ok;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown fixed wrapper type.");
        }
    }

    private static string _ToStringFixedWrapper(string typeName, object value) => typeName switch
    {
        nameof(U16BE) => new U16BE((ushort)value).ToString(),
        nameof(U32BE) => new U32BE((uint)value).ToString(),
        nameof(U64BE) => new U64BE((ulong)value).ToString(),
        nameof(U128BE) => new U128BE((UInt128)value).ToString(),
        nameof(U16LE) => new U16LE((ushort)value).ToString(),
        nameof(U32LE) => new U32LE((uint)value).ToString(),
        nameof(U64LE) => new U64LE((ulong)value).ToString(),
        nameof(U128LE) => new U128LE((UInt128)value).ToString(),
        nameof(I16BE) => new I16BE((short)value).ToString(),
        nameof(I32BE) => new I32BE((int)value).ToString(),
        nameof(I64BE) => new I64BE((long)value).ToString(),
        nameof(I128BE) => new I128BE(_ParseInt128(value)).ToString(),
        nameof(I16LE) => new I16LE((short)value).ToString(),
        nameof(I32LE) => new I32LE((int)value).ToString(),
        nameof(I64LE) => new I64LE((long)value).ToString(),
        nameof(I128LE) => new I128LE(_ParseInt128(value)).ToString(),
        nameof(F32BE) => new F32BE((float)value).ToString(),
        nameof(F32LE) => new F32LE((float)value).ToString(),
        nameof(F64BE) => new F64BE((double)value).ToString(),
        nameof(F64LE) => new F64LE((double)value).ToString(),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unsupported ToString wrapper type."),
    };

    private static string _ToStringFixedWrapperWithFormat(string typeName, object value, string format, IFormatProvider provider) => typeName switch
    {
        nameof(U16BE) => new U16BE((ushort)value).ToString(format, provider),
        nameof(U32BE) => new U32BE((uint)value).ToString(format, provider),
        nameof(U64BE) => new U64BE((ulong)value).ToString(format, provider),
        nameof(U128BE) => new U128BE(_ParseUInt128(value)).ToString(format, provider),
        nameof(I16BE) => new I16BE((short)value).ToString(format, provider),
        nameof(I32BE) => new I32BE((int)value).ToString(format, provider),
        nameof(I64BE) => new I64BE((long)value).ToString(format, provider),
        nameof(I128BE) => new I128BE(_ParseInt128(value)).ToString(format, provider),
        nameof(U16LE) => new U16LE((ushort)value).ToString(format, provider),
        nameof(U32LE) => new U32LE((uint)value).ToString(format, provider),
        nameof(U64LE) => new U64LE((ulong)value).ToString(format, provider),
        nameof(U128LE) => new U128LE(_ParseUInt128(value)).ToString(format, provider),
        nameof(I16LE) => new I16LE((short)value).ToString(format, provider),
        nameof(I32LE) => new I32LE((int)value).ToString(format, provider),
        nameof(I64LE) => new I64LE((long)value).ToString(format, provider),
        nameof(I128LE) => new I128LE(_ParseInt128(value)).ToString(format, provider),
        nameof(F32BE) => new F32BE((float)value).ToString(format, provider),
        nameof(F32LE) => new F32LE((float)value).ToString(format, provider),
        nameof(F64BE) => new F64BE((double)value).ToString(format, provider),
        nameof(F64LE) => new F64LE((double)value).ToString(format, provider),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unsupported formatted ToString wrapper type."),
    };

    private static object _SampleValue(string typeName) => typeName switch
    {
        nameof(U16BE) or nameof(U16LE) => (ushort)1,
        nameof(U32BE) or nameof(U32LE) => 1u,
        nameof(U64BE) or nameof(U64LE) => 1UL,
        nameof(U128BE) => UInt128.One,
        nameof(U128LE) => UInt128.One,
        nameof(I16BE) or nameof(I16LE) => (short)1,
        nameof(I32BE) or nameof(I32LE) => 1,
        nameof(I64BE) or nameof(I64LE) => 1L,
        nameof(I128BE) => Int128.One,
        nameof(I128LE) => Int128.One,
        nameof(F32BE) or nameof(F32LE) => 1f,
        nameof(F64BE) or nameof(F64LE) => 1d,
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unknown fixed wrapper type."),
    };

    private static int _Write<T>(T wrapper, Span<byte> destination) where T : IUtf8SpanFormattable
    {
        if (!wrapper.TryFormat(destination, out int written, default, null))
        {
            throw new InvalidOperationException($"TryFormat failed for {typeof(T).Name}.");
        }

        return written;
    }

    private static object _ImplicitConvertFixedWrapper(string typeName, object rawValue) => typeName switch
    {
        nameof(U32BE) when rawValue is int intValue => (U32BE)intValue,
        nameof(U32BE) => (U32BE)(uint)rawValue,
        nameof(U64BE) when rawValue is long longValue => (U64BE)longValue,
        nameof(U64BE) => (U64BE)(ulong)rawValue,
        nameof(U128BE) => (U128BE)_ParseUInt128(rawValue),
        nameof(I16BE) => (I16BE)(short)rawValue,
        nameof(I32BE) => (I32BE)(int)rawValue,
        nameof(I64BE) => (I64BE)(long)rawValue,
        nameof(I128BE) => (I128BE)_ParseInt128(rawValue),
        nameof(U16LE) when rawValue is short shortValue => (U16LE)shortValue,
        nameof(U16LE) => (U16LE)(ushort)rawValue,
        nameof(U32LE) when rawValue is int intValue => (U32LE)intValue,
        nameof(U32LE) => (U32LE)(uint)rawValue,
        nameof(U64LE) when rawValue is long longValue => (U64LE)longValue,
        nameof(U64LE) => (U64LE)(ulong)rawValue,
        nameof(U128LE) => (U128LE)_ParseUInt128(rawValue),
        nameof(I16LE) => (I16LE)(short)rawValue,
        nameof(I32LE) => (I32LE)(int)rawValue,
        nameof(I64LE) => (I64LE)(long)rawValue,
        nameof(I128LE) => (I128LE)_ParseInt128(rawValue),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unsupported implicit conversion wrapper type."),
    };

    private static object _ReadFixedWrapperValue(string typeName, object wrapper) => typeName switch
    {
        nameof(U32BE) => ((U32BE)wrapper).Value,
        nameof(U64BE) => ((U64BE)wrapper).Value,
        nameof(U128BE) => ((U128BE)wrapper).Value,
        nameof(I16BE) => ((I16BE)wrapper).Value,
        nameof(I32BE) => ((I32BE)wrapper).Value,
        nameof(I64BE) => ((I64BE)wrapper).Value,
        nameof(I128BE) => ((I128BE)wrapper).Value,
        nameof(U16LE) => ((U16LE)wrapper).Value,
        nameof(U32LE) => ((U32LE)wrapper).Value,
        nameof(U64LE) => ((U64LE)wrapper).Value,
        nameof(U128LE) => ((U128LE)wrapper).Value,
        nameof(I16LE) => ((I16LE)wrapper).Value,
        nameof(I32LE) => ((I32LE)wrapper).Value,
        nameof(I64LE) => ((I64LE)wrapper).Value,
        nameof(I128LE) => ((I128LE)wrapper).Value,
        _ => throw new ArgumentOutOfRangeException(nameof(typeName), typeName, "Unsupported wrapper value read type."),
    };

    private static object _NormalizeExpectedValue(string typeName, object rawValue) => typeName switch
    {
        nameof(U32BE) when rawValue is int intValue => unchecked((uint)intValue),
        nameof(U64BE) when rawValue is long longValue => unchecked((ulong)longValue),
        nameof(U128BE) => _ParseUInt128(rawValue),
        nameof(I128BE) => _ParseInt128(rawValue),
        nameof(U16LE) when rawValue is short shortValue => unchecked((ushort)shortValue),
        nameof(U32LE) when rawValue is int intValue => unchecked((uint)intValue),
        nameof(U64LE) when rawValue is long longValue => unchecked((ulong)longValue),
        nameof(U128LE) => _ParseUInt128(rawValue),
        nameof(I128LE) => _ParseInt128(rawValue),
        _ => rawValue,
    };

    private static (int Size, byte[] Value) _CreateRawFromSpan(byte[] data)
    {
        ReadOnlySpan<byte> span = data;
        Raw raw = span;
        return (raw.Size, raw.Value.ToArray());
    }

    private static UInt128 _ParseUInt128(object value) => value switch
    {
        UInt128 uint128 => uint128,
        string text => UInt128.Parse(text, NumberStyles.Integer),
        _ => UInt128.Parse(value.ToString()!, NumberStyles.Integer),
    };

    private static Int128 _ParseInt128(object value) => value switch
    {
        Int128 int128 => int128,
        string text => Int128.Parse(text, NumberStyles.Integer),
        _ => Int128.Parse(value.ToString()!, NumberStyles.Integer),
    };
}
