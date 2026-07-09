// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Benchmarks;

/// <summary>
/// Ethernet II frame header (14 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct EthernetHeader : IBinarySerializable
{
    /// <summary>Destination MAC address (6 bytes).</summary>
    public required byte[] DestinationMac { get; init; }

    /// <summary>Source MAC address (6 bytes).</summary>
    public required byte[] SourceMac { get; init; }

    /// <summary>EtherType (2 bytes, big-endian). 0x0800 = IPv4, 0x86DD = IPv6, 0x8100 = VLAN.</summary>
    public required U16BE EtherType { get; init; }

    /// <summary>On-wire size in bytes: 6 + 6 + 2.</summary>
    public const int Size = 14;

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
        DestinationMac.AsSpan().CopyTo(destination);
        SourceMac.AsSpan().CopyTo(destination[6..]);
        BinaryPrimitives.WriteUInt16BigEndian(destination[12..], EtherType.Value);
        bytesWritten = Size;
        return true;
    }

    /// <summary>Parses an Ethernet header from the first <see cref="Size"/> bytes of <paramref name="data"/>.</summary>
    /// <param name="data">Source bytes containing at least <see cref="Size"/> bytes.</param>
    /// <returns>The parsed header.</returns>
    public static EthernetHeader Parse(ReadOnlySpan<byte> data)
    {
        return new EthernetHeader
        {
            DestinationMac = data[..6].ToArray(),
            SourceMac = data[6..12].ToArray(),
            EtherType = new U16BE(BinaryPrimitives.ReadUInt16BigEndian(data[12..14]))
        };
    }
}

/// <summary>
/// IEEE 802.1Q VLAN tag (4 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct VlanHeader : IBinarySerializable
{
    /// <summary>Tag Protocol Identifier (2 bytes). Always 0x8100 for VLAN.</summary>
    public required U16BE Tpid { get; init; }

    /// <summary>Priority Code Point (3 bits), Drop Eligible Indicator (1 bit), VLAN ID (12 bits).</summary>
    public required U16BE Tci { get; init; }

    /// <summary>On-wire size in bytes.</summary>
    public const int Size = 4;

    /// <summary>Priority Code Point (0-7).</summary>
    public byte Pcp => (byte)((Tci.Value >> 13) & 0x07);

    /// <summary>Drop Eligible Indicator.</summary>
    public bool Dei => ((Tci.Value >> 12) & 0x01) != 0;

    /// <summary>VLAN Identifier (0-4095).</summary>
    public ushort VlanId => (ushort)(Tci.Value & 0x0FFF);

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
        BinaryPrimitives.WriteUInt16BigEndian(destination, Tpid.Value);
        BinaryPrimitives.WriteUInt16BigEndian(destination[2..], Tci.Value);
        bytesWritten = Size;
        return true;
    }

    /// <summary>Creates a VLAN tag from PCP, DEI, and VLAN ID fields.</summary>
    /// <param name="pcp">Priority Code Point (0-7).</param>
    /// <param name="dei">Drop Eligible Indicator.</param>
    /// <param name="vlanId">VLAN Identifier (0-4095).</param>
    /// <returns>A VLAN header with TPID 0x8100.</returns>
    public static VlanHeader Create(byte pcp, bool dei, ushort vlanId)
    {
        ushort tci = (ushort)(((pcp & 0x07) << 13) | ((dei ? 1 : 0) << 12) | (vlanId & 0x0FFF));
        return new VlanHeader
        {
            Tpid = new U16BE(0x8100),
            Tci = new U16BE(tci)
        };
    }

    /// <summary>Parses a VLAN tag from the first <see cref="Size"/> bytes of <paramref name="data"/>.</summary>
    /// <param name="data">Source bytes containing at least <see cref="Size"/> bytes.</param>
    /// <returns>The parsed VLAN header.</returns>
    public static VlanHeader Parse(ReadOnlySpan<byte> data)
    {
        return new VlanHeader
        {
            Tpid = new U16BE(BinaryPrimitives.ReadUInt16BigEndian(data[..2])),
            Tci = new U16BE(BinaryPrimitives.ReadUInt16BigEndian(data[2..4]))
        };
    }
}

