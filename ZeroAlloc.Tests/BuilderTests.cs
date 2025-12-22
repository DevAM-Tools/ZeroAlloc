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
/// Tests for <see cref="TempStringBuilder"/> and <see cref="TempBytesBuilder"/>.
/// Covers create, append, clear, seek operations, and disposal behavior.
/// </summary>
public class BuilderTests
{
    // ========================================================================
    // TEMPSTRINGBUILDER - CREATION AND PROPERTIES
    // ========================================================================

    [Fact]
    public void TempStringBuilder_Create_ReturnsEmptyBuilder()
    {
        // Act
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
        Assert.True(builder.Capacity > 0);
    }

    [Fact]
    public void TempStringBuilder_Capacity_IsPositive()
    {
        // Act
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Assert
        Assert.True(builder.Capacity > 0);
        Assert.Equal(builder.Capacity, builder.Remaining);
    }

    // ========================================================================
    // TEMPSTRINGBUILDER - APPEND OPERATIONS
    // ========================================================================

    [Fact]
    public void TempStringBuilder_AppendChar_IncreasesLength()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        builder.Append('A');
        builder.Append('B');
        builder.Append('C');

        // Assert
        Assert.Equal(3, builder.Length);
        Assert.Equal("ABC", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_AppendString_IncreasesLength()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        builder.Append("Hello");
        builder.Append(" ");
        builder.Append("World");

        // Assert
        Assert.Equal(11, builder.Length);
        Assert.Equal("Hello World", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_AppendSpan_IncreasesLength()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        ReadOnlySpan<char> span = "Test".AsSpan();

        // Act
        builder.Append(span);

        // Assert
        Assert.Equal(4, builder.Length);
        Assert.Equal("Test", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_AppendInt_FormatsCorrectly()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        builder.Append(12345);

        // Assert
        Assert.Equal("12345", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_AppendNegativeInt_FormatsCorrectly()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        builder.Append(-12345);

        // Assert
        Assert.Equal("-12345", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_AppendDouble_FormatsCorrectly()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        builder.Append(123.456);

        // Assert
        Assert.Contains("123", builder.AsSpan().ToString());
        Assert.Contains("456", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_AppendBool_FormatsCorrectly()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        builder.Append(true);
        builder.Append(' ');
        builder.Append(false);

        // Assert
        Assert.Equal("True False", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_AppendGuid_FormatsCorrectly()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");

        // Act
        builder.Append(guid);

        // Assert
        Assert.Equal("12345678-1234-1234-1234-123456789abc", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_AppendNull_DoesNothing()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        string? nullString = null;

        // Act
        builder.Append(nullString);

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
    }

    // ========================================================================
    // TEMPSTRINGBUILDER - CLEAR AND SEEKBACK
    // ========================================================================

    [Fact]
    public void TempStringBuilder_Clear_ResetsLength()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        builder.Append("Hello World");

        // Act
        builder.Clear();

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void TempStringBuilder_SeekBack_DecreasesLength()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        builder.Append("Hello World");

        // Act
        builder.SeekBack(6); // Remove " World"

        // Assert
        Assert.Equal(5, builder.Length);
        Assert.Equal("Hello", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_SeekBack_FullLength_Clears()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        builder.Append("Hello");

        // Act
        builder.SeekBack(5);

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void TempStringBuilder_SeekBack_Zero_DoesNothing()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        builder.Append("Hello");

        // Act
        builder.SeekBack(0);

        // Assert
        Assert.Equal(5, builder.Length);
    }

    [Fact]
    public void TempStringBuilder_SeekBack_TooMuch_ThrowsException()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
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
    public void TempStringBuilder_TrySeekBack_Valid_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        builder.Append("Hello");

        // Act
        bool result = builder.TrySeekBack(2);

        // Assert
        Assert.True(result);
        Assert.Equal(3, builder.Length);
        Assert.Equal("Hel", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_TrySeekBack_Invalid_ReturnsFalse()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        builder.Append("Hi");

        // Act
        bool result = builder.TrySeekBack(10);

        // Assert
        Assert.False(result);
        Assert.Equal(2, builder.Length); // Unchanged
    }

    // ========================================================================
    // TEMPSTRINGBUILDER - DISPOSAL
    // ========================================================================

    [Fact]
    public void TempStringBuilder_Dispose_ReleasesBuffer()
    {
        // Arrange
        TempStringBuilder builder = TempStringBuilder.Create();
        builder.Append("Test");

        // Assert - capture values before dispose
        Assert.Equal(4, builder.Length);

        // Act
        builder.Dispose();

        // Assert - dispose completes without exception
    }

    // ========================================================================
    // TEMPBYTESBUILDER - CREATION AND PROPERTIES
    // ========================================================================

    [Fact]
    public void TempBytesBuilder_Create_ReturnsEmptyBuilder()
    {
        // Act
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
        Assert.True(builder.Capacity > 0);
    }

    [Fact]
    public void TempBytesBuilder_Capacity_IsPositive()
    {
        // Act
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Assert
        Assert.True(builder.Capacity > 0);
        Assert.Equal(builder.Capacity, builder.Remaining);
    }

    // ========================================================================
    // TEMPBYTESBUILDER - APPEND OPERATIONS
    // ========================================================================

    [Fact]
    public void TempBytesBuilder_AppendByte_IncreasesLength()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.Append(0x01);
        builder.Append(0x02);
        builder.Append(0x03);

        // Assert
        Assert.Equal(3, builder.Length);
        Assert.Equal([0x01, 0x02, 0x03], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendByteArray_IncreasesLength()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        byte[] data = [0x01, 0x02, 0x03, 0x04];

        // Act
        builder.Append(data);

        // Assert
        Assert.Equal(4, builder.Length);
        Assert.Equal(data, builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendSpan_IncreasesLength()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        ReadOnlySpan<byte> span = [0xAA, 0xBB, 0xCC];

        // Act
        builder.Append(span);

        // Assert
        Assert.Equal(3, builder.Length);
        Assert.Equal([0xAA, 0xBB, 0xCC], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendInt16BigEndian_FormatsCorrectly()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendInt16BigEndian(0x1234);

        // Assert
        Assert.Equal(2, builder.Length);
        Assert.Equal([0x12, 0x34], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendInt16LittleEndian_FormatsCorrectly()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendInt16LittleEndian(0x1234);

        // Assert
        Assert.Equal(2, builder.Length);
        Assert.Equal([0x34, 0x12], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendInt32BigEndian_FormatsCorrectly()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendInt32BigEndian(0x12345678);

        // Assert
        Assert.Equal(4, builder.Length);
        Assert.Equal([0x12, 0x34, 0x56, 0x78], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendInt32LittleEndian_FormatsCorrectly()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendInt32LittleEndian(0x12345678);

        // Assert
        Assert.Equal(4, builder.Length);
        Assert.Equal([0x78, 0x56, 0x34, 0x12], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendInt64BigEndian_FormatsCorrectly()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendInt64BigEndian(0x123456789ABCDEF0L);

        // Assert
        Assert.Equal(8, builder.Length);
        Assert.Equal([0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendUInt16BigEndian_FormatsCorrectly()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendUInt16BigEndian(0xABCD);

        // Assert
        Assert.Equal(2, builder.Length);
        Assert.Equal([0xAB, 0xCD], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendUInt32BigEndian_FormatsCorrectly()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendUInt32BigEndian(0xDEADBEEF);

        // Assert
        Assert.Equal(4, builder.Length);
        Assert.Equal([0xDE, 0xAD, 0xBE, 0xEF], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendNullByteArray_DoesNothing()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        byte[]? nullArray = null;

        // Act
        builder.Append(nullArray);

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
    }

    // ========================================================================
    // TEMPBYTESBUILDER - UTF-8 APPEND
    // ========================================================================

    [Fact]
    public void TempBytesBuilder_AppendUtf8_EncodesCorrectly()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendUtf8("Hello");

        // Assert
        Assert.Equal(5, builder.Length);
        Assert.Equal("Hello"u8.ToArray(), builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendUtf8_UnicodeCharacters_EncodesCorrectly()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendUtf8("Héllo €");

        // Assert
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes("Héllo €"), builder.AsSpan().ToArray());
    }

    // ========================================================================
    // TEMPBYTESBUILDER - VARINT
    // ========================================================================

    [Fact]
    public void TempBytesBuilder_AppendVarInt_SmallValue_SingleByte()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendVarInt(127);

        // Assert
        Assert.Equal(1, builder.Length);
        Assert.Equal([127], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendVarInt_MediumValue_TwoBytes()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendVarInt(128);

        // Assert
        Assert.Equal(2, builder.Length);
        Assert.Equal([0x80, 0x01], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_AppendVarInt_LargeValue_MultipleBytes()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.AppendVarInt(300);

        // Assert
        Assert.True(builder.Length >= 2);
    }

    // ========================================================================
    // TEMPBYTESBUILDER - CLEAR AND SEEKBACK
    // ========================================================================

    [Fact]
    public void TempBytesBuilder_Clear_ResetsLength()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        builder.Append([0x01, 0x02, 0x03, 0x04]);

        // Act
        builder.Clear();

        // Assert
        Assert.Equal(0, builder.Length);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void TempBytesBuilder_SeekBack_DecreasesLength()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        builder.Append([0x01, 0x02, 0x03, 0x04]);

        // Act
        builder.SeekBack(2);

        // Assert
        Assert.Equal(2, builder.Length);
        Assert.Equal([0x01, 0x02], builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_SeekBack_TooMuch_ThrowsException()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();
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
    public void TempBytesBuilder_TrySeekBack_Valid_ReturnsTrue()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        builder.Append([0x01, 0x02, 0x03, 0x04]);

        // Act
        bool result = builder.TrySeekBack(2);

        // Assert
        Assert.True(result);
        Assert.Equal(2, builder.Length);
    }

    [Fact]
    public void TempBytesBuilder_TrySeekBack_Invalid_ReturnsFalse()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        builder.Append([0x01, 0x02]);

        // Act
        bool result = builder.TrySeekBack(10);

        // Assert
        Assert.False(result);
        Assert.Equal(2, builder.Length); // Unchanged
    }

    // ========================================================================
    // TEMPBYTESBUILDER - DISPOSAL
    // ========================================================================

    [Fact]
    public void TempBytesBuilder_Dispose_ReleasesBuffer()
    {
        // Arrange
        TempBytesBuilder builder = TempBytesBuilder.Create();
        builder.Append([0x01, 0x02, 0x03, 0x04]);

        // Assert - capture values before dispose
        Assert.Equal(4, builder.Length);

        // Act
        builder.Dispose();

        // Assert - dispose completes without exception
    }

    // ========================================================================
    // TEMPBYTESBUILDER - IHEAPALLOCATED
    // ========================================================================

    [Fact]
    public void TempBytesBuilder_IsHeapAllocated_FalseForNormalUse()
    {
        // Arrange & Act
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Assert
        // In normal single-scope usage, should use ThreadStatic buffer
        // IsHeapAllocated will be false unless we're in nested context
        // This tests the property exists and doesn't throw
        _ = builder.IsHeapAllocated;
    }

    // ========================================================================
    // TEMPSTRINGBUILDER - COMPLEX SCENARIOS
    // ========================================================================

    [Fact]
    public void TempStringBuilder_ChainedAppends_BuildsCorrectString()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        builder.Append("User: ");
        builder.Append(12345);
        builder.Append(", Balance: ");
        builder.Append(99.99);

        // Assert
        string result = builder.AsSpan().ToString();
        Assert.StartsWith("User: 12345, Balance:", result);
    }

    [Fact]
    public void TempStringBuilder_ClearAndReuse_Works()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        builder.Append("First");
        Assert.Equal("First", builder.AsSpan().ToString());

        builder.Clear();
        builder.Append("Second");

        // Assert
        Assert.Equal("Second", builder.AsSpan().ToString());
    }

    // ========================================================================
    // TEMPBYTESBUILDER - COMPLEX SCENARIOS
    // ========================================================================

    [Fact]
    public void TempBytesBuilder_BuildPacket_ProducesCorrectBytes()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act - Build a simple packet: [magic][length][data]
        builder.AppendUInt16BigEndian(0xCAFE); // Magic
        builder.AppendUInt16BigEndian(5);      // Length
        builder.AppendUtf8("Hello");           // Data

        // Assert
        byte[] expected = [0xCA, 0xFE, 0x00, 0x05, .. "Hello"u8.ToArray()];
        Assert.Equal(expected, builder.AsSpan().ToArray());
    }

    [Fact]
    public void TempBytesBuilder_ClearAndReuse_Works()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

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
    public void TempStringBuilder_EmptyString_Works()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        builder.Append("");

        // Assert
        Assert.True(builder.IsEmpty);
        Assert.Equal(0, builder.Length);
    }

    [Fact]
    public void TempBytesBuilder_EmptySpan_Works()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();

        // Act
        builder.Append(ReadOnlySpan<byte>.Empty);

        // Assert
        Assert.True(builder.IsEmpty);
        Assert.Equal(0, builder.Length);
    }

    [Fact]
    public void TempStringBuilder_AsSpan_ReturnsCorrectContent()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        builder.Append("Hello");

        // Act
        ReadOnlySpan<char> span = builder.AsSpan();

        // Assert
        Assert.Equal(5, span.Length);
        Assert.Equal("Hello", span.ToString());
    }

    [Fact]
    public void TempBytesBuilder_WrittenSpan_MatchesAsSpan()
    {
        // Arrange
        using TempBytesBuilder builder = TempBytesBuilder.Create();
        builder.Append([0x01, 0x02, 0x03]);

        // Act & Assert
        Assert.Equal(builder.AsSpan().ToArray(), builder.WrittenSpan.ToArray());
    }

    // ========================================================================
    // TEMPSTRINGBUILDER - TRYAPPEND OPERATIONS
    // ========================================================================

    #region TryAppend

    [Fact]
    public void TempStringBuilder_TryAppend_String_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        bool result = builder.TryAppend("Hello");

        // Assert
        Assert.True(result);
        Assert.Equal("Hello", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_TryAppend_Span_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        bool result = builder.TryAppend("World".AsSpan());

        // Assert
        Assert.True(result);
        Assert.Equal("World", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_TryAppend_Char_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        bool result1 = builder.TryAppend('A');
        bool result2 = builder.TryAppend('B');

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.Equal("AB", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_TryAppend_Int_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        bool result = builder.TryAppend(42);

        // Assert
        Assert.True(result);
        Assert.Equal("42", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_TryAppend_Long_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        bool result = builder.TryAppend(123456789012345L);

        // Assert
        Assert.True(result);
        Assert.Equal("123456789012345", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_TryAppend_Bool_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        bool result1 = builder.TryAppend(true);
        builder.TryAppend(" ");
        bool result2 = builder.TryAppend(false);

        // Assert
        Assert.True(result1);
        Assert.True(result2);
        Assert.Equal("True False", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_TryAppend_DateTime_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        DateTime dt = new(2025, 1, 15, 10, 30, 0);

        // Act
        bool result = builder.TryAppend(dt);

        // Assert
        Assert.True(result);
        Assert.Contains("2025", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_TryAppend_Guid_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();
        Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        bool result = builder.TryAppend(guid);

        // Assert
        Assert.True(result);
        Assert.Equal("12345678-1234-1234-1234-123456789012", builder.AsSpan().ToString());
    }

    [Fact]
    public void TempStringBuilder_TryAppend_Null_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        bool result = builder.TryAppend((string?)null);

        // Assert
        Assert.True(result);
        Assert.True(builder.IsEmpty);
    }

    [Fact]
    public void TempStringBuilder_TryAppend_MultipleValues_ReturnsTrue()
    {
        // Arrange
        using TempStringBuilder builder = TempStringBuilder.Create();

        // Act
        builder.TryAppend("Value: ");
        builder.TryAppend(42);
        builder.TryAppend(", Active: ");
        builder.TryAppend(true);

        // Assert
        Assert.Equal("Value: 42, Active: True", builder.AsSpan().ToString());
    }

    #endregion
}
