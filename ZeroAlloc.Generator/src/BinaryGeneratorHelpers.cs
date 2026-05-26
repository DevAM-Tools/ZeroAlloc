// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// ZeroAlloc Binary Generator Helpers
// ============================================================================
// Shared helper methods used by both BinaryParsableGenerator and
// BinaryWritableGenerator. Extracts common type recognition, size
// calculation, and bit counting logic to reduce code duplication and
// ensure consistency when new wrapper types are added.
// ============================================================================

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ZeroAlloc.Generator;

/// <summary>
/// Shared static helpers for binary source generators.
/// Contains type recognition, size calculation, and bit counting
/// logic used by both BinaryParsableGenerator and BinaryWritableGenerator.
/// </summary>
internal static class BinaryGeneratorHelpers
{
    #region Type Recognition

    /// <summary>
    /// Determines if a type name is an endian wrapper (U16BE, I32LE, etc.).
    /// </summary>
    /// <param name="typeName">The simple type name to check.</param>
    /// <returns><c>true</c> if the type is an endian wrapper.</returns>
    internal static bool IsEndianWrapper(string typeName) =>
        typeName is "U16BE" or "U16LE" or "U32BE" or "U32LE" or
                   "U64BE" or "U64LE" or "U128BE" or "U128LE" or
                   "I16BE" or "I16LE" or "I32BE" or "I32LE" or
                   "I64BE" or "I64LE" or "I128BE" or "I128LE" or
                   "F32BE" or "F32LE" or "F64BE" or "F64LE";

    /// <summary>
    /// Determines if a type is a primitive integer that requires explicit endianness.
    /// </summary>
    /// <param name="type">The type symbol to check.</param>
    /// <returns><c>true</c> if the type is a primitive integer.</returns>
    internal static bool IsPrimitiveInteger(ITypeSymbol type) =>
        type.SpecialType is SpecialType.System_SByte or
                           SpecialType.System_Int16 or SpecialType.System_UInt16 or
                           SpecialType.System_Int32 or SpecialType.System_UInt32 or
                           SpecialType.System_Int64 or SpecialType.System_UInt64 or
                           SpecialType.System_Single or SpecialType.System_Double or
                           SpecialType.System_IntPtr or SpecialType.System_UIntPtr ||
        type.ToDisplayString() is "System.Int128" or "System.UInt128" or "System.Half";

    #endregion

    #region Size Calculation

    /// <summary>
    /// Gets the size in bytes of an endian wrapper type.
    /// </summary>
    /// <param name="typeName">The simple type name (e.g. "U16BE").</param>
    /// <returns>Size in bytes, or <c>null</c> if not a recognized wrapper.</returns>
    internal static int? GetEndianWrapperSize(string typeName) => typeName switch
    {
        "U16BE" or "U16LE" or "I16BE" or "I16LE" => 2,
        "U32BE" or "U32LE" or "I32BE" or "I32LE" or "F32BE" or "F32LE" => 4,
        "U64BE" or "U64LE" or "I64BE" or "I64LE" or "F64BE" or "F64LE" => 8,
        "U128BE" or "U128LE" or "I128BE" or "I128LE" => 16,
        _ => null
    };

    /// <summary>
    /// Gets the size in bytes of a primitive integer type.
    /// </summary>
    /// <param name="fullTypeName">The full type name (e.g. "System.Int32").</param>
    /// <returns>Size in bytes, or <c>null</c> if not a recognized primitive.</returns>
    internal static int? GetPrimitiveSize(string fullTypeName) => fullTypeName switch
    {
        "sbyte" or "System.SByte" => 1,
        "short" or "ushort" or "System.Int16" or "System.UInt16" or "System.Half" => 2,
        "int" or "uint" or "System.Int32" or "System.UInt32" or "float" or "System.Single" => 4,
        "long" or "ulong" or "System.Int64" or "System.UInt64" or "double" or "System.Double"
            or "nint" or "nuint" or "System.IntPtr" or "System.UIntPtr" => 8,
        "System.Int128" or "System.UInt128" => 16,
        _ => null
    };

    /// <summary>
    /// Calculates the total fixed size in bytes for all members, if all are fixed-size.
    /// </summary>
    /// <remarks>
    /// Returns <c>null</c> if any member has variable size (VarInt, dynamic arrays, etc.).
    /// For fixed-size types, this enables a single length check at the start of
    /// generated TryParse/TryWrite methods.
    /// </remarks>
    /// <param name="members">The ordered list of members.</param>
    /// <returns>Total size in bytes, or <c>null</c> if variable.</returns>
    internal static int? CalculateFixedSize(List<ParsableMemberInfo> members)
    {
        int total = 0;
        foreach (ParsableMemberInfo member in members)
        {
            int? size = member.Kind switch
            {
                ParsableMemberKind.EndianWrapper => GetEndianWrapperSize(member.TypeName),
                ParsableMemberKind.Byte => 1,
                ParsableMemberKind.ByteArray => member.FixedLength,
                ParsableMemberKind.PrimitiveInteger => GetPrimitiveSize(member.FullTypeName),
                _ => null
            };

            if (size is null)
            {
                return null;
            }

            total += size.Value;
        }
        return total;
    }

