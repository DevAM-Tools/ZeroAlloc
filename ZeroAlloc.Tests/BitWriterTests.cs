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
// BitWriter Tests
// ============================================================================
// Tests for the BitWriter ref struct, covering:
// - Aligned and unaligned writes
// - Roundtrip with BitReader
// - Various bit counts
// ============================================================================

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="BitWriter"/>.
/// </summary>
public class BitWriterTests
{
    // ========================================================================
    // BASIC WRITE TESTS
    // ========================================================================

    /// <summary>
    /// Tests writing a single aligned byte.
    /// </summary>
    [Fact]
    public void WriteByte_AlignedWrite()
    {
        byte[] buffer = new byte[1];
        BitWriter writer = new(buffer);

        writer.WriteByte(0xAB);

        Assert.Equal(0xAB, buffer[0]);
        Assert.Equal(8, writer.BitOffset);
    }

    /// <summary>
    /// Tests writing multiple bytes sequentially.
    /// </summary>
    [Fact]
    public void WriteMultipleBytes_Sequential()
    {
        byte[] buffer = new byte[3];
        BitWriter writer = new(buffer);

        writer.WriteByte(0x12);
        writer.WriteByte(0x34);
        writer.WriteByte(0x56);

        Assert.Equal(new byte[] { 0x12, 0x34, 0x56 }, buffer);
        Assert.Equal(24, writer.BitOffset);
    }

    /// <summary>
    /// Tests writing aligned 16-bit value.
    /// </summary>
    [Fact]
    public void WriteUInt16_AlignedWrite()
    {
        byte[] buffer = new byte[2];
        BitWriter writer = new(buffer);

        writer.WriteUInt16(0x1234);

        Assert.Equal(new byte[] { 0x12, 0x34 }, buffer);
    }

