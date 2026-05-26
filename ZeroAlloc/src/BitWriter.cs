// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc;

#region BitWriter - High-Performance Bit-Level Serialization

/// <summary>
/// High-performance bit-level writer for serializing non-byte-aligned data.
/// Write counterpart to <see cref="BitReader"/> for automotive (CAN/FlexRay)
/// and network protocols with sub-byte fields.
/// </summary>
public ref struct BitWriter
{
    private readonly Span<byte> _Buffer;
    private int _BitOffset;

    /// <summary>
    /// Initializes a new BitWriter with the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitWriter(Span<byte> buffer)
    {
        _Buffer = buffer;
        _BitOffset = 0;

        // Clear the buffer to avoid leftover bits from previous data
        buffer.Clear();
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
    /// Gets whether the writer is currently byte-aligned.
    /// </summary>
    public readonly bool IsByteAligned => (_BitOffset & 7) == 0;

    /// <summary>
    /// Gets the number of bytes written so far (rounded up to include partial bytes).
    /// </summary>
    public readonly int BytesWritten => (_BitOffset + 7) >> 3;

    /// <summary>
    /// Writes up to 64 bits from an unsigned integer. Optimized for both aligned and non-aligned writes.
    /// </summary>
    /// <param name="value">The value to write (only the lower <paramref name="bitCount"/> bits are used).</param>
    /// <param name="bitCount">Number of bits to write (1-64).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteBits(ulong value, int bitCount)
    {
        if (bitCount is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "BitCount must be between 1 and 64");
        }

        if (bitCount > RemainingBits)
        {
            throw new InvalidOperationException(
                $"Insufficient buffer capacity: need {bitCount} bits at offset {_BitOffset}, "
                + $"but only {RemainingBits} bits remaining (buffer length: {_Buffer.Length} bytes).");
        }

        WriteBitsCore(value, bitCount);
    }

    /// <summary>
    /// Writes <paramref name="bitCount"/> bits without performing any validation.
    /// Callers must ensure <paramref name="bitCount"/> is in range (1–64) and that
    /// sufficient buffer capacity remains before calling this method.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteBitsCore(ulong value, int bitCount)
    {
        int bytePos = _BitOffset >> 3;
        int bitPos = _BitOffset & 7;

        // Mask the value to only include the relevant bits
        if (bitCount < 64)
        {
            value &= (1UL << bitCount) - 1;
        }

        // Fast path: byte-aligned writes
        if (bitPos == 0)
        {
            WriteBitsAligned(bytePos, value, bitCount);
        }
        else
        {
            // General path: non-aligned writes
            WriteBitsGeneric(bytePos, bitPos, value, bitCount);
        }

        _BitOffset += bitCount;
    }

    /// <summary>
    /// Optimized write for byte-aligned positions using direct integer writes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteBitsAligned(int bytePos, ulong value, int bitCount)
    {
        switch (bitCount)
        {
            case 8:
                _Buffer[bytePos] = (byte)value;
                return;
            case 16:
                BinaryPrimitives.WriteUInt16BigEndian(_Buffer.Slice(bytePos), (ushort)value);
                return;
            case 32:
                BinaryPrimitives.WriteUInt32BigEndian(_Buffer.Slice(bytePos), (uint)value);
                return;
            case 64:
                BinaryPrimitives.WriteUInt64BigEndian(_Buffer.Slice(bytePos), value);
                return;
            default:
                WriteBitsGeneric(bytePos, 0, value, bitCount);
                return;
        }
    }

    /// <summary>
    /// General bit-level write for arbitrary alignment and bit counts.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteBitsGeneric(int bytePos, int bitPos, ulong value, int bitCount)
    {
        int bitsRemaining = bitCount;

        // Write first partial byte if not aligned
        if (bitPos != 0)
        {
            int bitsInFirstByte = Math.Min(8 - bitPos, bitsRemaining);

            // Extract the top bits from the value
            byte bits = (byte)((value >> (bitsRemaining - bitsInFirstByte)) & (ulong)((1 << bitsInFirstByte) - 1));

            // Shift bits into position within the current byte and OR them in
            _Buffer[bytePos] |= (byte)(bits << (8 - bitPos - bitsInFirstByte));

            bitsRemaining -= bitsInFirstByte;
            bytePos++;
        }

        // Write complete bytes
        while (bitsRemaining >= 8)
        {
            bitsRemaining -= 8;
            _Buffer[bytePos++] = (byte)(value >> bitsRemaining);
        }

        // Write final partial byte if needed
        if (bitsRemaining > 0)
        {
            byte bits = (byte)((value & ((1UL << bitsRemaining) - 1)) << (8 - bitsRemaining));
            _Buffer[bytePos] |= bits;
        }
    }

    /// <summary>
    /// Writes a single byte.
    /// </summary>
    /// <param name="value">The byte value to write.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteByte(byte value)
    {
        if (IsByteAligned)
        {
            if (RemainingBits < 8)
            {
                throw new InvalidOperationException(
                    $"Insufficient buffer capacity: need 8 bits at offset {_BitOffset}, "
                    + $"but only {RemainingBits} bits remaining (buffer length: {_Buffer.Length} bytes).");
            }

            _Buffer[BytePosition] = value;
            _BitOffset += 8;
        }
        else
        {
            WriteBits(value, 8);
        }
    }

    /// <summary>
    /// Writes a 16-bit unsigned integer in big-endian order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt16(ushort value) => WriteBits(value, 16);

    /// <summary>
    /// Writes a 32-bit unsigned integer in big-endian order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt32(uint value) => WriteBits(value, 32);

    /// <summary>
    /// Writes a 64-bit unsigned integer in big-endian order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt64(ulong value) => WriteBits(value, 64);

    /// <summary>
    /// Writes a 16-bit signed integer in big-endian order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt16(short value) => WriteBits((ulong)(ushort)value, 16);

    /// <summary>
    /// Writes a 32-bit signed integer in big-endian order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt32(int value) => WriteBits((ulong)(uint)value, 32);

    /// <summary>
    /// Writes a 64-bit signed integer in big-endian order.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt64(long value) => WriteBits((ulong)value, 64);

    /// <summary>
    /// Writes a 128-bit unsigned integer in big-endian bit order.
    /// Writes the upper 64 bits first, then the lower 64 bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteUInt128(UInt128 value)
    {
        WriteBits((ulong)(value >> 64), 64);
        WriteBits((ulong)value, 64);
    }

    /// <summary>
    /// Writes a 128-bit signed integer in big-endian bit order.
    /// Writes the upper 64 bits first, then the lower 64 bits.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteInt128(Int128 value)
    {
        UInt128 bits = (UInt128)value;
        WriteBits((ulong)(bits >> 64), 64);
        WriteBits((ulong)bits, 64);
    }

    /// <summary>
    /// Aligns the writer to the next byte boundary by writing zero bits.
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
    /// Skips the specified number of bits (writes zeros).
    /// </summary>
    /// <param name="bitCount">Number of bits to skip.</param>
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
                $"Insufficient buffer capacity: need {bitCount} bits at offset {_BitOffset}, "
                + $"but only {RemainingBits} bits remaining (buffer length: {_Buffer.Length} bytes).");
        }

        _BitOffset += bitCount;
    }

    /// <summary>
    /// Writes a byte array. Must be byte-aligned.
    /// </summary>
    /// <param name="data">The bytes to write.</param>
    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        if ((_BitOffset & 7) != 0)
        {
            throw new InvalidOperationException("WriteBytes requires byte alignment. Call AlignToNextByte() first.");
        }

        if (data.Length > RemainingBytes)
        {
            throw new InvalidOperationException(
                $"Insufficient buffer capacity: need {data.Length} bytes at byte offset {BytePosition}, "
                + $"but only {RemainingBytes} bytes remaining (buffer length: {_Buffer.Length} bytes).");
        }

        int byteOffset = _BitOffset >> 3;
        data.CopyTo(_Buffer.Slice(byteOffset));
        _BitOffset += data.Length << 3;
    }

    /// <summary>
    /// Gets the remaining bits available in the buffer.
    /// </summary>
    public readonly int RemainingBits => (_Buffer.Length << 3) - _BitOffset;

    /// <summary>
    /// Gets the remaining bytes available in the buffer (rounded down).
    /// </summary>
    public readonly int RemainingBytes => ((_Buffer.Length << 3) - _BitOffset) >> 3;

    #region Try-Write Methods

    /// <summary>
    /// Attempts to write up to 64 bits from an unsigned integer.
    /// Returns <see langword="false"/> without modifying the buffer if fewer than <paramref name="bitCount"/> bits remain.
    /// Throws <see cref="ArgumentOutOfRangeException"/> for invalid <paramref name="bitCount"/> values (programming error).
    /// </summary>
    /// <param name="value">The value to write (only the lower <paramref name="bitCount"/> bits are used).</param>
    /// <param name="bitCount">Number of bits to write (1–64).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteBits(ulong value, int bitCount)
    {
        if (bitCount is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "BitCount must be between 1 and 64");
        }

        if (bitCount > RemainingBits)
        {
            return false;
        }

        WriteBitsCore(value, bitCount);
        return true;
    }

    /// <summary>
    /// Attempts to write a single byte.
    /// Returns <see langword="false"/> without modifying the buffer if fewer than 8 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteByte(byte value)
    {
        if (RemainingBits < 8)
        {
            return false;
        }

        // Replicate WriteByte's aligned fast path — avoids a second RemainingBits check.
        if (IsByteAligned)
        {
            _Buffer[BytePosition] = value;
            _BitOffset += 8;
        }
        else
        {
            WriteBitsCore(value, 8);
        }

        return true;
    }

    /// <summary>
    /// Attempts to write a 16-bit unsigned integer in big-endian order.
    /// Returns <see langword="false"/> without modifying the buffer if fewer than 16 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteUInt16(ushort value)
    {
        if (RemainingBits < 16)
        {
            return false;
        }

        WriteBitsCore(value, 16);
        return true;
    }

    /// <summary>
    /// Attempts to write a 32-bit unsigned integer in big-endian order.
    /// Returns <see langword="false"/> without modifying the buffer if fewer than 32 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteUInt32(uint value)
    {
        if (RemainingBits < 32)
        {
            return false;
        }

        WriteBitsCore(value, 32);
        return true;
    }

    /// <summary>
    /// Attempts to write a 64-bit unsigned integer in big-endian order.
    /// Returns <see langword="false"/> without modifying the buffer if fewer than 64 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteUInt64(ulong value)
    {
        if (RemainingBits < 64)
        {
            return false;
        }

        WriteBitsCore(value, 64);
        return true;
    }

    /// <summary>
    /// Attempts to write a 16-bit signed integer in big-endian order.
    /// Returns <see langword="false"/> without modifying the buffer if fewer than 16 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteInt16(short value)
    {
        if (RemainingBits < 16)
        {
            return false;
        }

        WriteBitsCore((ulong)(ushort)value, 16);
        return true;
    }

    /// <summary>
    /// Attempts to write a 32-bit signed integer in big-endian order.
    /// Returns <see langword="false"/> without modifying the buffer if fewer than 32 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteInt32(int value)
    {
        if (RemainingBits < 32)
        {
            return false;
        }

        WriteBitsCore((ulong)(uint)value, 32);
        return true;
    }

    /// <summary>
    /// Attempts to write a 64-bit signed integer in big-endian order.
    /// Returns <see langword="false"/> without modifying the buffer if fewer than 64 bits remain.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWriteInt64(long value)
    {
        if (RemainingBits < 64)
        {
            return false;
        }

        WriteBitsCore((ulong)value, 64);
        return true;
    }

    /// <summary>
    /// Attempts to skip (zero-fill) the specified number of bits.
    /// Returns <see langword="false"/> without modifying the buffer if fewer bits remain.
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
    /// Attempts to write a byte span. Must be byte-aligned.
    /// Returns <see langword="false"/> without modifying the buffer if the writer is not byte-aligned
    /// or insufficient bytes remain.
    /// </summary>
    public bool TryWriteBytes(ReadOnlySpan<byte> data)
    {
        if ((_BitOffset & 7) != 0 || data.Length > RemainingBytes)
        {
            return false;
        }

        int byteOffset = _BitOffset >> 3;
        data.CopyTo(_Buffer.Slice(byteOffset));
        _BitOffset += data.Length << 3;
        return true;
    }

    #endregion
}

#endregion
