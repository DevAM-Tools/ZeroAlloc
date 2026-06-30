// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// BinaryWritable Generator Tests
// ============================================================================
// Tests for the [BinaryWritable] source generator, including:
// - Basic struct writing
// - Endian wrapper support
// - BitField support
// - Nested types
// - Roundtrip (parse→write→compare)
// ============================================================================

namespace ZeroAlloc.Tests;

// ============================================================================
// TEST STRUCTS
// ============================================================================

#region Basic Writing Structs

/// <summary>
/// Simple struct with endian wrappers for writing.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableSimpleHeader
{
    /// <summary>Version field.</summary>
    public U16BE Version { get; init; }

    /// <summary>Message type field.</summary>
    public U32BE MessageType { get; init; }

    /// <summary>Length field.</summary>
    public U16BE Length { get; init; }
}

/// <summary>
/// Struct with mixed endianness for writing.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableMixedEndian
{
    /// <summary>Big-endian 16-bit.</summary>
    public U16BE BigEndian16 { get; init; }

    /// <summary>Little-endian 16-bit.</summary>
    public U16LE LittleEndian16 { get; init; }

    /// <summary>Big-endian 32-bit.</summary>
    public U32BE BigEndian32 { get; init; }

    /// <summary>Little-endian 32-bit.</summary>
    public U32LE LittleEndian32 { get; init; }
}

/// <summary>
/// Struct with byte member for writing.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableByteStruct
{
    /// <summary>First byte.</summary>
    public byte First { get; init; }

    /// <summary>Second byte.</summary>
    public byte Second { get; init; }

    /// <summary>Third byte.</summary>
    public byte Third { get; init; }
}

/// <summary>
/// Struct with [BinaryIgnore] for writing.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableWithIgnored
{
    /// <summary>Type field.</summary>
    public U16BE Type { get; init; }

    /// <summary>Length field.</summary>
    public U16BE Length { get; init; }

    /// <summary>Computed property - ignored during serialization.</summary>
    [BinaryIgnore]
    public bool IsControl => Type.Value < 100;
}

/// <summary>
/// Struct with explicit ordering for writing.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableReordered
{
    /// <summary>Written third.</summary>
    [BinaryOrder(2)]
    public U16BE Third { get; init; }

    /// <summary>Written first.</summary>
    [BinaryOrder(0)]
    public U16BE First { get; init; }

    /// <summary>Written second.</summary>
    [BinaryOrder(1)]
    public U16BE Second { get; init; }
}

/// <summary>
/// Struct with fixed-length byte array.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableMacAddress
{
    /// <summary>6-byte MAC address.</summary>
    [BinaryFixedLength(6)]
    public byte[] Address { get; init; }
}

#endregion

#region Mixed Variable/Fixed Writing Structs

/// <summary>
/// Variable-size writable struct: VarInt followed by two fixed fields.
/// Used to verify TryWrite returns false (not throws) on undersized destination.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableVarIntThenFixed
{
    /// <summary>Variable-length count.</summary>
    public VarInt Count { get; init; }

    /// <summary>Fixed 32-bit big-endian value.</summary>
    public U32BE Value { get; init; }

    /// <summary>Fixed 16-bit big-endian flags.</summary>
    public U16BE Flags { get; init; }
}

/// <summary>
/// Variable-size writable struct: VarInt-length string followed by a fixed field.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableStringThenFixed
{
    /// <summary>Variable-length UTF-8 label.</summary>
    [StringLengthVarInt]
    public string Label { get; init; }

    /// <summary>Fixed 32-bit big-endian identifier.</summary>
    public U32BE Id { get; init; }
}

/// <summary>
/// Variable-size writable struct with a 2-byte big-endian length-prefixed string.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableFixedBEString
{
    /// <summary>Variable-length UTF-8 content.</summary>
    [StringLengthBE(2)]
    public string Content { get; init; }

    /// <summary>Fixed 16-bit big-endian CRC.</summary>
    public U16BE Crc { get; init; }
}

/// <summary>
/// Variable-size writable struct with a fixed-width (no length prefix) string slot.
/// Used to verify that TryWrite returns false (not throws) when the destination is
/// too short to hold the fixed-length field, and that a string longer than the slot
/// also returns false rather than throwing.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableFixedLengthString
{
    /// <summary>Fixed 8-byte UTF-8 string slot; shorter strings are null-padded.</summary>
    [StringFixedLength(8)]
    public string Name { get; init; }
}

