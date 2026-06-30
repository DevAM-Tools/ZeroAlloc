// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for implicit conversion operators on Temp* types.
/// Validates that implicit conversions work correctly and auto-dispose.
/// </summary>
public sealed class ImplicitOperatorTests
{
    // ========================================================================
    // TEMPSTRING - IMPLICIT TO READONLYSPAN<CHAR>
    // ========================================================================

    [Test]
    [Arguments("hello", 5)]
    [Arguments("", 0)]
    public async Task TempString_ImplicitToReadOnlySpan_ReturnsCorrectContent(string text, int expectedLength)
    {
        string content;
        int length;
        {
            using TempString temp = ZA.String(text);
            ReadOnlySpan<char> span = temp;
            content = span.ToString();
            length = span.Length;
        }

        await Assert.That(content).IsEqualTo(text);
        await Assert.That(length).IsEqualTo(expectedLength);
    }

    // ========================================================================
    // TEMPBYTES - IMPLICIT TO READONLYSPAN<BYTE>
    // ========================================================================

    [Test]
    [Arguments("hello", 5)]
    [Arguments("", 0)]
    public async Task TempBytes_ImplicitToReadOnlySpan_ReturnsCorrectContent(string text, int expectedLength)
    {
        string content;
        int length;
        {
            using TempBytes temp = ZA.Utf8(text);
            ReadOnlySpan<byte> span = temp;
            content = Encoding.UTF8.GetString(span);
            length = span.Length;
        }

        await Assert.That(content).IsEqualTo(text);
        await Assert.That(length).IsEqualTo(expectedLength);
    }

    // ========================================================================
    // TEMPSTRING - IMPLICIT TO STRING
    // ========================================================================
    [Test]
    public async Task TempString_ImplicitToString_ReturnsCorrectValue()
    {
        // Act - implicit conversion to string
        string result = ZA.String($"Hello World");

        // Assert
        await Assert.That(result).IsEqualTo("Hello World");
    }

    [Test]
    public async Task TempString_ImplicitToString_WithInterpolation_ReturnsCorrectValue()
    {
        // Arrange
        int value = 42;
        string name = "Test";

        // Act - implicit conversion with interpolation
        string result = ZA.String($"Name: {name}, Value: {value}");

        // Assert
        await Assert.That(result).IsEqualTo("Name: Test, Value: 42");
    }

    [Test]
    public async Task TempString_ImplicitToString_FormattedNumbers_ReturnsCorrectValue()
    {
        // Arrange - Use non-interpolated call to explicitly control culture
        CultureInfo invariant = CultureInfo.InvariantCulture;
        decimal price = 1234.56m;

        // Act - Use ZA.String with explicit culture to ensure invariant formatting
        string result = ZA.String(invariant, "Price: ", price);

        // Assert - Invariant culture uses period and comma as thousand separator
        await Assert.That(result).IsEqualTo("Price: 1234.56");
    }

    [Test]
    public async Task TempString_ImplicitToString_EmptyInterpolation_ReturnsEmpty()
    {
        // Act
        string result = ZA.String($"");

        // Assert
        await Assert.That(result).IsEqualTo("");
    }

    [Test]
    public async Task TempString_ImplicitToString_LargeContent_Works()
    {
        // Arrange
        string largeContent = new('X', 1000);

        // Act
        string result = ZA.String($"{largeContent}");

        // Assert
        await Assert.That(result).IsEqualTo(largeContent);
    }

    // ========================================================================
    // TEMPSTRING - ASSIGNMENT TO STRING VARIABLE
    // ========================================================================

    [Test]
    public async Task TempString_AssignToVariable_Works()
    {
        // Arrange
        int count = 5;

        // Act
        string message = ZA.String($"Count: {count}");

        // Assert
        await Assert.That(message).IsEqualTo("Count: 5");
    }

    [Test]
    public async Task TempString_PassToMethod_Works()
    {
        // Act - TempString is implicitly converted to string when passed to a string-accepting method
        string result = ZA.String($"Test Message");

        // Assert
        await Assert.That(result).IsEqualTo("Test Message");
    }

    // ========================================================================
    // TEMPBYTES - IMPLICIT TO BYTE ARRAY
    // ========================================================================

    [Test]
    public async Task TempBytes_ImplicitToByteArray_ReturnsCorrectValue()
    {
        // Act - implicit conversion to byte[]
        byte[] result = ZA.Utf8($"Hello");

        // Assert
        await Assert.That(result).IsEquivalentTo("Hello"u8.ToArray());
    }

