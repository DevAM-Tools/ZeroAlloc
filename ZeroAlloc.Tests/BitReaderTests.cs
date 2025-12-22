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
/// Tests for <see cref="BitReader"/>.
/// Covers bit-level reading, alignment, and edge cases.
/// </summary>
public class BitReaderTests
{
    // ========================================================================
    // SINGLE BIT READS
    // ========================================================================

    [Fact]
    public void BitReader_ReadBit1_ReadsCorrectBit()
    {
        // Arrange - 0b10000000 = 0x80
        byte[] data = [0x80];
        BitReader reader = new(data);

        // Act
        Bit1 bit = reader.ReadBit1();

        // Assert
        Assert.True(bit.Value);
        Assert.Equal(1, reader.BitOffset);
    }

    [Fact]
    public void BitReader_ReadBit1_MultipleBits_ReadsInOrder()
    {
        // Arrange - 0b10110000 = 0xB0
        byte[] data = [0xB0];
        BitReader reader = new(data);

        // Act & Assert
        Assert.True(reader.ReadBit1().Value);   // Bit 7: 1
        Assert.False(reader.ReadBit1().Value);  // Bit 6: 0
        Assert.True(reader.ReadBit1().Value);   // Bit 5: 1
        Assert.True(reader.ReadBit1().Value);   // Bit 4: 1
        Assert.False(reader.ReadBit1().Value);  // Bit 3: 0
        Assert.False(reader.ReadBit1().Value);  // Bit 2: 0
        Assert.False(reader.ReadBit1().Value);  // Bit 1: 0
        Assert.False(reader.ReadBit1().Value);  // Bit 0: 0
    }

    // ========================================================================
    // MULTI-BIT READS (2-7 BITS)
    // ========================================================================

    [Fact]
    public void BitReader_ReadBit2_ReadsCorrectValue()
    {
        // Arrange - 0b11000000 = 0xC0 -> 2 bits = 3
        byte[] data = [0xC0];
        BitReader reader = new(data);

        // Act
        Bit2 result = reader.ReadBit2();

        // Assert
        Assert.Equal(3, result.Value);
        Assert.Equal(2, reader.BitOffset);
    }

    [Fact]
    public void BitReader_ReadBit3_ReadsCorrectValue()
    {
        // Arrange - 0b10100000 = 0xA0 -> 3 bits = 5
        byte[] data = [0xA0];
        BitReader reader = new(data);

        // Act
        Bit3 result = reader.ReadBit3();

        // Assert
        Assert.Equal(5, result.Value);
        Assert.Equal(3, reader.BitOffset);
    }

    [Fact]
    public void BitReader_ReadNibble_ReadsCorrectValue()
    {
        // Arrange - 0b11110000 = 0xF0 -> 4 bits = 15
        byte[] data = [0xF0];
        BitReader reader = new(data);

        // Act
        Nibble result = reader.ReadNibble();

        // Assert
        Assert.Equal(15, result.Value);
        Assert.Equal(4, reader.BitOffset);
    }

    [Fact]
    public void BitReader_ReadBit5_ReadsCorrectValue()
    {
        // Arrange - 0b10101000 = 0xA8 -> 5 bits = 21
        byte[] data = [0xA8];
        BitReader reader = new(data);

        // Act
        Bit5 result = reader.ReadBit5();

        // Assert
        Assert.Equal(21, result.Value);
        Assert.Equal(5, reader.BitOffset);
    }

    [Fact]
    public void BitReader_ReadBit6_ReadsCorrectValue()
    {
        // Arrange - 0b10101000 = 0xA8 -> 6 bits = 42
        byte[] data = [0xA8];
        BitReader reader = new(data);

        // Act
        Bit6 result = reader.ReadBit6();

        // Assert
        Assert.Equal(42, result.Value);
        Assert.Equal(6, reader.BitOffset);
    }

    [Fact]
    public void BitReader_ReadBit7_ReadsCorrectValue()
    {
        // Arrange - 0b10101010 = 0xAA -> 7 bits = 85
        byte[] data = [0xAA];
        BitReader reader = new(data);

        // Act
        Bit7 result = reader.ReadBit7();

        // Assert
        Assert.Equal(85, result.Value);
        Assert.Equal(7, reader.BitOffset);
    }

    // ========================================================================
    // READBITS - GENERIC BIT READING
    // ========================================================================

