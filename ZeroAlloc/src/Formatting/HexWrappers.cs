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
    private static ReadOnlySpan<char> HexChars => "0123456789ABCDEF";

    /// <summary>
    /// Uppercase hex byte lookup table (ASCII).
    /// </summary>
    private static ReadOnlySpan<byte> HexBytes => "0123456789ABCDEF"u8;

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

        destination[0] = HexChars[_Value >> 4];
        destination[1] = HexChars[_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <summary>
    /// Formats the byte value as 2-character uppercase hex (UTF-8).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            bytesWritten = 0;
            return false;
        }

        destination[0] = HexBytes[_Value >> 4];
        destination[1] = HexBytes[_Value & 0xF];
        bytesWritten = FormattedLength;
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
/// Formats a ushort as 4-character uppercase hexadecimal (0000-FFFF).
/// </summary>
public readonly struct Hex4 : ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>The formatted length (4 characters).</summary>
    public const int FormattedLength = 4;
    private static ReadOnlySpan<char> HexChars => "0123456789ABCDEF";
    private static ReadOnlySpan<byte> HexBytes => "0123456789ABCDEF"u8;

    private readonly ushort _Value;

    /// <summary>Creates a new Hex4 formatter for the specified value.</summary>
    /// <param name="value">The ushort value to format as hex.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex4(ushort value) => _Value = value;

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            charsWritten = 0;
            return false;
        }

        destination[0] = HexChars[(_Value >> 12) & 0xF];
        destination[1] = HexChars[(_Value >> 8) & 0xF];
        destination[2] = HexChars[(_Value >> 4) & 0xF];
        destination[3] = HexChars[_Value & 0xF];
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            bytesWritten = 0;
            return false;
        }

        destination[0] = HexBytes[(_Value >> 12) & 0xF];
        destination[1] = HexBytes[(_Value >> 8) & 0xF];
        destination[2] = HexBytes[(_Value >> 4) & 0xF];
        destination[3] = HexBytes[_Value & 0xF];
        bytesWritten = FormattedLength;
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
/// Formats a uint as 8-character uppercase hexadecimal (00000000-FFFFFFFF).
/// </summary>
public readonly struct Hex8 : ISpanFormattable, IUtf8SpanFormattable
{
    /// <summary>The formatted length (8 characters).</summary>
    public const int FormattedLength = 8;
    private static ReadOnlySpan<char> HexChars => "0123456789ABCDEF";
    private static ReadOnlySpan<byte> HexBytes => "0123456789ABCDEF"u8;

    private readonly uint _Value;

    /// <summary>Creates a new Hex8 formatter for the specified value.</summary>
    /// <param name="value">The uint value to format as hex.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex8(uint value) => _Value = value;

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
            destination[i] = HexChars[(int)(_Value >> (28 - i * 4)) & 0xF];
        }
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            bytesWritten = 0;
            return false;
        }

        for (int i = 0; i < 8; i++)
        {
            destination[i] = HexBytes[(int)(_Value >> (28 - i * 4)) & 0xF];
        }
        bytesWritten = FormattedLength;
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
    private static ReadOnlySpan<char> HexChars => "0123456789ABCDEF";
    private static ReadOnlySpan<byte> HexBytes => "0123456789ABCDEF"u8;

    private readonly ulong _Value;

    /// <summary>Creates a new Hex16 formatter for the specified value.</summary>
    /// <param name="value">The ulong value to format as hex.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Hex16(ulong value) => _Value = value;

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
            destination[i] = HexChars[(int)(_Value >> (60 - i * 4)) & 0xF];
        }
        charsWritten = FormattedLength;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (destination.Length < FormattedLength)
        {
            bytesWritten = 0;
            return false;
        }

        for (int i = 0; i < 16; i++)
        {
            destination[i] = HexBytes[(int)(_Value >> (60 - i * 4)) & 0xF];
        }
        bytesWritten = FormattedLength;
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