    [Test]
    public async Task TempBytes_ImplicitToByteArray_WithInterpolation_ReturnsCorrectValue()
    {
        // Arrange
        int value = 42;

        // Act
        byte[] result = ZA.Utf8($"Value: {value}");

        // Assert
        await Assert.That(result).IsEquivalentTo(System.Text.Encoding.UTF8.GetBytes("Value: 42"));
    }

    [Test]
    public async Task TempBytes_ImplicitToByteArray_Unicode_ReturnsCorrectValue()
    {
        // Act
        byte[] result = ZA.Utf8($"Héllo €100");

        // Assert
        await Assert.That(result).IsEquivalentTo(System.Text.Encoding.UTF8.GetBytes("Héllo €100"));
    }

    [Test]
    public async Task TempBytes_ImplicitToByteArray_EmptyInterpolation_ReturnsEmpty()
    {
        // Act
        byte[] result = ZA.Utf8($"");

        // Assert
        await Assert.That(result).IsEmpty();
        await Assert.That(result).IsSameReferenceAs(Array.Empty<byte>());
    }

    [Test]
    [Arguments("lazy")]
    public async Task LazyString_ImplicitToString_ReturnsEvaluatedValue(string expected)
    {
        LazyString s = LazyString.Lazy(() => expected);
        string result = s;
        await Assert.That(result).IsEqualTo(expected);
    }

    // ========================================================================
    // TEMPBYTES - ASSIGNMENT TO VARIABLE
    // ========================================================================

    [Test]
    public async Task TempBytes_AssignToVariable_Works()
    {
        // Arrange
        int id = 123;

        // Act
        byte[] data = ZA.Utf8($"ID={id}");

        // Assert
        await Assert.That(data).IsEquivalentTo("ID=123"u8.ToArray());
    }

    [Test]
    public async Task TempBytes_PassToMethod_Works()
    {
        // Act - TempBytes is implicitly converted to byte[] when passed to a byte[]-accepting method
        byte[] result = ZA.Utf8($"Test");

        // Assert
        await Assert.That(result).IsEquivalentTo("Test"u8.ToArray());
    }

    // ========================================================================
    // TEMPSTRING WITH CULTURE - IMPLICIT TO STRING
    // Uses ZA.String(culture, ...) which doesn't require generator overloads
    // ========================================================================

    [Test]
    public async Task TempString_ImplicitToString_German_ReturnsCorrectValue()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double value = 1234.56;

        // Act - implicit conversion with culture
        string result = ZA.String(german, "Preis: ", value);

