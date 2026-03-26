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
// ZeroAlloc Binary Writable Generator
// ============================================================================
// This Roslyn Incremental Source Generator analyzes structs marked with
// [BinaryWritable] and generates IBinarySerializable implementations for
// zero-allocation binary serialization.
//
// This is the write counterpart to BinaryParsableGenerator.
//
// ARCHITECTURE OVERVIEW:
// ----------------------
// 1. SYNTAX ANALYSIS PHASE
//    - Reuses ParsableTypeInfo / ParsableMemberInfo from BinaryParsableGenerator
//    - GetWritableTypeInfo: Semantic analysis to extract member information
//
// 2. MEMBER ANALYSIS PHASE
//    - Reuses AnalyzeMemberKind from BinaryParsableGenerator
//    - Same validation (ordering, alignment, etc.)
//
// 3. CODE GENERATION PHASE
//    - GenerateWritingCode: Generates partial struct implementing IBinarySerializable
//    - Generates TryWrite, TryGetSerializedSize, and WriteTo methods
//
// DIAGNOSTICS:
// - ZA3001: Member type not writable
// - ZA3002: Inconsistent ordering
// - ZA3003: Duplicate order value
// - ZA3004: byte[] without length specification
// - ZA3005: Only structs allowed
// - ZA3006: Type must be partial
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
// DIAGNOSTIC DESCRIPTORS FOR WRITING
// ============================================================================

