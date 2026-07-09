// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="SpanStringBuilder"/> covering append chains, TryAppend failures,
/// overflow paths, and formatting edge cases.
/// </summary>
public sealed class SpanStringBuilderTests
{

    // ========================================================================
    // CREATION AND PROPERTIES
    // ========================================================================


    /// <summary>Verifies Constructor SetsCapacityAndEmptyState.</summary>
    [Test]
    public async Task Constructor_SetsCapacityAndEmptyState()
    {
        int length;
        int capacity;
        int remaining;
        {
            Span<char> buffer = stackalloc char[256];
            SpanStringBuilder builder = new(buffer);
            length = builder.Length;
            capacity = builder.Capacity;
            remaining = builder.Remaining;
        }

        await Assert.That(length).IsEqualTo(0);
        await Assert.That(capacity).IsEqualTo(256);
        await Assert.That(remaining).IsEqualTo(256);
    }

    /// <summary>Verifies AsSpan AndWrittenSpan ReturnWrittenContent.</summary>
    [Test]
    public async Task AsSpan_AndWrittenSpan_ReturnWrittenContent()
    {
        string asSpanContent;
        string writtenSpanContent;
        {
            Span<char> buffer = stackalloc char[64];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello");
            asSpanContent = builder.AsSpan().ToString();
            writtenSpanContent = builder.WrittenSpan.ToString();
        }

        await Assert.That(asSpanContent).IsEqualTo("Hello");
        await Assert.That(writtenSpanContent).IsEqualTo("Hello");
    }

    /// <summary>Verifies ToString ReturnsWrittenContent.</summary>
    [Test]
    public async Task ToString_ReturnsWrittenContent()
    {
        string result;
        {
            Span<char> buffer = stackalloc char[64];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Test");
            result = builder.ToString();
        }

        await Assert.That(result).IsEqualTo("Test");
    }

    // ========================================================================
    // APPEND CHAIN — PRIMITIVE AND TEMPORAL TYPES
    // ========================================================================


    /// <summary>Verifies AppendChain AllPrimitiveTypes BuildsExpectedOutput.</summary>
    [Test]
    public async Task AppendChain_AllPrimitiveTypes_BuildsExpectedOutput()
    {
        string content;
        {
            Span<char> buffer = stackalloc char[512];
            SpanStringBuilder builder = new(buffer);
            builder.Append("n:");
            builder.Append((short)-12);
            builder.Append(',');
            builder.Append((ushort)65535);
            builder.Append(',');
            builder.Append((byte)255);
            builder.Append(',');
            builder.Append((sbyte)-1);
            builder.Append(',');
            builder.Append(42u);
            builder.Append(',');
            builder.Append(99ul);
            builder.Append(',');
            builder.Append(3.14f);
            builder.Append(',');
            builder.Append(2.718);
            builder.Append(',');
            builder.Append(1.5m);
            builder.Append(',');
            builder.Append(Half.One);
            builder.Append(',');
            builder.Append(true);
            builder.Append(',');
            builder.Append(new DateOnly(2025, 6, 17));
            builder.Append(',');
            builder.Append(new TimeOnly(14, 30, 0));
            builder.Append(',');
            builder.Append((Int128)(-1));
            builder.Append(',');
            builder.Append(UInt128.MaxValue);
            builder.Append(',');
            builder.Append((nint)7);
            builder.Append(',');
            builder.Append((nuint)8);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).StartsWith("n:-12,65535,255,-1,42,99,");
        await Assert.That(content).Contains("3");
        await Assert.That(content).Contains("14");
        await Assert.That(content).Contains("True");
        await Assert.That(content).Contains("2025");
        await Assert.That(content).Contains("14:30");
    }

