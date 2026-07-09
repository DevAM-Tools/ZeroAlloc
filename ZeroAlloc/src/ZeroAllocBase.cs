// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// ZeroAlloc - Base Class for User-Defined Allocation Helpers
// ============================================================================
// This file provides the base class that users inherit from to enable
// zero-allocation string and binary serialization.
//
// Usage (in user's assembly):
//   1. Define: partial class ZA : ZeroAllocBase { }
//   2. Call:   ZA.String("Hello ", name, "!")
//   3. The source generator fills in the specialized methods
// ============================================================================

namespace ZeroAlloc;

/// <summary>
/// Base class for zero-allocation string and binary building.
/// </summary>
/// <remarks>
/// <para>
/// To use ZeroAlloc, create a partial class in your assembly that inherits from this:
/// </para>
/// <code>
/// partial class ZA : ZeroAllocBase { }
/// </code>
/// <para>
/// Then call the generated static methods:
/// </para>
/// <code>
/// string result = ZA.String("Hello ", name, "!");
/// byte[] utf8 = ZA.Utf8("Count: ", count);
/// byte[] bytes = ZA.Bytes(header, payload);
/// </code>
/// <para>
/// The source generator analyzes your calls and generates optimized methods
/// directly in your partial class.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Step 1: Define your class (once per assembly)
/// partial class ZA : ZeroAllocBase { }
/// 
/// // Step 2: Use it anywhere
/// string greeting = ZA.String("Hello, ", userName, "!");
/// byte[] json = ZA.Utf8("{\"id\":", userId, "}");
/// 
/// // Try-variants for zero heap allocation
/// Span&lt;char&gt; buffer = stackalloc char[256];
/// if (ZA.TryString(buffer, out int written, "ID: ", id))
/// {
///     // Use buffer[..written]
/// }
/// </code>
/// </example>
public abstract class ZeroAllocBase
{
    // ========================================================================
    // PROTECTED CONSTRUCTOR
    // ========================================================================
    // Prevents direct instantiation - users must inherit from this class.
    // ========================================================================

    /// <summary>
    /// Protected constructor prevents direct instantiation.
    /// </summary>
    protected ZeroAllocBase() { }

    // ========================================================================
    // PUBLIC STATIC METHODS - API SURFACE
    // ========================================================================
    // These methods provide the public API surface for code completion and
    // discoverability. The source generator creates internal overloads with
    // specific type signatures that shadow these generic versions.
    // ========================================================================

    #region String API

    /// <summary>
    /// Concatenates the given values into a temporary string with zero intermediate allocations.
    /// </summary>
    /// <param name="values">The values to concatenate.</param>
    /// <returns>A <see cref="TempString"/> that can be implicitly converted to <see cref="string"/> or used as <see cref="ReadOnlySpan{Char}"/>.</returns>
    /// <remarks>
    /// <para>
    /// The returned <see cref="TempString"/> can be used in several ways:
    /// </para>
    /// <code>
    /// // Implicit conversion to string (allocates a string, disposes automatically)
    /// string result = ZA.String("Hello ", name, "!");
    /// 
    /// // Zero-allocation usage with span (must dispose manually)
    /// using TempString temp = ZA.String("Hello ", name, "!");
    /// ReadOnlySpan&lt;char&gt; span = temp.AsSpan();
    /// </code>
    /// <para>
    /// The source generator creates optimized overloads for specific type combinations.
    /// This method should never be called - if reached, the generator failed.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// This method should never be called. The source generator should create specific overloads.
    /// </exception>
    public static TempString String(params object?[] values)
    {
        throw new InvalidOperationException(
            "The generic String() method should never be called. " +
            "The source generator should have created a specific overload for your argument types. " +
            "Please rebuild your project to regenerate the source.");
    }