internal static class WriteDiagnostics
{
    /// <summary>
    /// ZA3001: Member type is not writable.
    /// </summary>
    public static readonly DiagnosticDescriptor MemberNotWritable = new(
        id: "ZA3001",
        title: "Member type is not writable",
        messageFormat: "Member '{0}' of type '{1}' cannot be serialized. Use an endian wrapper (U16BE, etc.), VarInt, or an IBinarySerializable type.",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3002: Inconsistent ordering.
    /// </summary>
    public static readonly DiagnosticDescriptor InconsistentOrdering = new(
        id: "ZA3002",
        title: "Inconsistent member ordering",
        messageFormat: "If any member uses [BinaryOrder], all non-ignored members must specify their order. Missing: {0}.",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3003: Duplicate order value.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateOrder = new(
        id: "ZA3003",
        title: "Duplicate order value",
        messageFormat: "Order value {0} is used by multiple members: {1}",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3004: byte[] without length specification.
    /// </summary>
    public static readonly DiagnosticDescriptor ByteArrayWithoutLength = new(
        id: "ZA3004",
        title: "byte[] requires length specification",
        messageFormat: "Member '{0}' is a byte[] but has no [BinaryFixedLength] or length encoding attribute. The serializer needs to know how many bytes to write.",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3005: Only structs allowed.
    /// </summary>
    public static readonly DiagnosticDescriptor OnlyStructsAllowed = new(
        id: "ZA3005",
        title: "[BinaryWritable] only allowed on structs",
        messageFormat: "Type '{0}' is marked with [BinaryWritable] but is not a struct. Only structs and record structs are supported.",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3006: Type must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor MustBePartial = new(
        id: "ZA3006",
        title: "Type must be partial",
        messageFormat: "Type '{0}' is marked with [BinaryWritable] but is not declared as partial. Add the 'partial' modifier.",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3007: [BinaryOrder] and [BinaryIgnore] conflict.
    /// </summary>
    public static readonly DiagnosticDescriptor OrderAndIgnoreConflict = new(
        id: "ZA3007",
        title: "[BinaryOrder] and [BinaryIgnore] conflict",
        messageFormat: "Member '{0}' has both [BinaryOrder] and [BinaryIgnore]. Ignored members cannot have an order.",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3008: Byte alignment required.
    /// </summary>
    public static readonly DiagnosticDescriptor RequiresByteAlignment = new(
        id: "ZA3008",
        title: "Type requires byte alignment",
        messageFormat: "Member '{0}' of type '{1}' requires byte alignment, but current bit offset is {2}. Add padding bits (total must be multiple of 8).",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3009: String requires length encoding.
    /// </summary>
    public static readonly DiagnosticDescriptor StringWithoutLengthEncoding = new(
        id: "ZA3009",
        title: "String requires length encoding",
        messageFormat: "Member '{0}' is a string but has no length encoding. Use [StringLengthVarInt], [StringLengthBE], [StringLengthLE], [StringNullTerminated], [StringFixedLength], or [StringLengthFromField].",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3010: Invalid padding bits.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidPaddingBits = new(
        id: "ZA3010",
        title: "Invalid padding bits value",
        messageFormat: "PaddingBits value {0} is invalid. Must be between 1 and 64.",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3011: Bytes/Memory requires length encoding.
    /// </summary>
    public static readonly DiagnosticDescriptor BytesWithoutLengthEncoding = new(
        id: "ZA3011",
        title: "Bytes/Memory requires length encoding",
        messageFormat: "Member '{0}' is a byte[]/Memory<byte> but has no length encoding. Use [BinaryFixedLength], [BytesLengthVarInt], [BytesLengthBE], [BytesLengthLE], or [BytesLengthFromField].",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3012: [BinaryFixedLength] or [StringFixedLength] value must be greater than zero.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidFixedLength = new(
        id: "ZA3012",
        title: "Fixed length must be greater than zero",
        messageFormat: "Member '{0}' has a fixed length of {1}, which is invalid. The length must be greater than zero.",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA3013: Multiple conflicting length/encoding attributes on a single member.
    /// </summary>
    public static readonly DiagnosticDescriptor ConflictingEncodingAttributes = new(
        id: "ZA3013",
        title: "Conflicting length/encoding attributes",
        messageFormat: "Member '{0}' has multiple conflicting length/encoding attributes. Only one length encoding attribute is allowed per member.",
        category: "ZeroAlloc.Writing",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}

// ============================================================================
// GENERATOR
// ============================================================================

/// <summary>
/// Roslyn incremental source generator for <c>[BinaryWritable]</c> types.
/// </summary>
/// <remarks>
/// <para>
/// This generator implements <c>IBinarySerializable</c> for structs marked with
/// <c>[BinaryWritable]</c>. The generated code handles:
/// </para>
/// <list type="bullet">
///   <item>Endian conversion for multi-byte integers</item>
///   <item>Variable-length integer encoding (VarInt)</item>
///   <item>String serialization with various length encodings</item>
///   <item>Nested IBinarySerializable types</item>
///   <item>Bit fields for sub-byte writing</item>
/// </list>
/// </remarks>
[Generator]
public sealed class BinaryWritableGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Initializes the incremental generator pipeline.
    /// </summary>
    /// <param name="context">The initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with [BinaryWritable] attribute
        IncrementalValuesProvider<ParsableTypeInfo?> writableTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                fullyQualifiedMetadataName: "ZeroAlloc.BinaryWritableAttribute",
                predicate: static (node, _) => node is StructDeclarationSyntax or RecordDeclarationSyntax,
                transform: static (ctx, _) => GetWritableTypeInfo(ctx))
            .Where(static info => info is not null);

        // Generate writing code for each [BinaryWritable] type
        context.RegisterSourceOutput(writableTypes, GenerateWritingCode);
    }

    /// <summary>
    /// Extracts type and member information from a <c>[BinaryWritable]</c> struct.
    /// Reuses <see cref="ParsableTypeInfo"/> and <see cref="ParsableMemberInfo"/> data types
    /// from <see cref="BinaryParsableGenerator"/> since the member analysis is identical.
    /// </summary>
    private static ParsableTypeInfo? GetWritableTypeInfo(GeneratorAttributeSyntaxContext ctx)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        if (!typeSymbol.IsValueType || typeSymbol.TypeKind != TypeKind.Struct)
        {
            return null;
        }

        SyntaxNode targetNode = ctx.TargetNode;
        bool isPartial = targetNode switch
        {
            StructDeclarationSyntax s => s.Modifiers.Any(SyntaxKind.PartialKeyword),
            RecordDeclarationSyntax r => r.Modifiers.Any(SyntaxKind.PartialKeyword),
            _ => false
        };

        if (!isPartial)
        {
            return null;
        }

        // Get attribute data
        AttributeData? binaryWritableAttr = ctx.Attributes.FirstOrDefault();
        Endianness defaultEndianness = Endianness.BigEndian;
        if (binaryWritableAttr is not null)
        {
            foreach (KeyValuePair<string, TypedConstant> arg in binaryWritableAttr.NamedArguments)
            {
                if (arg.Key == "DefaultEndianness" && arg.Value.Value is int endianValue)
                {
                    defaultEndianness = (Endianness)endianValue;
                }
            }
        }

        string typeName = typeSymbol.Name;
        string? ns = typeSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : typeSymbol.ContainingNamespace?.ToDisplayString();
        string fullTypeName = typeSymbol.ToDisplayString();
        bool isReadOnly = typeSymbol.IsReadOnly;
        bool isRecordStruct = targetNode is RecordDeclarationSyntax;

        List<ParsableMemberInfo> members = new();
        int declaredOrder = 0;

        foreach (ISymbol member in typeSymbol.GetMembers())
        {
            if (member.IsStatic)
            {
                continue;
            }

            ITypeSymbol? memberType = null;
            bool isProperty = false;
            Location memberLocation = member.Locations.FirstOrDefault() ?? Location.None;

            if (member is IFieldSymbol field)
            {
                if (field.IsImplicitlyDeclared || field.Name.StartsWith("<"))
                {
                    continue;
                }
                memberType = field.Type;
                isProperty = false;
            }
            else if (member is IPropertySymbol prop)
            {
                if (prop.IsIndexer)
                {
                    continue;
                }
                // For writing, we need readable properties (getter)
                if (prop.GetMethod is null)
                {
                    continue;
                }
                // Skip computed properties without setter/init (unless they have a getter and are not auto-properties)
                if (prop.SetMethod is null && !prop.IsRequired)
                {
                    continue;
                }
                memberType = prop.Type;
                isProperty = true;
            }
            else
            {
                continue;
            }

            bool isIgnored = member.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "BinaryIgnoreAttribute" ||
                a.AttributeClass?.ToDisplayString() == "ZeroAlloc.BinaryIgnoreAttribute");

            int? explicitOrder = null;
            AttributeData? orderAttr = member.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.Name == "BinaryOrderAttribute" ||
                a.AttributeClass?.ToDisplayString() == "ZeroAlloc.BinaryOrderAttribute");
            if (orderAttr is not null && orderAttr.ConstructorArguments.Length > 0)
            {
                explicitOrder = orderAttr.ConstructorArguments[0].Value as int?;
            }

            if (isIgnored && explicitOrder.HasValue)
            {
                members.Add(new ParsableMemberInfo(
                    member.Name, memberType.Name, memberType.ToDisplayString(),
                    explicitOrder, declaredOrder++, isProperty,
                    ParsableMemberKind.Unknown, null, null, 0, null, null, null, null,
                    memberLocation, 0, 0)
                { IsIgnoredWithOrder = true });
                continue;
            }

            if (isIgnored)
            {
                continue;
            }

            int? fixedLength = null;
            AttributeData? lengthAttr = member.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.Name == "BinaryFixedLengthAttribute" ||
                a.AttributeClass?.ToDisplayString() == "ZeroAlloc.BinaryFixedLengthAttribute");
            if (lengthAttr is not null && lengthAttr.ConstructorArguments.Length > 0)
            {
                fixedLength = lengthAttr.ConstructorArguments[0].Value as int?;
            }

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

            // Get string length encoding from attributes
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
                                (StringLengthEncodingKind)encodingValue, legacyLengthBytes, legacyFixedLength);
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
                }
            }

            string? combinedLengthFromField = lengthFromField ?? bytesLengthFromField;

            ParsableMemberKind kind = bitCount.HasValue && bitCount.Value > 0
                ? ParsableMemberKind.BitField
                : AnalyzeMemberKind(memberType, stringEncoding.HasValue, bytesEncoding.HasValue);

            members.Add(new ParsableMemberInfo(
                member.Name, memberType.Name, memberType.ToDisplayString(),
                explicitOrder, declaredOrder++, isProperty, kind,
                fixedLength, bitCount, paddingBits, explicitEndianness,
                stringEncoding, bytesEncoding, combinedLengthFromField, memberLocation,
                stringEncodingCount, bytesEncodingCount));
        }

        return new ParsableTypeInfo(
            typeName, ns, fullTypeName, isReadOnly, isRecordStruct,
            defaultEndianness, members.ToImmutableArray(), targetNode.GetLocation());
    }

    // ========================================================================
    // MEMBER TYPE ANALYSIS (shared logic with BinaryParsableGenerator)
    // ========================================================================

    /// <summary>
    /// Analyzes a member type to determine how to write it.
    /// </summary>
    private static ParsableMemberKind AnalyzeMemberKind(ITypeSymbol type, bool hasStringEncoding, bool hasBytesEncoding)
    {
        string typeName = type.Name;
        string fullName = type.ToDisplayString();

        if (type.SpecialType == SpecialType.System_String)
        {
            return hasStringEncoding ? ParsableMemberKind.String : ParsableMemberKind.Unknown;
        }

        if (IsEndianWrapper(typeName))
        {
            return ParsableMemberKind.EndianWrapper;
        }

        if (typeName is "VarInt" or "VarIntZigZag")
        {
            return ParsableMemberKind.VarInt;
        }

        if (type.SpecialType == SpecialType.System_Byte)
        {
            return ParsableMemberKind.Byte;
        }

        if (type is IArrayTypeSymbol arrayType && arrayType.ElementType.SpecialType == SpecialType.System_Byte)
        {
            return hasBytesEncoding ? ParsableMemberKind.ByteArrayDynamic : ParsableMemberKind.ByteArray;
        }

        if (fullName is "System.Memory<byte>" or "System.ReadOnlyMemory<byte>")
        {
            return hasBytesEncoding ? ParsableMemberKind.MemoryDynamic : ParsableMemberKind.Unknown;
        }

        // Check for [BinaryWritable] or [BinaryParsable] nested type (both use same structure)
        if (type.GetAttributes().Any(a =>
            a.AttributeClass?.Name is "BinaryWritableAttribute" or "BinaryParsableAttribute" ||
            a.AttributeClass?.ToDisplayString() is "ZeroAlloc.BinaryWritableAttribute" or "ZeroAlloc.BinaryParsableAttribute"))
        {
            return ParsableMemberKind.NestedParsable;
        }

        // Check for types implementing IBinarySerializable
        if (type.AllInterfaces.Any(i =>
            i.ToDisplayString() == "ZeroAlloc.IBinarySerializable" ||
            i.Name == "IBinarySerializable"))
        {
            return ParsableMemberKind.NestedParsable;
        }

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
    // VALIDATION
    // ========================================================================

    /// <summary>
    /// Validates member configuration and returns them in writing order.
    /// </summary>
    private static List<ParsableMemberInfo> ValidateAndOrderMembers(
        SourceProductionContext ctx,
        ParsableTypeInfo typeInfo)
    {
        ImmutableArray<ParsableMemberInfo> members = typeInfo.Members;

        // Check for [BinaryOrder] + [BinaryIgnore] conflicts
        foreach (ParsableMemberInfo member in members)
        {
            if (member.IsIgnoredWithOrder)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    WriteDiagnostics.OrderAndIgnoreConflict, member.Location, member.Name));
                return new List<ParsableMemberInfo>();
            }
        }

        // Check for unknown types
        foreach (ParsableMemberInfo member in members)
        {
            if (member.Kind == ParsableMemberKind.Unknown)
            {
                if (member.TypeName == "String" || member.FullTypeName == "string")
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        WriteDiagnostics.StringWithoutLengthEncoding, member.Location, member.Name));
                }
                else if (member.FullTypeName == "byte[]" || member.TypeName == "Byte[]")
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        WriteDiagnostics.BytesWithoutLengthEncoding, member.Location, member.Name));
                }
                else if (member.FullTypeName.StartsWith("System.Memory<System.Byte>") ||
                         member.FullTypeName.StartsWith("System.ReadOnlyMemory<System.Byte>"))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        WriteDiagnostics.BytesWithoutLengthEncoding, member.Location, member.Name));
                }
                else
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        WriteDiagnostics.MemberNotWritable, member.Location, member.Name, member.FullTypeName));
                }
                return new List<ParsableMemberInfo>();
            }

            if (member.Kind == ParsableMemberKind.ByteArray && member.FixedLength is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    WriteDiagnostics.ByteArrayWithoutLength, member.Location, member.Name));
                return new List<ParsableMemberInfo>();
            }

            if (member.PaddingBits < 0 || member.PaddingBits > 64)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    WriteDiagnostics.InvalidPaddingBits, member.Location, member.PaddingBits));
                return new List<ParsableMemberInfo>();
            }

            // ZA3012: Validate FixedLength > 0 (for [BinaryFixedLength] and [StringFixedLength])
            if (member.FixedLength is not null and <= 0)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    WriteDiagnostics.InvalidFixedLength, member.Location, member.Name, member.FixedLength));
                return new List<ParsableMemberInfo>();
            }

            if (member.StringEncoding is { Encoding: StringLengthEncodingKind.Fixed, FixedLength: <= 0 })
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    WriteDiagnostics.InvalidFixedLength, member.Location,
                    member.Name, member.StringEncoding.Value.FixedLength));
                return new List<ParsableMemberInfo>();
            }

            // ZA3013: Validate no conflicting encoding attributes
            if (member.StringEncodingAttributeCount > 1)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    WriteDiagnostics.ConflictingEncodingAttributes, member.Location, member.Name));
                return new List<ParsableMemberInfo>();
            }

            if (member.BytesEncodingAttributeCount > 1)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    WriteDiagnostics.ConflictingEncodingAttributes, member.Location, member.Name));
                return new List<ParsableMemberInfo>();
            }
        }

        // Check ordering consistency
        bool anyHasOrder = members.Any(m => m.ExplicitOrder.HasValue);
        bool allHaveOrder = members.All(m => m.ExplicitOrder.HasValue);

        if (anyHasOrder && !allHaveOrder)
        {
            IEnumerable<string> missing = members.Where(m => !m.ExplicitOrder.HasValue).Select(m => m.Name);
            ctx.ReportDiagnostic(Diagnostic.Create(
                WriteDiagnostics.InconsistentOrdering, typeInfo.Location, string.Join(", ", missing)));
            return new List<ParsableMemberInfo>();
        }

        if (allHaveOrder)
        {
            IEnumerable<IGrouping<int, ParsableMemberInfo>> duplicates = members
                .GroupBy(m => m.ExplicitOrder!.Value).Where(g => g.Count() > 1);

            foreach (IGrouping<int, ParsableMemberInfo> dup in duplicates)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    WriteDiagnostics.DuplicateOrder, typeInfo.Location,
                    dup.Key, string.Join(", ", dup.Select(m => m.Name))));
                return new List<ParsableMemberInfo>();
            }
        }

        List<ParsableMemberInfo> orderedMembers = allHaveOrder
            ? members.OrderBy(m => m.ExplicitOrder!.Value).ToList()
            : members.OrderBy(m => m.DeclaredOrder).ToList();

        if (!ValidateByteAlignment(ctx, orderedMembers))
        {
            return new List<ParsableMemberInfo>();
        }

        return orderedMembers;
    }

    /// <summary>
    /// Validates byte alignment for types that require it.
    /// </summary>
    private static bool ValidateByteAlignment(SourceProductionContext ctx, List<ParsableMemberInfo> orderedMembers)
    {
        int bitOffset = 0;

        foreach (ParsableMemberInfo member in orderedMembers)
        {
            bitOffset += member.PaddingBits;

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
                    WriteDiagnostics.RequiresByteAlignment, member.Location,
                    member.Name, member.FullTypeName, bitOffset % 8));
                return false;
            }

            int memberBits = member.Kind switch
            {
                ParsableMemberKind.BitField => member.BitCount ?? 0,
                ParsableMemberKind.Byte => 8,
                ParsableMemberKind.EndianWrapper => (GetEndianWrapperSize(member.TypeName) ?? 4) * 8,
                ParsableMemberKind.PrimitiveInteger => (GetPrimitiveSize(member.FullTypeName) ?? 4) * 8,
                ParsableMemberKind.Float => 32,
                ParsableMemberKind.Double => 64,
                _ => 0
            };

            bitOffset += memberBits;

            if (member.Kind is ParsableMemberKind.VarInt or
                ParsableMemberKind.ByteArray or ParsableMemberKind.ByteArrayDynamic or
                ParsableMemberKind.MemoryDynamic or ParsableMemberKind.NestedParsable or
                ParsableMemberKind.String)
            {
                bitOffset = 0;
            }
        }

        return true;
    }

    // ========================================================================
    // SIZE HELPERS
    // ========================================================================

    // Size calculation helpers delegate to BinaryGeneratorHelpers
    private static int? GetEndianWrapperSize(string typeName) => BinaryGeneratorHelpers.GetEndianWrapperSize(typeName);

    private static int? GetPrimitiveSize(string fullTypeName) => BinaryGeneratorHelpers.GetPrimitiveSize(fullTypeName);

    private static int? CalculateFixedSize(List<ParsableMemberInfo> members) => BinaryGeneratorHelpers.CalculateFixedSize(members);

    private static int? CalculateFixedBits(List<ParsableMemberInfo> members) => BinaryGeneratorHelpers.CalculateFixedBits(members);

    private static int GetDefaultBitCount(ParsableMemberInfo member) => BinaryGeneratorHelpers.GetDefaultBitCount(member);

    // ========================================================================
    // CODE GENERATION
    // ========================================================================

    /// <summary>
    /// Generates the partial struct implementing IBinarySerializable.
    /// </summary>
    private static void GenerateWritingCode(SourceProductionContext ctx, ParsableTypeInfo? typeInfoNullable)
    {
        if (typeInfoNullable is not ParsableTypeInfo typeInfo)
        {
            return;
        }

        List<ParsableMemberInfo> orderedMembers = ValidateAndOrderMembers(ctx, typeInfo);
        if (orderedMembers.Count == 0 && typeInfo.Members.Length > 0)
        {
            return;
        }

        bool usesBitWriter = orderedMembers.Any(m => m.Kind == ParsableMemberKind.BitField);
        int? fixedSize = CalculateFixedSize(orderedMembers);
        int? fixedBits = usesBitWriter ? CalculateFixedBits(orderedMembers) : null;

        StringBuilder sb = new();

        // File header
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Buffers.Binary;");
        if (usesBitWriter)
        {
            sb.AppendLine("using ZeroAlloc;");
        }
        sb.AppendLine();

        if (typeInfo.Namespace is not null)
        {
            sb.AppendLine($"namespace {typeInfo.Namespace};");
            sb.AppendLine();
        }

        string modifiers = typeInfo.IsReadOnly ? "readonly partial" : "partial";
        string keyword = typeInfo.IsRecordStruct ? "record struct" : "struct";
        sb.AppendLine($"{modifiers} {keyword} {typeInfo.TypeName} : ZeroAlloc.IBinarySerializable");
        sb.AppendLine("{");

        // ---- TryGetSerializedSize (instance) ----
        GenerateTryGetSerializedSize(sb, orderedMembers, fixedSize, fixedBits, usesBitWriter);

        // ---- TryWrite ----
        GenerateTryWrite(sb, typeInfo, orderedMembers, fixedSize, fixedBits, usesBitWriter);

        // ---- WriteTo convenience ----
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Writes this instance to the destination span. Throws if buffer is too small.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public void WriteTo(Span<byte> destination)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!TryWrite(destination, out _))");
        sb.AppendLine($"            throw new InvalidOperationException(\"Buffer too small to write {typeInfo.TypeName}\");");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        ctx.AddSource($"{typeInfo.TypeName}.Writing.g.cs", sb.ToString());
    }

    // ========================================================================
    // TryGetSerializedSize GENERATION
    // ========================================================================

    /// <summary>
    /// Generates TryGetSerializedSize instance method.
    /// </summary>
    private static void GenerateTryGetSerializedSize(
        StringBuilder sb,
        List<ParsableMemberInfo> orderedMembers,
        int? fixedSize,
        int? fixedBits,
        bool usesBitWriter)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Tries to get the serialized size in bytes of this instance.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    bool ZeroAlloc.IBinarySerializable.TryGetSerializedSize(out int size)");
        sb.AppendLine("    {");

        if (fixedBits.HasValue)
        {
            int byteSize = (fixedBits.Value + 7) / 8;
            sb.AppendLine($"        size = {byteSize};");
            sb.AppendLine("        return true;");
        }
        else if (fixedSize.HasValue)
        {
            sb.AppendLine($"        size = {fixedSize.Value};");
            sb.AppendLine("        return true;");
        }
        else
        {
            // Dynamic size: calculate at runtime
            sb.AppendLine("        int total = 0;");
            foreach (ParsableMemberInfo member in orderedMembers)
            {
                GenerateMemberSizeCalculation(sb, member);
            }
            sb.AppendLine("        size = total;");
            sb.AppendLine("        return true;");
        }

        sb.AppendLine("    }");
        sb.AppendLine();
    }

    /// <summary>
    /// Generates size calculation code for a single member.
    /// </summary>
    private static void GenerateMemberSizeCalculation(StringBuilder sb, ParsableMemberInfo member)
    {
        // Add padding bits size
        if (member.PaddingBits > 0)
        {
            int paddingBytes = member.PaddingBits / 8;
            if (paddingBytes > 0)
            {
                sb.AppendLine($"        total += {paddingBytes}; // padding");
            }
        }

        switch (member.Kind)
        {
            case ParsableMemberKind.EndianWrapper:
                int wrapperSize = GetEndianWrapperSize(member.TypeName) ?? 4;
                sb.AppendLine($"        total += {wrapperSize}; // {member.Name}");
                break;

            case ParsableMemberKind.Byte:
                sb.AppendLine($"        total += 1; // {member.Name}");
                break;

            case ParsableMemberKind.ByteArray:
                sb.AppendLine($"        total += {member.FixedLength!.Value}; // {member.Name}");
                break;

            case ParsableMemberKind.PrimitiveInteger:
                int primSize = GetPrimitiveSize(member.FullTypeName) ?? 4;
                sb.AppendLine($"        total += {primSize}; // {member.Name}");
                break;

            case ParsableMemberKind.Float:
                sb.AppendLine($"        total += 4; // {member.Name}");
                break;

            case ParsableMemberKind.Double:
                sb.AppendLine($"        total += 8; // {member.Name}");
                break;

            case ParsableMemberKind.VarInt:
                // VarInt has variable size - use EncodedSize
                sb.AppendLine($"        total += {member.Name}.EncodedSize; // {member.Name}");
                break;

            case ParsableMemberKind.NestedParsable:
                sb.AppendLine($"        if (!{member.Name}.TryGetSerializedSize(out int _{member.Name}Size))");
                sb.AppendLine("        {");
                sb.AppendLine("            size = 0;");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
                sb.AppendLine($"        total += _{member.Name}Size;");
                break;

            case ParsableMemberKind.ByteArrayDynamic:
                GenerateDynamicBytesSizeCalculation(sb, member);
                break;

            case ParsableMemberKind.MemoryDynamic:
                GenerateDynamicMemorySizeCalculation(sb, member);
                break;

            case ParsableMemberKind.String:
                GenerateStringSizeCalculation(sb, member);
                break;

            case ParsableMemberKind.BitField:
                // Bit fields are handled separately - should not reach here for dynamic size
                break;
        }
    }

    /// <summary>
    /// Generates size calculation for dynamic-length byte arrays.
    /// </summary>
    private static void GenerateDynamicBytesSizeCalculation(StringBuilder sb, ParsableMemberInfo member)
    {
        BytesLengthEncodingInfo encoding = member.BytesEncoding!.Value;
        string dataLen = $"{member.Name}.Length";

        switch (encoding.Encoding)
        {
            case BytesLengthEncodingKind.VarInt:
                sb.AppendLine($"        total += ZeroAlloc.EncodingHelper.VarIntSize({dataLen}) + {dataLen}; // {member.Name}");
                break;

            case BytesLengthEncodingKind.FixedBE:
            case BytesLengthEncodingKind.FixedLE:
                sb.AppendLine($"        total += {encoding.LengthBytes} + {dataLen}; // {member.Name}");
                break;

            case BytesLengthEncodingKind.FromField:
                // Length is written by the length field itself - only data here
                sb.AppendLine($"        total += {dataLen}; // {member.Name}");
                break;
        }
    }

    /// <summary>
    /// Generates size calculation for dynamic-length Memory&lt;byte&gt;.
    /// </summary>
    private static void GenerateDynamicMemorySizeCalculation(StringBuilder sb, ParsableMemberInfo member)
    {
        BytesLengthEncodingInfo encoding = member.BytesEncoding!.Value;
        string dataLen = $"{member.Name}.Length";

        switch (encoding.Encoding)
        {
            case BytesLengthEncodingKind.VarInt:
                sb.AppendLine($"        total += new ZeroAlloc.VarInt((ulong){dataLen}).EncodedSize + {dataLen}; // {member.Name}");
                break;

            case BytesLengthEncodingKind.FixedBE:
            case BytesLengthEncodingKind.FixedLE:
                sb.AppendLine($"        total += {encoding.LengthBytes} + {dataLen}; // {member.Name}");
                break;

            case BytesLengthEncodingKind.FromField:
                sb.AppendLine($"        total += {dataLen}; // {member.Name}");
                break;
        }
    }

    /// <summary>
    /// Generates size calculation for strings.
    /// </summary>
    private static void GenerateStringSizeCalculation(StringBuilder sb, ParsableMemberInfo member)
    {
        StringLengthEncodingInfo encoding = member.StringEncoding!.Value;

        switch (encoding.Encoding)
        {
            case StringLengthEncodingKind.VarInt:
                sb.AppendLine($"        int _{member.Name}ByteLen = System.Text.Encoding.UTF8.GetByteCount({member.Name});");
                sb.AppendLine($"        total += new ZeroAlloc.VarInt((ulong)_{member.Name}ByteLen).EncodedSize + _{member.Name}ByteLen; // {member.Name}");
                break;

            case StringLengthEncodingKind.FixedBE:
            case StringLengthEncodingKind.FixedLE:
                sb.AppendLine($"        total += {encoding.LengthBytes} + System.Text.Encoding.UTF8.GetByteCount({member.Name}); // {member.Name}");
                break;

            case StringLengthEncodingKind.NullTerminated:
                sb.AppendLine($"        total += System.Text.Encoding.UTF8.GetByteCount({member.Name}) + 1; // {member.Name} + null");
                break;

            case StringLengthEncodingKind.Fixed:
                sb.AppendLine($"        total += {encoding.FixedLength}; // {member.Name}");
                break;

            case StringLengthEncodingKind.FromField:
                // Length is written by the length field - only string data here
                sb.AppendLine($"        total += System.Text.Encoding.UTF8.GetByteCount({member.Name}); // {member.Name}");
                break;
        }
    }

    // ========================================================================
    // TryWrite GENERATION
    // ========================================================================

    /// <summary>
    /// Generates the TryWrite instance method.
    /// </summary>
    private static void GenerateTryWrite(
        StringBuilder sb,
        ParsableTypeInfo typeInfo,
        List<ParsableMemberInfo> orderedMembers,
        int? fixedSize,
        int? fixedBits,
        bool usesBitWriter)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Tries to write this instance to the destination span.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public bool TryWrite(Span<byte> destination, out int bytesWritten)");
        sb.AppendLine("    {");

        // Size check for fixed-size types
        int requiredBytes = fixedBits.HasValue ? (fixedBits.Value + 7) / 8 : (fixedSize ?? 0);
        if (requiredBytes > 0)
        {
            sb.AppendLine($"        if (destination.Length < {requiredBytes})");
            sb.AppendLine("        {");
            sb.AppendLine("            bytesWritten = 0;");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        if (usesBitWriter)
        {
            sb.AppendLine("        var writer = new BitWriter(destination);");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("        int offset = 0;");
            sb.AppendLine();
        }

        // Generate writing code for each member
        foreach (ParsableMemberInfo member in orderedMembers)
        {
            GenerateMemberWriting(sb, member, typeInfo.DefaultEndianness, usesBitWriter);
        }

        if (usesBitWriter)
        {
            sb.AppendLine("        bytesWritten = writer.BytesWritten;");
        }
        else
        {
            sb.AppendLine("        bytesWritten = offset;");
        }
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    // ========================================================================
    // MEMBER-SPECIFIC WRITING CODE GENERATION
    // ========================================================================

    /// <summary>
    /// Generates writing code for a single member.
    /// </summary>
    private static void GenerateMemberWriting(
        StringBuilder sb,
        ParsableMemberInfo member,
        Endianness defaultEndianness,
        bool usesBitWriter)
    {
        sb.AppendLine($"        // Write {member.Name} ({member.Kind}{(member.BitCount.HasValue ? $", {member.BitCount} bits" : "")})");

        if (usesBitWriter)
        {
            GenerateBitWriterWriting(sb, member, defaultEndianness);

            if (member.PaddingBits > 0)
            {
                sb.AppendLine($"        // Skip {member.PaddingBits} padding bits after {member.Name}");
                sb.AppendLine($"        writer.SkipBits({member.PaddingBits});");
            }
            sb.AppendLine();
            return;
        }

        // Byte-aligned writing
        switch (member.Kind)
        {
            case ParsableMemberKind.Byte:
                sb.AppendLine($"        destination[offset] = {member.Name};");
                sb.AppendLine("        offset += 1;");
                break;

            case ParsableMemberKind.ByteArray:
                int length = member.FixedLength!.Value;
                sb.AppendLine($"        {member.Name}.AsSpan().CopyTo(destination.Slice(offset, {length}));");
                sb.AppendLine($"        offset += {length};");
                break;

            case ParsableMemberKind.EndianWrapper:
                GenerateEndianWrapperWriting(sb, member);
                break;

            case ParsableMemberKind.VarInt:
                GenerateVarIntWriting(sb, member);
                break;

            case ParsableMemberKind.NestedParsable:
                sb.AppendLine($"        if (!{member.Name}.TryWrite(destination.Slice(offset), out int _{member.Name}Written))");
                sb.AppendLine("        {");
                sb.AppendLine("            bytesWritten = 0;");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
                sb.AppendLine($"        offset += _{member.Name}Written;");
                break;

            case ParsableMemberKind.PrimitiveInteger:
                GeneratePrimitiveIntegerWriting(sb, member, member.ExplicitEndianness ?? defaultEndianness);
                break;

            case ParsableMemberKind.String:
                GenerateStringWriting(sb, member);
                break;

            case ParsableMemberKind.ByteArrayDynamic:
                GenerateByteArrayDynamicWriting(sb, member);
                break;

            case ParsableMemberKind.MemoryDynamic:
                GenerateMemoryDynamicWriting(sb, member);
                break;
        }

        // Skip padding after field
        if (member.PaddingBits > 0)
        {
            int paddingBytes = member.PaddingBits / 8;
            if (paddingBytes > 0)
            {
                sb.AppendLine($"        // Skip {member.PaddingBits} padding bits ({paddingBytes} bytes) after {member.Name}");
                sb.AppendLine($"        destination.Slice(offset, {paddingBytes}).Clear();");
                sb.AppendLine($"        offset += {paddingBytes};");
            }
        }

        sb.AppendLine();
    }

    /// <summary>
    /// Generates write code for endian wrapper members (U16BE, I32LE, etc.).
    /// </summary>
    private static void GenerateEndianWrapperWriting(StringBuilder sb, ParsableMemberInfo member)
    {
        string typeName = member.TypeName;
        int size = GetEndianWrapperSize(typeName) ?? 4;
        bool isBigEndian = typeName.EndsWith("BE");

        string writeCall = (typeName, isBigEndian) switch
        {
            ("U16BE", _) or ("U16LE", _) =>
                $"BinaryPrimitives.WriteUInt16{(isBigEndian ? "BigEndian" : "LittleEndian")}(destination.Slice(offset), {member.Name}.Value)",
            ("U32BE", _) or ("U32LE", _) =>
                $"BinaryPrimitives.WriteUInt32{(isBigEndian ? "BigEndian" : "LittleEndian")}(destination.Slice(offset), {member.Name}.Value)",
            ("U64BE", _) or ("U64LE", _) =>
                $"BinaryPrimitives.WriteUInt64{(isBigEndian ? "BigEndian" : "LittleEndian")}(destination.Slice(offset), {member.Name}.Value)",
            ("U128BE", _) or ("U128LE", _) =>
                $"BinaryPrimitives.WriteUInt128{(isBigEndian ? "BigEndian" : "LittleEndian")}(destination.Slice(offset), {member.Name}.Value)",
            ("I16BE", _) or ("I16LE", _) =>
                $"BinaryPrimitives.WriteInt16{(isBigEndian ? "BigEndian" : "LittleEndian")}(destination.Slice(offset), {member.Name}.Value)",
            ("I32BE", _) or ("I32LE", _) =>
                $"BinaryPrimitives.WriteInt32{(isBigEndian ? "BigEndian" : "LittleEndian")}(destination.Slice(offset), {member.Name}.Value)",
            ("I64BE", _) or ("I64LE", _) =>
                $"BinaryPrimitives.WriteInt64{(isBigEndian ? "BigEndian" : "LittleEndian")}(destination.Slice(offset), {member.Name}.Value)",
            ("I128BE", _) or ("I128LE", _) =>
                $"BinaryPrimitives.WriteInt128{(isBigEndian ? "BigEndian" : "LittleEndian")}(destination.Slice(offset), {member.Name}.Value)",
            ("F32BE", _) or ("F32LE", _) =>
                $"BinaryPrimitives.WriteSingle{(isBigEndian ? "BigEndian" : "LittleEndian")}(destination.Slice(offset), {member.Name}.Value)",
            ("F64BE", _) or ("F64LE", _) =>
                $"BinaryPrimitives.WriteDouble{(isBigEndian ? "BigEndian" : "LittleEndian")}(destination.Slice(offset), {member.Name}.Value)",
            _ => "/* unsupported */"
        };

        sb.AppendLine($"        {writeCall};");
        sb.AppendLine($"        offset += {size};");
    }

    /// <summary>
    /// Generates write code for VarInt members.
    /// </summary>
    private static void GenerateVarIntWriting(StringBuilder sb, ParsableMemberInfo member)
    {
        sb.AppendLine($"        if (!{member.Name}.TryWrite(destination.Slice(offset), out int _{member.Name}Written))");
        sb.AppendLine("        {");
        sb.AppendLine("            bytesWritten = 0;");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine($"        offset += _{member.Name}Written;");
    }

    /// <summary>
    /// Generates write code for primitive integer members.
    /// </summary>
    private static void GeneratePrimitiveIntegerWriting(StringBuilder sb, ParsableMemberInfo member, Endianness endianness)
    {
        string endianSuffix = endianness == Endianness.BigEndian ? "BigEndian" : "LittleEndian";

        (string writeCall, int size) = member.FullTypeName switch
        {
            "sbyte" or "System.SByte" => ($"destination[offset] = (byte){member.Name}", 1),

            "short" or "System.Int16" =>
                ($"BinaryPrimitives.WriteInt16{endianSuffix}(destination.Slice(offset), {member.Name})", 2),
            "ushort" or "System.UInt16" =>
                ($"BinaryPrimitives.WriteUInt16{endianSuffix}(destination.Slice(offset), {member.Name})", 2),
            "System.Half" =>
                ($"BinaryPrimitives.WriteHalf{endianSuffix}(destination.Slice(offset), {member.Name})", 2),

            "int" or "System.Int32" =>
                ($"BinaryPrimitives.WriteInt32{endianSuffix}(destination.Slice(offset), {member.Name})", 4),
            "uint" or "System.UInt32" =>
                ($"BinaryPrimitives.WriteUInt32{endianSuffix}(destination.Slice(offset), {member.Name})", 4),
            "float" or "System.Single" =>
                ($"BinaryPrimitives.WriteSingle{endianSuffix}(destination.Slice(offset), {member.Name})", 4),

            "long" or "System.Int64" =>
                ($"BinaryPrimitives.WriteInt64{endianSuffix}(destination.Slice(offset), {member.Name})", 8),
            "ulong" or "System.UInt64" =>
                ($"BinaryPrimitives.WriteUInt64{endianSuffix}(destination.Slice(offset), {member.Name})", 8),
            "double" or "System.Double" =>
                ($"BinaryPrimitives.WriteDouble{endianSuffix}(destination.Slice(offset), {member.Name})", 8),
            "nint" or "System.IntPtr" =>
                ($"BinaryPrimitives.WriteInt64{endianSuffix}(destination.Slice(offset), (long){member.Name})", 8),
            "nuint" or "System.UIntPtr" =>
                ($"BinaryPrimitives.WriteUInt64{endianSuffix}(destination.Slice(offset), (ulong){member.Name})", 8),

            "System.Int128" =>
                ($"BinaryPrimitives.WriteInt128{endianSuffix}(destination.Slice(offset), {member.Name})", 16),
            "System.UInt128" =>
                ($"BinaryPrimitives.WriteUInt128{endianSuffix}(destination.Slice(offset), {member.Name})", 16),

            _ => ("/* unsupported */", 0)
        };

        sb.AppendLine($"        {writeCall};");
        sb.AppendLine($"        offset += {size};");
    }

    /// <summary>
    /// Generates string writing code based on length encoding.
    /// </summary>
    private static void GenerateStringWriting(StringBuilder sb, ParsableMemberInfo member)
    {
        StringLengthEncodingInfo encoding = member.StringEncoding!.Value;
        string memberName = member.Name;

        switch (encoding.Encoding)
        {
            case StringLengthEncodingKind.VarInt:
                sb.AppendLine($"        int _{memberName}ByteLen = System.Text.Encoding.UTF8.GetByteCount({memberName});");
                sb.AppendLine($"        if (!new ZeroAlloc.VarInt((ulong)_{memberName}ByteLen).TryWrite(destination.Slice(offset), out int _{memberName}VarIntLen))");
                sb.AppendLine("        {");
                sb.AppendLine("            bytesWritten = 0;");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
                sb.AppendLine($"        offset += _{memberName}VarIntLen;");
                sb.AppendLine($"        System.Text.Encoding.UTF8.GetBytes({memberName}.AsSpan(), destination.Slice(offset, _{memberName}ByteLen));");
                sb.AppendLine($"        offset += _{memberName}ByteLen;");
                break;

            case StringLengthEncodingKind.FixedBE:
            case StringLengthEncodingKind.FixedLE:
                {
                    bool isBE = encoding.Encoding == StringLengthEncodingKind.FixedBE;
                    string endianSuffix = isBE ? "BigEndian" : "LittleEndian";
                    sb.AppendLine($"        int _{memberName}ByteLen = System.Text.Encoding.UTF8.GetByteCount({memberName});");

                    switch (encoding.LengthBytes)
                    {
                        case 1:
                            sb.AppendLine($"        destination[offset] = (byte)_{memberName}ByteLen;");
                            sb.AppendLine("        offset += 1;");
                            break;
                        case 2:
                            sb.AppendLine($"        BinaryPrimitives.WriteUInt16{endianSuffix}(destination.Slice(offset), (ushort)_{memberName}ByteLen);");
                            sb.AppendLine("        offset += 2;");
                            break;
                        case 4:
                        default:
                            sb.AppendLine($"        BinaryPrimitives.WriteUInt32{endianSuffix}(destination.Slice(offset), (uint)_{memberName}ByteLen);");
                            sb.AppendLine("        offset += 4;");
                            break;
                    }

                    sb.AppendLine($"        System.Text.Encoding.UTF8.GetBytes({memberName}.AsSpan(), destination.Slice(offset, _{memberName}ByteLen));");
                    sb.AppendLine($"        offset += _{memberName}ByteLen;");
                    break;
                }

            case StringLengthEncodingKind.NullTerminated:
                sb.AppendLine($"        int _{memberName}ByteLen = System.Text.Encoding.UTF8.GetBytes({memberName}.AsSpan(), destination.Slice(offset));");
                sb.AppendLine($"        offset += _{memberName}ByteLen;");
                sb.AppendLine("        destination[offset] = 0; // null terminator");
                sb.AppendLine("        offset += 1;");
                break;

            case StringLengthEncodingKind.Fixed:
                {
                    int fixedLen = encoding.FixedLength > 0 ? encoding.FixedLength : 0;
                    sb.AppendLine($"        destination.Slice(offset, {fixedLen}).Clear(); // zero-fill for fixed length");
                    sb.AppendLine($"        System.Text.Encoding.UTF8.GetBytes({memberName}.AsSpan(), destination.Slice(offset, {fixedLen}));");
                    sb.AppendLine($"        offset += {fixedLen};");
                    break;
                }

            case StringLengthEncodingKind.FromField:
                // Length is written by the length field itself - only string data here
                sb.AppendLine($"        int _{memberName}ByteLen = System.Text.Encoding.UTF8.GetByteCount({memberName});");
                sb.AppendLine($"        System.Text.Encoding.UTF8.GetBytes({memberName}.AsSpan(), destination.Slice(offset, _{memberName}ByteLen));");
                sb.AppendLine($"        offset += _{memberName}ByteLen;");
                break;
        }
    }

    /// <summary>
    /// Generates write code for dynamic-length byte arrays.
    /// </summary>
    private static void GenerateByteArrayDynamicWriting(StringBuilder sb, ParsableMemberInfo member)
    {
        BytesLengthEncodingInfo encoding = member.BytesEncoding!.Value;
        string memberName = member.Name;

        switch (encoding.Encoding)
        {
            case BytesLengthEncodingKind.VarInt:
                sb.AppendLine($"        if (!new ZeroAlloc.VarInt((ulong){memberName}.Length).TryWrite(destination.Slice(offset), out int _{memberName}VarIntLen))");
                sb.AppendLine("        {");
                sb.AppendLine("            bytesWritten = 0;");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
                sb.AppendLine($"        offset += _{memberName}VarIntLen;");
                sb.AppendLine($"        {memberName}.AsSpan().CopyTo(destination.Slice(offset));");
                sb.AppendLine($"        offset += {memberName}.Length;");
                break;

            case BytesLengthEncodingKind.FixedBE:
            case BytesLengthEncodingKind.FixedLE:
                {
                    bool isBE = encoding.Encoding == BytesLengthEncodingKind.FixedBE;
                    string endianSuffix = isBE ? "BigEndian" : "LittleEndian";

                    switch (encoding.LengthBytes)
                    {
                        case 1:
                            sb.AppendLine($"        destination[offset] = (byte){memberName}.Length;");
                            sb.AppendLine("        offset += 1;");
                            break;
                        case 2:
                            sb.AppendLine($"        BinaryPrimitives.WriteUInt16{endianSuffix}(destination.Slice(offset), (ushort){memberName}.Length);");
                            sb.AppendLine("        offset += 2;");
                            break;
                        case 4:
                        default:
                            sb.AppendLine($"        BinaryPrimitives.WriteUInt32{endianSuffix}(destination.Slice(offset), (uint){memberName}.Length);");
                            sb.AppendLine("        offset += 4;");
                            break;
                    }

                    sb.AppendLine($"        {memberName}.AsSpan().CopyTo(destination.Slice(offset));");
                    sb.AppendLine($"        offset += {memberName}.Length;");
                    break;
                }

            case BytesLengthEncodingKind.FromField:
                sb.AppendLine($"        {memberName}.AsSpan().CopyTo(destination.Slice(offset));");
                sb.AppendLine($"        offset += {memberName}.Length;");
                break;
        }
    }

    /// <summary>
    /// Generates write code for dynamic-length Memory&lt;byte&gt;.
    /// </summary>
    private static void GenerateMemoryDynamicWriting(StringBuilder sb, ParsableMemberInfo member)
    {
        BytesLengthEncodingInfo encoding = member.BytesEncoding!.Value;
        string memberName = member.Name;

        switch (encoding.Encoding)
        {
            case BytesLengthEncodingKind.VarInt:
                sb.AppendLine($"        if (!new ZeroAlloc.VarInt((ulong){memberName}.Length).TryWrite(destination.Slice(offset), out int _{memberName}VarIntLen))");
                sb.AppendLine("        {");
                sb.AppendLine("            bytesWritten = 0;");
                sb.AppendLine("            return false;");
                sb.AppendLine("        }");
                sb.AppendLine($"        offset += _{memberName}VarIntLen;");
                sb.AppendLine($"        {memberName}.Span.CopyTo(destination.Slice(offset));");
                sb.AppendLine($"        offset += {memberName}.Length;");
                break;

            case BytesLengthEncodingKind.FixedBE:
            case BytesLengthEncodingKind.FixedLE:
                {
                    bool isBE = encoding.Encoding == BytesLengthEncodingKind.FixedBE;
                    string endianSuffix = isBE ? "BigEndian" : "LittleEndian";

                    switch (encoding.LengthBytes)
                    {
                        case 1:
                            sb.AppendLine($"        destination[offset] = (byte){memberName}.Length;");
                            sb.AppendLine("        offset += 1;");
                            break;
                        case 2:
                            sb.AppendLine($"        BinaryPrimitives.WriteUInt16{endianSuffix}(destination.Slice(offset), (ushort){memberName}.Length);");
                            sb.AppendLine("        offset += 2;");
                            break;
                        case 4:
                        default:
                            sb.AppendLine($"        BinaryPrimitives.WriteUInt32{endianSuffix}(destination.Slice(offset), (uint){memberName}.Length);");
                            sb.AppendLine("        offset += 4;");
                            break;
                    }

                    sb.AppendLine($"        {memberName}.Span.CopyTo(destination.Slice(offset));");
                    sb.AppendLine($"        offset += {memberName}.Length;");
                    break;
                }

            case BytesLengthEncodingKind.FromField:
                sb.AppendLine($"        {memberName}.Span.CopyTo(destination.Slice(offset));");
                sb.AppendLine($"        offset += {memberName}.Length;");
                break;
        }
    }

    // ========================================================================
    // BIT-LEVEL WRITING
    // ========================================================================

    /// <summary>
    /// Generates BitWriter-based writing for a single member.
    /// </summary>
    private static void GenerateBitWriterWriting(StringBuilder sb, ParsableMemberInfo member, Endianness defaultEndianness)
    {
        int bitCount = member.BitCount ?? GetDefaultBitCount(member);

        switch (member.Kind)
        {
            case ParsableMemberKind.BitField:
                if (bitCount == 1)
                {
                    if (member.FullTypeName is "bool" or "System.Boolean")
                    {
                        sb.AppendLine($"        writer.WriteBits({member.Name} ? 1UL : 0UL, 1);");
                    }
                    else
                    {
                        sb.AppendLine($"        writer.WriteBits((ulong){member.Name}, 1);");
                    }
                }
                else if (bitCount <= 64)
                {
                    sb.AppendLine($"        writer.WriteBits((ulong){member.Name}, {bitCount});");
                }
                else
                {
                    sb.AppendLine($"        // WARNING: {bitCount} bits exceeds 64-bit limit");
                }
                break;

            case ParsableMemberKind.Byte:
                sb.AppendLine($"        writer.WriteByte({member.Name});");
                break;

            case ParsableMemberKind.EndianWrapper:
                GenerateBitWriterEndianWrapperWriting(sb, member);
                break;

            case ParsableMemberKind.PrimitiveInteger:
                GenerateBitWriterPrimitiveIntegerWriting(sb, member);
                break;

            default:
                sb.AppendLine($"        // WARNING: {member.Kind} not supported in bit-level writing mode");
                break;
        }
    }

    private static void GenerateBitWriterEndianWrapperWriting(StringBuilder sb, ParsableMemberInfo member)
    {
        string typeName = member.TypeName;

        // Determine the write call based on wrapper size
        string writeCall = typeName switch
        {
            "U16BE" or "I16BE" => $"writer.WriteUInt16((ushort){member.Name}.Value)",
            "U16LE" or "I16LE" => $"writer.WriteUInt16((ushort){member.Name}.Value)", // BitWriter always writes big-endian
            "U32BE" or "I32BE" => $"writer.WriteUInt32((uint){member.Name}.Value)",
            "U32LE" or "I32LE" => $"writer.WriteUInt32((uint){member.Name}.Value)",
            "U64BE" or "I64BE" => $"writer.WriteUInt64((ulong){member.Name}.Value)",
            "U64LE" or "I64LE" => $"writer.WriteUInt64((ulong){member.Name}.Value)",
            _ => $"/* unsupported: {typeName} */"
        };

        sb.AppendLine($"        {writeCall};");
    }

    private static void GenerateBitWriterPrimitiveIntegerWriting(StringBuilder sb, ParsableMemberInfo member)
    {
        string writeCall = member.FullTypeName switch
        {
            "sbyte" or "System.SByte" => $"writer.WriteByte((byte){member.Name})",
            "byte" or "System.Byte" => $"writer.WriteByte({member.Name})",
            "short" or "System.Int16" => $"writer.WriteInt16({member.Name})",
            "ushort" or "System.UInt16" => $"writer.WriteUInt16({member.Name})",
            "int" or "System.Int32" => $"writer.WriteInt32({member.Name})",
            "uint" or "System.UInt32" => $"writer.WriteUInt32({member.Name})",
            "long" or "System.Int64" => $"writer.WriteInt64({member.Name})",
            "ulong" or "System.UInt64" => $"writer.WriteUInt64({member.Name})",
            "float" or "System.Single" => $"writer.WriteInt32(BitConverter.SingleToInt32Bits({member.Name}))",
            "double" or "System.Double" => $"writer.WriteInt64(BitConverter.DoubleToInt64Bits({member.Name}))",
            _ => $"/* unsupported: {member.FullTypeName} */"
        };

        sb.AppendLine($"        {writeCall};");
    }
}
