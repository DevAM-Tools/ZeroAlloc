// ============================================================================
// ZeroAlloc - String Encoding Wrappers
// ============================================================================
// Short-named wrappers for encoding strings to binary with optional length prefix.
//
// Naming convention: [Encoding][LengthPrefix]
//   Encoding: Utf8, Utf16BE, Utf16LE, Utf32BE, Utf32LE, Ascii, Latin1
//   LengthPrefix: (none), Var, FixBE, FixLE
//
// Examples:
//   Utf8      = UTF-8 without length prefix
//   Utf8Var   = UTF-8 with VarInt length prefix
//   Utf8FixBE = UTF-8 with 4-byte big-endian length prefix
//   Utf8FixLE = UTF-8 with 4-byte little-endian length prefix
// ============================================================================

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

#region Helper Methods

internal static class EncodingHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarInt(Span<byte> dest, int value)
    {
        int pos = 0;
        uint v = (uint)value;
        while (v >= 0x80)
        {
            dest[pos++] = (byte)(v | 0x80);
            v >>= 7;
        }
        dest[pos++] = (byte)v;
        return pos;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int VarIntSize(int value)
    {
        uint v = (uint)value;
        int size = 1;
        while (v >= 0x80) { v >>= 7; size++; }
        return size;
    }
}

#endregion

#region UTF-8 Wrappers

/// <summary>UTF-8 encoded string without length prefix.</summary>
public readonly struct Utf8 : IUtf8SpanFormattable
{
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { bytesWritten = 0; return true; }
        int needed = Encoding.UTF8.GetByteCount(_value);
        if (destination.Length < needed) { bytesWritten = 0; return false; }
        bytesWritten = Encoding.UTF8.GetBytes(_value, destination);
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf8"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf8(string? v) => new(v);
}

/// <summary>UTF-8 encoded string with VarInt length prefix.</summary>
public readonly struct Utf8Var : IUtf8SpanFormattable, IBinaryParsable<Utf8Var>
{
    private readonly string? _value;

    /// <summary>Gets the string value.</summary>
    public string Value => _value ?? "";

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8Var(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { destination[0] = 0; bytesWritten = 1; return true; }
        int strBytes = Encoding.UTF8.GetByteCount(_value);
        int varIntSize = EncodingHelper.VarIntSize(strBytes);
        if (destination.Length < varIntSize + strBytes) { bytesWritten = 0; return false; }
        int pos = EncodingHelper.WriteVarInt(destination, strBytes);
        Encoding.UTF8.GetBytes(_value, destination.Slice(pos));
        bytesWritten = pos + strBytes;
        return true;
    }

    /// <inheritdoc/>
    public static bool TryGetSerializedSize(out int size) { size = 0; return false; }

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<byte> source, out Utf8Var value, out int bytesConsumed)
    {
        if (!VarInt.TryParse(source, out VarInt lengthVar, out int lengthBytes))
        {
            value = default; bytesConsumed = 0; return false;
        }
        int length = (int)lengthVar.Value;
        if (source.Length < lengthBytes + length)
        {
            value = default; bytesConsumed = 0; return false;
        }
        value = new Utf8Var(Encoding.UTF8.GetString(source.Slice(lengthBytes, length)));
        bytesConsumed = lengthBytes + length;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf8Var"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf8Var(string? v) => new(v);

    /// <summary>Implicitly converts a <see cref="Utf8Var"/> to string.</summary>
    /// <param name="v">The wrapper to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Utf8Var v) => v.Value;
}