    /// <summary>
    /// Tries to write the concatenated values into the destination buffer.
    /// </summary>
    /// <param name="destination">The buffer to write to.</param>
    /// <param name="charsWritten">When successful, the number of characters written.</param>
    /// <param name="values">The values to concatenate.</param>
    /// <returns>True if the buffer was large enough; otherwise false.</returns>
    /// <remarks>
    /// The source generator creates optimized overloads for specific type combinations.
    /// This method should never be called - if reached, the generator failed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// This method should never be called. The source generator should create specific overloads.
    /// </exception>
    public static bool TryString(Span<char> destination, out int charsWritten, params object?[] values)
    {
        throw new InvalidOperationException(
            "The generic TryString() method should never be called. " +
            "The source generator should have created a specific overload for your argument types. " +
            "Please rebuild your project to regenerate the source.");
    }

    #endregion

    #region Utf8 API

    /// <summary>
    /// Encodes the concatenated values as temporary UTF-8 bytes with zero intermediate allocations.
    /// </summary>
    /// <param name="values">The values to concatenate and encode.</param>
    /// <returns>A <see cref="TempBytes"/> that can be implicitly converted to a byte array or used as <see cref="ReadOnlySpan{Byte}"/>.</returns>
    /// <remarks>
    /// <para>
    /// The returned <see cref="TempBytes"/> can be used in several ways:
    /// </para>
    /// <code>
    /// // Implicit conversion to byte[] (allocates an array, disposes automatically)
    /// byte[] result = ZA.Utf8("Hello ", name, "!");
    /// 
    /// // Zero-allocation usage with span (must dispose manually)
    /// using TempBytes temp = ZA.Utf8("Hello ", name, "!");
    /// ReadOnlySpan&lt;byte&gt; span = temp.AsSpan();
    /// await stream.WriteAsync(span);
    /// </code>
    /// <para>
    /// The source generator creates optimized overloads for specific type combinations.
    /// This method should never be called - if reached, the generator failed.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// This method should never be called. The source generator should create specific overloads.
    /// </exception>
    public static TempBytes Utf8(params object?[] values)
    {
        throw new InvalidOperationException(
            "The generic Utf8() method should never be called. " +
            "The source generator should have created a specific overload for your argument types. " +
            "Please rebuild your project to regenerate the source.");
    }

    /// <summary>
    /// Tries to encode the concatenated values as UTF-8 into the destination buffer.
    /// </summary>
    /// <param name="destination">The buffer to write to.</param>
    /// <param name="bytesWritten">When successful, the number of bytes written.</param>
    /// <param name="values">The values to concatenate and encode.</param>
    /// <returns>True if the buffer was large enough; otherwise false.</returns>
    /// <remarks>
    /// The source generator creates optimized overloads for specific type combinations.
    /// This method should never be called - if reached, the generator failed.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// This method should never be called. The source generator should create specific overloads.
    /// </exception>
    public static bool TryUtf8(Span<byte> destination, out int bytesWritten, params object?[] values)
    {
        throw new InvalidOperationException(
            "The generic TryUtf8() method should never be called. " +
            "The source generator should have created a specific overload for your argument types. " +
            "Please rebuild your project to regenerate the source.");
    }

    #endregion

    #region Bytes API

    /// <summary>
    /// Serializes the given values into temporary binary bytes with zero intermediate allocations.
    /// </summary>
    /// <param name="values">The values to serialize.</param>
    /// <returns>A <see cref="TempBytes"/> that can be implicitly converted to a byte array or used as <see cref="ReadOnlySpan{Byte}"/>.</returns>
    /// <remarks>
    /// <para>
    /// The returned <see cref="TempBytes"/> can be used in several ways:
    /// </para>
    /// <code>
    /// // Implicit conversion to byte[] (allocates an array, disposes automatically)
    /// byte[] result = ZA.Bytes(header, payload);
    /// 
    /// // Zero-allocation usage with span (must dispose manually)
    /// using TempBytes temp = ZA.Bytes(header, payload);
    /// ReadOnlySpan&lt;byte&gt; span = temp.AsSpan();
    /// await stream.WriteAsync(span);
    /// </code>
    /// <para>
    /// The source generator creates optimized overloads for specific type combinations.
    /// This generic version is not supported and will throw.
    /// </para>
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// The generic Bytes method is not supported. Use specific type overloads.
    /// </exception>
    public static TempBytes Bytes(params object?[] values)
    {
        // Binary serialization requires known types at compile time
        throw new NotSupportedException(
            "The generic Bytes() method is not supported. " +
            "Use the source generator to create type-specific overloads.");
    }