    /// <summary>Verifies AppendChain TemporalTypes FormatsCorrectly.</summary>
    [Test]
    public async Task AppendChain_TemporalTypes_FormatsCorrectly()
    {
        string content;
        {
            Span<char> buffer = stackalloc char[256];
            SpanStringBuilder builder = new(buffer);
            DateTime dt = new(2025, 1, 15, 10, 30, 0);
            DateTimeOffset dto = new(2025, 1, 15, 10, 30, 0, TimeSpan.FromHours(2));
            TimeSpan ts = TimeSpan.FromHours(1) + TimeSpan.FromMinutes(30);
            Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");

            builder.Append(dt);
            builder.Append('|');
            builder.Append(dto);
            builder.Append('|');
            builder.Append(ts);
            builder.Append('|');
            builder.Append(guid);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).Contains("2025");
        await Assert.That(content).Contains("+02:00");
        await Assert.That(content).Contains("01:30:00");
        await Assert.That(content).Contains("12345678-1234-1234-1234-123456789abc");
    }

    /// <summary>Verifies Append WithFormat FormatsCorrectly.</summary>
    [Test]
    public async Task Append_WithFormat_FormatsCorrectly()
    {
        string content;
        {
            Span<char> buffer = stackalloc char[128];
            SpanStringBuilder builder = new(buffer);
            builder.Append(42, "X4", CultureInfo.InvariantCulture);
            builder.Append('|');
            builder.Append(3.14159, "F2", CultureInfo.InvariantCulture);
            builder.Append('|');
            builder.Append(1.5f, "N1", CultureInfo.InvariantCulture);
            builder.Append('|');
            builder.Append(9.99m, "C", CultureInfo.InvariantCulture);
            builder.Append('|');
            DateTime dt = new(2025, 6, 17);
            builder.Append(dt, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            builder.Append('|');
            builder.Append(TimeSpan.FromMinutes(90), "c", CultureInfo.InvariantCulture);
            builder.Append('|');
            Guid guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            builder.Append(guid, "N");
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).StartsWith("002A|");
        await Assert.That(content).Contains("3");
        await Assert.That(content).Contains("14");
        await Assert.That(content).Contains("2025-06-17");
        await Assert.That(content).Contains("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee".Replace("-", ""));
    }

    /// <summary>Verifies Append GenericFormattable FormatsCorrectly.</summary>
    [Test]
    public async Task Append_GenericFormattable_FormatsCorrectly()
    {
        string content;
        {
            Span<char> buffer = stackalloc char[32];
            SpanStringBuilder builder = new(buffer);
            builder.Append(255, "X2", CultureInfo.InvariantCulture);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).IsEqualTo("FF");
    }

    // ========================================================================
    // APPENDLINE AND HEX/BINARY
    // ========================================================================


    /// <summary>Verifies AppendLine AppendsNewline.</summary>
    [Test]
    public async Task AppendLine_AppendsNewline()
    {
        string content;
        int length;
        {
            Span<char> buffer = stackalloc char[64];
            SpanStringBuilder builder = new(buffer);
            builder.AppendLine("Hello");
            builder.AppendLine();
            length = builder.Length;
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).StartsWith("Hello");
        await Assert.That(content).Contains(Environment.NewLine);
        await Assert.That(length).IsGreaterThan(5);
    }

    /// <summary>Verifies AppendHex AndBinary FormatsCorrectly.</summary>
    [Test]
    public async Task AppendHex_AndBinary_FormatsCorrectly()
    {
        string content;
        {
            Span<char> buffer = stackalloc char[256];
            SpanStringBuilder builder = new(buffer);
            builder.AppendHex2(0xAB);
            builder.Append('|');
            builder.AppendHex4(0x1234);
            builder.Append('|');
            builder.AppendHex8(0xDEADBEEFu);
            builder.Append('|');
            builder.AppendHex16(0x0102030405060708UL);
            builder.Append('|');
            builder.AppendBinary8(0b10101010);
            builder.Append('|');
            builder.AppendBinary16(0b1111000011110000);
            builder.Append('|');
            builder.AppendBinary32(0x80000001u);
            builder.Append('|');
            builder.AppendBinary64(1UL);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).IsEqualTo(
            "AB|1234|DEADBEEF|0102030405060708|10101010|1111000011110000|10000000000000000000000000000001|0000000000000000000000000000000000000000000000000000000000000001");
    }

    // ========================================================================
    // CLEAR, SEEKBACK, REMAINING
    // ========================================================================


    /// <summary>Verifies Clear ResetsLengthAndRemaining.</summary>
    [Test]
    public async Task Clear_ResetsLengthAndRemaining()
    {
        int length;
        int remaining;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello World");
            builder.Clear();
            length = builder.Length;
            remaining = builder.Remaining;
        }

        await Assert.That(length).IsEqualTo(0);
        await Assert.That(remaining).IsEqualTo(100);
    }

