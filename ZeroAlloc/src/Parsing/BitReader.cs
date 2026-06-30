// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc;

#region BitReader - High-Performance Bit-Level Deserialization

/// <summary>
/// High-performance bit-level reader for parsing non-byte-aligned data.
/// Optimized for automotive (CAN/FlexRay) and network protocols with sub-byte fields.
/// </summary>
public ref struct BitReader
{
    private readonly ReadOnlySpan<byte> _Buffer;
    private int _BitOffset; // Global bit offset from start of buffer

    /// <summary>
    /// Initializes a new BitReader with the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer to read from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitReader(ReadOnlySpan<byte> buffer)
    {
        _Buffer = buffer;
        _BitOffset = 0;
    }

    /// <summary>
    /// Gets the current bit offset from the start of the buffer.
    /// </summary>
    public readonly int BitOffset => _BitOffset;

    /// <summary>
    /// Gets the current byte position (rounded down).
    /// </summary>
    public readonly int BytePosition => _BitOffset >> 3;

    /// <summary>
    /// Gets the bit position within the current byte (0-7).
    /// </summary>
    public readonly int BitPositionInByte => _BitOffset & 7;

    /// <summary>
    /// Gets whether the reader is currently byte-aligned.
    /// </summary>
    public readonly bool IsByteAligned => (_BitOffset & 7) == 0;

    /// <summary>
    /// Reads up to 64 bits as an unsigned integer. Optimized for both aligned and non-aligned reads.
    /// </summary>
    /// <param name="bitCount">Number of bits to read (1-64).</param>
    /// <returns>The unsigned integer value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadBits(int bitCount)
    {
        if (bitCount is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "BitCount must be between 1 and 64");
        }

        // Ensure enough bits remain in the buffer before accessing data
        int totalBits = _Buffer.Length << 3;
        if (_BitOffset + bitCount > totalBits)
        {
            throw new InvalidOperationException(
                $"Cannot read {bitCount} bits: only {totalBits - _BitOffset} bits remaining");
        }

        return _ReadBitsCore(bitCount);
    }

    /// <summary>
    /// Reads <paramref name="bitCount"/> bits without performing any validation.
    /// Callers must ensure <paramref name="bitCount"/> is in range (1–64) and that
    /// sufficient bits remain before calling this method.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong _ReadBitsCore(int bitCount)
    {
        int bytePos = _BitOffset >> 3;
        int bitPos = _BitOffset & 7;
        ulong value = bitPos == 0 ? _ReadBitsAligned(bytePos, bitCount) : _ReadBitsUnaligned(bytePos, bitPos, bitCount);
        _BitOffset += bitCount;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ulong _ReadBitsAligned(int bytePos, int bitCount)
    {
        // Optimized for byte-aligned reads - use direct integer reads when possible
        return bitCount switch
        {
            8 => _Buffer[bytePos],
            16 => BinaryPrimitives.ReadUInt16BigEndian(_Buffer.Slice(bytePos)),
            32 => BinaryPrimitives.ReadUInt32BigEndian(_Buffer.Slice(bytePos)),
            64 => BinaryPrimitives.ReadUInt64BigEndian(_Buffer.Slice(bytePos)),
            _ => _ReadBitsGeneric(bytePos, 0, bitCount)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ulong _ReadBitsUnaligned(int bytePos, int bitPos, int bitCount) => _ReadBitsGeneric(bytePos, bitPos, bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ulong _ReadBitsGeneric(int bytePos, int bitPos, int bitCount)
    {
        ulong result = 0;
        int bitsRemaining = bitCount;

        // Read first partial byte if not aligned
        if (bitPos != 0)
        {
            int bitsInFirstByte = Math.Min(8 - bitPos, bitsRemaining);
            byte mask = (byte)((1 << bitsInFirstByte) - 1);
            byte value = (byte)((_Buffer[bytePos] >> (8 - bitPos - bitsInFirstByte)) & mask);
            result = value;
            bitsRemaining -= bitsInFirstByte;
            bytePos++;
        }

        // Read complete bytes
        while (bitsRemaining >= 8)
        {
            result = (result << 8) | _Buffer[bytePos++];
            bitsRemaining -= 8;
        }

        // Read final partial byte if needed
        if (bitsRemaining > 0)
        {
            byte value = (byte)(_Buffer[bytePos] >> (8 - bitsRemaining));
            result = (result << bitsRemaining) | value;
        }

        return result;
    }

    /// <summary>
    /// Reads a single bit.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit1 ReadBit1()
    {
        if (RemainingBits < 1)
        {
            throw new InvalidOperationException("Cannot read 1 bit: no bits remaining");
        }

        int bytePos = _BitOffset >> 3;
        int bitPos = _BitOffset & 7;
        bool bit = ((_Buffer[bytePos] >> (7 - bitPos)) & 1) == 1;
        _BitOffset++;
        return new Bit1(bit);
    }

    /// <summary>
    /// Reads 2 bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit2 ReadBit2() => new((byte)ReadBits(2));

    /// <summary>
    /// Reads 3 bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit3 ReadBit3() => new((byte)ReadBits(3));

    /// <summary>
    /// Reads a nibble (4 bits).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Nibble ReadNibble() => new((byte)ReadBits(4));

    /// <summary>
    /// Reads 5 bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit5 ReadBit5() => new((byte)ReadBits(5));

    /// <summary>
    /// Reads 6 bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit6 ReadBit6() => new((byte)ReadBits(6));

    /// <summary>
    /// Reads 7 bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Bit7 ReadBit7() => new((byte)ReadBits(7));

    /// <summary>
    /// Reads a variable-length unsigned integer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UIntBits ReadUIntBits(byte bitCount) => new(ReadBits(bitCount), bitCount);

    /// <summary>
    /// Reads a variable-length signed integer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IntBits ReadIntBits(byte bitCount)
    {
        ulong unsigned = ReadBits(bitCount);

        // Sign-extend if needed
        if (bitCount < 64)
        {
            ulong signBit = 1UL << (bitCount - 1);
            if ((unsigned & signBit) != 0)
            {
                // Negative number - sign extend
                ulong mask = ~((1UL << bitCount) - 1);
                unsigned |= mask;
            }
        }

        return new IntBits((long)unsigned, bitCount);
    }

    /// <summary>
    /// Reads standard integer types with optimized paths for byte-aligned reads.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        if (IsByteAligned)
        {
            if (RemainingBits < 8)
            {
                throw new InvalidOperationException(
                    $"Cannot read 8 bits: only {RemainingBits} bits remaining");
            }

            byte value = _Buffer[BytePosition];
            _BitOffset += 8;
            return value;
        }
        return (byte)ReadBits(8);
    }

    /// <summary>Reads a 16-bit unsigned integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16() => (ushort)ReadBits(16);

    /// <summary>Reads a 32-bit unsigned integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32() => (uint)ReadBits(32);

    /// <summary>Reads a 64-bit unsigned integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64() => ReadBits(64);

    /// <summary>Reads a 16-bit signed integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16() => (short)ReadBits(16);

    /// <summary>Reads a 32-bit signed integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32() => (int)ReadBits(32);

    /// <summary>Reads a 64-bit signed integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64() => (long)ReadBits(64);

    /// <summary>
    /// Reads a 128-bit unsigned integer in big-endian bit order.
    /// Reads the upper 64 bits first, then the lower 64 bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UInt128 ReadUInt128()
    {
        ulong high = ReadBits(64);
        ulong low = ReadBits(64);
        return new UInt128(high, low);
    }

    /// <summary>
    /// Reads a 128-bit signed integer in big-endian bit order.
    /// Reads the upper 64 bits first, then the lower 64 bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Int128 ReadInt128()
    {
        ulong high = ReadBits(64);
        ulong low = ReadBits(64);
        return (Int128)new UInt128(high, low);
    }

    /// <summary>
    /// Aligns the reader to the next byte boundary.
    /// If already aligned, does nothing.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AlignToNextByte()
    {
        int remainder = _BitOffset & 7;
        if (remainder != 0)
        {
            _BitOffset += 8 - remainder;
        }
    }

    /// <summary>
    /// Skips the specified number of bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SkipBits(int bitCount)
    {
        if (bitCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "BitCount must be non-negative");
        }

        if (bitCount > RemainingBits)
        {
            throw new InvalidOperationException(
                $"Cannot skip {bitCount} bits: only {RemainingBits} bits remaining");
        }

        _BitOffset += bitCount;
    }

    /// <summary>
    /// Reads a byte array of the specified length. Must be byte-aligned.
    /// </summary>
    /// <param name="byteCount">Number of bytes to read.</param>
    /// <returns>A span containing the requested bytes.</returns>
    public ReadOnlySpan<byte> ReadBytes(int byteCount)
    {
        // Ensure we're byte-aligned
        if ((_BitOffset & 7) != 0)
        {
            throw new InvalidOperationException("ReadBytes requires byte alignment. Call AlignToNextByte() first.");
        }

        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount), "ByteCount must be non-negative");
        }

        if (byteCount > RemainingBytes)
        {
            throw new InvalidOperationException(
                $"Cannot read {byteCount} bytes: only {RemainingBytes} bytes remaining");
        }

        // Capture offset before mutation so state is never partially modified on failure
        int byteOffset = _BitOffset >> 3;
        _BitOffset += byteCount << 3;
        return _Buffer.Slice(byteOffset, byteCount);
    }

    /// <summary>
    /// Gets the remaining bits available in the buffer.
    /// </summary>
    public readonly int RemainingBits => (_Buffer.Length << 3) - _BitOffset;

    /// <summary>
    /// Gets the remaining bytes available in the buffer (rounded down).
    /// </summary>
    public readonly int RemainingBytes => ((_Buffer.Length << 3) - _BitOffset) >> 3;

    #region Try-Read Methods

    /// <summary>
    /// Attempts to read up to 64 bits as an unsigned integer.
    /// Returns <see langword="false"/> without advancing the reader if fewer than <paramref name="bitCount"/> bits remain.
    /// Throws <see cref="ArgumentOutOfRangeException"/> for invalid <paramref name="bitCount"/> values (programming error).
    /// </summary>
    /// <param name="bitCount">Number of bits to read (1–64).</param>
    /// <param name="value">The unsigned integer value read, or <c>default</c> on failure.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBits(int bitCount, out ulong value)
    {
        if (bitCount is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "BitCount must be between 1 and 64");
        }

        if (RemainingBits < bitCount)
        {
            value = default;
            return false;
        }

        value = _ReadBitsCore(bitCount);
        return true;
    }

    /// <summary>
    /// Attempts to read a single bit.
    /// Returns <see langword="false"/> without advancing the reader if no bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBit1(out Bit1 value)
    {
        if (RemainingBits < 1)
        {
            value = default;
            return false;
        }

        // Inline ReadBit1's fast path — avoids a second RemainingBits check.
        int bytePos = _BitOffset >> 3;
        int bitPos = _BitOffset & 7;
        value = new Bit1(((_Buffer[bytePos] >> (7 - bitPos)) & 1) == 1);
        _BitOffset++;
        return true;
    }

    /// <summary>
    /// Attempts to read 2 bits.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 2 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBit2(out Bit2 value)
    {
        if (RemainingBits < 2)
        {
            value = default;
            return false;
        }

        value = new Bit2((byte)_ReadBitsCore(2));
        return true;
    }

    /// <summary>
    /// Attempts to read 3 bits.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 3 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBit3(out Bit3 value)
    {
        if (RemainingBits < 3)
        {
            value = default;
            return false;
        }

        value = new Bit3((byte)_ReadBitsCore(3));
        return true;
    }

    /// <summary>
    /// Attempts to read a nibble (4 bits).
    /// Returns <see langword="false"/> without advancing the reader if fewer than 4 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadNibble(out Nibble value)
    {
        if (RemainingBits < 4)
        {
            value = default;
            return false;
        }

        value = new Nibble((byte)_ReadBitsCore(4));
        return true;
    }

    /// <summary>
    /// Attempts to read 5 bits.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 5 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBit5(out Bit5 value)
    {
        if (RemainingBits < 5)
        {
            value = default;
            return false;
        }

        value = new Bit5((byte)_ReadBitsCore(5));
        return true;
    }

    /// <summary>
    /// Attempts to read 6 bits.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 6 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBit6(out Bit6 value)
    {
        if (RemainingBits < 6)
        {
            value = default;
            return false;
        }

        value = new Bit6((byte)_ReadBitsCore(6));
        return true;
    }

    /// <summary>
    /// Attempts to read 7 bits.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 7 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadBit7(out Bit7 value)
    {
        if (RemainingBits < 7)
        {
            value = default;
            return false;
        }

        value = new Bit7((byte)_ReadBitsCore(7));
        return true;
    }

    /// <summary>
    /// Attempts to read a variable-length unsigned integer of <paramref name="bitCount"/> bits.
    /// Returns <see langword="false"/> without advancing the reader if fewer bits remain.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="bitCount"/> is 0 or greater than 64.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUIntBits(byte bitCount, out UIntBits value)
    {
        if (bitCount is 0 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "BitCount must be between 1 and 64");
        }

        if (RemainingBits < bitCount)
        {
            value = default;
            return false;
        }

        value = new UIntBits(_ReadBitsCore(bitCount), bitCount);
        return true;
    }

    /// <summary>
    /// Attempts to read a variable-length signed integer of <paramref name="bitCount"/> bits.
    /// Returns <see langword="false"/> without advancing the reader if fewer bits remain.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="bitCount"/> is 0 or greater than 64.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadIntBits(byte bitCount, out IntBits value)
    {
        if (bitCount is 0 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "BitCount must be between 1 and 64");
        }

        if (RemainingBits < bitCount)
        {
            value = default;
            return false;
        }

        // Sign-extend after reading — mirrors the behaviour of ReadIntBits, but
        // calls _ReadBitsCore directly to avoid a second RemainingBits check.
        ulong unsigned = _ReadBitsCore(bitCount);
        if (bitCount < 64)
        {
            ulong signBit = 1UL << (bitCount - 1);
            if ((unsigned & signBit) != 0)
            {
                unsigned |= ~((1UL << bitCount) - 1);
            }
        }

        value = new IntBits((long)unsigned, bitCount);
        return true;
    }

    /// <summary>
    /// Attempts to read a byte.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 8 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadByte(out byte value)
    {
        if (RemainingBits < 8)
        {
            value = default;
            return false;
        }

        // Replicate ReadByte's aligned fast path — avoids a second RemainingBits check.
        if (IsByteAligned)
        {
            value = _Buffer[BytePosition];
            _BitOffset += 8;
        }
        else
        {
            value = (byte)_ReadBitsCore(8);
        }

        return true;
    }

    /// <summary>
    /// Attempts to read a 16-bit unsigned integer.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 16 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt16(out ushort value)
    {
        if (RemainingBits < 16)
        {
            value = default;
            return false;
        }

        value = (ushort)_ReadBitsCore(16);
        return true;
    }

    /// <summary>
    /// Attempts to read a 32-bit unsigned integer.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 32 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt32(out uint value)
    {
        if (RemainingBits < 32)
        {
            value = default;
            return false;
        }

        value = (uint)_ReadBitsCore(32);
        return true;
    }

    /// <summary>
    /// Attempts to read a 64-bit unsigned integer.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 64 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadUInt64(out ulong value)
    {
        if (RemainingBits < 64)
        {
            value = default;
            return false;
        }

        value = _ReadBitsCore(64);
        return true;
    }

    /// <summary>
    /// Attempts to read a 16-bit signed integer.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 16 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadInt16(out short value)
    {
        if (RemainingBits < 16)
        {
            value = default;
            return false;
        }

        value = (short)_ReadBitsCore(16);
        return true;
    }

    /// <summary>
    /// Attempts to read a 32-bit signed integer.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 32 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadInt32(out int value)
    {
        if (RemainingBits < 32)
        {
            value = default;
            return false;
        }

        value = (int)_ReadBitsCore(32);
        return true;
    }

    /// <summary>
    /// Attempts to read a 64-bit signed integer.
    /// Returns <see langword="false"/> without advancing the reader if fewer than 64 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryReadInt64(out long value)
    {
        if (RemainingBits < 64)
        {
            value = default;
            return false;
        }

        value = (long)_ReadBitsCore(64);
        return true;
    }

    /// <summary>
    /// Attempts to skip the specified number of bits.
    /// Returns <see langword="false"/> without advancing the reader if fewer bits remain.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="bitCount"/> is negative.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySkipBits(int bitCount)
    {
        if (bitCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "BitCount must be non-negative");
        }

        if (bitCount > RemainingBits)
        {
            return false;
        }

        _BitOffset += bitCount;
        return true;
    }

    /// <summary>
    /// Attempts to read a byte span of the specified length.
    /// Returns <see langword="false"/> without advancing the reader if the reader is not byte-aligned
    /// or fewer than <paramref name="byteCount"/> bytes remain.
    /// Throws <see cref="ArgumentOutOfRangeException"/> if <paramref name="byteCount"/> is negative.
    /// </summary>
    public bool TryReadBytes(int byteCount, out ReadOnlySpan<byte> value)
    {
        if (byteCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(byteCount), "ByteCount must be non-negative");
        }

        if ((_BitOffset & 7) != 0 || byteCount > RemainingBytes)
        {
            value = default;
            return false;
        }

        int byteOffset = _BitOffset >> 3;
        _BitOffset += byteCount << 3;
        value = _Buffer.Slice(byteOffset, byteCount);
        return true;
    }

    #endregion
}

#endregion
