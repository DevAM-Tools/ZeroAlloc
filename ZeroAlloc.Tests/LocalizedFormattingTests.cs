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

using System.Globalization;

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for culture-sensitive formatting with <see cref="TempString"/>.
/// Uses ZA.String(culture, ...) which supports runtime formatting with culture.
/// Note: ZA.Bytes/ZA.Utf8 do not have culture-aware overloads.
/// </summary>
public class LocalizedFormattingTests
{
    // ========================================================================
    // TEMPSTRING WITH CULTURE - DECIMAL FORMATTING
    // These tests use ZA.String(culture, ...) which has generated overloads
    // ========================================================================

    [Fact]
    public void String_German_FormatsDecimalWithComma()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double value = 1234.56;

        // Act
        using TempString result = ZA.String(german, "Preis: ", value);

        // Assert
        Assert.Equal("Preis: 1234,56", result.ToString());
    }

    [Fact]
    public void String_English_FormatsDecimalWithPeriod()
    {
        // Arrange
        CultureInfo english = CultureInfo.GetCultureInfo("en-US");
        double value = 1234.56;

        // Act
        using TempString result = ZA.String(english, "Price: ", value);

        // Assert
        Assert.Equal("Price: 1234.56", result.ToString());
    }

    [Fact]
    public void String_French_FormatsDecimalWithComma()
    {
        // Arrange - French uses comma as decimal separator
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
        double value = 1234.56;

        // Act
        using TempString result = ZA.String(french, "Prix: ", value);

        // Assert - French uses comma as decimal separator
        Assert.Contains(",", result.ToString());
    }

    [Fact]
    public void String_InvariantCulture_FormatsConsistently()
    {
        // Arrange
        CultureInfo invariant = CultureInfo.InvariantCulture;
        double value = 1234567.89;

        // Act
        using TempString result = ZA.String(invariant, "Value: ", value);

        // Assert - Invariant culture uses period
        Assert.Equal("Value: 1234567.89", result.ToString());
    }

    // ========================================================================
    // TEMPSTRING WITH CULTURE - PROPERTIES
    // ========================================================================

    [Fact]
    public void String_WithCulture_Length_ReturnsCorrectValue()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;
        int value = 12345;

        // Act
        using TempString result = ZA.String(culture, "Value: ", value);

        // Assert - "Value: 12345" is 12 characters
        Assert.Equal(12, result.Length);
    }

    [Fact]
    public void String_WithCulture_IsEmpty_ReturnsFalseForContent()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;

        // Act
        using TempString result = ZA.String(culture, "Hello");

        // Assert
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void String_WithCulture_AsSpan_ReturnsCorrectCharacters()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;
        int value = 42;

        // Act
        using TempString result = ZA.String(culture, "Value: ", value);

        // Assert
        Assert.Equal("Value: 42", result.AsSpan().ToString());
    }

    // ========================================================================
    // TEMPSTRING WITH CULTURE - IMPLICIT OPERATORS
    // ========================================================================

    [Fact]
    public void String_WithCulture_ImplicitToString_ReturnsCorrectValue()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double value = 1234.56;

        // Act - implicit conversion to string
        string result = ZA.String(german, "Preis: ", value);

        // Assert
        Assert.Equal("Preis: 1234,56", result);
    }

    [Fact]
    public void String_WithCulture_ImplicitToSpan_ReturnsCorrectValue()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        int value = 42;

        // Act - explicit scope
        {
            using TempString temp = ZA.String(german, "Wert: ", value);
            ReadOnlySpan<char> span = temp.AsSpan();

            // Assert
            Assert.Equal("Wert: 42", span.ToString());
        }
    }

    // ========================================================================
    // TEMPSTRING WITH CULTURE - MULTIPLE VALUES
    // ========================================================================

    [Fact]
    public void String_WithCulture_MultipleValues_FormatsAllCorrectly()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        int count = 5;
        double price = 99.99;

        // Act
        using TempString result = ZA.String(german, "Artikel: ", count, ", Preis: ", price, " €");

        // Assert
        Assert.Equal("Artikel: 5, Preis: 99,99 €", result.ToString());
    }

    [Fact]
    public void String_WithCulture_MultipleDoubles_AllUseCulture()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double d1 = 1.5;
        double d2 = 2.5;
        double d3 = 3.5;

        // Act
        using TempString result = ZA.String(german, "A=", d1, " B=", d2, " C=", d3);

        // Assert - All should use comma
        Assert.Equal("A=1,5 B=2,5 C=3,5", result.ToString());
    }

    // ========================================================================
    // TEMPBYTES (UTF8) - BASIC FORMATTING (Culture-independent)
    // Note: ZA.Utf8() does not have culture-aware overloads
    // ========================================================================

    [Fact]
    public void Utf8_SimpleString_EncodesCorrectly()
    {
        // Act
        using TempBytes result = ZA.Utf8("Hello World");

        // Assert
        Assert.Equal("Hello World"u8.ToArray(), result.AsSpan().ToArray());
    }

    [Fact]
    public void Utf8_WithValue_EncodesCorrectly()
    {
        // Act
        using TempBytes result = ZA.Utf8("Value: ", 42);

        // Assert
        Assert.Equal("Value: 42"u8.ToArray(), result.AsSpan().ToArray());
    }

    [Fact]
    public void Utf8_UnicodeCharacters_EncodesCorrectly()
    {
        // Arrange - € is a multi-byte UTF-8 character
        // Act
        using TempBytes result = ZA.Utf8("Price: €", 100);

        // Assert
        Assert.Equal("Price: €100", System.Text.Encoding.UTF8.GetString(result.AsSpan()));
    }

    [Fact]
    public void Utf8_GermanUmlauts_EncodesCorrectly()
    {
        // Act
        using TempBytes result = ZA.Utf8("Größe: äöüß");

        // Assert
        Assert.Equal("Größe: äöüß", System.Text.Encoding.UTF8.GetString(result.AsSpan()));
    }

    [Fact]
    public void Utf8_Emoji_EncodesCorrectly()
    {
        // Act
        using TempBytes result = ZA.Utf8("Status: ✓ OK 🎉");

        // Assert
        Assert.Equal("Status: ✓ OK 🎉", System.Text.Encoding.UTF8.GetString(result.AsSpan()));
    }

    // ========================================================================
    // TEMPBYTES (UTF8) - PROPERTIES
    // ========================================================================

    [Fact]
    public void Utf8_Length_ReturnsCorrectByteCount()
    {
        // Arrange
        int value = 12345;
        string expected = $"Value: {value}";

        // Act
        using TempBytes result = ZA.Utf8("Value: ", value);

        // Assert
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(expected), result.Length);
    }

    [Fact]
    public void Utf8_IsEmpty_ReturnsFalseForContent()
    {
        // Act
        using TempBytes result = ZA.Utf8("Hello");

        // Assert
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void Utf8_AsSpan_ReturnsCorrectBytes()
    {
        // Act
        using TempBytes result = ZA.Utf8("Test: ", 42);

        // Assert
        Assert.Equal("Test: 42"u8.ToArray(), result.AsSpan().ToArray());
    }

    // ========================================================================
    // TEMPBYTES (UTF8) - IMPLICIT OPERATORS
    // ========================================================================

    [Fact]
    public void Utf8_ImplicitToByteArray_ReturnsCorrectValue()
    {
        // Arrange
        byte[] expected = System.Text.Encoding.UTF8.GetBytes("Value: 42");

        // Act - implicit conversion to byte[]
        byte[] result = ZA.Utf8("Value: ", 42);

        // Assert
        Assert.Equal(expected, result);
    }

    // ========================================================================
    // DISPOSE BEHAVIOR
    // ========================================================================

    [Fact]
    public void String_WithCulture_Dispose_ReleasesBuffer()
    {
        // Arrange & Act
        TempString result = ZA.String(CultureInfo.InvariantCulture, "Test: ", 42);

        // Assert - capture length before dispose
        Assert.True(result.Length > 0);

        // Dispose
        result.Dispose();

        // After dispose, we shouldn't use the struct anymore
        // This test just validates that Dispose completes without exception
    }

    [Fact]
    public void Utf8_Dispose_ReleasesBuffer()
    {
        // Arrange & Act
        TempBytes result = ZA.Utf8("Test: ", 42);

        // Assert - capture length before dispose
        Assert.True(result.Length > 0);

        // Dispose
        result.Dispose();

        // After dispose, we shouldn't use the struct anymore
        // This test just validates that Dispose completes without exception
    }

    // ========================================================================
    // EDGE CASES
    // ========================================================================

    [Fact]
    public void String_WithCulture_EmptyString_ReturnsEmpty()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;

        // Act
        using TempString result = ZA.String(culture, "");

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void Utf8_EmptyString_ReturnsEmpty()
    {
        // Act
        using TempBytes result = ZA.Utf8("");

        // Assert
        Assert.True(result.IsEmpty);
        Assert.Equal(0, result.Length);
    }

    [Fact]
    public void String_WithCulture_LargeValue_HandlesCorrectly()
    {
        // Arrange
        CultureInfo invariant = CultureInfo.InvariantCulture;
        long largeValue = 9_999_999_999_999;

        // Act
        using TempString result = ZA.String(invariant, "Large: ", largeValue);

        // Assert
        Assert.Equal("Large: 9999999999999", result.ToString());
    }

    [Fact]
    public void Utf8_LargeValue_HandlesCorrectly()
    {
        // Arrange
        long largeValue = 9_999_999_999_999;

        // Act
        using TempBytes result = ZA.Utf8("Large: ", largeValue);

        // Assert
        Assert.Equal("Large: 9999999999999", System.Text.Encoding.UTF8.GetString(result.AsSpan()));
    }

    [Fact]
    public void String_WithCulture_NegativeValue_FormatsCorrectly()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double negativeValue = -1234.56;

        // Act
        using TempString result = ZA.String(german, "Wert: ", negativeValue);

        // Assert
        Assert.Equal("Wert: -1234,56", result.ToString());
    }

    [Fact]
    public void String_WithCulture_Zero_FormatsCorrectly()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;
        int zero = 0;

        // Act
        using TempString result = ZA.String(culture, "Count: ", zero);

        // Assert
        Assert.Equal("Count: 0", result.ToString());
    }

    // ========================================================================
    // COMPARISON WITH DIFFERENT CULTURES
    // ========================================================================

    [Fact]
    public void String_SameValue_DifferentCultures_ProducesDifferentOutput()
    {
        // Arrange
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo english = CultureInfo.GetCultureInfo("en-US");
        double value = 1234.56;

        // Act
        using TempString germanResult = ZA.String(german, value);
        using TempString englishResult = ZA.String(english, value);

        // Assert - Different cultures should produce different output
        Assert.NotEqual(germanResult.ToString(), englishResult.ToString());
        Assert.Contains(",", germanResult.ToString());
        Assert.Contains(".", englishResult.ToString());
    }

    // ========================================================================
    // EXPLICIT SCOPE VS IMPLICIT CONVERSION
    // ========================================================================

    [Fact]
    public void String_WithCulture_ExplicitScope_PreservesBuffer()
    {
        // This test validates the recommended pattern:
        // Using explicit scope with proper disposal
        CultureInfo culture = CultureInfo.InvariantCulture;
        string capturedValue;

        {
            using TempString temp = ZA.String(culture, "Hello ", "World");
            capturedValue = temp.AsSpan().ToString();
        }

        Assert.Equal("Hello World", capturedValue);
    }

    [Fact]
    public void Utf8_ExplicitScope_PreservesBuffer()
    {
        // This test validates the recommended pattern:
        // Using explicit scope with proper disposal
        byte[] capturedValue;

        {
            using TempBytes temp = ZA.Utf8("Hello ", "World");
            capturedValue = temp.AsSpan().ToArray();
        }

        Assert.Equal("Hello World"u8.ToArray(), capturedValue);
    }

    // ========================================================================
    // ISHEAPALLOCATED PROPERTY
    // ========================================================================

    [Fact]
    public void String_WithCulture_SmallContent_UsesThreadStaticBuffer()
    {
        // Arrange
        CultureInfo culture = CultureInfo.InvariantCulture;

        // Act
        using TempString result = ZA.String(culture, "Small: ", 42);

        // Assert - Small content should use the thread-static buffer
        Assert.False(result.IsHeapAllocated);
    }

    [Fact]
    public void Utf8_SmallContent_UsesThreadStaticBuffer()
    {
        // Act
        using TempBytes result = ZA.Utf8("Small: ", 42);

        // Assert - Small content should use the thread-static buffer
        Assert.False(result.IsHeapAllocated);
    }

    // ========================================================================
    // SEQUENTIAL CALLS WITH CULTURE
    // ========================================================================

    [Fact]
    public void String_WithCulture_SequentialCalls_WorkCorrectly()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");

        for (int i = 0; i < 10; i++)
        {
            double value = i + 0.5;
            using TempString temp = ZA.String(german, "Wert: ", value);
            Assert.Contains(",", temp.ToString());
        }
    }

    [Fact]
    public void Utf8_SequentialCalls_WorkCorrectly()
    {
        for (int i = 0; i < 10; i++)
        {
            using TempBytes temp = ZA.Utf8("Value: ", i);
            string result = System.Text.Encoding.UTF8.GetString(temp.AsSpan());
            Assert.Equal($"Value: {i}", result);
        }
    }
}
