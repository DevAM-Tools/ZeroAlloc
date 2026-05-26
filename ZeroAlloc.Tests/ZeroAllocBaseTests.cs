// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="ZeroAllocBase"/> fallback methods and all attribute types in
/// <c>Attributes.cs</c>. The fallback methods each throw to signal that the source
/// generator should have replaced them; the tests verify this contract. The attribute
/// tests confirm constructors and property values are correct.
/// </summary>
public sealed class ZeroAllocBaseTests
{
    // Concrete partial subclass used only to trigger the protected base constructor.
    private sealed partial class ConcreteZA : ZeroAllocBase { }

    // ========================================================================
    // ZeroAllocBase — Protected Constructor
    // ========================================================================

    /// <summary>
    /// Verifies the protected constructor is reachable via a derived class so that
    /// all constructor statements are covered.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_ProtectedConstructor_IsAccessibleViaDerivedClass()
    {
        // The constructor is intentionally empty; we just verify instantiation succeeds.
        ConcreteZA instance = new();
        await Assert.That(instance).IsNotNull();
    }

    // ========================================================================
    // ZeroAllocBase — Fallback Method Contract
    // Each public static method must throw to indicate missing generator output.
    // ========================================================================

    /// <summary>
    /// Verifies the generic String fallback throws InvalidOperationException, signalling
    /// that the source generator failed to produce a type-specific overload.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_String_Fallback_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ZeroAllocBase.String("a", "b"));
        await Assert.That(ex.Message).Contains("source generator");
    }

    /// <summary>
    /// Verifies the generic TryString fallback throws InvalidOperationException.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_TryString_Fallback_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ZeroAllocBase.TryString(new char[32], out int _, "a"));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Verifies the generic Utf8 fallback throws InvalidOperationException.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_Utf8_Fallback_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ZeroAllocBase.Utf8("hello"));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Verifies the generic TryUtf8 fallback throws InvalidOperationException.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_TryUtf8_Fallback_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ZeroAllocBase.TryUtf8(new byte[32], out int _, "hello"));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Verifies the generic Bytes fallback throws NotSupportedException (binary
    /// serialization requires known types at compile time).
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_Bytes_Fallback_ThrowsNotSupportedException()
    {
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => ZeroAllocBase.Bytes("x"));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Verifies the generic TryBytes fallback throws NotSupportedException.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_TryBytes_Fallback_ThrowsNotSupportedException()
    {
        NotSupportedException ex = Assert.Throws<NotSupportedException>(() => ZeroAllocBase.TryBytes(new byte[32], out int _, "x"));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Verifies the generic LocalizedString fallback throws InvalidOperationException.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_LocalizedString_Fallback_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ZeroAllocBase.LocalizedString(null, "a"));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Verifies the generic TryLocalizedString fallback throws InvalidOperationException.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_TryLocalizedString_Fallback_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ZeroAllocBase.TryLocalizedString(null, new char[32], out int _, "a"));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Verifies the generic LocalizedUtf8 fallback throws InvalidOperationException.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_LocalizedUtf8_Fallback_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ZeroAllocBase.LocalizedUtf8(null, "a"));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Verifies the generic TryLocalizedUtf8 fallback throws InvalidOperationException.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_TryLocalizedUtf8_Fallback_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ZeroAllocBase.TryLocalizedUtf8(null, new byte[32], out int _, "a"));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Verifies the generic Lazy fallback throws InvalidOperationException.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_Lazy_Fallback_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ZeroAllocBase.Lazy("a"));
        await Assert.That(ex).IsNotNull();
    }

    /// <summary>
    /// Verifies the generic LazyInterpolated fallback throws InvalidOperationException.
    /// </summary>
    [Test]
    public async Task ZeroAllocBase_LazyInterpolated_Fallback_ThrowsInvalidOperationException()
    {
        InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => ZeroAllocBase.LazyInterpolated("a"));
        await Assert.That(ex).IsNotNull();
    }
}

/// <summary>
/// Tests for all attribute types in <c>Attributes.cs</c>, verifying that constructors
/// store their arguments and that mutable properties accept new values.
/// </summary>
public sealed class AttributeTests
{
    // ========================================================================
    // BinaryParsableAttribute
    // ========================================================================
    [Test]
    public async Task BinaryParsableAttribute_DefaultEndianness_IsBigEndian()
    {
        BinaryParsableAttribute attr = new();
        await Assert.That(attr.DefaultEndianness).IsEqualTo(Endianness.BigEndian);
    }

