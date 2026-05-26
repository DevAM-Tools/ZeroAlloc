// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// ZeroAlloc - ThreadStatic Buffer Management
// ============================================================================
// This file provides ThreadStatic buffer access for zero-allocation string
// and byte serialization. Buffers are thread-local.
//
// Two separate concerns are configured via MSBuild properties:
//
// 1. RECURSIVE/NESTED CALLS (buffer already in use):
//    ZeroAlloc_RecursiveHeapFallback = true  (default): Fall back to heap
//    ZeroAlloc_RecursiveHeapFallback = false (strict):  Throw exception
//
// 2. BUFFER OVERFLOW (content exceeds buffer size):
//    ZeroAlloc_BufferOverflowBehavior = Grow        (default): Grow ThreadStatic buffer
//    ZeroAlloc_BufferOverflowBehavior = HeapFallback:          Allocate heap buffer
//    ZeroAlloc_BufferOverflowBehavior = Throw:                 Throw exception
//
// Configuration via MSBuild properties:
//   <ZeroAlloc_DefaultBufferSize>4194304</ZeroAlloc_DefaultBufferSize>
//   <ZeroAlloc_RecursiveHeapFallback>false</ZeroAlloc_RecursiveHeapFallback>
//   <ZeroAlloc_BufferOverflowBehavior>HeapFallback</ZeroAlloc_BufferOverflowBehavior>
//
// Default buffer size is 2 MiB (2,097,152 bytes/chars).
// ============================================================================

namespace ZeroAlloc;

/// <summary>
/// Provides ThreadStatic buffer access for zero-allocation formatting.
/// </summary>
/// <remarks>
/// <para>
/// This class manages thread-local buffers for string and byte formatting.
/// </para>
/// <para>
/// When the ThreadStatic buffer is already in use (nested/recursive call), behavior
/// depends on the <c>ZeroAlloc_RecursiveHeapFallback</c> MSBuild property:
/// <list type="bullet">
///   <item><term>true (default)</term><description>Falls back to heap allocation (graceful).</description></item>
///   <item><term>false (strict)</term><description>Throws <see cref="InvalidOperationException"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// When content exceeds the buffer size, behavior depends on the 
/// <c>ZeroAlloc_BufferOverflowBehavior</c> MSBuild property:
/// <list type="bullet">
///   <item><term>Grow (default)</term><description>Dynamically grows the ThreadStatic buffer.</description></item>
///   <item><term>HeapFallback</term><description>Allocates a new heap buffer for this call.</description></item>
///   <item><term>Throw</term><description>Throws <see cref="InvalidOperationException"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// Buffer size defaults to 2 MiB but can be configured via the MSBuild property
/// <c>ZeroAlloc_DefaultBufferSize</c> in your project file.
/// </para>
/// </remarks>
public static class ZeroAllocHelper
{
    // ========================================================================
    // CONSTANTS
    // ========================================================================

    /// <summary>
    /// Default buffer size in bytes/chars (2 MiB).
    /// Can be overridden via generated code from MSBuild property.
    /// </summary>
    public const int DefaultBufferSize = 2_097_152;

    // ========================================================================
    // THREADSTATIC CHAR BUFFER (for string operations)
    // ========================================================================

    /// <summary>
    /// Thread-local character buffer for string formatting.
    /// Lazily allocated on first use per thread.
    /// </summary>
    [ThreadStatic]
    private static char[]? _CharBuffer;

    /// <summary>
    /// Flag indicating whether the char buffer is currently in use.
    /// </summary>
    [ThreadStatic]
    private static bool _CharBufferInUse;

