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
public class NegativeAndEdgeCaseTests
{
    // ========================================================================
    // NULL STRING HANDLING
    // Tests for graceful null string handling
    // ========================================================================

    #region Null String Handling

    [Fact]
    public void String_NullString_HandlesGracefully()
    {
        string? nullStr = null;
        using TempString temp = ZA.String(nullStr!);
        Assert.Equal(0, temp.Length);
    }

    [Fact]
    public void String_MultipleNulls_HandlesGracefully()
    {
        string? null1 = null;
        string? null2 = null;
        using TempString temp = ZA.String(null1!, null2!);
        Assert.Equal(0, temp.Length);
    }

    [Fact]
    public void String_NullBetweenValues_HandlesGracefully()
    {
        string? nullStr = null;
        using TempString temp = ZA.String("A", nullStr!, "B");
        Assert.Equal("AB", temp.AsSpan().ToString());
    }

    [Fact]
    public void Utf8_NullString_HandlesGracefully()
    {
        string? nullStr = null;
        using TempBytes temp = ZA.Utf8(nullStr!);
        Assert.Equal(0, temp.Length);
    }

    #endregion

    // ========================================================================
    // EMPTY VALUE HANDLING
    // Tests for empty strings and empty collections
    // ========================================================================

    #region Empty Value Handling

    [Fact]
    public void String_EmptyString_ReturnsEmpty()
    {
        using TempString temp = ZA.String("");
        Assert.Equal(0, temp.Length);
    }

    [Fact]
    public void String_MultipleEmptyStrings_ReturnsEmpty()
    {
        using TempString temp = ZA.String("", "", "");
        Assert.Equal(0, temp.Length);
    }

    [Fact]
    public void String_EmptyBetweenValues_FormatsCorrectly()
    {
        using TempString temp = ZA.String("A", "", "B");
        Assert.Equal("AB", temp.AsSpan().ToString());
    }

    [Fact]
    public void Utf8_EmptyString_ReturnsEmpty()
    {
        using TempBytes temp = ZA.Utf8("");
        Assert.Equal(0, temp.Length);
    }

    [Fact]
    public void Bytes_EmptyRaw_ReturnsEmpty()
    {
        byte[] empty = [];
        using TempBytes temp = ZA.Bytes(new Raw(empty));
        Assert.Equal(0, temp.Length);
    }

    #endregion

    // ========================================================================
    // DISPOSE BEHAVIOR TESTS
    // Tests for correct disposal and buffer release
    // ========================================================================

    #region Dispose Behavior Tests

    [Fact]
    public void TempString_DisposeReleasesBuffer()
    {
        TempString temp = ZA.String("Test: ", 123);
        Assert.False(temp.IsHeapAllocated);
        temp.Dispose();

        // After dispose, next call should get ThreadStatic buffer
        using TempString temp2 = ZA.String("Test2: ", 456);
        Assert.False(temp2.IsHeapAllocated);
    }

    [Fact]
    public void TempBytes_DisposeReleasesBuffer()
    {
        TempBytes temp = ZA.Utf8("Test");
        Assert.False(temp.IsHeapAllocated);
        temp.Dispose();

        using TempBytes temp2 = ZA.Utf8("Test2");
        Assert.False(temp2.IsHeapAllocated);
    }

    [Fact]
    public void TempString_DoubleDispose_NoException()
    {
        TempString temp = ZA.String("Test");
        temp.Dispose();
        temp.Dispose(); // Should not throw
    }

    [Fact]
    public void TempBytes_DoubleDispose_NoException()
    {
        TempBytes temp = ZA.Utf8("Test");
        temp.Dispose();
        temp.Dispose(); // Should not throw
    }

    #endregion

    // ========================================================================
    // NUMERIC BOUNDARY TESTS
    // Tests for minimum and maximum values of all numeric types
    // ========================================================================

    #region Numeric Boundary Tests

