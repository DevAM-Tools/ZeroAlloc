// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

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
public sealed class BitWriterTests
{
    // ========================================================================
    // BASIC WRITE TESTS
    // ========================================================================
    /// <summary>
    /// Tests writing a single aligned byte.
    /// </summary>
    [Test]
    public async Task WriteByte_AlignedWrite()
    {
        byte[] buffer = new byte[1];
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            writer.WriteByte(0xAB);
            bitOffset = writer.BitOffset;
        }

        await Assert.That((int)buffer[0]).IsEqualTo(0xAB);
        await Assert.That(bitOffset).IsEqualTo(8);
    }

    /// <summary>
    /// Tests writing multiple bytes sequentially.
    /// </summary>
    [Test]
    public async Task WriteMultipleBytes_Sequential()
    {
        byte[] buffer = new byte[3];
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            writer.WriteByte(0x12);
            writer.WriteByte(0x34);
            writer.WriteByte(0x56);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(buffer).IsEquivalentTo((byte[])[0x12, 0x34, 0x56]);
        await Assert.That(bitOffset).IsEqualTo(24);
    }

    /// <summary>
    /// Tests writing aligned 16-bit value.
    /// </summary>
    [Test]
    public async Task WriteUInt16_AlignedWrite()
    {
        byte[] buffer = new byte[2];
        {
            BitWriter writer = new(buffer);
            writer.WriteUInt16(0x1234);
        }

        await Assert.That(buffer).IsEquivalentTo((byte[])[0x12, 0x34]);
    }

    /// <summary>
    /// Tests writing aligned 32-bit value.
    /// </summary>
    [Test]
    public async Task WriteUInt32_AlignedWrite()
    {
        byte[] buffer = new byte[4];
        {
            BitWriter writer = new(buffer);
            writer.WriteUInt32(0xDEADBEEF);
        }

        await Assert.That(buffer).IsEquivalentTo((byte[])[0xDE, 0xAD, 0xBE, 0xEF]);
    }

    /// <summary>
    /// Tests writing aligned 64-bit value.
    /// </summary>
    [Test]
    public async Task WriteUInt64_AlignedWrite()
    {
        byte[] buffer = new byte[8];
        {
            BitWriter writer = new(buffer);
            writer.WriteUInt64(0x0102030405060708);
        }

        await Assert.That(buffer).IsEquivalentTo((byte[])[0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);
    }

    // ========================================================================
    // BIT-LEVEL WRITE TESTS
    // ========================================================================

    /// <summary>
    /// Tests writing individual bits.
    /// </summary>
    [Test]
    public async Task WriteBits_SingleBits()
    {
        byte[] buffer = new byte[1];
        {
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
        }

        await Assert.That((int)buffer[0]).IsEqualTo(0b10110100);
    }

    /// <summary>
    /// Tests writing sub-byte fields.
    /// </summary>
    [Test]
    public async Task WriteBits_SubByteFields()
    {
        byte[] buffer = new byte[1];
        {
            BitWriter writer = new(buffer);
            // 3 bits (5=101) + 5 bits (19=10011) = 10110011 = 0xB3
            writer.WriteBits(5, 3);
            writer.WriteBits(19, 5);
        }

        await Assert.That((int)buffer[0]).IsEqualTo(0xB3);
    }

    /// <summary>
    /// Tests writing fields spanning byte boundaries.
    /// </summary>
    [Test]
    public async Task WriteBits_CrossByteBoundary()
    {
        byte[] buffer = new byte[3];
        int bytesWritten;
        {
            BitWriter writer = new(buffer);
            // Write 11-bit identifier (0x7FF = 11111111111)
            writer.WriteBits(0x7FF, 11);
            // Write 1-bit flag
            writer.WriteBits(1, 1);
            // Write 12-bit value (0xABC = 101010111100)
            writer.WriteBits(0xABC, 12);
            bytesWritten = writer.BytesWritten;
        }

        // Total: 24 bits = 3 bytes
        await Assert.That(bytesWritten).IsEqualTo(3);

        // Verify by reading back
        ulong read11, read1, read12;
        {
            BitReader reader = new(buffer);
            read11 = reader.ReadBits(11);
            read1 = reader.ReadBits(1);
            read12 = reader.ReadBits(12);
        }
        await Assert.That(read11).IsEqualTo(0x7FFu);
        await Assert.That(read1).IsEqualTo(1u);
        await Assert.That(read12).IsEqualTo(0xABCu);
    }

    /// <summary>
    /// Tests writing 48-bit value spanning multiple bytes.
    /// </summary>
    [Test]
    public async Task WriteBits_LargeValue()
    {
        byte[] buffer = new byte[6];
        {
            BitWriter writer = new(buffer);
            writer.WriteBits(0xAABBCCDDEEFF, 48);
        }

        await Assert.That(buffer).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF]);
    }

    // ========================================================================
    // ROUNDTRIP WITH BitReader
    // ========================================================================

    /// <summary>
    /// Tests roundtrip: write then read produces same values.
    /// </summary>
    [Test]
    public async Task Roundtrip_MixedBitFields()
    {
        byte[] buffer = new byte[4];
        {
            BitWriter writer = new(buffer);
            writer.WriteBits(5, 3);     // 3 bits
            writer.WriteBits(19, 5);    // 5 bits
            writer.WriteBits(0xABC, 12); // 12 bits
            writer.WriteBits(0xF, 4);   // 4 bits
            writer.WriteByte(0x42);     // 8 bits = 32 bits total
        }
        ulong read3, read5, read12, read4;
        byte readByte;
        {
            BitReader reader = new(buffer);
            read3 = reader.ReadBits(3);
            read5 = reader.ReadBits(5);
            read12 = reader.ReadBits(12);
            read4 = reader.ReadBits(4);
            readByte = reader.ReadByte();
        }
        await Assert.That(read3).IsEqualTo(5u);
        await Assert.That(read5).IsEqualTo(19u);
        await Assert.That(read12).IsEqualTo(0xABCu);
        await Assert.That(read4).IsEqualTo(0xFu);
        await Assert.That(readByte).IsEqualTo((byte)0x42);
    }

    /// <summary>
    /// Tests roundtrip with aligned integer types.
    /// </summary>
    [Test]
    public async Task Roundtrip_AlignedIntegers()
    {
        byte[] buffer = new byte[14];
        {
            BitWriter writer = new(buffer);
            writer.WriteUInt16(0x1234);
            writer.WriteUInt32(0xDEADBEEF);
            writer.WriteUInt64(0x0102030405060708);
        }
        ulong read16, read32, read64;
        {
            BitReader reader = new(buffer);
            read16 = reader.ReadBits(16);
            read32 = reader.ReadBits(32);
            read64 = reader.ReadBits(64);
        }
        await Assert.That(read16).IsEqualTo(0x1234u);
        await Assert.That(read32).IsEqualTo(0xDEADBEEFu);
        await Assert.That(read64).IsEqualTo(0x0102030405060708u);
    }

    /// <summary>
    /// Tests roundtrip with signed integers.
    /// </summary>
    [Test]
    public async Task Roundtrip_SignedIntegers()
    {
        byte[] buffer = new byte[14];
        {
            BitWriter writer = new(buffer);
            writer.WriteInt16(-1234);
            writer.WriteInt32(-100000);
            writer.WriteInt64(-9876543210L);
        }
        short readInt16;
        int readInt32;
        long readInt64;
        {
            BitReader reader = new(buffer);
            readInt16 = reader.ReadInt16();
            readInt32 = reader.ReadInt32();
            readInt64 = reader.ReadInt64();
        }
        await Assert.That(readInt16).IsEqualTo((short)-1234);
        await Assert.That(readInt32).IsEqualTo(-100000);
        await Assert.That(readInt64).IsEqualTo(-9876543210L);
    }

    [Test]
    public async Task Roundtrip_UInt128_PreservesAllBits()
    {
        // Arrange — distinct upper and lower halves
        UInt128 original = new UInt128(0x0102030405060708UL, 0x090A0B0C0D0E0F10UL);
        byte[] buffer = new byte[16];
        {
            BitWriter writer = new(buffer);
            writer.WriteUInt128(original);
        }
        UInt128 result;
        {
            BitReader reader = new(buffer);
            result = reader.ReadUInt128();
        }
        await Assert.That(result).IsEqualTo(original);
    }

    [Test]
    public async Task Roundtrip_Int128_AllOnes_PreservesMinusOne()
    {
        // Arrange — -1 as Int128
        Int128 original = -1;
        byte[] buffer = new byte[16];
        {
            BitWriter writer = new(buffer);
            writer.WriteInt128(original);
        }
        Int128 result;
        {
            BitReader reader = new(buffer);
            result = reader.ReadInt128();
        }
        await Assert.That(result).IsEqualTo(original);
    }

    [Test]
    public async Task Roundtrip_Int128_MaxValue_PreservesValue()
    {
        Int128 original = Int128.MaxValue;
        byte[] buffer = new byte[16];
        {
            BitWriter writer = new(buffer);
            writer.WriteInt128(original);
        }
        Int128 result;
        {
            BitReader reader = new(buffer);
            result = reader.ReadInt128();
        }
        await Assert.That(result).IsEqualTo(original);
    }

    [Test]
    public async Task WriteUInt128_WritesCorrectBytes()
    {
        // Arrange — upper=0x0102030405060708, lower=0x090A0B0C0D0E0F10
        byte[] buffer = new byte[16];
        {
            BitWriter writer = new(buffer);
            writer.WriteUInt128(new UInt128(0x0102030405060708UL, 0x090A0B0C0D0E0F10UL));
        }
        // Assert — bytes written in big-endian order, high half first
        await Assert.That(buffer[0]).IsEqualTo((byte)0x01);
        await Assert.That(buffer[7]).IsEqualTo((byte)0x08);
        await Assert.That(buffer[8]).IsEqualTo((byte)0x09);
        await Assert.That(buffer[15]).IsEqualTo((byte)0x10);
    }

    // ========================================================================
    // ALIGNMENT AND SKIP TESTS
    // ========================================================================

    /// <summary>
    /// Tests SkipBits advances position without writing.
    /// </summary>
    [Test]
    public async Task SkipBits_AdvancesPosition()
    {
        byte[] buffer = new byte[2];
        {
            BitWriter writer = new(buffer);
            writer.WriteBits(1, 1); // Write bit at position 0
            writer.SkipBits(7);     // Skip to byte 1
            writer.WriteByte(0xFF); // Write byte at position 1
        }

        await Assert.That((int)buffer[0]).IsEqualTo(0x80); // Only first bit set
        await Assert.That((int)buffer[1]).IsEqualTo(0xFF);
    }

    /// <summary>
    /// Tests AlignToNextByte pads to byte boundary.
    /// </summary>
    [Test]
    public async Task AlignToNextByte_PadsCorrectly()
    {
        byte[] buffer = new byte[2];
        bool isByteAlignedBefore, isByteAlignedAfter;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            writer.WriteBits(5, 3); // 3 bits
            isByteAlignedBefore = writer.IsByteAligned;
            writer.AlignToNextByte();
            isByteAlignedAfter = writer.IsByteAligned;
            bitOffset = writer.BitOffset;
            writer.WriteByte(0xFF);
        }
        await Assert.That(isByteAlignedBefore).IsFalse();

        await Assert.That(isByteAlignedAfter).IsTrue();
        await Assert.That(bitOffset).IsEqualTo(8);

        await Assert.That((int)buffer[1]).IsEqualTo(0xFF);
    }

    /// <summary>
    /// Tests AlignToNextByte does nothing when already aligned.
    /// </summary>
    [Test]
    public async Task AlignToNextByte_NoOp_WhenAligned()
    {
        byte[] buffer = new byte[2];
        bool isByteAligned;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            writer.WriteByte(0xAA);
            isByteAligned = writer.IsByteAligned;
            writer.AlignToNextByte();
            bitOffset = writer.BitOffset;
        }
        await Assert.That(isByteAligned).IsTrue();

        await Assert.That(bitOffset).IsEqualTo(8); // Unchanged
    }

    // ========================================================================
    // WriteBytes TESTS
    // ========================================================================

    /// <summary>
    /// Tests writing a byte array when aligned.
    /// </summary>
    [Test]
    public async Task WriteBytes_Aligned()
    {
        byte[] buffer = new byte[4];
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            writer.WriteBytes([0xDE, 0xAD, 0xBE, 0xEF]);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(buffer).IsEquivalentTo((byte[])[0xDE, 0xAD, 0xBE, 0xEF]);
        await Assert.That(bitOffset).IsEqualTo(32);
    }

    /// <summary>
    /// Tests WriteBytes throws when not aligned.
    /// </summary>
    [Test]
    public async Task WriteBytes_ThrowsWhenNotAligned()
    {
        byte[] buffer = new byte[4];
        InvalidOperationException ex = null!;
        {
            BitWriter writer = new(buffer);
            writer.WriteBits(1, 1); // Now not byte-aligned
            try
            {
                writer.WriteBytes([0xDE, 0xAD]);
            }
            catch (InvalidOperationException e)
            {
                ex = e;
            }
        }

        await Assert.That(ex).IsNotNull();
    }

    // ========================================================================
    // PROPERTY TESTS
    // ========================================================================

    /// <summary>
    /// Tests BytePosition property.
    /// </summary>
    [Test]
    public async Task BytePosition_TracksCorrectly()
    {
        byte[] buffer = new byte[4];
        int bytePosition0, bytePosition1, bytePosition2, bytePosition3;
        {
            BitWriter writer = new(buffer);
            bytePosition0 = writer.BytePosition;
            writer.WriteBits(1, 3);
            bytePosition1 = writer.BytePosition;
            writer.WriteBits(1, 5);
            bytePosition2 = writer.BytePosition;
            writer.WriteByte(0);
            bytePosition3 = writer.BytePosition;
        }
        await Assert.That(bytePosition0).IsEqualTo(0);

        await Assert.That(bytePosition1).IsEqualTo(0); // Still in first byte

        await Assert.That(bytePosition2).IsEqualTo(1); // Now at second byte

        await Assert.That(bytePosition3).IsEqualTo(2);
    }

    /// <summary>
    /// Tests BytesWritten rounds up for partial bytes.
    /// </summary>
    [Test]
    public async Task BytesWritten_RoundsUp()
    {
        byte[] buffer = new byte[4];
        int bytesWritten0, bytesWritten1, bytesWritten2, bytesWritten3;
        {
            BitWriter writer = new(buffer);
            bytesWritten0 = writer.BytesWritten;
            writer.WriteBits(1, 1);
            bytesWritten1 = writer.BytesWritten;
            writer.WriteBits(0, 7);
            bytesWritten2 = writer.BytesWritten;
            writer.WriteBits(1, 1);
            bytesWritten3 = writer.BytesWritten;
        }
        await Assert.That(bytesWritten0).IsEqualTo(0);

        await Assert.That(bytesWritten1).IsEqualTo(1); // 1 bit → 1 byte

        await Assert.That(bytesWritten2).IsEqualTo(1); // 8 bits → 1 byte

        await Assert.That(bytesWritten3).IsEqualTo(2); // 9 bits → 2 bytes
    }

    /// <summary>
    /// Tests RemainingBits/RemainingBytes properties.
    /// </summary>
    [Test]
    public async Task RemainingBits_TracksCorrectly()
    {
        byte[] buffer = new byte[4];
        int remainingBits0, remainingBytes0, remainingBits1, remainingBytes1;
        {
            BitWriter writer = new(buffer);
            remainingBits0 = writer.RemainingBits;
            remainingBytes0 = writer.RemainingBytes;
            writer.WriteByte(0);
            remainingBits1 = writer.RemainingBits;
            remainingBytes1 = writer.RemainingBytes;
        }
        await Assert.That(remainingBits0).IsEqualTo(32);
        await Assert.That(remainingBytes0).IsEqualTo(4);

        await Assert.That(remainingBits1).IsEqualTo(24);
        await Assert.That(remainingBytes1).IsEqualTo(3);
    }

    // ========================================================================
    // BOUNDARY VALIDATION — WriteBits / WriteByte / SkipBits / WriteBytes
    // ========================================================================

    /// <summary>Verifies WriteBits rejects bitCount below 1.</summary>
    [Test]
    public async Task BitWriter_WriteBits_ZeroBitCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);
        bool threw = false;
        try { writer.WriteBits(0, 0); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies WriteBits rejects bitCount above 64.</summary>
    [Test]
    public async Task BitWriter_WriteBits_TooLargeBitCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] buffer = new byte[16];
        BitWriter writer = new(buffer);
        bool threw = false;
        try { writer.WriteBits(0, 65); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies WriteBits throws when the requested bit count exceeds remaining capacity.</summary>
    [Test]
    public async Task BitWriter_WriteBits_ExceedsCapacity_ThrowsInvalidOperationException()
    {
        // 1-byte buffer = 8 bits available
        byte[] buffer = new byte[1];
        BitWriter writer = new(buffer);
        bool threw = false;
        try { writer.WriteBits(0, 9); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies WriteBits does not mutate the offset when it fails capacity check.</summary>
    [Test]
    public async Task BitWriter_WriteBits_ExceedsCapacity_DoesNotMutateOffset()
    {
        byte[] buffer = new byte[1];
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            try { writer.WriteBits(0, 9); } catch (InvalidOperationException) { /* expected */ }
            bitOffset = writer.BitOffset;
        }

        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies WriteByte aligned fast-path throws when no capacity remains.</summary>
    [Test]
    public async Task BitWriter_WriteByte_NoCapacity_ThrowsInvalidOperationException()
    {
        byte[] buffer = Array.Empty<byte>();
        BitWriter writer = new(buffer);
        bool threw = false;
        try { writer.WriteByte(0xFF); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies WriteByte aligned fast-path does not mutate offset when it fails.</summary>
    [Test]
    public async Task BitWriter_WriteByte_NoCapacity_DoesNotMutateOffset()
    {
        byte[] buffer = Array.Empty<byte>();
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            try { writer.WriteByte(0xFF); } catch (InvalidOperationException) { /* expected */ }
            bitOffset = writer.BitOffset;
        }

        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies SkipBits rejects a negative count.</summary>
    [Test]
    public async Task BitWriter_SkipBits_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);
        bool threw = false;
        try { writer.SkipBits(-1); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies SkipBits throws when the skip would exceed remaining capacity.</summary>
    [Test]
    public async Task BitWriter_SkipBits_ExceedsCapacity_ThrowsInvalidOperationException()
    {
        byte[] buffer = new byte[1];
        BitWriter writer = new(buffer);
        bool threw = false;
        try { writer.SkipBits(9); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies SkipBits does not mutate the offset on failure.</summary>
    [Test]
    public async Task BitWriter_SkipBits_ExceedsCapacity_DoesNotMutateOffset()
    {
        byte[] buffer = new byte[1];
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            try { writer.SkipBits(9); } catch (InvalidOperationException) { /* expected */ }
            bitOffset = writer.BitOffset;
        }

        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies SkipBits of exactly the remaining capacity succeeds without throwing.</summary>
    [Test]
    public async Task BitWriter_SkipBits_ExactCapacity_DoesNotThrow()
    {
        byte[] buffer = new byte[1];
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            writer.SkipBits(8);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(bitOffset).IsEqualTo(8);
    }

    /// <summary>Verifies WriteBytes throws when the data length exceeds remaining byte capacity.</summary>
    [Test]
    public async Task BitWriter_WriteBytes_ExceedsCapacity_ThrowsInvalidOperationException()
    {
        byte[] buffer = new byte[2];
        BitWriter writer = new(buffer);
        bool threw = false;
        try { writer.WriteBytes([0x01, 0x02, 0x03]); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies WriteBytes does not mutate the offset on capacity failure.</summary>
    [Test]
    public async Task BitWriter_WriteBytes_ExceedsCapacity_DoesNotMutateOffset()
    {
        byte[] buffer = new byte[2];
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            try { writer.WriteBytes([0x01, 0x02, 0x03]); } catch (InvalidOperationException) { /* expected */ }
            bitOffset = writer.BitOffset;
        }

        await Assert.That(bitOffset).IsEqualTo(0);
    }

    /// <summary>Verifies WriteBytes still throws the alignment error before the capacity error.</summary>
    [Test]
    public async Task BitWriter_WriteBytes_UnalignedAndOverCapacity_ThrowsAlignmentError()
    {
        byte[] buffer = new byte[4];
        InvalidOperationException? ex = null;
        {
            BitWriter writer = new(buffer);
            writer.WriteBits(1, 3); // 3-bit write → no longer aligned

            // Unaligned check must fire before capacity check
            try
            {
                writer.WriteBytes([0x01, 0x02, 0x03, 0x04, 0x05]);
                Assert.Fail("Expected exception was not thrown");
            }
            catch (InvalidOperationException e)
            {
                ex = e;
            }
        }
        await Assert.That(ex!.Message).Contains("alignment");
    }

    // ========================================================================
    // TRY-WRITE — SUCCESS PATHS
    // ========================================================================

    [Test]
    public async Task BitWriter_TryWriteBits_SufficientCapacity_ReturnsTrueAndWrites()
    {
        byte[] buffer = new byte[1];
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteBits(0b1011, 4);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(bitOffset).IsEqualTo(4);
        await Assert.That((int)buffer[0]).IsEqualTo(0b10110000);
    }

    [Test]
    public async Task BitWriter_TryWriteByte_SufficientCapacity_ReturnsTrueAndWrites()
    {
        byte[] buffer = new byte[1];
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteByte(0xAB);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)buffer[0]).IsEqualTo(0xAB);
        await Assert.That(bitOffset).IsEqualTo(8);
    }

    [Test]
    public async Task BitWriter_TryWriteUInt16_SufficientCapacity_ReturnsTrueAndWrites()
    {
        byte[] buffer = new byte[2];
        bool ok;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteUInt16(0x1234);
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)buffer[0]).IsEqualTo(0x12);
        await Assert.That((int)buffer[1]).IsEqualTo(0x34);
    }

    [Test]
    public async Task BitWriter_TryWriteUInt32_SufficientCapacity_ReturnsTrueAndWrites()
    {
        byte[] buffer = new byte[4];
        bool ok;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteUInt32(0x12345678u);
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)buffer[0]).IsEqualTo(0x12);
        await Assert.That((int)buffer[1]).IsEqualTo(0x34);
        await Assert.That((int)buffer[2]).IsEqualTo(0x56);
        await Assert.That((int)buffer[3]).IsEqualTo(0x78);
    }

    [Test]
    public async Task BitWriter_TryWriteUInt64_SufficientCapacity_ReturnsTrueAndWrites()
    {
        byte[] buffer = new byte[8];
        bool ok;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteUInt64(0x0102030405060708UL);
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)buffer[0]).IsEqualTo(0x01);
        await Assert.That((int)buffer[7]).IsEqualTo(0x08);
    }

    [Test]
    public async Task BitWriter_TryWriteInt16_SufficientCapacity_ReturnsTrueAndWrites()
    {
        byte[] buffer = new byte[2];
        bool ok;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteInt16(-2); // 0xFFFE
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)buffer[0]).IsEqualTo(0xFF);
        await Assert.That((int)buffer[1]).IsEqualTo(0xFE);
    }

    [Test]
    public async Task BitWriter_TryWriteInt32_SufficientCapacity_ReturnsTrueAndWrites()
    {
        byte[] buffer = new byte[4];
        bool ok;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteInt32(-1); // 0xFFFFFFFF
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)buffer[0]).IsEqualTo(0xFF);
        await Assert.That((int)buffer[3]).IsEqualTo(0xFF);
    }

    [Test]
    public async Task BitWriter_TryWriteInt64_SufficientCapacity_ReturnsTrueAndWrites()
    {
        byte[] buffer = new byte[8];
        bool ok;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteInt64(-1L); // 0xFFFFFFFFFFFFFFFF
        }

        await Assert.That(ok).IsTrue();
        for (int i = 0; i < 8; i++)
        {
            await Assert.That((int)buffer[i]).IsEqualTo(0xFF);
        }
    }

    [Test]
    public async Task BitWriter_TrySkipBits_SufficientCapacity_ReturnsTrueAndAdvances()
    {
        byte[] buffer = new byte[2];
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            ok = writer.TrySkipBits(8);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(bitOffset).IsEqualTo(8);
    }

    [Test]
    public async Task BitWriter_TryWriteBytes_AlignedSufficientCapacity_ReturnsTrueAndWrites()
    {
        byte[] buffer = new byte[4];
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteBytes([0xDE, 0xAD]);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That((int)buffer[0]).IsEqualTo(0xDE);
        await Assert.That((int)buffer[1]).IsEqualTo(0xAD);
        await Assert.That(bitOffset).IsEqualTo(16);
    }

    // ========================================================================
    // TRY-WRITE — FAILURE PATHS (insufficient capacity — returns false, no state change)
    // ========================================================================

    [Test]
    public async Task BitWriter_TryWriteBits_InsufficientCapacity_ReturnsFalseAndPreservesState()
    {
        byte[] buffer = new byte[1];
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            writer.TryWriteBits(0b111111, 6); // 6 written, 2 remain
            ok = writer.TryWriteBits(0b111, 3);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(6); // state must not advance
        await Assert.That((int)buffer[0]).IsEqualTo(0b11111100); // already-written bits unchanged
    }

    [Test]
    public async Task BitWriter_TryWriteByte_InsufficientCapacity_ReturnsFalse()
    {
        byte[] buffer = new byte[1];
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            writer.TryWriteBits(0, 4); // 4 bits written, 4 remain
            ok = writer.TryWriteByte(0xFF);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(4);
    }

    [Test]
    public async Task BitWriter_TryWriteUInt16_InsufficientCapacity_ReturnsFalse()
    {
        byte[] buffer = new byte[1]; // only 8 bits
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteUInt16(0x1234);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    [Test]
    public async Task BitWriter_TryWriteUInt32_InsufficientCapacity_ReturnsFalse()
    {
        byte[] buffer = new byte[2]; // only 16 bits
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteUInt32(0x12345678u);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    [Test]
    public async Task BitWriter_TryWriteUInt64_InsufficientCapacity_ReturnsFalse()
    {
        byte[] buffer = new byte[4]; // only 32 bits
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteUInt64(0x0102030405060708UL);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    [Test]
    public async Task BitWriter_TryWriteInt32_InsufficientCapacity_ReturnsFalse()
    {
        byte[] buffer = new byte[2]; // only 16 bits
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteInt32(-1);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    [Test]
    public async Task BitWriter_TrySkipBits_InsufficientCapacity_ReturnsFalseAndPreservesState()
    {
        byte[] buffer = new byte[1];
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            ok = writer.TrySkipBits(9);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    [Test]
    public async Task BitWriter_TrySkipBits_Zero_AlwaysReturnsTrue()
    {
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new([]);
            ok = writer.TrySkipBits(0);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsTrue();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    [Test]
    public async Task BitWriter_TryWriteBytes_NotAligned_ReturnsFalse()
    {
        byte[] buffer = new byte[4];
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            writer.TryWriteBits(0b1, 3); // misalign
            ok = writer.TryWriteBytes([0x01]);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(3); // state must not advance
    }

    [Test]
    public async Task BitWriter_TryWriteBytes_InsufficientCapacity_ReturnsFalse()
    {
        byte[] buffer = new byte[1];
        bool ok;
        int bitOffset;
        {
            BitWriter writer = new(buffer);
            ok = writer.TryWriteBytes([0x01, 0x02]);
            bitOffset = writer.BitOffset;
        }

        await Assert.That(ok).IsFalse();
        await Assert.That(bitOffset).IsEqualTo(0);
    }

    // ========================================================================
    // TRY-WRITE — INVALID ARGUMENT PATHS (programming errors — still throw)
    // ========================================================================

    [Test]
    public async Task BitWriter_TryWriteBits_BitCountZero_ThrowsArgumentOutOfRangeException()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);
        bool threw = false;
        try { writer.TryWriteBits(0, 0); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BitWriter_TryWriteBits_BitCountOver64_ThrowsArgumentOutOfRangeException()
    {
        byte[] buffer = new byte[16];
        BitWriter writer = new(buffer);
        bool threw = false;
        try { writer.TryWriteBits(0, 65); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task BitWriter_TrySkipBits_NegativeCount_ThrowsArgumentOutOfRangeException()
    {
        byte[] buffer = new byte[4];
        BitWriter writer = new(buffer);
        bool threw = false;
        try { writer.TrySkipBits(-1); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }
}
