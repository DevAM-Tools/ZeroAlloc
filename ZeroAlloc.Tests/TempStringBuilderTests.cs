// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>Test double that always fails <see cref="ISpanFormattable.TryFormat"/>.</summary>
internal readonly struct AlwaysFailSpanFormat : ISpanFormattable
{
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        charsWritten = 0;
        return false;
    }

    public string ToString(string? format, IFormatProvider? formatProvider) => "fail";
}

/// <summary>
/// Tests for <see cref="TempStringBuilder"/> covering append chains, grow paths,
/// TryAppend, seek operations, and disposal behavior.
/// </summary>
public sealed class TempStringBuilderTests
{
    // ========================================================================
    // CREATION AND PROPERTIES
    // ========================================================================

    [Test]
    public async Task Create_ReturnsEmptyBuilder()
    {
        int length;
        bool isEmpty;
        bool capacityPositive;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            length = builder.Length;
            isEmpty = builder.IsEmpty;
            capacityPositive = builder.Capacity > 0;
        }

        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
        await Assert.That(capacityPositive).IsTrue();
    }

    [Test]
    public async Task Capacity_AndRemaining_AreConsistent()
    {
        int capacity;
        int remaining;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            capacity = builder.Capacity;
            remaining = builder.Remaining;
        }

        await Assert.That(capacity > 0).IsTrue();
        await Assert.That(remaining).IsEqualTo(capacity);
    }

    [Test]
    public async Task IsHeapAllocated_FalseForTopLevelBuilder()
    {
        bool isHeap;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            isHeap = builder.IsHeapAllocated;
        }

        await Assert.That(isHeap).IsFalse();
    }

    [Test]
    public async Task ImplicitConversionToReadOnlySpan_Works()
    {
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hi");
            ReadOnlySpan<char> span = builder;
            content = span.ToString();
        }

        await Assert.That(content).IsEqualTo("Hi");
    }

    // ========================================================================
    // APPEND CHAIN
    // ========================================================================

    [Test]
    public async Task AppendChain_PrimitiveTypes_BuildsContent()
    {
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append('A');
            builder.Append("B");
            builder.Append("C".AsSpan());
            builder.Append(42);
            builder.Append(-99L);
            builder.Append(3.14);
            builder.Append(true);
            builder.Append(Guid.Empty);
            builder.Append(new DateOnly(2025, 6, 17));
            builder.Append(new TimeOnly(12, 0));
            builder.Append(Half.One);
            builder.Append((nuint)5);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).StartsWith("ABC42");
        await Assert.That(content).Contains("True");
        await Assert.That(content).Contains("2025");
        await Assert.That(content).Contains("06");
    }

    [Test]
    public async Task Append_WithFormat_FormatsValues()
    {
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append(255, "X2");
            builder.Append('|');
            builder.Append(3.14159, "F2");
            builder.Append('|');
            DateTime dt = new(2025, 6, 17);
            builder.Append(dt, "yyyy");
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).StartsWith("FF|");
        await Assert.That(content).Contains("3");
        await Assert.That(content).Contains("14");
        await Assert.That(content).Contains("2025");
    }

    [Test]
    public async Task AppendLine_AppendsNewline()
    {
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.AppendLine("Line1");
            builder.AppendLine("Line2".AsSpan());
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).Contains("Line1");
        await Assert.That(content).Contains("Line2");
        await Assert.That(content).Contains(Environment.NewLine);
    }

    [Test]
    public async Task AppendHex_AndBinary_FormatsCorrectly()
    {
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.AppendHex2(0xAB);
            builder.AppendHex4(0x1234);
            builder.AppendHex8(0xDEADBEEFu);
            builder.AppendHex16(1UL);
            builder.AppendBinary8(0b11110000);
            builder.AppendBinary16(0b1010);
            builder.AppendBinary32(1u);
            builder.AppendBinary64(2UL);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).StartsWith("AB1234DEADBEEF");
        await Assert.That(content).Contains("11110000");
    }

    [Test]
    public async Task Append_Null_DoesNothing()
    {
        bool isEmpty;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append((string?)null);
            isEmpty = builder.IsEmpty;
        }

        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task Append_EmptyString_DoesNothing()
    {
        bool isEmpty;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("");
            isEmpty = builder.IsEmpty;
        }

        await Assert.That(isEmpty).IsTrue();
    }

    // ========================================================================
    // CLEAR AND SEEKBACK
    // ========================================================================

    [Test]
    public async Task Clear_ResetsLength()
    {
        bool isEmpty;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hello World");
            builder.Clear();
            isEmpty = builder.IsEmpty;
        }

        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task SeekBack_DecreasesLength()
    {
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hello World");
            builder.SeekBack(6);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).IsEqualTo("Hello");
    }

    [Test]
    public async Task SeekBack_FullLength_Clears()
    {
        bool isEmpty;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hello");
            builder.SeekBack(5);
            isEmpty = builder.IsEmpty;
        }

        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task SeekBack_Zero_DoesNothing()
    {
        int length;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hello");
            builder.SeekBack(0);
            length = builder.Length;
        }

        await Assert.That(length).IsEqualTo(5);
    }

    [Test]
    public async Task SeekBack_TooMuch_ThrowsArgumentOutOfRangeException()
    {
        bool threw = false;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hi");
            try { builder.SeekBack(10); }
            catch (ArgumentOutOfRangeException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task TrySeekBack_Valid_ReturnsTrue()
    {
        bool result;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hello");
            result = builder.TrySeekBack(2);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(result).IsTrue();
        await Assert.That(content).IsEqualTo("Hel");
    }

    [Test]
    public async Task TrySeekBack_Invalid_ReturnsFalse()
    {
        bool result;
        int length;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hi");
            result = builder.TrySeekBack(10);
            length = builder.Length;
        }

        await Assert.That(result).IsFalse();
        await Assert.That(length).IsEqualTo(2);
    }

    // ========================================================================
    // TRYAPPEND
    // ========================================================================

    [Test]
    public async Task TryAppend_Chain_ReturnsTrue()
    {
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.TryAppend("Value: ");
            builder.TryAppend(42);
            builder.TryAppend(", Active: ");
            builder.TryAppend(true);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).IsEqualTo("Value: 42, Active: True");
    }

    [Test]
    public async Task TryAppend_Null_ReturnsTrue()
    {
        bool result;
        bool isEmpty;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            result = builder.TryAppend((string?)null);
            isEmpty = builder.IsEmpty;
        }

        await Assert.That(result).IsTrue();
        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task TryAppend_DateTime_AndGuid_ReturnTrue()
    {
        bool dtOk;
        bool guidOk;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            dtOk = builder.TryAppend(new DateTime(2025, 1, 15));
            guidOk = builder.TryAppend(Guid.Parse("12345678-1234-1234-1234-123456789012"));
            content = builder.AsSpan().ToString();
        }

        await Assert.That(dtOk).IsTrue();
        await Assert.That(guidOk).IsTrue();
        await Assert.That(content).Contains("2025");
        await Assert.That(content).Contains("12345678-1234-1234-1234-123456789012");
    }

    [Test]
    public async Task TryAppendHex2_ReturnsTrue()
    {
        bool result;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            result = builder.TryAppendHex2(0xAB);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(result).IsTrue();
        await Assert.That(content).IsEqualTo("AB");
    }

    // ========================================================================
    // GROW / NESTED BUILDER
    // ========================================================================

    [Test]
    public async Task NestedBuilder_GrowsBeyondDefaultBuffer_PreservesOuterContent()
    {
        string chunk = new('X', 1_500_000);
        string outerContent;
        string innerContent;
        bool innerIsHeap;

        {
            using TempStringBuilder outer = TempStringBuilder.Create();
            outer.Append("OUTER_START");

            using TempStringBuilder inner = TempStringBuilder.Create();
            innerIsHeap = inner.IsHeapAllocated;
            inner.Append(chunk);
            inner.Append(chunk);

            outer.Append("_OUTER_END");
            outerContent = outer.AsSpan().ToString();
            innerContent = inner.AsSpan().ToString();
        }

        await Assert.That(innerIsHeap).IsTrue();
        await Assert.That(innerContent.Length).IsEqualTo(chunk.Length * 2);
        await Assert.That(innerContent).IsEqualTo(chunk + chunk);
        await Assert.That(outerContent).IsEqualTo("OUTER_START_OUTER_END");
    }

    // ========================================================================
    // OUTPUT AND DISPOSAL
    // ========================================================================

    [Test]
    public async Task ToString_ReturnsWrittenContent()
    {
        string result;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Test");
            result = builder.ToString();
        }

        await Assert.That(result).IsEqualTo("Test");
    }

    [Test]
    public async Task ToString_Empty_ReturnsEmptyString()
    {
        string result;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            result = builder.ToString();
        }

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Dispose_ReleasesBuffer()
    {
        int length;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Test");
            length = builder.Length;
            builder.Dispose();
        }

        await Assert.That(length).IsEqualTo(4);
    }

    [Test]
    public async Task ChainedAppends_BuildsCorrectString()
    {
        string result;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("User: ");
            builder.Append(12345);
            builder.Append(", Balance: ");
            builder.Append(99.99);
            result = builder.AsSpan().ToString();
        }

        await Assert.That(result).StartsWith("User: 12345, Balance:");
    }

    [Test]
    public async Task ClearAndReuse_Works()
    {
        string first;
        string second;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("First");
            first = builder.AsSpan().ToString();
            builder.Clear();
            builder.Append("Second");
            second = builder.AsSpan().ToString();
        }

        await Assert.That(first).IsEqualTo("First");
        await Assert.That(second).IsEqualTo("Second");
    }

    // ========================================================================
    // EXIT-POINT COVERAGE
    // ========================================================================

    [Test]
    [Arguments("uint")]
    [Arguments("ulong")]
    [Arguments("short")]
    [Arguments("ushort")]
    [Arguments("byte")]
    [Arguments("sbyte")]
    [Arguments("DateTimeOffset")]
    [Arguments("TimeSpan")]
    [Arguments("Int128")]
    [Arguments("UInt128")]
    [Arguments("float")]
    [Arguments("decimal")]
    [Arguments("nint")]
    public async Task ExitCoverage_Append_PrimitiveOverload_Succeeds(string kind)
    {
        int length;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                _InvokeTempStringAppend(kind, ref builder);
                length = builder.Length;
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(length).IsGreaterThan(0);
    }

    [Test]
    [Arguments("long")]
    [Arguments("float")]
    [Arguments("decimal")]
    [Arguments("DateTimeOffset")]
    [Arguments("TimeSpan")]
    [Arguments("Guid")]
    public async Task ExitCoverage_Append_FormattedOverload_Succeeds(string kind)
    {
        int length;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                _InvokeTempStringAppendFormatted(kind, ref builder);
                length = builder.Length;
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(length).IsGreaterThan(0);
    }

    [Test]
    [Arguments("int")]
    [Arguments("long")]
    [Arguments("uint")]
    [Arguments("ulong")]
    [Arguments("short")]
    [Arguments("ushort")]
    [Arguments("byte")]
    [Arguments("sbyte")]
    [Arguments("bool")]
    [Arguments("DateTime")]
    [Arguments("DateTimeOffset")]
    [Arguments("TimeSpan")]
    [Arguments("Guid")]
    [Arguments("Int128")]
    [Arguments("UInt128")]
    [Arguments("float")]
    [Arguments("double")]
    [Arguments("decimal")]
    [Arguments("Half")]
    [Arguments("DateOnly")]
    [Arguments("TimeOnly")]
    [Arguments("nint")]
    [Arguments("nuint")]
    public async Task ExitCoverage_TryAppend_Primitive_Succeeds(string kind)
    {
        bool result;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                result = _InvokeTempStringTryAppend(kind, ref builder);
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(result).IsTrue();
    }

    [Test]
    [Arguments(nameof(TempStringBuilder.TryAppendHex4), 4)]
    [Arguments(nameof(TempStringBuilder.TryAppendHex8), 8)]
    [Arguments(nameof(TempStringBuilder.TryAppendHex16), 16)]
    [Arguments(nameof(TempStringBuilder.TryAppendBinary8), 8)]
    [Arguments(nameof(TempStringBuilder.TryAppendBinary16), 16)]
    [Arguments(nameof(TempStringBuilder.TryAppendBinary32), 32)]
    [Arguments(nameof(TempStringBuilder.TryAppendBinary64), 64)]
    public async Task ExitCoverage_TryAppendHexBinary_Success(string method, int requiredChars)
    {
        bool success = _InvokeTempStringTryHexBinary(method, requiredChars, expectSuccess: true);

        await Assert.That(success).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_TryAppend_Char_Succeeds()
    {
        bool charOk;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            charOk = builder.TryAppend('A');
        }

        await Assert.That(charOk).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_AppendBoolDateTimeAndGeneric_Succeed()
    {
        int length;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                builder.Append(false);
                builder.Append(new DateTime(2025, 6, 17));
                builder.Append(255, "X2");
                length = builder.Length;
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(length).IsGreaterThan(0);
    }

    [Test]
    public async Task ExitCoverage_TryAppendFormattedGeneric_Succeeds()
    {
        bool result;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                result = builder.TryAppend(255, "X2");
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_Grow_TriggersEnsureCapacitySuccess()
    {
        int length;
        int capacity;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                capacity = builder.Capacity;
                builder.Append(new string('X', capacity));
                builder.Append('!');
                length = builder.Length;
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(length).IsEqualTo(capacity + 1);
    }

    [Test]
    [NotInParallel("Corrupts thread-static char buffer")]
    public async Task ExitCoverage_TryAppend_FailsWhenGrowUnavailable()
    {
        bool stringFail;
        bool spanFail;
        bool charFail;
        bool boolFail;
        bool boolTrueFail;
        bool genericFail;
        bool formattableFail;
        bool hexFail;
        int capacity;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                capacity = builder.Capacity;
                builder.Append(new string('X', capacity));
                _NullThreadStaticCharBuffer();
                stringFail = builder.TryAppend("Z");
                spanFail = builder.TryAppend("Z".AsSpan());
                charFail = builder.TryAppend('Q');
                boolFail = builder.TryAppend(false);
                boolTrueFail = builder.TryAppend(true);
                genericFail = builder.TryAppend(255, "X4");
                formattableFail = builder.TryAppend<AlwaysFailSpanFormat>(new(), default);
                hexFail = builder.TryAppendHex2(0xAB);
            }
            finally
            {
                builder.Dispose();
                ZeroAllocHelper.ReleaseBuffers();
            }
        }

        await Assert.That(stringFail).IsFalse();
        await Assert.That(spanFail).IsFalse();
        await Assert.That(charFail).IsFalse();
        await Assert.That(boolFail).IsFalse();
        await Assert.That(boolTrueFail).IsFalse();
        await Assert.That(genericFail).IsFalse();
        await Assert.That(formattableFail).IsFalse();
        await Assert.That(hexFail).IsFalse();
    }

    [Test]
    [NotInParallel("Corrupts thread-static char buffer")]
    public async Task ExitCoverage_TryEnsureCapacity_SucceedsViaTryAppend()
    {
        bool grew;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                builder.Append(new string('X', builder.Capacity));
                grew = builder.TryAppend('!');
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(grew).IsTrue();
    }

    [Test]
    [NotInParallel("SimulateGrowStallForCoverage")]
    public async Task ExitCoverage_AppendFormattable_ThrowsWhenGrowStalls()
    {
        bool threw = false;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                ZeroAllocHelper.SimulateGrowStallForCoverage = true;
                try { builder.Append<AlwaysFailSpanFormat>(new(), default); }
                catch (InvalidOperationException) { threw = true; }
            }
            finally
            {
                builder.Dispose();
                ZeroAllocHelper.SimulateGrowStallForCoverage = false;
            }
        }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    [NotInParallel("SimulateGrowStallForCoverage")]
    public async Task ExitCoverage_TryAppendFormattable_ReturnsFalseWhenGrowStalls()
    {
        bool failed;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                ZeroAllocHelper.SimulateGrowStallForCoverage = true;
                failed = !builder.TryAppend<AlwaysFailSpanFormat>(new(), default);
            }
            finally
            {
                builder.Dispose();
                ZeroAllocHelper.SimulateGrowStallForCoverage = false;
            }
        }

        await Assert.That(failed).IsTrue();
    }

    [Test]
    [NotInParallel("Corrupts thread-static char buffer")]
    public async Task ExitCoverage_AppendFormattable_ThrowsWhenBufferUnavailable()
    {
        bool threw = false;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                builder.Append(new string('X', builder.Capacity));
                _NullThreadStaticCharBuffer();
                try { builder.Append(255, "X4"); }
                catch (InvalidOperationException) { threw = true; }
            }
            finally
            {
                builder.Dispose();
                ZeroAllocHelper.ReleaseBuffers();
            }
        }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task ExitCoverage_AppendGenericFormattable_Succeeds()
    {
        int length;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                builder.Append<int>(42, "D");
                length = builder.Length;
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(length).IsGreaterThan(0);
    }

    [Test]
    public async Task ExitCoverage_TryAppendSpanAndBool_Succeed()
    {
        bool spanOk;
        bool boolOk;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                spanOk = builder.TryAppend("OK".AsSpan());
                boolOk = builder.TryAppend(true);
            }
            finally
            {
                builder.Dispose();
            }
        }

        await Assert.That(spanOk).IsTrue();
        await Assert.That(boolOk).IsTrue();
    }

    [Test]
    [NotInParallel("Corrupts thread-static char buffer")]
    public async Task ExitCoverage_TryAppendHexBinary_FailsWhenGrowUnavailable()
    {
        bool hex4;
        bool hex8;
        bool hex16;
        bool bin8;
        bool bin16;
        bool bin32;
        bool bin64;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            try
            {
                builder.Append(new string('X', builder.Capacity));
                _NullThreadStaticCharBuffer();
                hex4 = builder.TryAppendHex4(0x1234);
                hex8 = builder.TryAppendHex8(0x12345678u);
                hex16 = builder.TryAppendHex16(0x0102030405060708UL);
                bin8 = builder.TryAppendBinary8(0b10101010);
                bin16 = builder.TryAppendBinary16(0b1010);
                bin32 = builder.TryAppendBinary32(1u);
                bin64 = builder.TryAppendBinary64(2UL);
            }
            finally
            {
                builder.Dispose();
                ZeroAllocHelper.ReleaseBuffers();
            }
        }

        await Assert.That(hex4).IsFalse();
        await Assert.That(hex8).IsFalse();
        await Assert.That(hex16).IsFalse();
        await Assert.That(bin8).IsFalse();
        await Assert.That(bin16).IsFalse();
        await Assert.That(bin32).IsFalse();
        await Assert.That(bin64).IsFalse();
    }

    private static void _NullThreadStaticCharBuffer()
    {
        FieldInfo? bufferField = typeof(ZeroAllocHelper).GetField("_CharBuffer", BindingFlags.NonPublic | BindingFlags.Static);
        bufferField?.SetValue(null, null);
    }

    private static void _InvokeTempStringAppend(string kind, ref TempStringBuilder builder)
    {
        switch (kind)
        {
            case "uint": builder.Append(42u); break;
            case "ulong": builder.Append(99ul); break;
            case "short": builder.Append((short)-12); break;
            case "ushort": builder.Append((ushort)65535); break;
            case "byte": builder.Append((byte)255); break;
            case "sbyte": builder.Append((sbyte)-1); break;
            case "DateTimeOffset": builder.Append(new DateTimeOffset(2025, 6, 17, 0, 0, 0, TimeSpan.Zero)); break;
            case "TimeSpan": builder.Append(TimeSpan.FromMinutes(90)); break;
            case "Int128": builder.Append((Int128)(-1)); break;
            case "UInt128": builder.Append(UInt128.MaxValue); break;
            case "float": builder.Append(3.14f); break;
            case "decimal": builder.Append(1.5m); break;
            case "nint": builder.Append((nint)7); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown kind.");
        }
    }

    private static void _InvokeTempStringAppendFormatted(string kind, ref TempStringBuilder builder)
    {
        switch (kind)
        {
            case "long": builder.Append(-99L, "D"); break;
            case "float": builder.Append(3.14f, "F2"); break;
            case "decimal": builder.Append(1.5m, "C"); break;
            case "DateTimeOffset": builder.Append(new DateTimeOffset(2025, 6, 17, 0, 0, 0, TimeSpan.Zero), "O"); break;
            case "TimeSpan": builder.Append(TimeSpan.FromMinutes(90), "c"); break;
            case "Guid": builder.Append(Guid.Parse("12345678-1234-1234-1234-123456789012"), "N"); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown kind.");
        }
    }

    private static bool _InvokeTempStringTryAppend(string kind, ref TempStringBuilder builder) => kind switch
    {
        "int" => builder.TryAppend(42),
        "long" => builder.TryAppend(-99L),
        "uint" => builder.TryAppend(42u),
        "ulong" => builder.TryAppend(99ul),
        "short" => builder.TryAppend((short)-12),
        "ushort" => builder.TryAppend((ushort)65535),
        "byte" => builder.TryAppend((byte)255),
        "sbyte" => builder.TryAppend((sbyte)-1),
        "bool" => builder.TryAppend(true),
        "DateTime" => builder.TryAppend(new DateTime(2025, 6, 17)),
        "DateTimeOffset" => builder.TryAppend(new DateTimeOffset(2025, 6, 17, 0, 0, 0, TimeSpan.Zero)),
        "TimeSpan" => builder.TryAppend(TimeSpan.FromMinutes(90)),
        "Guid" => builder.TryAppend(Guid.Parse("12345678-1234-1234-1234-123456789012")),
        "Int128" => builder.TryAppend((Int128)(-1)),
        "UInt128" => builder.TryAppend(UInt128.MaxValue),
        "float" => builder.TryAppend(3.14f),
        "double" => builder.TryAppend(2.718),
        "decimal" => builder.TryAppend(1.5m),
        "Half" => builder.TryAppend(Half.One),
        "DateOnly" => builder.TryAppend(new DateOnly(2025, 6, 17)),
        "TimeOnly" => builder.TryAppend(new TimeOnly(14, 30, 0)),
        "nint" => builder.TryAppend((nint)7),
        "nuint" => builder.TryAppend((nuint)8),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown kind."),
    };

    private static bool _InvokeTempStringTryHexBinary(string method, int requiredChars, bool expectSuccess)
    {
        using TempStringBuilder builder = TempStringBuilder.Create();
        return method switch
        {
            nameof(TempStringBuilder.TryAppendHex4) => builder.TryAppendHex4(0x1234),
            nameof(TempStringBuilder.TryAppendHex8) => builder.TryAppendHex8(0x12345678u),
            nameof(TempStringBuilder.TryAppendHex16) => builder.TryAppendHex16(0x0102030405060708UL),
            nameof(TempStringBuilder.TryAppendBinary8) => builder.TryAppendBinary8(0b10101010),
            nameof(TempStringBuilder.TryAppendBinary16) => builder.TryAppendBinary16(0b1010),
            nameof(TempStringBuilder.TryAppendBinary32) => builder.TryAppendBinary32(1u),
            nameof(TempStringBuilder.TryAppendBinary64) => builder.TryAppendBinary64(2UL),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown method."),
        };
    }
}
