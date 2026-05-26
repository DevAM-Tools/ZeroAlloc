// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Benchmarks;

/// <summary>
/// ZeroAlloc Benchmark Runner.
/// 
/// Run specific benchmarks:
///   dotnet run -c Release -- --filter *StringBenchmarks*
///   dotnet run -c Release -- --filter *Utf8*
///   dotnet run -c Release -- --filter *Bytes*
///   dotnet run -c Release -- --filter *Localized*
/// 
/// Run all benchmarks:
///   dotnet run -c Release
/// 
/// Verify LocalizedString output:
///   dotnet run -- --verify-local
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // Quick verification mode
        if (args.Length > 0 && args[0] == "--verify-local")
        {
            return VerifyLocalizedStringOutput();
        }

        // Print available benchmarks if no args
        if (args.Length == 0)
        {
            Console.WriteLine("ZeroAlloc Benchmarks");
            Console.WriteLine("====================");
            Console.WriteLine();
            Console.WriteLine("Available benchmark classes:");
            Console.WriteLine("  - StringBenchmarks       : String formatting vs interpolation/StringBuilder/ZString");
            Console.WriteLine("  - Utf8StringBenchmarks   : UTF-8 encoding vs Encoding.GetBytes/ZString");
            Console.WriteLine("  - BytesBenchmarks        : Binary serialization vs BinaryPrimitives");
            Console.WriteLine("  - BytesParseBenchmarks   : Binary deserialization");
            Console.WriteLine("  - LocalizedStringBenchmarks : Culture-aware formatting");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -c Release -- --filter *Simple*");
            Console.WriteLine("  dotnet run -c Release -- --filter *StringBenchmarks.Complex*");
            Console.WriteLine("  dotnet run -c Release -- --filter *ZeroAlloc*");
            Console.WriteLine();
        }

        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        return 0;
    }

    /// <summary>
    /// Verifies that LocalizedString produces identical output to interpolation.
    /// </summary>
    static int VerifyLocalizedStringOutput()
    {
        Console.WriteLine("=== LocalizedString Output Verification ===");
        Console.WriteLine();

        // Set German culture
        CultureInfo germanCulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = germanCulture;
        Console.WriteLine($"Current Culture: {CultureInfo.CurrentCulture.Name}");
        Console.WriteLine();

        // Test data
        int userId = 12345;
        double balance = 1234567.89;
        DateTime timestamp = new(2025, 6, 15, 14, 30, 45);

        bool allPassed = true;

        // Test 1: Simple integer
        string interp1 = $"User {userId} logged in";
        string local1 = Z.LocalizedString(germanCulture, "User ", userId, " logged in");
        bool pass1 = interp1 == local1;
        allPassed &= pass1;
        Console.WriteLine("Test 1 - Simple Integer:");
        Console.WriteLine($"  Interpolated:      {interp1}");
        Console.WriteLine($"  LocalizedString:   {local1}");
        Console.WriteLine($"  Equal: {pass1}");
        Console.WriteLine();

        // Test 2: Double with culture (German: comma as decimal separator)
        string interp2 = $"Balance: {balance}";
        string local2 = Z.LocalizedString(germanCulture, "Balance: ", balance);
        bool pass2 = interp2 == local2;
        allPassed &= pass2;
        Console.WriteLine("Test 2 - Double with Culture:");
        Console.WriteLine($"  Interpolated:      {interp2}");
        Console.WriteLine($"  LocalizedString:   {local2}");
        Console.WriteLine($"  Equal: {pass2}");
        Console.WriteLine();

        // Test 3: DateTime
        string interp3 = $"Time: {timestamp}";
        string local3 = Z.LocalizedString(germanCulture, "Time: ", timestamp);
        bool pass3 = interp3 == local3;
        allPassed &= pass3;
        Console.WriteLine("Test 3 - DateTime:");
        Console.WriteLine($"  Interpolated:      {interp3}");
        Console.WriteLine($"  LocalizedString:   {local3}");
        Console.WriteLine($"  Equal: {pass3}");
        Console.WriteLine();

        // Test 4: Complex mixed
        string interp4 = $"User {userId}: {balance} at {timestamp}";
        string local4 = Z.LocalizedString(germanCulture, "User ", userId, ": ", balance, " at ", timestamp);
        bool pass4 = interp4 == local4;
        allPassed &= pass4;
        Console.WriteLine("Test 4 - Complex Mixed:");
        Console.WriteLine($"  Interpolated:      {interp4}");
        Console.WriteLine($"  LocalizedString:   {local4}");
        Console.WriteLine($"  Equal: {pass4}");
        Console.WriteLine();

        // Summary
        Console.WriteLine($"=== All tests passed: {allPassed} ===");
        return allPassed ? 0 : 1;
    }
}
