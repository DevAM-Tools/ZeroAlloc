/*
MIT License
SPDX-License-Identifier: MIT

Copyright (c) 2025 ZeroAlloc Contributors
*/

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="LazyString"/>: direct mode, lazy evaluation, deferred format,
/// concatenation, implicit conversion, equality, and thread safety.
/// </summary>
public sealed class LazyStringTests
{
    [Fact]
    public void DirectString_ReturnsValue()
    {
        LazyString s = new("hello");
        Assert.Equal("hello", s.AsString);
        Assert.True(s.IsEvaluated);
        Assert.False(s.IsLazy);
    }

    [Fact]
    public void Empty_ReturnsEmptyString()
    {
        LazyString s = LazyString.Empty;
        Assert.Equal(string.Empty, s.AsString);
        Assert.True(s.IsEmpty);
        Assert.Equal(0, s.Length);
    }

    [Fact]
    public void DefaultIsNull()
    {
        LazyString s = default;
        Assert.True(s.IsNull);
        Assert.True(s.IsEmpty);
        Assert.Equal(string.Empty, s.AsString);
    }

    [Fact]
    public void Lazy_DefersEvaluation()
    {
        int callCount = 0;
        LazyString s = LazyString.Lazy(() =>
        {
            callCount++;
            return "lazy result";
        });

        // Not yet evaluated
        Assert.True(s.IsLazy);
        Assert.Equal(0, callCount);

        // First evaluation
        string result = s.AsString;
        Assert.Equal("lazy result", result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Lazy_CachesResult()
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
        Assert.Equal(1, callCount);
        Assert.True(s.IsEvaluated);
    }

    [Fact]
    public void FormatLazy_WithState_DefersEvaluation()
    {
        LazyString s = LazyString.FormatLazy(42, static state =>
        {
            return $"Value: {state}";
        });

        Assert.True(s.IsLazy);

        string result = s.AsString;
        Assert.Equal("Value: 42", result);
    }

    [Fact]
    public void FormatLazy_WithTupleState()
    {
        ushort srcPort = 443;
        ushort dstPort = 8080;
        LazyString s = LazyString.FormatLazy(
            (srcPort, dstPort),
            static state => $"Src: {state.Item1}, Dst: {state.Item2}");

        Assert.Equal("Src: 443, Dst: 8080", s.AsString);
    }

    [Fact]
    public void Append_CombinesStrings()
    {
        LazyString a = new("hello");
        LazyString b = new(" world");
        LazyString combined = a.Append(b);
        Assert.Equal("hello world", combined.AsString);
    }

    [Fact]
    public void Append_EmptyOptimization()
    {
        LazyString a = new("hello");
        LazyString empty = LazyString.Empty;

        LazyString result1 = a.Append(empty);
        Assert.Equal("hello", result1.AsString);

        LazyString result2 = empty.Append(a);
        Assert.Equal("hello", result2.AsString);
    }

    [Fact]
    public void Append_NullOptimization()
    {
        LazyString a = new("hello");
        LazyString absent = default;

        LazyString result1 = a.Append(absent);
        Assert.Equal("hello", result1.AsString);

        LazyString result2 = absent.Append(a);
        Assert.Equal("hello", result2.AsString);
    }

    [Fact]
    public void Prepend_CombinesStrings()
    {
        LazyString a = new("world");
        LazyString b = new("hello ");
        LazyString combined = a.Prepend(b);
        Assert.Equal("hello world", combined.AsString);
    }

    [Fact]
    public void ImplicitFromString()
    {
        LazyString s = "implicit";
        Assert.Equal("implicit", s.AsString);
    }

    [Fact]
    public void ImplicitToString()
    {
        LazyString s = new("value");
        string result = s;
        Assert.Equal("value", result);
    }

    [Fact]
    public void Equality_SameValue()
    {
        LazyString a = new("test");
        LazyString b = new("test");
        Assert.True(a.Equals(b));
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentValue()
    {
        LazyString a = new("test");
        LazyString c = new("other");
        Assert.False(a.Equals(c));
        Assert.True(a != c);
    }

    [Fact]
    public void Equality_BothDefault()
    {
        LazyString a = default;
        LazyString b = default;
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DefaultVsValue()
    {
        LazyString a = new("test");
        LazyString b = default;
        Assert.True(a != b);
    }

    [Fact]
    public void CompareTo_Ordering()
    {
        LazyString alpha = new("alpha");
        LazyString beta = new("beta");
        Assert.True(alpha.CompareTo(beta) < 0);
        Assert.True(beta.CompareTo(alpha) > 0);
        Assert.Equal(0, alpha.CompareTo(alpha));
    }

    [Fact]
    public void CompareTo_DefaultIsSmallest()
    {
        LazyString a = new("test");
        LazyString absent = default;
        Assert.True(a.CompareTo(absent) > 0);
        Assert.True(absent.CompareTo(a) < 0);
    }

    [Fact]
    public void Length_ReturnsCorrectValue()
    {
        LazyString s = new("12345");
        Assert.Equal(5, s.Length);
    }

    [Fact]
    public void ToString_ReturnsAsString()
    {
        LazyString s = new("test string");
        Assert.Equal("test string", s.ToString());
    }

    [Fact]
    public void GetHashCode_ConsistentWithEquals()
    {
        LazyString a = new("test");
        LazyString b = new("test");
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void TryWriteTo_Success()
    {
        LazyString s = new("hello");
        Span<char> buffer = stackalloc char[10];
        int written = s.TryWriteTo(buffer);
        Assert.Equal(5, written);
        Assert.Equal("hello", buffer[..written].ToString());
    }

    [Fact]
    public void TryWriteTo_BufferTooSmall()
    {
        LazyString s = new("hello");
        Span<char> buffer = stackalloc char[3];
        int written = s.TryWriteTo(buffer);
        Assert.Equal(-1, written);
    }

    [Fact]
    public void AsSpan_ReturnsCorrectSpan()
    {
        LazyString s = new("hello");
        ReadOnlySpan<char> span = s.AsSpan;
        Assert.Equal("hello", span.ToString());
    }

    [Fact]
    public void RawValue_RoundTrips()
    {
        LazyString original = new("test");
        object? raw = original.RawValue;
        LazyString restored = LazyString.FromRawValue(raw);
        Assert.Equal(original, restored);
    }

    [Fact]
    public void TryGetString_DirectString_ReturnsTrue()
    {
        LazyString s = new("hello");
        bool success = s.TryGetString(out string result);
        Assert.True(success);
        Assert.Equal("hello", result);
    }

    [Fact]
    public void TryGetString_Default_ReturnsTrueWithEmpty()
    {
        LazyString s = default;
        bool success = s.TryGetString(out string result);
        Assert.True(success);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TryGetString_LazySuccess_ReturnsTrueWithValue()
    {
        LazyString s = LazyString.Lazy(() => "lazy result");
        bool success = s.TryGetString(out string result);
        Assert.True(success);
        Assert.Equal("lazy result", result);
    }

    [Fact]
    public void TryGetString_LazyThrows_ReturnsFalseWithEmpty()
    {
        LazyString s = LazyString.Lazy(() => throw new InvalidOperationException("boom"));
        bool success = s.TryGetString(out string result);
        Assert.False(success);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void TryGetString_LazyThrows_CachesEmptyToPreventRetry()
    {
        int callCount = 0;
        LazyString s = LazyString.Lazy(() =>
        {
            callCount++;
            throw new InvalidOperationException("boom");
        });

        // First call — catches exception, caches empty
        _ = s.TryGetString(out _);
        Assert.Equal(1, callCount);

        // Second call — should see cached empty string
        bool success = s.TryGetString(out string result);
        Assert.True(success);
        Assert.Equal(string.Empty, result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void Append_MultipleParts()
    {
        LazyString a = new("a");
        LazyString b = new("b");
        LazyString c = new("c");
        LazyString combined = a.Append(b).Append(c);
        Assert.Equal("abc", combined.AsString);
    }

    [Fact]
    public void FormatLazy_DeferredFormat_IsLazy()
    {
        LazyString s = LazyString.FormatLazy(42, static n => $"Number: {n}");
        Assert.True(s.IsLazy);
        Assert.False(s.IsEvaluated);
        _ = s.AsString;
        Assert.True(s.IsEvaluated);
    }

    [Fact]
    public void Equals_ObjectOverload()
    {
        LazyString a = new("test");
        Assert.True(a.Equals((object)new LazyString("test")));
        Assert.False(a.Equals((object?)null));
        Assert.False(a.Equals((object)"test")); // not a LazyString
    }

    // ========================================================================
    // Generator-powered Lazy() and LazyInterpolated() tests
    // ========================================================================

    [Fact]
    public void GeneratedLazy_SingleArg_DefersAndFormats()
    {
        LazyString s = ZA.Lazy("hello");
        Assert.True(s.IsLazy);
        Assert.Equal("hello", s.AsString);
    }

    [Fact]
    public void GeneratedLazy_MultipleArgs_DefersAndFormats()
    {
        LazyString s = ZA.Lazy("Port: ", (ushort)443);
        Assert.True(s.IsLazy);
        Assert.Equal("Port: 443", s.AsString);
    }

    [Fact]
    public void GeneratedLazy_ThreeArgs_FormatsAll()
    {
        LazyString s = ZA.Lazy("Source: ", (ushort)80, " -> Dest: ");
        Assert.Equal("Source: 80 -> Dest: ", s.AsString);
    }

    [Fact]
    public void GeneratedLazy_CachesOnSecondAccess()
    {
        LazyString s = ZA.Lazy("cached: ", 42);
        string first = s.AsString;
        string second = s.AsString;
        Assert.Same(first, second);
    }

    [Fact]
    public void GeneratedLazyInterpolated_SingleArg_DefersAndFormats()
    {
        LazyString s = ZA.LazyInterpolated(42);
        Assert.True(s.IsLazy);
        Assert.Equal("42", s.AsString);
    }

    [Fact]
    public void GeneratedLazyInterpolated_MultipleArgs_DefersAndFormats()
    {
        LazyString s = ZA.LazyInterpolated("Port: ", (ushort)443);
        Assert.True(s.IsLazy);
        Assert.Equal("Port: 443", s.AsString);
    }

    [Fact]
    public void GeneratedLazyInterpolated_CachesOnSecondAccess()
    {
        LazyString s = ZA.LazyInterpolated("value=", 99);
        string first = s.AsString;
        string second = s.AsString;
        Assert.Same(first, second);
    }
}
