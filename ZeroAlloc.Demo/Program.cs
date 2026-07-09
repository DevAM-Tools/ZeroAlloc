// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Demo;

// ============================================================================
// ZeroAlloc User-Defined Allocation Helper
// ============================================================================
// Step 1: Define a partial class that inherits from ZeroAllocBase.
//         The class MUST be internal (not public) to limit usage to this assembly.
//         The source generator will add optimized static methods to this class.
// ============================================================================
internal sealed partial class ZA : ZeroAllocBase { }

// ============================================================================
// Demo Types for Generated Code Showcase
// ============================================================================

/// <summary>
/// Example: <see cref="ISpanFormattable"/> only (standard .NET interface).
/// The generator calls <see cref="ISpanFormattable.TryFormat"/> directly without a size pre-check.
/// </summary>
public readonly struct Temperature : ISpanFormattable
{
    /// <summary>Gets the temperature value in degrees Celsius.</summary>
    public double Celsius { get; init; }

    /// <inheritdoc />
    public bool TryFormat(Span<char> destination, out int charsWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return Celsius.TryFormat(destination, out charsWritten, format, provider);
    }

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
        => Celsius.ToString(format, formatProvider);
}

/// <summary>
/// Example: <see cref="ISpanFormattable"/> + <see cref="IStringSize"/> with a known formatted size.
/// The generator can pre-check the buffer via <see cref="IStringSize.TryGetStringSize"/>.
/// </summary>
public readonly struct IpAddress : ISpanFormattable, IStringSize
{
    /// <summary>Gets the first octet of the IPv4 address.</summary>
    public byte A { get; init; }

    /// <summary>Gets the second octet of the IPv4 address.</summary>
    public byte B { get; init; }

    /// <summary>Gets the third octet of the IPv4 address.</summary>
    public byte C { get; init; }

    /// <summary>Gets the fourth octet of the IPv4 address.</summary>
    public byte D { get; init; }

    /// <inheritdoc />
    public bool TryGetStringSize(ReadOnlySpan<char> format, IFormatProvider? provider, out int size)
    {
        // Upper bound for "255.255.255.255" = 15 chars
        size = 15;
        return true;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
        => $"{A}.{B}.{C}.{D}";
}

/// <summary>
/// Example: <see cref="ISpanFormattable"/> + <see cref="IStringSize"/> with unknown size.
/// <see cref="IStringSize.TryGetStringSize"/> returns <c>false</c>, so the generator formats without pre-check.
/// </summary>
public readonly struct DynamicMessage : ISpanFormattable, IStringSize
{
    /// <summary>Gets the message text, or <c>null</c> for an empty formatted result.</summary>
    public string? Text { get; init; }

    /// <inheritdoc />
    public bool TryGetStringSize(ReadOnlySpan<char> format, IFormatProvider? provider, out int size)
    {
        size = 0;
        return false;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
        => Text
        ?? string.Empty;
}

/// <summary>
/// Example: <see cref="IUtf8SpanFormattable"/> + <see cref="IUtf8Size"/> with a known UTF-8 size.
/// The generator can pre-check the buffer via <see cref="IUtf8Size.TryGetUtf8Size"/>.
/// </summary>
public readonly struct StatusCode : IUtf8SpanFormattable, IUtf8Size
{
    /// <summary>Gets the numeric HTTP status code (for example, 200 or 404).</summary>
    public int Code { get; init; }

    /// <inheritdoc />
    public bool TryGetUtf8Size(ReadOnlySpan<char> format, IFormatProvider? provider, out int size)
    {
        // HTTP status codes 100-599 are always 3 decimal digits in UTF-8
        size = 3;
        return true;
    }

    /// <inheritdoc />
    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        return Code.TryFormat(utf8Destination, out bytesWritten, format, provider);
    }

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
        => Code.ToString(format, formatProvider);
}

/// <summary>
/// Example: <see cref="IBinarySerializable"/> with a fixed serialized size.
/// </summary>
public readonly struct PacketHeader : IBinarySerializable
{
    /// <summary>Gets the packet type field (big-endian <see cref="ushort"/> on the wire).</summary>
    public ushort Type { get; init; }

    /// <summary>Gets the payload length field (big-endian <see cref="uint"/> on the wire).</summary>
    public uint Length { get; init; }

    /// <summary>Fixed on-wire size in bytes: 2-byte type + 4-byte length.</summary>
    public const int Size = 6;

    /// <inheritdoc />
    public bool TryGetWrittenSize(out int size)
    {
        size = Size;
        return true;
    }

    /// <inheritdoc />
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
/// Example: <see cref="IBinarySerializable"/> with unknown serialized size.
/// <see cref="IBinarySerializable.TryGetWrittenSize"/> returns <c>false</c> because the payload length is dynamic.
/// </summary>
public readonly struct DynamicPacket : IBinarySerializable
{
    /// <summary>Gets the variable-length payload written after a 4-byte big-endian length prefix.</summary>
    public byte[] Payload { get; init; }

    /// <inheritdoc />
    public bool TryGetWrittenSize(out int size)
    {
        size = 0;
        return false;
    }

    /// <inheritdoc />
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
/// Example: <see cref="IBinarySerializable"/> with a computable serialized size.
/// </summary>
public readonly struct ComputedSizePacket : IBinarySerializable
{
    /// <summary>Gets the data bytes written after a 4-byte big-endian length prefix.</summary>
    public byte[] Data { get; init; }

    /// <inheritdoc />
    public bool TryGetWrittenSize(out int size)
    {
        size = 4 + Data.Length;
        return true;
    }

    /// <inheritdoc />
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
/// Console demo for ZeroAlloc string, UTF-8, and binary formatting APIs.
/// </summary>
internal sealed class Program
{
    /// <summary>Runs all demo scenarios and prints results to the console.</summary>
    public static void Main()
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
        Temperature temp1 = new Temperature { Celsius = 23.5 };
        using (TempString ts = ZA.String("Temperature: ", temp1, "°C"))
        {
            Console.WriteLine($"  Result: {ts.AsSpan().ToString()}");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 10: ISpanFormattable + IStringSize with known size
        // ====================================================================
        Console.WriteLine("Test 10: ISpanFormattable + IStringSize (IpAddress)");
        // Generator can pre-check buffer via TryGetStringSize (returns 15)
        IpAddress ip = new IpAddress { A = 192, B = 168, C = 1, D = 100 };
        using (TempString ts = ZA.String("Server: ", ip))
        {
            Console.WriteLine($"  Result: {ts.AsSpan().ToString()}");
            Console.WriteLine($"  IpAddress.TryGetStringSize() = 15 (known size)");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 11: ISpanFormattable + IStringSize with unknown size
        // ====================================================================
        Console.WriteLine("Test 11: IStringSize unknown size (DynamicMessage)");
        // Generator cannot pre-check: TryGetStringSize returns false
        DynamicMessage msg = new DynamicMessage { Text = "Hello, World!" };
        using (TempString ts = ZA.String("Message: ", msg))
        {
            Console.WriteLine($"  Result: {ts.AsSpan().ToString()}");
            Console.WriteLine($"  DynamicMessage.TryGetStringSize() = false (unknown size)");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 12: IUtf8SpanFormattable + IUtf8Size with known size
        // ====================================================================
        Console.WriteLine("Test 12: IUtf8SpanFormattable + IUtf8Size (StatusCode)");
        // Generator can pre-check UTF-8 buffer via TryGetUtf8Size (returns 3)
        StatusCode status = new StatusCode { Code = 200 };
        using (TempBytes tb = ZA.Utf8("HTTP ", status, " OK"))
        {
            Console.WriteLine($"  Result: {System.Text.Encoding.UTF8.GetString(tb.AsSpan())}");
            Console.WriteLine($"  StatusCode.TryGetUtf8Size() = 3 (known size)");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 13: IBinarySerializable with known fixed size
        // ====================================================================
        Console.WriteLine("Test 13: IBinarySerializable with fixed size (PacketHeader)");
        PacketHeader header = new PacketHeader { Type = 0x0800, Length = 1500 };
        using (TempBytes tb = ZA.Bytes(header))
        {
            ReadOnlySpan<byte> bytes = tb.AsSpan();
            Console.Write("  Bytes: ");
            foreach (byte b in bytes)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine();
            Console.WriteLine($"  PacketHeader.TryGetWrittenSize() = 6 (fixed size)");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 14: IBinarySerializable with unknown size
        // ====================================================================
        Console.WriteLine("Test 14: IBinarySerializable unknown size (DynamicPacket)");
        DynamicPacket dynPacket = new DynamicPacket { Payload = [0xCA, 0xFE, 0xBA, 0xBE] };
        using (TempBytes tb = ZA.Bytes(dynPacket))
        {
            ReadOnlySpan<byte> bytes = tb.AsSpan();
            Console.Write("  Bytes: ");
            foreach (byte b in bytes)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine();
            Console.WriteLine($"  DynamicPacket.TryGetWrittenSize() = false (unknown size)");
        }

        Console.WriteLine();

        // ====================================================================
        // Test 15: IBinarySerializable with computable size
        // ====================================================================
        Console.WriteLine("Test 15: IBinarySerializable with computable size (ComputedSizePacket)");
        ComputedSizePacket computed = new ComputedSizePacket { Data = [0x01, 0x02, 0x03] };
        using (TempBytes tb = ZA.Bytes(computed))
        {
            ReadOnlySpan<byte> bytes = tb.AsSpan();
            Console.Write("  Bytes: ");
            foreach (byte b in bytes)
            {
                Console.Write($"{b:X2} ");
            }
            Console.WriteLine();
            computed.TryGetWrittenSize(out int computedSize);
            Console.WriteLine($"  ComputedSizePacket.TryGetWrittenSize() = {computedSize} (computable)");
        }

        Console.WriteLine();
        Console.WriteLine("=== Demo completed successfully! ===");
        Console.WriteLine();
        Console.WriteLine("Inspect the generated code in:");
        Console.WriteLine("  ZeroAlloc.Demo/obj/Generated/ZeroAlloc.Generator/ZeroAlloc.Generator.ZeroAllocGenerator/ZA.ZeroAlloc.g.cs");
    }
}
