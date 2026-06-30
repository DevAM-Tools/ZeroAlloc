// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

// ============================================================================
// ZeroAlloc Source Generator
// ============================================================================
// This Roslyn Incremental Source Generator analyzes the user's codebase to find
// classes inheriting from ZeroAllocBase and generates optimized static methods
// for zero-allocation string and byte formatting.
//
// ARCHITECTURE OVERVIEW:
// ----------------------
// 1. SYNTAX ANALYSIS PHASE
//    - IsPotentialUserClass: Quick syntax filter for class declarations
//    - GetUserClassInfo: Semantic analysis to confirm ZeroAllocBase inheritance
//    - IsPotentialApiCall: Quick syntax filter for method invocations
//    - GetMethodCallInfo: Semantic analysis to extract method call details
//
// 2. TYPE ANALYSIS PHASE
//    - AnalyzeArgumentType: Categorizes each argument type for optimal formatting
//    - Determines if type implements ISpanFormattable, IUtf8SpanFormattable, etc.
//    - Detects potential recursive ZeroAlloc usage that could cause heap fallback
//
// 3. CODE GENERATION PHASE
//    - GenerateStringMethod: Generates String() returning TempString
//    - GenerateUtf8Method: Generates Utf8() returning TempBytes
//    - GenerateBytesMethod: Generates Bytes() for binary serialization
//    - Each method has a Try* variant for non-throwing error handling
//
// KEY FEATURES:
// - Uses ThreadStatic buffers for zero-allocation in the common case
// - Supports buffer auto-grow when content exceeds capacity
// - Directly uses .NET interfaces (ISpanFormattable, IUtf8SpanFormattable)
// - Configurable via MSBuild properties (buffer size, overflow behavior)
// - Issues diagnostics for unsupported types and potential issues
//
// CONFIGURATION (via MSBuild properties in .csproj):
// - ZeroAlloc_DefaultBufferSize: Initial buffer size (default: 2 MiB)
// - ZeroAlloc_RecursiveHeapFallback: Allow heap fallback for nested calls (default: true)
// - ZeroAlloc_BufferOverflowBehavior: Grow | HeapFallback | Throw (default: Grow)
//
// GENERATED OUTPUT:
// For each user class "ZA : ZeroAllocBase", generates "ZA.ZeroAlloc.g.cs" with:
// - String(...) methods for each unique call signature
// - TryString(...) methods for safe formatting
// - Utf8(...) and TryUtf8(...) for UTF-8 output
// - Bytes(...) and TryBytes(...) for binary serialization
// ============================================================================


namespace ZeroAlloc.Generator;

// ============================================================================
// DIAGNOSTIC DESCRIPTORS
// ============================================================================

/// <summary>
/// Diagnostic descriptors for ZeroAlloc source generator warnings and errors.
/// </summary>
internal static class Diagnostics
{
    /// <summary>
    /// ZA1001: Unsupported argument type.
    /// </summary>
    public static readonly DiagnosticDescriptor UnsupportedType = new(
        id: "ZA1001",
        title: "Unsupported argument type",
        messageFormat: "Type '{0}' is not supported by ZeroAlloc. Implement ISpanFormattable, IUtf8SpanFormattable, or IStringSize.",
        category: "ZeroAlloc",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA1002: Potential recursive API call detected.
    /// </summary>
    public static readonly DiagnosticDescriptor PotentialRecursion = new(
        id: "ZA1002",
        title: "Potential recursive API call",
        messageFormat: "Type '{0}' implements ISpanFormattable and may use ZeroAlloc APIs internally, which could cause recursive buffer acquisition. Consider using ZeroStringBuilder with a stack-allocated buffer directly in your TryFormat implementation.",
        category: "ZeroAlloc",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA1003: Nested call detected at runtime (informational).
    /// </summary>
    public static readonly DiagnosticDescriptor NestedCallInfo = new(
        id: "ZA1003",
        title: "Nested ZeroAlloc call",
        messageFormat: "This call is nested within another ZeroAlloc call. A heap-allocated buffer will be used as fallback.",
        category: "ZeroAlloc",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA1004: Type falls back to ToString() allocation.
    /// Issued when a type does not implement ISpanFormattable or IUtf8SpanFormattable,
    /// requiring a heap-allocating ToString() call for formatting.
    /// </summary>
    public static readonly DiagnosticDescriptor ToStringFallback = new(
        id: "ZA1004",
        title: "ToString() fallback causes allocation",
        messageFormat: "Type '{0}' does not implement ISpanFormattable or IUtf8SpanFormattable. Formatting will use ToString() which allocates. Consider implementing ISpanFormattable for zero-allocation formatting.",
        category: "ZeroAlloc",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA0001: ZeroAllocBase derived class must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor ClassMustBePartial = new(
        id: "ZA0001",
        title: "ZeroAllocBase derived class must be partial",
        messageFormat: "Class '{0}' inherits from ZeroAllocBase but is not declared as partial. Add the 'partial' modifier to enable source generation.",
        category: "ZeroAlloc",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// ZA0002: ZeroAllocBase derived class should be internal.
    /// </summary>
    public static readonly DiagnosticDescriptor ClassShouldBeInternal = new(
        id: "ZA0002",
        title: "ZeroAllocBase derived class should be internal",
        messageFormat: "Class '{0}' inherits from ZeroAllocBase but is declared as public. Consider using 'internal partial class' for better encapsulation.",
        category: "ZeroAlloc",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}

// ============================================================================
// GENERATOR OPTIONS
// ============================================================================

/// <summary>
/// Defines behavior when formatted content exceeds the buffer size.
/// </summary>
internal enum BufferOverflowBehavior
{
    /// <summary>Dynamically grow the ThreadStatic buffer.</summary>
    Grow,
    /// <summary>Allocate a new heap buffer for this call only.</summary>
    HeapFallback,
    /// <summary>Throw InvalidOperationException.</summary>
    Throw
}

/// <summary>
/// Configuration options read from MSBuild properties.
/// </summary>
/// <param name="DefaultBufferSize">Initial ThreadStatic buffer size in bytes/chars.</param>
/// <param name="RecursiveHeapFallback">Allow heap allocation when ThreadStatic buffer is already in use (nested call).</param>
/// <param name="BufferOverflowBehavior">Behavior when content exceeds buffer size.</param>
internal readonly record struct GeneratorOptions(
    int DefaultBufferSize,
    bool RecursiveHeapFallback,
    BufferOverflowBehavior BufferOverflowBehavior);

// ============================================================================
// MAIN GENERATOR
// ============================================================================

[Generator]
public sealed class ZeroAllocGenerator : IIncrementalGenerator
{
    // ========================================================================
    // INITIALIZATION
    // ========================================================================

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Step 1: Read project options from MSBuild properties
        IncrementalValueProvider<GeneratorOptions> options = context.AnalyzerConfigOptionsProvider
            .Select((provider, _) => _ReadOptions(provider));

        // Step 2: Find all classes that inherit from ZeroAllocBase
        IncrementalValuesProvider<UserClassInfo?> userClasses = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => _IsPotentialUserClass(node),
                transform: static (ctx, _) => _GetUserClassInfo(ctx))
            .Where(static info => info is not null);

        // Step 3: Find all method invocations on user classes
        IncrementalValuesProvider<MethodCallInfo?> methodCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => _IsPotentialApiCall(node),
                transform: static (ctx, _) => _GetMethodCallInfo(ctx))
            .Where(static info => info is not null);

        // Step 4: Combine everything and generate
        IncrementalValueProvider<
            (((ImmutableArray<UserClassInfo?> Left, ImmutableArray<MethodCallInfo?> Right) Left, Compilation Right) Left, GeneratorOptions Right)> combined =
            userClasses.Collect()
            .Combine(methodCalls.Collect())
            .Combine(context.CompilationProvider)
            .Combine(options);

        context.RegisterSourceOutput(combined, _GenerateSource);
    }

    // ========================================================================
    // OPTIONS READING
    // ========================================================================

    /// <summary>
    /// Reads generator options from MSBuild properties.
    /// </summary>
    private static GeneratorOptions _ReadOptions(AnalyzerConfigOptionsProvider provider)
    {
        // Read buffer size (default 2 MiB)
        provider.GlobalOptions.TryGetValue("build_property.ZeroAlloc_DefaultBufferSize", out string? bufferSizeStr);
        int defaultBufferSize = int.TryParse(bufferSizeStr, out int parsed) ? parsed : 2_097_152;

        // Read recursive heap fallback setting (default true)
        provider.GlobalOptions.TryGetValue("build_property.ZeroAlloc_RecursiveHeapFallback", out string? recursiveHeapFallbackStr);
        bool recursiveHeapFallback = !string.Equals(recursiveHeapFallbackStr, "false", StringComparison.OrdinalIgnoreCase);

        // Read buffer overflow behavior (default Grow)
        provider.GlobalOptions.TryGetValue("build_property.ZeroAlloc_BufferOverflowBehavior", out string? overflowBehaviorStr);
        BufferOverflowBehavior overflowBehavior = overflowBehaviorStr switch
        {
            not null when string.Equals(overflowBehaviorStr, "heapfallback", StringComparison.OrdinalIgnoreCase)
                => BufferOverflowBehavior.HeapFallback,
            not null when string.Equals(overflowBehaviorStr, "throw", StringComparison.OrdinalIgnoreCase)
                => BufferOverflowBehavior.Throw,
            _ => BufferOverflowBehavior.Grow
        };

        return new GeneratorOptions(defaultBufferSize, recursiveHeapFallback, overflowBehavior);
    }

    // ========================================================================
    // SYNTAX ANALYSIS - User Class Detection
    // ========================================================================
    // This phase uses Roslyn's syntax provider to identify candidate classes.
    // We use a two-phase approach for performance:
    //   1. IsPotentialUserClass: Fast syntax-only check (no semantic info)
    //   2. GetUserClassInfo: Full semantic analysis for confirmed candidates
    // ========================================================================

    /// <summary>
    /// Quick syntax check to filter potential user classes.
    /// </summary>
    /// <remarks>
    /// This is the first filter in the incremental pipeline and should be as fast
    /// as possible. We only check if the node is a class declaration - semantic
    /// analysis (checking inheritance) happens in <see cref="GetUserClassInfo"/>.
    /// </remarks>
    /// <param name="node">The syntax node to check.</param>
    /// <returns><c>true</c> if the node is a class declaration; otherwise <c>false</c>.</returns>
    private static bool _IsPotentialUserClass(SyntaxNode node) => node is ClassDeclarationSyntax;

    /// <summary>
    /// Performs semantic analysis to verify if a class inherits from <c>ZeroAllocBase</c>.
    /// </summary>
    /// <remarks>
    /// Called for each class declaration that passed <see cref="IsPotentialUserClass"/>.
    /// We verify:
    /// <list type="bullet">
    ///   <item>The class has a base type list</item>
    ///   <item>The direct base type is named "ZeroAllocBase"</item>
    ///   <item>The base type is from the ZeroAlloc namespace (to avoid false positives)</item>
    /// </list>
    /// </remarks>
    /// <param name="ctx">The generator syntax context containing semantic model.</param>
    /// <returns>A <see cref="UserClassInfo"/> if the class qualifies; otherwise <c>null</c>.</returns>
    private static UserClassInfo? _GetUserClassInfo(GeneratorSyntaxContext ctx)
    {
        ClassDeclarationSyntax classDecl = (ClassDeclarationSyntax)ctx.Node;

        // Check if any base type is ZeroAllocBase
        if (classDecl.BaseList is null
            || ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol classSymbol)
        {
            return null;
        }

        INamedTypeSymbol? baseType = classSymbol.BaseType;
        if (baseType is null || baseType.Name != "ZeroAllocBase")
        {
            return null;
        }

        // Verify it's our ZeroAllocBase (in ZeroAlloc namespace)
        if (baseType.ContainingNamespace?.ToDisplayString() != "ZeroAlloc")
        {
            return null;
        }

        // Get class information
        string className = classSymbol.Name;
        string? namespaceName = classSymbol.ContainingNamespace?.IsGlobalNamespace == true
            ? null
            : classSymbol.ContainingNamespace?.ToDisplayString();
        string fullName = classSymbol.ToDisplayString();
        bool isPublic = classSymbol.DeclaredAccessibility == Accessibility.Public;

        // Check if the class is declared as partial
        bool isPartial = classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

        return new UserClassInfo(className, namespaceName, fullName, classDecl.GetLocation(), isPublic, isPartial);
    }

    // ========================================================================
    // SYNTAX ANALYSIS - Method Call Detection
    // ========================================================================
    // This phase identifies calls to ZeroAlloc API methods (String, Utf8, Bytes).
    // Similar to user class detection, we use a two-phase approach:
    //   1. IsPotentialApiCall: Fast syntax check for invocation expressions
    //   2. GetMethodCallInfo: Semantic analysis to verify target type and collect argument info
    // ========================================================================

    /// <summary>
    /// Quick syntax check to filter potential API method calls.
    /// </summary>
    /// <remarks>
    /// This filter runs on every syntax node and should be extremely fast.
    /// We simply check if the node is an invocation expression.
    /// Semantic verification happens in <see cref="GetMethodCallInfo"/>.
    /// </remarks>
    /// <param name="node">The syntax node to check.</param>
    /// <returns><c>true</c> if the node is an invocation expression; otherwise <c>false</c>.</returns>
    private static bool _IsPotentialApiCall(SyntaxNode node) => node is InvocationExpressionSyntax;

    /// <summary>
    /// Performs semantic analysis to verify if an invocation is a ZeroAlloc API call.
    /// </summary>
    /// <remarks>
    /// For each invocation that passed <see cref="IsPotentialApiCall"/>, we verify:
    /// <list type="bullet">
    ///   <item>The invocation is a member access (e.g., <c>MyClass.String(...)</c>)</item>
    ///   <item>The method name is one of our API methods (String, TryString, Utf8, TryUtf8, Bytes, TryBytes)</item>
    ///   <item>The target type inherits from ZeroAllocBase</item>
    /// </list>
    /// We also analyze each argument type to determine the optimal formatting strategy.
    /// </remarks>
    /// <param name="ctx">The generator syntax context containing semantic model.</param>
    /// <returns>A <see cref="MethodCallInfo"/> if the call qualifies; otherwise <c>null</c>.</returns>
    private static MethodCallInfo? _GetMethodCallInfo(GeneratorSyntaxContext ctx)
    {
        InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)ctx.Node;

        // Must be a member access (ClassName.MethodName)
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return null;
        }

        // Get method name
        string methodName = memberAccess.Name.Identifier.Text;

        // Check if it's one of our supported methods
        if (!_IsApiMethodName(methodName))
        {
            return null;
        }

        // Get the type being called on
        SymbolInfo symbolInfo = ctx.SemanticModel.GetSymbolInfo(memberAccess.Expression);
        if (symbolInfo.Symbol is not INamedTypeSymbol targetType)
        {
            return null;
        }

        // Check if target inherits from ZeroAllocBase
        INamedTypeSymbol? baseType = targetType.BaseType;
        if (baseType is null || baseType.Name != "ZeroAllocBase")
        {
            return null;
        }

        if (baseType.ContainingNamespace?.ToDisplayString() != "ZeroAlloc")
        {
            return null;
        }

        // Collect argument type information
        List<ArgumentTypeInfo> argTypes = new List<ArgumentTypeInfo>();
        foreach (ArgumentSyntax arg in invocation.ArgumentList.Arguments)
        {
            TypeInfo typeInfo = ctx.SemanticModel.GetTypeInfo(arg.Expression);
            ITypeSymbol? argType = typeInfo.Type;
            if (argType is null)
            {
                return null; // Can't determine type
            }

            ArgumentTypeInfo argTypeInfo = _AnalyzeArgumentType(argType, ctx.SemanticModel.Compilation);
            argTypes.Add(argTypeInfo);
        }

        return new MethodCallInfo(
            targetType.ToDisplayString(),
            methodName,
            argTypes.ToImmutableArray(),
            invocation.GetLocation());
    }

    /// <summary>
    /// Checks if the method name is one of our API methods.
    /// </summary>
    private static bool _IsApiMethodName(string name)
    {
        return name is "String" or "TryString" or "Utf8" or "TryUtf8" or "Bytes" or "TryBytes"
            or "LocalizedString" or "TryLocalizedString" or "LocalizedUtf8" or "TryLocalizedUtf8"
            or "Lazy" or "LazyInterpolated";
    }

    // ========================================================================
    // TYPE ANALYSIS
    // ========================================================================
    // This section categorizes argument types to determine the optimal formatting
    // strategy for each. The key categories are:
    //
    // - String/Char/Boolean: Special-cased for direct handling
    // - SpanFormattable: Uses ISpanFormattable.TryFormat for zero-allocation char formatting
    // - Utf8SpanFormattable: Uses IUtf8SpanFormattable.TryFormat for UTF-8 byte formatting
    // - BinarySerializable: Uses IBinarySerializable for binary data output
    // - Object: Falls back to ToString() (generates allocation warning ZA1004)
    //
    // We also detect types that might use ZeroAlloc internally to warn about
    // potential recursive buffer acquisition (ZA1002).
    // ========================================================================

    /// <summary>
    /// Analyzes an argument type to determine the optimal formatting strategy.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method examines the type's interfaces and special characteristics to
    /// categorize it for code generation. The order of checks matters:
    /// </para>
    /// <list type="number">
    ///   <item>Special types (string, char, bool, CultureInfo) are handled first</item>
    ///   <item>IBinarySerializable for binary formatting</item>
    ///   <item>ISpanFormattable for efficient char-based formatting</item>
    ///   <item>IUtf8SpanFormattable for efficient UTF-8 formatting</item>
    ///   <item>Fallback to Object category (uses ToString())</item>
    /// </list>
    /// <para>
    /// We also check for IStringSize and IUtf8Size interfaces which allow types
    /// to provide size hints for buffer pre-allocation.
    /// </para>
    /// </remarks>
    /// <param name="type">The type symbol to analyze.</param>
    /// <param name="compilation">The compilation context for resolving type metadata.</param>
    /// <returns>Detailed type information including category and formatting hints.</returns>
    private static ArgumentTypeInfo _AnalyzeArgumentType(ITypeSymbol type, Compilation compilation)
    {
        // Strip nullable annotation for signature deduplication (string? and string have the same CLR signature)
        string fullTypeName = type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
        string shortName = _GetShortName(type);
        bool isRefStruct = type.IsRefLikeType;
        bool isValueType = type.IsValueType;

        // Check for special types first
        if (type.SpecialType == SpecialType.System_String)
        {
            // String: length is known at runtime via .Length property
            return new ArgumentTypeInfo(fullTypeName, shortName, TypeCategory.String, isRefStruct, isValueType,
                MayUseZeroAllocInternally: false, KnownSize: null, MinimumSize: 0,
                ImplementsIStringSize: false, ImplementsIUtf8Size: false);
        }

        if (type.SpecialType == SpecialType.System_Char)
        {
            // Char: always 1 character
            return new ArgumentTypeInfo(fullTypeName, shortName, TypeCategory.Char, isRefStruct, isValueType,
                MayUseZeroAllocInternally: false, KnownSize: 1, MinimumSize: 1);
        }

        if (type.SpecialType == SpecialType.System_Boolean)
        {
            // Boolean: "True" (4) or "False" (5)
            return new ArgumentTypeInfo(fullTypeName, shortName, TypeCategory.Boolean, isRefStruct, isValueType,
                MayUseZeroAllocInternally: false, KnownSize: null, MinimumSize: 4);
        }

        // Check for CultureInfo
        if (fullTypeName == "System.Globalization.CultureInfo")
        {
            return new ArgumentTypeInfo(fullTypeName, "culture", TypeCategory.CultureInfo, isRefStruct, isValueType);
        }

        // Check for ZeroAlloc.Raw (ref struct, cannot implement interfaces - must detect by name)
        if (fullTypeName == "ZeroAlloc.Raw")
        {
            return new ArgumentTypeInfo(fullTypeName, "raw", TypeCategory.RawBytes, isRefStruct, isValueType,
                MayUseZeroAllocInternally: false, KnownSize: null, MinimumSize: 0,
                ImplementsIStringSize: false, ImplementsIUtf8Size: false);
        }

        // Check for IFormatProvider (interface)
        INamedTypeSymbol? formatProviderInterface = compilation.GetTypeByMetadataName("System.IFormatProvider");
        if (formatProviderInterface is not null &&
            (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, formatProviderInterface) ||
             type.AllInterfaces.Contains(formatProviderInterface, SymbolEqualityComparer.Default)))
        {
            return new ArgumentTypeInfo(fullTypeName, "provider", TypeCategory.FormatProvider, isRefStruct, isValueType);
        }

        // Get known size for numeric types
        (int? knownSize, int? minSize) = _GetNumericTypeSizeInfo(type);

        // Check for IStringSize (for TryGetStringSize)
        INamedTypeSymbol? stringSize = compilation.GetTypeByMetadataName("ZeroAlloc.IStringSize");
        bool implementsIStringSize = stringSize is not null &&
            type.AllInterfaces.Contains(stringSize, SymbolEqualityComparer.Default);

        // Check for IUtf8Size (for TryGetUtf8Size)
        INamedTypeSymbol? utf8Size = compilation.GetTypeByMetadataName("ZeroAlloc.IUtf8Size");
        bool implementsIUtf8Size = utf8Size is not null &&
            type.AllInterfaces.Contains(utf8Size, SymbolEqualityComparer.Default);

        // Check for IBinarySerializable (for binary serialization)
        INamedTypeSymbol? binarySerializable = compilation.GetTypeByMetadataName("ZeroAlloc.IBinarySerializable");
        if (binarySerializable is not null && type.AllInterfaces.Contains(binarySerializable, SymbolEqualityComparer.Default))
        {
            return new ArgumentTypeInfo(fullTypeName, shortName, TypeCategory.BinarySerializable, isRefStruct, isValueType,
                MayUseZeroAllocInternally: false, KnownSize: knownSize, MinimumSize: minSize,
                ImplementsIStringSize: implementsIStringSize, ImplementsIUtf8Size: implementsIUtf8Size);
        }

        // Check for ISpanFormattable
        INamedTypeSymbol? spanFormattable = compilation.GetTypeByMetadataName("System.ISpanFormattable");
        if (spanFormattable is not null && type.AllInterfaces.Contains(spanFormattable, SymbolEqualityComparer.Default))
        {
            bool mayUseZeroAlloc = _MayUseZeroAllocInternally(type, compilation);
            return new ArgumentTypeInfo(fullTypeName, shortName, TypeCategory.SpanFormattable, isRefStruct, isValueType,
                MayUseZeroAllocInternally: mayUseZeroAlloc, KnownSize: knownSize, MinimumSize: minSize,
                ImplementsIStringSize: implementsIStringSize, ImplementsIUtf8Size: implementsIUtf8Size);
        }

        // Check for IUtf8SpanFormattable
        INamedTypeSymbol? utf8SpanFormattable = compilation.GetTypeByMetadataName("System.IUtf8SpanFormattable");
        if (utf8SpanFormattable is not null && type.AllInterfaces.Contains(utf8SpanFormattable, SymbolEqualityComparer.Default))
        {
            bool mayUseZeroAlloc = _MayUseZeroAllocInternally(type, compilation);
            return new ArgumentTypeInfo(fullTypeName, shortName, TypeCategory.Utf8SpanFormattable, isRefStruct, isValueType,
                MayUseZeroAllocInternally: mayUseZeroAlloc, KnownSize: knownSize, MinimumSize: minSize,
                ImplementsIStringSize: implementsIStringSize, ImplementsIUtf8Size: implementsIUtf8Size);
        }

        // Check if type is an array (arrays cannot implement interfaces)
        bool isArray = type is IArrayTypeSymbol;

        // Check if type is a ZeroAlloc internal type (TempString, TempBytes, etc.)
        bool isZeroAllocInternal = fullTypeName.StartsWith("ZeroAlloc.Temp", StringComparison.Ordinal) ||
                                    fullTypeName.StartsWith("ZeroAlloc.Stack", StringComparison.Ordinal);

        // Fallback: assume it has ToString()
        return new ArgumentTypeInfo(fullTypeName, shortName, TypeCategory.Object, isRefStruct, isValueType,
            IsArray: isArray, IsZeroAllocInternal: isZeroAllocInternal);
    }

