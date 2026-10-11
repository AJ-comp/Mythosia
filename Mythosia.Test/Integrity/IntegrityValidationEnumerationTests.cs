using System.Collections;
using Mythosia.Integrity;
using Mythosia.Integrity.Checksum;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class IntegrityValidationEnumerationTests
{
    public enum Algorithm
    {
        ChecksumXor,
        ChecksumNmea,
        ChecksumModulo256,
        ChecksumTwosComplement,
        Crc8Basic,
        Crc8Classic,
        Crc8Maxim,
        Crc16Basic,
        Crc16Classic,
        Crc16Modbus,
        Crc16Xmodem,
        Crc16Dnp,
        Crc32Basic,
        Crc32Classic
    }

    public enum Damage
    {
        None,
        PayloadBit,
        TrailerBit
    }

    public enum FailureStage
    {
        InitialCountMoveNext,
        PayloadMoveNext,
        PayloadCurrent,
        TrailerMoveNext,
        TrailerCurrent
    }

    public enum Family
    {
        Checksum,
        Crc8,
        Crc16,
        Crc32
    }

    public static IEnumerable<object[]> Algorithms()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
            yield return new object[] { algorithm };
    }

    public static IEnumerable<object[]> AlgorithmsLengthsAndDamage()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
        foreach (int length in new[] { 257, 4096 })
        foreach (Damage damage in Enum.GetValues<Damage>())
            yield return new object[] { algorithm, length, damage };
    }

    public static IEnumerable<object[]> AlgorithmsAndShortLengths()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
        for (int length = 0; length <= Width(algorithm); length++)
            yield return new object[] { algorithm, length };
    }

    public static IEnumerable<object[]> AlgorithmsAndFailures()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
        foreach (FailureStage stage in Enum.GetValues<FailureStage>())
            yield return new object[] { algorithm, stage };
    }

    public static IEnumerable<object[]> SmallAlgorithms()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
            if (Width(algorithm) == 1)
                yield return new object[] { algorithm };
    }

    [Theory]
    [MemberData(nameof(AlgorithmsLengthsAndDamage))]
    public void IsError_RepeatableEnumerableAvoidsAnExtraFrameCountAndMatchesIndependentOracle(
        Algorithm algorithm, int payloadLength, Damage damage)
    {
        byte[] payload = BinaryPayload(payloadLength);
        byte[] frame = payload.Concat(ReferenceDigest(algorithm, payload)).ToArray();
        if (damage == Damage.PayloadBit)
            frame[0] ^= 0x80;
        if (damage == Damage.TrailerBit)
            frame[^1] ^= 0x01;
        byte[] snapshot = (byte[])frame.Clone();
        var source = new ObservedEnumerable(frame);

        Assert.Equal(damage != Damage.None, CreateDetector(algorithm).IsError(source));

        // One whole-frame Count, one trailer traversal, and the pre-existing
        // payload guards/calculation. These are upper bounds, so a future
        // implementation may use fewer reads. No timing or LINQ type names
        // are required to detect an additional full Count pass.
        int payloadPasses = PayloadPasses(algorithm);
        Assert.InRange(source.EnumeratorCount, 1, 2 + payloadPasses);
        Assert.InRange(source.MoveNextCount, 0,
            2 * (frame.Length + 1) + payloadPasses * (payloadLength + 1));
        AssertDisposed(source);
        Assert.Equal(snapshot, frame);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndShortLengths))]
    public void IsError_ShortFramesStopAfterTheInitialCount(Algorithm algorithm, int length)
    {
        var source = new ObservedEnumerable(BinaryPayload(length));

        Assert.True(CreateDetector(algorithm).IsError(source));

        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(length + 1, source.MoveNextCount);
        Assert.Equal(0, source.CurrentCount);
        AssertDisposed(source);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void IsErrorAndDecode_NullInputPreserveSourceParameterName(Algorithm algorithm)
    {
        ErrorDetection detector = CreateDetector(algorithm);

        var validationFailure = Assert.Throws<ArgumentNullException>(() => detector.IsError(null!));
        var decodingFailure = Assert.Throws<ArgumentNullException>(() => detector.Decode(null!));

        Assert.Equal("source", validationFailure.ParamName);
        Assert.Equal("source", decodingFailure.ParamName);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndFailures))]
    public void IsError_SourceFailuresRemainEagerAndDisposeEveryOpenedEnumerator(
        Algorithm algorithm, FailureStage stage)
    {
        byte[] payload = BinaryPayload(32);
        byte[] frame = payload.Concat(ReferenceDigest(algorithm, payload)).ToArray();
        var source = new ObservedEnumerable(frame);
        int enumeration = stage switch
        {
            FailureStage.InitialCountMoveNext => 1,
            FailureStage.PayloadMoveNext or FailureStage.PayloadCurrent => PayloadEnumeration(algorithm),
            FailureStage.TrailerMoveNext or FailureStage.TrailerCurrent => TrailerEnumeration(algorithm),
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        };
        source.FailOn(enumeration, index: 7,
            current: stage is FailureStage.PayloadCurrent or FailureStage.TrailerCurrent);

        var failure = Assert.Throws<InvalidOperationException>(() => CreateDetector(algorithm).IsError(source));

        Assert.Same(source.InjectedFailure, failure);
        AssertDisposed(source);
    }

    [Theory]
    [InlineData(Family.Checksum, -1)]
    [InlineData(Family.Checksum, int.MaxValue)]
    [InlineData(Family.Crc8, -1)]
    [InlineData(Family.Crc8, int.MaxValue)]
    [InlineData(Family.Crc16, -1)]
    [InlineData(Family.Crc16, int.MaxValue)]
    [InlineData(Family.Crc32, -1)]
    [InlineData(Family.Crc32, int.MaxValue)]
    public void IsError_InvalidAlgorithmTypesPreserveShortFrameAndValidationBehavior(Family family, int value)
    {
        ErrorDetection detector = family switch
        {
            Family.Checksum => new Checksum8((Checksum8Type)value),
            Family.Crc8 => new CRC8((CRC8Type)value),
            Family.Crc16 => new CRC16((CRC16Type)value),
            Family.Crc32 => new CRC32((CRC32Type)value),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
        int width = family switch { Family.Crc16 => 2, Family.Crc32 => 4, _ => 1 };

        Assert.True(detector.IsError(new byte[width]));
        var nullFailure = Assert.Throws<ArgumentNullException>(() => detector.IsError(null!));
        Assert.Equal("source", nullFailure.ParamName);
        var source = new ObservedEnumerable(BinaryPayload(17));
        if (width == 1)
            Assert.Throws<ArgumentOutOfRangeException>(() => detector.IsError(source));
        else
            Assert.True(detector.IsError(source));
        AssertDisposed(source);
    }

    [Theory]
    [InlineData(Family.Crc16)]
    [InlineData(Family.Crc32)]
    public void IsError_VirtualComputeReceivesOnlyPayloadOnceBeforeTrailerIsEnumerated(Family family)
    {
        byte[] payload = BinaryPayload(17);
        byte[] digest = family == Family.Crc16
            ? new byte[] { 0xD3, 0x5A }
            : new byte[] { 0xD3, 0x5A, 0x0E, 0xA7 };
        byte[] frame = payload.Concat(new byte[digest.Length]).ToArray();
        var source = new ObservedEnumerable(frame);
        var probe = new ComputeProbe(digest, () => digest.CopyTo(frame, payload.Length));
        ErrorDetection detector = CreateProbedDetector(family, probe);

        // Compute deliberately repairs the trailer. Acceptance proves TakeLast
        // still observes the input after the virtual call, without a snapshot.
        Assert.False(detector.IsError(source));

        Assert.Equal(1, probe.CallCount);
        Assert.Equal(payload, probe.Payload);
        Assert.InRange(source.EnumeratorCount, 1, 3);
        AssertDisposed(source);
    }

    [Theory]
    [MemberData(nameof(SmallAlgorithms))]
    public void IsError_ChecksumAndCrc8ContinueToUseTheirConcreteNumericExtensions(Algorithm algorithm)
    {
        byte[] payload = BinaryPayload(17);
        byte[] frame = payload.Concat(ReferenceDigest(algorithm, payload)).ToArray();
        var probe = new ComputeProbe(Array.Empty<byte>(), () => throw new InvalidOperationException(
            "IsError must keep its existing numeric-extension dispatch."));
        ErrorDetection detector = IsChecksum(algorithm)
            ? new ProbedChecksum(ChecksumType(algorithm), probe)
            : new ProbedCrc8(Crc8Type(algorithm), probe);

        Assert.False(detector.IsError(frame));
        frame[^1] ^= 0x01;
        Assert.True(detector.IsError(frame));
        Assert.Equal(0, probe.CallCount);
    }

    [Theory]
    [MemberData(nameof(SmallAlgorithms))]
    public void IsError_ChecksumAndCrc8ReadTrailerBeforeCalculatingPayload(Algorithm algorithm)
    {
        byte[] payload = BinaryPayload(17);
        byte[] frame = payload.Concat(ReferenceDigest(algorithm, payload)).ToArray();
        var source = new ObservedEnumerable(frame)
        {
            OnCurrentRead = index =>
            {
                // Count never reads Current, and Take(payloadLength) cannot
                // reach this index. Mutate only after Last captures the trailer.
                if (index == frame.Length - 1)
                    frame[0] ^= 0x80;
            }
        };

        Assert.True(CreateDetector(algorithm).IsError(source));

        AssertDisposed(source);
    }

    [Theory]
    [InlineData(Family.Crc16)]
    [InlineData(Family.Crc32)]
    public void IsError_DeferredVirtualDigestIsReadAfterTrailerTraversal(Family family)
    {
        byte[] payload = BinaryPayload(17);
        byte[] digest = family == Family.Crc16
            ? new byte[] { 0xD3, 0x5A }
            : new byte[] { 0xD3, 0x5A, 0x0E, 0xA7 };
        byte[] frame = payload.Concat(digest).ToArray();
        bool computeReturned = false;
        bool trailerTraversed = false;
        var source = new ObservedEnumerable(frame)
        {
            OnDispose = _ => trailerTraversed |= computeReturned
        };
        var probe = new ComputeProbe(DeferredDigest(), () => computeReturned = true);

        Assert.False(CreateProbedDetector(family, probe).IsError(source));

        Assert.True(trailerTraversed);
        Assert.Equal(1, probe.CallCount);
        Assert.Equal(payload, probe.Payload);
        AssertDisposed(source);

        IEnumerable<byte> DeferredDigest()
        {
            Assert.True(trailerTraversed,
                "The trailer source must finish before the virtual digest is read.");
            foreach (byte value in digest)
                yield return value;
        }
    }

    [Theory]
    [InlineData(Family.Checksum)]
    [InlineData(Family.Crc8)]
    [InlineData(Family.Crc16)]
    [InlineData(Family.Crc32)]
    public void IsError_ShortFramesNeverInvokeVirtualCompute(Family family)
    {
        int width = family switch { Family.Crc16 => 2, Family.Crc32 => 4, _ => 1 };
        var probe = new ComputeProbe(Array.Empty<byte>(), () => throw new InvalidOperationException(
            "A short frame must be rejected before calculation."));
        ErrorDetection detector = CreateProbedDetector(family, probe);

        for (int length = 0; length <= width; length++)
            Assert.True(detector.IsError(new byte[length]));

        Assert.Equal(0, probe.CallCount);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Encode_PreservesEagerDigestAndExistingPayloadMaterialization(Algorithm algorithm)
    {
        byte[] payload = BinaryPayload(17);
        byte[] originalPayload = (byte[])payload.Clone();
        byte[] digest = ReferenceDigest(algorithm, originalPayload);
        var source = new ObservedEnumerable(payload);

        IEnumerable<byte> encoded = CreateDetector(algorithm).Encode(source);
        int enumeratorsAtReturn = source.EnumeratorCount;
        Assert.True(enumeratorsAtReturn > 0);
        AssertDisposed(source);
        payload[0] ^= 0x80;

        byte[] expectedPayload = Width(algorithm) == 1 ? payload : originalPayload;
        byte[] expectedFrame = expectedPayload.Concat(digest).ToArray();
        Assert.Equal(expectedFrame, encoded.ToArray());
        Assert.Equal(expectedFrame, encoded.ToArray());
        if (Width(algorithm) == 1)
            Assert.True(source.EnumeratorCount > enumeratorsAtReturn);
        else
            Assert.Equal(enumeratorsAtReturn, source.EnumeratorCount);
        AssertDisposed(source);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Decode_ValidatesImmediatelyButReturnedPayloadRemainsDeferred(Algorithm algorithm)
    {
        byte[] payload = BinaryPayload(17);
        byte[] frame = payload.Concat(ReferenceDigest(algorithm, payload)).ToArray();
        var source = new ObservedEnumerable(frame);

        IEnumerable<byte> decoded = CreateDetector(algorithm).Decode(source);
        int enumeratorsAtReturn = source.EnumeratorCount;
        Assert.True(enumeratorsAtReturn > 0);
        AssertDisposed(source);
        frame[0] ^= 0x80;
        payload[0] ^= 0x80;

        Assert.Equal(payload, decoded.ToArray());
        Assert.Equal(payload, decoded.ToArray());
        Assert.True(source.EnumeratorCount > enumeratorsAtReturn);
        AssertDisposed(source);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Decode_DeferredPayloadSourceFailurePropagatesAndDisposesEnumerator(Algorithm algorithm)
    {
        byte[] payload = BinaryPayload(17);
        byte[] frame = payload.Concat(ReferenceDigest(algorithm, payload)).ToArray();
        var source = new ObservedEnumerable(frame);
        IEnumerable<byte> decoded = CreateDetector(algorithm).Decode(source);
        source.FailOn(source.EnumeratorCount + 1, index: 7, current: false);

        var failure = Assert.Throws<InvalidOperationException>(() => decoded.ToArray());

        Assert.Same(source.InjectedFailure, failure);
        AssertDisposed(source);
    }

    private static bool IsChecksum(Algorithm algorithm)
        => algorithm <= Algorithm.ChecksumTwosComplement;

    private static int Width(Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc16Basic or Algorithm.Crc16Classic or Algorithm.Crc16Modbus or
            Algorithm.Crc16Xmodem or Algorithm.Crc16Dnp => 2,
        Algorithm.Crc32Basic or Algorithm.Crc32Classic => 4,
        _ => 1
    };

    private static int PayloadPasses(Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc8Basic or Algorithm.Crc8Classic or Algorithm.Crc8Maxim => 3,
        Algorithm.Crc16Modbus => 1,
        _ => 2
    };

    private static int PayloadEnumeration(Algorithm algorithm)
        => Width(algorithm) == 1 ? 2 + PayloadPasses(algorithm) : 1 + PayloadPasses(algorithm);

    private static int TrailerEnumeration(Algorithm algorithm)
        => Width(algorithm) == 1 ? 2 : 2 + PayloadPasses(algorithm);

    private static Checksum8Type ChecksumType(Algorithm algorithm) => algorithm switch
    {
        Algorithm.ChecksumXor => Checksum8Type.Xor,
        Algorithm.ChecksumNmea => Checksum8Type.NMEA,
        Algorithm.ChecksumModulo256 => Checksum8Type.Modulo256,
        Algorithm.ChecksumTwosComplement => Checksum8Type.TwosComplement,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

#pragma warning disable CS0618 // Basic aliases remain part of the compatibility contract.
    private static CRC8Type Crc8Type(Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc8Basic => CRC8Type.Basic,
        Algorithm.Crc8Classic => CRC8Type.Classic,
        Algorithm.Crc8Maxim => CRC8Type.Maxim,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static CRC16Type Crc16Type(Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc16Basic => CRC16Type.Basic,
        Algorithm.Crc16Classic => CRC16Type.Classic,
        Algorithm.Crc16Modbus => CRC16Type.Modbus,
        Algorithm.Crc16Xmodem => CRC16Type.CCITTxModem,
        Algorithm.Crc16Dnp => CRC16Type.DNP,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static CRC32Type Crc32Type(Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc32Basic => CRC32Type.Basic,
        Algorithm.Crc32Classic => CRC32Type.Classic,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };
#pragma warning restore CS0618

    private static ErrorDetection CreateDetector(Algorithm algorithm)
    {
        if (IsChecksum(algorithm))
            return new Checksum8(ChecksumType(algorithm));
        return Width(algorithm) switch
        {
            1 => new CRC8(Crc8Type(algorithm)),
            2 => new CRC16(Crc16Type(algorithm)),
            4 => new CRC32(Crc32Type(algorithm)),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
    }

    private static byte[] ReferenceDigest(Algorithm algorithm, byte[] payload)
    {
        if (IsChecksum(algorithm))
        {
            int sum = payload.Sum(value => (int)value) & 0xFF;
            byte xor = payload.Aggregate((byte)0, (accumulator, value) => (byte)(accumulator ^ value));
            byte checksum = algorithm switch
            {
                Algorithm.ChecksumXor or Algorithm.ChecksumNmea => xor,
                Algorithm.ChecksumModulo256 => (byte)sum,
                Algorithm.ChecksumTwosComplement => (byte)((256 - sum) & 0xFF),
                _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
            };
            return new[] { checksum };
        }
        if (Width(algorithm) == 1)
            return new[] { CrcReference.Compute8(payload, Crc8Type(algorithm)) };
        if (Width(algorithm) == 4)
            return NativeBytes(CrcReference.Compute32(payload, Crc32Type(algorithm)), 4);

        ushort value = CrcReference.Compute16(payload, Crc16Type(algorithm));
        if (algorithm == Algorithm.Crc16Dnp)
            value = CrcReference.ToLegacyDnpRepresentation(value);
        return NativeBytes(value, 2);
    }

    private static byte[] NativeBytes(uint value, int width)
    {
        var bytes = new byte[width];
        for (int index = 0; index < width; index++)
        {
            int shift = (BitConverter.IsLittleEndian ? index : width - index - 1) * 8;
            bytes[index] = (byte)(value >> shift);
        }
        return bytes;
    }

    private static byte[] BinaryPayload(int length)
        => Enumerable.Range(0, length)
            .Select(index => (byte)((index * 73 + (index >> 3) * 29 + 0xA5) & 0xFF))
            .ToArray();

    private static void AssertDisposed(ObservedEnumerable source)
        => Assert.Equal(source.EnumeratorCount, source.DisposeCount);

    private static ErrorDetection CreateProbedDetector(Family family, ComputeProbe probe) => family switch
    {
        Family.Checksum => new ProbedChecksum(Checksum8Type.Xor, probe),
        Family.Crc8 => new ProbedCrc8(CRC8Type.Classic, probe),
        Family.Crc16 => new ProbedCrc16(probe),
        Family.Crc32 => new ProbedCrc32(probe),
        _ => throw new ArgumentOutOfRangeException(nameof(family))
    };

    private sealed class ComputeProbe(IEnumerable<byte> digest, Action onCompute)
    {
        public int CallCount { get; private set; }
        public byte[]? Payload { get; private set; }

        public IEnumerable<byte> Compute(IEnumerable<byte> source)
        {
            CallCount++;
            Payload = source.ToArray();
            onCompute();
            return digest;
        }
    }

    private sealed class ProbedChecksum(Checksum8Type type, ComputeProbe probe) : Checksum8(type)
    {
        public override IEnumerable<byte> Compute(IEnumerable<byte> source) => probe.Compute(source);
    }

    private sealed class ProbedCrc8(CRC8Type type, ComputeProbe probe) : CRC8(type)
    {
        public override IEnumerable<byte> Compute(IEnumerable<byte> source) => probe.Compute(source);
    }

    private sealed class ProbedCrc16(ComputeProbe probe) : CRC16
    {
        public override IEnumerable<byte> Compute(IEnumerable<byte> source) => probe.Compute(source);
    }

    private sealed class ProbedCrc32(ComputeProbe probe) : CRC32
    {
        public override IEnumerable<byte> Compute(IEnumerable<byte> source) => probe.Compute(source);
    }

    // Implements only IEnumerable so Count must traverse the input. The source
    // is repeatable, each enumerator is independent, and no one-shot contract
    // or snapshot semantics are introduced by these tests.
    private sealed class ObservedEnumerable(byte[] values) : IEnumerable<byte>
    {
        private int failureEnumeration;
        private int failureIndex;
        private bool failInCurrent;
        public int EnumeratorCount { get; private set; }
        public int MoveNextCount { get; private set; }
        public int CurrentCount { get; private set; }
        public int DisposeCount { get; private set; }
        public Action<int>? OnDispose { get; init; }
        public Action<int>? OnCurrentRead { get; init; }
        public InvalidOperationException InjectedFailure { get; } = new("Injected frame source failure.");

        public void FailOn(int enumeration, int index, bool current)
        {
            failureEnumeration = enumeration;
            failureIndex = index;
            failInCurrent = current;
        }

        public IEnumerator<byte> GetEnumerator() => new Enumerator(this, ++EnumeratorCount);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator(ObservedEnumerable owner, int number) : IEnumerator<byte>
        {
            private int index = -1;
            private bool disposed;

            public byte Current
            {
                get
                {
                    owner.CurrentCount++;
                    if (owner.failInCurrent && number == owner.failureEnumeration && index == owner.failureIndex)
                        throw owner.InjectedFailure;
                    byte value = owner.Values[index];
                    owner.OnCurrentRead?.Invoke(index);
                    return value;
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                owner.MoveNextCount++;
                index++;
                if (!owner.failInCurrent && number == owner.failureEnumeration && index == owner.failureIndex)
                    throw owner.InjectedFailure;
                return index < owner.Values.Length;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                owner.DisposeCount++;
                owner.OnDispose?.Invoke(number);
            }
        }

        private byte[] Values => values;
    }
}
