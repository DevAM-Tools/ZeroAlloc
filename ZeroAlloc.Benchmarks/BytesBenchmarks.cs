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

namespace ZeroAlloc.Benchmarks;

/// <summary>
/// Zero-allocation API for the benchmarks assembly.
/// The generator automatically creates optimized overloads.
/// </summary>
internal partial class Z : ZeroAllocBase { }

/// <summary>
/// Comprehensive benchmarks comparing ZeroAlloc.Bytes for binary serialization against:
/// - Manual BinaryPrimitives serialization
/// - Struct-based serialization
/// 
/// Binary serialization is critical for:
/// - Network packet construction (UDP, TCP)
/// - Protocol implementations (Ethernet, IPv4, IPv6)
/// - Binary file formats
/// - High-frequency trading systems
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class BytesBenchmarks
{
    // === Test Data ===
    private static readonly byte[] SrcMac = [0x00, 0x1A, 0x2B, 0x3C, 0x4D, 0x5E];
    private static readonly byte[] DstMac = [0x00, 0x5E, 0x00, 0x01, 0x02, 0x03];
    private static readonly UInt128 SrcIPv6 = UInt128.Parse("1");  // ::1
    private static readonly UInt128 DstIPv6 = UInt128.Parse("281473913978881");  // ::ffff:192.0.2.1
    private static readonly byte[] Payload = "Hello, UDP World!"u8.ToArray();

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 1: Simple UDP Header (8 bytes)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("SimpleUDP"), Benchmark(Baseline = true)]
    public byte[] SimpleUDP_Manual()
    {
        // UDP header: SrcPort(2) + DstPort(2) + Length(2) + Checksum(2) = 8 bytes
        byte[] result = new byte[8 + Payload.Length];
        int offset = 0;

        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 8080);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 53);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), (ushort)(8 + Payload.Length));
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 0);
        offset += 2;
        Payload.CopyTo(result, offset);

        return result;
    }

    [BenchmarkCategory("SimpleUDP"), Benchmark]
    public byte[] SimpleUDP_ZeroAlloc()
    {
        return Z.Bytes(
            new U16BE(8080),      // Source port
            new U16BE(53),        // Destination port
            new U16BE((ushort)(8 + Payload.Length)), // Length
            new U16BE(0),         // Checksum
            Payload).ToArray();
    }

    [BenchmarkCategory("SimpleUDP"), Benchmark]
    public byte[] SimpleUDP_ZeroAlloc_Struct()
    {
        var udp = new UdpHeader
        {
            SourcePort = new U16BE(8080),
            DestinationPort = new U16BE(53),
            Length = new U16BE((ushort)(8 + Payload.Length)),
            Checksum = new U16BE(0)
        };
        return Z.Bytes(udp, Payload).ToArray();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 2: Full Network Stack (Ethernet + IPv6 + UDP)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("FullPacket"), Benchmark(Baseline = true)]
    public byte[] FullPacket_Manual()
    {
        // Ethernet(14) + IPv6(40) + UDP(8) + Payload
        int totalSize = 14 + 40 + 8 + Payload.Length;
        byte[] result = new byte[totalSize];
        int offset = 0;

        // Ethernet header
        DstMac.CopyTo(result, offset); offset += 6;
        SrcMac.CopyTo(result, offset); offset += 6;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 0x86DD);
        offset += 2;

        // IPv6 header
        uint vtf = (6U << 28) | (0U << 20) | 0U;
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(offset), vtf);
        offset += 4;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), (ushort)(8 + Payload.Length));
        offset += 2;
        result[offset++] = 17; // UDP
        result[offset++] = 64; // Hop limit
        BinaryPrimitives.WriteUInt128BigEndian(result.AsSpan(offset), SrcIPv6);
        offset += 16;
        BinaryPrimitives.WriteUInt128BigEndian(result.AsSpan(offset), DstIPv6);
        offset += 16;

        // UDP header
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 8080);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 53);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), (ushort)(8 + Payload.Length));
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 0);
        offset += 2;

        Payload.CopyTo(result, offset);
        return result;
    }

    [BenchmarkCategory("FullPacket"), Benchmark]
    public byte[] FullPacket_ZeroAlloc_Inline()
    {
        return Z.Bytes(
            // Ethernet
            DstMac, SrcMac, new U16BE(0x86DD),
            // IPv6
            new U32BE((6U << 28) | (0U << 20) | 0U),
            new U16BE((ushort)(8 + Payload.Length)),
            (byte)17, (byte)64,
            new U128BE(SrcIPv6),
            new U128BE(DstIPv6),
            // UDP
            new U16BE(8080),
            new U16BE(53),
            new U16BE((ushort)(8 + Payload.Length)),
            new U16BE(0),
            Payload).ToArray();
    }

    [BenchmarkCategory("FullPacket"), Benchmark]
    public byte[] FullPacket_ZeroAlloc_Structs()
    {
        var packet = new UdpIPv6Packet
        {
            Ethernet = new EthernetHeader
            {
                DestinationMac = DstMac,
                SourceMac = SrcMac,
                EtherType = new U16BE(0x86DD)
            },
            IPv6 = IPv6Header.Create(
                trafficClass: 0,
                flowLabel: 0,
                payloadLength: (ushort)(8 + Payload.Length),
                nextHeader: 17,
                hopLimit: 64,
                sourceAddress: SrcIPv6,
                destAddress: DstIPv6),
            Udp = new UdpHeader
            {
                SourcePort = new U16BE(8080),
                DestinationPort = new U16BE(53),
                Length = new U16BE((ushort)(8 + Payload.Length)),
                Checksum = new U16BE(0)
            },
            Payload = Payload
        };
        return Z.Bytes(packet).ToArray();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 3: TryBytes (Stack-allocated destination)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("TryBytes"), Benchmark(Baseline = true)]
    public int TryBytes_Manual()
    {
        Span<byte> buffer = stackalloc byte[128];
        int offset = 0;

        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], 8080);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], 53);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)(8 + Payload.Length));
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], 0);
        offset += 2;
        Payload.CopyTo(buffer[offset..]);
        offset += Payload.Length;

        return offset;
    }

    [BenchmarkCategory("TryBytes"), Benchmark]
    public int TryBytes_ZeroAlloc()
    {
        Span<byte> buffer = stackalloc byte[128];
        Z.TryBytes(buffer, out int written,
            new U16BE(8080),
            new U16BE(53),
            new U16BE((ushort)(8 + Payload.Length)),
            new U16BE(0),
            Payload);
        return written;
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 4: Batch Serialization (100 packets)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("Batch"), Benchmark(Baseline = true)]
    public int Batch_Manual()
    {
        int totalSize = 0;
        for (int i = 0; i < 100; i++)
        {
            byte[] packet = new byte[8 + Payload.Length];
            int offset = 0;

            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(offset), (ushort)(8080 + i));
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(offset), 53);
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(offset), (ushort)(8 + Payload.Length));
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(offset), 0);
            offset += 2;
            Payload.CopyTo(packet, offset);

            totalSize += packet.Length;
        }
        return totalSize;
    }

    [BenchmarkCategory("Batch"), Benchmark]
    public int Batch_ZeroAlloc()
    {
        int totalSize = 0;
        for (int i = 0; i < 100; i++)
        {
            byte[] packet = Z.Bytes(
                new U16BE((ushort)(8080 + i)),
                new U16BE(53),
                new U16BE((ushort)(8 + Payload.Length)),
                new U16BE(0),
                Payload).ToArray();
            totalSize += packet.Length;
        }
        return totalSize;
    }

    #endregion
}

