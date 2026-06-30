// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests;

/// <summary>Tests for <see cref="BinaryGeneratorHelpers"/> type recognition and size calculation.</summary>
internal sealed class BinaryGeneratorHelpersTests
{
    [Test]
    [Arguments("U16BE", true)]
    [Arguments("U16LE", true)]
    [Arguments("U32BE", true)]
    [Arguments("U32LE", true)]
    [Arguments("U64BE", true)]
    [Arguments("U64LE", true)]
    [Arguments("U128BE", true)]
    [Arguments("U128LE", true)]
    [Arguments("I16BE", true)]
    [Arguments("I16LE", true)]
    [Arguments("I32BE", true)]
    [Arguments("I32LE", true)]
    [Arguments("I64BE", true)]
    [Arguments("I64LE", true)]
    [Arguments("I128BE", true)]
    [Arguments("I128LE", true)]
    [Arguments("F32BE", true)]
    [Arguments("F32LE", true)]
    [Arguments("F64BE", true)]
    [Arguments("F64LE", true)]
    [Arguments("I32LE", true)]
    [Arguments("VarInt", false)]
    [Arguments("string", false)]
    public async Task IsEndianWrapper_KnownTypes(string typeName, bool expected)
    {
        bool actual = BinaryGeneratorHelpers.IsEndianWrapper(typeName);

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments(SpecialType.System_Int32, true)]
    [Arguments(SpecialType.System_UInt16, true)]
    [Arguments(SpecialType.System_Int64, true)]
    [Arguments(SpecialType.System_UInt64, true)]
    [Arguments(SpecialType.System_Single, true)]
    [Arguments(SpecialType.System_Double, true)]
    [Arguments(SpecialType.System_IntPtr, true)]
    [Arguments(SpecialType.System_UIntPtr, true)]
    [Arguments(SpecialType.System_String, false)]
    [Arguments(SpecialType.System_Boolean, false)]
    public async Task IsPrimitiveInteger_Primitives(SpecialType specialType, bool expected)
    {
        Compilation compilation = CSharpCompilation.Create("t")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("class C { }"))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        SemanticModel model = compilation.GetSemanticModel(compilation.SyntaxTrees.First());
        ITypeSymbol type = compilation.GetSpecialType(specialType);

        bool actual = BinaryGeneratorHelpers.IsPrimitiveInteger(type);

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task IsPrimitiveInteger_Int128Half_ReturnsTrue()
    {
        Compilation compilation = CSharpCompilation.Create("t")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("class C { }"))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        ITypeSymbol int128 = compilation.GetTypeByMetadataName("System.Int128")!;
        ITypeSymbol uint128 = compilation.GetTypeByMetadataName("System.UInt128")!;
        ITypeSymbol half = compilation.GetTypeByMetadataName("System.Half")!;

