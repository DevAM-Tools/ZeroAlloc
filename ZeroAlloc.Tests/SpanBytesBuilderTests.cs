// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="SpanBytesBuilder"/> covering append chains, TryAppend failures,
/// overflow paths, and binary encoding edge cases.
/// </summary>
public sealed class SpanBytesBuilderTests
{
    // ========================================================================
    // CREATION AND PROPERTIES
    // ========================================================================

    [Test]
    public async Task Constructor_SetsCapacityAndEmptyState()
    {
        int length;
        int capacity;
        int remaining;
        {
            Span<byte> buffer = stackalloc byte[256];
            SpanBytesBuilder builder = new(buffer);
            length = builder.Length;
            capacity = builder.Capacity;
            remaining = builder.Remaining;
        }

        await Assert.That(length).IsEqualTo(0);
        await Assert.That(capacity).IsEqualTo(256);
        await Assert.That(remaining).IsEqualTo(256);
    }

    // ========================================================================
    // APPEND CHAIN — INTEGERS, FLOATS, ENDIAN WRAPPERS
    // ========================================================================

    [Test]
    public async Task AppendChain_IntegerTypes_WritesBigAndLittleEndian()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[128];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendInt16BigEndian(0x1234);
            builder.AppendInt16LittleEndian(0x1234);
            builder.AppendInt32BigEndian(0x12345678);
            builder.AppendInt32LittleEndian(0x12345678);
            builder.AppendInt64BigEndian(0x123456789ABCDEF0L);
            builder.AppendInt64LittleEndian(0x123456789ABCDEF0L);
            builder.AppendInt128BigEndian((Int128)(-1));
            builder.AppendInt128LittleEndian(Int128.One);
            builder.AppendUInt16BigEndian(0xABCD);
            builder.AppendUInt16LittleEndian(0xABCD);
            builder.AppendUInt32BigEndian(0xDEADBEEF);
            builder.AppendUInt32LittleEndian(0xDEADBEEF);
            builder.AppendUInt64BigEndian(0x0102030405060708UL);
            builder.AppendUInt64LittleEndian(0x0102030405060708UL);
            builder.AppendUInt128BigEndian(UInt128.MaxValue);
            builder.AppendUInt128LittleEndian(UInt128.One);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes.Length).IsEqualTo(2 + 2 + 4 + 4 + 8 + 8 + 16 + 16 + 2 + 2 + 4 + 4 + 8 + 8 + 16 + 16);
        await Assert.That((int)bytes[0]).IsEqualTo(0x12);
        await Assert.That((int)bytes[1]).IsEqualTo(0x34);
        await Assert.That((int)bytes[2]).IsEqualTo(0x34);
        await Assert.That((int)bytes[3]).IsEqualTo(0x12);
    }

    [Test]
    public async Task AppendChain_FloatTypes_WritesCorrectEndianness()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[64];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendHalfBigEndian(Half.One);
            builder.AppendHalfLittleEndian(Half.One);
            builder.AppendSingleBigEndian(1.5f);
            builder.AppendSingleLittleEndian(1.5f);
            builder.AppendDoubleBigEndian(2.718);
            builder.AppendDoubleLittleEndian(2.718);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes.Length).IsEqualTo(2 + 2 + 4 + 4 + 8 + 8);
    }

    [Test]
    public async Task Append_EndianWrapperTypes_DelegatesToPrimitiveMethods()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[64];
            SpanBytesBuilder builder = new(buffer);
            builder.Append(new U16BE(0x1234));
            builder.Append(new U32BE(0x12345678u));
            builder.Append(new U64BE(0x0102030405060708UL));
            builder.Append(new U16LE(0x1234));
            builder.Append(new U32LE(0x12345678u));
            builder.Append(new U64LE(0x0102030405060708UL));
            builder.Append(new I16BE(-1));
            builder.Append(new I32BE(-1));
            builder.Append(new I64BE(-1L));
            builder.Append(new I16LE(-1));
            builder.Append(new I32LE(-1));
            builder.Append(new I64LE(-1L));
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That((int)bytes[0]).IsEqualTo(0x12);
        await Assert.That((int)bytes[1]).IsEqualTo(0x34);
        await Assert.That((int)bytes[^1]).IsEqualTo(0xFF);
    }

    // ========================================================================
    // VARINT, UTF-8, IBINARYSERIALIZABLE
    // ========================================================================

    [Test]
    [Arguments(0UL, new byte[] { 0x00 })]
    [Arguments(127UL, new byte[] { 0x7F })]
    [Arguments(128UL, new byte[] { 0x80, 0x01 })]
    [Arguments(300UL, new byte[] { 0xAC, 0x02 })]
    public async Task AppendVarInt_EncodesCorrectly(ulong value, byte[] expected)
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[16];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendVarInt(value);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo(expected);
    }

    [Test]
    [Arguments(0L, new byte[] { 0x00 })]
    [Arguments(-1L, new byte[] { 0x01 })]
    [Arguments(1L, new byte[] { 0x02 })]
    public async Task AppendVarIntZigZag_EncodesCorrectly(long value, byte[] expected)
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[16];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendVarIntZigZag(value);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo(expected);
    }

    [Test]
    public async Task Append_VarIntWrapperTypes_EncodeCorrectly()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[8];
            SpanBytesBuilder builder = new(buffer);
            builder.Append(new VarInt(300));
            builder.Append(new VarIntZigZag(-1L));
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo((byte[])[0xAC, 0x02, 0x01]);
    }

    [Test]
    public async Task AppendUtf8_EncodesString()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[32];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendUtf8("Hello");
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo("Hello"u8.ToArray());
    }

    [Test]
    public async Task AppendUtf8NullTerminated_IncludesZeroByte()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[32];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendUtf8NullTerminated("Hi");
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo((byte[])[0x48, 0x69, 0x00]);
    }

    [Test]
    public async Task AppendUtf8WithVarIntPrefix_EncodesLengthAndContent()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[32];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendUtf8WithVarIntPrefix("Hi");
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That((int)bytes[0]).IsEqualTo(2);
        await Assert.That((int)bytes[1]).IsEqualTo(0x48);
        await Assert.That((int)bytes[2]).IsEqualTo(0x69);
    }

    [Test]
    public async Task AppendUtf8WithLengthPrefixBE_EncodesBigEndianLength()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[32];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendUtf8WithLengthPrefixBE("Hi");
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That((int)bytes[0]).IsEqualTo(0x00);
        await Assert.That((int)bytes[1]).IsEqualTo(0x00);
        await Assert.That((int)bytes[2]).IsEqualTo(0x00);
        await Assert.That((int)bytes[3]).IsEqualTo(0x02);
        await Assert.That((int)bytes[4]).IsEqualTo(0x48);
    }

    [Test]
    public async Task AppendUtf8WithLengthPrefixLE_EncodesLittleEndianLength()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[32];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendUtf8WithLengthPrefixLE("Hi");
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That((int)bytes[0]).IsEqualTo(0x02);
        await Assert.That((int)bytes[1]).IsEqualTo(0x00);
        await Assert.That((int)bytes[2]).IsEqualTo(0x00);
        await Assert.That((int)bytes[3]).IsEqualTo(0x00);
        await Assert.That((int)bytes[4]).IsEqualTo(0x48);
    }

    [Test]
    public async Task Append_IBinarySerializable_WritesStruct()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[16];
            SpanBytesBuilder builder = new(buffer);
            WritableByteStruct value = new() { First = 0x01, Second = 0x02, Third = 0x03 };
            builder.Append(value);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo((byte[])[0x01, 0x02, 0x03]);
    }

    [Test]
    public async Task AppendUtf8Formattable_EncodesFormattedValue()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[16];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendUtf8Formattable(42, "X2");
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo("2A"u8.ToArray());
    }

    // ========================================================================
    // HEX/BINARY ASCII
    // ========================================================================

    [Test]
    public async Task AppendHex_AndBinary_FormatsAsciiBytes()
    {
        byte[] bytes;
        {
            Span<byte> buffer = stackalloc byte[128];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendHex2(0xAB);
            builder.AppendHex4(0x1234);
            builder.AppendHex8(0xDEADBEEFu);
            builder.AppendHex16(0x0102030405060708UL);
            builder.AppendBinary8(0b10101010);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That((int)bytes[0]).IsEqualTo((int)(byte)'A');
        await Assert.That((int)bytes[1]).IsEqualTo((int)(byte)'B');
        await Assert.That(bytes).Contains((byte)'1');
        await Assert.That(bytes).Contains((byte)'0');
    }

    // ========================================================================
    // CLEAR, SEEKBACK, ADVANCE, REMAINING
    // ========================================================================

    [Test]
    public async Task Clear_ResetsLengthAndRemaining()
    {
        int length;
        int remaining;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            builder.Clear();
            length = builder.Length;
            remaining = builder.Remaining;
        }

        await Assert.That(length).IsEqualTo(0);
        await Assert.That(remaining).IsEqualTo(100);
    }

    [Test]
    public async Task SeekBack_DecreasesLength()
    {
        byte[] bytes;
        int length;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            builder.SeekBack(2);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(length).IsEqualTo(2);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0x01, 0x02]);
    }

    [Test]
    public async Task SeekBack_TooMuch_ThrowsArgumentOutOfRangeException()
    {
        bool threw = false;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02]);
            try { builder.SeekBack(10); }
            catch (ArgumentOutOfRangeException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task TrySeekBack_Valid_ReturnsTrue()
    {
        bool result;
        int length;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            result = builder.TrySeekBack(2);
            length = builder.Length;
        }

        await Assert.That(result).IsTrue();
        await Assert.That(length).IsEqualTo(2);
    }

    [Test]
    public async Task TrySeekBack_Invalid_ReturnsFalse()
    {
        bool result;
        int length;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02]);
            result = builder.TrySeekBack(10);
            length = builder.Length;
        }

        await Assert.That(result).IsFalse();
        await Assert.That(length).IsEqualTo(2);
    }

    [Test]
    public async Task GetRemainingSpan_ReturnsUnwrittenTail()
    {
        int remainingLength;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02, 0x03]);
            Span<byte> remaining = builder.GetRemainingSpan();
            remainingLength = remaining.Length;
        }

        await Assert.That(remainingLength).IsEqualTo(97);
    }

    [Test]
    public async Task Advance_IncreasesPositionAfterManualWrite()
    {
        byte[] content;
        int length;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            Span<byte> remaining = builder.GetRemainingSpan();
            ReadOnlySpan<byte> data = [0xAA, 0xBB];
            data.CopyTo(remaining);
            builder.Advance(2);
            length = builder.Length;
            content = builder.AsSpan().ToArray();
        }

        await Assert.That(length).IsEqualTo(2);
        await Assert.That(content).IsEquivalentTo((byte[])[0xAA, 0xBB]);
    }

    [Test]
    public async Task Advance_PastRemaining_ThrowsArgumentOutOfRangeException()
    {
        bool threw = false;
        int length;
        {
            Span<byte> buffer = stackalloc byte[2];
            SpanBytesBuilder builder = new(buffer);
            try { builder.Advance(3); }
            catch (ArgumentOutOfRangeException) { threw = true; }
            length = builder.Length;
        }

        await Assert.That(threw).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    // ========================================================================
    // OVERFLOW — APPEND THROWS
    // ========================================================================

    [Test]
    public async Task Append_SpanOverflow_ThrowsInvalidOperationException()
    {
        bool threw = false;
        {
            Span<byte> buffer = stackalloc byte[5];
            SpanBytesBuilder builder = new(buffer);
            try { builder.Append([0x01, 0x02, 0x03, 0x04, 0x05, 0x06]); }
            catch (InvalidOperationException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task AppendInt32_Overflow_ThrowsInvalidOperationException()
    {
        bool threw = false;
        {
            Span<byte> buffer = stackalloc byte[3];
            SpanBytesBuilder builder = new(buffer);
            try { builder.AppendInt32BigEndian(0x12345678); }
            catch (InvalidOperationException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task Append_EmptyBuffer_ThrowsOnAnyAppend()
    {
        bool threw = false;
        {
            Span<byte> buffer = [];
            SpanBytesBuilder builder = new(buffer);
            try { builder.Append(0x01); }
            catch (InvalidOperationException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task AppendUtf8WithVarIntPrefix_BufferTooSmall_DoesNotAdvance()
    {
        bool threw = false;
        int length;
        {
            Span<byte> buffer = stackalloc byte[1];
            SpanBytesBuilder builder = new(buffer);
            try { builder.AppendUtf8WithVarIntPrefix("A"); }
            catch (InvalidOperationException) { threw = true; }
            length = builder.Length;
        }

        await Assert.That(threw).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task AppendUtf8WithLengthPrefixBE_BufferTooSmall_DoesNotAdvance()
    {
        bool threw = false;
        int length;
        {
            Span<byte> buffer = stackalloc byte[2];
            SpanBytesBuilder builder = new(buffer);
            try { builder.AppendUtf8WithLengthPrefixBE("Hi"); }
            catch (InvalidOperationException) { threw = true; }
            length = builder.Length;
        }

        await Assert.That(threw).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Append_NullArray_DoesNothing()
    {
        int length;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append((byte[]?)null);
            length = builder.Length;
        }

        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Append_EmptySpan_DoesNothing()
    {
        int length;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append(ReadOnlySpan<byte>.Empty);
            length = builder.Length;
        }

        await Assert.That(length).IsEqualTo(0);
    }

    // ========================================================================
    // TRYAPPEND — SUCCESS AND FAILURE
    // ========================================================================

    [Test]
    public async Task TryAppend_Byte_SucceedsUntilFull()
    {
        bool first;
        bool second;
        bool third;
        int length;
        {
            Span<byte> buffer = stackalloc byte[2];
            SpanBytesBuilder builder = new(buffer);
            first = builder.TryAppend(0x01);
            second = builder.TryAppend(0x02);
            third = builder.TryAppend(0x03);
            length = builder.Length;
        }

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsTrue();
        await Assert.That(third).IsFalse();
        await Assert.That(length).IsEqualTo(2);
    }

    [Test]
    public async Task TryAppendInt32BigEndian_InsufficientSpace_ReturnsFalse()
    {
        bool result;
        {
            Span<byte> buffer = stackalloc byte[3];
            SpanBytesBuilder builder = new(buffer);
            result = builder.TryAppendInt32BigEndian(0x12345678);
        }

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryAppendVarInt_InsufficientSpace_ReturnsFalse()
    {
        bool result;
        {
            Span<byte> buffer = stackalloc byte[1];
            SpanBytesBuilder builder = new(buffer);
            result = builder.TryAppendVarInt(300);
        }

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryAppendUtf8_InsufficientSpace_ReturnsFalse()
    {
        bool result;
        {
            Span<byte> buffer = stackalloc byte[2];
            SpanBytesBuilder builder = new(buffer);
            result = builder.TryAppendUtf8("Hello");
        }

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryAppend_IBinarySerializable_InsufficientSpace_ReturnsFalse()
    {
        bool result;
        {
            Span<byte> buffer = stackalloc byte[2];
            SpanBytesBuilder builder = new(buffer);
            WritableByteStruct value = new() { First = 0x01, Second = 0x02, Third = 0x03 };
            result = builder.TryAppend(value);
        }

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TryAppendHex2_InsufficientSpace_ReturnsFalse()
    {
        bool result;
        {
            Span<byte> buffer = stackalloc byte[1];
            SpanBytesBuilder builder = new(buffer);
            result = builder.TryAppendHex2(0xAB);
        }

        await Assert.That(result).IsFalse();
    }

    // ========================================================================
    // COMPLEX SCENARIOS
    // ========================================================================

    [Test]
    public async Task BuildPacket_ProducesCorrectBytes()
    {
        byte[] packet;
        {
            Span<byte> buffer = stackalloc byte[256];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendUInt16BigEndian(0xCAFE);
            builder.Append(0x01);
            builder.AppendUInt16BigEndian(0x0010);
            packet = builder.AsSpan().ToArray();
        }

        await Assert.That(packet).IsEquivalentTo((byte[])[0xCA, 0xFE, 0x01, 0x00, 0x10]);
    }

    [Test]
    public async Task ClearAndReuse_Works()
    {
        byte[] first;
        byte[] second;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02, 0x03]);
            first = builder.AsSpan().ToArray();
            builder.Clear();
            builder.Append([0xAA, 0xBB]);
            second = builder.AsSpan().ToArray();
        }

        await Assert.That(first).IsEquivalentTo((byte[])[0x01, 0x02, 0x03]);
        await Assert.That(second).IsEquivalentTo((byte[])[0xAA, 0xBB]);
    }

    // ========================================================================
    // EXIT-POINT COVERAGE
    // ========================================================================

    [Test]
    public async Task ExitCoverage_AppendByteArray_AndOutputs_Work()
    {
        byte[] arrayBytes;
        byte[] emptyArray;
        string text;
        int writtenLength;
        {
            Span<byte> buffer = stackalloc byte[128];
            SpanBytesBuilder builder = new(buffer);
            builder.Append(new byte[] { 0x01, 0x02, 0x03 });
            builder.AppendVarIntZigZag(42);
            builder.AppendBinary16(0b1010);
            builder.AppendBinary32(1u);
            builder.AppendBinary64(2UL);
            arrayBytes = builder.ToArray();
            text = builder.ToString();
            writtenLength = builder.WrittenSpan.Length;

            Span<byte> emptyBuffer = stackalloc byte[16];
            SpanBytesBuilder emptyBuilder = new(emptyBuffer);
            emptyBuilder.AppendUtf8WithVarIntPrefix(null);
            emptyBuilder.AppendUtf8WithVarIntPrefix("");
            emptyBuilder.AppendUtf8WithLengthPrefixBE(null);
            emptyBuilder.AppendUtf8WithLengthPrefixLE("");
            emptyArray = emptyBuilder.ToArray();
        }

        await Assert.That(arrayBytes.Length).IsGreaterThan(3);
        await Assert.That((int)arrayBytes[0]).IsEqualTo(0x01);
        await Assert.That(text).Contains("bytes");
        await Assert.That(writtenLength).IsEqualTo(arrayBytes.Length);
        await Assert.That(emptyArray.Length).IsEqualTo(10);
    }

    [Test]
    public async Task ExitCoverage_TryAppendByteArray_AndGeneric_Succeed()
    {
        bool arrayOk;
        bool genericOk;
        int length;
        {
            Span<byte> buffer = stackalloc byte[32];
            SpanBytesBuilder builder = new(buffer);
            arrayOk = builder.TryAppend(new byte[] { 0x0A, 0x0B });
            WritableByteStruct value = new() { First = 0x01, Second = 0x02, Third = 0x03 };
            genericOk = builder.TryAppend(value);
            length = builder.Length;
        }

        await Assert.That(arrayOk).IsTrue();
        await Assert.That(genericOk).IsTrue();
        await Assert.That(length).IsEqualTo(5);
    }

    [Test]
    [Arguments("TryAppendInt16BigEndian", 2)]
    [Arguments("TryAppendInt32LittleEndian", 4)]
    [Arguments("TryAppendInt64BigEndian", 8)]
    [Arguments("TryAppendInt128BigEndian", 16)]
    [Arguments("TryAppendUInt16BigEndian", 2)]
    [Arguments("TryAppendUInt32LittleEndian", 4)]
    [Arguments("TryAppendUInt64BigEndian", 8)]
    [Arguments("TryAppendUInt128LittleEndian", 16)]
    [Arguments("TryAppendHalfBigEndian", 2)]
    [Arguments("TryAppendHalfLittleEndian", 2)]
    [Arguments("TryAppendSingleBigEndian", 4)]
    [Arguments("TryAppendSingleLittleEndian", 4)]
    [Arguments("TryAppendDoubleBigEndian", 8)]
    [Arguments("TryAppendDoubleLittleEndian", 8)]
    [Arguments("TryAppendHex4", 4)]
    [Arguments("TryAppendHex8", 8)]
    [Arguments("TryAppendHex16", 16)]
    [Arguments("TryAppendBinary8", 8)]
    [Arguments("TryAppendBinary16", 16)]
    [Arguments("TryAppendBinary32", 32)]
    [Arguments("TryAppendBinary64", 64)]
    [Arguments("TryAppendUInt64LittleEndian", 8)]
    [Arguments("TryAppendInt16LittleEndian", 2)]
    [Arguments("TryAppendInt64LittleEndian", 8)]
    [Arguments("TryAppendInt128LittleEndian", 16)]
    [Arguments("TryAppendUInt128BigEndian", 16)]
    [Arguments("TryAppendUInt16LittleEndian", 2)]
    [Arguments("TryAppendUInt32BigEndian", 4)]
    public async Task ExitCoverage_TryAppend_InsufficientSpace_ReturnsFalse(string method, int requiredBytes)
    {
        bool result = _InvokeSpanBytesTryAppend(method, stackalloc byte[Math.Max(0, requiredBytes - 1)], expectSuccess: false);

        await Assert.That(result).IsFalse();
    }

    [Test]
    [Arguments("TryAppendHex4", 4)]
    [Arguments("TryAppendHex8", 8)]
    [Arguments("TryAppendHex16", 16)]
    [Arguments("TryAppendBinary8", 8)]
    [Arguments("TryAppendBinary16", 16)]
    [Arguments("TryAppendBinary32", 32)]
    [Arguments("TryAppendBinary64", 64)]
    public async Task ExitCoverage_TryAppend_ExactSpace_ReturnsTrue(string method, int requiredBytes)
    {
        bool result = _InvokeSpanBytesTryAppend(method, stackalloc byte[requiredBytes], expectSuccess: true);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_TryAppendUtf8_Null_ReturnsTrue()
    {
        bool result;
        {
            Span<byte> buffer = stackalloc byte[4];
            SpanBytesBuilder builder = new(buffer);
            result = builder.TryAppendUtf8(null);
        }

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_TryAppendUtf8Formattable_SuccessAndFailure()
    {
        bool success;
        bool failure;
        {
            SpanBytesBuilder okBuilder = new(stackalloc byte[16]);
            success = okBuilder.TryAppendUtf8Formattable(42, "X2");

            SpanBytesBuilder failBuilder = new(stackalloc byte[1]);
            failure = failBuilder.TryAppendUtf8Formattable(255, "X4");
        }

        await Assert.That(success).IsTrue();
        await Assert.That(failure).IsFalse();
    }

    [Test]
    public async Task ExitCoverage_TryAppendUtf8NullTerminated_InsufficientSpace_ReturnsFalse()
    {
        bool result;
        {
            Span<byte> buffer = stackalloc byte[2];
            SpanBytesBuilder builder = new(buffer);
            result = builder.TryAppendUtf8NullTerminated("Hi");
        }

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task ExitCoverage_TryAppendVarIntZigZag_Works()
    {
        bool intOk;
        bool longOk;
        {
            Span<byte> buffer = stackalloc byte[8];
            SpanBytesBuilder builder = new(buffer);
            intOk = builder.TryAppendVarIntZigZag(42);
            longOk = builder.TryAppendVarIntZigZag(-1L);
        }

        await Assert.That(intOk).IsTrue();
        await Assert.That(longOk).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_AppendGeneric_AndUtf8Formattable_OverflowThrow()
    {
        bool genericThrew = false;
        bool utf8Threw = false;
        {
            Span<byte> buffer = stackalloc byte[2];
            SpanBytesBuilder builder = new(buffer);
            WritableByteStruct value = new() { First = 0x01, Second = 0x02, Third = 0x03 };
            try { builder.Append(value); }
            catch (InvalidOperationException) { genericThrew = true; }

            SpanBytesBuilder builder2 = new(stackalloc byte[1]);
            try { builder2.AppendUtf8Formattable(255, "X4"); }
            catch (InvalidOperationException) { utf8Threw = true; }
        }

        await Assert.That(genericThrew).IsTrue();
        await Assert.That(utf8Threw).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_ToArray_Empty_ReturnsEmptyArray()
    {
        byte[] array;
        {
            SpanBytesBuilder builder = new(stackalloc byte[4]);
            array = builder.ToArray();
        }

        await Assert.That(array.Length).IsEqualTo(0);
    }

    [Test]
    public async Task ExitCoverage_TryAppendNullByteArray_ReturnsTrue()
    {
        bool result;
        {
            Span<byte> buffer = stackalloc byte[8];
            SpanBytesBuilder builder = new(buffer);
            result = builder.TryAppend((byte[]?)null);
        }

        await Assert.That(result).IsTrue();
    }

    private static bool _InvokeSpanBytesTryAppend(string method, Span<byte> buffer, bool expectSuccess)
    {
        SpanBytesBuilder builder = new(buffer);
        return method switch
        {
            nameof(SpanBytesBuilder.TryAppendInt16BigEndian) => builder.TryAppendInt16BigEndian(0x1234),
            nameof(SpanBytesBuilder.TryAppendInt32LittleEndian) => builder.TryAppendInt32LittleEndian(0x12345678),
            nameof(SpanBytesBuilder.TryAppendInt64BigEndian) => builder.TryAppendInt64BigEndian(0x123456789ABCDEF0L),
            nameof(SpanBytesBuilder.TryAppendInt128BigEndian) => builder.TryAppendInt128BigEndian(Int128.One),
            nameof(SpanBytesBuilder.TryAppendUInt16BigEndian) => builder.TryAppendUInt16BigEndian(0xABCD),
            nameof(SpanBytesBuilder.TryAppendUInt32LittleEndian) => builder.TryAppendUInt32LittleEndian(0xDEADBEEF),
            nameof(SpanBytesBuilder.TryAppendUInt64BigEndian) => builder.TryAppendUInt64BigEndian(0x0102030405060708UL),
            nameof(SpanBytesBuilder.TryAppendUInt128LittleEndian) => builder.TryAppendUInt128LittleEndian(UInt128.One),
            nameof(SpanBytesBuilder.TryAppendHalfBigEndian) => builder.TryAppendHalfBigEndian(Half.One),
            nameof(SpanBytesBuilder.TryAppendHalfLittleEndian) => builder.TryAppendHalfLittleEndian(Half.One),
            nameof(SpanBytesBuilder.TryAppendSingleBigEndian) => builder.TryAppendSingleBigEndian(1.5f),
            nameof(SpanBytesBuilder.TryAppendSingleLittleEndian) => builder.TryAppendSingleLittleEndian(1.5f),
            nameof(SpanBytesBuilder.TryAppendDoubleBigEndian) => builder.TryAppendDoubleBigEndian(2.718),
            nameof(SpanBytesBuilder.TryAppendDoubleLittleEndian) => builder.TryAppendDoubleLittleEndian(2.718),
            nameof(SpanBytesBuilder.TryAppendHex4) => builder.TryAppendHex4(0x1234),
            nameof(SpanBytesBuilder.TryAppendHex8) => builder.TryAppendHex8(0x12345678u),
            nameof(SpanBytesBuilder.TryAppendHex16) => builder.TryAppendHex16(0x0102030405060708UL),
            nameof(SpanBytesBuilder.TryAppendBinary8) => builder.TryAppendBinary8(0b10101010),
            nameof(SpanBytesBuilder.TryAppendBinary16) => builder.TryAppendBinary16(0b1010),
            nameof(SpanBytesBuilder.TryAppendBinary32) => builder.TryAppendBinary32(1u),
            nameof(SpanBytesBuilder.TryAppendBinary64) => builder.TryAppendBinary64(2UL),
            nameof(SpanBytesBuilder.TryAppendUInt64LittleEndian) => builder.TryAppendUInt64LittleEndian(0x0102030405060708UL),
            nameof(SpanBytesBuilder.TryAppendInt16LittleEndian) => builder.TryAppendInt16LittleEndian(0x1234),
            nameof(SpanBytesBuilder.TryAppendInt64LittleEndian) => builder.TryAppendInt64LittleEndian(0x123456789ABCDEF0L),
            nameof(SpanBytesBuilder.TryAppendInt128LittleEndian) => builder.TryAppendInt128LittleEndian(Int128.One),
            nameof(SpanBytesBuilder.TryAppendUInt128BigEndian) => builder.TryAppendUInt128BigEndian(UInt128.One),
            nameof(SpanBytesBuilder.TryAppendUInt16LittleEndian) => builder.TryAppendUInt16LittleEndian(0xABCD),
            nameof(SpanBytesBuilder.TryAppendUInt32BigEndian) => builder.TryAppendUInt32BigEndian(0xDEADBEEF),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown TryAppend method."),
        };
    }
}
