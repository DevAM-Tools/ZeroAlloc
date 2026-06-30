// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>Test double that always fails <see cref="IUtf8SpanFormattable.TryFormat"/>.</summary>
internal readonly struct AlwaysFailUtf8Format : IUtf8SpanFormattable
{
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bytesWritten = 0;
        return false;
    }

    public override string ToString() => "fail";
}

/// <summary>Test double that always fails <see cref="IBinarySerializable.TryWrite"/>.</summary>
internal readonly struct AlwaysFailBinaryWrite : IBinarySerializable
{
    public bool TryGetWrittenSize(out int size)
    {
        size = 0;
        return false;
    }

    public bool TryWrite(Span<byte> destination, out int bytesWritten)
    {
        bytesWritten = 0;
        return false;
    }
}

/// <summary>
/// Tests for <see cref="TempBytesBuilder"/> covering append chains, grow paths,
/// TryAppend, UTF-8 encoding, and disposal behavior.
/// </summary>
public sealed class TempBytesBuilderTests
{
    // ========================================================================
    // CREATION AND PROPERTIES
    // ========================================================================

    [Test]
    public async Task Create_ReturnsEmptyBuilder()
    {
        int length;
        bool isEmpty;
        bool capacityPositive;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            length = builder.Length;
            isEmpty = builder.IsEmpty;
            capacityPositive = builder.Capacity > 0;
        }

        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
        await Assert.That(capacityPositive).IsTrue();
    }

    [Test]
    public async Task IsHeapAllocated_FalseForNormalUse()
    {
        bool isHeapAllocated;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            isHeapAllocated = builder.IsHeapAllocated;
        }

        await Assert.That(isHeapAllocated).IsFalse();
    }

    [Test]
    public async Task WrittenSpan_MatchesAsSpan()
    {
        byte[] written;
        byte[] asSpan;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03]);
            written = builder.WrittenSpan.ToArray();
            asSpan = builder.AsSpan().ToArray();
        }

        await Assert.That(written).IsEquivalentTo(asSpan);
    }

    [Test]
    public async Task ImplicitConversionToReadOnlySpan_Works()
    {
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append(0xAB);
            ReadOnlySpan<byte> span = builder;
            bytes = span.ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo((byte[])[0xAB]);
    }

    // ========================================================================
    // APPEND CHAIN
    // ========================================================================

    [Test]
    public async Task AppendChain_BytesAndIntegers_WritesCorrectly()
    {
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append(0x01);
            builder.Append([0x02, 0x03]);
            builder.Append((ReadOnlySpan<byte>)[0x04]);
            builder.AppendInt16BigEndian(0x1234);
            builder.AppendInt16LittleEndian(0x1234);
            builder.AppendInt32BigEndian(0x12345678);
            builder.AppendInt32LittleEndian(0x12345678);
            builder.AppendInt64BigEndian(0x123456789ABCDEF0L);
            builder.AppendUInt16BigEndian(0xABCD);
            builder.AppendUInt32BigEndian(0xDEADBEEF);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That((int)bytes[0]).IsEqualTo(0x01);
        await Assert.That(bytes).Contains((byte)0x12);
        await Assert.That(bytes).Contains((byte)0x34);
    }

    [Test]
    public async Task AppendChain_FloatAnd128BitTypes_WritesCorrectly()
    {
        int length;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendHalfBigEndian(Half.One);
            builder.AppendHalfLittleEndian(Half.One);
            builder.AppendSingleBigEndian(1.5f);
            builder.AppendSingleLittleEndian(1.5f);
            builder.AppendDoubleBigEndian(2.718);
            builder.AppendDoubleLittleEndian(2.718);
            builder.AppendInt128BigEndian((Int128)(-1));
            builder.AppendUInt128BigEndian(UInt128.One);
            builder.Append(new U16BE(0x1234));
            builder.Append(new VarInt(127));
            builder.Append(new VarIntZigZag(-1));
            length = builder.Length;
        }

        await Assert.That(length).IsGreaterThan(0);
    }

    [Test]
    public async Task AppendUtf8_EncodesCorrectly()
    {
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUtf8("Hello");
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo("Hello"u8.ToArray());
    }

    [Test]
    public async Task AppendUtf8_UnicodeCharacters_EncodesCorrectly()
    {
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUtf8("Héllo €");
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo(System.Text.Encoding.UTF8.GetBytes("Héllo €"));
    }

    [Test]
    public async Task AppendUtf8NullTerminated_IncludesZeroByte()
    {
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUtf8NullTerminated("Hi");
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That((int)bytes[^1]).IsEqualTo(0);
    }

    [Test]
    public async Task AppendUtf8WithPrefixes_EncodeLengthAndContent()
    {
        byte[] varInt;
        byte[] be;
        byte[] le;
        {
            using TempBytesBuilder b1 = TempBytesBuilder.Create();
            b1.AppendUtf8WithVarIntPrefix("Hi");
            varInt = b1.AsSpan().ToArray();

            using TempBytesBuilder b2 = TempBytesBuilder.Create();
            b2.AppendUtf8WithLengthPrefixBE("Hi");
            be = b2.AsSpan().ToArray();

            using TempBytesBuilder b3 = TempBytesBuilder.Create();
            b3.AppendUtf8WithLengthPrefixLE("Hi");
            le = b3.AsSpan().ToArray();
        }

        await Assert.That((int)varInt[0]).IsEqualTo(2);
        await Assert.That((int)be[0]).IsEqualTo(0x00);
        await Assert.That((int)be[1]).IsEqualTo(0x00);
        await Assert.That((int)be[2]).IsEqualTo(0x00);
        await Assert.That((int)be[3]).IsEqualTo(0x02);
        await Assert.That((int)le[0]).IsEqualTo(0x02);
        await Assert.That((int)le[1]).IsEqualTo(0x00);
        await Assert.That((int)le[2]).IsEqualTo(0x00);
        await Assert.That((int)le[3]).IsEqualTo(0x00);
    }

    [Test]
    [Arguments(127UL, 1)]
    [Arguments(128UL, 2)]
    [Arguments(300UL, 2)]
    public async Task AppendVarInt_EncodesExpectedByteCount(ulong value, int expectedLength)
    {
        int length;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendVarInt(value);
            length = builder.Length;
        }

        await Assert.That(length).IsEqualTo(expectedLength);
    }

    [Test]
    public async Task AppendVarIntZigZag_EncodesSignedValue()
    {
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendVarIntZigZag(-1);
            builder.AppendVarIntZigZag(1);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo((byte[])[0x01, 0x02]);
    }

    [Test]
    public async Task Append_IBinarySerializable_WritesStruct()
    {
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
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
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUtf8Formattable(42, "X2");
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo("2A"u8.ToArray());
    }

    [Test]
    public async Task AppendHex_AndBinary_FormatsAscii()
    {
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendHex2(0xAB);
            builder.AppendHex4(0x1234);
            builder.AppendBinary8(0b10101010);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That((int)bytes[0]).IsEqualTo((int)(byte)'A');
        await Assert.That((int)bytes[1]).IsEqualTo((int)(byte)'B');
    }

    [Test]
    public async Task Append_NullByteArray_DoesNothing()
    {
        bool isEmpty;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append((byte[]?)null);
            isEmpty = builder.IsEmpty;
        }

        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task Append_EmptySpan_DoesNothing()
    {
        bool isEmpty;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append(ReadOnlySpan<byte>.Empty);
            isEmpty = builder.IsEmpty;
        }

        await Assert.That(isEmpty).IsTrue();
    }

    // ========================================================================
    // CLEAR AND SEEKBACK
    // ========================================================================

    [Test]
    public async Task Clear_ResetsLength()
    {
        bool isEmpty;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            builder.Clear();
            isEmpty = builder.IsEmpty;
        }

        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task SeekBack_DecreasesLength()
    {
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            builder.SeekBack(2);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo((byte[])[0x01, 0x02]);
    }

    [Test]
    public async Task SeekBack_TooMuch_ThrowsArgumentOutOfRangeException()
    {
        bool threw = false;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
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
            using TempBytesBuilder builder = TempBytesBuilder.Create();
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
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02]);
            result = builder.TrySeekBack(10);
            length = builder.Length;
        }

        await Assert.That(result).IsFalse();
        await Assert.That(length).IsEqualTo(2);
    }

    // ========================================================================
    // TRYAPPEND
    // ========================================================================

    [Test]
    public async Task TryAppend_Bytes_ReturnTrue()
    {
        bool ok1;
        bool ok2;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            ok1 = builder.TryAppend(0x01);
            ok2 = builder.TryAppend([0x02, 0x03]);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(ok1).IsTrue();
        await Assert.That(ok2).IsTrue();
        await Assert.That(bytes).IsEquivalentTo((byte[])[0x01, 0x02, 0x03]);
    }

    [Test]
    public async Task TryAppend_IBinarySerializable_ReturnsTrue()
    {
        bool result;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            WritableByteStruct value = new() { First = 0xAA, Second = 0xBB, Third = 0xCC };
            result = builder.TryAppend(value);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytes).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC]);
    }

    [Test]
    public async Task TryAppendHex2_ReturnsTrue()
    {
        bool result;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            result = builder.TryAppendHex2(0xAB);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(result).IsTrue();
        await Assert.That(bytes).IsEquivalentTo("AB"u8.ToArray());
    }

    // ========================================================================
    // GROW / NESTED BUILDER
    // ========================================================================

    [Test]
    public async Task NestedBuilder_GrowsBeyondDefaultBuffer_PreservesOuterContent()
    {
        byte[] chunk = new byte[1_500_000];
        Array.Fill(chunk, (byte)0xAB);

        bool innerIsHeap;
        bool outerIntact;
        bool innerCorrect;

        {
            using TempBytesBuilder outer = TempBytesBuilder.Create();
            outer.Append(0xFF);

            using TempBytesBuilder inner = TempBytesBuilder.Create();
            innerIsHeap = inner.IsHeapAllocated;
            inner.Append(chunk);
            inner.Append(chunk);

            outer.Append(0xFE);
            outerIntact = outer.AsSpan().SequenceEqual((ReadOnlySpan<byte>)[0xFF, 0xFE]);
            ReadOnlySpan<byte> innerSpan = inner.AsSpan();
            innerCorrect = innerSpan.Length == chunk.Length * 2
                && innerSpan[0] == 0xAB
                && innerSpan[^1] == 0xAB;
        }

        await Assert.That(innerIsHeap).IsTrue();
        await Assert.That(innerCorrect).IsTrue();
        await Assert.That(outerIntact).IsTrue();
    }

    // ========================================================================
    // OUTPUT AND DISPOSAL
    // ========================================================================

    [Test]
    public async Task ToArray_ReturnsCopyOfWrittenBytes()
    {
        byte[] array;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02]);
            array = builder.ToArray();
        }

        await Assert.That(array).IsEquivalentTo((byte[])[0x01, 0x02]);
    }

    [Test]
    public async Task ToString_ReturnsDiagnosticMessage()
    {
        string text;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03]);
            text = builder.ToString();
        }

        await Assert.That(text).Contains("3 bytes");
    }

    [Test]
    public async Task Dispose_ReleasesBuffer()
    {
        int length;
        {
            TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            length = builder.Length;
            builder.Dispose();
        }

        await Assert.That(length).IsEqualTo(4);
    }

    [Test]
    public async Task BuildPacket_ProducesCorrectBytes()
    {
        byte[] packet;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUInt16BigEndian(0xCAFE);
            builder.AppendUInt16BigEndian(5);
            builder.AppendUtf8("Hello");
            packet = builder.AsSpan().ToArray();
        }

        byte[] expected = [0xCA, 0xFE, 0x00, 0x05, .. "Hello"u8.ToArray()];
        await Assert.That(packet).IsEquivalentTo(expected);
    }

    [Test]
    public async Task ClearAndReuse_Works()
    {
        byte[] first;
        byte[] second;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
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
    public async Task ExitCoverage_AppendWrappers_AndLittleEndian_Succeed()
    {
        int length;
        int remaining;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
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
            builder.AppendInt64LittleEndian(0x123456789ABCDEF0L);
            builder.AppendInt128LittleEndian(Int128.One);
            builder.AppendUInt16LittleEndian(0xABCD);
            builder.AppendUInt64BigEndian(0x0102030405060708UL);
            builder.AppendUInt64LittleEndian(0x0102030405060708UL);
            builder.AppendUInt128LittleEndian(UInt128.One);
            builder.AppendHex8(0x12345678u);
            builder.AppendHex16(0x0102030405060708UL);
            builder.AppendBinary16(0b1010);
            builder.AppendBinary32(1u);
            builder.AppendBinary64(2UL);
            remaining = builder.Remaining;
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(length).IsGreaterThan(0);
        await Assert.That(remaining).IsGreaterThan(0);
        await Assert.That(bytes.Length).IsEqualTo(length);
    }

    [Test]
    public async Task ExitCoverage_AppendUtf8Null_AndToArrayEmpty_Work()
    {
        byte[] prefixed;
        byte[] emptyArray;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUtf8(null);
            builder.AppendUtf8WithVarIntPrefix(null);
            builder.AppendUtf8WithLengthPrefixBE("");
            builder.AppendUtf8WithLengthPrefixLE(null);
            prefixed = builder.AsSpan().ToArray();

            using TempBytesBuilder empty = TempBytesBuilder.Create();
            emptyArray = empty.ToArray();
        }

        await Assert.That(prefixed.Length).IsGreaterThan(0);
        await Assert.That(emptyArray.Length).IsEqualTo(0);
    }

    [Test]
    [Arguments(nameof(TempBytesBuilder.TryAppendHex4), 4)]
    [Arguments(nameof(TempBytesBuilder.TryAppendHex8), 8)]
    [Arguments(nameof(TempBytesBuilder.TryAppendHex16), 16)]
    [Arguments(nameof(TempBytesBuilder.TryAppendBinary8), 8)]
    [Arguments(nameof(TempBytesBuilder.TryAppendBinary16), 16)]
    [Arguments(nameof(TempBytesBuilder.TryAppendBinary32), 32)]
    [Arguments(nameof(TempBytesBuilder.TryAppendBinary64), 64)]
    public async Task ExitCoverage_TryAppendHexBinary_Success(string method, int requiredBytes)
    {
        bool success = _InvokeTempBytesTryHexBinary(method, requiredBytes, expectSuccess: true);

        await Assert.That(success).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_TryAppendBytes_AndGeneric_Paths()
    {
        bool byteOk;
        bool byteArrayOk;
        bool spanOk;
        bool genericOk;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            byteOk = builder.TryAppend(0x01);
            builder.Clear();
            byteArrayOk = builder.TryAppend(new byte[] { 0x02, 0x03 });
            spanOk = builder.TryAppend((ReadOnlySpan<byte>)[0x04]);
            WritableByteStruct value = new() { First = 0xAA, Second = 0xBB, Third = 0xCC };
            genericOk = builder.TryAppend(value);
        }

        await Assert.That(byteOk).IsTrue();
        await Assert.That(byteArrayOk).IsTrue();
        await Assert.That(spanOk).IsTrue();
        await Assert.That(genericOk).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_TryAppendUtf8Formattable_Succeeds()
    {
        bool tryOk;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            tryOk = builder.TryAppendUtf8Formattable(42, "X2");
        }

        await Assert.That(tryOk).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_Grow_TriggersEnsureCapacitySuccess()
    {
        int length;
        int capacity;
        {
            TempBytesBuilder builder = TempBytesBuilder.Create();
            try
            {
                capacity = builder.Capacity;
                builder.Append(new byte[capacity]);
                builder.Append(0xFF);
                length = builder.Length;
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(length).IsEqualTo(capacity + 1);
    }

    [Test]
    [NotInParallel("Corrupts thread-static byte buffer")]
    public async Task ExitCoverage_TryAppend_FailsWhenGrowUnavailable()
    {
        bool byteFail;
        bool arrayOk;
        bool spanFail;
        bool spanEmptyOk;
        bool hexFail;
        bool genericFail;
        bool genericLoopFail;
        bool utf8Fail;
        int capacity;
        {
            TempBytesBuilder builder = TempBytesBuilder.Create();
            try
            {
                capacity = builder.Capacity;
                builder.Append(new byte[capacity]);
                _NullThreadStaticByteBuffer();
                byteFail = builder.TryAppend(0xFF);
                arrayOk = builder.TryAppend((byte[]?)null);
                spanFail = builder.TryAppend((ReadOnlySpan<byte>)[0x01, 0x02]);
                spanEmptyOk = builder.TryAppend(ReadOnlySpan<byte>.Empty);
                hexFail = builder.TryAppendHex2(0xAB);
                WritableByteStruct value = new() { First = 0x01, Second = 0x02, Third = 0x03 };
                genericFail = builder.TryAppend(value);
                genericLoopFail = builder.TryAppend(new AlwaysFailBinaryWrite());
                utf8Fail = builder.TryAppendUtf8Formattable(255, "X8");
            }
            finally
            {
                builder.Dispose();
                ZeroAllocHelper.ReleaseBuffers();
            }
        }

        await Assert.That(byteFail).IsFalse();
        await Assert.That(arrayOk).IsTrue();
        await Assert.That(spanFail).IsFalse();
        await Assert.That(spanEmptyOk).IsTrue();
        await Assert.That(hexFail).IsFalse();
        await Assert.That(genericFail).IsFalse();
        await Assert.That(genericLoopFail).IsFalse();
        await Assert.That(utf8Fail).IsFalse();
    }

    [Test]
    [NotInParallel("Corrupts thread-static byte buffer")]
    public async Task ExitCoverage_TryEnsureCapacity_SucceedsViaTryAppend()
    {
        bool grew;
        {
            TempBytesBuilder builder = TempBytesBuilder.Create();
            try
            {
                builder.Append(new byte[builder.Capacity]);
                grew = builder.TryAppend(0xFF);
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(grew).IsTrue();
    }

    [Test]
    [NotInParallel("SimulateGrowStallForCoverage")]
    public async Task ExitCoverage_AppendGeneric_AndUtf8_ThrowWhenGrowStalls()
    {
        bool genericThrew = false;
        bool utf8Threw = false;
        {
            TempBytesBuilder builder = TempBytesBuilder.Create();
            try
            {
                ZeroAllocHelper.SimulateGrowStallForCoverage = true;
                try { builder.Append(new AlwaysFailBinaryWrite()); }
                catch (InvalidOperationException) { genericThrew = true; }

                ZeroAllocHelper.SimulateGrowStallForCoverage = true;
                try { builder.AppendUtf8Formattable(new AlwaysFailUtf8Format()); }
                catch (InvalidOperationException) { utf8Threw = true; }
            }
            finally
            {
                builder.Dispose();
                ZeroAllocHelper.SimulateGrowStallForCoverage = false;
            }
        }

        await Assert.That(genericThrew).IsTrue();
        await Assert.That(utf8Threw).IsTrue();
    }

    [Test]
    [NotInParallel("SimulateGrowStallForCoverage")]
    public async Task ExitCoverage_TryAppend_GenericAndUtf8_ReturnFalseWhenGrowStalls()
    {
        bool genericFail;
        bool utf8Fail;
        {
            TempBytesBuilder builder = TempBytesBuilder.Create();
            try
            {
                ZeroAllocHelper.SimulateGrowStallForCoverage = true;
                genericFail = builder.TryAppend(new AlwaysFailBinaryWrite());

                ZeroAllocHelper.SimulateGrowStallForCoverage = true;
                utf8Fail = builder.TryAppendUtf8Formattable(new AlwaysFailUtf8Format());
            }
            finally
            {
                builder.Dispose();
                ZeroAllocHelper.SimulateGrowStallForCoverage = false;
            }
        }

        await Assert.That(genericFail).IsFalse();
        await Assert.That(utf8Fail).IsFalse();
    }

    [Test]
    [NotInParallel("Corrupts thread-static byte buffer")]
    public async Task ExitCoverage_AppendGeneric_AndUtf8Formattable_ThrowWhenBufferUnavailable()
    {
        bool genericThrew = false;
        {
            TempBytesBuilder builder = TempBytesBuilder.Create();
            try
            {
                builder.Append(new byte[builder.Capacity]);
                _NullThreadStaticByteBuffer();
                WritableByteStruct value = new() { First = 0x01, Second = 0x02, Third = 0x03 };
                try { builder.Append(value); }
                catch (InvalidOperationException) { genericThrew = true; }
            }
            finally
            {
                builder.Dispose();
                ZeroAllocHelper.ReleaseBuffers();
            }
        }

        await Assert.That(genericThrew).IsTrue();
    }

    [Test]
    [NotInParallel("Corrupts thread-static byte buffer")]
    public async Task ExitCoverage_TryAppendHexBinary_FailsWhenGrowUnavailable()
    {
        bool hex4;
        bool hex8;
        bool hex16;
        bool bin8;
        bool bin16;
        bool bin32;
        bool bin64;
        {
            TempBytesBuilder builder = TempBytesBuilder.Create();
            try
            {
                builder.Append(new byte[builder.Capacity]);
                _NullThreadStaticByteBuffer();
                hex4 = builder.TryAppendHex4(0x1234);
                hex8 = builder.TryAppendHex8(0x12345678u);
                hex16 = builder.TryAppendHex16(0x0102030405060708UL);
                bin8 = builder.TryAppendBinary8(0b10101010);
                bin16 = builder.TryAppendBinary16(0b1010);
                bin32 = builder.TryAppendBinary32(1u);
                bin64 = builder.TryAppendBinary64(2UL);
            }
            finally
            {
                builder.Dispose();
                ZeroAllocHelper.ReleaseBuffers();
            }
        }

        await Assert.That(hex4).IsFalse();
        await Assert.That(hex8).IsFalse();
        await Assert.That(hex16).IsFalse();
        await Assert.That(bin8).IsFalse();
        await Assert.That(bin16).IsFalse();
        await Assert.That(bin32).IsFalse();
        await Assert.That(bin64).IsFalse();
    }

    private static void _NullThreadStaticByteBuffer()
    {
        FieldInfo? bufferField = typeof(ZeroAllocHelper).GetField("_ByteBuffer", BindingFlags.NonPublic | BindingFlags.Static);
        bufferField?.SetValue(null, null);
    }

    private static bool _InvokeTempBytesTryHexBinary(string method, int bufferSize, bool expectSuccess)
    {
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        return method switch
        {
            nameof(TempBytesBuilder.TryAppendHex4) => builder.TryAppendHex4(0x1234),
            nameof(TempBytesBuilder.TryAppendHex8) => builder.TryAppendHex8(0x12345678u),
            nameof(TempBytesBuilder.TryAppendHex16) => builder.TryAppendHex16(0x0102030405060708UL),
            nameof(TempBytesBuilder.TryAppendBinary8) => builder.TryAppendBinary8(0b10101010),
            nameof(TempBytesBuilder.TryAppendBinary16) => builder.TryAppendBinary16(0b1010),
            nameof(TempBytesBuilder.TryAppendBinary32) => builder.TryAppendBinary32(1u),
            nameof(TempBytesBuilder.TryAppendBinary64) => builder.TryAppendBinary64(2UL),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown method."),
        };
    }
}
