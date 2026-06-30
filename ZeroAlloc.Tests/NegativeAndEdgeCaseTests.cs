// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// Negative and Edge Case Tests - Robustness Verification
// ============================================================================
// Tests for null handling, empty values, boundary conditions, dispose behavior,
// and other edge cases that might cause issues.
// ============================================================================

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for edge cases, error conditions, and negative scenarios.
/// </summary>
public sealed class NegativeAndEdgeCaseTests
{
    // ========================================================================
    // NULL STRING HANDLING
    // Tests for graceful null string handling
    // ========================================================================
    #region Null String Handling

    [Test]
    public async Task String_NullString_HandlesGracefully()
    {
        string? nullStr = null;
        int length;
        {
            using TempString temp = ZA.String(nullStr!);
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task String_MultipleNulls_HandlesGracefully()
    {
        string? null1 = null;
        string? null2 = null;
        int length;
        {
            using TempString temp = ZA.String(null1!, null2!);
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task String_NullBetweenValues_HandlesGracefully()
    {
        string? nullStr = null;
        string content;
        {
            using TempString temp = ZA.String("A", nullStr!, "B");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("AB");
    }

    [Test]
    public async Task Utf8_NullString_HandlesGracefully()
    {
        string? nullStr = null;
        int length;
        {
            using TempBytes temp = ZA.Utf8(nullStr!);
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(0);
    }

    #endregion

    // ========================================================================
    // EMPTY VALUE HANDLING
    // Tests for empty strings and empty collections
    // ========================================================================

    #region Empty Value Handling

    [Test]
    public async Task String_EmptyString_ReturnsEmpty()
    {
        int length;
        {
            using TempString temp = ZA.String("");
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task String_MultipleEmptyStrings_ReturnsEmpty()
    {
        int length;
        {
            using TempString temp = ZA.String("", "", "");
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task String_EmptyBetweenValues_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("A", "", "B");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("AB");
    }

    [Test]
    public async Task Utf8_EmptyString_ReturnsEmpty()
    {
        int length;
        {
            using TempBytes temp = ZA.Utf8("");
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Bytes_EmptyRaw_ReturnsEmpty()
    {
        byte[] empty = [];
        int length;
        {
            using TempBytes temp = ZA.Bytes(new Raw(empty));
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(0);
    }

    #endregion

    // ========================================================================
    // DISPOSE BEHAVIOR TESTS
    // Tests for correct disposal and buffer release
    // ========================================================================

    #region Dispose Behavior Tests

    [Test]
    public async Task TempString_DisposeReleasesBuffer()
    {
        bool isHeapAllocated;
        {
            TempString temp = ZA.String("Test: ", 123);
            isHeapAllocated = temp.IsHeapAllocated;
            temp.Dispose();
        }
        await Assert.That(isHeapAllocated).IsFalse();

        // After dispose, next call should get ThreadStatic buffer
        bool isHeapAllocated2;
        {
            using TempString temp2 = ZA.String("Test2: ", 456);
            isHeapAllocated2 = temp2.IsHeapAllocated;
        }
        await Assert.That(isHeapAllocated2).IsFalse();
    }

    [Test]
    public async Task TempBytes_DisposeReleasesBuffer()
    {
        bool isHeapAllocated;
        {
            TempBytes temp = ZA.Utf8("Test");
            isHeapAllocated = temp.IsHeapAllocated;
            temp.Dispose();
        }
        await Assert.That(isHeapAllocated).IsFalse();

        bool isHeapAllocated2;
        {
            using TempBytes temp2 = ZA.Utf8("Test2");
            isHeapAllocated2 = temp2.IsHeapAllocated;
        }
        await Assert.That(isHeapAllocated2).IsFalse();
    }

    [Test]
    public async Task TempString_DoubleDispose_NoException()
    {
        bool threw = false;
        try
        {
            TempString temp = ZA.String("Test");
            temp.Dispose();
            temp.Dispose();
        }
        catch (Exception) { threw = true; }
        await Assert.That(threw).IsFalse();
    }

    [Test]
    public async Task TempBytes_DoubleDispose_NoException()
    {
        bool threw = false;
        try
        {
            TempBytes temp = ZA.Utf8("Test");
            temp.Dispose();
            temp.Dispose();
        }
        catch (Exception) { threw = true; }
        await Assert.That(threw).IsFalse();
    }

    [Test]
    public async Task LazyString_FormatLazy_AsString_ThrowsOnFactoryFailure()
    {
        LazyString s = LazyString.FormatLazy(0, static _ => throw new InvalidOperationException("boom"));
        await Assert.That(() => _ = s.AsString).Throws<InvalidOperationException>();
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task TempString_ToString_AndIsEmpty_OnEmptyContent(bool useToString)
    {
        bool isEmpty;
        string content;
        {
            using TempString temp = ZA.String("");
            isEmpty = temp.IsEmpty;
            content = useToString ? temp.ToString() : temp.AsSpan().ToString();
        }

        await Assert.That(isEmpty).IsTrue();
        await Assert.That(content).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task TempBytes_ToArray_OnHeapFallback_ReturnsCopy()
    {
        byte[] array;
        {
            using TempBytes outer = ZA.Utf8("outer");
            using TempBytes inner = ZA.Utf8("inner");
            array = inner.ToArray();
        }

        await Assert.That(array).IsEquivalentTo("inner"u8.ToArray());
    }

    #endregion

    // ========================================================================
    // NUMERIC BOUNDARY TESTS
    // Tests for minimum and maximum values of all numeric types
    // ========================================================================

    #region Numeric Boundary Tests

    [Test]
    public async Task String_MaxInt_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Max: ", int.MaxValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Max: 2147483647");
    }

    [Test]
    public async Task String_MinInt_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Min: ", int.MinValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Min: -2147483648");
    }

    [Test]
    public async Task String_MaxLong_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Max: ", long.MaxValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Max: 9223372036854775807");
    }

    [Test]
    public async Task String_MinLong_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Min: ", long.MinValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Min: -9223372036854775808");
    }

    [Test]
    public async Task String_MaxByte_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Max: ", byte.MaxValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Max: 255");
    }

    [Test]
    public async Task String_MinSByte_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Min: ", sbyte.MinValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Min: -128");
    }

    [Test]
    public async Task String_MaxDecimal_FormatsWithoutOverflow()
    {
        bool hasContent;
        {
            using TempString temp = ZA.String("Max: ", decimal.MaxValue);
            hasContent = temp.Length > 5;
        }
        await Assert.That(hasContent).IsTrue();
    }

    [Test]
    public async Task String_MinDecimal_FormatsWithoutOverflow()
    {
        bool hasContent;
        {
            using TempString temp = ZA.String("Min: ", decimal.MinValue);
            hasContent = temp.Length > 5;
        }
        await Assert.That(hasContent).IsTrue();
    }

    #endregion

    // ========================================================================
    // FLOATING POINT SPECIAL VALUES
    // Tests for infinity, NaN, and other special floating point values
    // ========================================================================

    #region Floating Point Special Values

    [Test]
    public async Task String_PositiveInfinity_FormatsCorrectly()
    {
        string result;
        {
            using TempString temp = ZA.String("Infinity: ", double.PositiveInfinity);
            result = temp.AsSpan().ToString();
        }
        await Assert.That(result).Contains("Infinity");
    }

    [Test]
    public async Task String_NegativeInfinity_FormatsCorrectly()
    {
        string result;
        {
            using TempString temp = ZA.String("NegInf: ", double.NegativeInfinity);
            result = temp.AsSpan().ToString();
        }
        await Assert.That(result).Contains("Infinity");
    }

    [Test]
    public async Task String_NaN_FormatsCorrectly()
    {
        string result;
        {
            using TempString temp = ZA.String("NaN: ", double.NaN);
            result = temp.AsSpan().ToString();
        }
        await Assert.That(result).Contains("NaN");
    }

    [Test]
    public async Task String_FloatPositiveInfinity_FormatsCorrectly()
    {
        string result;
        {
            using TempString temp = ZA.String("Infinity: ", float.PositiveInfinity);
            result = temp.AsSpan().ToString();
        }
        await Assert.That(result).Contains("Infinity");
    }

    [Test]
    public async Task String_FloatNaN_FormatsCorrectly()
    {
        string result;
        {
            using TempString temp = ZA.String("NaN: ", float.NaN);
            result = temp.AsSpan().ToString();
        }
        await Assert.That(result).Contains("NaN");
    }

    [Test]
    public async Task String_Epsilon_FormatsCorrectly()
    {
        bool hasContent;
        {
            using TempString temp = ZA.String("Epsilon: ", double.Epsilon);
            hasContent = temp.Length > 9;
        }
        await Assert.That(hasContent).IsTrue(); // "Epsilon: " + some value
    }

    [Test]
    public async Task String_NegativeZero_FormatsAsZero()
    {
        string content;
        {
            using TempString temp = ZA.String(-0.0);
            // .NET formats -0.0 as "-0"
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("-0");
    }

    #endregion

    // ========================================================================
    // UNICODE EDGE CASES
    // Tests for complex Unicode scenarios
    // ========================================================================

    #region Unicode Edge Cases

    [Test]
    public async Task String_Emoji_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Emoji: ", "🎉🎊🎈");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Emoji: 🎉🎊🎈");
    }

    [Test]
    public async Task String_SurrogatePair_FormatsCorrectly()
    {
        // Musical G clef: U+1D11E (surrogate pair in UTF-16)
        string content;
        {
            using TempString temp = ZA.String("Music: ", "𝄞");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Music: 𝄞");
    }

    [Test]
    public async Task Utf8_Emoji_EncodesCorrectly()
    {
        // Emoji 🎉 is 4 bytes in UTF-8
        int length;
        {
            using TempBytes temp = ZA.Utf8("🎉");
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(4);
    }

    [Test]
    public async Task Utf8_SurrogatePair_EncodesCorrectly()
    {
        // Surrogate pair is 4 bytes in UTF-8
        int length;
        {
            using TempBytes temp = ZA.Utf8("𝄞");
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(4);
    }

    [Test]
    public async Task String_ZeroWidthChars_FormatsCorrectly()
    {
        // Zero-width non-joiner (U+200C)
        string content;
        {
            using TempString temp = ZA.String("A\u200CB");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("A\u200CB");
    }

    [Test]
    public async Task String_RightToLeft_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("RTL: مرحبا");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("RTL: مرحبا");
    }

    [Test]
    public async Task String_CombiningCharacters_FormatsCorrectly()
    {
        // e + combining acute accent - this preserves the combining character sequence
        string content;
        {
            using TempString temp = ZA.String("Cafe\u0301");
            // The combining sequence is preserved, not normalized to precomposed form
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Cafe\u0301");
    }

    #endregion

    // ========================================================================
    // THREADING TESTS (SEQUENTIAL)
    // Tests for correct behavior under sequential thread usage
    // ========================================================================

    #region Sequential Threading Tests

    [Test]
    public async Task String_SequentialCalls_WorkCorrectly()
    {
        for (int i = 0; i < 100; i++)
        {
            string content;
            {
                using TempString temp = ZA.String("Iteration: ", i);
                content = temp.AsSpan().ToString();
            }
            await Assert.That(content).IsEqualTo($"Iteration: {i}");
        }
    }

    [Test]
    public async Task Utf8_SequentialCalls_WorkCorrectly()
    {
        for (int i = 0; i < 100; i++)
        {
            string content;
            {
                using TempBytes temp = ZA.Utf8("Iteration: ", i);
                content = Encoding.UTF8.GetString(temp.AsSpan());
            }
            await Assert.That(content).IsEqualTo($"Iteration: {i}");
        }
    }

    #endregion

    // ========================================================================
    // GRACEFUL FALLBACK TESTS
    // Tests for graceful fallback behavior
    // ========================================================================

    #region Graceful Fallback Tests

    [Test]
    public async Task String_NestedCalls_GracefullyFallsBack()
    {
        bool outerIsHeapAllocated, innerIsHeapAllocated;
        string outerContent, innerContent;
        {
            using TempString outer = ZA.String("Outer");
            using TempString inner = ZA.String("Inner");

            // Outer uses ThreadStatic, inner uses heap fallback
            outerIsHeapAllocated = outer.IsHeapAllocated;
            innerIsHeapAllocated = inner.IsHeapAllocated;
            outerContent = outer.AsSpan().ToString();
            innerContent = inner.AsSpan().ToString();
        }

        await Assert.That(outerIsHeapAllocated).IsFalse();
        await Assert.That(innerIsHeapAllocated).IsTrue();

        // Both return correct content
        await Assert.That(outerContent).IsEqualTo("Outer");
        await Assert.That(innerContent).IsEqualTo("Inner");
    }

    [Test]
    public async Task String_DeepNesting_AllWork()
    {
        bool aIsHeap, bIsHeap, cIsHeap, dIsHeap, eIsHeap;
        string aContent, eContent;
        {
            using TempString a = ZA.String("A");
            using TempString b = ZA.String("B");
            using TempString c = ZA.String("C");
            using TempString d = ZA.String("D");
            using TempString e = ZA.String("E");

            aIsHeap = a.IsHeapAllocated;
            bIsHeap = b.IsHeapAllocated;
            cIsHeap = c.IsHeapAllocated;
            dIsHeap = d.IsHeapAllocated;
            eIsHeap = e.IsHeapAllocated;
            aContent = a.AsSpan().ToString();
            eContent = e.AsSpan().ToString();
        }

        await Assert.That(aIsHeap).IsFalse();
        await Assert.That(bIsHeap).IsTrue();
        await Assert.That(cIsHeap).IsTrue();
        await Assert.That(dIsHeap).IsTrue();
        await Assert.That(eIsHeap).IsTrue();

        await Assert.That(aContent).IsEqualTo("A");
        await Assert.That(eContent).IsEqualTo("E");
    }

    [Test]
    public async Task TryString_NestedCalls_GracefullyFallsBack()
    {
        bool success1 = ZA.TryString(out TempString result1, "First");
        bool success2 = ZA.TryString(out TempString result2, "Second");

        bool r1IsHeap, r2IsHeap;
        {
            using (result1)
            using (result2)
            {
                r1IsHeap = result1.IsHeapAllocated;
                r2IsHeap = result2.IsHeapAllocated;
            }
        }

        await Assert.That(success1).IsTrue();
        await Assert.That(success2).IsTrue();
        await Assert.That(r1IsHeap).IsFalse();
        await Assert.That(r2IsHeap).IsTrue();
    }

    #endregion

    // ========================================================================
    // BYTES METHOD TESTS
    // Tests for binary serialization edge cases
    // ========================================================================

    #region Bytes Method Tests

    [Test]
    public async Task Bytes_RawAndEndian_CombineCorrectly()
    {
        byte[] rawData = [0xFF, 0xFE];
        int bytesLength;
        byte b0, b1, b2, b3;
        {
            using TempBytes temp = ZA.Bytes(new Raw(rawData), new U16BE(0x1234));
            byte[] bytes = temp.AsSpan().ToArray();
            bytesLength = bytes.Length;
            b0 = bytes[0];
            b1 = bytes[1];
            b2 = bytes[2];
            b3 = bytes[3];
        }

        await Assert.That(bytesLength).IsEqualTo(4);
        await Assert.That(b0).IsEqualTo((byte)0xFF);
        await Assert.That(b1).IsEqualTo((byte)0xFE);
        await Assert.That(b2).IsEqualTo((byte)0x12);
        await Assert.That(b3).IsEqualTo((byte)0x34);
    }

    [Test]
    public async Task Bytes_VarInt_ZeroValue()
    {
        int length;
        byte firstByte;
        {
            using TempBytes temp = ZA.Bytes(new VarInt(0));
            length = temp.Length;
            firstByte = temp.AsSpan()[0];
        }
        await Assert.That(length).IsEqualTo(1);
        await Assert.That(firstByte).IsEqualTo((byte)0);
    }

    [Test]
    public async Task Bytes_VarInt_MaxValue()
    {
        bool lengthValid;
        {
            using TempBytes temp = ZA.Bytes(new VarInt(ulong.MaxValue));
            lengthValid = temp.Length <= 10;
        }
        await Assert.That(lengthValid).IsTrue(); // VarInt max encoding size
    }

    #endregion

    // ========================================================================
    // SPECIAL VALUE TESTS
    // Tests for DateTime, Guid, and other special values
    // ========================================================================

    #region Special Value Tests

    [Test]
    public async Task String_DateTimeMin_FormatsCorrectly()
    {
        bool hasContent;
        {
            using TempString temp = ZA.String("Date: ", DateTime.MinValue);
            hasContent = temp.Length > 6;
        }
        await Assert.That(hasContent).IsTrue();
    }

    [Test]
    public async Task String_DateTimeMax_FormatsCorrectly()
    {
        bool hasContent;
        {
            using TempString temp = ZA.String("Date: ", DateTime.MaxValue);
            hasContent = temp.Length > 6;
        }
        await Assert.That(hasContent).IsTrue();
    }

    [Test]
    public async Task String_GuidEmpty_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("GUID: ", Guid.Empty);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("GUID: 00000000-0000-0000-0000-000000000000");
    }

    [Test]
    public async Task String_TimeSpanZero_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Zero: ", TimeSpan.Zero);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).Contains("0");
    }

    [Test]
    public async Task String_TimeSpanNegative_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Neg: ", TimeSpan.FromHours(-1));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).Contains("-");
    }

    #endregion

    // ========================================================================
    // BINARYPARSER ERROR HANDLING
    // Tests for insufficient data and malformed input
    // ========================================================================

    #region BinaryParser Error Handling

    [Test]
    public async Task BinaryParser_EmptyBuffer_ThrowsOnRead()
    {
        byte[] empty = [];
        BinaryParser parser = new(empty);
        bool threw = false;
        try { parser.ReadByte(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_InsufficientForUInt16_Throws()
    {
        byte[] data = [0x01];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadUInt16BE(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_InsufficientForUInt32_Throws()
    {
        byte[] data = [0x01, 0x02, 0x03];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadUInt32BE(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_InsufficientForUInt64_Throws()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadUInt64BE(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_VarIntTooLong_Throws()
    {
        // VarInt that would exceed 64 bits
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadVarInt(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_VarIntTruncated_Throws()
    {
        // VarInt with continuation bit set but no more bytes
        byte[] data = [0x80];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadVarInt(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_SkipBeyondBuffer_Throws()
    {
        byte[] data = [0x01, 0x02];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.Skip(100); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadBytesBeyondBuffer_Throws()
    {
        byte[] data = [0x01, 0x02];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadBytes(100); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    #endregion

    // ========================================================================
    // BITREADER ERROR HANDLING
    // Tests for bit-level edge cases
    // ========================================================================

    #region BitReader Error Handling

    [Test]
    public async Task BitReader_ReadBitsZero_Throws()
    {
        byte[] data = [0x00];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.ReadBits(0); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BitReader_ReadBits65_Throws()
    {
        byte[] data = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.ReadBits(65); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BitReader_ReadBytesNotAligned_Throws()
    {
        byte[] data = [0x00, 0x01, 0x02];
        BitReader reader = new(data);
        reader.ReadBit1(); // Now not aligned
        bool threw = false;
        try { reader.ReadBytes(2); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    #endregion

    // ========================================================================
    // SPANBUILDER OVERFLOW TESTS
    // Tests for buffer overflow handling
    // ========================================================================

    #region SpanBuilder Overflow Tests

    [Test]
    public async Task SpanStringBuilder_OverflowSingleChar_Throws()
    {
        Span<char> buffer = stackalloc char[3];
        SpanStringBuilder builder = new(buffer);
        builder.Append("ABC");
        bool threw = false;
        // Append(char) now throws InvalidOperationException before indexing past the buffer
        try { builder.Append('X'); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanStringBuilder_OverflowString_Throws()
    {
        Span<char> buffer = stackalloc char[5];
        SpanStringBuilder builder = new(buffer);
        bool threw = false;
        // Append(string) now throws InvalidOperationException before copying into an undersized buffer
        try { builder.Append("Too long!"); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanBytesBuilder_OverflowInt32_Throws()
    {
        Span<byte> buffer = stackalloc byte[3];
        SpanBytesBuilder builder = new(buffer);
        bool threw = false;
        try { builder.AppendInt32BigEndian(0x12345678); }
        catch (Exception) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanBytesBuilder_OverflowInt64_Throws()
    {
        Span<byte> buffer = stackalloc byte[7];
        SpanBytesBuilder builder = new(buffer);
        bool threw = false;
        try { builder.AppendInt64BigEndian(0x123456789ABCDEF0L); }
        catch (Exception) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanStringBuilder_SeekBackNegative_Throws()
    {
        Span<char> buffer = stackalloc char[10];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Test");
        bool threw = false;
        try { builder.SeekBack(10); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanBytesBuilder_SeekBackNegative_Throws()
    {
        Span<byte> buffer = stackalloc byte[10];
        SpanBytesBuilder builder = new(buffer);
        builder.Append([0x01, 0x02, 0x03]);
        bool threw = false;
        try { builder.SeekBack(10); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    #endregion

    // ========================================================================
    // TEMPBUILDER EDGE CASES
    // Tests for TempStringBuilder and TempBytesBuilder edge cases
    // ========================================================================

    #region TempBuilder Edge Cases

    [Test]
    public async Task TempStringBuilder_SeekBackExact_ClearsBuffer()
    {
        int length;
        bool isEmpty;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hello");
            builder.SeekBack(5);
            length = builder.Length;
            isEmpty = builder.IsEmpty;
        }

        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task TempBytesBuilder_SeekBackExact_ClearsBuffer()
    {
        int length;
        bool isEmpty;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03]);
            builder.SeekBack(3);
            length = builder.Length;
            isEmpty = builder.IsEmpty;
        }

        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task TempStringBuilder_ClearThenAppend_Works()
    {
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("First");
            builder.Clear();
            builder.Append("Second");
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).IsEqualTo("Second");
    }

    [Test]
    public async Task TempBytesBuilder_ClearThenAppend_Works()
    {
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02]);
            builder.Clear();
            builder.Append([0xAA, 0xBB]);
            bytes = builder.AsSpan().ToArray();
        }

        await Assert.That(bytes).IsEquivalentTo((byte[])[0xAA, 0xBB]);
    }

    #endregion

    // ========================================================================
    // UNICODE AND ENCODING EDGE CASES
    // Tests for special Unicode characters
    // ========================================================================

    #region Unicode Edge Cases

    [Test]
    public async Task TempString_EmojisAndSpecialChars_HandlesCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String($"Hello 🌍🚀 World!");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).Contains("🌍");
        await Assert.That(content).Contains("🚀");
    }

    [Test]
    public async Task TempBytes_EmojisAndSpecialChars_EncodesCorrectly()
    {
        string decoded;
        {
            using TempBytes temp = ZA.Utf8($"Hello 🌍🚀 World!");
            decoded = System.Text.Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(decoded).Contains("🌍");
        await Assert.That(decoded).Contains("🚀");
    }

    [Test]
    public async Task TempString_NullCharInString_HandlesCorrectly()
    {
        string withNull = "Before\0After";
        string content;
        {
            using TempString temp = ZA.String($"{withNull}");
            // The null char is included in the output
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Before\0After");
    }

    [Test]
    public async Task TempBytes_SurrogatePairs_EncodesCorrectly()
    {
        // 𝄞 (Musical G Clef) - requires surrogate pair in UTF-16
        string musicNote = "𝄞";
        string decoded;
        {
            using TempBytes temp = ZA.Utf8($"{musicNote}");
            decoded = System.Text.Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(decoded).IsEqualTo(musicNote);
    }

    [Test]
    public async Task TempString_ControlCharacters_HandlesCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String($"Tab:\tNewline:\nCarriage:\r");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).Contains("\t");
        await Assert.That(content).Contains("\n");
        await Assert.That(content).Contains("\r");
    }

    #endregion

    // ========================================================================
    // BINARYPARSER BOUNDARY VALUES
    // Tests for parsing boundary values
    // ========================================================================

    #region BinaryParser Boundary Values

    [Test]
    public async Task BinaryParser_ReadMaxUInt16BE_ReturnsCorrectValue()
    {
        byte[] data = [0xFF, 0xFF];
        ushort value;
        {
            BinaryParser parser = new(data);
            value = parser.ReadUInt16BE();
        }
        await Assert.That(value).IsEqualTo(ushort.MaxValue);
    }

    [Test]
    public async Task BinaryParser_ReadMaxUInt32BE_ReturnsCorrectValue()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF];
        uint value;
        {
            BinaryParser parser = new(data);
            value = parser.ReadUInt32BE();
        }
        await Assert.That(value).IsEqualTo(uint.MaxValue);
    }

    [Test]
    public async Task BinaryParser_ReadMaxUInt64BE_ReturnsCorrectValue()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        ulong value;
        {
            BinaryParser parser = new(data);
            value = parser.ReadUInt64BE();
        }
        await Assert.That(value).IsEqualTo(ulong.MaxValue);
    }

    [Test]
    public async Task BinaryParser_ReadMinInt16BE_ReturnsCorrectValue()
    {
        byte[] data = [0x80, 0x00];
        short value;
        {
            BinaryParser parser = new(data);
            value = parser.ReadInt16BE();
        }

        await Assert.That(value).IsEqualTo(short.MinValue);
    }

    [Test]
    public async Task BinaryParser_ReadMinInt32BE_ReturnsCorrectValue()
    {
        byte[] data = [0x80, 0x00, 0x00, 0x00];
        int value;
        {
            BinaryParser parser = new(data);
            value = parser.ReadInt32BE();
        }

        await Assert.That(value).IsEqualTo(int.MinValue);
    }

    [Test]
    public async Task BinaryParser_ReadMinInt64BE_ReturnsCorrectValue()
    {
        byte[] data = [0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        long value;
        {
            BinaryParser parser = new(data);
            value = parser.ReadInt64BE();
        }

        await Assert.That(value).IsEqualTo(long.MinValue);
    }

    #endregion

    // ========================================================================
    // VARINT EDGE CASES
    // Tests for VarInt boundary values
    // ========================================================================

    #region VarInt Edge Cases

    [Test]
    public async Task BinaryParser_VarInt_Zero_ReturnsCorrectly()
    {
        byte[] data = [0x00];
        ulong value;
        {
            BinaryParser parser = new(data);
            value = parser.ReadVarInt();
        }
        await Assert.That(value).IsEqualTo(0UL);
    }

    [Test]
    public async Task BinaryParser_VarInt_127_SingleByte()
    {
        byte[] data = [0x7F];
        ulong value;
        int position;
        {
            BinaryParser parser = new(data);
            value = parser.ReadVarInt();
            position = parser.Position;
        }

        await Assert.That(value).IsEqualTo(127UL);
        await Assert.That(position).IsEqualTo(1);
    }

    [Test]
    public async Task BinaryParser_VarInt_128_TwoBytes()
    {
        byte[] data = [0x80, 0x01];
        ulong value;
        int position;
        {
            BinaryParser parser = new(data);
            value = parser.ReadVarInt();
            position = parser.Position;
        }

        await Assert.That(value).IsEqualTo(128UL);
        await Assert.That(position).IsEqualTo(2);
    }

    [Test]
    public async Task BinaryParser_VarIntZigZag_MinusOne_IsOne()
    {
        byte[] data = [0x01]; // ZigZag(-1) = 1
        long value;
        {
            BinaryParser parser = new(data);
            value = parser.ReadVarIntZigZag();
        }

        await Assert.That(value).IsEqualTo(-1L);
    }

    [Test]
    public async Task BinaryParser_VarIntZigZag_One_IsTwo()
    {
        byte[] data = [0x02]; // ZigZag(1) = 2
        long value;
        {
            BinaryParser parser = new(data);
            value = parser.ReadVarIntZigZag();
        }

        await Assert.That(value).IsEqualTo(1L);
    }

    #endregion

    // ========================================================================
    // FLOAT/DOUBLE SPECIAL VALUES
    // Tests for NaN, Infinity, etc.
    // ========================================================================

    #region Float/Double Special Values

    [Test]
    public async Task BinaryParser_ReadF32BE_PositiveInfinity()
    {
        // IEEE 754: +Infinity = 0x7F800000
        byte[] data = [0x7F, 0x80, 0x00, 0x00];
        bool isPositiveInfinity;
        {
            BinaryParser parser = new(data);
            isPositiveInfinity = float.IsPositiveInfinity(parser.ReadF32BE());
        }

        await Assert.That(isPositiveInfinity).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadF32BE_NegativeInfinity()
    {
        // IEEE 754: -Infinity = 0xFF800000
        byte[] data = [0xFF, 0x80, 0x00, 0x00];
        bool isNegativeInfinity;
        {
            BinaryParser parser = new(data);
            isNegativeInfinity = float.IsNegativeInfinity(parser.ReadF32BE());
        }

        await Assert.That(isNegativeInfinity).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadF32BE_NaN()
    {
        // IEEE 754: NaN = 0x7FC00000
        byte[] data = [0x7F, 0xC0, 0x00, 0x00];
        bool isNaN;
        {
            BinaryParser parser = new(data);
            isNaN = float.IsNaN(parser.ReadF32BE());
        }

        await Assert.That(isNaN).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadF64BE_PositiveInfinity()
    {
        // IEEE 754: +Infinity = 0x7FF0000000000000
        byte[] data = [0x7F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        bool isPositiveInfinity;
        {
            BinaryParser parser = new(data);
            isPositiveInfinity = double.IsPositiveInfinity(parser.ReadF64BE());
        }

        await Assert.That(isPositiveInfinity).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadF64BE_NegativeInfinity()
    {
        // IEEE 754: -Infinity = 0xFFF0000000000000
        byte[] data = [0xFF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        bool isNegativeInfinity;
        {
            BinaryParser parser = new(data);
            isNegativeInfinity = double.IsNegativeInfinity(parser.ReadF64BE());
        }

        await Assert.That(isNegativeInfinity).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadF64BE_NaN()
    {
        // IEEE 754: NaN = 0x7FF8000000000000
        byte[] data = [0x7F, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        bool isNaN;
        {
            BinaryParser parser = new(data);
            isNaN = double.IsNaN(parser.ReadF64BE());
        }

        await Assert.That(isNaN).IsTrue();
    }

    #endregion

    // ========================================================================
    // VARIABLE-SIZE STRUCT BOUNDS CHECKS
    // Tests that TryParse and TryWrite return false (not throw) on truncated
    // or undersized buffers for structs that have variable-length members
    // followed by fixed-size fields. These cover the grouped-check algorithm
    // and the length-prefix guards added in GenerateParsingCode /
    // GenerateTryWrite / GenerateStringParsing / GenerateStringWriting.
    // ========================================================================
    #region Variable-Size Struct Bounds Checks (Parse)

    // VarIntThenFixedStruct: VarInt(1 byte for small values) + U32BE(4) + U16BE(2) = 7 bytes min
    [Test]
    [Arguments(new byte[] { })]                                           // empty
    [Arguments(new byte[] { 0x2A })]                                      // only VarInt byte
    [Arguments(new byte[] { 0x2A, 0x01, 0x02, 0x03 })]                    // VarInt + 3 of 4 U32BE bytes
    [Arguments(new byte[] { 0x2A, 0x01, 0x02, 0x03, 0x04 })]              // VarInt + full U32BE, no U16BE
    [Arguments(new byte[] { 0x2A, 0x01, 0x02, 0x03, 0x04, 0xAB })]        // missing 1 byte of U16BE
    public async Task VarIntThenFixedStruct_TruncatedInput_ReturnsFalse(byte[] data)
    {
        // Act
        bool success = VarIntThenFixedStruct.TryParse(data, out _, out _);

        // Assert
        await Assert.That(success).IsFalse();
    }

    // StringThenFixedStruct: VarInt(length) + string bytes + U32BE(4)
    [Test]
    [Arguments(new byte[] { })]                                            // empty
    [Arguments(new byte[] { 0x02 })]                                       // length byte only
    [Arguments(new byte[] { 0x02, 0x68 })]                                 // length + 1 of 2 string bytes
    [Arguments(new byte[] { 0x02, 0x68, 0x69 })]                           // full string, no U32BE
    [Arguments(new byte[] { 0x02, 0x68, 0x69, 0xDE, 0xAD, 0xBE })]        // missing last byte of U32BE
    public async Task StringThenFixedStruct_TruncatedInput_ReturnsFalse(byte[] data)
    {
        // Act
        bool success = StringThenFixedStruct.TryParse(data, out _, out _);

        // Assert
        await Assert.That(success).IsFalse();
    }

    // FixedBEStringStruct: 2-byte BE length prefix + string bytes + U16BE(2)
    [Test]
    [Arguments(new byte[] { })]                                             // empty
    [Arguments(new byte[] { 0x00 })]                                        // 1 of 2 prefix bytes
    [Arguments(new byte[] { 0x00, 0x03 })]                                  // prefix only, no string
    [Arguments(new byte[] { 0x00, 0x03, 0x61, 0x62 })]                      // prefix + 2 of 3 string bytes
    [Arguments(new byte[] { 0x00, 0x03, 0x61, 0x62, 0x63 })]                // full string, no U16BE CRC
    [Arguments(new byte[] { 0x00, 0x03, 0x61, 0x62, 0x63, 0x12 })]          // missing 1 CRC byte
    public async Task FixedBEStringStruct_TruncatedInput_ReturnsFalse(byte[] data)
    {
        // Act
        bool success = FixedBEStringStruct.TryParse(data, out _, out _);

        // Assert
        await Assert.That(success).IsFalse();
    }

    #endregion

    #region Variable-Size Struct Bounds Checks (Write)

    // WritableVarIntThenFixed: min 1 (VarInt for small value) + 4 + 2 = 7 bytes
    [Test]
    [Arguments(0)]   // empty destination
    [Arguments(1)]   // space for VarInt only
    [Arguments(5)]   // VarInt + U32BE; no room for U16BE
    [Arguments(6)]   // one byte short
    public async Task WritableVarIntThenFixed_UndersizedDestination_ReturnsFalse(int destinationSize)
    {
        // Arrange
        WritableVarIntThenFixed packet = new()
        {
            Count = new VarInt(1),
            Value = new U32BE(0x01020304),
            Flags = new U16BE(0xABCD)
        };
        byte[] destination = new byte[destinationSize];

        // Act
        bool success = packet.TryWrite(destination, out _);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task WritableVarIntThenFixed_ExactSizedDestination_Succeeds()
    {
        // Arrange: VarInt(42)=1 byte + U32BE=4 bytes + U16BE=2 bytes = 7 bytes
        WritableVarIntThenFixed packet = new()
        {
            Count = new VarInt(42),
            Value = new U32BE(0x01020304),
            Flags = new U16BE(0xABCD)
        };
        byte[] destination = new byte[7];

        // Act
        bool success = packet.TryWrite(destination, out int written);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(7);
    }

    // WritableStringThenFixed: "hi" → VarInt(2)=1 byte + 2 string bytes + U32BE=4 bytes = 7 bytes
    [Test]
    [Arguments(0)]   // empty
    [Arguments(1)]   // only VarInt fits
    [Arguments(3)]   // VarInt + string, no room for U32BE
    [Arguments(6)]   // one byte short
    public async Task WritableStringThenFixed_UndersizedDestination_ReturnsFalse(int destinationSize)
    {
        // Arrange
        WritableStringThenFixed packet = new() { Label = "hi", Id = new U32BE(0xDEADBEEF) };
        byte[] destination = new byte[destinationSize];

        // Act
        bool success = packet.TryWrite(destination, out _);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task WritableStringThenFixed_ExactSizedDestination_Succeeds()
    {
        // Arrange: VarInt(2)=1 byte + "hi"=2 bytes + U32BE=4 bytes = 7 bytes
        WritableStringThenFixed packet = new() { Label = "hi", Id = new U32BE(0xDEADBEEF) };
        byte[] destination = new byte[7];

        // Act
        bool success = packet.TryWrite(destination, out int written);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(7);
    }

    // WritableFixedLengthString: [StringFixedLength(8)] — needs exactly 8 bytes
    [Test]
    [Arguments(0)]   // empty
    [Arguments(4)]   // half the required 8 bytes
    [Arguments(7)]   // one byte short
    public async Task WritableFixedLengthString_UndersizedDestination_ReturnsFalse(int destinationSize)
    {
        // Arrange
        WritableFixedLengthString packet = new() { Name = "hi" };
        byte[] destination = new byte[destinationSize];

        // Act
        bool success = packet.TryWrite(destination, out _);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task WritableFixedLengthString_ExactSizedDestination_Succeeds()
    {
        // Arrange: 8-byte fixed slot, "hi" UTF-8 (2 bytes) + 6 null bytes = 8 bytes
        WritableFixedLengthString packet = new() { Name = "hi" };
        byte[] destination = new byte[8];

        // Act
        bool success = packet.TryWrite(destination, out int written);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(8);
    }

    [Test]
    public async Task WritableFixedLengthString_StringTooLong_ReturnsFalse()
    {
        // Arrange: string encodes to 9 UTF-8 bytes but slot is only 8
        WritableFixedLengthString packet = new() { Name = "123456789" }; // 9 ASCII bytes
        byte[] destination = new byte[8];

        // Act
        bool success = packet.TryWrite(destination, out _);

        // Assert
        await Assert.That(success).IsFalse();
    }

    // WritableNullTermString: "hi" needs 3 bytes (2 UTF-8 + null terminator)
    [Test]
    [Arguments(0)]   // empty
    [Arguments(1)]   // room for 'h' only, no null
    [Arguments(2)]   // room for 'h' and 'i', no null
    public async Task WritableNullTermString_UndersizedDestination_ReturnsFalse(int destinationSize)
    {
        // Arrange
        WritableNullTermString packet = new() { Label = "hi" };
        byte[] destination = new byte[destinationSize];

        // Act
        bool success = packet.TryWrite(destination, out _);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task WritableNullTermString_ExactSizedDestination_Succeeds()
    {
        // Arrange: "hi"=2 UTF-8 bytes + null terminator = 3 bytes
        WritableNullTermString packet = new() { Label = "hi" };
        byte[] destination = new byte[3];

        // Act
        bool success = packet.TryWrite(destination, out int written);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(3);
        await Assert.That(destination).IsEquivalentTo((byte[])[0x68, 0x69, 0x00]);
    }

    // WritableFromFieldString: U16BE(2 bytes) + "hi"(2 bytes) = 4 bytes total
    [Test]
    [Arguments(0)]   // empty — grouped check for U16BE fails
    [Arguments(1)]   // grouped check for U16BE fails
    [Arguments(2)]   // U16BE written; 0 bytes remain for string data
    [Arguments(3)]   // U16BE written; 1 byte remains but 2 needed
    public async Task WritableFromFieldString_UndersizedDestination_ReturnsFalse(int destinationSize)
    {
        // Arrange: NameLength must equal UTF-8 byte count of Name
        WritableFromFieldString packet = new() { NameLength = new U16BE(2), Name = "hi" };
        byte[] destination = new byte[destinationSize];

        // Act
        bool success = packet.TryWrite(destination, out _);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task WritableFromFieldString_ExactSizedDestination_Succeeds()
    {
        // Arrange: U16BE=2 bytes + "hi"=2 bytes = 4 bytes
        WritableFromFieldString packet = new() { NameLength = new U16BE(2), Name = "hi" };
        byte[] destination = new byte[4];

        // Act
        bool success = packet.TryWrite(destination, out int written);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(4);
        await Assert.That(destination).IsEquivalentTo((byte[])[0x00, 0x02, 0x68, 0x69]);
    }

    // WritableFromFieldBytes: U16BE(2 bytes) + data(2 bytes) = 4 bytes total
    [Test]
    [Arguments(0)]   // empty — grouped check for U16BE fails
    [Arguments(1)]   // grouped check for U16BE fails
    [Arguments(2)]   // U16BE written; 0 bytes remain for data
    [Arguments(3)]   // U16BE written; 1 byte remains but 2 needed
    public async Task WritableFromFieldBytes_UndersizedDestination_ReturnsFalse(int destinationSize)
    {
        // Arrange
        WritableFromFieldBytes packet = new() { DataLength = new U16BE(2), Data = [0xAB, 0xCD] };
        byte[] destination = new byte[destinationSize];

        // Act
        bool success = packet.TryWrite(destination, out _);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task WritableFromFieldBytes_ExactSizedDestination_Succeeds()
    {
        // Arrange: U16BE=2 bytes + [0xAB, 0xCD]=2 bytes = 4 bytes
        WritableFromFieldBytes packet = new() { DataLength = new U16BE(2), Data = [0xAB, 0xCD] };
        byte[] destination = new byte[4];

        // Act
        bool success = packet.TryWrite(destination, out int written);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(4);
        await Assert.That(destination).IsEquivalentTo((byte[])[0x00, 0x02, 0xAB, 0xCD]);
    }

    #endregion
}
