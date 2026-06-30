// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests;

/// <summary>Scenario tests exercising binary generator code paths and diagnostics.</summary>
internal sealed class GeneratorScenarioTests
{
    private const string _Usings = """
        #nullable disable
        using System;
        using System.Globalization;
        using ZeroAlloc;
        """;

    #region BinaryParsable — success paths

    [Test]
    public async Task Parsable_ComprehensiveStruct_GeneratesAllMemberKinds()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct InnerHeader
            {
                public U16BE Version { get; init; }
            }

            [BinaryParsable(DefaultEndianness = Endianness.BigEndian)]
            public partial struct ComprehensivePacket
            {
                public U16BE U16BEField { get; init; }
                public U16LE U16LEField { get; init; }
                public I16BE I16BEField { get; init; }
                public I16LE I16LEField { get; init; }
                public U32BE U32BEField { get; init; }
                public U32LE U32LEField { get; init; }
                public I32BE I32BEField { get; init; }
                public I32LE I32LEField { get; init; }
                public F32BE F32BEField { get; init; }
                public F32LE F32LEField { get; init; }
                public U64BE U64BEField { get; init; }
                public U64LE U64LEField { get; init; }
                public I64BE I64BEField { get; init; }
                public I64LE I64LEField { get; init; }
                public F64BE F64BEField { get; init; }
                public F64LE F64LEField { get; init; }
                public U128BE U128BEField { get; init; }
                public U128LE U128LEField { get; init; }
                public I128BE I128BEField { get; init; }
                public I128LE I128LEField { get; init; }
                public byte RawByte { get; init; }
                public VarInt Count { get; init; }
                public VarIntZigZag SignedCount { get; init; }
                public short PrimitiveI16 { get; init; }
                public ushort PrimitiveU16 { get; init; }
                public int PrimitiveI32 { get; init; }
                public uint PrimitiveU32 { get; init; }
                public long PrimitiveI64 { get; init; }
                public ulong PrimitiveU64 { get; init; }
                public float PrimitiveF32 { get; init; }
                public double PrimitiveF64 { get; init; }
                public sbyte PrimitiveS8 { get; init; }
                [BinaryFixedLength(4)]
                public byte[] FixedData { get; init; }
                [BytesLengthVarInt]
                public byte[] DynamicData { get; init; }
                [BytesLengthBE(2)]
                public byte[] DynamicDataBE { get; init; }
                [BytesLengthLE(4)]
                public byte[] DynamicDataLE { get; init; }
                public U16BE DataLength { get; init; }
                [BytesLengthFromField(nameof(DataLength))]
                public byte[] FromFieldData { get; init; }
                [BytesLengthVarInt]
                public Memory<byte> MemoryData { get; init; }
                [BytesLengthBE(1)]
                public ReadOnlyMemory<byte> ReadOnlyMemoryData { get; init; }
                [StringLengthVarInt]
                public string VarIntString { get; init; }
                [StringLengthBE(2)]
                public string BeString { get; init; }
                [StringLengthLE(2)]
                public string LeString { get; init; }
                [StringNullTerminated]
                public string NullTermString { get; init; }
                [StringFixedLength(8)]
                public string FixedString { get; init; }
                public byte NameLength { get; init; }
                [StringLengthFromField(nameof(NameLength))]
                public string FromFieldString { get; init; }
                public InnerHeader Nested { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "ComprehensivePacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("TryParse", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("InnerHeader.TryParse", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("VarIntZigZag", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_BitFieldStruct_GeneratesBitReader()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct BitPacket
            {
                [BinaryField(BitCount = 11)]
                public ushort Identifier { get; init; }

                [BinaryField(BitCount = 1)]
                public byte Rtr { get; init; }

                [BinaryField(BitCount = 1)]
                public byte Ide { get; init; }

                [BinaryField(BitCount = 48)]
                public ulong LargeField { get; init; }

                [BinaryField(BitCount = 4, PaddingBits = 4)]
                public byte Flags { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "BitPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("BitReader", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_RecordStructAndReordered_Generates()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial record struct RecordPacket
            {
                public U16BE Version { get; init; }
            }

            [BinaryParsable]
            public partial struct ReorderedPacket
            {
                [BinaryOrder(2)]
                public U16BE Third { get; init; }

                [BinaryOrder(0)]
                public U16BE First { get; init; }

                [BinaryOrder(1)]
                public U16BE Second { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);

        await Assert.That(GeneratorTestHelper.GetGeneratedSource(output, "RecordPacket")).IsNotNull();
        await Assert.That(GeneratorTestHelper.GetGeneratedSource(output, "ReorderedPacket")).IsNotNull();
    }

    [Test]
    public async Task Parsable_VariableThenFixed_GeneratesGroupChecks()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct VarIntThenFixed
            {
                public VarInt Count { get; init; }
                public U32BE Value { get; init; }
                public U16BE Flags { get; init; }
            }

            [BinaryParsable]
            public partial struct StringThenFixed
            {
                [StringLengthVarInt]
                public string Label { get; init; }
                public U32BE Id { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "VarIntThenFixed");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("destination.Length - offset", StringComparison.Ordinal)
            || generated.Contains("source.Length - offset", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_NonPartialStruct_SkipsGeneration()
    {
        string source = _Usings + """

            [BinaryParsable]
            public struct NotPartialPacket
            {
                public U16BE Version { get; init; }
            }
            """;

        (Compilation output, ImmutableArray<Diagnostic> diagnostics) = _RunParsable(source);

        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "NotPartialPacket")).IsFalse();
        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    #endregion

    #region BinaryParsable — diagnostics ZA2001–ZA2015

    [Test]
    [Arguments("ZA2001", """
        [BinaryParsable]
        public partial struct BadType { public System.Guid Id { get; init; } }
        """)]
    [Arguments("ZA2002", """
        [BinaryParsable]
        public partial struct BadOrder
        {
            [BinaryOrder(0)] public U16BE A { get; init; }
            public U16BE B { get; init; }
        }
        """)]
    [Arguments("ZA2003", """
        [BinaryParsable]
        public partial struct DupOrder
        {
            [BinaryOrder(0)] public U16BE A { get; init; }
            [BinaryOrder(0)] public U16BE B { get; init; }
        }
        """)]
    [Arguments("ZA2004", """
        [BinaryParsable]
        public partial struct NoFixedLen { public byte[] Data { get; init; } }
        """)]
    [Arguments("ZA2007", """
        [BinaryParsable]
        public partial struct IgnoreOrder
        {
            [BinaryOrder(0)]
            [BinaryIgnore]
            public U16BE Ignored;
            public U16BE A { get; init; }
        }
        """)]
    [Arguments("ZA2008", """
        [BinaryParsable]
        public partial struct Misaligned
        {
            [BinaryField(BitCount = 3)] public byte Flags { get; init; }
            [StringLengthVarInt] public string Name { get; init; }
        }
        """)]
    [Arguments("ZA2009", """
        [BinaryParsable]
        public partial struct NoStrLen { public string Name { get; init; } }
        """)]
    [Arguments("ZA2010", """
        [BinaryParsable]
        public partial struct BadPadding
        {
            [BinaryField(BitCount = 4, PaddingBits = 65)] public byte Flags { get; init; }
            public U16BE A { get; init; }
        }
        """)]
    [Arguments("ZA2011", """
        [BinaryParsable]
        public partial struct LenOrder
        {
            [StringLengthFromField(nameof(Length))] public string Name { get; init; }
            public byte Length { get; init; }
        }
        """)]
    [Arguments("ZA2012", """
        [BinaryParsable]
        public partial struct LenMissing
        {
            public byte Length { get; init; }
            [StringLengthFromField(nameof(Missing))] public string Name { get; init; }
        }
        """)]
    [Arguments("ZA2013", """
        [BinaryParsable]
        public partial struct NoBytesLen { public Memory<byte> Data { get; init; } }
        """)]
    [Arguments("ZA2014", """
        [BinaryParsable]
        public partial struct ZeroFixed
        {
            [BinaryFixedLength(0)] public byte[] Data { get; init; }
        }
        """)]
    [Arguments("ZA2015", """
        [BinaryParsable]
        public partial struct Conflicting
        {
            [StringLengthVarInt][StringLengthBE(2)] public string Name { get; init; }
        }
        """)]
    public async Task Parsable_Diagnostic_Reported(string expectedId, string structSource)
    {
        (Compilation output, ImmutableArray<Diagnostic> diagnostics) = _RunParsable(_Usings + "\n" + structSource);

        await Assert.That(diagnostics.Any(d => d.Id == expectedId)
            || output.GetDiagnostics().Any(d => d.Id == expectedId)).IsTrue();
    }

    #endregion

    #region BinaryWritable — success paths

    [Test]
    public async Task Writable_ComprehensiveStruct_GeneratesAllMemberKinds()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableInner
            {
                public U16BE Version { get; init; }
            }

            [BinaryWritable(DefaultEndianness = Endianness.LittleEndian)]
            public partial struct WritableComprehensive
            {
                public U16BE U16BEField { get; init; }
                public U16LE U16LEField { get; init; }
                public I32BE I32BEField { get; init; }
                public I32LE I32LEField { get; init; }
                public F32BE F32BEField { get; init; }
                public F32LE F32LEField { get; init; }
                public U64BE U64BEField { get; init; }
                public U64LE U64LEField { get; init; }
                public I64BE I64BEField { get; init; }
                public I64LE I64LEField { get; init; }
                public F64BE F64BEField { get; init; }
                public F64LE F64LEField { get; init; }
                public U128BE U128BEField { get; init; }
                public I128LE I128LEField { get; init; }
                public byte RawByte { get; init; }
                public VarInt Count { get; init; }
                public VarIntZigZag SignedCount { get; init; }
                public int PrimitiveI32 { get; init; }
                public uint PrimitiveU32 { get; init; }
                public long PrimitiveI64 { get; init; }
                public float PrimitiveF32 { get; init; }
                public double PrimitiveF64 { get; init; }
                public sbyte PrimitiveS8 { get; init; }
                [BinaryFixedLength(4)]
                public byte[] FixedData { get; init; }
                [BytesLengthVarInt]
                public byte[] DynamicData { get; init; }
                [BytesLengthBE(1)]
                public byte[] DynamicDataBE { get; init; }
                [BytesLengthLE(2)]
                public byte[] DynamicDataLE { get; init; }
                public U16BE DataLength { get; init; }
                [BytesLengthFromField(nameof(DataLength))]
                public byte[] FromFieldData { get; init; }
                [BytesLengthVarInt]
                public Memory<byte> MemoryData { get; init; }
                [StringLengthVarInt]
                public string VarIntString { get; init; }
                [StringLengthBE(2)]
                public string BeString { get; init; }
                [StringLengthLE(2)]
                public string LeString { get; init; }
                [StringNullTerminated]
                public string NullTermString { get; init; }
                [StringFixedLength(8)]
                public string FixedString { get; init; }
                public byte NameLength { get; init; }
                [StringLengthFromField(nameof(NameLength))]
                public string FromFieldString { get; init; }
                public WritableInner Nested { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableComprehensive");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("TryWrite", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("BitWriter", StringComparison.Ordinal)).IsFalse();
        await Assert.That(generated.Contains("TryGetWrittenSize", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_BitFieldStruct_GeneratesBitWriter()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableBitPacket
            {
                [BinaryField(BitCount = 11)]
                public ushort Identifier { get; init; }

                [BinaryField(BitCount = 1)]
                public byte Rtr { get; init; }

                [BinaryField(BitCount = 48)]
                public ulong LargeField { get; init; }

                [BinaryField(BitCount = 4, PaddingBits = 4)]
                public byte Flags { get; init; }

                public I16BE PrimitiveInBits { get; init; }
                public U32LE WrapperInBits { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableBitPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("BitWriter", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_VariableSize_GeneratesDynamicSizeCalculation()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableDynamic
            {
                [StringLengthVarInt]
                public string Label { get; init; }

                [BytesLengthVarInt]
                public byte[] Payload { get; init; }

                [BytesLengthVarInt]
                public Memory<byte> MemoryPayload { get; init; }

                public WritableInner Header { get; init; }
            }

            [BinaryWritable]
            public partial struct WritableInner
            {
                public U16BE Version { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableDynamic");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("total +=", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_NonPartialStruct_SkipsGeneration()
    {
        string source = _Usings + """

            [BinaryWritable]
            public struct NotPartialWritable
            {
                public U16BE Version { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);

        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "NotPartialWritable")).IsFalse();
    }

    #endregion

    #region BinaryParsable — additional coverage paths

    [Test]
    public async Task Parsable_RecordClassAttribute_IsSkipped()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial record class RecordClassPacket
            {
                public U16BE Version { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);

        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "RecordClassPacket")).IsFalse();
    }

    [Test]
    public async Task Parsable_Utf8VarMember_UsesIBinaryParsablePath()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct Utf8VarPacket
            {
                public Utf8Var Label { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "Utf8VarPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("Utf8Var.TryParse", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_BinaryIgnoreMember_ExcludesFromOutput()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct IgnoredMemberPacket
            {
                public U16BE Type { get; init; }

                [BinaryIgnore]
                public bool IsControl => Type.Value < 100;
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "IgnoredMemberPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("IsControl", StringComparison.Ordinal)).IsFalse();
    }

    [Test]
    public async Task Parsable_FixedBEStringThenFixed_GeneratesGroupedCheck()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct FixedBEStringPacket
            {
                [StringLengthBE(2)]
                public string Content { get; init; }
                public U16BE Crc { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "FixedBEStringPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("Content", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Crc", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_BitFieldWithPrimitives_GeneratesBitReaderPaths()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct BitPrimitivePacket
            {
                [BinaryField(BitCount = 11)]
                public ushort Identifier { get; init; }

                [BinaryField(BitCount = 1)]
                public byte Rtr { get; init; }

                public I16BE SignedField { get; init; }
                public U32LE WrapperField { get; init; }
                public int PrimitiveField { get; init; }
                public float FloatField { get; init; }
                public System.Half HalfField { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "BitPrimitivePacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("BitReader", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("ReadInt128", StringComparison.Ordinal)).IsFalse();
        await Assert.That(generated.Contains("PrimitiveField", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_VarIntOnly_GeneratesVarIntParsing()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct VarIntOnlyPacket
            {
                public VarInt Count { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "VarIntOnlyPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("VarInt.TryParse", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_VarIntZigZagOnly_GeneratesZigZagParsing()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct ZigZagPacket
            {
                public VarIntZigZag SignedCount { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "ZigZagPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("VarIntZigZag.TryParse", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_DynamicBytesAllEncodings_GeneratesParsingArms()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct DynamicBytesPacket
            {
                [BytesLengthVarInt]
                public byte[] VarIntData { get; init; }

                [BytesLengthBE(2)]
                public byte[] BeData { get; init; }

                [BytesLengthLE(4)]
                public byte[] LeData { get; init; }

                public byte PayloadLength { get; init; }

                [BytesLengthFromField(nameof(PayloadLength))]
                public byte[] FromFieldData { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "DynamicBytesPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("VarIntData", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("BeData", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("FromFieldData", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_DynamicMemoryEncodings_GeneratesMemoryParsing()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct DynamicMemoryPacket
            {
                [BytesLengthVarInt]
                public Memory<byte> WritableMemory { get; init; }

                [BytesLengthBE(1)]
                public ReadOnlyMemory<byte> ReadOnlyMemory { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "DynamicMemoryPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("WritableMemory", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("ReadOnlyMemory", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_AllStringEncodings_GeneratesParsingArms()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct AllStringsPacket
            {
                [StringLengthVarInt]
                public string VarIntString { get; init; }

                [StringLengthBE(2)]
                public string BeString { get; init; }

                [StringLengthLE(2)]
                public string LeString { get; init; }

                [StringNullTerminated]
                public string NullTermString { get; init; }

                [StringFixedLength(8)]
                public string FixedString { get; init; }

                public byte NameLength { get; init; }

                [StringLengthFromField(nameof(NameLength))]
                public string FromFieldString { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "AllStringsPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("NullTermString", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("FixedString", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_PrimitiveIntegers_GeneratesEndianReads()
    {
        string source = _Usings + """

            [BinaryParsable(DefaultEndianness = Endianness.LittleEndian)]
            public partial struct PrimitivePacket
            {
                public short I16 { get; init; }
                public ushort U16 { get; init; }
                public int I32 { get; init; }
                public uint U32 { get; init; }
                public long I64 { get; init; }
                public ulong U64 { get; init; }
                public float F32 { get; init; }
                public double F64 { get; init; }
                public nint NInt { get; init; }
                public nuint NUInt { get; init; }
                public System.Half HalfValue { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "PrimitivePacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("ReadInt16LittleEndian", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("ReadHalfLittleEndian", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_FixedSizeStruct_GeneratesUpfrontSizeCheck()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct FixedSizePacket
            {
                public U16BE Version { get; init; }
                public byte Flags { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "FixedSizePacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("TryGetSerializedSize", StringComparison.Ordinal)).IsTrue();
    }

    #endregion

    #region BinaryWritable — additional coverage paths

    [Test]
    public async Task Writable_RecordClassAttribute_IsSkipped()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial record class WritableRecordClass
            {
                public U16BE Version { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);

        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "WritableRecordClass")).IsFalse();
    }

    [Test]
    public async Task Writable_Utf8VarMember_ReportsNotWritable()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableUtf8VarPacket
            {
                public Utf8Var Label { get; init; }
            }
            """;

        (Compilation output, ImmutableArray<Diagnostic> diagnostics) = _RunWritable(source);

        await Assert.That(diagnostics.Any(d => d.Id == "ZA3001")).IsTrue();
        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "WritableUtf8VarPacket")).IsFalse();
    }

    [Test]
    public async Task Writable_BitFieldWithPrimitives_GeneratesBitWriterPaths()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableBitPrimitivePacket
            {
                [BinaryField(BitCount = 11)]
                public ushort Identifier { get; init; }

                [BinaryField(BitCount = 1)]
                public byte Rtr { get; init; }

                public I16BE SignedField { get; init; }
                public U32LE WrapperField { get; init; }
                public int PrimitiveField { get; init; }
                public float FloatField { get; init; }
                public System.Half HalfField { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableBitPrimitivePacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("BitWriter", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("PrimitiveField", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_VarIntAndZigZag_GeneratesWritingArms()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableVarIntPacket
            {
                public VarInt Count { get; init; }
                public VarIntZigZag SignedCount { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableVarIntPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("SignedCount.TryWrite", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_DynamicBytesAndMemory_GeneratesSizeAndWriteArms()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableDynamicBytesPacket
            {
                [BytesLengthVarInt]
                public byte[] VarIntData { get; init; }

                [BytesLengthBE(2)]
                public byte[] BeData { get; init; }

                [BytesLengthLE(4)]
                public byte[] LeData { get; init; }

                public byte PayloadLength { get; init; }

                [BytesLengthFromField(nameof(PayloadLength))]
                public byte[] FromFieldData { get; init; }

                [BytesLengthVarInt]
                public Memory<byte> MemoryData { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableDynamicBytesPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("total +=", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("MemoryData", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_AllStringEncodings_GeneratesWriteArms()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableAllStringsPacket
            {
                [StringLengthVarInt]
                public string VarIntString { get; init; }

                [StringLengthBE(2)]
                public string BeString { get; init; }

                [StringLengthLE(2)]
                public string LeString { get; init; }

                [StringNullTerminated]
                public string NullTermString { get; init; }

                [StringFixedLength(8)]
                public string FixedString { get; init; }

                public byte NameLength { get; init; }

                [StringLengthFromField(nameof(NameLength))]
                public string FromFieldString { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableAllStringsPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("NullTermString", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_PrimitiveIntegers_GeneratesEndianWrites()
    {
        string source = _Usings + """

            [BinaryWritable(DefaultEndianness = Endianness.BigEndian)]
            public partial struct WritablePrimitivePacket
            {
                public short I16 { get; init; }
                public ushort U16 { get; init; }
                public int I32 { get; init; }
                public uint U32 { get; init; }
                public long I64 { get; init; }
                public ulong U64 { get; init; }
                public float F32 { get; init; }
                public double F64 { get; init; }
                public nint NInt { get; init; }
                public nuint NUInt { get; init; }
                public System.Half HalfValue { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritablePrimitivePacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("WriteInt16BigEndian", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("WriteHalfBigEndian", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_ReorderedAndIgnored_GeneratesOrderedOutput()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableIgnoredPacket
            {
                [BinaryOrder(1)]
                public U16BE Second { get; init; }

                [BinaryOrder(0)]
                public byte First { get; init; }

                [BinaryIgnore]
                public bool IsControl => Second.Value < 100;
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableIgnoredPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("First", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_NestedStruct_GeneratesNestedWrite()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableNestedInner
            {
                public U16BE Version { get; init; }
            }

            [BinaryWritable]
            public partial struct WritableNestedOuter
            {
                public WritableNestedInner Header { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableNestedOuter");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("Header.TryWrite", StringComparison.Ordinal)).IsTrue();
    }

    #endregion

    #region BinaryWritable — diagnostics ZA3001–ZA3013

    [Test]
    [Arguments("ZA3001", """
        [BinaryWritable]
        public partial struct BadWritableType { public System.Guid Id { get; init; } }
        """)]
    [Arguments("ZA3002", """
        [BinaryWritable]
        public partial struct BadWritableOrder
        {
            [BinaryOrder(0)] public U16BE A { get; init; }
            public U16BE B { get; init; }
        }
        """)]
    [Arguments("ZA3003", """
        [BinaryWritable]
        public partial struct DupWritableOrder
        {
            [BinaryOrder(0)] public U16BE A { get; init; }
            [BinaryOrder(0)] public U16BE B { get; init; }
        }
        """)]
    [Arguments("ZA3004", """
        [BinaryWritable]
        public partial struct NoWritableFixedLen { public byte[] Data { get; init; } }
        """)]
    [Arguments("ZA3007", """
        [BinaryWritable]
        public partial struct WritableIgnoreOrder
        {
            [BinaryOrder(0)]
            [BinaryIgnore]
            public U16BE Ignored;
            public U16BE A { get; init; }
        }
        """)]
    [Arguments("ZA3008", """
        [BinaryWritable]
        public partial struct WritableMisaligned
        {
            [BinaryField(BitCount = 3)] public byte Flags { get; init; }
            [StringLengthVarInt] public string Name { get; init; }
        }
        """)]
    [Arguments("ZA3009", """
        [BinaryWritable]
        public partial struct WritableNoStrLen { public string Name { get; init; } }
        """)]
    [Arguments("ZA3010", """
        [BinaryWritable]
        public partial struct WritableBadPadding
        {
            [BinaryField(BitCount = 4, PaddingBits = 70)] public byte Flags { get; init; }
            public U16BE A { get; init; }
        }
        """)]
    [Arguments("ZA3011", """
        [BinaryWritable]
        public partial struct WritableNoBytesLen { public Memory<byte> Data { get; init; } }
        """)]
    [Arguments("ZA3012", """
        [BinaryWritable]
        public partial struct WritableZeroFixed
        {
            [StringFixedLength(0)] public string Name { get; init; }
        }
        """)]
    [Arguments("ZA3013", """
        [BinaryWritable]
        public partial struct WritableConflicting
        {
            [BytesLengthVarInt][BytesLengthBE(2)] public byte[] Data { get; init; }
        }
        """)]
    public async Task Writable_Diagnostic_Reported(string expectedId, string structSource)
    {
        (Compilation output, ImmutableArray<Diagnostic> diagnostics) = _RunWritable(_Usings + "\n" + structSource);

        await Assert.That(diagnostics.Any(d => d.Id == expectedId)
            || output.GetDiagnostics().Any(d => d.Id == expectedId)).IsTrue();
    }

    #endregion

    private static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics) _RunParsable(string source)
    {
        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        return GeneratorTestHelper.RunGenerator(compilation, new BinaryParsableGenerator());
    }

    private static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics) _RunWritable(string source)
    {
        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        return GeneratorTestHelper.RunGenerator(compilation, new BinaryWritableGenerator());
    }
}
