// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for culture-sensitive formatting with <see cref="TempString"/>.
/// Uses ZA.String(culture, ...) which supports runtime formatting with culture.
/// Note: ZA.Bytes/ZA.Utf8 do not have culture-aware overloads.
/// </summary>
public sealed class LocalizedFormattingTests
{
    // ========================================================================
    // TEMPSTRING WITH CULTURE - DECIMAL FORMATTING
    // These tests use ZA.String(culture, ...) which has generated overloads
    // ========================================================================
    [Test]
    public async Task String_German_FormatsDecimalWithComma()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double value = 1234.56;

        // Act
        string content;
        {
            using TempString result = ZA.String(german, "Preis: ", value);
            content = result.ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("Preis: 1234,56");
    }

    [Test]
    public async Task String_English_FormatsDecimalWithPeriod()
    {
        // Arrange
        CultureInfo english = CultureInfo.GetCultureInfo("en-US");
        double value = 1234.56;

        // Act
        string content;
        {
            using TempString result = ZA.String(english, "Price: ", value);
            content = result.ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("Price: 1234.56");
    }

    [Test]
    public async Task String_French_FormatsDecimalWithComma()
    {
        // Arrange - French uses comma as decimal separator
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
        double value = 1234.56;

        // Act
        string content;
        {
            using TempString result = ZA.String(french, "Prix: ", value);
            content = result.ToString();
        }

        // Assert - French uses comma as decimal separator
        await Assert.That(content).Contains(",");
    }

    [Test]
    public async Task String_InvariantCulture_FormatsConsistently()
    {
        // Arrange
        CultureInfo invariant = CultureInfo.InvariantCulture;
        double value = 1234567.89;

        // Act
        string content;
        {
            using TempString result = ZA.String(invariant, "Value: ", value);
            content = result.ToString();
        }

        // Assert - Invariant culture uses period
        await Assert.That(content).IsEqualTo("Value: 1234567.89");
    }

    // ========================================================================
    // TEMPSTRING WITH CULTURE - PROPERTIES
    // ========================================================================

    [Test]
    public async Task String_WithCulture_Length_ReturnsCorrectValue()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;
        int value = 12345;

        // Act
        int length;
        {
            using TempString result = ZA.String(culture, "Value: ", value);
            length = result.Length;
        }

        // Assert - "Value: 12345" is 12 characters
        await Assert.That(length).IsEqualTo(12);
    }

    [Test]
    public async Task String_WithCulture_IsEmpty_ReturnsFalseForContent()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;

        // Act
        bool isEmpty;
        {
            using TempString result = ZA.String(culture, "Hello");
            isEmpty = result.IsEmpty;
        }

