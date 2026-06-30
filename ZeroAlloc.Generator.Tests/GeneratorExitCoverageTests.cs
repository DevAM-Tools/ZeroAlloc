// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests;

/// <summary>Targeted tests for remaining generator exit-point coverage gaps.</summary>
internal sealed class GeneratorExitCoverageTests
{
    private const string _Usings = """
        #nullable disable
        using System;
        using System.Globalization;
        using ZeroAlloc;
        """;

    #region ArgumentTypeInfo property getters

    [Test]
    public async Task ArgumentTypeInfo_SizeHints_ExposeKnownSizeFlags()
    {
        ArgumentTypeInfo sized = new(
            "Sized", "sized", TypeCategory.SpanFormattable, false, true,
            ImplementsIStringSize: true);
        ArgumentTypeInfo utf8Sized = new(
            "Utf8Sized", "utf8", TypeCategory.Utf8SpanFormattable, false, true,
            ImplementsIUtf8Size: true);
        ArgumentTypeInfo minimum = new(
            "System.Decimal", "decimal", TypeCategory.SpanFormattable, false, true,
            MinimumSize: 29);

        await Assert.That(sized.HasKnownExactSize).IsTrue();
        await Assert.That(utf8Sized.HasKnownExactSize).IsTrue();
        await Assert.That(minimum.HasKnownMinimumSize).IsTrue();
    }

    #endregion

    #region ZeroAllocGenerator — syntax filter exit paths