/// <summary>
/// Variable-size writable struct with a null-terminated string.
/// Used to verify that TryWrite returns false when the destination cannot fit
/// the string bytes plus the null terminator.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableNullTermString
{
    /// <summary>Null-terminated UTF-8 string.</summary>
    [StringNullTerminated]
    public string Label { get; init; }
}

/// <summary>
/// Variable-size writable struct: 2-byte BE length field followed by a FromField string.
/// The caller is responsible for setting NameLength to the UTF-8 byte count of Name.
/// Used to verify that TryWrite returns false when the destination cannot hold the string data.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableFromFieldString
{
    /// <summary>UTF-8 byte count of the Name field.</summary>
    public U16BE NameLength { get; init; }

    /// <summary>UTF-8 string data; length is determined by NameLength at parse time.</summary>
    [StringLengthFromField("NameLength")]
    public string Name { get; init; }
}

/// <summary>
/// Variable-size writable struct: 2-byte BE length field followed by a FromField byte array.
/// Used to verify that TryWrite returns false when the destination cannot hold the byte data.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableFromFieldBytes
{
    /// <summary>Byte count of the Data field.</summary>
    public U16BE DataLength { get; init; }

    /// <summary>Raw byte data; length is determined by DataLength at parse time.</summary>
    [BytesLengthFromField("DataLength")]
    public byte[] Data { get; init; }
}

#endregion

#region BitField Writing Structs

/// <summary>
/// CAN standard header for bit-level writing.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableCANHeader
{
    /// <summary>11-bit identifier.</summary>
    [BinaryField(BitCount = 11)]
    public ushort Identifier { get; init; }

    /// <summary>RTR bit.</summary>
    [BinaryField(BitCount = 1)]
    public byte RTR { get; init; }

    /// <summary>IDE bit.</summary>
    [BinaryField(BitCount = 1)]
    public byte IDE { get; init; }

    /// <summary>Reserved bit.</summary>
    [BinaryField(BitCount = 1)]
    public byte Reserved { get; init; }

    /// <summary>4-bit data length code.</summary>
    [BinaryField(BitCount = 4)]
    public byte DLC { get; init; }
}

/// <summary>
/// Simple mixed bit field struct.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableMixedBitField
{
    /// <summary>3-bit field.</summary>
    [BinaryField(BitCount = 3)]
    public byte Field3Bit { get; init; }

    /// <summary>5-bit field.</summary>
    [BinaryField(BitCount = 5)]
    public byte Field5Bit { get; init; }

    /// <summary>12-bit field.</summary>
    [BinaryField(BitCount = 12)]
    public ushort Field12Bit { get; init; }

    /// <summary>4-bit field.</summary>
    [BinaryField(BitCount = 4)]
    public byte Field4Bit { get; init; }
}

/// <summary>
/// Single-bit flags struct.
/// </summary>
[BinaryWritable]
public readonly partial struct WritableBitFlags
{
    /// <summary>Flag 1.</summary>
    [BinaryField(BitCount = 1)]
    public byte Flag1 { get; init; }

    /// <summary>Flag 2.</summary>
    [BinaryField(BitCount = 1)]
    public byte Flag2 { get; init; }

    /// <summary>Flag 3.</summary>
    [BinaryField(BitCount = 1)]
    public byte Flag3 { get; init; }

    /// <summary>Flag 4.</summary>
    [BinaryField(BitCount = 1)]
    public byte Flag4 { get; init; }

    /// <summary>Flag 5.</summary>
    [BinaryField(BitCount = 1)]
    public byte Flag5 { get; init; }

    /// <summary>Flag 6.</summary>
    [BinaryField(BitCount = 1)]
    public byte Flag6 { get; init; }

    /// <summary>Flag 7.</summary>
    [BinaryField(BitCount = 1)]
    public byte Flag7 { get; init; }

    /// <summary>Flag 8.</summary>
    [BinaryField(BitCount = 1)]
    public byte Flag8 { get; init; }
}

#endregion

#region Roundtrip Structs (BinaryParsable + BinaryWritable)

/// <summary>
/// Struct with both parsing and writing for roundtrip tests.
/// </summary>
[BinaryParsable]
[BinaryWritable]
public readonly partial struct RoundtripHeader
{
    /// <summary>Version.</summary>
    public U16BE Version { get; init; }

    /// <summary>Flags.</summary>
    public U32BE Flags { get; init; }

    /// <summary>Sequence.</summary>
    public U16BE Sequence { get; init; }
}