    [Test]
    public async Task BinaryParsableAttribute_DefaultEndianness_CanBeSetToLittleEndian()
    {
        BinaryParsableAttribute attr = new() { DefaultEndianness = Endianness.LittleEndian };
        await Assert.That(attr.DefaultEndianness).IsEqualTo(Endianness.LittleEndian);
    }

    // ========================================================================
    // BinaryWritableAttribute
    // ========================================================================

    [Test]
    public async Task BinaryWritableAttribute_DefaultEndianness_IsBigEndian()
    {
        BinaryWritableAttribute attr = new();
        await Assert.That(attr.DefaultEndianness).IsEqualTo(Endianness.BigEndian);
    }

    [Test]
    public async Task BinaryWritableAttribute_DefaultEndianness_CanBeSetToLittleEndian()
    {
        BinaryWritableAttribute attr = new() { DefaultEndianness = Endianness.LittleEndian };
        await Assert.That(attr.DefaultEndianness).IsEqualTo(Endianness.LittleEndian);
    }

    // ========================================================================
    // BinaryOrderAttribute
    // ========================================================================

    [Test]
    public async Task BinaryOrderAttribute_StoresOrder()
    {
        BinaryOrderAttribute attr = new(3);
        await Assert.That(attr.Order).IsEqualTo(3);
    }

    [Test]
    public async Task BinaryOrderAttribute_ZeroOrder_IsValid()
    {
        BinaryOrderAttribute attr = new(0);
        await Assert.That(attr.Order).IsEqualTo(0);
    }

    // ========================================================================
    // BinaryIgnoreAttribute
    // ========================================================================

    [Test]
    public async Task BinaryIgnoreAttribute_CanBeConstructed()
    {
        BinaryIgnoreAttribute attr = new();
        await Assert.That(attr).IsNotNull();
    }

    // ========================================================================
    // BinaryFixedLengthAttribute
    // ========================================================================

    [Test]
    public async Task BinaryFixedLengthAttribute_StoresLength()
    {
        BinaryFixedLengthAttribute attr = new(6);
        await Assert.That(attr.Length).IsEqualTo(6);
    }

    // ========================================================================
    // BinaryFieldAttribute
    // ========================================================================

    [Test]
    public async Task BinaryFieldAttribute_Defaults_AreCorrect()
    {
        BinaryFieldAttribute attr = new();
        await Assert.That(attr.Endianness).IsEqualTo(Endianness.BigEndian);
        await Assert.That(attr.BitCount).IsEqualTo(-1);
        await Assert.That(attr.PaddingBits).IsEqualTo(0);
    }

    [Test]
    public async Task BinaryFieldAttribute_Properties_CanBeSet()
    {
        BinaryFieldAttribute attr = new()
        {
            Endianness = Endianness.LittleEndian,
            BitCount = 11,
            PaddingBits = 5,
        };

        await Assert.That(attr.Endianness).IsEqualTo(Endianness.LittleEndian);
        await Assert.That(attr.BitCount).IsEqualTo(11);
        await Assert.That(attr.PaddingBits).IsEqualTo(5);
    }

    // ========================================================================
    // BinaryStringLengthAttribute
    // ========================================================================

    [Test]
    public async Task BinaryStringLengthAttribute_StoresEncoding()
    {
        BinaryStringLengthAttribute attr = new(StringLengthEncoding.VarInt);
        await Assert.That(attr.Encoding).IsEqualTo(StringLengthEncoding.VarInt);
        await Assert.That(attr.LengthBytes).IsEqualTo(4);   // default
        await Assert.That(attr.FixedLength).IsEqualTo(-1);  // default
    }

    [Test]
    public async Task BinaryStringLengthAttribute_Properties_CanBeSet()
    {
        BinaryStringLengthAttribute attr = new(StringLengthEncoding.FixedBE)
        {
            LengthBytes = 2,
            FixedLength = 32,
        };

        await Assert.That(attr.Encoding).IsEqualTo(StringLengthEncoding.FixedBE);
        await Assert.That(attr.LengthBytes).IsEqualTo(2);
        await Assert.That(attr.FixedLength).IsEqualTo(32);
    }

