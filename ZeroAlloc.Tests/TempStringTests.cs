// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

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
public sealed class TempStringTests
{
    // ========================================================================
    // BASIC FORMATTING TESTS
    // Tests for fundamental string formatting operations
    // ========================================================================
    #region Basic Formatting

    [Test]
    public async Task String_SimpleString_ReturnsCorrectContent()
    {
        string content;
        {
            using TempString temp = ZA.String("Hello");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Hello");
    }

    [Test]
    public async Task String_IntValue_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Value: ", 42);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Value: 42");
    }

    [Test]
    public async Task String_MultipleStrings_ConcatenatesCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Hello", " ", "World");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Hello World");
    }

    [Test]
    public async Task String_MixedTypes_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("User ", 12345, " logged in");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("User 12345 logged in");
    }

    [Test]
    public async Task String_Length_ReturnsCorrectLength()
    {
        int length;
        {
            using TempString temp = ZA.String("Test", 123);
            length = temp.Length;
        }
        await Assert.That(length).IsEqualTo(7); // "Test123"
    }

    [Test]
    public async Task String_ToString_ReturnsCorrectString()
    {
        string content;
        {
            using TempString temp = ZA.String("Hello ", "World");
            content = temp.ToString();
        }
        await Assert.That(content).IsEqualTo("Hello World");
    }

    [Test]
    public async Task TempString_ToString_Empty_ReturnsEmpty()
    {
        string content;
        {
            using TempString temp = ZA.String("");
            content = temp.ToString();
        }
        await Assert.That(content).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task TempString_IsEmpty_TrueForZeroLength()
    {
        bool isEmpty;
        {
            using TempString temp = ZA.String("");
            isEmpty = temp.IsEmpty;
        }
        await Assert.That(isEmpty).IsTrue();
    }

    [Test]
    public async Task TempString_Dispose_HeapAllocated_CompletesWithoutError()
    {
        bool innerWasHeapAllocated;
        {
            using TempString outer = ZA.String("outer");
            using TempString inner = ZA.String("inner");
            innerWasHeapAllocated = inner.IsHeapAllocated;
        }

        await Assert.That(innerWasHeapAllocated).IsTrue();
    }

    [Test]
    public async Task String_AsSpan_ReturnsSameAsToString()
    {
        string spanContent;
        string content;
        {
            using TempString temp = ZA.String("Test", 123, "abc");
            spanContent = temp.AsSpan().ToString();
            content = temp.ToString();
        }
        await Assert.That(spanContent).IsEqualTo(content);
    }

    #endregion

    // ========================================================================
    // TYPE-SPECIFIC TESTS
    // Tests for formatting specific .NET types
    // ========================================================================

    #region Boolean Formatting

    [Test]
    public async Task String_BooleanTrue_FormatsAsTrue()
    {
        string content;
        {
            using TempString temp = ZA.String("Result: ", true);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Result: True");
    }

    [Test]
    public async Task String_BooleanFalse_FormatsAsFalse()
    {
        string content;
        {
            using TempString temp = ZA.String("Result: ", false);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Result: False");
    }

    [Test]
    public async Task String_MultipleBooleans_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("T=", true, " F=", false);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("T=True F=False");
    }

    #endregion

    #region Character Formatting

    [Test]
    public async Task String_Char_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Letter: ", 'X');
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Letter: X");
    }

    [Test]
    public async Task String_MultipleChars_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String('A', 'B', 'C');
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("ABC");
    }

    [Test]
    public async Task String_UnicodeChar_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Euro: ", '€');
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Euro: €");
    }

    #endregion

    #region Numeric Types Formatting

    [Test]
    public async Task String_Int32_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Int: ", 42);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Int: 42");
    }

    [Test]
    public async Task String_Int64_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("ID: ", 9876543210L);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("ID: 9876543210");
    }

    [Test]
    public async Task String_Double_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Value: ", 3.14159);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Value: 3.14159");
    }

    [Test]
    public async Task String_Float_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Value: ", 1.5f);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Value: 1.5");
    }

    [Test]
    public async Task String_Decimal_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Price: ", 99.99m);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Price: 99.99");
    }

    [Test]
    public async Task String_Byte_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Byte: ", (byte)255);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Byte: 255");
    }

    [Test]
    public async Task String_SByte_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("SByte: ", (sbyte)-128);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("SByte: -128");
    }

    [Test]
    public async Task String_Int16_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Short: ", (short)-32768);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Short: -32768");
    }

    [Test]
    public async Task String_UInt16_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("UShort: ", (ushort)65535);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("UShort: 65535");
    }

    [Test]
    public async Task String_UInt32_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("UInt: ", uint.MaxValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo($"UInt: {uint.MaxValue}");
    }

    [Test]
    public async Task String_UInt64_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("ULong: ", ulong.MaxValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo($"ULong: {ulong.MaxValue}");
    }

    #endregion

    #region Guid and DateTime Formatting

    [Test]
    public async Task String_Guid_FormatsCorrectly()
    {
        Guid guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        string content;
        {
            using TempString temp = ZA.String("GUID: ", guid);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("GUID: 12345678-1234-1234-1234-123456789012");
    }

    [Test]
    public async Task String_GuidEmpty_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("GUID: ", Guid.Empty);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("GUID: 00000000-0000-0000-0000-000000000000");
    }

    [Test]
    public async Task String_DateTime_FormatsCorrectly()
    {
        DateTime dt = new(2025, 6, 15, 14, 30, 45);
        string content;
        {
            using TempString temp = ZA.String("Date: ", dt);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).Contains("2025");
    }

    [Test]
    public async Task String_DateTimeOffset_FormatsCorrectly()
    {
        DateTimeOffset dto = new(2025, 6, 15, 14, 30, 45, TimeSpan.Zero);
        string content;
        {
            using TempString temp = ZA.String("Offset: ", dto);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).Contains("2025");
    }

    [Test]
    public async Task String_TimeSpan_FormatsCorrectly()
    {
        TimeSpan ts = new(1, 23, 45);
        string content;
        {
            using TempString temp = ZA.String("Time: ", ts);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).Contains("23");
    }

    #endregion

    // ========================================================================
    // CULTURE INFO TESTS
    // Tests for locale-specific formatting
    // ========================================================================

    #region CultureInfo Tests

    [Test]
    public async Task String_GermanCulture_FormatsWithComma()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double value = 1234.56;
        string content;
        {
            using TempString temp = ZA.String(german, "Preis: ", value);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Preis: 1234,56");
    }

    [Test]
    public async Task String_USCulture_FormatsWithDot()
    {
        CultureInfo us = CultureInfo.GetCultureInfo("en-US");
        double value = 1234.56;
        string content;
        {
            using TempString temp = ZA.String(us, "Price: ", value);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Price: 1234.56");
    }

    [Test]
    public async Task String_FrenchCulture_FormatsDecimalCorrectly()
    {
        CultureInfo french = CultureInfo.GetCultureInfo("fr-FR");
        decimal value = 9876.54m;
        string content;
        {
            using TempString temp = ZA.String(french, "Prix: ", value);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).Contains(",");
    }

    [Test]
    public async Task String_CultureWithMultipleArgs_AllUseCorrectCulture()
    {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        double d1 = 1.5;
        double d2 = 2.5;
        string content;
        {
            using TempString temp = ZA.String(german, "A=", d1, " B=", d2);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("A=1,5 B=2,5");
    }

    #endregion

    // ========================================================================
    // NESTED CALL TESTS
    // Tests for graceful handling of recursive/nested ZeroAlloc calls
    // ========================================================================

    #region Nested Call Tests

    [Test]
    public async Task String_NestedCalls_OuterUsesThreadStatic()
    {
        bool isHeapAllocated;
        {
            using TempString outer = ZA.String("Outer: ", 1);
            isHeapAllocated = outer.IsHeapAllocated;
        }
        await Assert.That(isHeapAllocated).IsFalse();
    }

    [Test]
    public async Task String_NestedCalls_InnerUsesHeapFallback()
    {
        bool outerIsHeapAllocated;
        bool innerIsHeapAllocated;
        {
            using TempString outer = ZA.String("Outer: ", 1);
            using TempString inner = ZA.String("Inner: ", 2);
            outerIsHeapAllocated = outer.IsHeapAllocated;
            innerIsHeapAllocated = inner.IsHeapAllocated;
        }
        await Assert.That(outerIsHeapAllocated).IsFalse();
        await Assert.That(innerIsHeapAllocated).IsTrue();
    }

    [Test]
    public async Task String_NestedCalls_BothReturnCorrectContent()
    {
        string outerContent;
        string innerContent;
        {
            using TempString outer = ZA.String("Outer: ", 100);
            using TempString inner = ZA.String("Inner: ", 200);
            outerContent = outer.AsSpan().ToString();
            innerContent = inner.AsSpan().ToString();
        }
        await Assert.That(outerContent).IsEqualTo("Outer: 100");
        await Assert.That(innerContent).IsEqualTo("Inner: 200");
    }

    [Test]
    public async Task String_TripleNested_AllReturnCorrectContent()
    {
        string firstContent;
        string secondContent;
        string thirdContent;
        bool firstIsHeapAllocated;
        bool secondIsHeapAllocated;
        bool thirdIsHeapAllocated;
        {
            using TempString first = ZA.String("First: ", 1);
            using TempString second = ZA.String("Second: ", 2);
            using TempString third = ZA.String("Third: ", 3);
            firstContent = first.AsSpan().ToString();
            secondContent = second.AsSpan().ToString();
            thirdContent = third.AsSpan().ToString();
            firstIsHeapAllocated = first.IsHeapAllocated;
            secondIsHeapAllocated = second.IsHeapAllocated;
            thirdIsHeapAllocated = third.IsHeapAllocated;
        }
        await Assert.That(firstContent).IsEqualTo("First: 1");
        await Assert.That(secondContent).IsEqualTo("Second: 2");
        await Assert.That(thirdContent).IsEqualTo("Third: 3");
        await Assert.That(firstIsHeapAllocated).IsFalse();
        await Assert.That(secondIsHeapAllocated).IsTrue();
        await Assert.That(thirdIsHeapAllocated).IsTrue();
    }

    [Test]
    public async Task String_DeepNesting_AllWork()
    {
        bool aIsHeapAllocated;
        bool bIsHeapAllocated;
        bool cIsHeapAllocated;
        bool dIsHeapAllocated;
        bool eIsHeapAllocated;
        string aContent;
        string eContent;
        {
            using TempString a = ZA.String("A");
            using TempString b = ZA.String("B");
            using TempString c = ZA.String("C");
            using TempString d = ZA.String("D");
            using TempString e = ZA.String("E");
            aIsHeapAllocated = a.IsHeapAllocated;
            bIsHeapAllocated = b.IsHeapAllocated;
            cIsHeapAllocated = c.IsHeapAllocated;
            dIsHeapAllocated = d.IsHeapAllocated;
            eIsHeapAllocated = e.IsHeapAllocated;
            aContent = a.AsSpan().ToString();
            eContent = e.AsSpan().ToString();
        }
        await Assert.That(aIsHeapAllocated).IsFalse();
        await Assert.That(bIsHeapAllocated).IsTrue();
        await Assert.That(cIsHeapAllocated).IsTrue();
        await Assert.That(dIsHeapAllocated).IsTrue();
        await Assert.That(eIsHeapAllocated).IsTrue();
        await Assert.That(aContent).IsEqualTo("A");
        await Assert.That(eContent).IsEqualTo("E");
    }

    [Test]
    public async Task String_AfterDispose_ThreadStaticReusable()
    {
        bool isHeapAllocated;
        {
            TempString temp = ZA.String("Test: ", 123);
            isHeapAllocated = temp.IsHeapAllocated;
            temp.Dispose();
        }
        await Assert.That(isHeapAllocated).IsFalse();
        bool temp2IsHeapAllocated;
        {
            using TempString temp2 = ZA.String("Test2: ", 456);
            temp2IsHeapAllocated = temp2.IsHeapAllocated;
        }
        await Assert.That(temp2IsHeapAllocated).IsFalse();
    }

    #endregion

    // ========================================================================
    // TRYSTRING TESTS
    // Tests for TryString methods
    // ========================================================================

    #region TryString Tests

    [Test]
    public async Task TryString_ValidInput_ReturnsTrue()
    {
        bool success = ZA.TryString(out TempString result, "Test: ", 42);
        string content;
        using (result)
        {
            content = result.AsSpan().ToString();
        }
        await Assert.That(success).IsTrue();
        await Assert.That(content).IsEqualTo("Test: 42");
    }

    [Test]
    public async Task TryString_NestedCalls_GracefullyFallsBack()
    {
        bool success1 = ZA.TryString(out TempString result1, "First");
        bool success2 = ZA.TryString(out TempString result2, "Second");
        bool result1IsHeapAllocated;
        bool result2IsHeapAllocated;
        using (result1)
        using (result2)
        {
            result1IsHeapAllocated = result1.IsHeapAllocated;
            result2IsHeapAllocated = result2.IsHeapAllocated;
        }
        await Assert.That(success1).IsTrue();
        await Assert.That(success2).IsTrue();
        await Assert.That(result1IsHeapAllocated).IsFalse();
        await Assert.That(result2IsHeapAllocated).IsTrue();
    }

    [Test]
    public async Task TryString_EmptyString_ReturnsSuccess()
    {
        bool success = ZA.TryString(out TempString result, "");
        int length;
        using (result)
        {
            length = result.Length;
        }
        await Assert.That(success).IsTrue();
        await Assert.That(length).IsEqualTo(0);
    }

    #endregion

    // ========================================================================
    // HEX FORMATTER TESTS
    // Tests for Hex2, Hex4, Hex8, Hex16 formatters
    // ========================================================================

    #region Hex Formatter Tests

    [Test]
    public async Task String_Hex2_FormatsAsUppercaseHex()
    {
        string content;
        {
            using TempString temp = ZA.String("Byte: ", new Hex2(0xFF));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Byte: FF");
    }

    [Test]
    public async Task String_Hex2_LeadingZero()
    {
        string content;
        {
            using TempString temp = ZA.String("Byte: ", new Hex2(0x0A));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Byte: 0A");
    }

    [Test]
    public async Task String_Hex2_Zero()
    {
        string content;
        {
            using TempString temp = ZA.String(new Hex2(0x00));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("00");
    }

    [Test]
    public async Task String_Hex4_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Short: ", new Hex4(0x1234));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Short: 1234");
    }

    [Test]
    public async Task String_Hex4_Zero()
    {
        string content;
        {
            using TempString temp = ZA.String(new Hex4(0x0000));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("0000");
    }

    [Test]
    public async Task String_Hex8_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Int: ", new Hex8(0xDEADBEEF));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Int: DEADBEEF");
    }

    [Test]
    public async Task String_Hex8_Zero()
    {
        string content;
        {
            using TempString temp = ZA.String(new Hex8(0x00000000));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("00000000");
    }

    [Test]
    public async Task String_Hex16_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Long: ", new Hex16(0x123456789ABCDEF0));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Long: 123456789ABCDEF0");
    }

    [Test]
    public async Task String_Hex16_Zero()
    {
        string content;
        {
            using TempString temp = ZA.String(new Hex16(0x0000000000000000));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("0000000000000000");
    }

    [Test]
    public async Task String_MultipleHexValues_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String(new Hex2(0xAB), new Hex2(0xCD));
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("ABCD");
    }

    #endregion

    // ========================================================================
    // NUMERIC BOUNDARY TESTS
    // Tests for minimum and maximum values of numeric types
    // ========================================================================

    #region Numeric Boundary Tests

    [Test]
    public async Task String_MaxInt_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Max: ", int.MaxValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Max: 2147483647");
    }

    [Test]
    public async Task String_MinInt_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Min: ", int.MinValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Min: -2147483648");
    }

    [Test]
    public async Task String_MaxLong_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Max: ", long.MaxValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Max: 9223372036854775807");
    }

    [Test]
    public async Task String_MinLong_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Min: ", long.MinValue);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Min: -9223372036854775808");
    }

    [Test]
    public async Task String_PositiveInfinity_FormatsCorrectly()
    {
        string result;
        {
            using TempString temp = ZA.String("Infinity: ", double.PositiveInfinity);
            result = temp.AsSpan().ToString();
        }
        await Assert.That(result).Contains("Infinity");
    }

    [Test]
    public async Task String_NegativeInfinity_FormatsCorrectly()
    {
        string result;
        {
            using TempString temp = ZA.String("NegInf: ", double.NegativeInfinity);
            result = temp.AsSpan().ToString();
        }
        await Assert.That(result).Contains("Infinity");
    }

    [Test]
    public async Task String_NaN_FormatsCorrectly()
    {
        string result;
        {
            using TempString temp = ZA.String("NaN: ", double.NaN);
            result = temp.AsSpan().ToString();
        }
        await Assert.That(result).Contains("NaN");
    }

    [Test]
    public async Task String_Epsilon_FormatsNonEmpty()
    {
        bool lengthCheck;
        {
            using TempString temp = ZA.String("Epsilon: ", double.Epsilon);
            lengthCheck = temp.Length > 9;
        }
        await Assert.That(lengthCheck).IsTrue(); // "Epsilon: " + some value
    }

    [Test]
    public async Task String_ZeroValues_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String(0, 0L, 0.0, 0f);
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("0000");
    }

    [Test]
    public async Task String_NegativeZero_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String(-0.0);
            content = temp.AsSpan().ToString();
        }
        // .NET formats -0.0 as "-0"
        await Assert.That(content).IsEqualTo("-0");
    }

    #endregion

    // ========================================================================
    // UNICODE AND SPECIAL CHARACTER TESTS
    // Tests for Unicode handling and special characters
    // ========================================================================

    #region Unicode Tests

    [Test]
    public async Task String_Emoji_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Emoji: ", "🎉🎊🎈");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Emoji: 🎉🎊🎈");
    }

    [Test]
    public async Task String_SurrogatePair_FormatsCorrectly()
    {
        // Musical G clef: U+1D11E (surrogate pair in UTF-16)
        string content;
        {
            using TempString temp = ZA.String("Music: ", "𝄞");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Music: 𝄞");
    }

    [Test]
    public async Task String_GermanUmlauts_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("German: äöüÄÖÜß");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("German: äöüÄÖÜß");
    }

    [Test]
    public async Task String_ChineseCharacters_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Chinese: 你好世界");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Chinese: 你好世界");
    }

    [Test]
    public async Task String_ArabicCharacters_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Arabic: مرحبا");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Arabic: مرحبا");
    }

    [Test]
    public async Task String_MixedScripts_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Mixed: Hello 你好 مرحبا 🌍");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Mixed: Hello 你好 مرحبا 🌍");
    }

    [Test]
    public async Task String_Newlines_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Line1\nLine2\r\nLine3");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Line1\nLine2\r\nLine3");
    }

    [Test]
    public async Task String_Tabs_FormatsCorrectly()
    {
        string content;
        {
            using TempString temp = ZA.String("Col1\tCol2\tCol3");
            content = temp.AsSpan().ToString();
        }
        await Assert.That(content).IsEqualTo("Col1\tCol2\tCol3");
    }

    #endregion

    // ========================================================================
    // SEQUENTIAL CALL TESTS
    // Tests for repeated sequential usage
    // ========================================================================

    #region Sequential Call Tests

    [Test]
    public async Task String_SequentialCalls_WorkCorrectly()
    {
        for (int i = 0; i < 100; i++)
        {
            string content;
            {
                using TempString temp = ZA.String("Iteration: ", i);
                content = temp.AsSpan().ToString();
            }
            await Assert.That(content).IsEqualTo($"Iteration: {i}");
        }
    }

    [Test]
    public async Task String_SequentialCalls_AllUseThreadStatic()
    {
        for (int i = 0; i < 10; i++)
        {
            bool isHeapAllocated;
            {
                using TempString temp = ZA.String("Test: ", i);
                isHeapAllocated = temp.IsHeapAllocated;
            }
            await Assert.That(isHeapAllocated).IsFalse();
        }
    }

    #endregion

    // ========================================================================
    // DATETIME BOUNDARY TESTS
    // Tests for DateTime min/max values
    // ========================================================================

    #region DateTime Boundary Tests

    [Test]
    public async Task String_DateTimeMin_FormatsCorrectly()
    {
        bool lengthCheck;
        {
            using TempString temp = ZA.String("Date: ", DateTime.MinValue);
            lengthCheck = temp.Length > 6;
        }
        await Assert.That(lengthCheck).IsTrue();
    }

    [Test]
    public async Task String_DateTimeMax_FormatsCorrectly()
    {
        bool lengthCheck;
        {
            using TempString temp = ZA.String("Date: ", DateTime.MaxValue);
            lengthCheck = temp.Length > 6;
        }
        await Assert.That(lengthCheck).IsTrue();
    }

    [Test]
    public async Task String_TimeSpanMax_FormatsCorrectly()
    {
        bool lengthCheck;
        {
            using TempString temp = ZA.String("Max: ", TimeSpan.MaxValue);
            lengthCheck = temp.Length > 5;
        }
        await Assert.That(lengthCheck).IsTrue();
    }

    [Test]
    public async Task String_TimeSpanMin_FormatsCorrectly()
    {
        bool lengthCheck;
        {
            using TempString temp = ZA.String("Min: ", TimeSpan.MinValue);
            lengthCheck = temp.Length > 5;
        }
        await Assert.That(lengthCheck).IsTrue();
    }

    #endregion
}