/// <summary>UTF-8 encoded string with 4-byte big-endian length prefix.</summary>
public readonly struct Utf8FixBE : IUtf8SpanFormattable, IBinaryParsable<Utf8FixBE>
{
    private readonly string? _value;

    /// <summary>Gets the string value.</summary>
    public string Value => _value ?? "";

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8FixBE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32BigEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        int strBytes = Encoding.UTF8.GetByteCount(_value);
        if (destination.Length < 4 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)strBytes);
        Encoding.UTF8.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + strBytes;
        return true;
    }

    /// <inheritdoc/>
    public static bool TryGetSerializedSize(out int size) { size = 0; return false; }

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<byte> source, out Utf8FixBE value, out int bytesConsumed)
    {
        if (source.Length < 4) { value = default; bytesConsumed = 0; return false; }
        int length = (int)BinaryPrimitives.ReadUInt32BigEndian(source);
        if (source.Length < 4 + length) { value = default; bytesConsumed = 0; return false; }
        value = new Utf8FixBE(Encoding.UTF8.GetString(source.Slice(4, length)));
        bytesConsumed = 4 + length;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf8FixBE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf8FixBE(string? v) => new(v);

    /// <summary>Implicitly converts a <see cref="Utf8FixBE"/> to string.</summary>
    /// <param name="v">The wrapper to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Utf8FixBE v) => v.Value;
}

/// <summary>UTF-8 encoded string with 4-byte little-endian length prefix.</summary>
public readonly struct Utf8FixLE : IUtf8SpanFormattable, IBinaryParsable<Utf8FixLE>
{
    private readonly string? _value;

    /// <summary>Gets the string value.</summary>
    public string Value => _value ?? "";

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8FixLE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32LittleEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        int strBytes = Encoding.UTF8.GetByteCount(_value);
        if (destination.Length < 4 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)strBytes);
        Encoding.UTF8.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + strBytes;
        return true;
    }

    /// <inheritdoc/>
    public static bool TryGetSerializedSize(out int size) { size = 0; return false; }

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<byte> source, out Utf8FixLE value, out int bytesConsumed)
    {
        if (source.Length < 4) { value = default; bytesConsumed = 0; return false; }
        int length = (int)BinaryPrimitives.ReadUInt32LittleEndian(source);
        if (source.Length < 4 + length) { value = default; bytesConsumed = 0; return false; }
        value = new Utf8FixLE(Encoding.UTF8.GetString(source.Slice(4, length)));
        bytesConsumed = 4 + length;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf8FixLE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf8FixLE(string? v) => new(v);

    /// <summary>Implicitly converts a <see cref="Utf8FixLE"/> to string.</summary>
    /// <param name="v">The wrapper to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Utf8FixLE v) => v.Value;
}

/// <summary>UTF-8 encoded string with 16-bit (ushort) little-endian length prefix.</summary>
public readonly struct Utf8Fix16LE : IUtf8SpanFormattable, IBinaryParsable<Utf8Fix16LE>
{
    private readonly string? _value;

    /// <summary>Gets the string value.</summary>
    public string Value => _value ?? "";

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8Fix16LE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt16LittleEndian(destination, 0); bytesWritten = 2; return destination.Length >= 2; }
        int strBytes = Encoding.UTF8.GetByteCount(_value);
        if (destination.Length < 2 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt16LittleEndian(destination, (ushort)strBytes);
        Encoding.UTF8.GetBytes(_value, destination.Slice(2));
        bytesWritten = 2 + strBytes;
        return true;
    }

    /// <inheritdoc/>
    public static bool TryGetSerializedSize(out int size) { size = 0; return false; }

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<byte> source, out Utf8Fix16LE value, out int bytesConsumed)
    {
        if (source.Length < 2) { value = default; bytesConsumed = 0; return false; }
        int length = BinaryPrimitives.ReadUInt16LittleEndian(source);
        if (source.Length < 2 + length) { value = default; bytesConsumed = 0; return false; }
        value = new Utf8Fix16LE(Encoding.UTF8.GetString(source.Slice(2, length)));
        bytesConsumed = 2 + length;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf8Fix16LE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf8Fix16LE(string? v) => new(v);

    /// <summary>Implicitly converts a <see cref="Utf8Fix16LE"/> to string.</summary>
    /// <param name="v">The wrapper to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Utf8Fix16LE v) => v.Value;
}

