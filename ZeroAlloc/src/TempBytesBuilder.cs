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
// ZeroAlloc - TempBytesBuilder: Zero-Allocation Byte Builder
// ============================================================================
// A disposable ref struct that builds binary data without heap allocation.
// Uses ThreadStatic buffer with configurable overflow behavior.
//
// OVERFLOW BEHAVIOR (configured via ZeroAllocHelper):
//   - AutoGrow: Buffer grows automatically when capacity is exceeded (default)
//   - ThrowOnOverflow: Throws InvalidOperationException when buffer is full
//   - HeapFallback: Allocates heap buffer on overflow for nested calls
//
// IMPORTANT: Always use with 'using' statement to ensure buffer release:
//   using var builder = TempBytesBuilder.Create();
//   builder.AppendUInt16BigEndian(0x1234);
//   builder.AppendUtf8("Hello");
//   ReadOnlySpan<byte> result = builder.AsSpan();
//
// For caller-provided fixed-size buffers, use SpanBytesBuilder instead.
// ============================================================================

namespace ZeroAlloc;

/// <summary>
/// A disposable ref struct for building binary data without heap allocation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TempBytesBuilder"/> uses the thread-local buffer from 
/// <see cref="ZeroAllocHelper"/>. Overflow behavior depends on configuration:
/// AutoGrow (default), ThrowOnOverflow, or HeapFallback for nested calls.
/// </para>
/// <para><b>Example:</b></para>
/// <code>
/// using var builder = TempBytesBuilder.Create();
/// builder.AppendUInt16BigEndian(0x0100);
/// builder.AppendUtf8WithVarIntPrefix(message);
/// stream.Write(builder.AsSpan());
/// </code>
/// <para>
/// For caller-provided fixed-size buffers, use <see cref="SpanBytesBuilder"/> instead.
/// </para>
/// </remarks>
public ref struct TempBytesBuilder : IDisposable
{
    // ========================================================================
    // PRIVATE FIELDS
    // ========================================================================

    /// <summary>
    /// The array buffer.
    /// </summary>
    private byte[]? _array;

    /// <summary>
    /// The working span that we write to.
    /// </summary>
    private Span<byte> _span;

    /// <summary>
    /// Current write position in the buffer.
    /// </summary>
    private int _position;

    /// <summary>
    /// True if using the ThreadStatic buffer (needs release on Dispose).
    /// </summary>
    private readonly bool _isThreadStatic;

    // ========================================================================
    // CONSTRUCTORS
    // ========================================================================

    /// <summary>
    /// Private constructor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TempBytesBuilder(byte[] array, bool isThreadStatic)
    {
        _array = array;
        _span = array.AsSpan();
        _position = 0;
        _isThreadStatic = isThreadStatic;
    }

    // ========================================================================
    // FACTORY METHODS
    // ========================================================================

    /// <summary>
    /// Creates a new <see cref="TempBytesBuilder"/> using the ThreadStatic buffer.
    /// </summary>
    /// <returns>A new builder instance that must be disposed.</returns>
    /// <remarks>
    /// <para>
    /// This method acquires the thread-local byte buffer from <see cref="ZeroAllocHelper"/>.
    /// Overflow behavior depends on project configuration (see <see cref="ZeroAllocHelper"/>).
    /// </para>
    /// <para>
    /// <b>Important:</b> Always use with a <c>using</c> statement to ensure proper disposal.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TempBytesBuilder Create()
    {
        byte[] buffer = ZeroAllocHelper.AcquireByteBuffer(ZeroAllocHelper.DefaultBufferSize, out bool isThreadStatic);
        return new TempBytesBuilder(buffer, isThreadStatic);
    }

    // ========================================================================
    // PROPERTIES
    // ========================================================================

    /// <summary>
    /// Gets the current number of bytes written to the buffer.
    /// </summary>
    public readonly int Length => _position;

    /// <summary>
    /// Gets the current capacity of the buffer.
    /// </summary>
    public readonly int Capacity => _span.Length;

    /// <summary>
    /// Gets the remaining capacity in the buffer.
    /// </summary>
    public readonly int Remaining => _span.Length - _position;

    /// <summary>
    /// Gets a value indicating whether the content is empty.
    /// </summary>
    public readonly bool IsEmpty => _position == 0;

    /// <summary>
    /// Gets a value indicating whether this instance is using a heap-allocated buffer.
    /// </summary>
    public readonly bool IsHeapAllocated => !_isThreadStatic && _array is not null;

    // ========================================================================
    // SPAN ACCESS
    // ========================================================================

    /// <summary>
    /// Gets the written content as a read-only span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> AsSpan() => _span.Slice(0, _position);

    /// <summary>
    /// Gets the written content as a read-only span.
    /// </summary>
    public readonly ReadOnlySpan<byte> WrittenSpan => _span.Slice(0, _position);

    /// <summary>
    /// Clears the builder, resetting the position to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _position = 0;

    /// <summary>
    /// Moves the write position back by the specified number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to move back.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is negative or greater than the current position.
    /// </exception>
    /// <remarks>
    /// This effectively "unwrites" the last <paramref name="count"/> bytes,
    /// allowing you to overwrite them with new content.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SeekBack(int count)
    {
        if ((uint)count > (uint)_position)
        {
            throw new ArgumentOutOfRangeException(nameof(count),
                $"Cannot seek back {count} bytes when only {_position} bytes have been written.");
        }

        _position -= count;
    }

    /// <summary>
    /// Tries to move the write position back by the specified number of bytes without throwing.
    /// </summary>
    /// <param name="count">The number of bytes to move back.</param>
    /// <returns>True if successful; false if count is invalid.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TrySeekBack(int count)
    {
        if ((uint)count > (uint)_position)
        {
            return false;
        }

        _position -= count;
        return true;
    }

    // ========================================================================
    // BUFFER MANAGEMENT
    // ========================================================================

    /// <summary>
    /// Ensures the buffer has at least the specified capacity.
    /// Grows the buffer if needed using <see cref="ZeroAllocHelper.GrowByteBuffer"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _span.Length)
        {
            return;
        }

        // Grow the buffer via ZeroAllocHelper (behavior depends on configuration)
        _array = ZeroAllocHelper.GrowByteBuffer(requiredCapacity);
        _span = _array.AsSpan();
    }

    /// <summary>
    /// Tries to ensure the buffer has at least the specified capacity without throwing.
    /// </summary>
    /// <param name="requiredCapacity">The minimum required capacity.</param>
    /// <returns>True if capacity is available or was successfully grown; false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryEnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _span.Length)
        {
            return true;
        }

        // Try to grow - this may fail in ThrowOnOverflow mode
        byte[]? newBuffer = ZeroAllocHelper.TryGrowByteBuffer(requiredCapacity);
        if (newBuffer is null)
        {
            return false;
        }

        _array = newBuffer;
        _span = _array.AsSpan();
        return true;
    }

    // ========================================================================
    // APPEND - RAW BYTES
    // ========================================================================

    /// <summary>
    /// Appends a single byte to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(byte value)
    {
        EnsureCapacity(_position + 1);
        _span[_position++] = value;
    }

    /// <summary>
    /// Appends a byte array to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(byte[]? value)
    {
        if (value is null)
        {
            return;
        }

        Append(value.AsSpan());
    }

    /// <summary>
    /// Appends a byte span to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            return;
        }

        EnsureCapacity(_position + value.Length);
        value.CopyTo(_span.Slice(_position));
        _position += value.Length;
    }

    // ========================================================================
    // TRYAPPEND - RAW BYTES (NON-THROWING)
    // ========================================================================

    /// <summary>
    /// Tries to append a single byte to the buffer without throwing.
    /// </summary>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(byte value)
    {
        if (!TryEnsureCapacity(_position + 1))
        {
            return false;
        }

        _span[_position++] = value;
        return true;
    }

    /// <summary>
    /// Tries to append a byte array to the buffer without throwing.
    /// </summary>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(byte[]? value)
    {
        if (value is null)
        {
            return true;
        }

        return TryAppend(value.AsSpan());
    }

    /// <summary>
    /// Tries to append a byte span to the buffer without throwing.
    /// </summary>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty)
        {
            return true;
        }

        if (!TryEnsureCapacity(_position + value.Length))
        {
            return false;
        }

        value.CopyTo(_span.Slice(_position));
        _position += value.Length;
        return true;
    }

    // ========================================================================
    // APPEND - BIG-ENDIAN INTEGERS
    // ========================================================================

    /// <summary>Appends a 16-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt16BigEndian(short value)
    {
        EnsureCapacity(_position + 2);
        BinaryPrimitives.WriteInt16BigEndian(_span.Slice(_position), value);
        _position += 2;
    }

    /// <summary>Appends a 32-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt32BigEndian(int value)
    {
        EnsureCapacity(_position + 4);
        BinaryPrimitives.WriteInt32BigEndian(_span.Slice(_position), value);
        _position += 4;
    }

    /// <summary>Appends a 64-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt64BigEndian(long value)
    {
        EnsureCapacity(_position + 8);
        BinaryPrimitives.WriteInt64BigEndian(_span.Slice(_position), value);
        _position += 8;
    }

    /// <summary>Appends a 128-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt128BigEndian(Int128 value)
    {
        EnsureCapacity(_position + 16);
        BinaryPrimitives.WriteInt128BigEndian(_span.Slice(_position), value);
        _position += 16;
    }

    /// <summary>Appends a 16-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt16BigEndian(ushort value)
    {
        EnsureCapacity(_position + 2);
        BinaryPrimitives.WriteUInt16BigEndian(_span.Slice(_position), value);
        _position += 2;
    }

    /// <summary>Appends a 32-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt32BigEndian(uint value)
    {
        EnsureCapacity(_position + 4);
        BinaryPrimitives.WriteUInt32BigEndian(_span.Slice(_position), value);
        _position += 4;
    }

    /// <summary>Appends a 64-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt64BigEndian(ulong value)
    {
        EnsureCapacity(_position + 8);
        BinaryPrimitives.WriteUInt64BigEndian(_span.Slice(_position), value);
        _position += 8;
    }

    /// <summary>Appends a 128-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt128BigEndian(UInt128 value)
    {
        EnsureCapacity(_position + 16);
        BinaryPrimitives.WriteUInt128BigEndian(_span.Slice(_position), value);
        _position += 16;
    }

    // ========================================================================
    // APPEND - LITTLE-ENDIAN INTEGERS
    // ========================================================================

    /// <summary>Appends a 16-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt16LittleEndian(short value)
    {
        EnsureCapacity(_position + 2);
        BinaryPrimitives.WriteInt16LittleEndian(_span.Slice(_position), value);
        _position += 2;
    }

    /// <summary>Appends a 32-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt32LittleEndian(int value)
    {
        EnsureCapacity(_position + 4);
        BinaryPrimitives.WriteInt32LittleEndian(_span.Slice(_position), value);
        _position += 4;
    }

    /// <summary>Appends a 64-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt64LittleEndian(long value)
    {
        EnsureCapacity(_position + 8);
        BinaryPrimitives.WriteInt64LittleEndian(_span.Slice(_position), value);
        _position += 8;
    }

    /// <summary>Appends a 128-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt128LittleEndian(Int128 value)
    {
        EnsureCapacity(_position + 16);
        BinaryPrimitives.WriteInt128LittleEndian(_span.Slice(_position), value);
        _position += 16;
    }

    /// <summary>Appends a 16-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt16LittleEndian(ushort value)
    {
        EnsureCapacity(_position + 2);
        BinaryPrimitives.WriteUInt16LittleEndian(_span.Slice(_position), value);
        _position += 2;
    }

    /// <summary>Appends a 32-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt32LittleEndian(uint value)
    {
        EnsureCapacity(_position + 4);
        BinaryPrimitives.WriteUInt32LittleEndian(_span.Slice(_position), value);
        _position += 4;
    }

    /// <summary>Appends a 64-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt64LittleEndian(ulong value)
    {
        EnsureCapacity(_position + 8);
        BinaryPrimitives.WriteUInt64LittleEndian(_span.Slice(_position), value);
        _position += 8;
    }

    /// <summary>Appends a 128-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt128LittleEndian(UInt128 value)
    {
        EnsureCapacity(_position + 16);
        BinaryPrimitives.WriteUInt128LittleEndian(_span.Slice(_position), value);
        _position += 16;
    }

    // ========================================================================
    // APPEND - FLOATING POINT
    // ========================================================================

    /// <summary>Appends a half-precision float in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHalfBigEndian(Half value)
    {
        EnsureCapacity(_position + 2);
        BinaryPrimitives.WriteHalfBigEndian(_span.Slice(_position), value);
        _position += 2;
    }

    /// <summary>Appends a half-precision float in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHalfLittleEndian(Half value)
    {
        EnsureCapacity(_position + 2);
        BinaryPrimitives.WriteHalfLittleEndian(_span.Slice(_position), value);
        _position += 2;
    }

    /// <summary>Appends a single-precision float in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendSingleBigEndian(float value)
    {
        EnsureCapacity(_position + 4);
        BinaryPrimitives.WriteSingleBigEndian(_span.Slice(_position), value);
        _position += 4;
    }

    /// <summary>Appends a single-precision float in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendSingleLittleEndian(float value)
    {
        EnsureCapacity(_position + 4);
        BinaryPrimitives.WriteSingleLittleEndian(_span.Slice(_position), value);
        _position += 4;
    }

    /// <summary>Appends a double-precision float in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendDoubleBigEndian(double value)
    {
        EnsureCapacity(_position + 8);
        BinaryPrimitives.WriteDoubleBigEndian(_span.Slice(_position), value);
        _position += 8;
    }

    /// <summary>Appends a double-precision float in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendDoubleLittleEndian(double value)
    {
        EnsureCapacity(_position + 8);
        BinaryPrimitives.WriteDoubleLittleEndian(_span.Slice(_position), value);
        _position += 8;
    }

    // ========================================================================
    // APPEND - VARINT
    // ========================================================================

    /// <summary>Appends a variable-length unsigned integer (7-bit encoding).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendVarInt(ulong value)
    {
        // Max 10 bytes for 64-bit value
        EnsureCapacity(_position + VarInt.MaxSize);

        while (value >= 0x80)
        {
            _span[_position++] = (byte)(value | 0x80);
            value >>= 7;
        }
        _span[_position++] = (byte)value;
    }

    /// <summary>Appends a variable-length signed integer using ZigZag encoding.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendVarIntZigZag(long value)
    {
        // ZigZag encode: 0 → 0, -1 → 1, 1 → 2, -2 → 3, 2 → 4, ...
        ulong zigzag = (ulong)((value << 1) ^ (value >> 63));
        AppendVarInt(zigzag);
    }

    /// <summary>Appends a variable-length signed 32-bit integer using ZigZag encoding.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendVarIntZigZag(int value)
    {
        // ZigZag encode for 32-bit
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
        if (string.IsNullOrEmpty(value)) return;

        int maxBytes = Encoding.UTF8.GetMaxByteCount(value.Length);
        EnsureCapacity(_position + maxBytes);

        int written = Encoding.UTF8.GetBytes(value, _span.Slice(_position));
        _position += written;
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

        EnsureCapacity(_position + byteCount);
        int written = Encoding.UTF8.GetBytes(value, _span.Slice(_position));
        _position += written;
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

        EnsureCapacity(_position + byteCount);
        int written = Encoding.UTF8.GetBytes(value, _span.Slice(_position));
        _position += written;
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

        EnsureCapacity(_position + byteCount);
        int written = Encoding.UTF8.GetBytes(value, _span.Slice(_position));
        _position += written;
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
        // Try to get the size hint for optimal pre-allocation
        if (value.TryGetSerializedSize(out int size))
        {
            EnsureCapacity(_position + size);
        }

        int bytesWritten;
        // Try to write, growing if needed
        while (!value.TryWrite(_span.Slice(_position), out bytesWritten))
        {
            // Need more space - grow via ZeroAllocHelper (may throw depending on configuration)
            int newSize = ZeroAllocHelper.CalculateGrowth(_span.Length, _span.Length + 64);
            _array = ZeroAllocHelper.GrowByteBuffer(newSize);
            _span = _array.AsSpan();
        }

        _position += bytesWritten;
    }

    /// <summary>Tries to append any type implementing IBinarySerializable without throwing.</summary>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend<T>(T value) where T : IBinarySerializable
    {
        // Try to get the size hint for optimal pre-allocation
        if (value.TryGetSerializedSize(out int size))
        {
            if (!TryEnsureCapacity(_position + size))
            {
                return false;
            }
        }

        int bytesWritten;
        // Try to write, growing if needed
        while (!value.TryWrite(_span.Slice(_position), out bytesWritten))
        {
            int newSize = ZeroAllocHelper.CalculateGrowth(_span.Length, _span.Length + 64);
            byte[]? newBuffer = ZeroAllocHelper.TryGrowByteBuffer(newSize);
            if (newBuffer is null)
            {
                return false;
            }

            _array = newBuffer;
            _span = _array.AsSpan();
        }

        _position += bytesWritten;
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
        int bytesWritten;
        while (!value.TryFormat(_span.Slice(_position), out bytesWritten, format, provider))
        {
            // Need more space - grow via ZeroAllocHelper (may throw depending on configuration)
            int newSize = ZeroAllocHelper.CalculateGrowth(_span.Length, _span.Length + 64);
            _array = ZeroAllocHelper.GrowByteBuffer(newSize);
            _span = _array.AsSpan();
        }

        _position += bytesWritten;
    }

    /// <summary>Tries to append any IUtf8SpanFormattable value without throwing.</summary>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUtf8Formattable<T>(T value, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        int bytesWritten;
        while (!value.TryFormat(_span.Slice(_position), out bytesWritten, format, provider))
        {
            int newSize = ZeroAllocHelper.CalculateGrowth(_span.Length, _span.Length + 64);
            byte[]? newBuffer = ZeroAllocHelper.TryGrowByteBuffer(newSize);
            if (newBuffer is null)
            {
                return false;
            }

            _array = newBuffer;
            _span = _array.AsSpan();
        }

        _position += bytesWritten;
        return true;
    }

    // ========================================================================
    // HEX/BINARY FORMATTING (ASCII OUTPUT)
    // ========================================================================

    private static ReadOnlySpan<byte> HexCharsBytes => "0123456789ABCDEF"u8;

    /// <summary>Appends a byte as 2 hexadecimal ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex2(byte value)
    {
        EnsureCapacity(_position + 2);
        _span[_position++] = HexCharsBytes[value >> 4];
        _span[_position++] = HexCharsBytes[value & 0xF];
    }

    /// <summary>Appends a ushort as 4 hexadecimal ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex4(ushort value)
    {
        EnsureCapacity(_position + 4);
        _span[_position++] = HexCharsBytes[(value >> 12) & 0xF];
        _span[_position++] = HexCharsBytes[(value >> 8) & 0xF];
        _span[_position++] = HexCharsBytes[(value >> 4) & 0xF];
        _span[_position++] = HexCharsBytes[value & 0xF];
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
        EnsureCapacity(_position + 8);
        for (int i = 7; i >= 0; i--)
            _span[_position++] = (byte)('0' + ((value >> i) & 1));
    }

    /// <summary>Appends a ushort as 16 binary ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary16(ushort value)
    {
        EnsureCapacity(_position + 16);
        for (int i = 15; i >= 0; i--)
            _span[_position++] = (byte)('0' + ((value >> i) & 1));
    }

    /// <summary>Appends a uint as 32 binary ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary32(uint value)
    {
        EnsureCapacity(_position + 32);
        for (int i = 31; i >= 0; i--)
            _span[_position++] = (byte)('0' + ((value >> i) & 1));
    }

    /// <summary>Appends a ulong as 64 binary ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary64(ulong value)
    {
        EnsureCapacity(_position + 64);
        for (int i = 63; i >= 0; i--)
            _span[_position++] = (byte)('0' + (int)((value >> i) & 1));
    }

    /// <summary>Tries to append a byte as 2 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The byte value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex2(byte value)
    {
        if (!TryEnsureCapacity(_position + 2))
        {
            return false;
        }

        _span[_position++] = HexCharsBytes[value >> 4];
        _span[_position++] = HexCharsBytes[value & 0xF];
        return true;
    }

    /// <summary>Tries to append a ushort as 4 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex4(ushort value)
    {
        if (!TryEnsureCapacity(_position + 4))
        {
            return false;
        }

        _span[_position++] = HexCharsBytes[(value >> 12) & 0xF];
        _span[_position++] = HexCharsBytes[(value >> 8) & 0xF];
        _span[_position++] = HexCharsBytes[(value >> 4) & 0xF];
        _span[_position++] = HexCharsBytes[value & 0xF];
        return true;
    }

    /// <summary>Tries to append a uint as 8 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex8(uint value)
    {
        if (!TryEnsureCapacity(_position + 8))
        {
            return false;
        }

        AppendHex4((ushort)(value >> 16));
        AppendHex4((ushort)(value & 0xFFFF));
        return true;
    }

    /// <summary>Tries to append a ulong as 16 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex16(ulong value)
    {
        if (!TryEnsureCapacity(_position + 16))
        {
            return false;
        }

        AppendHex8((uint)(value >> 32));
        AppendHex8((uint)(value & 0xFFFFFFFF));
        return true;
    }

    /// <summary>Tries to append a byte as 8 binary ASCII characters without throwing.</summary>
    /// <param name="value">The byte value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary8(byte value)
    {
        if (!TryEnsureCapacity(_position + 8))
        {
            return false;
        }

        for (int i = 7; i >= 0; i--)
        {
            _span[_position++] = (byte)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a ushort as 16 binary ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary16(ushort value)
    {
        if (!TryEnsureCapacity(_position + 16))
        {
            return false;
        }

        for (int i = 15; i >= 0; i--)
        {
            _span[_position++] = (byte)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a uint as 32 binary ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary32(uint value)
    {
        if (!TryEnsureCapacity(_position + 32))
        {
            return false;
        }

        for (int i = 31; i >= 0; i--)
        {
            _span[_position++] = (byte)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a ulong as 64 binary ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary64(ulong value)
    {
        if (!TryEnsureCapacity(_position + 64))
        {
            return false;
        }

        for (int i = 63; i >= 0; i--)
        {
            _span[_position++] = (byte)('0' + (int)((value >> i) & 1));
        }
        return true;
    }

    // ========================================================================
    // TOARRAY / OUTPUT
    // ========================================================================

    /// <summary>
    /// Creates a heap-allocated byte array from the builder content.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly byte[] ToArray()
    {
        if (_position == 0)
            return Array.Empty<byte>();

        byte[] result = new byte[_position];
        _span.Slice(0, _position).CopyTo(result);
        return result;
    }

    /// <summary>
    /// Returns a string representation of the builder state.
    /// </summary>
    public readonly override string ToString() => $"TempBytesBuilder[{_position} bytes]";

    // ========================================================================
    // IDISPOSABLE IMPLEMENTATION
    // ========================================================================

    /// <summary>
    /// Releases the ThreadStatic buffer if applicable.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_isThreadStatic)
        {
            ZeroAllocHelper.ReleaseByteBuffer();
        }
    }

    // ========================================================================
    // OPERATORS
    // ========================================================================

    /// <summary>
    /// Implicitly converts a <see cref="TempBytesBuilder"/> to <see cref="ReadOnlySpan{Byte}"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(TempBytesBuilder builder)
    {
        return builder.AsSpan();
    }
}
