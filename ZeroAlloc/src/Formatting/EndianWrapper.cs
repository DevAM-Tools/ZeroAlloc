// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// ZeroAlloc - Endian Binary Wrappers
// ============================================================================
// Short-named wrappers for binary data with explicit byte order.
// Naming convention: [U|I][8|16|32|64|128][BE|LE] for integers, F32/F64 for floats
//
// Examples:
//   U16BE = ushort big endian
//   I32LE = int little endian
//   F64BE = double big endian
// ============================================================================

namespace ZeroAlloc;

#region Big Endian Unsigned Integer Wrappers

/// <summary>16-bit unsigned big-endian wrapper with Value property.</summary>
public readonly struct U16BE : IUtf8SpanFormattable, IBinaryParsable<U16BE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 2;

    /// <summary>Gets the underlying value.</summary>
    public ushort Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U16BE(ushort value)
    {
        Value = value;
    }

    /// <summary>Initializes a new instance with a signed 16-bit value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U16BE(short value)
    {
        Value = unchecked((ushort)value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt16BigEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out U16BE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new U16BE(BinaryPrimitives.ReadUInt16BigEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => string.Create(4, Value, static (chars, v) =>
    {
        ReadOnlySpan<char> hex = "0123456789ABCDEF";
        chars[0] = hex[(v >> 12) & 0xF];
        chars[1] = hex[(v >> 8) & 0xF];
        chars[2] = hex[(v >> 4) & 0xF];
        chars[3] = hex[v & 0xF];
    });

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a ushort to U16BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U16BE(ushort v) => new(v);

    /// <summary>Implicitly converts a short to U16BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U16BE(short v) => new(v);
}

/// <summary>32-bit unsigned big-endian wrapper with Value property.</summary>
public readonly struct U32BE : IUtf8SpanFormattable, IBinaryParsable<U32BE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 4;

    /// <summary>Gets the underlying value.</summary>
    public uint Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U32BE(uint value)
    {
        Value = value;
    }

    /// <summary>Initializes a new instance with a signed 32-bit value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U32BE(int value)
    {
        Value = unchecked((uint)value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32BigEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out U32BE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new U32BE(BinaryPrimitives.ReadUInt32BigEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => string.Create(8, Value, static (chars, v) =>
    {
        ReadOnlySpan<char> hex = "0123456789ABCDEF";
        for (int i = 0; i < 8; i++)
        {
            chars[i] = hex[(int)(v >> (28 - i * 4)) & 0xF];
        }
    });

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a uint to U32BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U32BE(uint v) => new(v);

    /// <summary>Implicitly converts an int to U32BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U32BE(int v) => new(v);
}

/// <summary>64-bit unsigned big-endian wrapper with Value property.</summary>
public readonly struct U64BE : IUtf8SpanFormattable, IBinaryParsable<U64BE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 8;

    /// <summary>Gets the underlying value.</summary>
    public ulong Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U64BE(ulong value)
    {
        Value = value;
    }

    /// <summary>Initializes a new instance with a signed 64-bit value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U64BE(long value)
    {
        Value = unchecked((ulong)value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt64BigEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out U64BE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new U64BE(BinaryPrimitives.ReadUInt64BigEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => string.Create(16, Value, static (chars, v) =>
    {
        ReadOnlySpan<char> hex = "0123456789ABCDEF";
        for (int i = 0; i < 16; i++)
        {
            chars[i] = hex[(int)(v >> (60 - i * 4)) & 0xF];
        }
    });

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a ulong to U64BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U64BE(ulong v) => new(v);

    /// <summary>Implicitly converts a long to U64BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U64BE(long v) => new(v);
}

/// <summary>128-bit unsigned big-endian wrapper with Value property.</summary>
public readonly struct U128BE : IUtf8SpanFormattable, IBinaryParsable<U128BE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 16;

    /// <summary>Gets the underlying value.</summary>
    public UInt128 Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U128BE(UInt128 value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt128BigEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out U128BE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new U128BE(BinaryPrimitives.ReadUInt128BigEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => string.Create(32, Value, static (chars, v) =>
    {
        ReadOnlySpan<char> hex = "0123456789ABCDEF";
        ulong hi = (ulong)(v >> 64);
        ulong lo = (ulong)v;
        for (int i = 0; i < 16; i++)
        {
            chars[i] = hex[(int)(hi >> (60 - i * 4)) & 0xF];
        }
        for (int i = 0; i < 16; i++)
        {
            chars[16 + i] = hex[(int)(lo >> (60 - i * 4)) & 0xF];
        }
    });

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a UInt128 to U128BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U128BE(UInt128 v) => new(v);
}

#endregion

#region Big Endian Signed Integer Wrappers

/// <summary>16-bit signed big-endian wrapper with Value property.</summary>
public readonly struct I16BE : IUtf8SpanFormattable, IBinaryParsable<I16BE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 2;

    /// <summary>Gets the underlying value.</summary>
    public short Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public I16BE(short value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteInt16BigEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out I16BE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new I16BE(BinaryPrimitives.ReadInt16BigEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a short to I16BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator I16BE(short v) => new(v);
}

/// <summary>32-bit signed big-endian wrapper with Value property.</summary>
public readonly struct I32BE : IUtf8SpanFormattable, IBinaryParsable<I32BE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 4;

    /// <summary>Gets the underlying value.</summary>
    public int Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public I32BE(int value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteInt32BigEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out I32BE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new I32BE(BinaryPrimitives.ReadInt32BigEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts an int to I32BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator I32BE(int v) => new(v);
}

/// <summary>64-bit signed big-endian wrapper with Value property.</summary>
public readonly struct I64BE : IUtf8SpanFormattable, IBinaryParsable<I64BE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 8;

    /// <summary>Gets the underlying value.</summary>
    public long Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public I64BE(long value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteInt64BigEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
    public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out I64BE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new I64BE(BinaryPrimitives.ReadInt64BigEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a long to I64BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator I64BE(long v) => new(v);
}

/// <summary>128-bit signed big-endian wrapper with Value property.</summary>
public readonly struct I128BE : IUtf8SpanFormattable, IBinaryParsable<I128BE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 16;

    /// <summary>Gets the underlying value.</summary>
    public Int128 Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public I128BE(Int128 value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteInt128BigEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out I128BE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new I128BE(BinaryPrimitives.ReadInt128BigEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts an Int128 to I128BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator I128BE(Int128 v) => new(v);
}

#endregion

#region Little Endian Unsigned Integer Wrappers

/// <summary>16-bit unsigned little-endian wrapper with Value property.</summary>
public readonly struct U16LE : IUtf8SpanFormattable, IBinaryParsable<U16LE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 2;

    /// <summary>Gets the underlying value.</summary>
    public ushort Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U16LE(ushort value)
    {
        Value = value;
    }

    /// <summary>Initializes a new instance with a signed 16-bit value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U16LE(short value)
    {
        Value = unchecked((ushort)value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt16LittleEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out U16LE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new U16LE(BinaryPrimitives.ReadUInt16LittleEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => string.Create(4, Value, static (chars, v) =>
    {
        ReadOnlySpan<char> hex = "0123456789ABCDEF";
        chars[0] = hex[(v >> 12) & 0xF];
        chars[1] = hex[(v >> 8) & 0xF];
        chars[2] = hex[(v >> 4) & 0xF];
        chars[3] = hex[v & 0xF];
    });

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a ushort to U16LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U16LE(ushort v) => new(v);

    /// <summary>Implicitly converts a short to U16LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U16LE(short v) => new(v);
}

/// <summary>32-bit unsigned little-endian wrapper with Value property.</summary>
public readonly struct U32LE : IUtf8SpanFormattable, IBinaryParsable<U32LE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 4;

    /// <summary>Gets the underlying value.</summary>
    public uint Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U32LE(uint value)
    {
        Value = value;
    }

    /// <summary>Initializes a new instance with a signed 32-bit value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U32LE(int value)
    {
        Value = unchecked((uint)value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32LittleEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out U32LE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new U32LE(BinaryPrimitives.ReadUInt32LittleEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => string.Create(8, Value, static (chars, v) =>
    {
        ReadOnlySpan<char> hex = "0123456789ABCDEF";
        for (int i = 0; i < 8; i++)
        {
            chars[i] = hex[(int)(v >> (28 - i * 4)) & 0xF];
        }
    });

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a uint to U32LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U32LE(uint v) => new(v);

    /// <summary>Implicitly converts an int to U32LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U32LE(int v) => new(v);
}

/// <summary>64-bit unsigned little-endian wrapper with Value property.</summary>
public readonly struct U64LE : IUtf8SpanFormattable, IBinaryParsable<U64LE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 8;

    /// <summary>Gets the underlying value.</summary>
    public ulong Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U64LE(ulong value)
    {
        Value = value;
    }

    /// <summary>Initializes a new instance with a signed 64-bit value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U64LE(long value)
    {
        Value = unchecked((ulong)value);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt64LittleEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out U64LE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new U64LE(BinaryPrimitives.ReadUInt64LittleEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => string.Create(16, Value, static (chars, v) =>
    {
        ReadOnlySpan<char> hex = "0123456789ABCDEF";
        for (int i = 0; i < 16; i++)
        {
            chars[i] = hex[(int)(v >> (60 - i * 4)) & 0xF];
        }
    });

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a ulong to U64LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U64LE(ulong v) => new(v);

    /// <summary>Implicitly converts a long to U64LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U64LE(long v) => new(v);
}

/// <summary>128-bit unsigned little-endian wrapper with Value property.</summary>
public readonly struct U128LE : IUtf8SpanFormattable, IBinaryParsable<U128LE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 16;

    /// <summary>Gets the underlying value.</summary>
    public UInt128 Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public U128LE(UInt128 value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt128LittleEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out U128LE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new U128LE(BinaryPrimitives.ReadUInt128LittleEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => string.Create(32, Value, static (chars, v) =>
    {
        ReadOnlySpan<char> hex = "0123456789ABCDEF";
        ulong hi = (ulong)(v >> 64);
        ulong lo = (ulong)v;
        for (int i = 0; i < 16; i++)
        {
            chars[i] = hex[(int)(hi >> (60 - i * 4)) & 0xF];
        }
        for (int i = 0; i < 16; i++)
        {
            chars[16 + i] = hex[(int)(lo >> (60 - i * 4)) & 0xF];
        }
    });

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a UInt128 to U128LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator U128LE(UInt128 v) => new(v);
}

#endregion

#region Little Endian Signed Integer Wrappers

/// <summary>16-bit signed little-endian wrapper with Value property.</summary>
public readonly struct I16LE : IUtf8SpanFormattable, IBinaryParsable<I16LE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 2;

    /// <summary>Gets the underlying value.</summary>
    public short Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public I16LE(short value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteInt16LittleEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out I16LE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new I16LE(BinaryPrimitives.ReadInt16LittleEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a short to I16LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator I16LE(short v) => new(v);
}

/// <summary>32-bit signed little-endian wrapper with Value property.</summary>
public readonly struct I32LE : IUtf8SpanFormattable, IBinaryParsable<I32LE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 4;

    /// <summary>Gets the underlying value.</summary>
    public int Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public I32LE(int value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteInt32LittleEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out I32LE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new I32LE(BinaryPrimitives.ReadInt32LittleEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts an int to I32LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator I32LE(int v) => new(v);
}

/// <summary>64-bit signed little-endian wrapper with Value property.</summary>
public readonly struct I64LE : IUtf8SpanFormattable, IBinaryParsable<I64LE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 8;

    /// <summary>Gets the underlying value.</summary>
    public long Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public I64LE(long value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteInt64LittleEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out I64LE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new I64LE(BinaryPrimitives.ReadInt64LittleEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a long to I64LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator I64LE(long v) => new(v);
}

/// <summary>128-bit signed little-endian wrapper with Value property.</summary>
public readonly struct I128LE : IUtf8SpanFormattable, IBinaryParsable<I128LE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 16;

    /// <summary>Gets the underlying value.</summary>
    public Int128 Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public I128LE(Int128 value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteInt128LittleEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out I128LE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new I128LE(BinaryPrimitives.ReadInt128LittleEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts an Int128 to I128LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator I128LE(Int128 v) => new(v);
}

#endregion

#region Floating Point Wrappers

/// <summary>Wraps a float for big-endian binary output (4 bytes).</summary>
public readonly struct F32BE : IUtf8SpanFormattable, IBinaryParsable<F32BE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 4;

    /// <summary>Gets the underlying value.</summary>
    public float Value { get; }

    /// <summary>Initializes a new instance with a single-precision floating-point value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public F32BE(float value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteSingleBigEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out F32BE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new F32BE(BinaryPrimitives.ReadSingleBigEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a float to F32BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator F32BE(float v) => new(v);
}

/// <summary>Wraps a float for little-endian binary output (4 bytes).</summary>
public readonly struct F32LE : IUtf8SpanFormattable, IBinaryParsable<F32LE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 4;

    /// <summary>Gets the underlying value.</summary>
    public float Value { get; }

    /// <summary>Initializes a new instance with a single-precision floating-point value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public F32LE(float value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteSingleLittleEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out F32LE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new F32LE(BinaryPrimitives.ReadSingleLittleEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a float to F32LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator F32LE(float v) => new(v);
}

/// <summary>Wraps a double for big-endian binary output (8 bytes).</summary>
public readonly struct F64BE : IUtf8SpanFormattable, IBinaryParsable<F64BE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 8;

    /// <summary>Gets the underlying value.</summary>
    public double Value { get; }

    /// <summary>Initializes a new instance with a double-precision floating-point value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public F64BE(double value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteDoubleBigEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out F64BE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new F64BE(BinaryPrimitives.ReadDoubleBigEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a double to F64BE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator F64BE(double v) => new(v);
}

/// <summary>Wraps a double for little-endian binary output (8 bytes).</summary>
public readonly struct F64LE : IUtf8SpanFormattable, IBinaryParsable<F64LE>
{
    /// <summary>The size in bytes of this type when serialized.</summary>
    public const int Size = 8;

    /// <summary>Gets the underlying value.</summary>
    public double Value { get; }

    /// <summary>Initializes a new instance with a double-precision floating-point value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public F64LE(double value)
    {
        Value = value;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < Size)
        { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteDoubleLittleEndian(utf8Destination, Value);
        bytesWritten = Size;
        return true;
    }

    /// <inheritdoc/>
        public static bool TryGetSerializedSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out F64LE value, out int bytesConsumed)
    {
        if (source.Length < Size)
        { value = default; bytesConsumed = 0; return false; }
        value = new F64LE(BinaryPrimitives.ReadDoubleLittleEndian(source));
        bytesConsumed = Size;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a double to F64LE.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator F64LE(double v) => new(v);
}

#endregion

#region Raw Binary Wrapper

/// <summary>Wraps raw binary data (ReadOnlySpan&lt;byte&gt;) for direct binary output.</summary>
/// <remarks>
/// This type is useful for embedding raw binary data that should be written
/// as-is without any transformation.
/// </remarks>
public readonly ref struct Raw
{
    private readonly ReadOnlySpan<byte> _Value;

    /// <summary>Gets the size in bytes of this instance when serialized.</summary>
    public int Size => _Value.Length;

    /// <summary>Gets the underlying raw data.</summary>
    public ReadOnlySpan<byte> Value => _Value;

    /// <summary>Initializes a new instance with the specified raw data.</summary>
    /// <param name="value">The raw bytes to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Raw(ReadOnlySpan<byte> value)
    {
        _Value = value;
    }

    /// <summary>Formats the raw bytes to the utf8Destination.</summary>
    /// <param name="utf8Destination">The destination span to write to.</param>
    /// <param name="bytesWritten">The number of bytes written.</param>
    /// <param name="format">Not used.</param>
    /// <param name="provider">Not used.</param>
    /// <returns><c>true</c> if the data was written successfully; otherwise <c>false</c>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (utf8Destination.Length < _Value.Length)
        { bytesWritten = 0; return false; }
        _Value.CopyTo(utf8Destination);
        bytesWritten = _Value.Length;
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Convert.ToHexString(_Value);

    /// <summary>Implicitly converts a ReadOnlySpan&lt;byte&gt; to Raw.</summary>
    /// <param name="v">The raw bytes to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Raw(ReadOnlySpan<byte> v) => new(v);

    /// <summary>Implicitly converts a byte array to Raw.</summary>
    /// <param name="v">The byte array to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Raw(byte[] v) => new(v);
}

#endregion

#region Variable-Length Integer Wrappers

/// <summary>
/// Variable-length unsigned integer wrapper (1-10 bytes).
/// Uses 7-bit encoding with MSB continuation flag.
/// </summary>
/// <remarks>
/// Encoding: Each byte uses 7 bits for data and 1 bit (MSB) to indicate continuation.
/// <list type="bullet">
/// <item>Values 0-127: 1 byte</item>
/// <item>Values 128-16383: 2 bytes</item>
/// <item>Values up to 2^64-1: max 10 bytes</item>
/// </list>
/// </remarks>
public readonly struct VarInt : IUtf8SpanFormattable, IBinarySerializable, IBinaryParsable<VarInt>
{
    /// <summary>Maximum encoded size in bytes for a 64-bit value.</summary>
    public const int MaxSize = 10;

    /// <summary>The unsigned value to encode.</summary>
    public ulong Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VarInt(ulong value)
    {
        Value = value;
    }

    /// <summary>Gets the actual encoded size in bytes for this value.</summary>
    public int EncodedSize
    {
        get
        {
            ulong v = Value;
            int size = 1;
            while (v >= 0x80)
            {
                v >>= 7;
                size++;
            }
            return size;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        ulong value = Value;
        int pos = 0;

        while (value >= 0x80)
        {
            if (pos >= utf8Destination.Length)
            { bytesWritten = 0; return false; }
            utf8Destination[pos++] = (byte)(value | 0x80);
            value >>= 7;
        }

        if (pos >= utf8Destination.Length)
        { bytesWritten = 0; return false; }
        utf8Destination[pos++] = (byte)value;
        bytesWritten = pos;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetWrittenSize(out int size)
    {
        size = EncodedSize;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSerializedSize(out int size)
    {
        size = 0;
        return false;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWrite(Span<byte> destination, out int bytesWritten) => TryFormat(destination, out bytesWritten, default, null);

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a ulong to VarInt.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VarInt(ulong v) => new(v);

    /// <summary>Implicitly converts a uint to VarInt.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VarInt(uint v) => new(v);

    /// <summary>Implicitly converts a ushort to VarInt.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VarInt(ushort v) => new(v);

    /// <summary>Implicitly converts a byte to VarInt.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VarInt(byte v) => new(v);

    /// <summary>
    /// Attempts to parse a VarInt from the given binary data.
    /// </summary>
    /// <param name="source">The source bytes to parse from.</param>
    /// <param name="value">The parsed value if successful.</param>
    /// <param name="bytesConsumed">The number of bytes consumed during parsing.</param>
    /// <returns><c>true</c> if parsing succeeded; <c>false</c> if there was insufficient data or invalid encoding.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out VarInt value, out int bytesConsumed)
    {
        ulong result = 0;
        int shift = 0;
        int pos = 0;

        while (pos < source.Length && shift < 64)
        {
            byte b = source[pos++];
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                value = new VarInt(result);
                bytesConsumed = pos;
                return true;
            }
            shift += 7;
        }

        value = default;
        bytesConsumed = 0;
        return false;
    }
}

/// <summary>
/// Variable-length signed integer wrapper using ZigZag encoding (1-10 bytes).
/// Maps signed integers to unsigned so small absolute values are small encoded values.
/// </summary>
/// <remarks>
/// ZigZag encoding: 0 → 0, -1 → 1, 1 → 2, -2 → 3, 2 → 4, ...
/// This is efficient for values that are typically small in absolute value,
/// as it avoids the 10-byte encoding for small negative numbers.
/// </remarks>
public readonly struct VarIntZigZag : IUtf8SpanFormattable, IBinarySerializable, IBinaryParsable<VarIntZigZag>
{
    /// <summary>Maximum encoded size in bytes for a 64-bit value.</summary>
    public const int MaxSize = 10;

    /// <summary>The signed value to encode.</summary>
    public long Value { get; }

    /// <summary>Initializes a new instance with the specified value.</summary>
    /// <param name="value">The value to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VarIntZigZag(long value)
    {
        Value = value;
    }

    /// <summary>Gets the ZigZag-encoded unsigned representation.</summary>
    public ulong ZigZagEncoded => (ulong)((Value << 1) ^ (Value >> 63));

    /// <summary>Gets the actual encoded size in bytes for this value.</summary>
    public int EncodedSize
    {
        get
        {
            ulong v = ZigZagEncoded;
            int size = 1;
            while (v >= 0x80)
            {
                v >>= 7;
                size++;
            }
            return size;
        }
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        ulong value = ZigZagEncoded;
        int pos = 0;

        while (value >= 0x80)
        {
            if (pos >= utf8Destination.Length)
            { bytesWritten = 0; return false; }
            utf8Destination[pos++] = (byte)(value | 0x80);
            value >>= 7;
        }

        if (pos >= utf8Destination.Length)
        { bytesWritten = 0; return false; }
        utf8Destination[pos++] = (byte)value;
        bytesWritten = pos;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetWrittenSize(out int size)
    {
        size = EncodedSize;
        return true;
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryGetSerializedSize(out int size)
    {
        size = 0;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    /// <summary>Implicitly converts a long to VarIntZigZag.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VarIntZigZag(long v) => new(v);

    /// <summary>Implicitly converts an int to VarIntZigZag.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VarIntZigZag(int v) => new(v);

    /// <summary>Implicitly converts a short to VarIntZigZag.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VarIntZigZag(short v) => new(v);

    /// <summary>Implicitly converts an sbyte to VarIntZigZag.</summary>
    /// <param name="v">The value to convert.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator VarIntZigZag(sbyte v) => new(v);

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryWrite(Span<byte> destination, out int bytesWritten) => TryFormat(destination, out bytesWritten, default, null);

    /// <summary>Decodes a ZigZag-encoded value back to signed.</summary>
    /// <param name="zigzag">The ZigZag-encoded unsigned value.</param>
    /// <returns>The decoded signed value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long DecodeZigZag(ulong zigzag) => (long)(zigzag >> 1) ^ -(long)(zigzag & 1);

    /// <summary>
    /// Attempts to parse a VarIntZigZag from the given binary data.
    /// </summary>
    /// <param name="source">The source bytes to parse from.</param>
    /// <param name="value">The parsed value if successful.</param>
    /// <param name="bytesConsumed">The number of bytes consumed during parsing.</param>
    /// <returns><c>true</c> if parsing succeeded; <c>false</c> if there was insufficient data or invalid encoding.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryParse(ReadOnlySpan<byte> source, out VarIntZigZag value, out int bytesConsumed)
    {
        if (VarInt.TryParse(source, out VarInt varint, out bytesConsumed))
        {
            value = new VarIntZigZag(DecodeZigZag(varint.Value));
            return true;
        }
        value = default;
        return false;
    }
}

#endregion