/// <summary>UTF-8 encoded string with null terminator.</summary>
public readonly struct Utf8Z : IUtf8SpanFormattable, IBinaryParsable<Utf8Z>
{
    private readonly string? _value;

    /// <summary>Gets the string value.</summary>
    public string Value => _value ?? "";

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8Z(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { destination[0] = 0; bytesWritten = 1; return destination.Length >= 1; }
        int strBytes = Encoding.UTF8.GetByteCount(_value);
        if (destination.Length < strBytes + 1) { bytesWritten = 0; return false; }
        Encoding.UTF8.GetBytes(_value, destination);
        destination[strBytes] = 0;
        bytesWritten = strBytes + 1;
        return true;
    }

    /// <inheritdoc/>
    public static bool TryGetSerializedSize(out int size) { size = 0; return false; }

    /// <inheritdoc/>
    public static bool TryParse(ReadOnlySpan<byte> source, out Utf8Z value, out int bytesConsumed)
    {
        int nullIndex = source.IndexOf((byte)0);
        if (nullIndex < 0) { value = default; bytesConsumed = 0; return false; }
        value = new Utf8Z(Encoding.UTF8.GetString(source.Slice(0, nullIndex)));
        bytesConsumed = nullIndex + 1;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf8Z"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf8Z(string? v) => new(v);

    /// <summary>Implicitly converts a <see cref="Utf8Z"/> to string.</summary>
    /// <param name="v">The wrapper to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(Utf8Z v) => v.Value;
}

#endregion

#region UTF-16 Big Endian Wrappers

/// <summary>UTF-16 BE encoded string without length prefix.</summary>
public readonly struct Utf16BE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.BigEndianUnicode;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf16BE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { bytesWritten = 0; return true; }
        int needed = _value.Length * 2;
        if (destination.Length < needed) { bytesWritten = 0; return false; }
        bytesWritten = Enc.GetBytes(_value, destination);
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf16BE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf16BE(string? v) => new(v);
}

/// <summary>UTF-16 BE encoded string with VarInt length prefix (byte count).</summary>
public readonly struct Utf16BEVar : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.BigEndianUnicode;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf16BEVar(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { destination[0] = 0; bytesWritten = 1; return true; }
        int strBytes = _value.Length * 2;
        int varIntSize = EncodingHelper.VarIntSize(strBytes);
        if (destination.Length < varIntSize + strBytes) { bytesWritten = 0; return false; }
        int pos = EncodingHelper.WriteVarInt(destination, strBytes);
        Enc.GetBytes(_value, destination.Slice(pos));
        bytesWritten = pos + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf16BEVar"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf16BEVar(string? v) => new(v);
}

/// <summary>UTF-16 BE encoded string with 4-byte big-endian length prefix.</summary>
public readonly struct Utf16BEFixBE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.BigEndianUnicode;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf16BEFixBE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32BigEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        int strBytes = _value.Length * 2;
        if (destination.Length < 4 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)strBytes);
        Enc.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf16BEFixBE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf16BEFixBE(string? v) => new(v);
}

/// <summary>UTF-16 BE encoded string with 4-byte little-endian length prefix.</summary>
public readonly struct Utf16BEFixLE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.BigEndianUnicode;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf16BEFixLE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32LittleEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        int strBytes = _value.Length * 2;
        if (destination.Length < 4 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)strBytes);
        Enc.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf16BEFixLE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf16BEFixLE(string? v) => new(v);
}

#endregion

#region UTF-16 Little Endian Wrappers

/// <summary>UTF-16 LE encoded string without length prefix.</summary>
public readonly struct Utf16LE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.Unicode;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf16LE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { bytesWritten = 0; return true; }
        int needed = _value.Length * 2;
        if (destination.Length < needed) { bytesWritten = 0; return false; }
        bytesWritten = Enc.GetBytes(_value, destination);
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf16LE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf16LE(string? v) => new(v);
}

