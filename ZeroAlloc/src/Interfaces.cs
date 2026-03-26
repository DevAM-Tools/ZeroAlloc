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
// ZeroAlloc - Interfaces
// ============================================================================
// Core interfaces for zero-allocation serialization.
// ============================================================================

namespace ZeroAlloc;

/// <summary>
/// Interface for types that can serialize themselves to binary format.
/// </summary>
/// <remarks>
/// <para>Implement this interface for custom types that need to be serialized
/// using <c>ZA.Bytes()</c> or <c>ZA.TryBytes()</c>.</para>
/// <para>The <see cref="TryWrite"/> method follows the same pattern as
/// <see cref="ISpanFormattable.TryFormat"/> - returns false if buffer too small.</para>
/// </remarks>
/// <example>
/// <code>
/// public readonly struct NetworkHeader : IBinarySerializable
/// {
///     public ushort Type { get; init; }
///     public uint Length { get; init; }
///     
///     public bool TryGetSerializedSize(out int size) { size = 6; return true; }
///     
///     public bool TryWrite(Span&lt;byte&gt; destination, out int bytesWritten)
///     {
///         if (destination.Length &lt; 6) { bytesWritten = 0; return false; }
///         BinaryPrimitives.WriteUInt16BigEndian(destination, Type);
///         BinaryPrimitives.WriteUInt32BigEndian(destination[2..], Length);
///         bytesWritten = 6;
///         return true;
///     }
/// }
/// </code>
/// </example>
public interface IBinarySerializable
{
    /// <summary>
    /// Tries to get the number of bytes required to serialize this instance.
    /// </summary>
    /// <param name="size">When this method returns true, contains the exact byte count.</param>
    /// <returns><c>true</c> if the size could be determined; <c>false</c> if unknown.</returns>
    /// <remarks>
    /// Return <c>false</c> if the size cannot be efficiently determined in advance.
    /// This allows the serializer to use alternative strategies for dynamic-size types.
    /// </remarks>
    bool TryGetSerializedSize(out int size);

    /// <summary>
    /// Tries to write this instance to the given span.
    /// </summary>
    /// <param name="destination">The destination span to write to.</param>
    /// <param name="bytesWritten">The number of bytes written.</param>
    /// <returns><c>true</c> if successful; <c>false</c> if the buffer is too small.</returns>
    bool TryWrite(Span<byte> destination, out int bytesWritten);
}

/// <summary>
/// Interface for types that can provide their formatted character count.
/// </summary>
/// <remarks>
/// <para><b>Important:</b> This interface should return the <b>actual size</b> that will be written
/// by <c>TryFormat</c>, not a rough estimate. The generator uses this value for precise buffer
/// pre-allocation and capacity checks.</para>
/// <para>Implement this interface alongside <see cref="ISpanFormattable"/> to enable
/// pre-allocation of the exact buffer size needed.</para>
/// <para>The generator will use <see cref="TryGetStringSize"/> to pre-check buffer capacity
/// before calling <c>TryFormat</c>, avoiding expensive exceptions.</para>
/// <para><b>Guidelines:</b></para>
/// <list type="bullet">
/// <item><description>Return the exact size if it can be determined efficiently (O(1)).</description></item>
/// <item><description>Return a tight upper bound if exact calculation would be expensive.</description></item>
/// <item><description>Return <c>false</c> only if the size truly cannot be determined in advance.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public readonly struct IpAddress : IStringSize, ISpanFormattable
/// {
///     public byte A, B, C, D;
///     
///     // Exact calculation: count digits in each octet
///     public bool TryGetStringSize(ReadOnlySpan&lt;char&gt; format, IFormatProvider? provider, out int size)
///     {
///         // Calculate actual size: each octet's digit count + 3 dots
///         size = DigitCount(A) + DigitCount(B) + DigitCount(C) + DigitCount(D) + 3;
///         return true;
///     }
///     
///     private static int DigitCount(byte b) =&gt; b &lt; 10 ? 1 : b &lt; 100 ? 2 : 3;
///     
///     public bool TryFormat(Span&lt;char&gt; dest, out int written, ...)
///     {
///         // Actual formatting logic
///     }
/// }
/// </code>
/// </example>
public interface IStringSize
{
    /// <summary>
    /// Tries to get the number of characters required to format this instance.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (culture).</param>
    /// <param name="size">
    /// When this method returns <c>true</c>, contains the <b>actual</b> character count that
    /// <see cref="ISpanFormattable.TryFormat"/> will write. This should be the exact size,
    /// not an estimate or maximum theoretical size.
    /// </param>
    /// <returns>
    /// <c>true</c> if the size could be determined; <c>false</c> if the size cannot be
    /// efficiently calculated in advance (e.g., for complex nested structures).
    /// </returns>
    bool TryGetStringSize(ReadOnlySpan<char> format, IFormatProvider? provider, out int size);
}

