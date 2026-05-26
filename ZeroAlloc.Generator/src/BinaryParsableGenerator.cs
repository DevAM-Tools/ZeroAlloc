// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// ZeroAlloc Binary Parsing Generator
// ============================================================================
// This Roslyn Incremental Source Generator analyzes structs marked with
// [BinaryParsable] and generates static TryParse/Parse methods for
// zero-allocation binary deserialization.
//
// ARCHITECTURE OVERVIEW:
// ----------------------
// 1. SYNTAX ANALYSIS PHASE
//    - IsPotentialBinaryParsableType: Quick syntax filter for struct declarations
//    - GetParsableTypeInfo: Semantic analysis to extract member information
//
// 2. MEMBER ANALYSIS PHASE
//    - AnalyzeMemberKind: Determines how to parse each member type
//    - Validates ordering (all explicit or all implicit)
//    - Detects byte arrays, strings, and nested [BinaryParsable] types
//
// 3. VALIDATION PHASE
//    - ValidateAndOrderMembers: Checks for ordering conflicts and duplicates
//    - Reports diagnostics for unparsable types, missing attributes, etc.
//
// 4. CODE GENERATION PHASE
//    - GenerateParsingCode: Generates the partial struct with TryParse/Parse
//    - Generates member-specific parsing code based on member kind
//
// SUPPORTED MEMBER TYPES:
// - Primitive integers with endian wrappers (U16BE, U32LE, etc.)
// - Variable-length integers (VarInt32, VarInt64)
// - Fixed-length byte arrays ([BinaryFixedLength])
// - Dynamic-length byte arrays (with length prefix or remaining)
// - Strings with various length encodings
// - Nested [BinaryParsable] structs
// - Memory<byte> for dynamic-length buffers
//
// CONFIGURATION (via attributes):
// - [BinaryOrder(n)]: Explicit member parsing order
// - [BinaryIgnore]: Exclude member from parsing
// - [BinaryFixedLength(n)]: Fixed-length for byte[] or string
// - [BinaryRemaining]: Use remaining bytes (last member only)
// - [BinaryStringEncoding(...)]: String encoding configuration
// - [BinaryBytesEncoding(...)]: Byte array length encoding
//
// DIAGNOSTICS:
// - ZA2001: Member type not parsable
// - ZA2002: Inconsistent ordering (mixed explicit/implicit)
// - ZA2003: Duplicate order value
// - ZA2004: byte[] without length specification
// - ZA2005-ZA2013: Various validation errors
// ============================================================================

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ZeroAlloc.Generator;

// ============================================================================
// ENDIANNESS ENUM (mirrored from ZeroAlloc for generator use)
// ============================================================================
// This enum is duplicated here because the generator runs at compile time
// and cannot reference the ZeroAlloc runtime types directly.
// ============================================================================

/// <summary>
/// Byte order for multi-byte integer serialization/deserialization.
/// </summary>
/// <remarks>
/// <para>
/// This is a compile-time mirror of <c>ZeroAlloc.Endianness</c> for use
/// within the source generator. Network protocols typically use BigEndian
/// (network byte order), while x86/x64 systems use LittleEndian natively.
/// </para>
/// </remarks>
internal enum Endianness
{
    /// <summary>Most significant byte first (network byte order, e.g., TCP/IP).</summary>
    BigEndian = 0,
    /// <summary>Least significant byte first (Intel/AMD native byte order).</summary>
    LittleEndian = 1
}

// ============================================================================
// DIAGNOSTIC DESCRIPTORS FOR PARSING
// ============================================================================
// These descriptors define the errors and warnings issued by the generator.
// Error codes are in the ZA2xxx range to distinguish from ZeroAllocGenerator
// (which uses ZA1xxx).
// ============================================================================

