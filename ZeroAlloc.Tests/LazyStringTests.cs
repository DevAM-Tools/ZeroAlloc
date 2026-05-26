// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="LazyString"/>: direct mode, lazy evaluation, deferred format,
/// concatenation, implicit conversion, equality, and thread safety.
/// </summary>
public sealed class LazyStringTests
{
    [Test]
    public async Task DirectString_ReturnsValue()
    {
        LazyString s = new("hello");
        await Assert.That(s.AsString).IsEqualTo("hello");
        await Assert.That(s.IsEvaluated).IsTrue();
        await Assert.That(s.IsLazy).IsFalse();
    }

    [Test]
    public async Task Empty_ReturnsEmptyString()
    {
        LazyString s = LazyString.Empty;
        await Assert.That(s.AsString).IsEqualTo(string.Empty);
        await Assert.That(s.IsEmpty).IsTrue();
        await Assert.That(s.Length).IsEqualTo(0);
    }

    [Test]
    public async Task DefaultIsNull()
    {
        LazyString s = default;
        await Assert.That(s.IsNull).IsTrue();
        await Assert.That(s.IsEmpty).IsTrue();
        await Assert.That(s.AsString).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Lazy_DefersEvaluation()
    {
        int callCount = 0;
        LazyString s = LazyString.Lazy(() =>
        {
            callCount++;
            return "lazy result";
        });

        // Not yet evaluated
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(callCount).IsEqualTo(0);

        // First evaluation
        string result = s.AsString;
        await Assert.That(result).IsEqualTo("lazy result");
        await Assert.That(callCount).IsEqualTo(1);
    }

    [Test]
    public async Task Lazy_CachesResult()
    {
        int callCount = 0;
        LazyString s = LazyString.Lazy(() =>
        {
            callCount++;
            return "cached";
        });

        _ = s.AsString;
        _ = s.AsString;
        _ = s.AsString;

        // Factory should only be called once (in struct copy, may be called once per copy)
        await Assert.That(callCount).IsEqualTo(1);
        await Assert.That(s.IsEvaluated).IsTrue();
    }

    [Test]
    public async Task FormatLazy_WithState_DefersEvaluation()
    {
        LazyString s = LazyString.FormatLazy(42, static state =>
        {
            return $"Value: {state}";
        });

        await Assert.That(s.IsLazy).IsTrue();

        string result = s.AsString;
        await Assert.That(result).IsEqualTo("Value: 42");
    }

    [Test]
    public async Task FormatLazy_WithTupleState()
    {
        ushort srcPort = 443;
        ushort dstPort = 8080;
        LazyString s = LazyString.FormatLazy(
            (srcPort, dstPort),
            static state => $"Src: {state.Item1}, Dst: {state.Item2}");

        await Assert.That(s.AsString).IsEqualTo("Src: 443, Dst: 8080");
    }

    [Test]
    public async Task Append_CombinesStrings()
    {
        LazyString a = new("hello");
        LazyString b = new(" world");
        LazyString combined = a.Append(b);
        await Assert.That(combined.AsString).IsEqualTo("hello world");
    }

    [Test]
    public async Task Append_EmptyOptimization()
    {
        LazyString a = new("hello");
        LazyString empty = LazyString.Empty;

        LazyString result1 = a.Append(empty);
        await Assert.That(result1.AsString).IsEqualTo("hello");

        LazyString result2 = empty.Append(a);
        await Assert.That(result2.AsString).IsEqualTo("hello");
    }

    [Test]
    public async Task Append_NullOptimization()
    {
        LazyString a = new("hello");
        LazyString absent = default;

        LazyString result1 = a.Append(absent);
        await Assert.That(result1.AsString).IsEqualTo("hello");

        LazyString result2 = absent.Append(a);
        await Assert.That(result2.AsString).IsEqualTo("hello");
    }

    [Test]
    public async Task Prepend_CombinesStrings()
    {
        LazyString a = new("world");
        LazyString b = new("hello ");
        LazyString combined = a.Prepend(b);
        await Assert.That(combined.AsString).IsEqualTo("hello world");
    }

    [Test]
    public async Task ImplicitFromString()
    {
        LazyString s = "implicit";
        await Assert.That(s.AsString).IsEqualTo("implicit");
    }

    [Test]
    public async Task ImplicitToString()
    {
        LazyString s = new("value");
        string result = s;
        await Assert.That(result).IsEqualTo("value");
    }

    [Test]
    public async Task Equality_SameValue()
    {
        LazyString a = new("test");
        LazyString b = new("test");
        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a == b).IsTrue();
    }

