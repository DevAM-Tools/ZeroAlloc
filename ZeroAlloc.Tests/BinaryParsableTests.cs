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
// BinaryParsable Generator Tests
// ============================================================================
// Tests for the [BinaryParsable] source generator, including:
// - Basic struct parsing
// - Endian wrapper support
// - BitField support (non-byte-aligned integers)
// - Nested types
// - Various edge cases
// ============================================================================

namespace ZeroAlloc.Tests;

// ============================================================================
// TEST STRUCTS
// ============================================================================

#region Basic Parsing Structs

/// <summary>
/// Simple struct with endian wrappers - standard use case.
/// </summary>
[BinaryParsable]
public readonly partial struct SimpleHeader
{
    public U16BE Version { get; init; }
    public U32BE MessageType { get; init; }
    public U16BE Length { get; init; }
}

/// <summary>
/// Struct with mixed endianness.
/// </summary>
[BinaryParsable]
public readonly partial struct MixedEndianStruct
{
    public U16BE BigEndian16 { get; init; }
    public U16LE LittleEndian16 { get; init; }
    public U32BE BigEndian32 { get; init; }
    public U32LE LittleEndian32 { get; init; }
}

/// <summary>
/// Struct with byte member.
/// </summary>
[BinaryParsable]
public readonly partial struct ByteStruct
{
    public byte First { get; init; }
    public byte Second { get; init; }
    public byte Third { get; init; }
}

/// <summary>
/// Struct with [BinaryIgnore] attribute.
/// </summary>
[BinaryParsable]
public readonly partial struct StructWithIgnoredMember
{
    public U16BE Type { get; init; }
    public U16BE Length { get; init; }

    [BinaryIgnore]
    public bool IsControl => Type.Value < 100;
}

/// <summary>
/// Struct with [BinaryOrder] for custom member order.
/// </summary>
[BinaryParsable]
public readonly partial struct ReorderedStruct
{
    [BinaryOrder(2)]
    public U16BE Third { get; init; }

    [BinaryOrder(0)]
    public U16BE First { get; init; }

    [BinaryOrder(1)]
    public U16BE Second { get; init; }
}

/// <summary>
/// Struct with fixed-length byte array.
/// </summary>
[BinaryParsable]
public readonly partial struct MacAddress
{
    [BinaryFixedLength(6)]
    public byte[] Address { get; init; }
}

#endregion

#region BitField Structs - Non-Byte-Aligned Parsing

/// <summary>
/// CAN frame header - classic example of bit-packed protocol.
/// Standard CAN: 11-bit ID, RTR, IDE, reserved, DLC
/// </summary>
[BinaryParsable]
public readonly partial struct CANStandardHeader
{
    [BinaryField(BitCount = 11)]
    public ushort Identifier { get; init; }    // 11 bits

    [BinaryField(BitCount = 1)]
    public byte RTR { get; init; }              // 1 bit (Remote Transmission Request)

    [BinaryField(BitCount = 1)]
    public byte IDE { get; init; }              // 1 bit (Identifier Extension)

    [BinaryField(BitCount = 1)]
    public byte Reserved { get; init; }         // 1 bit

    [BinaryField(BitCount = 4)]
    public byte DLC { get; init; }              // 4 bits (Data Length Code)
}
// Total: 18 bits = 3 bytes (rounded up, with 6 bits padding)

/// <summary>
/// CAN Extended header - 29-bit identifier.
/// </summary>
[BinaryParsable]
public readonly partial struct CANExtendedHeader
{
    [BinaryField(BitCount = 29)]
    public uint Identifier { get; init; }       // 29 bits

    [BinaryField(BitCount = 1)]
    public byte RTR { get; init; }              // 1 bit

    [BinaryField(BitCount = 1)]
    public byte IDE { get; init; }              // 1 bit

    [BinaryField(BitCount = 1)]
    public byte Reserved { get; init; }         // 1 bit
}
// Total: 32 bits = 4 bytes (exactly aligned)

/// <summary>
/// Simple bit field with various sizes.
/// </summary>
[BinaryParsable]
public readonly partial struct MixedBitFieldStruct
{
    [BinaryField(BitCount = 3)]
    public byte Field3Bit { get; init; }        // 3 bits

    [BinaryField(BitCount = 5)]
    public byte Field5Bit { get; init; }        // 5 bits

    [BinaryField(BitCount = 12)]
    public ushort Field12Bit { get; init; }     // 12 bits

    [BinaryField(BitCount = 4)]
    public byte Field4Bit { get; init; }        // 4 bits
}
// Total: 24 bits = 3 bytes

/// <summary>
/// Single-bit fields (flags).
/// </summary>
[BinaryParsable]
public readonly partial struct BitFlagsStruct
{
    [BinaryField(BitCount = 1)]
    public byte Flag1 { get; init; }

    [BinaryField(BitCount = 1)]
    public byte Flag2 { get; init; }

    [BinaryField(BitCount = 1)]
    public byte Flag3 { get; init; }

    [BinaryField(BitCount = 1)]
    public byte Flag4 { get; init; }

    [BinaryField(BitCount = 1)]
    public byte Flag5 { get; init; }

    [BinaryField(BitCount = 1)]
    public byte Flag6 { get; init; }

    [BinaryField(BitCount = 1)]
    public byte Flag7 { get; init; }

    [BinaryField(BitCount = 1)]
    public byte Flag8 { get; init; }
}
// Total: 8 bits = 1 byte

/// <summary>
/// Large bit field spanning multiple bytes.
/// </summary>
[BinaryParsable]
public readonly partial struct LargeBitFieldStruct
{
    [BinaryField(BitCount = 48)]
    public ulong Field48Bit { get; init; }      // 48 bits (like MAC address as integer)

    [BinaryField(BitCount = 16)]
    public ushort Field16Bit { get; init; }     // 16 bits
}
// Total: 64 bits = 8 bytes

#endregion

// ============================================================================
// TEST CLASSES
// ============================================================================

/// <summary>
/// Tests for [BinaryParsable] generated parsing methods.
/// </summary>
public class BinaryParsableTests
{
    // ========================================================================
    // BASIC PARSING TESTS
    // ========================================================================

