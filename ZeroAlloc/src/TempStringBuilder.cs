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
// ZeroAlloc - TempStringBuilder: Zero-Allocation String Builder
// ============================================================================
// A disposable ref struct that builds strings without heap allocation.
// Uses ThreadStatic buffer with configurable overflow behavior.
//
// OVERFLOW BEHAVIOR (configured via ZeroAllocHelper):
//   - AutoGrow: Buffer grows automatically when capacity is exceeded (default)
//   - ThrowOnOverflow: Throws InvalidOperationException when buffer is full
//   - HeapFallback: Allocates heap buffer on overflow for nested calls
//
// IMPORTANT: Always use with 'using' statement to ensure buffer release:
//   using var builder = TempStringBuilder.Create();
//   builder.Append("Hello ");
//   builder.Append(42);
//   ReadOnlySpan<char> result = builder.AsSpan();
//
// For caller-provided fixed-size buffers, use SpanStringBuilder instead.
// ============================================================================

namespace ZeroAlloc;

/// <summary>
/// A disposable ref struct for building strings without heap allocation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TempStringBuilder"/> uses the thread-local buffer from 
/// <see cref="ZeroAllocHelper"/>. Overflow behavior depends on configuration:
/// AutoGrow (default), ThrowOnOverflow, or HeapFallback for nested calls.
/// </para>
/// <para><b>Example:</b></para>
/// <code>
/// using var builder = TempStringBuilder.Create();
/// builder.Append("User ");
/// builder.Append(userId);
/// if (isAdmin) builder.Append(" [ADMIN]");
/// ReadOnlySpan&lt;char&gt; result = builder.AsSpan();
/// </code>
/// <para>
/// For caller-provided fixed-size buffers, use <see cref="SpanStringBuilder"/> instead.
/// </para>
/// </remarks>
public ref struct TempStringBuilder : IDisposable
{
    // ========================================================================
    // PRIVATE FIELDS
    // ========================================================================

    /// <summary>
    /// The array buffer.
    /// </summary>
    private char[]? _array;

    /// <summary>
    /// The working span that we write to.
    /// </summary>
    private Span<char> _span;

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
    /// Private constructor for ThreadStatic mode.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TempStringBuilder(char[] array, bool isThreadStatic)
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
    /// Creates a new <see cref="TempStringBuilder"/> using the ThreadStatic buffer.
    /// </summary>
    /// <returns>A new builder instance that must be disposed.</returns>
    /// <remarks>
    /// <para>
    /// This method acquires the thread-local char buffer from <see cref="ZeroAllocHelper"/>.
    /// Overflow behavior depends on project configuration (see <see cref="ZeroAllocHelper"/>).
    /// </para>
    /// <para>
    /// <b>Important:</b> Always use with a <c>using</c> statement to ensure proper disposal.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var builder = TempStringBuilder.Create();
    /// builder.Append("Hello ");
    /// builder.Append(name);
    /// Console.WriteLine(builder.AsSpan().ToString());
    /// </code>
    /// </example>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TempStringBuilder Create()
    {
        char[] buffer = ZeroAllocHelper.AcquireCharBuffer(ZeroAllocHelper.DefaultBufferSize, out bool isThreadStatic);
        return new TempStringBuilder(buffer, isThreadStatic);
    }

    // ========================================================================
    // PROPERTIES
    // ========================================================================

    /// <summary>
    /// Gets the current number of characters written to the buffer.
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
    /// Gets a value indicating whether this instance is using a heap-allocated buffer
    /// (fallback mode due to nested call) rather than the ThreadStatic buffer.
    /// </summary>
    public readonly bool IsHeapAllocated => !_isThreadStatic && _array is not null;

    // ========================================================================
    // SPAN ACCESS
    // ========================================================================

    /// <summary>
    /// Gets the written content as a read-only span.
    /// </summary>
    /// <returns>A read-only span containing the written characters.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<char> AsSpan() => _span.Slice(0, _position);

    /// <summary>
    /// Clears the builder, resetting the position to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _position = 0;

    /// <summary>
    /// Moves the write position back by the specified number of characters.
    /// </summary>
    /// <param name="count">The number of characters to move back.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is negative or greater than the current position.
    /// </exception>
    /// <remarks>
    /// This effectively "unwrite" the last <paramref name="count"/> characters,
    /// allowing you to overwrite them with new content.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SeekBack(int count)
    {
        if ((uint)count > (uint)_position)
        {
            throw new ArgumentOutOfRangeException(nameof(count),
                $"Cannot seek back {count} characters when only {_position} characters have been written.");
        }

        _position -= count;
    }

    /// <summary>
    /// Tries to move the write position back by the specified number of characters without throwing.
    /// </summary>
    /// <param name="count">The number of characters to move back.</param>
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
    /// Grows the buffer if needed using <see cref="ZeroAllocHelper.GrowCharBuffer"/>.
    /// </summary>
    /// <param name="requiredCapacity">The minimum required capacity.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= _span.Length)
        {
            return;
        }

        // Grow the buffer via ZeroAllocHelper (behavior depends on configuration)
        _array = ZeroAllocHelper.GrowCharBuffer(requiredCapacity);
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
        char[]? newBuffer = ZeroAllocHelper.TryGrowCharBuffer(requiredCapacity);
        if (newBuffer is null)
        {
            return false;
        }

        _array = newBuffer;
        _span = _array.AsSpan();
        return true;
    }

    // ========================================================================
    // APPEND - STRING LITERALS
    // ========================================================================

    /// <summary>
    /// Appends a string to the buffer.
    /// </summary>
    /// <param name="value">The string to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string? value)
    {
        if (value is null) return;
        int requiredCapacity = _position + value.Length;
        EnsureCapacity(requiredCapacity);
        value.AsSpan().CopyTo(_span.Slice(_position));
        _position += value.Length;
    }

    /// <summary>
    /// Appends a character span to the buffer.
    /// </summary>
    /// <param name="value">The span to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<char> value)
    {
        int requiredCapacity = _position + value.Length;
        EnsureCapacity(requiredCapacity);
        value.CopyTo(_span.Slice(_position));
        _position += value.Length;
    }

    /// <summary>
    /// Appends a single character to the buffer.
    /// </summary>
    /// <param name="value">The character to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char value)
    {
        EnsureCapacity(_position + 1);
        _span[_position++] = value;
    }

    // ========================================================================
    // APPEND - PRIMITIVE TYPES
    // ========================================================================

    /// <summary>Appends an integer value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(int value) => AppendFormattable(value);

    /// <summary>Appends a long value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(long value) => AppendFormattable(value);

    /// <summary>Appends an unsigned integer value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(uint value) => AppendFormattable(value);

    /// <summary>Appends an unsigned long value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ulong value) => AppendFormattable(value);

    /// <summary>Appends a short value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(short value) => AppendFormattable(value);

    /// <summary>Appends an unsigned short value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ushort value) => AppendFormattable(value);

    /// <summary>Appends a byte value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(byte value) => AppendFormattable(value);

    /// <summary>Appends a signed byte value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(sbyte value) => AppendFormattable((int)value);

    /// <summary>Appends a boolean value ("True" or "False") to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(bool value)
    {
        if (value)
        {
            EnsureCapacity(_position + 4);
            "True".AsSpan().CopyTo(_span.Slice(_position));
            _position += 4;
        }
        else
        {
            EnsureCapacity(_position + 5);
            "False".AsSpan().CopyTo(_span.Slice(_position));
            _position += 5;
        }
    }

    /// <summary>Appends a DateTime value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(DateTime value) => AppendFormattable(value);

    /// <summary>Appends a DateTimeOffset value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(DateTimeOffset value) => AppendFormattable(value);

    /// <summary>Appends a TimeSpan value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(TimeSpan value) => AppendFormattable(value);

    /// <summary>Appends a GUID value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(Guid value) => AppendFormattable(value);

    /// <summary>Appends a 128-bit signed integer value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(Int128 value) => AppendFormattable(value);

    /// <summary>Appends a 128-bit unsigned integer value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(UInt128 value) => AppendFormattable(value);

    /// <summary>Appends a single-precision floating-point value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(float value) => AppendFormattable(value);

    /// <summary>Appends a double-precision floating-point value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(double value) => AppendFormattable(value);

    /// <summary>Appends a decimal value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(decimal value) => AppendFormattable(value);

    /// <summary>Appends a half-precision floating-point value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(Half value) => AppendFormattable(value);

    /// <summary>Appends a DateOnly value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(DateOnly value) => AppendFormattable(value);

    /// <summary>Appends a TimeOnly value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(TimeOnly value) => AppendFormattable(value);

    /// <summary>Appends a native integer value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(nint value) => AppendFormattable(value);

    /// <summary>Appends a native unsigned integer value to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(nuint value) => AppendFormattable(value);

    // ========================================================================
    // APPEND - WITH FORMAT
    // ========================================================================

    /// <summary>Appends an integer value with a format string to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(int value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        => AppendFormattable(value, format, provider);

    /// <summary>Appends a long value with a format string to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(long value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        => AppendFormattable(value, format, provider);

    /// <summary>Appends a double value with a format string to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(double value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        => AppendFormattable(value, format, provider);

    /// <summary>Appends a float value with a format string to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(float value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        => AppendFormattable(value, format, provider);

    /// <summary>Appends a decimal value with a format string to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(decimal value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        => AppendFormattable(value, format, provider);

    /// <summary>Appends a DateTime value with a format string to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(DateTime value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        => AppendFormattable(value, format, provider);

    /// <summary>Appends a DateTimeOffset value with a format string to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(DateTimeOffset value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        => AppendFormattable(value, format, provider);

    /// <summary>Appends a TimeSpan value with a format string to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(TimeSpan value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        => AppendFormattable(value, format, provider);

    /// <summary>Appends a GUID value with a format string to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(Guid value, ReadOnlySpan<char> format)
        => AppendFormattable(value, format, null);

    /// <summary>
    /// Appends a formattable value with a format string.
    /// </summary>
    /// <typeparam name="T">The type of the value (must implement ISpanFormattable).</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append<T>(T value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        where T : ISpanFormattable
        => AppendFormattable(value, format, provider);

    // ========================================================================
    // APPEND - HELPER FOR FORMATTABLES
    // ========================================================================

    /// <summary>
    /// Core implementation for appending ISpanFormattable types.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AppendFormattable<T>(T value, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : ISpanFormattable
    {
        int charsWritten;
        // Try to format in the remaining space
        while (!value.TryFormat(_span.Slice(_position), out charsWritten, format, provider))
        {
            // Need more space - grow via ZeroAllocHelper (may throw depending on configuration)
            int newSize = ZeroAllocHelper.CalculateGrowth(_span.Length, _span.Length + 64);
            _array = ZeroAllocHelper.GrowCharBuffer(newSize);
            _span = _array.AsSpan();
        }

        _position += charsWritten;
    }

    /// <summary>
    /// Core implementation for trying to append ISpanFormattable types without throwing.
    /// </summary>
    /// <returns>True if the value was appended; false if buffer is full.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool TryAppendFormattable<T>(T value, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
        where T : ISpanFormattable
    {
        int charsWritten;
        // Try to format in the remaining space
        while (!value.TryFormat(_span.Slice(_position), out charsWritten, format, provider))
        {
            // Need more space - try to grow
            int newSize = ZeroAllocHelper.CalculateGrowth(_span.Length, _span.Length + 64);
            char[]? newBuffer = ZeroAllocHelper.TryGrowCharBuffer(newSize);
            if (newBuffer is null)
            {
                return false;
            }

            _array = newBuffer;
            _span = _array.AsSpan();
        }

        _position += charsWritten;
        return true;
    }

    // ========================================================================
    // APPENDLINE
    // ========================================================================

    /// <summary>Appends the platform-specific newline sequence to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine() => Append(Environment.NewLine);

    /// <summary>Appends a string followed by a newline to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine(string? value) { Append(value); AppendLine(); }

    /// <summary>Appends a character span followed by a newline to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine(ReadOnlySpan<char> value) { Append(value); AppendLine(); }

    // ========================================================================
    // TRYAPPEND - NON-THROWING VARIANTS
    // ========================================================================

    /// <summary>
    /// Tries to append a string to the buffer without throwing.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(string? value)
    {
        if (value is null)
        {
            return true;
        }

        int requiredCapacity = _position + value.Length;
        if (!TryEnsureCapacity(requiredCapacity))
        {
            return false;
        }

        value.AsSpan().CopyTo(_span.Slice(_position));
        _position += value.Length;
        return true;
    }

    /// <summary>
    /// Tries to append a character span to the buffer without throwing.
    /// </summary>
    /// <param name="value">The span to append.</param>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(ReadOnlySpan<char> value)
    {
        int requiredCapacity = _position + value.Length;
        if (!TryEnsureCapacity(requiredCapacity))
        {
            return false;
        }

        value.CopyTo(_span.Slice(_position));
        _position += value.Length;
        return true;
    }

    /// <summary>
    /// Tries to append a single character to the buffer without throwing.
    /// </summary>
    /// <param name="value">The character to append.</param>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(char value)
    {
        if (!TryEnsureCapacity(_position + 1))
        {
            return false;
        }

        _span[_position++] = value;
        return true;
    }

    /// <summary>Tries to append an integer value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(int value) => TryAppendFormattable(value);

    /// <summary>Tries to append a long value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(long value) => TryAppendFormattable(value);

    /// <summary>Tries to append an unsigned integer value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(uint value) => TryAppendFormattable(value);

    /// <summary>Tries to append an unsigned long value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(ulong value) => TryAppendFormattable(value);

    /// <summary>Tries to append a short value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(short value) => TryAppendFormattable(value);

    /// <summary>Tries to append an unsigned short value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(ushort value) => TryAppendFormattable(value);

    /// <summary>Tries to append a byte value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(byte value) => TryAppendFormattable(value);

    /// <summary>Tries to append a signed byte value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(sbyte value) => TryAppendFormattable((int)value);

    /// <summary>Tries to append a boolean value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(bool value)
    {
        if (value)
        {
            if (!TryEnsureCapacity(_position + 4))
            {
                return false;
            }

            "True".AsSpan().CopyTo(_span.Slice(_position));
            _position += 4;
        }
        else
        {
            if (!TryEnsureCapacity(_position + 5))
            {
                return false;
            }

            "False".AsSpan().CopyTo(_span.Slice(_position));
            _position += 5;
        }
        return true;
    }

    /// <summary>Tries to append a DateTime value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(DateTime value) => TryAppendFormattable(value);

    /// <summary>Tries to append a DateTimeOffset value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(DateTimeOffset value) => TryAppendFormattable(value);

    /// <summary>Tries to append a TimeSpan value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(TimeSpan value) => TryAppendFormattable(value);

    /// <summary>Tries to append a GUID value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(Guid value) => TryAppendFormattable(value);

    /// <summary>Tries to append a 128-bit signed integer value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(Int128 value) => TryAppendFormattable(value);

    /// <summary>Tries to append a 128-bit unsigned integer value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(UInt128 value) => TryAppendFormattable(value);

    /// <summary>Tries to append a float value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(float value) => TryAppendFormattable(value);

    /// <summary>Tries to append a double value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(double value) => TryAppendFormattable(value);

    /// <summary>Tries to append a decimal value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(decimal value) => TryAppendFormattable(value);

    /// <summary>Tries to append a Half value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(Half value) => TryAppendFormattable(value);

    /// <summary>Tries to append a DateOnly value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(DateOnly value) => TryAppendFormattable(value);

    /// <summary>Tries to append a TimeOnly value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(TimeOnly value) => TryAppendFormattable(value);

    /// <summary>Tries to append a native integer value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(nint value) => TryAppendFormattable(value);

    /// <summary>Tries to append a native unsigned integer value without throwing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(nuint value) => TryAppendFormattable(value);

    /// <summary>
    /// Tries to append a formattable value with a format string without throwing.
    /// </summary>
    /// <typeparam name="T">The type of the value (must implement ISpanFormattable).</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    /// <returns>True if successful; false if buffer is full and cannot grow.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend<T>(T value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        where T : ISpanFormattable
        => TryAppendFormattable(value, format, provider);

    // ========================================================================
    // HEX/BINARY FORMATTING
    // ========================================================================

    private static ReadOnlySpan<char> HexChars => "0123456789ABCDEF";

    /// <summary>Appends a byte as 2 hexadecimal characters to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex2(byte value)
    {
        EnsureCapacity(_position + 2);
        _span[_position++] = HexChars[value >> 4];
        _span[_position++] = HexChars[value & 0xF];
    }

    /// <summary>Appends a ushort as 4 hexadecimal characters to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex4(ushort value)
    {
        EnsureCapacity(_position + 4);
        _span[_position++] = HexChars[(value >> 12) & 0xF];
        _span[_position++] = HexChars[(value >> 8) & 0xF];
        _span[_position++] = HexChars[(value >> 4) & 0xF];
        _span[_position++] = HexChars[value & 0xF];
    }

    /// <summary>Appends a uint as 8 hexadecimal characters to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex8(uint value)
    {
        AppendHex4((ushort)(value >> 16));
        AppendHex4((ushort)(value & 0xFFFF));
    }

    /// <summary>Appends a ulong as 16 hexadecimal characters to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex16(ulong value)
    {
        AppendHex8((uint)(value >> 32));
        AppendHex8((uint)(value & 0xFFFFFFFF));
    }

    /// <summary>Appends a byte as 8 binary characters to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary8(byte value)
    {
        EnsureCapacity(_position + 8);
        for (int i = 7; i >= 0; i--)
            _span[_position++] = (char)('0' + ((value >> i) & 1));
    }

    /// <summary>Appends a ushort as 16 binary characters to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary16(ushort value)
    {
        EnsureCapacity(_position + 16);
        for (int i = 15; i >= 0; i--)
            _span[_position++] = (char)('0' + ((value >> i) & 1));
    }

    /// <summary>Appends a uint as 32 binary characters to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary32(uint value)
    {
        EnsureCapacity(_position + 32);
        for (int i = 31; i >= 0; i--)
            _span[_position++] = (char)('0' + ((value >> i) & 1));
    }

    /// <summary>Appends a ulong as 64 binary characters to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary64(ulong value)
    {
        EnsureCapacity(_position + 64);
        for (int i = 63; i >= 0; i--)
            _span[_position++] = (char)('0' + (int)((value >> i) & 1));
    }

    /// <summary>Tries to append a byte as 2 hexadecimal characters without throwing.</summary>
    /// <param name="value">The byte value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex2(byte value)
    {
        if (!TryEnsureCapacity(_position + 2))
        {
            return false;
        }

        _span[_position++] = HexChars[value >> 4];
        _span[_position++] = HexChars[value & 0xF];
        return true;
    }

    /// <summary>Tries to append a ushort as 4 hexadecimal characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer growth failed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex4(ushort value)
    {
        if (!TryEnsureCapacity(_position + 4))
        {
            return false;
        }

        _span[_position++] = HexChars[(value >> 12) & 0xF];
        _span[_position++] = HexChars[(value >> 8) & 0xF];
        _span[_position++] = HexChars[(value >> 4) & 0xF];
        _span[_position++] = HexChars[value & 0xF];
        return true;
    }

    /// <summary>Tries to append a uint as 8 hexadecimal characters without throwing.</summary>
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

    /// <summary>Tries to append a ulong as 16 hexadecimal characters without throwing.</summary>
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

    /// <summary>Tries to append a byte as 8 binary characters without throwing.</summary>
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
            _span[_position++] = (char)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a ushort as 16 binary characters without throwing.</summary>
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
            _span[_position++] = (char)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a uint as 32 binary characters without throwing.</summary>
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
            _span[_position++] = (char)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a ulong as 64 binary characters without throwing.</summary>
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
            _span[_position++] = (char)('0' + (int)((value >> i) & 1));
        }
        return true;
    }

    // ========================================================================
    // TOSTRING / OUTPUT
    // ========================================================================

    /// <summary>
    /// Creates a heap-allocated string from the builder content.
    /// </summary>
    /// <returns>A new string containing the written characters.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override string ToString()
    {
        if (_position == 0)
            return string.Empty;

        return new string(_span.Slice(0, _position));
    }

    // ========================================================================
    // IDISPOSABLE IMPLEMENTATION
    // ========================================================================

    /// <summary>
    /// Releases the ThreadStatic buffer back to <see cref="ZeroAllocHelper"/>.
    /// </summary>
    /// <remarks>
    /// In heap-fallback mode (nested calls): The heap-allocated buffer is simply discarded.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_isThreadStatic)
        {
            ZeroAllocHelper.ReleaseCharBuffer();
        }
    }

    // ========================================================================
    // OPERATORS
    // ========================================================================

    /// <summary>
    /// Implicitly converts a <see cref="TempStringBuilder"/> to <see cref="ReadOnlySpan{Char}"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<char>(TempStringBuilder builder)
    {
        return builder.AsSpan();
    }
}
