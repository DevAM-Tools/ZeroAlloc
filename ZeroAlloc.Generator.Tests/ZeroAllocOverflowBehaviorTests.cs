// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Generator.Tests;

/// <summary>
/// Compiles generated <c>ZA.String</c> overflow modes and runs them against <see cref="ZeroAllocHelper"/>.
/// </summary>
/// <remarks>
/// Not in parallel: probes share <see cref="ZeroAllocHelper"/> ThreadStatic state with the loaded assembly.
/// </remarks>
[NotInParallel]
internal sealed class ZeroAllocOverflowBehaviorTests
{
    private const int _TinyBufferChars = 16;
    private const int _OverflowPayloadChars = 64;
    private const int _HeapFallbackPayloadChars = 65_536;

    /// <summary>Drops leftover Grow capacity so later Throw/Try probes still overflow.</summary>
    [Before(Test)]
    public void ReleaseLeftoverThreadStaticBuffers()
    {
        if (!ZeroAllocHelper.IsCharBufferAvailable() || !ZeroAllocHelper.IsByteBufferAvailable())
        {
            return;
        }

        ZeroAllocHelper.ReleaseBuffers();
    }

    private const string _ProbeSource = """
        using System;
        using ZeroAlloc;

        internal partial class OverflowApi : ZeroAllocBase { }

        public static class OverflowProbe
        {
            public static int ThrowAndBufferState(string payload)
            {
                try
                {
                    OverflowApi.String(payload).Dispose();
                    return 0;
                }
                catch (InvalidOperationException)
                {
                    return ZeroAllocHelper.IsCharBufferAvailable() ? 1 : 2;
                }
            }

            public static bool GrowKeepsThreadStatic(string payload)
            {
                using TempString formatted = OverflowApi.String(payload);
                return !formatted.IsHeapAllocated;
            }

            public static bool HeapFallbackSetsHeapAllocated(string payload)
            {
                using TempString formatted = OverflowApi.String(payload);
                return formatted.IsHeapAllocated;
            }

            public static bool TryStringOverflowReturnsTrue(string payload)
            {
                bool ok = OverflowApi.TryString(out TempString formatted, payload);
                formatted.Dispose();
                return ok;
            }
        }
        """;

    /// <summary>Throw overflow must not pin the ThreadStatic char lock.</summary>
    [Test]
    public async Task GeneratedString_ThrowOverflow_ReleasesCharBuffer()
    {
        System.Reflection.Assembly assembly = _CompileOverflowApi("Throw", "ThrowOverflow");
        int state = _InvokeInt(assembly, "ThrowAndBufferState", _OverflowPayloadChars);

        await Assert.That(state).IsEqualTo(1);
    }

    /// <summary>Grow overflow must keep ThreadStatic ownership.</summary>
    [Test]
    public async Task GeneratedString_GrowOverflow_KeepsThreadStaticBuffer()
    {
        System.Reflection.Assembly assembly = _CompileOverflowApi("Grow", "GrowOverflow");
        bool staysThreadStatic = _InvokeBool(assembly, "GrowKeepsThreadStatic", _OverflowPayloadChars);

        await Assert.That(staysThreadStatic).IsTrue();
    }

    /// <summary>HeapFallback overflow must return a heap TempString.</summary>
    [Test]
    public async Task GeneratedString_HeapFallbackOverflow_SetsIsHeapAllocated()
    {
        System.Reflection.Assembly assembly = _CompileOverflowApi("HeapFallback", "HeapFallbackOverflow");
        bool isHeap = _InvokeBool(assembly, "HeapFallbackSetsHeapAllocated", _HeapFallbackPayloadChars);

        await Assert.That(isHeap).IsTrue();
    }

    /// <summary>TryString must not throw when the generated const is Throw.</summary>
    [Test]
    public async Task GeneratedTryString_ThrowOverflow_ReturnsTrueWithoutThrowing()
    {
        System.Reflection.Assembly assembly = _CompileOverflowApi("Throw", "TryThrowOverflow");
        bool ok = _InvokeBool(assembly, "TryStringOverflowReturnsTrue", _OverflowPayloadChars);

        await Assert.That(ok).IsTrue();
    }

    private static System.Reflection.Assembly _CompileOverflowApi(string overflowBehavior, string assemblyName)
    {
        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(_ProbeSource, assemblyName);
        TestAnalyzerConfigOptionsProvider options = TestAnalyzerConfigOptionsProvider.ForZeroAlloc(
            overflowBehavior,
            _TinyBufferChars);
        (Compilation output, _) = GeneratorTestHelper.RunGenerator(compilation, options, new ZeroAllocGenerator());
        return GeneratorTestHelper.EmitToAssembly(output);
    }

    private static int _InvokeInt(System.Reflection.Assembly assembly, string methodName, int payloadChars)
    {
        MethodInfo method = _GetProbeMethod(assembly, methodName);
        return (int)method.Invoke(null, [new string('x', payloadChars)])!;
    }

    private static bool _InvokeBool(System.Reflection.Assembly assembly, string methodName, int payloadChars)
    {
        MethodInfo method = _GetProbeMethod(assembly, methodName);
        return (bool)method.Invoke(null, [new string('x', payloadChars)])!;
    }

    private static MethodInfo _GetProbeMethod(System.Reflection.Assembly assembly, string methodName)
    {
        Type probeType = assembly.GetType("OverflowProbe")
            ?? throw new InvalidOperationException("OverflowProbe was not found in the emitted assembly.");
        return probeType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException($"OverflowProbe.{methodName} was not found.");
    }
}
