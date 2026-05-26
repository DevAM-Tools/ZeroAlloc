// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// ZeroAlloc - TempBytes: Zero-Allocation Temporary Byte Buffer
// ============================================================================
// A disposable ref struct that provides a ReadOnlySpan<byte> view of
// formatted UTF-8 or binary content without heap allocation. The internal
// buffer is managed via ThreadStatic storage with graceful fallback to heap
// allocation for nested calls.
//
// IMPORTANT: Always use with 'using' statement to properly release the buffer:
//   using TempBytes temp = ZA.Utf8("Hello ", name);
//   ReadOnlySpan<byte> span = temp.AsSpan();
// ============================================================================

namespace ZeroAlloc;

/// <summary>
/// A disposable ref struct that holds temporary UTF-8/binary bytes without heap allocation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TempBytes"/> provides zero-allocation access to formatted UTF-8 or binary content.
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
/// using TempBytes temp = ZA.Utf8("User ", userId, " logged in");
/// ReadOnlySpan&lt;byte&gt; span = temp.AsSpan();
/// 
/// // Write directly to stream without allocation
/// stream.Write(span);
/// </code>
/// </remarks>
public readonly ref struct TempBytes : IDisposable
{
    // ========================================================================
    // PRIVATE FIELDS
    // ========================================================================

    /// <summary>
    /// The buffer containing the formatted bytes.
    /// Either ThreadStatic or heap-allocated depending on context.
    /// </summary>
    private readonly byte[] _Buffer;

    /// <summary>
    /// The number of valid bytes in the buffer.
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
    /// Initializes a new <see cref="TempBytes"/> with the given buffer, length, and ownership info.
    /// </summary>
    /// <param name="buffer">The buffer containing the formatted content.</param>
    /// <param name="length">The number of valid bytes written to the buffer.</param>
    /// <param name="isThreadStatic">True if using ThreadStatic buffer, false if heap-allocated fallback.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TempBytes(byte[] buffer, int length, bool isThreadStatic)
    {
        _Buffer = buffer;
        _Length = length;
        _IsThreadStatic = isThreadStatic;
    }

    // ========================================================================
    // PROPERTIES
    // ========================================================================

    /// <summary>
    /// Gets the length of the content in bytes.
    /// </summary>
    public int Length
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _Length;
    }

    /// <summary>
    /// Gets a value indicating whether the content is empty.
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
    /// Consider restructuring code to avoid nested TempBytes calls for optimal performance.
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
    /// Returns a <see cref="ReadOnlySpan{Byte}"/> view of the content.
    /// </summary>
    /// <returns>A read-only span containing the formatted bytes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsSpan() => new ReadOnlySpan<byte>(_Buffer, 0, _Length);

    /// <summary>
    /// Creates a heap-allocated byte array from the temporary content.
    /// </summary>
    /// <returns>A new byte array containing the formatted bytes.</returns>
    /// <remarks>
    /// This method allocates a new array on the heap. Use <see cref="AsSpan"/>
    /// when possible to avoid allocation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ToArray()
    {
        if (_Length == 0)
        {
            return Array.Empty<byte>();
        }

        byte[] result = new byte[_Length];
        Array.Copy(_Buffer, 0, result, 0, _Length);
        return result;
    }

    /// <summary>
    /// Returns a string representation for debugging purposes.
    /// </summary>
    /// <returns>A string showing the byte count.</returns>
    public override string ToString() => $"TempBytes[{_Length} bytes]";

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
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        // Only release if we're using the ThreadStatic buffer
        if (_IsThreadStatic)
        {
            ZeroAllocHelper.ReleaseByteBuffer();
        }
        // Heap-allocated buffers are simply garbage collected
    }

    // ========================================================================
    // OPERATORS
    // ========================================================================

    /// <summary>
    /// Implicitly converts a <see cref="TempBytes"/> to <see cref="ReadOnlySpan{Byte}"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ReadOnlySpan<byte>(TempBytes temp) => temp.AsSpan();

    /// <summary>
    /// Implicitly converts a <see cref="TempBytes"/> to a heap-allocated <see cref="T:byte[]"/>.
    /// </summary>
    /// <param name="temp">The temporary bytes to convert.</param>
    /// <returns>A new heap-allocated byte array containing the content.</returns>
    /// <remarks>
    /// <para>
    /// This conversion allocates a new byte array on the heap and automatically disposes
    /// the <see cref="TempBytes"/> to release the underlying buffer.
    /// </para>
    /// <para>
    /// This allows convenient usage patterns like:
    /// </para>
    /// <code>
    /// byte[] result = ZA.Utf8("Hello ", name, "!");
    /// byte[] binary = ZA.Bytes(header, payload);
    /// </code>
    /// <para>
    /// For zero-allocation scenarios, use <see cref="AsSpan"/> or the implicit
    /// <see cref="ReadOnlySpan{Byte}"/> conversion instead.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte[](TempBytes temp)
    {
        byte[] result = temp.ToArray();
        temp.Dispose();
        return result;
    }
}
