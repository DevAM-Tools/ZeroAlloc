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
// TempBytes Tests - Comprehensive UTF-8 and Binary Formatting Tests
// ============================================================================
// Tests for TempBytes: UTF-8 encoding, binary serialization, nested calls,
// endian wrappers, VarInt encoding, and edge cases.
// ============================================================================

namespace ZeroAlloc.Tests;

/// <summary>
/// Comprehensive tests for <see cref="TempBytes"/>, <see cref="ZA.Utf8"/>, and <see cref="ZA.Bytes"/> methods.
/// </summary>
public class TempBytesTests
{
    // ========================================================================
    // BASIC UTF-8 TESTS
    // Tests for fundamental UTF-8 encoding operations
    // ========================================================================

    #region Basic UTF-8 Tests

    [Fact]
    public void Utf8_SimpleString_ReturnsCorrectBytes()
    {
        using TempBytes temp = ZA.Utf8("Hello");
        string result = Encoding.UTF8.GetString(temp.AsSpan());
        Assert.Equal("Hello", result);
    }

    [Fact]
    public void Utf8_IntValue_FormatsCorrectly()
    {
        using TempBytes temp = ZA.Utf8("Value: ", 42);
        string result = Encoding.UTF8.GetString(temp.AsSpan());
        Assert.Equal("Value: 42", result);
    }

    [Fact]
    public void Utf8_MixedTypes_FormatsCorrectly()
    {
        using TempBytes temp = ZA.Utf8("User ", 12345, " logged in");
        string result = Encoding.UTF8.GetString(temp.AsSpan());
        Assert.Equal("User 12345 logged in", result);
    }

    [Fact]
    public void Utf8_Length_ReturnsCorrectByteCount()
    {
        using TempBytes temp = ZA.Utf8("Test", 123);
        Assert.Equal(7, temp.Length); // "Test123" = 7 ASCII bytes
    }

    [Fact]
    public void Utf8_EmptyString_ReturnsEmpty()
    {
        using TempBytes temp = ZA.Utf8("");
        Assert.Equal(0, temp.Length);
    }

    [Fact]
    public void Utf8_BooleanTrue_FormatsCorrectly()
    {
        using TempBytes temp = ZA.Utf8("Result: ", true);
        string result = Encoding.UTF8.GetString(temp.AsSpan());
        Assert.Equal("Result: True", result);
    }

    [Fact]
    public void Utf8_BooleanFalse_FormatsCorrectly()
    {
        using TempBytes temp = ZA.Utf8("Result: ", false);
        string result = Encoding.UTF8.GetString(temp.AsSpan());
        Assert.Equal("Result: False", result);
    }

    #endregion

    // ========================================================================
    // NESTED CALL TESTS
    // Tests for graceful handling of recursive/nested ZeroAlloc calls
    // ========================================================================

    #region Nested Call Tests

    [Fact]
    public void Utf8_NestedCalls_OuterUsesThreadStatic()
    {
        using TempBytes outer = ZA.Utf8("Outer");
        Assert.False(outer.IsHeapAllocated);
    }

    [Fact]
    public void Utf8_NestedCalls_InnerUsesHeapFallback()
    {
        using TempBytes outer = ZA.Utf8("Outer");
        using TempBytes inner = ZA.Utf8("Inner");

        Assert.False(outer.IsHeapAllocated);
        Assert.True(inner.IsHeapAllocated);
    }

    [Fact]
    public void Utf8_NestedCalls_BothReturnCorrectContent()
    {
        using TempBytes outer = ZA.Utf8("Outer: ", 100);
        using TempBytes inner = ZA.Utf8("Inner: ", 200);

        Assert.Equal("Outer: 100", Encoding.UTF8.GetString(outer.AsSpan()));
        Assert.Equal("Inner: 200", Encoding.UTF8.GetString(inner.AsSpan()));
    }

    [Fact]
    public void Utf8_AfterDispose_ThreadStaticReusable()
    {
        TempBytes temp = ZA.Utf8("Test");
        Assert.False(temp.IsHeapAllocated);
        temp.Dispose();

        using TempBytes temp2 = ZA.Utf8("Test2");
        Assert.False(temp2.IsHeapAllocated);
    }

