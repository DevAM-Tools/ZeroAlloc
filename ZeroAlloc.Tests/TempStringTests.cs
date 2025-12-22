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

// ============================================================================
// TempString Tests - Comprehensive String Formatting Tests
// ============================================================================
// Tests for TempString: basic operations, type formatting, CultureInfo support,
// nested calls, TryString, Hex formatters, edge cases, and error handling.
// ============================================================================

namespace ZeroAlloc.Tests;

/// <summary>
/// Comprehensive tests for <see cref="TempString"/> and <see cref="ZA.String"/> methods.
/// </summary>
public class TempStringTests
{
    // ========================================================================
    // BASIC FORMATTING TESTS
    // Tests for fundamental string formatting operations
    // ========================================================================

    #region Basic Formatting

    [Fact]
    public void String_SimpleString_ReturnsCorrectContent()
    {
        using TempString temp = ZA.String("Hello");
        Assert.Equal("Hello", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_IntValue_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Value: ", 42);
        Assert.Equal("Value: 42", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MultipleStrings_ConcatenatesCorrectly()
    {
        using TempString temp = ZA.String("Hello", " ", "World");
        Assert.Equal("Hello World", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MixedTypes_FormatsCorrectly()
    {
        using TempString temp = ZA.String("User ", 12345, " logged in");
        Assert.Equal("User 12345 logged in", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Length_ReturnsCorrectLength()
    {
        using TempString temp = ZA.String("Test", 123);
        Assert.Equal(7, temp.Length); // "Test123"
    }

    [Fact]
    public void String_ToString_ReturnsCorrectString()
    {
        using TempString temp = ZA.String("Hello ", "World");
        Assert.Equal("Hello World", temp.ToString());
    }

    [Fact]
    public void String_AsSpan_ReturnsSameAsToString()
    {
        using TempString temp = ZA.String("Test", 123, "abc");
        Assert.Equal(temp.ToString(), temp.AsSpan().ToString());
    }

    #endregion

    // ========================================================================
    // TYPE-SPECIFIC TESTS
    // Tests for formatting specific .NET types
    // ========================================================================

    #region Boolean Formatting

    [Fact]
    public void String_BooleanTrue_FormatsAsTrue()
    {
        using TempString temp = ZA.String("Result: ", true);
        Assert.Equal("Result: True", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_BooleanFalse_FormatsAsFalse()
    {
        using TempString temp = ZA.String("Result: ", false);
        Assert.Equal("Result: False", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MultipleBooleans_FormatsCorrectly()
    {
        using TempString temp = ZA.String("T=", true, " F=", false);
        Assert.Equal("T=True F=False", temp.AsSpan().ToString());
    }

    #endregion

    #region Character Formatting

    [Fact]
    public void String_Char_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Letter: ", 'X');
        Assert.Equal("Letter: X", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MultipleChars_FormatsCorrectly()
    {
        using TempString temp = ZA.String('A', 'B', 'C');
        Assert.Equal("ABC", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_UnicodeChar_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Euro: ", '€');
        Assert.Equal("Euro: €", temp.AsSpan().ToString());
    }

    #endregion

    #region Numeric Types Formatting

    [Fact]
    public void String_Int32_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Int: ", 42);
        Assert.Equal("Int: 42", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Int64_FormatsCorrectly()
    {
        using TempString temp = ZA.String("ID: ", 9876543210L);
        Assert.Equal("ID: 9876543210", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Double_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Value: ", 3.14159);
        Assert.Equal("Value: 3.14159", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Float_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Value: ", 1.5f);
        Assert.Equal("Value: 1.5", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Decimal_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Price: ", 99.99m);
        Assert.Equal("Price: 99.99", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Byte_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Byte: ", (byte)255);
        Assert.Equal("Byte: 255", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_SByte_FormatsCorrectly()
    {
        using TempString temp = ZA.String("SByte: ", (sbyte)-128);
        Assert.Equal("SByte: -128", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Int16_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Short: ", (short)-32768);
        Assert.Equal("Short: -32768", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_UInt16_FormatsCorrectly()
    {
        using TempString temp = ZA.String("UShort: ", (ushort)65535);
        Assert.Equal("UShort: 65535", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_UInt32_FormatsCorrectly()
    {
        using TempString temp = ZA.String("UInt: ", uint.MaxValue);
        Assert.Equal($"UInt: {uint.MaxValue}", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_UInt64_FormatsCorrectly()
    {
        using TempString temp = ZA.String("ULong: ", ulong.MaxValue);
        Assert.Equal($"ULong: {ulong.MaxValue}", temp.AsSpan().ToString());
    }

    #endregion

    #region Guid and DateTime Formatting

    [Fact]
    public void String_Guid_FormatsCorrectly()
    {
        Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        using TempString temp = ZA.String("GUID: ", guid);
        Assert.Equal("GUID: 12345678-1234-1234-1234-123456789012", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_GuidEmpty_FormatsCorrectly()
    {
        using TempString temp = ZA.String("GUID: ", Guid.Empty);
        Assert.Equal("GUID: 00000000-0000-0000-0000-000000000000", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_DateTime_FormatsCorrectly()
    {
        DateTime dt = new(2025, 6, 15, 14, 30, 45);
        using TempString temp = ZA.String("Date: ", dt);
        Assert.Contains("2025", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_DateTimeOffset_FormatsCorrectly()
    {
        DateTimeOffset dto = new(2025, 6, 15, 14, 30, 45, TimeSpan.Zero);
        using TempString temp = ZA.String("Offset: ", dto);
        Assert.Contains("2025", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_TimeSpan_FormatsCorrectly()
    {
        TimeSpan ts = new(1, 23, 45);
        using TempString temp = ZA.String("Time: ", ts);
        Assert.Contains("23", temp.AsSpan().ToString());
    }

    #endregion

    // ========================================================================
    // CULTURE INFO TESTS
    // Tests for locale-specific formatting
    // ========================================================================

    #region CultureInfo Tests

    [Fact]
    public void String_GermanCulture_FormatsWithComma()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double value = 1234.56;
        using TempString temp = ZA.String(german, "Preis: ", value);
        Assert.Equal("Preis: 1234,56", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_USCulture_FormatsWithDot()
    {
        CultureInfo us = CultureInfo.GetCultureInfo("en-US");
        double value = 1234.56;
        using TempString temp = ZA.String(us, "Price: ", value);
        Assert.Equal("Price: 1234.56", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_FrenchCulture_FormatsDecimalCorrectly()
    {
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
        decimal value = 9876.54m;
        using TempString temp = ZA.String(french, "Prix: ", value);
        Assert.Contains(",", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_CultureWithMultipleArgs_AllUseCorrectCulture()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double d1 = 1.5;
        double d2 = 2.5;
        using TempString temp = ZA.String(german, "A=", d1, " B=", d2);
        Assert.Equal("A=1,5 B=2,5", temp.AsSpan().ToString());
    }

    #endregion

    // ========================================================================
    // NESTED CALL TESTS
    // Tests for graceful handling of recursive/nested ZeroAlloc calls
    // ========================================================================

    #region Nested Call Tests

    [Fact]
    public void String_NestedCalls_OuterUsesThreadStatic()
    {
        using TempString outer = ZA.String("Outer: ", 1);
        Assert.False(outer.IsHeapAllocated);
    }

    [Fact]
    public void String_NestedCalls_InnerUsesHeapFallback()
    {
        using TempString outer = ZA.String("Outer: ", 1);
        using TempString inner = ZA.String("Inner: ", 2);

        Assert.False(outer.IsHeapAllocated);
        Assert.True(inner.IsHeapAllocated);
    }

    [Fact]
    public void String_NestedCalls_BothReturnCorrectContent()
    {
        using TempString outer = ZA.String("Outer: ", 100);
        using TempString inner = ZA.String("Inner: ", 200);

        Assert.Equal("Outer: 100", outer.AsSpan().ToString());
        Assert.Equal("Inner: 200", inner.AsSpan().ToString());
    }

    [Fact]
    public void String_TripleNested_AllReturnCorrectContent()
    {
        using TempString first = ZA.String("First: ", 1);
        using TempString second = ZA.String("Second: ", 2);
        using TempString third = ZA.String("Third: ", 3);

        Assert.Equal("First: 1", first.AsSpan().ToString());
        Assert.Equal("Second: 2", second.AsSpan().ToString());
        Assert.Equal("Third: 3", third.AsSpan().ToString());

        Assert.False(first.IsHeapAllocated);
        Assert.True(second.IsHeapAllocated);
        Assert.True(third.IsHeapAllocated);
    }

    [Fact]
    public void String_DeepNesting_AllWork()
    {
        using TempString a = ZA.String("A");
        using TempString b = ZA.String("B");
        using TempString c = ZA.String("C");
        using TempString d = ZA.String("D");
        using TempString e = ZA.String("E");

        Assert.False(a.IsHeapAllocated);
        Assert.True(b.IsHeapAllocated);
        Assert.True(c.IsHeapAllocated);
        Assert.True(d.IsHeapAllocated);
        Assert.True(e.IsHeapAllocated);

        Assert.Equal("A", a.AsSpan().ToString());
        Assert.Equal("E", e.AsSpan().ToString());
    }

    [Fact]
    public void String_AfterDispose_ThreadStaticReusable()
    {
        TempString temp = ZA.String("Test: ", 123);
        Assert.False(temp.IsHeapAllocated);
        temp.Dispose();

        using TempString temp2 = ZA.String("Test2: ", 456);
        Assert.False(temp2.IsHeapAllocated);
    }

    #endregion

    // ========================================================================
    // TRYSTRING TESTS
    // Tests for TryString methods
    // ========================================================================

    #region TryString Tests

    [Fact]
    public void TryString_ValidInput_ReturnsTrue()
    {
        bool success = ZA.TryString(out TempString result, "Test: ", 42);
        using (result)
        {
            Assert.True(success);
            Assert.Equal("Test: 42", result.AsSpan().ToString());
        }
    }

    [Fact]
    public void TryString_NestedCalls_GracefullyFallsBack()
    {
        bool success1 = ZA.TryString(out TempString result1, "First");
        bool success2 = ZA.TryString(out TempString result2, "Second");

        using (result1)
        using (result2)
        {
            Assert.True(success1);
            Assert.True(success2);
            Assert.False(result1.IsHeapAllocated);
            Assert.True(result2.IsHeapAllocated);
        }
    }

    [Fact]
    public void TryString_EmptyString_ReturnsSuccess()
    {
        bool success = ZA.TryString(out TempString result, "");
        using (result)
        {
            Assert.True(success);
            Assert.Equal(0, result.Length);
        }
    }

    #endregion

    // ========================================================================
    // HEX FORMATTER TESTS
    // Tests for Hex2, Hex4, Hex8, Hex16 formatters
    // ========================================================================

    #region Hex Formatter Tests

    [Fact]
    public void String_Hex2_FormatsAsUppercaseHex()
    {
        using TempString temp = ZA.String("Byte: ", new Hex2(0xFF));
        Assert.Equal("Byte: FF", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Hex2_LeadingZero()
    {
        using TempString temp = ZA.String("Byte: ", new Hex2(0x0A));
        Assert.Equal("Byte: 0A", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Hex2_Zero()
    {
        using TempString temp = ZA.String(new Hex2(0x00));
        Assert.Equal("00", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Hex4_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Short: ", new Hex4(0x1234));
        Assert.Equal("Short: 1234", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Hex4_Zero()
    {
        using TempString temp = ZA.String(new Hex4(0x0000));
        Assert.Equal("0000", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Hex8_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Int: ", new Hex8(0xDEADBEEF));
        Assert.Equal("Int: DEADBEEF", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Hex8_Zero()
    {
        using TempString temp = ZA.String(new Hex8(0x00000000));
        Assert.Equal("00000000", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Hex16_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Long: ", new Hex16(0x123456789ABCDEF0));
        Assert.Equal("Long: 123456789ABCDEF0", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Hex16_Zero()
    {
        using TempString temp = ZA.String(new Hex16(0x0000000000000000));
        Assert.Equal("0000000000000000", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MultipleHexValues_FormatsCorrectly()
    {
        using TempString temp = ZA.String(new Hex2(0xAB), new Hex2(0xCD));
        Assert.Equal("ABCD", temp.AsSpan().ToString());
    }

    #endregion

    // ========================================================================
    // NUMERIC BOUNDARY TESTS
    // Tests for minimum and maximum values of numeric types
    // ========================================================================

    #region Numeric Boundary Tests

    [Fact]
    public void String_MaxInt_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Max: ", int.MaxValue);
        Assert.Equal("Max: 2147483647", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MinInt_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Min: ", int.MinValue);
        Assert.Equal("Min: -2147483648", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MaxLong_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Max: ", long.MaxValue);
        Assert.Equal("Max: 9223372036854775807", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MinLong_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Min: ", long.MinValue);
        Assert.Equal("Min: -9223372036854775808", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_PositiveInfinity_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Infinity: ", double.PositiveInfinity);
        string result = temp.AsSpan().ToString();
        Assert.Contains("Infinity", result);
    }

    [Fact]
    public void String_NegativeInfinity_FormatsCorrectly()
    {
        using TempString temp = ZA.String("NegInf: ", double.NegativeInfinity);
        string result = temp.AsSpan().ToString();
        Assert.Contains("Infinity", result);
    }

    [Fact]
    public void String_NaN_FormatsCorrectly()
    {
        using TempString temp = ZA.String("NaN: ", double.NaN);
        string result = temp.AsSpan().ToString();
        Assert.Contains("NaN", result);
    }

    [Fact]
    public void String_Epsilon_FormatsNonEmpty()
    {
        using TempString temp = ZA.String("Epsilon: ", double.Epsilon);
        Assert.True(temp.Length > 9); // "Epsilon: " + some value
    }

    [Fact]
    public void String_ZeroValues_FormatsCorrectly()
    {
        using TempString temp = ZA.String(0, 0L, 0.0, 0f);
        Assert.Equal("0000", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_NegativeZero_FormatsCorrectly()
    {
        using TempString temp = ZA.String(-0.0);
        // .NET formats -0.0 as "-0"
        Assert.Equal("-0", temp.AsSpan().ToString());
    }

    #endregion

    // ========================================================================
    // UNICODE AND SPECIAL CHARACTER TESTS
    // Tests for Unicode handling and special characters
    // ========================================================================

    #region Unicode Tests

    [Fact]
    public void String_Emoji_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Emoji: ", "🎉🎊🎈");
        Assert.Equal("Emoji: 🎉🎊🎈", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_SurrogatePair_FormatsCorrectly()
    {
        // Musical G clef: U+1D11E (surrogate pair in UTF-16)
        using TempString temp = ZA.String("Music: ", "𝄞");
        Assert.Equal("Music: 𝄞", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_GermanUmlauts_FormatsCorrectly()
    {
        using TempString temp = ZA.String("German: äöüÄÖÜß");
        Assert.Equal("German: äöüÄÖÜß", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_ChineseCharacters_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Chinese: 你好世界");
        Assert.Equal("Chinese: 你好世界", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_ArabicCharacters_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Arabic: مرحبا");
        Assert.Equal("Arabic: مرحبا", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_MixedScripts_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Mixed: Hello 你好 مرحبا 🌍");
        Assert.Equal("Mixed: Hello 你好 مرحبا 🌍", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Newlines_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Line1\nLine2\r\nLine3");
        Assert.Equal("Line1\nLine2\r\nLine3", temp.AsSpan().ToString());
    }

    [Fact]
    public void String_Tabs_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Col1\tCol2\tCol3");
        Assert.Equal("Col1\tCol2\tCol3", temp.AsSpan().ToString());
    }

    #endregion

    // ========================================================================
    // SEQUENTIAL CALL TESTS
    // Tests for repeated sequential usage
    // ========================================================================

    #region Sequential Call Tests

    [Fact]
    public void String_SequentialCalls_WorkCorrectly()
    {
        for (int i = 0; i < 100; i++)
        {
            using TempString temp = ZA.String("Iteration: ", i);
            Assert.Equal($"Iteration: {i}", temp.AsSpan().ToString());
        }
    }

    [Fact]
    public void String_SequentialCalls_AllUseThreadStatic()
    {
        for (int i = 0; i < 10; i++)
        {
            using TempString temp = ZA.String("Test: ", i);
            Assert.False(temp.IsHeapAllocated);
        }
    }

    #endregion

    // ========================================================================
    // DATETIME BOUNDARY TESTS
    // Tests for DateTime min/max values
    // ========================================================================

    #region DateTime Boundary Tests

    [Fact]
    public void String_DateTimeMin_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Date: ", DateTime.MinValue);
        Assert.True(temp.Length > 6);
    }

    [Fact]
    public void String_DateTimeMax_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Date: ", DateTime.MaxValue);
        Assert.True(temp.Length > 6);
    }

    [Fact]
    public void String_TimeSpanMax_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Max: ", TimeSpan.MaxValue);
        Assert.True(temp.Length > 5);
    }

    [Fact]
    public void String_TimeSpanMin_FormatsCorrectly()
    {
        using TempString temp = ZA.String("Min: ", TimeSpan.MinValue);
        Assert.True(temp.Length > 5);
    }

    #endregion
}

