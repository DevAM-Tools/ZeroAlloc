// Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information.

namespace ZeroAlloc.Tests;

/// <summary>
/// Tests for <see cref="ZeroAllocHelper"/> buffer acquisition, growth, and lifecycle.
/// </summary>
[NotInParallel("Uses ThreadStatic buffers shared per thread.")]
public sealed class ZeroAllocHelperTests
{

    // ========================================================================
    // CHAR BUFFER ACQUISITION
    // ========================================================================


    /// <summary>Verifies AcquireCharBuffer FirstCall ReturnsThreadStaticBuffer.</summary>
    [Test]
    public async Task AcquireCharBuffer_FirstCall_ReturnsThreadStaticBuffer()
    {
        bool isThreadStatic;
        int length;
        {
            char[] buffer = ZeroAllocHelper.AcquireCharBuffer(64, out isThreadStatic);
            length = buffer.Length;
            ZeroAllocHelper.ReleaseCharBuffer();
        }

        await Assert.That(isThreadStatic).IsTrue();
        await Assert.That(length).IsGreaterThanOrEqualTo(64);
    }
    /// <summary>Verifies AcquireCharBuffer NestedWithFallback ReturnsHeapBuffer.</summary>
    [Test]
    public async Task AcquireCharBuffer_NestedWithFallback_ReturnsHeapBuffer()
    {
        bool outerThreadStatic;
        bool innerThreadStatic;
        {
            char[] outer = ZeroAllocHelper.AcquireCharBuffer(64, out outerThreadStatic);
            char[] inner = ZeroAllocHelper.AcquireCharBuffer(64, recursiveHeapFallback: true, out innerThreadStatic);
            _ = outer;
            _ = inner;
            ZeroAllocHelper.ReleaseCharBuffer();
            ZeroAllocHelper.ReleaseCharBuffer();
        }

        await Assert.That(outerThreadStatic).IsTrue();
        await Assert.That(innerThreadStatic).IsFalse();
    }
    /// <summary>Verifies AcquireCharBuffer NestedWithoutFallback ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task AcquireCharBuffer_NestedWithoutFallback_ThrowsInvalidOperationException()
    {
        bool threw = false;
        {
            char[] outer = ZeroAllocHelper.AcquireCharBuffer(64, out _);
            _ = outer;
            try
            {
                ZeroAllocHelper.AcquireCharBuffer(64, recursiveHeapFallback: false, out _);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            ZeroAllocHelper.ReleaseCharBuffer();
        }

        await Assert.That(threw).IsTrue();
    }
    /// <summary>Verifies IsCharBufferAvailable ReflectsAcquisitionState.</summary>
    [Test]
    public async Task IsCharBufferAvailable_ReflectsAcquisitionState()
    {
        bool availableBefore;
        bool availableDuring;
        bool availableAfter;
        {
            availableBefore = ZeroAllocHelper.IsCharBufferAvailable();
            ZeroAllocHelper.AcquireCharBuffer(64, out _);
            availableDuring = ZeroAllocHelper.IsCharBufferAvailable();
            ZeroAllocHelper.ReleaseCharBuffer();
            availableAfter = ZeroAllocHelper.IsCharBufferAvailable();
        }

        await Assert.That(availableBefore).IsTrue();
        await Assert.That(availableDuring).IsFalse();
        await Assert.That(availableAfter).IsTrue();
    }

    // ========================================================================
    // BYTE BUFFER ACQUISITION
    // ========================================================================


    /// <summary>Verifies AcquireByteBuffer FirstCall ReturnsThreadStaticBuffer.</summary>
    [Test]
    public async Task AcquireByteBuffer_FirstCall_ReturnsThreadStaticBuffer()
    {
        bool isThreadStatic;
        {
            byte[] buffer = ZeroAllocHelper.AcquireByteBuffer(64, out isThreadStatic);
            _ = buffer;
            ZeroAllocHelper.ReleaseByteBuffer();
        }

        await Assert.That(isThreadStatic).IsTrue();
    }
    /// <summary>Verifies AcquireByteBuffer NestedWithoutFallback ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task AcquireByteBuffer_NestedWithoutFallback_ThrowsInvalidOperationException()
    {
        bool threw = false;
        {
            ZeroAllocHelper.AcquireByteBuffer(64, out _);
            try
            {
                ZeroAllocHelper.AcquireByteBuffer(64, recursiveHeapFallback: false, out _);
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            ZeroAllocHelper.ReleaseByteBuffer();
        }

        await Assert.That(threw).IsTrue();
    }
    /// <summary>Verifies IsByteBufferAvailable ReflectsAcquisitionState.</summary>
    [Test]
    public async Task IsByteBufferAvailable_ReflectsAcquisitionState()
    {
        bool availableBefore;
        bool availableDuring;
        bool availableAfter;
        {
            availableBefore = ZeroAllocHelper.IsByteBufferAvailable();
            ZeroAllocHelper.AcquireByteBuffer(64, out _);
            availableDuring = ZeroAllocHelper.IsByteBufferAvailable();
            ZeroAllocHelper.ReleaseByteBuffer();
            availableAfter = ZeroAllocHelper.IsByteBufferAvailable();
        }

        await Assert.That(availableBefore).IsTrue();
        await Assert.That(availableDuring).IsFalse();
        await Assert.That(availableAfter).IsTrue();
    }

    // ========================================================================
    // RESIZE AND RELEASE
    // ========================================================================


    /// <summary>Verifies ResizeCharBuffer ValidSize UpdatesBufferSize.</summary>
    [Test]
    public async Task ResizeCharBuffer_ValidSize_UpdatesBufferSize()
    {
        int size;
        {
            ZeroAllocHelper.ResizeCharBuffer(128);
            size = ZeroAllocHelper.GetCharBufferSize();
        }

        await Assert.That(size).IsEqualTo(128);
    }
    /// <summary>Verifies ResizeCharBuffer InvalidSize ThrowsArgumentOutOfRangeException.</summary>
    [Test]
    public async Task ResizeCharBuffer_InvalidSize_ThrowsArgumentOutOfRangeException()
    {
        bool threw = false;
        try { ZeroAllocHelper.ResizeCharBuffer(0); }
        catch (ArgumentOutOfRangeException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }
    /// <summary>Verifies ResizeCharBuffer WhileInUse ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task ResizeCharBuffer_WhileInUse_ThrowsInvalidOperationException()
    {
        bool threw = false;
        {
            ZeroAllocHelper.AcquireCharBuffer(64, out _);
            try { ZeroAllocHelper.ResizeCharBuffer(128); }
            catch (InvalidOperationException) { threw = true; }
            ZeroAllocHelper.ReleaseCharBuffer();
        }

        await Assert.That(threw).IsTrue();
    }
    /// <summary>Verifies ResizeByteBuffer ValidSize UpdatesBufferSize.</summary>
    [Test]
    public async Task ResizeByteBuffer_ValidSize_UpdatesBufferSize()
    {
        int size;
        {
            ZeroAllocHelper.ResizeByteBuffer(256);
            size = ZeroAllocHelper.GetByteBufferSize();
        }

        await Assert.That(size).IsEqualTo(256);
    }
    /// <summary>Verifies ResizeByteBuffer InvalidSize ThrowsArgumentOutOfRangeException.</summary>
    [Test]
    public async Task ResizeByteBuffer_InvalidSize_ThrowsArgumentOutOfRangeException()
    {
        bool threw = false;
        try { ZeroAllocHelper.ResizeByteBuffer(-1); }
        catch (ArgumentOutOfRangeException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }
    /// <summary>Verifies ReleaseBuffers WhenIdle ClearsBuffers.</summary>
    [Test]
    public async Task ReleaseBuffers_WhenIdle_ClearsBuffers()
    {
        int charSize;
        int byteSize;
        {
            ZeroAllocHelper.ResizeCharBuffer(64);
            ZeroAllocHelper.ResizeByteBuffer(64);
            ZeroAllocHelper.ReleaseBuffers();
            charSize = ZeroAllocHelper.GetCharBufferSize();
            byteSize = ZeroAllocHelper.GetByteBufferSize();
        }

        await Assert.That(charSize).IsEqualTo(0);
        await Assert.That(byteSize).IsEqualTo(0);
    }
    /// <summary>Verifies ReleaseBuffers WhileCharBufferInUse ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task ReleaseBuffers_WhileCharBufferInUse_ThrowsInvalidOperationException()
    {
        bool threw = false;
        {
            ZeroAllocHelper.AcquireCharBuffer(64, out _);
            try { ZeroAllocHelper.ReleaseBuffers(); }
            catch (InvalidOperationException) { threw = true; }
            ZeroAllocHelper.ReleaseCharBuffer();
        }

        await Assert.That(threw).IsTrue();
    }

    // ========================================================================
    // GROWTH
    // ========================================================================


    /// <summary>Verifies CalculateGrowth ReturnsAtLeastRequiredSize.</summary>
    [Test]
    [Arguments(100, 200, 300)]
    [Arguments(1_048_576, 2_000_000, 3_145_728)]
    [Arguments(16_777_216, 20_000_000, 33_554_432)]
    public async Task CalculateGrowth_ReturnsAtLeastRequiredSize(int current, int required, int expectedMin)
    {
        int newSize = ZeroAllocHelper.CalculateGrowth(current, required);

        await Assert.That(newSize).IsGreaterThanOrEqualTo(required);
        await Assert.That(newSize).IsGreaterThanOrEqualTo(expectedMin);
    }
    /// <summary>Verifies GrowCharBuffer CopiesExistingContent.</summary>
    [Test]
    public async Task GrowCharBuffer_CopiesExistingContent()
    {
        char firstChar;
        int newLength;
        {
            char[] buffer = ZeroAllocHelper.AcquireCharBuffer(64, out _);
            buffer[0] = 'Z';
            char[] grown = ZeroAllocHelper.GrowCharBuffer(128);
            firstChar = grown[0];
            newLength = grown.Length;
            ZeroAllocHelper.ReleaseCharBuffer();
        }

        await Assert.That(firstChar).IsEqualTo('Z');
        await Assert.That(newLength).IsGreaterThanOrEqualTo(128);
    }
    /// <summary>Verifies TryGrowCharBuffer WhenBufferNotAllocated ReturnsNull.</summary>
    [Test]
    public async Task TryGrowCharBuffer_WhenBufferNotAllocated_ReturnsNull()
    {
        char[]? result;
        {
            ZeroAllocHelper.ReleaseBuffers();
            result = ZeroAllocHelper.TryGrowCharBuffer(128);
        }

        await Assert.That(result).IsNull();
    }
    /// <summary>Verifies GrowByteBuffer CopiesExistingContent.</summary>
    [Test]
    public async Task GrowByteBuffer_CopiesExistingContent()
    {
        byte firstByte;
        {
            byte[] buffer = ZeroAllocHelper.AcquireByteBuffer(64, out _);
            buffer[0] = 0xAB;
            byte[] grown = ZeroAllocHelper.GrowByteBuffer(128);
            firstByte = grown[0];
            ZeroAllocHelper.ReleaseByteBuffer();
        }

        await Assert.That((int)firstByte).IsEqualTo(0xAB);
    }
    /// <summary>Verifies TryGrowByteBuffer WhenBufferNotAllocated ReturnsNull.</summary>
    [Test]
    public async Task TryGrowByteBuffer_WhenBufferNotAllocated_ReturnsNull()
    {
        byte[]? result;
        {
            ZeroAllocHelper.ReleaseBuffers();
            result = ZeroAllocHelper.TryGrowByteBuffer(128);
        }

        await Assert.That(result).IsNull();
    }
    /// <summary>Verifies DefaultBufferSize IsTwoMiB.</summary>
    [Test]
    public async Task DefaultBufferSize_IsTwoMiB()
    {
        int expected = int.Parse("2097152", CultureInfo.InvariantCulture);
        int actual = ZeroAllocHelper.DefaultBufferSize;

        await Assert.That(actual).IsEqualTo(expected);
    }

    // ========================================================================
    // EXIT-POINT COVERAGE — byte resize/release while in use / TryGrow success
    // ========================================================================


    /// <summary>Verifies ResizeByteBuffer WhileInUse ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task ResizeByteBuffer_WhileInUse_ThrowsInvalidOperationException()
    {
        bool threw = false;
        {
            ZeroAllocHelper.AcquireByteBuffer(64, out _);
            try { ZeroAllocHelper.ResizeByteBuffer(128); }
            catch (InvalidOperationException) { threw = true; }
            ZeroAllocHelper.ReleaseByteBuffer();
        }

        await Assert.That(threw).IsTrue();
    }
    /// <summary>Verifies ReleaseBuffers WhileByteBufferInUse ThrowsInvalidOperationException.</summary>
    [Test]
    public async Task ReleaseBuffers_WhileByteBufferInUse_ThrowsInvalidOperationException()
    {
        bool threw = false;
        {
            ZeroAllocHelper.AcquireByteBuffer(64, out _);
            try { ZeroAllocHelper.ReleaseBuffers(); }
            catch (InvalidOperationException) { threw = true; }
            ZeroAllocHelper.ReleaseByteBuffer();
        }

        await Assert.That(threw).IsTrue();
    }
    /// <summary>Verifies TryGrowCharBuffer WhenBufferAcquired ReturnsGrownBuffer.</summary>
    [Test]
    public async Task TryGrowCharBuffer_WhenBufferAcquired_ReturnsGrownBuffer()
    {
        char firstChar;
        int newLength;
        {
            char[] buffer = ZeroAllocHelper.AcquireCharBuffer(64, out _);
            buffer[0] = 'X';
            char[]? grown = ZeroAllocHelper.TryGrowCharBuffer(128);
            firstChar = grown![0];
            newLength = grown.Length;
            ZeroAllocHelper.ReleaseCharBuffer();
        }

        await Assert.That(firstChar).IsEqualTo('X');
        await Assert.That(newLength).IsGreaterThanOrEqualTo(128);
    }
    /// <summary>Verifies TryGrowByteBuffer WhenBufferAcquired ReturnsGrownBuffer.</summary>
    [Test]
    public async Task TryGrowByteBuffer_WhenBufferAcquired_ReturnsGrownBuffer()
    {
        byte firstByte;
        int newLength;
        {
            byte[] buffer = ZeroAllocHelper.AcquireByteBuffer(64, out _);
            buffer[0] = 0xCD;
            byte[]? grown = ZeroAllocHelper.TryGrowByteBuffer(128);
            firstByte = grown![0];
            newLength = grown.Length;
            ZeroAllocHelper.ReleaseByteBuffer();
        }

        await Assert.That((int)firstByte).IsEqualTo(0xCD);
        await Assert.That(newLength).IsGreaterThanOrEqualTo(128);
    }

    /// <summary>Verifies ConsumeSimulatedGrowStall WhenUnset ReturnsFalse.</summary>
    [Test]
    public async Task ConsumeSimulatedGrowStall_WhenUnset_ReturnsFalse()
    {
        bool consumed;
        {
            ZeroAllocHelper.SimulateGrowStallForCoverage = false;
            consumed = ZeroAllocHelper.ConsumeSimulatedGrowStall();
        }

        await Assert.That(consumed).IsFalse();
    }
    /// <summary>Verifies ConsumeSimulatedGrowStall WhenSet ReturnsTrueAndClearsFlag.</summary>
    [Test]
    public async Task ConsumeSimulatedGrowStall_WhenSet_ReturnsTrueAndClearsFlag()
    {
        bool first;
        bool second;
        {
            ZeroAllocHelper.SimulateGrowStallForCoverage = true;
            first = ZeroAllocHelper.ConsumeSimulatedGrowStall();
            second = ZeroAllocHelper.ConsumeSimulatedGrowStall();
            ZeroAllocHelper.SimulateGrowStallForCoverage = false;
        }

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsFalse();
    }

    // ========================================================================
    // GROW-STALL SIMULATION (test coverage hook)
    // ========================================================================

    /// <summary>Verifies ConsumeSimulatedGrowStall WhenFlagUnset ReturnsFalse.</summary>
    [Test]
    public async Task ConsumeSimulatedGrowStall_WhenFlagUnset_ReturnsFalse()
    {
        ZeroAllocHelper.SimulateGrowStallForCoverage = false;

        bool result = ZeroAllocHelper.ConsumeSimulatedGrowStall();

        await Assert.That(result).IsFalse();
    }
    /// <summary>Verifies ConsumeSimulatedGrowStall WhenFlagSet ReturnsTrueAndClearsFlag.</summary>
    [Test]
    [NotInParallel("SimulateGrowStallForCoverage")]
    public async Task ConsumeSimulatedGrowStall_WhenFlagSet_ReturnsTrueAndClearsFlag()
    {
        bool first;
        bool second;
        {
            ZeroAllocHelper.SimulateGrowStallForCoverage = true;
            first = ZeroAllocHelper.ConsumeSimulatedGrowStall();
            second = ZeroAllocHelper.ConsumeSimulatedGrowStall();
            ZeroAllocHelper.SimulateGrowStallForCoverage = false;
        }

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsFalse();
    }
}
