// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc;

// ============================================================================
// Binary Parsing & Writing Attributes
// ============================================================================
// These attributes enable automatic source generation of TryParse and TryWrite
// methods for structs and records. The generators create efficient,
// zero-allocation parsing and serialization code based on the type's members.
// ============================================================================

/// <summary>
/// Marks a struct or record for automatic binary parsing via source generator.
/// </summary>
/// <remarks>
/// <para>The generator will create a static <c>TryParse</c> method that reads
/// members in declaration order (or explicit order if <see cref="BinaryOrderAttribute"/> is used).</para>
/// <para><b>Generated method signature:</b></para>
/// <code>
/// public static bool TryParse(ReadOnlySpan&lt;byte&gt; source, out T value, out int bytesConsumed)
/// </code>
/// <para><b>Supported member types:</b></para>
/// <list type="bullet">
/// <item><description>Endian wrappers: <c>U16BE</c>, <c>U32LE</c>, <c>I64BE</c>, etc.</description></item>
/// <item><description>Variable-length: <c>VarInt</c>, <c>VarIntZigZag</c></description></item>
/// <item><description>Nested <c>[BinaryParsable]</c> types</description></item>
/// <item><description>Fixed-size byte arrays with <c>[BinaryFixedLength(n)]</c></description></item>
/// </list>
/// <para><b>Requirements:</b></para>
/// <list type="bullet">
/// <item><description>Type must be a <c>struct</c> or <c>record struct</c></description></item>
/// <item><description>All non-ignored members must be parsable types</description></item>
/// <item><description>If any member uses <c>[BinaryOrder]</c>, ALL members must specify order</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [BinaryParsable]
/// public readonly partial struct PacketHeader
/// {
///     public U16BE Version { get; init; }
///     public U32BE MessageType { get; init; }
///     public U16BE PayloadLength { get; init; }
///     
///     [BinaryIgnore]
///     public bool IsValid =&gt; Version.Value == 0x0100;
/// }
/// 
/// // Usage:
/// if (PacketHeader.TryParse(data, out var header, out int consumed))
/// {
///     Console.WriteLine($"Version: {header.Version.Value}");
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class BinaryParsableAttribute : Attribute
{
    /// <summary>
    /// Default endianness for primitive integer fields without explicit endian wrapper.
    /// </summary>
    /// <remarks>
    /// <para>This applies to <c>ushort</c>, <c>uint</c>, <c>ulong</c>, etc. when used directly.</para>
    /// <para>Endian wrapper types (<c>U16BE</c>, <c>U32LE</c>) ignore this setting.</para>
    /// </remarks>
    public Endianness DefaultEndianness { get; set; } = Endianness.BigEndian;
}

/// <summary>
/// Marks a struct or record for automatic binary serialization via source generator.
/// </summary>
/// <remarks>
/// <para>The generator will implement <see cref="IBinarySerializable"/> with
/// <c>TryWrite</c> and <c>TryGetWrittenSize</c> methods that write
/// members in declaration order (or explicit order if <see cref="BinaryOrderAttribute"/> is used).</para>
/// <para><b>Generated method signatures:</b></para>
/// <code>
/// public bool TryWrite(Span&lt;byte&gt; destination, out int bytesWritten)
/// public bool TryGetWrittenSize(out int size)
/// </code>
/// <para><b>Supported member types:</b></para>
/// <list type="bullet">
/// <item><description>Endian wrappers: <c>U16BE</c>, <c>U32LE</c>, <c>I64BE</c>, etc.</description></item>
/// <item><description>Variable-length: <c>VarInt</c>, <c>VarIntZigZag</c></description></item>
/// <item><description>Nested <c>[BinaryWritable]</c> or <c>IBinarySerializable</c> types</description></item>
/// <item><description>Fixed-size byte arrays with <c>[BinaryFixedLength(n)]</c></description></item>
/// <item><description>Strings with length encoding attributes</description></item>
/// </list>
/// <para><b>Requirements:</b></para>
/// <list type="bullet">
/// <item><description>Type must be a <c>struct</c> or <c>record struct</c></description></item>
/// <item><description>All non-ignored members must be writable types</description></item>
/// <item><description>If any member uses <c>[BinaryOrder]</c>, ALL members must specify order</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// [BinaryWritable]
/// public readonly partial struct PacketHeader : IBinarySerializable
/// {
///     public U16BE Version { get; init; }
///     public U32BE MessageType { get; init; }
///     public U16BE PayloadLength { get; init; }
/// }
/// 
/// // Usage with ZA.Bytes():
/// var header = new PacketHeader { Version = new(1), MessageType = new(42), PayloadLength = new(100) };
/// using var bytes = ZA.Bytes(header);
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Struct, Inherited = false)]
public sealed class BinaryWritableAttribute : Attribute
{
    /// <summary>
    /// Default endianness for primitive integer fields without explicit endian wrapper.
    /// </summary>
    /// <remarks>
    /// <para>This applies to <c>ushort</c>, <c>uint</c>, <c>ulong</c>, etc. when used directly.</para>
    /// <para>Endian wrapper types (<c>U16BE</c>, <c>U32LE</c>) ignore this setting.</para>
    /// </remarks>
    public Endianness DefaultEndianness { get; set; } = Endianness.BigEndian;
}