    [Fact]
    public void BitReader_ReadBits_1Bit_ReadsCorrectly()
    {
        // Arrange - 0x80 = 0b10000000
        byte[] data = [0x80];
        BitReader reader = new(data);

        // Act
        ulong result = reader.ReadBits(1);

        // Assert
        Assert.Equal(1UL, result);
    }

    [Fact]
    public void BitReader_ReadBits_8Bits_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0xAB];
        BitReader reader = new(data);

        // Act
        ulong result = reader.ReadBits(8);

        // Assert
        Assert.Equal(0xABUL, result);
    }

    [Fact]
    public void BitReader_ReadBits_16Bits_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34];
        BitReader reader = new(data);

        // Act
        ulong result = reader.ReadBits(16);

        // Assert
        Assert.Equal(0x1234UL, result);
    }

    [Fact]
    public void BitReader_ReadBits_32Bits_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78];
        BitReader reader = new(data);

        // Act
        ulong result = reader.ReadBits(32);

        // Assert
        Assert.Equal(0x12345678UL, result);
    }

    [Fact]
    public void BitReader_ReadBits_64Bits_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0];
        BitReader reader = new(data);

        // Act
        ulong result = reader.ReadBits(64);

        // Assert
        Assert.Equal(0x123456789ABCDEF0UL, result);
    }

    [Fact]
    public void BitReader_ReadBits_NonAligned_ReadsCorrectly()
    {
        // Arrange - Read 12 bits from 0xABC
        byte[] data = [0xAB, 0xC0]; // 0xABC shifted into high bits
        BitReader reader = new(data);

        // Act
        ulong result = reader.ReadBits(12);

        // Assert
        Assert.Equal(0xABCUL, result);
    }

    [Fact]
    public void BitReader_ReadBits_InvalidBitCount_ThrowsException()
    {
        // Arrange
        byte[] data = [0x00];
        BitReader reader = new(data);

        // Act & Assert (try-catch because ref struct can't be in lambda)
        try
        {
            reader.ReadBits(0);
            Assert.Fail("Expected ArgumentOutOfRangeException for 0 bits");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }

        // Reset reader for second test
        BitReader reader2 = new(data);
        try
        {
            reader2.ReadBits(65);
            Assert.Fail("Expected ArgumentOutOfRangeException for 65 bits");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    // ========================================================================
    // BYTE READS
    // ========================================================================

    [Fact]
    public void BitReader_ReadByte_ByteAligned_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0xAB];
        BitReader reader = new(data);

        // Act
        byte result = reader.ReadByte();

        // Assert
        Assert.Equal(0xAB, result);
        Assert.Equal(8, reader.BitOffset);
    }

    [Fact]
    public void BitReader_ReadByte_NotByteAligned_ReadsCorrectly()
    {
        // Arrange - 0b11111111 0b11110000 -> after 4 bits, next byte = 0xFF
        byte[] data = [0xFF, 0xF0];
        BitReader reader = new(data);

        // Skip 4 bits first
        reader.ReadNibble();

        // Act
        byte result = reader.ReadByte();

        // Assert
        Assert.Equal(0xFF, result);
    }

    // ========================================================================
    // INTEGER READS
    // ========================================================================

    [Fact]
    public void BitReader_ReadUInt16_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34];
        BitReader reader = new(data);

        // Act
        ushort result = reader.ReadUInt16();

        // Assert
        Assert.Equal(0x1234, result);
    }

    [Fact]
    public void BitReader_ReadUInt32_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78];
        BitReader reader = new(data);

        // Act
        uint result = reader.ReadUInt32();

        // Assert
        Assert.Equal(0x12345678U, result);
    }

    [Fact]
    public void BitReader_ReadUInt64_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0];
        BitReader reader = new(data);

        // Act
        ulong result = reader.ReadUInt64();

        // Assert
        Assert.Equal(0x123456789ABCDEF0UL, result);
    }

    [Fact]
    public void BitReader_ReadInt16_ReadsCorrectly()
    {
        // Arrange - -2 in big-endian
        byte[] data = [0xFF, 0xFE];
        BitReader reader = new(data);

        // Act
        short result = reader.ReadInt16();

        // Assert
        Assert.Equal(-2, result);
    }

    [Fact]
    public void BitReader_ReadInt32_ReadsCorrectly()
    {
        // Arrange - -1 in big-endian
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF];
        BitReader reader = new(data);

        // Act
        int result = reader.ReadInt32();

        // Assert
        Assert.Equal(-1, result);
    }

    [Fact]
    public void BitReader_ReadInt64_ReadsCorrectly()
    {
        // Arrange - -1 in big-endian
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        BitReader reader = new(data);

        // Act
        long result = reader.ReadInt64();

        // Assert
        Assert.Equal(-1L, result);
    }

    // ========================================================================
    // UINTBITS AND INTBITS
    // ========================================================================

    [Fact]
    public void BitReader_ReadUIntBits_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0xAB, 0xC0];
        BitReader reader = new(data);

        // Act
        UIntBits result = reader.ReadUIntBits(12);

        // Assert
        Assert.Equal(0xABCUL, result.Value);
        Assert.Equal(12, result.BitCount);
    }

    [Fact]
    public void BitReader_ReadIntBits_PositiveValue_ReadsCorrectly()
    {
        // Arrange - 0b0101 = 5 (positive)
        byte[] data = [0x50];
        BitReader reader = new(data);

        // Act
        IntBits result = reader.ReadIntBits(4);

        // Assert
        Assert.Equal(5L, result.Value);
    }

    [Fact]
    public void BitReader_ReadIntBits_NegativeValue_ReadsCorrectly()
    {
        // Arrange - 0b1111 = -1 as 4-bit signed
        byte[] data = [0xF0];
        BitReader reader = new(data);

        // Act
        IntBits result = reader.ReadIntBits(4);

        // Assert
        Assert.Equal(-1L, result.Value);
    }

    // ========================================================================
    // ALIGNMENT
    // ========================================================================

    [Fact]
    public void BitReader_IsByteAligned_InitiallyTrue()
    {
        // Arrange
        byte[] data = [0x00];
        BitReader reader = new(data);

        // Assert
        Assert.True(reader.IsByteAligned);
    }

    [Fact]
    public void BitReader_IsByteAligned_FalseAfterBitRead()
    {
        // Arrange
        byte[] data = [0x00];
        BitReader reader = new(data);

        // Act
        reader.ReadBit1();

        // Assert
        Assert.False(reader.IsByteAligned);
    }

    [Fact]
    public void BitReader_AlignToNextByte_AlignsCorrectly()
    {
        // Arrange
        byte[] data = [0xFF, 0xAB];
        BitReader reader = new(data);
        reader.ReadBit1(); // Now at bit 1

        // Act
        reader.AlignToNextByte();

        // Assert
        Assert.True(reader.IsByteAligned);
        Assert.Equal(8, reader.BitOffset);
    }

    [Fact]
    public void BitReader_AlignToNextByte_AlreadyAligned_NoChange()
    {
        // Arrange
        byte[] data = [0x00];
        BitReader reader = new(data);

        // Act
        reader.AlignToNextByte();

        // Assert
        Assert.Equal(0, reader.BitOffset);
    }

    [Fact]
    public void BitReader_AlignToNextByte_AfterNibble_AlignsTo8()
    {
        // Arrange
        byte[] data = [0x00, 0x00];
        BitReader reader = new(data);
        reader.ReadNibble(); // Now at bit 4

        // Act
        reader.AlignToNextByte();

        // Assert
        Assert.Equal(8, reader.BitOffset);
        Assert.True(reader.IsByteAligned);
    }

    // ========================================================================
    // SKIP BITS
    // ========================================================================

    [Fact]
    public void BitReader_SkipBits_AdvancesCorrectly()
    {
        // Arrange
        byte[] data = [0x00, 0xAB];
        BitReader reader = new(data);

        // Act
        reader.SkipBits(8);

        // Assert
        Assert.Equal(8, reader.BitOffset);
        Assert.Equal(0xAB, reader.ReadByte());
    }

    [Fact]
    public void BitReader_SkipBits_NonByteAligned_AdvancesCorrectly()
    {
        // Arrange
        byte[] data = [0x00, 0x00];
        BitReader reader = new(data);

        // Act
        reader.SkipBits(5);

        // Assert
        Assert.Equal(5, reader.BitOffset);
        Assert.False(reader.IsByteAligned);
    }

    // ========================================================================
    // READ BYTES
    // ========================================================================

    [Fact]
    public void BitReader_ReadBytes_ByteAligned_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        BitReader reader = new(data);

        // Act
        ReadOnlySpan<byte> result = reader.ReadBytes(3);

        // Assert
        Assert.Equal([0x01, 0x02, 0x03], result.ToArray());
        Assert.Equal(24, reader.BitOffset);
    }

    [Fact]
    public void BitReader_ReadBytes_NotByteAligned_ThrowsException()
    {
        // Arrange
        byte[] data = [0x00, 0x01, 0x02, 0x03];
        BitReader reader = new(data);
        reader.ReadBit1(); // Not aligned

        // Act & Assert (try-catch because ref struct can't be in lambda)
        try
        {
            reader.ReadBytes(2);
            Assert.Fail("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    // ========================================================================
    // PROPERTIES
    // ========================================================================

    [Fact]
    public void BitReader_BitOffset_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x00, 0x00];
        BitReader reader = new(data);

        // Act & Assert
        Assert.Equal(0, reader.BitOffset);
        reader.ReadBit1();
        Assert.Equal(1, reader.BitOffset);
        reader.ReadNibble();
        Assert.Equal(5, reader.BitOffset);
    }

    [Fact]
    public void BitReader_BytePosition_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x00, 0x00, 0x00];
        BitReader reader = new(data);

        // Act & Assert
        Assert.Equal(0, reader.BytePosition);
        reader.SkipBits(8);
        Assert.Equal(1, reader.BytePosition);
        reader.SkipBits(4);
        Assert.Equal(1, reader.BytePosition); // Still in byte 1
        reader.SkipBits(4);
        Assert.Equal(2, reader.BytePosition);
    }

    [Fact]
    public void BitReader_BitPositionInByte_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x00, 0x00];
        BitReader reader = new(data);

        // Act & Assert
        Assert.Equal(0, reader.BitPositionInByte);
        reader.ReadBit1();
        Assert.Equal(1, reader.BitPositionInByte);
        reader.ReadNibble();
        Assert.Equal(5, reader.BitPositionInByte);
        reader.ReadBit3();
        Assert.Equal(0, reader.BitPositionInByte); // Rolled over to next byte
    }

    [Fact]
    public void BitReader_RemainingBits_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x00, 0x00]; // 16 bits total
        BitReader reader = new(data);

        // Act & Assert
        Assert.Equal(16, reader.RemainingBits);
        reader.ReadByte();
        Assert.Equal(8, reader.RemainingBits);
    }

    // ========================================================================
    // CROSS-BYTE READING
    // ========================================================================

    [Fact]
    public void BitReader_ReadBitsCrossingBytesBoundary_ReadsCorrectly()
    {
        // Arrange - Read 12 bits that span across 2 bytes
        // 0xAB = 0b10101011, 0xCD = 0b11001101
        // Reading 12 bits from bit 4: 0b1011 1100 1101 = 0xBCD
        byte[] data = [0xAB, 0xCD];
        BitReader reader = new(data);
        reader.SkipBits(4); // Start at bit 4

        // Act
        ulong result = reader.ReadBits(12);

        // Assert
        Assert.Equal(0xBCDUL, result);
    }

    [Fact]
    public void BitReader_ReadNibblesAcrossBytes_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0xAB, 0xCD];
        BitReader reader = new(data);

        // Act
        Nibble n1 = reader.ReadNibble(); // 0xA
        Nibble n2 = reader.ReadNibble(); // 0xB
        Nibble n3 = reader.ReadNibble(); // 0xC
        Nibble n4 = reader.ReadNibble(); // 0xD

        // Assert
        Assert.Equal(0x0A, n1.Value);
        Assert.Equal(0x0B, n2.Value);
        Assert.Equal(0x0C, n3.Value);
        Assert.Equal(0x0D, n4.Value);
    }

    // ========================================================================
    // COMPLEX SCENARIOS
    // ========================================================================

    [Fact]
    public void BitReader_MixedReads_WorkCorrectly()
    {
        // Arrange - Simulate CAN-like protocol
        // Byte 0: 1 bit flag, 3 bit priority, 4 bit length
        // Byte 1-2: 16 bit ID
        byte[] data = [0b1_101_0100, 0x12, 0x34];
        BitReader reader = new(data);

        // Act
        Bit1 flag = reader.ReadBit1();
        Bit3 priority = reader.ReadBit3();
        Nibble length = reader.ReadNibble();
        ushort id = reader.ReadUInt16();

        // Assert
        Assert.True(flag.Value);
        Assert.Equal(5, priority.Value); // 0b101 = 5
        Assert.Equal(4, length.Value);   // 0b0100 = 4
        Assert.Equal(0x1234, id);
    }

    [Fact]
    public void BitReader_ReadThenAlign_WorksCorrectly()
    {
        // Arrange
        byte[] data = [0xFF, 0xAB, 0xCD];
        BitReader reader = new(data);

        // Act
        reader.ReadBit3(); // Read 3 bits
        reader.AlignToNextByte();
        ushort value = reader.ReadUInt16();

        // Assert
        Assert.Equal(0xABCD, value);
    }
}