    /// <summary>
    /// Acquires access to the ThreadStatic char buffer, or allocates a new one if busy.
    /// </summary>
    /// <param name="bufferSize">The required buffer size (from generated code).</param>
    /// <param name="isThreadStatic">Output: true if ThreadStatic buffer was acquired, false if heap-allocated.</param>
    /// <returns>The char buffer ready for writing.</returns>
    /// <remarks>
    /// <para>
    /// This method provides graceful fallback: if the ThreadStatic buffer is already
    /// in use (nested call), a new heap-allocated buffer is returned instead.
    /// </para>
    /// <para>
    /// The <paramref name="isThreadStatic"/> output parameter indicates whether the
    /// returned buffer is the ThreadStatic one (and needs to be released via
    /// <see cref="ReleaseCharBuffer"/>) or a heap-allocated fallback (which does not).
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char[] AcquireCharBuffer(int bufferSize, out bool isThreadStatic)
        => AcquireCharBuffer(bufferSize, recursiveHeapFallback: true, out isThreadStatic);

    /// <summary>
    /// Acquires access to the ThreadStatic char buffer with configurable fallback behavior.
    /// </summary>
    /// <param name="bufferSize">The required buffer size (from generated code).</param>
    /// <param name="recursiveHeapFallback">
    /// If true, falls back to heap allocation when buffer is busy (nested call).
    /// If false, throws <see cref="InvalidOperationException"/> when buffer is busy.
    /// </param>
    /// <param name="isThreadStatic">Output: true if ThreadStatic buffer was acquired, false if heap-allocated.</param>
    /// <returns>The char buffer ready for writing.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="recursiveHeapFallback"/> is false and the buffer is already in use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char[] AcquireCharBuffer(int bufferSize, bool recursiveHeapFallback, out bool isThreadStatic)
    {
        // ----------------------------------------------------------------
        // If buffer is already in use, handle according to fallback setting
        // ----------------------------------------------------------------
        if (_CharBufferInUse)
        {
            if (!recursiveHeapFallback)
            {
                throw new InvalidOperationException(
                    "Nested ZeroAlloc call detected: ThreadStatic buffer is already in use. " +
                    "Enable ZeroAlloc_RecursiveHeapFallback to allow heap allocation fallback, " +
                    "or ensure TempString is disposed before the next formatting call.");
            }

            isThreadStatic = false;
            return new char[bufferSize];
        }

        // ----------------------------------------------------------------
        // Mark buffer as in use BEFORE allocation
        // ----------------------------------------------------------------
        _CharBufferInUse = true;
        isThreadStatic = true;

        // ----------------------------------------------------------------
        // Lazy allocation: create buffer on first use for this thread
        // Reallocate if requested size exceeds current buffer
        // ----------------------------------------------------------------
        if (_CharBuffer is null || _CharBuffer.Length < bufferSize)
        {
            _CharBuffer = new char[bufferSize];
        }

        return _CharBuffer;
    }

    /// <summary>
    /// Releases the ThreadStatic char buffer, making it available for the next acquisition.
    /// </summary>
    /// <remarks>
    /// Called automatically by <see cref="TempString.Dispose"/> when using the ThreadStatic buffer.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReleaseCharBuffer() => _CharBufferInUse = false;

    /// <summary>
    /// Checks whether the ThreadStatic char buffer is available for acquisition.
    /// </summary>
    /// <returns><c>true</c> if the buffer is available; <c>false</c> if it is currently in use.</returns>
    /// <remarks>
    /// Used by TryString methods to check availability before attempting acquisition
    /// when heap fallback is disabled.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCharBufferAvailable() => !_CharBufferInUse;

    // ========================================================================
    // THREADSTATIC BYTE BUFFER (for UTF-8 and binary operations)
    // ========================================================================

    /// <summary>
    /// Thread-local byte buffer for UTF-8 and binary formatting.
    /// Lazily allocated on first use per thread.
    /// </summary>
    [ThreadStatic]
    private static byte[]? _ByteBuffer;

    /// <summary>
    /// Flag indicating whether the byte buffer is currently in use.
    /// </summary>
    [ThreadStatic]
    private static bool _ByteBufferInUse;