/// <summary>UTF-16 LE encoded string with VarInt length prefix (byte count).</summary>
public readonly struct Utf16LEVar : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.Unicode;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf16LEVar(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { destination[0] = 0; bytesWritten = 1; return true; }
        int strBytes = _value.Length * 2;
        int varIntSize = EncodingHelper.VarIntSize(strBytes);
        if (destination.Length < varIntSize + strBytes) { bytesWritten = 0; return false; }
        int pos = EncodingHelper.WriteVarInt(destination, strBytes);
        Enc.GetBytes(_value, destination.Slice(pos));
        bytesWritten = pos + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf16LEVar"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf16LEVar(string? v) => new(v);
}

/// <summary>UTF-16 LE encoded string with 4-byte big-endian length prefix.</summary>
public readonly struct Utf16LEFixBE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.Unicode;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf16LEFixBE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32BigEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        int strBytes = _value.Length * 2;
        if (destination.Length < 4 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)strBytes);
        Enc.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf16LEFixBE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf16LEFixBE(string? v) => new(v);
}

/// <summary>UTF-16 LE encoded string with 4-byte little-endian length prefix.</summary>
public readonly struct Utf16LEFixLE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.Unicode;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf16LEFixLE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32LittleEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        int strBytes = _value.Length * 2;
        if (destination.Length < 4 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)strBytes);
        Enc.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf16LEFixLE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf16LEFixLE(string? v) => new(v);
}

#endregion

#region UTF-32 Big Endian Wrappers

/// <summary>UTF-32 BE encoded string without length prefix.</summary>
public readonly struct Utf32BE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = new UTF32Encoding(bigEndian: true, byteOrderMark: false);
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf32BE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { bytesWritten = 0; return true; }
        int needed = Enc.GetByteCount(_value);
        if (destination.Length < needed) { bytesWritten = 0; return false; }
        bytesWritten = Enc.GetBytes(_value, destination);
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf32BE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf32BE(string? v) => new(v);
}

/// <summary>UTF-32 BE encoded string with VarInt length prefix.</summary>
public readonly struct Utf32BEVar : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = new UTF32Encoding(bigEndian: true, byteOrderMark: false);
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf32BEVar(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { destination[0] = 0; bytesWritten = 1; return true; }
        int strBytes = Enc.GetByteCount(_value);
        int varIntSize = EncodingHelper.VarIntSize(strBytes);
        if (destination.Length < varIntSize + strBytes) { bytesWritten = 0; return false; }
        int pos = EncodingHelper.WriteVarInt(destination, strBytes);
        Enc.GetBytes(_value, destination.Slice(pos));
        bytesWritten = pos + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf32BEVar"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf32BEVar(string? v) => new(v);
}

/// <summary>UTF-32 BE encoded string with 4-byte big-endian length prefix.</summary>
public readonly struct Utf32BEFixBE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = new UTF32Encoding(bigEndian: true, byteOrderMark: false);
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf32BEFixBE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32BigEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        int strBytes = Enc.GetByteCount(_value);
        if (destination.Length < 4 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)strBytes);
        Enc.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf32BEFixBE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf32BEFixBE(string? v) => new(v);
}

/// <summary>UTF-32 BE encoded string with 4-byte little-endian length prefix.</summary>
public readonly struct Utf32BEFixLE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = new UTF32Encoding(bigEndian: true, byteOrderMark: false);
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf32BEFixLE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32LittleEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        int strBytes = Enc.GetByteCount(_value);
        if (destination.Length < 4 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)strBytes);
        Enc.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf32BEFixLE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf32BEFixLE(string? v) => new(v);
}

#endregion

#region UTF-32 Little Endian Wrappers

/// <summary>UTF-32 LE encoded string without length prefix.</summary>
public readonly struct Utf32LE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.UTF32;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf32LE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { bytesWritten = 0; return true; }
        int needed = Enc.GetByteCount(_value);
        if (destination.Length < needed) { bytesWritten = 0; return false; }
        bytesWritten = Enc.GetBytes(_value, destination);
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf32LE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf32LE(string? v) => new(v);
}

