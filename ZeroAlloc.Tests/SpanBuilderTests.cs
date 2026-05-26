// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="SpanStringBuilder"/> and <see cref="SpanBytesBuilder"/>.
/// Covers buffer-based operations and overflow scenarios.
/// </summary>
public sealed class SpanBuilderTests
{
    // ========================================================================
    // SPANSTRINGBUILDER - CREATION AND PROPERTIES
    // ========================================================================
    [Test]
    public async Task SpanStringBuilder_Constructor_SetsCapacity()
    {
        // Arrange & Act
        int length, capacity, remaining;
        {
            Span<char> buffer = stackalloc char[256];
            SpanStringBuilder builder = new(buffer);
            length = builder.Length;
            capacity = builder.Capacity;
            remaining = builder.Remaining;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(capacity).IsEqualTo(256);
        await Assert.That(remaining).IsEqualTo(256);
    }

    [Test]
    public async Task SpanStringBuilder_SmallBuffer_Works()
    {
        // Arrange & Act
        int length;
        string content;
        {
            Span<char> buffer = stackalloc char[10];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello");
            length = builder.Length;
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(length).IsEqualTo(5);
        await Assert.That(content).IsEqualTo("Hello");
    }

    // ========================================================================
    // SPANSTRINGBUILDER - APPEND OPERATIONS
    // ========================================================================

    [Test]
    public async Task SpanStringBuilder_AppendChar_IncreasesLength()
    {
        // Arrange & Act
        int length;
        string content;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append('A');
            builder.Append('B');
            builder.Append('C');
            length = builder.Length;
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(length).IsEqualTo(3);
        await Assert.That(content).IsEqualTo("ABC");
    }

    [Test]
    public async Task SpanStringBuilder_AppendString_IncreasesLength()
    {
        // Arrange & Act
        int length;
        string content;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello");
            builder.Append(" ");
            builder.Append("World");
            length = builder.Length;
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(length).IsEqualTo(11);
        await Assert.That(content).IsEqualTo("Hello World");
    }

    [Test]
    public async Task SpanStringBuilder_AppendInt_FormatsCorrectly()
    {
        // Arrange & Act
        string content;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append(12345);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("12345");
    }

    [Test]
    public async Task SpanStringBuilder_AppendNegativeInt_FormatsCorrectly()
    {
        // Arrange & Act
        string content;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append(-12345);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("-12345");
    }

    [Test]
    public async Task SpanStringBuilder_AppendGuid_FormatsCorrectly()
    {
        // Arrange & Act
        Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        string content;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append(guid);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("12345678-1234-1234-1234-123456789abc");
    }

    // ========================================================================
    // SPANSTRINGBUILDER - BUFFER OVERFLOW
    // ========================================================================

    [Test]
    public async Task SpanStringBuilder_Overflow_ThrowsInvalidOperationException()
    {
        // Arrange
        Span<char> buffer = stackalloc char[5];
        SpanStringBuilder builder = new(buffer);
        bool threw = false;
        // Act & Assert
        try { builder.Append("This is too long"); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanStringBuilder_OverflowByOneChar_ThrowsInvalidOperationException()
    {
        // Arrange
        Span<char> buffer = stackalloc char[5];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Hello"); // Exactly fills buffer
        bool threw = false;
        // Act & Assert
        try { builder.Append('!'); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanStringBuilder_ExactFit_Works()
    {
        // Arrange & Act
        int length, remaining;
        string content;
        {
            Span<char> buffer = stackalloc char[5];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello"); // Exactly fits
            length = builder.Length;
            remaining = builder.Remaining;
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(length).IsEqualTo(5);
        await Assert.That(remaining).IsEqualTo(0);
        await Assert.That(content).IsEqualTo("Hello");
    }

    // ========================================================================
    // SPANSTRINGBUILDER - CLEAR AND SEEKBACK
    // ========================================================================

    [Test]
    public async Task SpanStringBuilder_Clear_ResetsLength()
    {
        // Arrange & Act
        int length, remaining;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello World");
            builder.Clear();
            length = builder.Length;
            remaining = builder.Remaining;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(remaining).IsEqualTo(100);
    }

    [Test]
    public async Task SpanStringBuilder_SeekBack_DecreasesLength()
    {
        // Arrange & Act
        int length;
        string content;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello World");
            builder.SeekBack(6); // Remove " World"
            length = builder.Length;
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(length).IsEqualTo(5);
        await Assert.That(content).IsEqualTo("Hello");
    }

    [Test]
    public async Task SpanStringBuilder_SeekBack_TooMuch_ThrowsException()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Hi");
        bool threw = false;
        // Act & Assert - try-catch because SpanStringBuilder is a ref struct
        try { builder.SeekBack(10); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanStringBuilder_TrySeekBack_Valid_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        int length;
        string content;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello");
            result = builder.TrySeekBack(2);
            length = builder.Length;
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(length).IsEqualTo(3);
        await Assert.That(content).IsEqualTo("Hel");
    }

    [Test]
    public async Task SpanStringBuilder_TrySeekBack_Invalid_ReturnsFalse()
    {
        // Arrange & Act
        bool result;
        int length;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hi");
            result = builder.TrySeekBack(10);
            length = builder.Length;
        }

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(length).IsEqualTo(2);
    }

    // ========================================================================
    // SPANBYTESBUILDER - CREATION AND PROPERTIES
    // ========================================================================

    [Test]
    public async Task SpanBytesBuilder_Constructor_SetsCapacity()
    {
        // Arrange & Act
        int length, capacity, remaining;
        {
            Span<byte> buffer = stackalloc byte[256];
            SpanBytesBuilder builder = new(buffer);
            length = builder.Length;
            capacity = builder.Capacity;
            remaining = builder.Remaining;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(capacity).IsEqualTo(256);
        await Assert.That(remaining).IsEqualTo(256);
    }

    [Test]
    public async Task SpanBytesBuilder_SmallBuffer_Works()
    {
        // Arrange & Act
        int length;
        byte[] content;
        {
            Span<byte> buffer = stackalloc byte[10];
            SpanBytesBuilder builder = new(buffer);
            builder.Append(0x01);
            builder.Append(0x02);
            builder.Append(0x03);
            length = builder.Length;
            content = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(3);
        await Assert.That(content).IsEquivalentTo((byte[])[0x01, 0x02, 0x03]);
    }

    // ========================================================================
    // SPANBYTESBUILDER - APPEND OPERATIONS
    // ========================================================================

    [Test]
    public async Task SpanBytesBuilder_AppendByte_IncreasesLength()
    {
        // Arrange & Act
        int length;
        byte[] content;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append(0xAA);
            builder.Append(0xBB);
            builder.Append(0xCC);
            length = builder.Length;
            content = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(3);
        await Assert.That(content).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC]);
    }

    [Test]
    public async Task SpanBytesBuilder_AppendInt16BigEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] content;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendInt16BigEndian(0x1234);
            length = builder.Length;
            content = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(content).IsEquivalentTo((byte[])[0x12, 0x34]);
    }

    [Test]
    public async Task SpanBytesBuilder_AppendInt16LittleEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] content;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendInt16LittleEndian(0x1234);
            length = builder.Length;
            content = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(content).IsEquivalentTo((byte[])[0x34, 0x12]);
    }

    [Test]
    public async Task SpanBytesBuilder_AppendInt32BigEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] content;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendInt32BigEndian(0x12345678);
            length = builder.Length;
            content = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(4);
        await Assert.That(content).IsEquivalentTo((byte[])[0x12, 0x34, 0x56, 0x78]);
    }