    #region Basic Parsing

    [Fact]
    public void SimpleHeader_ParsesCorrectly()
    {
        // Arrange: Version=0x0100, Type=0x12345678, Length=0x00FF
        byte[] data = [0x01, 0x00, 0x12, 0x34, 0x56, 0x78, 0x00, 0xFF];

        // Act
        bool success = SimpleHeader.TryParse(data, out var header, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(8, consumed);
        Assert.Equal(0x0100, header.Version.Value);
        Assert.Equal(0x12345678u, header.MessageType.Value);
        Assert.Equal(0x00FF, header.Length.Value);
    }

    [Fact]
    public void SimpleHeader_FailsWithInsufficientData()
    {
        // Arrange: Only 5 bytes, but header needs 8
        byte[] data = [0x01, 0x00, 0x12, 0x34, 0x56];

        // Act
        bool success = SimpleHeader.TryParse(data, out var header, out int consumed);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void MixedEndian_ParsesCorrectly()
    {
        // Arrange: BE16=0x1234, LE16=0x5678 (stored as 78 56), BE32=0xAABBCCDD, LE32=0x11223344 (stored as 44 33 22 11)
        byte[] data = [0x12, 0x34, 0x78, 0x56, 0xAA, 0xBB, 0xCC, 0xDD, 0x44, 0x33, 0x22, 0x11];

        // Act
        bool success = MixedEndianStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(12, consumed);
        Assert.Equal(0x1234, result.BigEndian16.Value);
        Assert.Equal(0x5678, result.LittleEndian16.Value);
        Assert.Equal(0xAABBCCDDu, result.BigEndian32.Value);
        Assert.Equal(0x11223344u, result.LittleEndian32.Value);
    }

    [Fact]
    public void ByteStruct_ParsesCorrectly()
    {
        // Arrange
        byte[] data = [0x11, 0x22, 0x33];

        // Act
        bool success = ByteStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(3, consumed);
        Assert.Equal(0x11, result.First);
        Assert.Equal(0x22, result.Second);
        Assert.Equal(0x33, result.Third);
    }

    [Fact]
    public void StructWithIgnoredMember_ParsesOnlyRelevantFields()
    {
        // Arrange: Type=0x0010, Length=0x0100
        byte[] data = [0x00, 0x10, 0x01, 0x00];

        // Act
        bool success = StructWithIgnoredMember.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(4, consumed);
        Assert.Equal(0x0010, result.Type.Value);
        Assert.Equal(0x0100, result.Length.Value);
        Assert.True(result.IsControl); // 16 < 100
    }

    [Fact]
    public void ReorderedStruct_ParsesInCorrectOrder()
    {
        // Arrange: Data is read as First(0), Second(1), Third(2)
        // So bytes: First=0x1111, Second=0x2222, Third=0x3333
        byte[] data = [0x11, 0x11, 0x22, 0x22, 0x33, 0x33];

        // Act
        bool success = ReorderedStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(6, consumed);
        Assert.Equal(0x1111, result.First.Value);
        Assert.Equal(0x2222, result.Second.Value);
        Assert.Equal(0x3333, result.Third.Value);
    }

    [Fact]
    public void MacAddress_ParsesFixedLengthArray()
    {
        // Arrange: MAC address AA:BB:CC:DD:EE:FF
        byte[] data = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF];

        // Act
        bool success = MacAddress.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(6, consumed);
        Assert.Equal(data, result.Address);
    }

    #endregion

    // ========================================================================
    // BITFIELD PARSING TESTS
    // ========================================================================

    #region BitField Parsing

    [Fact]
    public void CANStandardHeader_ParsesCorrectly()
    {
        // Arrange: Using specific bit pattern
        // The BitReader reads bits MSB first (big-endian bit order)
        //
        // Layout (18 bits):
        // - Identifier: 11 bits
        // - RTR: 1 bit
        // - IDE: 1 bit  
        // - Reserved: 1 bit
        // - DLC: 4 bits
        //
        // Example: ID=0x123, RTR=1, IDE=0, Reserved=0, DLC=8
        // ID = 0x123 = 0b001_0010_0011 (11 bits, reading MSB first)
        // 
        // Full bit stream (MSB first):
        // 00100100011 | 1 | 0 | 0 | 1000
        //
        // Byte 0: 00100100 = 0x24
        // Byte 1: 01110010 = 0x72  (remaining 3 bits of ID: 011, RTR=1, IDE=0, Reserved=0, DLC high bits=10)
        // Byte 2: 00?????? = 0x00 (DLC low bits=00, padding)

        byte[] data = [0x24, 0x72, 0x00];

        // Act
        bool success = CANStandardHeader.TryParse(data, out var header, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(3, consumed);
        Assert.Equal(0x123, header.Identifier);
        Assert.Equal(1, header.RTR);
        Assert.Equal(0, header.IDE);
        Assert.Equal(0, header.Reserved);
        Assert.Equal(8, header.DLC);
    }

    [Fact]
    public void CANExtendedHeader_ParsesCorrectly()
    {
        // Arrange: 29-bit ID, RTR, IDE, Reserved = 32 bits total
        // ID = 0x1FFFFFFF (max 29-bit value)
        // RTR = 1, IDE = 1, Reserved = 0
        //
        // Bit stream: 11111111111111111111111111111_1_1_0 = 32 bits
        // All bits packed into 4 bytes
        // Byte 0: 11111111 = 0xFF
        // Byte 1: 11111111 = 0xFF
        // Byte 2: 11111111 = 0xFF
        // Byte 3: 11111110 = 0xFE (last bit of ID + RTR=1, IDE=1, Reserved=0)

        byte[] data = [0xFF, 0xFF, 0xFF, 0xFE];

        // Act
        bool success = CANExtendedHeader.TryParse(data, out var header, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(4, consumed);
        Assert.Equal(0x1FFFFFFFu, header.Identifier);
        Assert.Equal(1, header.RTR);
        Assert.Equal(1, header.IDE);
        Assert.Equal(0, header.Reserved);
    }

    [Fact]
    public void MixedBitFieldStruct_ParsesCorrectly()
    {
        // Arrange: 3-bit + 5-bit + 12-bit + 4-bit = 24 bits = 3 bytes
        // Field3Bit = 5 = 0b101
        // Field5Bit = 17 = 0b10001
        // Field12Bit = 2047 = 0b0111_1111_1111
        // Field4Bit = 9 = 0b1001
        //
        // Bit stream: 101_10001_011111111111_1001 = 24 bits
        // Byte 0: 10110001 = 0xB1
        // Byte 1: 01111111 = 0x7F
        // Byte 2: 11111001 = 0xF9

        byte[] data = [0xB1, 0x7F, 0xF9];

        // Act
        bool success = MixedBitFieldStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(3, consumed);
        Assert.Equal(5, result.Field3Bit);
        Assert.Equal(17, result.Field5Bit);
        Assert.Equal(2047, result.Field12Bit);
        Assert.Equal(9, result.Field4Bit);
    }

    [Fact]
    public void BitFlagsStruct_ParsesAllFlags()
    {
        // Arrange: 8 x 1-bit = 8 bits = 1 byte
        // Flags: 1, 0, 1, 1, 0, 0, 1, 0 = 0b10110010 = 0xB2
        byte[] data = [0xB2];

        // Act
        bool success = BitFlagsStruct.TryParse(data, out var flags, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(1, consumed);
        Assert.Equal(1, flags.Flag1);
        Assert.Equal(0, flags.Flag2);
        Assert.Equal(1, flags.Flag3);
        Assert.Equal(1, flags.Flag4);
        Assert.Equal(0, flags.Flag5);
        Assert.Equal(0, flags.Flag6);
        Assert.Equal(1, flags.Flag7);
        Assert.Equal(0, flags.Flag8);
    }

    [Fact]
    public void LargeBitFieldStruct_ParsesCorrectly()
    {
        // Arrange: 48-bit + 16-bit = 64 bits = 8 bytes
        // Field48Bit = 0xAABBCCDDEEFF (48 bits)
        // Field16Bit = 0x1234 (16 bits)
        byte[] data = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x12, 0x34];

        // Act
        bool success = LargeBitFieldStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(8, consumed);
        Assert.Equal(0xAABBCCDDEEFFuL, result.Field48Bit);
        Assert.Equal(0x1234, result.Field16Bit);
    }

    [Fact]
    public void CANStandardHeader_FailsWithInsufficientData()
    {
        // Arrange: Only 2 bytes, but header needs 3 (18 bits)
        byte[] data = [0x00, 0x00];

        // Act
        bool success = CANStandardHeader.TryParse(data, out var header, out int consumed);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void BitField_ZeroValues_ParsesCorrectly()
    {
        // Arrange: All zeros
        byte[] data = [0x00, 0x00, 0x00];

        // Act
        bool success = CANStandardHeader.TryParse(data, out var header, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(0, header.Identifier);
        Assert.Equal(0, header.RTR);
        Assert.Equal(0, header.IDE);
        Assert.Equal(0, header.Reserved);
        Assert.Equal(0, header.DLC);
    }

    [Fact]
    public void BitField_MaxValues_ParsesCorrectly()
    {
        // Arrange: Maximum values for each field
        // ID = 0x7FF (11 bits max), RTR=1, IDE=1, Reserved=1, DLC=15 (4 bits max)
        // Bit stream: 11111111111_1_1_1_1111 = 18 bits
        // Byte 0: 11111111 = 0xFF
        // Byte 1: 11111111 = 0xFF
        // Byte 2: 11?????? = 0xC0 (with padding)

        byte[] data = [0xFF, 0xFF, 0xC0];

        // Act
        bool success = CANStandardHeader.TryParse(data, out var header, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(0x7FF, header.Identifier); // Max 11-bit value
        Assert.Equal(1, header.RTR);
        Assert.Equal(1, header.IDE);
        Assert.Equal(1, header.Reserved);
        Assert.Equal(15, header.DLC); // Max 4-bit value
    }

    #endregion

    // ========================================================================
    // EDGE CASES
    // ========================================================================

    #region Edge Cases

    [Fact]
    public void SimpleHeader_EmptySpan_ReturnsFalse()
    {
        // Arrange
        ReadOnlySpan<byte> empty = [];

        // Act
        bool success = SimpleHeader.TryParse(empty, out var header, out int consumed);

        // Assert
        Assert.False(success);
    }

    [Fact]
    public void SimpleHeader_ExtraData_IgnoresExtraBytes()
    {
        // Arrange: Valid header + extra bytes
        byte[] data = [0x01, 0x00, 0x12, 0x34, 0x56, 0x78, 0x00, 0xFF, 0xDE, 0xAD, 0xBE, 0xEF];

        // Act
        bool success = SimpleHeader.TryParse(data, out var header, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(8, consumed); // Only consumed what was needed
    }

    #endregion

    // ========================================================================
    // PADDING BITS TESTS
    // ========================================================================

    #region PaddingBits Tests

    [Fact]
    public void PaddingBitsStruct_ParsesCorrectlyWithPadding()
    {
        // Arrange: 4-bit flags + 4-bit padding (skipped after Flags) + 4-byte payload
        // Byte 0: Flags(4 bits) = 0xA, Padding(4 bits, skipped) = 0xB  => 0xAB
        // Bytes 1-4: Payload BE = 0x12345678
        byte[] data = [0xAB, 0x12, 0x34, 0x56, 0x78];

        // Act
        bool success = PaddingBitsTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(0xA, result.Flags);
        Assert.Equal(0x12345678u, result.Payload.Value);
        Assert.Equal(5, consumed);
    }

    #endregion

    // ========================================================================
    // PRIMITIVE INTEGER TESTS
    // ========================================================================

    #region Primitive Integer Tests

    [Fact]
    public void PrimitiveIntegerStructBE_ParsesAllTypes()
    {
        // Arrange: All primitive types in Big-Endian order
        // short (2) + ushort (2) + int (4) + uint (4) + long (8) + ulong (8) + float (4) + double (8) + sbyte (1) = 41 bytes
        byte[] data =
        [
            0x00, 0x64,                                         // short: 100 (BE)
            0x00, 0xC8,                                         // ushort: 200 (BE)
            0x00, 0x00, 0x01, 0x2C,                             // int: 300 (BE)
            0x00, 0x00, 0x01, 0x90,                             // uint: 400 (BE)
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0xF4,     // long: 500 (BE)
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x58,     // ulong: 600 (BE)
            0x44, 0x96, 0x00, 0x00,                             // float: 1200.0f (BE)
            0x40, 0xA9, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,     // double: 3200.0 (BE)
            0xFE                                                 // sbyte: -2
        ];

        // Act
        bool success = PrimitiveIntegerTestStructBE.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(100, result.Int16Value);
        Assert.Equal(200, result.UInt16Value);
        Assert.Equal(300, result.Int32Value);
        Assert.Equal(400u, result.UInt32Value);
        Assert.Equal(500L, result.Int64Value);
        Assert.Equal(600UL, result.UInt64Value);
        Assert.Equal(1200.0f, result.SingleValue);
        Assert.Equal(3200.0, result.DoubleValue);
        Assert.Equal(-2, result.SByteValue);
        Assert.Equal(41, consumed);
    }

    [Fact]
    public void PrimitiveIntegerStructLE_ParsesLittleEndian()
    {
        // Arrange: int (4) + float (4) in Little-Endian
        byte[] data =
        [
            0x2C, 0x01, 0x00, 0x00,                             // int: 300 (LE)
            0x00, 0x00, 0x96, 0x44                              // float: 1200.0f (LE)
        ];

        // Act
        bool success = PrimitiveIntegerTestStructLE.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(300, result.Int32Value);
        Assert.Equal(1200.0f, result.SingleValue);
        Assert.Equal(8, consumed);
    }

    [Fact]
    public void PrimitiveIntegerStructBE_ReportsTryGetSerializedSizeCorrectly()
    {
        // Act
        bool hasFixedSize = PrimitiveIntegerTestStructBE.TryGetSerializedSize(out int size);

        // Assert
        Assert.True(hasFixedSize);
        Assert.Equal(41, size); // 2+2+4+4+8+8+4+8+1 = 41 bytes
    }

    #endregion

    // ========================================================================
    // STRING PARSING TESTS
    // ========================================================================

    #region String Parsing Tests

    [Fact]
    public void StringVarIntStruct_ParsesVarIntPrefixedString()
    {
        // Arrange: VarInt length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringVarIntTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(6, consumed); // 1 byte VarInt + 5 bytes string
    }

    [Fact]
    public void StringFixedBEStruct_ParsesBigEndianLengthPrefixedString()
    {
        // Arrange: 2-byte BE length (5) + "Hello" UTF-8
        byte[] data = [0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringFixedBE2TestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(7, consumed); // 2 bytes length + 5 bytes string
    }

    [Fact]
    public void StringFixedLEStruct_ParsesLittleEndianLengthPrefixedString()
    {
        // Arrange: 2-byte LE length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x00, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringFixedLE2TestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(7, consumed); // 2 bytes length + 5 bytes string
    }

    [Fact]
    public void StringNullTerminatedStruct_ParsesNullTerminatedString()
    {
        // Arrange: "Hello" + null byte
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00];

        // Act
        bool success = StringNullTerminatedTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(6, consumed); // 5 bytes string + 1 null
    }

    [Fact]
    public void StringFixedLengthStruct_ParsesFixedLengthString()
    {
        // Arrange: "Hello" padded to 8 bytes
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x00, 0x00];

        // Act
        bool success = StringFixedLengthTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(8, consumed); // Fixed 8 bytes
    }

    [Fact]
    public void StringWithHeaderStruct_ParsesHeaderAndString()
    {
        // Arrange: 2-byte version BE + VarInt length (5) + "Hello"
        byte[] data = [0x01, 0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringWithHeaderTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(0x0100, result.Version.Value);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(8, consumed);
    }

    [Fact]
    public void Utf8Var_ParsesVarIntPrefixedString()
    {
        // Arrange: VarInt length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = Utf8Var.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Value);
        Assert.Equal(6, consumed);
    }

    [Fact]
    public void Utf8FixBE_ParsesBigEndianLengthPrefixedString()
    {
        // Arrange: 4-byte BE length (5) + "Hello" UTF-8
        byte[] data = [0x00, 0x00, 0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = Utf8FixBE.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Value);
        Assert.Equal(9, consumed);
    }

    [Fact]
    public void Utf8Z_ParsesNullTerminatedString()
    {
        // Arrange: "Hello" + null byte
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00];

        // Act
        bool success = Utf8Z.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Value);
        Assert.Equal(6, consumed);
    }

    [Fact]
    public void StringWrapperInBinaryParsable_WorksWithGenerator()
    {
        // Arrange: 4-byte BE length (5) + "Hello" UTF-8
        byte[] data = [0x00, 0x00, 0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = Utf8FixBETestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message.Value);
        Assert.Equal(9, consumed);
    }

    #endregion

    // ========================================================================
    // NEW STRING ATTRIBUTE TESTS
    // ========================================================================

    #region New String Attribute Tests

    [Fact]
    public void StringVarIntAttr_ParsesVarIntPrefixedString()
    {
        // Arrange: VarInt length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringVarIntAttrTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(6, consumed);
    }

    [Fact]
    public void StringBE2Attr_ParsesBigEndianLengthPrefixedString()
    {
        // Arrange: 2-byte BE length (5) + "Hello" UTF-8
        byte[] data = [0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringBE2AttrTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(7, consumed);
    }

    [Fact]
    public void StringLE2Attr_ParsesLittleEndianLengthPrefixedString()
    {
        // Arrange: 2-byte LE length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x00, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringLE2AttrTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(7, consumed);
    }

    [Fact]
    public void StringNullTermAttr_ParsesNullTerminatedString()
    {
        // Arrange: "Hello" + null byte
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00];

        // Act
        bool success = StringNullTermAttrTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(6, consumed);
    }

    [Fact]
    public void StringFixedAttr_ParsesFixedLengthString()
    {
        // Arrange: "Hello" padded to 8 bytes
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x00, 0x00];

        // Act
        bool success = StringFixedAttrTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(8, consumed);
    }

    [Fact]
    public void StringFromField_ParsesStringWithLengthFromField()
    {
        // Arrange: 1-byte length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringFromFieldTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(5, result.NameLength);
        Assert.Equal("Hello", result.Name);
        Assert.Equal(6, consumed);
    }

    [Fact]
    public void StringFromFieldBE_ParsesStringWithLengthFromBEField()
    {
        // Arrange: 2-byte BE length (5) + "Hello" UTF-8
        byte[] data = [0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringFromFieldBETestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(5, result.MessageLength.Value);
        Assert.Equal("Hello", result.Message);
        Assert.Equal(7, consumed);
    }

    #endregion

    // ========================================================================
    // BYTE ARRAY ATTRIBUTE TESTS
    // ========================================================================

    #region Byte Array Attribute Tests

    [Fact]
    public void ByteArrayVarInt_ParsesVarIntPrefixedArray()
    {
        // Arrange: VarInt length (4) + data bytes
        byte[] data = [0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = ByteArrayVarIntTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal([0xAA, 0xBB, 0xCC, 0xDD], result.Data);
        Assert.Equal(5, consumed);
    }

    [Fact]
    public void ByteArrayBE2_ParsesBigEndianLengthPrefixedArray()
    {
        // Arrange: 2-byte BE length (4) + data bytes
        byte[] data = [0x00, 0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = ByteArrayBE2TestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal([0xAA, 0xBB, 0xCC, 0xDD], result.Data);
        Assert.Equal(6, consumed);
    }

    [Fact]
    public void ByteArrayLE4_ParsesLittleEndianLengthPrefixedArray()
    {
        // Arrange: 4-byte LE length (4) + data bytes
        byte[] data = [0x04, 0x00, 0x00, 0x00, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = ByteArrayLE4TestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal([0xAA, 0xBB, 0xCC, 0xDD], result.Data);
        Assert.Equal(8, consumed);
    }

    [Fact]
    public void ByteArrayFromField_ParsesArrayWithLengthFromField()
    {
        // Arrange: 2-byte BE length (4) + data bytes
        byte[] data = [0x00, 0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = ByteArrayFromFieldTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(4, result.DataLength.Value);
        Assert.Equal([0xAA, 0xBB, 0xCC, 0xDD], result.Data);
        Assert.Equal(6, consumed);
    }

    [Fact]
    public void MemoryVarInt_ParsesVarIntPrefixedMemory()
    {
        // Arrange: VarInt length (4) + data bytes
        byte[] data = [0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = MemoryVarIntTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(4, result.Data.Length);
        Assert.Equal([0xAA, 0xBB, 0xCC, 0xDD], result.Data.ToArray());
        Assert.Equal(5, consumed);
    }

    [Fact]
    public void ReadOnlyMemoryBE2_ParsesBigEndianLengthPrefixedMemory()
    {
        // Arrange: 2-byte BE length (4) + data bytes
        byte[] data = [0x00, 0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = ReadOnlyMemoryBE2TestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(4, result.Data.Length);
        Assert.Equal([0xAA, 0xBB, 0xCC, 0xDD], result.Data.ToArray());
        Assert.Equal(6, consumed);
    }

    [Fact]
    public void MemoryFromField_ParsesMemoryWithLengthFromField()
    {
        // Arrange: 1-byte length (4) + data bytes
        byte[] data = [0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = MemoryFromFieldTestStruct.TryParse(data, out var result, out int consumed);

        // Assert
        Assert.True(success);
        Assert.Equal(4, result.DataSize);
        Assert.Equal(4, result.Data.Length);
        Assert.Equal([0xAA, 0xBB, 0xCC, 0xDD], result.Data.ToArray());
        Assert.Equal(5, consumed);
    }

    #endregion
}

// ============================================================================
// TEST STRUCTS FOR NEW FEATURES
// ============================================================================

#region PaddingBits Test Structs

/// <summary>
/// Test struct with PaddingBits to skip reserved bits AFTER a field.
/// </summary>
[BinaryParsable]
public readonly partial struct PaddingBitsTestStruct
{
    [BinaryField(BitCount = 4, PaddingBits = 4)]  // 4 bits for Flags, then skip 4 padding bits → byte-aligned!
    public byte Flags { get; init; }

    public U32BE Payload { get; init; }       // 4 bytes
}

#endregion

#region String Parsing Test Structs

/// <summary>
/// String with VarInt length prefix.
/// </summary>
[BinaryParsable]
public readonly partial struct StringVarIntTestStruct
{
    [BinaryStringLength(StringLengthEncoding.VarInt)]
    public string Message { get; init; }
}

/// <summary>
/// String with 2-byte big-endian length prefix.
/// </summary>
[BinaryParsable]
public readonly partial struct StringFixedBE2TestStruct
{
    [BinaryStringLength(StringLengthEncoding.FixedBE, LengthBytes = 2)]
    public string Message { get; init; }
}

/// <summary>
/// String with 2-byte little-endian length prefix.
/// </summary>
[BinaryParsable]
public readonly partial struct StringFixedLE2TestStruct
{
    [BinaryStringLength(StringLengthEncoding.FixedLE, LengthBytes = 2)]
    public string Message { get; init; }
}

/// <summary>
/// Null-terminated string.
/// </summary>
[BinaryParsable]
public readonly partial struct StringNullTerminatedTestStruct
{
    [BinaryStringLength(StringLengthEncoding.NullTerminated)]
    public string Message { get; init; }
}

/// <summary>
/// Fixed-length string (8 bytes).
/// </summary>
[BinaryParsable]
public readonly partial struct StringFixedLengthTestStruct
{
    [BinaryStringLength(StringLengthEncoding.Fixed, FixedLength = 8)]
    public string Message { get; init; }
}

/// <summary>
/// Struct with header + string.
/// </summary>
[BinaryParsable]
public readonly partial struct StringWithHeaderTestStruct
{
    public U16BE Version { get; init; }

    [BinaryStringLength(StringLengthEncoding.VarInt)]
    public string Message { get; init; }
}

#endregion

#region Primitive Integer Test Structs

/// <summary>
/// Test struct with primitive integer types (explicit endianness via BinaryParsable).
/// </summary>
[BinaryParsable(DefaultEndianness = Endianness.BigEndian)]
public readonly partial struct PrimitiveIntegerTestStructBE
{
    public short Int16Value { get; init; }
    public ushort UInt16Value { get; init; }
    public int Int32Value { get; init; }
    public uint UInt32Value { get; init; }
    public long Int64Value { get; init; }
    public ulong UInt64Value { get; init; }
    public float SingleValue { get; init; }
    public double DoubleValue { get; init; }
    public sbyte SByteValue { get; init; }
}

/// <summary>
/// Test struct with primitive integer types (little-endian).
/// </summary>
[BinaryParsable(DefaultEndianness = Endianness.LittleEndian)]
public readonly partial struct PrimitiveIntegerTestStructLE
{
    public int Int32Value { get; init; }
    public float SingleValue { get; init; }
}

#endregion

#region String Wrapper Test Structs

/// <summary>
/// Test struct using Utf8FixBE wrapper type directly.
/// </summary>
[BinaryParsable]
public readonly partial struct Utf8FixBETestStruct
{
    public Utf8FixBE Message { get; init; }
}

#endregion

#region New String Attribute Test Structs

/// <summary>
/// String with VarInt length prefix using new attribute.
/// </summary>
[BinaryParsable]
public readonly partial struct StringVarIntAttrTestStruct
{
    [StringLengthVarInt]
    public string Message { get; init; }
}

/// <summary>
/// String with 2-byte big-endian length prefix using new attribute.
/// </summary>
[BinaryParsable]
public readonly partial struct StringBE2AttrTestStruct
{
    [StringLengthBE(2)]
    public string Message { get; init; }
}

/// <summary>
/// String with 2-byte little-endian length prefix using new attribute.
/// </summary>
[BinaryParsable]
public readonly partial struct StringLE2AttrTestStruct
{
    [StringLengthLE(2)]
    public string Message { get; init; }
}

/// <summary>
/// Null-terminated string using new attribute.
/// </summary>
[BinaryParsable]
public readonly partial struct StringNullTermAttrTestStruct
{
    [StringNullTerminated]
    public string Message { get; init; }
}

/// <summary>
/// Fixed-length string using new attribute.
/// </summary>
[BinaryParsable]
public readonly partial struct StringFixedAttrTestStruct
{
    [StringFixedLength(8)]
    public string Message { get; init; }
}

/// <summary>
/// String with length from another field.
/// </summary>
[BinaryParsable]
public readonly partial struct StringFromFieldTestStruct
{
    public byte NameLength { get; init; }

    [StringLengthFromField(nameof(NameLength))]
    public string Name { get; init; }
}

/// <summary>
/// String with length from U16BE field.
/// </summary>
[BinaryParsable]
public readonly partial struct StringFromFieldBETestStruct
{
    public U16BE MessageLength { get; init; }

    [StringLengthFromField(nameof(MessageLength))]
    public string Message { get; init; }
}

#endregion

#region Byte Array Attribute Test Structs

/// <summary>
/// Byte array with VarInt length prefix.
/// </summary>
[BinaryParsable]
public readonly partial struct ByteArrayVarIntTestStruct
{
    [BytesLengthVarInt]
    public byte[] Data { get; init; }
}

/// <summary>
/// Byte array with 2-byte big-endian length prefix.
/// </summary>
[BinaryParsable]
public readonly partial struct ByteArrayBE2TestStruct
{
    [BytesLengthBE(2)]
    public byte[] Data { get; init; }
}

/// <summary>
/// Byte array with 4-byte little-endian length prefix.
/// </summary>
[BinaryParsable]
public readonly partial struct ByteArrayLE4TestStruct
{
    [BytesLengthLE(4)]
    public byte[] Data { get; init; }
}

/// <summary>
/// Byte array with length from another field.
/// </summary>
[BinaryParsable]
public readonly partial struct ByteArrayFromFieldTestStruct
{
    public U16BE DataLength { get; init; }

    [BytesLengthFromField(nameof(DataLength))]
    public byte[] Data { get; init; }
}

/// <summary>
/// Memory with VarInt length prefix.
/// </summary>
[BinaryParsable]
public readonly partial struct MemoryVarIntTestStruct
{
    [BytesLengthVarInt]
    public Memory<byte> Data { get; init; }
}

/// <summary>
/// ReadOnlyMemory with 2-byte big-endian length prefix.
/// </summary>
[BinaryParsable]
public readonly partial struct ReadOnlyMemoryBE2TestStruct
{
    [BytesLengthBE(2)]
    public ReadOnlyMemory<byte> Data { get; init; }
}

/// <summary>
/// Memory with length from field.
/// </summary>
[BinaryParsable]
public readonly partial struct MemoryFromFieldTestStruct
{
    public byte DataSize { get; init; }

    [BytesLengthFromField(nameof(DataSize))]
    public Memory<byte> Data { get; init; }
}

#endregion

#region VarInt and VarIntZigZag IBinaryParsable Tests

/// <summary>
/// Tests for VarInt IBinaryParsable implementation.
/// </summary>
public class VarIntTests
{
    [Theory]
    [InlineData(0UL, new byte[] { 0x00 })]
    [InlineData(127UL, new byte[] { 0x7F })]
    [InlineData(128UL, new byte[] { 0x80, 0x01 })]
    [InlineData(300UL, new byte[] { 0xAC, 0x02 })]
    [InlineData(16383UL, new byte[] { 0xFF, 0x7F })]
    [InlineData(16384UL, new byte[] { 0x80, 0x80, 0x01 })]
    [InlineData(2097151UL, new byte[] { 0xFF, 0xFF, 0x7F })]                       // 3-byte max (2^21 - 1)
    [InlineData(2097152UL, new byte[] { 0x80, 0x80, 0x80, 0x01 })]                 // 4-byte min (2^21)
    [InlineData(268435455UL, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F })]               // 4-byte max (2^28 - 1)
    [InlineData(268435456UL, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x01 })]         // 5-byte min (2^28)
    [InlineData(34359738367UL, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x7F })]       // 5-byte max (2^35 - 1)
    public void VarInt_TryParse_ParsesCorrectly(ulong expected, byte[] data)
    {
        // Act
        bool success = VarInt.TryParse(data, out VarInt value, out int bytesConsumed);

        // Assert
        Assert.True(success);
        Assert.Equal(expected, value.Value);
        Assert.Equal(data.Length, bytesConsumed);
    }

    [Fact]
    public void VarInt_TryFormat_WritesCorrectly()
    {
        // Arrange
        VarInt varInt = new(300);
        Span<byte> buffer = stackalloc byte[10];

        // Act
        bool success = varInt.TryFormat(buffer, out int bytesWritten, default, null);

        // Assert
        Assert.True(success);
        Assert.Equal(2, bytesWritten);
        Assert.Equal(0xAC, buffer[0]);
        Assert.Equal(0x02, buffer[1]);
    }

    [Fact]
    public void VarInt_EncodedSize_ReturnsCorrectSize()
    {
        // Arrange & Act & Assert
        Assert.Equal(1, new VarInt(0).EncodedSize);
        Assert.Equal(1, new VarInt(127).EncodedSize);              // 1-byte max
        Assert.Equal(2, new VarInt(128).EncodedSize);              // 2-byte min
        Assert.Equal(2, new VarInt(16383).EncodedSize);            // 2-byte max
        Assert.Equal(3, new VarInt(16384).EncodedSize);            // 3-byte min
        Assert.Equal(3, new VarInt(2097151).EncodedSize);          // 3-byte max (2^21 - 1)
        Assert.Equal(4, new VarInt(2097152).EncodedSize);          // 4-byte min (2^21)
        Assert.Equal(4, new VarInt(268435455).EncodedSize);        // 4-byte max (2^28 - 1)
        Assert.Equal(5, new VarInt(268435456).EncodedSize);        // 5-byte min (2^28)
        Assert.Equal(5, new VarInt(34359738367).EncodedSize);      // 5-byte max (2^35 - 1)
        Assert.Equal(10, new VarInt(ulong.MaxValue).EncodedSize);  // maximum: 10 bytes
    }

    /// <summary>Verifies VarInt roundtrip for large / extreme values including ulong.MaxValue.</summary>
    [Fact]
    public void VarInt_RoundTrip_ExtremeValues()
    {
        ulong[] testValues =
        [
            0, 127, 128, 16383, 16384,
            2097151, 2097152,           // 3/4-byte boundary
            268435455, 268435456,       // 4/5-byte boundary
            34359738367, 34359738368,   // 5/6-byte boundary
            ulong.MaxValue
        ];
        byte[] buffer = new byte[VarInt.MaxSize];

        foreach (ulong expected in testValues)
        {
            VarInt original = new(expected);

            // Write
            bool writeSuccess = original.TryFormat(buffer, out int bytesWritten, default, null);
            Assert.True(writeSuccess, $"Failed to write {expected}");

            // Read
            bool readSuccess = VarInt.TryParse(buffer[..bytesWritten], out VarInt parsed, out int bytesConsumed);
            Assert.True(readSuccess, $"Failed to read {expected}");
            Assert.Equal(expected, parsed.Value);
            Assert.Equal(bytesWritten, bytesConsumed);
        }
    }

    [Fact]
    public void VarInt_TryParse_InsufficientData_ReturnsFalse()
    {
        // Arrange: Continuation bit set but no more data
        byte[] data = [0x80];

        // Act
        bool success = VarInt.TryParse(data, out VarInt value, out int bytesConsumed);

        // Assert
        Assert.False(success);
        Assert.Equal(0, bytesConsumed);
    }

    [Fact]
    public void VarInt_TryGetSerializedSize_Instance_ReturnsEncodedSize()
    {
        // VarInt instance TryGetSerializedSize returns the actual encoded size
        VarInt varInt0 = new(0);
        VarInt varInt127 = new(127);
        VarInt varInt128 = new(128);
        VarInt varInt16383 = new(16383);

        Assert.True(varInt0.TryGetSerializedSize(out int size0));
        Assert.Equal(1, size0);

        Assert.True(varInt127.TryGetSerializedSize(out int size127));
        Assert.Equal(1, size127);

        Assert.True(varInt128.TryGetSerializedSize(out int size128));
        Assert.Equal(2, size128);

        Assert.True(varInt16383.TryGetSerializedSize(out int size16383));
        Assert.Equal(2, size16383);
    }

    [Fact]
    public void VarInt_ToString_ReturnsDecimalString()
    {
        // Arrange
        VarInt varInt = new(12345);

        // Act & Assert
        Assert.Equal("12345", varInt.ToString());
    }

    [Fact]
    public void VarInt_ImplicitConversions_Work()
    {
        // Arrange & Act
        VarInt fromUlong = 12345UL;
        VarInt fromUint = 12345U;
        VarInt fromUshort = (ushort)12345;
        VarInt fromByte = (byte)123;

        // Assert
        Assert.Equal(12345UL, fromUlong.Value);
        Assert.Equal(12345UL, fromUint.Value);
        Assert.Equal(12345UL, fromUshort.Value);
        Assert.Equal(123UL, fromByte.Value);
    }
}

/// <summary>
/// Tests for VarIntZigZag IBinaryParsable implementation.
/// </summary>
public class VarIntZigZagTests
{
    [Theory]
    [InlineData(0L, new byte[] { 0x00 })]
    [InlineData(-1L, new byte[] { 0x01 })]
    [InlineData(1L, new byte[] { 0x02 })]
    [InlineData(-2L, new byte[] { 0x03 })]
    [InlineData(2L, new byte[] { 0x04 })]
    [InlineData(-64L, new byte[] { 0x7F })]
    [InlineData(64L, new byte[] { 0x80, 0x01 })]
    public void VarIntZigZag_TryParse_ParsesCorrectly(long expected, byte[] data)
    {
        // Act
        bool success = VarIntZigZag.TryParse(data, out VarIntZigZag value, out int bytesConsumed);

        // Assert
        Assert.True(success);
        Assert.Equal(expected, value.Value);
        Assert.Equal(data.Length, bytesConsumed);
    }

    [Fact]
    public void VarIntZigZag_TryFormat_WritesCorrectly()
    {
        // Arrange
        VarIntZigZag varInt = new(-1);
        Span<byte> buffer = stackalloc byte[10];

        // Act
        bool success = varInt.TryFormat(buffer, out int bytesWritten, default, null);

        // Assert
        Assert.True(success);
        Assert.Equal(1, bytesWritten);
        Assert.Equal(0x01, buffer[0]);
    }

    [Fact]
    public void VarIntZigZag_ZigZagEncoded_ReturnsCorrectValue()
    {
        // ZigZag encoding: 0 → 0, -1 → 1, 1 → 2, -2 → 3, 2 → 4
        Assert.Equal(0UL, new VarIntZigZag(0).ZigZagEncoded);
        Assert.Equal(1UL, new VarIntZigZag(-1).ZigZagEncoded);
        Assert.Equal(2UL, new VarIntZigZag(1).ZigZagEncoded);
        Assert.Equal(3UL, new VarIntZigZag(-2).ZigZagEncoded);
        Assert.Equal(4UL, new VarIntZigZag(2).ZigZagEncoded);
    }

    [Fact]
    public void VarIntZigZag_DecodeZigZag_DecodesCorrectly()
    {
        // Arrange & Act & Assert
        Assert.Equal(0L, VarIntZigZag.DecodeZigZag(0));
        Assert.Equal(-1L, VarIntZigZag.DecodeZigZag(1));
        Assert.Equal(1L, VarIntZigZag.DecodeZigZag(2));
        Assert.Equal(-2L, VarIntZigZag.DecodeZigZag(3));
        Assert.Equal(2L, VarIntZigZag.DecodeZigZag(4));
    }

    [Fact]
    public void VarIntZigZag_EncodedSize_ReturnsCorrectSize()
    {
        // Arrange & Act & Assert
        Assert.Equal(1, new VarIntZigZag(0).EncodedSize);
        Assert.Equal(1, new VarIntZigZag(-1).EncodedSize);
        Assert.Equal(1, new VarIntZigZag(-64).EncodedSize);         // 1-byte max negative
        Assert.Equal(1, new VarIntZigZag(63).EncodedSize);          // 1-byte max positive
        Assert.Equal(2, new VarIntZigZag(64).EncodedSize);          // 2-byte min
        Assert.Equal(2, new VarIntZigZag(-65).EncodedSize);         // 2-byte min negative
        Assert.Equal(2, new VarIntZigZag(8191).EncodedSize);        // 2-byte max positive
        Assert.Equal(2, new VarIntZigZag(-8192).EncodedSize);       // 2-byte max negative
        Assert.Equal(3, new VarIntZigZag(8192).EncodedSize);        // 3-byte min
        Assert.Equal(3, new VarIntZigZag(-8193).EncodedSize);       // 3-byte min negative
        Assert.Equal(3, new VarIntZigZag(1048575).EncodedSize);     // 3-byte max positive
        Assert.Equal(3, new VarIntZigZag(-1048576).EncodedSize);    // 3-byte max negative
        Assert.Equal(4, new VarIntZigZag(1048576).EncodedSize);     // 4-byte min
        Assert.Equal(4, new VarIntZigZag(-1048577).EncodedSize);    // 4-byte min negative
        Assert.Equal(10, new VarIntZigZag(long.MaxValue).EncodedSize);
        Assert.Equal(10, new VarIntZigZag(long.MinValue).EncodedSize);
    }

    [Fact]
    public void VarIntZigZag_TryParse_InsufficientData_ReturnsFalse()
    {
        // Arrange: Continuation bit set but no more data
        byte[] data = [0x80];

        // Act
        bool success = VarIntZigZag.TryParse(data, out VarIntZigZag value, out int bytesConsumed);

        // Assert
        Assert.False(success);
        Assert.Equal(0, bytesConsumed);
    }

    [Fact]
    public void VarIntZigZag_ToString_ReturnsDecimalString()
    {
        // Arrange
        VarIntZigZag varInt = new(-12345);

        // Act & Assert
        Assert.Equal("-12345", varInt.ToString());
    }

    [Fact]
    public void VarIntZigZag_ImplicitConversions_Work()
    {
        // Arrange & Act
        VarIntZigZag fromLong = -12345L;
        VarIntZigZag fromInt = -12345;
        VarIntZigZag fromShort = (short)-12345;
        VarIntZigZag fromSbyte = (sbyte)-123;

        // Assert
        Assert.Equal(-12345L, fromLong.Value);
        Assert.Equal(-12345L, fromInt.Value);
        Assert.Equal(-12345L, fromShort.Value);
        Assert.Equal(-123L, fromSbyte.Value);
    }

    [Fact]
    public void VarIntZigZag_RoundTrip_PreservesValue()
    {
        // Arrange
        long[] testValues = [0, 1, -1, 63, -64, 64, -65, 8191, -8192, 8192, -8193,
            1048575, -1048576, 1048576, -1048577, long.MaxValue, long.MinValue];
        byte[] buffer = new byte[VarIntZigZag.MaxSize];

        foreach (long expected in testValues)
        {
            VarIntZigZag original = new(expected);

            // Act - Write
            bool writeSuccess = original.TryFormat(buffer, out int bytesWritten, default, null);

            // Act - Read
            bool readSuccess = VarIntZigZag.TryParse(buffer[..bytesWritten], out VarIntZigZag parsed, out int bytesConsumed);

            // Assert
            Assert.True(writeSuccess, $"Failed to write {expected}");
            Assert.True(readSuccess, $"Failed to read {expected}");
            Assert.Equal(expected, parsed.Value);
            Assert.Equal(bytesWritten, bytesConsumed);
        }
    }
}

#endregion
