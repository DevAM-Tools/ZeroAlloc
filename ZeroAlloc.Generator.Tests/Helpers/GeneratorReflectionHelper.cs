// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests.Helpers;

/// <summary>Reflection helpers for exercising generator internals in coverage tests.</summary>
internal static class GeneratorReflectionHelper
{
    private static readonly Assembly _GeneratorAssembly = typeof(BinaryParsableGenerator).Assembly;
    private static readonly object _SyntaxHelper = _ResolveSyntaxHelper();

    /// <summary>Invokes a private static method on a generator type.</summary>
    public static object? InvokePrivateStatic(Type generatorType, string methodName, params object?[] args)
    {
        MethodInfo? method = generatorType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Method '{methodName}' was not found on {generatorType.Name}.");

        return method.Invoke(null, args);
    }

    /// <summary>Calls <c>CommentHelper.GetFormattingExplanation</c> for the given category.</summary>
    public static string GetFormattingExplanation(TypeCategory category)
    {
        Type commentHelper = _GeneratorAssembly
            .GetTypes()
            .Single(static t => t.Name.Contains("CommentHelper", StringComparison.Ordinal));

        MethodInfo? method = commentHelper.GetMethod(
            "GetFormattingExplanation",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("GetFormattingExplanation was not found.");

        return (string)method.Invoke(null, [category])!;
    }

    /// <summary>Builds a default attribute context whose target symbol is unset.</summary>
    public static GeneratorAttributeSyntaxContext CreateDefaultAttributeContext() => default;

    /// <summary>Builds a <see cref="GeneratorSyntaxContext"/> for a partial class without a base list.</summary>
    public static GeneratorSyntaxContext CreateClassWithoutBaseListContext()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            """
            internal partial class NoBaseList { }
            """);

        ClassDeclarationSyntax classDecl = tree
            .GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();

        CSharpCompilation compilation = CSharpCompilation
            .Create("NoBaseList", [tree], GeneratorTestHelper.CreateCompilation("class X { }").References);

        SemanticModel semanticModel = compilation.GetSemanticModel(tree);

        return _CreateGeneratorSyntaxContext(classDecl, semanticModel);
    }

    /// <summary>Builds a <see cref="GeneratorSyntaxContext"/> for an invocation with an untyped null argument.</summary>
    public static GeneratorSyntaxContext CreateUnboundArgumentContext()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call() => Api.String(null);
            }
            """;

        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "UnboundArgument",
            [tree],
            GeneratorTestHelper.CreateCompilation("class X { }").References);

        InvocationExpressionSyntax invocation = tree
            .GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .First(static n => n.Expression.ToString().Contains("Api.String", StringComparison.Ordinal));

        SemanticModel semanticModel = compilation.GetSemanticModel(tree);

        return _CreateGeneratorSyntaxContext(invocation, semanticModel);
    }

    /// <summary>Method names mirrored from <c>ZeroAllocGenerator</c> recursion detection.</summary>
    public static HashSet<string> CreateZeroAllocMethodNames() =>
    [
        "String", "TryString",
        "Utf8", "TryUtf8",
        "Bytes", "TryBytes",
        "Acquire", "Release",
    ];

    /// <summary>Type names mirrored from <c>ZeroAllocGenerator</c> recursion detection.</summary>
    public static HashSet<string> CreateZeroAllocTypeNames() =>
    [
        "TempString", "TempBytes",
        "TempStringBuilder", "TempBytesBuilder",
        "ZeroStringBuilder", "ZeroBytesBuilder",
        "StackStringBuilder", "StackBytesBuilder",
    ];

    private static GeneratorSyntaxContext _CreateGeneratorSyntaxContext(SyntaxNode node, SemanticModel semanticModel)
    {
        Lazy<SemanticModel> lazyModel = new(() => semanticModel);

        ConstructorInfo ctor = typeof(GeneratorSyntaxContext).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Single();

        return (GeneratorSyntaxContext)ctor.Invoke([node, lazyModel, _SyntaxHelper]);
    }

    private static object _ResolveSyntaxHelper()
    {
        Type? helperType = typeof(CSharpCompilation).Assembly.GetType("Microsoft.CodeAnalysis.CSharp.CSharpSyntaxHelper");

        FieldInfo? instanceField = helperType?.GetField(
            "Instance",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        if (instanceField is not null)
        {
            return instanceField.GetValue(null)
                ?? throw new InvalidOperationException("CSharpSyntaxHelper.Instance field was null.");
        }

        PropertyInfo? instanceProperty = helperType?.GetProperty(
            "Instance",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        return instanceProperty?.GetValue(null)
            ?? throw new InvalidOperationException("CSharpSyntaxHelper.Instance was not found.");
    }
}