    [Fact]
    public void String_MaxInt_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Max: ", int.MaxValue);
        Assert.Equal("Max: 2147483647", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MinInt_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Min: ", int.MinValue);
        Assert.Equal("Min: -2147483648", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MaxLong_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Max: ", long.MaxValue);
        Assert.Equal("Max: 9223372036854775807", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MinLong_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Min: ", long.MinValue);
        Assert.Equal("Min: -9223372036854775808", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MaxByte_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Max: ", byte.MaxValue);
        Assert.Equal("Max: 255", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MinSByte_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Min: ", sbyte.MinValue);
        Assert.Equal("Min: -128", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MaxDecimal_FormatsWithoutOverflow()
    {
        using TempString temp = ZA.String("Max: ", decimal.MaxValue);
        Assert.True(temp.Length > 5);
    }

    [Fact]
    public void String_MinDecimal_FormatsWithoutOverflow()
    {
        using TempString temp = ZA.String("Min: ", decimal.MinValue);
        Assert.True(temp.Length > 5);
    }

    #endregion

    // ========================================================================
    // FLOATING POINT SPECIAL VALUES
    // Tests for infinity, NaN, and other special floating point values
    // ========================================================================

    #region Floating Point Special Values

    [Fact]
    public void String_PositiveInfinity_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Infinity: ", double.PositiveInfinity);
        string result = temp.AsSpan().ToString();
        Assert.Contains("Infinity", result);
    }

    [Fact]
    public void String_NegativeInfinity_FormatsCorrectly()
    {
        using TempString temp = ZA.String("NegInf: ", double.NegativeInfinity);
        string result = temp.AsSpan().ToString();
        Assert.Contains("Infinity", result);
    }

    [Fact]
    public void String_NaN_FormatsCorrectly()
    {
        using TempString temp = ZA.String("NaN: ", double.NaN);
        string result = temp.AsSpan().ToString();
        Assert.Contains("NaN", result);
    }

    [Fact]
    public void String_FloatPositiveInfinity_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Infinity: ", float.PositiveInfinity);
        string result = temp.AsSpan().ToString();
        Assert.Contains("Infinity", result);
    }

    [Fact]
    public void String_FloatNaN_FormatsCorrectly()
    {
        using TempString temp = ZA.String("NaN: ", float.NaN);
        string result = temp.AsSpan().ToString();
        Assert.Contains("NaN", result);
    }

    [Fact]
    public void String_Epsilon_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Epsilon: ", double.Epsilon);
        Assert.True(temp.Length > 9); // "Epsilon: " + some value
    }

    [Fact]
    public void String_NegativeZero_FormatsAsZero()
    {
        using TempString temp = ZA.String(-0.0);
        // .NET formats -0.0 as "-0"
        Assert.Equal("-0", temp.AsSpan().ToString());
    }

    #endregion

    // ========================================================================
    // UNICODE EDGE CASES
    // Tests for complex Unicode scenarios
    // ========================================================================

    #region Unicode Edge Cases

    [Fact]
    public void String_Emoji_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Emoji: ", "🎉🎊🎈");
        Assert.Equal("Emoji: 🎉🎊🎈", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_SurrogatePair_FormatsCorrectly()
    {
        // Musical G clef: U+1D11E (surrogate pair in UTF-16)
        using TempString temp = ZA.String("Music: ", "𝄞");
        Assert.Equal("Music: 𝄞", temp.AsSpan().ToString());
    }

    [Fact]
    public void Utf8_Emoji_EncodesCorrectly()
    {
        using TempBytes temp = ZA.Utf8("🎉");
        // Emoji 🎉 is 4 bytes in UTF-8
        Assert.Equal(4, temp.Length);
    }

    [Fact]
    public void Utf8_SurrogatePair_EncodesCorrectly()
    {
        using TempBytes temp = ZA.Utf8("𝄞");
        // Surrogate pair is 4 bytes in UTF-8
        Assert.Equal(4, temp.Length);
    }

    [Fact]
    public void String_ZeroWidthChars_FormatsCorrectly()
    {
        // Zero-width non-joiner (U+200C)
        using TempString temp = ZA.String("A\u200CB");
        Assert.Equal("A\u200CB", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_RightToLeft_FormatsCorrectly()
    {
        using TempString temp = ZA.String("RTL: مرحبا");
        Assert.Equal("RTL: مرحبا", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_CombiningCharacters_FormatsCorrectly()
    {
        // e + combining acute accent - this preserves the combining character sequence
        using TempString temp = ZA.String("Cafe\u0301");
        // The combining sequence is preserved, not normalized to precomposed form
        Assert.Equal("Cafe\u0301", temp.AsSpan().ToString());
    }

    #endregion

    // ========================================================================
    // THREADING TESTS (SEQUENTIAL)
    // Tests for correct behavior under sequential thread usage
    // ========================================================================

    #region Sequential Threading Tests

    [Fact]
    public void String_SequentialCalls_WorkCorrectly()
    {
        for (int i = 0; i < 100; i++)
        {
            using TempString temp = ZA.String("Iteration: ", i);
            Assert.Equal($"Iteration: {i}", temp.AsSpan().ToString());
        }
    }

    [Fact]
    public void Utf8_SequentialCalls_WorkCorrectly()
    {
        for (int i = 0; i < 100; i++)
        {
            using TempBytes temp = ZA.Utf8("Iteration: ", i);
            Assert.Equal($"Iteration: {i}", Encoding.UTF8.GetString(temp.AsSpan()));
        }
    }

    #endregion

    // ========================================================================
    // GRACEFUL FALLBACK TESTS
    // Tests for graceful fallback behavior
    // ========================================================================

    #region Graceful Fallback Tests

    [Fact]
    public void String_NestedCalls_GracefullyFallsBack()
    {
        using TempString outer = ZA.String("Outer");
        using TempString inner = ZA.String("Inner");

        // Outer uses ThreadStatic, inner uses heap fallback
        Assert.False(outer.IsHeapAllocated);
        Assert.True(inner.IsHeapAllocated);

        // Both return correct content
        Assert.Equal("Outer", outer.AsSpan().ToString());
        Assert.Equal("Inner", inner.AsSpan().ToString());
    }

    [Fact]
    public void String_DeepNesting_AllWork()
    {
        using TempString a = ZA.String("A");
        using TempString b = ZA.String("B");
        using TempString c = ZA.String("C");
        using TempString d = ZA.String("D");
        using TempString e = ZA.String("E");

        Assert.False(a.IsHeapAllocated);
        Assert.True(b.IsHeapAllocated);
        Assert.True(c.IsHeapAllocated);
        Assert.True(d.IsHeapAllocated);
        Assert.True(e.IsHeapAllocated);

        Assert.Equal("A", a.AsSpan().ToString());
        Assert.Equal("E", e.AsSpan().ToString());
    }

    [Fact]
    public void TryString_NestedCalls_GracefullyFallsBack()
    {
        bool success1 = ZA.TryString(out TempString result1, "First");
        bool success2 = ZA.TryString(out TempString result2, "Second");

        using (result1)
        using (result2)
        {
            Assert.True(success1);
            Assert.True(success2);
            Assert.False(result1.IsHeapAllocated);
            Assert.True(result2.IsHeapAllocated);
        }
    }

    #endregion

    // ========================================================================
    // BYTES METHOD TESTS
    // Tests for binary serialization edge cases
    // ========================================================================

    #region Bytes Method Tests

    [Fact]
    public void Bytes_RawAndEndian_CombineCorrectly()
    {
        byte[] rawData = [0xFF, 0xFE];
        using TempBytes temp = ZA.Bytes(new Raw(rawData), new U16BE(0x1234));
        ReadOnlySpan<byte> bytes = temp.AsSpan();

        Assert.Equal(4, bytes.Length);
        Assert.Equal(0xFF, bytes[0]);
        Assert.Equal(0xFE, bytes[1]);
        Assert.Equal(0x12, bytes[2]);
        Assert.Equal(0x34, bytes[3]);
    }

    [Fact]
    public void Bytes_VarInt_ZeroValue()
    {
        using TempBytes temp = ZA.Bytes(new VarInt(0));
        Assert.Equal(1, temp.Length);
        Assert.Equal(0, temp.AsSpan()[0]);
    }

    [Fact]
    public void Bytes_VarInt_MaxValue()
    {
        using TempBytes temp = ZA.Bytes(new VarInt(ulong.MaxValue));
        Assert.True(temp.Length <= 10); // VarInt max encoding size
    }

    #endregion

    // ========================================================================
    // SPECIAL VALUE TESTS
    // Tests for DateTime, Guid, and other special values
    // ========================================================================

    #region Special Value Tests

    [Fact]
    public void String_DateTimeMin_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Date: ", DateTime.MinValue);
        Assert.True(temp.Length > 6);
    }

    [Fact]
    public void String_DateTimeMax_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Date: ", DateTime.MaxValue);
        Assert.True(temp.Length > 6);
    }

    [Fact]
    public void String_GuidEmpty_FormatsCorrectly()
    {
        using TempString temp = ZA.String("GUID: ", Guid.Empty);
        Assert.Equal("GUID: 00000000-0000-0000-0000-000000000000", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_TimeSpanZero_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Zero: ", TimeSpan.Zero);
        Assert.Contains("0", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_TimeSpanNegative_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Neg: ", TimeSpan.FromHours(-1));
        Assert.Contains("-", temp.AsSpan().ToString());
    }

    #endregion

    // ========================================================================
    // BINARYPARSER ERROR HANDLING
    // Tests for insufficient data and malformed input
    // ========================================================================

    #region BinaryParser Error Handling

    [Fact]
    public void BinaryParser_EmptyBuffer_ThrowsOnRead()
    {
        byte[] empty = [];
        BinaryParser parser = new(empty);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            parser.ReadByte();
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [Fact]
    public void BinaryParser_InsufficientForUInt16_Throws()
    {
        byte[] data = [0x01];
        BinaryParser parser = new(data);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            parser.ReadUInt16BE();
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [Fact]
    public void BinaryParser_InsufficientForUInt32_Throws()
    {
        byte[] data = [0x01, 0x02, 0x03];
        BinaryParser parser = new(data);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            parser.ReadUInt32BE();
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [Fact]
    public void BinaryParser_InsufficientForUInt64_Throws()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];
        BinaryParser parser = new(data);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            parser.ReadUInt64BE();
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [Fact]
    public void BinaryParser_VarIntTooLong_Throws()
    {
        // VarInt that would exceed 64 bits
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01];
        BinaryParser parser = new(data);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            parser.ReadVarInt();
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [Fact]
    public void BinaryParser_VarIntTruncated_Throws()
    {
        // VarInt with continuation bit set but no more bytes
        byte[] data = [0x80];
        BinaryParser parser = new(data);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            parser.ReadVarInt();
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [Fact]
    public void BinaryParser_SkipBeyondBuffer_Throws()
    {
        byte[] data = [0x01, 0x02];
        BinaryParser parser = new(data);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            parser.Skip(100);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [Fact]
    public void BinaryParser_ReadBytesBeyondBuffer_Throws()
    {
        byte[] data = [0x01, 0x02];
        BinaryParser parser = new(data);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            parser.ReadBytes(100);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    #endregion

    // ========================================================================
    // BITREADER ERROR HANDLING
    // Tests for bit-level edge cases
    // ========================================================================

    #region BitReader Error Handling

    [Fact]
    public void BitReader_ReadBitsZero_Throws()
    {
        byte[] data = [0x00];
        BitReader reader = new(data);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            reader.ReadBits(0);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    [Fact]
    public void BitReader_ReadBits65_Throws()
    {
        byte[] data = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        BitReader reader = new(data);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            reader.ReadBits(65);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    [Fact]
    public void BitReader_ReadBytesNotAligned_Throws()
    {
        byte[] data = [0x00, 0x01, 0x02];
        BitReader reader = new(data);
        reader.ReadBit1(); // Now not aligned

        // Use try-catch because ref struct can't be in lambda
        try
        {
            reader.ReadBytes(2);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    #endregion

    // ========================================================================
    // SPANBUILDER OVERFLOW TESTS
    // Tests for buffer overflow handling
    // ========================================================================

    #region SpanBuilder Overflow Tests

    [Fact]
    public void SpanStringBuilder_OverflowSingleChar_Throws()
    {
        Span<char> buffer = stackalloc char[3];
        SpanStringBuilder builder = new(buffer);
        builder.Append("ABC");

        // Append(char) uses index access which throws IndexOutOfRangeException
        try
        {
            builder.Append('X');
            Assert.Fail("Expected IndexOutOfRangeException");
        }
        catch (IndexOutOfRangeException)
        {
            // Expected - direct index access on full buffer
        }
    }

    [Fact]
    public void SpanStringBuilder_OverflowString_Throws()
    {
        Span<char> buffer = stackalloc char[5];
        SpanStringBuilder builder = new(buffer);

        // Append(string) uses CopyTo which throws ArgumentException
        try
        {
            builder.Append("Too long!");
            Assert.Fail("Expected ArgumentException");
        }
        catch (ArgumentException)
        {
            // Expected - CopyTo destination too short
        }
    }

    [Fact]
    public void SpanBytesBuilder_OverflowInt32_Throws()
    {
        Span<byte> buffer = stackalloc byte[3];
        SpanBytesBuilder builder = new(buffer);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            builder.AppendInt32BigEndian(0x12345678);
            Assert.Fail("Expected exception");
        }
        catch (Exception)
        {
            // Expected
        }
    }

    [Fact]
    public void SpanBytesBuilder_OverflowInt64_Throws()
    {
        Span<byte> buffer = stackalloc byte[7];
        SpanBytesBuilder builder = new(buffer);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            builder.AppendInt64BigEndian(0x123456789ABCDEF0L);
            Assert.Fail("Expected exception");
        }
        catch (Exception)
        {
            // Expected
        }
    }

    [Fact]
    public void SpanStringBuilder_SeekBackNegative_Throws()
    {
        Span<char> buffer = stackalloc char[10];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Test");

        // Use try-catch because ref struct can't be in lambda
        try
        {
            builder.SeekBack(10);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    [Fact]
    public void SpanBytesBuilder_SeekBackNegative_Throws()
    {
        Span<byte> buffer = stackalloc byte[10];
        SpanBytesBuilder builder = new(buffer);
        builder.Append([0x01, 0x02, 0x03]);

        // Use try-catch because ref struct can't be in lambda
        try
        {
            builder.SeekBack(10);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    #endregion

    // ========================================================================
    // TEMPBUILDER EDGE CASES
    // Tests for TempStringBuilder and TempBytesBuilder edge cases
    // ========================================================================

    #region TempBuilder Edge Cases

    [Fact]
    public void TempStringBuilder_SeekBackExact_ClearsBuffer()
    {
        using TempStringBuilder builder = TempStringBuilder.Create();
        builder.Append("Hello");
        builder.SeekBack(5);

        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void TempBytesBuilder_SeekBackExact_ClearsBuffer()
    {
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        builder.Append([0x01, 0x02, 0x03]);
        builder.SeekBack(3);

        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void TempStringBuilder_ClearThenAppend_Works()
    {
        using TempStringBuilder builder = TempStringBuilder.Create();
        builder.Append("First");
        builder.Clear();
        builder.Append("Second");

        Assert.Equal("Second", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempBytesBuilder_ClearThenAppend_Works()
    {
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        builder.Append([0x01, 0x02]);
        builder.Clear();
        builder.Append([0xAA, 0xBB]);

        Assert.Equal([0xAA, 0xBB], builder.AsSpan().ToArray());
    }

    #endregion

    // ========================================================================
    // UNICODE AND ENCODING EDGE CASES
    // Tests for special Unicode characters
    // ========================================================================

    #region Unicode Edge Cases

    [Fact]
    public void TempString_EmojisAndSpecialChars_HandlesCorrectly()
    {
        using TempString temp = ZA.String($"Hello 🌍🚀 World!");
        Assert.Contains("🌍", temp.AsSpan().ToString());
        Assert.Contains("🚀", temp.AsSpan().ToString());
    }

    [Fact]
    public void TempBytes_EmojisAndSpecialChars_EncodesCorrectly()
    {
        using TempBytes temp = ZA.Utf8($"Hello 🌍🚀 World!");
        string decoded = System.Text.Encoding.UTF8.GetString(temp.AsSpan());
        Assert.Contains("🌍", decoded);
        Assert.Contains("🚀", decoded);
    }

    [Fact]
    public void TempString_NullCharInString_HandlesCorrectly()
    {
        string withNull = "Before\0After";
        using TempString temp = ZA.String($"{withNull}");
        // The null char is included in the output
        Assert.Equal("Before\0After", temp.AsSpan().ToString());
    }

    [Fact]
    public void TempBytes_SurrogatePairs_EncodesCorrectly()
    {
        // 𝄞 (Musical G Clef) - requires surrogate pair in UTF-16
        string musicNote = "𝄞";
        using TempBytes temp = ZA.Utf8($"{musicNote}");
        string decoded = System.Text.Encoding.UTF8.GetString(temp.AsSpan());
        Assert.Equal(musicNote, decoded);
    }

    [Fact]
    public void TempString_ControlCharacters_HandlesCorrectly()
    {
        using TempString temp = ZA.String($"Tab:\tNewline:\nCarriage:\r");
        Assert.Contains("\t", temp.AsSpan().ToString());
        Assert.Contains("\n", temp.AsSpan().ToString());
        Assert.Contains("\r", temp.AsSpan().ToString());
    }

    #endregion

    // ========================================================================
    // BINARYPARSER BOUNDARY VALUES
    // Tests for parsing boundary values
    // ========================================================================

    #region BinaryParser Boundary Values

    [Fact]
    public void BinaryParser_ReadMaxUInt16BE_ReturnsCorrectValue()
    {
        byte[] data = [0xFF, 0xFF];
        BinaryParser parser = new(data);

        Assert.Equal(ushort.MaxValue, parser.ReadUInt16BE());
    }

    [Fact]
    public void BinaryParser_ReadMaxUInt32BE_ReturnsCorrectValue()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF];
        BinaryParser parser = new(data);

        Assert.Equal(uint.MaxValue, parser.ReadUInt32BE());
    }

    [Fact]
    public void BinaryParser_ReadMaxUInt64BE_ReturnsCorrectValue()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        BinaryParser parser = new(data);

        Assert.Equal(ulong.MaxValue, parser.ReadUInt64BE());
    }

    [Fact]
    public void BinaryParser_ReadMinInt16BE_ReturnsCorrectValue()
    {
        byte[] data = [0x80, 0x00];
        BinaryParser parser = new(data);

        Assert.Equal(short.MinValue, parser.ReadInt16BE());
    }

    [Fact]
    public void BinaryParser_ReadMinInt32BE_ReturnsCorrectValue()
    {
        byte[] data = [0x80, 0x00, 0x00, 0x00];
        BinaryParser parser = new(data);

        Assert.Equal(int.MinValue, parser.ReadInt32BE());
    }

    [Fact]
    public void BinaryParser_ReadMinInt64BE_ReturnsCorrectValue()
    {
        byte[] data = [0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        BinaryParser parser = new(data);

        Assert.Equal(long.MinValue, parser.ReadInt64BE());
    }

    #endregion

    // ========================================================================
    // VARINT EDGE CASES
    // Tests for VarInt boundary values
    // ========================================================================

    #region VarInt Edge Cases

    [Fact]
    public void BinaryParser_VarInt_Zero_ReturnsCorrectly()
    {
        byte[] data = [0x00];
        BinaryParser parser = new(data);

        Assert.Equal(0UL, parser.ReadVarInt());
    }

    [Fact]
    public void BinaryParser_VarInt_127_SingleByte()
    {
        byte[] data = [0x7F];
        BinaryParser parser = new(data);

        Assert.Equal(127UL, parser.ReadVarInt());
        Assert.Equal(1, parser.Position);
    }

    [Fact]
    public void BinaryParser_VarInt_128_TwoBytes()
    {
        byte[] data = [0x80, 0x01];
        BinaryParser parser = new(data);

        Assert.Equal(128UL, parser.ReadVarInt());
        Assert.Equal(2, parser.Position);
    }

    [Fact]
    public void BinaryParser_VarIntZigZag_MinusOne_IsOne()
    {
        byte[] data = [0x01]; // ZigZag(-1) = 1
        BinaryParser parser = new(data);

        Assert.Equal(-1L, parser.ReadVarIntZigZag());
    }

    [Fact]
    public void BinaryParser_VarIntZigZag_One_IsTwo()
    {
        byte[] data = [0x02]; // ZigZag(1) = 2
        BinaryParser parser = new(data);

        Assert.Equal(1L, parser.ReadVarIntZigZag());
    }

    #endregion

    // ========================================================================
    // FLOAT/DOUBLE SPECIAL VALUES
    // Tests for NaN, Infinity, etc.
    // ========================================================================

    #region Float/Double Special Values

    [Fact]
    public void BinaryParser_ReadF32BE_PositiveInfinity()
    {
        // IEEE 754: +Infinity = 0x7F800000
        byte[] data = [0x7F, 0x80, 0x00, 0x00];
        BinaryParser parser = new(data);

        Assert.True(float.IsPositiveInfinity(parser.ReadF32BE()));
    }

    [Fact]
    public void BinaryParser_ReadF32BE_NegativeInfinity()
    {
        // IEEE 754: -Infinity = 0xFF800000
        byte[] data = [0xFF, 0x80, 0x00, 0x00];
        BinaryParser parser = new(data);

        Assert.True(float.IsNegativeInfinity(parser.ReadF32BE()));
    }

    [Fact]
    public void BinaryParser_ReadF32BE_NaN()
    {
        // IEEE 754: NaN = 0x7FC00000
        byte[] data = [0x7F, 0xC0, 0x00, 0x00];
        BinaryParser parser = new(data);

        Assert.True(float.IsNaN(parser.ReadF32BE()));
    }

    [Fact]
    public void BinaryParser_ReadF64BE_PositiveInfinity()
    {
        // IEEE 754: +Infinity = 0x7FF0000000000000
        byte[] data = [0x7F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        BinaryParser parser = new(data);

        Assert.True(double.IsPositiveInfinity(parser.ReadF64BE()));
    }

    [Fact]
    public void BinaryParser_ReadF64BE_NegativeInfinity()
    {
        // IEEE 754: -Infinity = 0xFFF0000000000000
        byte[] data = [0xFF, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        BinaryParser parser = new(data);

        Assert.True(double.IsNegativeInfinity(parser.ReadF64BE()));
    }

    [Fact]
    public void BinaryParser_ReadF64BE_NaN()
    {
        // IEEE 754: NaN = 0x7FF8000000000000
        byte[] data = [0x7F, 0xF8, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        BinaryParser parser = new(data);

        Assert.True(double.IsNaN(parser.ReadF64BE()));
    }

    #endregion
}

