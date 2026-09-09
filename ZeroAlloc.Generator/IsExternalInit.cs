// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// Polyfill for init-only setters on netstandard2.0 (Roslyn generator host).
#if !NET5_0_OR_GREATER

namespace System.Runtime.CompilerServices;

/// <summary>
/// Reserved to be used by the compiler for tracking metadata.
/// This class should not be used by developers in source code.
/// </summary>
internal static class IsExternalInit
{
}

#endif
