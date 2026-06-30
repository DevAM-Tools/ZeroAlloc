// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

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

#region Mixed Variable/Fixed Structs

/// <summary>
/// Variable-size struct: VarInt followed by two fixed fields.
/// Used to verify that TryParse returns false (not throws) when the buffer
/// is sufficient for the VarInt but not for the trailing fixed fields.
/// </summary>
[BinaryParsable]
public readonly partial struct VarIntThenFixedStruct
{
    public VarInt Count { get; init; }
    public U32BE Value { get; init; }
    public U16BE Flags { get; init; }
}

/// <summary>
/// Variable-size struct: VarInt-length-prefixed string followed by a fixed field.
/// Exercises the grouped bounds check emitted after the dynamic string member.
/// </summary>
[BinaryParsable]
public readonly partial struct StringThenFixedStruct
{
    [StringLengthVarInt]
    public string Label { get; init; }
    public U32BE Id { get; init; }
}

/// <summary>
/// Variable-size struct with a 2-byte big-endian length-prefixed string followed by a fixed field.
/// Exercises the length-prefix bounds check in GenerateStringParsing and the grouped check on Crc.
/// </summary>
[BinaryParsable]
public readonly partial struct FixedBEStringStruct
{
    [StringLengthBE(2)]
    public string Content { get; init; }
    public U16BE Crc { get; init; }
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
public sealed class BinaryParsableTests
{
    // ========================================================================
    // BASIC PARSING TESTS
    // ========================================================================
    #region Basic Parsing

    [Test]
    public async Task SimpleHeader_ParsesCorrectly()
    {
        // Arrange: Version=0x0100, Type=0x12345678, Length=0x00FF
        byte[] data = [0x01, 0x00, 0x12, 0x34, 0x56, 0x78, 0x00, 0xFF];

        // Act
        bool success = SimpleHeader.TryParse(data, out SimpleHeader header, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(8);
        await Assert.That((int)header.Version.Value).IsEqualTo(0x0100);
        await Assert.That(header.MessageType.Value).IsEqualTo(0x12345678u);
        await Assert.That((int)header.Length.Value).IsEqualTo(0x00FF);
    }

    [Test]
    public async Task SimpleHeader_FailsWithInsufficientData()
    {
        // Arrange: Only 5 bytes, but header needs 8
        byte[] data = [0x01, 0x00, 0x12, 0x34, 0x56];

        // Act
        bool success = SimpleHeader.TryParse(data, out SimpleHeader header, out int consumed);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task MixedEndian_ParsesCorrectly()
    {
        // Arrange: BE16=0x1234, LE16=0x5678 (stored as 78 56), BE32=0xAABBCCDD, LE32=0x11223344 (stored as 44 33 22 11)
        byte[] data = [0x12, 0x34, 0x78, 0x56, 0xAA, 0xBB, 0xCC, 0xDD, 0x44, 0x33, 0x22, 0x11];

        // Act
        bool success = MixedEndianStruct.TryParse(data, out MixedEndianStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(12);
        await Assert.That((int)result.BigEndian16.Value).IsEqualTo(0x1234);
        await Assert.That((int)result.LittleEndian16.Value).IsEqualTo(0x5678);
        await Assert.That(result.BigEndian32.Value).IsEqualTo(0xAABBCCDDu);
        await Assert.That(result.LittleEndian32.Value).IsEqualTo(0x11223344u);
    }

    [Test]
    public async Task ByteStruct_ParsesCorrectly()
    {
        // Arrange
        byte[] data = [0x11, 0x22, 0x33];

        // Act
        bool success = ByteStruct.TryParse(data, out ByteStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(3);
        await Assert.That((int)result.First).IsEqualTo(0x11);
        await Assert.That((int)result.Second).IsEqualTo(0x22);
        await Assert.That((int)result.Third).IsEqualTo(0x33);
    }

    [Test]
    public async Task StructWithIgnoredMember_ParsesOnlyRelevantFields()
    {
        // Arrange: Type=0x0010, Length=0x0100
        byte[] data = [0x00, 0x10, 0x01, 0x00];

        // Act
        bool success = StructWithIgnoredMember.TryParse(data, out StructWithIgnoredMember result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(4);
        await Assert.That((int)result.Type.Value).IsEqualTo(0x0010);
        await Assert.That((int)result.Length.Value).IsEqualTo(0x0100);
        await Assert.That(result.IsControl).IsTrue(); // 16 < 100
    }

    [Test]
    public async Task ReorderedStruct_ParsesInCorrectOrder()
    {
        // Arrange: Data is read as First(0), Second(1), Third(2)
        // So bytes: First=0x1111, Second=0x2222, Third=0x3333
        byte[] data = [0x11, 0x11, 0x22, 0x22, 0x33, 0x33];

        // Act
        bool success = ReorderedStruct.TryParse(data, out ReorderedStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(6);
        await Assert.That((int)result.First.Value).IsEqualTo(0x1111);
        await Assert.That((int)result.Second.Value).IsEqualTo(0x2222);
        await Assert.That((int)result.Third.Value).IsEqualTo(0x3333);
    }

    [Test]
    public async Task MacAddress_ParsesFixedLengthArray()
    {
        // Arrange: MAC address AA:BB:CC:DD:EE:FF
        byte[] data = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF];

        // Act
        bool success = MacAddress.TryParse(data, out MacAddress result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(6);
        await Assert.That(result.Address).IsEquivalentTo(data);
    }

    #endregion

    // ========================================================================
    // BITFIELD PARSING TESTS
    // ========================================================================

    #region BitField Parsing

    [Test]
    public async Task CANStandardHeader_ParsesCorrectly()
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
        bool success = CANStandardHeader.TryParse(data, out CANStandardHeader header, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(3);
        await Assert.That((int)header.Identifier).IsEqualTo(0x123);
        await Assert.That((int)header.RTR).IsEqualTo(1);
        await Assert.That((int)header.IDE).IsEqualTo(0);
        await Assert.That((int)header.Reserved).IsEqualTo(0);
        await Assert.That((int)header.DLC).IsEqualTo(8);
    }

    [Test]
    public async Task CANExtendedHeader_ParsesCorrectly()
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
        bool success = CANExtendedHeader.TryParse(data, out CANExtendedHeader header, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(4);
        await Assert.That(header.Identifier).IsEqualTo(0x1FFFFFFFu);
        await Assert.That((int)header.RTR).IsEqualTo(1);
        await Assert.That((int)header.IDE).IsEqualTo(1);
        await Assert.That((int)header.Reserved).IsEqualTo(0);
    }

    [Test]
    public async Task MixedBitFieldStruct_ParsesCorrectly()
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
        bool success = MixedBitFieldStruct.TryParse(data, out MixedBitFieldStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(3);
        await Assert.That((int)result.Field3Bit).IsEqualTo(5);
        await Assert.That((int)result.Field5Bit).IsEqualTo(17);
        await Assert.That((int)result.Field12Bit).IsEqualTo(2047);
        await Assert.That((int)result.Field4Bit).IsEqualTo(9);
    }

    [Test]
    public async Task BitFlagsStruct_ParsesAllFlags()
    {
        // Arrange: 8 x 1-bit = 8 bits = 1 byte
        // Flags: 1, 0, 1, 1, 0, 0, 1, 0 = 0b10110010 = 0xB2
        byte[] data = [0xB2];

        // Act
        bool success = BitFlagsStruct.TryParse(data, out BitFlagsStruct flags, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(1);
        await Assert.That((int)flags.Flag1).IsEqualTo(1);
        await Assert.That((int)flags.Flag2).IsEqualTo(0);
        await Assert.That((int)flags.Flag3).IsEqualTo(1);
        await Assert.That((int)flags.Flag4).IsEqualTo(1);
        await Assert.That((int)flags.Flag5).IsEqualTo(0);
        await Assert.That((int)flags.Flag6).IsEqualTo(0);
        await Assert.That((int)flags.Flag7).IsEqualTo(1);
        await Assert.That((int)flags.Flag8).IsEqualTo(0);
    }

    [Test]
    public async Task LargeBitFieldStruct_ParsesCorrectly()
    {
        // Arrange: 48-bit + 16-bit = 64 bits = 8 bytes
        // Field48Bit = 0xAABBCCDDEEFF (48 bits)
        // Field16Bit = 0x1234 (16 bits)
        byte[] data = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x12, 0x34];

        // Act
        bool success = LargeBitFieldStruct.TryParse(data, out LargeBitFieldStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(8);
        await Assert.That(result.Field48Bit).IsEqualTo(0xAABBCCDDEEFFuL);
        await Assert.That((int)result.Field16Bit).IsEqualTo(0x1234);
    }

    [Test]
    public async Task CANStandardHeader_FailsWithInsufficientData()
    {
        // Arrange: Only 2 bytes, but header needs 3 (18 bits)
        byte[] data = [0x00, 0x00];

        // Act
        bool success = CANStandardHeader.TryParse(data, out CANStandardHeader header, out int consumed);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task BitField_ZeroValues_ParsesCorrectly()
    {
        // Arrange: All zeros
        byte[] data = [0x00, 0x00, 0x00];

        // Act
        bool success = CANStandardHeader.TryParse(data, out CANStandardHeader header, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That((int)header.Identifier).IsEqualTo(0);
        await Assert.That((int)header.RTR).IsEqualTo(0);
        await Assert.That((int)header.IDE).IsEqualTo(0);
        await Assert.That((int)header.Reserved).IsEqualTo(0);
        await Assert.That((int)header.DLC).IsEqualTo(0);
    }

    [Test]
    public async Task BitField_MaxValues_ParsesCorrectly()
    {
        // Arrange: Maximum values for each field
        // ID = 0x7FF (11 bits max), RTR=1, IDE=1, Reserved=1, DLC=15 (4 bits max)
        // Bit stream: 11111111111_1_1_1_1111 = 18 bits
        // Byte 0: 11111111 = 0xFF
        // Byte 1: 11111111 = 0xFF
        // Byte 2: 11?????? = 0xC0 (with padding)

        byte[] data = [0xFF, 0xFF, 0xC0];

        // Act
        bool success = CANStandardHeader.TryParse(data, out CANStandardHeader header, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That((int)header.Identifier).IsEqualTo(0x7FF); // Max 11-bit value
        await Assert.That((int)header.RTR).IsEqualTo(1);
        await Assert.That((int)header.IDE).IsEqualTo(1);
        await Assert.That((int)header.Reserved).IsEqualTo(1);
        await Assert.That((int)header.DLC).IsEqualTo(15); // Max 4-bit value
    }

    #endregion

    // ========================================================================
    // EDGE CASES
    // ========================================================================

    #region Edge Cases

    [Test]
    public async Task SimpleHeader_EmptySpan_ReturnsFalse()
    {
        // Arrange
        ReadOnlySpan<byte> empty = [];

        // Act
        bool success = SimpleHeader.TryParse(empty, out SimpleHeader header, out int consumed);

        // Assert
        await Assert.That(success).IsFalse();
    }

    [Test]
    public async Task SimpleHeader_ExtraData_IgnoresExtraBytes()
    {
        // Arrange: Valid header + extra bytes
        byte[] data = [0x01, 0x00, 0x12, 0x34, 0x56, 0x78, 0x00, 0xFF, 0xDE, 0xAD, 0xBE, 0xEF];

        // Act
        bool success = SimpleHeader.TryParse(data, out SimpleHeader header, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(8); // Only consumed what was needed
    }

    #endregion

    // ========================================================================
    // PADDING BITS TESTS
    // ========================================================================

    #region PaddingBits Tests

    [Test]
    public async Task PaddingBitsStruct_ParsesCorrectlyWithPadding()
    {
        // Arrange: 4-bit flags + 4-bit padding (skipped after Flags) + 4-byte payload
        // Byte 0: Flags(4 bits) = 0xA, Padding(4 bits, skipped) = 0xB  => 0xAB
        // Bytes 1-4: Payload BE = 0x12345678
        byte[] data = [0xAB, 0x12, 0x34, 0x56, 0x78];

        // Act
        bool success = PaddingBitsTestStruct.TryParse(data, out PaddingBitsTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That((int)result.Flags).IsEqualTo(0xA);
        await Assert.That(result.Payload.Value).IsEqualTo(0x12345678u);
        await Assert.That(consumed).IsEqualTo(5);
    }

    #endregion

    // ========================================================================
    // PRIMITIVE INTEGER TESTS
    // ========================================================================

    #region Primitive Integer Tests

    [Test]
    public async Task PrimitiveIntegerStructBE_ParsesAllTypes()
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
        bool success = PrimitiveIntegerTestStructBE.TryParse(data, out PrimitiveIntegerTestStructBE result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That((int)result.Int16Value).IsEqualTo(100);
        await Assert.That((int)result.UInt16Value).IsEqualTo(200);
        await Assert.That(result.Int32Value).IsEqualTo(300);
        await Assert.That(result.UInt32Value).IsEqualTo(400u);
        await Assert.That(result.Int64Value).IsEqualTo(500L);
        await Assert.That(result.UInt64Value).IsEqualTo(600UL);
        await Assert.That(result.SingleValue).IsEqualTo(1200.0f);
        await Assert.That(result.DoubleValue).IsEqualTo(3200.0);
        await Assert.That((int)result.SByteValue).IsEqualTo(-2);
        await Assert.That(consumed).IsEqualTo(41);
    }

    [Test]
    public async Task PrimitiveIntegerStructLE_ParsesLittleEndian()
    {
        // Arrange: int (4) + float (4) in Little-Endian
        byte[] data =
        [
            0x2C, 0x01, 0x00, 0x00,                             // int: 300 (LE)
            0x00, 0x00, 0x96, 0x44                              // float: 1200.0f (LE)
        ];

        // Act
        bool success = PrimitiveIntegerTestStructLE.TryParse(data, out PrimitiveIntegerTestStructLE result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Int32Value).IsEqualTo(300);
        await Assert.That(result.SingleValue).IsEqualTo(1200.0f);
        await Assert.That(consumed).IsEqualTo(8);
    }

    [Test]
    public async Task PrimitiveIntegerStructBE_ReportsTryGetSerializedSizeCorrectly()
    {
        // Act
        bool hasFixedSize = PrimitiveIntegerTestStructBE.TryGetSerializedSize(out int size);

        // Assert
        await Assert.That(hasFixedSize).IsTrue();
        await Assert.That(size).IsEqualTo(41); // 2+2+4+4+8+8+4+8+1 = 41 bytes
    }

    #endregion

    // ========================================================================
    // STRING PARSING TESTS
    // ========================================================================

    #region String Parsing Tests

    [Test]
    public async Task StringVarIntStruct_ParsesVarIntPrefixedString()
    {
        // Arrange: VarInt length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringVarIntTestStruct.TryParse(data, out StringVarIntTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(6); // 1 byte VarInt + 5 bytes string
    }

    [Test]
    public async Task StringFixedBEStruct_ParsesBigEndianLengthPrefixedString()
    {
        // Arrange: 2-byte BE length (5) + "Hello" UTF-8
        byte[] data = [0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringFixedBE2TestStruct.TryParse(data, out StringFixedBE2TestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(7); // 2 bytes length + 5 bytes string
    }

    [Test]
    public async Task StringFixedLEStruct_ParsesLittleEndianLengthPrefixedString()
    {
        // Arrange: 2-byte LE length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x00, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringFixedLE2TestStruct.TryParse(data, out StringFixedLE2TestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(7); // 2 bytes length + 5 bytes string
    }

    [Test]
    public async Task StringNullTerminatedStruct_ParsesNullTerminatedString()
    {
        // Arrange: "Hello" + null byte
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00];

        // Act
        bool success = StringNullTerminatedTestStruct.TryParse(data, out StringNullTerminatedTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(6); // 5 bytes string + 1 null
    }

    [Test]
    public async Task StringFixedLengthStruct_ParsesFixedLengthString()
    {
        // Arrange: "Hello" padded to 8 bytes
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x00, 0x00];

        // Act
        bool success = StringFixedLengthTestStruct.TryParse(data, out StringFixedLengthTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(8); // Fixed 8 bytes
    }

    [Test]
    public async Task StringWithHeaderStruct_ParsesHeaderAndString()
    {
        // Arrange: 2-byte version BE + VarInt length (5) + "Hello"
        byte[] data = [0x01, 0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringWithHeaderTestStruct.TryParse(data, out StringWithHeaderTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That((int)result.Version.Value).IsEqualTo(0x0100);
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(8);
    }

    [Test]
    public async Task Utf8Var_ParsesVarIntPrefixedString()
    {
        // Arrange: VarInt length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = Utf8Var.TryParse(data, out Utf8Var result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Value).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(6);
    }

    [Test]
    public async Task Utf8FixBE_ParsesBigEndianLengthPrefixedString()
    {
        // Arrange: 4-byte BE length (5) + "Hello" UTF-8
        byte[] data = [0x00, 0x00, 0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = Utf8FixBE.TryParse(data, out Utf8FixBE result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Value).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(9);
    }

    [Test]
    public async Task Utf8Z_ParsesNullTerminatedString()
    {
        // Arrange: "Hello" + null byte
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00];

        // Act
        bool success = Utf8Z.TryParse(data, out Utf8Z result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Value).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(6);
    }

    [Test]
    public async Task StringWrapperInBinaryParsable_WorksWithGenerator()
    {
        // Arrange: 4-byte BE length (5) + "Hello" UTF-8
        byte[] data = [0x00, 0x00, 0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = Utf8FixBETestStruct.TryParse(data, out Utf8FixBETestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message.Value).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(9);
    }

    #endregion

    // ========================================================================
    // NEW STRING ATTRIBUTE TESTS
    // ========================================================================

    #region New String Attribute Tests

    [Test]
    public async Task StringVarIntAttr_ParsesVarIntPrefixedString()
    {
        // Arrange: VarInt length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringVarIntAttrTestStruct.TryParse(data, out StringVarIntAttrTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(6);
    }

    [Test]
    public async Task StringBE2Attr_ParsesBigEndianLengthPrefixedString()
    {
        // Arrange: 2-byte BE length (5) + "Hello" UTF-8
        byte[] data = [0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringBE2AttrTestStruct.TryParse(data, out StringBE2AttrTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(7);
    }

    [Test]
    public async Task StringLE2Attr_ParsesLittleEndianLengthPrefixedString()
    {
        // Arrange: 2-byte LE length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x00, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringLE2AttrTestStruct.TryParse(data, out StringLE2AttrTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(7);
    }

    [Test]
    public async Task StringNullTermAttr_ParsesNullTerminatedString()
    {
        // Arrange: "Hello" + null byte
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00];

        // Act
        bool success = StringNullTermAttrTestStruct.TryParse(data, out StringNullTermAttrTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(6);
    }

    [Test]
    public async Task StringFixedAttr_ParsesFixedLengthString()
    {
        // Arrange: "Hello" padded to 8 bytes
        byte[] data = [0x48, 0x65, 0x6C, 0x6C, 0x6F, 0x00, 0x00, 0x00];

        // Act
        bool success = StringFixedAttrTestStruct.TryParse(data, out StringFixedAttrTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(8);
    }

    [Test]
    public async Task StringFromField_ParsesStringWithLengthFromField()
    {
        // Arrange: 1-byte length (5) + "Hello" UTF-8
        byte[] data = [0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringFromFieldTestStruct.TryParse(data, out StringFromFieldTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That((int)result.NameLength).IsEqualTo(5);
        await Assert.That(result.Name).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(6);
    }

    [Test]
    public async Task StringFromFieldBE_ParsesStringWithLengthFromBEField()
    {
        // Arrange: 2-byte BE length (5) + "Hello" UTF-8
        byte[] data = [0x00, 0x05, 0x48, 0x65, 0x6C, 0x6C, 0x6F];

        // Act
        bool success = StringFromFieldBETestStruct.TryParse(data, out StringFromFieldBETestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That((int)result.MessageLength.Value).IsEqualTo(5);
        await Assert.That(result.Message).IsEqualTo("Hello");
        await Assert.That(consumed).IsEqualTo(7);
    }

    #endregion

    // ========================================================================
    // BYTE ARRAY ATTRIBUTE TESTS
    // ========================================================================

    #region Byte Array Attribute Tests

    [Test]
    public async Task ByteArrayVarInt_ParsesVarIntPrefixedArray()
    {
        // Arrange: VarInt length (4) + data bytes
        byte[] data = [0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = ByteArrayVarIntTestStruct.TryParse(data, out ByteArrayVarIntTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Data).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC, 0xDD]);
        await Assert.That(consumed).IsEqualTo(5);
    }

    [Test]
    public async Task ByteArrayBE2_ParsesBigEndianLengthPrefixedArray()
    {
        // Arrange: 2-byte BE length (4) + data bytes
        byte[] data = [0x00, 0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = ByteArrayBE2TestStruct.TryParse(data, out ByteArrayBE2TestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Data).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC, 0xDD]);
        await Assert.That(consumed).IsEqualTo(6);
    }

    [Test]
    public async Task ByteArrayLE4_ParsesLittleEndianLengthPrefixedArray()
    {
        // Arrange: 4-byte LE length (4) + data bytes
        byte[] data = [0x04, 0x00, 0x00, 0x00, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = ByteArrayLE4TestStruct.TryParse(data, out ByteArrayLE4TestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Data).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC, 0xDD]);
        await Assert.That(consumed).IsEqualTo(8);
    }

    [Test]
    public async Task ByteArrayFromField_ParsesArrayWithLengthFromField()
    {
        // Arrange: 2-byte BE length (4) + data bytes
        byte[] data = [0x00, 0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = ByteArrayFromFieldTestStruct.TryParse(data, out ByteArrayFromFieldTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That((int)result.DataLength.Value).IsEqualTo(4);
        await Assert.That(result.Data).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC, 0xDD]);
        await Assert.That(consumed).IsEqualTo(6);
    }

    [Test]
    public async Task MemoryVarInt_ParsesVarIntPrefixedMemory()
    {
        // Arrange: VarInt length (4) + data bytes
        byte[] data = [0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = MemoryVarIntTestStruct.TryParse(data, out MemoryVarIntTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Data.Length).IsEqualTo(4);
        await Assert.That(result.Data.ToArray()).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC, 0xDD]);
        await Assert.That(consumed).IsEqualTo(5);
    }

    [Test]
    public async Task ReadOnlyMemoryBE2_ParsesBigEndianLengthPrefixedMemory()
    {
        // Arrange: 2-byte BE length (4) + data bytes
        byte[] data = [0x00, 0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = ReadOnlyMemoryBE2TestStruct.TryParse(data, out ReadOnlyMemoryBE2TestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(result.Data.Length).IsEqualTo(4);
        await Assert.That(result.Data.ToArray()).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC, 0xDD]);
        await Assert.That(consumed).IsEqualTo(6);
    }

    [Test]
    public async Task MemoryFromField_ParsesMemoryWithLengthFromField()
    {
        // Arrange: 1-byte length (4) + data bytes
        byte[] data = [0x04, 0xAA, 0xBB, 0xCC, 0xDD];

        // Act
        bool success = MemoryFromFieldTestStruct.TryParse(data, out MemoryFromFieldTestStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That((int)result.DataSize).IsEqualTo(4);
        await Assert.That(result.Data.Length).IsEqualTo(4);
        await Assert.That(result.Data.ToArray()).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC, 0xDD]);
        await Assert.That(consumed).IsEqualTo(5);
    }

    #endregion

    // ========================================================================
    // MIXED VARIABLE/FIXED PARSING TESTS
    // ========================================================================
    #region Mixed Variable/Fixed Parsing

    [Test]
    public async Task VarIntThenFixedStruct_ParsesCorrectly()
    {
        // Arrange: VarInt=42 (single byte 0x2A), U32BE=0x01020304, U16BE=0xABCD
        byte[] data = [0x2A, 0x01, 0x02, 0x03, 0x04, 0xAB, 0xCD];

        // Act
        bool success = VarIntThenFixedStruct.TryParse(data, out VarIntThenFixedStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(7);
        await Assert.That(result.Count.Value).IsEqualTo(42UL);
        await Assert.That(result.Value.Value).IsEqualTo(0x01020304u);
        await Assert.That((int)result.Flags.Value).IsEqualTo(0xABCD);
    }

    [Test]
    public async Task StringThenFixedStruct_ParsesCorrectly()
    {
        // Arrange: VarInt-length=2, "hi" (0x68 0x69), U32BE=0xDEADBEEF
        byte[] data = [0x02, 0x68, 0x69, 0xDE, 0xAD, 0xBE, 0xEF];

        // Act
        bool success = StringThenFixedStruct.TryParse(data, out StringThenFixedStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(7);
        await Assert.That(result.Label).IsEqualTo("hi");
        await Assert.That(result.Id.Value).IsEqualTo(0xDEADBEEFu);
    }

    [Test]
    public async Task FixedBEStringStruct_ParsesCorrectly()
    {
        // Arrange: 2-byte BE length=3, "abc" (0x61 0x62 0x63), U16BE=0x1234
        byte[] data = [0x00, 0x03, 0x61, 0x62, 0x63, 0x12, 0x34];

        // Act
        bool success = FixedBEStringStruct.TryParse(data, out FixedBEStringStruct result, out int consumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(consumed).IsEqualTo(7);
        await Assert.That(result.Content).IsEqualTo("abc");
        await Assert.That((int)result.Crc.Value).IsEqualTo(0x1234);
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
public sealed class VarIntTests
{
    [Test]
    [Arguments(0UL, new byte[] { 0x00 })]
    [Arguments(127UL, new byte[] { 0x7F })]
    [Arguments(128UL, new byte[] { 0x80, 0x01 })]
    [Arguments(300UL, new byte[] { 0xAC, 0x02 })]
    [Arguments(16383UL, new byte[] { 0xFF, 0x7F })]
    [Arguments(16384UL, new byte[] { 0x80, 0x80, 0x01 })]
    [Arguments(2097151UL, new byte[] { 0xFF, 0xFF, 0x7F })]                       // 3-byte max (2^21 - 1)
    [Arguments(2097152UL, new byte[] { 0x80, 0x80, 0x80, 0x01 })]                 // 4-byte min (2^21)
    [Arguments(268435455UL, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F })]               // 4-byte max (2^28 - 1)
    [Arguments(268435456UL, new byte[] { 0x80, 0x80, 0x80, 0x80, 0x01 })]         // 5-byte min (2^28)
    [Arguments(34359738367UL, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x7F })]       // 5-byte max (2^35 - 1)
    public async Task VarInt_TryParse_ParsesCorrectly(ulong expected, byte[] data)
    {
        // Act
        bool success = VarInt.TryParse(data, out VarInt value, out int bytesConsumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(value.Value).IsEqualTo(expected);
        await Assert.That(bytesConsumed).IsEqualTo(data.Length);
    }

    [Test]
    public async Task VarInt_TryFormat_WritesCorrectly()
    {
        // Arrange
        VarInt varInt = new(300);
        bool success;
        int bytesWritten;
        byte byte0, byte1;
        {
            Span<byte> buffer = stackalloc byte[10];

            // Act
            success = varInt.TryFormat(buffer, out bytesWritten, default, null);
            byte0 = buffer[0];
            byte1 = buffer[1];
        }

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(2);
        await Assert.That((int)byte0).IsEqualTo(0xAC);
        await Assert.That((int)byte1).IsEqualTo(0x02);
    }

    [Test]
    public async Task VarInt_EncodedSize_ReturnsCorrectSize()
    {
        // Arrange & Act & Assert
        await Assert.That(new VarInt(0).EncodedSize).IsEqualTo(1);
        await Assert.That(new VarInt(127).EncodedSize).IsEqualTo(1);              // 1-byte max
        await Assert.That(new VarInt(128).EncodedSize).IsEqualTo(2);              // 2-byte min
        await Assert.That(new VarInt(16383).EncodedSize).IsEqualTo(2);            // 2-byte max
        await Assert.That(new VarInt(16384).EncodedSize).IsEqualTo(3);            // 3-byte min
        await Assert.That(new VarInt(2097151).EncodedSize).IsEqualTo(3);          // 3-byte max (2^21 - 1)
        await Assert.That(new VarInt(2097152).EncodedSize).IsEqualTo(4);          // 4-byte min (2^21)
        await Assert.That(new VarInt(268435455).EncodedSize).IsEqualTo(4);        // 4-byte max (2^28 - 1)
        await Assert.That(new VarInt(268435456).EncodedSize).IsEqualTo(5);        // 5-byte min (2^28)
        await Assert.That(new VarInt(34359738367).EncodedSize).IsEqualTo(5);      // 5-byte max (2^35 - 1)
        await Assert.That(new VarInt(ulong.MaxValue).EncodedSize).IsEqualTo(10);  // maximum: 10 bytes
    }

    /// <summary>Verifies VarInt roundtrip for large / extreme values including ulong.MaxValue.</summary>
    [Test]
    public async Task VarInt_RoundTrip_ExtremeValues()
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
            await Assert.That(writeSuccess).IsTrue().Because($"Failed to write {expected}");

            // Read
            bool readSuccess = VarInt.TryParse(buffer[..bytesWritten], out VarInt parsed, out int bytesConsumed);
            await Assert.That(readSuccess).IsTrue().Because($"Failed to read {expected}");
            await Assert.That(parsed.Value).IsEqualTo(expected);
            await Assert.That(bytesConsumed).IsEqualTo(bytesWritten);
        }
    }

    [Test]
    public async Task VarInt_TryParse_InsufficientData_ReturnsFalse()
    {
        // Arrange: Continuation bit set but no more data
        byte[] data = [0x80];

        // Act
        bool success = VarInt.TryParse(data, out VarInt value, out int bytesConsumed);

        // Assert
        await Assert.That(success).IsFalse();
        await Assert.That(bytesConsumed).IsEqualTo(0);
    }

    [Test]
    public async Task VarInt_TryGetWrittenSize_ReturnsEncodedSize()
    {
        VarInt varInt0 = new(0);
        VarInt varInt127 = new(127);
        VarInt varInt128 = new(128);
        VarInt varInt16383 = new(16383);

        await Assert.That(varInt0.TryGetWrittenSize(out int size0)).IsTrue();
        await Assert.That(size0).IsEqualTo(1);

        await Assert.That(varInt127.TryGetWrittenSize(out int size127)).IsTrue();
        await Assert.That(size127).IsEqualTo(1);

        await Assert.That(varInt128.TryGetWrittenSize(out int size128)).IsTrue();
        await Assert.That(size128).IsEqualTo(2);

        await Assert.That(varInt16383.TryGetWrittenSize(out int size16383)).IsTrue();
        await Assert.That(size16383).IsEqualTo(2);
    }

    [Test]
    public async Task VarInt_ToString_ReturnsDecimalString()
    {
        // Arrange
        VarInt varInt = new(12345);

        // Act & Assert
        await Assert.That(varInt.ToString()).IsEqualTo("12345");
    }

    [Test]
    public async Task VarInt_ImplicitConversions_Work()
    {
        // Arrange & Act
        VarInt fromUlong = 12345UL;
        VarInt fromUint = 12345U;
        VarInt fromUshort = (ushort)12345;
        VarInt fromByte = (byte)123;

        // Assert
        await Assert.That(fromUlong.Value).IsEqualTo(12345UL);
        await Assert.That(fromUint.Value).IsEqualTo(12345UL);
        await Assert.That(fromUshort.Value).IsEqualTo(12345UL);
        await Assert.That(fromByte.Value).IsEqualTo(123UL);
    }
}

/// <summary>
/// Tests for VarIntZigZag IBinaryParsable implementation.
/// </summary>
public sealed class VarIntZigZagTests
{
    [Test]
    [Arguments(0L, new byte[] { 0x00 })]
    [Arguments(-1L, new byte[] { 0x01 })]
    [Arguments(1L, new byte[] { 0x02 })]
    [Arguments(-2L, new byte[] { 0x03 })]
    [Arguments(2L, new byte[] { 0x04 })]
    [Arguments(-64L, new byte[] { 0x7F })]
    [Arguments(64L, new byte[] { 0x80, 0x01 })]
    public async Task VarIntZigZag_TryParse_ParsesCorrectly(long expected, byte[] data)
    {
        // Act
        bool success = VarIntZigZag.TryParse(data, out VarIntZigZag value, out int bytesConsumed);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(value.Value).IsEqualTo(expected);
        await Assert.That(bytesConsumed).IsEqualTo(data.Length);
    }

    [Test]
    public async Task VarIntZigZag_TryFormat_WritesCorrectly()
    {
        // Arrange
        VarIntZigZag varInt = new(-1);
        bool success;
        int bytesWritten;
        byte byte0;
        {
            Span<byte> buffer = stackalloc byte[10];

            // Act
            success = varInt.TryFormat(buffer, out bytesWritten, default, null);
            byte0 = buffer[0];
        }

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(1);
        await Assert.That((int)byte0).IsEqualTo(0x01);
    }

    [Test]
    public async Task VarIntZigZag_ZigZagEncoded_ReturnsCorrectValue()
    {
        // ZigZag encoding: 0 → 0, -1 → 1, 1 → 2, -2 → 3, 2 → 4
        await Assert.That(new VarIntZigZag(0).ZigZagEncoded).IsEqualTo(0UL);
        await Assert.That(new VarIntZigZag(-1).ZigZagEncoded).IsEqualTo(1UL);
        await Assert.That(new VarIntZigZag(1).ZigZagEncoded).IsEqualTo(2UL);
        await Assert.That(new VarIntZigZag(-2).ZigZagEncoded).IsEqualTo(3UL);
        await Assert.That(new VarIntZigZag(2).ZigZagEncoded).IsEqualTo(4UL);
    }

    [Test]
    public async Task VarIntZigZag_DecodeZigZag_DecodesCorrectly()
    {
        // Arrange & Act & Assert
        await Assert.That(VarIntZigZag.DecodeZigZag(0)).IsEqualTo(0L);
        await Assert.That(VarIntZigZag.DecodeZigZag(1)).IsEqualTo(-1L);
        await Assert.That(VarIntZigZag.DecodeZigZag(2)).IsEqualTo(1L);
        await Assert.That(VarIntZigZag.DecodeZigZag(3)).IsEqualTo(-2L);
        await Assert.That(VarIntZigZag.DecodeZigZag(4)).IsEqualTo(2L);
    }

    [Test]
    public async Task VarIntZigZag_EncodedSize_ReturnsCorrectSize()
    {
        // Arrange & Act & Assert
        await Assert.That(new VarIntZigZag(0).EncodedSize).IsEqualTo(1);
        await Assert.That(new VarIntZigZag(-1).EncodedSize).IsEqualTo(1);
        await Assert.That(new VarIntZigZag(-64).EncodedSize).IsEqualTo(1);         // 1-byte max negative
        await Assert.That(new VarIntZigZag(63).EncodedSize).IsEqualTo(1);          // 1-byte max positive
        await Assert.That(new VarIntZigZag(64).EncodedSize).IsEqualTo(2);          // 2-byte min
        await Assert.That(new VarIntZigZag(-65).EncodedSize).IsEqualTo(2);         // 2-byte min negative
        await Assert.That(new VarIntZigZag(8191).EncodedSize).IsEqualTo(2);        // 2-byte max positive
        await Assert.That(new VarIntZigZag(-8192).EncodedSize).IsEqualTo(2);       // 2-byte max negative
        await Assert.That(new VarIntZigZag(8192).EncodedSize).IsEqualTo(3);        // 3-byte min
        await Assert.That(new VarIntZigZag(-8193).EncodedSize).IsEqualTo(3);       // 3-byte min negative
        await Assert.That(new VarIntZigZag(1048575).EncodedSize).IsEqualTo(3);     // 3-byte max positive
        await Assert.That(new VarIntZigZag(-1048576).EncodedSize).IsEqualTo(3);    // 3-byte max negative
        await Assert.That(new VarIntZigZag(1048576).EncodedSize).IsEqualTo(4);     // 4-byte min
        await Assert.That(new VarIntZigZag(-1048577).EncodedSize).IsEqualTo(4);    // 4-byte min negative
        await Assert.That(new VarIntZigZag(long.MaxValue).EncodedSize).IsEqualTo(10);
        await Assert.That(new VarIntZigZag(long.MinValue).EncodedSize).IsEqualTo(10);
    }

    [Test]
    public async Task VarIntZigZag_TryParse_InsufficientData_ReturnsFalse()
    {
        // Arrange: Continuation bit set but no more data
        byte[] data = [0x80];

        // Act
        bool success = VarIntZigZag.TryParse(data, out VarIntZigZag value, out int bytesConsumed);

        // Assert
        await Assert.That(success).IsFalse();
        await Assert.That(bytesConsumed).IsEqualTo(0);
    }

    [Test]
    public async Task VarIntZigZag_ToString_ReturnsDecimalString()
    {
        // Arrange
        VarIntZigZag varInt = new(-12345);

        // Act & Assert
        await Assert.That(varInt.ToString()).IsEqualTo("-12345");
    }

    [Test]
    public async Task VarIntZigZag_ImplicitConversions_Work()
    {
        // Arrange & Act
        VarIntZigZag fromLong = -12345L;
        VarIntZigZag fromInt = -12345;
        VarIntZigZag fromShort = (short)-12345;
        VarIntZigZag fromSbyte = (sbyte)-123;

        // Assert
        await Assert.That(fromLong.Value).IsEqualTo(-12345L);
        await Assert.That(fromInt.Value).IsEqualTo(-12345L);
        await Assert.That(fromShort.Value).IsEqualTo(-12345L);
        await Assert.That(fromSbyte.Value).IsEqualTo(-123L);
    }

    [Test]
    public async Task VarIntZigZag_RoundTrip_PreservesValue()
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
            await Assert.That(writeSuccess).IsTrue().Because($"Failed to write {expected}");
            await Assert.That(readSuccess).IsTrue().Because($"Failed to read {expected}");
            await Assert.That(parsed.Value).IsEqualTo(expected);
            await Assert.That(bytesConsumed).IsEqualTo(bytesWritten);
        }
    }
}

#endregion