/// <summary>
/// IPv6 header (40 bytes fixed, no extension headers).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct IPv6Header : IBinarySerializable
{
    /// <summary>Version (4 bits) + Traffic Class (8 bits) + Flow Label (20 bits).</summary>
    public required U32BE VersionTrafficFlow { get; init; }

    /// <summary>Payload Length (2 bytes).</summary>
    public required U16BE PayloadLength { get; init; }

    /// <summary>Next Header (1 byte). 17 = UDP, 6 = TCP.</summary>
    public required byte NextHeader { get; init; }

    /// <summary>Hop Limit (1 byte).</summary>
    public required byte HopLimit { get; init; }

    /// <summary>Source address (16 bytes).</summary>
    public required U128BE SourceAddress { get; init; }

    /// <summary>Destination address (16 bytes).</summary>
    public required U128BE DestinationAddress { get; init; }

    /// <summary>On-wire size in bytes.</summary>
    public const int Size = 40;

    /// <summary>IP version (should always be 6).</summary>
    public byte Version => (byte)((VersionTrafficFlow.Value >> 28) & 0x0F);

    /// <summary>Traffic class (DSCP + ECN).</summary>
    public byte TrafficClass => (byte)((VersionTrafficFlow.Value >> 20) & 0xFF);

    /// <summary>Flow label.</summary>
    public uint FlowLabel => VersionTrafficFlow.Value & 0x000FFFFF;

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
        BinaryPrimitives.WriteUInt32BigEndian(destination, VersionTrafficFlow.Value);
        BinaryPrimitives.WriteUInt16BigEndian(destination[4..], PayloadLength.Value);
        destination[6] = NextHeader;
        destination[7] = HopLimit;
        BinaryPrimitives.WriteUInt128BigEndian(destination[8..], SourceAddress.Value);
        BinaryPrimitives.WriteUInt128BigEndian(destination[24..], DestinationAddress.Value);
        bytesWritten = Size;
        return true;
    }

    /// <summary>Creates an IPv6 header from logical field values.</summary>
    /// <param name="trafficClass">Traffic class (8 bits).</param>
    /// <param name="flowLabel">Flow label (20 bits).</param>
    /// <param name="payloadLength">Payload length in bytes.</param>
    /// <param name="nextHeader">Next header protocol number.</param>
    /// <param name="hopLimit">Hop limit.</param>
    /// <param name="sourceAddress">Source IPv6 address.</param>
    /// <param name="destAddress">Destination IPv6 address.</param>
    /// <returns>A serialized IPv6 header with version 6.</returns>
    public static IPv6Header Create(byte trafficClass, uint flowLabel, ushort payloadLength,
        byte nextHeader, byte hopLimit, UInt128 sourceAddress, UInt128 destAddress)
    {
        uint vtf = (6U << 28) | ((uint)trafficClass << 20) | (flowLabel & 0x000FFFFF);
        return new IPv6Header
        {
            VersionTrafficFlow = new U32BE(vtf),
            PayloadLength = new U16BE(payloadLength),
            NextHeader = nextHeader,
            HopLimit = hopLimit,
            SourceAddress = new U128BE(sourceAddress),
            DestinationAddress = new U128BE(destAddress)
        };
    }

    /// <summary>Parses an IPv6 header from the first <see cref="Size"/> bytes of <paramref name="data"/>.</summary>
    /// <param name="data">Source bytes containing at least <see cref="Size"/> bytes.</param>
    /// <returns>The parsed IPv6 header.</returns>
    public static IPv6Header Parse(ReadOnlySpan<byte> data)
    {
        return new IPv6Header
        {
            VersionTrafficFlow = new U32BE(BinaryPrimitives.ReadUInt32BigEndian(data[..4])),
            PayloadLength = new U16BE(BinaryPrimitives.ReadUInt16BigEndian(data[4..6])),
            NextHeader = data[6],
            HopLimit = data[7],
            SourceAddress = new U128BE(BinaryPrimitives.ReadUInt128BigEndian(data[8..24])),
            DestinationAddress = new U128BE(BinaryPrimitives.ReadUInt128BigEndian(data[24..40]))
        };
    }
}

