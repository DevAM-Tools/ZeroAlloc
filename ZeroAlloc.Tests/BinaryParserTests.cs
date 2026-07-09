// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>Stub type for BinaryParseValidator name-pattern coverage.</summary>
public struct UIntTestStub;

/// <summary>Reference type stub for unsupported-type validation tests.</summary>
public sealed class UnsupportedReferenceType;

/// <summary>Value type stub without IBinaryParsable for validation tests.</summary>
public struct UnsupportedValueType;

/// <summary>
/// Direct tests for <see cref="BinaryParser"/>.
/// Covers primitive reads, VarInt, strings, and error handling.
/// </summary>
public sealed class BinaryParserTests
{

    // ========================================================================
    // PRIMITIVE READS - BIG ENDIAN
    // ========================================================================


    /// <summary>Verifies BinaryParser ReadByte ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadSByte ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadUInt16BE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadInt16BE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadUInt32BE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadInt32BE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadUInt64BE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadInt64BE ReturnsCorrectValue.</summary>
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


    /// <summary>Verifies BinaryParser ReadUInt16LE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadInt16LE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadUInt32LE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadInt32LE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadUInt64LE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadInt64LE ReturnsCorrectValue.</summary>
    [Test]
    public async Task BinaryParser_ReadInt64LE_ReturnsCorrectValue()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

        long result;
        {
            BinaryParser parser = new(data);
            result = parser.ReadInt64LE();
        }

