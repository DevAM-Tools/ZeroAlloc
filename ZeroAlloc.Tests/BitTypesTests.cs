// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for sub-byte and variable-width bit types in <c>BitTypes.cs</c>.
/// </summary>
public sealed class BitTypesTests
{
    // ========================================================================
    // SUB-BYTE TYPES
    // ========================================================================

    [Test]
    [Arguments(true, (byte)1)]
    [Arguments(false, (byte)0)]
    public async Task Bit1_ImplicitConversions_Work(bool value, byte expectedByte)
    {
        Bit1 bit = value;
        bool roundTrip = bit;
        byte asByte = bit;

        await Assert.That(roundTrip).IsEqualTo(value);
        await Assert.That(asByte).IsEqualTo(expectedByte);
    }

    [Test]
    [Arguments((byte)0b11111111, (byte)0b11)]
    [Arguments((byte)0b00000010, (byte)0b10)]
    public async Task Bit2_MasksToTwoBits(byte input, byte expected)
    {
        Bit2 bits = input;
        byte asByte = bits;

        await Assert.That(asByte).IsEqualTo(expected);
    }

    [Test]
    [Arguments((byte)0b11111111, (byte)0b111)]
    [Arguments((byte)0b00000101, (byte)0b101)]
    public async Task Bit3_MasksToThreeBits(byte input, byte expected)
    {
        Bit3 bits = input;
        await Assert.That((byte)bits).IsEqualTo(expected);
    }

    [Test]
    [Arguments((byte)0xFF, (byte)0x0F)]
    [Arguments((byte)0x1A, (byte)0x0A)]
    public async Task Nibble_MasksToFourBits(byte input, byte expected)
    {
        Nibble nibble = input;
        await Assert.That((byte)nibble).IsEqualTo(expected);
    }

    [Test]
    [Arguments((byte)0xFF, (byte)0x1F)]
    public async Task Bit5_MasksToFiveBits(byte input, byte expected)
    {
        Bit5 bits = input;
        await Assert.That((byte)bits).IsEqualTo(expected);
    }

    [Test]
    [Arguments((byte)0xFF, (byte)0x3F)]
    public async Task Bit6_MasksToSixBits(byte input, byte expected)
    {
        Bit6 bits = input;
        await Assert.That((byte)bits).IsEqualTo(expected);
    }

    [Test]
    [Arguments((byte)0xFF, (byte)0x7F)]
    public async Task Bit7_MasksToSevenBits(byte input, byte expected)
    {
        Bit7 bits = input;
        await Assert.That((byte)bits).IsEqualTo(expected);
    }

    // ========================================================================
    // UINTBITS
    // ========================================================================

    [Test]
    public async Task UIntBits_ValidConstruction_MasksValue()
    {
        UIntBits bits = new(0xFFFF, 12);
        ulong value = bits;

        await Assert.That((int)bits.BitCount).IsEqualTo(12);
        await Assert.That(value).IsEqualTo(0x0FFFUL);
    }

    [Test]
    public async Task UIntBits_SixtyFourBits_PreservesFullValue()
    {
        UIntBits bits = new(ulong.MaxValue, 64);
        await Assert.That((ulong)bits).IsEqualTo(ulong.MaxValue);
    }

    [Test]
    [Arguments((byte)0)]
    [Arguments((byte)65)]
    public async Task UIntBits_InvalidBitCount_ThrowsArgumentOutOfRangeException(byte bitCount)
    {
        bool threw = false;
        try { _ = new UIntBits(1, bitCount); }
        catch (ArgumentOutOfRangeException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    // ========================================================================
    // INTBITS
    // ========================================================================

    [Test]
    public async Task IntBits_PositiveValue_SignExtendsCorrectly()
    {
        IntBits bits = new(0b0111, 4);
        await Assert.That((long)bits).IsEqualTo(7);
        await Assert.That((int)bits.BitCount).IsEqualTo(4);
    }

    [Test]
    public async Task IntBits_NegativeValue_SignExtendsCorrectly()
    {
        IntBits bits = new(0b1000, 4);
        await Assert.That((long)bits).IsEqualTo(-8);
    }

    [Test]
    public async Task IntBits_SixtyFourBits_PreservesFullValue()
    {
        IntBits bits = new(long.MinValue, 64);
        await Assert.That((long)bits).IsEqualTo(long.MinValue);
    }

    [Test]
    [Arguments((byte)0)]
    [Arguments((byte)65)]
    public async Task IntBits_InvalidBitCount_ThrowsArgumentOutOfRangeException(byte bitCount)
    {
        bool threw = false;
        try { _ = new IntBits(1, bitCount); }
        catch (ArgumentOutOfRangeException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }
}
