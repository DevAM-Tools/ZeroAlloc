<div align="center">
  <img src="https://raw.githubusercontent.com/DevAM-Tools/ZeroAlloc/main/icon.png" alt="ZeroAlloc Logo" width="200"/>
</div>

# ZeroAlloc

[![NuGet](https://img.shields.io/nuget/v/ZeroAlloc.svg)](https://www.nuget.org/packages/ZeroAlloc/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

**Zero-allocation serialization library with source generators for .NET 10+**

High-performance, zero-allocation serialization library for binary data and strings. Uses Roslyn source generators to create optimized, type-specific formatting code at compile time - no reflection, no boxing, no runtime code generation. Includes `LazyString` for deferred string evaluation — build display text lazily, evaluate only when needed, cache the result atomically. Perfect for network protocols, CAN/FlexRay communication, embedded systems, logging, UI trees, and any scenario where performance and memory efficiency are critical.

> **Main Idea:** ZeroAlloc provides an **interpolated-string-like API** that feels natural and readable, while eliminating heap allocations in 99% of use cases. With `LazyString`, you can defer formatting entirely — strings are only built when someone actually reads them. Focus on writing clean code - let the source generator handle the performance optimizations.

---

## Table of Contents

1. [Quick Start](#quick-start)
2. [Core Concepts](#core-concepts)
3. [String Formatting](#string-formatting)
   - [ZA.String()](#zastring---generated-string-concatenation)
   - [TempStringBuilder](#tempstringbuilder---manual-string-building)
   - [SpanStringBuilder](#spanstringbuilder---span-based-building)
4. [Deferred / Lazy Strings](#deferred--lazy-strings)
   - [LazyString](#lazystring---deferred-string-evaluation)
   - [ZA.Lazy()](#zalazy---generated-deferred-formatting)
   - [ZA.LazyInterpolated()](#zalazyinterpolated---deferred-interpolation)
   - [LazyString.FormatLazy&lt;TState&gt;()](#lazystringformatlazytstate---manual-deferred-formatting)
5. [UTF-8 Generation](#utf-8-generation)
   - [ZA.Utf8()](#zautf8---generated-utf-8-bytes)
6. [Localized Formatting](#localized-formatting)
   - [ZA.LocalizedString()](#zalocalizedstring---culture-sensitive-strings)
   - [ZA.LocalizedUtf8()](#zalocalizedutf8---culture-sensitive-utf-8)
7. [Binary Serialization](#binary-serialization)
   - [ZA.Bytes()](#zabytes---generated-binary-serialization)
   - [TempBytesBuilder](#tempbytesbuilder---manual-byte-building)
   - [SpanBytesBuilder](#spanbytesbuilder---span-based-building)
8. [Binary Parsing](#binary-parsing)
   - [ZA.ParseBytes()](#zaparsebytes---generated-parsing)
   - [[BinaryParsable] Attribute](#binaryparsable-attribute)
   - [BinaryParser](#binaryparser---manual-parsing)
   - [BitReader](#bitreader---bit-level-parsing)
9. [Type Wrappers](#type-wrappers)
10. [Custom Types](#custom-types)
11. [Configuration](#configuration)
12. [Installation](#installation)

---

## Quick Start

```csharp
using ZeroAlloc;

// Step 1: Create a class inheriting from ZeroAllocBase (must be internal partial!)
internal partial class ZA : ZeroAllocBase { }

// Step 2: Use with implicit conversion (convenient, allocates result)
string greeting = ZA.String("User ", userId, " logged in at ", timestamp);
byte[] utf8Data = ZA.Utf8("Count: ", count);
byte[] binary = ZA.Bytes(new U16BE(0x1234), new Utf8("Hello"));

// Step 3: Use with explicit span (zero-allocation, must dispose)
using TempString temp = ZA.String("User ", userId, " logged in");
ReadOnlySpan<char> span = temp.AsSpan();

// Step 4: Defer string formatting until actually needed (lazy evaluation)
LazyString displayText = ZA.Lazy("User ", userId, " logged in at ", timestamp);
// String is NOT built yet — zero work done. Only evaluated on first access:
string text = displayText;  // implicitly converted → string is built and cached.

// Step 5: Use culture-sensitive formatting
string localized = ZA.LocalizedString(CultureInfo.GetCultureInfo("de-DE"), "Price: ", amount, " €");
byte[] localizedUtf8 = ZA.LocalizedUtf8(culture, "Date: ", DateTime.Now);

// Step 6: Parse binary data
if (PacketHeader.TryParse(data, out var header, out int consumed))
{
    Console.WriteLine($"Version: {header.Version.Value}");
}
```

---

## Core Concepts

### Zero-Allocation Guarantee

The zero-allocation promise is valid **up to the configurable buffer size** (default: 2 MiB per thread):

- **Compile-time**: Set `ZeroAlloc_DefaultBufferSize` MSBuild property
- **Runtime**: Use `ZeroAllocHelper.ResizeCharBuffer()` / `ResizeByteBuffer()`

### Dispose Requirements

**All Temp* structs must be properly disposed after use!**

ZeroAlloc provides two usage patterns, each with different allocation behavior:

#### Pattern 1: Implicit Conversion (Convenient, Allocates Result)

When you assign directly to `string` or `byte[]`, implicit conversion operators handle the conversion and dispose automatically:

```csharp
// ✅ Implicit conversion - allocates result string, auto-disposes buffer
string greeting = ZA.String("Hello ", name);

// ✅ Implicit conversion - allocates result byte[], auto-disposes buffer
byte[] utf8 = ZA.Utf8("Count: ", count);
byte[] binary = ZA.Bytes(header, payload);

// ✅ Culture-sensitive versions work the same way
string localized = ZA.LocalizedString(culture, "Price: ", amount);
byte[] localizedUtf8 = ZA.LocalizedUtf8(culture, "Date: ", DateTime.Now);
```

#### Pattern 2: Explicit Span (Zero-Allocation, Manual Dispose)

For true zero-allocation scenarios, use the `using` statement and work with spans:

```csharp
// ✅ Zero-allocation: using statement + span access
using TempString temp = ZA.String("Hello ", name);
ReadOnlySpan<char> span = temp.AsSpan();
// Work with span here...

// ✅ Zero-allocation UTF-8
using TempBytes utf8 = ZA.Utf8("Count: ", count);
await stream.WriteAsync(utf8.AsSpan().ToArray()); // Or use span directly

// ✅ Zero-allocation with culture
using TempString localized = ZA.LocalizedString(culture, "Price: ", amount);
ProcessSpan(localized.AsSpan());

// ❌ Wrong: No dispose - buffer stays locked!
var temp = ZA.String("Hello ", name);
// Buffer is never released, next call may allocate on heap
```

#### ⚠️ Warning: Nested API Calls with `using var`

**Be careful when using `using var` (without braces) with multiple ZeroAlloc calls!**

The `using var` declaration keeps the buffer locked until the end of the enclosing scope. If you make multiple ZeroAlloc API calls in the same scope, only the first one uses the ThreadStatic buffer - subsequent calls trigger heap allocation fallback.

```csharp
// ❌ PROBLEMATIC: All three calls share the same scope!
void ProcessData()
{
    using var temp1 = ZA.String("First ", value1);   // Uses ThreadStatic buffer ✅
    using var temp2 = ZA.String("Second ", value2);  // Buffer locked! Falls back to heap ⚠️
    using var temp3 = ZA.String("Third ", value3);   // Buffer locked! Falls back to heap ⚠️
    
    // temp1 is still holding the ThreadStatic buffer until method ends!
    DoSomething(temp1.AsSpan());
    DoSomething(temp2.AsSpan());  // temp2 was heap-allocated
    DoSomething(temp3.AsSpan());  // temp3 was heap-allocated
}

// ✅ CORRECT: Use explicit scopes to release buffers immediately
void ProcessDataCorrectly()
{
    {
        using var temp1 = ZA.String("First ", value1);  // Uses ThreadStatic buffer ✅
        DoSomething(temp1.AsSpan());
    } // temp1 disposed, buffer released
    
    {
        using var temp2 = ZA.String("Second ", value2); // Uses ThreadStatic buffer ✅
        DoSomething(temp2.AsSpan());
    } // temp2 disposed, buffer released
    
    {
        using var temp3 = ZA.String("Third ", value3);  // Uses ThreadStatic buffer ✅
        DoSomething(temp3.AsSpan());
    } // temp3 disposed, buffer released
}

// ✅ ALTERNATIVE: Use implicit conversion (allocates result, auto-disposes)
void ProcessDataSimple()
{
    string result1 = ZA.String("First ", value1);   // Allocates string, buffer released
    string result2 = ZA.String("Second ", value2);  // Allocates string, buffer released
    string result3 = ZA.String("Third ", value3);   // Allocates string, buffer released
}
```

**Key Insight:** The ThreadStatic buffer is shared per-thread. When you hold a reference to it (via `TempString`, `TempBytes`, etc.), subsequent calls on the same thread must use heap allocation. Check `IsHeapAllocated` property to detect this situation:

```csharp
using var temp = ZA.String("Hello ", name);
if (temp.IsHeapAllocated)
{
    // This indicates nested usage or a very rare concurrent access issue
    Console.WriteLine("Warning: Heap allocation was used!");
}
```

### Builder Types Overview

ZeroAlloc provides four builder types for manual string and binary construction. Each has specific use cases and trade-offs:

### Temp Struct Types

ZeroAlloc uses disposable ref structs to provide zero-allocation access to formatted content:

| Return Type | From Methods | Implicit Conversion | Buffer Source | Dispose |
|-------------|--------------|---------------------|---------------|---------|
| **TempString** | `ZA.String()`, `ZA.LocalizedString()` | `string`, `ReadOnlySpan<char>` | ThreadStatic | ✅ Required (auto on implicit cast) |
| **TempBytes** | `ZA.Utf8()`, `ZA.Bytes()`, `ZA.LocalizedUtf8()` | `byte[]`, `ReadOnlySpan<byte>` | ThreadStatic | ✅ Required (auto on implicit cast) |

### Builder Types Overview

| Builder | Buffer Source | Dispose | Auto-Grow | Best For |
|---------|---------------|---------|-----------|----------|
| **TempStringBuilder** | ThreadStatic | ✅ Required | ✅ Yes | General string building with unknown sizes |
| **TempBytesBuilder** | ThreadStatic | ✅ Required | ✅ Yes | General binary building with unknown sizes |
| **SpanStringBuilder** | User-provided | ❌ Not needed | ❌ No | Hot paths with known maximum sizes |
| **SpanBytesBuilder** | User-provided | ❌ Not needed | ❌ No | Hot paths with known maximum sizes |

#### TempStringBuilder / TempBytesBuilder

**When to use:**
- Building content with unknown or variable size
- Conditional logic that affects output length
- Processing collections where total size is unknown
- General-purpose building without size constraints

**Characteristics:**
- Uses ThreadStatic buffer (one per thread, no heap allocation in normal case)
- **Must be disposed** to release the buffer for reuse
- Auto-grows if content exceeds buffer (triggers one-time allocation)

**Usage pattern:**
```csharp
using var builder = TempStringBuilder.Create();
builder.Append("User ");
builder.Append(userId);
if (isAdmin) builder.Append(" [ADMIN]");
ReadOnlySpan<char> result = builder.AsSpan();
```

**⚠️ Important constraints:**
- Only one ThreadStatic buffer per thread - nested calls fall back to heap allocation
- Always use `using` statement to ensure disposal
- Not thread-safe (each thread has its own buffer)

#### SpanStringBuilder / SpanBytesBuilder

**When to use:**
- Hot paths where every nanosecond counts
- Known maximum output size
- Tight loops processing many items
- When you want zero heap allocations guaranteed
- Inside `TempString` / `TempBytes` callbacks (to avoid nested ThreadStatic usage)

**Characteristics:**
- Uses user-provided buffer (typically `stackalloc` or pre-allocated array)
- **No dispose needed** - you manage the buffer lifetime
- **Throws** `InvalidOperationException` if buffer is too small
- Slightly faster than TempBuilder due to no ThreadStatic lookup

**Usage pattern:**
```csharp
Span<byte> buffer = stackalloc byte[256];
var builder = new SpanBytesBuilder(buffer);
builder.AppendUInt16BigEndian(0x1234);
builder.AppendUtf8("Hello");
ReadOnlySpan<byte> result = builder.AsSpan();
// No dispose needed!
```

**⚠️ Important constraints:**
- You must know the maximum size upfront
- Throws if buffer overflows (no recovery)
- Stack size is limited (~1 MB on most platforms)

#### Performance Comparison

| Scenario | Recommended Builder | Reason |
|----------|---------------------|--------|
| Unknown/variable size | `TempStringBuilder` / `TempBytesBuilder` | Auto-grow handles any size |
| Known max size, hot path | `SpanStringBuilder` / `SpanBytesBuilder` | Zero overhead, user-managed buffer |
| Inside `TempString` callback | `SpanStringBuilder` | Avoids nested ThreadStatic usage |
| Large outputs (>1 KB) | `TempStringBuilder` / `TempBytesBuilder` | Stack size limits stackalloc |

---

## String Formatting

### ZA.String() - Generated String Concatenation

The source generator creates optimized methods for each call site. Returns a `TempString` that can be used in two ways:

```csharp
// Implicit conversion to string (allocates string, auto-disposes)
string result = ZA.String("User ", userId, " logged in at ", timestamp);

// Zero-allocation with span (must dispose manually)
using TempString temp = ZA.String("User ", userId, " logged in at ", timestamp);
ReadOnlySpan<char> span = temp.AsSpan();
```

#### Supported Types for ZA.String()

| Category | Types |
|----------|-------|
| **Text** | `string`, `char`, `ReadOnlySpan<char>` |
| **Signed Integers** | `sbyte`, `short`, `int`, `long`, `Int128`, `nint` |
| **Unsigned Integers** | `byte`, `ushort`, `uint`, `ulong`, `UInt128`, `nuint` |
| **Floating Point** | `Half`, `float`, `double`, `decimal` |
| **Date/Time** | `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly` |
| **Other** | `Guid`, `bool` |
| **Custom** | Any type implementing `ISpanFormattable` |
| **With Size Hint** | Any type implementing `IStringSize` (for optimal pre-allocation) |

---

### TempStringBuilder - Manual String Building

Use when you need conditional logic or loops.

```csharp
using var builder = TempStringBuilder.Create();
builder.Append("User ");
builder.Append(userId);
if (isAdmin) builder.Append(" [ADMIN]");
builder.AppendLine();

ReadOnlySpan<char> result = builder.AsSpan();
```

#### TempStringBuilder.Append() Supported Types

| Category | Types |
|----------|-------|
| **Text** | `string`, `char`, `ReadOnlySpan<char>` |
| **Signed Integers** | `sbyte`, `short`, `int`, `long`, `Int128`, `nint` |
| **Unsigned Integers** | `byte`, `ushort`, `uint`, `ulong`, `UInt128`, `nuint` |
| **Floating Point** | `Half`, `float`, `double`, `decimal` |
| **Date/Time** | `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly` |
| **Other** | `Guid`, `bool` |
| **Formatted** | `Append<T>(T value, format, provider)` where `T : ISpanFormattable` |

#### TempStringBuilder Special Methods

| Method | Description | Example |
|--------|-------------|---------|
| `AppendLine()` | Appends newline | |
| `AppendLine(string)` | Appends text + newline | |
| `AppendHex2(byte)` | 2 hex chars | `0xAB` → `"AB"` |
| `AppendHex4(ushort)` | 4 hex chars | `0x1234` → `"1234"` |
| `AppendHex8(uint)` | 8 hex chars | `0xDEADBEEF` → `"DEADBEEF"` |
| `AppendHex16(ulong)` | 16 hex chars | |
| `AppendBinary8(byte)` | 8 binary chars | `0b10101010` → `"10101010"` |
| `AppendBinary16(ushort)` | 16 binary chars | |
| `AppendBinary32(uint)` | 32 binary chars | |
| `AppendBinary64(ulong)` | 64 binary chars | |

---

### SpanStringBuilder - Span-Based Building

For maximum performance with known buffer sizes. No dispose needed.

```csharp
Span<char> buffer = stackalloc char[128];
var builder = new SpanStringBuilder(buffer);
builder.Append("Value: ");
builder.Append(42);
ReadOnlySpan<char> result = builder.AsSpan();
```

> **Note:** `SpanStringBuilder` throws if the buffer is too small. Use `TempStringBuilder` for auto-growing buffers.

**Supported types:** Same as `TempStringBuilder`

---

## Deferred / Lazy Strings

### LazyString - Deferred String Evaluation

`LazyString` is an 8-byte readonly struct that wraps a string value which may be evaluated immediately or deferred until first access. Once evaluated, the result is atomically cached so subsequent reads are a single pointer dereference.

**Use cases:**
- Protocol dissectors that build display text for thousands of fields per packet, but only a few are ever shown to the user
- Logging frameworks where most log messages are filtered before rendering
- UI trees where only visible nodes need their text materialized
- Any scenario where you build many strings but consume only a fraction

#### Direct / Immediate Values

```csharp
// From a string literal or variable
LazyString name = new LazyString("Hello");

// Implicit conversion from string
LazyString name2 = "Hello";

// Empty and null
LazyString empty = LazyString.Empty;       // "", not null
LazyString absent = default;                // null / absent
```

#### Deferred with ZA.Lazy() (Recommended)

The source generator creates optimized `Lazy()` overloads that capture arguments in a value tuple and defer to the generated `String()` method on first access. This is the **recommended** way to create deferred strings because it combines zero-allocation formatting with lazy evaluation:

```csharp
// Deferred: no string is built here — arguments are captured in a tuple
LazyString text = ZA.Lazy("User ", userId, " logged in at ", timestamp);

// The string is built on first access and then cached forever:
string result = text;            // implicit conversion → builds and caches
string again  = text.ToString(); // same cached string, no re-evaluation
```

See [ZA.Lazy()](#zalazy---generated-deferred-formatting) for details.

#### Deferred with LazyString.FormatLazy&lt;TState&gt;()

For advanced scenarios where you need full control over the formatting logic:

```csharp
// Capture state in a value tuple, use a static lambda to avoid closure allocation
LazyString text = LazyString.FormatLazy(
    (srcPort, dstPort),
    static s => $"Src: {s.Item1}, Dst: {s.Item2}");
```

See [LazyString.FormatLazy&lt;TState&gt;()](#lazystringformatlazytstate---manual-deferred-formatting) for details.

#### Deferred with Func&lt;string&gt;

For simple one-off deferred evaluation without captured state:

```csharp
LazyString text = LazyString.Lazy(() => ExpensiveComputation());
```

#### Properties and Methods

| Member | Description |
|--------|-------------|
| `ToString()` | Evaluates and returns the string (caches result). Returns `""` if absent. Standard .NET method. |
| `AsString` | Same as `ToString()`. Property alternative for expression contexts. |
| `TryGetString(out string)` | Safe evaluation: returns `false` if the factory threw an exception. |
| `IsNull` | `true` if no value is set (`default` struct). |
| `IsEmpty` | `true` if absent or the evaluated string is zero-length. |
| `IsLazy` | `true` if wrapping an unevaluated deferred factory. |
| `IsEvaluated` | `true` if already resolved to a string (direct, cached, or null). |
| `Length` | Length of the evaluated string (triggers evaluation). |
| `AsSpan` | `ReadOnlySpan<char>` of the evaluated string. |
| `Append(LazyString)` | Eager concatenation—both sides are evaluated immediately. |
| `Prepend(LazyString)` | Eager prepend. |
| `TryWriteTo(Span<char>)` | Writes the evaluated string into a destination span. Returns chars written or -1. |

#### Implicit Conversions

```csharp
// string → LazyString (wraps directly, no copy)
LazyString a = "Hello";

// LazyString → string (evaluates if lazy, returns cached result)
string b = a;              // implicit conversion
string c = a.ToString();   // explicit call — same result
```

#### Equality and Comparison

`LazyString` implements `IEquatable<LazyString>` and `IComparable<LazyString>`. Comparison is ordinal and triggers evaluation of lazy values:

```csharp
LazyString a = "Hello";
LazyString b = ZA.Lazy("Hel", "lo");

bool equal = a == b;               // true (ordinal comparison)
int order  = a.CompareTo(b);       // 0
```

#### Thread Safety

`LazyString` uses `Interlocked.CompareExchange` for lock-free atomic caching. Multiple threads can safely read the same `LazyString`. If two threads race to evaluate, the factory may run twice but only one result is stored—the other is discarded. Subsequent reads on all threads see the cached string.

#### Advanced: RawValue / FromRawValue

For storage in union-style containers (e.g., a field value that can be int, bool, or LazyString), `RawValue` exposes the inner `object?` and `FromRawValue` reconstructs the struct:

```csharp
// Store the inner object in a union field
object? raw = myLazyString.RawValue;

// Reconstruct later
LazyString restored = LazyString.FromRawValue(raw);
```

---

### ZA.Lazy() - Generated Deferred Formatting

`ZA.Lazy()` is the **recommended** way to create deferred strings. The source generator creates a unique overload for each call site's argument types. Internally, it captures all arguments in a `ValueTuple`, wraps it in a `DeferredFormat<T>`, and calls the generated `ZA.String()` method on first evaluation.

**Cost model:**
- **At call site:** 1 heap allocation (the `DeferredFormat<ValueTuple<...>>` object)
- **On first access:** Uses the zero-allocation `String()` pipeline, allocates the result `string`
- **On subsequent access:** Returns the cached `string` (single pointer dereference)

**Supports the same types as [ZA.String()](#zastring---generated-string-concatenation).**

#### Examples

```csharp
// Simple concatenation (deferred)
LazyString greeting = ZA.Lazy("Hello, ", name, "!");

// Numeric formatting
LazyString portInfo = ZA.Lazy("Src: ", srcPort, " → Dst: ", dstPort);

// Single argument
LazyString label = ZA.Lazy(protocolName);

// Use in a data structure — only materialized when displayed
record FieldNode(string Name, LazyString DisplayText);
List<FieldNode> fields =
[
    new("src_port", ZA.Lazy("Source Port: ", srcPort)),
    new("dst_port", ZA.Lazy("Destination Port: ", dstPort)),
    new("length",   ZA.Lazy("Length: ", length, " bytes")),
];

// Only the field the user clicks on gets its string materialized:
Console.WriteLine(fields[0].DisplayText.ToString());
```

#### Conditional / Ternary Pattern

When the display text depends on a condition, evaluate the condition eagerly and choose between an immediate `LazyString` and a deferred one:

```csharp
// ✅ Correct: condition is cheap, string building is deferred
LazyString lifetime = (seconds == 0xFFFFFFFF)
    ? new LazyString("Infinity")
    : ZA.Lazy(seconds, " seconds");
```

---

### ZA.LazyInterpolated() - Deferred Interpolation

`ZA.LazyInterpolated()` works like `ZA.Lazy()` but uses C# string interpolation instead of the `String()` pipeline for evaluation. Use it when `ZA.Lazy()` would cause ThreadStatic buffer contention (e.g., inside another `String()` scope).

```csharp
// Uses interpolation on evaluation (not the zero-alloc pipeline)
LazyString text = ZA.LazyInterpolated("Port: ", port, " (", protocolName, ")");
```

**When to use `LazyInterpolated` vs `Lazy`:**

| Scenario | Use |
|----------|-----|
| Normal deferred formatting | `ZA.Lazy()` |
| Inside an existing `ZA.String()` scope | `ZA.LazyInterpolated()` |
| Maximum formatting performance | `ZA.Lazy()` |

---

### LazyString.FormatLazy&lt;TState&gt;() - Manual Deferred Formatting

For scenarios where `ZA.Lazy()` doesn't fit—custom formatting logic, conditional branches, complex transformations—use the static `FormatLazy<TState>()` method directly:

```csharp
// Capture state as a value tuple, use a static lambda to avoid closure allocation
LazyString text = LazyString.FormatLazy(
    (srcPort, dstPort, protocol),
    static s => ZA.String(s.protocol, ": ", s.srcPort, " → ", s.dstPort));
```

**Rules:**
1. **Always use a `static` lambda** — prevents a closure object allocation, keeping total allocations at 1 (the `DeferredFormat<T>` box)
2. **Capture as value tuple** — tuples are value types, so they're boxed once inside `DeferredFormat<T>`
3. **Named tuple elements work** — `(srcPort, dstPort)` gives you `s.srcPort`, `s.dstPort`

#### Allocation Comparison

| Pattern | Allocations per call |
|---------|---------------------|
| `ZA.Lazy("A: ", value)` | 1 (`DeferredFormat<ValueTuple<string, int>>`) |
| `LazyString.FormatLazy((a, b), static s => ...)` | 1 (`DeferredFormat<ValueTuple<...>>`) |
| `LazyString.FormatLazy(x, static s => s.ToString())` | 1 (`DeferredFormat<T>`) |
| `new LazyString("literal")` | 0 (direct string wrap) |
| `LazyString.Lazy(() => Compute())` | 1 (`Func<string>` delegate, unless cached) |

---

## UTF-8 Generation

### ZA.Utf8() - Generated UTF-8 Bytes

Generate UTF-8 encoded bytes without intermediate string allocation. Returns a `TempBytes` that can be used in two ways:

```csharp
// Implicit conversion to byte[] (allocates array, auto-disposes)
byte[] result = ZA.Utf8("User ", userId, " logged in");

// Zero-allocation with span (must dispose manually)
using TempBytes temp = ZA.Utf8("User ", userId, " logged in");
await stream.WriteAsync(temp.AsSpan().ToArray());
```

#### Supported Types for ZA.Utf8()

| Category | Types |
|----------|-------|
| **Text** | `string`, `char`, `ReadOnlySpan<char>` |
| **Signed Integers** | `sbyte`, `short`, `int`, `long`, `Int128`, `nint` |
| **Unsigned Integers** | `byte`, `ushort`, `uint`, `ulong`, `UInt128`, `nuint` |
| **Floating Point** | `Half`, `float`, `double`, `decimal` |
| **Date/Time** | `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly` |
| **Other** | `Guid`, `bool` |
| **Custom** | Any type implementing `IUtf8SpanFormattable` |
| **With Size Hint** | Any type implementing `IUtf8Size` (for optimal pre-allocation) |

---

## Localized Formatting

For culture-sensitive formatting of numbers, dates, and other values, use the `LocalizedString` and `LocalizedUtf8` methods. These produce output identical to standard C# interpolated strings with the given culture.

### ZA.LocalizedString() - Culture-Sensitive Strings

Generate culture-formatted strings. Returns a `TempString` that can be implicitly converted to `string` or used as a span:

```csharp
using System.Globalization;

// Implicit conversion to string (allocates string, auto-disposes)
CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
string price = ZA.LocalizedString(german, "Price: ", 1234.56, " €");
// Result: "Price: 1234,56 €" (German decimal separator)

// Zero-allocation with span (must dispose manually)
using TempString temp = ZA.LocalizedString(german, "Date: ", DateTime.Now);
ReadOnlySpan<char> span = temp.AsSpan();
```

#### Supported Types for LocalizedString()

| Category | Types |
|----------|-------|
| **Text** | `string`, `char`, `ReadOnlySpan<char>` |
| **Signed Integers** | `sbyte`, `short`, `int`, `long`, `Int128`, `nint` |
| **Unsigned Integers** | `byte`, `ushort`, `uint`, `ulong`, `UInt128`, `nuint` |
| **Floating Point** | `Half`, `float`, `double`, `decimal` |
| **Date/Time** | `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly` |
| **Other** | `Guid`, `bool` |
| **Custom** | Any type implementing `ISpanFormattable` |

---

### ZA.LocalizedUtf8() - Culture-Sensitive UTF-8

Generate culture-formatted UTF-8 bytes directly. Returns a `TempBytes` that can be implicitly converted to `byte[]` or used as a span:

```csharp
using System.Globalization;

// Implicit conversion to byte[] (allocates array, auto-disposes)
CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
byte[] utf8 = ZA.LocalizedUtf8(french, "Total: ", 9876.54);
// Result: UTF-8 bytes for "Total: 9876,54" (French decimal separator)

// Zero-allocation with span (must dispose manually)
using TempBytes temp = ZA.LocalizedUtf8(french, "Date: ", DateTime.Now);
await stream.WriteAsync(temp.AsSpan().ToArray());
```

#### Supported Types for LocalizedUtf8()

| Category | Types |
|----------|-------|
| **Text** | `string`, `char`, `ReadOnlySpan<char>` |
| **Signed Integers** | `sbyte`, `short`, `int`, `long`, `Int128`, `nint` |
| **Unsigned Integers** | `byte`, `ushort`, `uint`, `ulong`, `UInt128`, `nuint` |
| **Floating Point** | `Half`, `float`, `double`, `decimal` |
| **Date/Time** | `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly` |
| **Other** | `Guid`, `bool` |
| **Custom** | Any type implementing `IUtf8SpanFormattable` |

---

### TryLocalizedString / TryLocalizedUtf8

For buffer-based formatting with culture support:

```csharp
// TryLocalizedString - write to char buffer
Span<char> charBuffer = stackalloc char[256];
if (ZA.TryLocalizedString(german, charBuffer, out int charsWritten, "Price: ", amount))
{
    // Use charBuffer[..charsWritten]
}

// TryLocalizedUtf8 - write to byte buffer
Span<byte> byteBuffer = stackalloc byte[256];
if (ZA.TryLocalizedUtf8(german, byteBuffer, out int bytesWritten, "Price: ", amount))
{
    // Use byteBuffer[..bytesWritten]
}
```

---

## Binary Serialization

### ZA.Bytes() - Generated Binary Serialization

Build binary data with precise control over byte order and encoding. Returns a `TempBytes` that can be used in two ways:

```csharp
// Implicit conversion to byte[] (allocates array, auto-disposes)
byte[] binary = ZA.Bytes(
    new U16BE(0x1234),       // Big-endian ushort (short form)
    new Utf8Var("Hello"),    // VarInt-prefixed UTF-8 string
    new Raw(payload)         // Raw byte array
);

// Zero-allocation with span (must dispose manually)
using TempBytes temp = ZA.Bytes(header, payload);
await stream.WriteAsync(temp.AsSpan().ToArray());
```

#### Supported Types for ZA.Bytes()

| Category | Types |
|----------|-------|
| **Big-Endian Integers** | `U16BE`, `U32BE`, `U64BE`, `U128BE`, `I16BE`, `I32BE`, `I64BE`, `I128BE` |
| **Little-Endian Integers** | `U16LE`, `U32LE`, `U64LE`, `U128LE`, `I16LE`, `I32LE`, `I64LE`, `I128LE` |
| **Floating Point** | `F32BE`, `F32LE`, `F64BE`, `F64LE` |
| **Variable-Length** | `VarInt`, `VarIntZigZag` |
| **UTF-8 Strings** | `Utf8`, `Utf8Z`, `Utf8Var`, `Utf8FixBE`, `Utf8FixLE`, `Utf8Fix16LE` |
| **Other Encodings** | `Ascii`, `Latin1`, `Utf16BE`, `Utf16LE` |
| **Raw Bytes** | `Raw` |
| **Custom** | Any type implementing `IBinarySerializable` or `IUtf8SpanFormattable` |

---

### TempBytesBuilder - Manual Byte Building

Use when you need conditional logic or loops.

```csharp
using var builder = TempBytesBuilder.Create();
builder.AppendUInt16BigEndian(0x0100);       // Version
builder.AppendUtf8WithVarIntPrefix(message); // VarInt-prefixed string
if (hasPayload) builder.Append(payload);

stream.Write(builder.AsSpan());
```

#### TempBytesBuilder.Append() Supported Types

| Category | Types |
|----------|-------|
| **Raw Bytes** | `byte`, `byte[]`, `ReadOnlySpan<byte>` |
| **Big-Endian Wrappers** | `U16BE`, `U32BE`, `U64BE`, `U128BE`, `I16BE`, `I32BE`, `I64BE`, `I128BE` |
| **Little-Endian Wrappers** | `U16LE`, `U32LE`, `U64LE`, `U128LE`, `I16LE`, `I32LE`, `I64LE`, `I128LE` |
| **String Wrappers** | `Utf8Raw`, `Utf8NullTerminated`, `Utf8VarInt`, `Utf8BEFixed`, `Utf8LEFixed` |
| **Variable-Length** | `VarInt`, `VarIntZigZag` |
| **Custom** | `IBinarySerializable` |

#### TempBytesBuilder Integer Methods

| Big-Endian | Little-Endian | Size |
|------------|---------------|------|
| `AppendInt16BigEndian(short)` | `AppendInt16LittleEndian(short)` | 2 bytes |
| `AppendInt32BigEndian(int)` | `AppendInt32LittleEndian(int)` | 4 bytes |
| `AppendInt64BigEndian(long)` | `AppendInt64LittleEndian(long)` | 8 bytes |
| `AppendInt128BigEndian(Int128)` | `AppendInt128LittleEndian(Int128)` | 16 bytes |
| `AppendUInt16BigEndian(ushort)` | `AppendUInt16LittleEndian(ushort)` | 2 bytes |
| `AppendUInt32BigEndian(uint)` | `AppendUInt32LittleEndian(uint)` | 4 bytes |
| `AppendUInt64BigEndian(ulong)` | `AppendUInt64LittleEndian(ulong)` | 8 bytes |
| `AppendUInt128BigEndian(UInt128)` | `AppendUInt128LittleEndian(UInt128)` | 16 bytes |

#### TempBytesBuilder Floating Point Methods

| Big-Endian | Little-Endian | Size |
|------------|---------------|------|
| `AppendHalfBigEndian(Half)` | `AppendHalfLittleEndian(Half)` | 2 bytes |
| `AppendSingleBigEndian(float)` | `AppendSingleLittleEndian(float)` | 4 bytes |
| `AppendDoubleBigEndian(double)` | `AppendDoubleLittleEndian(double)` | 8 bytes |

#### TempBytesBuilder String Methods

| Method | Description |
|--------|-------------|
| `AppendUtf8(string)` | Raw UTF-8 bytes |
| `AppendUtf8NullTerminated(string)` | UTF-8 + null byte |
| `AppendUtf8WithVarIntPrefix(string)` | VarInt length prefix + UTF-8 |
| `AppendUtf8WithLengthPrefixBE(string)` | 4-byte BE length + UTF-8 |
| `AppendUtf8WithLengthPrefixLE(string)` | 4-byte LE length + UTF-8 |

#### TempBytesBuilder VarInt Methods

| Method | Description |
|--------|-------------|
| `AppendVarInt(ulong)` | 7-bit encoded unsigned integer |
| `AppendVarIntZigZag(long)` | ZigZag encoded signed integer |
| `AppendVarIntZigZag(int)` | ZigZag encoded signed 32-bit integer |

#### TempBytesBuilder Special Methods

| Method | Description |
|--------|-------------|
| `AppendHex2(byte)` | 2 hex ASCII bytes |
| `AppendHex4(ushort)` | 4 hex ASCII bytes |
| `AppendHex8(uint)` | 8 hex ASCII bytes |
| `AppendHex16(ulong)` | 16 hex ASCII bytes |
| `AppendBinary8(byte)` | 8 binary ASCII bytes |
| `AppendBinary16(ushort)` | 16 binary ASCII bytes |
| `AppendBinary32(uint)` | 32 binary ASCII bytes |
| `AppendBinary64(ulong)` | 64 binary ASCII bytes |
| `AppendUtf8Formattable<T>(T)` | Any `IUtf8SpanFormattable` |

---

### SpanBytesBuilder - Span-Based Building

For maximum performance with known buffer sizes. No dispose needed.

```csharp
Span<byte> buffer = stackalloc byte[256];
var builder = new SpanBytesBuilder(buffer);
builder.AppendUInt16BigEndian(0x1234);
builder.AppendUtf8("Hello");
ReadOnlySpan<byte> result = builder.AsSpan();
```

> **Note:** `SpanBytesBuilder` throws if the buffer is too small. Use `TempBytesBuilder` for auto-growing buffers.

**Supported types:** Same as `TempBytesBuilder`

---

## Binary Parsing

### ZA.ParseBytes() - Generated Parsing

Parse binary data into tuples using source-generated methods.

```csharp
byte[] data = [0x12, 0x34, 0xDE, 0xAD, 0xBE, 0xEF];

ZA.ParseBytes(data, out (U16BE header, U32BE value) result);
Console.WriteLine($"Header: 0x{result.header.Value:X4}"); // 0x1234
Console.WriteLine($"Value: 0x{result.value.Value:X8}");   // 0xDEADBEEF
```

#### Using [BinaryParsable] Types in Tuples

You can use any `[BinaryParsable]` struct as a tuple member:

```csharp
[BinaryParsable]
public readonly partial struct PacketHeader
{
    public U16BE Version { get; init; }
    public U16BE Flags { get; init; }
}

// Use PacketHeader directly in ParseBytes tuple
ZA.ParseBytes(data, out (PacketHeader header, U32BE payload) result);
Console.WriteLine($"Version: {result.header.Version.Value}");
```

#### Supported Types for ParseBytes

| Category | Types |
|----------|-------|
| **Big-Endian Integers** | `U16BE`, `U32BE`, `U64BE`, `U128BE`, `I16BE`, `I32BE`, `I64BE`, `I128BE` |
| **Little-Endian Integers** | `U16LE`, `U32LE`, `U64LE`, `U128LE`, `I16LE`, `I32LE`, `I64LE`, `I128LE` |
| **Floating Point** | `F32BE`, `F32LE`, `F64BE`, `F64LE` |
| **Variable-Length** | `VarInt`, `VarIntZigZag` |
| **Raw Bytes** | `Raw` |
| **Custom** | Any type implementing `IBinaryParsable<T>` (including `[BinaryParsable]` structs) |

---

### [BinaryParsable] Attribute

Use the `[BinaryParsable]` attribute to auto-generate `TryParse` methods for structs.

```csharp
[BinaryParsable]
public readonly partial struct PacketHeader
{
    public U16BE Version { get; init; }
    public U32BE MessageType { get; init; }
    public U32BE PayloadLength { get; init; }
}

// Generated methods:
// - static bool TryParse(ReadOnlySpan<byte> source, out PacketHeader value, out int bytesConsumed)
// - static PacketHeader Parse(ReadOnlySpan<byte> source)
// - static int FixedSize { get; }  → Returns fixed byte count, or -1 for variable-length types

// Usage
if (PacketHeader.TryParse(data, out var header, out int consumed))
{
    Console.WriteLine($"Version: {header.Version.Value}");
}
```

#### Generated Interface: `IBinaryParsable<T>`

The generated struct implements `IBinaryParsable<T>` which includes:

| Member | Description |
|--------|-------------|
| `static bool TryGetSerializedSize(out int size)` | Returns `true` and the fixed byte count for fixed-size types. Returns `false` for variable-length types (containing strings, VarInt, or variable arrays). |
| `static bool TryParse(...)` | Parses an instance from binary data, returning bytes consumed. |
| `static T Parse(...)` | Parses or throws if insufficient data. |

#### Supported Member Types for [BinaryParsable]

| Category | Types |
|----------|-------|
| **Big-Endian Integers** | `U16BE`, `U32BE`, `U64BE`, `U128BE`, `I16BE`, `I32BE`, `I64BE`, `I128BE` |
| **Little-Endian Integers** | `U16LE`, `U32LE`, `U64LE`, `U128LE`, `I16LE`, `I32LE`, `I64LE`, `I128LE` |
| **Floating Point** | `F32BE`, `F32LE`, `F64BE`, `F64LE` |
| **Primitives** | `byte`, `sbyte` |
| **Primitive Integers** | `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `Half`, `Int128`, `UInt128` (requires `DefaultEndianness`) |
| **Variable-Length** | `VarInt`, `VarIntZigZag` ⚠️ byte-aligned only |
| **Bit Fields** | Any integer with `[BinaryField(BitCount = n)]` |
| **Byte Arrays** | `byte[]` with `[BinaryFixedLength(n)]` or `[BytesLengthVarInt]`, etc. ⚠️ byte-aligned only |
| **Memory Types** | `Memory<byte>` / `ReadOnlyMemory<byte>` with bytes length attributes ⚠️ byte-aligned only |
| **Strings** | `string` with string length attributes ⚠️ byte-aligned only |
| **String Wrappers** | `Utf8Var`, `Utf8FixBE`, `Utf8FixLE`, `Utf8Fix16LE`, `Utf8Z` ⚠️ byte-aligned only |
| **Nested Types** | Any type implementing `IBinaryParsable<T>` ⚠️ byte-aligned only |

> **⚠️ Byte-Alignment Requirement:** Variable-length types (`VarInt`, `VarIntZigZag`), strings, byte arrays, and nested parsable types can only be used when they are byte-aligned. If you use bit fields before these types, ensure the total bit count is a multiple of 8, or use `[BinaryField(PaddingBits = n)]` to add padding bits **after** the field.

#### Parsing Attributes

| Attribute | Properties | Description |
|-----------|------------|-------------|
| `[BinaryOrder(n)]` | `Order` | Explicit member order (0-based). If used on ANY member, ALL non-ignored members must have it. |
| `[BinaryIgnore]` | - | Exclude member from parsing (computed properties, cached values). |
| `[BinaryFixedLength(n)]` | `Length` | Fixed byte count for `byte[]` arrays. |
| `[BinaryField(...)]` | `BitCount`, `Endianness`, `PaddingBits` | Bit-level field width, endianness override, and/or padding bits to skip. |

#### String Length Attributes

Strings must specify how their length is encoded using one of the dedicated string length attributes:

| Attribute | Description | Example |
|-----------|-------------|---------|
| `[StringLengthVarInt]` | VarInt-prefixed length | Most compact |
| `[StringLengthBE(n)]` | Big-endian length prefix (1, 2, or 4 bytes) | Network protocols |
| `[StringLengthLE(n)]` | Little-endian length prefix (1, 2, or 4 bytes) | Windows formats |
| `[StringNullTerminated]` | Null-terminated (C-style) | File paths, BSTR |
| `[StringFixedLength(n)]` | Fixed byte count (null-padded) | Fixed record formats |
| `[StringLengthFromField(name)]` | Length from another field | Dynamic protocols |

```csharp
[BinaryParsable]
public readonly partial struct Message
{
    [StringLengthVarInt]
    public string Name { get; init; }              // VarInt-prefixed (most compact)
    
    [StringLengthBE(2)]
    public string ShortText { get; init; }         // 2-byte big-endian length prefix
    
    [StringLengthLE(4)]
    public string LongText { get; init; }          // 4-byte little-endian length prefix
    
    [StringNullTerminated]
    public string Path { get; init; }              // Null-terminated (C-style)
    
    [StringFixedLength(32)]
    public string FixedName { get; init; }         // Exactly 32 bytes (padded with nulls)
}
```

#### Length-from-Field Pattern

For protocols where the length is stored in a separate field:

```csharp
[BinaryParsable]
public readonly partial struct DynamicMessage
{
    public U16BE NameLength { get; init; }     // Length field FIRST
    
    [StringLengthFromField(nameof(NameLength))]
    public string Name { get; init; }              // Uses NameLength bytes
}
```

> **⚠️ Field Order Requirement:** The length field MUST be declared BEFORE the data field. The generator validates this at compile time.

#### Byte Array / Memory Length Attributes

Byte arrays and `Memory<byte>` types require length specification:

| Attribute | Description | Applies To |
|-----------|-------------|------------|
| `[BinaryFixedLength(n)]` | Fixed byte count | `byte[]` only |
| `[BytesLengthVarInt]` | VarInt-prefixed length | `byte[]`, `Memory<byte>`, `ReadOnlyMemory<byte>` |
| `[BytesLengthBE(n)]` | Big-endian length prefix | `byte[]`, `Memory<byte>`, `ReadOnlyMemory<byte>` |
| `[BytesLengthLE(n)]` | Little-endian length prefix | `byte[]`, `Memory<byte>`, `ReadOnlyMemory<byte>` |
| `[BytesLengthFromField(name)]` | Length from another field | `byte[]`, `Memory<byte>`, `ReadOnlyMemory<byte>` |

```csharp
[BinaryParsable]
public readonly partial struct DataPacket
{
    // Fixed-length byte array
    [BinaryFixedLength(6)]
    public byte[] MacAddress { get; init; }
    
    // Dynamic-length byte array with VarInt prefix
    [BytesLengthVarInt]
    public byte[] Payload { get; init; }
    
    // Memory with length from field
    public U32BE DataLength { get; init; }
    
    [BytesLengthFromField(nameof(DataLength))]
    public Memory<byte> Data { get; init; }
}
```

| Encoding | Description | Example Bytes for `[0xAA, 0xBB]` |
|----------|-------------|----------------------------------|
| `VarInt` | VarInt length prefix + data | `[0x02, 0xAA, 0xBB]` |
| `FixedBE` | N-byte big-endian length + data | `[0x00, 0x02, 0xAA, 0xBB]` (2-byte) |
| `FixedLE` | N-byte little-endian length + data | `[0x02, 0x00, 0xAA, 0xBB]` (2-byte) |
| `NullTerminated` | N/A for byte arrays | - |
| `Fixed` | Fixed byte count | `[0xAA, 0xBB, 0x00, ...]` |

#### Padding Bits

Use `[BinaryField(PaddingBits = n)]` to skip reserved bits **after** a field:

```csharp
[BinaryParsable]
public readonly partial struct FlagsPacket
{
    [BinaryField(BitCount = 4, PaddingBits = 4)] // 4 bits for Flags, skip 4 reserved bits after → byte-aligned!
    public byte Flags { get; init; }
    
    public U32BE Payload { get; init; }        // 4 bytes (requires byte alignment)
}
```

> **Note:** `PaddingBits` specifies bits to skip **after** reading the field, not before. This makes it intuitive: "read my field, then skip N reserved bits."

#### Attribute Rules and Constraints

| Rule | Behavior |
|------|----------|
| **`[BinaryOrder]` on one → all need it** | If any member uses `[BinaryOrder]`, ALL non-ignored members must specify order. Compile error otherwise. |
| **`[BinaryOrder]` + `[BinaryIgnore]` conflict** | Using both on the same member is an error - ignored members cannot have an order. |
| **Bit fields must end byte-aligned** | The total bit count must be a multiple of 8 before byte-aligned types. Use `PaddingBits` to align. |
| **Strings require length attribute** | All `string` members must have a string length attribute. Compile error otherwise. |
| **Bytes/Memory require length attribute** | Dynamic `byte[]`/`Memory<byte>` members require a length attribute. |
| **`FromField` order requirement** | Length fields must be declared BEFORE the data field using `[StringLengthFromField]` or `[BytesLengthFromField]`. |

> **Legacy Support:** The legacy `[BinaryStringLength(StringLengthEncoding.VarInt)]` attribute is still supported for backwards compatibility, but the new dedicated attributes are recommended for clarity.

#### Bit-Level Parsing Example

For protocols with non-byte-aligned fields (CAN, FlexRay, etc.):

```csharp
[BinaryParsable]
public readonly partial struct CANHeader
{
    [BinaryField(BitCount = 11)]
    public ushort Identifier { get; init; }     // 11 bits

    [BinaryField(BitCount = 1)]
    public byte RTR { get; init; }               // 1 bit

    [BinaryField(BitCount = 1)]
    public byte IDE { get; init; }               // 1 bit

    [BinaryField(BitCount = 1)]
    public byte Reserved { get; init; }          // 1 bit (padding/reserved)

    [BinaryField(BitCount = 4)]
    public byte DLC { get; init; }               // 4 bits
}
// Total: 18 bits → 3 bytes (rounded up)
// Note: Last 6 bits of byte 3 are unused (padding to byte boundary)
```

#### Mixing Bit Fields with Byte-Aligned Types

Use `PaddingBits` on the last bit field to align before byte-aligned types:

```csharp
[BinaryParsable]
public readonly partial struct MixedPacket
{
    [BinaryField(BitCount = 4, PaddingBits = 4)] // 4 bits + 4 padding after → byte-aligned!
    public byte Flags { get; init; }
    
    public U32BE Payload { get; init; }       // 4 bytes (requires byte alignment)
    
    [BinaryStringLength(StringLengthEncoding.VarInt)]
    public string Message { get; init; }         // Variable-length string
}
```

#### Primitive Integers with Default Endianness

Use `DefaultEndianness` in `[BinaryParsable]` to parse primitive integers:

```csharp
[BinaryParsable(DefaultEndianness = Endianness.BigEndian)]
public readonly partial struct NumericPacket
{
    public short Int16Value { get; init; }       // 2 bytes BE
    public int Int32Value { get; init; }         // 4 bytes BE
    public float SingleValue { get; init; }      // 4 bytes BE
    public double DoubleValue { get; init; }     // 8 bytes BE
}
```

**Supported primitive types:** `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`, `nint`, `nuint`, `Half`, `Int128`, `UInt128`

---

### BinaryParser - Manual Parsing

For fine-grained control over parsing.

```csharp
var parser = new BinaryParser(data);
ushort version = parser.ReadUInt16BE();
uint length = parser.ReadUInt32BE();
ReadOnlySpan<byte> payload = parser.ReadBytes((int)length);
```

#### BinaryParser Methods

| Category | Methods |
|----------|---------|
| **Big-Endian Integers** | `ReadInt16BE()`, `ReadInt32BE()`, `ReadInt64BE()`, `ReadUInt16BE()`, `ReadUInt32BE()`, `ReadUInt64BE()` |
| **Little-Endian Integers** | `ReadInt16LE()`, `ReadInt32LE()`, `ReadInt64LE()`, `ReadUInt16LE()`, `ReadUInt32LE()`, `ReadUInt64LE()` |
| **Floating Point** | `ReadF32BE()`, `ReadF32LE()`, `ReadF64BE()`, `ReadF64LE()` |
| **VarInt** | `ReadVarInt()`, `ReadVarIntZigZag()` |
| **Bytes** | `ReadByte()`, `ReadSByte()`, `ReadBytes(int)`, `Skip(int)` |
| **UTF-8 Strings** | `ReadUtf8Bytes(int)`, `ReadUtf8Var()`, `ReadUtf8FixedBE16()`, `ReadUtf8FixedBE32()`, `ReadUtf8Null()`, `ReadAsciiBytes(int)` |
| **Generic** | `Read<T>()`, `TryRead<T>(out T)` where `T : IBinaryParsable<T>` |
| **Arrays** | `ReadArray<T>(count, span)`, `ReadArrayVarInt<T>(span)`, `ReadArrayBE16<T>(span)`, `ReadArrayBE32<T>(span)` |

---

### BitReader - Bit-Level Parsing

For protocols with non-byte-aligned fields.

```csharp
var reader = new BitReader(data);

// Read individual bits
bool flag = reader.ReadBit1().Value != 0;
byte nibble = reader.ReadNibble().Value;      // 4 bits
byte priority = reader.ReadBit3().Value;      // 3 bits

// Variable-width integers
ulong value12 = reader.ReadBits(12);          // 12 bits
ulong value29 = reader.ReadBits(29);          // 29 bits

// Standard aligned types
ushort id = reader.ReadUInt16();
uint payload = reader.ReadUInt32();
```

#### BitReader Methods

| Method | Description |
|--------|-------------|
| `ReadBit1()` | Read 1 bit → `Bit1` |
| `ReadBit2()` | Read 2 bits → `Bit2` |
| `ReadBit3()` | Read 3 bits → `Bit3` |
| `ReadNibble()` | Read 4 bits → `Nibble` |
| `ReadBit5()` | Read 5 bits → `Bit5` |
| `ReadBit6()` | Read 6 bits → `Bit6` |
| `ReadBit7()` | Read 7 bits → `Bit7` |
| `ReadBits(int)` | Read N bits → `ulong` |
| `ReadUIntBits(byte)` | Read N bits → `UIntBits` |
| `ReadIntBits(byte)` | Read N bits (signed) → `IntBits` |
| `ReadByte()` | Read 8 bits → `byte` |
| `ReadUInt16()` | Read 16 bits → `ushort` |
| `ReadUInt32()` | Read 32 bits → `uint` |
| `ReadUInt64()` | Read 64 bits → `ulong` |
| `ReadInt16()` | Read 16 bits → `short` |
| `ReadInt32()` | Read 32 bits → `int` |
| `ReadInt64()` | Read 64 bits → `long` |
| `AlignToNextByte()` | Skip to next byte boundary |
| `SkipBits(int)` | Skip N bits |
| `ReadBytes(int)` | Read N bytes (must be byte-aligned) |

---

## Type Wrappers

### Endian Integer Wrappers

| Type | Size | Description |
|------|------|-------------|
| `U16BE` / `U16LE` | 2 bytes | Unsigned 16-bit |
| `U32BE` / `U32LE` | 4 bytes | Unsigned 32-bit |
| `U64BE` / `U64LE` | 8 bytes | Unsigned 64-bit |
| `U128BE` / `U128LE` | 16 bytes | Unsigned 128-bit |
| `I16BE` / `I16LE` | 2 bytes | Signed 16-bit |
| `I32BE` / `I32LE` | 4 bytes | Signed 32-bit |
| `I64BE` / `I64LE` | 8 bytes | Signed 64-bit |
| `I128BE` / `I128LE` | 16 bytes | Signed 128-bit |

### Floating Point Wrappers

| Big-Endian | Little-Endian | Size |
|------------|---------------|------|
| `F32BE` | `F32LE` | 4 bytes (float) |
| `F64BE` | `F64LE` | 8 bytes (double) |

### Variable-Length Wrappers

| Type | Description |
|------|-------------|
| `VarInt` | 7-bit encoded unsigned integer (1-10 bytes) |
| `VarIntZigZag` | ZigZag encoded signed integer (efficient for small absolute values) |

### String Encoding Wrappers

These wrappers implement both `IUtf8SpanFormattable` (for serialization) and `IBinaryParsable<T>` (for parsing), making them usable in both directions.

| Wrapper | Description | Serialization | Parsing |
|---------|-------------|---------------|---------|
| `Utf8` | Raw UTF-8 bytes | ✅ | ❌ |
| `Utf8Z` | Null-terminated UTF-8 | ✅ | ✅ |
| `Utf8Var` | VarInt length prefix + UTF-8 | ✅ | ✅ |
| `Utf8FixBE` | 4-byte BE length prefix + UTF-8 | ✅ | ✅ |
| `Utf8FixLE` | 4-byte LE length prefix + UTF-8 | ✅ | ✅ |
| `Utf8Fix16LE` | 2-byte LE length prefix + UTF-8 | ✅ | ✅ |
| `Ascii` | ASCII encoding (1 byte/char) | ✅ | ❌ |
| `Latin1` | ISO-8859-1 (1 byte/char) | ✅ | ❌ |

**Usage in `[BinaryParsable]` structs:**

```csharp
[BinaryParsable]
public readonly partial struct MessagePacket
{
    public U16BE Id { get; init; }
    public Utf8Var Name { get; init; }      // Wrapper type, no attribute needed!
    public Utf8FixBE Payload { get; init; } // 4-byte BE length prefix
}
```

### Bit Types

| Type | Bits | Range |
|------|------|-------|
| `Bit1` | 1 | 0-1 |
| `Bit2` | 2 | 0-3 |
| `Bit3` | 3 | 0-7 |
| `Nibble` | 4 | 0-15 |
| `Bit5` | 5 | 0-31 |
| `Bit6` | 6 | 0-63 |
| `Bit7` | 7 | 0-127 |
| `UIntBits` | 1-64 | variable |
| `IntBits` | 1-64 | signed |

### Raw Bytes

| Type | Description |
|------|-------------|
| `Raw` | Wraps `byte[]` or `ReadOnlySpan<byte>` for direct inclusion |

---

## Custom Types

### IBinarySerializable

Implement for custom binary serialization:

```csharp
public readonly struct NetworkPacket : IBinarySerializable
{
    public ushort Version { get; init; }
    public string Payload { get; init; }

    public bool TryGetSerializedSize(out int size)
    {
        size = 2 + Encoding.UTF8.GetByteCount(Payload);
        return true;
    }

    public bool TryWrite(Span<byte> destination, out int bytesWritten)
    {
        var builder = new SpanBytesBuilder(destination);
        builder.Append(new U16BE(Version));
        builder.AppendUtf8(Payload);
        bytesWritten = builder.Length;
        return true;
    }
}
```

### IStringSize

Implement alongside `ISpanFormattable` to provide size hints:

```csharp
public readonly struct IpAddress : IStringSize, ISpanFormattable
{
    public uint Value { get; init; }

    public bool TryGetStringSize(ReadOnlySpan<char> format, IFormatProvider? provider, out int size)
    { size = 15; return true; } // "255.255.255.255"

    public bool TryFormat(Span<char> dest, out int written, ReadOnlySpan<char> format, IFormatProvider? provider)
    { /* ... */ }
}
```

### IBinaryParsable&lt;T&gt;

Implement for custom parsing:

```csharp
public readonly struct CustomPacket : IBinaryParsable<CustomPacket>
{
    public ushort Version { get; init; }
    
    public static bool TryGetSerializedSize(out int size)
    {
        size = 2;
        return true; // Fixed size of 2 bytes
    }

    public static bool TryParse(ReadOnlySpan<byte> source, out CustomPacket value, out int bytesConsumed)
    {
        if (source.Length < 2) { value = default; bytesConsumed = 0; return false; }
        value = new CustomPacket { Version = BinaryPrimitives.ReadUInt16BigEndian(source) };
        bytesConsumed = 2;
        return true;
    }
}
```

---

## Configuration

Configure via MSBuild properties in your `.csproj`:

```xml
<PropertyGroup>
  <!-- Initial buffer size (default: 2 MiB = 2097152) -->
  <ZeroAlloc_DefaultBufferSize>4194304</ZeroAlloc_DefaultBufferSize>
  
  <!-- Heap fallback on nested/recursive calls (default: true) -->
  <ZeroAlloc_RecursiveHeapFallback>true</ZeroAlloc_RecursiveHeapFallback>
  
  <!-- Buffer overflow behavior: Grow, HeapFallback, or Throw (default: Grow) -->
  <ZeroAlloc_BufferOverflowBehavior>Grow</ZeroAlloc_BufferOverflowBehavior>
</PropertyGroup>
```

| Property | Default | Description |
|----------|---------|-------------|
| `ZeroAlloc_DefaultBufferSize` | `2097152` | Initial ThreadStatic buffer size in bytes/chars |
| `ZeroAlloc_RecursiveHeapFallback` | `true` | Heap fallback on nested calls; `false` throws exception |
| `ZeroAlloc_BufferOverflowBehavior` | `Grow` | `Grow`=grow buffer, `HeapFallback`=heap alloc, `Throw`=exception |

### Runtime Buffer Management

```csharp
// Get current buffer sizes
int charSize = ZeroAllocHelper.GetCharBufferSize();
int byteSize = ZeroAllocHelper.GetByteBufferSize();

// Resize buffers (must not be in use)
ZeroAllocHelper.ResizeCharBuffer(4_194_304);  // 4 MiB
ZeroAllocHelper.ResizeByteBuffer(1_048_576);  // 1 MiB

// Release buffers to free memory
ZeroAllocHelper.ReleaseBuffers();
```

---

## Installation

```bash
dotnet add package ZeroAlloc
dotnet add package ZeroAlloc.Generator
```

Or in `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="ZeroAlloc" Version="1.0.0" />
  <PackageReference Include="ZeroAlloc.Generator" Version="1.0.0" 
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

### Per-Assembly Setup

Each assembly must define its own `ZeroAllocBase` subclass:

```csharp
// In your project (must be internal partial!)
using ZeroAlloc;
internal partial class ZA : ZeroAllocBase { }
```

**Requirements:**
- .NET 10.0 or later
- C# 13.0 or later

---

## License

MIT License - See [LICENSE](LICENSE) file for details.
