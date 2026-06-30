// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

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
    private static readonly byte[] _SrcMac = [0x00, 0x1A, 0x2B, 0x3C, 0x4D, 0x5E];
    private static readonly byte[] _DstMac = [0x00, 0x5E, 0x00, 0x01, 0x02, 0x03];
    private static readonly UInt128 _SrcIPv6 = UInt128.Parse("1");  // ::1
    private static readonly UInt128 _DstIPv6 = UInt128.Parse("281473913978881");  // ::ffff:192.0.2.1
    private static readonly byte[] _Payload = "Hello, UDP World!"u8.ToArray();

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 1: Simple UDP Header (8 bytes)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("SimpleUDP"), Benchmark(Baseline = true)]
    public byte[] SimpleUDP_Manual()
    {
        // UDP header: SrcPort(2) + DstPort(2) + Length(2) + Checksum(2) = 8 bytes
        byte[] result = new byte[8 + _Payload.Length];
        int offset = 0;

        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 8080);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 53);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), (ushort)(8 + _Payload.Length));
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 0);
        offset += 2;
        _Payload.CopyTo(result, offset);

        return result;
    }

    [BenchmarkCategory("SimpleUDP"), Benchmark]
    public byte[] SimpleUDP_ZeroAlloc()
    {
        return Z.Bytes(
            new U16BE(8080),      // Source port
            new U16BE(53),        // Destination port
            new U16BE((ushort)(8 + _Payload.Length)), // Length
            new U16BE(0),         // Checksum
            _Payload).ToArray();
    }

    [BenchmarkCategory("SimpleUDP"), Benchmark]
    public byte[] SimpleUDP_ZeroAlloc_Struct()
    {
        UdpHeader udp = new UdpHeader
        {
            SourcePort = new U16BE(8080),
            DestinationPort = new U16BE(53),
            Length = new U16BE((ushort)(8 + _Payload.Length)),
            Checksum = new U16BE(0)
        };
        return Z.Bytes(udp, _Payload).ToArray();
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 2: Full Network Stack (Ethernet + IPv6 + UDP)
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("FullPacket"), Benchmark(Baseline = true)]
    public byte[] FullPacket_Manual()
    {
        // Ethernet(14) + IPv6(40) + UDP(8) + _Payload
        int totalSize = 14 + 40 + 8 + _Payload.Length;
        byte[] result = new byte[totalSize];
        int offset = 0;

        // Ethernet header
        _DstMac.CopyTo(result, offset); offset += 6;
        _SrcMac.CopyTo(result, offset); offset += 6;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 0x86DD);
        offset += 2;

        // IPv6 header
        uint vtf = (6U << 28) | (0U << 20) | 0U;
        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(offset), vtf);
        offset += 4;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), (ushort)(8 + _Payload.Length));
        offset += 2;
        result[offset++] = 17; // UDP
        result[offset++] = 64; // Hop limit
        BinaryPrimitives.WriteUInt128BigEndian(result.AsSpan(offset), _SrcIPv6);
        offset += 16;
        BinaryPrimitives.WriteUInt128BigEndian(result.AsSpan(offset), _DstIPv6);
        offset += 16;

        // UDP header
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 8080);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 53);
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), (ushort)(8 + _Payload.Length));
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset), 0);
        offset += 2;

        _Payload.CopyTo(result, offset);
        return result;
    }

    [BenchmarkCategory("FullPacket"), Benchmark]
    public byte[] FullPacket_ZeroAlloc_Inline()
    {
        return Z.Bytes(
            // Ethernet
            _DstMac, _SrcMac, new U16BE(0x86DD),
            // IPv6
            new U32BE((6U << 28) | (0U << 20) | 0U),
            new U16BE((ushort)(8 + _Payload.Length)),
            (byte)17, (byte)64,
            new U128BE(_SrcIPv6),
            new U128BE(_DstIPv6),
            // UDP
            new U16BE(8080),
            new U16BE(53),
            new U16BE((ushort)(8 + _Payload.Length)),
            new U16BE(0),
            _Payload).ToArray();
    }

    [BenchmarkCategory("FullPacket"), Benchmark]
    public byte[] FullPacket_ZeroAlloc_Structs()
    {
        UdpIPv6Packet packet = new UdpIPv6Packet
        {
            Ethernet = new EthernetHeader
            {
                DestinationMac = _DstMac,
                SourceMac = _SrcMac,
                EtherType = new U16BE(0x86DD)
            },
            IPv6 = IPv6Header.Create(
                trafficClass: 0,
                flowLabel: 0,
                payloadLength: (ushort)(8 + _Payload.Length),
                nextHeader: 17,
                hopLimit: 64,
                sourceAddress: _SrcIPv6,
                destAddress: _DstIPv6),
            Udp = new UdpHeader
            {
                SourcePort = new U16BE(8080),
                DestinationPort = new U16BE(53),
                Length = new U16BE((ushort)(8 + _Payload.Length)),
                Checksum = new U16BE(0)
            },
            Payload = _Payload
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
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], (ushort)(8 + _Payload.Length));
        offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(buffer[offset..], 0);
        offset += 2;
        _Payload.CopyTo(buffer[offset..]);
        offset += _Payload.Length;

        return offset;
    }

    [BenchmarkCategory("TryBytes"), Benchmark]
    public int TryBytes_ZeroAlloc()
    {
        Span<byte> buffer = stackalloc byte[128];
        Z.TryBytes(buffer, out int written,
            new U16BE(8080),
            new U16BE(53),
            new U16BE((ushort)(8 + _Payload.Length)),
            new U16BE(0),
            _Payload);
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
            byte[] packet = new byte[8 + _Payload.Length];
            int offset = 0;

            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(offset), (ushort)(8080 + i));
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(offset), 53);
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(offset), (ushort)(8 + _Payload.Length));
            offset += 2;
            BinaryPrimitives.WriteUInt16BigEndian(packet.AsSpan(offset), 0);
            offset += 2;
            _Payload.CopyTo(packet, offset);

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
                new U16BE((ushort)(8 + _Payload.Length)),
                new U16BE(0),
                _Payload).ToArray();
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
    private byte[] _UdpPacket = null!;
    private byte[] _FullPacket = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create sample UDP packet
        _UdpPacket = new byte[8 + 17];
        BinaryPrimitives.WriteUInt16BigEndian(_UdpPacket.AsSpan(0), 8080);
        BinaryPrimitives.WriteUInt16BigEndian(_UdpPacket.AsSpan(2), 53);
        BinaryPrimitives.WriteUInt16BigEndian(_UdpPacket.AsSpan(4), 25);
        BinaryPrimitives.WriteUInt16BigEndian(_UdpPacket.AsSpan(6), 0);
        "Hello, UDP World!"u8.CopyTo(_UdpPacket.AsSpan(8));

        // Create full packet (Ethernet + IPv6 + UDP + _Payload)
        _FullPacket = new byte[14 + 40 + 8 + 17];
        int offset = 0;

        // Ethernet
        new byte[] { 0x00, 0x5E, 0x00, 0x01, 0x02, 0x03 }.CopyTo(_FullPacket, offset); offset += 6;
        new byte[] { 0x00, 0x1A, 0x2B, 0x3C, 0x4D, 0x5E }.CopyTo(_FullPacket, offset); offset += 6;
        BinaryPrimitives.WriteUInt16BigEndian(_FullPacket.AsSpan(offset), 0x86DD); offset += 2;

        // IPv6
        BinaryPrimitives.WriteUInt32BigEndian(_FullPacket.AsSpan(offset), 0x60000000); offset += 4;
        BinaryPrimitives.WriteUInt16BigEndian(_FullPacket.AsSpan(offset), 25); offset += 2;
        _FullPacket[offset++] = 17;
        _FullPacket[offset++] = 64;
        BinaryPrimitives.WriteUInt128BigEndian(_FullPacket.AsSpan(offset), UInt128.One); offset += 16;
        BinaryPrimitives.WriteUInt128BigEndian(_FullPacket.AsSpan(offset), UInt128.One); offset += 16;

        // UDP
        BinaryPrimitives.WriteUInt16BigEndian(_FullPacket.AsSpan(offset), 8080); offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(_FullPacket.AsSpan(offset), 53); offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(_FullPacket.AsSpan(offset), 25); offset += 2;
        BinaryPrimitives.WriteUInt16BigEndian(_FullPacket.AsSpan(offset), 0); offset += 2;

        // _Payload
        "Hello, UDP World!"u8.CopyTo(_FullPacket.AsSpan(offset));
    }

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 1: UDP Header Parsing
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("ParseUDP"), Benchmark(Baseline = true)]
    public (ushort, ushort, ushort, ushort) ParseUDP_Manual()
    {
        ushort srcPort = BinaryPrimitives.ReadUInt16BigEndian(_UdpPacket.AsSpan(0));
        ushort dstPort = BinaryPrimitives.ReadUInt16BigEndian(_UdpPacket.AsSpan(2));
        ushort length = BinaryPrimitives.ReadUInt16BigEndian(_UdpPacket.AsSpan(4));
        ushort checksum = BinaryPrimitives.ReadUInt16BigEndian(_UdpPacket.AsSpan(6));
        return (srcPort, dstPort, length, checksum);
    }

    [BenchmarkCategory("ParseUDP"), Benchmark]
    public UdpHeader ParseUDP_Struct()
    {
        return UdpHeader.Parse(_UdpPacket);
    }

    #endregion

    // ═══════════════════════════════════════════════════════════════════════════
    #region Category 2: Full Packet Parsing
    // ═══════════════════════════════════════════════════════════════════════════

    [BenchmarkCategory("ParseFull"), Benchmark(Baseline = true)]
    public (ushort EtherType, byte NextHeader, ushort SrcPort) ParseFull_Manual()
    {
        ushort etherType = BinaryPrimitives.ReadUInt16BigEndian(_FullPacket.AsSpan(12));
        byte nextHeader = _FullPacket[6 + 14];
        ushort srcPort = BinaryPrimitives.ReadUInt16BigEndian(_FullPacket.AsSpan(54));
        return (etherType, nextHeader, srcPort);
    }

    [BenchmarkCategory("ParseFull"), Benchmark]
    public UdpIPv6Packet ParseFull_Structs()
    {
        return UdpIPv6Packet.Parse(_FullPacket);
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
            ushort srcPort = BinaryPrimitives.ReadUInt16BigEndian(_UdpPacket.AsSpan(0));
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
            UdpHeader udp = UdpHeader.Parse(_UdpPacket);
            sum += udp.SourcePort.Value;
        }
        return sum;
    }

    #endregion
}