    /// <summary>
    /// Gets size information for numeric types.
    /// </summary>
    /// <returns>Tuple of (exact size if fixed, minimum size if variable).</returns>
    private static (int? KnownSize, int? MinSize) _GetNumericTypeSizeInfo(ITypeSymbol type)
    {
        // For numeric types, we know the maximum formatted length
        return type.SpecialType switch
        {
            // Signed integers: max digits + sign
            SpecialType.System_SByte => (null, 1),    // -128 to 127 (max 4 chars)
            SpecialType.System_Int16 => (null, 1),    // -32768 to 32767 (max 6 chars)
            SpecialType.System_Int32 => (null, 1),    // max 11 chars
            SpecialType.System_Int64 => (null, 1),    // max 20 chars

            // Unsigned integers: just digits
            SpecialType.System_Byte => (null, 1),     // 0 to 255 (max 3 chars)
            SpecialType.System_UInt16 => (null, 1),   // max 5 chars
            SpecialType.System_UInt32 => (null, 1),   // max 10 chars
            SpecialType.System_UInt64 => (null, 1),   // max 20 chars

            // Floating point: variable
            SpecialType.System_Single => (null, 1),
            SpecialType.System_Double => (null, 1),
            SpecialType.System_Decimal => (null, 1),

            _ => (null, null)
        };
    }

