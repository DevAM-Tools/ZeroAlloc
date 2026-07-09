// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="BitReader"/>.
/// Covers bit-level reading, alignment, and edge cases.
/// </summary>
public sealed class BitReaderTests
{

    // ========================================================================
    // SINGLE BIT READS
    // ========================================================================


    /// <summary>Verifies BitReader ReadBit1 ReadsCorrectBit.</summary>
    [Test]
    public async Task BitReader_ReadBit1_ReadsCorrectBit()
    {
        // Arrange - 0b10000000 = 0x80
        byte[] data = [0x80];
        bool bitValue;
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            Bit1 bit = reader.ReadBit1();
            bitValue = bit.Value;
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That(bitValue).IsTrue();
        await Assert.That(bitOffset).IsEqualTo(1);
    }

    /// <summary>Verifies BitReader ReadBit1 MultipleBits ReadsInOrder.</summary>
    [Test]
    public async Task BitReader_ReadBit1_MultipleBits_ReadsInOrder()
    {
        // Arrange - 0b10110000 = 0xB0
        byte[] data = [0xB0];
        bool bit7, bit6, bit5, bit4, bit3, bit2, bit1, bit0;
        {
            BitReader reader = new(data);

            // Act
            bit7 = reader.ReadBit1().Value;   // Bit 7: 1
            bit6 = reader.ReadBit1().Value;   // Bit 6: 0
            bit5 = reader.ReadBit1().Value;   // Bit 5: 1
            bit4 = reader.ReadBit1().Value;   // Bit 4: 1
            bit3 = reader.ReadBit1().Value;   // Bit 3: 0
            bit2 = reader.ReadBit1().Value;   // Bit 2: 0
            bit1 = reader.ReadBit1().Value;   // Bit 1: 0
            bit0 = reader.ReadBit1().Value;   // Bit 0: 0
        }

        // Assert
        await Assert.That(bit7).IsTrue();
        await Assert.That(bit6).IsFalse();
        await Assert.That(bit5).IsTrue();
        await Assert.That(bit4).IsTrue();
        await Assert.That(bit3).IsFalse();
        await Assert.That(bit2).IsFalse();
        await Assert.That(bit1).IsFalse();
        await Assert.That(bit0).IsFalse();
    }

    // ========================================================================
    // MULTI-BIT READS (2-7 BITS)
    // ========================================================================


    /// <summary>Verifies BitReader ReadBit2 ReadsCorrectValue.</summary>
    [Test]
    public async Task BitReader_ReadBit2_ReadsCorrectValue()
    {
        // Arrange - 0b11000000 = 0xC0 -> 2 bits = 3
        byte[] data = [0xC0];
        byte resultValue;
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            Bit2 result = reader.ReadBit2();
            resultValue = result.Value;
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That((int)resultValue).IsEqualTo(3);
        await Assert.That(bitOffset).IsEqualTo(2);
    }

    /// <summary>Verifies BitReader ReadBit3 ReadsCorrectValue.</summary>
    [Test]
    public async Task BitReader_ReadBit3_ReadsCorrectValue()
    {
        // Arrange - 0b10100000 = 0xA0 -> 3 bits = 5
        byte[] data = [0xA0];
        byte resultValue;
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            Bit3 result = reader.ReadBit3();
            resultValue = result.Value;
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That((int)resultValue).IsEqualTo(5);
        await Assert.That(bitOffset).IsEqualTo(3);
    }

    /// <summary>Verifies BitReader ReadNibble ReadsCorrectValue.</summary>
    [Test]
    public async Task BitReader_ReadNibble_ReadsCorrectValue()
    {
        // Arrange - 0b11110000 = 0xF0 -> 4 bits = 15
        byte[] data = [0xF0];
        byte resultValue;
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            Nibble result = reader.ReadNibble();
            resultValue = result.Value;
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That((int)resultValue).IsEqualTo(15);
        await Assert.That(bitOffset).IsEqualTo(4);
    }

    /// <summary>Verifies BitReader ReadBit5 ReadsCorrectValue.</summary>
    [Test]
    public async Task BitReader_ReadBit5_ReadsCorrectValue()
    {
        // Arrange - 0b10101000 = 0xA8 -> 5 bits = 21
        byte[] data = [0xA8];
        byte resultValue;
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            Bit5 result = reader.ReadBit5();
            resultValue = result.Value;
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That((int)resultValue).IsEqualTo(21);
        await Assert.That(bitOffset).IsEqualTo(5);
    }

    /// <summary>Verifies BitReader ReadBit6 ReadsCorrectValue.</summary>
    [Test]
    public async Task BitReader_ReadBit6_ReadsCorrectValue()
    {
        // Arrange - 0b10101000 = 0xA8 -> 6 bits = 42
        byte[] data = [0xA8];
        byte resultValue;
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            Bit6 result = reader.ReadBit6();
            resultValue = result.Value;
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That((int)resultValue).IsEqualTo(42);
        await Assert.That(bitOffset).IsEqualTo(6);
    }

    /// <summary>Verifies BitReader ReadBit7 ReadsCorrectValue.</summary>
    [Test]
    public async Task BitReader_ReadBit7_ReadsCorrectValue()
    {
        // Arrange - 0b10101010 = 0xAA -> 7 bits = 85
        byte[] data = [0xAA];
        byte resultValue;
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            Bit7 result = reader.ReadBit7();
            resultValue = result.Value;
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That((int)resultValue).IsEqualTo(85);
        await Assert.That(bitOffset).IsEqualTo(7);
    }

    // ========================================================================
    // READBITS - GENERIC BIT READING
    // ========================================================================


