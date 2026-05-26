// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Direct tests for <see cref="BinaryParser"/>.
/// Covers primitive reads, VarInt, strings, and error handling.
/// </summary>
public sealed class BinaryParserTests
{
    // ========================================================================
    // PRIMITIVE READS - BIG ENDIAN
    // ========================================================================
    [Test]
    public async Task BinaryParser_ReadByte_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xAB];

        // Act
        byte result;
        int position;
        {
            BinaryParser parser = new(data);
            result = parser.ReadByte();
            position = parser.Position;
        }

        // Assert
        await Assert.That((int)result).IsEqualTo(0xAB);
        await Assert.That(position).IsEqualTo(1);
    }

    [Test]
    public async Task BinaryParser_ReadSByte_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFF]; // -1 as sbyte

        // Act
        sbyte result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadSByte();
        }

        // Assert
        await Assert.That((int)result).IsEqualTo(-1);
    }

    [Test]
    public async Task BinaryParser_ReadUInt16BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x12, 0x34];

        // Act
        ushort result;
        int position;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUInt16BE();
            position = parser.Position;
        }

        // Assert
        await Assert.That((int)result).IsEqualTo(0x1234);
        await Assert.That(position).IsEqualTo(2);
    }

    [Test]
    public async Task BinaryParser_ReadInt16BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFF, 0xFE]; // -2 in big-endian

        // Act
        short result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadInt16BE();
        }

        // Assert
        await Assert.That((int)result).IsEqualTo(-2);
    }

    [Test]
    public async Task BinaryParser_ReadUInt32BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78];

        // Act
        uint result;
        int position;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUInt32BE();
            position = parser.Position;
        }

        // Assert
        await Assert.That(result).IsEqualTo(0x12345678U);
        await Assert.That(position).IsEqualTo(4);
    }

    [Test]
    public async Task BinaryParser_ReadInt32BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF]; // -1 in big-endian

        // Act
        int result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadInt32BE();
        }

        // Assert
        await Assert.That(result).IsEqualTo(-1);
    }

    [Test]
    public async Task BinaryParser_ReadUInt64BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0];

        // Act
        ulong result;
        int position;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUInt64BE();
            position = parser.Position;
        }

        // Assert
        await Assert.That(result).IsEqualTo(0x123456789ABCDEF0UL);
        await Assert.That(position).IsEqualTo(8);
    }

    [Test]
    public async Task BinaryParser_ReadInt64BE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]; // -1

        // Act
        long result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadInt64BE();
        }

        // Assert
        await Assert.That(result).IsEqualTo(-1L);
    }

    // ========================================================================
    // PRIMITIVE READS - LITTLE ENDIAN
    // ========================================================================

    [Test]
    public async Task BinaryParser_ReadUInt16LE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x34, 0x12]; // 0x1234 in little-endian

        // Act
        ushort result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUInt16LE();
        }

        // Assert
        await Assert.That((int)result).IsEqualTo(0x1234);
    }

    [Test]
    public async Task BinaryParser_ReadInt16LE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFE, 0xFF]; // -2 in little-endian

        // Act
        short result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadInt16LE();
        }

        // Assert
        await Assert.That((int)result).IsEqualTo(-2);
    }

    [Test]
    public async Task BinaryParser_ReadUInt32LE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x78, 0x56, 0x34, 0x12]; // 0x12345678 in little-endian

        // Act
        uint result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUInt32LE();
        }

        // Assert
        await Assert.That(result).IsEqualTo(0x12345678U);
    }

    [Test]
    public async Task BinaryParser_ReadInt32LE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF]; // -1

        // Act
        int result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadInt32LE();
        }

        // Assert
        await Assert.That(result).IsEqualTo(-1);
    }

    [Test]
    public async Task BinaryParser_ReadUInt64LE_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0xF0, 0xDE, 0xBC, 0x9A, 0x78, 0x56, 0x34, 0x12]; // 0x123456789ABCDEF0 in LE

        // Act
        ulong result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUInt64LE();
        }

        // Assert
        await Assert.That(result).IsEqualTo(0x123456789ABCDEF0UL);
    }

    // ========================================================================
    // FLOATING POINT
    // ========================================================================

    [Test]
    public async Task BinaryParser_ReadF32BE_ReturnsCorrectValue()
    {
        // Arrange - 1.0f in big-endian IEEE 754: 0x3F800000
        byte[] data = [0x3F, 0x80, 0x00, 0x00];

        // Act
        float result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadF32BE();
        }

        // Assert
        await Assert.That(result).IsEqualTo(1.0f);
    }

    [Test]
    public async Task BinaryParser_ReadF32LE_ReturnsCorrectValue()
    {
        // Arrange - 1.0f in little-endian IEEE 754: 0x3F800000
        byte[] data = [0x00, 0x00, 0x80, 0x3F];

        // Act
        float result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadF32LE();
        }

        // Assert
        await Assert.That(result).IsEqualTo(1.0f);
    }

    [Test]
    public async Task BinaryParser_ReadF64BE_ReturnsCorrectValue()
    {
        // Arrange - 1.0 in big-endian IEEE 754: 0x3FF0000000000000
        byte[] data = [0x3F, 0xF0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00];

        // Act
        double result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadF64BE();
        }

        // Assert
        await Assert.That(result).IsEqualTo(1.0);
    }

    [Test]
    public async Task BinaryParser_ReadF64LE_ReturnsCorrectValue()
    {
        // Arrange - 1.0 in little-endian IEEE 754
        byte[] data = [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xF0, 0x3F];

        // Act
        double result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadF64LE();
        }

        // Assert
        await Assert.That(result).IsEqualTo(1.0);
    }

    // ========================================================================
    // VARINT
    // ========================================================================

    [Test]
    public async Task BinaryParser_ReadVarInt_SingleByte_ReturnsCorrectValue()
    {
        // Arrange - 127 (single byte, high bit not set)
        byte[] data = [127];

        // Act
        ulong result;
        int position;
        {
            BinaryParser parser = new(data);
            result = parser.ReadVarInt();
            position = parser.Position;
        }

        // Assert
        await Assert.That(result).IsEqualTo(127UL);
        await Assert.That(position).IsEqualTo(1);
    }

    [Test]
    public async Task BinaryParser_ReadVarInt_TwoBytes_ReturnsCorrectValue()
    {
        // Arrange - 128 (0x80, 0x01)
        byte[] data = [0x80, 0x01];

        // Act
        ulong result;
        int position;
        {
            BinaryParser parser = new(data);
            result = parser.ReadVarInt();
            position = parser.Position;
        }

        // Assert
        await Assert.That(result).IsEqualTo(128UL);
        await Assert.That(position).IsEqualTo(2);
    }

    [Test]
    public async Task BinaryParser_ReadVarInt_MultipleBytes_ReturnsCorrectValue()
    {
        // Arrange - 300 (0xAC, 0x02)
        byte[] data = [0xAC, 0x02];

        // Act
        ulong result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadVarInt();
        }

        // Assert
        await Assert.That(result).IsEqualTo(300UL);
    }

    [Test]
    public async Task BinaryParser_ReadVarIntZigZag_PositiveValue_ReturnsCorrectValue()
    {
        // Arrange - ZigZag encoding: 1 -> 2 -> [0x02]
        byte[] data = [0x02];

        // Act
        long result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadVarIntZigZag();
        }

        // Assert
        await Assert.That(result).IsEqualTo(1L);
    }

    [Test]
    public async Task BinaryParser_ReadVarIntZigZag_NegativeValue_ReturnsCorrectValue()
    {
        // Arrange - ZigZag encoding: -1 -> 1 -> [0x01]
        byte[] data = [0x01];

        // Act
        long result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadVarIntZigZag();
        }

        // Assert
        await Assert.That(result).IsEqualTo(-1L);
    }

    [Test]
    public async Task BinaryParser_ReadVarIntZigZag_Zero_ReturnsCorrectValue()
    {
        // Arrange - ZigZag encoding: 0 -> 0 -> [0x00]
        byte[] data = [0x00];

        // Act
        long result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadVarIntZigZag();
        }

        // Assert
        await Assert.That(result).IsEqualTo(0L);
    }

    // ========================================================================
    // RAW BYTES AND SKIP
    // ========================================================================

    [Test]
    public async Task BinaryParser_ReadBytes_ReturnsCorrectSpan()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05];

        // Act
        byte[] result;
        int position;
        {
            BinaryParser parser = new(data);
            result = parser.ReadBytes(3).ToArray();
            position = parser.Position;
        }

        // Assert
        await Assert.That(result).IsEquivalentTo((byte[])[0x01, 0x02, 0x03]);
        await Assert.That(position).IsEqualTo(3);
    }

    [Test]
    public async Task BinaryParser_Skip_AdvancesPosition()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05];

        // Act
        int position;
        byte byteAfterSkip;
        {
            BinaryParser parser = new(data);
            parser.Skip(3);
            position = parser.Position;
            byteAfterSkip = parser.ReadByte();
        }

        // Assert
        await Assert.That(position).IsEqualTo(3);
        await Assert.That((int)byteAfterSkip).IsEqualTo(0x04);
    }

    // ========================================================================
    // STRING PARSING
    // ========================================================================

    [Test]
    public async Task BinaryParser_ReadUtf8Bytes_ReturnsCorrectSpan()
    {
        // Arrange
        byte[] data = "Hello"u8.ToArray();

        // Act
        byte[] result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUtf8Bytes(5).ToArray();
        }

        // Assert
        await Assert.That(result).IsEquivalentTo("Hello"u8.ToArray());
    }

    [Test]
    public async Task BinaryParser_ReadUtf8Var_ReturnsCorrectSpan()
    {
        // Arrange - VarInt length 5, then "Hello"
        byte[] data = [0x05, .. "Hello"u8.ToArray()];

        // Act
        byte[] result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUtf8Var().ToArray();
        }

        // Assert
        await Assert.That(result).IsEquivalentTo("Hello"u8.ToArray());
    }

    [Test]
    public async Task BinaryParser_ReadUtf8FixedBE16_ReturnsCorrectSpan()
    {
        // Arrange - 16-bit BE length 5, then "Hello"
        byte[] data = [0x00, 0x05, .. "Hello"u8.ToArray()];

        // Act
        byte[] result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUtf8FixedBE16().ToArray();
        }

        // Assert
        await Assert.That(result).IsEquivalentTo("Hello"u8.ToArray());
    }

    [Test]
    public async Task BinaryParser_ReadUtf8FixedBE32_ReturnsCorrectSpan()
    {
        // Arrange - 32-bit BE length 5, then "Hello"
        byte[] data = [0x00, 0x00, 0x00, 0x05, .. "Hello"u8.ToArray()];

        // Act
        byte[] result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUtf8FixedBE32().ToArray();
        }

        // Assert
        await Assert.That(result).IsEquivalentTo("Hello"u8.ToArray());
    }

    [Test]
    public async Task BinaryParser_ReadUtf8Null_ReturnsCorrectSpan()
    {
        // Arrange - Null-terminated string
        byte[] data = [.. "Hello"u8.ToArray(), 0x00, 0xAB]; // Extra byte after null

        // Act
        byte[] result;
        int position;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUtf8Null().ToArray();
            position = parser.Position;
        }

        // Assert
        await Assert.That(result).IsEquivalentTo("Hello"u8.ToArray());
        await Assert.That(position).IsEqualTo(6); // Position after null terminator
    }

    [Test]
    public async Task BinaryParser_ReadUtf8Null_NoTerminator_ReadsToEnd()
    {
        // Arrange - No null terminator
        byte[] data = "Hello"u8.ToArray();

        // Act
        byte[] result;
        bool isAtEnd;
        {
            BinaryParser parser = new(data);
            result = parser.ReadUtf8Null().ToArray();
            isAtEnd = parser.IsAtEnd;
        }

        // Assert
        await Assert.That(result).IsEquivalentTo("Hello"u8.ToArray());
        await Assert.That(isAtEnd).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadAsciiBytes_ReturnsCorrectSpan()
    {
        // Arrange
        byte[] data = "Hello"u8.ToArray();

        // Act
        byte[] result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadAsciiBytes(5).ToArray();
        }

        // Assert
        await Assert.That(result).IsEquivalentTo("Hello"u8.ToArray());
    }

    // ========================================================================
    // PROPERTIES
    // ========================================================================

    [Test]
    public async Task BinaryParser_Position_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04];

        // Act & Assert
        int pos0, pos1, pos2;
        {
            BinaryParser parser = new(data);
            pos0 = parser.Position;
            parser.ReadByte();
            pos1 = parser.Position;
            parser.ReadByte();
            pos2 = parser.Position;
        }
        await Assert.That(pos0).IsEqualTo(0);
        await Assert.That(pos1).IsEqualTo(1);
        await Assert.That(pos2).IsEqualTo(2);
    }

    [Test]
    public async Task BinaryParser_Remaining_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04];

        // Act & Assert
        int rem0, rem1;
        {
            BinaryParser parser = new(data);
            rem0 = parser.Remaining;
            parser.ReadByte();
            rem1 = parser.Remaining;
        }
        await Assert.That(rem0).IsEqualTo(4);
        await Assert.That(rem1).IsEqualTo(3);
    }

    [Test]
    public async Task BinaryParser_Length_ReturnsBufferLength()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04];

        // Act & Assert
        int len0, len1;
        {
            BinaryParser parser = new(data);
            len0 = parser.Length;
            parser.ReadByte();
            len1 = parser.Length;
        }
        await Assert.That(len0).IsEqualTo(4);
        await Assert.That(len1).IsEqualTo(4); // Length doesn't change
    }

    [Test]
    public async Task BinaryParser_IsAtEnd_ReturnsTrueWhenExhausted()
    {
        // Arrange
        byte[] data = [0x01];

        // Act & Assert
        bool atEnd0, atEnd1;
        {
            BinaryParser parser = new(data);
            atEnd0 = parser.IsAtEnd;
            parser.ReadByte();
            atEnd1 = parser.IsAtEnd;
        }
        await Assert.That(atEnd0).IsFalse();
        await Assert.That(atEnd1).IsTrue();
    }

    [Test]
    public async Task BinaryParser_RemainingSpan_ReturnsCorrectSlice()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04];

        // Act
        byte[] remaining;
        {
            BinaryParser parser = new(data);
            parser.ReadByte();
            remaining = parser.RemainingSpan.ToArray();
        }

        // Assert
        await Assert.That(remaining).IsEquivalentTo((byte[])[0x02, 0x03, 0x04]);
    }

    // ========================================================================
    // ERROR HANDLING
    // Note: Using try-catch instead of Assert.Throws because BinaryParser is a ref struct
    // ========================================================================

    [Test]
    public async Task BinaryParser_ReadByte_InsufficientData_ThrowsException()
    {
        byte[] data = [];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadByte(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadUInt16BE_InsufficientData_ThrowsException()
    {
        byte[] data = [0x01]; // Only 1 byte, need 2
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadUInt16BE(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadUInt32BE_InsufficientData_ThrowsException()
    {
        byte[] data = [0x01, 0x02]; // Only 2 bytes, need 4
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadUInt32BE(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadUInt64BE_InsufficientData_ThrowsException()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04]; // Only 4 bytes, need 8
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadUInt64BE(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadBytes_InsufficientData_ThrowsException()
    {
        byte[] data = [0x01, 0x02];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadBytes(10); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_Skip_InsufficientData_ThrowsException()
    {
        byte[] data = [0x01, 0x02];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.Skip(10); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadVarInt_MalformedInput_ThrowsException()
    {
        // All bytes have high bit set, creating an overflow
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadVarInt(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BinaryParser_ReadVarInt_TruncatedData_ThrowsException()
    {
        byte[] data = [0x80]; // High bit set, needs more bytes
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadVarInt(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    // ========================================================================
    // SEQUENTIAL READS
    // ========================================================================

    [Test]
    public async Task BinaryParser_SequentialReads_WorkCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78, 0x00, 0x05, .. "Hello"u8.ToArray()];

        // Act
        uint magic;
        ushort length;
        byte[] str;
        bool isAtEnd;
        {
            BinaryParser parser = new(data);
            magic = parser.ReadUInt32BE();
            length = parser.ReadUInt16BE();
            str = parser.ReadBytes(length).ToArray();
            isAtEnd = parser.IsAtEnd;
        }

        // Assert
        await Assert.That(magic).IsEqualTo(0x12345678U);
        await Assert.That((int)length).IsEqualTo(5);
        await Assert.That(str).IsEquivalentTo("Hello"u8.ToArray());
        await Assert.That(isAtEnd).IsTrue();
    }

    [Test]
    public async Task BinaryParser_MixedEndianReads_WorkCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x78, 0x56]; // BE: 0x1234, LE: 0x5678

        // Act
        ushort be;
        ushort le;
        {
            BinaryParser parser = new(data);
            be = parser.ReadUInt16BE();
            le = parser.ReadUInt16LE();
        }

        // Assert
        await Assert.That((int)be).IsEqualTo(0x1234);
        await Assert.That((int)le).IsEqualTo(0x5678);
    }

    // ========================================================================
    // TRY DECODE UTF8
    // ========================================================================

    [Test]
    public async Task BinaryParser_TryDecodeUtf8_ValidInput_ReturnsTrue()
    {
        // Arrange
        byte[] utf8Bytes = "Hello"u8.ToArray();

        // Act
        bool result;
        int charsWritten;
        string decoded;
        {
            Span<char> destination = stackalloc char[10];
            result = BinaryParser.TryDecodeUtf8(utf8Bytes, destination, out charsWritten);
            decoded = destination[..charsWritten].ToString();
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(charsWritten).IsEqualTo(5);
        await Assert.That(decoded).IsEqualTo("Hello");
    }

    [Test]
    public async Task BinaryParser_TryDecodeUtf8_DestinationTooSmall_ReturnsFalse()
    {
        // Arrange
        byte[] utf8Bytes = "Hello World"u8.ToArray();

        // Act
        bool result;
        {
            Span<char> destination = stackalloc char[5]; // Too small
            result = BinaryParser.TryDecodeUtf8(utf8Bytes, destination, out _);
        }

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task BinaryParser_TryDecodeUtf8_Unicode_ReturnsCorrectChars()
    {
        // Arrange
        byte[] utf8Bytes = "Héllo €"u8.ToArray();

        // Act
        bool result;
        string decoded;
        {
            Span<char> destination = stackalloc char[20];
            result = BinaryParser.TryDecodeUtf8(utf8Bytes, destination, out int charsWritten);
            decoded = destination[..charsWritten].ToString();
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(decoded).IsEqualTo("Héllo €");
    }

    // ========================================================================
    // EMPTY BUFFER
    // ========================================================================

    [Test]
    public async Task BinaryParser_EmptyBuffer_PropertiesCorrect()
    {
        // Arrange
        byte[] data = [];

        // Assert
        int position, remaining, length;
        bool isAtEnd;
        byte[] remainingSpan;
        {
            BinaryParser parser = new(data);
            position = parser.Position;
            remaining = parser.Remaining;
            length = parser.Length;
            isAtEnd = parser.IsAtEnd;
            remainingSpan = parser.RemainingSpan.ToArray();
        }
        await Assert.That(position).IsEqualTo(0);
        await Assert.That(remaining).IsEqualTo(0);
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isAtEnd).IsTrue();
        await Assert.That(remainingSpan).IsEmpty();
    }

    // ========================================================================
    // BOUNDARY VALIDATION — ReadBytes / Skip / ReadUtf8Var / ReadArray / ReadArrayBE32
    // ========================================================================

    /// <summary>Verifies ReadBytes rejects a negative length.</summary>
    [Test]
    public async Task BinaryParser_ReadBytes_NegativeLength_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0x01, 0x02, 0x03];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadBytes(-1); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies ReadBytes does not advance position on negative length.</summary>
    [Test]
    public async Task BinaryParser_ReadBytes_NegativeLength_DoesNotMutatePosition()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03];

        // Act
        int position;
        {
            BinaryParser parser = new(data);
            try { parser.ReadBytes(-5); } catch (ArgumentOutOfRangeException) { /* expected */ }
            position = parser.Position;
        }

        // Assert — position must be unchanged
        await Assert.That(position).IsEqualTo(0);
    }

    /// <summary>Verifies Skip rejects a negative count.</summary>
    [Test]
    public async Task BinaryParser_Skip_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0x01, 0x02, 0x03];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.Skip(-1); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies Skip does not advance position on negative count.</summary>
    [Test]
    public async Task BinaryParser_Skip_NegativeCount_DoesNotMutatePosition()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03];

        // Act
        int position;
        {
            BinaryParser parser = new(data);
            try { parser.Skip(-3); } catch (ArgumentOutOfRangeException) { /* expected */ }
            position = parser.Position;
        }

        // Assert — position must be unchanged
        await Assert.That(position).IsEqualTo(0);
    }

    /// <summary>
    /// Verifies ReadUtf8Var throws when the VarInt length prefix exceeds int.MaxValue.
    /// The bytes [0x80,0x80,0x80,0x80,0x08] encode VarInt value 2^31 (= int.MaxValue + 1).
    /// </summary>
    [Test]
    public async Task BinaryParser_ReadUtf8Var_OversizedVarIntLength_ThrowsInvalidOperationException()
    {
        // VarInt 2^31 (0x80000000) encoded as 5-byte sequence
        byte[] data = [0x80, 0x80, 0x80, 0x80, 0x08];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadUtf8Var(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies ReadArray rejects a negative count.</summary>
    [Test]
    public async Task BinaryParser_ReadArray_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        // U16BE is a fixed-size (2 byte) IBinaryParsable type from the library
        byte[] data = [0x00, 0x01, 0x00, 0x02];
        BinaryParser parser = new(data);
        U16BE[] destination = new U16BE[10];
        bool threw = false;
        try { parser.ReadArray<U16BE>(-1, destination); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies ReadArray does not advance position on negative count.</summary>
    [Test]
    public async Task BinaryParser_ReadArray_NegativeCount_DoesNotMutatePosition()
    {
        // Arrange
        byte[] data = [0x00, 0x01, 0x00, 0x02];
        U16BE[] destination = new U16BE[10];

        // Act
        int position;
        {
            BinaryParser parser = new(data);
            try { parser.ReadArray<U16BE>(-1, destination); } catch (ArgumentOutOfRangeException) { /* expected */ }
            position = parser.Position;
        }

        // Assert — position must be unchanged
        await Assert.That(position).IsEqualTo(0);
    }

    /// <summary>
    /// Verifies ReadArrayBE32 throws when the BE32 count prefix exceeds int.MaxValue.
    /// The bytes [0x80,0x00,0x00,0x01] encode uint value 2147483649 (int.MaxValue + 2).
    /// </summary>
    [Test]
    public async Task BinaryParser_ReadArrayBE32_OversizedCount_ThrowsInvalidOperationException()
    {
        // 4 bytes encoding uint 0x80000001
        byte[] data = [0x80, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00];
        BinaryParser parser = new(data);
        U16BE[] destination = new U16BE[10];
        bool threw = false;
        try { parser.ReadArrayBE32<U16BE>(destination); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>
    /// Verifies ReadUtf8FixedBE32 throws when the BE32 length prefix exceeds int.MaxValue.
    /// The bytes [0x80,0x00,0x00,0x01] encode uint value 2147483649 (int.MaxValue + 2).
    /// </summary>
    [Test]
    public async Task BinaryParser_ReadUtf8FixedBE32_OversizedLength_ThrowsInvalidOperationException()
    {
        // 4 bytes encoding uint 0x80000001
        byte[] data = [0x80, 0x00, 0x00, 0x01];
        BinaryParser parser = new(data);
        bool threw = false;
        try { parser.ReadUtf8FixedBE32(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }
}