/// <summary>
/// Benchmarks for binary data parsing (deserialization).
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class BytesParseBenchmarks
{
    private byte[] _udpPacket = null!;
    private byte[] _fullPacket = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create sample UDP packet
        _udpPacket = new byte[8 + 17];
        BinaryPrimitives.WriteUInt16BigEndian(_udpPacket.AsSpan(0), 8080);
        BinaryPrimitives.WriteUInt16BigEndian(_udpPacket.AsSpan(2), 53);
        BinaryPrimitives.WriteUInt16BigEndian(_udpPacket.AsSpan(4), 25);
        BinaryPrimitives.WriteUInt16BigEndian(_udpPacket.AsSpan(6), 0);
        "Hello, UDP World!"u8.CopyTo(_udpPacket.AsSpan(8));

        // Create full packet (Ethernet + IPv6 + UDP + Payload)
        _fullPacket = new byte[14 + 40 + 8 + 17];
        int offset = 0;

        // Ethernet
        new byte[] { 0x00, 0x5E, 0x00, 0x01, 0x02, 0x03 }.CopyTo(_fullPacket, offset); offset += 6;
        new byte[] { 0x00, 0x1A, 0x2B, 0x3C, 0x4D, 0x5E }.CopyTo(_fullPacket, offset); offset += 6;
        BinaryPrimitives.WriteUInt16BigEndian(_fullPacket.AsSpan(offset), 0x86DD); offset += 2;

        // IPv6
        BinaryPrimitives.WriteUInt32BigEndian(_fullPacket.AsSpan(offset), 0x60000000); offset += 4;
        BinaryPrimitives.WriteUInt16BigEndian(_fullPacket.AsSpan(offset), 25); offset += 2;
        _fullPacket[offset++] = 17;
        _fullPacket[offset++] = 64;
        BinaryPrimitives.WriteUInt128BigEndian(_fullPacket.AsSpan(offset), UInt128.One); offset += 16;
        BinaryPrimitives.WriteUInt128BigEndian(_fullPacket.AsSpan(offset), UInt128.One); offset += 16;

        // UDP
        BinaryPrimitives.WriteUInt16BigEndian(_fullPacket.AsSpan(offset), 8080); offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(_fullPacket.AsSpan(offset), 53); offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(_fullPacket.AsSpan(offset), 25); offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(_fullPacket.AsSpan(offset), 0); offset += 2;

        // Payload
        "Hello, UDP World!"u8.CopyTo(_fullPacket.AsSpan(offset));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 1: UDP Header Parsing
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("ParseUDP"), Benchmark(Baseline = true)]
    public (ushort, ushort, ushort, ushort) ParseUDP_Manual()
    {
        ushort srcPort = BinaryPrimitives.ReadUInt16BigEndian(_udpPacket.AsSpan(0));
        ushort dstPort = BinaryPrimitives.ReadUInt16BigEndian(_udpPacket.AsSpan(2));
        ushort length = BinaryPrimitives.ReadUInt16BigEndian(_udpPacket.AsSpan(4));
        ushort checksum = BinaryPrimitives.ReadUInt16BigEndian(_udpPacket.AsSpan(6));
        return (srcPort, dstPort, length, checksum);
    }

    [BenchmarkCategory("ParseUDP"), Benchmark]
    public UdpHeader ParseUDP_Struct()
    {
        return UdpHeader.Parse(_udpPacket);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 2: Full Packet Parsing
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("ParseFull"), Benchmark(Baseline = true)]
    public (ushort EtherType, byte NextHeader, ushort SrcPort) ParseFull_Manual()
    {
        ushort etherType = BinaryPrimitives.ReadUInt16BigEndian(_fullPacket.AsSpan(12));
        byte nextHeader = _fullPacket[6 + 14];
        ushort srcPort = BinaryPrimitives.ReadUInt16BigEndian(_fullPacket.AsSpan(54));
        return (etherType, nextHeader, srcPort);
    }

    [BenchmarkCategory("ParseFull"), Benchmark]
    public UdpIPv6Packet ParseFull_Structs()
    {
        return UdpIPv6Packet.Parse(_fullPacket);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 3: Batch Parsing (100 iterations)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("BatchParse"), Benchmark(Baseline = true)]
    public int BatchParse_Manual()
    {
        int sum = 0;
        for (int i = 0; i < 100; i++)
        {
            ushort srcPort = BinaryPrimitives.ReadUInt16BigEndian(_udpPacket.AsSpan(0));
            sum += srcPort;
        }
        return sum;
    }

    [BenchmarkCategory("BatchParse"), Benchmark]
    public int BatchParse_Struct()
    {
        int sum = 0;
        for (int i = 0; i < 100; i++)
        {
            var udp = UdpHeader.Parse(_udpPacket);
            sum += udp.SourcePort.Value;
        }
        return sum;
    }

    #endregion
}