/// <summary>
/// UDP header (8 bytes).
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct UdpHeader : IBinarySerializable
{
    /// <summary>Source port (2 bytes).</summary>
    public required U16BE SourcePort { get; init; }

    /// <summary>Destination port (2 bytes).</summary>
    public required U16BE DestinationPort { get; init; }

    /// <summary>Length (2 bytes) - includes header + data.</summary>
    public required U16BE Length { get; init; }

    /// <summary>Checksum (2 bytes).</summary>
    public required U16BE Checksum { get; init; }

    /// <summary>On-wire size in bytes.</summary>
    public const int Size = 8;

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
        BinaryPrimitives.WriteUInt16BigEndian(destination, SourcePort.Value);
        BinaryPrimitives.WriteUInt16BigEndian(destination[2..], DestinationPort.Value);
        BinaryPrimitives.WriteUInt16BigEndian(destination[4..], Length.Value);
        BinaryPrimitives.WriteUInt16BigEndian(destination[6..], Checksum.Value);
        bytesWritten = Size;
        return true;
    }

    /// <summary>Parses a UDP header from the first <see cref="Size"/> bytes of <paramref name="data"/>.</summary>
    /// <param name="data">Source bytes containing at least <see cref="Size"/> bytes.</param>
    /// <returns>The parsed UDP header.</returns>
    public static UdpHeader Parse(ReadOnlySpan<byte> data)
    {
        return new UdpHeader
        {
            SourcePort = new U16BE(BinaryPrimitives.ReadUInt16BigEndian(data[..2])),
            DestinationPort = new U16BE(BinaryPrimitives.ReadUInt16BigEndian(data[2..4])),
            Length = new U16BE(BinaryPrimitives.ReadUInt16BigEndian(data[4..6])),
            Checksum = new U16BE(BinaryPrimitives.ReadUInt16BigEndian(data[6..8]))
        };
    }
}

/// <summary>
/// Complete UDP over IPv6 packet with Ethernet header.
/// </summary>
public readonly record struct UdpIPv6Packet : IBinarySerializable
{
    /// <summary>Gets the Ethernet frame header.</summary>
    public required EthernetHeader Ethernet { get; init; }

    /// <summary>Gets the IPv6 network-layer header.</summary>
    public required IPv6Header IPv6 { get; init; }

    /// <summary>Gets the UDP transport-layer header.</summary>
    public required UdpHeader Udp { get; init; }

    /// <summary>Gets the UDP payload bytes following the headers.</summary>
    public required byte[] Payload { get; init; }

    /// <inheritdoc />
    public bool TryGetWrittenSize(out int size)
    {
        size = EthernetHeader.Size + IPv6Header.Size + UdpHeader.Size + Payload.Length;
        return true;
    }

    /// <inheritdoc />
    public bool TryWrite(Span<byte> destination, out int bytesWritten)
    {
        int totalSize = EthernetHeader.Size + IPv6Header.Size + UdpHeader.Size + Payload.Length;
        if (destination.Length < totalSize) { bytesWritten = 0; return false; }

        int offset = 0;
        Ethernet.TryWrite(destination[offset..], out int written);
        offset += written;
        IPv6.TryWrite(destination[offset..], out written);
        offset += written;
        Udp.TryWrite(destination[offset..], out written);
        offset += written;
        Payload.AsSpan().CopyTo(destination[offset..]);
        offset += Payload.Length;

        bytesWritten = offset;
        return true;
    }

    /// <summary>Parses a full Ethernet/IPv6/UDP packet from <paramref name="data"/>.</summary>
    /// <param name="data">Complete on-wire packet bytes.</param>
    /// <returns>The parsed packet; remaining bytes become <see cref="Payload"/>.</returns>
    public static UdpIPv6Packet Parse(ReadOnlySpan<byte> data)
    {
        int offset = 0;
        EthernetHeader ethernet = EthernetHeader.Parse(data[offset..(offset + EthernetHeader.Size)]);
        offset += EthernetHeader.Size;

        IPv6Header ipv6 = IPv6Header.Parse(data[offset..(offset + IPv6Header.Size)]);
        offset += IPv6Header.Size;

        UdpHeader udp = UdpHeader.Parse(data[offset..(offset + UdpHeader.Size)]);
        offset += UdpHeader.Size;

        byte[] payload = data[offset..].ToArray();

        return new UdpIPv6Packet
        {
            Ethernet = ethernet,
            IPv6 = ipv6,
            Udp = udp,
            Payload = payload
        };
    }
}
