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
/// Tests for implicit conversion operators on Temp* types.
/// Validates that implicit conversions work correctly and auto-dispose.
/// </summary>
public class ImplicitOperatorTests
{
    // ========================================================================
    // TEMPSTRING - IMPLICIT TO STRING
    // ========================================================================

    [Fact]
    public void TempString_ImplicitToString_ReturnsCorrectValue()
    {
        // Act - implicit conversion to string
        string result = ZA.String($"Hello World");

        // Assert
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void TempString_ImplicitToString_WithInterpolation_ReturnsCorrectValue()
    {
        // Arrange
        int value = 42;
        string name = "Test";

        // Act - implicit conversion with interpolation
        string result = ZA.String($"Name: {name}, Value: {value}");

        // Assert
        Assert.Equal("Name: Test, Value: 42", result);
    }

    [Fact]
    public void TempString_ImplicitToString_FormattedNumbers_ReturnsCorrectValue()
    {
        // Arrange - Use non-interpolated call to explicitly control culture
        CultureInfo invariant = CultureInfo.InvariantCulture;
        decimal price = 1234.56m;

        // Act - Use ZA.String with explicit culture to ensure invariant formatting
        string result = ZA.String(invariant, "Price: ", price);

        // Assert - Invariant culture uses period and comma as thousand separator
        Assert.Equal("Price: 1234.56", result);
    }

    [Fact]
    public void TempString_ImplicitToString_EmptyInterpolation_ReturnsEmpty()
    {
        // Act
        string result = ZA.String($"");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void TempString_ImplicitToString_LargeContent_Works()
    {
        // Arrange
        string largeContent = new('X', 1000);

        // Act
        string result = ZA.String($"{largeContent}");

        // Assert
        Assert.Equal(largeContent, result);
    }

    // ========================================================================
    // TEMPSTRING - ASSIGNMENT TO STRING VARIABLE
    // ========================================================================

    [Fact]
    public void TempString_AssignToVariable_Works()
    {
        // Arrange
        int count = 5;

        // Act
        string message = ZA.String($"Count: {count}");

        // Assert
        Assert.Equal("Count: 5", message);
    }

    [Fact]
    public void TempString_PassToMethod_Works()
    {
        // Arrange
        static void AssertMessage(string msg) => Assert.Equal("Test Message", msg);

        // Act - TempString is implicitly converted when passed to method
        AssertMessage(ZA.String($"Test Message"));
    }

    // ========================================================================
    // TEMPBYTES - IMPLICIT TO BYTE ARRAY
    // ========================================================================

    [Fact]
    public void TempBytes_ImplicitToByteArray_ReturnsCorrectValue()
    {
        // Act - implicit conversion to byte[]
        byte[] result = ZA.Utf8($"Hello");

        // Assert
        Assert.Equal("Hello"u8.ToArray(), result);
    }

    [Fact]
    public void TempBytes_ImplicitToByteArray_WithInterpolation_ReturnsCorrectValue()
    {
        // Arrange
        int value = 42;

        // Act
        byte[] result = ZA.Utf8($"Value: {value}");

        // Assert
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes("Value: 42"), result);
    }

    [Fact]
    public void TempBytes_ImplicitToByteArray_Unicode_ReturnsCorrectValue()
    {
        // Act
        byte[] result = ZA.Utf8($"Héllo €100");

        // Assert
        Assert.Equal(System.Text.Encoding.UTF8.GetBytes("Héllo €100"), result);
    }

    [Fact]
    public void TempBytes_ImplicitToByteArray_EmptyInterpolation_ReturnsEmpty()
    {
        // Act
        byte[] result = ZA.Utf8($"");

        // Assert
        Assert.Empty(result);
    }

    // ========================================================================
    // TEMPBYTES - ASSIGNMENT TO VARIABLE
    // ========================================================================

    [Fact]
    public void TempBytes_AssignToVariable_Works()
    {
        // Arrange
        int id = 123;

        // Act
        byte[] data = ZA.Utf8($"ID={id}");

        // Assert
        Assert.Equal("ID=123"u8.ToArray(), data);
    }

    [Fact]
    public void TempBytes_PassToMethod_Works()
    {
        // Arrange
        static void AssertData(byte[] d) => Assert.Equal("Test"u8.ToArray(), d);

        // Act - TempBytes is implicitly converted when passed to method
        AssertData(ZA.Utf8($"Test"));
    }

    // ========================================================================
    // TEMPSTRING WITH CULTURE - IMPLICIT TO STRING
    // Uses ZA.String(culture, ...) which doesn't require generator overloads
    // ========================================================================

    [Fact]
    public void TempString_ImplicitToString_German_ReturnsCorrectValue()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double value = 1234.56;

        // Act - implicit conversion with culture
        string result = ZA.String(german, "Preis: ", value);

        // Assert - German uses comma as decimal separator
        Assert.Equal("Preis: 1234,56", result);
    }

    [Fact]
    public void TempString_ImplicitToString_English_ReturnsCorrectValue()
    {
        // Arrange
        CultureInfo english = CultureInfo.GetCultureInfo("en-US");
        double value = 1234.56;

        // Act - implicit conversion with culture
        string result = ZA.String(english, "Price: ", value);

        // Assert - English uses period as decimal separator
        Assert.Equal("Price: 1234.56", result);
    }