    /// <summary>
    /// Tries to serialize the given values into the destination buffer.
    /// </summary>
    /// <param name="destination">The buffer to write to.</param>
    /// <param name="bytesWritten">When successful, the number of bytes written.</param>
    /// <param name="values">The values to serialize.</param>
    /// <returns>True if the buffer was large enough; otherwise false.</returns>
    /// <remarks>
    /// The source generator creates optimized overloads for specific type combinations.
    /// This generic version is not supported and will throw.
    /// </remarks>
    /// <exception cref="NotSupportedException">
    /// The generic TryBytes method is not supported. Use specific type overloads.
    /// </exception>
    public static bool TryBytes(Span<byte> destination, out int bytesWritten, params object?[] values)
    {
        // Binary serialization requires known types at compile time
        throw new NotSupportedException(
            "The generic TryBytes() method is not supported. " +
            "Use the source generator to create type-specific overloads.");
    }

    #endregion

    #region LocalizedString API (Culture-Sensitive)

    /// <summary>
    /// Concatenates the given values into a temporary string using the specified format provider for formatting.
    /// </summary>
    /// <param name="provider">The format provider (culture) to use for formatting.</param>
    /// <param name="values">The values to concatenate.</param>
    /// <returns>A <see cref="TempString"/> that can be implicitly converted to <see cref="string"/> or used as <see cref="ReadOnlySpan{Char}"/>.</returns>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="String(object?[])"/>, this method formats numbers, dates, and other values
    /// using the specified <paramref name="provider"/>, producing output identical
    /// to standard C# interpolated strings with the given culture.
    /// </para>
    /// <code>
    /// // Implicit conversion to string (allocates a string, disposes automatically)
    /// string result = ZA.LocalizedString(CultureInfo.GetCultureInfo("de-DE"), "Price: ", amount);
    /// 
    /// // Zero-allocation usage with span (must dispose manually)
    /// using TempString temp = ZA.LocalizedString(culture, "Price: ", amount);
    /// ReadOnlySpan&lt;char&gt; span = temp.AsSpan();
    /// </code>
    /// <para>
    /// The source generator creates optimized overloads for specific type combinations.
    /// This method should never be called - if reached, the generator failed.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// This method should never be called. The source generator should create specific overloads.
    /// </exception>
    public static TempString LocalizedString(IFormatProvider? provider, params object?[] values)
    {
        throw new InvalidOperationException(
            "The generic LocalizedString() method should never be called. " +
            "The source generator should have created a specific overload for your argument types. " +
            "Please rebuild your project to regenerate the source.");
    }

    /// <summary>
    /// Tries to write the concatenated values into the destination buffer using the specified format provider.
    /// </summary>
    /// <param name="provider">The format provider (culture) to use for formatting.</param>
    /// <param name="destination">The buffer to write to.</param>
    /// <param name="charsWritten">When successful, the number of characters written.</param>
    /// <param name="values">The values to concatenate.</param>
    /// <returns>True if the buffer was large enough; otherwise false.</returns>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="TryString(Span{char}, out int, object?[])"/>, this method formats numbers,
    /// dates, and other values using the specified <paramref name="provider"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// This method should never be called. The source generator should create specific overloads.
    /// </exception>
    public static bool TryLocalizedString(IFormatProvider? provider, Span<char> destination, out int charsWritten, params object?[] values)
    {
        throw new InvalidOperationException(
            "The generic TryLocalizedString() method should never be called. " +
            "The source generator should have created a specific overload for your argument types. " +
            "Please rebuild your project to regenerate the source.");
    }

