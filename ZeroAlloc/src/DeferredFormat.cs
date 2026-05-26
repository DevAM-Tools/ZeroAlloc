// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc;

/// <summary>
/// Stores a captured state value and a <c>static</c> formatter delegate for lazy string evaluation.
/// Used by <see cref="LazyString.FormatLazy{TState}"/> to avoid per-call closure object allocation.
/// The state is boxed once as this object; the formatter should be a <c>static</c> lambda so no
/// additional closure object is allocated.
/// </summary>
/// <typeparam name="TState">The type of the captured state (usually a ValueTuple).</typeparam>
internal sealed class DeferredFormat<TState>(TState state, Func<TState, string> formatter) : IDeferredFormat
{
    /// <summary>The captured state value.</summary>
    private readonly TState _State = state;

    /// <summary>The static formatter delegate.</summary>
    private readonly Func<TState, string> _Formatter = formatter;

    /// <inheritdoc/>
    public string Evaluate() => _Formatter(_State);
}