        await Assert.That(BinaryGeneratorHelpers.IsPrimitiveInteger(int128)).IsTrue();
        await Assert.That(BinaryGeneratorHelpers.IsPrimitiveInteger(uint128)).IsTrue();
        await Assert.That(BinaryGeneratorHelpers.IsPrimitiveInteger(half)).IsTrue();
    }

    [Test]
    [Arguments("U16BE", 2)]
    [Arguments("U16LE", 2)]
    [Arguments("I16BE", 2)]
    [Arguments("I16LE", 2)]
    [Arguments("U32BE", 4)]
    [Arguments("U32LE", 4)]
    [Arguments("I32BE", 4)]
    [Arguments("I32LE", 4)]
    [Arguments("F32BE", 4)]
    [Arguments("F32LE", 4)]
    [Arguments("U64BE", 8)]
    [Arguments("U64LE", 8)]
    [Arguments("I64BE", 8)]
    [Arguments("I64LE", 8)]
    [Arguments("F64BE", 8)]
    [Arguments("F64LE", 8)]
    [Arguments("U128BE", 16)]
    [Arguments("U128LE", 16)]
    [Arguments("I128BE", 16)]
    [Arguments("I128LE", 16)]
    [Arguments("Unknown", null)]
    public async Task GetEndianWrapperSize_ReturnsExpected(string typeName, int? expected)
    {
        int? actual = BinaryGeneratorHelpers.GetEndianWrapperSize(typeName);

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    [Arguments("sbyte", 1)]
    [Arguments("System.SByte", 1)]
    [Arguments("short", 2)]
    [Arguments("ushort", 2)]
    [Arguments("System.Int16", 2)]
    [Arguments("System.UInt16", 2)]
    [Arguments("System.Half", 2)]
    [Arguments("int", 4)]
    [Arguments("uint", 4)]
    [Arguments("System.Int32", 4)]
    [Arguments("System.UInt32", 4)]
    [Arguments("float", 4)]
    [Arguments("System.Single", 4)]
    [Arguments("long", 8)]
    [Arguments("ulong", 8)]
    [Arguments("System.Int64", 8)]
    [Arguments("System.UInt64", 8)]
    [Arguments("double", 8)]
    [Arguments("System.Double", 8)]
    [Arguments("nint", 8)]
    [Arguments("nuint", 8)]
    [Arguments("System.IntPtr", 8)]
    [Arguments("System.UIntPtr", 8)]
    [Arguments("System.Int128", 16)]
    [Arguments("System.UInt128", 16)]
    [Arguments("System.Guid", null)]
    public async Task GetPrimitiveSize_ReturnsExpected(string fullTypeName, int? expected)
    {
        int? actual = BinaryGeneratorHelpers.GetPrimitiveSize(fullTypeName);

        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task AppendCode_AppendsTrailingNewlineWhenMissing()
    {
        StringBuilder sb = new();

        BinaryGeneratorHelpers.AppendCode(sb, "line");

        await Assert.That(sb.ToString()).IsEqualTo("line\n");
    }

    [Test]
    public async Task AppendCode_PreservesExistingNewline()
    {
        StringBuilder sb = new();

        BinaryGeneratorHelpers.AppendCode(sb, "line\n");

        await Assert.That(sb.ToString()).IsEqualTo("line\n");
    }

    [Test]
    public async Task CalculateFixedSize_AllFixedMembers_ReturnsSum()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("A", "U16BE", ParsableMemberKind.EndianWrapper, fixedLength: null),
            _Member("B", "byte", ParsableMemberKind.Byte, fixedLength: null),
        ];

        int? size = BinaryGeneratorHelpers.CalculateFixedSize(members);

        await Assert.That(size).IsEqualTo(3);
    }

    [Test]
    public async Task CalculateFixedSize_VarIntMember_ReturnsNull()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("A", "VarInt", ParsableMemberKind.VarInt, fixedLength: null),
        ];

        int? size = BinaryGeneratorHelpers.CalculateFixedSize(members);

        await Assert.That(size).IsNull();
    }

    [Test]
    public async Task CalculateFixedBits_SumsBitCounts()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("A", "U16BE", ParsableMemberKind.EndianWrapper, fixedLength: null, bitCount: 4),
            _Member("B", "byte", ParsableMemberKind.Byte, fixedLength: null, bitCount: null),
        ];

        int? bits = BinaryGeneratorHelpers.CalculateFixedBits(members);

        await Assert.That(bits).IsEqualTo(24);
    }

    [Test]
    [Arguments(ParsableMemberKind.EndianWrapper, 16)]
    [Arguments(ParsableMemberKind.Byte, 8)]
    [Arguments(ParsableMemberKind.VarInt, 32)]
    public async Task GetDefaultBitCount_ReturnsExpected(ParsableMemberKind kind, int expected)
    {
        ParsableMemberInfo member = _Member("X", "U16BE", kind, fixedLength: null);

        int bits = BinaryGeneratorHelpers.GetDefaultBitCount(member);

        await Assert.That(bits).IsEqualTo(expected);
    }

    [Test]
    public async Task GetFixedMemberByteSize_EndianWrapper()
    {
        ParsableMemberInfo member = _Member("A", "U32BE", ParsableMemberKind.EndianWrapper, fixedLength: null);

        int? size = BinaryGeneratorHelpers.GetFixedMemberByteSize(member);

        await Assert.That(size).IsEqualTo(4);
    }

    [Test]
    public async Task BuildGroupChecks_GroupsFixedSizeRuns()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("Flags", "byte", ParsableMemberKind.Byte, fixedLength: null, bitCount: 3),
            _Member("Value", "U16BE", ParsableMemberKind.EndianWrapper, fixedLength: null, bitCount: 4),
        ];

        Dictionary<int, int> checks = BinaryGeneratorHelpers.BuildGroupChecks(members, usesBitMode: false, fixedSize: null);

        await Assert.That(checks.Count).IsEqualTo(1);
        await Assert.That(checks[0]).IsEqualTo(3);
    }

    [Test]
    public async Task BuildGroupChecks_BitMode_ReturnsEmpty()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("A", "U16BE", ParsableMemberKind.EndianWrapper, fixedLength: null),
        ];

        Dictionary<int, int> checks = BinaryGeneratorHelpers.BuildGroupChecks(members, usesBitMode: true, fixedSize: null);

        await Assert.That(checks.Count).IsEqualTo(0);
    }

    [Test]
    public async Task CalculateFixedSize_ByteArrayAndPrimitive_ReturnsSum()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("A", "byte", ParsableMemberKind.Byte, fixedLength: null),
            _Member("B", "byte[]", ParsableMemberKind.ByteArray, fixedLength: 4, fullTypeName: "byte[]"),
            _Member("C", "int", ParsableMemberKind.PrimitiveInteger, fixedLength: null, fullTypeName: "int"),
        ];

        int? size = BinaryGeneratorHelpers.CalculateFixedSize(members);

        await Assert.That(size).IsEqualTo(9);
    }

    [Test]
    public async Task CalculateFixedSize_DynamicMember_ReturnsNull()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("A", "U16BE", ParsableMemberKind.EndianWrapper, fixedLength: null),
            _Member("B", "byte[]", ParsableMemberKind.ByteArrayDynamic, fixedLength: null),
        ];

        int? size = BinaryGeneratorHelpers.CalculateFixedSize(members);

        await Assert.That(size).IsNull();
    }

    [Test]
    public async Task CalculateFixedBits_BitFieldAndPrimitive_ReturnsSum()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("A", "byte", ParsableMemberKind.BitField, fixedLength: null, bitCount: 3, paddingBits: 5),
            _Member("B", "int", ParsableMemberKind.PrimitiveInteger, fixedLength: null, fullTypeName: "int"),
        ];

        int? bits = BinaryGeneratorHelpers.CalculateFixedBits(members);

        await Assert.That(bits).IsEqualTo(40);
    }

    [Test]
    public async Task CalculateFixedBits_VarIntMember_ReturnsNull()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("A", "VarInt", ParsableMemberKind.VarInt, fixedLength: null),
        ];

        int? bits = BinaryGeneratorHelpers.CalculateFixedBits(members);

        await Assert.That(bits).IsNull();
    }

    [Test]
    public async Task GetDefaultBitCount_PrimitiveInteger_ReturnsTypeBits()
    {
        ParsableMemberInfo member = _Member("X", "int", ParsableMemberKind.PrimitiveInteger, fixedLength: null, fullTypeName: "int");

        int bits = BinaryGeneratorHelpers.GetDefaultBitCount(member);

        await Assert.That(bits).IsEqualTo(32);
    }

    [Test]
    public async Task GetDefaultBitCount_UnknownKind_Returns32()
    {
        ParsableMemberInfo member = _Member("X", "VarInt", ParsableMemberKind.VarInt, fixedLength: null);

        int bits = BinaryGeneratorHelpers.GetDefaultBitCount(member);

        await Assert.That(bits).IsEqualTo(32);
    }

    [Test]
    public async Task GetFixedMemberByteSize_WithPadding_IncludesPaddingBytes()
    {
        ParsableMemberInfo member = _Member("A", "U16BE", ParsableMemberKind.EndianWrapper, fixedLength: null, paddingBits: 16);

        int? size = BinaryGeneratorHelpers.GetFixedMemberByteSize(member);

        await Assert.That(size).IsEqualTo(4);
    }

    [Test]
    public async Task GetFixedMemberByteSize_VariableMember_ReturnsNull()
    {
        ParsableMemberInfo member = _Member("A", "VarInt", ParsableMemberKind.VarInt, fixedLength: null);

        int? size = BinaryGeneratorHelpers.GetFixedMemberByteSize(member);

        await Assert.That(size).IsNull();
    }

    [Test]
    public async Task BuildGroupChecks_FixedSizeStruct_ReturnsEmpty()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("A", "U16BE", ParsableMemberKind.EndianWrapper, fixedLength: null),
            _Member("B", "byte", ParsableMemberKind.Byte, fixedLength: null),
        ];

        Dictionary<int, int> checks = BinaryGeneratorHelpers.BuildGroupChecks(members, usesBitMode: false, fixedSize: 3);

        await Assert.That(checks.Count).IsEqualTo(0);
    }

    [Test]
    public async Task BuildGroupChecks_VariableAfterFixed_GroupsRun()
    {
        List<ParsableMemberInfo> members =
        [
            _Member("A", "VarInt", ParsableMemberKind.VarInt, fixedLength: null),
            _Member("B", "U32BE", ParsableMemberKind.EndianWrapper, fixedLength: null),
            _Member("C", "byte", ParsableMemberKind.Byte, fixedLength: null),
        ];

        Dictionary<int, int> checks = BinaryGeneratorHelpers.BuildGroupChecks(members, usesBitMode: false, fixedSize: null);

        await Assert.That(checks.Count).IsEqualTo(1);
        await Assert.That(checks[1]).IsEqualTo(5);
    }

    [Test]
    public async Task AppendCode_EmptyString_AppendsNewline()
    {
        StringBuilder sb = new();

        BinaryGeneratorHelpers.AppendCode(sb, string.Empty);

        await Assert.That(sb.ToString()).IsEqualTo("\n");
    }

    private static ParsableMemberInfo _Member(
        string name,
        string typeName,
        ParsableMemberKind kind,
        int? fixedLength,
        int? bitCount = null,
        int paddingBits = 0,
        string? fullTypeName = null)
    {
        return new ParsableMemberInfo(
            Name: name,
            TypeName: typeName,
            FullTypeName: fullTypeName ?? typeName,
            ExplicitOrder: null,
            DeclaredOrder: 0,
            IsProperty: true,
            Kind: kind,
            FixedLength: fixedLength,
            BitCount: bitCount,
            PaddingBits: paddingBits,
            ExplicitEndianness: null,
            StringEncoding: null,
            BytesEncoding: null,
            LengthFromField: null,
            Location: Location.None,
            StringEncodingAttributeCount: 0,
            BytesEncodingAttributeCount: 0);
    }
}
