// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

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
/// <b>Thread-safety:</b> Not thread-safe. Each builder instance must be used from a single thread.
/// The underlying buffer is thread-local via <see cref="ZeroAllocHelper"/>; do not share one builder
/// across threads.
/// </para>
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
    private byte[]? _Array;

    /// <summary>
    /// The working span that we write to.
    /// </summary>
    private Span<byte> _Span;

    /// <summary>
    /// Current write position in the buffer.
    /// </summary>
    private int _Position;

    /// <summary>
    /// True if using the ThreadStatic buffer (needs release on Dispose).
    /// </summary>
    private readonly bool _IsThreadStatic;

    // ========================================================================
    // CONSTRUCTORS
    // ========================================================================

    /// <summary>
    /// Private constructor.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TempBytesBuilder(byte[] array, bool isThreadStatic)
    {
        _Array = array;
        _Span = array.AsSpan();
        _Position = 0;
        _IsThreadStatic = isThreadStatic;
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
    public readonly int Length => _Position;

    /// <summary>
    /// Gets the current capacity of the buffer.
    /// </summary>
    public readonly int Capacity => _Span.Length;

    /// <summary>
    /// Gets the remaining capacity in the buffer.
    /// </summary>
    public readonly int Remaining => _Span.Length - _Position;

    /// <summary>
    /// Gets a value indicating whether the content is empty.
    /// </summary>
    public readonly bool IsEmpty => _Position == 0;

    /// <summary>
    /// Gets a value indicating whether this instance is using a heap-allocated buffer.
    /// </summary>
    public readonly bool IsHeapAllocated => !_IsThreadStatic && _Array is not null;

    // ========================================================================
    // SPAN ACCESS
    // ========================================================================

    /// <summary>
    /// Gets the written content as a read-only span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> AsSpan() => _Span.Slice(0, _Position);

    /// <summary>
    /// Gets the written content as a read-only span.
    /// </summary>
    public readonly ReadOnlySpan<byte> WrittenSpan => _Span.Slice(0, _Position);

    /// <summary>
    /// Clears the builder, resetting the position to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _Position = 0;

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

    // ========================================================================
    // BUFFER MANAGEMENT
    // ========================================================================

    /// <summary>
    /// Ensures the buffer has at least the specified capacity.
    /// When using the ThreadStatic buffer, delegates growth to <see cref="ZeroAllocHelper.GrowByteBuffer"/>.
    /// When using a heap-fallback buffer (nested call), grows the private array without touching the ThreadStatic field.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void _EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _Span.Length)
        {
            return;
        }

        if (_IsThreadStatic)
        {
            // We own the ThreadStatic buffer — grow it through the helper.
            _Array = ZeroAllocHelper.GrowByteBuffer(requiredCapacity);
        }
        else
        {
            // Heap-fallback mode: grow our own private array; leave ThreadStatic untouched.
            int newSize = ZeroAllocHelper.CalculateGrowth(_Array!.Length, requiredCapacity);
            byte[] grown = new byte[newSize];
            _Array.AsSpan(0, _Position).CopyTo(grown);
            _Array = grown;
        }

        _Span = _Array.AsSpan();
    }

    /// <summary>
    /// Tries to ensure the buffer has at least the specified capacity without throwing.
    /// When using the ThreadStatic buffer, delegates growth to <see cref="ZeroAllocHelper.TryGrowByteBuffer"/>.
    /// When using a heap-fallback buffer (nested call), grows the private array without touching the ThreadStatic field.
    /// </summary>
    /// <param name="requiredCapacity">The minimum required capacity.</param>
    /// <returns>True if capacity is available or was successfully grown; false otherwise.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool _TryEnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _Span.Length)
        {
            return true;
        }

        if (_IsThreadStatic)
        {
            // Try to grow the ThreadStatic buffer — may return null in ThrowOnOverflow mode.
            byte[]? newBuffer = ZeroAllocHelper.TryGrowByteBuffer(requiredCapacity);
            if (newBuffer is null)
            {
                return false;
            }

            _Array = newBuffer;
        }
        else
        {
            // Heap-fallback mode: grow our own private array; leave ThreadStatic untouched.
            int newSize = ZeroAllocHelper.CalculateGrowth(_Array!.Length, requiredCapacity);
            byte[] grown = new byte[newSize];
            _Array.AsSpan(0, _Position).CopyTo(grown);
            _Array = grown;
        }

        _Span = _Array.AsSpan();
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
        _EnsureCapacity(_Position + 1);
        _Span[_Position++] = value;
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

        _EnsureCapacity(_Position + value.Length);
        value.CopyTo(_Span.Slice(_Position));
        _Position += value.Length;
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
        if (!_TryEnsureCapacity(_Position + 1))
        {
            return false;
        }

        _Span[_Position++] = value;
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

        if (!_TryEnsureCapacity(_Position + value.Length))
        {
            return false;
        }

        value.CopyTo(_Span.Slice(_Position));
        _Position += value.Length;
        return true;
    }

    // ========================================================================
    // APPEND - BIG-ENDIAN INTEGERS
    // ========================================================================

    /// <summary>Appends a 16-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt16BigEndian(short value)
    {
        _EnsureCapacity(_Position + 2);
        BinaryPrimitives.WriteInt16BigEndian(_Span.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a 32-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt32BigEndian(int value)
    {
        _EnsureCapacity(_Position + 4);
        BinaryPrimitives.WriteInt32BigEndian(_Span.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a 64-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt64BigEndian(long value)
    {
        _EnsureCapacity(_Position + 8);
        BinaryPrimitives.WriteInt64BigEndian(_Span.Slice(_Position), value);
        _Position += 8;
    }

    /// <summary>Appends a 128-bit signed integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt128BigEndian(Int128 value)
    {
        _EnsureCapacity(_Position + 16);
        BinaryPrimitives.WriteInt128BigEndian(_Span.Slice(_Position), value);
        _Position += 16;
    }

    /// <summary>Appends a 16-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt16BigEndian(ushort value)
    {
        _EnsureCapacity(_Position + 2);
        BinaryPrimitives.WriteUInt16BigEndian(_Span.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a 32-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt32BigEndian(uint value)
    {
        _EnsureCapacity(_Position + 4);
        BinaryPrimitives.WriteUInt32BigEndian(_Span.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a 64-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt64BigEndian(ulong value)
    {
        _EnsureCapacity(_Position + 8);
        BinaryPrimitives.WriteUInt64BigEndian(_Span.Slice(_Position), value);
        _Position += 8;
    }

    /// <summary>Appends a 128-bit unsigned integer in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt128BigEndian(UInt128 value)
    {
        _EnsureCapacity(_Position + 16);
        BinaryPrimitives.WriteUInt128BigEndian(_Span.Slice(_Position), value);
        _Position += 16;
    }

    // ========================================================================
    // APPEND - LITTLE-ENDIAN INTEGERS
    // ========================================================================

    /// <summary>Appends a 16-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt16LittleEndian(short value)
    {
        _EnsureCapacity(_Position + 2);
        BinaryPrimitives.WriteInt16LittleEndian(_Span.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a 32-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt32LittleEndian(int value)
    {
        _EnsureCapacity(_Position + 4);
        BinaryPrimitives.WriteInt32LittleEndian(_Span.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a 64-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt64LittleEndian(long value)
    {
        _EnsureCapacity(_Position + 8);
        BinaryPrimitives.WriteInt64LittleEndian(_Span.Slice(_Position), value);
        _Position += 8;
    }

    /// <summary>Appends a 128-bit signed integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendInt128LittleEndian(Int128 value)
    {
        _EnsureCapacity(_Position + 16);
        BinaryPrimitives.WriteInt128LittleEndian(_Span.Slice(_Position), value);
        _Position += 16;
    }

    /// <summary>Appends a 16-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt16LittleEndian(ushort value)
    {
        _EnsureCapacity(_Position + 2);
        BinaryPrimitives.WriteUInt16LittleEndian(_Span.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a 32-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt32LittleEndian(uint value)
    {
        _EnsureCapacity(_Position + 4);
        BinaryPrimitives.WriteUInt32LittleEndian(_Span.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a 64-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt64LittleEndian(ulong value)
    {
        _EnsureCapacity(_Position + 8);
        BinaryPrimitives.WriteUInt64LittleEndian(_Span.Slice(_Position), value);
        _Position += 8;
    }

    /// <summary>Appends a 128-bit unsigned integer in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendUInt128LittleEndian(UInt128 value)
    {
        _EnsureCapacity(_Position + 16);
        BinaryPrimitives.WriteUInt128LittleEndian(_Span.Slice(_Position), value);
        _Position += 16;
    }

    // ========================================================================
    // APPEND - FLOATING POINT
    // ========================================================================

    /// <summary>Appends a half-precision float in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHalfBigEndian(Half value)
    {
        _EnsureCapacity(_Position + 2);
        BinaryPrimitives.WriteHalfBigEndian(_Span.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a half-precision float in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHalfLittleEndian(Half value)
    {
        _EnsureCapacity(_Position + 2);
        BinaryPrimitives.WriteHalfLittleEndian(_Span.Slice(_Position), value);
        _Position += 2;
    }

    /// <summary>Appends a single-precision float in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendSingleBigEndian(float value)
    {
        _EnsureCapacity(_Position + 4);
        BinaryPrimitives.WriteSingleBigEndian(_Span.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a single-precision float in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendSingleLittleEndian(float value)
    {
        _EnsureCapacity(_Position + 4);
        BinaryPrimitives.WriteSingleLittleEndian(_Span.Slice(_Position), value);
        _Position += 4;
    }

    /// <summary>Appends a double-precision float in big-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendDoubleBigEndian(double value)
    {
        _EnsureCapacity(_Position + 8);
        BinaryPrimitives.WriteDoubleBigEndian(_Span.Slice(_Position), value);
        _Position += 8;
    }

    /// <summary>Appends a double-precision float in little-endian format.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendDoubleLittleEndian(double value)
    {
        _EnsureCapacity(_Position + 8);
        BinaryPrimitives.WriteDoubleLittleEndian(_Span.Slice(_Position), value);
        _Position += 8;
    }

    // ========================================================================
    // APPEND - VARINT
    // ========================================================================

    /// <summary>Appends a variable-length unsigned integer (7-bit encoding).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendVarInt(ulong value)
    {
        // Max 10 bytes for 64-bit value
        _EnsureCapacity(_Position + VarInt.MaxSize);

        while (value >= 0x80)
        {
            _Span[_Position++] = (byte)(value | 0x80);
            value >>= 7;
        }
        _Span[_Position++] = (byte)value;
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
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        int maxBytes = Encoding.UTF8.GetMaxByteCount(value.Length);
        _EnsureCapacity(_Position + maxBytes);

        int written = Encoding.UTF8.GetBytes(value, _Span.Slice(_Position));
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

        _EnsureCapacity(_Position + byteCount);
        int written = Encoding.UTF8.GetBytes(value, _Span.Slice(_Position));
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

        _EnsureCapacity(_Position + byteCount);
        int written = Encoding.UTF8.GetBytes(value, _Span.Slice(_Position));
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

        _EnsureCapacity(_Position + byteCount);
        int written = Encoding.UTF8.GetBytes(value, _Span.Slice(_Position));
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
        // Try to get the size hint for optimal pre-allocation
        if (value.TryGetWrittenSize(out int size))
        {
            _EnsureCapacity(_Position + size);
        }

        int bytesWritten;
        // Try to write, growing if needed
        while (!value.TryWrite(_Span.Slice(_Position), out bytesWritten))
        {
            // Need more space - grow via ZeroAllocHelper (may throw depending on configuration)
            int previousLength = _Span.Length;
            int newSize = ZeroAllocHelper.CalculateGrowth(_Span.Length, _Span.Length + 64);
            _Array = ZeroAllocHelper.GrowByteBuffer(newSize);
            _Span = _Array.AsSpan();

            // Safety guard: if the buffer didn't actually grow, TryWrite will never succeed
            if (_Span.Length <= previousLength || ZeroAllocHelper.ConsumeSimulatedGrowStall())
            {
                throw new InvalidOperationException("Buffer failed to grow during serialization.");
            }
        }

        _Position += bytesWritten;
    }

    /// <summary>Tries to append any type implementing IBinarySerializable without throwing.</summary>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend<T>(T value) where T : IBinarySerializable
    {
        // Try to get the size hint for optimal pre-allocation
        if (value.TryGetWrittenSize(out int size))
        {
            if (!_TryEnsureCapacity(_Position + size))
            {
                return false;
            }
        }

        int bytesWritten;
        // Try to write, growing if needed
        while (!value.TryWrite(_Span.Slice(_Position), out bytesWritten))
        {
            int previousLength = _Span.Length;
            int newSize = ZeroAllocHelper.CalculateGrowth(_Span.Length, _Span.Length + 64);
            byte[]? newBuffer = ZeroAllocHelper.TryGrowByteBuffer(newSize);
            if (newBuffer is null)
            {
                return false;
            }

            _Array = newBuffer;
            _Span = _Array.AsSpan();

            // Safety guard: if the buffer didn't actually grow, TryWrite will never succeed
            if (_Span.Length <= previousLength || ZeroAllocHelper.ConsumeSimulatedGrowStall())
            {
                return false;
            }
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
        int bytesWritten;
        while (!value.TryFormat(_Span.Slice(_Position), out bytesWritten, format, provider))
        {
            // Need more space - grow via ZeroAllocHelper (may throw depending on configuration)
            int previousLength = _Span.Length;
            int newSize = ZeroAllocHelper.CalculateGrowth(_Span.Length, _Span.Length + 64);
            _Array = ZeroAllocHelper.GrowByteBuffer(newSize);
            _Span = _Array.AsSpan();

            // Safety guard: if the buffer didn't actually grow, TryFormat will never succeed
            if (_Span.Length <= previousLength || ZeroAllocHelper.ConsumeSimulatedGrowStall())
            {
                throw new InvalidOperationException("Buffer failed to grow during UTF-8 formatting.");
            }
        }

        _Position += bytesWritten;
    }

    /// <summary>Tries to append any IUtf8SpanFormattable value without throwing.</summary>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendUtf8Formattable<T>(T value, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : IUtf8SpanFormattable
    {
        int bytesWritten;
        while (!value.TryFormat(_Span.Slice(_Position), out bytesWritten, format, provider))
        {
            int previousLength = _Span.Length;
            int newSize = ZeroAllocHelper.CalculateGrowth(_Span.Length, _Span.Length + 64);
            byte[]? newBuffer = ZeroAllocHelper.TryGrowByteBuffer(newSize);
            if (newBuffer is null)
            {
                return false;
            }

            _Array = newBuffer;
            _Span = _Array.AsSpan();

            // Safety guard: if the buffer didn't actually grow, TryFormat will never succeed
            if (_Span.Length <= previousLength || ZeroAllocHelper.ConsumeSimulatedGrowStall())
            {
                return false;
            }
        }

        _Position += bytesWritten;
        return true;
    }

    // ========================================================================
    // HEX/BINARY FORMATTING (ASCII OUTPUT)
    // ========================================================================

    private static ReadOnlySpan<byte> _HexCharsBytes => "0123456789ABCDEF"u8;

    /// <summary>Appends a byte as 2 hexadecimal ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex2(byte value)
    {
        _EnsureCapacity(_Position + 2);
        _Span[_Position++] = _HexCharsBytes[value >> 4];
        _Span[_Position++] = _HexCharsBytes[value & 0xF];
    }

    /// <summary>Appends a ushort as 4 hexadecimal ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex4(ushort value)
    {
        _EnsureCapacity(_Position + 4);
        _Span[_Position++] = _HexCharsBytes[(value >> 12) & 0xF];
        _Span[_Position++] = _HexCharsBytes[(value >> 8) & 0xF];
        _Span[_Position++] = _HexCharsBytes[(value >> 4) & 0xF];
        _Span[_Position++] = _HexCharsBytes[value & 0xF];
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
        _EnsureCapacity(_Position + 8);
        for (int i = 7; i >= 0; i--)
        {
            _Span[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
    }

    /// <summary>Appends a ushort as 16 binary ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary16(ushort value)
    {
        _EnsureCapacity(_Position + 16);
        for (int i = 15; i >= 0; i--)
        {
            _Span[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
    }

    /// <summary>Appends a uint as 32 binary ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary32(uint value)
    {
        _EnsureCapacity(_Position + 32);
        for (int i = 31; i >= 0; i--)
        {
            _Span[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
    }

    /// <summary>Appends a ulong as 64 binary ASCII bytes.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary64(ulong value)
    {
        _EnsureCapacity(_Position + 64);
        for (int i = 63; i >= 0; i--)
        {
            _Span[_Position++] = (byte)('0' + (int)((value >> i) & 1));
        }
    }

    /// <summary>Tries to append a byte as 2 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The byte value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex2(byte value)
    {
        if (!_TryEnsureCapacity(_Position + 2))
        {
            return false;
        }

        _Span[_Position++] = _HexCharsBytes[value >> 4];
        _Span[_Position++] = _HexCharsBytes[value & 0xF];
        return true;
    }

    /// <summary>Tries to append a ushort as 4 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex4(ushort value)
    {
        if (!_TryEnsureCapacity(_Position + 4))
        {
            return false;
        }

        _Span[_Position++] = _HexCharsBytes[(value >> 12) & 0xF];
        _Span[_Position++] = _HexCharsBytes[(value >> 8) & 0xF];
        _Span[_Position++] = _HexCharsBytes[(value >> 4) & 0xF];
        _Span[_Position++] = _HexCharsBytes[value & 0xF];
        return true;
    }

    /// <summary>Tries to append a uint as 8 hexadecimal ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex8(uint value)
    {
        if (!_TryEnsureCapacity(_Position + 8))
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
        if (!_TryEnsureCapacity(_Position + 16))
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
        if (!_TryEnsureCapacity(_Position + 8))
        {
            return false;
        }

        for (int i = 7; i >= 0; i--)
        {
            _Span[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a ushort as 16 binary ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary16(ushort value)
    {
        if (!_TryEnsureCapacity(_Position + 16))
        {
            return false;
        }

        for (int i = 15; i >= 0; i--)
        {
            _Span[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a uint as 32 binary ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary32(uint value)
    {
        if (!_TryEnsureCapacity(_Position + 32))
        {
            return false;
        }

        for (int i = 31; i >= 0; i--)
        {
            _Span[_Position++] = (byte)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a ulong as 64 binary ASCII characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendBinary64(ulong value)
    {
        if (!_TryEnsureCapacity(_Position + 64))
        {
            return false;
        }

        for (int i = 63; i >= 0; i--)
        {
            _Span[_Position++] = (byte)('0' + (int)((value >> i) & 1));
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
        if (_Position == 0)
        {
            return Array.Empty<byte>();
        }

        byte[] result = new byte[_Position];
        _Span.Slice(0, _Position).CopyTo(result);
        return result;
    }

    /// <summary>
    /// Returns a string representation of the builder state.
    /// </summary>
    public readonly override string ToString() => $"TempBytesBuilder[{_Position} bytes]";

    // ========================================================================
    // IDISPOSABLE IMPLEMENTATION
    // ========================================================================

    /// <summary>
    /// Releases the ThreadStatic buffer if applicable.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_IsThreadStatic)
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
    public static implicit operator ReadOnlySpan<byte>(TempBytesBuilder builder) => builder.AsSpan();
}
