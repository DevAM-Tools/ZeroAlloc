// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="LazyString"/>: direct mode, lazy evaluation, deferred format,
/// concatenation, implicit conversion, equality, and thread safety.
/// </summary>
public sealed class LazyStringTests
{

    /// <summary>Verifies DirectString ReturnsValue.</summary>
    [Test]
    public async Task DirectString_ReturnsValue()
    {
        LazyString s = new("hello");
        await Assert.That(s.AsString).IsEqualTo("hello");
        await Assert.That(s.IsEvaluated).IsTrue();
        await Assert.That(s.IsLazy).IsFalse();
    }
    /// <summary>Verifies Empty ReturnsEmptyString.</summary>
    [Test]
    public async Task Empty_ReturnsEmptyString()
    {
        LazyString s = LazyString.Empty;
        await Assert.That(s.AsString).IsEqualTo(string.Empty);
        await Assert.That(s.IsEmpty).IsTrue();
        await Assert.That(s.Length).IsEqualTo(0);
    }
    /// <summary>Verifies DefaultIsNull.</summary>
    [Test]
    public async Task DefaultIsNull()
    {
        LazyString s = default;
        await Assert.That(s.IsNull).IsTrue();
        await Assert.That(s.IsEmpty).IsTrue();
        await Assert.That(s.AsString).IsEqualTo(string.Empty);
    }
    /// <summary>Verifies Lazy DefersEvaluation.</summary>
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
    /// <summary>Verifies Lazy CachesResult.</summary>
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
    /// <summary>Verifies FormatLazy WithState DefersEvaluation.</summary>
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
    /// <summary>Verifies FormatLazy WithTupleState.</summary>
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
    /// <summary>Verifies Append CombinesStrings.</summary>
    [Test]
    public async Task Append_CombinesStrings()
    {
        LazyString a = new("hello");
        LazyString b = new(" world");
        LazyString combined = a.Append(b);
        await Assert.That(combined.AsString).IsEqualTo("hello world");
    }
    /// <summary>Verifies Append EmptyOptimization.</summary>
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
    /// <summary>Verifies Append DirectEmptyString ReturnsOther.</summary>
    [Test]
    [Arguments("")]
    public async Task Append_DirectEmptyString_ReturnsOther(string emptyText)
    {
        LazyString a = new("hello");
        LazyString empty = new(emptyText);
        LazyString result = a.Append(empty);
        await Assert.That(result.AsString).IsEqualTo("hello");
    }
    /// <summary>Verifies Append DirectEmptyString OnOtherSide ReturnsThis.</summary>
    [Test]
    public async Task Append_DirectEmptyString_OnOtherSide_ReturnsThis()
    {
        LazyString empty = new("");
        LazyString value = new("hello");
        LazyString result = empty.Append(value);
        await Assert.That(result.AsString).IsEqualTo("hello");
    }
    /// <summary>Verifies Append NullOptimization.</summary>
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
    /// <summary>Verifies Prepend CombinesStrings.</summary>
    [Test]
    public async Task Prepend_CombinesStrings()
    {
        LazyString a = new("world");
        LazyString b = new("hello ");
        LazyString combined = a.Prepend(b);
        await Assert.That(combined.AsString).IsEqualTo("hello world");
    }
    /// <summary>Verifies ImplicitFromString.</summary>
    [Test]
    public async Task ImplicitFromString()
    {
        LazyString s = "implicit";
        await Assert.That(s.AsString).IsEqualTo("implicit");
    }
    /// <summary>Verifies ImplicitToString.</summary>
    [Test]
    public async Task ImplicitToString()
    {
        LazyString s = new("value");
        string result = s;
        await Assert.That(result).IsEqualTo("value");
    }
    /// <summary>Verifies Equality SameValue.</summary>
    [Test]
    public async Task Equality_SameValue()
    {
        LazyString a = new("test");
        LazyString b = new("test");
        await Assert.That(a.Equals(b)).IsTrue();
        await Assert.That(a == b).IsTrue();
    }
    /// <summary>Verifies Equality DifferentValue.</summary>
    [Test]
    public async Task Equality_DifferentValue()
    {
        LazyString a = new("test");
        LazyString c = new("other");
        await Assert.That(a.Equals(c)).IsFalse();
        await Assert.That(a != c).IsTrue();
    }
    /// <summary>Verifies Equality BothDefault.</summary>
    [Test]
    public async Task Equality_BothDefault()
    {
        LazyString a = default;
        LazyString b = default;
        await Assert.That(a == b).IsTrue();
    }
    /// <summary>Verifies Equality DefaultVsValue.</summary>
    [Test]
    public async Task Equality_DefaultVsValue()
    {
        LazyString a = new("test");
        LazyString b = default;
        await Assert.That(a != b).IsTrue();
    }
    /// <summary>Verifies CompareTo Ordering.</summary>
    [Test]
    public async Task CompareTo_Ordering()
    {
        LazyString alpha = new("alpha");
        LazyString beta = new("beta");
        await Assert.That(alpha.CompareTo(beta) < 0).IsTrue();
        await Assert.That(beta.CompareTo(alpha) > 0).IsTrue();
        await Assert.That(alpha.CompareTo(alpha)).IsEqualTo(0);
    }
    /// <summary>Verifies CompareTo DefaultIsSmallest.</summary>
    [Test]
    public async Task CompareTo_DefaultIsSmallest()
    {
        LazyString a = new("test");
        LazyString absent = default;
        await Assert.That(a.CompareTo(absent) > 0).IsTrue();
        await Assert.That(absent.CompareTo(a) < 0).IsTrue();
    }
    /// <summary>Verifies CompareTo BothNull ReturnsZero.</summary>
    [Test]
    public async Task CompareTo_BothNull_ReturnsZero()
    {
        LazyString left = default;
        LazyString right = default;
        await Assert.That(left.CompareTo(right)).IsEqualTo(0);
    }
    /// <summary>Verifies IsEmpty LazyDeferredValue UsesEvaluatedLength.</summary>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task IsEmpty_LazyDeferredValue_UsesEvaluatedLength(bool useFormatLazy)
    {
        LazyString lazy = useFormatLazy
            ? LazyString.FormatLazy(0, static _ => string.Empty)
            : LazyString.Lazy(static () => string.Empty);

        await Assert.That(lazy.IsLazy).IsTrue();
        await Assert.That(lazy.IsEmpty).IsTrue();
    }
    /// <summary>Verifies GetHashCode NullValue ReturnsZero.</summary>
    [Test]
    public async Task GetHashCode_NullValue_ReturnsZero()
    {
        LazyString absent = default;
        await Assert.That(absent.GetHashCode()).IsEqualTo(0);
    }
    /// <summary>Verifies FromRawValue UnknownType EvaluatesToEmpty.</summary>
    [Test]
    public async Task FromRawValue_UnknownType_EvaluatesToEmpty()
    {
        LazyString s = LazyString.FromRawValue(42);
        await Assert.That(s.AsString).IsEqualTo(string.Empty);
        await Assert.That(s.TryGetString(out string result)).IsTrue();
        await Assert.That(result).IsEqualTo(string.Empty);
    }
    /// <summary>Verifies Length ReturnsCorrectValue.</summary>
    [Test]
    public async Task Length_ReturnsCorrectValue()
    {
        LazyString s = new("12345");
        await Assert.That(s.Length).IsEqualTo(5);
    }
    /// <summary>Verifies ToString ReturnsAsString.</summary>
    [Test]
    public async Task ToString_ReturnsAsString()
    {
        LazyString s = new("test string");
        await Assert.That(s.ToString()).IsEqualTo("test string");
    }
    /// <summary>Verifies GetHashCode ConsistentWithEquals.</summary>
    [Test]
    public async Task GetHashCode_ConsistentWithEquals()
    {
        LazyString a = new("test");
        LazyString b = new("test");
        await Assert.That(b.GetHashCode()).IsEqualTo(a.GetHashCode());
    }
    /// <summary>Verifies TryWriteTo Success.</summary>
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
    /// <summary>Verifies TryWriteTo BufferTooSmall.</summary>
    [Test]
    public async Task TryWriteTo_BufferTooSmall()
    {
        LazyString s = new("hello");
        Span<char> buffer = stackalloc char[3];
        int written = s.TryWriteTo(buffer);
        await Assert.That(written).IsEqualTo(-1);
    }
    /// <summary>Verifies AsSpan ReturnsCorrectSpan.</summary>
    [Test]
    public async Task AsSpan_ReturnsCorrectSpan()
    {
        LazyString s = new("hello");
        ReadOnlySpan<char> span = s.AsSpan;
        await Assert.That(span.ToString()).IsEqualTo("hello");
    }
    /// <summary>Verifies RawValue RawValue roundtrips.</summary>
    [Test]
    public async Task RawValue_RoundTrips()
    {
        LazyString original = new("test");
        object? raw = original.RawValue;
        LazyString restored = LazyString.FromRawValue(raw);
        await Assert.That(restored).IsEqualTo(original);
    }
    /// <summary>Verifies TryGetString DirectString ReturnsTrue.</summary>
    [Test]
    public async Task TryGetString_DirectString_ReturnsTrue()
    {
        LazyString s = new("hello");
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo("hello");
    }
    /// <summary>Verifies TryGetString Default ReturnsTrueWithEmpty.</summary>
    [Test]
    public async Task TryGetString_Default_ReturnsTrueWithEmpty()
    {
        LazyString s = default;
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo(string.Empty);
    }
    /// <summary>Verifies TryGetString LazySuccess ReturnsTrueWithValue.</summary>
    [Test]
    public async Task TryGetString_LazySuccess_ReturnsTrueWithValue()
    {
        LazyString s = LazyString.Lazy(() => "lazy result");
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo("lazy result");
    }
    /// <summary>Verifies TryGetString FormatLazy succeeds.</summary>
    [Test]
    public async Task TryGetString_FormatLazy_Succeeds()
    {
        LazyString s = LazyString.FormatLazy(7, static n => $"N={n}");
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsTrue();
        await Assert.That(result).IsEqualTo("N=7");
    }
    /// <summary>Verifies TryGetString FormatLazyThrows ReturnsFalseWithEmpty.</summary>
    [Test]
    public async Task TryGetString_FormatLazyThrows_ReturnsFalseWithEmpty()
    {
        LazyString s = LazyString.FormatLazy(0, static _ => throw new InvalidOperationException("boom"));
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsFalse();
        await Assert.That(result).IsEqualTo(string.Empty);
    }
    /// <summary>Verifies TryGetString LazyThrows ReturnsFalseWithEmpty.</summary>
    [Test]
    public async Task TryGetString_LazyThrows_ReturnsFalseWithEmpty()
    {
        LazyString s = LazyString.Lazy(() => throw new InvalidOperationException("boom"));
        bool success = s.TryGetString(out string result);
        await Assert.That(success).IsFalse();
        await Assert.That(result).IsEqualTo(string.Empty);
    }
    /// <summary>Verifies TryGetString LazyThrows CachesEmptyToPreventRetry.</summary>
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
    /// <summary>Verifies Append MultipleParts.</summary>
    [Test]
    public async Task Append_MultipleParts()
    {
        LazyString a = new("a");
        LazyString b = new("b");
        LazyString c = new("c");
        LazyString combined = a.Append(b).Append(c);
        await Assert.That(combined.AsString).IsEqualTo("abc");
    }
    /// <summary>Verifies FormatLazy DeferredFormat IsLazy.</summary>
    [Test]
    public async Task FormatLazy_DeferredFormat_IsLazy()
    {
        LazyString s = LazyString.FormatLazy(42, static n => $"Number: {n}");
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(s.IsEvaluated).IsFalse();
        _ = s.AsString;
        await Assert.That(s.IsEvaluated).IsTrue();
    }
    /// <summary>Verifies Equals ObjectOverload.</summary>
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