    /// <summary>Verifies BitReader ReadBits 1Bit ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadBits_1Bit_ReadsCorrectly()
    {
        // Arrange - 0x80 = 0b10000000
        byte[] data = [0x80];
        ulong result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadBits(1);
        }

        // Assert
        await Assert.That(result).IsEqualTo(1UL);
    }

    /// <summary>Verifies BitReader ReadBits 8Bits ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadBits_8Bits_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0xAB];
        ulong result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadBits(8);
        }

        // Assert
        await Assert.That(result).IsEqualTo(0xABUL);
    }

    /// <summary>Verifies BitReader ReadBits 16Bits ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadBits_16Bits_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34];
        ulong result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadBits(16);
        }

        // Assert
        await Assert.That(result).IsEqualTo(0x1234UL);
    }

    /// <summary>Verifies BitReader ReadBits 32Bits ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadBits_32Bits_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78];
        ulong result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadBits(32);
        }

        // Assert
        await Assert.That(result).IsEqualTo(0x12345678UL);
    }

    /// <summary>Verifies BitReader ReadBits 64Bits ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadBits_64Bits_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0];
        ulong result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadBits(64);
        }

        // Assert
        await Assert.That(result).IsEqualTo(0x123456789ABCDEF0UL);
    }

    /// <summary>Verifies BitReader ReadBits NonAligned ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadBits_NonAligned_ReadsCorrectly()
    {
        // Arrange - Read 12 bits from 0xABC
        byte[] data = [0xAB, 0xC0]; // 0xABC shifted into high bits
        ulong result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadBits(12);
        }

        // Assert
        await Assert.That(result).IsEqualTo(0xABCUL);
    }

    /// <summary>Verifies BitReader ReadBits InvalidBitCount ThrowsException.</summary>
    [Test]
    public async Task BitReader_ReadBits_InvalidBitCount_ThrowsException()
    {
        // Arrange
        byte[] data = [0x00];
        BitReader reader = new(data);

        // Act & Assert — zero bits (try-catch because BitReader is a ref struct)
        bool threw0 = false;
        try { reader.ReadBits(0); }
        catch (ArgumentOutOfRangeException) { threw0 = true; }
        await Assert.That(threw0).IsTrue();

        // Act & Assert — 65 bits
        BitReader reader2 = new(data);
        bool threw65 = false;
        try { reader2.ReadBits(65); }
        catch (ArgumentOutOfRangeException) { threw65 = true; }
        await Assert.That(threw65).IsTrue();
    }

    // ========================================================================
    // BYTE READS
    // ========================================================================


    /// <summary>Verifies BitReader ReadByte ByteAligned ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadByte_ByteAligned_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0xAB];
        byte result;
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadByte();
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That((int)result).IsEqualTo(0xAB);
        await Assert.That(bitOffset).IsEqualTo(8);
    }

    /// <summary>Verifies BitReader ReadByte NotByteAligned ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadByte_NotByteAligned_ReadsCorrectly()
    {
        // Arrange - 0b11111111 0b11110000 -> after 4 bits, next byte = 0xFF
        byte[] data = [0xFF, 0xF0];
        byte result;
        {
            BitReader reader = new(data);

            // Skip 4 bits first
            reader.ReadNibble();

            // Act
            result = reader.ReadByte();
        }

        // Assert
        await Assert.That((int)result).IsEqualTo(0xFF);
    }

    // ========================================================================
    // INTEGER READS
    // ========================================================================


    /// <summary>Verifies BitReader ReadUInt16 ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadUInt16_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34];
        ushort result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadUInt16();
        }

        // Assert
        await Assert.That((int)result).IsEqualTo(0x1234);
    }

    /// <summary>Verifies BitReader ReadUInt32 ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadUInt32_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78];
        uint result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadUInt32();
        }

        // Assert
        await Assert.That(result).IsEqualTo(0x12345678U);
    }

    /// <summary>Verifies BitReader ReadUInt64 ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadUInt64_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0];
        ulong result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadUInt64();
        }

        // Assert
        await Assert.That(result).IsEqualTo(0x123456789ABCDEF0UL);
    }

    /// <summary>Verifies BitReader ReadInt16 ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadInt16_ReadsCorrectly()
    {
        // Arrange - -2 in big-endian
        byte[] data = [0xFF, 0xFE];
        short result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadInt16();
        }

        // Assert
        await Assert.That((int)result).IsEqualTo(-2);
    }

    /// <summary>Verifies BitReader ReadInt32 ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadInt32_ReadsCorrectly()
    {
        // Arrange - -1 in big-endian
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF];
        int result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadInt32();
        }

        // Assert
        await Assert.That(result).IsEqualTo(-1);
    }

    /// <summary>Verifies BitReader ReadInt64 ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadInt64_ReadsCorrectly()
    {
        // Arrange - -1 in big-endian
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        long result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadInt64();
        }

        // Assert
        await Assert.That(result).IsEqualTo(-1L);
    }

    /// <summary>Verifies BitReader ReadUInt128 ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadUInt128_ReadsCorrectly()
    {
        // Arrange — 0x0102030405060708_090A0B0C0D0E0F10 spread over 16 bytes
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];
        UInt128 result;
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadUInt128();
            bitOffset = reader.BitOffset;
        }

        // Assert — upper half is 0x0102030405060708, lower half is 0x090A0B0C0D0E0F10
        await Assert.That(result).IsEqualTo(new UInt128(0x0102030405060708UL, 0x090A0B0C0D0E0F10UL));
        await Assert.That(bitOffset).IsEqualTo(128);
    }

    /// <summary>Verifies BitReader ReadInt128 AllOnes ReturnsMinusOne.</summary>
    [Test]
    public async Task BitReader_ReadInt128_AllOnes_ReturnsMinusOne()
    {
        // Arrange — all 0xFF bytes → Int128 = -1
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
                        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];
        Int128 result;
        {
            BitReader reader = new(data);

            // Act
            result = reader.ReadInt128();
        }

        // Assert
        await Assert.That(result).IsEqualTo((Int128)(-1));
    }

    /// <summary>Verifies BitReader ReadUInt128 AfterOtherRead ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadUInt128_AfterOtherRead_ReadsCorrectly()
    {
        // Arrange — first byte 0xAB then 16 bytes for UInt128
        byte[] data = [0xAB,
                        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
                        0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10];
        byte first;
        UInt128 result;
        {
            BitReader reader = new(data);

            // Act
            first = reader.ReadByte();
            result = reader.ReadUInt128();
        }

        // Assert
        await Assert.That(first).IsEqualTo((byte)0xAB);
        await Assert.That(result).IsEqualTo(new UInt128(0x0102030405060708UL, 0x090A0B0C0D0E0F10UL));
    }

    // ========================================================================
    // UINTBITS AND INTBITS
    // ========================================================================


    /// <summary>Verifies BitReader ReadUIntBits ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadUIntBits_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0xAB, 0xC0];
        ulong resultValue;
        byte resultBitCount;
        {
            BitReader reader = new(data);

            // Act
            UIntBits result = reader.ReadUIntBits(12);
            resultValue = result.Value;
            resultBitCount = result.BitCount;
        }

        // Assert
        await Assert.That(resultValue).IsEqualTo(0xABCUL);
        await Assert.That((int)resultBitCount).IsEqualTo(12);
    }

    /// <summary>Verifies BitReader ReadIntBits PositiveValue ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadIntBits_PositiveValue_ReadsCorrectly()
    {
        // Arrange - 0b0101 = 5 (positive)
        byte[] data = [0x50];
        long resultValue;
        {
            BitReader reader = new(data);

            // Act
            IntBits result = reader.ReadIntBits(4);
            resultValue = result.Value;
        }

        // Assert
        await Assert.That(resultValue).IsEqualTo(5L);
    }

    /// <summary>Verifies BitReader ReadIntBits NegativeValue ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadIntBits_NegativeValue_ReadsCorrectly()
    {
        // Arrange - 0b1111 = -1 as 4-bit signed
        byte[] data = [0xF0];
        long resultValue;
        {
            BitReader reader = new(data);

            // Act
            IntBits result = reader.ReadIntBits(4);
            resultValue = result.Value;
        }

        // Assert
        await Assert.That(resultValue).IsEqualTo(-1L);
    }

    // ========================================================================
    // ALIGNMENT
    // ========================================================================


    /// <summary>Verifies BitReader IsByteAligned InitiallyTrue.</summary>
    [Test]
    public async Task BitReader_IsByteAligned_InitiallyTrue()
    {
        // Arrange
        byte[] data = [0x00];
        bool isByteAligned;
        {
            BitReader reader = new(data);
            isByteAligned = reader.IsByteAligned;
        }

        // Assert
        await Assert.That(isByteAligned).IsTrue();
    }

    /// <summary>Verifies BitReader IsByteAligned FalseAfterBitRead.</summary>
    [Test]
    public async Task BitReader_IsByteAligned_FalseAfterBitRead()
    {
        // Arrange
        byte[] data = [0x00];
        bool isByteAligned;
        {
            BitReader reader = new(data);

            // Act
            reader.ReadBit1();
            isByteAligned = reader.IsByteAligned;
        }

        // Assert
        await Assert.That(isByteAligned).IsFalse();
    }

    /// <summary>Verifies BitReader AlignToNextByte AlignsCorrectly.</summary>
    [Test]
    public async Task BitReader_AlignToNextByte_AlignsCorrectly()
    {
        // Arrange
        byte[] data = [0xFF, 0xAB];
        bool isByteAligned;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.ReadBit1(); // Now at bit 1

            // Act
            reader.AlignToNextByte();
            isByteAligned = reader.IsByteAligned;
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That(isByteAligned).IsTrue();
        await Assert.That(bitOffset).IsEqualTo(8);
    }

    /// <summary>Verifies BitReader AlignToNextByte AlreadyAligned NoChange.</summary>
    [Test]
    public async Task BitReader_AlignToNextByte_AlreadyAligned_NoChange()
    {
        // Arrange
        byte[] data = [0x00];
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            reader.AlignToNextByte();
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies BitReader AlignToNextByte AfterNibble AlignsTo8.</summary>
    [Test]
    public async Task BitReader_AlignToNextByte_AfterNibble_AlignsTo8()
    {
        // Arrange
        byte[] data = [0x00, 0x00];
        int bitOffset;
        bool isByteAligned;
        {
            BitReader reader = new(data);
            reader.ReadNibble(); // Now at bit 4

            // Act
            reader.AlignToNextByte();
            bitOffset = reader.BitOffset;
            isByteAligned = reader.IsByteAligned;
        }

        // Assert
        await Assert.That(bitOffset).IsEqualTo(8);
        await Assert.That(isByteAligned).IsTrue();
    }

    // ========================================================================
    // SKIP BITS
    // ========================================================================


    /// <summary>Verifies BitReader SkipBits AdvancesCorrectly.</summary>
    [Test]
    public async Task BitReader_SkipBits_AdvancesCorrectly()
    {
        // Arrange
        byte[] data = [0x00, 0xAB];
        int bitOffset;
        byte nextByte;
        {
            BitReader reader = new(data);

            // Act
            reader.SkipBits(8);
            bitOffset = reader.BitOffset;
            nextByte = reader.ReadByte();
        }

        // Assert
        await Assert.That(bitOffset).IsEqualTo(8);
        await Assert.That((int)nextByte).IsEqualTo(0xAB);
    }

    /// <summary>Verifies BitReader SkipBits NonByteAligned AdvancesCorrectly.</summary>
    [Test]
    public async Task BitReader_SkipBits_NonByteAligned_AdvancesCorrectly()
    {
        // Arrange
        byte[] data = [0x00, 0x00];
        int bitOffset;
        bool isByteAligned;
        {
            BitReader reader = new(data);

            // Act
            reader.SkipBits(5);
            bitOffset = reader.BitOffset;
            isByteAligned = reader.IsByteAligned;
        }

        // Assert
        await Assert.That(bitOffset).IsEqualTo(5);
        await Assert.That(isByteAligned).IsFalse();
    }

    // ========================================================================
    // READ BYTES
    // ========================================================================


    /// <summary>Verifies BitReader ReadBytes ByteAligned ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadBytes_ByteAligned_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        byte[] resultArray;
        int bitOffset;
        {
            BitReader reader = new(data);

            // Act
            ReadOnlySpan<byte> result = reader.ReadBytes(3);
            resultArray = result.ToArray();
            bitOffset = reader.BitOffset;
        }

        // Assert
        await Assert.That(resultArray).IsEquivalentTo((byte[])[0x01, 0x02, 0x03]);
        await Assert.That(bitOffset).IsEqualTo(24);
    }

    /// <summary>Verifies BitReader ReadBytes NotByteAligned ThrowsException.</summary>
    [Test]
    public async Task BitReader_ReadBytes_NotByteAligned_ThrowsException()
    {
        // Arrange
        byte[] data = [0x00, 0x01, 0x02, 0x03];
        BitReader reader = new(data);
        reader.ReadBit1(); // Not aligned

        // Act & Assert (try-catch because BitReader is a ref struct)
        bool threw = false;
        try { reader.ReadBytes(2); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    // ========================================================================
    // PROPERTIES
    // ========================================================================


    /// <summary>Verifies BitReader BitOffset ReturnsCorrectValue.</summary>
    [Test]
    public async Task BitReader_BitOffset_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x00, 0x00];
        int bitOffset0, bitOffset1, bitOffset5;
        {
            BitReader reader = new(data);

            // Act & Assert
            bitOffset0 = reader.BitOffset;
            reader.ReadBit1();
            bitOffset1 = reader.BitOffset;
            reader.ReadNibble();
            bitOffset5 = reader.BitOffset;
        }

        await Assert.That(bitOffset0).IsEqualTo(0);
        await Assert.That(bitOffset1).IsEqualTo(1);
        await Assert.That(bitOffset5).IsEqualTo(5);
    }

    /// <summary>Verifies BitReader BytePosition ReturnsCorrectValue.</summary>
    [Test]
    public async Task BitReader_BytePosition_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x00, 0x00, 0x00];
        int bytePos0, bytePos1, bytePos1b, bytePos2;
        {
            BitReader reader = new(data);

            // Act & Assert
            bytePos0 = reader.BytePosition;
            reader.SkipBits(8);
            bytePos1 = reader.BytePosition;
            reader.SkipBits(4);
            bytePos1b = reader.BytePosition;
            reader.SkipBits(4);
            bytePos2 = reader.BytePosition;
        }

        await Assert.That(bytePos0).IsEqualTo(0);
        await Assert.That(bytePos1).IsEqualTo(1);
        await Assert.That(bytePos1b).IsEqualTo(1); // Still in byte 1
        await Assert.That(bytePos2).IsEqualTo(2);
    }

    /// <summary>Verifies BitReader BitPositionInByte ReturnsCorrectValue.</summary>
    [Test]
    public async Task BitReader_BitPositionInByte_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x00, 0x00];
        int bitPosInByte0, bitPosInByte1, bitPosInByte5, bitPosInByte0r;
        {
            BitReader reader = new(data);

            // Act & Assert
            bitPosInByte0 = reader.BitPositionInByte;
            reader.ReadBit1();
            bitPosInByte1 = reader.BitPositionInByte;
            reader.ReadNibble();
            bitPosInByte5 = reader.BitPositionInByte;
            reader.ReadBit3();
            bitPosInByte0r = reader.BitPositionInByte;
        }

        await Assert.That(bitPosInByte0).IsEqualTo(0);
        await Assert.That(bitPosInByte1).IsEqualTo(1);
        await Assert.That(bitPosInByte5).IsEqualTo(5);
        await Assert.That(bitPosInByte0r).IsEqualTo(0); // Rolled over to next byte
    }

    /// <summary>Verifies BitReader RemainingBits ReturnsCorrectValue.</summary>
    [Test]
    public async Task BitReader_RemainingBits_ReturnsCorrectValue()
    {
        // Arrange
        byte[] data = [0x00, 0x00]; // 16 bits total
        int remaining0, remaining1;
        {
            BitReader reader = new(data);

            // Act & Assert
            remaining0 = reader.RemainingBits;
            reader.ReadByte();
            remaining1 = reader.RemainingBits;
        }

        await Assert.That(remaining0).IsEqualTo(16);
        await Assert.That(remaining1).IsEqualTo(8);
    }

    // ========================================================================
    // CROSS-BYTE READING
    // ========================================================================


    /// <summary>Verifies BitReader ReadBitsCrossingBytesBoundary ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadBitsCrossingBytesBoundary_ReadsCorrectly()
    {
        // Arrange - Read 12 bits that span across 2 bytes
        // 0xAB = 0b10101011, 0xCD = 0b11001101
        // Reading 12 bits from bit 4: 0b1011 1100 1101 = 0xBCD
        byte[] data = [0xAB, 0xCD];
        ulong result;
        {
            BitReader reader = new(data);
            reader.SkipBits(4); // Start at bit 4

            // Act
            result = reader.ReadBits(12);
        }

        // Assert
        await Assert.That(result).IsEqualTo(0xBCDUL);
    }

    /// <summary>Verifies BitReader ReadNibblesAcrossBytes ReadsCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadNibblesAcrossBytes_ReadsCorrectly()
    {
        // Arrange
        byte[] data = [0xAB, 0xCD];
        byte n1Value, n2Value, n3Value, n4Value;
        {
            BitReader reader = new(data);

            // Act
            n1Value = reader.ReadNibble().Value; // 0xA
            n2Value = reader.ReadNibble().Value; // 0xB
            n3Value = reader.ReadNibble().Value; // 0xC
            n4Value = reader.ReadNibble().Value; // 0xD
        }

        // Assert
        await Assert.That((int)n1Value).IsEqualTo(0x0A);
        await Assert.That((int)n2Value).IsEqualTo(0x0B);
        await Assert.That((int)n3Value).IsEqualTo(0x0C);
        await Assert.That((int)n4Value).IsEqualTo(0x0D);
    }

    // ========================================================================
    // COMPLEX SCENARIOS
    // ========================================================================


    /// <summary>Verifies BitReader MixedReads WorkCorrectly.</summary>
    [Test]
    public async Task BitReader_MixedReads_WorkCorrectly()
    {
        // Arrange - Simulate CAN-like protocol
        // Byte 0: 1 bit flag, 3 bit priority, 4 bit length
        // Byte 1-2: 16 bit ID
        byte[] data = [0b1_101_0100, 0x12, 0x34];
        bool flagValue;
        byte priorityValue, lengthValue;
        ushort id;
        {
            BitReader reader = new(data);

            // Act
            flagValue = reader.ReadBit1().Value;
            priorityValue = reader.ReadBit3().Value;
            lengthValue = reader.ReadNibble().Value;
            id = reader.ReadUInt16();
        }

        // Assert
        await Assert.That(flagValue).IsTrue();
        await Assert.That((int)priorityValue).IsEqualTo(5); // 0b101 = 5
        await Assert.That((int)lengthValue).IsEqualTo(4);   // 0b0100 = 4
        await Assert.That((int)id).IsEqualTo(0x1234);
    }

    /// <summary>Verifies BitReader ReadThenAlign WorksCorrectly.</summary>
    [Test]
    public async Task BitReader_ReadThenAlign_WorksCorrectly()
    {
        // Arrange
        byte[] data = [0xFF, 0xAB, 0xCD];
        ushort value;
        {
            BitReader reader = new(data);

            // Act
            reader.ReadBit3(); // Read 3 bits
            reader.AlignToNextByte();
            value = reader.ReadUInt16();
        }

        // Assert
        await Assert.That((int)value).IsEqualTo(0xABCD);
    }

    // ========================================================================
    // BOUNDARY VALIDATION — ReadBit1
    // ========================================================================


    /// <summary>Verifies ReadBit1 throws when no bits remain.</summary>
    [Test]
    public async Task BitReader_ReadBit1_NoBitsRemaining_ThrowsInvalidOperationException()
    {
        byte[] data = [0xFF];
        BitReader reader = new(data);
        for (int i = 0; i < 8; i++) { reader.ReadBit1(); }
        bool threw = false;
        try { reader.ReadBit1(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies ReadBit1 throws when buffer is empty.</summary>
    [Test]
    public async Task BitReader_ReadBit1_EmptyBuffer_ThrowsInvalidOperationException()
    {
        byte[] empty = [];
        BitReader reader = new(empty);
        bool threw = false;
        try { reader.ReadBit1(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    // ========================================================================
    // BOUNDARY VALIDATION — ReadByte (aligned fast-path)
    // ========================================================================


    /// <summary>Verifies aligned ReadByte throws when no bytes remain.</summary>
    [Test]
    public async Task BitReader_ReadByte_Aligned_NoBytesRemaining_ThrowsInvalidOperationException()
    {
        byte[] data = [0xAB];
        BitReader reader = new(data);
        reader.ReadByte();
        bool threw = false;
        try { reader.ReadByte(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies aligned ReadByte throws on empty buffer.</summary>
    [Test]
    public async Task BitReader_ReadByte_Aligned_EmptyBuffer_ThrowsInvalidOperationException()
    {
        byte[] empty = [];
        BitReader reader = new(empty);
        bool threw = false;
        try { reader.ReadByte(); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    // ========================================================================
    // BOUNDARY VALIDATION — SkipBits
    // ========================================================================


    /// <summary>Verifies SkipBits throws for negative count.</summary>
    [Test]
    public async Task BitReader_SkipBits_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0xFF, 0xFF];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.SkipBits(-1); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies SkipBits throws when count exceeds remaining bits.</summary>
    [Test]
    public async Task BitReader_SkipBits_ExceedsRemaining_ThrowsInvalidOperationException()
    {
        byte[] data = [0xFF];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.SkipBits(9); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies SkipBits of exactly remaining bits does not throw.</summary>
    [Test]
    public async Task BitReader_SkipBits_ExactlyRemaining_DoesNotThrow()
    {
        byte[] data = [0xFF];
        int remainingBits;
        {
            BitReader reader = new(data);

            reader.SkipBits(8); // skip all 8 bits — must not throw
            remainingBits = reader.RemainingBits;
        }

        await Assert.That(remainingBits).IsEqualTo(0);
    }

    /// <summary>Verifies SkipBits preserves bit offset on failure (no state mutation before check).</summary>
    [Test]
    public async Task BitReader_SkipBits_Overflow_DoesNotMutateOffset()
    {
        byte[] data = [0xFF];
        int offsetBefore, offsetAfter;
        {
            BitReader reader = new(data);
            offsetBefore = reader.BitOffset;

            try { reader.SkipBits(100); }
            catch (InvalidOperationException) { /* expected */ }

            offsetAfter = reader.BitOffset;
        }

        await Assert.That(offsetAfter).IsEqualTo(offsetBefore);
    }

    // ========================================================================
    // BOUNDARY VALIDATION — ReadBytes
    // ========================================================================


    /// <summary>Verifies ReadBytes throws for negative byteCount.</summary>
    [Test]
    public async Task BitReader_ReadBytes_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0xFF, 0xFF];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.ReadBytes(-1); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies ReadBytes throws when count exceeds remaining bytes.</summary>
    [Test]
    public async Task BitReader_ReadBytes_ExceedsRemaining_ThrowsInvalidOperationException()
    {
        byte[] data = [0xFF];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.ReadBytes(2); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies ReadBytes does not mutate BitOffset on failure (checks before mutation).</summary>
    [Test]
    public async Task BitReader_ReadBytes_Overflow_DoesNotMutateOffset()
    {
        byte[] data = [0xFF, 0xFF];
        int offsetBefore, offsetAfter;
        {
            BitReader reader = new(data);
            offsetBefore = reader.BitOffset;

            try { reader.ReadBytes(100); }
            catch (InvalidOperationException) { /* expected */ }

            offsetAfter = reader.BitOffset;
        }

        await Assert.That(offsetAfter).IsEqualTo(offsetBefore);
    }

    /// <summary>Verifies ReadBytes throws when not byte-aligned.</summary>
    [Test]
    public async Task BitReader_ReadBytes_NotAligned_ThrowsInvalidOperationException()
    {
        byte[] data = [0xFF, 0xFF];
        BitReader reader = new(data);
        reader.ReadBit1();
        bool threw = false;
        try { reader.ReadBytes(1); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    // ========================================================================
    // TRY-READ — SUCCESS PATHS
    // ========================================================================


    /// <summary>Verifies BitReader TryReadBits SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadBits_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0b10110100];
        bool ok;
        ulong value;
        int bitOffset;
        {
            BitReader reader = new(data);
            ok = reader.TryReadBits(4, out value);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(value).IsEqualTo(0b1011UL);
        await Assert.That(bitOffset).IsEqualTo(4);
    }

    /// <summary>Verifies BitReader TryReadBit1 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadBit1_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0b10000000];
        bool ok;
        bool valueValue;
        {
            BitReader reader = new(data);
            ok = reader.TryReadBit1(out Bit1 value);
            valueValue = value.Value;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(valueValue).IsTrue();
    }

    /// <summary>Verifies BitReader TryReadBit2 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadBit2_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0b11000000];
        bool ok;
        byte valueValue;
        {
            BitReader reader = new(data);
            ok = reader.TryReadBit2(out Bit2 value);
            valueValue = value.Value;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)valueValue).IsEqualTo(3);
    }

    /// <summary>Verifies BitReader TryReadBit3 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadBit3_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0b11100000];
        bool ok;
        byte valueValue;
        {
            BitReader reader = new(data);
            ok = reader.TryReadBit3(out Bit3 value);
            valueValue = value.Value;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)valueValue).IsEqualTo(7);
    }

    /// <summary>Verifies BitReader TryReadNibble SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadNibble_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0b11110000];
        bool ok;
        byte valueValue;
        {
            BitReader reader = new(data);
            ok = reader.TryReadNibble(out Nibble value);
            valueValue = value.Value;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)valueValue).IsEqualTo(0xF);
    }

    /// <summary>Verifies BitReader TryReadBit5 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadBit5_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0b11111000];
        bool ok;
        byte valueValue;
        {
            BitReader reader = new(data);
            ok = reader.TryReadBit5(out Bit5 value);
            valueValue = value.Value;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)valueValue).IsEqualTo(31);
    }

    /// <summary>Verifies BitReader TryReadBit6 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadBit6_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0b11111100];
        bool ok;
        byte valueValue;
        {
            BitReader reader = new(data);
            ok = reader.TryReadBit6(out Bit6 value);
            valueValue = value.Value;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)valueValue).IsEqualTo(63);
    }

    /// <summary>Verifies BitReader TryReadBit7 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadBit7_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0b11111110];
        bool ok;
        byte valueValue;
        {
            BitReader reader = new(data);
            ok = reader.TryReadBit7(out Bit7 value);
            valueValue = value.Value;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)valueValue).IsEqualTo(127);
    }

    /// <summary>Verifies BitReader TryReadUIntBits SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadUIntBits_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0b10101010];
        bool ok;
        ulong valueValue;
        {
            BitReader reader = new(data);
            ok = reader.TryReadUIntBits(8, out UIntBits value);
            valueValue = value.Value;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(valueValue).IsEqualTo(0b10101010UL);
    }

    /// <summary>Verifies BitReader TryReadIntBits SufficientData PositiveValue ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadIntBits_SufficientData_PositiveValue_ReturnsTrueAndValue()
    {
        // 0b0101 = +5 as signed 4-bit
        byte[] data = [0b01010000];
        bool ok;
        long valueValue;
        {
            BitReader reader = new(data);
            ok = reader.TryReadIntBits(4, out IntBits value);
            valueValue = value.Value;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(valueValue).IsEqualTo(5L);
    }

    /// <summary>Verifies BitReader TryReadIntBits SufficientData NegativeValue ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadIntBits_SufficientData_NegativeValue_ReturnsTrueAndValue()
    {
        // 0b1101 = -3 as signed 4-bit (sign bit set)
        byte[] data = [0b11010000];
        bool ok;
        long valueValue;
        {
            BitReader reader = new(data);
            ok = reader.TryReadIntBits(4, out IntBits value);
            valueValue = value.Value;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(valueValue).IsEqualTo(-3L);
    }

    /// <summary>Verifies BitReader TryReadByte SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadByte_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0xAB];
        bool ok;
        byte value;
        {
            BitReader reader = new(data);
            ok = reader.TryReadByte(out value);
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)value).IsEqualTo(0xAB);
    }

    /// <summary>Verifies BitReader TryReadUInt16 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadUInt16_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0x12, 0x34];
        bool ok;
        ushort value;
        {
            BitReader reader = new(data);
            ok = reader.TryReadUInt16(out value);
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)value).IsEqualTo(0x1234);
    }

    /// <summary>Verifies BitReader TryReadUInt32 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadUInt32_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0x12, 0x34, 0x56, 0x78];
        bool ok;
        uint value;
        {
            BitReader reader = new(data);
            ok = reader.TryReadUInt32(out value);
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(value).IsEqualTo(0x12345678u);
    }

    /// <summary>Verifies BitReader TryReadUInt64 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadUInt64_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        bool ok;
        ulong value;
        {
            BitReader reader = new(data);
            ok = reader.TryReadUInt64(out value);
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(value).IsEqualTo(0x0102030405060708UL);
    }

    /// <summary>Verifies BitReader TryReadInt16 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadInt16_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0xFF, 0xFE]; // -2 in big-endian int16
        bool ok;
        short value;
        {
            BitReader reader = new(data);
            ok = reader.TryReadInt16(out value);
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)value).IsEqualTo(-2);
    }

    /// <summary>Verifies BitReader TryReadInt32 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadInt32_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF]; // -1 in big-endian int32
        bool ok;
        int value;
        {
            BitReader reader = new(data);
            ok = reader.TryReadInt32(out value);
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(value).IsEqualTo(-1);
    }

    /// <summary>Verifies BitReader TryReadInt64 SufficientData ReturnsTrueAndValue.</summary>
    [Test]
    public async Task BitReader_TryReadInt64_SufficientData_ReturnsTrueAndValue()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]; // -1 in big-endian int64
        bool ok;
        long value;
        {
            BitReader reader = new(data);
            ok = reader.TryReadInt64(out value);
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(value).IsEqualTo(-1L);
    }

    /// <summary>Verifies BitReader TrySkipBits SufficientData ReturnsTrueAndAdvances.</summary>
    [Test]
    public async Task BitReader_TrySkipBits_SufficientData_ReturnsTrueAndAdvances()
    {
        byte[] data = [0xAB, 0xCD];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            ok = reader.TrySkipBits(8);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(bitOffset).IsEqualTo(8);
    }

    /// <summary>Verifies BitReader TryReadBytes AlignedSufficientData ReturnsTrueAndSpan.</summary>
    [Test]
    public async Task BitReader_TryReadBytes_AlignedSufficientData_ReturnsTrueAndSpan()
    {
        byte[] data = [0x01, 0x02, 0x03];
        bool ok;
        byte[] valueArray;
        int bitOffset;
        {
            BitReader reader = new(data);
            ok = reader.TryReadBytes(2, out ReadOnlySpan<byte> value);
            valueArray = value.ToArray();
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(valueArray.Length).IsEqualTo(2);
        await Assert.That((int)valueArray[0]).IsEqualTo(0x01);
        await Assert.That((int)valueArray[1]).IsEqualTo(0x02);
        await Assert.That(bitOffset).IsEqualTo(16);
    }

    // ========================================================================
    // TRY-READ — FAILURE PATHS (insufficient data — returns false, no state change)
    // ========================================================================


    /// <summary>Verifies BitReader TryReadBits InsufficientData ReturnsFalseAndPreservesState.</summary>
    [Test]
    public async Task BitReader_TryReadBits_InsufficientData_ReturnsFalseAndPreservesState()
    {
        byte[] data = [0xFF];
        bool ok;
        ulong value;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.TryReadBits(6, out ulong _); // consume 6 bits, 2 remain
            ok = reader.TryReadBits(3, out value);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(value).IsEqualTo(0UL);
        await Assert.That(bitOffset).IsEqualTo(6); // state must not advance
    }

    /// <summary>Verifies BitReader TryReadBit1 EmptyBuffer ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadBit1_EmptyBuffer_ReturnsFalse()
    {
        bool ok;
        int bitOffset;
        {
            BitReader reader = new([]);
            ok = reader.TryReadBit1(out Bit1 _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies BitReader TryReadBit2 InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadBit2_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0xFF];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.TryReadBits(7, out ulong _); // 1 bit left
            ok = reader.TryReadBit2(out Bit2 _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(7);
    }

    /// <summary>Verifies BitReader TryReadNibble InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadNibble_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0xFF];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.TryReadBits(5, out ulong _); // 3 bits left
            ok = reader.TryReadNibble(out Nibble _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(5);
    }

    /// <summary>Verifies BitReader TryReadByte InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadByte_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0xFF];
        bool ok;
        byte value;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.TryReadBits(4, out ulong _); // 4 bits left
            ok = reader.TryReadByte(out value);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That((int)value).IsEqualTo(0);
        await Assert.That(bitOffset).IsEqualTo(4);
    }

    /// <summary>Verifies BitReader TryReadUInt16 InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadUInt16_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0xFF]; // only 8 bits
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            ok = reader.TryReadUInt16(out ushort _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies BitReader TryReadUInt32 InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadUInt32_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0x01, 0x02]; // only 16 bits
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            ok = reader.TryReadUInt32(out uint _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies BitReader TryReadUInt64 InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadUInt64_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0x01, 0x02, 0x03, 0x04]; // only 32 bits
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            ok = reader.TryReadUInt64(out ulong _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies BitReader TryReadInt32 InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadInt32_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0x01, 0x02]; // only 16 bits
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            ok = reader.TryReadInt32(out int _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies BitReader TrySkipBits InsufficientData ReturnsFalseAndPreservesState.</summary>
    [Test]
    public async Task BitReader_TrySkipBits_InsufficientData_ReturnsFalseAndPreservesState()
    {
        byte[] data = [0xFF];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            ok = reader.TrySkipBits(9);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies BitReader TrySkipBits Zero AlwaysReturnsTrue.</summary>
    [Test]
    public async Task BitReader_TrySkipBits_Zero_AlwaysReturnsTrue()
    {
        bool ok;
        int bitOffset;
        {
            BitReader reader = new([]);
            ok = reader.TrySkipBits(0);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies BitReader TryReadBytes NotAligned ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadBytes_NotAligned_ReturnsFalse()
    {
        byte[] data = [0xFF, 0xFF];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.TryReadBit1(out Bit1 _);
            ok = reader.TryReadBytes(1, out ReadOnlySpan<byte> _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(1); // state must not advance
    }

    /// <summary>Verifies BitReader TryReadBytes InsufficientBytes ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadBytes_InsufficientBytes_ReturnsFalse()
    {
        byte[] data = [0xFF];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            ok = reader.TryReadBytes(2, out ReadOnlySpan<byte> _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    // ========================================================================
    // TRY-READ — INVALID ARGUMENT PATHS (programming errors — still throw)
    // ========================================================================


    /// <summary>Verifies BitReader TryReadBits BitCountZero ThrowsArgumentOutOfRangeException.</summary>
    [Test]
    public async Task BitReader_TryReadBits_BitCountZero_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0xFF];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.TryReadBits(0, out ulong _); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BitReader TryReadBits BitCountOver64 ThrowsArgumentOutOfRangeException.</summary>
    [Test]
    public async Task BitReader_TryReadBits_BitCountOver64_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = new byte[16];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.TryReadBits(65, out ulong _); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BitReader TrySkipBits NegativeCount ThrowsArgumentOutOfRangeException.</summary>
    [Test]
    public async Task BitReader_TrySkipBits_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0xFF];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.TrySkipBits(-1); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BitReader TryReadBytes NegativeCount ThrowsArgumentOutOfRangeException.</summary>
    [Test]
    public async Task BitReader_TryReadBytes_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0xFF];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.TryReadBytes(-1, out ReadOnlySpan<byte> _); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BitReader TryReadUIntBits ZeroBitCount ThrowsArgumentOutOfRangeException.</summary>
    [Test]
    public async Task BitReader_TryReadUIntBits_ZeroBitCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0xFF];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.TryReadUIntBits(0, out UIntBits _); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BitReader TryReadIntBits ZeroBitCount ThrowsArgumentOutOfRangeException.</summary>
    [Test]
    public async Task BitReader_TryReadIntBits_ZeroBitCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] data = [0xFF];
        BitReader reader = new(data);
        bool threw = false;
        try { reader.TryReadIntBits(0, out IntBits _); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    // ========================================================================
    // EXIT-POINT COVERAGE — ReadBits throw / TryRead failure paths
    // ========================================================================


    /// <summary>Verifies BitReader ReadBits InsufficientData ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task BitReader_ReadBits_InsufficientData_ThrowsInvalidOperationException()
    {
        byte[] data = [0xFF];
        bool threw = false;
        {
            BitReader reader = new(data);
            reader.ReadBits(6);
            try { reader.ReadBits(8); }
            catch (InvalidOperationException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies BitReader TryReadBitFields InsufficientBits ReturnsFalse.</summary>
    [Test]
    [Arguments(6, 3)]
    [Arguments(4, 5)]
    [Arguments(3, 6)]
    [Arguments(2, 7)]
    public async Task BitReader_TryReadBitFields_InsufficientBits_ReturnsFalse(int consumedBits, int fieldBits)
    {
        byte[] data = [0xFF];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.ReadBits(consumedBits);
            ok = fieldBits switch
            {
                3 => reader.TryReadBit3(out Bit3 _),
                5 => reader.TryReadBit5(out Bit5 _),
                6 => reader.TryReadBit6(out Bit6 _),
                7 => reader.TryReadBit7(out Bit7 _),
                _ => throw new InvalidOperationException("Unsupported field width."),
            };
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(consumedBits);
    }

    /// <summary>Verifies BitReader TryReadUIntBits InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadUIntBits_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0xFF];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.ReadBits(1);
            ok = reader.TryReadUIntBits(8, out UIntBits _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(1);
    }

    /// <summary>Verifies BitReader TryReadIntBits InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadIntBits_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0xFF];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.ReadBits(5);
            ok = reader.TryReadIntBits(4, out IntBits _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(5);
    }

    /// <summary>Verifies BitReader TryReadInt16 InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadInt16_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0xFF];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.ReadBits(1);
            ok = reader.TryReadInt16(out short _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(1);
    }

    /// <summary>Verifies BitReader TryReadInt64 InsufficientBits ReturnsFalse.</summary>
    [Test]
    public async Task BitReader_TryReadInt64_InsufficientBits_ReturnsFalse()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF];
        bool ok;
        int bitOffset;
        {
            BitReader reader = new(data);
            reader.ReadBits(1);
            ok = reader.TryReadInt64(out long _);
            bitOffset = reader.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(1);
    }
}