    [Test]
    public async Task ZeroAlloc_StaticApiCall_IsIgnored()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    string s = String("skip-static");
                    Api.String("ok");
                }

                internal static string String(string value) => value;
            }
            """;

        (Compilation output, _) = _RunZeroAlloc(source);
        await Assert.That(GeneratorTestHelper.GetGeneratedSource(output, "Api")).IsNotNull();
    }

    [Test]
    public async Task ZeroAlloc_InstanceApiCall_IsIgnored()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal sealed class Holder
            {
                internal void Call()
                {
                    var api = new Api();
                    api.String("skip-instance");
                }
            }
            """;

        (Compilation output, _) = _RunZeroAlloc(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "Api");
        await Assert.That(generated is null || !generated.Contains("skip-instance", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_UntypedArgument_IsIgnored()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    Api.String((string)null);
                }
            }
            """;

        (Compilation output, _) = _RunZeroAlloc(source);
        await Assert.That(GeneratorTestHelper.GetGeneratedSource(output, "Api")).IsNotNull();
    }

    [Test]
    public async Task ZeroAlloc_CultureInfoMidArgument_EmitsCultureComments()
    {
        const string source = """
            using System.Globalization;
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call()
                {
                    using TempString s = Api.String("prefix=", CultureInfo.InvariantCulture, "suffix");
                    using TempString p = Api.String("a=", CultureInfo.InvariantCulture);
                }
            }
            """;

        (Compilation output, _) = _RunZeroAlloc(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "Api");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("Format provider (not written)", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Used as format provider for other arguments", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_FormatProviderMidArgument_EmitsProviderComments()
    {
        const string source = """
            using System;
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal static class Usage
            {
                private static readonly IFormatProvider _Provider = System.Globalization.CultureInfo.InvariantCulture;

                internal static void Call()
                {
                    using TempString s = Api.String("a=", _Provider, 1);
                }
            }
            """;

        (Compilation output, _) = _RunZeroAlloc(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "Api");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("Format provider (not written)", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_TempStringCreationInTryFormat_DetectsRecursion()
    {
        const string source = """
            using System;
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal struct TempStringFormatter : ISpanFormattable
            {
                public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
                {
                    using TempString inner = new TempString(new char[4], 0, false);
                    charsWritten = 0;
                    return true;
                }
            }

            internal static class Usage
            {
                internal static void Call() => Api.String(new TempStringFormatter());
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics) = _RunZeroAlloc(source);
        await Assert.That(diagnostics.Any(d => d.Id == "ZA1002")).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_TypeWithoutTryFormat_DoesNotFlagRecursion()
    {
        const string source = """
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal struct PlainFormatter
            {
                public override string ToString() => "plain";
            }

            internal static class Usage
            {
                internal static void Call() => Api.String(new PlainFormatter());
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics) = _RunZeroAlloc(source);
        await Assert.That(diagnostics.Any(d => d.Id == "ZA1004")).IsTrue();
        await Assert.That(diagnostics.Any(d => d.Id == "ZA1002")).IsFalse();
    }

    [Test]
    public async Task ZeroAlloc_WrongBaseType_IsIgnored()
    {
        const string source = """
            namespace App;

            internal class ZeroAllocBase { }

            internal partial class Api : ZeroAllocBase { }

            internal static class Usage
            {
                internal static void Call() => Api.String("x");
            }
            """;

        (Compilation output, _) = _RunZeroAlloc(source);
        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "Api")).IsFalse();
    }

    #endregion

    #region BinaryParsable — remaining validation and codegen paths

    [Test]
    public async Task Parsable_StringFixedLengthZero_ReportsZA2014()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct ZeroStringFixed
            {
                [StringFixedLength(0)]
                public string Name { get; init; }
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics) = _RunParsable(source);
        await Assert.That(diagnostics.Any(d => d.Id == "ZA2014")).IsTrue();
    }

    [Test]
    public async Task Parsable_ConflictingBytesEncodings_ReportsZA2015()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct ConflictingBytes
            {
                [BytesLengthVarInt]
                [BytesLengthBE(2)]
                public byte[] Data { get; init; }
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics) = _RunParsable(source);
        await Assert.That(diagnostics.Any(d => d.Id == "ZA2015")).IsTrue();
    }

    [Test]
    public async Task Parsable_MemoryAllEncodings_GeneratesParsingArms()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct MemoryEncodingPacket
            {
                [BytesLengthVarInt]
                public Memory<byte> VarIntMemory { get; init; }

                [BytesLengthBE(2)]
                public ReadOnlyMemory<byte> BeMemory { get; init; }

                [BytesLengthLE(4)]
                public Memory<byte> LeMemory { get; init; }

                public byte PayloadLength { get; init; }

                [BytesLengthFromField(nameof(PayloadLength))]
                public Memory<byte> FromFieldMemory { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "MemoryEncodingPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("FromFieldMemory", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("LeMemory", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Parsable_BitModeWithUnsupportedMember_GeneratesWarningComment()
    {
        string source = _Usings + """

            [BinaryParsable]
            public partial struct BitWithVarInt
            {
                [BinaryField(BitCount = 3)]
                public byte Flags { get; init; }

                [BinaryField(BitCount = 5)]
                public byte AlignPad { get; init; }

                public VarInt Count { get; init; }
            }
            """;

        (Compilation output, _) = _RunParsable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "BitWithVarInt");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("not supported in bit-level parsing mode", StringComparison.Ordinal)).IsTrue();
    }

    #endregion

    #region BinaryWritable — remaining validation and codegen paths

    [Test]
    public async Task Writable_StringFixedLengthZero_ReportsZA3012()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableZeroString
            {
                [StringFixedLength(0)]
                public string Name { get; init; }
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics) = _RunWritable(source);
        await Assert.That(diagnostics.Any(d => d.Id == "ZA3012")).IsTrue();
    }

    [Test]
    public async Task Writable_ConflictingBytesEncodings_ReportsZA3013()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableConflictingBytes
            {
                [BytesLengthVarInt]
                [BytesLengthLE(2)]
                public byte[] Data { get; init; }
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics) = _RunWritable(source);
        await Assert.That(diagnostics.Any(d => d.Id == "ZA3013")).IsTrue();
    }

    [Test]
    public async Task Writable_IBinarySerializableMember_GeneratesTryWrite()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct SerializableMemberPacket
            {
                public VarInt Count { get; init; }
                public VarIntZigZag Signed { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "SerializableMemberPacket");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("Count.TryWrite", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("Signed.TryWrite", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_MemoryAllEncodings_GeneratesSizeAndWriteArms()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableMemoryEncodings
            {
                [BytesLengthVarInt]
                public Memory<byte> VarIntMemory { get; init; }

                [BytesLengthBE(1)]
                public Memory<byte> BeMemory { get; init; }

                [BytesLengthLE(2)]
                public ReadOnlyMemory<byte> LeMemory { get; init; }

                public byte PayloadLength { get; init; }

                [BytesLengthFromField(nameof(PayloadLength))]
                public Memory<byte> FromFieldMemory { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableMemoryEncodings");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("FromFieldMemory", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("LeMemory", StringComparison.Ordinal)).IsTrue();
        await Assert.That(generated.Contains("total +=", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_BitModeWithUnsupportedMember_GeneratesWarningComment()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableBitWithVarInt
            {
                [BinaryField(BitCount = 3)]
                public byte Flags { get; init; }

                [BinaryField(BitCount = 5)]
                public byte AlignPad { get; init; }

                public VarInt Count { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableBitWithVarInt");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("not supported in bit-level writing mode", StringComparison.Ordinal)).IsTrue();
    }

    #endregion

    #region Generator internals — reflection coverage

    [Test]
    public async Task Parsable_GetTypeInfo_NonTypeTargetSymbol_ReturnsNull()
    {
        GeneratorAttributeSyntaxContext context = GeneratorReflectionHelper.CreateDefaultAttributeContext();

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(BinaryParsableGenerator),
            "_GetParsableTypeInfo",
            context);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Writable_GetTypeInfo_NonTypeTargetSymbol_ReturnsNull()
    {
        GeneratorAttributeSyntaxContext context = GeneratorReflectionHelper.CreateDefaultAttributeContext();

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(BinaryWritableGenerator),
            "_GetWritableTypeInfo",
            context);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Parsable_GenerateParsingCode_NullTypeInfo_ReturnsEarly()
    {
        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(BinaryParsableGenerator),
            "_GenerateParsingCode",
            null,
            null);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Writable_GenerateWritingCode_NullTypeInfo_ReturnsEarly()
    {
        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(BinaryWritableGenerator),
            "_GenerateWritingCode",
            null,
            null);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task Writable_ManualIBinarySerializableMember_ClassifiesAsNestedParsable()
    {
        string source = _Usings + """

            internal readonly struct ManualSerializable : IBinarySerializable
            {
                public bool TryGetWrittenSize(out int size)
                {
                    size = 1;
                    return true;
                }

                public bool TryWrite(Span<byte> destination, out int bytesWritten)
                {
                    bytesWritten = 1;
                    return destination.Length >= 1;
                }
            }

            [BinaryWritable]
            public partial struct WritableManualPayload
            {
                public ManualSerializable Payload { get; init; }
            }
            """;

        (Compilation output, _) = _RunWritable(source);
        string? generated = GeneratorTestHelper.GetGeneratedSource(output, "WritableManualPayload");

        await Assert.That(generated).IsNotNull();
        await Assert.That(generated!.Contains("Payload.TryWrite", StringComparison.Ordinal)).IsTrue();
    }

    [Test]
    public async Task Writable_BinaryFixedLengthZero_ReportsZA3012()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct ZeroBinaryFixed
            {
                [BinaryFixedLength(0)]
                public byte[] Data { get; init; }
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics) = _RunWritable(source);

        await Assert.That(diagnostics.Any(d => d.Id == "ZA3012")).IsTrue();
    }

    [Test]
    public async Task Writable_ConflictingStringEncodings_ReportsZA3013()
    {
        string source = _Usings + """

            [BinaryWritable]
            public partial struct WritableConflictingString
            {
                [StringLengthVarInt]
                [StringLengthBE(2)]
                public string Name { get; init; }
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics) = _RunWritable(source);

        await Assert.That(diagnostics.Any(d => d.Id == "ZA3013")).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_GetUserClassInfo_NoBaseList_ReturnsNull()
    {
        GeneratorSyntaxContext context = GeneratorReflectionHelper.CreateClassWithoutBaseListContext();

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_GetUserClassInfo",
            context);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ZeroAlloc_GetUserClassInfo_NonZeroAllocBase_ReturnsNull()
    {
        const string source = """
            using ZeroAlloc;

            internal class PlainBase { }

            internal partial class Api : PlainBase { }

            internal static class Usage
            {
                internal static void Call() => Api.String("x");
            }
            """;

        (Compilation output, _) = _RunZeroAlloc(source);

        await Assert.That(GeneratorTestHelper.HasGeneratedSource(output, "Api")).IsFalse();
    }

    [Test]
    public async Task ZeroAlloc_GetMethodCallInfo_UnboundArgument_ReturnsNull()
    {
        GeneratorSyntaxContext context = GeneratorReflectionHelper.CreateUnboundArgumentContext();

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_GetMethodCallInfo",
            context);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ZeroAlloc_MayUseZeroAllocInternally_NullNamespace_ReturnsFalse()
    {
        ITypeSymbol typeWithNullNamespace = _NullNamespaceTypeProxy.Create();

        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation("internal static class Host { }");

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_MayUseZeroAllocInternally",
            typeWithNullNamespace,
            compilation);

        await Assert.That((bool)result!).IsFalse();
    }

    [Test]
    public async Task ZeroAlloc_RecursionProbe_BytesCallInTryFormat_DetectsUsage()
    {
        const string source = """
            using System;
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal struct BytesInTryFormat : ISpanFormattable
            {
                public bool TryFormat(
                    Span<char> destination,
                    out int charsWritten,
                    ReadOnlySpan<char> format,
                    IFormatProvider provider)
                {
                    using TempBytes payload = Api.Bytes(new U16BE(1));
                    charsWritten = 0;
                    return true;
                }
            }

            internal static class Usage
            {
                internal static void Call() => Api.String(new BytesInTryFormat());
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics) = _RunZeroAlloc(source);

        await Assert.That(diagnostics.Any(d => d.Id == "ZA1002")).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_RecursionProbe_TempBytesCreationInTryFormat_DetectsUsage()
    {
        const string source = """
            using System;
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal struct TempBytesInTryFormat : ISpanFormattable
            {
                public bool TryFormat(
                    Span<char> destination,
                    out int charsWritten,
                    ReadOnlySpan<char> format,
                    IFormatProvider provider)
                {
                    TempBytes payload = new TempBytes(new byte[4], 0, false);
                    _ = payload.Length;
                    charsWritten = 0;
                    return true;
                }
            }

            internal static class Usage
            {
                internal static void Call() => Api.String(new TempBytesInTryFormat());
            }
            """;

        (_, ImmutableArray<Diagnostic> diagnostics) = _RunZeroAlloc(source);

        await Assert.That(diagnostics.Any(d => d.Id == "ZA1002")).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_FindTryFormatMethod_NoMatchingInstanceMethod_ReturnsNull()
    {
        const string source = """
            using System;

            internal struct WrongTryFormat
            {
                public bool TryFormat(
                    ReadOnlySpan<char> destination,
                    out int charsWritten,
                    ReadOnlySpan<char> format,
                    IFormatProvider provider)
                {
                    charsWritten = 0;
                    return false;
                }
            }
            """;

        Compilation compilation = GeneratorTestHelper.CreateCompilation(source);
        INamedTypeSymbol type = compilation.GetTypeByMetadataName("WrongTryFormat")!;

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_FindTryFormatMethod",
            type);

        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task ZeroAlloc_ContainsZeroAllocApiCalls_BytesInvocation_ReturnsTrue()
    {
        const string source = """
            using System;
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal struct BytesInTryFormat : ISpanFormattable
            {
                public bool TryFormat(
                    Span<char> destination,
                    out int charsWritten,
                    ReadOnlySpan<char> format,
                    IFormatProvider provider)
                {
                    using TempBytes payload = Api.Bytes(new U16BE(1));
                    charsWritten = 0;
                    return true;
                }
            }
            """;

        Compilation compilation = GeneratorTestHelper.CreateCompilation(source);
        INamedTypeSymbol type = compilation.GetTypeByMetadataName("BytesInTryFormat")!;
        IMethodSymbol tryFormat = type.GetMembers("TryFormat").OfType<IMethodSymbol>().Single();
        SyntaxNode body = tryFormat.DeclaringSyntaxReferences[0].GetSyntax();

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_ContainsZeroAllocApiCalls",
            body);

        await Assert.That((bool)result!).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_ContainsZeroAllocApiCalls_StringInvocation_ReturnsTrue()
    {
        const string source = """
            using System;
            using ZeroAlloc;

            internal partial class Api : ZeroAllocBase { }

            internal struct StringInTryFormat : ISpanFormattable
            {
                public bool TryFormat(
                    Span<char> destination,
                    out int charsWritten,
                    ReadOnlySpan<char> format,
                    IFormatProvider provider)
                {
                    _ = Api.String("x");
                    charsWritten = 0;
                    return true;
                }
            }
            """;

        Compilation compilation = GeneratorTestHelper.CreateCompilation(source);
        INamedTypeSymbol type = compilation.GetTypeByMetadataName("StringInTryFormat")!;
        IMethodSymbol tryFormat = type.GetMembers("TryFormat").OfType<IMethodSymbol>().Single();
        SyntaxNode body = tryFormat.DeclaringSyntaxReferences[0].GetSyntax();

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_ContainsZeroAllocApiCalls",
            body);

        await Assert.That((bool)result!).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_ContainsZeroAllocApiCalls_TempBytesCreation_ReturnsTrue()
    {
        const string source = """
            using System;
            using ZeroAlloc;

            internal struct TempBytesInTryFormat : ISpanFormattable
            {
                public bool TryFormat(
                    Span<char> destination,
                    out int charsWritten,
                    ReadOnlySpan<char> format,
                    IFormatProvider provider)
                {
                    TempBytes payload = new TempBytes(new byte[4], 0, false);
                    _ = payload.Length;
                    charsWritten = 0;
                    return true;
                }
            }
            """;

        Compilation compilation = GeneratorTestHelper.CreateCompilation(source);
        INamedTypeSymbol type = compilation.GetTypeByMetadataName("TempBytesInTryFormat")!;
        IMethodSymbol tryFormat = type.GetMembers("TryFormat").OfType<IMethodSymbol>().Single();
        SyntaxNode body = tryFormat.DeclaringSyntaxReferences[0].GetSyntax();

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_ContainsZeroAllocApiCalls",
            body);

        await Assert.That((bool)result!).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_ContainsZeroAllocApiCalls_TempStringCreation_ReturnsTrue()
    {
        const string source = """
            using System;
            using ZeroAlloc;

            internal struct TempStringInTryFormat : ISpanFormattable
            {
                public bool TryFormat(
                    Span<char> destination,
                    out int charsWritten,
                    ReadOnlySpan<char> format,
                    IFormatProvider provider)
                {
                    TempString payload = new TempString(new char[4], 0, false);
                    _ = payload.Length;
                    charsWritten = 0;
                    return true;
                }
            }
            """;

        Compilation compilation = GeneratorTestHelper.CreateCompilation(source);
        INamedTypeSymbol type = compilation.GetTypeByMetadataName("TempStringInTryFormat")!;
        IMethodSymbol tryFormat = type.GetMembers("TryFormat").OfType<IMethodSymbol>().Single();
        SyntaxNode body = tryFormat.DeclaringSyntaxReferences[0].GetSyntax();

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_ContainsZeroAllocApiCalls",
            body);

        await Assert.That((bool)result!).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_ContainsZeroAllocApiCalls_StackStringBuilderIdentifier_ReturnsTrue()
    {
        const string source = """
            using System;
            using ZeroAlloc;

            internal struct StackBuilderInTryFormat : ISpanFormattable
            {
                public bool TryFormat(
                    Span<char> destination,
                    out int charsWritten,
                    ReadOnlySpan<char> format,
                    IFormatProvider provider)
                {
                    StackStringBuilder builder = default;
                    charsWritten = builder.TryAppend('x') ? 1 : 0;
                    return charsWritten > 0;
                }
            }
            """;

        Compilation compilation = GeneratorTestHelper.CreateCompilation(source);
        INamedTypeSymbol type = compilation.GetTypeByMetadataName("StackBuilderInTryFormat")!;
        IMethodSymbol tryFormat = type.GetMembers("TryFormat").OfType<IMethodSymbol>().Single();
        SyntaxNode body = tryFormat.DeclaringSyntaxReferences[0].GetSyntax();

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_ContainsZeroAllocApiCalls",
            body);

        await Assert.That((bool)result!).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_DescendantUsesZeroAllocApi_ObjectCreation_ReturnsTrue()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "class Host { void M() { new ZeroAlloc.TempBytes(new byte[1], 0, false); } }");

        ObjectCreationExpressionSyntax creation = tree
            .GetRoot()
            .DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .First();

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_DescendantUsesZeroAllocApi",
            creation,
            GeneratorReflectionHelper.CreateZeroAllocMethodNames(),
            GeneratorReflectionHelper.CreateZeroAllocTypeNames());

        await Assert.That((bool)result!).IsTrue();
    }

    [Test]
    public async Task ZeroAlloc_DescendantUsesZeroAllocApi_ObjectCreationTypeIdentifier_ReturnsFalse()
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(
            "class Host { void M() { new ZeroAlloc.TempBytes(new byte[1], 0, false); } }");

        IdentifierNameSyntax typeIdentifier = tree
            .GetRoot()
            .DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .First(static n => n.Identifier.Text == "TempBytes");

        object? result = GeneratorReflectionHelper.InvokePrivateStatic(
            typeof(ZeroAllocGenerator),
            "_DescendantUsesZeroAllocApi",
            typeIdentifier,
            GeneratorReflectionHelper.CreateZeroAllocMethodNames(),
            GeneratorReflectionHelper.CreateZeroAllocTypeNames());

        await Assert.That((bool)result!).IsFalse();
    }

    [Test]
    public async Task CommentHelper_GetFormattingExplanation_BinaryAndRawCategories()
    {
        string binary = GeneratorReflectionHelper.GetFormattingExplanation(TypeCategory.BinarySerializable);
        string raw = GeneratorReflectionHelper.GetFormattingExplanation(TypeCategory.RawBytes);

        await Assert.That(binary.Contains("serializes", StringComparison.Ordinal)).IsTrue();
        await Assert.That(raw.Contains("raw bytes", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    #endregion

    /// <summary>Dispatch proxy returning null <see cref="ISymbol.ContainingNamespace"/>.</summary>
    private class _NullNamespaceTypeProxy : DispatchProxy
    {
        public static ITypeSymbol Create()
            => DispatchProxy.Create<ITypeSymbol, _NullNamespaceTypeProxy>();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            return targetMethod?.Name switch
            {
                "get_ContainingNamespace" => null,
                "get_Kind" => SymbolKind.ErrorType,
                "get_Name" => "NullNamespaceType",
                "get_Language" => LanguageNames.CSharp,
                "get_IsErrorType" => true,
                "Equals" when args?.Length == 1 => ReferenceEquals(this, args[0]),
                "GetHashCode" => RuntimeHelpers.GetHashCode(this),
                "ToString" => "NullNamespaceType",
                _ => targetMethod?.ReturnType == typeof(void)
                    ? null
                    : (targetMethod?.ReturnType.IsValueType == true
                        ? Activator.CreateInstance(targetMethod.ReturnType)
                        : null),
            };
        }
    }

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

    private static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics) _RunZeroAlloc(string source)
    {
        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(source);
        return GeneratorTestHelper.RunGenerator(compilation, new ZeroAllocGenerator());
    }
}
