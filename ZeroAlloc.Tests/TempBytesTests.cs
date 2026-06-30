// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

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
public sealed class TempBytesTests
{
    // ========================================================================
    // BASIC UTF-8 TESTS
    // Tests for fundamental UTF-8 encoding operations
    // ========================================================================
    #region Basic UTF-8 Tests

    [Test]
    public async Task Utf8_SimpleString_ReturnsCorrectBytes()
    {
        string content;
        {
            using TempBytes temp = ZA.Utf8("Hello");
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(content).IsEqualTo("Hello");
    }

    [Test]
    public async Task Utf8_IntValue_FormatsCorrectly()
    {
        string content;
        {
            using TempBytes temp = ZA.Utf8("Value: ", 42);
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(content).IsEqualTo("Value: 42");
    }

    [Test]
    public async Task Utf8_MixedTypes_FormatsCorrectly()
    {
        string content;
        {
            using TempBytes temp = ZA.Utf8("User ", 12345, " logged in");
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(content).IsEqualTo("User 12345 logged in");
    }

    [Test]
    public async Task Utf8_Length_ReturnsCorrectByteCount()
    {
        int length;
        {
            using TempBytes temp = ZA.Utf8("Test", 123);
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(7); // "Test123" = 7 ASCII bytes
    }

    [Test]
    [Arguments(0, "TempBytes[0 bytes]")]
    [Arguments(5, "TempBytes[5 bytes]")]
    [Arguments(128, "TempBytes[128 bytes]")]
    public async Task TempBytes_ToString_ReturnsDebugRepresentation(int byteCount, string expected)
    {
        byte[] buffer = new byte[byteCount];
        string representation;
        {
            using TempBytes temp = new(buffer, byteCount, isThreadStatic: false);
            representation = temp.ToString();
        }

        await Assert.That(representation).IsEqualTo(expected);
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
    public async Task TempBytes_ToArray_Empty_ReturnsEmptyArray()
    {
        byte[] array;
        {
            using TempBytes temp = ZA.Utf8("");
            array = temp.ToArray();
        }

        await Assert.That(array).IsEmpty();
        await Assert.That(array).IsSameReferenceAs(Array.Empty<byte>());
    }

    [Test]
    public async Task TempBytes_ToArray_NonEmpty_ReturnsIndependentCopy()
    {
        byte[] array;
        {
            using TempBytes temp = ZA.Utf8("abc");
            array = temp.ToArray();
        }

        await Assert.That(array).IsEquivalentTo("abc"u8.ToArray());
    }

    [Test]
    public async Task TempBytes_IsEmpty_TrueForZeroLength()
    {
        bool isEmpty;
        {
            using TempBytes temp = ZA.Utf8("");
            isEmpty = temp.IsEmpty;
        }

        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task TempBytes_Dispose_HeapAllocated_CompletesWithoutError()
    {
        bool innerWasHeapAllocated;
        {
            using TempBytes outer = ZA.Utf8("outer");
            using TempBytes inner = ZA.Utf8("inner");
            innerWasHeapAllocated = inner.IsHeapAllocated;
        }

        await Assert.That(innerWasHeapAllocated).IsTrue();
    }

    [Test]
    public async Task Utf8_BooleanTrue_FormatsCorrectly()
    {
        string content;
        {
            using TempBytes temp = ZA.Utf8("Result: ", true);
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(content).IsEqualTo("Result: True");
    }

    [Test]
    public async Task Utf8_BooleanFalse_FormatsCorrectly()
    {
        string content;
        {
            using TempBytes temp = ZA.Utf8("Result: ", false);
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(content).IsEqualTo("Result: False");
    }

    #endregion

    // ========================================================================
    // NESTED CALL TESTS
    // Tests for graceful handling of recursive/nested ZeroAlloc calls
    // ========================================================================

    #region Nested Call Tests

    [Test]
    public async Task Utf8_NestedCalls_OuterUsesThreadStatic()
    {
        bool isHeapAllocated;
        {
            using TempBytes outer = ZA.Utf8("Outer");
            isHeapAllocated = outer.IsHeapAllocated;
        }
        await Assert.That(isHeapAllocated).IsFalse();
    }

    [Test]
    public async Task Utf8_NestedCalls_InnerUsesHeapFallback()
    {
        bool outerIsHeapAllocated;
        bool innerIsHeapAllocated;
        {
            using TempBytes outer = ZA.Utf8("Outer");
            using TempBytes inner = ZA.Utf8("Inner");
            outerIsHeapAllocated = outer.IsHeapAllocated;
            innerIsHeapAllocated = inner.IsHeapAllocated;
        }
        await Assert.That(outerIsHeapAllocated).IsFalse();
        await Assert.That(innerIsHeapAllocated).IsTrue();
    }

    [Test]
    public async Task Utf8_NestedCalls_BothReturnCorrectContent()
    {
        string outerContent;
        string innerContent;
        {
            using TempBytes outer = ZA.Utf8("Outer: ", 100);
            using TempBytes inner = ZA.Utf8("Inner: ", 200);
            outerContent = Encoding.UTF8.GetString(outer.AsSpan());
            innerContent = Encoding.UTF8.GetString(inner.AsSpan());
        }
        await Assert.That(outerContent).IsEqualTo("Outer: 100");
        await Assert.That(innerContent).IsEqualTo("Inner: 200");
    }

    [Test]
    public async Task Utf8_AfterDispose_ThreadStaticReusable()
    {
        bool isHeapAllocated1;
        {
            TempBytes temp = ZA.Utf8("Test");
            isHeapAllocated1 = temp.IsHeapAllocated;
            temp.Dispose();
        }
        bool isHeapAllocated2;
        {
            using TempBytes temp2 = ZA.Utf8("Test2");
            isHeapAllocated2 = temp2.IsHeapAllocated;
        }
        await Assert.That(isHeapAllocated1).IsFalse();
        await Assert.That(isHeapAllocated2).IsFalse();
    }

    #endregion

    // ========================================================================
    // TRYUTF8 TESTS
    // Tests for TryUtf8 methods
    // ========================================================================

    #region TryUtf8 Tests

    [Test]
    public async Task TryUtf8_ValidInput_ReturnsTrue()
    {
        bool success = ZA.TryUtf8(out TempBytes result, "Test: ", 42);
        string str;
        using (result)
        {
            str = Encoding.UTF8.GetString(result.AsSpan());
        }
        await Assert.That(success).IsTrue();
        await Assert.That(str).IsEqualTo("Test: 42");
    }

    [Test]
    public async Task TryUtf8_NestedCalls_GracefullyFallsBack()
    {
        bool success1 = ZA.TryUtf8(out TempBytes result1, "First");
        bool success2 = ZA.TryUtf8(out TempBytes result2, "Second");

        bool isHeapAllocated1;
        bool isHeapAllocated2;
        using (result1)
        using (result2)
        {
            isHeapAllocated1 = result1.IsHeapAllocated;
            isHeapAllocated2 = result2.IsHeapAllocated;
        }
        await Assert.That(success1).IsTrue();
        await Assert.That(success2).IsTrue();
        await Assert.That(isHeapAllocated1).IsFalse();
        await Assert.That(isHeapAllocated2).IsTrue();
    }

    #endregion

    // ========================================================================
    // UNICODE ENCODING TESTS
    // Tests for correct UTF-8 encoding of various Unicode characters
    // ========================================================================

    #region Unicode Encoding Tests

    [Test]
    public async Task Utf8_Emoji_EncodesCorrectly()
    {
        int length;
        string content;
        {
            using TempBytes temp = ZA.Utf8("🎉");
            // Emoji 🎉 is 4 bytes in UTF-8 (F0 9F 8E 89)
            length = temp.Length;
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(length).IsEqualTo(4);
        await Assert.That(content).IsEqualTo("🎉");
    }

    [Test]
    public async Task Utf8_GermanUmlauts_EncodesCorrectly()
    {
        string content;
        {
            using TempBytes temp = ZA.Utf8("German: äöü");
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(content).IsEqualTo("German: äöü");
    }

    [Test]
    public async Task Utf8_ChineseCharacters_EncodesCorrectly()
    {
        int length;
        string content;
        {
            using TempBytes temp = ZA.Utf8("你好");
            // Chinese characters are 3 bytes each in UTF-8
            length = temp.Length;
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(length).IsEqualTo(6);
        await Assert.That(content).IsEqualTo("你好");
    }

    [Test]
    public async Task Utf8_SurrogatePair_EncodesCorrectly()
    {
        int length;
        string content;
        {
            // Musical G clef: U+1D11E
            using TempBytes temp = ZA.Utf8("𝄞");
            length = temp.Length;
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(length).IsEqualTo(4); // 4-byte UTF-8 sequence
        await Assert.That(content).IsEqualTo("𝄞");
    }

    [Test]
    public async Task Utf8_Euro_EncodesCorrectly()
    {
        int length;
        string content;
        {
            using TempBytes temp = ZA.Utf8("€");
            // Euro sign is 3 bytes in UTF-8 (E2 82 AC)
            length = temp.Length;
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(length).IsEqualTo(3);
        await Assert.That(content).IsEqualTo("€");
    }

    [Test]
    public async Task Utf8_MixedContent_EncodesCorrectly()
    {
        string content;
        {
            using TempBytes temp = ZA.Utf8("Hello 你好 🌍");
            content = Encoding.UTF8.GetString(temp.AsSpan());
        }
        await Assert.That(content).IsEqualTo("Hello 你好 🌍");
    }

    #endregion

    // ========================================================================
    // ENDIAN WRAPPER TESTS
    // Tests for big-endian and little-endian serialization
    // ========================================================================

    #region Endian Wrapper Tests

    [Test]
    public async Task Bytes_U16BE_SerializesCorrectly()
    {
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new U16BE(0x1234));
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(2);
        await Assert.That((int)bytes[0]).IsEqualTo(0x12);
        await Assert.That((int)bytes[1]).IsEqualTo(0x34);
    }

    [Test]
    public async Task Bytes_U16LE_SerializesCorrectly()
    {
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new U16LE(0x1234));
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(2);
        await Assert.That((int)bytes[0]).IsEqualTo(0x34);
        await Assert.That((int)bytes[1]).IsEqualTo(0x12);
    }

    [Test]
    public async Task Bytes_U32BE_SerializesCorrectly()
    {
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new U32BE(0x12345678));
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(4);
        await Assert.That((int)bytes[0]).IsEqualTo(0x12);
        await Assert.That((int)bytes[1]).IsEqualTo(0x34);
        await Assert.That((int)bytes[2]).IsEqualTo(0x56);
        await Assert.That((int)bytes[3]).IsEqualTo(0x78);
    }

    [Test]
    public async Task Bytes_U32LE_SerializesCorrectly()
    {
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new U32LE(0x12345678));
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(4);
        await Assert.That((int)bytes[0]).IsEqualTo(0x78);
        await Assert.That((int)bytes[1]).IsEqualTo(0x56);
        await Assert.That((int)bytes[2]).IsEqualTo(0x34);
        await Assert.That((int)bytes[3]).IsEqualTo(0x12);
    }

    [Test]
    public async Task Bytes_U64BE_SerializesCorrectly()
    {
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new U64BE(0x123456789ABCDEF0));
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(8);
        await Assert.That((int)bytes[0]).IsEqualTo(0x12);
        await Assert.That((int)bytes[7]).IsEqualTo(0xF0);
    }

    [Test]
    public async Task Bytes_U64LE_SerializesCorrectly()
    {
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new U64LE(0x123456789ABCDEF0));
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(8);
        await Assert.That((int)bytes[0]).IsEqualTo(0xF0);
        await Assert.That((int)bytes[7]).IsEqualTo(0x12);
    }

    [Test]
    public async Task Bytes_I16BE_SerializesNegativeCorrectly()
    {
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new I16BE(-1));
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(2);
        await Assert.That((int)bytes[0]).IsEqualTo(0xFF);
        await Assert.That((int)bytes[1]).IsEqualTo(0xFF);
    }

    [Test]
    public async Task Bytes_I32BE_SerializesNegativeCorrectly()
    {
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new I32BE(-1));
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(4);
        await Assert.That((int)bytes[0]).IsEqualTo(0xFF);
    }

    [Test]
    public async Task Bytes_F32BE_SerializesCorrectly()
    {
        int length;
        {
            using TempBytes temp = ZA.Bytes(new F32BE(1.0f));
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(4);
    }

    [Test]
    public async Task Bytes_F64BE_SerializesCorrectly()
    {
        int length;
        {
            using TempBytes temp = ZA.Bytes(new F64BE(1.0));
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(8);
    }

    #endregion

    // ========================================================================
    // RAW BYTES TESTS
    // Tests for raw byte array serialization
    // ========================================================================

    #region Raw Bytes Tests

    [Test]
    public async Task Bytes_Raw_CopiesCorrectly()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new Raw(data));
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(4);
        await Assert.That((int)bytes[0]).IsEqualTo(0x01);
        await Assert.That((int)bytes[3]).IsEqualTo(0x04);
    }

    [Test]
    public async Task Bytes_RawEmpty_ReturnsEmpty()
    {
        byte[] data = [];
        int length;
        {
            using TempBytes temp = ZA.Bytes(new Raw(data));
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Bytes_RawAndEndian_CombineCorrectly()
    {
        byte[] rawData = [0xFF, 0xFE];
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new Raw(rawData), new U16BE(0x1234));
            bytes = temp.AsSpan().ToArray();
        }

        await Assert.That(bytes.Length).IsEqualTo(4);
        await Assert.That((int)bytes[0]).IsEqualTo(0xFF);
        await Assert.That((int)bytes[1]).IsEqualTo(0xFE);
        await Assert.That((int)bytes[2]).IsEqualTo(0x12);
        await Assert.That((int)bytes[3]).IsEqualTo(0x34);
    }

    #endregion

    // ========================================================================
    // VARINT TESTS
    // Tests for variable-length integer encoding
    // ========================================================================

    #region VarInt Tests

    [Test]
    public async Task Bytes_VarInt_SmallValue_SingleByte()
    {
        int length;
        byte firstByte;
        {
            using TempBytes temp = ZA.Bytes(new VarInt(127));
            length = temp.Length;
            firstByte = temp.AsSpan()[0];
        }
        await Assert.That(length).IsEqualTo(1);
        await Assert.That((int)firstByte).IsEqualTo(127);
    }

    [Test]
    public async Task Bytes_VarInt_TwoByteValue()
    {
        int length;
        {
            using TempBytes temp = ZA.Bytes(new VarInt(128));
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(2);
    }

    [Test]
    public async Task Bytes_VarInt_LargeValue()
    {
        int length;
        {
            using TempBytes temp = ZA.Bytes(new VarInt(int.MaxValue));
            length = temp.Length;
        }
        await Assert.That(length <= 5).IsTrue();
    }

    [Test]
    public async Task Bytes_VarIntZigZag_PositiveValue()
    {
        byte firstByte;
        {
            using TempBytes temp = ZA.Bytes(new VarIntZigZag(1));
            firstByte = temp.AsSpan()[0];
        }
        await Assert.That((int)firstByte).IsEqualTo(2); // 1 encodes as 2 in ZigZag
    }

    [Test]
    public async Task Bytes_VarIntZigZag_NegativeValue()
    {
        byte firstByte;
        {
            using TempBytes temp = ZA.Bytes(new VarIntZigZag(-1));
            firstByte = temp.AsSpan()[0];
        }
        await Assert.That((int)firstByte).IsEqualTo(1); // -1 encodes as 1 in ZigZag
    }

    [Test]
    public async Task Bytes_VarIntZigZag_Zero()
    {
        int length;
        byte firstByte;
        {
            using TempBytes temp = ZA.Bytes(new VarIntZigZag(0));
            length = temp.Length;
            firstByte = temp.AsSpan()[0];
        }
        await Assert.That(length).IsEqualTo(1);
        await Assert.That((int)firstByte).IsEqualTo(0);
    }

    #endregion

    // ========================================================================
    // SEQUENTIAL CALL TESTS
    // Tests for repeated sequential usage
    // ========================================================================

    #region Sequential Call Tests

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

    [Test]
    public async Task Bytes_SequentialCalls_AllUseThreadStatic()
    {
        for (int i = 0; i < 10; i++)
        {
            bool isHeapAllocated;
            {
                using TempBytes temp = ZA.Bytes(new U32BE((uint)i));
                isHeapAllocated = temp.IsHeapAllocated;
            }
            await Assert.That(isHeapAllocated).IsFalse();
        }
    }

    #endregion

    // ========================================================================
    // ENCODING WRAPPER TESTS
    // Tests for ASCII and other encoding wrappers
    // ========================================================================

    #region Encoding Wrapper Tests

    [Test]
    public async Task Bytes_Ascii_EncodesCorrectly()
    {
        int length;
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(new Ascii("Hello"));
            length = temp.Length;
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(length).IsEqualTo(5);
        await Assert.That(bytes[0]).IsEqualTo((byte)'H');
        await Assert.That(bytes[4]).IsEqualTo((byte)'o');
    }

    [Test]
    public async Task Bytes_Utf8Encoded_EncodesCorrectly()
    {
        int length;
        {
            using TempBytes temp = ZA.Bytes(new Utf8("äöü"));
            // Each umlaut is 2 bytes in UTF-8
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(6);
    }

    #endregion

    // ========================================================================
    // COMBINED FORMATTING TESTS
    // Tests for combining multiple wrapper types
    // ========================================================================

    #region Combined Formatting Tests

    [Test]
    public async Task Bytes_MultipleWrappers_SerializeInOrder()
    {
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(
                new U16BE(0x0001),   // 2 bytes
                new U32BE(0x12345678), // 4 bytes
                new Raw([0xAB, 0xCD])  // 2 bytes
            );
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(8);

        // First U16BE
        await Assert.That((int)bytes[0]).IsEqualTo(0x00);
        await Assert.That((int)bytes[1]).IsEqualTo(0x01);

        // Then U32BE
        await Assert.That((int)bytes[2]).IsEqualTo(0x12);
        await Assert.That((int)bytes[5]).IsEqualTo(0x78);

        // Finally Raw
        await Assert.That((int)bytes[6]).IsEqualTo(0xAB);
        await Assert.That((int)bytes[7]).IsEqualTo(0xCD);
    }

    [Test]
    public async Task Bytes_StringWithBinaryPrefix_WorksCorrectly()
    {
        byte[] bytes;
        {
            using TempBytes temp = ZA.Bytes(
                new U16BE(5),        // Length prefix
                new Ascii("Hello")     // String data
            );
            bytes = temp.AsSpan().ToArray();
        }
        await Assert.That(bytes.Length).IsEqualTo(7);
        await Assert.That((int)bytes[0]).IsEqualTo(0x00); // High byte of length
        await Assert.That((int)bytes[1]).IsEqualTo(0x05); // Low byte of length
        await Assert.That(bytes[2]).IsEqualTo((byte)'H');
    }

    #endregion
}
