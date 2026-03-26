/*
MIT License
SPDX-License-Identifier: MIT

Copyright (c) 2025 ZeroAlloc Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

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
