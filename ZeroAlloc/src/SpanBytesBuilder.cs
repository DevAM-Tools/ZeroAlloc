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
// ZeroAlloc - SpanBytesBuilder: Span-Based Byte Builder
// ============================================================================
// A byte builder that writes directly to a user-provided Span<byte>.
// For maximum performance with known buffer sizes. No dispose needed.
//
// Example:
//   Span<byte> buffer = stackalloc byte[256];
//   var builder = new SpanBytesBuilder(buffer);
//   builder.AppendUInt16BigEndian(0x1234);
//   builder.AppendUtf8("Hello");
//   ReadOnlySpan<byte> result = builder.AsSpan();
// ============================================================================

namespace ZeroAlloc;

/// <summary>
/// A span-based byte builder that writes directly to a user-provided <see cref="Span{Byte}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="SpanBytesBuilder"/> when you know the maximum buffer size upfront
/// and want to avoid ThreadStatic buffer overhead. The builder throws 
/// <see cref="InvalidOperationException"/> if the buffer overflows.
/// </para>
/// <para><b>Example:</b></para>
/// <code>
/// Span&lt;byte&gt; buffer = stackalloc byte[256];
/// var builder = new SpanBytesBuilder(buffer);
/// builder.AppendUInt16BigEndian(0x1234);
/// builder.AppendUtf8("Hello");
/// ReadOnlySpan&lt;byte&gt; result = builder.AsSpan();
/// </code>
/// </remarks>
public ref struct SpanBytesBuilder
{
    private Span<byte> _Buffer;
    private int _Position;

    /// <summary>
    /// Initializes a new instance with the given buffer.
    /// </summary>
    /// <param name="buffer">The destination buffer to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanBytesBuilder(Span<byte> buffer)
    {
        _Buffer = buffer;
        _Position = 0;
    }

    /// <summary>Gets the current position (number of bytes written).</summary>
    public readonly int Length => _Position;

    /// <summary>Gets the current capacity of the buffer.</summary>
    public readonly int Capacity => _Buffer.Length;

    /// <summary>Gets the remaining capacity in the buffer.</summary>
    public readonly int Remaining => _Buffer.Length - _Position;

    /// <summary>Gets the written content as a read-only span.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> AsSpan() => _Buffer.Slice(0, _Position);

    /// <summary>Clears the builder, resetting the position to zero.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _Position = 0;

    /// <summary>
    /// Moves the write position back by the specified number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to move back.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is negative or greater than the current position.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SeekBack(int count)
    {
        if ((uint)count > (uint)_Position)
        {
            throw new ArgumentOutOfRangeException(nameof(count),
                $"Cannot seek back {count} bytes when only {_Position} bytes have been written.");
        }

        _Position -= count;
    }

    /// <summary>
    /// Tries to move the write position back by the specified number of bytes without throwing.
    /// </summary>
    /// <param name="count">The number of bytes to move back.</param>
    /// <returns>True if successful; false if count is invalid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySeekBack(int count)
    {
        if ((uint)count > (uint)_Position)
        {
            return false;
        }

        _Position -= count;
        return true;
    }

    /// <summary>Gets the remaining span that can be written to.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly Span<byte> GetRemainingSpan() => _Buffer.Slice(_Position);

    /// <summary>Advances the position by the specified number of bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => _Position += count;

    // ========================================================================
    // APPEND - RAW BYTES
    // ========================================================================

    /// <summary>Appends a single byte to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(byte value) => _Buffer[_Position++] = value;

    /// <summary>Appends a byte array to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(byte[]? value)
    {
        if (value is null)
        {
            return;
        }

        Append(value.AsSpan());
    }

    /// <summary>Appends a byte span to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<byte> value)
    {
        value.CopyTo(_Buffer.Slice(_Position));
        _Position += value.Length;
    }

    // ========================================================================
    // APPEND - BIG-ENDIAN INTEGERS
    // ========================================================================

    /// <summary>Appends a 16-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt16BigEndian(short value)
    {
        BinaryPrimitives.WriteInt16BigEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a 32-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt32BigEndian(int value)
    {
        BinaryPrimitives.WriteInt32BigEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a 64-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt64BigEndian(long value)
    {
        BinaryPrimitives.WriteInt64BigEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
    }

    /// <summary>Appends a 128-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt128BigEndian(Int128 value)
    {
        BinaryPrimitives.WriteInt128BigEndian(_Buffer.Slice(_Position), value);
        _Position += 16;
    }

    /// <summary>Appends a 16-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt16BigEndian(ushort value)
    {
        BinaryPrimitives.WriteUInt16BigEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a 32-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt32BigEndian(uint value)
    {
        BinaryPrimitives.WriteUInt32BigEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a 64-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt64BigEndian(ulong value)
    {
        BinaryPrimitives.WriteUInt64BigEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
    }

    /// <summary>Appends a 128-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt128BigEndian(UInt128 value)
    {
        BinaryPrimitives.WriteUInt128BigEndian(_Buffer.Slice(_Position), value);
        _Position += 16;
    }

    // ========================================================================
    // APPEND - LITTLE-ENDIAN INTEGERS
    // ========================================================================

    /// <summary>Appends a 16-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt16LittleEndian(short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a 32-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt32LittleEndian(int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a 64-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt64LittleEndian(long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
    }

    /// <summary>Appends a 128-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt128LittleEndian(Int128 value)
    {
        BinaryPrimitives.WriteInt128LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 16;
    }

    /// <summary>Appends a 16-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt16LittleEndian(ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a 32-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt32LittleEndian(uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a 64-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt64LittleEndian(ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
    }

    /// <summary>Appends a 128-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt128LittleEndian(UInt128 value)
    {
        BinaryPrimitives.WriteUInt128LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 16;
    }

    // ========================================================================
    // APPEND - FLOATING POINT
    // ========================================================================

    /// <summary>Appends a half-precision float in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHalfBigEndian(Half value)
    {
        BinaryPrimitives.WriteHalfBigEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a half-precision float in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHalfLittleEndian(Half value)
    {
        BinaryPrimitives.WriteHalfLittleEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a single-precision float in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendSingleBigEndian(float value)
    {
        BinaryPrimitives.WriteSingleBigEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a single-precision float in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendSingleLittleEndian(float value)
    {
        BinaryPrimitives.WriteSingleLittleEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a double-precision float in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendDoubleBigEndian(double value)
    {
        BinaryPrimitives.WriteDoubleBigEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
    }

    /// <summary>Appends a double-precision float in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendDoubleLittleEndian(double value)
    {
        BinaryPrimitives.WriteDoubleLittleEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
    }

    // ========================================================================
    // APPEND - VARINT
    // ========================================================================

    /// <summary>Appends a variable-length unsigned integer (7-bit encoding).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendVarInt(ulong value)
    {
        while (value >= 0x80)
        {
            _Buffer[_Position++] = (byte)(value | 0x80);
            value >>= 7;
        }
        _Buffer[_Position++] = (byte)value;
    }

    /// <summary>Appends a variable-length signed integer using ZigZag encoding.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendVarIntZigZag(long value)
    {
        ulong zigzag = (ulong)((value << 1) ^ (value >> 63));
        AppendVarInt(zigzag);
    }

    /// <summary>Appends a variable-length signed 32-bit integer using ZigZag encoding.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendVarIntZigZag(int value)
    {
        uint zigzag = (uint)((value << 1) ^ (value >> 31));
        AppendVarInt(zigzag);
    }

    // ========================================================================
    // APPEND - UTF-8 STRINGS
    // ========================================================================

    /// <summary>Appends a string as raw UTF-8 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUtf8(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        int written = Encoding.UTF8.GetBytes(value, _Buffer.Slice(_Position));
        _Position += written;
    }

    /// <summary>Appends a string as UTF-8 bytes followed by a null terminator.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUtf8NullTerminated(string? value)
    {
        AppendUtf8(value);
        Append((byte)0);
    }

    /// <summary>Appends a string with a VarInt length prefix followed by UTF-8 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUtf8WithVarIntPrefix(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            AppendVarInt(0);
            return;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        AppendVarInt((ulong)byteCount);
        int written = Encoding.UTF8.GetBytes(value, _Buffer.Slice(_Position));
        _Position += written;
    }

    /// <summary>Appends a string with a 4-byte big-endian length prefix followed by UTF-8 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUtf8WithLengthPrefixBE(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            AppendUInt32BigEndian(0);
            return;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        AppendUInt32BigEndian((uint)byteCount);
        int written = Encoding.UTF8.GetBytes(value, _Buffer.Slice(_Position));
        _Position += written;
    }

    /// <summary>Appends a string with a 4-byte little-endian length prefix followed by UTF-8 bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUtf8WithLengthPrefixLE(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            AppendUInt32LittleEndian(0);
            return;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        AppendUInt32LittleEndian((uint)byteCount);
        int written = Encoding.UTF8.GetBytes(value, _Buffer.Slice(_Position));
        _Position += written;
    }

    // ========================================================================
    // APPEND - WRAPPER TYPES
    // ========================================================================

    /// <summary>Appends a U16BE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(U16BE value) => AppendUInt16BigEndian(value.Value);

    /// <summary>Appends a U32BE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(U32BE value) => AppendUInt32BigEndian(value.Value);

    /// <summary>Appends a U64BE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(U64BE value) => AppendUInt64BigEndian(value.Value);

    /// <summary>Appends a U16LE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(U16LE value) => AppendUInt16LittleEndian(value.Value);

    /// <summary>Appends a U32LE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(U32LE value) => AppendUInt32LittleEndian(value.Value);

    /// <summary>Appends a U64LE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(U64LE value) => AppendUInt64LittleEndian(value.Value);

    /// <summary>Appends an I16BE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(I16BE value) => AppendInt16BigEndian(value.Value);

    /// <summary>Appends an I32BE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(I32BE value) => AppendInt32BigEndian(value.Value);

    /// <summary>Appends an I64BE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(I64BE value) => AppendInt64BigEndian(value.Value);

    /// <summary>Appends an I16LE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(I16LE value) => AppendInt16LittleEndian(value.Value);

    /// <summary>Appends an I32LE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(I32LE value) => AppendInt32LittleEndian(value.Value);

    /// <summary>Appends an I64LE wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(I64LE value) => AppendInt64LittleEndian(value.Value);

    /// <summary>Appends a VarInt wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(VarInt value) => AppendVarInt(value.Value);

    /// <summary>Appends a VarIntZigZag wrapper.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(VarIntZigZag value) => AppendVarIntZigZag(value.Value);

    // ========================================================================
    // APPEND - IBINARYSERIALIZABLE
    // ========================================================================

    /// <summary>Appends any type implementing IBinarySerializable.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append<T>(T value) where T : IBinarySerializable
    {
        if (!value.TryWrite(_Buffer.Slice(_Position), out int bytesWritten))
        {
            throw new InvalidOperationException($"Buffer too small for {typeof(T).Name}.");
        }
        _Position += bytesWritten;
    }

    // ========================================================================
    // TRYAPPEND - NON-THROWING VARIANTS
    // ========================================================================

    /// <summary>Tries to append a single byte without throwing.</summary>
    /// <param name="value">The byte to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(byte value)
    {
        if (Remaining < 1)
        {
            return false;
        }

        _Buffer[_Position++] = value;
        return true;
    }

    /// <summary>Tries to append a byte array without throwing.</summary>
    /// <param name="value">The byte array to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(byte[]? value)
    {
        if (value is null)
        {
            return true;
        }

        return TryAppend(value.AsSpan());
    }

    /// <summary>Tries to append a byte span without throwing.</summary>
    /// <param name="value">The byte span to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(ReadOnlySpan<byte> value)
    {
        if (Remaining < value.Length)
        {
            return false;
        }

        value.CopyTo(_Buffer.Slice(_Position));
        _Position += value.Length;
        return true;
    }

    /// <summary>Tries to append a 16-bit signed integer in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendInt16BigEndian(short value)
    {
        if (Remaining < 2)
        {
            return false;
        }

        BinaryPrimitives.WriteInt16BigEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
        return true;
    }

    /// <summary>Tries to append a 32-bit signed integer in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendInt32BigEndian(int value)
    {
        if (Remaining < 4)
        {
            return false;
        }

        BinaryPrimitives.WriteInt32BigEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
        return true;
    }

    /// <summary>Tries to append a 64-bit signed integer in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendInt64BigEndian(long value)
    {
        if (Remaining < 8)
        {
            return false;
        }

        BinaryPrimitives.WriteInt64BigEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
        return true;
    }

    /// <summary>Tries to append a 128-bit signed integer in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendInt128BigEndian(Int128 value)
    {
        if (Remaining < 16)
        {
            return false;
        }

        BinaryPrimitives.WriteInt128BigEndian(_Buffer.Slice(_Position), value);
        _Position += 16;
        return true;
    }

    /// <summary>Tries to append a 16-bit unsigned integer in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUInt16BigEndian(ushort value)
    {
        if (Remaining < 2)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt16BigEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
        return true;
    }

    /// <summary>Tries to append a 32-bit unsigned integer in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUInt32BigEndian(uint value)
    {
        if (Remaining < 4)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt32BigEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
        return true;
    }

    /// <summary>Tries to append a 64-bit unsigned integer in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUInt64BigEndian(ulong value)
    {
        if (Remaining < 8)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt64BigEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
        return true;
    }

    /// <summary>Tries to append a 128-bit unsigned integer in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUInt128BigEndian(UInt128 value)
    {
        if (Remaining < 16)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt128BigEndian(_Buffer.Slice(_Position), value);
        _Position += 16;
        return true;
    }

    /// <summary>Tries to append a 16-bit signed integer in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendInt16LittleEndian(short value)
    {
        if (Remaining < 2)
        {
            return false;
        }

        BinaryPrimitives.WriteInt16LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
        return true;
    }

    /// <summary>Tries to append a 32-bit signed integer in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendInt32LittleEndian(int value)
    {
        if (Remaining < 4)
        {
            return false;
        }

        BinaryPrimitives.WriteInt32LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
        return true;
    }

    /// <summary>Tries to append a 64-bit signed integer in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendInt64LittleEndian(long value)
    {
        if (Remaining < 8)
        {
            return false;
        }

        BinaryPrimitives.WriteInt64LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
        return true;
    }

    /// <summary>Tries to append a 128-bit signed integer in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendInt128LittleEndian(Int128 value)
    {
        if (Remaining < 16)
        {
            return false;
        }

        BinaryPrimitives.WriteInt128LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 16;
        return true;
    }

    /// <summary>Tries to append a 16-bit unsigned integer in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUInt16LittleEndian(ushort value)
    {
        if (Remaining < 2)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt16LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
        return true;
    }

    /// <summary>Tries to append a 32-bit unsigned integer in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUInt32LittleEndian(uint value)
    {
        if (Remaining < 4)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt32LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
        return true;
    }

    /// <summary>Tries to append a 64-bit unsigned integer in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUInt64LittleEndian(ulong value)
    {
        if (Remaining < 8)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt64LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
        return true;
    }

    /// <summary>Tries to append a 128-bit unsigned integer in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUInt128LittleEndian(UInt128 value)
    {
        if (Remaining < 16)
        {
            return false;
        }

        BinaryPrimitives.WriteUInt128LittleEndian(_Buffer.Slice(_Position), value);
        _Position += 16;
        return true;
    }

    /// <summary>Tries to append a half-precision float in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHalfBigEndian(Half value)
    {
        if (Remaining < 2)
        {
            return false;
        }

        BinaryPrimitives.WriteHalfBigEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
        return true;
    }

    /// <summary>Tries to append a half-precision float in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHalfLittleEndian(Half value)
    {
        if (Remaining < 2)
        {
            return false;
        }

        BinaryPrimitives.WriteHalfLittleEndian(_Buffer.Slice(_Position), value);
        _Position += 2;
        return true;
    }

    /// <summary>Tries to append a single-precision float in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendSingleBigEndian(float value)
    {
        if (Remaining < 4)
        {
            return false;
        }

        BinaryPrimitives.WriteSingleBigEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
        return true;
    }

    /// <summary>Tries to append a single-precision float in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendSingleLittleEndian(float value)
    {
        if (Remaining < 4)
        {
            return false;
        }

        BinaryPrimitives.WriteSingleLittleEndian(_Buffer.Slice(_Position), value);
        _Position += 4;
        return true;
    }

    /// <summary>Tries to append a double-precision float in big-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendDoubleBigEndian(double value)
    {
        if (Remaining < 8)
        {
            return false;
        }

        BinaryPrimitives.WriteDoubleBigEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
        return true;
    }

    /// <summary>Tries to append a double-precision float in little-endian format without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendDoubleLittleEndian(double value)
    {
        if (Remaining < 8)
        {
            return false;
        }

        BinaryPrimitives.WriteDoubleLittleEndian(_Buffer.Slice(_Position), value);
        _Position += 8;
        return true;
    }

    /// <summary>Tries to append a variable-length unsigned integer without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendVarInt(ulong value)
    {
        // Calculate required bytes (1-10 bytes for ulong)
        int requiredBytes = 1;
        ulong temp = value;
        while (temp >= 0x80)
        {
            requiredBytes++;
            temp >>= 7;
        }

        if (Remaining < requiredBytes)
        {
            return false;
        }

        while (value >= 0x80)
        {
            _Buffer[_Position++] = (byte)(value | 0x80);
            value >>= 7;
        }
        _Buffer[_Position++] = (byte)value;
        return true;
    }

    /// <summary>Tries to append a variable-length signed integer using ZigZag encoding without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendVarIntZigZag(long value)
    {
        ulong zigzag = (ulong)((value << 1) ^ (value >> 63));
        return TryAppendVarInt(zigzag);
    }

    /// <summary>Tries to append a variable-length signed 32-bit integer using ZigZag encoding without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendVarIntZigZag(int value)
    {
        uint zigzag = (uint)((value << 1) ^ (value >> 31));
        return TryAppendVarInt(zigzag);
    }

    /// <summary>Tries to append a string as raw UTF-8 bytes without throwing.</summary>
    /// <param name="value">The string to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUtf8(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        int byteCount = Encoding.UTF8.GetByteCount(value);
        if (Remaining < byteCount)
        {
            return false;
        }

        int written = Encoding.UTF8.GetBytes(value, _Buffer.Slice(_Position));
        _Position += written;
        return true;
    }

    /// <summary>Tries to append a string as UTF-8 bytes followed by a null terminator without throwing.</summary>
    /// <param name="value">The string to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUtf8NullTerminated(string? value)
    {
        int byteCount = string.IsNullOrEmpty(value) ? 1 : Encoding.UTF8.GetByteCount(value) + 1;
        if (Remaining < byteCount)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(value))
        {
            int written = Encoding.UTF8.GetBytes(value, _Buffer.Slice(_Position));
            _Position += written;
        }
        _Buffer[_Position++] = 0;
        return true;
    }

    /// <summary>Tries to append any type implementing IBinarySerializable without throwing.</summary>
    /// <typeparam name="T">The type implementing IBinarySerializable.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend<T>(T value) where T : IBinarySerializable
    {
        if (!value.TryWrite(_Buffer.Slice(_Position), out int bytesWritten))
        {
            return false;
        }

        _Position += bytesWritten;
        return true;
    }

    /// <summary>Tries to append any IUtf8SpanFormattable value without throwing.</summary>
    /// <typeparam name="T">The type implementing IUtf8SpanFormattable.</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUtf8Formattable<T>(T value, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int bytesWritten, format, provider))
        {
            return false;
        }

        _Position += bytesWritten;
        return true;
    }

    // ========================================================================
    // APPEND - UTF8 FORMATTABLE
    // ========================================================================

    /// <summary>Appends any IUtf8SpanFormattable value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUtf8Formattable<T>(T value, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int bytesWritten, format, provider))
        {
            throw new InvalidOperationException($"Buffer too small for {typeof(T).Name}.");
        }
        _Position += bytesWritten;
    }

    // ========================================================================
    // HEX/BINARY FORMATTING (ASCII OUTPUT)
    // ========================================================================

    private static ReadOnlySpan<byte> HexCharsBytes => "0123456789ABCDEF"u8;

    /// <summary>Appends a byte as 2 hexadecimal ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex2(byte value)
    {
        _Buffer[_Position++] = HexCharsBytes[value >> 4];
        _Buffer[_Position++] = HexCharsBytes[value & 0xF];
    }

    /// <summary>Appends a ushort as 4 hexadecimal ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex4(ushort value)
    {
        _Buffer[_Position++] = HexCharsBytes[(value >> 12) & 0xF];
        _Buffer[_Position++] = HexCharsBytes[(value >> 8) & 0xF];
        _Buffer[_Position++] = HexCharsBytes[(value >> 4) & 0xF];
        _Buffer[_Position++] = HexCharsBytes[value & 0xF];
    }

    /// <summary>Appends a uint as 8 hexadecimal ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex8(uint value)
    {
        AppendHex4((ushort)(value >> 16));
        AppendHex4((ushort)(value & 0xFFFF));
    }

    /// <summary>Appends a ulong as 16 hexadecimal ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex16(ulong value)
    {
        AppendHex8((uint)(value >> 32));
        AppendHex8((uint)(value & 0xFFFFFFFF));
    }

    /// <summary>Appends a byte as 8 binary ASCII bytes ('0' or '1').</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary8(byte value)
    {
        for (int i = 7; i >= 0; i--)
        {
            _Buffer[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
    }

    /// <summary>Appends a ushort as 16 binary ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary16(ushort value)
    {
        for (int i = 15; i >= 0; i--)
        {
            _Buffer[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
    }

    /// <summary>Appends a uint as 32 binary ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary32(uint value)
    {
        for (int i = 31; i >= 0; i--)
        {
            _Buffer[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
    }

    /// <summary>Appends a ulong as 64 binary ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary64(ulong value)
    {
        for (int i = 63; i >= 0; i--)
        {
            _Buffer[_Position++] = (byte)('0' + (int)((value >> i) & 1));
        }
    }

    /// <summary>Tries to append a byte as 2 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The byte value to format.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex2(byte value)
    {
        if (Remaining < 2)
        {
            return false;
        }

        _Buffer[_Position++] = HexCharsBytes[value >> 4];
        _Buffer[_Position++] = HexCharsBytes[value & 0xF];
        return true;
    }

    /// <summary>Tries to append a ushort as 4 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex4(ushort value)
    {
        if (Remaining < 4)
        {
            return false;
        }

        _Buffer[_Position++] = HexCharsBytes[(value >> 12) & 0xF];
        _Buffer[_Position++] = HexCharsBytes[(value >> 8) & 0xF];
        _Buffer[_Position++] = HexCharsBytes[(value >> 4) & 0xF];
        _Buffer[_Position++] = HexCharsBytes[value & 0xF];
        return true;
    }

    /// <summary>Tries to append a uint as 8 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex8(uint value)
    {
        if (Remaining < 8)
        {
            return false;
        }

        AppendHex4((ushort)(value >> 16));
        AppendHex4((ushort)(value & 0xFFFF));
        return true;
    }

    /// <summary>Tries to append a ulong as 16 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex16(ulong value)
    {
        if (Remaining < 16)
        {
            return false;
        }

        AppendHex8((uint)(value >> 32));
        AppendHex8((uint)(value & 0xFFFFFFFF));
        return true;
    }

    /// <summary>Tries to append a byte as 8 binary ASCII characters without throwing.</summary>
    /// <param name="value">The byte value to format.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary8(byte value)
    {
        if (Remaining < 8)
        {
            return false;
        }

        for (int i = 7; i >= 0; i--)
        {
            _Buffer[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a ushort as 16 binary ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary16(ushort value)
    {
        if (Remaining < 16)
        {
            return false;
        }

        for (int i = 15; i >= 0; i--)
        {
            _Buffer[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a uint as 32 binary ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary32(uint value)
    {
        if (Remaining < 32)
        {
            return false;
        }

        for (int i = 31; i >= 0; i--)
        {
            _Buffer[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a ulong as 64 binary ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary64(ulong value)
    {
        if (Remaining < 64)
        {
            return false;
        }

        for (int i = 63; i >= 0; i--)
        {
            _Buffer[_Position++] = (byte)('0' + (int)((value >> i) & 1));
        }
        return true;
    }

    // ========================================================================
    // OUTPUT
    // ========================================================================

    /// <summary>Gets the written content as a read-only span.</summary>
    public readonly ReadOnlySpan<byte> WrittenSpan => _Buffer.Slice(0, _Position);

    /// <summary>Creates a heap-allocated byte array from the builder content.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte[] ToArray()
    {
        if (_Position == 0)
        {
            return Array.Empty<byte>();
        }

        byte[] result = new byte[_Position];
        _Buffer.Slice(0, _Position).CopyTo(result);
        return result;
    }

    /// <summary>Returns a string representation of the builder state.</summary>
    public readonly override string ToString() => $"SpanBytesBuilder[{_Position} bytes]";
}
