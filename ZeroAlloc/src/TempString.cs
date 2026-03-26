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
// ZeroAlloc - TempString: Zero-Allocation Temporary String
// ============================================================================
// A disposable ref struct that provides a ReadOnlySpan<char> view of
// formatted content without heap allocation. The internal buffer is
// managed via ThreadStatic storage with graceful fallback to heap allocation
// for nested calls.
//
// IMPORTANT: Always use with 'using' statement to properly release the buffer:
//   using TempString temp = ZA.String("Hello ", name);
//   ReadOnlySpan<char> span = temp.AsSpan();
// ============================================================================

namespace ZeroAlloc;

/// <summary>
/// A disposable ref struct that holds a temporary string without heap allocation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TempString"/> provides zero-allocation access to formatted string content.
/// The internal buffer is managed via ThreadStatic storage and should be released
/// by calling <see cref="Dispose"/> (automatically done via <c>using</c> statement).
/// </para>
/// <para>
/// If the ThreadStatic buffer is already in use (nested call), a heap-allocated
/// buffer is used as fallback. The <see cref="IsHeapAllocated"/> property indicates
/// which mode is active.
/// </para>
/// <para><b>Usage:</b></para>
/// <code>
/// using TempString temp = ZA.String("User ", userId, " logged in");
/// ReadOnlySpan&lt;char&gt; span = temp.AsSpan();
/// 
/// // Use span for parsing, comparison, logging, etc.
/// if (span.StartsWith("User"))
/// {
///     Console.WriteLine(span.ToString()); // Only allocates if ToString() is called
/// }
/// </code>
/// <para>
/// Nested calls now work gracefully (with heap allocation fallback):
/// </para>
/// <code>
/// using TempString outer = ZA.String("Outer");
/// using TempString inner = ZA.String("Inner"); // Works! (heap-allocated)
/// </code>
/// </remarks>
public readonly ref struct TempString : IDisposable
{
    // ========================================================================
    // PRIVATE FIELDS
    // ========================================================================

    /// <summary>
    /// The buffer containing the formatted characters.
    /// Either ThreadStatic or heap-allocated depending on context.
    /// </summary>
    private readonly char[] _Buffer;

    /// <summary>
    /// The number of valid characters in the buffer.
    /// </summary>
    private readonly int _Length;

    /// <summary>
    /// True if using the ThreadStatic buffer (needs release on Dispose).
    /// False if using a heap-allocated fallback buffer (no release needed).
    /// </summary>
    private readonly bool _IsThreadStatic;

    // ========================================================================
    // CONSTRUCTOR
    // ========================================================================

    /// <summary>
    /// Initializes a new <see cref="TempString"/> with the given buffer, length, and ownership info.
    /// </summary>
    /// <param name="buffer">The buffer containing the formatted content.</param>
    /// <param name="length">The number of valid characters written to the buffer.</param>
    /// <param name="isThreadStatic">True if using ThreadStatic buffer, false if heap-allocated fallback.</param>
    /// <remarks>
    /// This constructor is called by generated code after formatting is complete.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TempString(char[] buffer, int length, bool isThreadStatic)
    {
        _Buffer = buffer;
        _Length = length;
        _IsThreadStatic = isThreadStatic;
    }

    // ========================================================================
    // PROPERTIES
    // ========================================================================

    /// <summary>
    /// Gets the length of the string content in characters.
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Length;
    }

    /// <summary>
    /// Gets a value indicating whether the string content is empty.
    /// </summary>
    public bool IsEmpty
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Length == 0;
    }

    /// <summary>
    /// Gets a value indicating whether this instance is using a heap-allocated buffer
    /// (fallback mode due to nested call) rather than the ThreadStatic buffer.
    /// </summary>
    /// <remarks>
    /// When <c>true</c>, this indicates a performance degradation due to nested usage.
    /// Consider restructuring code to avoid nested TempString calls for optimal performance.
    /// </remarks>
    public bool IsHeapAllocated
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => !_IsThreadStatic;
    }

    // ========================================================================
    // METHODS
    // ========================================================================

    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{Char}"/> view of the string content.
    /// </summary>
    /// <returns>A read-only span containing the formatted characters.</returns>
    /// <remarks>
    /// This is the primary way to access the content without allocation.
    /// The span is valid until <see cref="Dispose"/> is called.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<char> AsSpan() => new ReadOnlySpan<char>(_Buffer, 0, _Length);

    /// <summary>
    /// Creates a heap-allocated string from the temporary content.
    /// </summary>
    /// <returns>A new string containing the formatted characters.</returns>
    /// <remarks>
    /// <para>
    /// This method allocates a new string on the heap. Use <see cref="AsSpan"/>
    /// when possible to avoid allocation.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override string ToString()
    {
        if (_Length == 0)
        {
            return string.Empty;
        }

        return new string(_Buffer, 0, _Length);
    }

    // ========================================================================
    // IDISPOSABLE IMPLEMENTATION
    // ========================================================================

    /// <summary>
    /// Releases the ThreadStatic buffer if applicable, making it available for the next acquisition.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method releases the ThreadStatic buffer if one was used.
    /// If a heap-allocated fallback buffer was used (due to nested call),
    /// this method does nothing (the buffer will be garbage collected).
    /// </para>
    /// <para>
    /// Always use <c>using</c> statements to ensure proper disposal.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        // Only release if we're using the ThreadStatic buffer
        if (_IsThreadStatic)
        {
            ZeroAllocHelper.ReleaseCharBuffer();
        }
        // Heap-allocated buffers are simply garbage collected
    }

    // ========================================================================
    // OPERATORS
    // ========================================================================

    /// <summary>
    /// Implicitly converts a <see cref="TempString"/> to <see cref="ReadOnlySpan{Char}"/>.
    /// </summary>
    /// <param name="temp">The temporary string to convert.</param>
    /// <returns>A read-only span view of the content.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<char>(TempString temp) => temp.AsSpan();

    /// <summary>
    /// Implicitly converts a <see cref="TempString"/> to a heap-allocated <see cref="string"/>.
    /// </summary>
    /// <param name="temp">The temporary string to convert.</param>
    /// <returns>A new heap-allocated string containing the content.</returns>
    /// <remarks>
    /// <para>
    /// This conversion allocates a new string on the heap and automatically disposes
    /// the <see cref="TempString"/> to release the underlying buffer.
    /// </para>
    /// <para>
    /// This allows convenient usage patterns like:
    /// </para>
    /// <code>
    /// string result = ZA.String("Hello ", name, "!");
    /// </code>
    /// <para>
    /// For zero-allocation scenarios, use <see cref="AsSpan"/> or the implicit
    /// <see cref="ReadOnlySpan{Char}"/> conversion instead.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(TempString temp)
    {
        string result = temp.ToString();
        temp.Dispose();
        return result;
    }
}
