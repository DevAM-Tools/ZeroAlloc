// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc;

/// <summary>
/// Non-generic interface for deferred format objects stored inside <see cref="LazyString"/>.
/// Allows pattern matching without knowing the generic type parameter.
/// </summary>
internal interface IDeferredFormat
{
    #region Public API

    /// <summary>Evaluates the deferred format and returns the result string.</summary>
    string Evaluate();

    #endregion
}
