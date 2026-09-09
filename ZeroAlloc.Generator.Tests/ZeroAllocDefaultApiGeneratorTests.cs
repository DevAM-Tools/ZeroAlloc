// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests;

/// <summary>Tests for the generated default <c>ZeroAlloc.ZA</c> API class.</summary>
internal sealed class ZeroAllocDefaultApiGeneratorTests
{
    [Test]
    public async Task Generate_NoHandwrittenSubclass_BindsDefaultZaString()
    {
        const string source = """
            using ZeroAlloc;

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString s = ZA.String("x=", 1);
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());

        await Assert.That(output.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error)).IsFalse();
        await Assert.That(GeneratorTestHelper.HasGeneratedHint(output, "ZeroAlloc.ZA.Stub.g.cs")).IsTrue();

        INamedTypeSymbol? za = output.GetTypeByMetadataName("ZeroAlloc.ZA");
        await Assert.That(za).IsNotNull();
        bool hasTypedString = za!.GetMembers("String").OfType<IMethodSymbol>().Any(static method =>
            method.Parameters.Length == 2
            && method.Parameters[0].Type.SpecialType == SpecialType.System_String
            && method.Parameters[1].Type.SpecialType == SpecialType.System_Int32);
        await Assert.That(hasTypedString).IsTrue();
    }

    [Test]
    public async Task Generate_ExtraSubclass_EmitsDefaultZaAndExtraApi()
    {
        const string source = """
            using ZeroAlloc;

            internal sealed partial class ExtraApi : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString a = ZA.String("a=", 1);
                    using TempString b = ExtraApi.String("b=", 2);
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());

