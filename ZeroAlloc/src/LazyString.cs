// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc;

/// <summary>
/// Lightweight value-type string wrapper that supports immediate strings and deferred evaluation.
/// Wraps a single <c>object?</c>: <c>null</c> = absent, <see cref="string"/> = direct value,
/// <see cref="Func{String}"/> = stateless deferred, or <see cref="IDeferredFormat"/> = stateful deferred.
/// <para>On first evaluation of a lazy instance, the deferred reference is
/// atomically replaced with the resulting <see cref="string"/> via
/// <see cref="Interlocked.CompareExchange{T}"/>. This eliminates the wrapper-class allocation
/// that a separate cache object would require and reduces subsequent reads to a single
/// pointer dereference.</para>
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct LazyString : IEquatable<LazyString>, IComparable<LazyString>
{
    /// <summary>Empty string singleton (non-null, zero-length).</summary>
    public static readonly LazyString Empty = new(string.Empty);

    // The single wrapped value:
    //   null          → absent / no value
    //   string        → direct (already evaluated)
    //   Func<string>  → deferred evaluation (atomically replaced with result on first access)
    //   IDeferredFormat → stateful deferred (FormatLazy / generator-produced code)
    private readonly object? _Value;

    /// <summary>Creates from an already-evaluated string.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LazyString(string value)
    {
        _Value = value;
    }

    // Private constructor for raw object wrapping (used by factory methods)
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private LazyString(object? value)
    {
        _Value = value;
    }

    /// <summary>Creates a lazily-evaluated string. The factory is called at most once per storage location.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LazyString Lazy(Func<string> factory) => new(factory);

    /// <summary>
    /// Creates a lazily-evaluated formatted string with captured state.
    /// Use with a <c>static</c> lambda to avoid per-call closure allocation:
    /// <code>
    /// LazyString.FormatLazy((srcPort, dstPort), static s =&gt; ZA.String("Src: ", s.Item1, ", Dst: ", s.Item2));
    /// </code>
    /// The <typeparamref name="TState"/> value is boxed once into the internal storage.
    /// The <paramref name="formatter"/> delegate should be <c>static</c> so no closure object is allocated.
    /// This reduces allocations from 2 (Func + closure) to 1 (boxed state) per call.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LazyString FormatLazy<TState>(TState state, Func<TState, string> formatter)
        => new(new DeferredFormat<TState>(state, formatter));

    /// <summary>
    /// Reconstructs a <see cref="LazyString"/> from a raw inner value.
    /// This is intended for advanced storage scenarios (e.g., field value data)
    /// where the struct itself cannot be stored directly.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LazyString FromRawValue(object? value) => new(value);

    /// <summary>
    /// The raw inner value for storage optimization scenarios.
    /// Avoids boxing the struct when stored in a union-like container.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public object? RawValue
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Value;
    }

    /// <summary>The evaluated string value. Returns <see cref="string.Empty"/> if absent.</summary>
    public string AsString
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Value switch
        {
            string s => s,
            Func<string> => _EvaluateAndCache(),
            IDeferredFormat => _EvaluateAndCache(),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Tries to get the evaluated string value without throwing exceptions.
    /// If the value is a direct string, sets <paramref name="result"/> and returns <c>true</c>.
    /// If the value is a lazy factory, evaluates it safely: on success sets <paramref name="result"/>
    /// and returns <c>true</c>; on factory exception caches an empty string to prevent
    /// retry-forever behavior and returns <c>false</c>.
    /// If the value is absent (null/default), sets <paramref name="result"/> to <see cref="string.Empty"/>
    /// and returns <c>true</c>.
    /// </summary>
    /// <param name="result">The evaluated string, or <see cref="string.Empty"/> on failure.</param>
    /// <returns><c>true</c> if the string was obtained successfully; <c>false</c> if the factory threw.</returns>
    public bool TryGetString(out string result)
    {
        switch (_Value)
        {
            case string s:
                result = s;
                return true;
            case Func<string>:
            case IDeferredFormat:
                return _TryEvaluateAndCache(out result);
            default:
                result = string.Empty;
                return true;
        }
    }

    /// <summary>
    /// Evaluates the factory and atomically replaces <c>_Value</c> with the result.
    /// Uses <c>Unsafe.AsRef</c> to mutate the readonly field — this is safe because:
    /// <list type="bullet">
    /// <item>The write is a single reference-sized atomic operation.</item>
    /// <item><see cref="Interlocked.CompareExchange{T}"/> guarantees exactly one winner.</item>
    /// <item>Concurrent readers see either the original deferred object (and re-evaluate)
    ///   or the cached <see cref="string"/>.</item>
    /// </list>
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private string _EvaluateAndCache()
    {
        object deferred = _Value!;
        string result = deferred switch
        {
            Func<string> factory => factory(),
            IDeferredFormat fmt => fmt.Evaluate(),
            _ => string.Empty,
        };

        // Atomically replace the deferred object with the evaluated string.
        // If another thread already swapped, the CAS is a no-op.
        Interlocked.CompareExchange(ref Unsafe.AsRef(in _Value), result, deferred);
        return result;
    }

    /// <summary>
    /// Tries to evaluate the lazy factory and cache the result.
    /// On exception, caches <see cref="string.Empty"/> to prevent retry-forever behavior.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool _TryEvaluateAndCache(out string result)
    {
        object deferred = _Value!;
        try
        {
            result = deferred switch
            {
                Func<string> factory => factory(),
                IDeferredFormat fmt => fmt.Evaluate(),
                _ => string.Empty,
            };
        }
        catch
        {
            result = string.Empty;
            Interlocked.CompareExchange(ref Unsafe.AsRef(in _Value), result, deferred);
            return false;
        }

        Interlocked.CompareExchange(ref Unsafe.AsRef(in _Value), result, deferred);
        return true;
    }

    /// <summary>Whether this instance has no value (default/absent).</summary>
    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Value is null;
    }

    /// <summary>Whether the string is empty (absent or zero-length).</summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Value switch
        {
            null => true,
            string s => s.Length == 0,
            _ => AsString.Length == 0,
        };
    }

    /// <summary>Length of the evaluated string (0 if absent).</summary>
    public int Length => AsString.Length;

    /// <summary>Whether this wraps a lazy factory that has not yet been evaluated.</summary>
    public bool IsLazy => _Value is Func<string> or IDeferredFormat;

    /// <summary>Whether the value has been evaluated (direct string, null, or cached result).</summary>
    public bool IsEvaluated => _Value is not (Func<string> or IDeferredFormat);

    /// <summary>
    /// Eagerly concatenates this string with another. Both sides are evaluated immediately.
    /// Returns the other value if this is absent/empty, and vice versa.
    /// </summary>
    public LazyString Append(LazyString other)
    {
        if (_Value is null)
        {
            return other;
        }
        if (other._Value is null)
        {
            return this;
        }

        if (_Value is string s && s.Length == 0)
        {
            return other;
        }
        if (other._Value is string os && os.Length == 0)
        {
            return this;
        }

        return new LazyString(string.Concat(AsString, other.AsString));
    }

    /// <summary>Prepends another LazyString (eagerly evaluated).</summary>
    public LazyString Prepend(LazyString other) => other.Append(this);

    /// <summary>
    /// Tries to write the evaluated string into the destination span.
    /// Returns the number of characters written, or -1 if the destination is too small.
    /// </summary>
    public int TryWriteTo(Span<char> destination)
    {
        string s = AsString;
        if (s.Length > destination.Length)
        {
            return -1;
        }
        s.AsSpan().CopyTo(destination);
        return s.Length;
    }

    /// <summary>
    /// Gets the evaluated string as a <see cref="ReadOnlySpan{T}"/> of <see langword="char"/>.
    /// </summary>
    public ReadOnlySpan<char> AsSpan => AsString.AsSpan();

    // === Equality & Comparison ===

    /// <inheritdoc/>
    public bool Equals(LazyString other)
    {
        if (_Value is null && other._Value is null)
        {
            return true;
        }
        if (_Value is null || other._Value is null)
        {
            return false;
        }
        return string.Equals(AsString, other.AsString, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is LazyString other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _Value is not null
        ? AsString.GetHashCode(StringComparison.Ordinal)
        : 0;

    /// <inheritdoc/>
    public int CompareTo(LazyString other)
    {
        if (_Value is null && other._Value is null)
        {
            return 0;
        }
        if (_Value is null)
        {
            return -1;
        }
        if (other._Value is null)
        {
            return 1;
        }
        return string.Compare(AsString, other.AsString, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override string ToString() => AsString;

    // === Conversions ===

    /// <summary>Implicit conversion from <see cref="string"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator LazyString(string value) => new(value);

    /// <summary>Implicit conversion to <see cref="string"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(LazyString value) => value.AsString;

    /// <summary>Equality operator.</summary>
    public static bool operator ==(LazyString left, LazyString right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(LazyString left, LazyString right) => !left.Equals(right);
}
