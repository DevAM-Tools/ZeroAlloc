// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests.Helpers;

/// <summary>Roslyn compilation helpers for generator tests.</summary>
internal static class GeneratorTestHelper
{
    /// <summary>Creates a C# compilation with ZeroAlloc reference assemblies.</summary>
    public static CSharpCompilation CreateCompilation(string source, string assemblyName = "GeneratorTests")
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: "Test.cs");
        MetadataReference[] references = _GetReferences();
        return CSharpCompilation.Create(
            assemblyName,
            [tree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>Runs one or more incremental generators and returns the updated compilation.</summary>
    public static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(
        CSharpCompilation compilation,
        params IIncrementalGenerator[] generators)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generators);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out Compilation output,
            out ImmutableArray<Diagnostic> compilationDiagnostics);

        ImmutableArray<Diagnostic> generatorDiagnostics = driver.GetRunResult().Results
            .SelectMany(static result => result.Diagnostics)
            .ToImmutableArray();

        ImmutableArray<Diagnostic> mergedDiagnostics = generatorDiagnostics
            .Concat(compilationDiagnostics.Where(static d => d.Id.StartsWith("ZA", StringComparison.Ordinal)))
            .GroupBy(static d => (d.Id, d.Location.SourceSpan.Start))
            .Select(static g => g.First())
            .ToImmutableArray();

        if (mergedDiagnostics.Length > 0)
        {
            return (output, mergedDiagnostics);
        }

        return (output, compilationDiagnostics);
    }

    /// <summary>Runs a single incremental generator.</summary>
    public static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(
        IIncrementalGenerator generator,
        CSharpCompilation compilation)
        => RunGenerator(compilation, generator);

    /// <summary>Gets generated source text for the first generated syntax tree matching the filter.</summary>
    public static string? GetGeneratedSource(Compilation compilation, string hintNameContains)
    {
        foreach (SyntaxTree tree in compilation.SyntaxTrees.Where(static t => !_IsUserSourceTree(t)))
        {
            string text = tree.GetText().ToString();
            if (tree.FilePath.Contains(hintNameContains, StringComparison.Ordinal)
                || text.Contains($"partial struct {hintNameContains}", StringComparison.Ordinal)
                || text.Contains($"partial record struct {hintNameContains}", StringComparison.Ordinal)
                || text.Contains($"partial class {hintNameContains}", StringComparison.Ordinal))
            {
                return text;
            }
        }

        SyntaxTree[] generatedTrees = compilation.SyntaxTrees
            .Where(static t => !_IsUserSourceTree(t))
            .ToArray();

        return generatedTrees.Length > 0
            ? generatedTrees[^1].GetText().ToString()
            : null;
    }

    /// <summary>Returns true when the compilation contains generated output for the type name.</summary>
    public static bool HasGeneratedSource(Compilation compilation, string typeName)
    {
        foreach (SyntaxTree tree in compilation.SyntaxTrees.Where(static t => !_IsUserSourceTree(t)))
        {
            string text = tree.GetText().ToString();
            if (tree.FilePath.Contains(typeName, StringComparison.Ordinal)
                || text.Contains($"partial struct {typeName}", StringComparison.Ordinal)
                || text.Contains($"partial record struct {typeName}", StringComparison.Ordinal)
                || text.Contains($"partial class {typeName}", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool _IsUserSourceTree(SyntaxTree tree)
        => string.Equals(tree.FilePath, "Test.cs", StringComparison.OrdinalIgnoreCase);

    private static MetadataReference[] _GetReferences()
    {
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase)
        {
            typeof(object).Assembly.Location,
            typeof(ZeroAlloc.ZeroAllocBase).Assembly.Location,
        };

        string? trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (!string.IsNullOrEmpty(trusted))
        {
            foreach (string path in trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    paths.Add(path);
                }
            }
        }
        else
        {
            string[] fallbackAssemblies =
            [
                typeof(Attribute).Assembly.Location,
                typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location,
                typeof(System.Buffers.Binary.BinaryPrimitives).Assembly.Location,
                typeof(System.Collections.Generic.IEnumerable<>).Assembly.Location,
                typeof(System.ComponentModel.DescriptionAttribute).Assembly.Location,
                typeof(System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute).Assembly.Location,
                typeof(System.Globalization.CultureInfo).Assembly.Location,
                typeof(System.IO.Stream).Assembly.Location,
                typeof(System.Linq.Enumerable).Assembly.Location,
                typeof(System.Memory<>).Assembly.Location,
                typeof(System.Numerics.BitOperations).Assembly.Location,
                typeof(System.Runtime.CompilerServices.MethodImplAttribute).Assembly.Location,
                typeof(System.Text.Encoding).Assembly.Location,
                typeof(System.Threading.Tasks.Task).Assembly.Location,
            ];

            foreach (string path in fallbackAssemblies)
            {
                paths.Add(path);
            }
        }

        return paths.Select(static path => MetadataReference.CreateFromFile(path)).ToArray();
    }
}