    #endregion

    // ========================================================================
    // TRYUTF8 TESTS
    // Tests for TryUtf8 methods
    // ========================================================================

    #region TryUtf8 Tests

    [Fact]
    public void TryUtf8_ValidInput_ReturnsTrue()
    {
        bool success = ZA.TryUtf8(out TempBytes result, "Test: ", 42);
        using (result)
        {
            Assert.True(success);
            string str = Encoding.UTF8.GetString(result.AsSpan());
            Assert.Equal("Test: 42", str);
        }
    }

    [Fact]
    public void TryUtf8_NestedCalls_GracefullyFallsBack()
    {
        bool success1 = ZA.TryUtf8(out TempBytes result1, "First");
        bool success2 = ZA.TryUtf8(out TempBytes result2, "Second");

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
    // UNICODE ENCODING TESTS
    // Tests for correct UTF-8 encoding of various Unicode characters
    // ========================================================================

    #region Unicode Encoding Tests

    [Fact]
    public void Utf8_Emoji_EncodesCorrectly()
    {
        using TempBytes temp = ZA.Utf8("🎉");
        // Emoji 🎉 is 4 bytes in UTF-8 (F0 9F 8E 89)
        Assert.Equal(4, temp.Length);
        Assert.Equal("🎉", Encoding.UTF8.GetString(temp.AsSpan()));
    }

    [Fact]
    public void Utf8_GermanUmlauts_EncodesCorrectly()
    {
        using TempBytes temp = ZA.Utf8("German: äöü");
        string result = Encoding.UTF8.GetString(temp.AsSpan());
        Assert.Equal("German: äöü", result);
    }

    [Fact]
    public void Utf8_ChineseCharacters_EncodesCorrectly()
    {
        using TempBytes temp = ZA.Utf8("你好");
        // Chinese characters are 3 bytes each in UTF-8
        Assert.Equal(6, temp.Length);
        Assert.Equal("你好", Encoding.UTF8.GetString(temp.AsSpan()));
    }

    [Fact]
    public void Utf8_SurrogatePair_EncodesCorrectly()
    {
        // Musical G clef: U+1D11E
        using TempBytes temp = ZA.Utf8("𝄞");
        Assert.Equal(4, temp.Length); // 4-byte UTF-8 sequence
        Assert.Equal("𝄞", Encoding.UTF8.GetString(temp.AsSpan()));
    }

    [Fact]
    public void Utf8_Euro_EncodesCorrectly()
    {
        using TempBytes temp = ZA.Utf8("€");
        // Euro sign is 3 bytes in UTF-8 (E2 82 AC)
        Assert.Equal(3, temp.Length);
        Assert.Equal("€", Encoding.UTF8.GetString(temp.AsSpan()));
    }

    [Fact]
    public void Utf8_MixedContent_EncodesCorrectly()
    {
        using TempBytes temp = ZA.Utf8("Hello 你好 🌍");
        string result = Encoding.UTF8.GetString(temp.AsSpan());
        Assert.Equal("Hello 你好 🌍", result);
    }

    #endregion

    // ========================================================================
    // ENDIAN WRAPPER TESTS
    // Tests for big-endian and little-endian serialization
    // ========================================================================

    #region Endian Wrapper Tests

    [Fact]
    public void Bytes_U16BE_SerializesCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new U16BE(0x1234));
        ReadOnlySpan<byte> bytes = temp.AsSpan();
        Assert.Equal(2, bytes.Length);
        Assert.Equal(0x12, bytes[0]);
        Assert.Equal(0x34, bytes[1]);
    }

    [Fact]
    public void Bytes_U16LE_SerializesCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new U16LE(0x1234));
        ReadOnlySpan<byte> bytes = temp.AsSpan();
        Assert.Equal(2, bytes.Length);
        Assert.Equal(0x34, bytes[0]);
        Assert.Equal(0x12, bytes[1]);
    }

    [Fact]
    public void Bytes_U32BE_SerializesCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new U32BE(0x12345678));
        ReadOnlySpan<byte> bytes = temp.AsSpan();
        Assert.Equal(4, bytes.Length);
        Assert.Equal(0x12, bytes[0]);
        Assert.Equal(0x34, bytes[1]);
        Assert.Equal(0x56, bytes[2]);
        Assert.Equal(0x78, bytes[3]);
    }

    [Fact]
    public void Bytes_U32LE_SerializesCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new U32LE(0x12345678));
        ReadOnlySpan<byte> bytes = temp.AsSpan();
        Assert.Equal(4, bytes.Length);
        Assert.Equal(0x78, bytes[0]);
        Assert.Equal(0x56, bytes[1]);
        Assert.Equal(0x34, bytes[2]);
        Assert.Equal(0x12, bytes[3]);
    }

    [Fact]
    public void Bytes_U64BE_SerializesCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new U64BE(0x123456789ABCDEF0));
        ReadOnlySpan<byte> bytes = temp.AsSpan();
        Assert.Equal(8, bytes.Length);
        Assert.Equal(0x12, bytes[0]);
        Assert.Equal(0xF0, bytes[7]);
    }

    [Fact]
    public void Bytes_U64LE_SerializesCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new U64LE(0x123456789ABCDEF0));
        ReadOnlySpan<byte> bytes = temp.AsSpan();
        Assert.Equal(8, bytes.Length);
        Assert.Equal(0xF0, bytes[0]);
        Assert.Equal(0x12, bytes[7]);
    }

    [Fact]
    public void Bytes_I16BE_SerializesNegativeCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new I16BE(-1));
        ReadOnlySpan<byte> bytes = temp.AsSpan();
        Assert.Equal(2, bytes.Length);
        Assert.Equal(0xFF, bytes[0]);
        Assert.Equal(0xFF, bytes[1]);
    }

    [Fact]
    public void Bytes_I32BE_SerializesNegativeCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new I32BE(-1));
        ReadOnlySpan<byte> bytes = temp.AsSpan();
        Assert.Equal(4, bytes.Length);
        Assert.All(bytes.ToArray(), b => Assert.Equal(0xFF, b));
    }

    [Fact]
    public void Bytes_F32BE_SerializesCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new F32BE(1.0f));
        Assert.Equal(4, temp.Length);
    }

    [Fact]
    public void Bytes_F64BE_SerializesCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new F64BE(1.0));
        Assert.Equal(8, temp.Length);
    }

    #endregion

    // ========================================================================
    // RAW BYTES TESTS
    // Tests for raw byte array serialization
    // ========================================================================

    #region Raw Bytes Tests

    [Fact]
    public void Bytes_Raw_CopiesCorrectly()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        using TempBytes temp = ZA.Bytes(new Raw(data));
        ReadOnlySpan<byte> bytes = temp.AsSpan();

        Assert.Equal(4, bytes.Length);
        Assert.Equal(0x01, bytes[0]);
        Assert.Equal(0x04, bytes[3]);
    }

    [Fact]
    public void Bytes_RawEmpty_ReturnsEmpty()
    {
        byte[] data = [];
        using TempBytes temp = ZA.Bytes(new Raw(data));
        Assert.Equal(0, temp.Length);
    }

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

    #endregion

    // ========================================================================
    // VARINT TESTS
    // Tests for variable-length integer encoding
    // ========================================================================

    #region VarInt Tests

    [Fact]
    public void Bytes_VarInt_SmallValue_SingleByte()
    {
        using TempBytes temp = ZA.Bytes(new VarInt(127));
        Assert.Equal(1, temp.Length);
        Assert.Equal(127, temp.AsSpan()[0]);
    }

    [Fact]
    public void Bytes_VarInt_TwoByteValue()
    {
        using TempBytes temp = ZA.Bytes(new VarInt(128));
        Assert.Equal(2, temp.Length);
    }

    [Fact]
    public void Bytes_VarInt_LargeValue()
    {
        using TempBytes temp = ZA.Bytes(new VarInt(int.MaxValue));
        Assert.True(temp.Length <= 5);
    }

    [Fact]
    public void Bytes_VarIntZigZag_PositiveValue()
    {
        using TempBytes temp = ZA.Bytes(new VarIntZigZag(1));
        Assert.Equal(2, temp.AsSpan()[0]); // 1 encodes as 2 in ZigZag
    }

    [Fact]
    public void Bytes_VarIntZigZag_NegativeValue()
    {
        using TempBytes temp = ZA.Bytes(new VarIntZigZag(-1));
        Assert.Equal(1, temp.AsSpan()[0]); // -1 encodes as 1 in ZigZag
    }

    [Fact]
    public void Bytes_VarIntZigZag_Zero()
    {
        using TempBytes temp = ZA.Bytes(new VarIntZigZag(0));
        Assert.Equal(1, temp.Length);
        Assert.Equal(0, temp.AsSpan()[0]);
    }

    #endregion

    // ========================================================================
    // SEQUENTIAL CALL TESTS
    // Tests for repeated sequential usage
    // ========================================================================

    #region Sequential Call Tests

    [Fact]
    public void Utf8_SequentialCalls_WorkCorrectly()
    {
        for (int i = 0; i < 100; i++)
        {
            using TempBytes temp = ZA.Utf8("Iteration: ", i);
            Assert.Equal($"Iteration: {i}", Encoding.UTF8.GetString(temp.AsSpan()));
        }
    }

    [Fact]
    public void Bytes_SequentialCalls_AllUseThreadStatic()
    {
        for (int i = 0; i < 10; i++)
        {
            using TempBytes temp = ZA.Bytes(new U32BE((uint)i));
            Assert.False(temp.IsHeapAllocated);
        }
    }

    #endregion

    // ========================================================================
    // ENCODING WRAPPER TESTS
    // Tests for ASCII and other encoding wrappers
    // ========================================================================

    #region Encoding Wrapper Tests

    [Fact]
    public void Bytes_Ascii_EncodesCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new Ascii("Hello"));
        Assert.Equal(5, temp.Length);
        Assert.Equal((byte)'H', temp.AsSpan()[0]);
        Assert.Equal((byte)'o', temp.AsSpan()[4]);
    }

    [Fact]
    public void Bytes_Utf8Encoded_EncodesCorrectly()
    {
        using TempBytes temp = ZA.Bytes(new Utf8("äöü"));
        // Each umlaut is 2 bytes in UTF-8
        Assert.Equal(6, temp.Length);
    }

    #endregion

    // ========================================================================
    // COMBINED FORMATTING TESTS
    // Tests for combining multiple wrapper types
    // ========================================================================

    #region Combined Formatting Tests

    [Fact]
    public void Bytes_MultipleWrappers_SerializeInOrder()
    {
        using TempBytes temp = ZA.Bytes(
            new U16BE(0x0001),   // 2 bytes
            new U32BE(0x12345678), // 4 bytes
            new Raw([0xAB, 0xCD])  // 2 bytes
        );

        ReadOnlySpan<byte> bytes = temp.AsSpan();
        Assert.Equal(8, bytes.Length);

        // First U16BE
        Assert.Equal(0x00, bytes[0]);
        Assert.Equal(0x01, bytes[1]);

        // Then U32BE
        Assert.Equal(0x12, bytes[2]);
        Assert.Equal(0x78, bytes[5]);

        // Finally Raw
        Assert.Equal(0xAB, bytes[6]);
        Assert.Equal(0xCD, bytes[7]);
    }

    [Fact]
    public void Bytes_StringWithBinaryPrefix_WorksCorrectly()
    {
        using TempBytes temp = ZA.Bytes(
            new U16BE(5),        // Length prefix
            new Ascii("Hello")     // String data
        );

        ReadOnlySpan<byte> bytes = temp.AsSpan();
        Assert.Equal(7, bytes.Length);
        Assert.Equal(0x00, bytes[0]); // High byte of length
        Assert.Equal(0x05, bytes[1]); // Low byte of length
        Assert.Equal((byte)'H', bytes[2]);
    }

    #endregion
}

