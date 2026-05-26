// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="TempStringBuilder"/> and <see cref="TempBytesBuilder"/>.
/// Covers create, append, clear, seek operations, and disposal behavior.
/// </summary>
public sealed class BuilderTests
{
    // ========================================================================
    // TEMPSTRINGBUILDER - CREATION AND PROPERTIES
    // ========================================================================
    [Test]
    public async Task TempStringBuilder_Create_ReturnsEmptyBuilder()
    {
        // Act
        int length;
        bool isEmpty;
        bool capacityPositive;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            length = builder.Length;
            isEmpty = builder.IsEmpty;
            capacityPositive = builder.Capacity > 0;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
        await Assert.That(capacityPositive).IsTrue();
    }

    [Test]
    public async Task TempStringBuilder_Capacity_IsPositive()
    {
        // Act
        int capacity;
        int remaining;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            capacity = builder.Capacity;
            remaining = builder.Remaining;
        }

        // Assert
        await Assert.That(capacity > 0).IsTrue();
        await Assert.That(remaining).IsEqualTo(capacity);
    }

    // ========================================================================
    // TEMPSTRINGBUILDER - APPEND OPERATIONS
    // ========================================================================

    [Test]
    public async Task TempStringBuilder_AppendChar_IncreasesLength()
    {
        // Arrange & Act
        int length;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
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
    public async Task TempStringBuilder_AppendString_IncreasesLength()
    {
        // Arrange & Act
        int length;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
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
    public async Task TempStringBuilder_AppendSpan_IncreasesLength()
    {
        // Arrange & Act
        int length;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            ReadOnlySpan<char> span = "Test".AsSpan();
            builder.Append(span);
            length = builder.Length;
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(length).IsEqualTo(4);
        await Assert.That(content).IsEqualTo("Test");
    }

    [Test]
    public async Task TempStringBuilder_AppendInt_FormatsCorrectly()
    {
        // Arrange & Act
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append(12345);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("12345");
    }

    [Test]
    public async Task TempStringBuilder_AppendNegativeInt_FormatsCorrectly()
    {
        // Arrange & Act
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append(-12345);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("-12345");
    }

    [Test]
    public async Task TempStringBuilder_AppendDouble_FormatsCorrectly()
    {
        // Arrange & Act
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append(123.456);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).Contains("123");
        await Assert.That(content).Contains("456");
    }

    [Test]
    public async Task TempStringBuilder_AppendBool_FormatsCorrectly()
    {
        // Arrange & Act
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append(true);
            builder.Append(' ');
            builder.Append(false);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("True False");
    }

    [Test]
    public async Task TempStringBuilder_AppendGuid_FormatsCorrectly()
    {
        // Arrange & Act
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");
            builder.Append(guid);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("12345678-1234-1234-1234-123456789abc");
    }

    [Test]
    public async Task TempStringBuilder_AppendNull_DoesNothing()
    {
        // Arrange & Act
        int length;
        bool isEmpty;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            string? nullString = null;
            builder.Append(nullString);
            length = builder.Length;
            isEmpty = builder.IsEmpty;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
    }

    // ========================================================================
    // TEMPSTRINGBUILDER - CLEAR AND SEEKBACK
    // ========================================================================

    [Test]
    public async Task TempStringBuilder_Clear_ResetsLength()
    {
        // Arrange & Act
        int length;
        bool isEmpty;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hello World");
            builder.Clear();
            length = builder.Length;
            isEmpty = builder.IsEmpty;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task TempStringBuilder_SeekBack_DecreasesLength()
    {
        // Arrange & Act
        int length;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
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
    public async Task TempStringBuilder_SeekBack_FullLength_Clears()
    {
        // Arrange & Act
        int length;
        bool isEmpty;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hello");
            builder.SeekBack(5);
            length = builder.Length;
            isEmpty = builder.IsEmpty;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task TempStringBuilder_SeekBack_Zero_DoesNothing()
    {
        // Arrange & Act
        int length;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hello");
            builder.SeekBack(0);
            length = builder.Length;
        }

        // Assert
        await Assert.That(length).IsEqualTo(5);
    }

    [Test]
    public async Task TempStringBuilder_SeekBack_TooMuch_ThrowsException()
    {
        bool threw = false;
        {
            // Act & Assert - try-catch because TempStringBuilder is a ref struct and cannot be captured in a lambda
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hi");
            try { builder.SeekBack(10); }
            catch (ArgumentOutOfRangeException) { threw = true; }
        }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task TempStringBuilder_TrySeekBack_Valid_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        int length;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
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
    public async Task TempStringBuilder_TrySeekBack_Invalid_ReturnsFalse()
    {
        // Arrange & Act
        bool result;
        int length;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hi");
            result = builder.TrySeekBack(10);
            length = builder.Length;
        }

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(length).IsEqualTo(2); // Unchanged
    }

    // ========================================================================
    // TEMPSTRINGBUILDER - DISPOSAL
    // ========================================================================

    [Test]
    public async Task TempStringBuilder_Dispose_ReleasesBuffer()
    {
        // Arrange & Act
        int length;
        {
            TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Test");
            length = builder.Length;
            builder.Dispose();
        }

        // Assert - capture values before dispose
        await Assert.That(length).IsEqualTo(4);
        // Assert - dispose completes without exception
    }

    // ========================================================================
    // TEMPBYTESBUILDER - CREATION AND PROPERTIES
    // ========================================================================

    [Test]
    public async Task TempBytesBuilder_Create_ReturnsEmptyBuilder()
    {
        // Act
        int length;
        bool isEmpty;
        bool capacityPositive;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            length = builder.Length;
            isEmpty = builder.IsEmpty;
            capacityPositive = builder.Capacity > 0;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
        await Assert.That(capacityPositive).IsTrue();
    }

    [Test]
    public async Task TempBytesBuilder_Capacity_IsPositive()
    {
        // Act
        int capacity;
        int remaining;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            capacity = builder.Capacity;
            remaining = builder.Remaining;
        }

        // Assert
        await Assert.That(capacity > 0).IsTrue();
        await Assert.That(remaining).IsEqualTo(capacity);
    }

    // ========================================================================
    // TEMPBYTESBUILDER - APPEND OPERATIONS
    // ========================================================================

    [Test]
    public async Task TempBytesBuilder_AppendByte_IncreasesLength()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append(0x01);
            builder.Append(0x02);
            builder.Append(0x03);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(3);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0x01, 0x02, 0x03]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendByteArray_IncreasesLength()
    {
        // Arrange & Act
        byte[] data = [0x01, 0x02, 0x03, 0x04];
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append(data);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(4);
        await Assert.That(bytes).IsEquivalentTo(data);
    }

    [Test]
    public async Task TempBytesBuilder_AppendSpan_IncreasesLength()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            ReadOnlySpan<byte> span = [0xAA, 0xBB, 0xCC];
            builder.Append(span);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(3);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0xAA, 0xBB, 0xCC]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendInt16BigEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendInt16BigEndian(0x1234);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0x12, 0x34]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendInt16LittleEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendInt16LittleEndian(0x1234);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0x34, 0x12]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendInt32BigEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendInt32BigEndian(0x12345678);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(4);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0x12, 0x34, 0x56, 0x78]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendInt32LittleEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendInt32LittleEndian(0x12345678);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(4);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0x78, 0x56, 0x34, 0x12]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendInt64BigEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendInt64BigEndian(0x123456789ABCDEF0L);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(8);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendUInt16BigEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUInt16BigEndian(0xABCD);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0xAB, 0xCD]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendUInt32BigEndian_FormatsCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUInt32BigEndian(0xDEADBEEF);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(4);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0xDE, 0xAD, 0xBE, 0xEF]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendNullByteArray_DoesNothing()
    {
        // Arrange & Act
        int length;
        bool isEmpty;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            byte[]? nullArray = null;
            builder.Append(nullArray);
            length = builder.Length;
            isEmpty = builder.IsEmpty;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
    }

    // ========================================================================
    // TEMPBYTESBUILDER - UTF-8 APPEND
    // ========================================================================

    [Test]
    public async Task TempBytesBuilder_AppendUtf8_EncodesCorrectly()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUtf8("Hello");
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(5);
        await Assert.That(bytes).IsEquivalentTo("Hello"u8.ToArray());
    }

    [Test]
    public async Task TempBytesBuilder_AppendUtf8_UnicodeCharacters_EncodesCorrectly()
    {
        // Arrange & Act
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUtf8("Héllo €");
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(bytes).IsEquivalentTo(System.Text.Encoding.UTF8.GetBytes("Héllo €"));
    }

    // ========================================================================
    // TEMPBYTESBUILDER - VARINT
    // ========================================================================

    [Test]
    public async Task TempBytesBuilder_AppendVarInt_SmallValue_SingleByte()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendVarInt(127);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(1);
        await Assert.That(bytes).IsEquivalentTo((byte[])[127]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendVarInt_MediumValue_TwoBytes()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendVarInt(128);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0x80, 0x01]);
    }

    [Test]
    public async Task TempBytesBuilder_AppendVarInt_LargeValue_MultipleBytes()
    {
        // Arrange & Act
        bool lengthAtLeastTwo;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendVarInt(300);
            lengthAtLeastTwo = builder.Length >= 2;
        }

        // Assert
        await Assert.That(lengthAtLeastTwo).IsTrue();
    }

    // ========================================================================
    // TEMPBYTESBUILDER - CLEAR AND SEEKBACK
    // ========================================================================

    [Test]
    public async Task TempBytesBuilder_Clear_ResetsLength()
    {
        // Arrange & Act
        int length;
        bool isEmpty;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            builder.Clear();
            length = builder.Length;
            isEmpty = builder.IsEmpty;
        }

        // Assert
        await Assert.That(length).IsEqualTo(0);
        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task TempBytesBuilder_SeekBack_DecreasesLength()
    {
        // Arrange & Act
        int length;
        byte[] bytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            builder.SeekBack(2);
            length = builder.Length;
            bytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(bytes).IsEquivalentTo((byte[])[0x01, 0x02]);
    }

    [Test]
    public async Task TempBytesBuilder_SeekBack_TooMuch_ThrowsException()
    {
        bool threw = false;
        {
            // Act & Assert - try-catch because TempBytesBuilder is a ref struct and cannot be captured in a lambda
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02]);
            try { builder.SeekBack(10); }
            catch (ArgumentOutOfRangeException) { threw = true; }
        }
        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task TempBytesBuilder_TrySeekBack_Valid_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        int length;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            result = builder.TrySeekBack(2);
            length = builder.Length;
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(length).IsEqualTo(2);
    }

    [Test]
    public async Task TempBytesBuilder_TrySeekBack_Invalid_ReturnsFalse()
    {
        // Arrange & Act
        bool result;
        int length;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02]);
            result = builder.TrySeekBack(10);
            length = builder.Length;
        }

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(length).IsEqualTo(2); // Unchanged
    }

    // ========================================================================
    // TEMPBYTESBUILDER - DISPOSAL
    // ========================================================================

    [Test]
    public async Task TempBytesBuilder_Dispose_ReleasesBuffer()
    {
        // Arrange & Act
        int length;
        {
            TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03, 0x04]);
            length = builder.Length;
            builder.Dispose();
        }

        // Assert - capture values before dispose
        await Assert.That(length).IsEqualTo(4);
        // Assert - dispose completes without exception
    }

    // ========================================================================
    // TEMPBYTESBUILDER - IHEAPALLOCATED
    // ========================================================================

    [Test]
    public async Task TempBytesBuilder_IsHeapAllocated_FalseForNormalUse()
    {
        // Arrange & Act - capture value before await; TempBytesBuilder is a ref struct
        bool isHeapAllocated;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            isHeapAllocated = builder.IsHeapAllocated;
        }

        // Assert - single-scope usage must use the ThreadStatic buffer, not the heap
        await Assert.That(isHeapAllocated).IsFalse();
    }

    // ========================================================================
    // TEMPSTRINGBUILDER - COMPLEX SCENARIOS
    // ========================================================================

    [Test]
    public async Task TempStringBuilder_ChainedAppends_BuildsCorrectString()
    {
        // Arrange & Act
        string result;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("User: ");
            builder.Append(12345);
            builder.Append(", Balance: ");
            builder.Append(99.99);
            result = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(result).StartsWith("User: 12345, Balance:");
    }

    [Test]
    public async Task TempStringBuilder_ClearAndReuse_Works()
    {
        // Arrange & Act
        string firstContent;
        string secondContent;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("First");
            firstContent = builder.AsSpan().ToString();
            builder.Clear();
            builder.Append("Second");
            secondContent = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(firstContent).IsEqualTo("First");
        await Assert.That(secondContent).IsEqualTo("Second");
    }

    // ========================================================================
    // TEMPBYTESBUILDER - COMPLEX SCENARIOS
    // ========================================================================

    [Test]
    public async Task TempBytesBuilder_BuildPacket_ProducesCorrectBytes()
    {
        // Arrange & Act
        byte[] packet;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.AppendUInt16BigEndian(0xCAFE); // Magic
            builder.AppendUInt16BigEndian(5);      // Length
            builder.AppendUtf8("Hello");           // Data
            packet = builder.AsSpan().ToArray();
        }

        // Assert
        byte[] expected = [0xCA, 0xFE, 0x00, 0x05, .. "Hello"u8.ToArray()];
        await Assert.That(packet).IsEquivalentTo(expected);
    }

    [Test]
    public async Task TempBytesBuilder_ClearAndReuse_Works()
    {
        // Arrange & Act
        byte[] firstBytes;
        byte[] secondBytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03]);
            firstBytes = builder.AsSpan().ToArray();
            builder.Clear();
            builder.Append([0xAA, 0xBB]);
            secondBytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(firstBytes).IsEquivalentTo((byte[])[0x01, 0x02, 0x03]);
        await Assert.That(secondBytes).IsEquivalentTo((byte[])[0xAA, 0xBB]);
    }

    // ========================================================================
    // EDGE CASES
    // ========================================================================

    [Test]
    public async Task TempStringBuilder_EmptyString_Works()
    {
        // Arrange & Act
        bool isEmpty;
        int length;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("");
            isEmpty = builder.IsEmpty;
            length = builder.Length;
        }

        // Assert
        await Assert.That(isEmpty).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task TempBytesBuilder_EmptySpan_Works()
    {
        // Arrange & Act
        bool isEmpty;
        int length;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append(ReadOnlySpan<byte>.Empty);
            isEmpty = builder.IsEmpty;
            length = builder.Length;
        }

        // Assert
        await Assert.That(isEmpty).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task TempStringBuilder_AsSpan_ReturnsCorrectContent()
    {
        // Arrange & Act
        int spanLength;
        string spanContent;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.Append("Hello");
            ReadOnlySpan<char> span = builder.AsSpan();
            spanLength = span.Length;
            spanContent = span.ToString();
        }

        // Assert
        await Assert.That(spanLength).IsEqualTo(5);
        await Assert.That(spanContent).IsEqualTo("Hello");
    }

    [Test]
    public async Task TempBytesBuilder_WrittenSpan_MatchesAsSpan()
    {
        // Arrange & Act
        byte[] writtenBytes;
        byte[] asSpanBytes;
        {
            using TempBytesBuilder builder = TempBytesBuilder.Create();
            builder.Append([0x01, 0x02, 0x03]);
            writtenBytes = builder.WrittenSpan.ToArray();
            asSpanBytes = builder.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(writtenBytes).IsEquivalentTo(asSpanBytes);
    }

    // ========================================================================
    // TEMPSTRINGBUILDER - TRYAPPEND OPERATIONS
    // ========================================================================

    #region TryAppend

    [Test]
    public async Task TempStringBuilder_TryAppend_String_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            result = builder.TryAppend("Hello");
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(content).IsEqualTo("Hello");
    }

    [Test]
    public async Task TempStringBuilder_TryAppend_Span_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            result = builder.TryAppend("World".AsSpan());
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(content).IsEqualTo("World");
    }

    [Test]
    public async Task TempStringBuilder_TryAppend_Char_ReturnsTrue()
    {
        // Arrange & Act
        bool result1;
        bool result2;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            result1 = builder.TryAppend('A');
            result2 = builder.TryAppend('B');
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(result1).IsTrue();
        await Assert.That(result2).IsTrue();
        await Assert.That(content).IsEqualTo("AB");
    }

    [Test]
    public async Task TempStringBuilder_TryAppend_Int_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            result = builder.TryAppend(42);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(content).IsEqualTo("42");
    }

    [Test]
    public async Task TempStringBuilder_TryAppend_Long_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            result = builder.TryAppend(123456789012345L);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(content).IsEqualTo("123456789012345");
    }

    [Test]
    public async Task TempStringBuilder_TryAppend_Bool_ReturnsTrue()
    {
        // Arrange & Act
        bool result1;
        bool result2;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            result1 = builder.TryAppend(true);
            builder.TryAppend(" ");
            result2 = builder.TryAppend(false);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(result1).IsTrue();
        await Assert.That(result2).IsTrue();
        await Assert.That(content).IsEqualTo("True False");
    }

    [Test]
    public async Task TempStringBuilder_TryAppend_DateTime_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            DateTime dt = new(2025, 1, 15, 10, 30, 0);
            result = builder.TryAppend(dt);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(content).Contains("2025");
    }

    [Test]
    public async Task TempStringBuilder_TryAppend_Guid_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
            result = builder.TryAppend(guid);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(content).IsEqualTo("12345678-1234-1234-1234-123456789012");
    }

    [Test]
    public async Task TempStringBuilder_TryAppend_Null_ReturnsTrue()
    {
        // Arrange & Act
        bool result;
        bool isEmpty;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            result = builder.TryAppend((string?)null);
            isEmpty = builder.IsEmpty;
        }

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task TempStringBuilder_TryAppend_MultipleValues_ReturnsTrue()
    {
        // Arrange & Act
        string content;
        {
            using TempStringBuilder builder = TempStringBuilder.Create();
            builder.TryAppend("Value: ");
            builder.TryAppend(42);
            builder.TryAppend(", Active: ");
            builder.TryAppend(true);
            content = builder.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("Value: 42, Active: True");
    }

    #endregion
}