        await Assert.That(output.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error)).IsFalse();
        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "ExtraApi")).IsTrue();
        await Assert.That(GeneratorTestHelper.HasGeneratedHint(output, "ZeroAlloc.ZA.ZeroAlloc.g.cs")).IsTrue();
        await Assert.That(GeneratorTestHelper.HasGeneratedHint(output, "ExtraApi.ZeroAlloc.g.cs")).IsTrue();
    }

    [Test]
    public async Task Generate_TwoClassesNamedZa_EmitsUniqueHints()
    {
        const string source = """
            using ZeroAlloc;

            namespace Other
            {
                internal sealed partial class ZA : ZeroAllocBase { }
            }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString d = ZA.String("d=", 2);
                    using TempString o = Other.ZA.String("o=", 1);
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());

        await Assert.That(output.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error)).IsFalse();
        await Assert.That(GeneratorTestHelper.HasGeneratedHint(output, "ZeroAlloc.ZA.ZeroAlloc.g.cs")).IsTrue();
        await Assert.That(GeneratorTestHelper.HasGeneratedHint(output, "Other.ZA.ZeroAlloc.g.cs")).IsTrue();
    }

    [Test]
    public async Task Generate_SuppressSymbol_DoesNotBindDefaultZa()
    {
        const string source = """
            using ZeroAlloc;

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString s = ZA.String("x");
                }
            }
            """;

        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols("ZEROALLOC_SUPPRESS_DEFAULT_ZA");
        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source, parseOptions: parseOptions);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, parseOptions, new ZeroAllocGenerator());

        await Assert.That(output.GetTypeByMetadataName("ZeroAlloc.ZA")).IsNull();
        await Assert.That(output.GetDiagnostics().Any(static d => d.Id == "CS0103")).IsTrue();
    }

    [Test]
    public async Task Generate_SuppressSymbol_StillEmitsExtraSubclass()
    {
        const string source = """
            using ZeroAlloc;

            internal sealed partial class FullApi : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString s = FullApi.String("x=", 1);
                }
            }
            """;

        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols("ZEROALLOC_SUPPRESS_DEFAULT_ZA");
        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source, parseOptions: parseOptions);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, parseOptions, new ZeroAllocGenerator());

        await Assert.That(output.GetTypeByMetadataName("ZeroAlloc.ZA")).IsNull();
        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "FullApi")).IsTrue();
        await Assert.That(output.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error)).IsFalse();
    }

    [Test]
    public async Task ZeroAllocAssembly_DoesNotDefineDefaultZa()
    {
        bool hasZa = typeof(ZeroAlloc.ZeroAllocBase).Assembly.GetTypes().Any(static t => t.FullName == "ZeroAlloc.ZA");

        await Assert.That(hasZa).IsFalse();
    }

    [Test]
    public async Task Generate_UnsealedHandwrittenZa_MergesWithDefaultStub()
    {
        const string source = """
            using ZeroAlloc;

            namespace ZeroAlloc
            {
                internal partial class ZA : ZeroAllocBase { }
            }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString s = ZA.String("x=", 1);
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());

        await Assert.That(output.GetDiagnostics().Any(static d => d.Id == "CS0267")).IsFalse();
        await Assert.That(output.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error)).IsFalse();
        await Assert.That(GeneratorTestHelper.HasGeneratedHint(output, "ZeroAlloc.ZA.Stub.g.cs")).IsTrue();

        INamedTypeSymbol? za = output.GetTypeByMetadataName("ZeroAlloc.ZA");
        await Assert.That(za).IsNotNull();
        await Assert.That(za!.IsSealed).IsFalse();
        bool hasTypedString = za.GetMembers("String").OfType<IMethodSymbol>().Any(static method =>
            method.Parameters.Length == 2
            && method.Parameters[0].Type.SpecialType == SpecialType.System_String
            && method.Parameters[1].Type.SpecialType == SpecialType.System_Int32);
        await Assert.That(hasTypedString).IsTrue();
    }

    [Test]
    public async Task Generate_ConsumerNamespaceLeftoverZa_IsSecondType()
    {
        const string source = """
            using ZeroAlloc;

            namespace MyApp
            {
                internal partial class ZA : ZeroAllocBase { }

                internal static class Usage
                {
                    internal static void Call()
                    {
                        using TempString s = ZA.String("x=", 1);
                    }
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());

        await Assert.That(output.GetDiagnostics().Any(static d => d.Id == "CS0267")).IsFalse();
        await Assert.That(output.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error)).IsFalse();
        await Assert.That(GeneratorTestHelper.HasGeneratedHint(output, "ZeroAlloc.ZA.Stub.g.cs")).IsTrue();
        await Assert.That(output.GetTypeByMetadataName("ZeroAlloc.ZA")).IsNotNull();
        await Assert.That(output.GetTypeByMetadataName("MyApp.ZA")).IsNotNull();
        await Assert.That(output.GetTypeByMetadataName("ZeroAlloc.ZA")!.IsSealed).IsFalse();
    }

    [Test]
    public async Task Generate_GlobalLeftoverZa_IsSecondType()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class ZA : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString s = ZA.String("x=", 1);
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());

        await Assert.That(output.GetDiagnostics().Any(static d => d.Id == "CS0267")).IsFalse();
        await Assert.That(output.GetDiagnostics().Any(static d => d.Severity == DiagnosticSeverity.Error)).IsFalse();
        await Assert.That(GeneratorTestHelper.HasGeneratedHint(output, "ZeroAlloc.ZA.Stub.g.cs")).IsTrue();
        await Assert.That(output.GetTypeByMetadataName("ZeroAlloc.ZA")).IsNotNull();
        await Assert.That(output.GetTypeByMetadataName("ZA")).IsNotNull();
        await Assert.That(output.GetTypeByMetadataName("ZeroAlloc.ZA")!.IsSealed).IsFalse();
        await Assert.That(output.GetTypeByMetadataName("ZA")!.ContainingNamespace.IsGlobalNamespace).IsTrue();
    }

    [Test]
    public async Task Generate_UnchangedCompilation_RecomputesIdenticalSources()
    {
        const string source = """
            using ZeroAlloc;

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString s = ZA.String("x=", 1);
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new ZeroAllocGenerator());
        driver = driver.RunGenerators(compilation);
        string[] first = driver.GetRunResult().GeneratedTrees.Select(static t => t.GetText().ToString()).ToArray();
        driver = driver.RunGenerators(compilation);
        string[] second = driver.GetRunResult().GeneratedTrees.Select(static t => t.GetText().ToString()).ToArray();

        await Assert.That(second).IsEquivalentTo(first);
    }

    [Test]
    public async Task GeneratorTargets_ContainsOptOutPropertyAndSymbol()
    {
        string repoRoot = _FindRepoRoot();
        string targetsPath = Path.Combine(repoRoot, "ZeroAlloc.Generator", "build", "ZeroAlloc.Generator.targets");
        string text = await File.ReadAllTextAsync(targetsPath);

        await Assert.That(text.Contains("ZeroAlloc_GenerateDefaultClass", StringComparison.Ordinal)).IsTrue();
        await Assert.That(text.Contains("ZEROALLOC_SUPPRESS_DEFAULT_ZA", StringComparison.Ordinal)).IsTrue();
        await Assert.That(text.Contains("Condition=\"'$(ZeroAlloc_GenerateDefaultClass)' != 'true'\"", StringComparison.Ordinal)).IsTrue();
        await Assert.That(text.Contains("<DefineConstants>$(DefineConstants);ZEROALLOC_SUPPRESS_DEFAULT_ZA</DefineConstants>", StringComparison.Ordinal)).IsTrue();

        string packPath = Path.Combine(repoRoot, "build", "ZeroAlloc.Pack.targets");
        string packText = await File.ReadAllTextAsync(packPath);
        await Assert.That(packText.Contains("PackagePath=\"buildTransitive/ZeroAlloc.targets\"", StringComparison.Ordinal)).IsTrue();

        string directoryTargetsPath = Path.Combine(repoRoot, "Directory.Build.targets");
        string directoryTargetsText = await File.ReadAllTextAsync(directoryTargetsPath);
        await Assert.That(directoryTargetsText.Contains("ZeroAlloc.Generator.targets", StringComparison.Ordinal)).IsTrue();
    }

    /// <summary>Walks from <see cref="AppContext.BaseDirectory"/> to the repository root that contains <c>ZeroAlloc.slnx</c>.</summary>
    private static string _FindRepoRoot()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ZeroAlloc.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("ZeroAlloc.slnx was not found from AppContext.BaseDirectory.");
    }
}