    /// <summary>
    /// Acquires access to the ThreadStatic byte buffer, or allocates a new one if busy.
    /// </summary>
    /// <param name="bufferSize">The required buffer size (from generated code).</param>
    /// <param name="isThreadStatic">Output: true if ThreadStatic buffer was acquired, false if heap-allocated.</param>
    /// <returns>The byte buffer ready for writing.</returns>
    /// <remarks>
    /// <para>
    /// This method provides graceful fallback: if the ThreadStatic buffer is already
    /// in use (nested call), a new heap-allocated buffer is returned instead.
    /// </para>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] AcquireByteBuffer(int bufferSize, out bool isThreadStatic)
        => AcquireByteBuffer(bufferSize, recursiveHeapFallback: true, out isThreadStatic);

    /// <summary>
    /// Acquires access to the ThreadStatic byte buffer with configurable fallback behavior.
    /// </summary>
    /// <param name="bufferSize">The required buffer size (from generated code).</param>
    /// <param name="recursiveHeapFallback">
    /// If true, falls back to heap allocation when buffer is busy (nested call).
    /// If false, throws <see cref="InvalidOperationException"/> when buffer is busy.
    /// </param>
    /// <param name="isThreadStatic">Output: true if ThreadStatic buffer was acquired, false if heap-allocated.</param>
    /// <returns>The byte buffer ready for writing.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="recursiveHeapFallback"/> is false and the buffer is already in use.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] AcquireByteBuffer(int bufferSize, bool recursiveHeapFallback, out bool isThreadStatic)
    {
        // ----------------------------------------------------------------
        // If buffer is already in use, handle according to fallback setting
        // ----------------------------------------------------------------
        if (_ByteBufferInUse)
        {
            if (!recursiveHeapFallback)
            {
                throw new InvalidOperationException(
                    "Nested ZeroAlloc call detected: ThreadStatic buffer is already in use. " +
                    "Enable ZeroAlloc_RecursiveHeapFallback to allow heap allocation fallback, " +
                    "or ensure TempBytes is disposed before the next formatting call.");
            }

            isThreadStatic = false;
            return new byte[bufferSize];
        }

        // ----------------------------------------------------------------
        // Mark buffer as in use BEFORE allocation
        // ----------------------------------------------------------------
        _ByteBufferInUse = true;
        isThreadStatic = true;

        // ----------------------------------------------------------------
        // Lazy allocation: create buffer on first use for this thread
        // ----------------------------------------------------------------
        if (_ByteBuffer is null || _ByteBuffer.Length < bufferSize)
        {
            _ByteBuffer = new byte[bufferSize];
        }

        return _ByteBuffer;
    }

    /// <summary>
    /// Releases the ThreadStatic byte buffer, making it available for the next acquisition.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReleaseByteBuffer() => _ByteBufferInUse = false;

    /// <summary>
    /// Checks whether the ThreadStatic byte buffer is available for acquisition.
    /// </summary>
    /// <returns><c>true</c> if the buffer is available; <c>false</c> if it is currently in use.</returns>
    /// <remarks>
    /// Used by TryUtf8/TryBytes methods to check availability before attempting acquisition
    /// when heap fallback is disabled.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsByteBufferAvailable() => !_ByteBufferInUse;

    // ========================================================================
    // BUFFER RESIZING
    // ========================================================================

