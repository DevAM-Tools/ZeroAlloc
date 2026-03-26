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

        int bytePos = _BitOffset >> 3;
        int bitPos = _BitOffset & 7;

        ulong value;

        // Fast path: byte-aligned reads
        if (bitPos == 0)
        {
            value = ReadBitsAligned(bytePos, bitCount);
        }
        else
        {
            // General path: non-aligned reads
            value = ReadBitsUnaligned(bytePos, bitPos, bitCount);
        }

        _BitOffset += bitCount;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ulong ReadBitsAligned(int bytePos, int bitCount)
    {
        // Optimized for byte-aligned reads - use direct integer reads when possible
        return bitCount switch
        {
            8 => _Buffer[bytePos],
            16 => BinaryPrimitives.ReadUInt16BigEndian(_Buffer.Slice(bytePos)),
            32 => BinaryPrimitives.ReadUInt32BigEndian(_Buffer.Slice(bytePos)),
            64 => BinaryPrimitives.ReadUInt64BigEndian(_Buffer.Slice(bytePos)),
            _ => ReadBitsGeneric(bytePos, 0, bitCount)
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ulong ReadBitsUnaligned(int bytePos, int bitPos, int bitCount) => ReadBitsGeneric(bytePos, bitPos, bitCount);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ulong ReadBitsGeneric(int bytePos, int bitPos, int bitCount)
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
    public void SkipBits(int bitCount) => _BitOffset += bitCount;

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
}

#endregion
