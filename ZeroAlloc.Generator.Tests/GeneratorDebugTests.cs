// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests;

/// <summary>Debug harness for generator behavior (kept for regression probes).</summary>
internal sealed class GeneratorDebugTests
{
    private const string _Usings = """
        using ZeroAlloc;
        """;

    [Test]
    public async Task Debug_BadOrder_PrintsDiagnostics()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct BadOrder
            {
                [BinaryOrder(0)] public U16BE A { get; init; }
                public U16BE B { get; init; }
            }
            """;

        (Compilation output, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(
            GeneratorTestHelper.CreateCompilation(source),
            new BinaryParsableGenerator());

        string ids = string.Join(", ", diagnostics.Select(static d => d.Id));
        string generated = string.Join(
            "\n",
            output.SyntaxTrees
                .Where(static t => !string.Equals(t.FilePath, "Test.cs", StringComparison.OrdinalIgnoreCase))
                .Select(static t => t.FilePath));

        await Assert.That(ids).Contains("ZA2002");
        await Assert.That(generated).IsNotNull();
    }

    [Test]
    public async Task Debug_BitPacket_GeneratesBitReader()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct BitPacket
            {
                [BinaryField(BitCount = 11)]
                public ushort Identifier { get; init; }

                [BinaryField(BitCount = 1)]
                public byte Rtr { get; init; }
            }
            """;

        (Compilation output, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(
            GeneratorTestHelper.CreateCompilation(source),
            new BinaryParsableGenerator());

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "BitPacket");
        string ids = string.Join(", ", diagnostics.Select(static d => d.Id));

        await Assert.That(ids).IsEqualTo(string.Empty);
        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("BitReader", StringComparison.Ordinal)).IsTrue();
    }
}