/// <summary>
/// Roundtrip struct with byte member.
/// </summary>
[BinaryParsable]
[BinaryWritable]
public readonly partial struct RoundtripWithByte
{
    /// <summary>Type field.</summary>
    public byte Type { get; init; }

    /// <summary>Payload length.</summary>
    public U16BE PayloadLength { get; init; }

    /// <summary>Checksum.</summary>
    public byte Checksum { get; init; }
}

/// <summary>
/// Roundtrip struct with fixed-length byte array.
/// </summary>
[BinaryParsable]
[BinaryWritable]
public readonly partial struct RoundtripMacAddress
{
    /// <summary>MAC address bytes.</summary>
    [BinaryFixedLength(6)]
    public byte[] Address { get; init; }

    /// <summary>EtherType.</summary>
    public U16BE EtherType { get; init; }
}

/// <summary>
/// Roundtrip bit field struct.
/// </summary>
[BinaryParsable]
[BinaryWritable]
public readonly partial struct RoundtripBitField
{
    /// <summary>3-bit field.</summary>
    [BinaryField(BitCount = 3)]
    public byte Field3Bit { get; init; }

    /// <summary>5-bit field.</summary>
    [BinaryField(BitCount = 5)]
    public byte Field5Bit { get; init; }

    /// <summary>16-bit field.</summary>
    [BinaryField(BitCount = 16)]
    public ushort Field16Bit { get; init; }
}

#endregion

// ============================================================================
// TEST CLASSES
// ============================================================================

/// <summary>
/// Tests for [BinaryWritable] generated serialization methods.
/// </summary>
public sealed class BinaryWritableTests
{
    // ========================================================================
    // BASIC WRITING TESTS
    // ========================================================================
    #region Basic Writing

    /// <summary>
    /// Tests writing a simple header with endian wrappers.
    /// </summary>
    [Test]
    public async Task SimpleHeader_WritesCorrectly()
    {
        // Arrange
        WritableSimpleHeader header = new()
        {
            Version = new U16BE(0x0100),
            MessageType = new U32BE(0x12345678),
            Length = new U16BE(0x00FF)
        };
        byte[] buffer = new byte[8];

        // Act
        bool success = header.TryWrite(buffer, out int written);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(8);
        await Assert.That(buffer).IsEquivalentTo((byte[])[0x01, 0x00, 0x12, 0x34, 0x56, 0x78, 0x00, 0xFF]);
    }

    /// <summary>
    /// Tests writing fails with insufficient buffer.
    /// </summary>
    [Test]
    public async Task SimpleHeader_FailsWithInsufficientBuffer()
    {
        WritableSimpleHeader header = new()
        {
            Version = new U16BE(1),
            MessageType = new U32BE(2),
            Length = new U16BE(3)
        };
        byte[] buffer = new byte[5]; // Too small

        bool success = header.TryWrite(buffer, out int written);

        await Assert.That(success).IsFalse();
        await Assert.That(written).IsEqualTo(0);
    }

    /// <summary>
    /// Tests writing with mixed endianness.
    /// </summary>
    [Test]
    public async Task MixedEndian_WritesCorrectly()
    {
        WritableMixedEndian value = new()
        {
            BigEndian16 = new U16BE(0x1234),
            LittleEndian16 = new U16LE(0x5678),
            BigEndian32 = new U32BE(0xAABBCCDD),
            LittleEndian32 = new U32LE(0x11223344)
        };
        byte[] buffer = new byte[12];

        bool success = value.TryWrite(buffer, out int written);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(12);
        // BE16=0x1234 → 12 34
        // LE16=0x5678 → 78 56
        // BE32=0xAABBCCDD → AA BB CC DD
        // LE32=0x11223344 → 44 33 22 11
        await Assert.That(buffer).IsEquivalentTo((byte[])[0x12, 0x34, 0x78, 0x56, 0xAA, 0xBB, 0xCC, 0xDD, 0x44, 0x33, 0x22, 0x11]);
    }