/// <summary>UTF-32 LE encoded string with VarInt length prefix.</summary>
public readonly struct Utf32LEVar : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.UTF32;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf32LEVar(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { destination[0] = 0; bytesWritten = 1; return true; }
        int strBytes = Enc.GetByteCount(_value);
        int varIntSize = EncodingHelper.VarIntSize(strBytes);
        if (destination.Length < varIntSize + strBytes) { bytesWritten = 0; return false; }
        int pos = EncodingHelper.WriteVarInt(destination, strBytes);
        Enc.GetBytes(_value, destination.Slice(pos));
        bytesWritten = pos + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf32LEVar"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf32LEVar(string? v) => new(v);
}

/// <summary>UTF-32 LE encoded string with 4-byte big-endian length prefix.</summary>
public readonly struct Utf32LEFixBE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.UTF32;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf32LEFixBE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32BigEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        int strBytes = Enc.GetByteCount(_value);
        if (destination.Length < 4 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)strBytes);
        Enc.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf32LEFixBE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf32LEFixBE(string? v) => new(v);
}

/// <summary>UTF-32 LE encoded string with 4-byte little-endian length prefix.</summary>
public readonly struct Utf32LEFixLE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.UTF32;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf32LEFixLE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32LittleEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        int strBytes = Enc.GetByteCount(_value);
        if (destination.Length < 4 + strBytes) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)strBytes);
        Enc.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Utf32LEFixLE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Utf32LEFixLE(string? v) => new(v);
}

#endregion

#region ASCII Wrappers

/// <summary>ASCII encoded string without length prefix (non-ASCII chars become '?').</summary>
public readonly struct Ascii : IUtf8SpanFormattable
{
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ascii(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { bytesWritten = 0; return true; }
        if (destination.Length < _value.Length) { bytesWritten = 0; return false; }
        for (int i = 0; i < _value.Length; i++)
        {
            char c = _value[i];
            destination[i] = c < 128 ? (byte)c : (byte)'?';
        }
        bytesWritten = _value.Length;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Ascii"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Ascii(string? v) => new(v);
}

/// <summary>ASCII encoded string with VarInt length prefix.</summary>
public readonly struct AsciiVar : IUtf8SpanFormattable
{
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AsciiVar(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { destination[0] = 0; bytesWritten = 1; return true; }
        int strBytes = _value.Length;
        int varIntSize = EncodingHelper.VarIntSize(strBytes);
        if (destination.Length < varIntSize + strBytes) { bytesWritten = 0; return false; }
        int pos = EncodingHelper.WriteVarInt(destination, strBytes);
        for (int i = 0; i < _value.Length; i++)
        {
            char c = _value[i];
            destination[pos + i] = c < 128 ? (byte)c : (byte)'?';
        }
        bytesWritten = pos + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="AsciiVar"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator AsciiVar(string? v) => new(v);
}

/// <summary>ASCII encoded string with 4-byte big-endian length prefix.</summary>
public readonly struct AsciiFixBE : IUtf8SpanFormattable
{
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AsciiFixBE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32BigEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        if (destination.Length < 4 + _value.Length) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)_value.Length);
        for (int i = 0; i < _value.Length; i++)
        {
            char c = _value[i];
            destination[4 + i] = c < 128 ? (byte)c : (byte)'?';
        }
        bytesWritten = 4 + _value.Length;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="AsciiFixBE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator AsciiFixBE(string? v) => new(v);
}

/// <summary>ASCII encoded string with 4-byte little-endian length prefix.</summary>
public readonly struct AsciiFixLE : IUtf8SpanFormattable
{
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AsciiFixLE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32LittleEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        if (destination.Length < 4 + _value.Length) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)_value.Length);
        for (int i = 0; i < _value.Length; i++)
        {
            char c = _value[i];
            destination[4 + i] = c < 128 ? (byte)c : (byte)'?';
        }
        bytesWritten = 4 + _value.Length;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="AsciiFixLE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator AsciiFixLE(string? v) => new(v);
}