    [Fact]
    public void TempString_WithCulture_PassToMethod_Works()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;
        static void AssertMessage(string msg) => Assert.Equal("Test: 42", msg);

        // Act
        AssertMessage(ZA.String(culture, "Test: ", 42));
    }

    // ========================================================================
    // TEMPBYTES (UTF8) - IMPLICIT TO BYTE ARRAY
    // Note: ZA.Utf8() does not have culture-aware overloads
    // ========================================================================

    [Fact]
    public void TempBytes_ImplicitToByteArray_SimpleValue_ReturnsCorrectValue()
    {
        // Arrange
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("Value: 42");

        // Act - implicit conversion
        byte[] result = ZA.Utf8("Value: ", 42);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TempBytes_ImplicitToByteArray_WithUnicode_ReturnsCorrectValue()
    {
        // Arrange
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("Price: €100");

        // Act - implicit conversion
        byte[] result = ZA.Utf8("Price: €", 100);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TempBytes_PassToMethodWithNumber_Works()
    {
        // Arrange
        static void AssertData(byte[] d) => Assert.Equal("42"u8.ToArray(), d);

        // Act
        AssertData(ZA.Utf8(42));
    }

    // ========================================================================
    // MULTIPLE CONVERSIONS IN SEQUENCE
    // ========================================================================

    [Fact]
    public void ImplicitOperators_MultipleConversionsInSequence_Work()
    {
        // Act - Multiple implicit conversions in sequence
        string s1 = ZA.String($"First");
        string s2 = ZA.String($"Second");
        string s3 = ZA.String($"Third");

        // Assert
        Assert.Equal("First", s1);
        Assert.Equal("Second", s2);
        Assert.Equal("Third", s3);
    }

    [Fact]
    public void ImplicitOperators_MixedTypes_Work()
    {
        // Act
        string str = ZA.String($"Hello");
        byte[] bytes = ZA.Utf8($"World");
        string cultureStr = ZA.String(CultureInfo.InvariantCulture, "Test: ", 42);
        byte[] cultureBytes = ZA.Bytes(CultureInfo.InvariantCulture, "Data: ", 123);

        // Assert
        Assert.Equal("Hello", str);
        Assert.Equal("World"u8.ToArray(), bytes);
        Assert.Equal("Test: 42", cultureStr);
        Assert.Equal("Data: 123"u8.ToArray(), cultureBytes);
    }

    // ========================================================================
    // IMPLICIT IN COLLECTIONS/EXPRESSIONS
    // ========================================================================

    [Fact]
    public void TempString_ImplicitInListInitializer_Works()
    {
        // Act
        List<string> messages =
        [
            ZA.String($"Message 1"),
            ZA.String($"Message 2"),
            ZA.String($"Message 3")
        ];

        // Assert
        Assert.Equal(3, messages.Count);
        Assert.Equal("Message 1", messages[0]);
        Assert.Equal("Message 2", messages[1]);
        Assert.Equal("Message 3", messages[2]);
    }

    [Fact]
    public void TempBytes_ImplicitInArrayInitializer_Works()
    {
        // Act
        byte[][] dataArray =
        [
            ZA.Utf8($"First"),
            ZA.Utf8($"Second"),
            ZA.Utf8($"Third")
        ];

        // Assert
        Assert.Equal(3, dataArray.Length);
        Assert.Equal("First"u8.ToArray(), dataArray[0]);
        Assert.Equal("Second"u8.ToArray(), dataArray[1]);
        Assert.Equal("Third"u8.ToArray(), dataArray[2]);
    }

    // ========================================================================
    // IMPLICIT IN STRING OPERATIONS
    // ========================================================================

    [Fact]
    public void TempString_ImplicitInStringConcat_Works()
    {
        // Arrange
        int id = 123;

        // Act
        string result = "Prefix: " + ZA.String($"ID={id}") + " :Suffix";

        // Assert
        Assert.Equal("Prefix: ID=123 :Suffix", result);
    }

    [Fact]
    public void TempString_ImplicitInStringEquals_Works()
    {
        // Act
        bool isEqual = ZA.String($"Hello") == "Hello";

        // Assert
        Assert.True(isEqual);
    }

    // ========================================================================
    // EDGE CASES
    // ========================================================================

    [Fact]
    public void TempString_ImplicitWithNullValue_Works()
    {
        // Arrange
        string? nullStr = null;

        // Act
        string result = ZA.String($"Value: {nullStr}");

        // Assert
        Assert.Equal("Value: ", result);
    }

    [Fact]
    public void TempBytes_ImplicitWithSpecialChars_Works()
    {
        // Act
        byte[] result = ZA.Utf8($"Line1\r\nLine2\tTabbed");

        // Assert
        Assert.Equal("Line1\r\nLine2\tTabbed"u8.ToArray(), result);
    }

    [Fact]
    public void TempString_ImplicitWithGuid_Works()
    {
        // Arrange
        Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");

        // Act
        string result = ZA.String($"{guid}");

        // Assert
        Assert.Equal("12345678-1234-1234-1234-123456789abc", result);
    }

    [Fact]
    public void TempBytes_ImplicitWithDateTime_Works()
    {
        // Arrange
        DateTime dt = new(2025, 6, 15, 10, 30, 0);

        // Act
        byte[] result = ZA.Utf8($"{dt:yyyy-MM-dd}");

        // Assert
        Assert.Equal("2025-06-15"u8.ToArray(), result);
    }
}