/// <summary>
/// Specifies the explicit order of a member in the binary layout.
/// </summary>
/// <remarks>
/// <para><b>Important:</b> If ANY member uses <c>[BinaryOrder]</c>, then ALL
/// non-ignored members MUST specify their order. Mixed implicit/explicit ordering
/// is not allowed and will cause a compile-time error.</para>
/// <para>Order values must be unique and should be sequential starting from 0.</para>
/// </remarks>
/// <example>
/// <code>
/// [BinaryParsable]
/// public readonly partial struct ReorderedPacket
/// {
///     [BinaryOrder(2)]  // Read third
///     public U16BE Checksum { get; init; }
///     
///     [BinaryOrder(0)]  // Read first
///     public U16BE Type { get; init; }
///     
///     [BinaryOrder(1)]  // Read second
///     public U32BE Length { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BinaryOrderAttribute : Attribute
{
    /// <summary>
    /// The zero-based order of this member in the binary layout.
    /// </summary>
    public int Order { get; }

    /// <summary>
    /// Creates a new <see cref="BinaryOrderAttribute"/> with the specified order.
    /// </summary>
    /// <param name="order">The zero-based order (0 = first, 1 = second, etc.).</param>
    public BinaryOrderAttribute(int order)
    {
        Order = order;
    }
}

/// <summary>
/// Excludes a member from binary parsing.
/// </summary>
/// <remarks>
/// <para>Use this for computed properties, cached values, or any member that
/// should not be read from the binary data.</para>
/// </remarks>
/// <example>
/// <code>
/// [BinaryParsable]
/// public readonly partial struct Message
/// {
///     public U16BE Type { get; init; }
///     public U16BE Length { get; init; }
///     
///     [BinaryIgnore]
///     public bool IsControl =&gt; Type.Value &lt; 100;
///     
///     [BinaryIgnore]
///     public DateTime ParsedAt { get; init; } = DateTime.UtcNow;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BinaryIgnoreAttribute : Attribute
{
}

/// <summary>
/// Specifies that a byte array or span member has a fixed length.
/// </summary>
/// <remarks>
/// <para>Required for <c>byte[]</c> members since the parser needs to know
/// how many bytes to read.</para>
/// </remarks>
/// <example>
/// <code>
/// [BinaryParsable]
/// public readonly partial struct MacAddress
/// {
///     [BinaryFixedLength(6)]
///     public byte[] Address { get; init; }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BinaryFixedLengthAttribute : Attribute
{
    /// <summary>
    /// The fixed number of bytes for this member.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Creates a new <see cref="BinaryFixedLengthAttribute"/> with the specified length.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    public BinaryFixedLengthAttribute(int length)
    {
        Length = length;
    }
}

/// <summary>
/// Configures custom parsing behavior for a member.
/// </summary>
/// <remarks>
/// <para>Use this for advanced scenarios like bit-level parsing, custom endianness, or padding.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BinaryFieldAttribute : Attribute
{
    /// <summary>
    /// Endianness for this specific field (overrides type-level default).
    /// Only applies to primitive integer types and bit fields, not to endian wrappers.
    /// </summary>
    public Endianness Endianness { get; set; } = Endianness.BigEndian;

    /// <summary>
    /// For sub-byte fields, specifies the number of bits to read.
    /// </summary>
    /// <remarks>
    /// <para>When set, the parser will use bit-level reading for this field.</para>
    /// <para>Valid values depend on the target type:</para>
    /// <list type="bullet">
    /// <item><description><c>byte</c>: 1-8 bits</description></item>
    /// <item><description><c>ushort</c>: 1-16 bits</description></item>
    /// <item><description><c>uint</c>: 1-32 bits</description></item>
    /// <item><description><c>ulong</c>: 1-64 bits</description></item>
    /// </list>
    /// </remarks>
    public int BitCount { get; set; } = -1;

    /// <summary>
    /// Number of padding bits to skip AFTER reading this field.
    /// </summary>
    /// <remarks>
    /// <para>Use this to skip reserved bits after a field or align to byte boundaries.</para>
    /// <para>Valid range: 1-64 bits.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// [BinaryParsable]
    /// public readonly partial struct FlagsPacket
    /// {
    ///     [BinaryField(BitCount = 4, PaddingBits = 4)]  // 4 bits + skip 4 reserved → byte-aligned
    ///     public byte Flags { get; init; }
    ///     
    ///     public U32BE Payload { get; init; }         // 4 bytes
    /// }
    /// </code>
    /// </example>
    public int PaddingBits { get; set; }
}