/// <summary>ASCII encoded string with null terminator.</summary>
public readonly struct AsciiZ : IUtf8SpanFormattable
{
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AsciiZ(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { destination[0] = 0; bytesWritten = 1; return destination.Length >= 1; }
        if (destination.Length < _value.Length + 1) { bytesWritten = 0; return false; }
        for (int i = 0; i < _value.Length; i++)
        {
            char c = _value[i];
            destination[i] = c < 128 ? (byte)c : (byte)'?';
        }
        destination[_value.Length] = 0;
        bytesWritten = _value.Length + 1;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="AsciiZ"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator AsciiZ(string? v) => new(v);
}

#endregion

#region Latin1 Wrappers

/// <summary>Latin1 (ISO-8859-1) encoded string without length prefix.</summary>
public readonly struct Latin1 : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.Latin1;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Latin1(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { bytesWritten = 0; return true; }
        if (destination.Length < _value.Length) { bytesWritten = 0; return false; }
        bytesWritten = Enc.GetBytes(_value, destination);
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Latin1"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Latin1(string? v) => new(v);
}

/// <summary>Latin1 encoded string with VarInt length prefix.</summary>
public readonly struct Latin1Var : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.Latin1;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Latin1Var(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { destination[0] = 0; bytesWritten = 1; return true; }
        int strBytes = _value.Length;
        int varIntSize = EncodingHelper.VarIntSize(strBytes);
        if (destination.Length < varIntSize + strBytes) { bytesWritten = 0; return false; }
        int pos = EncodingHelper.WriteVarInt(destination, strBytes);
        Enc.GetBytes(_value, destination.Slice(pos));
        bytesWritten = pos + strBytes;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Latin1Var"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Latin1Var(string? v) => new(v);
}

/// <summary>Latin1 encoded string with 4-byte big-endian length prefix.</summary>
public readonly struct Latin1FixBE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.Latin1;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Latin1FixBE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32BigEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        if (destination.Length < 4 + _value.Length) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)_value.Length);
        Enc.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + _value.Length;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Latin1FixBE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Latin1FixBE(string? v) => new(v);
}

/// <summary>Latin1 encoded string with 4-byte little-endian length prefix.</summary>
public readonly struct Latin1FixLE : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.Latin1;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Latin1FixLE(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { BinaryPrimitives.WriteUInt32LittleEndian(destination, 0); bytesWritten = 4; return destination.Length >= 4; }
        if (destination.Length < 4 + _value.Length) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32LittleEndian(destination, (uint)_value.Length);
        Enc.GetBytes(_value, destination.Slice(4));
        bytesWritten = 4 + _value.Length;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Latin1FixLE"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Latin1FixLE(string? v) => new(v);
}

/// <summary>Latin1 encoded string with null terminator.</summary>
public readonly struct Latin1Z : IUtf8SpanFormattable
{
    private static readonly Encoding Enc = Encoding.Latin1;
    private readonly string? _value;

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The string value to encode.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Latin1Z(string? value) => _value = value;

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (_value is null) { destination[0] = 0; bytesWritten = 1; return destination.Length >= 1; }
        if (destination.Length < _value.Length + 1) { bytesWritten = 0; return false; }
        Enc.GetBytes(_value, destination);
        destination[_value.Length] = 0;
        bytesWritten = _value.Length + 1;
        return true;
    }

    /// <inheritdoc/>

    /// <inheritdoc/>
    /// <inheritdoc/>
    public override string ToString() => _value ?? "";
    /// <inheritdoc/>
    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>Implicitly converts a string to <see cref="Latin1Z"/>.</summary>
    /// <param name="v">The string value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Latin1Z(string? v) => new(v);
}

#endregion