    [Test]
    public async Task Equality_DifferentValue()
    {
        LazyString a = new("test");
        LazyString c = new("other");
        await Assert.That(a.Equals(c)).IsFalse();
        await Assert.That(a != c).IsTrue();
    }

    [Test]
    public async Task Equality_BothDefault()
    {
        LazyString a = default;
        LazyString b = default;
        await Assert.That(a == b).IsTrue();
    }

    [Test]
    public async Task Equality_DefaultVsValue()
    {
        LazyString a = new("test");
        LazyString b = default;
        await Assert.That(a != b).IsTrue();
    }

    [Test]
    public async Task CompareTo_Ordering()
    {
        LazyString alpha = new("alpha");
        LazyString beta = new("beta");
        await Assert.That(alpha.CompareTo(beta) < 0).IsTrue();
        await Assert.That(beta.CompareTo(alpha) > 0).IsTrue();
        await Assert.That(alpha.CompareTo(alpha)).IsEqualTo(0);
    }

    [Test]
    public async Task CompareTo_DefaultIsSmallest()
    {
        LazyString a = new("test");
        LazyString absent = default;
        await Assert.That(a.CompareTo(absent) > 0).IsTrue();
        await Assert.That(absent.CompareTo(a) < 0).IsTrue();
    }

    [Test]
    public async Task Length_ReturnsCorrectValue()
    {
        LazyString s = new("12345");
        await Assert.That(s.Length).IsEqualTo(5);
    }

    [Test]
    public async Task ToString_ReturnsAsString()
    {
        LazyString s = new("test string");
        await Assert.That(s.ToString()).IsEqualTo("test string");
    }

    [Test]
    public async Task GetHashCode_ConsistentWithEquals()
    {
        LazyString a = new("test");
        LazyString b = new("test");
        await Assert.That(b.GetHashCode()).IsEqualTo(a.GetHashCode());
    }

    [Test]
    public async Task TryWriteTo_Success()
    {
        LazyString s = new("hello");
        int written;
        string writtenContent;
        {
            Span<char> buffer = stackalloc char[10];
            written = s.TryWriteTo(buffer);
            writtenContent = buffer[..written].ToString();
        }

        await Assert.That(written).IsEqualTo(5);
        await Assert.That(writtenContent).IsEqualTo("hello");
    }

    [Test]
    public async Task TryWriteTo_BufferTooSmall()
    {
        LazyString s = new("hello");
        Span<char> buffer = stackalloc char[3];
        int written = s.TryWriteTo(buffer);
        await Assert.That(written).IsEqualTo(-1);
    }

    [Test]
    public async Task AsSpan_ReturnsCorrectSpan()
    {
        LazyString s = new("hello");
        ReadOnlySpan<char> span = s.AsSpan;
        await Assert.That(span.ToString()).IsEqualTo("hello");
    }

    [Test]
    public async Task RawValue_RoundTrips()
    {
        LazyString original = new("test");
        object? raw = original.RawValue;
        LazyString restored = LazyString.FromRawValue(raw);
        await Assert.That(restored).IsEqualTo(original);
    }

    [Test]
    public async Task TryGetString_DirectString_ReturnsTrue()
    {
        LazyString s = new("hello");
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo("hello");
    }