    /// <summary>
    /// Resizes the ThreadStatic char buffer to the specified size.
    /// </summary>
    /// <param name="newSize">The new buffer size in characters.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="newSize"/> is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the buffer is currently in use.</exception>
    /// <remarks>
    /// <para>
    /// Call this method during application startup or between operations to adjust
    /// the buffer size for your workload. A larger buffer reduces reallocations,
    /// while a smaller buffer reduces memory usage.
    /// </para>
    /// <para>
    /// This method only affects the current thread's buffer. Each thread has its own
    /// ThreadStatic buffer that can be resized independently.
    /// </para>
    /// </remarks>
    public static void ResizeCharBuffer(int newSize)
    {
        if (newSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), "Buffer size must be greater than zero.");
        }

        if (_CharBufferInUse)
        {
            throw new InvalidOperationException("Cannot resize buffer while it is in use. Ensure all TempString instances are disposed.");
        }

        _CharBuffer = new char[newSize];
    }

    /// <summary>
    /// Resizes the ThreadStatic byte buffer to the specified size.
    /// </summary>
    /// <param name="newSize">The new buffer size in bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="newSize"/> is less than or equal to zero.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the buffer is currently in use.</exception>
    /// <remarks>
    /// <para>
    /// Call this method during application startup or between operations to adjust
    /// the buffer size for your workload. A larger buffer reduces reallocations,
    /// while a smaller buffer reduces memory usage.
    /// </para>
    /// <para>
    /// This method only affects the current thread's buffer. Each thread has its own
    /// ThreadStatic buffer that can be resized independently.
    /// </para>
    /// </remarks>
    public static void ResizeByteBuffer(int newSize)
    {
        if (newSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize), "Buffer size must be greater than zero.");
        }

        if (_ByteBufferInUse)
        {
            throw new InvalidOperationException("Cannot resize buffer while it is in use. Ensure all TempBytes instances are disposed.");
        }

        _ByteBuffer = new byte[newSize];
    }

    /// <summary>
    /// Gets the current size of the ThreadStatic char buffer for this thread.
    /// </summary>
    /// <returns>The buffer size in characters, or 0 if the buffer has not been allocated yet.</returns>
    public static int GetCharBufferSize() => _CharBuffer?.Length ?? 0;

    /// <summary>
    /// Gets the current size of the ThreadStatic byte buffer for this thread.
    /// </summary>
    /// <returns>The buffer size in bytes, or 0 if the buffer has not been allocated yet.</returns>
    public static int GetByteBufferSize() => _ByteBuffer?.Length ?? 0;

    /// <summary>
    /// Releases the ThreadStatic buffers, freeing memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Call this method to release memory when the buffers are no longer needed,
    /// for example during application shutdown or when a thread is about to be
    /// returned to a thread pool.
    /// </para>
    /// <para>
    /// This method only affects the current thread's buffers.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when either buffer is currently in use.</exception>
    public static void ReleaseBuffers()
    {
        if (_CharBufferInUse)
        {
            throw new InvalidOperationException("Cannot release buffers while char buffer is in use.");
        }

        if (_ByteBufferInUse)
        {
            throw new InvalidOperationException("Cannot release buffers while byte buffer is in use.");
        }

        _CharBuffer = null;
        _ByteBuffer = null;
    }

    // ========================================================================
    // BUFFER GROWTH (for TempStringBuilder/TempBytesBuilder)
    // ========================================================================

    /// <summary>
    /// Calculates the new buffer size using the growth strategy.
    /// </summary>
    /// <param name="currentSize">Current buffer size.</param>
    /// <param name="requiredSize">Minimum required size.</param>
    /// <returns>New buffer size that is at least <paramref name="requiredSize"/>.</returns>
    /// <remarks>
    /// Growth strategy:
    /// <list type="bullet">
    ///   <item>≤ 1 MiB: 3x growth</item>
    ///   <item>≤ 16 MiB: 2x growth</item>
    ///   <item>≤ 512 MiB: 1.5x growth</item>
    ///   <item>> 512 MiB: 1.25x growth</item>
    /// </list>
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateGrowth(int currentSize, int requiredSize)
    {
        const int OneMiB = 1_048_576;
        const int SixteenMiB = 16_777_216;
        const int FiveHundredTwelveMiB = 536_870_912;

        // Use long arithmetic to prevent integer overflow at large buffer sizes (>1.7 GiB)
        long newSizeLong = currentSize switch
        {
            <= OneMiB => (long)currentSize * 3,
            <= SixteenMiB => (long)currentSize * 2,
            <= FiveHundredTwelveMiB => currentSize + (long)(currentSize / 2), // 1.5x
            _ => currentSize + (long)(currentSize / 4) // 1.25x
        };

        int newSize = (int)Math.Min(newSizeLong, int.MaxValue);
        return Math.Max(newSize, requiredSize);
    }

    /// <summary>
    /// Grows the ThreadStatic char buffer to accommodate at least <paramref name="requiredSize"/> characters.
    /// </summary>
    /// <param name="requiredSize">The minimum required buffer size.</param>
    /// <returns>The new (grown) char buffer.</returns>
    /// <remarks>
    /// This method should only be called when the char buffer is in use and needs to grow.
    /// The existing content is copied to the new buffer.
    /// </remarks>
    public static char[] GrowCharBuffer(int requiredSize)
    {
        char[] oldBuffer = _CharBuffer ?? throw new InvalidOperationException("Char buffer not acquired.");
        int newSize = CalculateGrowth(oldBuffer.Length, requiredSize);
        char[] newBuffer = new char[newSize];
        oldBuffer.AsSpan().CopyTo(newBuffer);
        _CharBuffer = newBuffer;
        return newBuffer;
    }

    /// <summary>
    /// Tries to grow the ThreadStatic char buffer without throwing on failure.
    /// </summary>
    /// <param name="requiredSize">The minimum required buffer size.</param>
    /// <returns>The new (grown) char buffer, or null if the buffer cannot be grown.</returns>
    /// <remarks>
    /// Returns null if the char buffer is not acquired. Used for non-throwing TryAppend methods.
    /// </remarks>
    public static char[]? TryGrowCharBuffer(int requiredSize)
    {
        char[]? oldBuffer = _CharBuffer;
        if (oldBuffer is null)
        {
            return null;
        }

        int newSize = CalculateGrowth(oldBuffer.Length, requiredSize);
        char[] newBuffer = new char[newSize];
        oldBuffer.AsSpan().CopyTo(newBuffer);
        _CharBuffer = newBuffer;
        return newBuffer;
    }

    /// <summary>
    /// Grows the ThreadStatic byte buffer to accommodate at least <paramref name="requiredSize"/> bytes.
    /// </summary>
    /// <param name="requiredSize">The minimum required buffer size.</param>
    /// <returns>The new (grown) byte buffer.</returns>
    /// <remarks>
    /// This method should only be called when the byte buffer is in use and needs to grow.
    /// The existing content is copied to the new buffer.
    /// </remarks>
    public static byte[] GrowByteBuffer(int requiredSize)
    {
        byte[] oldBuffer = _ByteBuffer ?? throw new InvalidOperationException("Byte buffer not acquired.");
        int newSize = CalculateGrowth(oldBuffer.Length, requiredSize);
        byte[] newBuffer = new byte[newSize];
        oldBuffer.AsSpan().CopyTo(newBuffer);
        _ByteBuffer = newBuffer;
        return newBuffer;
    }

    /// <summary>
    /// Tries to grow the ThreadStatic byte buffer without throwing on failure.
    /// </summary>
    /// <param name="requiredSize">The minimum required buffer size.</param>
    /// <returns>The new (grown) byte buffer, or null if the buffer cannot be grown.</returns>
    /// <remarks>
    /// Returns null if the byte buffer is not acquired. Used for non-throwing TryAppend methods.
    /// </remarks>
    public static byte[]? TryGrowByteBuffer(int requiredSize)
    {
        byte[]? oldBuffer = _ByteBuffer;
        if (oldBuffer is null)
        {
            return null;
        }

        int newSize = CalculateGrowth(oldBuffer.Length, requiredSize);
        byte[] newBuffer = new byte[newSize];
        oldBuffer.AsSpan().CopyTo(newBuffer);
        _ByteBuffer = newBuffer;
        return newBuffer;
    }
}
