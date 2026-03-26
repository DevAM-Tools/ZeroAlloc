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
public class BinaryWritableTests
{
    // ========================================================================
    // BASIC WRITING TESTS
    // ========================================================================

    #region Basic Writing

    /// <summary>
    /// Tests writing a simple header with endian wrappers.
    /// </summary>
    [Fact]
    public void SimpleHeader_WritesCorrectly()
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
        Assert.True(success);
        Assert.Equal(8, written);
        Assert.Equal(new byte[] { 0x01, 0x00, 0x12, 0x34, 0x56, 0x78, 0x00, 0xFF }, buffer);
    }

    /// <summary>
    /// Tests writing fails with insufficient buffer.
    /// </summary>
    [Fact]
    public void SimpleHeader_FailsWithInsufficientBuffer()
    {
        WritableSimpleHeader header = new()
        {
            Version = new U16BE(1),
            MessageType = new U32BE(2),
            Length = new U16BE(3)
        };
        byte[] buffer = new byte[5]; // Too small

        bool success = header.TryWrite(buffer, out int written);

        Assert.False(success);
        Assert.Equal(0, written);
    }

    /// <summary>
    /// Tests writing with mixed endianness.
    /// </summary>
    [Fact]
    public void MixedEndian_WritesCorrectly()
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

        Assert.True(success);
        Assert.Equal(12, written);
        // BE16=0x1234 → 12 34
        // LE16=0x5678 → 78 56
        // BE32=0xAABBCCDD → AA BB CC DD
        // LE32=0x11223344 → 44 33 22 11
        Assert.Equal(new byte[] { 0x12, 0x34, 0x78, 0x56, 0xAA, 0xBB, 0xCC, 0xDD, 0x44, 0x33, 0x22, 0x11 }, buffer);
    }

    /// <summary>
    /// Tests writing byte members.
    /// </summary>
    [Fact]
    public void ByteStruct_WritesCorrectly()
    {
        WritableByteStruct value = new() { First = 0xAA, Second = 0xBB, Third = 0xCC };
        byte[] buffer = new byte[3];

        bool success = value.TryWrite(buffer, out int written);

        Assert.True(success);
        Assert.Equal(3, written);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC }, buffer);
    }

    /// <summary>
    /// Tests that ignored members are not written.
    /// </summary>
    [Fact]
    public void IgnoredMember_NotWritten()
    {
        WritableWithIgnored value = new()
        {
            Type = new U16BE(42),
            Length = new U16BE(100)
        };
        byte[] buffer = new byte[4];

        bool success = value.TryWrite(buffer, out int written);

        Assert.True(success);
        Assert.Equal(4, written); // Only Type + Length, not IsControl
        Assert.Equal(new byte[] { 0x00, 42, 0x00, 100 }, buffer);
    }

    /// <summary>
    /// Tests explicit member ordering.
    /// </summary>
    [Fact]
    public void ReorderedStruct_WritesInSpecifiedOrder()
    {
        WritableReordered value = new()
        {
            Third = new U16BE(0x0003),
            First = new U16BE(0x0001),
            Second = new U16BE(0x0002)
        };
        byte[] buffer = new byte[6];

        bool success = value.TryWrite(buffer, out int written);

        Assert.True(success);
        Assert.Equal(6, written);
        // Written in order: First(0x0001), Second(0x0002), Third(0x0003)
        Assert.Equal(new byte[] { 0x00, 0x01, 0x00, 0x02, 0x00, 0x03 }, buffer);
    }

    /// <summary>
    /// Tests writing fixed-length byte array.
    /// </summary>
    [Fact]
    public void FixedByteArray_WritesCorrectly()
    {
        WritableMacAddress mac = new()
        {
            Address = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF]
        };
        byte[] buffer = new byte[6];

        bool success = mac.TryWrite(buffer, out int written);

        Assert.True(success);
        Assert.Equal(6, written);
        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }, buffer);
    }

    #endregion

    // ========================================================================
    // SIZE CALCULATION TESTS
    // ========================================================================

    #region TryGetSerializedSize

    /// <summary>
    /// Tests fixed-size type returns correct size.
    /// </summary>
    [Fact]
    public void TryGetSerializedSize_FixedSize_ReturnsCorrectSize()
    {
        WritableSimpleHeader header = new()
        {
            Version = new U16BE(1),
            MessageType = new U32BE(2),
            Length = new U16BE(3)
        };

        bool success = ((IBinarySerializable)header).TryGetSerializedSize(out int size);

        Assert.True(success);
        Assert.Equal(8, size); // 2 + 4 + 2
    }

    /// <summary>
    /// Tests byte struct size calculation.
    /// </summary>
    [Fact]
    public void TryGetSerializedSize_ByteStruct_ReturnsCorrectSize()
    {
        WritableByteStruct value = new() { First = 1, Second = 2, Third = 3 };

        bool success = ((IBinarySerializable)value).TryGetSerializedSize(out int size);

        Assert.True(success);
        Assert.Equal(3, size);
    }

    /// <summary>
    /// Tests MAC address size calculation.
    /// </summary>
    [Fact]
    public void TryGetSerializedSize_ByteArray_ReturnsCorrectSize()
    {
        WritableMacAddress mac = new()
        {
            Address = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF]
        };

        bool success = ((IBinarySerializable)mac).TryGetSerializedSize(out int size);

        Assert.True(success);
        Assert.Equal(6, size);
    }

    #endregion

    // ========================================================================
    // BIT FIELD WRITING TESTS
    // ========================================================================

    #region BitField Writing

    /// <summary>
    /// Tests writing CAN header with bit-level fields.
    /// </summary>
    [Fact]
    public void CANHeader_WritesCorrectly()
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

        Assert.True(success);
        Assert.True(written >= 2); // At least 2 bytes for 18 bits

        // Verify by reading back
        BitReader reader = new(buffer);
        Assert.Equal(0x7FFu, reader.ReadBits(11));
        Assert.Equal(1u, reader.ReadBits(1));
        Assert.Equal(0u, reader.ReadBits(1));
        Assert.Equal(1u, reader.ReadBits(1));
        Assert.Equal(8u, reader.ReadBits(4));
    }

    /// <summary>
    /// Tests writing mixed bit field struct.
    /// </summary>
    [Fact]
    public void MixedBitField_WritesCorrectly()
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

        Assert.True(success);
        Assert.Equal(3, written);

        // Verify by reading back
        BitReader reader = new(buffer);
        Assert.Equal(5u, reader.ReadBits(3));
        Assert.Equal(19u, reader.ReadBits(5));
        Assert.Equal(0xABCu, reader.ReadBits(12));
        Assert.Equal(0xFu, reader.ReadBits(4));
    }

    /// <summary>
    /// Tests writing single-bit flags.
    /// </summary>
    [Fact]
    public void BitFlags_WritesCorrectly()
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

        Assert.True(success);
        Assert.Equal(1, written);
        Assert.Equal(0b10101101, buffer[0]); // Flags: 1,0,1,0,1,1,0,1
    }

    #endregion

    // ========================================================================
    // ROUNDTRIP TESTS (Parse → Write → Compare)
    // ========================================================================

    #region Roundtrip

    /// <summary>
    /// Tests roundtrip: parse then write produces identical bytes.
    /// </summary>
    [Fact]
    public void Roundtrip_SimpleHeader()
    {
        byte[] original = [0x01, 0x00, 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x42];

        bool parsed = RoundtripHeader.TryParse(original, out RoundtripHeader header, out int consumed);
        Assert.True(parsed);
        Assert.Equal(8, consumed);

        byte[] output = new byte[8];
        bool written = header.TryWrite(output, out int bytesWritten);
        Assert.True(written);
        Assert.Equal(8, bytesWritten);

        Assert.Equal(original, output);
    }

    /// <summary>
    /// Tests roundtrip with byte members.
    /// </summary>
    [Fact]
    public void Roundtrip_WithByte()
    {
        byte[] original = [0x42, 0x01, 0x00, 0xFF];

        bool parsed = RoundtripWithByte.TryParse(original, out RoundtripWithByte value, out int consumed);
        Assert.True(parsed);
        Assert.Equal(4, consumed);

        byte[] output = new byte[4];
        bool written = value.TryWrite(output, out int bytesWritten);
        Assert.True(written);
        Assert.Equal(4, bytesWritten);

        Assert.Equal(original, output);
    }

    /// <summary>
    /// Tests roundtrip with fixed byte array.
    /// </summary>
    [Fact]
    public void Roundtrip_MacAddress()
    {
        byte[] original = [0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF, 0x08, 0x00];

        bool parsed = RoundtripMacAddress.TryParse(original, out RoundtripMacAddress mac, out int consumed);
        Assert.True(parsed);
        Assert.Equal(8, consumed);

        byte[] output = new byte[8];
        bool written = mac.TryWrite(output, out int bytesWritten);
        Assert.True(written);
        Assert.Equal(8, bytesWritten);

        Assert.Equal(original, output);
    }

    /// <summary>
    /// Tests roundtrip with bit fields.
    /// </summary>
    [Fact]
    public void Roundtrip_BitField()
    {
        // 3 bits (5=101) + 5 bits (19=10011) + 16 bits (0x1234)
        // Binary: 101 10011 0001 0010 0011 0100
        // Bytes: 10110011 00010010 00110100 = 0xB3 0x12 0x34
        byte[] original = [0xB3, 0x12, 0x34];

        bool parsed = RoundtripBitField.TryParse(original, out RoundtripBitField value, out int consumed);
        Assert.True(parsed);

        byte[] output = new byte[3];
        bool written = value.TryWrite(output, out int bytesWritten);
        Assert.True(written);

        Assert.Equal(original, output);
    }

    #endregion

    // ========================================================================
    // IBinarySerializable INTERFACE TESTS
    // ========================================================================

    #region IBinarySerializable

    /// <summary>
    /// Tests that generated struct implements IBinarySerializable.
    /// </summary>
    [Fact]
    public void ImplementsIBinarySerializable()
    {
        WritableSimpleHeader header = new()
        {
            Version = new U16BE(1),
            MessageType = new U32BE(2),
            Length = new U16BE(3)
        };

        // Verify it can be used as IBinarySerializable
        IBinarySerializable serializable = header;
        Assert.True(serializable.TryGetSerializedSize(out int size));
        Assert.Equal(8, size);

        byte[] buffer = new byte[size];
        Assert.True(serializable.TryWrite(buffer, out int written));
        Assert.Equal(8, written);
    }

    /// <summary>
    /// Tests WriteTo convenience method.
    /// </summary>
    [Fact]
    public void WriteTo_SucceedsWithAdequateBuffer()
    {
        WritableByteStruct value = new() { First = 1, Second = 2, Third = 3 };
        byte[] buffer = new byte[3];

        value.WriteTo(buffer);

        Assert.Equal(new byte[] { 1, 2, 3 }, buffer);
    }

    /// <summary>
    /// Tests WriteTo throws on insufficient buffer.
    /// </summary>
    [Fact]
    public void WriteTo_ThrowsOnInsufficientBuffer()
    {
        WritableSimpleHeader header = new()
        {
            Version = new U16BE(1),
            MessageType = new U32BE(2),
            Length = new U16BE(3)
        };

        Assert.Throws<InvalidOperationException>(() => header.WriteTo(new byte[2]));
    }

    #endregion
}