    /// <summary>
    /// Creates temporary UTF-8 content using the specified format provider.
    /// </summary>
    /// <param name="provider">The format provider (culture) to use for formatting.</param>
    /// <param name="values">The values to encode.</param>
    /// <returns>A <see cref="TempBytes"/> that can be implicitly converted to a byte array or used as <see cref="ReadOnlySpan{Byte}"/>.</returns>
    /// <remarks>
    /// <para>
    /// Unlike <see cref="Utf8(object?[])"/>, this method formats numbers, dates, and other values
    /// using the specified <paramref name="provider"/>, producing culture-specific UTF-8 output.
    /// </para>
    /// <code>
    /// // Implicit conversion to byte[] (allocates an array, disposes automatically)
    /// byte[] result = ZA.LocalizedUtf8(CultureInfo.GetCultureInfo("de-DE"), "Price: ", amount);
    /// 
    /// // Zero-allocation usage with span (must dispose manually)
    /// using TempBytes temp = ZA.LocalizedUtf8(culture, "Price: ", amount);
    /// ReadOnlySpan&lt;byte&gt; span = temp.AsSpan();
    /// await stream.WriteAsync(span);
    /// </code>
    /// <para>
    /// The source generator creates optimized overloads for specific type combinations.
    /// This method should never be called - if reached, the generator failed.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// This method should never be called. The source generator should create specific overloads.
    /// </exception>
    public static TempBytes LocalizedUtf8(IFormatProvider? provider, params object?[] values)
    {
        throw new InvalidOperationException(
            "The generic LocalizedUtf8() method should never be called. " +
            "The source generator should have created a specific overload for your argument types. " +
            "Please rebuild your project to regenerate the source.");
    }

    /// <summary>
    /// Tries to write the concatenated UTF-8 values into the destination buffer using the specified format provider.
    /// </summary>
    /// <param name="provider">The format provider (culture) to use for formatting.</param>
    /// <param name="destination">The buffer to write to.</param>
    /// <param name="bytesWritten">When successful, the number of bytes written.</param>
    /// <param name="values">The values to concatenate.</param>
    /// <returns>True if the buffer was large enough; otherwise false.</returns>
    /// <exception cref="InvalidOperationException">
    /// This method should never be called. The source generator should create specific overloads.
    /// </exception>
    public static bool TryLocalizedUtf8(IFormatProvider? provider, Span<byte> destination, out int bytesWritten, params object?[] values)
    {
        throw new InvalidOperationException(
            "The generic TryLocalizedUtf8() method should never be called. " +
            "The source generator should have created a specific overload for your argument types. " +
            "Please rebuild your project to regenerate the source.");
    }

    #endregion

    #region Lazy API

    /// <summary>
    /// Creates a <see cref="LazyString"/> that defers string building using <c>ZA.String()</c> internally.
    /// Optimal when the ThreadStatic buffer is available (not during a nested <c>ZA.String</c> call).
    /// </summary>
    /// <param name="values">The values to format lazily.</param>
    /// <returns>A <see cref="LazyString"/> whose string is built on first access.</returns>
    /// <exception cref="InvalidOperationException">
    /// This method should never be called. The source generator should create specific overloads.
    /// </exception>
    public static LazyString Lazy(params object?[] values)
    {
        throw new InvalidOperationException(
            "The generic Lazy() method should never be called. " +
            "The source generator should have created a specific overload for your argument types. " +
            "Please rebuild your project to regenerate the source.");
    }

    /// <summary>
    /// Creates a <see cref="LazyString"/> that defers string building using <c>$"..."</c> interpolation.
    /// Safe for recursive or nested calls where the ThreadStatic buffer may already be in use.
    /// </summary>
    /// <param name="values">The values to format lazily.</param>
    /// <returns>A <see cref="LazyString"/> whose string is built on first access.</returns>
    /// <exception cref="InvalidOperationException">
    /// This method should never be called. The source generator should create specific overloads.
    /// </exception>
    public static LazyString LazyInterpolated(params object?[] values)
    {
        throw new InvalidOperationException(
            "The generic LazyInterpolated() method should never be called. " +
            "The source generator should have created a specific overload for your argument types. " +
            "Please rebuild your project to regenerate the source.");
    }

    #endregion
}