    /// <summary>
    /// Tests writing aligned 32-bit value.
    /// </summary>
    [Fact]
    public void WriteUInt32_AlignedWrite()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);

        writer.WriteUInt32(0xDEADBEEF);

        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, buffer);
    }

    /// <summary>
    /// Tests writing aligned 64-bit value.
    /// </summary>
    [Fact]
    public void WriteUInt64_AlignedWrite()
    {
        byte[] buffer = new byte[8];
        BitWriter writer = new(buffer);

        writer.WriteUInt64(0x0102030405060708);

        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }, buffer);
    }

    // ========================================================================
    // BIT-LEVEL WRITE TESTS
    // ========================================================================

    /// <summary>
    /// Tests writing individual bits.
    /// </summary>
    [Fact]
    public void WriteBits_SingleBits()
    {
        byte[] buffer = new byte[1];
        BitWriter writer = new(buffer);

        // Write 10110100 bit by bit
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.WriteBits(1, 1);
        writer.WriteBits(0, 1);
        writer.WriteBits(0, 1);

        Assert.Equal(0b10110100, buffer[0]);
    }

    /// <summary>
    /// Tests writing sub-byte fields.
    /// </summary>
    [Fact]
    public void WriteBits_SubByteFields()
    {
        byte[] buffer = new byte[1];
        BitWriter writer = new(buffer);

        // 3 bits (5=101) + 5 bits (19=10011) = 10110011 = 0xB3
        writer.WriteBits(5, 3);
        writer.WriteBits(19, 5);

        Assert.Equal(0xB3, buffer[0]);
    }

    /// <summary>
    /// Tests writing fields spanning byte boundaries.
    /// </summary>
    [Fact]
    public void WriteBits_CrossByteBoundary()
    {
        byte[] buffer = new byte[3];
        BitWriter writer = new(buffer);

        // Write 11-bit identifier (0x7FF = 11111111111)
        writer.WriteBits(0x7FF, 11);
        // Write 1-bit flag
        writer.WriteBits(1, 1);
        // Write 12-bit value (0xABC = 101010111100)
        writer.WriteBits(0xABC, 12);

        // Total: 24 bits = 3 bytes
        Assert.Equal(3, writer.BytesWritten);

        // Verify by reading back
        BitReader reader = new(buffer);
        Assert.Equal(0x7FFu, reader.ReadBits(11));
        Assert.Equal(1u, reader.ReadBits(1));
        Assert.Equal(0xABCu, reader.ReadBits(12));
    }

    /// <summary>
    /// Tests writing 48-bit value spanning multiple bytes.
    /// </summary>
    [Fact]
    public void WriteBits_LargeValue()
    {
        byte[] buffer = new byte[6];
        BitWriter writer = new(buffer);

        writer.WriteBits(0xAABBCCDDEEFF, 48);

        Assert.Equal(new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF }, buffer);
    }

    // ========================================================================
    // ROUNDTRIP WITH BitReader
    // ========================================================================

    /// <summary>
    /// Tests roundtrip: write then read produces same values.
    /// </summary>
    [Fact]
    public void Roundtrip_MixedBitFields()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);

        writer.WriteBits(5, 3);     // 3 bits
        writer.WriteBits(19, 5);    // 5 bits
        writer.WriteBits(0xABC, 12); // 12 bits
        writer.WriteBits(0xF, 4);   // 4 bits
        writer.WriteByte(0x42);     // 8 bits = 32 bits total

        BitReader reader = new(buffer);
        Assert.Equal(5u, reader.ReadBits(3));
        Assert.Equal(19u, reader.ReadBits(5));
        Assert.Equal(0xABCu, reader.ReadBits(12));
        Assert.Equal(0xFu, reader.ReadBits(4));
        Assert.Equal((byte)0x42, reader.ReadByte());
    }

    /// <summary>
    /// Tests roundtrip with aligned integer types.
    /// </summary>
    [Fact]
    public void Roundtrip_AlignedIntegers()
    {
        byte[] buffer = new byte[14];
        BitWriter writer = new(buffer);

        writer.WriteUInt16(0x1234);
        writer.WriteUInt32(0xDEADBEEF);
        writer.WriteUInt64(0x0102030405060708);

        BitReader reader = new(buffer);
        Assert.Equal(0x1234u, reader.ReadBits(16));
        Assert.Equal(0xDEADBEEFu, reader.ReadBits(32));
        Assert.Equal(0x0102030405060708u, reader.ReadBits(64));
    }

    /// <summary>
    /// Tests roundtrip with signed integers.
    /// </summary>
    [Fact]
    public void Roundtrip_SignedIntegers()
    {
        byte[] buffer = new byte[14];
        BitWriter writer = new(buffer);

        writer.WriteInt16(-1234);
        writer.WriteInt32(-100000);
        writer.WriteInt64(-9876543210L);

        BitReader reader = new(buffer);
        Assert.Equal((short)-1234, reader.ReadInt16());
        Assert.Equal(-100000, reader.ReadInt32());
        Assert.Equal(-9876543210L, reader.ReadInt64());
    }

    // ========================================================================
    // ALIGNMENT AND SKIP TESTS
    // ========================================================================

    /// <summary>
    /// Tests SkipBits advances position without writing.
    /// </summary>
    [Fact]
    public void SkipBits_AdvancesPosition()
    {
        byte[] buffer = new byte[2];
        BitWriter writer = new(buffer);

        writer.WriteBits(1, 1); // Write bit at position 0
        writer.SkipBits(7);     // Skip to byte 1
        writer.WriteByte(0xFF); // Write byte at position 1

        Assert.Equal(0x80, buffer[0]); // Only first bit set
        Assert.Equal(0xFF, buffer[1]);
    }

    /// <summary>
    /// Tests AlignToNextByte pads to byte boundary.
    /// </summary>
    [Fact]
    public void AlignToNextByte_PadsCorrectly()
    {
        byte[] buffer = new byte[2];
        BitWriter writer = new(buffer);

        writer.WriteBits(5, 3); // 3 bits
        Assert.False(writer.IsByteAligned);

        writer.AlignToNextByte();
        Assert.True(writer.IsByteAligned);
        Assert.Equal(8, writer.BitOffset);

        writer.WriteByte(0xFF);
        Assert.Equal(0xFF, buffer[1]);
    }

    /// <summary>
    /// Tests AlignToNextByte does nothing when already aligned.
    /// </summary>
    [Fact]
    public void AlignToNextByte_NoOp_WhenAligned()
    {
        byte[] buffer = new byte[2];
        BitWriter writer = new(buffer);

        writer.WriteByte(0xAA);
        Assert.True(writer.IsByteAligned);

        writer.AlignToNextByte();
        Assert.Equal(8, writer.BitOffset); // Unchanged
    }

    // ========================================================================
    // WriteBytes TESTS
    // ========================================================================

    /// <summary>
    /// Tests writing a byte array when aligned.
    /// </summary>
    [Fact]
    public void WriteBytes_Aligned()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);

        writer.WriteBytes(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF });

        Assert.Equal(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, buffer);
        Assert.Equal(32, writer.BitOffset);
    }

    /// <summary>
    /// Tests WriteBytes throws when not aligned.
    /// </summary>
    [Fact]
    public void WriteBytes_ThrowsWhenNotAligned()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);

        writer.WriteBits(1, 1); // Now not byte-aligned

        InvalidOperationException ex = null!;
        try
        {
            writer.WriteBytes(new byte[] { 0xDE, 0xAD });
        }
        catch (InvalidOperationException e)
        {
            ex = e;
        }

        Assert.NotNull(ex);
    }

    // ========================================================================
    // PROPERTY TESTS
    // ========================================================================

    /// <summary>
    /// Tests BytePosition property.
    /// </summary>
    [Fact]
    public void BytePosition_TracksCorrectly()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);

        Assert.Equal(0, writer.BytePosition);

        writer.WriteBits(1, 3);
        Assert.Equal(0, writer.BytePosition); // Still in first byte

        writer.WriteBits(1, 5);
        Assert.Equal(1, writer.BytePosition); // Now at second byte

        writer.WriteByte(0);
        Assert.Equal(2, writer.BytePosition);
    }

    /// <summary>
    /// Tests BytesWritten rounds up for partial bytes.
    /// </summary>
    [Fact]
    public void BytesWritten_RoundsUp()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);

        Assert.Equal(0, writer.BytesWritten);

        writer.WriteBits(1, 1);
        Assert.Equal(1, writer.BytesWritten); // 1 bit → 1 byte

        writer.WriteBits(0, 7);
        Assert.Equal(1, writer.BytesWritten); // 8 bits → 1 byte

        writer.WriteBits(1, 1);
        Assert.Equal(2, writer.BytesWritten); // 9 bits → 2 bytes
    }

    /// <summary>
    /// Tests RemainingBits/RemainingBytes properties.
    /// </summary>
    [Fact]
    public void RemainingBits_TracksCorrectly()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);

        Assert.Equal(32, writer.RemainingBits);
        Assert.Equal(4, writer.RemainingBytes);

        writer.WriteByte(0);
        Assert.Equal(24, writer.RemainingBits);
        Assert.Equal(3, writer.RemainingBytes);
    }
}