    /// <summary>
    /// Tests writing byte members.
    /// </summary>
    [Test]
    public async Task ByteStruct_WritesCorrectly()
    {
        WritableByteStruct value = new() { First = 0xAA, Second = 0xBB, Third = 0xCC };
        byte[] buffer = new byte[3];

        bool success = value.TryWrite(buffer, out int written);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(3);
        await Assert.That(buffer).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC]);
    }

    /// <summary>
    /// Tests that ignored members are not written.
    /// </summary>
    [Test]
    public async Task IgnoredMember_NotWritten()
    {
        WritableWithIgnored value = new()
        {
            Type = new U16BE(42),
            Length = new U16BE(100)
        };
        byte[] buffer = new byte[4];

        bool success = value.TryWrite(buffer, out int written);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(4); // Only Type + Length, not IsControl
        await Assert.That(buffer).IsEquivalentTo((byte[])[0x00, 42, 0x00, 100]);
    }

    /// <summary>
    /// Tests explicit member ordering.
    /// </summary>
    [Test]
    public async Task ReorderedStruct_WritesInSpecifiedOrder()
    {
        WritableReordered value = new()
        {
            Third = new U16BE(0x0003),
            First = new U16BE(0x0001),
            Second = new U16BE(0x0002)
        };
        byte[] buffer = new byte[6];

        bool success = value.TryWrite(buffer, out int written);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(6);
        // Written in order: First(0x0001), Second(0x0002), Third(0x0003)
        await Assert.That(buffer).IsEquivalentTo((byte[])[0x00, 0x01, 0x00, 0x02, 0x00, 0x03]);
    }

    /// <summary>
    /// Tests writing fixed-length byte array.
    /// </summary>
    [Test]
    public async Task FixedByteArray_WritesCorrectly()
    {
        WritableMacAddress mac = new()
        {
            Address = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF]
        };
        byte[] buffer = new byte[6];

        bool success = mac.TryWrite(buffer, out int written);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(6);
        await Assert.That(buffer).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF]);
    }

    #endregion

    // ========================================================================
    // SIZE CALCULATION TESTS
    // ========================================================================

    #region TryGetWrittenSize

    /// <summary>
    /// Tests fixed-size type returns correct size.
    /// </summary>
    [Test]
    public async Task TryGetWrittenSize_FixedSize_ReturnsCorrectSize()
    {
        WritableSimpleHeader header = new()
        {
            Version = new U16BE(1),
            MessageType = new U32BE(2),
            Length = new U16BE(3)
        };

        bool success = ((IBinarySerializable)header).TryGetWrittenSize(out int size);

        await Assert.That(success).IsTrue();
        await Assert.That(size).IsEqualTo(8); // 2 + 4 + 2
    }

    /// <summary>
    /// Tests byte struct size calculation.
    /// </summary>
    [Test]
    public async Task TryGetWrittenSize_ByteStruct_ReturnsCorrectSize()
    {
        WritableByteStruct value = new() { First = 1, Second = 2, Third = 3 };

        bool success = ((IBinarySerializable)value).TryGetWrittenSize(out int size);

        await Assert.That(success).IsTrue();
        await Assert.That(size).IsEqualTo(3);
    }

    /// <summary>
    /// Tests MAC address size calculation.
    /// </summary>
    [Test]
    public async Task TryGetWrittenSize_ByteArray_ReturnsCorrectSize()
    {
        WritableMacAddress mac = new()
        {
            Address = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF]
        };

        bool success = ((IBinarySerializable)mac).TryGetWrittenSize(out int size);

        await Assert.That(success).IsTrue();
        await Assert.That(size).IsEqualTo(6);
    }

    #endregion

    // ========================================================================
    // BIT FIELD WRITING TESTS
    // ========================================================================

    #region BitField Writing

    /// <summary>
    /// Tests writing CAN header with bit-level fields.
    /// </summary>
    [Test]
    public async Task CANHeader_WritesCorrectly()
    {
        WritableCANHeader header = new()
        {
            Identifier = 0x7FF, // 11 bits all set
            RTR = 1,
            IDE = 0,
            Reserved = 1,
            DLC = 8
        };
        byte[] buffer = new byte[3]; // 18 bits → 3 bytes

        bool success = header.TryWrite(buffer, out int written);

        await Assert.That(success).IsTrue();
        await Assert.That(written >= 2).IsTrue(); // At least 2 bytes for 18 bits

        // Verify by reading back
        ulong id, rtr, ide, reserved, dlc;
        {
            BitReader reader = new(buffer);
            id = reader.ReadBits(11);
            rtr = reader.ReadBits(1);
            ide = reader.ReadBits(1);
            reserved = reader.ReadBits(1);
            dlc = reader.ReadBits(4);
        }
        await Assert.That(id).IsEqualTo(0x7FFu);
        await Assert.That(rtr).IsEqualTo(1u);
        await Assert.That(ide).IsEqualTo(0u);
        await Assert.That(reserved).IsEqualTo(1u);
        await Assert.That(dlc).IsEqualTo(8u);
    }

    /// <summary>
    /// Tests writing mixed bit field struct.
    /// </summary>
    [Test]
    public async Task MixedBitField_WritesCorrectly()
    {
        WritableMixedBitField value = new()
        {
            Field3Bit = 5,  // 101
            Field5Bit = 19, // 10011
            Field12Bit = 0xABC,
            Field4Bit = 0xF
        };
        byte[] buffer = new byte[3]; // 24 bits = 3 bytes

        bool success = value.TryWrite(buffer, out int written);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(3);

        // Verify by reading back
        ulong f3, f5, f12, f4;
        {
            BitReader reader = new(buffer);
            f3 = reader.ReadBits(3);
            f5 = reader.ReadBits(5);
            f12 = reader.ReadBits(12);
            f4 = reader.ReadBits(4);
        }
        await Assert.That(f3).IsEqualTo(5u);
        await Assert.That(f5).IsEqualTo(19u);
        await Assert.That(f12).IsEqualTo(0xABCu);
        await Assert.That(f4).IsEqualTo(0xFu);
    }

    /// <summary>
    /// Tests writing single-bit flags.
    /// </summary>
    [Test]
    public async Task BitFlags_WritesCorrectly()
    {
        WritableBitFlags flags = new()
        {
            Flag1 = 1,
            Flag2 = 0,
            Flag3 = 1,
            Flag4 = 0,
            Flag5 = 1,
            Flag6 = 1,
            Flag7 = 0,
            Flag8 = 1
        };
        byte[] buffer = new byte[1]; // 8 bits = 1 byte

        bool success = flags.TryWrite(buffer, out int written);

        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(1);
        await Assert.That((int)buffer[0]).IsEqualTo(0b10101101); // Flags: 1,0,1,0,1,1,0,1
    }

    #endregion

    // ========================================================================
    // ROUNDTRIP TESTS (Parse → Write → Compare)
    // ========================================================================

    #region Roundtrip

    /// <summary>
    /// Tests roundtrip: parse then write produces identical bytes.
    /// </summary>
    [Test]
    public async Task Roundtrip_SimpleHeader()
    {
        byte[] original = [0x01, 0x00, 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x42];

        bool parsed = RoundtripHeader.TryParse(original, out RoundtripHeader header, out int consumed);
        await Assert.That(parsed).IsTrue();
        await Assert.That(consumed).IsEqualTo(8);

        byte[] output = new byte[8];
        bool written = header.TryWrite(output, out int bytesWritten);
        await Assert.That(written).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(8);

        await Assert.That(output).IsEquivalentTo(original);
    }

    /// <summary>
    /// Tests roundtrip with byte members.
    /// </summary>
    [Test]
    public async Task Roundtrip_WithByte()
    {
        byte[] original = [0x42, 0x01, 0x00, 0xFF];

        bool parsed = RoundtripWithByte.TryParse(original, out RoundtripWithByte value, out int consumed);
        await Assert.That(parsed).IsTrue();
        await Assert.That(consumed).IsEqualTo(4);

        byte[] output = new byte[4];
        bool written = value.TryWrite(output, out int bytesWritten);
        await Assert.That(written).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(4);

        await Assert.That(output).IsEquivalentTo(original);
    }

    /// <summary>
    /// Tests roundtrip with fixed byte array.
    /// </summary>
    [Test]
    public async Task Roundtrip_MacAddress()
    {
        byte[] original = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x08, 0x00];

        bool parsed = RoundtripMacAddress.TryParse(original, out RoundtripMacAddress mac, out int consumed);
        await Assert.That(parsed).IsTrue();
        await Assert.That(consumed).IsEqualTo(8);

        byte[] output = new byte[8];
        bool written = mac.TryWrite(output, out int bytesWritten);
        await Assert.That(written).IsTrue();
        await Assert.That(bytesWritten).IsEqualTo(8);

        await Assert.That(output).IsEquivalentTo(original);
    }

    /// <summary>
    /// Tests roundtrip with bit fields.
    /// </summary>
    [Test]
    public async Task Roundtrip_BitField()
    {
        // 3 bits (5=101) + 5 bits (19=10011) + 16 bits (0x1234)
        // Binary: 101 10011 0001 0010 0011 0100
        // Bytes: 10110011 00010010 00110100 = 0xB3 0x12 0x34
        byte[] original = [0xB3, 0x12, 0x34];

        bool parsed = RoundtripBitField.TryParse(original, out RoundtripBitField value, out int consumed);
        await Assert.That(parsed).IsTrue();

        byte[] output = new byte[3];
        bool written = value.TryWrite(output, out int bytesWritten);
        await Assert.That(written).IsTrue();

        await Assert.That(output).IsEquivalentTo(original);
    }

    #endregion

    // ========================================================================
    // IBinarySerializable INTERFACE TESTS
    // ========================================================================

    #region IBinarySerializable

    /// <summary>
    /// Tests that generated struct implements IBinarySerializable.
    /// </summary>
    [Test]
    public async Task ImplementsIBinarySerializable()
    {
        WritableSimpleHeader header = new()
        {
            Version = new U16BE(1),
            MessageType = new U32BE(2),
            Length = new U16BE(3)
        };

        // Verify it can be used as IBinarySerializable
        IBinarySerializable serializable = header;
        await Assert.That(serializable.TryGetWrittenSize(out int size)).IsTrue();
        await Assert.That(size).IsEqualTo(8);

        byte[] buffer = new byte[size];
        await Assert.That(serializable.TryWrite(buffer, out int written)).IsTrue();
        await Assert.That(written).IsEqualTo(8);
    }

    /// <summary>
    /// Tests WriteTo convenience method.
    /// </summary>
    [Test]
    public async Task WriteTo_SucceedsWithAdequateBuffer()
    {
        WritableByteStruct value = new() { First = 1, Second = 2, Third = 3 };
        byte[] buffer = new byte[3];

        value.WriteTo(buffer);

        await Assert.That(buffer).IsEquivalentTo((byte[])[1, 2, 3]);
    }

    /// <summary>
    /// Tests WriteTo throws on insufficient buffer.
    /// </summary>
    [Test]
    public async Task WriteTo_ThrowsOnInsufficientBuffer()
    {
        WritableSimpleHeader header = new()
        {
            Version = new U16BE(1),
            MessageType = new U32BE(2),
            Length = new U16BE(3)
        };

        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => header.WriteTo(new byte[2]));
        await Assert.That(ex).IsNotNull();
    }

    #endregion

    // ========================================================================
    // MIXED VARIABLE/FIXED WRITING TESTS
    // ========================================================================
    #region Mixed Variable/Fixed Writing

    [Test]
    public async Task WritableVarIntThenFixed_WritesCorrectly()
    {
        // Arrange: VarInt(42)=0x2A (1 byte), U32BE=0x01020304, U16BE=0xABCD → 7 bytes total
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
        await Assert.That(destination).IsEquivalentTo((byte[])[0x2A, 0x01, 0x02, 0x03, 0x04, 0xAB, 0xCD]);
    }

    [Test]
    public async Task WritableStringThenFixed_WritesCorrectly()
    {
        // Arrange: VarInt(2)=0x02 + "hi"=0x68 0x69 + U32BE=0xDEADBEEF → 7 bytes total
        WritableStringThenFixed packet = new()
        {
            Label = "hi",
            Id = new U32BE(0xDEADBEEF)
        };
        byte[] destination = new byte[7];

        // Act
        bool success = packet.TryWrite(destination, out int written);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(7);
        await Assert.That(destination).IsEquivalentTo((byte[])[0x02, 0x68, 0x69, 0xDE, 0xAD, 0xBE, 0xEF]);
    }

    [Test]
    public async Task WritableFixedBEString_WritesCorrectly()
    {
        // Arrange: 2-byte BE length=3 + "abc"=0x61 0x62 0x63 + U16BE=0x1234 → 7 bytes total
        WritableFixedBEString packet = new()
        {
            Content = "abc",
            Crc = new U16BE(0x1234)
        };
        byte[] destination = new byte[7];

        // Act
        bool success = packet.TryWrite(destination, out int written);

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(written).IsEqualTo(7);
        await Assert.That(destination).IsEquivalentTo((byte[])[0x00, 0x03, 0x61, 0x62, 0x63, 0x12, 0x34]);
    }

    #endregion
}