    /// <summary>Verifies GeneratedLazy SingleArg DefersAndFormats.</summary>
    [Test]
    public async Task GeneratedLazy_SingleArg_DefersAndFormats()
    {
        LazyString s = ZA.Lazy("hello");
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(s.AsString).IsEqualTo("hello");
    }
    /// <summary>Verifies GeneratedLazy MultipleArgs DefersAndFormats.</summary>
    [Test]
    public async Task GeneratedLazy_MultipleArgs_DefersAndFormats()
    {
        LazyString s = ZA.Lazy("Port: ", (ushort)443);
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(s.AsString).IsEqualTo("Port: 443");
    }
    /// <summary>Verifies GeneratedLazy ThreeArgs FormatsAll.</summary>
    [Test]
    public async Task GeneratedLazy_ThreeArgs_FormatsAll()
    {
        LazyString s = ZA.Lazy("Source: ", (ushort)80, " -> Dest: ");
        await Assert.That(s.AsString).IsEqualTo("Source: 80 -> Dest: ");
    }
    /// <summary>Verifies GeneratedLazy CachesOnSecondAccess.</summary>
    [Test]
    public async Task GeneratedLazy_CachesOnSecondAccess()
    {
        LazyString s = ZA.Lazy("cached: ", 42);
        string first = s.AsString;
        string second = s.AsString;
        await Assert.That(second).IsSameReferenceAs(first);
    }
    /// <summary>Verifies GeneratedLazyInterpolated SingleArg DefersAndFormats.</summary>
    [Test]
    public async Task GeneratedLazyInterpolated_SingleArg_DefersAndFormats()
    {
        LazyString s = ZA.LazyInterpolated(42);
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(s.AsString).IsEqualTo("42");
    }
    /// <summary>Verifies GeneratedLazyInterpolated MultipleArgs DefersAndFormats.</summary>
    [Test]
    public async Task GeneratedLazyInterpolated_MultipleArgs_DefersAndFormats()
    {
        LazyString s = ZA.LazyInterpolated("Port: ", (ushort)443);
        await Assert.That(s.IsLazy).IsTrue();
        await Assert.That(s.AsString).IsEqualTo("Port: 443");
    }
    /// <summary>Verifies GeneratedLazyInterpolated CachesOnSecondAccess.</summary>
    [Test]
    public async Task GeneratedLazyInterpolated_CachesOnSecondAccess()
    {
        LazyString s = ZA.LazyInterpolated("value=", 99);
        string first = s.AsString;
        string second = s.AsString;
        await Assert.That(second).IsSameReferenceAs(first);
    }
}