/// <summary>
/// Specifies how a string member's length is encoded in the binary data.
/// </summary>
/// <remarks>
/// <para>Required for string members in <c>[BinaryParsable]</c> types.</para>
/// <para>Strings are always encoded as UTF-8.</para>
/// </remarks>
/// <example>
/// <code>
/// [BinaryParsable]
/// public readonly partial struct Message
/// {
///     [BinaryStringLength(StringLengthEncoding.VarInt)]
///     public string Name { get; init; }              // VarInt prefix
///     
///     [BinaryStringLength(StringLengthEncoding.FixedBE, LengthBytes = 2)]
///     public string ShortText { get; init; }         // 2-byte BE prefix
///     
///     [BinaryStringLength(StringLengthEncoding.NullTerminated)]
///     public string Path { get; init; }              // Null-terminated
///     
///     [BinaryStringLength(StringLengthEncoding.Fixed, FixedLength = 32)]
///     public string FixedName { get; init; }         // Exactly 32 bytes
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BinaryStringLengthAttribute : Attribute
{
    /// <summary>
    /// The encoding used for the string length prefix.
    /// </summary>
    public StringLengthEncoding Encoding { get; }

    /// <summary>
    /// For <see cref="StringLengthEncoding.FixedBE"/> and <see cref="StringLengthEncoding.FixedLE"/>,
    /// specifies the number of bytes used for the length prefix (1, 2, or 4).
    /// Default is 4.
    /// </summary>
    public int LengthBytes { get; set; } = 4;

    /// <summary>
    /// For <see cref="StringLengthEncoding.Fixed"/>, specifies the exact byte length.
    /// The string will be truncated or padded with null bytes.
    /// </summary>
    public int FixedLength { get; set; } = -1;

    /// <summary>
    /// Creates a new <see cref="BinaryStringLengthAttribute"/> with the specified encoding.
    /// </summary>
    /// <param name="encoding">The length encoding to use.</param>
    public BinaryStringLengthAttribute(StringLengthEncoding encoding)
    {
        Encoding = encoding;
    }
}

/// <summary>
/// How string length is encoded in binary data.
/// </summary>
public enum StringLengthEncoding
{
    /// <summary>VarInt-encoded length prefix (1-10 bytes, most compact).</summary>
    VarInt,
    /// <summary>Fixed-size big-endian length prefix. Use <see cref="BinaryStringLengthAttribute.LengthBytes"/> to set size.</summary>
    FixedBE,
    /// <summary>Fixed-size little-endian length prefix. Use <see cref="BinaryStringLengthAttribute.LengthBytes"/> to set size.</summary>
    FixedLE,
    /// <summary>Null-terminated string (C-style).</summary>
    NullTerminated,
    /// <summary>Fixed byte length (no prefix). Use <see cref="BinaryStringLengthAttribute.FixedLength"/> to set size.</summary>
    Fixed
}

/// <summary>
/// Byte order for integer serialization/deserialization.
/// </summary>
public enum Endianness
{
    /// <summary>Most significant byte first (network byte order).</summary>
    BigEndian,
    /// <summary>Least significant byte first (Intel byte order).</summary>
    LittleEndian
}

// ============================================================================
// STRING LENGTH ATTRIBUTES
// ============================================================================

/// <summary>
/// Marks a string member as having a VarInt-encoded length prefix.
/// </summary>
/// <example>
/// <code>
/// [BinaryParsable]
/// public readonly partial struct Message
/// {
///     [StringLengthVarInt]
///     public string Name { get; init; }  // VarInt length + UTF-8 bytes
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StringLengthVarIntAttribute : Attribute { }

/// <summary>
/// Marks a string member as having a fixed-size big-endian length prefix.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StringLengthBEAttribute : Attribute
{
    /// <summary>Number of bytes for the length prefix (1, 2, or 4).</summary>
    public int LengthBytes { get; }

    /// <summary>Creates a new attribute for big-endian length-prefixed strings.</summary>
    /// <param name="lengthBytes">Number of bytes for the length prefix (1, 2, or 4). Default is 4.</param>
    public StringLengthBEAttribute(int lengthBytes = 4)
    {
        LengthBytes = lengthBytes;
    }
}

