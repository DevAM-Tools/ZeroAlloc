// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests.Helpers;

/// <summary>Analyzer config options for generator tests that must see MSBuild properties.</summary>
internal sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
    #region Fields

    private readonly TestAnalyzerConfigOptions _Options;

    #endregion

    #region Lifecycle

    /// <summary>Creates a provider whose global and per-file options share <paramref name="globalOptions"/>.</summary>
    public TestAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> globalOptions)
    {
        _Options = new TestAnalyzerConfigOptions(globalOptions);
    }

    /// <summary>Builds ZeroAlloc MSBuild properties for overflow-behavior harnesses.</summary>
    public static TestAnalyzerConfigOptionsProvider ForZeroAlloc(string overflowBehavior, int defaultBufferSize)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal)
        {
            ["build_property.ZeroAlloc_DefaultBufferSize"] = defaultBufferSize.ToString(CultureInfo.InvariantCulture),
            ["build_property.ZeroAlloc_BufferOverflowBehavior"] = overflowBehavior,
            ["build_property.ZeroAlloc_RecursiveHeapFallback"] = "true",
        };
        return new TestAnalyzerConfigOptionsProvider(values);
    }

    #endregion

    #region Public API

    /// <inheritdoc />
    public override AnalyzerConfigOptions GlobalOptions => _Options;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _Options;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _Options;

    #endregion

    #region Nested

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _Values;

        public TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values)
        {
            _Values = values;
        }

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
            => _Values.TryGetValue(key, out value);
    }

    #endregion
}
