// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// ZeroAlloc - Hex Formatting Wrappers
// ============================================================================
// These structs implement ISpanFormattable and IUtf8SpanFormattable for
// formatting values as uppercase hexadecimal strings with fixed widths.
//
// Usage:
//   using TempString temp = ZA.String("Value: ", new Hex2(0xFF));
//   // Result: "Value: FF"
// ============================================================================

namespace ZeroAlloc;

/// <summary>
/// Formats a nibble (4 bits) as 1-character uppercase hexadecimal (0-F).
/// </summary>
/// <remarks>
/// <para><b>Usage:</b></para>
/// <code>
/// using TempString temp = ZA.String("Nibble: ", new Hex1(0xA));
/// // Result: "Nibble: A"
/// </code>
/// </remarks>
public readonly struct Hex1 : ISpanFormattable, IUtf8SpanFormattable
{
    // ========================================================================
    // CONSTANTS
    // ========================================================================

    /// <summary>
    /// The formatted output is always exactly 1 character.
    /// </summary>
    public const int FormattedLength = 1;

    /// <summary>
    /// Uppercase hex character lookup table.
    /// </summary>
    private static ReadOnlySpan<char> _HexChars => "0123456789ABCDEF";

    /// <summary>
    /// Uppercase hex byte lookup table (ASCII).
    /// </summary>
    private static ReadOnlySpan<byte> _HexBytes => "0123456789ABCDEF"u8;

    // ========================================================================
    // FIELDS
    // ========================================================================

    /// <summary>
    /// The nibble value to format (0-15).
    /// </summary>
    private readonly byte _Value;

    // ========================================================================
    // CONSTRUCTOR
    // ========================================================================

    /// <summary>
    /// Creates a new <see cref="Hex1"/> wrapper for the given nibble.
    /// </summary>
    /// <param name="value">The nibble value to format as hex (only lower 4 bits used).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex1(byte value)
    {
        _Value = (byte)(value & 0xF);
    }

    // ========================================================================
    // ISPANFORMATTABLE IMPLEMENTATION
    // ========================================================================

    /// <summary>
    /// Formats the nibble value as 1-character uppercase hex.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexChars[_Value];
        charsWritten = FormattedLength;
        return true;
    }

    /// <summary>
    /// Formats the nibble value as 1-character uppercase hex (UTF-8).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexBytes[_Value];
        charsWritten = FormattedLength;
        return true;
    }

    /// <summary>
    /// Returns the hex string representation.
    /// </summary>
    public override string ToString()
    {
        return string.Create(FormattedLength, _Value, static (chars, value) =>
        {
            ReadOnlySpan<char> hex = "0123456789ABCDEF";
            chars[0] = hex[value];
        });
    }

    /// <summary>
    /// Required by IFormattable - delegates to parameterless ToString.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    // ========================================================================
    // OPERATORS
    // ========================================================================

    /// <summary>
    /// Implicit conversion from byte to Hex1.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Hex1(byte value) => new Hex1(value);
}

