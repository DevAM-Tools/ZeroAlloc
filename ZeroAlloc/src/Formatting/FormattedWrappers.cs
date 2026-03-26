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

// ============================================================================
// ZeroAlloc - Formatted Wrappers
// ============================================================================
// Simple containers for format string and culture info.
// No caching - just pass-through to TryFormat with format/culture.
// ============================================================================

namespace ZeroAlloc;

#region Formatted<T> - String formatting wrapper

/// <summary>
/// Wraps an ISpanFormattable value with format string and culture info.
/// </summary>
/// <typeparam name="T">The type implementing ISpanFormattable.</typeparam>
/// <remarks>
/// <para><b>USAGE:</b></para>
/// <code>
/// var formatted = new Formatted&lt;double&gt;(1234.56, "N2", germanCulture);
/// // Use in ZA.String() or ZA.Utf8()
/// </code>
/// </remarks>
public readonly struct Formatted<T> : ISpanFormattable where T : ISpanFormattable
{
    private readonly T _Value;
    private readonly string? _Format;
    private readonly IFormatProvider? _Provider;

    /// <summary>
    /// Initializes a new formatted wrapper.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The format string (e.g., "C2", "N0").</param>
    /// <param name="provider">The format provider for culture-specific formatting.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Formatted(T value, string? format = null, IFormatProvider? provider = null)
    {
        _Value = value;
        _Format = format;
        _Provider = provider;
    }

    /// <summary>
    /// Tries to format the value into the destination span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        // Use our stored format/provider, ignore passed ones
        _Value.TryFormat(destination, out charsWritten, _Format, _Provider ?? provider);

    /// <summary>
    /// Returns the formatted string.
    /// </summary>
    public override string ToString()
    {
        // First try with a reasonable stack buffer
        Span<char> buffer = stackalloc char[256];
        if (_Value.TryFormat(buffer, out int written, _Format, _Provider))
        {
            return new string(buffer.Slice(0, written));
        }

        return _Value.ToString(_Format, _Provider) ?? "";
    }

    /// <summary>
    /// Returns the formatted string.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>
    /// Creates a formatted wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Formatted<T> Create(T value, string? format = null, IFormatProvider? provider = null)
        => new(value, format, provider);
}

#endregion

#region Utf8Formatted<T> - UTF-8 formatting wrapper

/// <summary>
/// Wraps an IUtf8SpanFormattable value with format string and culture info.
/// </summary>
/// <typeparam name="T">The type implementing IUtf8SpanFormattable.</typeparam>
public readonly struct Utf8Formatted<T> : IUtf8SpanFormattable where T : IUtf8SpanFormattable
{
    private readonly T _Value;
    private readonly string? _Format;
    private readonly IFormatProvider? _Provider;

    /// <summary>
    /// Initializes a new UTF-8 formatted wrapper.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <param name="format">The format string.</param>
    /// <param name="provider">The format provider.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Utf8Formatted(T value, string? format = null, IFormatProvider? provider = null)
    {
        _Value = value;
        _Format = format;
        _Provider = provider;
    }

    /// <summary>
    /// Tries to format the value as UTF-8 into the destination span.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryFormat(Span<byte> destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        // Use our stored format/provider, ignore passed ones
        _Value.TryFormat(destination, out bytesWritten, _Format, _Provider ?? provider);

    /// <summary>
    /// Returns the formatted string (converts UTF-8 to string).
    /// </summary>
    public override string ToString()
    {
        // First try with a reasonable stack buffer
        Span<byte> buffer = stackalloc byte[256];
        if (_Value.TryFormat(buffer, out int written, _Format, _Provider))
        {
            return Encoding.UTF8.GetString(buffer.Slice(0, written));
        }

        return _Value.ToString() ?? "";
    }

    /// <summary>
    /// Returns the formatted string.
    /// </summary>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <summary>
    /// Creates a UTF-8 formatted wrapper.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Utf8Formatted<T> Create(T value, string? format = null, IFormatProvider? provider = null)
        => new(value, format, provider);
}

#endregion
