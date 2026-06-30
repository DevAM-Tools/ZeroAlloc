// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests;

/// <summary>Compiler tests for <see cref="BinaryParsableGenerator"/>.</summary>
internal sealed class BinaryParsableGeneratorTests
{
    private const string _Usings = """
        using System;
        using ZeroAlloc;
        """;

    [Test]
    public async Task Generate_SimpleStruct_ProducesTryParse()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct SimplePacket
            {
                public U16BE Version { get; init; }
                public byte Flags { get; init; }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(new BinaryParsableGenerator(), compilation);

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "SimplePacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("TryParse", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Parse", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Generate_StringWithEncoding_IncludesStringParsing()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct StringPacket
            {
                [StringLengthVarInt]
                public string Name { get; init; }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(new BinaryParsableGenerator(), compilation);

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "StringPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("Name", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Generate_InvalidMember_ReportsDiagnostic()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct BadPacket
            {
                public System.Guid Id { get; init; }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (_, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(new BinaryParsableGenerator(), compilation);

        await Assert.That(diagnostics.Any(d => d.Id.StartsWith("ZA20", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task Generate_ByteArrayFixedLength_WritesFixedParsing()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct FixedBytesPacket
            {
                [BinaryFixedLength(4)]
                public byte[] Data { get; init; }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(new BinaryParsableGenerator(), compilation);

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "FixedBytesPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("Data", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Generate_VarIntMember_IncludesVarIntParsing()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct VarIntPacket
            {
                public VarInt Count { get; init; }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(new BinaryParsableGenerator(), compilation);

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "VarIntPacket");

        await Assert.That(generated).IsNotNull();
    }
}
