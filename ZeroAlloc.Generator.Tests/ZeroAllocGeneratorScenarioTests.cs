// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests;

/// <summary>Scenario tests exercising <see cref="ZeroAllocGenerator"/> code paths and diagnostics.</summary>
internal sealed class ZeroAllocGeneratorScenarioTests
{
    [Test]
    public async Task Generate_FullFormattingSurface_EmitsAllStrategyComments()
    {
        const string source = """
            #nullable disable
            using System;
            using System.Globalization;
            using ZeroAlloc;

            internal partial class FormatSurfaceApi : ZeroAllocBase { }

            internal readonly struct SizedFormattable : IStringSize, ISpanFormattable
            {
                public static int StringSize => 4;
                public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
                {
                    "abcd".AsSpan().CopyTo(destination);
                    charsWritten = 4;
                    return true;
                }
            }

            internal readonly struct Utf8Formattable : IUtf8Size, IUtf8SpanFormattable
            {
                public static int Utf8Size => 3;
                public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider provider)
                {
                    destination[0] = (byte)'x';
                    destination[1] = (byte)'y';
                    destination[2] = (byte)'z';
                    bytesWritten = 3;
                    return true;
                }
            }

            internal sealed class UnformattableType { public override string ToString() => "obj"; }

            internal struct RecursiveFormatter : ISpanFormattable
            {
                public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
                {
                    using TempString inner = FormatSurfaceApi.String("inner");
                    charsWritten = Math.Min(destination.Length, inner.Length);
                    inner.AsSpan().CopyTo(destination);
                    return true;
                }
            }

            internal partial class RecursiveApi : ZeroAllocBase { }

            internal static class Usage
            {
                private static readonly IFormatProvider _Provider = CultureInfo.InvariantCulture;

                internal static void Call()
                {
                    using TempString s1 = FormatSurfaceApi.String("a=", 'b', true, (sbyte)1, (byte)2, (short)3, (ushort)4, 5, 5u, 6L, 6ul, 7f, 8d, (decimal)9);
                    using TempString s2 = FormatSurfaceApi.String(CultureInfo.InvariantCulture, "c=", 10);
                    using TempString s3 = FormatSurfaceApi.String(_Provider, new SizedFormattable(), new Utf8Formattable());
                    FormatSurfaceApi.TryString(out TempString ts, "t=", 11);
                    using TempBytes u1 = FormatSurfaceApi.Utf8("u=", 12);
                    FormatSurfaceApi.TryUtf8(new byte[64], out int uw, "u2=", 13);
                    using TempBytes b1 = FormatSurfaceApi.Bytes(new U16BE(1), new VarInt(2), new VarIntZigZag(-1));
                    FormatSurfaceApi.TryBytes(new byte[64], out int bw, new U32BE(3));
                    using TempString ls = FormatSurfaceApi.LocalizedString(_Provider, "l=", 14);
                    FormatSurfaceApi.TryLocalizedString(_Provider, out TempString tls, "tl=", 15);
                    using TempBytes lu = FormatSurfaceApi.LocalizedUtf8(_Provider, "lu=", 16);
                    FormatSurfaceApi.TryLocalizedUtf8(_Provider, new byte[64], out int luw, "tlu=", 17);
                    LazyString lazy = FormatSurfaceApi.Lazy("lazy=", 18);
                    LazyString lazyMulti = FormatSurfaceApi.Lazy("a=", 19, "b=", 20);
                    LazyString lazyInterp = FormatSurfaceApi.LazyInterpolated("interp=", 21);
                    LazyString lazyInterpMulti = FormatSurfaceApi.LazyInterpolated("a=", 22, "b=", 23);
                    FormatSurfaceApi.String(new UnformattableType());
                    FormatSurfaceApi.String(new RecursiveFormatter());
                    using TempBytes raw = FormatSurfaceApi.Bytes(new Raw(new byte[] { 1, 2, 3 }));
                    FormatSurfaceApi.LocalizedString("missing-provider");
                    FormatSurfaceApi.LocalizedUtf8("missing-provider");
                    using TempString nested = RecursiveApi.String("nested");
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(
            compilation,
            new ZeroAllocGenerator());

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "FormatSurfaceApi");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("Direct copy (string.AsSpan)", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Single character write", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Literal \"True\"/\"False\" copy", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("ISpanFormattable.TryFormat", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("IUtf8SpanFormattable.TryFormat", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("IBinarySerializable.TryWrite", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Raw bytes (direct copy)", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Format provider (not written)", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Fallback to ToString() (allocates!)", StringComparison.Ordinal)).IsTrue();
        await Assert.That(diagnostics.Any(d => d.Id == "ZA1002")).IsTrue();
        await Assert.That(diagnostics.Any(d => d.Id == "ZA1004")).IsTrue();
        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "RecursiveApi")).IsTrue();
    }

    [Test]
    public async Task Generate_InvalidCallTargets_AreIgnored()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class RealApi : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    RealApi.String("ok");
                    NotApi.String("skip");
                    RealApi.String("ok2");
                }
            }

            internal static class NotApi
            {
                internal static string String(string s) => s;
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());

        await Assert.That(GeneratorTestHelper.GetGeneratedSource(output, "RealApi")).IsNotNull();
        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "NotApi")).IsFalse();
    }

    [Test]
    public async Task Generate_ClassWithoutZeroAllocBase_IsIgnored()
    {
        const string source = """
            internal partial class PlainClass { }

            internal static class Usage
            {
                internal static void Call() => PlainClass.ToString();
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(
            compilation,
            new ZeroAllocGenerator());

        await Assert.That(output.SyntaxTrees.Count()).IsEqualTo(1);
        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    [Test]
    public async Task Generate_ClassWithoutBaseList_IsIgnored()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class NoBaseApi { }

            internal static class Usage
            {
                internal static void Call()
                {
                    NoBaseApi.String("x");
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());

        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "NoBaseApi")).IsFalse();
    }