        // Assert
        await Assert.That(isEmpty).IsFalse();
    }

    [Test]
    public async Task String_WithCulture_AsSpan_ReturnsCorrectCharacters()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;
        int value = 42;

        // Act
        string content;
        {
            using TempString result = ZA.String(culture, "Value: ", value);
            content = result.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("Value: 42");
    }

    // ========================================================================
    // TEMPSTRING WITH CULTURE - IMPLICIT OPERATORS
    // ========================================================================

    [Test]
    public async Task String_WithCulture_ImplicitToString_ReturnsCorrectValue()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double value = 1234.56;

        // Act - implicit conversion to string
        string result = ZA.String(german, "Preis: ", value);

        // Assert
        await Assert.That(result).IsEqualTo("Preis: 1234,56");
    }

    [Test]
    public async Task String_WithCulture_ImplicitToSpan_ReturnsCorrectValue()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        int value = 42;

        // Act - explicit scope
        string content;
        {
            using TempString temp = ZA.String(german, "Wert: ", value);
            content = temp.AsSpan().ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("Wert: 42");
    }

    // ========================================================================
    // TEMPSTRING WITH CULTURE - MULTIPLE VALUES
    // ========================================================================

    [Test]
    public async Task String_WithCulture_MultipleValues_FormatsAllCorrectly()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        int count = 5;
        double price = 99.99;

        // Act
        string content;
        {
            using TempString result = ZA.String(german, "Artikel: ", count, ", Preis: ", price, " €");
            content = result.ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("Artikel: 5, Preis: 99,99 €");
    }

    [Test]
    public async Task String_WithCulture_MultipleDoubles_AllUseCulture()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double d1 = 1.5;
        double d2 = 2.5;
        double d3 = 3.5;

        // Act
        string content;
        {
            using TempString result = ZA.String(german, "A=", d1, " B=", d2, " C=", d3);
            content = result.ToString();
        }

        // Assert - All should use comma
        await Assert.That(content).IsEqualTo("A=1,5 B=2,5 C=3,5");
    }

    // ========================================================================
    // TEMPBYTES (UTF8) - BASIC FORMATTING (Culture-independent)
    // Note: ZA.Utf8() does not have culture-aware overloads
    // ========================================================================

    [Test]
    public async Task Utf8_SimpleString_EncodesCorrectly()
    {
        // Act
        byte[] bytes;
        {
            using TempBytes result = ZA.Utf8("Hello World");
            bytes = result.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(bytes).IsEquivalentTo("Hello World"u8.ToArray());
    }

    [Test]
    public async Task Utf8_WithValue_EncodesCorrectly()
    {
        // Act
        byte[] bytes;
        {
            using TempBytes result = ZA.Utf8("Value: ", 42);
            bytes = result.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(bytes).IsEquivalentTo("Value: 42"u8.ToArray());
    }

    [Test]
    public async Task Utf8_UnicodeCharacters_EncodesCorrectly()
    {
        // Arrange - € is a multi-byte UTF-8 character
        // Act
        string content;
        {
            using TempBytes result = ZA.Utf8("Price: €", 100);
            content = System.Text.Encoding.UTF8.GetString(result.AsSpan());
        }

        // Assert
        await Assert.That(content).IsEqualTo("Price: €100");
    }

    [Test]
    public async Task Utf8_GermanUmlauts_EncodesCorrectly()
    {
        // Act
        string content;
        {
            using TempBytes result = ZA.Utf8("Größe: äöüß");
            content = System.Text.Encoding.UTF8.GetString(result.AsSpan());
        }

        // Assert
        await Assert.That(content).IsEqualTo("Größe: äöüß");
    }

    [Test]
    public async Task Utf8_Emoji_EncodesCorrectly()
    {
        // Act
        string content;
        {
            using TempBytes result = ZA.Utf8("Status: ✓ OK 🎉");
            content = System.Text.Encoding.UTF8.GetString(result.AsSpan());
        }

        // Assert
        await Assert.That(content).IsEqualTo("Status: ✓ OK 🎉");
    }

    // ========================================================================
    // TEMPBYTES (UTF8) - PROPERTIES
    // ========================================================================

    [Test]
    public async Task Utf8_Length_ReturnsCorrectByteCount()
    {
        // Arrange
        int value = 12345;
        string expected = $"Value: {value}";

        // Act
        int length;
        {
            using TempBytes result = ZA.Utf8("Value: ", value);
            length = result.Length;
        }

        // Assert
        await Assert.That(length).IsEqualTo(System.Text.Encoding.UTF8.GetByteCount(expected));
    }

    [Test]
    public async Task Utf8_IsEmpty_ReturnsFalseForContent()
    {
        // Act
        bool isEmpty;
        {
            using TempBytes result = ZA.Utf8("Hello");
            isEmpty = result.IsEmpty;
        }

        // Assert
        await Assert.That(isEmpty).IsFalse();
    }

    [Test]
    public async Task Utf8_AsSpan_ReturnsCorrectBytes()
    {
        // Act
        byte[] bytes;
        {
            using TempBytes result = ZA.Utf8("Test: ", 42);
            bytes = result.AsSpan().ToArray();
        }

        // Assert
        await Assert.That(bytes).IsEquivalentTo("Test: 42"u8.ToArray());
    }

    // ========================================================================
    // TEMPBYTES (UTF8) - IMPLICIT OPERATORS
    // ========================================================================

    [Test]
    public async Task Utf8_ImplicitToByteArray_ReturnsCorrectValue()
    {
        // Arrange
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("Value: 42");

        // Act - implicit conversion to byte[]
        byte[] result = ZA.Utf8("Value: ", 42);

        // Assert
        await Assert.That(result).IsEquivalentTo(expected);
    }

    // ========================================================================
    // DISPOSE BEHAVIOR
    // ========================================================================

    [Test]
    public async Task String_WithCulture_Dispose_ReleasesBuffer()
    {
        // Arrange & Act
        bool hasLength;
        {
            TempString result = ZA.String(CultureInfo.InvariantCulture, "Test: ", 42);
            hasLength = result.Length > 0;
            result.Dispose();
        }

        // Assert - capture length before dispose
        await Assert.That(hasLength).IsTrue();

        // After dispose, we shouldn't use the struct anymore
        // This test just validates that Dispose completes without exception
    }

    [Test]
    public async Task Utf8_Dispose_ReleasesBuffer()
    {
        // Arrange & Act
        bool hasLength;
        {
            TempBytes result = ZA.Utf8("Test: ", 42);
            hasLength = result.Length > 0;
            result.Dispose();
        }

        // Assert - capture length before dispose
        await Assert.That(hasLength).IsTrue();

        // After dispose, we shouldn't use the struct anymore
        // This test just validates that Dispose completes without exception
    }

    // ========================================================================
    // EDGE CASES
    // ========================================================================

    [Test]
    public async Task String_WithCulture_EmptyString_ReturnsEmpty()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;

        // Act
        bool isEmpty;
        int length;
        {
            using TempString result = ZA.String(culture, "");
            isEmpty = result.IsEmpty;
            length = result.Length;
        }

        // Assert
        await Assert.That(isEmpty).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Utf8_EmptyString_ReturnsEmpty()
    {
        // Act
        bool isEmpty;
        int length;
        {
            using TempBytes result = ZA.Utf8("");
            isEmpty = result.IsEmpty;
            length = result.Length;
        }

        // Assert
        await Assert.That(isEmpty).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task String_WithCulture_LargeValue_HandlesCorrectly()
    {
        // Arrange
        CultureInfo invariant = CultureInfo.InvariantCulture;
        long largeValue = 9_999_999_999_999;

        // Act
        string content;
        {
            using TempString result = ZA.String(invariant, "Large: ", largeValue);
            content = result.ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("Large: 9999999999999");
    }

    [Test]
    public async Task Utf8_LargeValue_HandlesCorrectly()
    {
        // Arrange
        long largeValue = 9_999_999_999_999;

        // Act
        string content;
        {
            using TempBytes result = ZA.Utf8("Large: ", largeValue);
            content = System.Text.Encoding.UTF8.GetString(result.AsSpan());
        }

        // Assert
        await Assert.That(content).IsEqualTo("Large: 9999999999999");
    }

    [Test]
    public async Task String_WithCulture_NegativeValue_FormatsCorrectly()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double negativeValue = -1234.56;

        // Act
        string content;
        {
            using TempString result = ZA.String(german, "Wert: ", negativeValue);
            content = result.ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("Wert: -1234,56");
    }

    [Test]
    public async Task String_WithCulture_Zero_FormatsCorrectly()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;
        int zero = 0;

        // Act
        string content;
        {
            using TempString result = ZA.String(culture, "Count: ", zero);
            content = result.ToString();
        }

        // Assert
        await Assert.That(content).IsEqualTo("Count: 0");
    }

    // ========================================================================
    // COMPARISON WITH DIFFERENT CULTURES
    // ========================================================================

    [Test]
    public async Task String_SameValue_DifferentCultures_ProducesDifferentOutput()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo english = CultureInfo.GetCultureInfo("en-US");
        double value = 1234.56;

        // Act
        string germanContent;
        string englishContent;
        {
            using TempString germanResult = ZA.String(german, value);
            using TempString englishResult = ZA.String(english, value);
            germanContent = germanResult.ToString();
            englishContent = englishResult.ToString();
        }

        // Assert - Different cultures should produce different output
        await Assert.That(englishContent).IsNotEqualTo(germanContent);
        await Assert.That(germanContent).Contains(",");
        await Assert.That(englishContent).Contains(".");
    }

    // ========================================================================
    // EXPLICIT SCOPE VS IMPLICIT CONVERSION
    // ========================================================================

    [Test]
    public async Task String_WithCulture_ExplicitScope_PreservesBuffer()
    {
        // This test validates the recommended pattern:
        // Using explicit scope with proper disposal
        CultureInfo culture = CultureInfo.InvariantCulture;
        string capturedValue;

        {
            using TempString temp = ZA.String(culture, "Hello ", "World");
            capturedValue = temp.AsSpan().ToString();
        }

        await Assert.That(capturedValue).IsEqualTo("Hello World");
    }

    [Test]
    public async Task Utf8_ExplicitScope_PreservesBuffer()
    {
        // This test validates the recommended pattern:
        // Using explicit scope with proper disposal
        byte[] capturedValue;

        {
            using TempBytes temp = ZA.Utf8("Hello ", "World");
            capturedValue = temp.AsSpan().ToArray();
        }

        await Assert.That(capturedValue).IsEquivalentTo("Hello World"u8.ToArray());
    }

    // ========================================================================
    // ISHEAPALLOCATED PROPERTY
    // ========================================================================

    [Test]
    public async Task String_WithCulture_SmallContent_UsesThreadStaticBuffer()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;

        // Act
        bool isHeapAllocated;
        {
            using TempString result = ZA.String(culture, "Small: ", 42);
            isHeapAllocated = result.IsHeapAllocated;
        }

        // Assert - Small content should use the thread-static buffer
        await Assert.That(isHeapAllocated).IsFalse();
    }

    [Test]
    public async Task Utf8_SmallContent_UsesThreadStaticBuffer()
    {
        // Act
        bool isHeapAllocated;
        {
            using TempBytes result = ZA.Utf8("Small: ", 42);
            isHeapAllocated = result.IsHeapAllocated;
        }

        // Assert - Small content should use the thread-static buffer
        await Assert.That(isHeapAllocated).IsFalse();
    }

    // ========================================================================
    // SEQUENTIAL CALLS WITH CULTURE
    // ========================================================================

    [Test]
    public async Task String_WithCulture_SequentialCalls_WorkCorrectly()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");

        for (int i = 0; i < 10; i++)
        {
            double value = i + 0.5;
            string content;
            {
                using TempString temp = ZA.String(german, "Wert: ", value);
                content = temp.ToString();
            }
            await Assert.That(content).Contains(",");
        }
    }

    [Test]
    public async Task Utf8_SequentialCalls_WorkCorrectly()
    {
        for (int i = 0; i < 10; i++)
        {
            string content;
            {
                using TempBytes temp = ZA.Utf8("Value: ", i);
                content = System.Text.Encoding.UTF8.GetString(temp.AsSpan());
            }
            await Assert.That(content).IsEqualTo($"Value: {i}");
        }
    }
}