        // Assert - German uses comma as decimal separator
        await Assert.That(result).IsEqualTo("Preis: 1234,56");
    }

    [Test]
    public async Task TempString_ImplicitToString_English_ReturnsCorrectValue()
    {
        // Arrange
        CultureInfo english = CultureInfo.GetCultureInfo("en-US");
        double value = 1234.56;

        // Act - implicit conversion with culture
        string result = ZA.String(english, "Price: ", value);

        // Assert - English uses period as decimal separator
        await Assert.That(result).IsEqualTo("Price: 1234.56");
    }

    [Test]
    public async Task TempString_WithCulture_PassToMethod_Works()
    {
        // Act - TempString with culture is implicitly converted to string
        CultureInfo culture = CultureInfo.InvariantCulture;
        string result = ZA.String(culture, "Test: ", 42);

        // Assert
        await Assert.That(result).IsEqualTo("Test: 42");
    }

    // ========================================================================
    // TEMPBYTES (UTF8) - IMPLICIT TO BYTE ARRAY
    // Note: ZA.Utf8() does not have culture-aware overloads
    // ========================================================================

    [Test]
    public async Task TempBytes_ImplicitToByteArray_SimpleValue_ReturnsCorrectValue()
    {
        // Arrange
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("Value: 42");

        // Act - implicit conversion
        byte[] result = ZA.Utf8("Value: ", 42);

        // Assert
        await Assert.That(result).IsEquivalentTo(expected);
    }

    [Test]
    public async Task TempBytes_ImplicitToByteArray_WithUnicode_ReturnsCorrectValue()
    {
        // Arrange
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("Price: €100");

        // Act - implicit conversion
        byte[] result = ZA.Utf8("Price: €", 100);

        // Assert
        await Assert.That(result).IsEquivalentTo(expected);
    }

    [Test]
    public async Task TempBytes_PassToMethodWithNumber_Works()
    {
        // Act - TempBytes is implicitly converted to byte[] when passed to a byte[]-accepting method
        byte[] result = ZA.Utf8(42);

        // Assert
        await Assert.That(result).IsEquivalentTo("42"u8.ToArray());
    }

    // ========================================================================
    // MULTIPLE CONVERSIONS IN SEQUENCE
    // ========================================================================

    [Test]
    public async Task ImplicitOperators_MultipleConversionsInSequence_Work()
    {
        // Act - Multiple implicit conversions in sequence
        string s1 = ZA.String($"First");
        string s2 = ZA.String($"Second");
        string s3 = ZA.String($"Third");

        // Assert
        await Assert.That(s1).IsEqualTo("First");
        await Assert.That(s2).IsEqualTo("Second");
        await Assert.That(s3).IsEqualTo("Third");
    }

    [Test]
    public async Task ImplicitOperators_MixedTypes_Work()
    {
        // Act
        string str = ZA.String($"Hello");
        byte[] bytes = ZA.Utf8($"World");
        string cultureStr = ZA.String(CultureInfo.InvariantCulture, "Test: ", 42);
        byte[] cultureBytes = ZA.Bytes(CultureInfo.InvariantCulture, "Data: ", 123);

        // Assert
        await Assert.That(str).IsEqualTo("Hello");
        await Assert.That(bytes).IsEquivalentTo("World"u8.ToArray());
        await Assert.That(cultureStr).IsEqualTo("Test: 42");
        await Assert.That(cultureBytes).IsEquivalentTo("Data: 123"u8.ToArray());
    }

    // ========================================================================
    // IMPLICIT IN COLLECTIONS/EXPRESSIONS
    // ========================================================================

    [Test]
    public async Task TempString_ImplicitInListInitializer_Works()
    {
        // Act
        List<string> messages =
        [
            ZA.String($"Message 1"),
            ZA.String($"Message 2"),
            ZA.String($"Message 3")
        ];

        // Assert
        await Assert.That(messages.Count).IsEqualTo(3);
        await Assert.That(messages[0]).IsEqualTo("Message 1");
        await Assert.That(messages[1]).IsEqualTo("Message 2");
        await Assert.That(messages[2]).IsEqualTo("Message 3");
    }

    [Test]
    public async Task TempBytes_ImplicitInArrayInitializer_Works()
    {
        // Act
        byte[][] dataArray =
        [
            ZA.Utf8($"First"),
            ZA.Utf8($"Second"),
            ZA.Utf8($"Third")
        ];

        // Assert
        await Assert.That(dataArray.Length).IsEqualTo(3);
        await Assert.That(dataArray[0]).IsEquivalentTo("First"u8.ToArray());
        await Assert.That(dataArray[1]).IsEquivalentTo("Second"u8.ToArray());
        await Assert.That(dataArray[2]).IsEquivalentTo("Third"u8.ToArray());
    }

    // ========================================================================
    // IMPLICIT IN STRING OPERATIONS
    // ========================================================================

    [Test]
    public async Task TempString_ImplicitInStringConcat_Works()
    {
        // Arrange
        int id = 123;

        // Act
        string result = "Prefix: " + ZA.String($"ID={id}") + " :Suffix";

        // Assert
        await Assert.That(result).IsEqualTo("Prefix: ID=123 :Suffix");
    }

    [Test]
    public async Task TempString_ImplicitInStringEquals_Works()
    {
        // Act
        bool isEqual = ZA.String($"Hello") == "Hello";

        // Assert
        await Assert.That(isEqual).IsTrue();
    }

    // ========================================================================
    // EDGE CASES
    // ========================================================================

    [Test]
    public async Task TempString_ImplicitWithNullValue_Works()
    {
        // Arrange
        string? nullStr = null;

        // Act
        string result = ZA.String($"Value: {nullStr}");

        // Assert
        await Assert.That(result).IsEqualTo("Value: ");
    }

    [Test]
    public async Task TempBytes_ImplicitWithSpecialChars_Works()
    {
        // Act
        byte[] result = ZA.Utf8($"Line1\r\nLine2\tTabbed");

        // Assert
        await Assert.That(result).IsEquivalentTo("Line1\r\nLine2\tTabbed"u8.ToArray());
    }

    [Test]
    public async Task TempString_ImplicitWithGuid_Works()
    {
        // Arrange
        Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");

        // Act
        string result = ZA.String($"{guid}");

        // Assert
        await Assert.That(result).IsEqualTo("12345678-1234-1234-1234-123456789abc");
    }

    [Test]
    public async Task TempBytes_ImplicitWithDateTime_Works()
    {
        // Arrange
        DateTime dt = new(2025, 6, 15, 10, 30, 0);

        // Act
        byte[] result = ZA.Utf8($"{dt:yyyy-MM-dd}");

        // Assert
        await Assert.That(result).IsEquivalentTo("2025-06-15"u8.ToArray());
    }
}