internal static partial class ParseDiagnostics
{
    /// <summary>
    /// ZA2001: Member type is not parsable.
    /// </summary>
    public static readonly DiagnosticDescriptor MemberNotParsable = new(
        id: "ZA2001",
        title: "Member type is not parsable",
        messageFormat: "Member '{0}' of type '{1}' cannot be parsed. Use an endian wrapper (U16BE, etc.), VarInt, or a [BinaryParsable] type.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2002: Inconsistent ordering - some members have [BinaryOrder] and some don't.
    /// </summary>
    public static readonly DiagnosticDescriptor InconsistentOrdering = new(
        id: "ZA2002",
        title: "Inconsistent member ordering",
        messageFormat: "If any member uses [BinaryOrder], all non-ignored members must specify their order. Missing: {0}.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2003: Duplicate order value.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateOrder = new(
        id: "ZA2003",
        title: "Duplicate order value",
        messageFormat: "Order value {0} is used by multiple members: {1}",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2004: byte[] without [BinaryFixedLength].
    /// </summary>
    public static readonly DiagnosticDescriptor ByteArrayWithoutLength = new(
        id: "ZA2004",
        title: "byte[] requires [BinaryFixedLength]",
        messageFormat: "Member '{0}' is a byte[] but has no [BinaryFixedLength] attribute. The parser needs to know how many bytes to read.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2005: [BinaryParsable] only allowed on structs.
    /// </summary>
    public static readonly DiagnosticDescriptor OnlyStructsAllowed = new(
        id: "ZA2005",
        title: "[BinaryParsable] only allowed on structs",
        messageFormat: "Type '{0}' is marked with [BinaryParsable] but is not a struct. Only structs and record structs are supported.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2006: Type must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "ZA2006",
        title: "Type must be partial",
        messageFormat: "Type '{0}' is marked with [BinaryParsable] but is not declared as partial. Add the 'partial' modifier.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2007: [BinaryOrder] and [BinaryIgnore] conflict.
    /// </summary>
    public static readonly DiagnosticDescriptor OrderAndIgnoreConflict = new(
        id: "ZA2007",
        title: "[BinaryOrder] and [BinaryIgnore] conflict",
        messageFormat: "Member '{0}' has both [BinaryOrder] and [BinaryIgnore]. Ignored members cannot have an order.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2008: Type requires byte alignment.
    /// </summary>
    public static readonly DiagnosticDescriptor RequiresByteAlignment = new(
        id: "ZA2008",
        title: "Type requires byte alignment",
        messageFormat: "Member '{0}' of type '{1}' requires byte alignment, but current bit offset is {2}. Add padding bits (total must be multiple of 8).",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2009: String requires length encoding attribute.
    /// </summary>
    public static readonly DiagnosticDescriptor StringWithoutLengthEncoding = new(
        id: "ZA2009",
        title: "String requires length encoding",
        messageFormat: "Member '{0}' is a string but has no length encoding. Use [StringLengthVarInt], [StringLengthBE], [StringLengthLE], [StringNullTerminated], [StringFixedLength], or [StringLengthFromField].",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2010: Invalid padding bits value.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidPaddingBits = new(
        id: "ZA2010",
        title: "Invalid padding bits value",
        messageFormat: "PaddingBits value {0} is invalid. Must be between 1 and 64.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2011: Length field must be declared before the field that uses it.
    /// </summary>
    public static readonly DiagnosticDescriptor LengthFieldOrderError = new(
        id: "ZA2011",
        title: "Length field must come before dependent field",
        messageFormat: "Member '{0}' references length field '{1}'. The length field must be declared before '{0}' in the parsing order.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2012: Length field not found.
    /// </summary>
    public static readonly DiagnosticDescriptor LengthFieldNotFound = new(
        id: "ZA2012",
        title: "Length field not found",
        messageFormat: "Member '{0}' references length field '{1}'. No such field exists.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2013: Bytes/Memory requires length encoding.
    /// </summary>
    public static readonly DiagnosticDescriptor BytesWithoutLengthEncoding = new(
        id: "ZA2013",
        title: "Bytes/Memory requires length encoding",
        messageFormat: "Member '{0}' is a byte[]/Memory<byte> but has no length encoding. Use [BinaryFixedLength], [BytesLengthVarInt], [BytesLengthBE], [BytesLengthLE], or [BytesLengthFromField].",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2014: [BinaryFixedLength] or [StringFixedLength] value must be greater than zero.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidFixedLength = new(
        id: "ZA2014",
        title: "Fixed length must be greater than zero",
        messageFormat: "Member '{0}' has a fixed length of {1}, which is invalid. The length must be greater than zero.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA2015: Multiple conflicting length/encoding attributes on a single member.
    /// </summary>
    public static readonly DiagnosticDescriptor ConflictingEncodingAttributes = new(
        id: "ZA2015",
        title: "Conflicting length/encoding attributes",
        messageFormat: "Member '{0}' has multiple conflicting length/encoding attributes. Only one length encoding attribute is allowed per member.",
        category: "ZeroAlloc.Parsing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}

// ============================================================================
// DATA TYPES
// ============================================================================
// These records hold information extracted during syntax/semantic analysis.
// They are used during code generation to produce the correct parsing code.
//
// Key types:
// - ParsableTypeInfo: Represents a [BinaryParsable] struct
// - ParsableMemberInfo: Represents a single member to parse
// - StringLengthEncodingInfo: How string length is encoded
// - BytesLengthEncodingInfo: How byte array length is encoded
// ============================================================================

/// <summary>
/// Information about a <c>[BinaryParsable]</c> struct for code generation.
/// </summary>
/// <remarks>
/// This record contains all information needed to generate the partial struct
/// with TryParse and Parse methods.
/// </remarks>
/// <param name="TypeName">Simple type name (e.g., "NetworkPacket").</param>
/// <param name="Namespace">Containing namespace, or <c>null</c> for global.</param>
/// <param name="FullTypeName">Fully qualified type name.</param>
/// <param name="IsReadOnly">Whether the struct is declared readonly.</param>
/// <param name="IsRecordStruct">Whether this is a record struct.</param>
/// <param name="DefaultEndianness">Default byte order for multi-byte integers.</param>
/// <param name="Members">Ordered list of members to parse.</param>
/// <param name="Location">Source location for diagnostics.</param>
internal readonly record struct ParsableTypeInfo(
    string TypeName,
    string? Namespace,
    string FullTypeName,
    bool IsReadOnly,
    bool IsRecordStruct,
    Endianness DefaultEndianness,
    ImmutableArray<ParsableMemberInfo> Members,
    Location Location);

/// <summary>
/// Information about a single member to parse within a <c>[BinaryParsable]</c> struct.
/// </summary>
/// <remarks>
/// <para>
/// Each member has a <see cref="Kind"/> that determines which parsing code to generate.
/// The parsing order is determined by:
/// </para>
/// <list type="number">
///   <item><see cref="ExplicitOrder"/> if <c>[BinaryOrder]</c> is specified</item>
///   <item><see cref="DeclaredOrder"/> (source order) otherwise</item>
/// </list>
/// </remarks>
internal record struct ParsableMemberInfo(
    string Name,
    string TypeName,
    string FullTypeName,
    int? ExplicitOrder,
    int DeclaredOrder,
    bool IsProperty,
    ParsableMemberKind Kind,
    int? FixedLength,
    int? BitCount,
    int PaddingBits,
    Endianness? ExplicitEndianness,
    StringLengthEncodingInfo? StringEncoding,
    BytesLengthEncodingInfo? BytesEncoding,
    string? LengthFromField,
    Location Location,
    int StringEncodingAttributeCount,
    int BytesEncodingAttributeCount)
{
    /// <summary>
    /// True if this member has both <c>[BinaryIgnore]</c> and <c>[BinaryOrder]</c> (conflict).
    /// </summary>
    public bool IsIgnoredWithOrder { get; init; } = false;
}

/// <summary>
/// Information about how a string's length is encoded in the binary data.
/// </summary>
/// <param name="Encoding">The encoding kind (VarInt, Fixed, NullTerminated, etc.).</param>
/// <param name="LengthBytes">Number of bytes for the length prefix (1, 2, or 4).</param>
/// <param name="FixedLength">Fixed length in bytes for <see cref="StringLengthEncodingKind.Fixed"/>.</param>
internal readonly record struct StringLengthEncodingInfo(
    StringLengthEncodingKind Encoding,
    int LengthBytes,
    int FixedLength);

/// <summary>
/// Information about how a byte array's length is encoded in the binary data.
/// </summary>
/// <param name="Encoding">The encoding kind (VarInt, Fixed, FromField, etc.).</param>
/// <param name="LengthBytes">Number of bytes for the length prefix (1, 2, or 4).</param>
internal readonly record struct BytesLengthEncodingInfo(
    BytesLengthEncodingKind Encoding,
    int LengthBytes);

/// <summary>
/// How string length is encoded in the binary data.
/// </summary>
/// <remarks>
/// <para>The encoding determines how many bytes to read for the string content:</para>
/// <list type="bullet">
///   <item><b>VarInt:</b> Variable-length integer prefix (1-5 bytes)</item>
///   <item><b>FixedBE/FixedLE:</b> Fixed-size integer prefix (1/2/4 bytes)</item>
///   <item><b>NullTerminated:</b> Read until null byte (0x00)</item>
///   <item><b>Fixed:</b> Fixed byte count from <c>[BinaryFixedLength]</c></item>
///   <item><b>FromField:</b> Length comes from another parsed field</item>
/// </list>
/// </remarks>
internal enum StringLengthEncodingKind
{
    /// <summary>Variable-length integer prefix.</summary>
    VarInt = 0,
    /// <summary>Fixed-size big-endian integer prefix.</summary>
    FixedBE = 1,
    /// <summary>Fixed-size little-endian integer prefix.</summary>
    FixedLE = 2,
    /// <summary>Read until null byte (0x00).</summary>
    NullTerminated = 3,
    /// <summary>Fixed byte count from <c>[BinaryFixedLength]</c>.</summary>
    Fixed = 4,
    /// <summary>Length from another parsed field.</summary>
    FromField = 5
}

/// <summary>
/// How byte array/Memory length is encoded in the binary data.
/// </summary>
/// <remarks>
/// Similar to <see cref="StringLengthEncodingKind"/> but without null-terminated option.
/// </remarks>
internal enum BytesLengthEncodingKind
{
    /// <summary>Fixed byte count from <c>[BinaryFixedLength]</c>.</summary>
    Fixed = 0,
    /// <summary>Variable-length integer prefix.</summary>
    VarInt = 1,
    /// <summary>Fixed-size big-endian integer prefix.</summary>
    FixedBE = 2,
    /// <summary>Fixed-size little-endian integer prefix.</summary>
    FixedLE = 3,
    /// <summary>Length from another parsed field.</summary>
    FromField = 4
}

/// <summary>
/// Kind of parsable member, determining which parsing code to generate.
/// </summary>
/// <remarks>
/// <para>
/// Each kind maps to specific parsing logic in the generated code:
/// </para>
/// <list type="bullet">
///   <item><b>EndianWrapper:</b> U16BE.TryParse(), I32LE.TryParse(), etc.</item>
///   <item><b>VarInt:</b> VarInt.TryReadValue() or VarIntZigZag.TryReadValue()</item>
///   <item><b>NestedParsable:</b> Recursive TypeName.TryParse() call</item>
///   <item><b>BitField:</b> Uses BitReader for sub-byte reading</item>
///   <item><b>String:</b> Length-prefixed or null-terminated UTF-8 string</item>
///   <item><b>ByteArray/ByteArrayDynamic:</b> Fixed or dynamic-length byte[]</item>
///   <item><b>MemoryDynamic:</b> Dynamic-length Memory&lt;byte&gt;</item>
/// </list>
/// </remarks>
internal enum ParsableMemberKind
{
    /// <summary>Endian wrapper types: U16BE, I32LE, U64BE, etc.</summary>
    EndianWrapper,
    /// <summary>Variable-length integer: VarInt32, VarInt64, VarIntZigZag32, etc.</summary>
    VarInt,
    /// <summary>Nested type with <c>[BinaryParsable]</c> attribute.</summary>
    NestedParsable,
    /// <summary>Fixed-length byte array with <c>[BinaryFixedLength]</c>.</summary>
    ByteArray,
    /// <summary>Dynamic-length byte array with length prefix or FromField.</summary>
    ByteArrayDynamic,
    /// <summary><c>Memory&lt;byte&gt;</c> or <c>ReadOnlyMemory&lt;byte&gt;</c> with dynamic length.</summary>
    MemoryDynamic,
    /// <summary>Single byte (byte type).</summary>
    Byte,
    /// <summary>Primitive integer with explicit endianness attribute.</summary>
    PrimitiveInteger,
    /// <summary>Bit field with <c>[BinaryField(BitCount = n)]</c> for sub-byte reading.</summary>
    BitField,
    /// <summary>String with length encoding (VarInt, fixed, null-terminated, etc.).</summary>
    String,
    /// <summary>Unknown/unsupported type - will generate ZA2001 error.</summary>
    Unknown
}

// ============================================================================
// GENERATOR
// ============================================================================
// The generator uses Roslyn's ForAttributeWithMetadataName for efficient
// filtering of [BinaryParsable] types. This is faster than the generic
// CreateSyntaxProvider used by ZeroAllocGenerator.
// ============================================================================

/// <summary>
/// Roslyn incremental source generator for <c>[BinaryParsable]</c> types.
/// </summary>
/// <remarks>
/// <para>
/// This generator produces TryParse and Parse methods for structs marked with
/// <c>[BinaryParsable]</c>. The generated code handles:
/// </para>
/// <list type="bullet">
///   <item>Endian conversion for multi-byte integers</item>
///   <item>Variable-length integer decoding (VarInt)</item>
///   <item>String length encoding (VarInt, fixed, null-terminated)</item>
///   <item>Nested [BinaryParsable] types</item>
///   <item>Bit fields for sub-byte parsing</item>
/// </list>
/// </remarks>
[Generator]
public sealed class BinaryParsableGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental generator pipeline.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with [BinaryParsable] attribute
        // ForAttributeWithMetadataName is more efficient than CreateSyntaxProvider
        // because Roslyn can filter by attribute name directly
        IncrementalValuesProvider<ParsableTypeInfo?> parsableTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: "ZeroAlloc.BinaryParsableAttribute",
                predicate: static (node, _) => node is StructDeclarationSyntax or RecordDeclarationSyntax,
                transform: static (ctx, _) => GetParsableTypeInfo(ctx))
            .Where(static info => info is not null);

        // Generate parsing code for each [BinaryParsable] type
        context.RegisterSourceOutput(parsableTypes, GenerateParsingCode);
    }

    /// <summary>
    /// Extracts type and member information from a <c>[BinaryParsable]</c> struct.
    /// </summary>
    /// <remarks>
    /// This method analyzes the struct's members to determine:
    /// <list type="bullet">
    ///   <item>Which members to parse (excludes <c>[BinaryIgnore]</c>)</item>
    ///   <item>Parsing order (<c>[BinaryOrder]</c> or declaration order)</item>
    ///   <item>How to parse each member type</item>
    /// </list>
    /// </remarks>
    /// <param name="ctx">The generator attribute syntax context.</param>
    /// <returns>Type information if valid; otherwise <c>null</c>.</returns>
    private static ParsableTypeInfo? GetParsableTypeInfo(GeneratorAttributeSyntaxContext ctx)
    {
        // Get the struct/record symbol
        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        // Validate it's a struct
        if (!typeSymbol.IsValueType || typeSymbol.TypeKind != TypeKind.Struct)
        {
            // Will report diagnostic during generation
            return null;
        }

        // Check if partial
        SyntaxNode targetNode = ctx.TargetNode;
        bool isPartial = targetNode switch
        {
            StructDeclarationSyntax s => s.Modifiers.Any(SyntaxKind.PartialKeyword),
            RecordDeclarationSyntax r => r.Modifiers.Any(SyntaxKind.PartialKeyword),
            _ => false
        };

        if (!isPartial)
        {
            // Will report diagnostic during generation
            return null;
        }

        // Get attribute data
        AttributeData? binaryParsableAttr = ctx.Attributes.FirstOrDefault();
        Endianness defaultEndianness = Endianness.BigEndian;
        if (binaryParsableAttr is not null)
        {
            foreach (KeyValuePair<string, TypedConstant> arg in binaryParsableAttr.NamedArguments)
            {
                if (arg.Key == "DefaultEndianness" && arg.Value.Value is int endianValue)
                {
                    defaultEndianness = (Endianness)endianValue;
                }
            }
        }

        // Get namespace and type info
        string typeName = typeSymbol.Name;
        string? ns = typeSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : typeSymbol.ContainingNamespace?.ToDisplayString();
        string fullTypeName = typeSymbol.ToDisplayString();
        bool isReadOnly = typeSymbol.IsReadOnly;
        bool isRecordStruct = targetNode is RecordDeclarationSyntax;

        // Collect members
        List<ParsableMemberInfo> members = new();
        int declaredOrder = 0;

        foreach (ISymbol member in typeSymbol.GetMembers())
        {
            // Skip static, methods, etc.
            if (member.IsStatic)
            {
                continue;
            }

            // Only fields and properties
            ITypeSymbol? memberType = null;
            bool isProperty = false;
            Location memberLocation = member.Locations.FirstOrDefault() ?? Location.None;

            if (member is IFieldSymbol field)
            {
                // Skip compiler-generated backing fields for auto-properties
                if (field.IsImplicitlyDeclared || field.Name.StartsWith("<"))
                {
                    continue;
                }

                memberType = field.Type;
                isProperty = false;
            }
            else if (member is IPropertySymbol prop)
            {
                // Skip indexers and computed properties without setter/init
                if (prop.IsIndexer)
                {
                    continue;
                }

                if (prop.SetMethod is null && !prop.IsRequired)
                {
                    continue; // Read-only computed property
                }

                memberType = prop.Type;
                isProperty = true;
            }
            else
            {
                continue;
            }

            // Check for [BinaryIgnore]
            bool isIgnored = member.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "BinaryIgnoreAttribute" ||
                a.AttributeClass?.ToDisplayString() == "ZeroAlloc.BinaryIgnoreAttribute");

            // Get [BinaryOrder] if present
            int? explicitOrder = null;
            AttributeData? orderAttr = member.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.Name == "BinaryOrderAttribute" ||
                a.AttributeClass?.ToDisplayString() == "ZeroAlloc.BinaryOrderAttribute");
            if (orderAttr is not null && orderAttr.ConstructorArguments.Length > 0)
            {
                explicitOrder = orderAttr.ConstructorArguments[0].Value as int?;
            }

            // Check for [BinaryOrder] + [BinaryIgnore] conflict
            // We track this to report an error during validation
            if (isIgnored && explicitOrder.HasValue)
            {
                // We'll still add this member so validation can report the error
                members.Add(new ParsableMemberInfo(
                    member.Name,
                    memberType.Name,
                    memberType.ToDisplayString(),
                    explicitOrder,
                    declaredOrder++,
                    isProperty,
                    ParsableMemberKind.Unknown, // Mark as unknown, error will be reported
                    null, null, 0, null, null, null, null,
                    memberLocation, 0, 0)
                { IsIgnoredWithOrder = true });
                continue;
            }

            if (isIgnored)
            {
                continue;
            }

            // Get [BinaryFixedLength] if present
            int? fixedLength = null;
            AttributeData? lengthAttr = member.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.Name == "BinaryFixedLengthAttribute" ||
                a.AttributeClass?.ToDisplayString() == "ZeroAlloc.BinaryFixedLengthAttribute");
            if (lengthAttr is not null && lengthAttr.ConstructorArguments.Length > 0)
            {
                fixedLength = lengthAttr.ConstructorArguments[0].Value as int?;
            }

            // Get [BinaryField] if present
            int? bitCount = null;
            int paddingBits = 0;
            Endianness? explicitEndianness = null;
            AttributeData? fieldAttr = member.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.Name == "BinaryFieldAttribute" ||
                a.AttributeClass?.ToDisplayString() == "ZeroAlloc.BinaryFieldAttribute");
            if (fieldAttr is not null)
            {
                foreach (KeyValuePair<string, TypedConstant> arg in fieldAttr.NamedArguments)
                {
                    if (arg.Key == "BitCount" && arg.Value.Value is int bc && bc > 0)
                    {
                        bitCount = bc;
                    }
                    else if (arg.Key == "Endianness" && arg.Value.Value is int endian)
                    {
                        explicitEndianness = (Endianness)endian;
                    }
                    else if (arg.Key == "PaddingBits" && arg.Value.Value is int pb && pb > 0)
                    {
                        paddingBits = pb;
                    }
                }
            }

            // Get string length encoding from attributes (new separate attributes + legacy BinaryStringLength)
            StringLengthEncodingInfo? stringEncoding = null;
            string? lengthFromField = null;
            int stringEncodingCount = 0; // Track conflicting attributes

            foreach (AttributeData attr in member.GetAttributes())
            {
                string attrName = attr.AttributeClass?.Name ?? "";

                switch (attrName)
                {
                    case "StringLengthVarIntAttribute":
                        stringEncoding = new StringLengthEncodingInfo(StringLengthEncodingKind.VarInt, 0, -1);
                        stringEncodingCount++;
                        break;
                    case "StringLengthBEAttribute":
                        int beLengthBytes = attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int beLen ? beLen : 4;
                        stringEncoding = new StringLengthEncodingInfo(StringLengthEncodingKind.FixedBE, beLengthBytes, -1);
                        stringEncodingCount++;
                        break;
                    case "StringLengthLEAttribute":
                        int leLengthBytes = attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int leLen ? leLen : 4;
                        stringEncoding = new StringLengthEncodingInfo(StringLengthEncodingKind.FixedLE, leLengthBytes, -1);
                        stringEncodingCount++;
                        break;
                    case "StringNullTerminatedAttribute":
                        stringEncoding = new StringLengthEncodingInfo(StringLengthEncodingKind.NullTerminated, 0, -1);
                        stringEncodingCount++;
                        break;
                    case "StringFixedLengthAttribute":
                        int strFixedLen = attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is int sfl ? sfl : 0;
                        stringEncoding = new StringLengthEncodingInfo(StringLengthEncodingKind.Fixed, 0, strFixedLen);
                        stringEncodingCount++;
                        break;
                    case "StringLengthFromFieldAttribute":
                        lengthFromField = attr.ConstructorArguments.Length > 0 ? attr.ConstructorArguments[0].Value as string : null;
                        stringEncoding = new StringLengthEncodingInfo(StringLengthEncodingKind.FromField, 0, -1);
                        stringEncodingCount++;
                        break;
                    case "BinaryStringLengthAttribute":
                        // Legacy attribute support
                        stringEncodingCount++;
                        if (attr.ConstructorArguments.Length > 0)
                        {
                            int encodingValue = (int)(attr.ConstructorArguments[0].Value ?? 0);
                            int legacyLengthBytes = 4;
                            int legacyFixedLength = -1;

                            foreach (KeyValuePair<string, TypedConstant> arg in attr.NamedArguments)
                            {
                                if (arg.Key == "LengthBytes" && arg.Value.Value is int lb)
                                {
                                    legacyLengthBytes = lb;
                                }
                                else if (arg.Key == "FixedLength" && arg.Value.Value is int fl)
                                {
                                    legacyFixedLength = fl;
                                }
                            }

                            stringEncoding = new StringLengthEncodingInfo(
                                (StringLengthEncodingKind)encodingValue,
                                legacyLengthBytes,
                                legacyFixedLength);
                        }
                        break;
                }
            }

            // Get bytes length encoding from attributes
            BytesLengthEncodingInfo? bytesEncoding = null;
            string? bytesLengthFromField = null;
            int bytesEncodingCount = 0; // Track conflicting attributes

            foreach (AttributeData bytesAttr in member.GetAttributes())
            {
                string attrName = bytesAttr.AttributeClass?.Name ?? "";

                switch (attrName)
                {
                    case "BytesLengthVarIntAttribute":
                        bytesEncoding = new BytesLengthEncodingInfo(BytesLengthEncodingKind.VarInt, 0);
                        bytesEncodingCount++;
                        break;
                    case "BytesLengthBEAttribute":
                        int beBytesLen = bytesAttr.ConstructorArguments.Length > 0 && bytesAttr.ConstructorArguments[0].Value is int bebl ? bebl : 4;
                        bytesEncoding = new BytesLengthEncodingInfo(BytesLengthEncodingKind.FixedBE, beBytesLen);
                        bytesEncodingCount++;
                        break;
                    case "BytesLengthLEAttribute":
                        int leBytesLen = bytesAttr.ConstructorArguments.Length > 0 && bytesAttr.ConstructorArguments[0].Value is int lebl ? lebl : 4;
                        bytesEncoding = new BytesLengthEncodingInfo(BytesLengthEncodingKind.FixedLE, leBytesLen);
                        bytesEncodingCount++;
                        break;
                    case "BytesLengthFromFieldAttribute":
                        bytesLengthFromField = bytesAttr.ConstructorArguments.Length > 0 ? bytesAttr.ConstructorArguments[0].Value as string : null;
                        bytesEncoding = new BytesLengthEncodingInfo(BytesLengthEncodingKind.FromField, 0);
                        bytesEncodingCount++;
                        break;
                    case "BinaryFixedLengthAttribute":
                        // Existing attribute - already handled by fixedLength above
                        break;
                }
            }

            // Combine lengthFromField - prefer string, then bytes
            string? combinedLengthFromField = lengthFromField ?? bytesLengthFromField;

            // Determine member kind
            // If BitCount is specified, it's a bit field regardless of the underlying type
            ParsableMemberKind kind = bitCount.HasValue && bitCount.Value > 0
                ? ParsableMemberKind.BitField
                : AnalyzeMemberKind(memberType, stringEncoding.HasValue, bytesEncoding.HasValue);

            members.Add(new ParsableMemberInfo(
                member.Name,
                memberType.Name,
                memberType.ToDisplayString(),
                explicitOrder,
                declaredOrder++,
                isProperty,
                kind,
                fixedLength,
                bitCount,
                paddingBits,
                explicitEndianness,
                stringEncoding,
                bytesEncoding,
                combinedLengthFromField,
                memberLocation,
                stringEncodingCount,
                bytesEncodingCount));
        }

        return new ParsableTypeInfo(
            typeName,
            ns,
            fullTypeName,
            isReadOnly,
            isRecordStruct,
            defaultEndianness,
            members.ToImmutableArray(),
            targetNode.GetLocation());
    }

    /// <summary>
    /// Analyzes a member type to determine how to parse it.
    /// </summary>
    private static ParsableMemberKind AnalyzeMemberKind(ITypeSymbol type, bool hasStringEncoding, bool hasBytesEncoding)
    {
        string typeName = type.Name;
        string fullName = type.ToDisplayString();

        // Check for string with encoding
        if (type.SpecialType == SpecialType.System_String)
        {
            return hasStringEncoding ? ParsableMemberKind.String : ParsableMemberKind.Unknown;
        }

        // Check for endian wrappers
        if (IsEndianWrapper(typeName))
        {
            return ParsableMemberKind.EndianWrapper;
        }

        // Check for VarInt types
        if (typeName is "VarInt" or "VarIntZigZag")
        {
            return ParsableMemberKind.VarInt;
        }

        // Check for byte
        if (type.SpecialType == SpecialType.System_Byte)
        {
            return ParsableMemberKind.Byte;
        }

        // Check for byte[]
        if (type is IArrayTypeSymbol arrayType && arrayType.ElementType.SpecialType == SpecialType.System_Byte)
        {
            return hasBytesEncoding ? ParsableMemberKind.ByteArrayDynamic : ParsableMemberKind.ByteArray;
        }

        // Check for Memory<byte> and ReadOnlyMemory<byte>
        if (fullName is "System.Memory<byte>" or "System.ReadOnlyMemory<byte>")
        {
            return hasBytesEncoding ? ParsableMemberKind.MemoryDynamic : ParsableMemberKind.Unknown;
        }

        // Check for [BinaryParsable] nested type
        if (type.GetAttributes().Any(a =>
            a.AttributeClass?.Name == "BinaryParsableAttribute" ||
            a.AttributeClass?.ToDisplayString() == "ZeroAlloc.BinaryParsableAttribute"))
        {
            return ParsableMemberKind.NestedParsable;
        }

        // Check for types implementing IBinaryParsable<T> (including our wrappers like Utf8Var, BytesVar, etc.)
        if (type.AllInterfaces.Any(i =>
            i.OriginalDefinition.ToDisplayString() == "ZeroAlloc.IBinaryParsable<T>" ||
            (i.IsGenericType && i.Name == "IBinaryParsable" && i.ContainingNamespace?.Name == "ZeroAlloc")))
        {
            return ParsableMemberKind.NestedParsable;
        }

        // Check for primitive integers (need explicit endianness)
        if (IsPrimitiveInteger(type))
        {
            return ParsableMemberKind.PrimitiveInteger;
        }

        return ParsableMemberKind.Unknown;
    }

    // Type recognition helpers delegate to BinaryGeneratorHelpers
    private static bool IsEndianWrapper(string typeName) => BinaryGeneratorHelpers.IsEndianWrapper(typeName);
    private static bool IsPrimitiveInteger(ITypeSymbol type) => BinaryGeneratorHelpers.IsPrimitiveInteger(type);

    // ========================================================================
    // CODE GENERATION
    // ========================================================================
    // This section generates the actual TryParse and Parse methods for
    // [BinaryParsable] structs. The generated code handles:
    //
    // - Size validation (if fixed size is known)
    // - Sequential parsing of each member in order
    // - BitReader for sub-byte (bit field) parsing
    // - Error propagation for nested TryParse failures
    // ========================================================================

    /// <summary>
    /// Generates the partial struct with TryParse and Parse methods.
    /// </summary>
    /// <remarks>
    /// <para>The generated code follows this pattern:</para>
    /// <list type="number">
    ///   <item>TryGetSerializedSize() - Returns fixed size if known</item>
    ///   <item>TryParse() - Attempts to parse, returns false on failure</item>
    ///   <item>Parse() - Parses or throws InvalidOperationException</item>
    /// </list>
    /// <para>
    /// For types with bit fields, a BitReader is used to handle sub-byte parsing.
    /// For types with only byte-aligned fields, direct span slicing is used.
    /// </para>
    /// </remarks>
    /// <param name="ctx">The source production context for output and diagnostics.</param>
    /// <param name="typeInfoNullable">The type information, or null if invalid.</param>
    private static void GenerateParsingCode(SourceProductionContext ctx, ParsableTypeInfo? typeInfoNullable)
    {
        if (typeInfoNullable is not ParsableTypeInfo typeInfo)
        {
            return;
        }

        // Validate members
        List<ParsableMemberInfo> orderedMembers = ValidateAndOrderMembers(ctx, typeInfo);
        if (orderedMembers.Count == 0 && typeInfo.Members.Length > 0)
        {
            return; // Errors were reported
        }

        // Check if any member uses bit-level parsing
        bool usesBitReader = orderedMembers.Any(m => m.Kind == ParsableMemberKind.BitField);

        // Generate code
        StringBuilder sb = new();

        // File header — two using directives are always emitted; ZeroAlloc is added only for BitReader types
        sb.Append("""
        // <auto-generated />
        // Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.
        #nullable enable

        using System;
        using System.Buffers.Binary;
        """);
        if (usesBitReader)
        {
            sb.AppendLine();
            sb.Append("using ZeroAlloc;");
        }
        sb.AppendLine();
        sb.AppendLine();

        // Namespace
        if (typeInfo.Namespace is not null)
        {
            sb.Append($$"""
            namespace {{typeInfo.Namespace}};

            """);
        }

        // Struct declaration
        string modifiers = typeInfo.IsReadOnly ? "readonly partial" : "partial";
        string keyword = typeInfo.IsRecordStruct ? "record struct" : "struct";
        sb.Append($$"""
        {{modifiers}} {{keyword}} {{typeInfo.TypeName}}
        {

        """);

        // Calculate fixed size if possible
        int? fixedSize = CalculateFixedSize(orderedMembers);
        int? fixedBits = usesBitReader ? CalculateFixedBits(orderedMembers) : null;

        // Generate TryGetSerializedSize method
        sb.Append("""
            /// <summary>
            /// Tries to get the fixed size in bytes of this type.
            /// </summary>
            public static bool TryGetSerializedSize(out int size)
            {

        """);
        if (fixedBits.HasValue)
        {
            int byteSize = (fixedBits.Value + 7) / 8;
            sb.Append($$"""
                    size = {{byteSize}};
                    return true;

            """);
        }
        else if (fixedSize.HasValue)
        {
            sb.Append($$"""
                    size = {{fixedSize.Value}};
                    return true;

            """);
        }
        else
        {
            sb.Append("""
                    size = -1;
                    return false;

            """);
        }
        sb.Append("""
            }

        """);

        // Generate TryParse method
        sb.Append($$"""
            /// <summary>
            /// Attempts to parse an instance from binary data.
            /// </summary>
            public static bool TryParse(ReadOnlySpan<byte> source, out {{typeInfo.TypeName}} value, out int bytesConsumed)
            {

        """);

        // Size check for fixed-size types
        int requiredBytes = fixedBits.HasValue ? (fixedBits.Value + 7) / 8 : (fixedSize ?? 0);
        if (requiredBytes > 0)
        {
            sb.Append($$"""
                    if (source.Length < {{requiredBytes}})
                    {
                        value = default;
                        bytesConsumed = 0;
                        return false;
                    }

            """);
        }

        if (usesBitReader)
        {
            sb.Append("""
                    var reader = new BitReader(source);

            """);
        }
        else
        {
            sb.Append("""
                    int offset = 0;

            """);
        }

        // Pre-compute grouped bounds checks for variable-size structs in byte-aligned mode.
        // For each consecutive run of fixed-size members, one check covers the entire run,
        // preventing ArgumentOutOfRangeException on span.Slice() when the buffer is too short.
        Dictionary<int, int> groupChecks = BinaryGeneratorHelpers.BuildGroupChecks(orderedMembers, usesBitReader, fixedSize);

        // Generate parsing code for each member
        for (int mi = 0; mi < orderedMembers.Count; mi++)
        {
            // Emit a single bounds check before the first member of each fixed-size run
            if (groupChecks.TryGetValue(mi, out int groupSize))
            {
                sb.Append($$"""
                        if (source.Length - offset < {{groupSize}})
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }

                """);
            }
            GenerateMemberParsing(sb, orderedMembers[mi], typeInfo.DefaultEndianness, usesBitReader);
        }

        // Construct result
        sb.AppendLine();
        sb.Append($$"""
                value = new {{typeInfo.TypeName}}
                {

        """);
        for (int i = 0; i < orderedMembers.Count; i++)
        {
            ParsableMemberInfo member = orderedMembers[i];
            string comma = i < orderedMembers.Count - 1 ? "," : "";
            sb.AppendLine($"            {member.Name} = _{member.Name}{comma}");
        }
        sb.Append("""
                };

        """);

        if (usesBitReader)
        {
            // Calculate bytes consumed: (totalBitsRead + 7) / 8
            sb.Append("""
                    bytesConsumed = (reader.BitOffset + 7) / 8;
                    return true;
                }
            """);
        }
        else
        {
            sb.Append("""
                    bytesConsumed = offset;
                    return true;
                }
            """);
        }

        // Generate Parse method (throws on failure)
        sb.Append($$"""

            /// <summary>
            /// Parses an instance from binary data. Throws if data is insufficient.
            /// </summary>
            public static {{typeInfo.TypeName}} Parse(ReadOnlySpan<byte> source)
            {
                if (!TryParse(source, out {{typeInfo.TypeName}} value, out _))
                    throw new InvalidOperationException("Insufficient data to parse {{typeInfo.TypeName}}");
                return value;
            }
        """);

        // If any member uses FromField, generate helper method
        if (orderedMembers.Any(m => m.LengthFromField is not null))
        {
            sb.Append("""

                // Helper method to extract length from various field types
                [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
                private static int GetLengthFromField<T>(T value) where T : struct
                {
                    // Handle primitive types and endian wrapper types
                    return value switch
                    {
                        byte v => v,
                        sbyte v => v,
                        short v => v,
                        ushort v => v,
                        int v => v,
                        uint v => (int)v,
                        long v => (int)v,
                        ulong v => (int)v,
                        ZeroAlloc.VarInt v => (int)v.Value,
                        ZeroAlloc.VarIntZigZag v => (int)v.Value,
                        nint v => (int)v,
                        nuint v => (int)v,
                        ZeroAlloc.U16BE v => v.Value,
                        ZeroAlloc.U16LE v => v.Value,
                        ZeroAlloc.I16BE v => v.Value,
                        ZeroAlloc.I16LE v => v.Value,
                        ZeroAlloc.U32BE v => (int)v.Value,
                        ZeroAlloc.U32LE v => (int)v.Value,
                        ZeroAlloc.I32BE v => v.Value,
                        ZeroAlloc.I32LE v => v.Value,
                        ZeroAlloc.U64BE v => (int)v.Value,
                        ZeroAlloc.U64LE v => (int)v.Value,
                        ZeroAlloc.I64BE v => (int)v.Value,
                        ZeroAlloc.I64LE v => (int)v.Value,
                        _ => throw new NotSupportedException($"Length field type {typeof(T).Name} is not supported")
                    };
                }
            """);
        }

        sb.AppendLine("}");

        // Add source
        ctx.AddSource($"{typeInfo.TypeName}.Parsing.g.cs", sb.ToString());
    }

    // ========================================================================
    // MEMBER VALIDATION AND ORDERING
    // ========================================================================
    // Before generating code, we validate all members and determine parsing order.
    // Validation checks:
    // - No [BinaryOrder] + [BinaryIgnore] conflicts
    // - Consistent ordering (all explicit or all implicit)
    // - No duplicate order values
    // - All member types are parsable
    // - Length fields are declared before dependent fields
    // ========================================================================

    /// <summary>
    /// Validates member configuration and returns them in parsing order.
    /// </summary>
    /// <remarks>
    /// <para>This method performs several validation steps:</para>
    /// <list type="number">
    ///   <item>Checks for <c>[BinaryOrder]</c> + <c>[BinaryIgnore]</c> conflicts</item>
    ///   <item>Validates ordering consistency (all explicit or all implicit)</item>
    ///   <item>Checks for duplicate order values</item>
    ///   <item>Validates all member types are parsable</item>
    ///   <item>Verifies length fields come before dependent fields</item>
    /// </list>
    /// <para>
    /// If any validation fails, diagnostics are reported and an empty list is returned,
    /// preventing code generation for invalid types.
    /// </para>
    /// </remarks>
    /// <param name="ctx">The source production context for reporting diagnostics.</param>
    /// <param name="typeInfo">The type information to validate.</param>
    /// <returns>Ordered list of members, or empty list if validation failed.</returns>
    private static List<ParsableMemberInfo> ValidateAndOrderMembers(
        SourceProductionContext ctx,
        ParsableTypeInfo typeInfo)
    {
        ImmutableArray<ParsableMemberInfo> members = typeInfo.Members;

        // Check for [BinaryOrder] + [BinaryIgnore] conflicts first
        foreach (ParsableMemberInfo member in members)
        {
            if (member.IsIgnoredWithOrder)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ParseDiagnostics.OrderAndIgnoreConflict,
                    member.Location,
                    member.Name));
                return new List<ParsableMemberInfo>();
            }
        }

        // Check for unknown types and strings without encoding
        foreach (ParsableMemberInfo member in members)
        {
            if (member.Kind == ParsableMemberKind.Unknown)
            {
                // Check if it's a string without encoding
                if (member.TypeName == "String" || member.FullTypeName == "string")
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        ParseDiagnostics.StringWithoutLengthEncoding,
                        member.Location,
                        member.Name));
                }
                // Check if it's a byte[] without encoding
                else if (member.FullTypeName == "byte[]" || member.TypeName == "Byte[]")
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        ParseDiagnostics.BytesWithoutLengthEncoding,
                        member.Location,
                        member.Name));
                }
                // Check if it's Memory<byte> or ReadOnlyMemory<byte> without encoding.
                // ToDisplayString() returns the keyword-alias form "System.Memory<byte>",
                // not the CLR name "System.Memory<System.Byte>" — match accordingly.
                else if (member.FullTypeName is "System.Memory<byte>" or "System.ReadOnlyMemory<byte>")
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        ParseDiagnostics.BytesWithoutLengthEncoding,
                        member.Location,
                        member.Name));
                }
                else
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        ParseDiagnostics.MemberNotParsable,
                        member.Location,
                        member.Name,
                        member.FullTypeName));
                }
                return new List<ParsableMemberInfo>();
            }

            if (member.Kind == ParsableMemberKind.ByteArray && member.FixedLength is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ParseDiagnostics.ByteArrayWithoutLength,
                    member.Location,
                    member.Name));
                return new List<ParsableMemberInfo>();
            }

            // Validate PaddingBits
            if (member.PaddingBits < 0 || member.PaddingBits > 64)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ParseDiagnostics.InvalidPaddingBits,
                    member.Location,
                    member.PaddingBits));
                return new List<ParsableMemberInfo>();
            }

            // ZA2014: Validate FixedLength > 0 (for [BinaryFixedLength] and [StringFixedLength])
            if (member.FixedLength is not null and <= 0)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ParseDiagnostics.InvalidFixedLength,
                    member.Location,
                    member.Name,
                    member.FixedLength));
                return new List<ParsableMemberInfo>();
            }

            if (member.StringEncoding is { Encoding: StringLengthEncodingKind.Fixed, FixedLength: <= 0 })
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ParseDiagnostics.InvalidFixedLength,
                    member.Location,
                    member.Name,
                    member.StringEncoding.Value.FixedLength));
                return new List<ParsableMemberInfo>();
            }

            // ZA2015: Validate no conflicting encoding attributes
            if (member.StringEncodingAttributeCount > 1)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ParseDiagnostics.ConflictingEncodingAttributes,
                    member.Location,
                    member.Name));
                return new List<ParsableMemberInfo>();
            }

            if (member.BytesEncodingAttributeCount > 1)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ParseDiagnostics.ConflictingEncodingAttributes,
                    member.Location,
                    member.Name));
                return new List<ParsableMemberInfo>();
            }
        }

        // Check ordering consistency
        bool anyHasOrder = members.Any(m => m.ExplicitOrder.HasValue);
        bool allHaveOrder = members.All(m => m.ExplicitOrder.HasValue);

        if (anyHasOrder && !allHaveOrder)
        {
            IEnumerable<string> missing = members
                .Where(m => !m.ExplicitOrder.HasValue)
                .Select(m => m.Name);

            ctx.ReportDiagnostic(Diagnostic.Create(
                ParseDiagnostics.InconsistentOrdering,
                typeInfo.Location,
                string.Join(", ", missing)));
            return new List<ParsableMemberInfo>();
        }

        // Check for duplicate orders
        if (allHaveOrder)
        {
            IEnumerable<IGrouping<int, ParsableMemberInfo>> duplicates = members
                .GroupBy(m => m.ExplicitOrder!.Value)
                .Where(g => g.Count() > 1);

            foreach (IGrouping<int, ParsableMemberInfo> dup in duplicates)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ParseDiagnostics.DuplicateOrder,
                    typeInfo.Location,
                    dup.Key,
                    string.Join(", ", dup.Select(m => m.Name))));
                return new List<ParsableMemberInfo>();
            }
        }

        // Order members first, then validate byte alignment
        List<ParsableMemberInfo> orderedMembers = allHaveOrder
            ? members.OrderBy(m => m.ExplicitOrder!.Value).ToList()
            : members.OrderBy(m => m.DeclaredOrder).ToList();

        // Validate LengthFromField references
        if (!ValidateLengthFromFieldReferences(ctx, orderedMembers))
        {
            return new List<ParsableMemberInfo>();
        }

        // Validate byte alignment for types that require it
        if (!ValidateByteAlignment(ctx, orderedMembers))
        {
            return new List<ParsableMemberInfo>();
        }

        return orderedMembers;
    }

    /// <summary>
    /// Validates that <c>[StringLengthFromField]</c> and <c>[BytesLengthFromField]</c> references are valid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A FromField reference is valid if:
    /// </para>
    /// <list type="bullet">
    ///   <item>The referenced field exists in the struct</item>
    ///   <item>The referenced field is parsed before the dependent field</item>
    /// </list>
    /// <para>
    /// This ensures the length value is available when parsing the dependent field.
    /// </para>
    /// </remarks>
    /// <param name="ctx">The source production context for diagnostics.</param>
    /// <param name="orderedMembers">Members in parsing order.</param>
    /// <returns><c>true</c> if all references are valid; otherwise <c>false</c>.</returns>
    private static bool ValidateLengthFromFieldReferences(SourceProductionContext ctx, List<ParsableMemberInfo> orderedMembers)
    {
        HashSet<string> parsedFieldNames = new();

        for (int i = 0; i < orderedMembers.Count; i++)
        {
            ParsableMemberInfo member = orderedMembers[i];

            if (member.LengthFromField is not null)
            {
                // Check if the referenced field exists
                ParsableMemberInfo? referencedField = orderedMembers.FirstOrDefault(m => m.Name == member.LengthFromField);
                if (referencedField is null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        ParseDiagnostics.LengthFieldNotFound,
                        member.Location,
                        member.Name,
                        member.LengthFromField));
                    return false;
                }

                // Check if the referenced field comes before this field
                if (!parsedFieldNames.Contains(member.LengthFromField))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        ParseDiagnostics.LengthFieldOrderError,
                        member.Location,
                        member.Name,
                        member.LengthFromField));
                    return false;
                }
            }

            parsedFieldNames.Add(member.Name);
        }

        return true;
    }

    /// <summary>
    /// Validates that types requiring byte alignment are properly aligned.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Some member types require byte alignment (bit offset % 8 == 0):
    /// </para>
    /// <list type="bullet">
    ///   <item>VarInt - Variable-length encoding starts at byte boundary</item>
    ///   <item>ByteArray/ByteArrayDynamic - Byte arrays start at byte boundary</item>
    ///   <item>NestedParsable - Nested types expect byte-aligned input</item>
    ///   <item>String - String encoding starts at byte boundary</item>
    /// </list>
    /// <para>
    /// When using bit fields, use <c>PaddingBits</c> to align to byte boundaries
    /// before these types.
    /// </para>
    /// </remarks>
    /// <param name="ctx">The source production context for diagnostics.</param>
    /// <param name="orderedMembers">Members in parsing order.</param>
    /// <returns><c>true</c> if alignment is valid; otherwise <c>false</c>.</returns>
    private static bool ValidateByteAlignment(SourceProductionContext ctx, List<ParsableMemberInfo> orderedMembers)
    {
        int bitOffset = 0;

        foreach (ParsableMemberInfo member in orderedMembers)
        {
            // Add padding bits before this member
            bitOffset += member.PaddingBits;

            // Check if this member type requires byte alignment
            bool requiresByteAlignment = member.Kind is
                ParsableMemberKind.VarInt or
                ParsableMemberKind.ByteArray or
                ParsableMemberKind.ByteArrayDynamic or
                ParsableMemberKind.MemoryDynamic or
                ParsableMemberKind.NestedParsable or
                ParsableMemberKind.String;

            if (requiresByteAlignment && (bitOffset % 8) != 0)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    ParseDiagnostics.RequiresByteAlignment,
                    member.Location,
                    member.Name,
                    member.FullTypeName,
                    bitOffset % 8));
                return false;
            }

            // Calculate bits consumed by this member
            int memberBits = member.Kind switch
            {
                ParsableMemberKind.BitField => member.BitCount ?? 0,
                ParsableMemberKind.Byte => 8,
                ParsableMemberKind.EndianWrapper => (GetEndianWrapperSize(member.TypeName) ?? 4) * 8,
                ParsableMemberKind.PrimitiveInteger => (GetPrimitiveSize(member.FullTypeName) ?? 4) * 8,
                // Variable-length types: we just track that they're byte-aligned
                ParsableMemberKind.VarInt or
                ParsableMemberKind.ByteArray or
                ParsableMemberKind.ByteArrayDynamic or
                ParsableMemberKind.MemoryDynamic or
                ParsableMemberKind.NestedParsable or
                ParsableMemberKind.String => 0, // Reset to 0, will be byte-aligned after
                _ => 0
            };

            bitOffset += memberBits;

            // Variable-length types reset alignment (they consume whole bytes)
            if (member.Kind is ParsableMemberKind.VarInt or
                ParsableMemberKind.ByteArray or
                ParsableMemberKind.ByteArrayDynamic or
                ParsableMemberKind.MemoryDynamic or
                ParsableMemberKind.NestedParsable or
                ParsableMemberKind.String)
            {
                bitOffset = 0; // Reset - these types handle their own byte consumption
            }
        }

        return true;
    }

    // ========================================================================
    // SIZE CALCULATION HELPERS
    // ========================================================================
    // These methods calculate fixed sizes for types when possible.
    // Fixed sizes enable early length validation in generated code.
    // ========================================================================

    private static int? CalculateFixedSize(List<ParsableMemberInfo> members) => BinaryGeneratorHelpers.CalculateFixedSize(members);

    // Size calculation helpers delegate to BinaryGeneratorHelpers
    private static int? GetEndianWrapperSize(string typeName) => BinaryGeneratorHelpers.GetEndianWrapperSize(typeName);
    private static int? GetPrimitiveSize(string fullTypeName) => BinaryGeneratorHelpers.GetPrimitiveSize(fullTypeName);

    private static int? CalculateFixedBits(List<ParsableMemberInfo> members) => BinaryGeneratorHelpers.CalculateFixedBits(members);

    // ========================================================================
    // MEMBER-SPECIFIC PARSING CODE GENERATION
    // ========================================================================
    // Each member kind has specialized parsing logic. The methods below
    // generate the appropriate code for each type category.
    //
    // When usesBitReader is true, all parsing goes through BitReader.
    // When false, direct span slicing is used for efficiency.
    // ========================================================================

    /// <summary>
    /// Generates parsing code for a single member.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The generated code follows this pattern for each member:
    /// </para>
    /// <list type="number">
    ///   <item>Inline comment documenting the member being parsed</item>
    ///   <item>Parsing logic specific to the member kind</item>
    ///   <item>Error handling (return false on failure)</item>
    ///   <item>Offset/position update</item>
    /// </list>
    /// </remarks>
    /// <param name="sb">The string builder to append code to.</param>
    /// <param name="member">The member to generate parsing for.</param>
    /// <param name="defaultEndianness">Default byte order when not specified.</param>
    /// <param name="usesBitReader">Whether bit-level parsing is used.</param>
    private static void GenerateMemberParsing(StringBuilder sb, ParsableMemberInfo member, Endianness defaultEndianness, bool usesBitReader)
    {
        string varName = $"_{member.Name}";

        sb.AppendLine($"        // Parse {member.Name} ({member.Kind}{(member.BitCount.HasValue ? $", {member.BitCount} bits" : "")})");

        // If we're using BitReader, all members use bit-level parsing
        if (usesBitReader)
        {
            GenerateBitReaderParsing(sb, member, varName, defaultEndianness);

            // Skip padding bits AFTER reading this field
            if (member.PaddingBits > 0)
            {
                sb.Append($$"""
                        // Skip {{member.PaddingBits}} padding bits after {{member.Name}}
                        reader.SkipBits({{member.PaddingBits}});
                """);
            }
            sb.AppendLine();
            return;
        }

        // Byte-aligned parsing (original logic)
        switch (member.Kind)
        {
            case ParsableMemberKind.Byte:
                sb.Append($$"""
                        byte {{varName}} = source[offset];
                        offset += 1;
                """);
                break;

            case ParsableMemberKind.ByteArray:
                int length = member.FixedLength!.Value;
                sb.Append($$"""
                        byte[] {{varName}} = source.Slice(offset, {{length}}).ToArray();
                        offset += {{length}};
                """);
                break;

            case ParsableMemberKind.EndianWrapper:
                GenerateEndianWrapperParsing(sb, member, varName);
                break;

            case ParsableMemberKind.VarInt:
                GenerateVarIntParsing(sb, member, varName);
                break;

            case ParsableMemberKind.NestedParsable:
                sb.Append($$"""
                        if (!{{member.FullTypeName}}.TryParse(source.Slice(offset), out {{member.FullTypeName}} {{varName}}, out int {{varName}}Consumed))
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        offset += {{varName}}Consumed;
                """);
                break;

            case ParsableMemberKind.PrimitiveInteger:
                GeneratePrimitiveIntegerParsing(sb, member, varName, member.ExplicitEndianness ?? defaultEndianness);
                break;

            case ParsableMemberKind.String:
                GenerateStringParsing(sb, member, varName);
                break;

            case ParsableMemberKind.ByteArrayDynamic:
                GenerateByteArrayDynamicParsing(sb, member, varName);
                break;

            case ParsableMemberKind.MemoryDynamic:
                GenerateMemoryDynamicParsing(sb, member, varName);
                break;
        }

        // Skip padding bits/bytes AFTER reading this field (byte-aligned mode)
        if (member.PaddingBits > 0)
        {
            // In byte-aligned mode, padding should be in multiples of 8
            int paddingBytes = member.PaddingBits / 8;
            if (paddingBytes > 0)
            {
                sb.Append($$"""
                        // Skip {{member.PaddingBits}} padding bits ({{paddingBytes}} bytes) after {{member.Name}}
                        offset += {{paddingBytes}};
                """);
            }
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Generates string parsing code based on the length encoding configuration.
    /// </summary>
    /// <remarks>
    /// <para>Handles various string length encodings:</para>
    /// <list type="bullet">
    ///   <item><b>VarInt:</b> Read VarInt prefix, then UTF-8 bytes</item>
    ///   <item><b>FixedBE/FixedLE:</b> Read 1/2/4 byte length, then UTF-8</item>
    ///   <item><b>NullTerminated:</b> Read until 0x00 byte</item>
    ///   <item><b>Fixed:</b> Read fixed byte count, trim nulls</item>
    ///   <item><b>FromField:</b> Use previously parsed field as length</item>
    /// </list>
    /// </remarks>
    /// <param name="sb">The string builder to append code to.</param>
    /// <param name="member">The string member to parse.</param>
    /// <param name="varName">Variable name for the parsed string.</param>
    private static void GenerateStringParsing(StringBuilder sb, ParsableMemberInfo member, string varName)
    {
        StringLengthEncodingInfo encoding = member.StringEncoding!.Value;

        switch (encoding.Encoding)
        {
            case StringLengthEncodingKind.VarInt:
                sb.Append($$"""
                        if (!ZeroAlloc.VarInt.TryParse(source.Slice(offset), out ZeroAlloc.VarInt {{varName}}Length, out int {{varName}}LengthBytes))
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        offset += {{varName}}LengthBytes;
                        int {{varName}}ByteLen = (int){{varName}}Length.Value;
                        if (source.Length - offset < {{varName}}ByteLen)
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        string {{varName}} = System.Text.Encoding.UTF8.GetString(source.Slice(offset, {{varName}}ByteLen));
                        offset += {{varName}}ByteLen;
                """);
                break;

            case StringLengthEncodingKind.FixedBE:
            case StringLengthEncodingKind.FixedLE:
                bool isBE = encoding.Encoding == StringLengthEncodingKind.FixedBE;
                string endianSuffix = isBE ? "BigEndian" : "LittleEndian";

                // Guard against reading the length-prefix bytes when the buffer is too short
                sb.Append($$"""
                        if (source.Length - offset < {{encoding.LengthBytes}})
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                """);
                switch (encoding.LengthBytes)
                {
                    case 1:
                        sb.Append($$"""
                                int {{varName}}ByteLen = source[offset];
                                offset += 1;
                        """);
                        break;
                    case 2:
                        sb.Append($$"""
                                int {{varName}}ByteLen = BinaryPrimitives.ReadUInt16{{endianSuffix}}(source.Slice(offset));
                                offset += 2;
                        """);
                        break;
                    case 4:
                    default:
                        sb.Append($$"""
                                int {{varName}}ByteLen = (int)BinaryPrimitives.ReadUInt32{{endianSuffix}}(source.Slice(offset));
                                offset += 4;
                        """);
                        break;
                }
                sb.Append($$"""
                        if (source.Length - offset < {{varName}}ByteLen)
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        string {{varName}} = System.Text.Encoding.UTF8.GetString(source.Slice(offset, {{varName}}ByteLen));
                        offset += {{varName}}ByteLen;
                """);
                break;

            case StringLengthEncodingKind.NullTerminated:
                sb.Append($$"""
                        int {{varName}}End = source.Slice(offset).IndexOf((byte)0);
                        if ({{varName}}End < 0)
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        string {{varName}} = System.Text.Encoding.UTF8.GetString(source.Slice(offset, {{varName}}End));
                        offset += {{varName}}End + 1; // +1 for null terminator
                """);
                break;

            case StringLengthEncodingKind.Fixed:
                int fixedLen = encoding.FixedLength > 0 ? encoding.FixedLength : 0;
                sb.Append($$"""
                        if (source.Length - offset < {{fixedLen}})
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        ReadOnlySpan<byte> {{varName}}Bytes = source.Slice(offset, {{fixedLen}});
                        int {{varName}}ActualLen = {{varName}}Bytes.IndexOf((byte)0);
                        if ({{varName}}ActualLen < 0) {{varName}}ActualLen = {{fixedLen}};
                        string {{varName}} = System.Text.Encoding.UTF8.GetString({{varName}}Bytes.Slice(0, {{varName}}ActualLen));
                        offset += {{fixedLen}};
                """);
                break;

            case StringLengthEncodingKind.FromField:
                string lengthField = member.LengthFromField ?? "0";
                // Handle endian wrapper types (.Value) and primitive types
                sb.Append($$"""
                        int {{varName}}ByteLen = GetLengthFromField(_{{lengthField}});
                        if (source.Length - offset < {{varName}}ByteLen)
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        string {{varName}} = System.Text.Encoding.UTF8.GetString(source.Slice(offset, {{varName}}ByteLen));
                        offset += {{varName}}ByteLen;
                """);
                break;
        }
    }

    /// <summary>
    /// Generates byte array parsing code for dynamic-length byte arrays.
    /// </summary>
    /// <remarks>
    /// <para>Similar to string parsing, but outputs byte[] instead of string:</para>
    /// <list type="bullet">
    ///   <item><b>VarInt:</b> Read VarInt prefix, then bytes</item>
    ///   <item><b>FixedBE/FixedLE:</b> Read 1/2/4 byte length, then bytes</item>
    ///   <item><b>FromField:</b> Use previously parsed field as length</item>
    /// </list>
    /// </remarks>
    /// <param name="sb">The string builder to append code to.</param>
    /// <param name="member">The byte[] member to parse.</param>
    /// <param name="varName">Variable name for the parsed array.</param>
    private static void GenerateByteArrayDynamicParsing(StringBuilder sb, ParsableMemberInfo member, string varName)
    {
        BytesLengthEncodingInfo encoding = member.BytesEncoding!.Value;

        switch (encoding.Encoding)
        {
            case BytesLengthEncodingKind.VarInt:
                sb.Append($$"""
                        if (!ZeroAlloc.VarInt.TryParse(source.Slice(offset), out ZeroAlloc.VarInt {{varName}}Length, out int {{varName}}LengthBytes))
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        offset += {{varName}}LengthBytes;
                        int {{varName}}ByteLen = (int){{varName}}Length.Value;
                        if (source.Length - offset < {{varName}}ByteLen)
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        byte[] {{varName}} = source.Slice(offset, {{varName}}ByteLen).ToArray();
                        offset += {{varName}}ByteLen;
                """);
                break;

            case BytesLengthEncodingKind.FixedBE:
            case BytesLengthEncodingKind.FixedLE:
                bool isBE = encoding.Encoding == BytesLengthEncodingKind.FixedBE;
                string endianSuffix = isBE ? "BigEndian" : "LittleEndian";

                // Guard against reading the length-prefix bytes when the buffer is too short
                sb.Append($$"""
                        if (source.Length - offset < {{encoding.LengthBytes}})
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                """);
                switch (encoding.LengthBytes)
                {
                    case 1:
                        sb.Append($$"""
                                int {{varName}}ByteLen = source[offset];
                                offset += 1;
                        """);
                        break;
                    case 2:
                        sb.Append($$"""
                                int {{varName}}ByteLen = BinaryPrimitives.ReadUInt16{{endianSuffix}}(source.Slice(offset));
                                offset += 2;
                        """);
                        break;
                    case 4:
                    default:
                        sb.Append($$"""
                                int {{varName}}ByteLen = (int)BinaryPrimitives.ReadUInt32{{endianSuffix}}(source.Slice(offset));
                                offset += 4;
                        """);
                        break;
                }
                sb.Append($$"""
                        if (source.Length - offset < {{varName}}ByteLen)
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        byte[] {{varName}} = source.Slice(offset, {{varName}}ByteLen).ToArray();
                        offset += {{varName}}ByteLen;
                """);
                break;

            case BytesLengthEncodingKind.FromField:
                string lengthField = member.LengthFromField ?? "0";
                sb.Append($$"""
                        int {{varName}}ByteLen = GetLengthFromField(_{{lengthField}});
                        if (source.Length - offset < {{varName}}ByteLen)
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        byte[] {{varName}} = source.Slice(offset, {{varName}}ByteLen).ToArray();
                        offset += {{varName}}ByteLen;
                """);
                break;
        }
    }

    /// <summary>
    /// Generates parsing code for <c>Memory&lt;byte&gt;</c> or <c>ReadOnlyMemory&lt;byte&gt;</c>.
    /// </summary>
    /// <remarks>
    /// Similar to byte array parsing, but produces Memory&lt;byte&gt; instead of byte[].
    /// The byte array is allocated and wrapped in a Memory structure.
    /// </remarks>
    /// <param name="sb">The string builder to append code to.</param>
    /// <param name="member">The Memory member to parse.</param>
    /// <param name="varName">Variable name for the parsed Memory.</param>
    private static void GenerateMemoryDynamicParsing(StringBuilder sb, ParsableMemberInfo member, string varName)
    {
        BytesLengthEncodingInfo encoding = member.BytesEncoding!.Value;
        bool isReadOnly = member.FullTypeName.Contains("ReadOnlyMemory");
        string memoryType = isReadOnly ? "ReadOnlyMemory<byte>" : "Memory<byte>";

        switch (encoding.Encoding)
        {
            case BytesLengthEncodingKind.VarInt:
                sb.Append($$"""
                        if (!ZeroAlloc.VarInt.TryParse(source.Slice(offset), out ZeroAlloc.VarInt {{varName}}Length, out int {{varName}}LengthBytes))
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        offset += {{varName}}LengthBytes;
                        int {{varName}}ByteLen = (int){{varName}}Length.Value;
                        if (source.Length - offset < {{varName}}ByteLen)
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        {{memoryType}} {{varName}} = source.Slice(offset, {{varName}}ByteLen).ToArray();
                        offset += {{varName}}ByteLen;
                """);
                break;

            case BytesLengthEncodingKind.FixedBE:
            case BytesLengthEncodingKind.FixedLE:
                bool isBE = encoding.Encoding == BytesLengthEncodingKind.FixedBE;
                string endianSuffix = isBE ? "BigEndian" : "LittleEndian";

                // Guard against reading the length-prefix bytes when the buffer is too short
                sb.Append($$"""
                        if (source.Length - offset < {{encoding.LengthBytes}})
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                """);
                switch (encoding.LengthBytes)
                {
                    case 1:
                        sb.Append($$"""
                                int {{varName}}ByteLen = source[offset];
                                offset += 1;
                        """);
                        break;
                    case 2:
                        sb.Append($$"""
                                int {{varName}}ByteLen = BinaryPrimitives.ReadUInt16{{endianSuffix}}(source.Slice(offset));
                                offset += 2;
                        """);
                        break;
                    case 4:
                    default:
                        sb.Append($$"""
                                int {{varName}}ByteLen = (int)BinaryPrimitives.ReadUInt32{{endianSuffix}}(source.Slice(offset));
                                offset += 4;
                        """);
                        break;
                }
                sb.Append($$"""
                        if (source.Length - offset < {{varName}}ByteLen)
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        {{memoryType}} {{varName}} = source.Slice(offset, {{varName}}ByteLen).ToArray();
                        offset += {{varName}}ByteLen;
                """);
                break;

            case BytesLengthEncodingKind.FromField:
                string lengthField = member.LengthFromField ?? "0";
                sb.Append($$"""
                        int {{varName}}ByteLen = GetLengthFromField(_{{lengthField}});
                        if (source.Length - offset < {{varName}}ByteLen)
                        {
                            value = default;
                            bytesConsumed = 0;
                            return false;
                        }
                        {{memoryType}} {{varName}} = source.Slice(offset, {{varName}}ByteLen).ToArray();
                        offset += {{varName}}ByteLen;
                """);
                break;
        }
    }

    // ========================================================================
    // BIT-LEVEL PARSING
    // ========================================================================
    // When any member uses [BinaryField(BitCount = n)], the generated code
    // uses BitReader for all parsing. BitReader tracks the current bit offset
    // and provides methods for reading arbitrary bit counts.
    // ========================================================================

    /// <summary>
    /// Generates BitReader-based parsing for a single member.
    /// </summary>
    /// <remarks>
    /// <para>
    /// BitReader enables sub-byte parsing for protocols with bit-packed fields.
    /// Each member reads its bits and advances the bit offset accordingly.
    /// </para>
    /// <para>
    /// For members without explicit BitCount, the default size is used
    /// (e.g., 8 bits for byte, 32 bits for U32BE, etc.).
    /// </para>
    /// </remarks>
    /// <param name="sb">The string builder to append code to.</param>
    /// <param name="member">The member to generate parsing for.</param>
    /// <param name="varName">Variable name for the parsed value.</param>
    /// <param name="defaultEndianness">Default byte order for multi-byte reads.</param>
    private static void GenerateBitReaderParsing(StringBuilder sb, ParsableMemberInfo member, string varName, Endianness defaultEndianness)
    {
        int bitCount = member.BitCount ?? GetDefaultBitCount(member);

        // Determine the target type for the cast
        string targetType = member.FullTypeName;

        switch (member.Kind)
        {
            case ParsableMemberKind.BitField:
                // Bit field - read exact number of bits using ReadBits(int) which returns ulong
                if (bitCount == 1)
                {
                    // Special case for single bit -> bool or byte
                    if (member.FullTypeName is "bool" or "System.Boolean")
                    {
                        sb.AppendLine($"        bool {varName} = reader.ReadBits(1) != 0;");
                    }
                    else
                    {
                        sb.AppendLine($"        {targetType} {varName} = ({targetType})reader.ReadBits(1);");
                    }
                }
                else if (bitCount <= 64)
                {
                    sb.AppendLine($"        {targetType} {varName} = ({targetType})reader.ReadBits({bitCount});");
                }
                else
                {
                    // For > 64 bits, not directly supported
                    sb.Append($$"""
                            // WARNING: {{bitCount}} bits exceeds 64-bit limit
                            {{targetType}} {{varName}} = default;
                    """);
                }
                break;

            case ParsableMemberKind.Byte:
                sb.AppendLine($"        byte {varName} = reader.ReadByte();");
                break;

            case ParsableMemberKind.EndianWrapper:
                GenerateBitReaderEndianWrapperParsing(sb, member, varName);
                break;

            case ParsableMemberKind.PrimitiveInteger:
                GenerateBitReaderPrimitiveIntegerParsing(sb, member, varName);
                break;

            default:
                // Unsupported in bit mode
                sb.Append($$"""
                        // WARNING: {{member.Kind}} not supported in bit-level parsing mode
                        {{targetType}} {{varName}} = default;
                """);
                break;
        }
    }

    private static int GetDefaultBitCount(ParsableMemberInfo member) => BinaryGeneratorHelpers.GetDefaultBitCount(member);

    private static void GenerateBitReaderEndianWrapperParsing(StringBuilder sb, ParsableMemberInfo member, string varName)
    {
        string typeName = member.TypeName;

        // BitReader operates MSB-first; byte-order distinctions (LE vs BE) are irrelevant inside a
        // bit-level stream. Both variants use the same big-endian-oriented read path. Signed types
        // require an explicit cast because the wrapper constructors accept signed parameters.
        string readMethod = typeName switch
        {
            "U16BE" or "U16LE" => "reader.ReadUInt16()",
            "I16BE" or "I16LE" => "(short)reader.ReadUInt16()",
            "U32BE" or "U32LE" => "reader.ReadUInt32()",
            "I32BE" or "I32LE" => "(int)reader.ReadUInt32()",
            "U64BE" or "U64LE" => "reader.ReadUInt64()",
            "I64BE" or "I64LE" => "(long)reader.ReadUInt64()",
            "F32BE" or "F32LE" => "BitConverter.Int32BitsToSingle((int)reader.ReadUInt32())",
            "F64BE" or "F64LE" => "BitConverter.Int64BitsToDouble((long)reader.ReadUInt64())",
            "U128BE" or "U128LE" => "reader.ReadUInt128()",
            "I128BE" or "I128LE" => "reader.ReadInt128()",
            _ => "0" // Unreachable: all IsEndianWrapper types are handled above.
        };

        sb.AppendLine($"        {member.FullTypeName} {varName} = new {member.FullTypeName}({readMethod});");
    }

    private static void GenerateBitReaderPrimitiveIntegerParsing(StringBuilder sb, ParsableMemberInfo member, string varName)
    {
        string readMethod = member.FullTypeName switch
        {
            "sbyte" or "System.SByte" => "(sbyte)reader.ReadByte()",
            "byte" or "System.Byte" => "reader.ReadByte()",
            "short" or "System.Int16" => "(short)reader.ReadUInt16()",
            "ushort" or "System.UInt16" => "reader.ReadUInt16()",
            "int" or "System.Int32" => "(int)reader.ReadUInt32()",
            "uint" or "System.UInt32" => "reader.ReadUInt32()",
            "long" or "System.Int64" => "(long)reader.ReadUInt64()",
            "ulong" or "System.UInt64" => "reader.ReadUInt64()",
            "nint" or "System.IntPtr" => "(nint)reader.ReadUInt64()",
            "nuint" or "System.UIntPtr" => "(nuint)reader.ReadUInt64()",
            "float" or "System.Single" => "BitConverter.Int32BitsToSingle((int)reader.ReadUInt32())",
            "double" or "System.Double" => "BitConverter.Int64BitsToDouble((long)reader.ReadUInt64())",
            "System.Half" => "BitConverter.Int16BitsToHalf((short)reader.ReadUInt16())",
            "System.UInt128" => "reader.ReadUInt128()",
            "System.Int128" => "reader.ReadInt128()",
            _ => "0" // Unreachable: all types recognised by IsPrimitiveInteger are handled above.
        };

        sb.AppendLine($"        {member.FullTypeName} {varName} = {readMethod};");
    }

    private static void GenerateEndianWrapperParsing(StringBuilder sb, ParsableMemberInfo member, string varName)
    {
        string typeName = member.TypeName;
        int size = GetEndianWrapperSize(typeName) ?? 4;
        bool isBigEndian = typeName.EndsWith("BE");

        string primitiveType = typeName switch
        {
            "U16BE" or "U16LE" => "ushort",
            "U32BE" or "U32LE" => "uint",
            "U64BE" or "U64LE" => "ulong",
            "U128BE" or "U128LE" => "UInt128",
            "I16BE" or "I16LE" => "short",
            "I32BE" or "I32LE" => "int",
            "I64BE" or "I64LE" => "long",
            "I128BE" or "I128LE" => "Int128",
            "F32BE" or "F32LE" => "float",
            "F64BE" or "F64LE" => "double",
            _ => "uint"
        };

        string readMethod = (typeName, isBigEndian) switch
        {
            ("U16BE", _) or ("U16LE", _) => $"BinaryPrimitives.ReadUInt16{(isBigEndian ? "BigEndian" : "LittleEndian")}(source.Slice(offset))",
            ("U32BE", _) or ("U32LE", _) => $"BinaryPrimitives.ReadUInt32{(isBigEndian ? "BigEndian" : "LittleEndian")}(source.Slice(offset))",
            ("U64BE", _) or ("U64LE", _) => $"BinaryPrimitives.ReadUInt64{(isBigEndian ? "BigEndian" : "LittleEndian")}(source.Slice(offset))",
            ("U128BE", _) or ("U128LE", _) => $"BinaryPrimitives.ReadUInt128{(isBigEndian ? "BigEndian" : "LittleEndian")}(source.Slice(offset))",
            ("I16BE", _) or ("I16LE", _) => $"BinaryPrimitives.ReadInt16{(isBigEndian ? "BigEndian" : "LittleEndian")}(source.Slice(offset))",
            ("I32BE", _) or ("I32LE", _) => $"BinaryPrimitives.ReadInt32{(isBigEndian ? "BigEndian" : "LittleEndian")}(source.Slice(offset))",
            ("I64BE", _) or ("I64LE", _) => $"BinaryPrimitives.ReadInt64{(isBigEndian ? "BigEndian" : "LittleEndian")}(source.Slice(offset))",
            ("I128BE", _) or ("I128LE", _) => $"BinaryPrimitives.ReadInt128{(isBigEndian ? "BigEndian" : "LittleEndian")}(source.Slice(offset))",
            ("F32BE", _) or ("F32LE", _) => $"BinaryPrimitives.ReadSingle{(isBigEndian ? "BigEndian" : "LittleEndian")}(source.Slice(offset))",
            ("F64BE", _) or ("F64LE", _) => $"BinaryPrimitives.ReadDouble{(isBigEndian ? "BigEndian" : "LittleEndian")}(source.Slice(offset))",
            _ => "0"
        };

        sb.Append($$"""
                {{member.FullTypeName}} {{varName}} = new {{member.FullTypeName}}({{readMethod}});
                offset += {{size}};
        """);
    }

    private static void GenerateVarIntParsing(StringBuilder sb, ParsableMemberInfo member, string varName)
    {
        if (member.TypeName == "VarInt")
        {
            sb.Append($$"""
                    if (!ZeroAlloc.VarInt.TryParse(source.Slice(offset), out ZeroAlloc.VarInt {{varName}}, out int {{varName}}Consumed))
                    {
                        value = default;
                        bytesConsumed = 0;
                        return false;
                    }
                    offset += {{varName}}Consumed;
            """);
        }
        else // VarIntZigZag
        {
            sb.Append($$"""
                    if (!ZeroAlloc.VarIntZigZag.TryParse(source.Slice(offset), out ZeroAlloc.VarIntZigZag {{varName}}, out int {{varName}}Consumed))
                    {
                        value = default;
                        bytesConsumed = 0;
                        return false;
                    }
                    offset += {{varName}}Consumed;
            """);
        }
    }

    private static void GeneratePrimitiveIntegerParsing(StringBuilder sb, ParsableMemberInfo member, string varName, Endianness endianness)
    {
        string endianSuffix = endianness == Endianness.BigEndian ? "BigEndian" : "LittleEndian";

        (string readMethod, int size) = member.FullTypeName switch
        {
            // 1-byte types (no endianness needed)
            "sbyte" or "System.SByte" => ("(sbyte)source[offset]", 1),

            // 2-byte types
            "short" or "System.Int16" => ($"BinaryPrimitives.ReadInt16{endianSuffix}(source.Slice(offset))", 2),
            "ushort" or "System.UInt16" => ($"BinaryPrimitives.ReadUInt16{endianSuffix}(source.Slice(offset))", 2),
            "System.Half" => ($"BinaryPrimitives.ReadHalf{endianSuffix}(source.Slice(offset))", 2),

            // 4-byte types
            "int" or "System.Int32" => ($"BinaryPrimitives.ReadInt32{endianSuffix}(source.Slice(offset))", 4),
            "uint" or "System.UInt32" => ($"BinaryPrimitives.ReadUInt32{endianSuffix}(source.Slice(offset))", 4),
            "float" or "System.Single" => ($"BinaryPrimitives.ReadSingle{endianSuffix}(source.Slice(offset))", 4),

            // 8-byte types
            "long" or "System.Int64" => ($"BinaryPrimitives.ReadInt64{endianSuffix}(source.Slice(offset))", 8),
            "ulong" or "System.UInt64" => ($"BinaryPrimitives.ReadUInt64{endianSuffix}(source.Slice(offset))", 8),
            "double" or "System.Double" => ($"BinaryPrimitives.ReadDouble{endianSuffix}(source.Slice(offset))", 8),
            "nint" or "System.IntPtr" => ($"(nint)BinaryPrimitives.ReadInt64{endianSuffix}(source.Slice(offset))", 8),
            "nuint" or "System.UIntPtr" => ($"(nuint)BinaryPrimitives.ReadUInt64{endianSuffix}(source.Slice(offset))", 8),

            // 16-byte types
            "System.Int128" => ($"BinaryPrimitives.ReadInt128{endianSuffix}(source.Slice(offset))", 16),
            "System.UInt128" => ($"BinaryPrimitives.ReadUInt128{endianSuffix}(source.Slice(offset))", 16),

            _ => ("0", 0)
        };

        sb.Append($$"""
                {{member.FullTypeName}} {{varName}} = {{readMethod}};
                offset += {{size}};
        """);
    }
}
