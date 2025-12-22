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

namespace ZeroAlloc.Tests;

/// <summary>
/// Direct tests for <see cref="BinaryParser"/>.
/// Covers primitive reads, VarInt, strings, and error handling.
/// </summary>
public class BinaryParserTests
{
    // ========================================================================
    // PRIMITIVE READS - BIG ENDIAN
    // ========================================================================

    [Fact]
    public void BinaryParser_ReadByte_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xAB];
        BinaryParser parser = new(data);

        // Act
        byte result = parser.ReadByte();

        // Assert
        Assert.Equal(0xAB, result);
        Assert.Equal(1, parser.Position);
    }

    [Fact]
    public void BinaryParser_ReadSByte_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFF]; // -1 as sbyte
        BinaryParser parser = new(data);

        // Act
        sbyte result = parser.ReadSByte();

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void BinaryParser_ReadUInt16BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x12, 0x34];
        BinaryParser parser = new(data);

        // Act
        ushort result = parser.ReadUInt16BE();

        // Assert
        Assert.Equal(0x1234, result);
        Assert.Equal(2, parser.Position);
    }

    [Fact]
    public void BinaryParser_ReadInt16BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFF, 0xFE]; // -2 in big-endian
        BinaryParser parser = new(data);

        // Act
        short result = parser.ReadInt16BE();

        // Assert
        Assert.Equal(-2, result);
    }

    [Fact]
    public void BinaryParser_ReadUInt32BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78];
        BinaryParser parser = new(data);

        // Act
        uint result = parser.ReadUInt32BE();

        // Assert
        Assert.Equal(0x12345678U, result);
        Assert.Equal(4, parser.Position);
    }

    [Fact]
    public void BinaryParser_ReadInt32BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF]; // -1 in big-endian
        BinaryParser parser = new(data);

        // Act
        int result = parser.ReadInt32BE();

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void BinaryParser_ReadUInt64BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0];
        BinaryParser parser = new(data);

        // Act
        ulong result = parser.ReadUInt64BE();

        // Assert
        Assert.Equal(0x123456789ABCDEF0UL, result);
        Assert.Equal(8, parser.Position);
    }

    [Fact]
    public void BinaryParser_ReadInt64BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]; // -1
        BinaryParser parser = new(data);

        // Act
        long result = parser.ReadInt64BE();

        // Assert
        Assert.Equal(-1L, result);
    }

    // ========================================================================
    // PRIMITIVE READS - LITTLE ENDIAN
    // ========================================================================

    [Fact]
    public void BinaryParser_ReadUInt16LE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x34, 0x12]; // 0x1234 in little-endian
        BinaryParser parser = new(data);

        // Act
        ushort result = parser.ReadUInt16LE();

        // Assert
        Assert.Equal(0x1234, result);
    }

    [Fact]
    public void BinaryParser_ReadInt16LE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFE, 0xFF]; // -2 in little-endian
        BinaryParser parser = new(data);

        // Act
        short result = parser.ReadInt16LE();

        // Assert
        Assert.Equal(-2, result);
    }

    [Fact]
    public void BinaryParser_ReadUInt32LE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x78, 0x56, 0x34, 0x12]; // 0x12345678 in little-endian
        BinaryParser parser = new(data);

        // Act
        uint result = parser.ReadUInt32LE();

        // Assert
        Assert.Equal(0x12345678U, result);
    }

    [Fact]
    public void BinaryParser_ReadInt32LE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF]; // -1
        BinaryParser parser = new(data);

        // Act
        int result = parser.ReadInt32LE();

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void BinaryParser_ReadUInt64LE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12]; // 0x123456789ABCDEF0 in LE
        BinaryParser parser = new(data);

        // Act
        ulong result = parser.ReadUInt64LE();

        // Assert
        Assert.Equal(0x123456789ABCDEF0UL, result);
    }

    // ========================================================================
    // FLOATING POINT
    // ========================================================================

    [Fact]
    public void BinaryParser_ReadF32BE_ReturnsCorrectValue()
    {
        // Arrange - 1.0f in big-endian IEEE 754: 0x3F800000
        byte[] data = [0x3F, 0x80, 0x00, 0x00];
        BinaryParser parser = new(data);

        // Act
        float result = parser.ReadF32BE();

        // Assert
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void BinaryParser_ReadF32LE_ReturnsCorrectValue()
    {
        // Arrange - 1.0f in little-endian IEEE 754: 0x3F800000
        byte[] data = [0x00, 0x00, 0x80, 0x3F];
        BinaryParser parser = new(data);

        // Act
        float result = parser.ReadF32LE();

        // Assert
        Assert.Equal(1.0f, result);
    }

    [Fact]
    public void BinaryParser_ReadF64BE_ReturnsCorrectValue()
    {
        // Arrange - 1.0 in big-endian IEEE 754: 0x3FF0000000000000
        byte[] data = [0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];
        BinaryParser parser = new(data);

        // Act
        double result = parser.ReadF64BE();

        // Assert
        Assert.Equal(1.0, result);
    }

    [Fact]
    public void BinaryParser_ReadF64LE_ReturnsCorrectValue()
    {
        // Arrange - 1.0 in little-endian IEEE 754
        byte[] data = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F];
        BinaryParser parser = new(data);

        // Act
        double result = parser.ReadF64LE();

        // Assert
        Assert.Equal(1.0, result);
    }

    // ========================================================================
    // VARINT
    // ========================================================================

    [Fact]
    public void BinaryParser_ReadVarInt_SingleByte_ReturnsCorrectValue()
    {
        // Arrange - 127 (single byte, high bit not set)
        byte[] data = [127];
        BinaryParser parser = new(data);

        // Act
        ulong result = parser.ReadVarInt();

        // Assert
        Assert.Equal(127UL, result);
        Assert.Equal(1, parser.Position);
    }

    [Fact]
    public void BinaryParser_ReadVarInt_TwoBytes_ReturnsCorrectValue()
    {
        // Arrange - 128 (0x80, 0x01)
        byte[] data = [0x80, 0x01];
        BinaryParser parser = new(data);

        // Act
        ulong result = parser.ReadVarInt();

        // Assert
        Assert.Equal(128UL, result);
        Assert.Equal(2, parser.Position);
    }

    [Fact]
    public void BinaryParser_ReadVarInt_MultipleBytes_ReturnsCorrectValue()
    {
        // Arrange - 300 (0xAC, 0x02)
        byte[] data = [0xAC, 0x02];
        BinaryParser parser = new(data);

        // Act
        ulong result = parser.ReadVarInt();

        // Assert
        Assert.Equal(300UL, result);
    }

    [Fact]
    public void BinaryParser_ReadVarIntZigZag_PositiveValue_ReturnsCorrectValue()
    {
        // Arrange - ZigZag encoding: 1 -> 2 -> [0x02]
        byte[] data = [0x02];
        BinaryParser parser = new(data);

        // Act
        long result = parser.ReadVarIntZigZag();

        // Assert
        Assert.Equal(1L, result);
    }

    [Fact]
    public void BinaryParser_ReadVarIntZigZag_NegativeValue_ReturnsCorrectValue()
    {
        // Arrange - ZigZag encoding: -1 -> 1 -> [0x01]
        byte[] data = [0x01];
        BinaryParser parser = new(data);

        // Act
        long result = parser.ReadVarIntZigZag();

        // Assert
        Assert.Equal(-1L, result);
    }

    [Fact]
    public void BinaryParser_ReadVarIntZigZag_Zero_ReturnsCorrectValue()
    {
        // Arrange - ZigZag encoding: 0 -> 0 -> [0x00]
        byte[] data = [0x00];
        BinaryParser parser = new(data);

        // Act
        long result = parser.ReadVarIntZigZag();

        // Assert
        Assert.Equal(0L, result);
    }

    // ========================================================================
    // RAW BYTES AND SKIP
    // ========================================================================

    [Fact]
    public void BinaryParser_ReadBytes_ReturnsCorrectSpan()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05];
        BinaryParser parser = new(data);

        // Act
        ReadOnlySpan<byte> result = parser.ReadBytes(3);

        // Assert
        Assert.Equal([0x01, 0x02, 0x03], result.ToArray());
        Assert.Equal(3, parser.Position);
    }

    [Fact]
    public void BinaryParser_Skip_AdvancesPosition()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05];
        BinaryParser parser = new(data);

        // Act
        parser.Skip(3);

        // Assert
        Assert.Equal(3, parser.Position);
        Assert.Equal(0x04, parser.ReadByte());
    }

    // ========================================================================
    // STRING PARSING
    // ========================================================================

    [Fact]
    public void BinaryParser_ReadUtf8Bytes_ReturnsCorrectSpan()
    {
        // Arrange
        byte[] data = "Hello"u8.ToArray();
        BinaryParser parser = new(data);

        // Act
        ReadOnlySpan<byte> result = parser.ReadUtf8Bytes(5);

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result.ToArray());
    }

    [Fact]
    public void BinaryParser_ReadUtf8Var_ReturnsCorrectSpan()
    {
        // Arrange - VarInt length 5, then "Hello"
        byte[] data = [0x05, .. "Hello"u8.ToArray()];
        BinaryParser parser = new(data);

        // Act
        ReadOnlySpan<byte> result = parser.ReadUtf8Var();

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result.ToArray());
    }

    [Fact]
    public void BinaryParser_ReadUtf8FixedBE16_ReturnsCorrectSpan()
    {
        // Arrange - 16-bit BE length 5, then "Hello"
        byte[] data = [0x00, 0x05, .. "Hello"u8.ToArray()];
        BinaryParser parser = new(data);

        // Act
        ReadOnlySpan<byte> result = parser.ReadUtf8FixedBE16();

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result.ToArray());
    }

    [Fact]
    public void BinaryParser_ReadUtf8FixedBE32_ReturnsCorrectSpan()
    {
        // Arrange - 32-bit BE length 5, then "Hello"
        byte[] data = [0x00, 0x00, 0x00, 0x05, .. "Hello"u8.ToArray()];
        BinaryParser parser = new(data);

        // Act
        ReadOnlySpan<byte> result = parser.ReadUtf8FixedBE32();

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result.ToArray());
    }

    [Fact]
    public void BinaryParser_ReadUtf8Null_ReturnsCorrectSpan()
    {
        // Arrange - Null-terminated string
        byte[] data = [.. "Hello"u8.ToArray(), 0x00, 0xAB]; // Extra byte after null
        BinaryParser parser = new(data);

        // Act
        ReadOnlySpan<byte> result = parser.ReadUtf8Null();

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result.ToArray());
        Assert.Equal(6, parser.Position); // Position after null terminator
    }

    [Fact]
    public void BinaryParser_ReadUtf8Null_NoTerminator_ReadsToEnd()
    {
        // Arrange - No null terminator
        byte[] data = "Hello"u8.ToArray();
        BinaryParser parser = new(data);

        // Act
        ReadOnlySpan<byte> result = parser.ReadUtf8Null();

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result.ToArray());
        Assert.True(parser.IsAtEnd);
    }

    [Fact]
    public void BinaryParser_ReadAsciiBytes_ReturnsCorrectSpan()
    {
        // Arrange
        byte[] data = "Hello"u8.ToArray();
        BinaryParser parser = new(data);

        // Act
        ReadOnlySpan<byte> result = parser.ReadAsciiBytes(5);

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result.ToArray());
    }

    // ========================================================================
    // PROPERTIES
    // ========================================================================

    [Fact]
    public void BinaryParser_Position_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        BinaryParser parser = new(data);

        // Act & Assert
        Assert.Equal(0, parser.Position);
        parser.ReadByte();
        Assert.Equal(1, parser.Position);
        parser.ReadByte();
        Assert.Equal(2, parser.Position);
    }

    [Fact]
    public void BinaryParser_Remaining_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        BinaryParser parser = new(data);

        // Act & Assert
        Assert.Equal(4, parser.Remaining);
        parser.ReadByte();
        Assert.Equal(3, parser.Remaining);
    }

    [Fact]
    public void BinaryParser_Length_ReturnsBufferLength()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        BinaryParser parser = new(data);

        // Act & Assert
        Assert.Equal(4, parser.Length);
        parser.ReadByte();
        Assert.Equal(4, parser.Length); // Length doesn't change
    }

    [Fact]
    public void BinaryParser_IsAtEnd_ReturnsTrueWhenExhausted()
    {
        // Arrange
        byte[] data = [0x01];
        BinaryParser parser = new(data);

        // Act & Assert
        Assert.False(parser.IsAtEnd);
        parser.ReadByte();
        Assert.True(parser.IsAtEnd);
    }

    [Fact]
    public void BinaryParser_RemainingSpan_ReturnsCorrectSlice()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        BinaryParser parser = new(data);
        parser.ReadByte();

        // Act
        ReadOnlySpan<byte> remaining = parser.RemainingSpan;

        // Assert
        Assert.Equal([0x02, 0x03, 0x04], remaining.ToArray());
    }

    // ========================================================================
    // ERROR HANDLING
    // Note: Using try-catch instead of Assert.Throws because BinaryParser is a ref struct
    // ========================================================================

    [Fact]
    public void BinaryParser_ReadByte_InsufficientData_ThrowsException()
    {
        // Arrange
        byte[] data = [];
        BinaryParser parser = new(data);

        // Act & Assert (try-catch because ref struct can't be in lambda)
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
    public void BinaryParser_ReadUInt16BE_InsufficientData_ThrowsException()
    {
        // Arrange
        byte[] data = [0x01]; // Only 1 byte, need 2
        BinaryParser parser = new(data);

        // Act & Assert (try-catch because ref struct can't be in lambda)
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
    public void BinaryParser_ReadUInt32BE_InsufficientData_ThrowsException()
    {
        // Arrange
        byte[] data = [0x01, 0x02]; // Only 2 bytes, need 4
        BinaryParser parser = new(data);

        // Act & Assert (try-catch because ref struct can't be in lambda)
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
    public void BinaryParser_ReadUInt64BE_InsufficientData_ThrowsException()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04]; // Only 4 bytes, need 8
        BinaryParser parser = new(data);

        // Act & Assert (try-catch because ref struct can't be in lambda)
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
    public void BinaryParser_ReadBytes_InsufficientData_ThrowsException()
    {
        // Arrange
        byte[] data = [0x01, 0x02];
        BinaryParser parser = new(data);

        // Act & Assert (try-catch because ref struct can't be in lambda)
        try
        {
            parser.ReadBytes(10);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [Fact]
    public void BinaryParser_Skip_InsufficientData_ThrowsException()
    {
        // Arrange
        byte[] data = [0x01, 0x02];
        BinaryParser parser = new(data);

        // Act & Assert (try-catch because ref struct can't be in lambda)
        try
        {
            parser.Skip(10);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    [Fact]
    public void BinaryParser_ReadVarInt_MalformedInput_ThrowsException()
    {
        // Arrange - All bytes have high bit set, creating an overflow
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01];
        BinaryParser parser = new(data);

        // Act & Assert (try-catch because ref struct can't be in lambda)
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
    public void BinaryParser_ReadVarInt_TruncatedData_ThrowsException()
    {
        // Arrange - VarInt that expects more bytes
        byte[] data = [0x80]; // High bit set, needs more bytes
        BinaryParser parser = new(data);

        // Act & Assert (try-catch because ref struct can't be in lambda)
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

    // ========================================================================
    // SEQUENTIAL READS
    // ========================================================================

    [Fact]
    public void BinaryParser_SequentialReads_WorkCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78, 0x00, 0x05, .. "Hello"u8.ToArray()];
        BinaryParser parser = new(data);

        // Act
        uint magic = parser.ReadUInt32BE();
        ushort length = parser.ReadUInt16BE();
        ReadOnlySpan<byte> str = parser.ReadBytes(length);

        // Assert
        Assert.Equal(0x12345678U, magic);
        Assert.Equal(5, length);
        Assert.Equal("Hello"u8.ToArray(), str.ToArray());
        Assert.True(parser.IsAtEnd);
    }

    [Fact]
    public void BinaryParser_MixedEndianReads_WorkCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x78, 0x56]; // BE: 0x1234, LE: 0x5678
        BinaryParser parser = new(data);

        // Act
        ushort be = parser.ReadUInt16BE();
        ushort le = parser.ReadUInt16LE();

        // Assert
        Assert.Equal(0x1234, be);
        Assert.Equal(0x5678, le);
    }

    // ========================================================================
    // TRY DECODE UTF8
    // ========================================================================

    [Fact]
    public void BinaryParser_TryDecodeUtf8_ValidInput_ReturnsTrue()
    {
        // Arrange
        byte[] utf8Bytes = "Hello"u8.ToArray();
        Span<char> destination = stackalloc char[10];

        // Act
        bool result = BinaryParser.TryDecodeUtf8(utf8Bytes, destination, out int charsWritten);

        // Assert
        Assert.True(result);
        Assert.Equal(5, charsWritten);
        Assert.Equal("Hello", destination[..charsWritten].ToString());
    }

    [Fact]
    public void BinaryParser_TryDecodeUtf8_DestinationTooSmall_ReturnsFalse()
    {
        // Arrange
        byte[] utf8Bytes = "Hello World"u8.ToArray();
        Span<char> destination = stackalloc char[5]; // Too small

        // Act
        bool result = BinaryParser.TryDecodeUtf8(utf8Bytes, destination, out int charsWritten);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void BinaryParser_TryDecodeUtf8_Unicode_ReturnsCorrectChars()
    {
        // Arrange
        byte[] utf8Bytes = "Héllo €"u8.ToArray();
        Span<char> destination = stackalloc char[20];

        // Act
        bool result = BinaryParser.TryDecodeUtf8(utf8Bytes, destination, out int charsWritten);

        // Assert
        Assert.True(result);
        Assert.Equal("Héllo €", destination[..charsWritten].ToString());
    }

    // ========================================================================
    // EMPTY BUFFER
    // ========================================================================

    [Fact]
    public void BinaryParser_EmptyBuffer_PropertiesCorrect()
    {
        // Arrange
        byte[] data = [];
        BinaryParser parser = new(data);

        // Assert
        Assert.Equal(0, parser.Position);
        Assert.Equal(0, parser.Remaining);
        Assert.Equal(0, parser.Length);
        Assert.True(parser.IsAtEnd);
        Assert.Empty(parser.RemainingSpan.ToArray());
    }
}