    /// <summary>
    /// Gets a short name suitable for parameter naming.
    /// </summary>
    private static string _GetShortName(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => "s",
            SpecialType.System_Char => "c",
            SpecialType.System_Boolean => "b",
            SpecialType.System_Byte => "u8",
            SpecialType.System_SByte => "i8",
            SpecialType.System_Int16 => "i16",
            SpecialType.System_UInt16 => "u16",
            SpecialType.System_Int32 => "i",
            SpecialType.System_UInt32 => "u",
            SpecialType.System_Int64 => "i64",
            SpecialType.System_UInt64 => "u64",
            SpecialType.System_Single => "f",
            SpecialType.System_Double => "d",
            SpecialType.System_Decimal => "m",
            _ => "v"
        };
    }

    /// <summary>
    /// Determines if a user-defined type might use ZeroAlloc APIs internally.
    /// This is used to detect potential recursive buffer acquisition that could
    /// cause the inner call to fall back to heap allocation.
    /// </summary>
    /// <remarks>
    /// A type is considered potentially recursive only if we can prove it uses ZeroAlloc APIs:
    /// <list type="bullet">
    ///   <item>For types with source available: Analyze TryFormat method body for ZeroAlloc calls</item>
    ///   <item>For types without source: Only warn if they inherit from ZeroAllocBase</item>
    ///   <item>System/Microsoft types: Never use ZeroAlloc internally</item>
    /// </list>
    /// This provides precise detection instead of the overly broad "assembly references ZeroAlloc" check.
    /// </remarks>
    private static bool _MayUseZeroAllocInternally(ITypeSymbol type, Compilation compilation)
    {
        // Built-in types and System types cannot use ZeroAlloc internally
        string? containingNamespace = type.ContainingNamespace?.ToDisplayString();
        if (containingNamespace is null)
        {
            return false;
        }

        if (containingNamespace.StartsWith("System", StringComparison.Ordinal) ||
            containingNamespace.StartsWith("Microsoft", StringComparison.Ordinal))
        {
            return false;
        }

        // Check if the type inherits from ZeroAllocBase (definite ZeroAlloc user)
        INamedTypeSymbol? zeroAllocBase = compilation.GetTypeByMetadataName("ZeroAlloc.ZeroAllocBase");
        if (zeroAllocBase is not null && _InheritsFrom(type, zeroAllocBase))
        {
            return true;
        }

        // If the type has source available, analyze the TryFormat method body
        if (type is INamedTypeSymbol namedType)
        {
            IMethodSymbol? tryFormatMethod = _FindTryFormatMethod(namedType);
            if (tryFormatMethod is not null)
            {
                // Check if we have access to the source code
                foreach (SyntaxReference syntaxRef in tryFormatMethod.DeclaringSyntaxReferences)
                {
                    SyntaxNode syntaxNode = syntaxRef.GetSyntax();
                    if (_ContainsZeroAllocApiCalls(syntaxNode))
                    {
                        return true;
                    }
                }
            }
        }

        // No evidence of ZeroAlloc usage found
        return false;
    }

    /// <summary>
    /// Checks if a type inherits from a specified base type.
    /// </summary>
    private static bool _InheritsFrom(ITypeSymbol type, INamedTypeSymbol baseType)
    {
        INamedTypeSymbol? current = type.BaseType;
        while (current is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
            current = current.BaseType;
        }
        return false;
    }

    /// <summary>
    /// Finds the TryFormat method on a type (ISpanFormattable or IUtf8SpanFormattable implementation).
    /// </summary>
    private static IMethodSymbol? _FindTryFormatMethod(INamedTypeSymbol type)
    {
        // Look for TryFormat method with typical signatures
        foreach (ISymbol member in type.GetMembers("TryFormat"))
        {
            if (member is IMethodSymbol method && !method.IsStatic)
            {
                // ISpanFormattable.TryFormat has Span<char> first param
                // IUtf8SpanFormattable.TryFormat has Span<byte> first param
                if (method.Parameters.Length >= 2)
                {
                    ITypeSymbol firstParamType = method.Parameters[0].Type;
                    if (firstParamType is INamedTypeSymbol namedParamType &&
                        namedParamType.OriginalDefinition.ToDisplayString() == "System.Span<T>")
                    {
                        return method;
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Analyzes a syntax node to determine if it contains ZeroAlloc API calls.
    /// </summary>
    /// <remarks>
    /// Searches for invocations of known ZeroAlloc methods:
    /// <list type="bullet">
    ///   <item>ZA.String(), ZA.Utf8(), ZA.Bytes() and their Try* variants</item>
    ///   <item>TempString, TempBytes, TempStringBuilder usage</item>
    ///   <item>ZeroStringBuilder, ZeroBytesBuilder methods</item>
    /// </list>
    /// </remarks>
    private static bool _ContainsZeroAllocApiCalls(SyntaxNode node)
    {
        // Known ZeroAlloc API method names that could cause recursion
        HashSet<string> zeroAllocMethods =
        [
            "String", "TryString",
            "Utf8", "TryUtf8",
            "Bytes", "TryBytes",
            "Acquire", "Release"
        ];

        // Known ZeroAlloc type names
        HashSet<string> zeroAllocTypes =
        [
            "TempString", "TempBytes",
            "TempStringBuilder", "TempBytesBuilder",
            "ZeroStringBuilder", "ZeroBytesBuilder",
            "StackStringBuilder", "StackBytesBuilder"
        ];

        foreach (SyntaxNode descendant in node.DescendantNodesAndSelf())
        {
            if (_DescendantUsesZeroAllocApi(descendant, zeroAllocMethods, zeroAllocTypes))
            {
                return _MatchedZeroAllocApiCall();
            }
        }

        return false;
    }

    private static bool _DescendantUsesZeroAllocApi(
        SyntaxNode descendant,
        HashSet<string> zeroAllocMethods,
        HashSet<string> zeroAllocTypes)
    {
        if (descendant is InvocationExpressionSyntax invocation)
        {
            string invocationText = invocation.Expression.ToString();

            foreach (string method in zeroAllocMethods)
            {
                if (invocationText.EndsWith("." + method, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        if (descendant is ObjectCreationExpressionSyntax creation)
        {
            string typeName = creation.Type.ToString();
            foreach (string zeroAllocType in zeroAllocTypes)
            {
                if (typeName.Contains(zeroAllocType, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        if (descendant is IdentifierNameSyntax identifier)
        {
            if (identifier.Ancestors().Any(static ancestor => ancestor is ObjectCreationExpressionSyntax))
            {
                return false;
            }

            string name = identifier.Identifier.Text;
            foreach (string zeroAllocType in zeroAllocTypes)
            {
                if (name == zeroAllocType)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool _MatchedZeroAllocApiCall()
    {
        return true;
    }

    // ========================================================================
    // CODE GENERATION
    // ========================================================================
    // This section generates the actual source code for each user class.
    // The generated code follows this pattern:
    //
    // 1. A partial class matching the user's class declaration
    // 2. Configuration constants from MSBuild properties
    // 3. String()/TryString() methods for string formatting
    // 4. Utf8()/TryUtf8() methods for UTF-8 byte formatting
    // 5. Bytes()/TryBytes() methods for binary serialization
    //
    // Each method uses TempStringBuilder or TempBytesBuilder internally
    // to achieve zero-allocation formatting in the common case.
    // ========================================================================

    /// <summary>
    /// Main entry point for code generation. Called by Roslyn when source output is ready.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method processes the combined incremental pipeline data:
    /// </para>
    /// <list type="bullet">
    ///   <item>Groups user classes by their full name (deduplicates partial classes)</item>
    ///   <item>Groups method calls by target class</item>
    ///   <item>Reports diagnostics for problematic types (ZA1002, ZA1004)</item>
    ///   <item>Generates a single source file per user class</item>
    /// </list>
    /// </remarks>
    /// <param name="ctx">The source production context for adding generated sources and diagnostics.</param>
    /// <param name="input">Combined data from the incremental pipeline.</param>
    private static void _GenerateSource(
        SourceProductionContext ctx,
        (((ImmutableArray<UserClassInfo?> UserClasses, ImmutableArray<MethodCallInfo?> MethodCalls), Compilation Compilation), GeneratorOptions Options) input)
    {
        ImmutableArray<UserClassInfo?> userClasses = input.Item1.Item1.UserClasses;
        ImmutableArray<MethodCallInfo?> methodCalls = input.Item1.Item1.MethodCalls;
        Compilation compilation = input.Item1.Compilation;
        GeneratorOptions options = input.Options;

        // Group user classes by full name
        Dictionary<string, UserClassInfo> classMap = new Dictionary<string, UserClassInfo>();
        foreach (UserClassInfo? info in userClasses)
        {
            if (info is not null && !classMap.ContainsKey(info.Value.FullName))
            {
                classMap[info.Value.FullName] = info.Value;
            }
        }

        // Group method calls by target class
        Dictionary<string, List<MethodCallInfo>> callsByClass = new Dictionary<string, List<MethodCallInfo>>();
        foreach (MethodCallInfo? call in methodCalls)
        {
            if (call is not null)
            {
                if (!callsByClass.TryGetValue(call.Value.TargetClass, out List<MethodCallInfo>? classCalls))
                {
                    classCalls = new List<MethodCallInfo>();
                    callsByClass[call.Value.TargetClass] = classCalls;
                }
                classCalls.Add(call.Value);

                // Check for diagnostic warnings
                foreach (ArgumentTypeInfo argInfo in call.Value.Arguments)
                {
                    // Warn about potential recursive ZeroAlloc usage
                    if (argInfo.MayUseZeroAllocInternally)
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.PotentialRecursion,
                            call.Value.Location,
                            argInfo.FullTypeName));
                    }

                    // Warn about types that fall back to ToString() (causes allocation)
                    // Only warn if the type could actually implement ISpanFormattable
                    // Skip: ref structs (can't implement interfaces), arrays, ZeroAlloc internal types
                    if (argInfo.Category == TypeCategory.Object && argInfo.CanImplementISpanFormattable)
                    {
                        ctx.ReportDiagnostic(Diagnostic.Create(
                            Diagnostics.ToStringFallback,
                            call.Value.Location,
                            argInfo.FullTypeName));
                    }
                }
            }
        }

        // Generate code for each user class
        foreach (KeyValuePair<string, UserClassInfo> kvp in classMap)
        {
            UserClassInfo classInfo = kvp.Value;

            // Check if the class is partial (required for source generation)
            if (!classInfo.IsPartial)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ClassMustBePartial,
                    classInfo.Location,
                    classInfo.ClassName));
                continue; // Skip generation for non-partial classes
            }

            // Warn if the class is public (should be internal)
            if (classInfo.IsPublic)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.ClassShouldBeInternal,
                    classInfo.Location,
                    classInfo.ClassName));
            }

            // Get all method calls for this class
            if (!callsByClass.TryGetValue(classInfo.FullName, out List<MethodCallInfo>? calls))
            {
                calls = new List<MethodCallInfo>();
            }

            // Generate the partial class
            string sourceCode = _GenerateClassSource(classInfo, calls, options);
            string fileName = $"{classInfo.ClassName}.ZeroAlloc.g.cs";
            ctx.AddSource(fileName, sourceCode);
        }
    }

    /// <summary>
    /// Generates source code for a single user class.
    /// </summary>
    private static string _GenerateClassSource(
        UserClassInfo classInfo,
        List<MethodCallInfo> calls,
        GeneratorOptions options)
    {
        StringBuilder sb = new StringBuilder();
        string recursiveHeapFallbackLiteral = options.RecursiveHeapFallback ? "true" : "false";
        string recursiveHeapFallbackStatus = options.RecursiveHeapFallback ? "enabled" : "disabled";

        // File header
        BinaryGeneratorHelpers.AppendCode(sb,"""
            // <auto-generated>
            // This file was generated by the ZeroAlloc source generator.
            // Do not modify this file directly - changes will be overwritten.
            // </auto-generated>
            // Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

            #nullable enable

            using System;
            using System.Globalization;
            using System.Runtime.CompilerServices;
            using System.Text;
            using ZeroAlloc;

            """);

        // Namespace
        if (classInfo.Namespace is not null)
        {
            BinaryGeneratorHelpers.AppendCode(sb,$$"""
            namespace {{classInfo.Namespace}};

            """);
        }

        // Class documentation and configuration constants
        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            /// <summary>
            /// Generated zero-allocation formatting methods.
            /// </summary>
            /// <remarks>
            /// <para>
            /// All methods return <see cref="TempString"/> or <see cref="TempBytes"/> which MUST be
            /// disposed using a <c>using</c> statement.
            /// </para>
            /// <para>
            /// Default buffer size: {{options.DefaultBufferSize:N0}} bytes (configurable via ZeroAlloc_DefaultBufferSize MSBuild property).
            /// </para>
            /// <para>
            /// Recursive heap fallback: {{recursiveHeapFallbackStatus}} (configurable via ZeroAlloc_RecursiveHeapFallback MSBuild property).
            /// </para>
            /// <para>
            /// Buffer overflow behavior: {{options.BufferOverflowBehavior}} (configurable via ZeroAlloc_BufferOverflowBehavior MSBuild property).
            /// </para>
            /// </remarks>
            partial class {{classInfo.ClassName}}
            {
                // ========================================================================
                // CONFIGURATION CONSTANTS
                // ========================================================================
                // These constants are set at compile-time from MSBuild properties in your .csproj.
                // You can customize them by adding the following to your project file:
                //
                //   <PropertyGroup>
                //     <ZeroAlloc_DefaultBufferSize>4194304</ZeroAlloc_DefaultBufferSize>
                //     <ZeroAlloc_RecursiveHeapFallback>true</ZeroAlloc_RecursiveHeapFallback>
                //     <ZeroAlloc_BufferOverflowBehavior>Grow</ZeroAlloc_BufferOverflowBehavior>
                //   </PropertyGroup>
                // ========================================================================

                /// <summary>
                /// Initial buffer size for ThreadStatic buffers.
                /// Configured via ZeroAlloc_DefaultBufferSize MSBuild property.
                /// </summary>
                private const int DefaultBufferSize = {{options.DefaultBufferSize}};

                /// <summary>
                /// Whether to fall back to heap allocation when the ThreadStatic buffer is already in use (nested/recursive call).
                /// Configured via ZeroAlloc_RecursiveHeapFallback MSBuild property.
                /// </summary>
                /// <remarks>
                /// <para>
                /// IMPORTANT: Each thread has exactly ONE ThreadStatic buffer. If you call ZeroAlloc APIs
                /// while still holding a TempString/TempBytes (i.e., nested calls), the inner call cannot
                /// use the ThreadStatic buffer because the outer call is still using it.
                /// </para>
                /// <para>
                /// When true: Inner calls allocate a temporary heap buffer (small allocation).
                /// When false: Inner calls throw InvalidOperationException.
                /// </para>
                /// </remarks>
                private const bool RecursiveHeapFallback = {{recursiveHeapFallbackLiteral}};

                /// <summary>
                /// Behavior when content exceeds buffer size: 0=Grow, 1=HeapFallback, 2=Throw.
                /// Configured via ZeroAlloc_BufferOverflowBehavior MSBuild property.
                /// </summary>
                private const int BufferOverflowBehavior = {{(int)options.BufferOverflowBehavior}}; // {{options.BufferOverflowBehavior}}


            """);

        // Deduplicate method signatures
        HashSet<string> generatedSignatures = new HashSet<string>();

        // Collect all unique argument signatures for String/TryString, Utf8/TryUtf8, Bytes/TryBytes,
        // LocalizedString/TryLocalizedString, and LocalizedUtf8/TryLocalizedUtf8 pairs
        HashSet<ImmutableArray<ArgumentTypeInfo>> stringSignatures = new HashSet<ImmutableArray<ArgumentTypeInfo>>(new ArgumentListComparer());
        HashSet<ImmutableArray<ArgumentTypeInfo>> utf8Signatures = new HashSet<ImmutableArray<ArgumentTypeInfo>>(new ArgumentListComparer());
        HashSet<ImmutableArray<ArgumentTypeInfo>> bytesSignatures = new HashSet<ImmutableArray<ArgumentTypeInfo>>(new ArgumentListComparer());
        HashSet<ImmutableArray<ArgumentTypeInfo>> localizedStringSignatures = new HashSet<ImmutableArray<ArgumentTypeInfo>>(new ArgumentListComparer());
        HashSet<ImmutableArray<ArgumentTypeInfo>> localizedUtf8Signatures = new HashSet<ImmutableArray<ArgumentTypeInfo>>(new ArgumentListComparer());
        HashSet<ImmutableArray<ArgumentTypeInfo>> lazySignatures = new HashSet<ImmutableArray<ArgumentTypeInfo>>(new ArgumentListComparer());
        HashSet<ImmutableArray<ArgumentTypeInfo>> lazyInterpolatedSignatures = new HashSet<ImmutableArray<ArgumentTypeInfo>>(new ArgumentListComparer());

        // Collect all signatures
        foreach (MethodCallInfo call in calls)
        {
            switch (call.MethodName)
            {
                case "String":
                case "TryString":
                    stringSignatures.Add(call.Arguments);
                    break;
                case "Utf8":
                case "TryUtf8":
                    utf8Signatures.Add(call.Arguments);
                    break;
                case "Bytes":
                case "TryBytes":
                    bytesSignatures.Add(call.Arguments);
                    break;
                case "LocalizedString":
                case "TryLocalizedString":
                    localizedStringSignatures.Add(call.Arguments);
                    break;
                case "LocalizedUtf8":
                case "TryLocalizedUtf8":
                    localizedUtf8Signatures.Add(call.Arguments);
                    break;
                case "Lazy":
                    lazySignatures.Add(call.Arguments);
                    // Also generate corresponding String() method for the same signature
                    stringSignatures.Add(call.Arguments);
                    break;
                case "LazyInterpolated":
                    lazyInterpolatedSignatures.Add(call.Arguments);
                    break;
            }
        }

        // Generate String() and TryString() for all string signatures
        foreach (ImmutableArray<ArgumentTypeInfo> args in stringSignatures)
        {
            string signatureKey = $"String({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateStringMethod(sb, args);
            }

            signatureKey = $"TryString({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateTryStringMethod(sb, args);
            }
        }

        // Generate Utf8() and TryUtf8() for all UTF-8 signatures
        foreach (ImmutableArray<ArgumentTypeInfo> args in utf8Signatures)
        {
            string signatureKey = $"Utf8({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateUtf8Method(sb, args);
            }

            signatureKey = $"TryUtf8({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateTryUtf8Method(sb, args);
            }
        }

        // Generate Bytes() and TryBytes() for all bytes signatures
        foreach (ImmutableArray<ArgumentTypeInfo> args in bytesSignatures)
        {
            string signatureKey = $"Bytes({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateBytesMethod(sb, args);
            }

            signatureKey = $"TryBytes({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateTryBytesMethod(sb, args);
            }
        }

        // Generate LocalizedString() and TryLocalizedString() for all localized string signatures
        foreach (ImmutableArray<ArgumentTypeInfo> args in localizedStringSignatures)
        {
            string signatureKey = $"LocalizedString({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateLocalizedStringMethod(sb, args);
            }

            signatureKey = $"TryLocalizedString({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateTryLocalizedStringMethod(sb, args);
            }
        }

        // Generate LocalizedUtf8() and TryLocalizedUtf8() for all localized UTF-8 signatures
        foreach (ImmutableArray<ArgumentTypeInfo> args in localizedUtf8Signatures)
        {
            string signatureKey = $"LocalizedUtf8({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateLocalizedUtf8Method(sb, args);
            }

            signatureKey = $"TryLocalizedUtf8({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateTryLocalizedUtf8Method(sb, args);
            }
        }

        // Generate Lazy() for all lazy signatures
        foreach (ImmutableArray<ArgumentTypeInfo> args in lazySignatures)
        {
            string signatureKey = $"Lazy({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateLazyMethod(sb, args);
            }
        }

        // Generate LazyInterpolated() for all lazy interpolated signatures
        foreach (ImmutableArray<ArgumentTypeInfo> args in lazyInterpolatedSignatures)
        {
            string signatureKey = $"LazyInterpolated({string.Join(",", args.Select(a => a.FullTypeName))})";
            if (!generatedSignatures.Contains(signatureKey))
            {
                generatedSignatures.Add(signatureKey);
                _GenerateLazyInterpolatedMethod(sb, args);
            }
        }

        BinaryGeneratorHelpers.AppendCode(sb,"}\n");

        return sb.ToString();
    }

    // ========================================================================
    // METHOD GENERATION - String
    // ========================================================================
    // These methods generate the String() and TryString() methods for char-based
    // string formatting. The generated code:
    //
    // 1. Acquires a ThreadStatic char buffer (or falls back to heap if nested)
    // 2. Formats each argument directly into the buffer using ISpanFormattable
    // 3. Handles buffer overflow by growing (default) or throwing
    // 4. Returns a TempString that wraps the buffer
    //
    // Key optimizations:
    // - Pre-checks string length before copying
    // - Uses ISpanFormattable.TryFormat for structured types
    // - Loop with retry for types that may need buffer growth
    // ========================================================================

    /// <summary>
    /// Generates a <c>String()</c> method that returns a <see cref="TempString"/>.
    /// </summary>
    /// <remarks>
    /// The generated method:
    /// <list type="bullet">
    ///   <item>Acquires a ThreadStatic char buffer</item>
    ///   <item>Formats all arguments directly into the buffer</item>
    ///   <item>Handles buffer overflow according to configuration</item>
    ///   <item>Returns TempString which the caller must dispose</item>
    /// </list>
    /// </remarks>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="args">The argument types for this overload.</param>
    private static void _GenerateStringMethod(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        // Check if first argument is CultureInfo
        bool hasCultureInfo = args.Length > 0 && args[0].Category == TypeCategory.CultureInfo;
        int startIndex = hasCultureInfo ? 1 : 0;
        string cultureVar = hasCultureInfo ? "culture0" : "CultureInfo.InvariantCulture";

        // Build parameter list
        List<string> parameters = new List<string>();
        List<string> paramNames = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string usageExample = hasCultureInfo
            ? "    /// using TempString temp = ZA.String(CultureInfo.GetCultureInfo(\"de-DE\"), ...);"
            : "    /// using TempString temp = ZA.String(...);";
        string parameterList = string.Join(", ", parameters);

        // Method documentation
        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            // ========================================================================
            // String() - Zero-allocation string formatting
            // ========================================================================

            /// <summary>
            /// Formats the given values into a temporary string without heap allocation.
            /// </summary>
            {{paramDocs}}
            /// <returns>A <see cref="TempString"/> that MUST be disposed with a using statement.</returns>
            /// <remarks>
            /// <para><b>Usage:</b></para>
            /// <code>
            {{usageExample}}
            /// ReadOnlySpan&lt;char&gt; span = temp.AsSpan();
            /// // Use span for comparison, parsing, logging, etc.
            /// </code>
            /// <para>
            /// If the ThreadStatic buffer is already in use (nested call), behavior depends on
            /// the <c>ZeroAlloc_RecursiveHeapFallback</c> MSBuild property. Check <see cref="TempString.IsHeapAllocated"/> if needed.
            /// </para>
            /// </remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static TempString String({{parameterList}})
            {
                // ================================================================
                // STEP 1: Acquire Buffer
                // ================================================================
                // ZeroAlloc uses a ThreadStatic buffer to avoid heap allocations.
                // Each thread has exactly ONE buffer. If this call is nested inside
                // another ZeroAlloc call (e.g., you're still holding a TempString),
                // behavior depends on RecursiveHeapFallback:
                //   - true:  Allocate a temporary heap buffer (small allocation)
                //   - false: Throw InvalidOperationException
                //
                // The 'isThreadStatic' flag tracks which buffer we got:
                //   - true:  We own the ThreadStatic buffer, must release on Dispose
                //   - false: We got a heap buffer, no release needed
                // ================================================================
                char[] buffer = ZeroAllocHelper.AcquireCharBuffer(DefaultBufferSize, RecursiveHeapFallback, out bool isThreadStatic);
                Span<char> span = buffer.AsSpan();
                int position = 0;

                // ================================================================
                // STEP 2: Format Each Argument Into Buffer
                // ================================================================
                // Each argument is formatted directly into the buffer without
                // intermediate string allocations. The formatting strategy varies
                // by type (see inline comments for each argument below).
                // ================================================================

            """);

        // Generate formatting code for each argument (skip CultureInfo at index 0 if present)
        for (int i = startIndex; i < args.Length; i++)
        {
            sb.Append('\n');
            _GenerateCharFormatCode(sb, args[i], paramNames[i], i, cultureVar);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

                // ================================================================
                // STEP 3: Return TempString Wrapper
                // ================================================================
                // The TempString wraps the buffer and tracks ownership.
                // IMPORTANT: The caller MUST dispose this TempString!
                //
                // Option A: Implicit conversion (allocates string, auto-disposes):
                //   string result = ZA.String("Hello", name);
                //
                // Option B: Explicit span (zero-allocation, manual dispose):
                //   using var temp = ZA.String("Hello", name);
                //   ReadOnlySpan<char> span = temp.AsSpan();
                // ================================================================
                return new TempString(buffer, position, isThreadStatic);
            }

            """);
    }

    /// <summary>
    /// Generates formatting code for a single argument into a char buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method generates the actual formatting logic for each argument type.
    /// The strategy varies by <see cref="TypeCategory"/>:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>String:</b> Length check + <c>AsSpan().CopyTo()</c></item>
    ///   <item><b>Char:</b> Single capacity check + direct assignment</item>
    ///   <item><b>Boolean:</b> Length check (4 or 5) + <c>TryFormat()</c></item>
    ///   <item><b>SpanFormattable:</b> Loop with <c>TryFormat()</c> + buffer grow on failure</item>
    ///   <item><b>Object:</b> Fallback to <c>ToString()</c> (allocates!)</item>
    /// </list>
    /// <para>
    /// Each case includes buffer overflow handling that either grows the buffer
    /// or throws, depending on the configured <see cref="BufferOverflowBehavior"/>.
    /// </para>
    /// </remarks>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="arg">Type information for the argument.</param>
    /// <param name="paramName">The parameter name in generated code.</param>
    /// <param name="index">The argument index for generating unique variable names.</param>
    /// <param name="cultureVar">The variable name holding the CultureInfo to use.</param>
    private static void _GenerateCharFormatCode(
        StringBuilder sb,
        ArgumentTypeInfo arg,
        string paramName,
        int index,
        string cultureVar = "CultureInfo.InvariantCulture")
    {
        string formattingStrategy = CommentHelper.GetFormattingStrategy(arg.Category);
        string formattingExplanation = CommentHelper.GetFormattingExplanation(arg.Category);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
                // ----------------------------------------------------------------
                // Argument {{index}}: {{arg.FullTypeName}}
                // Strategy: {{formattingStrategy}}
                // {{formattingExplanation}}
                // ----------------------------------------------------------------

            """);

        switch (arg.Category)
        {
            case TypeCategory.String:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // String formatting: Copy chars directly via AsSpan().CopyTo()
                        // This is zero-allocation as we copy directly into our buffer.
                        if ({{paramName}} is not null)
                        {
                            int len{{index}} = {{paramName}}.Length;
                            // Check if buffer has enough space for this string
                            if (position + len{{index}} > span.Length)
                            {
                                // Buffer overflow: Grow buffer (release ThreadStatic first if we own it)
                                if (isThreadStatic) ZeroAllocHelper.ReleaseCharBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + len{{index}});
                                char[] newBuffer = new char[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer); // Preserve existing content
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false; // Now using heap buffer
                            }
                            {{paramName}}.AsSpan().CopyTo(span.Slice(position));
                            position += len{{index}};
                        }
                """);
                break;

            case TypeCategory.Char:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // Char formatting: Direct assignment to buffer (1 character)
                        if (position >= span.Length)
                        {
                            // Buffer overflow: Grow buffer
                            if (isThreadStatic) ZeroAllocHelper.ReleaseCharBuffer();
                            char[] newBuffer = new char[buffer.Length * 2];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        span[position++] = {{paramName}};
                """);
                break;

            case TypeCategory.Boolean:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // Boolean formatting: "True" (4 chars) or "False" (5 chars)
                        int boolLen{{index}} = {{paramName}} ? 4 : 5;
                        if (position + boolLen{{index}} > span.Length)
                        {
                            // Buffer overflow: Grow buffer
                            if (isThreadStatic) ZeroAllocHelper.ReleaseCharBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + boolLen{{index}});
                            char[] newBuffer = new char[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        ({{paramName}} ? "True" : "False").AsSpan().CopyTo(span.Slice(position));
                        position += boolLen{{index}};
                """);
                break;

            case TypeCategory.SpanFormattable:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // ISpanFormattable: Use TryFormat for zero-allocation formatting
                        // If TryFormat fails (buffer too small), we fall back to ToString()
                        if (!((ISpanFormattable){{paramName}}).TryFormat(span.Slice(position), out int written{{index}}, default, {{cultureVar}}))
                        {
                            // TryFormat failed (buffer too small) - fall back to ToString()
                            string fallback{{index}} = {{paramName}}.ToString() ?? "";
                            int fbLen{{index}} = fallback{{index}}.Length;
                            if (position + fbLen{{index}} > span.Length)
                            {
                                // Buffer overflow: Grow buffer
                                if (isThreadStatic) ZeroAllocHelper.ReleaseCharBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + fbLen{{index}});
                                char[] newBuffer = new char[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            fallback{{index}}.AsSpan().CopyTo(span.Slice(position));
                            position += fbLen{{index}};
                        }
                        else
                        {
                            // TryFormat succeeded - advance position by bytes written
                            position += written{{index}};
                        }
                """);
                break;

            case TypeCategory.CultureInfo:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // CultureInfo: Not written to output, used as format provider
                        // {{paramName}} is used for formatting other arguments in this call
                """);
                break;

            case TypeCategory.Object:
            default:
                string toStringAssignment = arg.CannotUseNullConditional
                    ? $"string str{index} = {paramName}.ToString() ?? \"\";"
                    : $"string str{index} = {paramName}?.ToString() ?? \"\";";
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // WARNING: Object fallback - ToString() ALLOCATES a temporary string!
                        // Consider implementing ISpanFormattable on this type for zero-allocation.
                        {{toStringAssignment}}
                        int strLen{{index}} = str{{index}}.Length;
                        if (position + strLen{{index}} > span.Length)
                        {
                            // Buffer overflow: Grow buffer
                            if (isThreadStatic) ZeroAllocHelper.ReleaseCharBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + strLen{{index}});
                            char[] newBuffer = new char[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        str{{index}}.AsSpan().CopyTo(span.Slice(position));
                        position += strLen{{index}};
                """);
                break;
        }
    }

    // ========================================================================
    // METHOD GENERATION - TryString
    // ========================================================================
    // TryString is the "safe" variant of String that returns false instead of
    // throwing when the buffer is unavailable and RecursiveHeapFallback is disabled.
    // The generated formatting logic is identical to String().
    // ========================================================================

    /// <summary>
    /// Generates a <c>TryString()</c> method that returns <c>false</c> on buffer unavailability.
    /// </summary>
    /// <remarks>
    /// Unlike <c>String()</c>, this method:
    /// <list type="bullet">
    ///   <item>Returns <c>false</c> if the buffer is busy and heap fallback is disabled</item>
    ///   <item>Uses an <c>out</c> parameter for the result</item>
    ///   <item>Never throws due to buffer issues</item>
    /// </list>
    /// </remarks>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="args">The argument types for this overload.</param>
    private static void _GenerateTryStringMethod(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        // Check if first argument is CultureInfo
        bool hasCultureInfo = args.Length > 0 && args[0].Category == TypeCategory.CultureInfo;
        int startIndex = hasCultureInfo ? 1 : 0;
        string cultureVar = hasCultureInfo ? "culture0" : "CultureInfo.InvariantCulture";

        // Build parameter list
        List<string> parameters = new List<string> { "out TempString result" };
        List<string> paramNames = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            // ========================================================================
            // TryString() - Safe zero-allocation string formatting
            // ========================================================================

            /// <summary>
            /// Tries to format the given values into a temporary string.
            /// </summary>
            /// <param name="result">The resulting TempString if successful.</param>
            {{paramDocs}}
            /// <returns><c>true</c> on success; <c>false</c> if RecursiveHeapFallback is disabled and buffer is busy.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool TryString({{parameterList}})
            {
                // ----------------------------------------------------------------
                // Try to acquire buffer (respects RecursiveHeapFallback setting)
                // ----------------------------------------------------------------
                if (!RecursiveHeapFallback && !ZeroAllocHelper.IsCharBufferAvailable())
                {
                    result = default;
                    return false;
                }

                char[] buffer = ZeroAllocHelper.AcquireCharBuffer(DefaultBufferSize, RecursiveHeapFallback, out bool isThreadStatic);
                Span<char> span = buffer.AsSpan();
                int position = 0;

                // ----------------------------------------------------------------
                // Format each value into the buffer
                // ----------------------------------------------------------------

            """);

        for (int i = startIndex; i < args.Length; i++)
        {
            sb.Append('\n');
            _GenerateTryCharFormatCode(sb, args[i], paramNames[i], i, cultureVar);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

                result = new TempString(buffer, position, isThreadStatic);
                return true;
            }

            """);
    }

    /// <summary>
    /// Generates try-format code for a char buffer with safe error handling.
    /// </summary>
    /// <remarks>
    /// This is the TryString equivalent of <see cref="GenerateCharFormatCode"/>.
    /// The logic is nearly identical, but buffer overflow always results in
    /// buffer growth rather than throwing (since TryString is the "safe" API).
    /// </remarks>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="arg">Type information for the argument.</param>
    /// <param name="paramName">The parameter name in generated code.</param>
    /// <param name="index">The argument index for generating unique variable names.</param>
    /// <param name="cultureVar">The variable name holding the CultureInfo to use.</param>
    private static void _GenerateTryCharFormatCode(
        StringBuilder sb,
        ArgumentTypeInfo arg,
        string paramName,
        int index,
        string cultureVar = "CultureInfo.InvariantCulture")
    {
        string formattingStrategy = CommentHelper.GetFormattingStrategy(arg.Category);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
                // ----------------------------------------------------------------
                // Argument {{index}}: {{arg.FullTypeName}}
                // Strategy: {{formattingStrategy}}
                // ----------------------------------------------------------------

            """);

        switch (arg.Category)
        {
            case TypeCategory.String:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if ({{paramName}} is not null)
                        {
                            int len{{index}} = {{paramName}}.Length;
                            if (position + len{{index}} > span.Length)
                            {
                                // Buffer too small - grow like List<T>
                                if (isThreadStatic) ZeroAllocHelper.ReleaseCharBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + len{{index}});
                                char[] newBuffer = new char[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            {{paramName}}.AsSpan().CopyTo(span.Slice(position));
                            position += len{{index}};
                        }
                """);
                break;

            case TypeCategory.Char:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if (position >= span.Length)
                        {
                            // Buffer too small - grow like List<T>
                            if (isThreadStatic) ZeroAllocHelper.ReleaseCharBuffer();
                            char[] newBuffer = new char[buffer.Length * 2];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        span[position++] = {{paramName}};
                """);
                break;

            case TypeCategory.Boolean:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        int boolLen{{index}} = {{paramName}} ? 4 : 5;
                        if (position + boolLen{{index}} > span.Length)
                        {
                            // Buffer too small - grow like List<T>
                            if (isThreadStatic) ZeroAllocHelper.ReleaseCharBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + boolLen{{index}});
                            char[] newBuffer = new char[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        ({{paramName}} ? "True" : "False").AsSpan().CopyTo(span.Slice(position));
                        position += boolLen{{index}};
                """);
                break;

            case TypeCategory.SpanFormattable:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if (!((ISpanFormattable){{paramName}}).TryFormat(span.Slice(position), out int written{{index}}, default, {{cultureVar}}))
                        {
                            string fallback{{index}} = {{paramName}}.ToString() ?? "";
                            int fbLen{{index}} = fallback{{index}}.Length;
                            if (position + fbLen{{index}} > span.Length)
                            {
                                // Buffer too small - grow like List<T>
                                if (isThreadStatic) ZeroAllocHelper.ReleaseCharBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + fbLen{{index}});
                                char[] newBuffer = new char[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            fallback{{index}}.AsSpan().CopyTo(span.Slice(position));
                            position += fbLen{{index}};
                        }
                        else
                        {
                            position += written{{index}};
                        }
                """);
                break;

            case TypeCategory.CultureInfo:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // CultureInfo {{paramName}} is used for formatting other arguments
                """);
                break;

            case TypeCategory.Object:
            default:
                string toStringAssignment = arg.CannotUseNullConditional
                    ? $"string str{index} = {paramName}.ToString() ?? \"\";"
                    : $"string str{index} = {paramName}?.ToString() ?? \"\";";
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        {{toStringAssignment}}
                        int strLen{{index}} = str{{index}}.Length;
                        if (position + strLen{{index}} > span.Length)
                        {
                            // Buffer too small - grow like List<T>
                            if (isThreadStatic) ZeroAllocHelper.ReleaseCharBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + strLen{{index}});
                            char[] newBuffer = new char[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        str{{index}}.AsSpan().CopyTo(span.Slice(position));
                        position += strLen{{index}};
                """);
                break;
        }
    }

    // ========================================================================
    // METHOD GENERATION - Utf8
    // ========================================================================
    // These methods generate Utf8() and TryUtf8() for UTF-8 byte formatting.
    // Similar structure to String/TryString, but:
    //
    // - Uses byte[] buffer instead of char[]
    // - Uses IUtf8SpanFormattable.TryFormat when available
    // - Falls back to Encoding.UTF8.GetBytes() for strings and char
    // - Returns TempBytes instead of TempString
    //
    // UTF-8 formatting is more efficient for network protocols and file I/O
    // since no additional encoding step is needed.
    // ========================================================================

    /// <summary>
    /// Generates a <c>Utf8()</c> method that returns a <see cref="TempBytes"/>.
    /// </summary>
    /// <remarks>
    /// The generated method formats all arguments as UTF-8 bytes directly,
    /// avoiding the intermediate string representation entirely when types
    /// implement <see cref="IUtf8SpanFormattable"/>.
    /// </remarks>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="args">The argument types for this overload.</param>
    private static void _GenerateUtf8Method(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        List<string> parameters = new List<string>();
        List<string> paramNames = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            // ========================================================================
            // Utf8() - Zero-allocation UTF-8 formatting
            // ========================================================================

            /// <summary>
            /// Formats the given values into UTF-8 bytes without heap allocation.
            /// </summary>
            {{paramDocs}}
            /// <returns>A <see cref="TempBytes"/> that MUST be disposed with a using statement.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static TempBytes Utf8({{parameterList}})
            {
                // ----------------------------------------------------------------
                // Acquire buffer (ThreadStatic if available, fallback depends on RecursiveHeapFallback setting)
                // ----------------------------------------------------------------
                byte[] buffer = ZeroAllocHelper.AcquireByteBuffer(DefaultBufferSize, RecursiveHeapFallback, out bool isThreadStatic);
                Span<byte> span = buffer.AsSpan();
                int position = 0;

                // ----------------------------------------------------------------
                // Format each value as UTF-8 directly into the buffer
                // ----------------------------------------------------------------

            """);

        for (int i = 0; i < args.Length; i++)
        {
            sb.Append('\n');
            _GenerateUtf8FormatCode(sb, args[i], paramNames[i], i);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

                return new TempBytes(buffer, position, isThreadStatic);
            }

            """);
    }

    /// <summary>
    /// Generates UTF-8 formatting code for a single argument into a byte buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method generates UTF-8-specific formatting logic:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>String:</b> <c>Encoding.UTF8.GetByteCount()</c> + <c>GetBytes()</c></item>
    ///   <item><b>Char:</b> Single char to UTF-8 (max 4 bytes)</item>
    ///   <item><b>Boolean:</b> <c>"True"u8</c> or <c>"False"u8</c> literals</item>
    ///   <item><b>Utf8SpanFormattable:</b> Direct <c>IUtf8SpanFormattable.TryFormat()</c></item>
    ///   <item><b>SpanFormattable:</b> Format to char buffer, then encode to UTF-8</item>
    ///   <item><b>Object:</b> <c>ToString()</c> + UTF-8 encoding</item>
    /// </list>
    /// </remarks>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="arg">Type information for the argument.</param>
    /// <param name="paramName">The parameter name in generated code.</param>
    /// <param name="index">The argument index for generating unique variable names.</param>
    private static void _GenerateUtf8FormatCode(
        StringBuilder sb,
        ArgumentTypeInfo arg,
        string paramName,
        int index)
    {
        string formattingStrategy = CommentHelper.GetFormattingStrategy(arg.Category);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
                // ----------------------------------------------------------------
                // Argument {{index}}: {{arg.FullTypeName}}
                // Strategy: {{formattingStrategy}}
                // ----------------------------------------------------------------

            """);

        switch (arg.Category)
        {
            case TypeCategory.String:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if ({{paramName}} is not null)
                        {
                            int byteCount{{index}} = Encoding.UTF8.GetByteCount({{paramName}});
                            if (position + byteCount{{index}} > span.Length)
                            {
                                // Buffer too small - grow like List<T>
                                if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + byteCount{{index}});
                                byte[] newBuffer = new byte[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            position += Encoding.UTF8.GetBytes({{paramName}}, span.Slice(position));
                        }
                """);
                break;

            case TypeCategory.Char:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if (position + 4 > span.Length)
                        {
                            // Buffer too small - grow like List<T>
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            byte[] newBuffer = new byte[buffer.Length * 2];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        Span<char> charSpan{{index}} = stackalloc char[1] { {{paramName}} };
                        position += Encoding.UTF8.GetBytes(charSpan{{index}}, span.Slice(position));
                """);
                break;

            case TypeCategory.Boolean:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        int boolLen{{index}} = {{paramName}} ? 4 : 5;
                        if (position + boolLen{{index}} > span.Length)
                        {
                            // Buffer too small - grow like List<T>
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + boolLen{{index}});
                            byte[] newBuffer = new byte[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        ({{paramName}} ? "True"u8 : "False"u8).CopyTo(span.Slice(position));
                        position += boolLen{{index}};
                """);
                break;

            case TypeCategory.Utf8SpanFormattable:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if (!{{paramName}}.TryFormat(span.Slice(position), out int written{{index}}, default, CultureInfo.InvariantCulture))
                        {
                            string utf8Fallback{{index}} = {{paramName}}.ToString() ?? "";
                            int neededBytes{{index}} = Encoding.UTF8.GetByteCount(utf8Fallback{{index}});
                            if (position + neededBytes{{index}} > span.Length)
                            {
                                // Buffer too small - grow like List<T>
                                if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + neededBytes{{index}});
                                byte[] newBuffer = new byte[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            position += Encoding.UTF8.GetBytes(utf8Fallback{{index}}, span.Slice(position));
                        }
                        else
                        {
                            position += written{{index}};
                        }
                """);
                break;

            case TypeCategory.SpanFormattable:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        Span<char> charBuf{{index}} = stackalloc char[64];
                        if (((ISpanFormattable){{paramName}}).TryFormat(charBuf{{index}}, out int charWritten{{index}}, default, CultureInfo.InvariantCulture))
                        {
                            int byteCount{{index}} = Encoding.UTF8.GetByteCount(charBuf{{index}}.Slice(0, charWritten{{index}}));
                            if (position + byteCount{{index}} > span.Length)
                            {
                                // Buffer too small - grow like List<T>
                                if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + byteCount{{index}});
                                byte[] newBuffer = new byte[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            position += Encoding.UTF8.GetBytes(charBuf{{index}}.Slice(0, charWritten{{index}}), span.Slice(position));
                        }
                        else
                        {
                            string spanFallback{{index}} = {{paramName}}.ToString() ?? "";
                            int fbByteCount{{index}} = Encoding.UTF8.GetByteCount(spanFallback{{index}});
                            if (position + fbByteCount{{index}} > span.Length)
                            {
                                // Buffer too small - grow like List<T>
                                if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + fbByteCount{{index}});
                                byte[] newBuffer = new byte[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            position += Encoding.UTF8.GetBytes(spanFallback{{index}}, span.Slice(position));
                        }
                """);
                break;

            case TypeCategory.BinarySerializable:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // IBinarySerializable: {{arg.FullTypeName}}
                        if (!((IBinarySerializable){{paramName}}).TryWrite(span.Slice(position), out int written{{index}}))
                        {
                            throw new InvalidOperationException("Buffer too small for IBinarySerializable value.");
                        }
                        position += written{{index}};
                """);
                break;

            case TypeCategory.RawBytes:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // Raw bytes: {{arg.FullTypeName}}
                        if (!{{paramName}}.TryFormat(span.Slice(position), out int rawWritten{{index}}, default, null))
                        {
                            // Buffer too small - grow
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + {{paramName}}.Size);
                            byte[] newBuffer = new byte[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                            if (!{{paramName}}.TryFormat(span.Slice(position), out rawWritten{{index}}, default, null))
                            {
                                throw new InvalidOperationException("Buffer too small for Raw value after resize.");
                            }
                        }
                        position += rawWritten{{index}};
                """);
                break;

            case TypeCategory.Object:
            default:
                string toStringAssignment = arg.CannotUseNullConditional
                    ? $"string str{index} = {paramName}.ToString() ?? \"\";"
                    : $"string str{index} = {paramName}?.ToString() ?? \"\";";
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        {{toStringAssignment}}
                        int strByteCount{{index}} = Encoding.UTF8.GetByteCount(str{{index}});
                        if (position + strByteCount{{index}} > span.Length)
                        {
                            // Buffer too small - grow like List<T>
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + strByteCount{{index}});
                            byte[] newBuffer = new byte[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        position += Encoding.UTF8.GetBytes(str{{index}}, span.Slice(position));
                """);
                break;
        }
    }

    /// <summary>
    /// Generates UTF-8 formatting code with culture-sensitive formatting.
    /// Similar to GenerateUtf8FormatCode but uses the specified format provider.
    /// </summary>
    private static void _GenerateUtf8FormatCodeWithCulture(
        StringBuilder sb,
        ArgumentTypeInfo arg,
        string paramName,
        int index,
        string cultureVar)
    {
        string formattingStrategy = CommentHelper.GetFormattingStrategy(arg.Category);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
                // ----------------------------------------------------------------
                // Argument {{index}}: {{arg.FullTypeName}} (culture-sensitive)
                // Strategy: {{formattingStrategy}}
                // ----------------------------------------------------------------

            """);

        switch (arg.Category)
        {
            case TypeCategory.String:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if ({{paramName}} is not null)
                        {
                            int byteCount{{index}} = Encoding.UTF8.GetByteCount({{paramName}});
                            if (position + byteCount{{index}} > span.Length)
                            {
                                if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + byteCount{{index}});
                                byte[] newBuffer = new byte[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            position += Encoding.UTF8.GetBytes({{paramName}}, span.Slice(position));
                        }
                """);
                break;

            case TypeCategory.Char:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if (position + 4 > span.Length)
                        {
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            byte[] newBuffer = new byte[buffer.Length * 2];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        Span<char> charSpan{{index}} = stackalloc char[1] { {{paramName}} };
                        position += Encoding.UTF8.GetBytes(charSpan{{index}}, span.Slice(position));
                """);
                break;

            case TypeCategory.Boolean:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        int boolLen{{index}} = {{paramName}} ? 4 : 5;
                        if (position + boolLen{{index}} > span.Length)
                        {
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + boolLen{{index}});
                            byte[] newBuffer = new byte[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        ({{paramName}} ? "True"u8 : "False"u8).CopyTo(span.Slice(position));
                        position += boolLen{{index}};
                """);
                break;

            case TypeCategory.Utf8SpanFormattable:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if (!{{paramName}}.TryFormat(span.Slice(position), out int written{{index}}, default, {{cultureVar}}))
                        {
                            string utf8Fallback{{index}} = {{paramName}}.ToString() ?? "";
                            int neededBytes{{index}} = Encoding.UTF8.GetByteCount(utf8Fallback{{index}});
                            if (position + neededBytes{{index}} > span.Length)
                            {
                                if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + neededBytes{{index}});
                                byte[] newBuffer = new byte[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            position += Encoding.UTF8.GetBytes(utf8Fallback{{index}}, span.Slice(position));
                        }
                        else
                        {
                            position += written{{index}};
                        }
                """);
                break;

            case TypeCategory.SpanFormattable:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        Span<char> charBuf{{index}} = stackalloc char[64];
                        if (((ISpanFormattable){{paramName}}).TryFormat(charBuf{{index}}, out int charWritten{{index}}, default, {{cultureVar}}))
                        {
                            int byteCount{{index}} = Encoding.UTF8.GetByteCount(charBuf{{index}}.Slice(0, charWritten{{index}}));
                            if (position + byteCount{{index}} > span.Length)
                            {
                                if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + byteCount{{index}});
                                byte[] newBuffer = new byte[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            position += Encoding.UTF8.GetBytes(charBuf{{index}}.Slice(0, charWritten{{index}}), span.Slice(position));
                        }
                        else
                        {
                            string spanFallback{{index}} = {{paramName}}.ToString() ?? "";
                            int fbByteCount{{index}} = Encoding.UTF8.GetByteCount(spanFallback{{index}});
                            if (position + fbByteCount{{index}} > span.Length)
                            {
                                if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + fbByteCount{{index}});
                                byte[] newBuffer = new byte[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            position += Encoding.UTF8.GetBytes(spanFallback{{index}}, span.Slice(position));
                        }
                """);
                break;

            case TypeCategory.Object:
            default:
                string toStringAssignment = arg.CannotUseNullConditional
                    ? $"string str{index} = {paramName}.ToString() ?? \"\";"
                    : $"string str{index} = {paramName}?.ToString() ?? \"\";";
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        {{toStringAssignment}}
                        int strByteCount{{index}} = Encoding.UTF8.GetByteCount(str{{index}});
                        if (position + strByteCount{{index}} > span.Length)
                        {
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + strByteCount{{index}});
                            byte[] newBuffer = new byte[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        position += Encoding.UTF8.GetBytes(str{{index}}, span.Slice(position));
                """);
                break;
        }
    }

    // ========================================================================
    // METHOD GENERATION - TryUtf8
    // ========================================================================
    // TryUtf8 is the "safe" variant of Utf8, returning false when the buffer
    // is unavailable. See TryString documentation for the pattern.
    // ========================================================================

    /// <summary>
    /// Generates a <c>TryUtf8()</c> method that returns <c>false</c> on buffer unavailability.
    /// </summary>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="args">The argument types for this overload.</param>
    private static void _GenerateTryUtf8Method(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        List<string> parameters = new List<string> { "out TempBytes result" };
        List<string> paramNames = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            // ========================================================================
            // TryUtf8() - Safe zero-allocation UTF-8 formatting
            // ========================================================================

            /// <summary>
            /// Tries to format the given values into UTF-8 bytes.
            /// </summary>
            /// <param name="result">The resulting TempBytes if successful.</param>
            {{paramDocs}}
            /// <returns><c>true</c> on success; <c>false</c> if RecursiveHeapFallback is disabled and buffer is busy.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool TryUtf8({{parameterList}})
            {
                // ----------------------------------------------------------------
                // Try to acquire buffer (respects RecursiveHeapFallback setting)
                // ----------------------------------------------------------------
                if (!RecursiveHeapFallback && !ZeroAllocHelper.IsByteBufferAvailable())
                {
                    result = default;
                    return false;
                }

                byte[] buffer = ZeroAllocHelper.AcquireByteBuffer(DefaultBufferSize, RecursiveHeapFallback, out bool isThreadStatic);
                Span<byte> span = buffer.AsSpan();
                int position = 0;

                // ----------------------------------------------------------------
                // Format each value as UTF-8 directly into the buffer
                // ----------------------------------------------------------------

            """);

        for (int i = 0; i < args.Length; i++)
        {
            sb.Append('\n');
            _GenerateTryUtf8FormatCode(sb, args[i], paramNames[i], i);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

                result = new TempBytes(buffer, position, isThreadStatic);
                return true;
            }

            """);
    }

    /// <summary>
    /// Generates try-format code for UTF-8 output with safe error handling.
    /// </summary>
    /// <remarks>
    /// Similar to <see cref="GenerateUtf8FormatCode"/> but used in TryUtf8
    /// where buffer growth is always attempted instead of throwing.
    /// </remarks>
    private static void _GenerateTryUtf8FormatCode(
        StringBuilder sb,
        ArgumentTypeInfo arg,
        string paramName,
        int index)
    {
        string formattingStrategy = CommentHelper.GetFormattingStrategy(arg.Category);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
                // ----------------------------------------------------------------
                // Argument {{index}}: {{arg.FullTypeName}}
                // Strategy: {{formattingStrategy}}
                // ----------------------------------------------------------------

            """);

        switch (arg.Category)
        {
            case TypeCategory.String:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if ({{paramName}} is not null)
                        {
                            int byteCount{{index}} = Encoding.UTF8.GetByteCount({{paramName}});
                            if (position + byteCount{{index}} > span.Length)
                            {
                                // Buffer too small - grow like List<T>
                                if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                                int newSize{{index}} = Math.Max(buffer.Length * 2, position + byteCount{{index}});
                                byte[] newBuffer = new byte[newSize{{index}}];
                                span.Slice(0, position).CopyTo(newBuffer);
                                buffer = newBuffer;
                                span = buffer.AsSpan();
                                isThreadStatic = false;
                            }
                            position += Encoding.UTF8.GetBytes({{paramName}}, span.Slice(position));
                        }
                """);
                break;

            case TypeCategory.Char:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        if (position + 4 > span.Length)
                        {
                            // Buffer too small - grow like List<T>
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            byte[] newBuffer = new byte[buffer.Length * 2];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        Span<char> cs{{index}} = stackalloc char[1] { {{paramName}} };
                        position += Encoding.UTF8.GetBytes(cs{{index}}, span.Slice(position));
                """);
                break;

            case TypeCategory.Boolean:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        int boolLen{{index}} = {{paramName}} ? 4 : 5;
                        if (position + boolLen{{index}} > span.Length)
                        {
                            // Buffer too small - grow like List<T>
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + boolLen{{index}});
                            byte[] newBuffer = new byte[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        ({{paramName}} ? "True"u8 : "False"u8).CopyTo(span.Slice(position));
                        position += boolLen{{index}};
                """);
                break;

            case TypeCategory.BinarySerializable:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // IBinarySerializable: {{arg.FullTypeName}}
                        if (!((IBinarySerializable){{paramName}}).TryWrite(span.Slice(position), out int bw{{index}}))
                        {
                            throw new InvalidOperationException("Buffer too small for IBinarySerializable value.");
                        }
                        position += bw{{index}};
                """);
                break;

            case TypeCategory.RawBytes:
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        // Raw bytes: {{arg.FullTypeName}}
                        if (!{{paramName}}.TryFormat(span.Slice(position), out int rw{{index}}, default, null))
                        {
                            // Buffer too small - grow
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + {{paramName}}.Size);
                            byte[] newBuffer = new byte[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                            if (!{{paramName}}.TryFormat(span.Slice(position), out rw{{index}}, default, null))
                            {
                                throw new InvalidOperationException("Buffer too small for Raw value after resize.");
                            }
                        }
                        position += rw{{index}};
                """);
                break;

            default:
                string toStringAssignment = arg.CannotUseNullConditional
                    ? $"string s{index} = {paramName}.ToString() ?? \"\";"
                    : $"string s{index} = {paramName}?.ToString() ?? \"\";";
                BinaryGeneratorHelpers.AppendCode(sb,$$"""
                        {{toStringAssignment}}
                        int sLen{{index}} = Encoding.UTF8.GetByteCount(s{{index}});
                        if (position + sLen{{index}} > span.Length)
                        {
                            // Buffer too small - grow like List<T>
                            if (isThreadStatic) ZeroAllocHelper.ReleaseByteBuffer();
                            int newSize{{index}} = Math.Max(buffer.Length * 2, position + sLen{{index}});
                            byte[] newBuffer = new byte[newSize{{index}}];
                            span.Slice(0, position).CopyTo(newBuffer);
                            buffer = newBuffer;
                            span = buffer.AsSpan();
                            isThreadStatic = false;
                        }
                        position += Encoding.UTF8.GetBytes(s{{index}}, span.Slice(position));
                """);
                break;
        }
    }

    // ========================================================================
    // METHOD GENERATION - Bytes (Binary Serialization)
    // ========================================================================
    // Bytes() and TryBytes() are for binary (non-text) serialization.
    // They use the same byte buffer infrastructure as Utf8, but are intended
    // for types that implement IBinarySerializable for direct binary output.
    //
    // Note: Currently reuses Utf8 formatting code, but future versions may
    // add specialized binary serialization (fixed-width integers, etc.).
    // ========================================================================

    /// <summary>
    /// Generates a <c>Bytes()</c> method for binary serialization.
    /// </summary>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="args">The argument types for this overload.</param>
    private static void _GenerateBytesMethod(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        List<string> parameters = new List<string>();
        List<string> paramNames = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            // ========================================================================
            // Bytes() - Binary serialization
            // ========================================================================

            /// <summary>
            /// Serializes the given values into binary bytes.
            /// </summary>
            {{paramDocs}}
            /// <returns>A <see cref="TempBytes"/> that MUST be disposed with a using statement.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static TempBytes Bytes({{parameterList}})
            {
                // ----------------------------------------------------------------
                // Acquire buffer (ThreadStatic if available, fallback depends on RecursiveHeapFallback setting)
                // ----------------------------------------------------------------
                byte[] buffer = ZeroAllocHelper.AcquireByteBuffer(DefaultBufferSize, RecursiveHeapFallback, out bool isThreadStatic);
                Span<byte> span = buffer.AsSpan();
                int position = 0;

                // ----------------------------------------------------------------
                // Serialize each value into the buffer
                // ----------------------------------------------------------------

            """);

        for (int i = 0; i < args.Length; i++)
        {
            sb.Append('\n');
            _GenerateUtf8FormatCode(sb, args[i], paramNames[i], i);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

                return new TempBytes(buffer, position, isThreadStatic);
            }

            """);
    }

    /// <summary>
    /// Generates a <c>TryBytes()</c> method for safe binary serialization.
    /// </summary>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="args">The argument types for this overload.</param>
    private static void _GenerateTryBytesMethod(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        List<string> parameters = new List<string> { "out TempBytes result" };
        List<string> paramNames = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            /// <summary>
            /// Tries to serialize the given values into binary bytes.
            /// </summary>
            /// <param name="result">The resulting TempBytes if successful.</param>
            {{paramDocs}}
            /// <returns><c>true</c> on success; <c>false</c> if RecursiveHeapFallback is disabled and buffer is busy.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool TryBytes({{parameterList}})
            {
                // ----------------------------------------------------------------
                // Try to acquire buffer (respects RecursiveHeapFallback setting)
                // ----------------------------------------------------------------
                if (!RecursiveHeapFallback && !ZeroAllocHelper.IsByteBufferAvailable())
                {
                    result = default;
                    return false;
                }

                byte[] buffer = ZeroAllocHelper.AcquireByteBuffer(DefaultBufferSize, RecursiveHeapFallback, out bool isThreadStatic);
                Span<byte> span = buffer.AsSpan();
                int position = 0;

                // ----------------------------------------------------------------
                // Serialize each value into the buffer
                // ----------------------------------------------------------------

            """);

        for (int i = 0; i < args.Length; i++)
        {
            sb.Append('\n');
            _GenerateTryUtf8FormatCode(sb, args[i], paramNames[i], i);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

                result = new TempBytes(buffer, position, isThreadStatic);
                return true;
            }

            """);
    }

    // ========================================================================
    // METHOD GENERATION - LocalizedString
    // ========================================================================
    // These methods generate the LocalizedString() and TryLocalizedString() methods
    // for culture-sensitive char-based string formatting. Similar to String() but:
    // - Always expects IFormatProvider as first parameter
    // - Returns TempString (same as String, culture is handled via formatting)
    // ========================================================================

    /// <summary>
    /// Generates a <c>LocalizedString()</c> method that returns a <see cref="TempString"/>.
    /// </summary>
    private static void _GenerateLocalizedStringMethod(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        // First argument should be IFormatProvider
        if (args.Length == 0 || args[0].Category != TypeCategory.FormatProvider)
        {
            return; // Invalid signature
        }

        int startIndex = 1; // Skip IFormatProvider
        string cultureVar = "provider0";

        // Build parameter list
        List<string> parameters = new List<string>();
        List<string> paramNames = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = i == 0 ? "provider0" : $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            // ========================================================================
            // LocalizedString() - Culture-sensitive zero-allocation string formatting
            // ========================================================================

            /// <summary>
            /// Formats the given values into a temporary string using the specified culture.
            /// </summary>
            {{paramDocs}}
            /// <returns>A <see cref="TempString"/> that MUST be disposed with a using statement.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static TempString LocalizedString({{parameterList}})
            {
                char[] buffer = ZeroAllocHelper.AcquireCharBuffer(DefaultBufferSize, RecursiveHeapFallback, out bool isThreadStatic);
                Span<char> span = buffer.AsSpan();
                int position = 0;

                // ----------------------------------------------------------------
                // Format each value into the buffer using the specified culture
                // ----------------------------------------------------------------

            """);

        for (int i = startIndex; i < args.Length; i++)
        {
            sb.Append('\n');
            _GenerateCharFormatCode(sb, args[i], paramNames[i], i, cultureVar);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

                return new TempString(buffer, position, isThreadStatic);
            }

            """);
    }

    /// <summary>
    /// Generates a <c>TryLocalizedString()</c> method for non-throwing localized string formatting.
    /// </summary>
    private static void _GenerateTryLocalizedStringMethod(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        // First argument should be IFormatProvider
        if (args.Length == 0 || args[0].Category != TypeCategory.FormatProvider)
        {
            return; // Invalid signature
        }

        int startIndex = 1;
        string cultureVar = "provider0";

        // Build parameter list - TryLocalizedString has out TempString result first
        List<string> parameters = new List<string> { "out TempString result" };
        List<string> paramNames = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = i == 0 ? "provider0" : $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            /// <summary>
            /// Tries to format the given values into a temporary localized string.
            /// </summary>
            /// <param name="result">The resulting TempString if successful.</param>
            {{paramDocs}}
            /// <returns><c>true</c> on success; <c>false</c> if RecursiveHeapFallback is disabled and buffer is busy.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool TryLocalizedString({{parameterList}})
            {
                if (!RecursiveHeapFallback && !ZeroAllocHelper.IsCharBufferAvailable())
                {
                    result = default;
                    return false;
                }

                char[] buffer = ZeroAllocHelper.AcquireCharBuffer(DefaultBufferSize, RecursiveHeapFallback, out bool isThreadStatic);
                Span<char> span = buffer.AsSpan();
                int position = 0;

                // ----------------------------------------------------------------
                // Format each value into the buffer using the specified culture
                // ----------------------------------------------------------------

            """);

        for (int i = startIndex; i < args.Length; i++)
        {
            sb.Append('\n');
            _GenerateTryCharFormatCode(sb, args[i], paramNames[i], i, cultureVar);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

                result = new TempString(buffer, position, isThreadStatic);
                return true;
            }

            """);
    }

    // ========================================================================
    // METHOD GENERATION - LocalizedUtf8
    // ========================================================================
    // These methods generate the LocalizedUtf8() and TryLocalizedUtf8() methods
    // for culture-sensitive UTF-8 byte formatting. Similar to Utf8() but:
    // - Always expects IFormatProvider as first parameter
    // - Returns TempBytes (same as Utf8, culture is handled via formatting)
    // ========================================================================

    /// <summary>
    /// Generates a <c>LocalizedUtf8()</c> method that returns a <see cref="TempBytes"/>.
    /// </summary>
    private static void _GenerateLocalizedUtf8Method(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        // First argument should be IFormatProvider
        if (args.Length == 0 || args[0].Category != TypeCategory.FormatProvider)
        {
            return; // Invalid signature
        }

        int startIndex = 1;
        string cultureVar = "provider0";

        // Build parameter list
        List<string> parameters = new List<string>();
        List<string> paramNames = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = i == 0 ? "provider0" : $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            // ========================================================================
            // LocalizedUtf8() - Culture-sensitive zero-allocation UTF-8 formatting
            // ========================================================================

            /// <summary>
            /// Formats the given values into temporary UTF-8 bytes using the specified culture.
            /// </summary>
            {{paramDocs}}
            /// <returns>A <see cref="TempBytes"/> that MUST be disposed with a using statement.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static TempBytes LocalizedUtf8({{parameterList}})
            {
                byte[] buffer = ZeroAllocHelper.AcquireByteBuffer(DefaultBufferSize, RecursiveHeapFallback, out bool isThreadStatic);
                Span<byte> span = buffer.AsSpan();
                int position = 0;

                // ----------------------------------------------------------------
                // Format each value as UTF-8 using the specified culture
                // ----------------------------------------------------------------

            """);

        for (int i = startIndex; i < args.Length; i++)
        {
            sb.Append('\n');
            _GenerateUtf8FormatCodeWithCulture(sb, args[i], paramNames[i], i, cultureVar);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

                return new TempBytes(buffer, position, isThreadStatic);
            }

            """);
    }

    /// <summary>
    /// Generates a <c>TryLocalizedUtf8()</c> method for non-throwing localized UTF-8 formatting.
    /// </summary>
    private static void _GenerateTryLocalizedUtf8Method(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        // First argument should be IFormatProvider
        if (args.Length == 0 || args[0].Category != TypeCategory.FormatProvider)
        {
            return; // Invalid signature
        }

        int startIndex = 1;
        string cultureVar = "provider0";

        // Build parameter list - TryLocalizedUtf8 has out TempBytes result first
        List<string> parameters = new List<string> { "out TempBytes result" };
        List<string> paramNames = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = i == 0 ? "provider0" : $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            /// <summary>
            /// Tries to format the given values into temporary localized UTF-8 bytes.
            /// </summary>
            /// <param name="result">The resulting TempBytes if successful.</param>
            {{paramDocs}}
            /// <returns><c>true</c> on success; <c>false</c> if RecursiveHeapFallback is disabled and buffer is busy.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static bool TryLocalizedUtf8({{parameterList}})
            {
                if (!RecursiveHeapFallback && !ZeroAllocHelper.IsByteBufferAvailable())
                {
                    result = default;
                    return false;
                }

                byte[] buffer = ZeroAllocHelper.AcquireByteBuffer(DefaultBufferSize, RecursiveHeapFallback, out bool isThreadStatic);
                Span<byte> span = buffer.AsSpan();
                int position = 0;

                // ----------------------------------------------------------------
                // Format each value as UTF-8 using the specified culture
                // ----------------------------------------------------------------

            """);

        for (int i = startIndex; i < args.Length; i++)
        {
            sb.Append('\n');
            _GenerateUtf8FormatCodeWithCulture(sb, args[i], paramNames[i], i, cultureVar);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

                result = new TempBytes(buffer, position, isThreadStatic);
                return true;
            }

            """);
    }

    // ========================================================================
    // METHOD GENERATION - Lazy
    // ========================================================================
    // Generates Lazy() methods that return LazyString. These defer evaluation
    // by capturing arguments in a tuple and calling the generated String()
    // method when the LazyString is first accessed.
    //
    // Benefits over manual LazyString.FormatLazy:
    // - Eliminates verbose tuple + lambda boilerplate at call sites
    // - Automatically selects optimal state capture (direct vs tuple)
    // - Generated String() method provides zero-allocation formatting
    // ========================================================================

    /// <summary>
    /// Generates a <c>Lazy()</c> method that returns a <see cref="LazyString"/>.
    /// The method defers evaluation by capturing arguments and calling the corresponding
    /// <c>String()</c> method on first access.
    /// </summary>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="args">The argument types for this overload.</param>
    private static void _GenerateLazyMethod(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        // Build parameter list
        List<string> parameters = [];
        List<string> paramNames = [];
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            // ========================================================================
            // Lazy() - Deferred zero-allocation string formatting
            // ========================================================================

            /// <summary>
            /// Creates a <see cref="LazyString"/> that defers formatting until first access.
            /// On evaluation, uses the generated <c>String()</c> method for zero-allocation formatting.
            /// </summary>
            {{paramDocs}}
            /// <returns>A <see cref="LazyString"/> that evaluates lazily on first access.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static LazyString Lazy({{parameterList}})
            {

            """);

        if (args.Length == 1)
        {
            BinaryGeneratorHelpers.AppendCode(sb,$$"""
                    return LazyString.FormatLazy(
                        {{paramNames[0]}},
                        static s => String(s));
            """);
        }
        else
        {
            string tupleArgs = string.Join(", ", paramNames);
            string tupleAccess = string.Join(", ", Enumerable.Range(1, args.Length).Select(i => $"s.Item{i}"));
            BinaryGeneratorHelpers.AppendCode(sb,$$"""
                    return LazyString.FormatLazy(
                        ({{tupleArgs}}),
                        static s => String({{tupleAccess}}));
            """);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

            }

            """);
    }

    // ========================================================================
    // METHOD GENERATION - LazyInterpolated
    // ========================================================================
    // Generates LazyInterpolated() methods that return LazyString. These defer
    // evaluation by capturing arguments in a tuple and using string interpolation
    // when the LazyString is first accessed.
    //
    // Use LazyInterpolated when:
    // - The call site is inside another String() scope (ThreadStatic contention)
    // - The formatting pattern is simple concatenation
    //
    // Note: LazyInterpolated allocates a string on evaluation via interpolation,
    // while Lazy() uses the zero-allocation String() pipeline.
    // ========================================================================

    /// <summary>
    /// Generates a <c>LazyInterpolated()</c> method that returns a <see cref="LazyString"/>.
    /// The method defers evaluation by capturing arguments and using string interpolation
    /// when evaluated, avoiding ThreadStatic buffer contention.
    /// </summary>
    /// <param name="sb">The string builder to append generated code to.</param>
    /// <param name="args">The argument types for this overload.</param>
    private static void _GenerateLazyInterpolatedMethod(
        StringBuilder sb,
        ImmutableArray<ArgumentTypeInfo> args)
    {
        // Build parameter list
        List<string> parameters = [];
        List<string> paramNames = [];
        for (int i = 0; i < args.Length; i++)
        {
            string paramName = $"{args[i].ShortName}{i}";
            parameters.Add($"{args[i].FullTypeName} {paramName}");
            paramNames.Add(paramName);
        }

        string paramDocs = string.Join(
            "\n",
            Enumerable.Range(0, args.Length)
                .Select(i => $"    /// <param name=\"{paramNames[i]}\">Value of type <see cref=\"{args[i].FullTypeName}\"/>.</param>"));
        string parameterList = string.Join(", ", parameters);

        BinaryGeneratorHelpers.AppendCode(sb,$$"""
            // ========================================================================
            // LazyInterpolated() - Deferred string formatting via interpolation
            // ========================================================================

            /// <summary>
            /// Creates a <see cref="LazyString"/> that defers formatting until first access.
            /// On evaluation, uses string interpolation (allocates on eval but avoids
            /// ThreadStatic buffer contention with nested <c>String()</c> calls).
            /// </summary>
            {{paramDocs}}
            /// <returns>A <see cref="LazyString"/> that evaluates lazily on first access.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static LazyString LazyInterpolated({{parameterList}})
            {

            """);

        if (args.Length == 1)
        {
            BinaryGeneratorHelpers.AppendCode(sb,$$"""
                    return LazyString.FormatLazy(
                        {{paramNames[0]}},
                        static s => s.ToString()!);
            """);
        }
        else
        {
            string tupleArgs = string.Join(", ", paramNames);
            string interpolationParts = string.Join("", Enumerable.Range(1, args.Length).Select(i => $"{{s.Item{i}}}"));
            string evalLambda = $"static s => $\"{interpolationParts}\"";
            BinaryGeneratorHelpers.AppendCode(sb,$$"""
                    return LazyString.FormatLazy(
                        ({{tupleArgs}}),
                        {{evalLambda}});
            """);
        }

        BinaryGeneratorHelpers.AppendCode(sb,"""

            }

            """);
    }
}

// ============================================================================
// INTERNAL DATA STRUCTURES
// ============================================================================
// These records and enums hold the data extracted during syntax/semantic
// analysis for use during code generation. They are designed to be:
//
// - Immutable (readonly record struct) for incremental generator caching
// - Equatable for proper cache hit detection
// - Small enough to avoid excessive memory pressure
//
// Key structures:
// - UserClassInfo: Represents a class inheriting from ZeroAllocBase
// - MethodCallInfo: Represents a call to String/Utf8/Bytes and its arguments
// - ArgumentTypeInfo: Detailed info about a single argument's type
// - TypeCategory: Enum categorizing types for code generation strategy
// ============================================================================

/// <summary>
/// Information about a user-defined class that inherits from <c>ZeroAllocBase</c>.
/// </summary>
/// <remarks>
/// This is collected during syntax analysis and used during code generation
/// to create the partial class with generated methods.
/// </remarks>
/// <param name="ClassName">The simple name of the class (e.g., "ZA").</param>
/// <param name="Namespace">The namespace containing the class, or <c>null</c> for global namespace.</param>
/// <param name="FullName">The fully qualified name (e.g., "MyApp.ZA").</param>
/// <param name="Location">Source location for diagnostic reporting.</param>
/// <param name="IsPublic">Whether the class has public accessibility.</param>
/// <param name="IsPartial">Whether the class is declared as partial.</param>
internal readonly record struct UserClassInfo(
    string ClassName,
    string? Namespace,
    string FullName,
    Location Location,
    bool IsPublic,
    bool IsPartial);

/// <summary>
/// Information about a method call to a ZeroAlloc API method (String, Utf8, Bytes).
/// </summary>
/// <remarks>
/// Each unique combination of target class and argument types results in
/// a generated method overload. Multiple calls with the same signature
/// are deduplicated.
/// </remarks>
/// <param name="TargetClass">Fully qualified name of the class being called on.</param>
/// <param name="MethodName">The API method name (String, TryString, Utf8, etc.).</param>
/// <param name="Arguments">Type information for each argument.</param>
/// <param name="Location">Source location for diagnostic reporting.</param>
internal readonly record struct MethodCallInfo(
    string TargetClass,
    string MethodName,
    ImmutableArray<ArgumentTypeInfo> Arguments,
    Location Location);

/// <summary>
/// Detailed information about an argument type for code generation.
/// </summary>
/// <remarks>
/// <para>
/// This record contains all the information needed to generate optimal
/// formatting code for a single argument:
/// </para>
/// <list type="bullet">
///   <item><b>Category:</b> Determines which formatting strategy to use</item>
///   <item><b>Size hints:</b> Enable buffer pre-allocation optimization</item>
///   <item><b>Interface flags:</b> Track ISpanFormattable, IUtf8SpanFormattable support</item>
///   <item><b>Ref struct flag:</b> Determines null-checking strategy</item>
/// </list>
/// </remarks>
/// <param name="FullTypeName">Fully qualified type name for parameter declaration.</param>
/// <param name="ShortName">Short name for parameter naming (e.g., "s" for string).</param>
/// <param name="Category">Type category determining formatting strategy.</param>
/// <param name="IsRefStruct">True if the type is a ref struct (can't use ?. operator).</param>
/// <param name="IsValueType">True if the type is a value type.</param>
/// <param name="IsArray">True if the type is an array type.</param>
/// <param name="IsZeroAllocInternal">True if this is a ZeroAlloc type (TempString, etc.).</param>
/// <param name="MayUseZeroAllocInternally">True if the type might use ZeroAlloc in TryFormat.</param>
/// <param name="KnownSize">Exact size if known at compile time (e.g., char = 1).</param>
/// <param name="MinimumSize">Minimum formatted size if known.</param>
/// <param name="ImplementsIStringSize">True if type provides string size hints.</param>
/// <param name="ImplementsIUtf8Size">True if type provides UTF-8 size hints.</param>
internal readonly record struct ArgumentTypeInfo(
    string FullTypeName,
    string ShortName,
    TypeCategory Category,
    bool IsRefStruct = false,
    bool IsValueType = false,
    bool IsArray = false,
    bool IsZeroAllocInternal = false,
    bool MayUseZeroAllocInternally = false,
    int? KnownSize = null,
    int? MinimumSize = null,
    bool ImplementsIStringSize = false,
    bool ImplementsIUtf8Size = false)
{
    /// <summary>
    /// Returns true if this type does not support the ?. (null-conditional) operator.
    /// This includes ref structs and non-nullable value types.
    /// </summary>
    public bool CannotUseNullConditional => IsRefStruct || IsValueType;

    /// <summary>
    /// Returns true if this type could potentially implement ISpanFormattable.
    /// False for ref structs, arrays, and ZeroAlloc internal types.
    /// </summary>
    public bool CanImplementISpanFormattable => !IsRefStruct && !IsArray && !IsZeroAllocInternal;

    /// <summary>
    /// Returns true if the exact size can be determined before formatting.
    /// </summary>
    public bool HasKnownExactSize => KnownSize.HasValue || ImplementsIStringSize || ImplementsIUtf8Size;

    /// <summary>
    /// Returns true if at least a minimum size can be determined quickly.
    /// </summary>
    public bool HasKnownMinimumSize => MinimumSize.HasValue || KnownSize.HasValue;
}

/// <summary>
/// Category of argument types that determines the code generation strategy.
/// </summary>
/// <remarks>
/// <para>
/// The order of preference for formatting is:
/// </para>
/// <list type="number">
///   <item>Built-in types (String, Char, Boolean) - specialized fast paths</item>
///   <item>ISpanFormattable/IUtf8SpanFormattable - zero-allocation formatting</item>
///   <item>IBinarySerializable - direct binary output</item>
///   <item>Object - fallback using ToString() (generates warning ZA1004)</item>
/// </list>
/// </remarks>
internal enum TypeCategory
{
    /// <summary>System.String - direct copy via <c>AsSpan()</c>.</summary>
    String,
    /// <summary>System.Char - single character, always 1 char/1-4 bytes UTF-8.</summary>
    Char,
    /// <summary>System.Boolean - "True" (4) or "False" (5) chars.</summary>
    Boolean,
    /// <summary>ISpanFormattable - uses <c>TryFormat</c> to char span.</summary>
    SpanFormattable,
    /// <summary>IUtf8SpanFormattable - uses <c>TryFormat</c> directly to UTF-8 bytes.</summary>
    Utf8SpanFormattable,
    /// <summary>IBinarySerializable - uses <c>TryWrite</c> for binary data.</summary>
    BinarySerializable,
    /// <summary>ZeroAlloc.Raw - raw bytes wrapper (ref struct, cannot implement interfaces).</summary>
    RawBytes,
    /// <summary>Fallback - uses <c>ToString()</c> which allocates a string.</summary>
    Object,
    /// <summary>CultureInfo - used as IFormatProvider, not written to output.</summary>
    CultureInfo,
    /// <summary>IFormatProvider - used for culture-sensitive formatting, not written to output.</summary>
    FormatProvider
}

/// <summary>
/// Helper class providing human-readable descriptions for inline code comments.
/// </summary>
/// <remarks>
/// The generated code includes comments explaining the formatting strategy
/// for each argument. This improves readability when debugging generated code.
/// </remarks>
file static class CommentHelper
{
    /// <summary>
    /// Gets a human-readable description of the formatting strategy for a type category.
    /// </summary>
    /// <param name="category">The type category.</param>
    /// <returns>A description suitable for inline code comments.</returns>
    public static string GetFormattingStrategy(TypeCategory category) => category switch
    {
        TypeCategory.String => "Direct copy (string.AsSpan)",
        TypeCategory.Char => "Single character write",
        TypeCategory.Boolean => "Literal \"True\"/\"False\" copy",
        TypeCategory.SpanFormattable => "ISpanFormattable.TryFormat",
        TypeCategory.Utf8SpanFormattable => "IUtf8SpanFormattable.TryFormat",
        TypeCategory.BinarySerializable => "IBinarySerializable.TryWrite",
        TypeCategory.RawBytes => "Raw bytes (direct copy)",
        TypeCategory.CultureInfo => "Format provider (not written)",
        TypeCategory.FormatProvider => "Format provider (not written)",
        _ => "Fallback to ToString() (allocates!)"
    };

    /// <summary>
    /// Gets a detailed explanation of the formatting behavior for a type category.
    /// This is included in generated code to help developers understand what's happening.
    /// </summary>
    /// <param name="category">The type category.</param>
    /// <returns>A detailed explanation suitable for inline code comments.</returns>
    public static string GetFormattingExplanation(TypeCategory category) => category switch
    {
        TypeCategory.String => "Zero-allocation: copies chars directly into buffer",
        TypeCategory.Char => "Zero-allocation: single char assignment",
        TypeCategory.Boolean => "Zero-allocation: writes \"True\" or \"False\" literal",
        TypeCategory.SpanFormattable => "Zero-allocation: formats directly into buffer via TryFormat",
        TypeCategory.Utf8SpanFormattable => "Zero-allocation: encodes directly to UTF-8 via TryFormat",
        TypeCategory.BinarySerializable => "Zero-allocation: serializes directly into buffer",
        TypeCategory.RawBytes => "Zero-allocation: copies raw bytes directly into buffer",
        TypeCategory.CultureInfo => "Used as format provider for other arguments",
        TypeCategory.FormatProvider => "Used as format provider for other arguments",
        _ => "ALLOCATES: Falls back to ToString() which creates a temporary string"
    };
}

/// <summary>
/// Equality comparer for deduplicating argument lists by type name.
/// </summary>
/// <remarks>
/// <para>
/// Used to identify unique method signatures. Two argument lists are equal if:
/// </para>
/// <list type="bullet">
///   <item>They have the same length</item>
///   <item>Each corresponding argument has the same <see cref="ArgumentTypeInfo.FullTypeName"/></item>
/// </list>
/// <para>
/// This enables signature deduplication even when the same method is called
/// multiple times in the source code.
/// </para>
/// </remarks>
internal sealed class ArgumentListComparer : IEqualityComparer<ImmutableArray<ArgumentTypeInfo>>
{
    /// <summary>
    /// Determines whether two argument lists represent the same method signature.
    /// </summary>
    public bool Equals(ImmutableArray<ArgumentTypeInfo> x, ImmutableArray<ArgumentTypeInfo> y)
    {
        if (x.Length != y.Length)
        {
            return false;
        }

        for (int i = 0; i < x.Length; i++)
        {
            if (x[i].FullTypeName != y[i].FullTypeName)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Computes a hash code for an argument list based on type names.
    /// </summary>
    /// <remarks>
    /// Uses a simple multiplicative hash combining all type names.
    /// </remarks>
    public int GetHashCode(ImmutableArray<ArgumentTypeInfo> obj)
    {
        int hash = 17;
        foreach (ArgumentTypeInfo arg in obj)
        {
            hash = hash * 31 + arg.FullTypeName.GetHashCode();
        }
        return hash;
    }
}

