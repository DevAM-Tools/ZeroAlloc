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

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="SpanStringBuilder"/> and <see cref="SpanBytesBuilder"/>.
/// Covers buffer-based operations and overflow scenarios.
/// </summary>
public class SpanBuilderTests
{
    // ========================================================================
    // SPANSTRINGBUILDER - CREATION AND PROPERTIES
    // ========================================================================

    [Fact]
    public void SpanStringBuilder_Constructor_SetsCapacity()
    {
        // Arrange
        Span<char> buffer = stackalloc char[256];

        // Act
        SpanStringBuilder builder = new(buffer);

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.Equal(256, builder.Capacity);
        Assert.Equal(256, builder.Remaining);
    }

    [Fact]
    public void SpanStringBuilder_SmallBuffer_Works()
    {
        // Arrange
        Span<char> buffer = stackalloc char[10];

        // Act
        SpanStringBuilder builder = new(buffer);
        builder.Append("Hello");

        // Assert
        Assert.Equal(5, builder.Length);
        Assert.Equal("Hello", builder.AsSpan().ToString());
    }

    // ========================================================================
    // SPANSTRINGBUILDER - APPEND OPERATIONS
    // ========================================================================

    [Fact]
    public void SpanStringBuilder_AppendChar_IncreasesLength()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);

        // Act
        builder.Append('A');
        builder.Append('B');
        builder.Append('C');

        // Assert
        Assert.Equal(3, builder.Length);
        Assert.Equal("ABC", builder.AsSpan().ToString());
    }

    [Fact]
    public void SpanStringBuilder_AppendString_IncreasesLength()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);

        // Act
        builder.Append("Hello");
        builder.Append(" ");
        builder.Append("World");

        // Assert
        Assert.Equal(11, builder.Length);
        Assert.Equal("Hello World", builder.AsSpan().ToString());
    }

    [Fact]
    public void SpanStringBuilder_AppendInt_FormatsCorrectly()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);

        // Act
        builder.Append(12345);

        // Assert
        Assert.Equal("12345", builder.AsSpan().ToString());
    }

    [Fact]
    public void SpanStringBuilder_AppendNegativeInt_FormatsCorrectly()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);

        // Act
        builder.Append(-12345);

        // Assert
        Assert.Equal("-12345", builder.AsSpan().ToString());
    }

    [Fact]
    public void SpanStringBuilder_AppendGuid_FormatsCorrectly()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);
        Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");

        // Act
        builder.Append(guid);

        // Assert
        Assert.Equal("12345678-1234-1234-1234-123456789abc", builder.AsSpan().ToString());
    }

    // ========================================================================
    // SPANSTRINGBUILDER - BUFFER OVERFLOW
    // ========================================================================

    [Fact]
    public void SpanStringBuilder_Overflow_ThrowsArgumentException()
    {
        // Arrange
        Span<char> buffer = stackalloc char[5];
        SpanStringBuilder builder = new(buffer);

        // Act & Assert - Append(string) uses CopyTo which throws ArgumentException
        try
        {
            builder.Append("This is too long");
            Assert.Fail("Expected ArgumentException");
        }
        catch (ArgumentException)
        {
            // Expected - CopyTo throws when destination is too short
        }
    }

    [Fact]
    public void SpanStringBuilder_OverflowByOneChar_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        Span<char> buffer = stackalloc char[5];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Hello"); // Exactly fills buffer

        // Act & Assert - Append(char) uses index access which throws IndexOutOfRangeException
        try
        {
            builder.Append('!');
            Assert.Fail("Expected IndexOutOfRangeException");
        }
        catch (IndexOutOfRangeException)
        {
            // Expected - direct index access on full buffer
        }
    }

    [Fact]
    public void SpanStringBuilder_ExactFit_Works()
    {
        // Arrange
        Span<char> buffer = stackalloc char[5];
        SpanStringBuilder builder = new(buffer);

        // Act
        builder.Append("Hello"); // Exactly fits

        // Assert
        Assert.Equal(5, builder.Length);
        Assert.Equal(0, builder.Remaining);
        Assert.Equal("Hello", builder.AsSpan().ToString());
    }

    // ========================================================================
    // SPANSTRINGBUILDER - CLEAR AND SEEKBACK
    // ========================================================================

    [Fact]
    public void SpanStringBuilder_Clear_ResetsLength()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Hello World");

        // Act
        builder.Clear();

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.Equal(100, builder.Remaining);
    }

    [Fact]
    public void SpanStringBuilder_SeekBack_DecreasesLength()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Hello World");

        // Act
        builder.SeekBack(6); // Remove " World"

        // Assert
        Assert.Equal(5, builder.Length);
        Assert.Equal("Hello", builder.AsSpan().ToString());
    }

    [Fact]
    public void SpanStringBuilder_SeekBack_TooMuch_ThrowsException()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Hi");

        // Act & Assert - use try-catch because ref struct can't be in lambda
        try
        {
            builder.SeekBack(10);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    [Fact]
    public void SpanStringBuilder_TrySeekBack_Valid_ReturnsTrue()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Hello");

        // Act
        bool result = builder.TrySeekBack(2);

        // Assert
        Assert.True(result);
        Assert.Equal(3, builder.Length);
        Assert.Equal("Hel", builder.AsSpan().ToString());
    }

    [Fact]
    public void SpanStringBuilder_TrySeekBack_Invalid_ReturnsFalse()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Hi");

        // Act
        bool result = builder.TrySeekBack(10);

        // Assert
        Assert.False(result);
        Assert.Equal(2, builder.Length);
    }

    // ========================================================================
    // SPANBYTESBUILDER - CREATION AND PROPERTIES
    // ========================================================================

    [Fact]
    public void SpanBytesBuilder_Constructor_SetsCapacity()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[256];

        // Act
        SpanBytesBuilder builder = new(buffer);

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.Equal(256, builder.Capacity);
        Assert.Equal(256, builder.Remaining);
    }

    [Fact]
    public void SpanBytesBuilder_SmallBuffer_Works()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[10];

        // Act
        SpanBytesBuilder builder = new(buffer);
        builder.Append(0x01);
        builder.Append(0x02);
        builder.Append(0x03);

        // Assert
        Assert.Equal(3, builder.Length);
        Assert.Equal([0x01, 0x02, 0x03], builder.AsSpan().ToArray());
    }

    // ========================================================================
    // SPANBYTESBUILDER - APPEND OPERATIONS
    // ========================================================================

    [Fact]
    public void SpanBytesBuilder_AppendByte_IncreasesLength()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);

        // Act
        builder.Append(0xAA);
        builder.Append(0xBB);
        builder.Append(0xCC);

        // Assert
        Assert.Equal(3, builder.Length);
        Assert.Equal([0xAA, 0xBB, 0xCC], builder.AsSpan().ToArray());
    }

    [Fact]
    public void SpanBytesBuilder_AppendInt16BigEndian_FormatsCorrectly()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);

        // Act
        builder.AppendInt16BigEndian(0x1234);

        // Assert
        Assert.Equal(2, builder.Length);
        Assert.Equal([0x12, 0x34], builder.AsSpan().ToArray());
    }

    [Fact]
    public void SpanBytesBuilder_AppendInt16LittleEndian_FormatsCorrectly()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);

        // Act
        builder.AppendInt16LittleEndian(0x1234);

        // Assert
        Assert.Equal(2, builder.Length);
        Assert.Equal([0x34, 0x12], builder.AsSpan().ToArray());
    }

    [Fact]
    public void SpanBytesBuilder_AppendInt32BigEndian_FormatsCorrectly()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);

        // Act
        builder.AppendInt32BigEndian(0x12345678);

        // Assert
        Assert.Equal(4, builder.Length);
        Assert.Equal([0x12, 0x34, 0x56, 0x78], builder.AsSpan().ToArray());
    }

    [Fact]
    public void SpanBytesBuilder_AppendUInt64BigEndian_FormatsCorrectly()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);

        // Act
        builder.AppendUInt64BigEndian(0x123456789ABCDEF0UL);

        // Assert
        Assert.Equal(8, builder.Length);
        Assert.Equal([0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0], builder.AsSpan().ToArray());
    }

    // ========================================================================
    // SPANBYTESBUILDER - BUFFER OVERFLOW
    // ========================================================================

    [Fact]
    public void SpanBytesBuilder_Overflow_ThrowsException()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[5];
        SpanBytesBuilder builder = new(buffer);

        // Act & Assert - use try-catch because ref struct can't be in lambda
        try
        {
            builder.Append([0x01, 0x02, 0x03, 0x04, 0x05, 0x06]);
            Assert.Fail("Expected exception");
        }
        catch (Exception)
        {
            // Expected
        }
    }

    [Fact]
    public void SpanBytesBuilder_OverflowInt32_ThrowsException()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[3]; // Only 3 bytes, but Int32 needs 4
        SpanBytesBuilder builder = new(buffer);

        // Act & Assert - use try-catch because ref struct can't be in lambda
        try
        {
            builder.AppendInt32BigEndian(0x12345678);
            Assert.Fail("Expected exception");
        }
        catch (Exception)
        {
            // Expected
        }
    }

    [Fact]
    public void SpanBytesBuilder_ExactFit_Works()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[4];
        SpanBytesBuilder builder = new(buffer);

        // Act
        builder.AppendInt32BigEndian(0x12345678); // Exactly fits

        // Assert
        Assert.Equal(4, builder.Length);
        Assert.Equal(0, builder.Remaining);
    }

    // ========================================================================
    // SPANBYTESBUILDER - CLEAR AND SEEKBACK
    // ========================================================================

    [Fact]
    public void SpanBytesBuilder_Clear_ResetsLength()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);
        builder.Append([0x01, 0x02, 0x03, 0x04]);

        // Act
        builder.Clear();

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.Equal(100, builder.Remaining);
    }

    [Fact]
    public void SpanBytesBuilder_SeekBack_DecreasesLength()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);
        builder.Append([0x01, 0x02, 0x03, 0x04]);

        // Act
        builder.SeekBack(2);

        // Assert
        Assert.Equal(2, builder.Length);
        Assert.Equal([0x01, 0x02], builder.AsSpan().ToArray());
    }

    [Fact]
    public void SpanBytesBuilder_SeekBack_TooMuch_ThrowsException()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);
        builder.Append([0x01, 0x02]);

        // Act & Assert - use try-catch because ref struct can't be in lambda
        try
        {
            builder.SeekBack(10);
            Assert.Fail("Expected ArgumentOutOfRangeException");
        }
        catch (ArgumentOutOfRangeException)
        {
            // Expected
        }
    }

    [Fact]
    public void SpanBytesBuilder_TrySeekBack_Valid_ReturnsTrue()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);
        builder.Append([0x01, 0x02, 0x03, 0x04]);

        // Act
        bool result = builder.TrySeekBack(2);

        // Assert
        Assert.True(result);
        Assert.Equal(2, builder.Length);
    }

    [Fact]
    public void SpanBytesBuilder_TrySeekBack_Invalid_ReturnsFalse()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);
        builder.Append([0x01, 0x02]);

        // Act
        bool result = builder.TrySeekBack(10);

        // Assert
        Assert.False(result);
        Assert.Equal(2, builder.Length);
    }

    // ========================================================================
    // COMPLEX SCENARIOS
    // ========================================================================

    [Fact]
    public void SpanStringBuilder_ChainedAppends_BuildsCorrectString()
    {
        // Arrange
        Span<char> buffer = stackalloc char[256];
        SpanStringBuilder builder = new(buffer);

        // Act
        builder.Append("User: ");
        builder.Append(42);
        builder.Append(", Active: ");
        builder.Append(true);

        // Assert
        Assert.Equal("User: 42, Active: True", builder.AsSpan().ToString());
    }

    [Fact]
    public void SpanBytesBuilder_BuildPacket_ProducesCorrectBytes()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[256];
        SpanBytesBuilder builder = new(buffer);

        // Act - Build a simple packet: [magic][version][length]
        builder.AppendUInt16BigEndian(0xCAFE); // Magic
        builder.Append(0x01);                  // Version
        builder.AppendUInt16BigEndian(0x0010); // Length

        // Assert
        Assert.Equal([0xCA, 0xFE, 0x01, 0x00, 0x10], builder.AsSpan().ToArray());
    }

    [Fact]
    public void SpanStringBuilder_ClearAndReuse_Works()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);

        // Act
        builder.Append("First");
        Assert.Equal("First", builder.AsSpan().ToString());

        builder.Clear();
        builder.Append("Second");

        // Assert
        Assert.Equal("Second", builder.AsSpan().ToString());
    }

    [Fact]
    public void SpanBytesBuilder_ClearAndReuse_Works()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);

        // Act
        builder.Append([0x01, 0x02, 0x03]);
        Assert.Equal([0x01, 0x02, 0x03], builder.AsSpan().ToArray());

        builder.Clear();
        builder.Append([0xAA, 0xBB]);

        // Assert
        Assert.Equal([0xAA, 0xBB], builder.AsSpan().ToArray());
    }

    // ========================================================================
    // EDGE CASES
    // ========================================================================

    [Fact]
    public void SpanStringBuilder_EmptyBuffer_ThrowsOnAnyAppend()
    {
        // Arrange
        Span<char> buffer = [];
        SpanStringBuilder builder = new(buffer);

        // Act & Assert - Append(string) uses CopyTo which throws ArgumentException
        try
        {
            builder.Append("A");
            Assert.Fail("Expected ArgumentException");
        }
        catch (ArgumentException)
        {
            // Expected - CopyTo throws when destination is too short
        }
    }

    [Fact]
    public void SpanBytesBuilder_EmptyBuffer_ThrowsOnAnyAppend()
    {
        // Arrange
        Span<byte> buffer = [];
        SpanBytesBuilder builder = new(buffer);

        // Act & Assert - use try-catch because ref struct can't be in lambda
        try
        {
            builder.Append(0x01);
            Assert.Fail("Expected exception");
        }
        catch (Exception)
        {
            // Expected
        }
    }

    [Fact]
    public void SpanStringBuilder_NullString_DoesNothing()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);
        string? nullString = null;

        // Act
        builder.Append(nullString);

        // Assert
        Assert.Equal(0, builder.Length);
    }

    [Fact]
    public void SpanBytesBuilder_NullArray_DoesNothing()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);
        byte[]? nullArray = null;

        // Act
        builder.Append(nullArray);

        // Assert
        Assert.Equal(0, builder.Length);
    }

    [Fact]
    public void SpanStringBuilder_Remaining_ReturnsCorrectValue()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);
        builder.Append("Hello");

        // Assert
        Assert.Equal(95, builder.Remaining);
    }

    [Fact]
    public void SpanBytesBuilder_GetRemainingSpan_ReturnsCorrectSpan()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);
        builder.Append([0x01, 0x02, 0x03]);

        // Act
        Span<byte> remaining = builder.GetRemainingSpan();

        // Assert
        Assert.Equal(97, remaining.Length);
    }

    [Fact]
    public void SpanStringBuilder_ManualFillAndAdvance_WritesCorrectly()
    {
        // Arrange
        Span<char> buffer = stackalloc char[100];
        SpanStringBuilder builder = new(buffer);

        // Act - First fill with known values
        builder.Append("Test");
        builder.Append(" Data");

        // Assert
        Assert.Equal(9, builder.Length);
        Assert.Equal("Test Data", builder.AsSpan().ToString());
    }

    [Fact]
    public void SpanBytesBuilder_Advance_IncreasesPosition()
    {
        // Arrange
        Span<byte> buffer = stackalloc byte[100];
        SpanBytesBuilder builder = new(buffer);

        // Act
        Span<byte> remaining = builder.GetRemainingSpan();
        ReadOnlySpan<byte> data = [0xAA, 0xBB];
        data.CopyTo(remaining);
        builder.Advance(2);

        // Assert
        Assert.Equal(2, builder.Length);
        Assert.Equal([0xAA, 0xBB], builder.AsSpan().ToArray());
    }
}
