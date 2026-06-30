// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests;

/// <summary>Compiler tests for <see cref="BinaryWritableGenerator"/>.</summary>
internal sealed class BinaryWritableGeneratorTests
{
    private const string _Usings = """
        using System;
        using ZeroAlloc;
        """;

    [Test]
    public async Task Generate_SimpleStruct_ProducesTryWrite()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritablePacket
            {
                public U16BE Version { get; init; }
                public byte Flags { get; init; }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(new BinaryWritableGenerator(), compilation);

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritablePacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("TryWrite", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("IBinarySerializable", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Generate_InvalidMember_ReportsDiagnostic()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct BadWritable
            {
                public System.Guid Id { get; init; }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (_, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(new BinaryWritableGenerator(), compilation);

        await Assert.That(diagnostics.Any(d => d.Id.StartsWith("ZA30", StringComparison.Ordinal))).IsTrue();
    }

    [Test]
    public async Task Generate_ExplicitOrder_GeneratesOrderedWrite()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct OrderedPacket
            {
                [BinaryOrder(1)]
                public U16BE Second { get; init; }

                [BinaryOrder(0)]
                public byte First { get; init; }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(new BinaryWritableGenerator(), compilation);

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "OrderedPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("First", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Second", StringComparison.Ordinal)).IsTrue();
    }
}