    /// <summary>Verifies SeekBack DecreasesLength.</summary>
    [Test]
    public async Task SeekBack_DecreasesLength()
    {
        string content;
        int length;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello World");
            builder.SeekBack(6);
            length = builder.Length;
            content = builder.AsSpan().ToString();
        }

        await Assert.That(length).IsEqualTo(5);
        await Assert.That(content).IsEqualTo("Hello");
    }

    /// <summary>Verifies SeekBack Zero DoesNothing.</summary>
    [Test]
    public async Task SeekBack_Zero_DoesNothing()
    {
        int length;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello");
            builder.SeekBack(0);
            length = builder.Length;
        }

        await Assert.That(length).IsEqualTo(5);
    }

    /// <summary>Verifies SeekBack TooMuch ThrowsArgumentOutOfRangeException.</summary>
    [Test]
    public async Task SeekBack_TooMuch_ThrowsArgumentOutOfRangeException()
    {
        bool threw = false;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hi");
            try { builder.SeekBack(10); }
            catch (ArgumentOutOfRangeException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies TrySeekBack Valid ReturnsTrue.</summary>
    [Test]
    public async Task TrySeekBack_Valid_ReturnsTrue()
    {
        bool result;
        string content;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello");
            result = builder.TrySeekBack(2);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(result).IsTrue();
        await Assert.That(content).IsEqualTo("Hel");
    }

    /// <summary>Verifies TrySeekBack Invalid ReturnsFalse.</summary>
    [Test]
    public async Task TrySeekBack_Invalid_ReturnsFalse()
    {
        bool result;
        int length;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hi");
            result = builder.TrySeekBack(10);
            length = builder.Length;
        }

        await Assert.That(result).IsFalse();
        await Assert.That(length).IsEqualTo(2);
    }

    /// <summary>Verifies Remaining ReturnsCorrectValue.</summary>
    [Test]
    public async Task Remaining_ReturnsCorrectValue()
    {
        int remaining;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello");
            remaining = builder.Remaining;
        }

        await Assert.That(remaining).IsEqualTo(95);
    }

    // ========================================================================
    // OVERFLOW — APPEND THROWS
    // ========================================================================


    /// <summary>Verifies Append StringOverflow ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task Append_StringOverflow_ThrowsInvalidOperationException()
    {
        bool threw = false;
        int length;
        {
            Span<char> buffer = stackalloc char[5];
            SpanStringBuilder builder = new(buffer);
            try { builder.Append("This is too long"); }
            catch (InvalidOperationException) { threw = true; }
            length = builder.Length;
        }

        await Assert.That(threw).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    /// <summary>Verifies Append OneCharPastCapacity ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task Append_OneCharPastCapacity_ThrowsInvalidOperationException()
    {
        bool threw = false;
        {
            Span<char> buffer = stackalloc char[5];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello");
            try { builder.Append('!'); }
            catch (InvalidOperationException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies Append ExactFit succeeds.</summary>
    [Test]
    public async Task Append_ExactFit_Succeeds()
    {
        int length;
        int remaining;
        string content;
        {
            Span<char> buffer = stackalloc char[5];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello");
            length = builder.Length;
            remaining = builder.Remaining;
            content = builder.AsSpan().ToString();
        }

        await Assert.That(length).IsEqualTo(5);
        await Assert.That(remaining).IsEqualTo(0);
        await Assert.That(content).IsEqualTo("Hello");
    }

    /// <summary>Verifies Append EmptyBuffer ThrowsOnAnyAppend.</summary>
    [Test]
    public async Task Append_EmptyBuffer_ThrowsOnAnyAppend()
    {
        bool threw = false;
        {
            Span<char> buffer = [];
            SpanStringBuilder builder = new(buffer);
            try { builder.Append("A"); }
            catch (InvalidOperationException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies AppendLine BufferTooSmall DoesNotAdvance.</summary>
    [Test]
    public async Task AppendLine_BufferTooSmall_DoesNotAdvance()
    {
        bool threw = false;
        int length;
        {
            Span<char> buffer = stackalloc char[5];
            SpanStringBuilder builder = new(buffer);
            try { builder.AppendLine("Hello"); }
            catch (InvalidOperationException) { threw = true; }
            length = builder.Length;
        }

        await Assert.That(threw).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    /// <summary>Verifies AppendHex2 BufferTooSmall ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task AppendHex2_BufferTooSmall_ThrowsInvalidOperationException()
    {
        bool threw = false;
        {
            Span<char> buffer = stackalloc char[1];
            SpanStringBuilder builder = new(buffer);
            try { builder.AppendHex2(0xAB); }
            catch (InvalidOperationException) { threw = true; }
        }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies Append NullString DoesNothing.</summary>
    [Test]
    public async Task Append_NullString_DoesNothing()
    {
        int length;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append((string?)null);
            length = builder.Length;
        }

        await Assert.That(length).IsEqualTo(0);
    }

    // ========================================================================
    // TRYAPPEND — SUCCESS AND FAILURE
    // ========================================================================


    /// <summary>Verifies TryAppend Chain ReturnsTrueAndBuildsContent.</summary>
    [Test]
    public async Task TryAppend_Chain_ReturnsTrueAndBuildsContent()
    {
        bool allSucceeded;
        string content;
        {
            Span<char> buffer = stackalloc char[128];
            SpanStringBuilder builder = new(buffer);
            bool r1 = builder.TryAppend("Value: ");
            bool r2 = builder.TryAppend(42);
            bool r3 = builder.TryAppend(", Active: ");
            bool r4 = builder.TryAppend(false);
            allSucceeded = r1 && r2 && r3 && r4;
            content = builder.AsSpan().ToString();
        }

        await Assert.That(allSucceeded).IsTrue();
        await Assert.That(content).IsEqualTo("Value: 42, Active: False");
    }

    /// <summary>Verifies TryAppend String FullBuffer ReturnsFalse.</summary>
    [Test]
    public async Task TryAppend_String_FullBuffer_ReturnsFalse()
    {
        bool result;
        int length;
        {
            Span<char> buffer = stackalloc char[4];
            SpanStringBuilder builder = new(buffer);
            builder.Append("ABCD");
            result = builder.TryAppend("X");
            length = builder.Length;
        }

        await Assert.That(result).IsFalse();
        await Assert.That(length).IsEqualTo(4);
    }

    /// <summary>Verifies TryAppend Char AtCapacity ReturnsExpected.</summary>
    [Test]
    [Arguments(1, true)]
    [Arguments(2, false)]
    public async Task TryAppend_Char_AtCapacity_ReturnsExpected(int charsWritten, bool expected)
    {
        bool result;
        {
            Span<char> buffer = stackalloc char[2];
            SpanStringBuilder builder = new(buffer);
            if (charsWritten >= 1) { builder.Append('A'); }
            if (charsWritten >= 2) { builder.Append('B'); }
            result = builder.TryAppend('C');
        }

        await Assert.That(result).IsEqualTo(expected);
    }

    /// <summary>Verifies TryAppend Char InsufficientSpace ReturnsFalse.</summary>
    [Test]
    public async Task TryAppend_Char_InsufficientSpace_ReturnsFalse()
    {
        bool result;
        {
            Span<char> buffer = stackalloc char[2];
            SpanStringBuilder builder = new(buffer);
            builder.Append("AB");
            result = builder.TryAppend('C');
        }

        await Assert.That(result).IsFalse();
    }

    /// <summary>Verifies TryAppend Null ReturnsTrue.</summary>
    [Test]
    public async Task TryAppend_Null_ReturnsTrue()
    {
        bool result;
        int length;
        {
            Span<char> buffer = stackalloc char[16];
            SpanStringBuilder builder = new(buffer);
            result = builder.TryAppend((string?)null);
            length = builder.Length;
        }

        await Assert.That(result).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    /// <summary>Verifies TryAppendHex2 InsufficientSpace ReturnsFalse.</summary>
    [Test]
    public async Task TryAppendHex2_InsufficientSpace_ReturnsFalse()
    {
        bool result;
        {
            Span<char> buffer = stackalloc char[1];
            SpanStringBuilder builder = new(buffer);
            result = builder.TryAppendHex2(0xAB);
        }

        await Assert.That(result).IsFalse();
    }

    /// <summary>Verifies TryAppendBinary64 InsufficientSpace ReturnsFalse.</summary>
    [Test]
    public async Task TryAppendBinary64_InsufficientSpace_ReturnsFalse()
    {
        bool result;
        {
            Span<char> buffer = stackalloc char[32];
            SpanStringBuilder builder = new(buffer);
            result = builder.TryAppendBinary64(ulong.MaxValue);
        }

        await Assert.That(result).IsFalse();
    }

    // ========================================================================
    // COMPLEX SCENARIOS
    // ========================================================================


    /// <summary>Verifies ChainedAppends BuildsCorrectString.</summary>
    [Test]
    public async Task ChainedAppends_BuildsCorrectString()
    {
        string content;
        {
            Span<char> buffer = stackalloc char[256];
            SpanStringBuilder builder = new(buffer);
            builder.Append("User: ");
            builder.Append(42);
            builder.Append(", Active: ");
            builder.Append(true);
            content = builder.AsSpan().ToString();
        }

        await Assert.That(content).IsEqualTo("User: 42, Active: True");
    }

    /// <summary>Verifies ClearAndReuse ClearAndReuse works.</summary>
    [Test]
    public async Task ClearAndReuse_Works()
    {
        string first;
        string second;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
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


    /// <summary>Verifies ExitCoverage TryAppend Primitive succeeds.</summary>
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
        bool result = _InvokeSpanStringTryAppend(kind, stackalloc char[128], expectSuccess: true);

        await Assert.That(result).IsTrue();
    }

    /// <summary>Verifies ExitCoverage TryAppend Primitive InsufficientSpace ReturnsFalse.</summary>
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
    public async Task ExitCoverage_TryAppend_Primitive_InsufficientSpace_ReturnsFalse(string kind)
    {
        bool result = _InvokeSpanStringTryAppend(kind, stackalloc char[1], expectSuccess: false);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Verifies ExitCoverage Append Primitive Overflow throws.</summary>
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
    public async Task ExitCoverage_Append_Primitive_Overflow_Throws(string kind)
    {
        bool threw = false;
        try { _InvokeSpanStringAppendThrow(kind, stackalloc char[0]); }
        catch (InvalidOperationException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies ExitCoverage AppendSpan AndAppendLineSpan OverflowThrow.</summary>
    [Test]
    public async Task ExitCoverage_AppendSpan_AndAppendLineSpan_OverflowThrow()
    {
        bool spanThrew = false;
        bool lineThrew = false;
        {
            SpanStringBuilder builder = new(stackalloc char[2]);
            try { builder.Append("ABC".AsSpan()); }
            catch (InvalidOperationException) { spanThrew = true; }

            SpanStringBuilder lineBuilder = new(stackalloc char[2]);
            try { lineBuilder.AppendLine("ABCDE".AsSpan()); }
            catch (InvalidOperationException) { lineThrew = true; }
        }

        await Assert.That(spanThrew).IsTrue();
        await Assert.That(lineThrew).IsTrue();
    }

    /// <summary>Verifies ExitCoverage TryAppendSpan FailureAndSuccess.</summary>
    [Test]
    public async Task ExitCoverage_TryAppendSpan_FailureAndSuccess()
    {
        bool failure;
        bool success;
        {
            SpanStringBuilder failBuilder = new(stackalloc char[2]);
            failure = failBuilder.TryAppend("ABC".AsSpan());

            SpanStringBuilder okBuilder = new(stackalloc char[8]);
            success = okBuilder.TryAppend("ABC".AsSpan());
        }

        await Assert.That(failure).IsFalse();
        await Assert.That(success).IsTrue();
    }

    /// <summary>Verifies ExitCoverage TryAppendHexBinary InsufficientSpace ReturnsFalse.</summary>
    [Test]
    [Arguments(nameof(SpanStringBuilder.TryAppendHex4), 4)]
    [Arguments(nameof(SpanStringBuilder.TryAppendHex8), 8)]
    [Arguments(nameof(SpanStringBuilder.TryAppendHex16), 16)]
    [Arguments(nameof(SpanStringBuilder.TryAppendBinary8), 8)]
    [Arguments(nameof(SpanStringBuilder.TryAppendBinary16), 16)]
    [Arguments(nameof(SpanStringBuilder.TryAppendBinary32), 32)]
    public async Task ExitCoverage_TryAppendHexBinary_InsufficientSpace_ReturnsFalse(string method, int requiredChars)
    {
        bool result = _InvokeSpanStringTryHexBinary(method, stackalloc char[Math.Max(0, requiredChars - 1)], expectSuccess: false);

        await Assert.That(result).IsFalse();
    }

    /// <summary>Verifies ExitCoverage AppendHexBinary Overflow throws.</summary>
    [Test]
    [Arguments(nameof(SpanStringBuilder.AppendHex4), 4)]
    [Arguments(nameof(SpanStringBuilder.AppendHex8), 8)]
    [Arguments(nameof(SpanStringBuilder.AppendHex16), 16)]
    [Arguments(nameof(SpanStringBuilder.AppendBinary8), 8)]
    [Arguments(nameof(SpanStringBuilder.AppendBinary16), 16)]
    [Arguments(nameof(SpanStringBuilder.AppendBinary32), 32)]
    [Arguments(nameof(SpanStringBuilder.AppendBinary64), 64)]
    public async Task ExitCoverage_AppendHexBinary_Overflow_Throws(string method, int requiredChars)
    {
        bool threw = false;
        try { _InvokeSpanStringAppendHexBinaryThrow(method, stackalloc char[Math.Max(0, requiredChars - 1)]); }
        catch (InvalidOperationException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies ExitCoverage AppendFormatted Overflow throws.</summary>
    [Test]
    [Arguments("int")]
    [Arguments("double")]
    [Arguments("float")]
    [Arguments("decimal")]
    [Arguments("DateTime")]
    [Arguments("DateTimeOffset")]
    [Arguments("TimeSpan")]
    [Arguments("long")]
    [Arguments("Guid")]
    public async Task ExitCoverage_AppendFormatted_Overflow_Throws(string kind)
    {
        bool threw = false;
        try { _InvokeSpanStringAppendFormattedThrow(kind, stackalloc char[1]); }
        catch (InvalidOperationException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    /// <summary>Verifies ExitCoverage TryAppendGeneric AndAppendGeneric Overflow.</summary>
    [Test]
    public async Task ExitCoverage_TryAppendGeneric_AndAppendGeneric_Overflow()
    {
        bool tryFailed;
        bool appendThrew = false;
        {
            SpanStringBuilder tryBuilder = new(stackalloc char[1]);
            tryFailed = tryBuilder.TryAppend(255, "X4", CultureInfo.InvariantCulture);

            SpanStringBuilder appendBuilder = new(stackalloc char[1]);
            try { appendBuilder.Append(255, "X4", CultureInfo.InvariantCulture); }
            catch (InvalidOperationException) { appendThrew = true; }
        }

        await Assert.That(tryFailed).IsFalse();
        await Assert.That(appendThrew).IsTrue();
    }

    /// <summary>Verifies ExitCoverage AppendSpanLongFormattedAndLine Succeed.</summary>
    [Test]
    public async Task ExitCoverage_AppendSpanLongFormattedAndLine_Succeed()
    {
        int length;
        {
            Span<char> buffer = stackalloc char[256];
            SpanStringBuilder builder = new(buffer);
            builder.Append("ab".AsSpan());
            builder.Append(-42L);
            builder.Append(100L, "D", CultureInfo.InvariantCulture);
            builder.Append(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(1)), "O", CultureInfo.InvariantCulture);
            builder.AppendLine("z".AsSpan());
            builder.Append(255, "X2", CultureInfo.InvariantCulture);
            length = builder.Length;
        }

        await Assert.That(length).IsGreaterThan(0);
    }

    /// <summary>Verifies ExitCoverage TryAppendBoolFalse InsufficientSpace ReturnsFalse.</summary>
    [Test]
    public async Task ExitCoverage_TryAppendBoolFalse_InsufficientSpace_ReturnsFalse()
    {
        bool result;
        {
            SpanStringBuilder builder = new(stackalloc char[4]);
            builder.Append("True".AsSpan());
            result = builder.TryAppend(false);
        }

        await Assert.That(result).IsFalse();
    }

    /// <summary>Verifies ExitCoverage TryAppendFormattedGeneric succeeds.</summary>
    [Test]
    public async Task ExitCoverage_TryAppendFormattedGeneric_Succeeds()
    {
        bool result;
        {
            SpanStringBuilder builder = new(stackalloc char[32]);
            result = builder.TryAppend(255, "X2");
        }

        await Assert.That(result).IsTrue();
    }

    /// <summary>Verifies ExitCoverage AppendFormattedGeneric succeeds.</summary>
    [Test]
    public async Task ExitCoverage_AppendFormattedGeneric_Succeeds()
    {
        int length;
        {
            Span<char> buffer = stackalloc char[32];
            SpanStringBuilder builder = new(buffer);
            builder.Append(255, "X2", CultureInfo.InvariantCulture);
            length = builder.Length;
        }

        await Assert.That(length).IsGreaterThan(0);
    }

    /// <summary>Verifies ExitCoverage AppendGenericFormattable ThrowAndSuccess.</summary>
    [Test]
    public async Task ExitCoverage_AppendGenericFormattable_ThrowAndSuccess()
    {
        bool threw = false;
        int length;
        {
            Span<char> buffer = stackalloc char[32];
            SpanStringBuilder okBuilder = new(buffer);
            okBuilder.Append<int>(42, "D");
            length = okBuilder.Length;

            try
            {
                SpanStringBuilder failBuilder = new(stackalloc char[1]);
                failBuilder.Append<int>(255, "X4");
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }
        }

        await Assert.That(length).IsGreaterThan(0);
        await Assert.That(threw).IsTrue();
    }

    private static bool _InvokeSpanStringTryAppend(string kind, Span<char> buffer, bool expectSuccess)
    {
        SpanStringBuilder builder = new(buffer);
        if (!expectSuccess && kind is not "bool")
        {
            builder.Append('X');
        }

        return kind switch
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
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown primitive kind."),
        };
    }

    private static void _InvokeSpanStringAppendThrow(string kind, Span<char> buffer)
    {
        SpanStringBuilder builder = new(buffer);
        switch (kind)
        {
            case "int": builder.Append(42); break;
            case "long": builder.Append(-99L); break;
            case "uint": builder.Append(42u); break;
            case "ulong": builder.Append(99ul); break;
            case "short": builder.Append((short)-12); break;
            case "ushort": builder.Append((ushort)65535); break;
            case "byte": builder.Append((byte)255); break;
            case "sbyte": builder.Append((sbyte)-1); break;
            case "bool": builder.Append(true); break;
            case "DateTime": builder.Append(new DateTime(2025, 6, 17)); break;
            case "DateTimeOffset": builder.Append(new DateTimeOffset(2025, 6, 17, 0, 0, 0, TimeSpan.Zero)); break;
            case "TimeSpan": builder.Append(TimeSpan.FromMinutes(90)); break;
            case "Guid": builder.Append(Guid.Parse("12345678-1234-1234-1234-123456789012")); break;
            case "Int128": builder.Append((Int128)(-1)); break;
            case "UInt128": builder.Append(UInt128.MaxValue); break;
            case "float": builder.Append(3.14f); break;
            case "double": builder.Append(2.718); break;
            case "decimal": builder.Append(1.5m); break;
            case "Half": builder.Append(Half.One); break;
            case "DateOnly": builder.Append(new DateOnly(2025, 6, 17)); break;
            case "TimeOnly": builder.Append(new TimeOnly(14, 30, 0)); break;
            case "nint": builder.Append((nint)7); break;
            case "nuint": builder.Append((nuint)8); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown primitive kind.");
        }
    }

    private static void _InvokeSpanStringAppendFormattedThrow(string kind, Span<char> buffer)
    {
        SpanStringBuilder builder = new(buffer);
        switch (kind)
        {
            case "int": builder.Append(42, "X4", CultureInfo.InvariantCulture); break;
            case "long": builder.Append(100L, "D", CultureInfo.InvariantCulture); break;
            case "double": builder.Append(2.718, "F4", CultureInfo.InvariantCulture); break;
            case "float": builder.Append(3.14f, "F2", CultureInfo.InvariantCulture); break;
            case "decimal": builder.Append(1.5m, "C", CultureInfo.InvariantCulture); break;
            case "DateTime": builder.Append(new DateTime(2025, 6, 17), "yyyy-MM-dd", CultureInfo.InvariantCulture); break;
            case "DateTimeOffset": builder.Append(new DateTimeOffset(2025, 6, 17, 0, 0, 0, TimeSpan.Zero), "O", CultureInfo.InvariantCulture); break;
            case "TimeSpan": builder.Append(TimeSpan.FromMinutes(90), "c", CultureInfo.InvariantCulture); break;
            case "Guid": builder.Append(Guid.Parse("12345678-1234-1234-1234-123456789012"), "N"); break;
            default: throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown formatted kind.");
        }
    }

    private static bool _InvokeSpanStringTryHexBinary(string method, Span<char> buffer, bool expectSuccess)
    {
        SpanStringBuilder builder = new(buffer);
        return method switch
        {
            nameof(SpanStringBuilder.TryAppendHex4) => builder.TryAppendHex4(0x1234),
            nameof(SpanStringBuilder.TryAppendHex8) => builder.TryAppendHex8(0x12345678u),
            nameof(SpanStringBuilder.TryAppendHex16) => builder.TryAppendHex16(0x0102030405060708UL),
            nameof(SpanStringBuilder.TryAppendBinary8) => builder.TryAppendBinary8(0b10101010),
            nameof(SpanStringBuilder.TryAppendBinary16) => builder.TryAppendBinary16(0b1010),
            nameof(SpanStringBuilder.TryAppendBinary32) => builder.TryAppendBinary32(1u),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown method."),
        };
    }

    private static void _InvokeSpanStringAppendHexBinaryThrow(string method, Span<char> buffer)
    {
        SpanStringBuilder builder = new(buffer);
        switch (method)
        {
            case nameof(SpanStringBuilder.AppendHex4): builder.AppendHex4(0x1234); break;
            case nameof(SpanStringBuilder.AppendHex8): builder.AppendHex8(0x12345678u); break;
            case nameof(SpanStringBuilder.AppendHex16): builder.AppendHex16(0x0102030405060708UL); break;
            case nameof(SpanStringBuilder.AppendBinary8): builder.AppendBinary8(0b10101010); break;
            case nameof(SpanStringBuilder.AppendBinary16): builder.AppendBinary16(0b1010); break;
            case nameof(SpanStringBuilder.AppendBinary32): builder.AppendBinary32(1u); break;
            case nameof(SpanStringBuilder.AppendBinary64): builder.AppendBinary64(2UL); break;
            default: throw new ArgumentOutOfRangeException(nameof(method), method, "Unknown method.");
        }
    }
}
