// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// ZeroAlloc - Binary Parser
// ============================================================================
// Provides high-performance, zero-allocation binary parsing capabilities.
// 
// Features:
// - Struct/Record parsing via IBinaryParsable<T>
// - Array/List parsing with length prefixes
// - Zero-allocation string parsing (Utf8, Ascii, Latin1)
// - Type validation with descriptive exceptions
//
// Note on Bit-Level Alignment:
// The BitReader operates at the bit level. After reading non-byte-aligned
// data, call AlignToNextByte() before reading byte-aligned data.
// ============================================================================

namespace ZeroAlloc;

#region BinaryParser - Main Parser Class

/// <summary>
/// High-performance binary parser for structured data.
/// Supports structs, records, arrays, and strings with zero allocations.
/// </summary>
/// <remarks>
/// <para>
/// <b>Thread-safety:</b> Not thread-safe. Use one parser instance per thread and do not
/// share mutable parser state across threads.
/// </para>
/// <para>
/// <b>Untrusted input:</b> Length-prefixed reads return spans bounded only by the source
/// buffer. When decoding to <see cref="string"/>, enforce a caller-defined maximum length
/// (for example via <see cref="TryReadUtf8Var(int, out ReadOnlySpan{byte})"/>) before allocation.
/// </para>
/// </remarks>
public ref struct BinaryParser
{
    private readonly ReadOnlySpan<byte> _Buffer;
    private int _Position;

    /// <summary>
    /// Initializes a new BinaryParser with the specified buffer.
    /// </summary>
    /// <param name="buffer">The buffer to parse from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BinaryParser(ReadOnlySpan<byte> buffer)
    {
        _Buffer = buffer;
        _Position = 0;
    }

    /// <summary>Gets the current byte position in the buffer.</summary>
    public readonly int Position => _Position;

    /// <summary>Gets the remaining bytes available.</summary>
    public readonly int Remaining => _Buffer.Length - _Position;

    /// <summary>Gets the total buffer length.</summary>
    public readonly int Length => _Buffer.Length;

    /// <summary>Gets whether the parser has reached the end of the buffer.</summary>
    public readonly bool IsAtEnd => _Position >= _Buffer.Length;

    /// <summary>Gets a span of the remaining bytes.</summary>
    public readonly ReadOnlySpan<byte> RemainingSpan => _Buffer.Slice(_Position);

    // ========================================================================
    // PRIMITIVE READS - Big Endian (Network Byte Order)
    // ========================================================================

    /// <summary>Reads a byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte ReadByte()
    {
        if (_Position >= _Buffer.Length)
        {
            _ThrowInsufficientData(1);
        }

        return _Buffer[_Position++];
    }

    /// <summary>Reads a signed byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public sbyte ReadSByte() => (sbyte)ReadByte();

    /// <summary>Reads a 16-bit unsigned integer in big-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16BE()
    {
        if (_Position + 2 > _Buffer.Length)
        {
            _ThrowInsufficientData(2);
        }

        ushort value = BinaryPrimitives.ReadUInt16BigEndian(_Buffer.Slice(_Position));
        _Position += 2;
        return value;
    }

    /// <summary>Reads a 32-bit unsigned integer in big-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32BE()
    {
        if (_Position + 4 > _Buffer.Length)
        {
            _ThrowInsufficientData(4);
        }

        uint value = BinaryPrimitives.ReadUInt32BigEndian(_Buffer.Slice(_Position));
        _Position += 4;
        return value;
    }

    /// <summary>Reads a 64-bit unsigned integer in big-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64BE()
    {
        if (_Position + 8 > _Buffer.Length)
        {
            _ThrowInsufficientData(8);
        }

        ulong value = BinaryPrimitives.ReadUInt64BigEndian(_Buffer.Slice(_Position));
        _Position += 8;
        return value;
    }

    /// <summary>Reads a 16-bit signed integer in big-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16BE() => (short)ReadUInt16BE();

    /// <summary>Reads a 32-bit signed integer in big-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32BE() => (int)ReadUInt32BE();

    /// <summary>Reads a 64-bit signed integer in big-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64BE() => (long)ReadUInt64BE();

    // ========================================================================
    // PRIMITIVE READS - Little Endian
    // ========================================================================

    /// <summary>Reads a 16-bit unsigned integer in little-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16LE()
    {
        if (_Position + 2 > _Buffer.Length)
        {
            _ThrowInsufficientData(2);
        }

        ushort value = BinaryPrimitives.ReadUInt16LittleEndian(_Buffer.Slice(_Position));
        _Position += 2;
        return value;
    }

    /// <summary>Reads a 32-bit unsigned integer in little-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32LE()
    {
        if (_Position + 4 > _Buffer.Length)
        {
            _ThrowInsufficientData(4);
        }

        uint value = BinaryPrimitives.ReadUInt32LittleEndian(_Buffer.Slice(_Position));
        _Position += 4;
        return value;
    }

    /// <summary>Reads a 64-bit unsigned integer in little-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ulong ReadUInt64LE()
    {
        if (_Position + 8 > _Buffer.Length)
        {
            _ThrowInsufficientData(8);
        }

        ulong value = BinaryPrimitives.ReadUInt64LittleEndian(_Buffer.Slice(_Position));
        _Position += 8;
        return value;
    }

    /// <summary>Reads a 16-bit signed integer in little-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public short ReadInt16LE() => (short)ReadUInt16LE();

    /// <summary>Reads a 32-bit signed integer in little-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ReadInt32LE() => (int)ReadUInt32LE();

    /// <summary>Reads a 64-bit signed integer in little-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadInt64LE() => (long)ReadUInt64LE();

    // ========================================================================
    // FLOATING POINT
    // ========================================================================

    /// <summary>Reads a 32-bit float in big-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadF32BE()
    {
        uint bits = ReadUInt32BE();
        return BitConverter.Int32BitsToSingle((int)bits);
    }

    /// <summary>Reads a 32-bit float in little-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadF32LE()
    {
        uint bits = ReadUInt32LE();
        return BitConverter.Int32BitsToSingle((int)bits);
    }

    /// <summary>Reads a 64-bit double in big-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadF64BE()
    {
        ulong bits = ReadUInt64BE();
        return BitConverter.Int64BitsToDouble((long)bits);
    }

    /// <summary>Reads a 64-bit double in little-endian order.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ReadF64LE()
    {
        ulong bits = ReadUInt64LE();
        return BitConverter.Int64BitsToDouble((long)bits);
    }

    // ========================================================================
    // VARINT
    // ========================================================================

    /// <summary>
    /// Reads a VarInt (7-bit encoded unsigned integer).
    /// </summary>
    /// <returns>The decoded value.</returns>
    public ulong ReadVarInt()
    {
        ulong result = 0;
        int shift = 0;

        while (true)
        {
            if (_Position >= _Buffer.Length)
            {
                _ThrowInsufficientData(1);
            }

            byte b = _Buffer[_Position++];
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                return result;
            }

            shift += 7;
            if (shift >= 70) // Overflow protection (10 bytes max)
            {
                throw new InvalidOperationException("VarInt is too long (exceeds 64 bits)");
            }
        }
    }

    /// <summary>
    /// Reads a VarInt and decodes it using ZigZag encoding for signed values.
    /// </summary>
    /// <returns>The decoded signed value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ReadVarIntZigZag()
    {
        ulong unsigned = ReadVarInt();
        return (long)((unsigned >> 1) ^ (ulong)(-(long)(unsigned & 1)));
    }

    // ========================================================================
    // RAW BYTES
    // ========================================================================

    /// <summary>
    /// Reads a span of raw bytes without copying.
    /// </summary>
    /// <param name="length">Number of bytes to read.</param>
    /// <returns>A span containing the requested bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadBytes(int length)
    {
        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be non-negative");
        }

        if (_Position + length > _Buffer.Length)
        {
            _ThrowInsufficientData(length);
        }

        ReadOnlySpan<byte> span = _Buffer.Slice(_Position, length);
        _Position += length;
        return span;
    }

    /// <summary>
    /// Skips the specified number of bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Skip(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        }

        if (_Position + count > _Buffer.Length)
        {
            _ThrowInsufficientData(count);
        }

        _Position += count;
    }

    // ========================================================================
    // STRING PARSING - Zero Allocation
    // ========================================================================

    /// <summary>
    /// Reads a UTF-8 string with a fixed byte length (no allocation).
    /// </summary>
    /// <param name="byteLength">The byte length of the string.</param>
    /// <returns>A span containing the UTF-8 bytes (can be decoded with Encoding.UTF8).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadUtf8Bytes(int byteLength) => ReadBytes(byteLength);

    /// <summary>
    /// Reads a UTF-8 string with a VarInt length prefix (no allocation).
    /// </summary>
    /// <returns>A span containing the UTF-8 bytes.</returns>
    /// <remarks>
    /// The returned span length is limited only by the remaining source buffer. When parsing
    /// untrusted payloads, prefer <see cref="TryReadUtf8Var(int, out ReadOnlySpan{byte})"/> and
    /// enforce a maximum length before calling <c>Encoding.UTF8.GetString</c>.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadUtf8Var()
    {
        ulong rawLength = ReadVarInt();
        if (rawLength > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"VarInt length prefix {rawLength} exceeds the maximum allowed value of {int.MaxValue}");
        }

        int length = (int)rawLength;
        return ReadBytes(length);
    }

    /// <summary>
    /// Tries to read a VarInt length-prefixed UTF-8 span without throwing for expected failures.
    /// </summary>
    /// <param name="maxLength">Maximum allowed byte length (caller policy for untrusted input).</param>
    /// <param name="span">The UTF-8 bytes when successful.</param>
    /// <returns>
    /// <c>true</c> when the length prefix and payload fit within <paramref name="maxLength"/> and
    /// the remaining buffer; otherwise <c>false</c> without advancing the parser position.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxLength"/> is negative.
    /// </exception>
    public bool TryReadUtf8Var(int maxLength, out ReadOnlySpan<byte> span)
    {
        if (maxLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxLength), "Max length must be non-negative.");
        }

        int startPosition = _Position;
        if (!_TryReadVarInt(out ulong rawLength))
        {
            span = default;
            return false;
        }

        if (rawLength > int.MaxValue || rawLength > (ulong)maxLength)
        {
            _Position = startPosition;
            span = default;
            return false;
        }

        int length = (int)rawLength;
        if (_Position + length > _Buffer.Length)
        {
            _Position = startPosition;
            span = default;
            return false;
        }

        span = _Buffer.Slice(_Position, length);
        _Position += length;
        return true;
    }

    /// <summary>
    /// Reads a UTF-8 string with a 16-bit big-endian length prefix (no allocation).
    /// </summary>
    /// <returns>A span containing the UTF-8 bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadUtf8FixedBE16()
    {
        ushort length = ReadUInt16BE();
        return ReadBytes(length);
    }

    /// <summary>
    /// Reads a UTF-8 string with a 32-bit big-endian length prefix (no allocation).
    /// </summary>
    /// <returns>A span containing the UTF-8 bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadUtf8FixedBE32()
    {
        uint rawLength = ReadUInt32BE();
        if (rawLength > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"BE32 length prefix {rawLength} exceeds the maximum allowed value of {int.MaxValue}");
        }

        int length = (int)rawLength;
        return ReadBytes(length);
    }

    /// <summary>
    /// Reads a null-terminated UTF-8 string (no allocation).
    /// </summary>
    /// <returns>A span containing the UTF-8 bytes (excluding the null terminator).</returns>
    public ReadOnlySpan<byte> ReadUtf8Null()
    {
        int start = _Position;
        while (_Position < _Buffer.Length && _Buffer[_Position] != 0)
        {
            _Position++;
        }

        ReadOnlySpan<byte> span = _Buffer.Slice(start, _Position - start);

        // Skip the null terminator if present
        if (_Position < _Buffer.Length)
        {
            _Position++;
        }

        return span;
    }

    /// <summary>
    /// Reads an ASCII string with a fixed byte length (no allocation).
    /// </summary>
    /// <param name="byteLength">The byte length of the string.</param>
    /// <returns>A span containing the ASCII bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> ReadAsciiBytes(int byteLength) => ReadBytes(byteLength);

    /// <summary>
    /// Decodes UTF-8 bytes to a character span without heap allocation.
    /// </summary>
    /// <param name="utf8Bytes">The UTF-8 encoded bytes.</param>
    /// <param name="destination">The destination span for decoded characters.</param>
    /// <param name="charsWritten">Number of characters written.</param>
    /// <returns><c>true</c> if decoding succeeded; <c>false</c> if destination is too small.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool TryDecodeUtf8(ReadOnlySpan<byte> utf8Bytes, Span<char> destination, out int charsWritten)
    {
        return Encoding.UTF8.TryGetChars(utf8Bytes, destination, out charsWritten);
    }

    // ========================================================================
    // STRUCT/RECORD PARSING
    // ========================================================================

    /// <summary>
    /// Parses a value of type T that implements IBinaryParsable.
    /// </summary>
    /// <typeparam name="T">The type to parse (must implement IBinaryParsable&lt;T&gt;).</typeparam>
    /// <returns>The parsed value.</returns>
    /// <exception cref="InvalidOperationException">Thrown when there is insufficient data.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T Read<T>() where T : IBinaryParsable<T>
    {
        BinaryParseValidator.ValidateType<T>();

        if (!T.TryParse(RemainingSpan, out T value, out int bytesConsumed))
        {
            int requiredSize = T.TryGetSerializedSize(out int size) && size > 0 ? size : 1;
            _ThrowInsufficientData(requiredSize);
        }

        _Position += bytesConsumed;
        return value;
    }

    /// <summary>
    /// Attempts to parse a value of type T that implements IBinaryParsable.
    /// </summary>
    /// <typeparam name="T">The type to parse (must implement IBinaryParsable&lt;T&gt;).</typeparam>
    /// <param name="value">The parsed value if successful.</param>
    /// <returns><c>true</c> if parsing succeeded; <c>false</c> if there was insufficient data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead<T>(out T value) where T : IBinaryParsable<T>
    {
        BinaryParseValidator.ValidateType<T>();

        if (T.TryParse(RemainingSpan, out value, out int bytesConsumed))
        {
            _Position += bytesConsumed;
            return true;
        }
        return false;
    }

    // ========================================================================
    // ARRAY/LIST PARSING WITH LENGTH PREFIX
    // ========================================================================

    /// <summary>
    /// Reads an array of fixed-size values with a byte length prefix.
    /// </summary>
    /// <typeparam name="T">The element type (must implement IBinaryParsable&lt;T&gt; with fixed size).</typeparam>
    /// <param name="count">Number of elements to read.</param>
    /// <param name="destination">Destination span to write elements into.</param>
    /// <returns>Number of elements actually read.</returns>
    /// <exception cref="ArgumentException">Thrown when T has variable size.</exception>
    public int ReadArray<T>(int count, Span<T> destination) where T : IBinaryParsable<T>
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative");
        }

        if (!T.TryGetSerializedSize(out int size) || size < 0)
        {
            throw new ArgumentException($"Type {typeof(T).Name} has variable size and cannot be read as a fixed-size array. Use ReadList<T> instead.");
        }

        int elementsToRead = Math.Min(count, destination.Length);
        int elementsRead = 0;

        for (int i = 0; i < elementsToRead; i++)
        {
            if (!TryRead<T>(out T? value))
            {
                break;
            }

            destination[i] = value;
            elementsRead++;
        }

        return elementsRead;
    }

    /// <summary>
    /// Reads an array with a VarInt count prefix into a destination span.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="destination">Destination span to write elements into.</param>
    /// <returns>Number of elements actually read.</returns>
    public int ReadArrayVarInt<T>(Span<T> destination) where T : IBinaryParsable<T>
    {
        ulong rawCount = ReadVarInt();
        if (rawCount > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"VarInt count prefix {rawCount} exceeds the maximum allowed value of {int.MaxValue}");
        }

        int count = (int)rawCount;
        return ReadArray(count, destination);
    }

    /// <summary>
    /// Reads an array with a 16-bit big-endian count prefix into a destination span.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="destination">Destination span to write elements into.</param>
    /// <returns>Number of elements actually read.</returns>
    public int ReadArrayBE16<T>(Span<T> destination) where T : IBinaryParsable<T>
    {
        int count = ReadUInt16BE();
        return ReadArray(count, destination);
    }

    /// <summary>
    /// Reads an array with a 32-bit big-endian count prefix into a destination span.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="destination">Destination span to write elements into.</param>
    /// <returns>Number of elements actually read.</returns>
    public int ReadArrayBE32<T>(Span<T> destination) where T : IBinaryParsable<T>
    {
        uint rawCount = ReadUInt32BE();
        if (rawCount > int.MaxValue)
        {
            throw new InvalidOperationException(
                $"BE32 count prefix {rawCount} exceeds the maximum allowed value of {int.MaxValue}");
        }

        int count = (int)rawCount;
        return ReadArray(count, destination);
    }

    // ========================================================================
    // ERROR HANDLING
    // ========================================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    private readonly void _ThrowInsufficientData(int bytesNeeded)
    {
        throw new InvalidOperationException(
            $"Insufficient data: need {bytesNeeded} bytes at position {_Position}, " +
            $"but only {Remaining} bytes remaining (buffer length: {_Buffer.Length}).");
    }

    /// <summary>
    /// Tries to read a VarInt without throwing when data is insufficient or malformed.
    /// </summary>
    private bool _TryReadVarInt(out ulong value)
    {
        ulong result = 0;
        int shift = 0;
        int position = _Position;

        while (true)
        {
            if (position >= _Buffer.Length)
            {
                value = 0;
                return false;
            }

            byte b = _Buffer[position++];
            result |= (ulong)(b & 0x7F) << shift;

            if ((b & 0x80) == 0)
            {
                _Position = position;
                value = result;
                return true;
            }

            shift += 7;
            if (shift >= 70)
            {
                value = 0;
                return false;
            }
        }
    }
}