/// <summary>
/// Marks a string member as having a fixed-size little-endian length prefix.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StringLengthLEAttribute : Attribute
{
    /// <summary>Number of bytes for the length prefix (1, 2, or 4).</summary>
    public int LengthBytes { get; }

    /// <summary>Creates a new attribute for little-endian length-prefixed strings.</summary>
    /// <param name="lengthBytes">Number of bytes for the length prefix (1, 2, or 4). Default is 4.</param>
    public StringLengthLEAttribute(int lengthBytes = 4)
    {
        LengthBytes = lengthBytes;
    }
}

/// <summary>
/// Marks a string member as null-terminated (C-style string).
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StringNullTerminatedAttribute : Attribute { }

/// <summary>
/// Marks a string member as having a fixed byte length (no length prefix).
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StringFixedLengthAttribute : Attribute
{
    /// <summary>The exact number of bytes to read.</summary>
    public int Length { get; }

    /// <summary>Creates a new attribute for fixed-length strings.</summary>
    /// <param name="length">The exact number of bytes to read.</param>
    public StringFixedLengthAttribute(int length)
    {
        Length = length;
    }
}

/// <summary>
/// Marks a string member whose length is read from another field.
/// </summary>
/// <remarks>
/// <para>The length field must be declared BEFORE this field and must be an integer type.</para>
/// </remarks>
/// <example>
/// <code>
/// [BinaryParsable]
/// public readonly partial struct DynamicMessage
/// {
///     public U16BE NameLength { get; init; }  // Length field comes first
///     
///     [StringLengthFromField(nameof(NameLength))]
///     public string Name { get; init; }           // Reads NameLength bytes
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class StringLengthFromFieldAttribute : Attribute
{
    /// <summary>The name of the field containing the length.</summary>
    public string LengthFieldName { get; }

    /// <summary>Creates a new attribute for strings whose length comes from another field.</summary>
    /// <param name="lengthFieldName">The name of the field containing the length.</param>
    public StringLengthFromFieldAttribute(string lengthFieldName)
    {
        LengthFieldName = lengthFieldName;
    }
}

// ============================================================================
// BYTE ARRAY LENGTH ATTRIBUTES
// ============================================================================

/// <summary>
/// Marks a byte array/Memory member as having a VarInt-encoded length prefix.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BytesLengthVarIntAttribute : Attribute { }

/// <summary>
/// Marks a byte array/Memory member as having a fixed-size big-endian length prefix.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BytesLengthBEAttribute : Attribute
{
    /// <summary>Number of bytes for the length prefix (1, 2, or 4).</summary>
    public int LengthBytes { get; }

    /// <summary>Creates a new attribute for big-endian length-prefixed byte arrays/memory.</summary>
    /// <param name="lengthBytes">Number of bytes for the length prefix (1, 2, or 4). Default is 4.</param>
    public BytesLengthBEAttribute(int lengthBytes = 4)
    {
        LengthBytes = lengthBytes;
    }
}

/// <summary>
/// Marks a byte array/Memory member as having a fixed-size little-endian length prefix.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BytesLengthLEAttribute : Attribute
{
    /// <summary>Number of bytes for the length prefix (1, 2, or 4).</summary>
    public int LengthBytes { get; }

    /// <summary>Creates a new attribute for little-endian length-prefixed byte arrays/memory.</summary>
    /// <param name="lengthBytes">Number of bytes for the length prefix (1, 2, or 4). Default is 4.</param>
    public BytesLengthLEAttribute(int lengthBytes = 4)
    {
        LengthBytes = lengthBytes;
    }
}

/// <summary>
/// Marks a byte array/Memory member whose length is read from another field.
/// </summary>
/// <remarks>
/// <para>The length field must be declared BEFORE this field and must be an integer type.</para>
/// </remarks>
/// <example>
/// <code>
/// [BinaryParsable]
/// public readonly partial struct DataPacket
/// {
///     public U32BE DataLength { get; init; }  // Length field comes first
///     
///     [BytesLengthFromField(nameof(DataLength))]
///     public byte[] Data { get; init; }           // Reads DataLength bytes
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class BytesLengthFromFieldAttribute : Attribute
{
    /// <summary>The name of the field containing the length.</summary>
    public string LengthFieldName { get; }

    /// <summary>Creates a new attribute for byte arrays/memory whose length comes from another field.</summary>
    /// <param name="lengthFieldName">The name of the field containing the length.</param>
    public BytesLengthFromFieldAttribute(string lengthFieldName)
    {
        LengthFieldName = lengthFieldName;
    }
}