    [Test]
    public async Task TryGetString_Default_ReturnsTrueWithEmpty()
    {
        LazyString s = default;
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task TryGetString_LazySuccess_ReturnsTrueWithValue()
    {
        LazyString s = LazyString.Lazy(() => "lazy result");
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo("lazy result");
    }

    [Test]
    public async Task TryGetString_LazyThrows_ReturnsFalseWithEmpty()
    {
        LazyString s = LazyString.Lazy(() => throw new InvalidOperationException("boom"));
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsFalse();
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task TryGetString_LazyThrows_CachesEmptyToPreventRetry()
    {
        int callCount = 0;
        LazyString s = LazyString.Lazy(() =>
        {
            callCount++;
            throw new InvalidOperationException("boom");
        });

        // First call — catches exception, caches empty
        _ = s.TryGetString(out _);
        await Assert.That(callCount).IsEqualTo(1);

        // Second call — should see cached empty string
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo(string.Empty);
        await Assert.That(callCount).IsEqualTo(1);
    }

    [Test]
    public async Task Append_MultipleParts()
    {
        LazyString a = new("a");
        LazyString b = new("b");
        LazyString c = new("c");
        LazyString combined = a.Append(b).Append(c);
        await Assert.That(combined.AsString).IsEqualTo("abc");
    }

    [Test]
    public async Task FormatLazy_DeferredFormat_IsLazy()
    {
        LazyString s = LazyString.FormatLazy(42, static n => $"Number: {n}");
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(s.IsEvaluated).IsFalse();
        _ = s.AsString;
        await Assert.That(s.IsEvaluated).IsTrue();
    }

    [Test]
    public async Task Equals_ObjectOverload()
    {
        LazyString a = new("test");
        await Assert.That(a.Equals((object)new LazyString("test"))).IsTrue();
        await Assert.That(a.Equals((object?)null)).IsFalse();
        await Assert.That(a.Equals((object)"test")).IsFalse(); // not a LazyString
    }

    // ========================================================================
    // Generator-powered Lazy() and LazyInterpolated() tests
    // ========================================================================

    [Test]
    public async Task GeneratedLazy_SingleArg_DefersAndFormats()
    {
        LazyString s = ZA.Lazy("hello");
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(s.AsString).IsEqualTo("hello");
    }

    [Test]
    public async Task GeneratedLazy_MultipleArgs_DefersAndFormats()
    {
        LazyString s = ZA.Lazy("Port: ", (ushort)443);
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(s.AsString).IsEqualTo("Port: 443");
    }

    [Test]
    public async Task GeneratedLazy_ThreeArgs_FormatsAll()
    {
        LazyString s = ZA.Lazy("Source: ", (ushort)80, " -> Dest: ");
        await Assert.That(s.AsString).IsEqualTo("Source: 80 -> Dest: ");
    }

    [Test]
    public async Task GeneratedLazy_CachesOnSecondAccess()
    {
        LazyString s = ZA.Lazy("cached: ", 42);
        string first = s.AsString;
        string second = s.AsString;
        await Assert.That(second).IsSameReferenceAs(first);
    }

    [Test]
    public async Task GeneratedLazyInterpolated_SingleArg_DefersAndFormats()
    {
        LazyString s = ZA.LazyInterpolated(42);
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(s.AsString).IsEqualTo("42");
    }

    [Test]
    public async Task GeneratedLazyInterpolated_MultipleArgs_DefersAndFormats()
    {
        LazyString s = ZA.LazyInterpolated("Port: ", (ushort)443);
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(s.AsString).IsEqualTo("Port: 443");
    }

    [Test]
    public async Task GeneratedLazyInterpolated_CachesOnSecondAccess()
    {
        LazyString s = ZA.LazyInterpolated("value=", 99);
        string first = s.AsString;
        string second = s.AsString;
        await Assert.That(second).IsSameReferenceAs(first);
    }
}