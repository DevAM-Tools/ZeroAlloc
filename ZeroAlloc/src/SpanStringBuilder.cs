// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc;

/// <summary>
/// A span-based string builder that writes directly to a user-provided <see cref="Span{Char}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="SpanStringBuilder"/> when you know the maximum buffer size upfront
/// and want to avoid ThreadStatic buffer overhead. The builder throws 
/// <see cref="InvalidOperationException"/> if the buffer overflows.
/// </para>
/// <para><b>Example:</b></para>
/// <code>
/// Span&lt;char&gt; buffer = stackalloc char[128];
/// var builder = new SpanStringBuilder(buffer);
/// builder.Append("Value: ");
/// builder.Append(42);
/// ReadOnlySpan&lt;char&gt; result = builder.AsSpan();
/// </code>
/// </remarks>
public ref struct SpanStringBuilder
{
    private Span<char> _Buffer;
    private int _Position;

    /// <summary>
    /// Initializes a new instance of <see cref="SpanStringBuilder"/> with the given buffer.
    /// </summary>
    /// <param name="buffer">The destination buffer to write to.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SpanStringBuilder(Span<char> buffer)
    {
        _Buffer = buffer;
        _Position = 0;
    }

    /// <summary>
    /// Gets the current position (number of characters written).
    /// </summary>
    public readonly int Length => _Position;

    /// <summary>
    /// Gets the current capacity of the buffer.
    /// </summary>
    public readonly int Capacity => _Buffer.Length;

    /// <summary>
    /// Gets the remaining capacity in the buffer.
    /// </summary>
    public readonly int Remaining => _Buffer.Length - _Position;

    /// <summary>
    /// Gets the written content as a read-only span.
    /// </summary>
    public readonly ReadOnlySpan<char> AsSpan() => _Buffer.Slice(0, _Position);

    /// <summary>
    /// Clears the builder, resetting the position to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear() => _Position = 0;

    /// <summary>
    /// Moves the write position back by the specified number of characters.
    /// </summary>
    /// <param name="count">The number of characters to move back.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is negative or greater than the current position.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SeekBack(int count)
    {
        if ((uint)count > (uint)_Position)
        {
            throw new ArgumentOutOfRangeException(nameof(count),
                $"Cannot seek back {count} characters when only {_Position} characters have been written.");
        }

        _Position -= count;
    }

    /// <summary>
    /// Tries to move the write position back by the specified number of characters without throwing.
    /// </summary>
    /// <param name="count">The number of characters to move back.</param>
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
    #region Append - String Literals

    /// <summary>
    /// Appends a string literal to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(string? value)
    {
        if (!TryAppend(value))
        {
            throw new InvalidOperationException("Buffer too small for string value.");
        }
    }

    /// <summary>
    /// Appends a character span to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ReadOnlySpan<char> value)
    {
        if (!TryAppend(value))
        {
            throw new InvalidOperationException("Buffer too small for character span value.");
        }
    }

    /// <summary>
    /// Appends a single character to the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(char value)
    {
        if (!TryAppend(value))
        {
            throw new InvalidOperationException("Buffer too small for character value.");
        }
    }

    #endregion

    #region Append - Primitive Types

    /// <summary>Appends an integer value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(int value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for int value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a long value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(long value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for long value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends an unsigned integer value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(uint value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for uint value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends an unsigned long value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ulong value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for ulong value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a short value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(short value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for short value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends an unsigned short value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(ushort value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for ushort value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a byte value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(byte value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for byte value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a signed byte value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(sbyte value)
    {
        if (!((int)value).TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for sbyte value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a boolean value ("True" or "False") to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(bool value)
    {
        if (!TryAppend(value))
        {
            throw new InvalidOperationException("Buffer too small for boolean value.");
        }
    }

    /// <summary>Appends a DateTime value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(DateTime value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for DateTime value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a DateTimeOffset value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(DateTimeOffset value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for DateTimeOffset value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a TimeSpan value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(TimeSpan value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for TimeSpan value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a GUID value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(Guid value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten))
        {
            throw new InvalidOperationException("Buffer too small for Guid value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a 128-bit signed integer value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(Int128 value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for Int128 value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a 128-bit unsigned integer value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(UInt128 value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for UInt128 value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a single-precision floating-point value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(float value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for float value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a double-precision floating-point value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(double value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for double value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a decimal value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(decimal value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for decimal value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a half-precision floating-point value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(Half value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for Half value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a DateOnly value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(DateOnly value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for DateOnly value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a TimeOnly value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(TimeOnly value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for TimeOnly value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a native integer value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(nint value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for nint value.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a native unsigned integer value to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(nuint value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException("Buffer too small for nuint value.");
        }

        _Position += charsWritten;
    }

    #endregion

    #region Append - With Format

    /// <summary>Appends an integer value with a format string to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(int value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format, provider))
        {
            throw new InvalidOperationException("Buffer too small.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a long value with a format string to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(long value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format, provider))
        {
            throw new InvalidOperationException("Buffer too small.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a double value with a format string to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(double value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format, provider))
        {
            throw new InvalidOperationException("Buffer too small.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a float value with a format string to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(float value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format, provider))
        {
            throw new InvalidOperationException("Buffer too small.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a decimal value with a format string to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(decimal value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format, provider))
        {
            throw new InvalidOperationException("Buffer too small.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a DateTime value with a format string to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(DateTime value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format, provider))
        {
            throw new InvalidOperationException("Buffer too small.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a DateTimeOffset value with a format string to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(DateTimeOffset value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format, provider))
        {
            throw new InvalidOperationException("Buffer too small.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a TimeSpan value with a format string to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(TimeSpan value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format, provider))
        {
            throw new InvalidOperationException("Buffer too small.");
        }

        _Position += charsWritten;
    }

    /// <summary>Appends a GUID value with a format string to the buffer.</summary>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Append(Guid value, ReadOnlySpan<char> format)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format))
        {
            throw new InvalidOperationException("Buffer too small.");
        }

        _Position += charsWritten;
    }

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
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format, provider))
        {
            throw new InvalidOperationException($"Buffer too small for {typeof(T).Name} value.");
        }

        _Position += charsWritten;
    }

    #endregion

    #region TryAppend - Non-throwing Variants

    /// <summary>
    /// Tries to append a string to the buffer without throwing.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(string? value)
    {
        if (value is null)
        {
            return true;
        }

        if (Remaining < value.Length)
        {
            return false;
        }

        value.AsSpan().CopyTo(_Buffer.Slice(_Position));
        _Position += value.Length;
        return true;
    }

    /// <summary>
    /// Tries to append a character span to the buffer without throwing.
    /// </summary>
    /// <param name="value">The span to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(ReadOnlySpan<char> value)
    {
        if (Remaining < value.Length)
        {
            return false;
        }

        value.CopyTo(_Buffer.Slice(_Position));
        _Position += value.Length;
        return true;
    }

    /// <summary>
    /// Tries to append a single character to the buffer without throwing.
    /// </summary>
    /// <param name="value">The character to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(char value)
    {
        if (Remaining < 1)
        {
            return false;
        }

        _Buffer[_Position++] = value;
        return true;
    }

    /// <summary>Tries to append an integer value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(int value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a long value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(long value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append an unsigned integer value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(uint value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append an unsigned long value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(ulong value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a short value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(short value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append an unsigned short value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(ushort value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a byte value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(byte value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a signed byte value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(sbyte value)
    {
        if (!((int)value).TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a boolean value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(bool value)
    {
        if (value)
        {
            if (Remaining < 4)
            {
                return false;
            }

            "True".AsSpan().CopyTo(_Buffer.Slice(_Position));
            _Position += 4;
        }
        else
        {
            if (Remaining < 5)
            {
                return false;
            }

            "False".AsSpan().CopyTo(_Buffer.Slice(_Position));
            _Position += 5;
        }
        return true;
    }

    /// <summary>Tries to append a DateTime value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(DateTime value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a DateTimeOffset value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(DateTimeOffset value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a TimeSpan value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(TimeSpan value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a GUID value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(Guid value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a 128-bit signed integer value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(Int128 value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a 128-bit unsigned integer value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(UInt128 value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a float value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(float value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a double value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(double value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a decimal value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(decimal value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a Half value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(Half value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a DateOnly value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(DateOnly value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a TimeOnly value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(TimeOnly value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a native integer value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(nint value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>Tries to append a native unsigned integer value without throwing.</summary>
    /// <param name="value">The value to append.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend(nuint value)
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, default, CultureInfo.InvariantCulture))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    /// <summary>
    /// Tries to append a formattable value with a format string without throwing.
    /// </summary>
    /// <typeparam name="T">The type of the value (must implement ISpanFormattable).</typeparam>
    /// <param name="value">The value to append.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (optional).</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppend<T>(T value, ReadOnlySpan<char> format, IFormatProvider? provider = null)
        where T : ISpanFormattable
    {
        if (!value.TryFormat(_Buffer.Slice(_Position), out int charsWritten, format, provider))
        {
            return false;
        }

        _Position += charsWritten;
        return true;
    }

    #endregion

    #region AppendLine

    /// <summary>Appends the platform-specific newline sequence to the buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine() => Append(Environment.NewLine);

    /// <summary>Appends a string followed by a newline to the buffer.</summary>
    /// <param name="value">The string to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine(string? value)
    {
        int requiredLength = checked((value?.Length ?? 0) + Environment.NewLine.Length);
        if (Remaining < requiredLength)
        {
            throw new InvalidOperationException("Buffer too small for line value.");
        }

        Append(value);
        AppendLine();
    }

    /// <summary>Appends a character span followed by a newline to the buffer.</summary>
    /// <param name="value">The span to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendLine(ReadOnlySpan<char> value)
    {
        int requiredLength = checked(value.Length + Environment.NewLine.Length);
        if (Remaining < requiredLength)
        {
            throw new InvalidOperationException("Buffer too small for line span value.");
        }

        Append(value);
        AppendLine();
    }

    #endregion

    #region Hex/Binary

    private static ReadOnlySpan<char> _HexChars => "0123456789ABCDEF";

    /// <summary>Appends a byte as 2 hexadecimal characters to the buffer.</summary>
    /// <param name="value">The byte value to format.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex2(byte value)
    {
        if (!TryAppendHex2(value))
        {
            throw new InvalidOperationException("Buffer too small for 2-digit hexadecimal byte value.");
        }
    }

    /// <summary>Appends a ushort as 4 hexadecimal characters to the buffer.</summary>
    /// <param name="value">The value to format.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex4(ushort value)
    {
        if (!TryAppendHex4(value))
        {
            throw new InvalidOperationException("Buffer too small for 4-digit hexadecimal ushort value.");
        }
    }

    /// <summary>Appends a uint as 8 hexadecimal characters to the buffer.</summary>
    /// <param name="value">The value to format.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex8(uint value)
    {
        if (!TryAppendHex8(value))
        {
            throw new InvalidOperationException("Buffer too small for 8-digit hexadecimal uint value.");
        }
    }

    /// <summary>Appends a ulong as 16 hexadecimal characters to the buffer.</summary>
    /// <param name="value">The value to format.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendHex16(ulong value)
    {
        if (!TryAppendHex16(value))
        {
            throw new InvalidOperationException("Buffer too small for 16-digit hexadecimal ulong value.");
        }
    }

    /// <summary>Appends a byte as 8 binary characters to the buffer.</summary>
    /// <param name="value">The byte value to format.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary8(byte value)
    {
        if (!TryAppendBinary8(value))
        {
            throw new InvalidOperationException("Buffer too small for 8-character binary byte value.");
        }
    }

    /// <summary>Appends a ushort as 16 binary characters to the buffer.</summary>
    /// <param name="value">The value to format.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary16(ushort value)
    {
        if (!TryAppendBinary16(value))
        {
            throw new InvalidOperationException("Buffer too small for 16-character binary ushort value.");
        }
    }

    /// <summary>Appends a uint as 32 binary characters to the buffer.</summary>
    /// <param name="value">The value to format.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary32(uint value)
    {
        if (!TryAppendBinary32(value))
        {
            throw new InvalidOperationException("Buffer too small for 32-character binary uint value.");
        }
    }

    /// <summary>Appends a ulong as 64 binary characters to the buffer.</summary>
    /// <param name="value">The value to format.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AppendBinary64(ulong value)
    {
        if (!TryAppendBinary64(value))
        {
            throw new InvalidOperationException("Buffer too small for 64-character binary ulong value.");
        }
    }

    /// <summary>Tries to append a byte as 2 hexadecimal characters without throwing.</summary>
    /// <param name="value">The byte value to format.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex2(byte value)
    {
        if (Remaining < 2)
        {
            return false;
        }

        _Buffer[_Position++] = _HexChars[value >> 4];
        _Buffer[_Position++] = _HexChars[value & 0xF];
        return true;
    }

    /// <summary>Tries to append a ushort as 4 hexadecimal characters without throwing.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>True if successful; false if buffer space is insufficient.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAppendHex4(ushort value)
    {
        if (Remaining < 4)
        {
            return false;
        }

        _Buffer[_Position++] = _HexChars[(value >> 12) & 0xF];
        _Buffer[_Position++] = _HexChars[(value >> 8) & 0xF];
        _Buffer[_Position++] = _HexChars[(value >> 4) & 0xF];
        _Buffer[_Position++] = _HexChars[value & 0xF];
        return true;
    }

    /// <summary>Tries to append a uint as 8 hexadecimal characters without throwing.</summary>
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

    /// <summary>Tries to append a ulong as 16 hexadecimal characters without throwing.</summary>
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

    /// <summary>Tries to append a byte as 8 binary characters without throwing.</summary>
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
            _Buffer[_Position++] = (char)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a ushort as 16 binary characters without throwing.</summary>
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
            _Buffer[_Position++] = (char)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a uint as 32 binary characters without throwing.</summary>
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
            _Buffer[_Position++] = (char)('0' + ((value >> i) & 1));
        }
        return true;
    }

    /// <summary>Tries to append a ulong as 64 binary characters without throwing.</summary>
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
            _Buffer[_Position++] = (char)('0' + (int)((value >> i) & 1));
        }
        return true;
    }

    #endregion

    #region ToString

    /// <summary>Returns the written content as a new string.</summary>
    /// <returns>A string containing the written characters.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly override string ToString() => new string(_Buffer.Slice(0, _Position));

    /// <summary>Gets the written content as a read-only span.</summary>
    public readonly ReadOnlySpan<char> WrittenSpan => _Buffer.Slice(0, _Position);

    #endregion
}