/// <summary>
/// Interface for types that can provide their UTF-8 formatted byte count.
/// </summary>
/// <remarks>
/// <para><b>Important:</b> This interface should return the <b>actual size</b> that will be written
/// by <c>TryFormat</c>, not a rough estimate. The generator uses this value for precise buffer
/// pre-allocation and capacity checks.</para>
/// <para>Implement this interface alongside <see cref="IUtf8SpanFormattable"/> to enable
/// pre-allocation of the exact buffer size needed.</para>
/// <para>The generator will use <see cref="TryGetUtf8Size"/> to pre-check buffer capacity
/// before calling <c>TryFormat</c>, avoiding expensive exceptions.</para>
/// <para><b>Guidelines:</b></para>
/// <list type="bullet">
/// <item><description>Return the exact size if it can be determined efficiently (O(1)).</description></item>
/// <item><description>Return a tight upper bound if exact calculation would be expensive.</description></item>
/// <item><description>Return <c>false</c> only if the size truly cannot be determined in advance.</description></item>
/// </list>
/// </remarks>
public interface IUtf8Size
{
    /// <summary>
    /// Tries to get the number of UTF-8 bytes required to format this instance.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider (culture).</param>
    /// <param name="size">
    /// When this method returns <c>true</c>, contains the <b>actual</b> byte count that
    /// <see cref="IUtf8SpanFormattable.TryFormat"/> will write. This should be the exact size,
    /// not an estimate or maximum theoretical size.
    /// </param>
    /// <returns>
    /// <c>true</c> if the size could be determined; <c>false</c> if the size cannot be
    /// efficiently calculated in advance (e.g., for complex nested structures).
    /// </returns>
    bool TryGetUtf8Size(ReadOnlySpan<char> format, IFormatProvider? provider, out int size);
}

/// <summary>
/// Interface for types that can be parsed from binary data.
/// </summary>
/// <remarks>
/// <para><b>Recommended:</b> Use the <c>[BinaryParsable]</c> attribute instead of implementing
/// this interface manually. The source generator will create an efficient <c>TryParse</c> method
/// automatically based on the struct's members.</para>
/// <para>This interface is primarily used by <see cref="BinaryParser.Read{T}"/> to parse
/// values generically. Types marked with <c>[BinaryParsable]</c> can optionally implement
/// this interface to enable generic parsing via <c>BinaryParser</c>.</para>
/// </remarks>
/// <typeparam name="TSelf">The implementing type (CRTP pattern).</typeparam>
/// <example>
/// <code>
/// // Preferred: Use attribute (generator creates TryParse automatically)
/// [BinaryParsable]
/// public readonly partial struct PacketHeader
/// {
///     public U16BE Version { get; init; }
///     public U32BE Length { get; init; }
/// }
/// 
/// // The generated code includes:
/// // - static bool TryParse(ReadOnlySpan&lt;byte&gt;, out PacketHeader, out int)
/// // - static PacketHeader Parse(ReadOnlySpan&lt;byte&gt;)
/// // - static int FixedSize { get; }
/// </code>
/// </example>
public interface IBinaryParsable<TSelf> where TSelf : IBinaryParsable<TSelf>
{
    /// <summary>
    /// Tries to get the fixed size in bytes of this type.
    /// </summary>
    /// <param name="size">
    /// When this method returns <c>true</c>, contains the exact byte count.
    /// For variable-length types, this should be -1 or the method should return <c>false</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the size is fixed and known; <c>false</c> for variable-length types.
    /// </returns>
    /// <remarks>
    /// Return <c>true</c> with a positive size for fixed-size types to enable optimized array parsing.
    /// Return <c>false</c> or set size to -1 for variable-length types (containing strings, VarInt, etc.).
    /// </remarks>
    static abstract bool TryGetSerializedSize(out int size);

    /// <summary>
    /// Attempts to parse an instance from the given binary data.
    /// </summary>
    /// <param name="source">The source bytes to parse from.</param>
    /// <param name="value">The parsed value if successful.</param>
    /// <param name="bytesConsumed">The number of bytes consumed during parsing.</param>
    /// <returns><c>true</c> if parsing succeeded; <c>false</c> if there was insufficient data.</returns>
    static abstract bool TryParse(ReadOnlySpan<byte> source, out TSelf value, out int bytesConsumed);
}