    [Test]
    public async Task SpanBytesBuilder_AppendUInt64BigEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] content;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendUInt64BigEndian(0x123456789ABCDEF0UL);
            length = builder.Length;
            content = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(8);
        await Assert.That(content).IsEquivalentTo((byte[])[0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0]);
    }

    // ========================================================================
    // SPANBYTESBUILDER - BUFFER OVERFLOW
    // ========================================================================

    [Test]
    public async Task SpanBytesBuilder_Overflow_ThrowsInvalidOperationException()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[5];
        SpanBytesBuilder builder = new(buffer);
        bool threw = false;
        // Act & Assert
        try { builder.Append([0x01, 0x02, 0x03, 0x04, 0x05, 0x06]); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanBytesBuilder_OverflowInt32_ThrowsInvalidOperationException()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[3]; // Only 3 bytes, but Int32 needs 4
        SpanBytesBuilder builder = new(buffer);
        bool threw = false;
        // Act & Assert
        try { builder.AppendInt32BigEndian(0x12345678); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanBytesBuilder_ExactFit_Works()
    {
        // Arrange & Act
        int length, remaining;
        {
            Span<byte> buffer = stackalloc byte[4];
            SpanBytesBuilder builder = new(buffer);
            builder.AppendInt32BigEndian(0x12345678); // Exactly fits
            length = builder.Length;
            remaining = builder.Remaining;
        }

        // Assert
        await Assert.That(length).IsEqualTo(4);
        await Assert.That(remaining).IsEqualTo(0);
    }

    // ========================================================================
    // SPANBYTESBUILDER - CLEAR AND SEEKBACK
    // ========================================================================

    [Test]
    public async Task SpanBytesBuilder_Clear_ResetsLength()
    {
        // Arrange & Act
        int length, remaining;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            builder.Clear();
            length = builder.Length;
            remaining = builder.Remaining;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(remaining).IsEqualTo(100);
    }

    [Test]
    public async Task SpanBytesBuilder_SeekBack_DecreasesLength()
    {
        // Arrange & Act
        int length;
        byte[] content;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            builder.SeekBack(2);
            length = builder.Length;
            content = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(content).IsEquivalentTo((byte[])[0x01, 0x02]);
    }

    [Test]
    public async Task SpanBytesBuilder_SeekBack_TooMuch_ThrowsException()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);
        builder.Append([0x01, 0x02]);
        bool threw = false;
        // Act & Assert - try-catch because SpanBytesBuilder is a ref struct
        try { builder.SeekBack(10); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanBytesBuilder_TrySeekBack_Valid_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        int length;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            result = builder.TrySeekBack(2);
            length = builder.Length;
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(length).IsEqualTo(2);
    }

    [Test]
    public async Task SpanBytesBuilder_TrySeekBack_Invalid_ReturnsFalse()
    {
        // Arrange & Act
        bool result;
        int length;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02]);
            result = builder.TrySeekBack(10);
            length = builder.Length;
        }

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(length).IsEqualTo(2);
    }

    // ========================================================================
    // COMPLEX SCENARIOS
    // ========================================================================

    [Test]
    public async Task SpanStringBuilder_ChainedAppends_BuildsCorrectString()
    {
        // Arrange & Act
        string content;
        {
            Span<char> buffer = stackalloc char[256];
            SpanStringBuilder builder = new(buffer);
            builder.Append("User: ");
            builder.Append(42);
            builder.Append(", Active: ");
            builder.Append(true);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("User: 42, Active: True");
    }

    [Test]
    public async Task SpanBytesBuilder_BuildPacket_ProducesCorrectBytes()
    {
        // Arrange & Act
        byte[] content;
        {
            Span<byte> buffer = stackalloc byte[256];
            SpanBytesBuilder builder = new(buffer);
            // Act - Build a simple packet: [magic][version][length]
            builder.AppendUInt16BigEndian(0xCAFE); // Magic
            builder.Append(0x01);                  // Version
            builder.AppendUInt16BigEndian(0x0010); // Length
            content = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(content).IsEquivalentTo((byte[])[0xCA, 0xFE, 0x01, 0x00, 0x10]);
    }

    [Test]
    public async Task SpanStringBuilder_ClearAndReuse_Works()
    {
        // Arrange & Act — capture both intermediate and final values before any await
        string first, second;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("First");
            first = builder.AsSpan().ToString();
            builder.Clear();
            builder.Append("Second");
            second = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(first).IsEqualTo("First");
        await Assert.That(second).IsEqualTo("Second");
    }

    [Test]
    public async Task SpanBytesBuilder_ClearAndReuse_Works()
    {
        // Arrange & Act — capture both intermediate and final values before any await
        byte[] first, second;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02, 0x03]);
            first = builder.AsSpan().ToArray();
            builder.Clear();
            builder.Append([0xAA, 0xBB]);
            second = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(first).IsEquivalentTo((byte[])[0x01, 0x02, 0x03]);
        await Assert.That(second).IsEquivalentTo((byte[])[0xAA, 0xBB]);
    }

    // ========================================================================
    // EDGE CASES
    // ========================================================================

    [Test]
    public async Task SpanStringBuilder_EmptyBuffer_ThrowsOnAnyAppend()
    {
        // Arrange
        Span<char> buffer = [];
        SpanStringBuilder builder = new(buffer);
        bool threw = false;
        // Act & Assert
        try { builder.Append("A"); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanBytesBuilder_EmptyBuffer_ThrowsOnAnyAppend()
    {
        // Arrange
        Span<byte> buffer = [];
        SpanBytesBuilder builder = new(buffer);
        bool threw = false;
        // Act & Assert
        try { builder.Append(0x01); }
        catch (InvalidOperationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task SpanStringBuilder_NullString_DoesNothing()
    {
        // Arrange & Act
        int length;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            string? nullString = null;
            builder.Append(nullString);
            length = builder.Length;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task SpanBytesBuilder_NullArray_DoesNothing()
    {
        // Arrange & Act
        int length;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            byte[]? nullArray = null;
            builder.Append(nullArray);
            length = builder.Length;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task SpanStringBuilder_Remaining_ReturnsCorrectValue()
    {
        // Arrange & Act
        int remaining;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Hello");
            remaining = builder.Remaining;
        }

        // Assert
        await Assert.That(remaining).IsEqualTo(95);
    }

    [Test]
    public async Task SpanBytesBuilder_GetRemainingSpan_ReturnsCorrectSpan()
    {
        // Arrange & Act
        int remainingLength;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            builder.Append([0x01, 0x02, 0x03]);
            Span<byte> remaining = builder.GetRemainingSpan();
            remainingLength = remaining.Length;
        }

        // Assert
        await Assert.That(remainingLength).IsEqualTo(97);
    }

    [Test]
    public async Task SpanStringBuilder_ManualFillAndAdvance_WritesCorrectly()
    {
        // Arrange & Act
        int length;
        string content;
        {
            Span<char> buffer = stackalloc char[100];
            SpanStringBuilder builder = new(buffer);
            builder.Append("Test");
            builder.Append(" Data");
            length = builder.Length;
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(length).IsEqualTo(9);
        await Assert.That(content).IsEqualTo("Test Data");
    }

    [Test]
    public async Task SpanBytesBuilder_Advance_IncreasesPosition()
    {
        // Arrange & Act
        int length;
        byte[] content;
        {
            Span<byte> buffer = stackalloc byte[100];
            SpanBytesBuilder builder = new(buffer);
            Span<byte> remaining = builder.GetRemainingSpan();
            ReadOnlySpan<byte> data = [0xAA, 0xBB];
            data.CopyTo(remaining);
            builder.Advance(2);
            length = builder.Length;
            content = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(content).IsEquivalentTo((byte[])[0xAA, 0xBB]);
    }

    [Test]
    public async Task SpanBytesBuilder_Advance_PastRemaining_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[2];
        SpanBytesBuilder builder = new(buffer);
        bool threw = false;
        int length;

        // Act & Assert
        try { builder.Advance(3); }
        catch (ArgumentOutOfRangeException) { threw = true; }

        length = builder.Length;

        await Assert.That(threw).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task SpanBytesBuilder_AppendUtf8WithVarIntPrefix_BufferTooSmall_DoesNotAdvance()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[1];
        SpanBytesBuilder builder = new(buffer);
        bool threw = false;
        int length;

        // Act & Assert
        try { builder.AppendUtf8WithVarIntPrefix("A"); }
        catch (InvalidOperationException) { threw = true; }

        length = builder.Length;

        await Assert.That(threw).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task SpanStringBuilder_AppendLine_BufferTooSmall_DoesNotAdvance()
    {
        // Arrange
        Span<char> buffer = stackalloc char[5];
        SpanStringBuilder builder = new(buffer);
        bool threw = false;
        int length;

        // Act & Assert
        try { builder.AppendLine("Hello"); }
        catch (InvalidOperationException) { threw = true; }

        length = builder.Length;

        await Assert.That(threw).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }
}