    // ========================================================================
    // StringLengthVarIntAttribute
    // ========================================================================

    [Test]
    public async Task StringLengthVarIntAttribute_CanBeConstructed()
    {
        StringLengthVarIntAttribute attr = new();
        await Assert.That(attr).IsNotNull();
    }

    // ========================================================================
    // StringLengthBEAttribute
    // ========================================================================

    [Test]
    public async Task StringLengthBEAttribute_DefaultLengthBytes_IsFour()
    {
        StringLengthBEAttribute attr = new();
        await Assert.That(attr.LengthBytes).IsEqualTo(4);
    }

    [Test]
    public async Task StringLengthBEAttribute_StoresLengthBytes()
    {
        StringLengthBEAttribute attr = new(2);
        await Assert.That(attr.LengthBytes).IsEqualTo(2);
    }

    // ========================================================================
    // StringLengthLEAttribute
    // ========================================================================

    [Test]
    public async Task StringLengthLEAttribute_DefaultLengthBytes_IsFour()
    {
        StringLengthLEAttribute attr = new();
        await Assert.That(attr.LengthBytes).IsEqualTo(4);
    }

    [Test]
    public async Task StringLengthLEAttribute_StoresLengthBytes()
    {
        StringLengthLEAttribute attr = new(2);
        await Assert.That(attr.LengthBytes).IsEqualTo(2);
    }

    // ========================================================================
    // StringNullTerminatedAttribute
    // ========================================================================

    [Test]
    public async Task StringNullTerminatedAttribute_CanBeConstructed()
    {
        StringNullTerminatedAttribute attr = new();
        await Assert.That(attr).IsNotNull();
    }

    // ========================================================================
    // StringFixedLengthAttribute
    // ========================================================================

    [Test]
    public async Task StringFixedLengthAttribute_StoresLength()
    {
        StringFixedLengthAttribute attr = new(8);
        await Assert.That(attr.Length).IsEqualTo(8);
    }

    // ========================================================================
    // StringLengthFromFieldAttribute
    // ========================================================================

    [Test]
    public async Task StringLengthFromFieldAttribute_StoresFieldName()
    {
        StringLengthFromFieldAttribute attr = new("NameLength");
        await Assert.That(attr.LengthFieldName).IsEqualTo("NameLength");
    }

    // ========================================================================
    // BytesLengthVarIntAttribute
    // ========================================================================

    [Test]
    public async Task BytesLengthVarIntAttribute_CanBeConstructed()
    {
        BytesLengthVarIntAttribute attr = new();
        await Assert.That(attr).IsNotNull();
    }

    // ========================================================================
    // BytesLengthBEAttribute
    // ========================================================================

    [Test]
    public async Task BytesLengthBEAttribute_DefaultLengthBytes_IsFour()
    {
        BytesLengthBEAttribute attr = new();
        await Assert.That(attr.LengthBytes).IsEqualTo(4);
    }

    [Test]
    public async Task BytesLengthBEAttribute_StoresLengthBytes()
    {
        BytesLengthBEAttribute attr = new(2);
        await Assert.That(attr.LengthBytes).IsEqualTo(2);
    }

    // ========================================================================
    // BytesLengthLEAttribute
    // ========================================================================

    [Test]
    public async Task BytesLengthLEAttribute_DefaultLengthBytes_IsFour()
    {
        BytesLengthLEAttribute attr = new();
        await Assert.That(attr.LengthBytes).IsEqualTo(4);
    }

    [Test]
    public async Task BytesLengthLEAttribute_StoresLengthBytes()
    {
        BytesLengthLEAttribute attr = new(2);
        await Assert.That(attr.LengthBytes).IsEqualTo(2);
    }

    // ========================================================================
    // BytesLengthFromFieldAttribute
    // ========================================================================

    [Test]
    public async Task BytesLengthFromFieldAttribute_StoresFieldName()
    {
        BytesLengthFromFieldAttribute attr = new("DataLength");
        await Assert.That(attr.LengthFieldName).IsEqualTo("DataLength");
    }
}