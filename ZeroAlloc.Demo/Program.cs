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

using System;
using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;
using ZeroAlloc;

namespace ZeroAlloc.Demo;

// ============================================================================
// ZeroAlloc User-Defined Allocation Helper
// ============================================================================
// Step 1: Define a partial class that inherits from ZeroAllocBase.
//         The class MUST be internal (not public) to limit usage to this assembly.
//         The source generator will add optimized static methods to this class.
// ============================================================================
internal partial class ZA : ZeroAllocBase { }

// ============================================================================
// Demo Types for Generated Code Showcase
// ============================================================================

/// <summary>
/// Example: ISpanFormattable only (standard .NET interface).
/// The generator will call TryFormat directly.
/// </summary>
public readonly struct Temperature : ISpanFormattable
{
    public double Celsius { get; init; }

    public bool TryFormat(Span<char> destination, out int charsWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return Celsius.TryFormat(destination, out charsWritten, format, provider);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
        => Celsius.ToString(format, formatProvider);
}

/// <summary>
/// Example: ISpanFormattable + IStringSize with known size.
/// The generator can pre-check the buffer size before calling TryFormat.
/// </summary>
public readonly struct IpAddress : ISpanFormattable, IStringSize
{
    public byte A { get; init; }
    public byte B { get; init; }
    public byte C { get; init; }
    public byte D { get; init; }

    // Max: "255.255.255.255" = 15 chars
    public bool TryGetStringSize(ReadOnlySpan<char> format, IFormatProvider? provider, out int size)
    { size = 15; return true; }

    public bool TryFormat(Span<char> destination, out int charsWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        Span<char> buffer = stackalloc char[15];
        int pos = 0;
        A.TryFormat(buffer[pos..], out int written, default, provider); pos += written;
        buffer[pos++] = '.';
        B.TryFormat(buffer[pos..], out written, default, provider); pos += written;
        buffer[pos++] = '.';
        C.TryFormat(buffer[pos..], out written, default, provider); pos += written;
        buffer[pos++] = '.';
        D.TryFormat(buffer[pos..], out written, default, provider); pos += written;

        if (destination.Length < pos)
        {
            charsWritten = 0;
            return false;
        }
        buffer[..pos].CopyTo(destination);
        charsWritten = pos;
        return true;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
        => $"{A}.{B}.{C}.{D}";
}

/// <summary>
/// Example: ISpanFormattable + IStringSize returning null (unknown size).
/// The generator must use TryFormat without pre-check.
/// </summary>
public readonly struct DynamicMessage : ISpanFormattable, IStringSize
{
    public string? Text { get; init; }

    // Size is unknown at compile time
    public bool TryGetStringSize(ReadOnlySpan<char> format, IFormatProvider? provider, out int size)
    { size = 0; return false; }

    public bool TryFormat(Span<char> destination, out int charsWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        if (Text is null)
        {
            charsWritten = 0;
            return true;
        }
        if (destination.Length < Text.Length)
        {
            charsWritten = 0;
            return false;
        }
        Text.AsSpan().CopyTo(destination);
        charsWritten = Text.Length;
        return true;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
        => Text ?? string.Empty;
}

/// <summary>
/// Example: IUtf8SpanFormattable + IUtf8Size with known size.
/// The generator can pre-check the UTF-8 buffer size.
/// </summary>
public readonly struct StatusCode : IUtf8SpanFormattable, IUtf8Size
{
    public int Code { get; init; }

    // HTTP status codes: 100-599, always 3 digits
    public bool TryGetUtf8Size(ReadOnlySpan<char> format, IFormatProvider? provider, out int size)
    { size = 3; return true; }

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return Code.TryFormat(utf8Destination, out bytesWritten, format, provider);
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
        => Code.ToString(format, formatProvider);
}

/// <summary>
/// Example: IBinarySerializable with known size.
/// Simple fixed-size binary structure.
/// </summary>
public readonly struct PacketHeader : IBinarySerializable
{
    public ushort Type { get; init; }
    public uint Length { get; init; }

    public const int Size = 6;

    public bool TryGetSerializedSize(out int size) { size = Size; return true; }

    public bool TryWrite(Span<byte> destination, out int bytesWritten)
    {
        if (destination.Length < Size) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt16BigEndian(destination, Type);
        BinaryPrimitives.WriteUInt32BigEndian(destination[2..], Length);
        bytesWritten = Size;
        return true;
    }
}

/// <summary>
/// Example: IBinarySerializable with dynamic size (returns null).
/// Size depends on payload content.
/// </summary>
public readonly struct DynamicPacket : IBinarySerializable
{
    public byte[] Payload { get; init; }

    // Size is dynamic, return false
    public bool TryGetSerializedSize(out int size) { size = 0; return false; }

    public bool TryWrite(Span<byte> destination, out int bytesWritten)
    {
        int needed = 4 + Payload.Length;
        if (destination.Length < needed) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)Payload.Length);
        Payload.AsSpan().CopyTo(destination[4..]);
        bytesWritten = needed;
        return true;
    }
}

/// <summary>
/// Example: IBinarySerializable with computable size.
/// Size can be determined but requires calculation.
/// </summary>
public readonly struct ComputedSizePacket : IBinarySerializable
{
    public byte[] Data { get; init; }

    // Size can be computed: 4 bytes header + data length
    public bool TryGetSerializedSize(out int size) { size = 4 + Data.Length; return true; }

    public bool TryWrite(Span<byte> destination, out int bytesWritten)
    {
        int needed = 4 + Data.Length;
        if (destination.Length < needed) { bytesWritten = 0; return false; }
        BinaryPrimitives.WriteUInt32BigEndian(destination, (uint)Data.Length);
        Data.AsSpan().CopyTo(destination[4..]);
        bytesWritten = needed;
        return true;
    }
}

/// <summary>
/// Demonstrates the ZeroAlloc library's zero-allocation string formatting.
/// </summary>
class Program
{
    static void Main()
    {
        Console.WriteLine("=== ZeroAlloc Demo - Radically Simplified ===");
        Console.WriteLine();

        // ====================================================================
        // Test 1: TempString - Zero-allocation short-lived string
        // ====================================================================
        Console.WriteLine("Test 1: TempString (zero heap allocation)");

        int userId = 12345;
        using (TempString temp = ZA.String("User ", userId, " logged in"))
        {
            // Access the content as a span - no heap allocation!
            ReadOnlySpan<char> span = temp.AsSpan();
            Console.WriteLine($"  Result: {span.ToString()}");
            Console.WriteLine($"  Length: {temp.Length}");
        } // Buffer automatically released back to ThreadStatic storage

        Console.WriteLine();

        // ====================================================================
        // Test 2: TempBytes - Zero-allocation UTF-8 encoding
        // ====================================================================
        Console.WriteLine("Test 2: TempBytes (zero heap allocation UTF-8)");

        using (TempBytes temp = ZA.Utf8("Count: ", 42))
        {
            ReadOnlySpan<byte> utf8Span = temp.AsSpan();
            Console.WriteLine($"  UTF-8 bytes: {utf8Span.Length} bytes");
            Console.WriteLine($"  Content: {System.Text.Encoding.UTF8.GetString(utf8Span)}");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 3: Converting to actual String (when needed)
        // ====================================================================
        Console.WriteLine("Test 3: ToString() for heap allocation when needed");

        string actualString;
        using (TempString temp = ZA.String("Hello ", "World"))
        {
            // Only allocate when we actually need a persistent string
            actualString = temp.ToString();
        }
        Console.WriteLine($"  Heap-allocated string: \"{actualString}\"");

        Console.WriteLine();

        // ====================================================================
        // Test 4: Using Hex formatters
        // ====================================================================
        Console.WriteLine("Test 4: Hex formatting with ISpanFormattable");

        using (TempString temp = ZA.String("MAC: ", new Hex2(0xDE), ":", new Hex2(0xAD)))
        {
            Console.WriteLine($"  Result: {temp.AsSpan().ToString()}");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 5: Demonstrate graceful nested call handling
        // ====================================================================
        Console.WriteLine("Test 5: Graceful nested call handling");

        using (TempString outer = ZA.String("Outer: ", 100))
        {
            // Nested call uses heap allocation instead of throwing exception
            using (TempString inner = ZA.String("Inner: ", 200))
            {
                Console.WriteLine($"  Outer (ThreadStatic): {outer.AsSpan().ToString()}, IsHeapAllocated={outer.IsHeapAllocated}");
                Console.WriteLine($"  Inner (Heap fallback): {inner.AsSpan().ToString()}, IsHeapAllocated={inner.IsHeapAllocated}");
            }
        }

        Console.WriteLine();

        // ====================================================================
        // Test 6: TryString for safe formatting
        // ====================================================================
        Console.WriteLine("Test 6: TryString for safe formatting");

        // Use the same signature as Test 1 (string, int, string)
        if (ZA.TryString(out TempString result, "Safe: ", 999, " done"))
        {
            using (result)
            {
                Console.WriteLine($"  Success: {result.AsSpan().ToString()}");
            }
        }

        Console.WriteLine();

        // ====================================================================
        // Test 7: CultureInfo support for locale-specific formatting
        // ====================================================================
        Console.WriteLine("Test 7: CultureInfo support");

        double price = 1234567.89;

        // German formatting (uses comma as decimal separator, dot as thousands)
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        using (TempString temp = ZA.String(german, "Preis: ", price, " EUR"))
        {
            Console.WriteLine($"  German: {temp.AsSpan().ToString()}");
        }

        // US formatting (uses dot as decimal separator, comma as thousands)  
        CultureInfo us = CultureInfo.GetCultureInfo("en-US");
        using (TempString temp = ZA.String(us, "Price: ", price, " USD"))
        {
            Console.WriteLine($"  US: {temp.AsSpan().ToString()}");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 8: Endianness wrappers for binary data
        // ====================================================================
        Console.WriteLine("Test 8: Endianness wrappers for binary data");

        using (TempBytes temp = ZA.Bytes(new U16BE(0x1234), new U32LE(0xDEADBEEF)))
        {
            ReadOnlySpan<byte> bytes = temp.AsSpan();
            Console.Write("  Bytes: ");
            foreach (byte b in bytes)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine();
            Console.WriteLine($"  Length: {bytes.Length} bytes");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 9: ISpanFormattable only (no size hint)
        // ====================================================================
        Console.WriteLine("Test 9: ISpanFormattable only (Temperature)");
        // Generator uses TryFormat directly without pre-check
        var temp1 = new Temperature { Celsius = 23.5 };
        using (TempString ts = ZA.String("Temperature: ", temp1, "°C"))
        {
            Console.WriteLine($"  Result: {ts.AsSpan().ToString()}");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 10: ISpanFormattable + IStringSize with known size
        // ====================================================================
        Console.WriteLine("Test 10: ISpanFormattable + IStringSize (IpAddress)");
        // Generator can pre-check buffer: GetCharCount() returns 15
        var ip = new IpAddress { A = 192, B = 168, C = 1, D = 100 };
        using (TempString ts = ZA.String("Server: ", ip))
        {
            Console.WriteLine($"  Result: {ts.AsSpan().ToString()}");
            Console.WriteLine($"  IpAddress.GetCharCount() = 15 (known size)");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 11: ISpanFormattable + IStringSize returning null
        // ====================================================================
        Console.WriteLine("Test 11: IStringSize returning null (DynamicMessage)");
        // Generator cannot pre-check: GetCharCount() returns null
        var msg = new DynamicMessage { Text = "Hello, World!" };
        using (TempString ts = ZA.String("Message: ", msg))
        {
            Console.WriteLine($"  Result: {ts.AsSpan().ToString()}");
            Console.WriteLine($"  DynamicMessage.GetCharCount() = null (unknown size)");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 12: IUtf8SpanFormattable + IUtf8Size with known size
        // ====================================================================
        Console.WriteLine("Test 12: IUtf8SpanFormattable + IUtf8Size (StatusCode)");
        // Generator can pre-check UTF-8 buffer: GetUtf8ByteCount() returns 3
        var status = new StatusCode { Code = 200 };
        using (TempBytes tb = ZA.Utf8("HTTP ", status, " OK"))
        {
            Console.WriteLine($"  Result: {System.Text.Encoding.UTF8.GetString(tb.AsSpan())}");
            Console.WriteLine($"  StatusCode.GetUtf8ByteCount() = 3 (known size)");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 13: IBinarySerializable with known fixed size
        // ====================================================================
        Console.WriteLine("Test 13: IBinarySerializable with fixed size (PacketHeader)");
        var header = new PacketHeader { Type = 0x0800, Length = 1500 };
        using (TempBytes tb = ZA.Bytes(header))
        {
            ReadOnlySpan<byte> bytes = tb.AsSpan();
            Console.Write("  Bytes: ");
            foreach (byte b in bytes)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine();
            Console.WriteLine($"  PacketHeader.TryGetSerializedSize() = 6 (fixed size)");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 14: IBinarySerializable returning null (unknown size)
        // ====================================================================
        Console.WriteLine("Test 14: IBinarySerializable returning null (DynamicPacket)");
        var dynPacket = new DynamicPacket { Payload = new byte[] { 0xCA, 0xFE, 0xBA, 0xBE } };
        using (TempBytes tb = ZA.Bytes(dynPacket))
        {
            ReadOnlySpan<byte> bytes = tb.AsSpan();
            Console.Write("  Bytes: ");
            foreach (byte b in bytes)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine();
            Console.WriteLine($"  DynamicPacket.TryGetSerializedSize() = false (unknown size)");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 15: IBinarySerializable with computable size
        // ====================================================================
        Console.WriteLine("Test 15: IBinarySerializable with computable size (ComputedSizePacket)");
        var computed = new ComputedSizePacket { Data = new byte[] { 0x01, 0x02, 0x03 } };
        using (TempBytes tb = ZA.Bytes(computed))
        {
            ReadOnlySpan<byte> bytes = tb.AsSpan();
            Console.Write("  Bytes: ");
            foreach (byte b in bytes)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine();
            computed.TryGetSerializedSize(out int computedSize);
            Console.WriteLine($"  ComputedSizePacket.TryGetSerializedSize() = {computedSize} (computable)");
        }

        Console.WriteLine();
        Console.WriteLine("=== Demo completed successfully! ===");
        Console.WriteLine();
        Console.WriteLine("Inspect the generated code in:");
        Console.WriteLine("  ZeroAlloc.Demo/obj/Generated/ZeroAlloc.Generator/ZA.g.cs");
    }
}