    [Test]
    public async Task Generate_WrongNamespaceBase_IsIgnored()
    {
        const string source = """
            namespace FakeZeroAlloc;

            internal class ZeroAllocBase { }

            internal partial class FakeApi : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    FakeApi.String("x");
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());

        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "FakeApi")).IsFalse();
    }

    [Test]
    public async Task Generate_ZeroAllocBaseSubclass_ReportsRecursionRisk()
    {
        const string source = """
            using System;
            using ZeroAlloc;

            internal partial class OuterApi : ZeroAllocBase { }

            internal partial class InnerFormatter : ZeroAllocBase, ISpanFormattable
            {
                public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
                {
                    "x".AsSpan().CopyTo(destination);
                    charsWritten = 1;
                    return true;
                }
            }

            internal static class Usage
            {
                internal static void Call()
                {
                    OuterApi.String(new InnerFormatter());
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (_, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());

        await Assert.That(diagnostics.Any(d => d.Id == "ZA1002")).IsTrue();
    }

    [Test]
    public async Task Generate_MultipleSignaturesPerClass_DeduplicatesMethods()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class MultiSigApi : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString a = MultiSigApi.String("a=", 1);
                    using TempString b = MultiSigApi.String("b=", 2);
                    using TempBytes c = MultiSigApi.Utf8("c=", 3);
                    using TempBytes d = MultiSigApi.Utf8("d=", 4);
                    using TempBytes e = MultiSigApi.Bytes(new U16BE(1));
                    using TempBytes f = MultiSigApi.Bytes(new U32BE(2));
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "MultiSigApi");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Split("internal static TempString String(", StringSplitOptions.None).Length).IsEqualTo(2);
        await Assert.That(generated.Split("internal static TempBytes Utf8(", StringSplitOptions.None).Length).IsEqualTo(2);
    }

    [Test]
    public async Task Generate_FormattedGeneric_ParamDocsUseEscapedCodeTags()
    {
        const string source = """
            using System.Globalization;
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    Formatted<double> formatted = new(1.5, "F2", CultureInfo.InvariantCulture);
                    Api.String(formatted);
                    Utf8Formatted<int> utf8 = new(42, "D5", CultureInfo.InvariantCulture);
                    Api.Utf8(utf8);
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(
            compilation,
            new ZeroAllocGenerator());
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "Api");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!).Contains("Formatted&lt;double&gt;");
        await Assert.That(generated).Contains("Utf8Formatted&lt;int&gt;");
        await Assert.That(generated).DoesNotContain("cref=\"ZeroAlloc.Formatted<double>");

        string[] xmlDocErrors = output.GetDiagnostics()
            .Where(static d => d.Severity == DiagnosticSeverity.Error
                && d.Id is "CS1570" or "CS1584" or "CS1658")
            .Select(static d => d.ToString())
            .ToArray();

        await Assert.That(xmlDocErrors).IsEmpty();
        await Assert.That(diagnostics.Length).IsEqualTo(0);
    }

    [Test]
    public async Task ArgumentListComparer_DistinguishesSignatures()
    {
        ArgumentListComparer comparer = new();
        ImmutableArray<ArgumentTypeInfo> one = ImmutableArray.Create(
            new ArgumentTypeInfo("System.String", "s", TypeCategory.String, false, false));
        ImmutableArray<ArgumentTypeInfo> two = ImmutableArray.Create(
            new ArgumentTypeInfo("System.String", "s", TypeCategory.String, false, false),
            new ArgumentTypeInfo("System.Int32", "i", TypeCategory.SpanFormattable, false, true));
        ImmutableArray<ArgumentTypeInfo> different = ImmutableArray.Create(
            new ArgumentTypeInfo("System.Int32", "i", TypeCategory.SpanFormattable, false, true));

        await Assert.That(comparer.Equals(one, two)).IsFalse();
        await Assert.That(comparer.Equals(one, different)).IsFalse();
        await Assert.That(comparer.Equals(one, one)).IsTrue();
        await Assert.That(comparer.GetHashCode(one)).IsNotEqualTo(comparer.GetHashCode(two));
    }
}