/// <summary>
/// Formats a byte as 2-character uppercase hexadecimal (00-FF).
/// </summary>
/// <remarks>
/// <para><b>Usage:</b></para>
/// <code>
/// using TempString temp = ZA.String("Byte: ", new Hex2(0xFF));
/// // Result: "Byte: FF"
/// </code>
/// </remarks>
public readonly struct Hex2 : ISpanFormattable, IUtf8SpanFormattable
{
    // ========================================================================
    // CONSTANTS
    // ========================================================================

    /// <summary>
    /// The formatted output is always exactly 2 characters.
    /// </summary>
    public const int FormattedLength = 2;

    /// <summary>
    /// Uppercase hex character lookup table.
    /// </summary>
    private static ReadOnlySpan<char> _HexChars => "0123456789ABCDEF";

    /// <summary>
    /// Uppercase hex byte lookup table (ASCII).
    /// </summary>
    private static ReadOnlySpan<byte> _HexBytes => "0123456789ABCDEF"u8;

    // ========================================================================
    // FIELDS
    // ========================================================================

    /// <summary>
    /// The byte value to format.
    /// </summary>
    private readonly byte _Value;

    // ========================================================================
    // CONSTRUCTOR
    // ========================================================================

    /// <summary>
    /// Creates a new <see cref="Hex2"/> wrapper for the given byte.
    /// </summary>
    /// <param name="value">The byte value to format as hex.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex2(byte value)
    {
        _Value = value;
    }

    // ========================================================================
    // ISPANFORMATTABLE IMPLEMENTATION
    // ========================================================================

    /// <summary>
    /// Formats the byte value as 2-character uppercase hex.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexChars[_Value >> 4];
        destination[1] = _HexChars[_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <summary>
    /// Formats the byte value as 2-character uppercase hex (UTF-8).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexBytes[_Value >> 4];
        destination[1] = _HexBytes[_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <summary>
    /// Returns the hex string representation.
    /// </summary>
    public override string ToString()
    {
        return string.Create(FormattedLength, _Value, static (chars, value) =>
        {
            ReadOnlySpan<char> hex = "0123456789ABCDEF";
            chars[0] = hex[value >> 4];
            chars[1] = hex[value & 0xF];
        });
    }

    /// <summary>
    /// Required by IFormattable - delegates to parameterless ToString.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    // ========================================================================
    // OPERATORS
    // ========================================================================

    /// <summary>
    /// Implicit conversion from byte to Hex2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Hex2(byte value) => new Hex2(value);
}

/// <summary>
/// Formats a value as 3-character uppercase hexadecimal (000-FFF).
/// Useful for 12-bit values.
/// </summary>
public readonly struct Hex3 : ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>The formatted length (3 characters).</summary>
    public const int FormattedLength = 3;
    private static ReadOnlySpan<char> _HexChars => "0123456789ABCDEF";
    private static ReadOnlySpan<byte> _HexBytes => "0123456789ABCDEF"u8;

    private readonly ushort _Value;

    /// <summary>Creates a new Hex3 formatter for the specified value.</summary>
    /// <param name="value">The value to format as hex (only lower 12 bits used).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex3(ushort value)
    {
        _Value = (ushort)(value & 0xFFF);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexChars[(_Value >> 8) & 0xF];
        destination[1] = _HexChars[(_Value >> 4) & 0xF];
        destination[2] = _HexChars[_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexBytes[(_Value >> 8) & 0xF];
        destination[1] = _HexBytes[(_Value >> 4) & 0xF];
        destination[2] = _HexBytes[_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Create(FormattedLength, _Value, static (chars, value) =>
        {
            ReadOnlySpan<char> hex = "0123456789ABCDEF";
            chars[0] = hex[(value >> 8) & 0xF];
            chars[1] = hex[(value >> 4) & 0xF];
            chars[2] = hex[value & 0xF];
        });
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a ushort to Hex3.</summary>
    /// <param name="value">The ushort value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Hex3(ushort value) => new Hex3(value);
}

/// <summary>
/// Formats a ushort as 4-character uppercase hexadecimal (0000-FFFF).
/// </summary>
public readonly struct Hex4 : ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>The formatted length (4 characters).</summary>
    public const int FormattedLength = 4;
    private static ReadOnlySpan<char> _HexChars => "0123456789ABCDEF";
    private static ReadOnlySpan<byte> _HexBytes => "0123456789ABCDEF"u8;

    private readonly ushort _Value;

    /// <summary>Creates a new Hex4 formatter for the specified value.</summary>
    /// <param name="value">The ushort value to format as hex.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex4(ushort value)
    {
        _Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexChars[(_Value >> 12) & 0xF];
        destination[1] = _HexChars[(_Value >> 8) & 0xF];
        destination[2] = _HexChars[(_Value >> 4) & 0xF];
        destination[3] = _HexChars[_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexBytes[(_Value >> 12) & 0xF];
        destination[1] = _HexBytes[(_Value >> 8) & 0xF];
        destination[2] = _HexBytes[(_Value >> 4) & 0xF];
        destination[3] = _HexBytes[_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Create(FormattedLength, _Value, static (chars, value) =>
        {
            ReadOnlySpan<char> hex = "0123456789ABCDEF";
            chars[0] = hex[(value >> 12) & 0xF];
            chars[1] = hex[(value >> 8) & 0xF];
            chars[2] = hex[(value >> 4) & 0xF];
            chars[3] = hex[value & 0xF];
        });
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a ushort to Hex4.</summary>
    /// <param name="value">The ushort value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Hex4(ushort value) => new Hex4(value);
}

/// <summary>
/// Formats a value as 5-character uppercase hexadecimal (00000-FFFFF).
/// Useful for 20-bit values.
/// </summary>
public readonly struct Hex5 : ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>The formatted length (5 characters).</summary>
    public const int FormattedLength = 5;
    private static ReadOnlySpan<char> _HexChars => "0123456789ABCDEF";
    private static ReadOnlySpan<byte> _HexBytes => "0123456789ABCDEF"u8;

    private readonly uint _Value;

    /// <summary>Creates a new Hex5 formatter for the specified value.</summary>
    /// <param name="value">The value to format as hex (only lower 20 bits used).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex5(uint value)
    {
        _Value = value & 0xFFFFF;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexChars[(int)(_Value >> 16) & 0xF];
        destination[1] = _HexChars[(int)(_Value >> 12) & 0xF];
        destination[2] = _HexChars[(int)(_Value >> 8) & 0xF];
        destination[3] = _HexChars[(int)(_Value >> 4) & 0xF];
        destination[4] = _HexChars[(int)_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexBytes[(int)(_Value >> 16) & 0xF];
        destination[1] = _HexBytes[(int)(_Value >> 12) & 0xF];
        destination[2] = _HexBytes[(int)(_Value >> 8) & 0xF];
        destination[3] = _HexBytes[(int)(_Value >> 4) & 0xF];
        destination[4] = _HexBytes[(int)_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Create(FormattedLength, _Value, static (chars, value) =>
        {
            ReadOnlySpan<char> hex = "0123456789ABCDEF";
            chars[0] = hex[(int)(value >> 16) & 0xF];
            chars[1] = hex[(int)(value >> 12) & 0xF];
            chars[2] = hex[(int)(value >> 8) & 0xF];
            chars[3] = hex[(int)(value >> 4) & 0xF];
            chars[4] = hex[(int)value & 0xF];
        });
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a uint to Hex5.</summary>
    /// <param name="value">The uint value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Hex5(uint value) => new Hex5(value);
}

/// <summary>
/// Formats a value as 6-character uppercase hexadecimal (000000-FFFFFF).
/// Useful for 24-bit values like RGB colors.
/// </summary>
public readonly struct Hex6 : ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>The formatted length (6 characters).</summary>
    public const int FormattedLength = 6;
    private static ReadOnlySpan<char> _HexChars => "0123456789ABCDEF";
    private static ReadOnlySpan<byte> _HexBytes => "0123456789ABCDEF"u8;

    private readonly uint _Value;

    /// <summary>Creates a new Hex6 formatter for the specified value.</summary>
    /// <param name="value">The value to format as hex (only lower 24 bits used).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex6(uint value)
    {
        _Value = value & 0xFFFFFF;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexChars[(int)(_Value >> 20) & 0xF];
        destination[1] = _HexChars[(int)(_Value >> 16) & 0xF];
        destination[2] = _HexChars[(int)(_Value >> 12) & 0xF];
        destination[3] = _HexChars[(int)(_Value >> 8) & 0xF];
        destination[4] = _HexChars[(int)(_Value >> 4) & 0xF];
        destination[5] = _HexChars[(int)_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexBytes[(int)(_Value >> 20) & 0xF];
        destination[1] = _HexBytes[(int)(_Value >> 16) & 0xF];
        destination[2] = _HexBytes[(int)(_Value >> 12) & 0xF];
        destination[3] = _HexBytes[(int)(_Value >> 8) & 0xF];
        destination[4] = _HexBytes[(int)(_Value >> 4) & 0xF];
        destination[5] = _HexBytes[(int)_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Create(FormattedLength, _Value, static (chars, value) =>
        {
            ReadOnlySpan<char> hex = "0123456789ABCDEF";
            chars[0] = hex[(int)(value >> 20) & 0xF];
            chars[1] = hex[(int)(value >> 16) & 0xF];
            chars[2] = hex[(int)(value >> 12) & 0xF];
            chars[3] = hex[(int)(value >> 8) & 0xF];
            chars[4] = hex[(int)(value >> 4) & 0xF];
            chars[5] = hex[(int)value & 0xF];
        });
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a uint to Hex6.</summary>
    /// <param name="value">The uint value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Hex6(uint value) => new Hex6(value);
}

/// <summary>
/// Formats a value as 7-character uppercase hexadecimal (0000000-FFFFFFF).
/// Useful for 28-bit values.
/// </summary>
public readonly struct Hex7 : ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>The formatted length (7 characters).</summary>
    public const int FormattedLength = 7;
    private static ReadOnlySpan<char> _HexChars => "0123456789ABCDEF";
    private static ReadOnlySpan<byte> _HexBytes => "0123456789ABCDEF"u8;

    private readonly uint _Value;

    /// <summary>Creates a new Hex7 formatter for the specified value.</summary>
    /// <param name="value">The value to format as hex (only lower 28 bits used).</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex7(uint value)
    {
        _Value = value & 0xFFFFFFF;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexChars[(int)(_Value >> 24) & 0xF];
        destination[1] = _HexChars[(int)(_Value >> 20) & 0xF];
        destination[2] = _HexChars[(int)(_Value >> 16) & 0xF];
        destination[3] = _HexChars[(int)(_Value >> 12) & 0xF];
        destination[4] = _HexChars[(int)(_Value >> 8) & 0xF];
        destination[5] = _HexChars[(int)(_Value >> 4) & 0xF];
        destination[6] = _HexChars[(int)_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = _HexBytes[(int)(_Value >> 24) & 0xF];
        destination[1] = _HexBytes[(int)(_Value >> 20) & 0xF];
        destination[2] = _HexBytes[(int)(_Value >> 16) & 0xF];
        destination[3] = _HexBytes[(int)(_Value >> 12) & 0xF];
        destination[4] = _HexBytes[(int)(_Value >> 8) & 0xF];
        destination[5] = _HexBytes[(int)(_Value >> 4) & 0xF];
        destination[6] = _HexBytes[(int)_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Create(FormattedLength, _Value, static (chars, value) =>
        {
            ReadOnlySpan<char> hex = "0123456789ABCDEF";
            chars[0] = hex[(int)(value >> 24) & 0xF];
            chars[1] = hex[(int)(value >> 20) & 0xF];
            chars[2] = hex[(int)(value >> 16) & 0xF];
            chars[3] = hex[(int)(value >> 12) & 0xF];
            chars[4] = hex[(int)(value >> 8) & 0xF];
            chars[5] = hex[(int)(value >> 4) & 0xF];
            chars[6] = hex[(int)value & 0xF];
        });
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a uint to Hex7.</summary>
    /// <param name="value">The uint value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Hex7(uint value) => new Hex7(value);
}

/// <summary>
/// Formats a uint as 8-character uppercase hexadecimal (00000000-FFFFFFFF).
/// </summary>
public readonly struct Hex8 : ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>The formatted length (8 characters).</summary>
    public const int FormattedLength = 8;
    private static ReadOnlySpan<char> _HexChars => "0123456789ABCDEF";
    private static ReadOnlySpan<byte> _HexBytes => "0123456789ABCDEF"u8;

    private readonly uint _Value;

    /// <summary>Creates a new Hex8 formatter for the specified value.</summary>
    /// <param name="value">The uint value to format as hex.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex8(uint value)
    {
        _Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        for (int i = 0; i < 8; i++)
        {
            destination[i] = _HexChars[(int)(_Value >> (28 - i * 4)) & 0xF];
        }
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        for (int i = 0; i < 8; i++)
        {
            destination[i] = _HexBytes[(int)(_Value >> (28 - i * 4)) & 0xF];
        }
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Create(FormattedLength, _Value, static (chars, value) =>
        {
            ReadOnlySpan<char> hex = "0123456789ABCDEF";
            for (int i = 0; i < 8; i++)
            {
                chars[i] = hex[(int)(value >> (28 - i * 4)) & 0xF];
            }
        });
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a uint to Hex8.</summary>
    /// <param name="value">The uint value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Hex8(uint value) => new Hex8(value);
}

/// <summary>
/// Formats a ulong as 16-character uppercase hexadecimal.
/// </summary>
public readonly struct Hex16 : ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>The formatted length (16 characters).</summary>
    public const int FormattedLength = 16;
    private static ReadOnlySpan<char> _HexChars => "0123456789ABCDEF";
    private static ReadOnlySpan<byte> _HexBytes => "0123456789ABCDEF"u8;

    private readonly ulong _Value;

    /// <summary>Creates a new Hex16 formatter for the specified value.</summary>
    /// <param name="value">The ulong value to format as hex.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex16(ulong value)
    {
        _Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        for (int i = 0; i < 16; i++)
        {
            destination[i] = _HexChars[(int)(_Value >> (60 - i * 4)) & 0xF];
        }
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        for (int i = 0; i < 16; i++)
        {
            destination[i] = _HexBytes[(int)(_Value >> (60 - i * 4)) & 0xF];
        }
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return string.Create(FormattedLength, _Value, static (chars, value) =>
        {
            ReadOnlySpan<char> hex = "0123456789ABCDEF";
            for (int i = 0; i < 16; i++)
            {
                chars[i] = hex[(int)(value >> (60 - i * 4)) & 0xF];
            }
        });
    }

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a ulong to Hex16.</summary>
    /// <param name="value">The ulong value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Hex16(ulong value) => new Hex16(value);
}