    /// <summary>
    /// Calculates the total bit count for types using bit-level parsing/writing.
    /// </summary>
    /// <remarks>
    /// Used when any member has <c>[BinaryField(BitCount = n)]</c>.
    /// Returns <c>null</c> if any member has variable bit size.
    /// </remarks>
    /// <param name="members">The ordered list of members.</param>
    /// <returns>Total size in bits, or <c>null</c> if variable.</returns>
    internal static int? CalculateFixedBits(List<ParsableMemberInfo> members)
    {
        int totalBits = 0;
        foreach (ParsableMemberInfo member in members)
        {
            // Add padding bits
            totalBits += member.PaddingBits;

            int? bits = member.Kind switch
            {
                ParsableMemberKind.BitField => member.BitCount,
                ParsableMemberKind.Byte => 8,
                ParsableMemberKind.EndianWrapper => GetEndianWrapperSize(member.TypeName) * 8,
                ParsableMemberKind.PrimitiveInteger => GetPrimitiveSize(member.FullTypeName) * 8,
                _ => null
            };

            if (bits is null)
            {
                return null;
            }

            totalBits += bits.Value;
        }
        return totalBits;
    }

    /// <summary>
    /// Gets the default bit count for a member when no explicit BitCount is specified.
    /// </summary>
    /// <param name="member">The member info.</param>
    /// <returns>The default number of bits for the member's type.</returns>
    internal static int GetDefaultBitCount(ParsableMemberInfo member)
    {
        return member.Kind switch
        {
            ParsableMemberKind.Byte => 8,
            ParsableMemberKind.EndianWrapper => (GetEndianWrapperSize(member.TypeName) ?? 4) * 8,
            ParsableMemberKind.PrimitiveInteger => (GetPrimitiveSize(member.FullTypeName) ?? 4) * 8,
            _ => 32
        };
    }

    /// <summary>
    /// Returns the fixed byte size of a member (own bytes plus trailing padding), or <c>null</c>
    /// if the member has variable length (VarInt, dynamic array, nested parsable, string, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Used by the grouped-bounds-check algorithm to batch consecutive fixed-size members into a
    /// single <c>if (source.Length - offset &lt; N) return false;</c> guard, preventing
    /// <see cref="System.ArgumentOutOfRangeException"/> on span slicing for variable-size structs.
    /// </para>
    /// <para>
    /// PaddingBits must be a multiple of 8 in byte-aligned mode; if the division leaves a
    /// remainder it is intentionally discarded (the validator rejects misaligned padding).
    /// </para>
    /// </remarks>
    /// <param name="member">The member whose fixed size is queried.</param>
    /// <returns>Fixed byte size including padding, or <c>null</c> for variable-size members.</returns>
    internal static int? GetFixedMemberByteSize(ParsableMemberInfo member)
    {
        int? ownSize = member.Kind switch
        {
            ParsableMemberKind.Byte => 1,
            ParsableMemberKind.ByteArray => member.FixedLength,
            ParsableMemberKind.EndianWrapper => GetEndianWrapperSize(member.TypeName),
            ParsableMemberKind.PrimitiveInteger => GetPrimitiveSize(member.FullTypeName),
            _ => null
        };

        if (ownSize is null)
        {
            return null;
        }

        int paddingBytes = member.PaddingBits / 8;
        return ownSize.Value + paddingBytes;
    }

    /// <summary>
    /// Builds a map from member index to the summed fixed byte size of the consecutive fixed-size
    /// run starting at that index. Used by the grouped-bounds-check algorithm in both
    /// <c>GenerateParsingCode</c> and <c>GenerateTryWrite</c> to emit a single
    /// <c>if (source/destination.Length - offset &lt; N) return false;</c> guard before the first
    /// member of each run, preventing <see cref="System.ArgumentOutOfRangeException"/> on span
    /// slicing when the buffer is too short.
    /// </summary>
    /// <remarks>
    /// Returns an empty dictionary when <paramref name="usesBitMode"/> is <c>true</c> (the struct
    /// uses <c>BitReader</c>/<c>BitWriter</c> which handles bounds internally) or when
    /// <paramref name="fixedSize"/> has a value (the single upfront size check already covers
    /// the full struct).
    /// </remarks>
    /// <param name="members">Ordered list of members.</param>
    /// <param name="usesBitMode">
    /// <c>true</c> when bit-level parsing or writing is active; grouped checks are skipped in bit mode.
    /// </param>
    /// <param name="fixedSize">
    /// Pre-computed total fixed size in bytes, or <c>null</c> when the struct is variable-size.
    /// </param>
    /// <returns>
    /// Dictionary mapping each run-start member index to the total byte size of that run,
    /// or an empty dictionary when grouped checks are not applicable.
    /// </returns>
    internal static Dictionary<int, int> BuildGroupChecks(
        List<ParsableMemberInfo> members,
        bool usesBitMode,
        int? fixedSize)
    {
        Dictionary<int, int> groupChecks = new();
        if (usesBitMode || fixedSize.HasValue)
        {
            return groupChecks;
        }

        int k = 0;
        while (k < members.Count)
        {
            if (GetFixedMemberByteSize(members[k]) is not null)
            {
                // Accumulate the total byte size of this consecutive fixed-size run
                int groupTotal = 0;
                int j = k;
                while (j < members.Count && GetFixedMemberByteSize(members[j]) is int sz)
                {
                    groupTotal += sz;
                    j++;
                }
                groupChecks[k] = groupTotal;
                k = j;
            }
            else
            {
                k++;
            }
        }

        return groupChecks;
    }

    #endregion
}
