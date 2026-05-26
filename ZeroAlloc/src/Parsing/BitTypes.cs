// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc;

#region Sub-Byte Types for Bit-Level Deserialization

/// <summary>
/// Represents a single bit (0 or 1).
/// </summary>
public readonly record struct Bit1(bool Value)
{
    /// <summary>Converts the bit to a boolean value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator bool(Bit1 bit) => bit.Value;

    /// <summary>Creates a bit from a boolean value.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Bit1(bool value) => new(value);

    /// <summary>Converts the bit to a byte (0 or 1).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(Bit1 bit) => (byte)(bit.Value ? 1 : 0);
}

/// <summary>
/// Represents a 2-bit value (0-3).
/// </summary>
public readonly record struct Bit2(byte Value)
{
    /// <summary>Converts the 2-bit value to a byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(Bit2 bits) => bits.Value;

    /// <summary>Creates a 2-bit value from a byte, masking to the lower 2 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Bit2(byte value) => new((byte)(value & 0b11));
}

/// <summary>
/// Represents a 3-bit value (0-7).
/// </summary>
public readonly record struct Bit3(byte Value)
{
    /// <summary>Converts the 3-bit value to a byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(Bit3 bits) => bits.Value;

    /// <summary>Creates a 3-bit value from a byte, masking to the lower 3 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Bit3(byte value) => new((byte)(value & 0b111));
}

/// <summary>
/// Represents a 4-bit value / nibble (0-15).
/// </summary>
public readonly record struct Nibble(byte Value)
{
    /// <summary>Converts the nibble to a byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(Nibble nibble) => nibble.Value;

    /// <summary>Creates a nibble from a byte, masking to the lower 4 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Nibble(byte value) => new((byte)(value & 0x0F));
}

/// <summary>
/// Represents a 5-bit value (0-31).
/// </summary>
public readonly record struct Bit5(byte Value)
{
    /// <summary>Converts the 5-bit value to a byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(Bit5 bits) => bits.Value;

    /// <summary>Creates a 5-bit value from a byte, masking to the lower 5 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Bit5(byte value) => new((byte)(value & 0b11111));
}

/// <summary>
/// Represents a 6-bit value (0-63).
/// </summary>
public readonly record struct Bit6(byte Value)
{
    /// <summary>Converts the 6-bit value to a byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(Bit6 bits) => bits.Value;

    /// <summary>Creates a 6-bit value from a byte, masking to the lower 6 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Bit6(byte value) => new((byte)(value & 0b111111));
}

/// <summary>
/// Represents a 7-bit value (0-127).
/// </summary>
public readonly record struct Bit7(byte Value)
{
    /// <summary>Converts the 7-bit value to a byte.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator byte(Bit7 bits) => bits.Value;

    /// <summary>Creates a 7-bit value from a byte, masking to the lower 7 bits.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Bit7(byte value) => new((byte)(value & 0b1111111));
}

#endregion

#region Variable-Length Bit Integers

/// <summary>
/// Represents an unsigned integer of specified bit length (1-64 bits).
/// Used for non-standard width integers in automotive and protocol parsing.
/// </summary>
public readonly record struct UIntBits
{
    /// <summary>Gets the integer value.</summary>
    public ulong Value { get; init; }
    /// <summary>Gets the bit width of this integer (1-64).</summary>
    public byte BitCount { get; init; }

    /// <summary>Initializes a new variable-length unsigned integer.</summary>
    /// <param name="value">The integer value.</param>
    /// <param name="bitCount">The bit width (1-64).</param>
    public UIntBits(ulong value, byte bitCount)
    {
        if (bitCount is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "BitCount must be between 1 and 64");
        }

        Value = bitCount == 64 ? value : value & ((1UL << bitCount) - 1);
        BitCount = bitCount;
    }

    /// <summary>Converts the variable-length integer to a 64-bit unsigned integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ulong(UIntBits bits) => bits.Value;
}

/// <summary>
/// Represents a signed integer of specified bit length (1-64 bits).
/// </summary>
public readonly record struct IntBits
{
    /// <summary>Gets the integer value.</summary>
    public long Value { get; init; }
    /// <summary>Gets the bit width of this integer (1-64).</summary>
    public byte BitCount { get; init; }

    /// <summary>Initializes a new variable-length signed integer with sign extension.</summary>
    /// <param name="value">The integer value.</param>
    /// <param name="bitCount">The bit width (1-64).</param>
    public IntBits(long value, byte bitCount)
    {
        if (bitCount is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(bitCount), "BitCount must be between 1 and 64");
        }

        // Sign-extend the value
        if (bitCount < 64)
        {
            long mask = (1L << bitCount) - 1;
            long signBit = 1L << (bitCount - 1);
            Value = value & mask;
            if ((Value & signBit) != 0)
            {
                Value |= ~mask; // Sign extend
            }
        }
        else
        {
            Value = value;
        }

        BitCount = bitCount;
    }

    /// <summary>Converts the variable-length integer to a 64-bit signed integer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator long(IntBits bits) => bits.Value;
}

#endregion