        await Assert.That(result).IsEqualTo(-1L);
    }

    // ========================================================================
    // FLOATING POINT
    // ========================================================================


    /// <summary>Verifies BinaryParser ReadF32BE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadF32LE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadF64BE ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadF64LE ReturnsCorrectValue.</summary>
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


    /// <summary>Verifies BinaryParser ReadVarInt SingleByte ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadVarInt TwoBytes ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadVarInt MultipleBytes ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadVarIntZigZag PositiveValue ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadVarIntZigZag NegativeValue ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser ReadVarIntZigZag Zero ReturnsCorrectValue.</summary>
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


    /// <summary>Verifies BinaryParser ReadBytes ReturnsCorrectSpan.</summary>
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

    /// <summary>Verifies BinaryParser Skip AdvancesPosition.</summary>
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


    /// <summary>Verifies BinaryParser ReadUtf8Bytes ReturnsCorrectSpan.</summary>
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

    /// <summary>Verifies BinaryParser ReadUtf8Var ReturnsCorrectSpan.</summary>
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

    /// <summary>Verifies BinaryParser ReadUtf8FixedBE16 ReturnsCorrectSpan.</summary>
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

    /// <summary>Verifies BinaryParser ReadUtf8FixedBE32 ReturnsCorrectSpan.</summary>
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

    /// <summary>Verifies BinaryParser ReadUtf8Null ReturnsCorrectSpan.</summary>
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

    /// <summary>Verifies BinaryParser ReadUtf8Null NoTerminator ReadsToEnd.</summary>
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

    /// <summary>Verifies BinaryParser ReadAsciiBytes ReturnsCorrectSpan.</summary>
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


    /// <summary>Verifies BinaryParser Position ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser Remaining ReturnsCorrectValue.</summary>
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

    /// <summary>Verifies BinaryParser Length ReturnsBufferLength.</summary>
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

    /// <summary>Verifies BinaryParser IsAtEnd ReturnsTrueWhenExhausted.</summary>
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

    /// <summary>Verifies BinaryParser RemainingSpan ReturnsCorrectSlice.</summary>
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


    /// <summary>Verifies BinaryParser ReadByte InsufficientData ThrowsException.</summary>
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

    /// <summary>Verifies BinaryParser ReadUInt16BE InsufficientData ThrowsException.</summary>
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

    /// <summary>Verifies BinaryParser ReadUInt32BE InsufficientData ThrowsException.</summary>
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

    /// <summary>Verifies BinaryParser ReadUInt64BE InsufficientData ThrowsException.</summary>
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

    /// <summary>Verifies BinaryParser ReadBytes InsufficientData ThrowsException.</summary>
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

    /// <summary>Verifies BinaryParser Skip InsufficientData ThrowsException.</summary>
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

    /// <summary>Verifies BinaryParser ReadVarInt MalformedInput ThrowsException.</summary>
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

    /// <summary>Verifies BinaryParser ReadVarInt TruncatedData ThrowsException.</summary>
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


    /// <summary>Verifies BinaryParser SequentialReads WorkCorrectly.</summary>
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

    /// <summary>Verifies BinaryParser MixedEndianReads WorkCorrectly.</summary>
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


    /// <summary>Verifies BinaryParser TryDecodeUtf8 ValidInput ReturnsTrue.</summary>
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

    /// <summary>Verifies BinaryParser TryDecodeUtf8 DestinationTooSmall ReturnsFalse.</summary>
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

    /// <summary>Verifies BinaryParser TryDecodeUtf8 Unicode ReturnsCorrectChars.</summary>
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


    /// <summary>Verifies BinaryParser EmptyBuffer PropertiesCorrect.</summary>
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

    // ========================================================================
    // EXIT-POINT COVERAGE — TryReadUtf8Var / generic reads / arrays / validation
    // ========================================================================


    /// <summary>Verifies BinaryParser TryReadUtf8Var NegativeMaxLength ThrowsArgumentOutOfRangeException.</summary>
    [Test]
    public async Task BinaryParser_TryReadUtf8Var_NegativeMaxLength_ThrowsArgumentOutOfRangeException()
    {
        BinaryParser parser = new([0x01, 0x41]);
        bool threw = false;
        try { parser.TryReadUtf8Var(-1, out ReadOnlySpan<byte> _); }
        catch (ArgumentOutOfRangeException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BinaryParser TryReadUtf8Var IncompleteVarInt ReturnsFalse.</summary>
    [Test]
    public async Task BinaryParser_TryReadUtf8Var_IncompleteVarInt_ReturnsFalse()
    {
        bool ok;
        int spanLength;
        int position;
        {
            BinaryParser parser = new([0x80]);
            ok = parser.TryReadUtf8Var(10, out ReadOnlySpan<byte> span);
            spanLength = span.Length;
            position = parser.Position;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(spanLength).IsEqualTo(0);
        await Assert.That(position).IsEqualTo(0);
    }

    /// <summary>Verifies BinaryParser TryReadUtf8Var LengthExceedsMaxLength ReturnsFalse.</summary>
    [Test]
    public async Task BinaryParser_TryReadUtf8Var_LengthExceedsMaxLength_ReturnsFalse()
    {
        bool ok;
        int spanLength;
        int position;
        {
            BinaryParser parser = new([0x05, 0x41, 0x42, 0x43, 0x44, 0x45]);
            ok = parser.TryReadUtf8Var(4, out ReadOnlySpan<byte> span);
            spanLength = span.Length;
            position = parser.Position;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(spanLength).IsEqualTo(0);
        await Assert.That(position).IsEqualTo(0);
    }

    /// <summary>Verifies BinaryParser TryReadUtf8Var PayloadTruncated ReturnsFalse.</summary>
    [Test]
    public async Task BinaryParser_TryReadUtf8Var_PayloadTruncated_ReturnsFalse()
    {
        bool ok;
        int spanLength;
        int position;
        {
            BinaryParser parser = new([0x03, 0x41, 0x42]);
            ok = parser.TryReadUtf8Var(10, out ReadOnlySpan<byte> span);
            spanLength = span.Length;
            position = parser.Position;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(spanLength).IsEqualTo(0);
        await Assert.That(position).IsEqualTo(0);
    }

    /// <summary>Verifies BinaryParser TryReadUtf8Var ValidPayload ReturnsTrue.</summary>
    [Test]
    public async Task BinaryParser_TryReadUtf8Var_ValidPayload_ReturnsTrue()
    {
        byte[] payload;
        bool ok;
        int position;
        {
            byte[] data = [0x03, 0x41, 0x42, 0x43];
            BinaryParser parser = new(data);
            ok = parser.TryReadUtf8Var(10, out ReadOnlySpan<byte> span);
            payload = span.ToArray();
            position = parser.Position;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(payload).IsEquivalentTo((byte[])[0x41, 0x42, 0x43]);
        await Assert.That(position).IsEqualTo(4);
    }

    /// <summary>Verifies BinaryParser TryReadUtf8Var MalformedVarInt ReturnsFalse.</summary>
    [Test]
    public async Task BinaryParser_TryReadUtf8Var_MalformedVarInt_ReturnsFalse()
    {
        bool ok;
        int spanLength;
        int position;
        {
            byte[] data = new byte[12];
            Array.Fill(data, (byte)0xFF);
            BinaryParser parser = new(data);
            ok = parser.TryReadUtf8Var(100, out ReadOnlySpan<byte> span);
            spanLength = span.Length;
            position = parser.Position;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(spanLength).IsEqualTo(0);
        await Assert.That(position).IsEqualTo(0);
    }

    /// <summary>Verifies BinaryParser Read U16BE ReturnsParsedValue.</summary>
    [Test]
    public async Task BinaryParser_Read_U16BE_ReturnsParsedValue()
    {
        byte[] data = [0x12, 0x34];
        U16BE result;
        int position;
        {
            BinaryParser parser = new(data);
            result = parser.Read<U16BE>();
            position = parser.Position;
        }

        await Assert.That(result.Value).IsEqualTo((ushort)0x1234);
        await Assert.That(position).IsEqualTo(2);
    }

    /// <summary>Verifies BinaryParser Read U16BE InsufficientData ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task BinaryParser_Read_U16BE_InsufficientData_ThrowsInvalidOperationException()
    {
        BinaryParser parser = new([0x12]);
        bool threw = false;
        try { parser.Read<U16BE>(); }
        catch (InvalidOperationException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BinaryParser TryRead U16BE SufficientData ReturnsTrue.</summary>
    [Test]
    public async Task BinaryParser_TryRead_U16BE_SufficientData_ReturnsTrue()
    {
        BinaryParser parser = new([0x00, 0x7B]);
        bool ok = parser.TryRead<U16BE>(out U16BE value);
        int position = parser.Position;

        await Assert.That(ok).IsTrue();
        await Assert.That(value.Value).IsEqualTo((ushort)123);
        await Assert.That(position).IsEqualTo(2);
    }

    /// <summary>Verifies BinaryParser TryRead U16BE InsufficientData ReturnsFalse.</summary>
    [Test]
    public async Task BinaryParser_TryRead_U16BE_InsufficientData_ReturnsFalse()
    {
        BinaryParser parser = new([0x12]);
        bool ok = parser.TryRead<U16BE>(out U16BE value);
        int position = parser.Position;

        await Assert.That(ok).IsFalse();
        await Assert.That(value.Value).IsEqualTo((ushort)0);
        await Assert.That(position).IsEqualTo(0);
    }

    /// <summary>Verifies BinaryParser ReadArray VarIntElementType ThrowsArgumentException.</summary>
    [Test]
    public async Task BinaryParser_ReadArray_VarIntElementType_ThrowsArgumentException()
    {
        bool threw = false;
        {
            BinaryParser parser = new([0x01]);
            VarInt[] destination = new VarInt[1];
            try { parser.ReadArray<VarInt>(1, destination); }
            catch (ArgumentException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BinaryParser ReadArray U16BE ReturnsElementsRead.</summary>
    [Test]
    public async Task BinaryParser_ReadArray_U16BE_ReturnsElementsRead()
    {
        byte[] data = [0x12, 0x34, 0x56, 0x78];
        U16BE[] destination = new U16BE[2];
        int elementsRead;
        {
            BinaryParser parser = new(data);
            elementsRead = parser.ReadArray(2, destination.AsSpan());
        }

        await Assert.That(elementsRead).IsEqualTo(2);
        await Assert.That(destination[0].Value).IsEqualTo((ushort)0x1234);
        await Assert.That(destination[1].Value).IsEqualTo((ushort)0x5678);
    }

    /// <summary>Verifies BinaryParser ReadArrayVarInt U16BE ReturnsElementsRead.</summary>
    [Test]
    public async Task BinaryParser_ReadArrayVarInt_U16BE_ReturnsElementsRead()
    {
        byte[] data = [0x02, 0x00, 0x0A, 0x00, 0x14];
        U16BE[] destination = new U16BE[2];
        int elementsRead;
        {
            BinaryParser parser = new(data);
            elementsRead = parser.ReadArrayVarInt(destination.AsSpan());
        }

        await Assert.That(elementsRead).IsEqualTo(2);
        await Assert.That(destination[0].Value).IsEqualTo((ushort)10);
        await Assert.That(destination[1].Value).IsEqualTo((ushort)20);
    }

    /// <summary>Verifies BinaryParser ReadArrayVarInt OversizedCount ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task BinaryParser_ReadArrayVarInt_OversizedCount_ThrowsInvalidOperationException()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F];
        U16BE[] destination = new U16BE[1];
        bool threw = false;
        try { new BinaryParser(data).ReadArrayVarInt(destination.AsSpan()); }
        catch (InvalidOperationException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BinaryParser ReadArrayBE16 U16BE ReturnsElementsRead.</summary>
    [Test]
    public async Task BinaryParser_ReadArrayBE16_U16BE_ReturnsElementsRead()
    {
        byte[] data = [0x00, 0x02, 0x00, 0x03, 0x00, 0x04];
        U16BE[] destination = new U16BE[2];
        int elementsRead;
        {
            BinaryParser parser = new(data);
            elementsRead = parser.ReadArrayBE16(destination.AsSpan());
        }

        await Assert.That(elementsRead).IsEqualTo(2);
        await Assert.That(destination[0].Value).IsEqualTo((ushort)3);
        await Assert.That(destination[1].Value).IsEqualTo((ushort)4);
    }

    /// <summary>Verifies BinaryParser ReadArrayBE32 U16BE ReturnsElementsRead.</summary>
    [Test]
    public async Task BinaryParser_ReadArrayBE32_U16BE_ReturnsElementsRead()
    {
        byte[] data = [0x00, 0x00, 0x00, 0x01, 0x00, 0x05];
        U16BE[] destination = new U16BE[1];
        int elementsRead;
        {
            BinaryParser parser = new(data);
            elementsRead = parser.ReadArrayBE32(destination.AsSpan());
        }

        await Assert.That(elementsRead).IsEqualTo(1);
        await Assert.That(destination[0].Value).IsEqualTo((ushort)5);
    }

    /// <summary>Verifies BinaryParseValidator ValidateType Primitive DoesNotThrow.</summary>
    [Test]
    public async Task BinaryParseValidator_ValidateType_Primitive_DoesNotThrow()
    {
        bool threw = false;
        try { BinaryParseValidator.ValidateType<int>(); }
        catch (NotSupportedException) { threw = true; }

        await Assert.That(threw).IsFalse();
    }

    /// <summary>Verifies BinaryParseValidator ValidateType KnownWrapperName DoesNotThrow.</summary>
    [Test]
    public async Task BinaryParseValidator_ValidateType_KnownWrapperName_DoesNotThrow()
    {
        bool threw = false;
        try { BinaryParseValidator.ValidateType<UIntTestStub>(); }
        catch (NotSupportedException) { threw = true; }

        await Assert.That(threw).IsFalse();
    }

    /// <summary>Verifies BinaryParseValidator ValidateType Array ThrowsNotSupportedException.</summary>
    [Test]
    public async Task BinaryParseValidator_ValidateType_Array_ThrowsNotSupportedException()
    {
        bool threw = false;
        try { BinaryParseValidator.ValidateType<byte[]>(); }
        catch (NotSupportedException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BinaryParseValidator ValidateType UnsupportedType ThrowsNotSupportedException.</summary>
    [Test]
    [Arguments(typeof(object))]
    [Arguments(typeof(string))]
    [Arguments(typeof(UnsupportedReferenceType))]
    [Arguments(typeof(int[]))]
    [Arguments(typeof(UnsupportedValueType))]
    public async Task BinaryParseValidator_ValidateType_UnsupportedType_ThrowsNotSupportedException(Type type)
    {
        bool threw = false;
        try
        {
            System.Reflection.MethodInfo method = typeof(BinaryParseValidator)
                .GetMethod(nameof(BinaryParseValidator.ValidateType))!
                .MakeGenericMethod(type);
            method.Invoke(null, null);
        }
        catch (System.Reflection.TargetInvocationException ex) when (ex.InnerException is NotSupportedException)
        {
            threw = true;
        }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BinaryParseValidator GetTypeErrorMessage ReturnsGuidance.</summary>
    [Test]
    [Arguments(typeof(string), "ReadUtf8Bytes(length)")]
    [Arguments(typeof(byte[]), "ReadArray<T>(count, destination)")]
    [Arguments(typeof(UnsupportedValueType), "is not supported")]
    public async Task BinaryParseValidator_GetTypeErrorMessage_ReturnsGuidance(Type type, string expectedFragment)
    {
        string message = BinaryParseValidator.GetTypeErrorMessage(type);

        await Assert.That(message).Contains(expectedFragment);
    }
}