#endregion

#region Type Validation Helper

/// <summary>
/// Provides type validation for binary parsing operations.
/// </summary>
public static class BinaryParseValidator
{
    /// <summary>
    /// Validates that the specified type is supported for parsing.
    /// </summary>
    /// <typeparam name="T">The type to validate.</typeparam>
    /// <exception cref="NotSupportedException">Thrown when the type is not supported.</exception>
    public static void ValidateType<T>()
    {
        Type type = typeof(T);

        // Check for unsupported types
        if (type == typeof(object))
        {
            throw new NotSupportedException(
                "Type 'object' is not supported for binary parsing. Use a specific type.");
        }

        if (type == typeof(string))
        {
            throw new NotSupportedException(
                "Type 'string' is not directly supported. Use ReadUtf8Bytes(), ReadUtf8Var(), or ReadUtf8Null() instead.");
        }

        if (type.IsArray)
        {
            throw new NotSupportedException(
                $"Array type '{type.Name}' is not directly supported. Use ReadArray<T>() or ReadArrayVarInt<T>() methods.");
        }

        if (type.IsClass && !type.IsValueType)
        {
            throw new NotSupportedException(
                $"Reference type '{type.Name}' is not supported. Binary parsing requires value types (struct/record struct).");
        }

        // Check if type implements IBinaryParsable
        Type[] interfaces = type.GetInterfaces();
        bool implementsParsable = false;
        foreach (Type iface in interfaces)
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IBinaryParsable<>))
            {
                implementsParsable = true;
                break;
            }
        }

        if (!implementsParsable && !_IsPrimitiveOrKnownType(type))
        {
            throw new NotSupportedException(
                $"Type '{type.Name}' does not implement IBinaryParsable<{type.Name}>. " +
                $"Implement this interface to enable parsing, or use a built-in wrapper type.");
        }
    }

    /// <summary>
    /// Checks if the type is a primitive or known wrapper type.
    /// </summary>
    private static bool _IsPrimitiveOrKnownType(Type type)
    {
        // Primitives
        if (type.IsPrimitive)
        {
            return true;
        }

        // Known wrapper types (check by name pattern)
        string name = type.Name;
        return name.StartsWith("UInt") || name.StartsWith("Int") ||
               name.StartsWith("F32") || name.StartsWith("F64") ||
               name == "VarInt" || name == "VarIntZigZag" ||
               name == "Raw" || name.StartsWith("Utf8") ||
               name.StartsWith("Bit") || name == "Nibble";
    }

    /// <summary>
    /// Gets a descriptive error message for type validation.
    /// </summary>
    /// <param name="type">The type that failed validation.</param>
    /// <returns>A detailed error message with suggestions.</returns>
    public static string GetTypeErrorMessage(Type type)
    {
        if (type == typeof(string))
        {
            return "To parse strings, use one of these methods:\n" +
                   "  - ReadUtf8Bytes(length) for fixed-length UTF-8\n" +
                   "  - ReadUtf8Var() for VarInt length-prefixed UTF-8\n" +
                   "  - ReadUtf8Null() for null-terminated UTF-8\n" +
                   "  - ReadAsciiBytes(length) for fixed-length ASCII";
        }

        if (type.IsArray)
        {
            return $"To parse arrays, use one of these methods:\n" +
                   $"  - ReadArray<T>(count, destination) for fixed count\n" +
                   $"  - ReadArrayVarInt<T>(destination) for VarInt count prefix\n" +
                   $"  - ReadArrayBE16<T>(destination) for 16-bit count prefix";
        }

        return $"Type '{type.Name}' is not supported. " +
               $"Implement IBinaryParsable<{type.Name}> to enable parsing.";
    }
}

#endregion
