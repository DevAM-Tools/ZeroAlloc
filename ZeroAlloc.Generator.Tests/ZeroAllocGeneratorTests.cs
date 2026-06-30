// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests;

/// <summary>Compiler tests for <see cref="ZeroAllocGenerator"/>.</summary>
internal sealed class ZeroAllocGeneratorTests
{
    [Test]
    public async Task Generate_FullApiSurface_AddsAllMethodFamilies()
    {
        const string source = """
            #nullable disable
            using System;
            using System.Globalization;
            using ZeroAlloc;

            internal partial class FullApi : ZeroAllocBase { }

            internal static class Usage
            {
                private static readonly IFormatProvider _Provider = CultureInfo.InvariantCulture;

                internal static void Call()
                {
                    using TempString s1 = FullApi.String("a=", 'b', true, (sbyte)1, (byte)2, (short)3, (ushort)4, 5, 5u, 6L, 6ul, 7f, 8d, (decimal)9);
                    using TempString s2 = FullApi.String(CultureInfo.InvariantCulture, "c=", 10);
                    FullApi.TryString(out TempString ts, "t=", 11);
                    using TempBytes u1 = FullApi.Utf8("u=", 12);
                    FullApi.TryUtf8(new byte[64], out int uw, "u2=", 13);
                    using TempBytes b1 = FullApi.Bytes(new U16BE(1), new VarInt(2), new VarIntZigZag(-1));
                    FullApi.TryBytes(new byte[64], out int bw, new U32BE(3));
                    using TempString ls = FullApi.LocalizedString(_Provider, "l=", 14);
                    FullApi.TryLocalizedString(_Provider, out TempString tls, "tl=", 15);
                    using TempBytes lu = FullApi.LocalizedUtf8(_Provider, "lu=", 16);
                    FullApi.TryLocalizedUtf8(_Provider, new byte[64], out int luw, "tlu=", 17);
                    LazyString lazy = FullApi.Lazy("lazy=", 18);
                    LazyString lazyMulti = FullApi.Lazy("a=", 19, "b=", 20);
                    LazyString lazyInterp = FullApi.LazyInterpolated("interp=", 21);
                    LazyString lazyInterpMulti = FullApi.LazyInterpolated("a=", 22, "b=", 23);
                    FullApi.String(new UnformattableType());
                    FullApi.String(new RecursiveFormatter());
                }
            }

            internal sealed class UnformattableType { public override string ToString() => "x"; }

            internal struct RecursiveFormatter : ISpanFormattable
            {
                public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
                {
                    using TempString inner = FullApi.String("inner");
                    charsWritten = Math.Min(destination.Length, inner.Length);
                    inner.AsSpan().CopyTo(destination);
                    return true;
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(
            compilation,
            new ZeroAllocGenerator());

        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "FullApi");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("String(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("TryString(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Utf8(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("TryUtf8(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Bytes(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("TryBytes(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("LocalizedString(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("TryLocalizedString(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("LocalizedUtf8(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("TryLocalizedUtf8(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Lazy(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("LazyInterpolated(", StringComparison.Ordinal)).IsTrue();
        await Assert.That(diagnostics.Any(d => d.Id == "ZA1004")).IsTrue();
        await Assert.That(diagnostics.Any(d => d.Id == "ZA1002")).IsTrue();
    }

    [Test]
    public async Task Generate_NonPartialClass_ReportsZA0001()
    {
        const string source = """
            using ZeroAlloc;

            internal class NotPartialApi : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString s = NotPartialApi.String("x");
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (_, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(
            compilation,
            new ZeroAllocGenerator());

        await Assert.That(diagnostics.Any(d => d.Id == "ZA0001")).IsTrue();
    }

    [Test]
    public async Task Generate_PublicClass_ReportsZA0002()
    {
        const string source = """
            using ZeroAlloc;

            public partial class PublicApi : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString s = PublicApi.String("x");
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (_, ImmutableArray<Diagnostic> diagnostics) = GeneratorTestHelper.RunGenerator(
            compilation,
            new ZeroAllocGenerator());

        await Assert.That(diagnostics.Any(d => d.Id == "ZA0002")).IsTrue();
    }

    [Test]
    public async Task Generate_MultipleCallsSameSignature_DeduplicatesMethods()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class DupApi : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString a = DupApi.String("a=", 1);
                    using TempString b = DupApi.String("b=", 2);
                    using TempBytes c = DupApi.Utf8("c=", 3);
                    using TempBytes d = DupApi.Utf8("d=", 4);
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "DupApi");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Split("internal static TempString String(", StringSplitOptions.None).Length).IsEqualTo(2);
    }

    [Test]
    public async Task Generate_DefaultOptions_EmitsConfiguration()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class OptionsApi : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString s = OptionsApi.String("x");
                }
            }
            """;

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "OptionsApi");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("DefaultBufferSize", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("RecursiveHeapFallback", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("BufferOverflowBehavior", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Generate_InvalidApiCallTarget_IsIgnored()
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
}
