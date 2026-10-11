using System.Collections;
using Mythosia.Integrity;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class CrcArrayPathTests
{
    public enum Algorithm
    {
        Crc8Basic,
        Crc8Classic,
        Crc8Maxim,
        Crc16Basic,
        Crc16Classic,
        Crc16Modbus,
        Crc16Xmodem
    }

    public enum FailureStage
    {
        CountMoveNext,
        CalculationMoveNext,
        CalculationCurrent
    }

    public static IEnumerable<object[]> Algorithms()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
            yield return new object[] { algorithm };
    }

    public static IEnumerable<object[]> AlgorithmsAndLengths()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
        foreach (int length in new[] { 0, 1, 257, 65537 })
            yield return new object[] { algorithm, length };
    }

    public static IEnumerable<object[]> AlgorithmsAndFailures()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
        foreach (FailureStage stage in Enum.GetValues<FailureStage>())
            if (algorithm != Algorithm.Crc16Modbus || stage != FailureStage.CountMoveNext)
                yield return new object[] { algorithm, stage };
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void ArrayAndArrayThroughInterface_PreserveDigestNumericExtensionAndEncodedBytes(Algorithm algorithm)
    {
        // Exhaustive byte-pair and large-pattern accuracy is covered by
        // CrcAlgorithmTests. These cases focus on the runtime array branch and
        // preserve its public API, empty behavior, and native packing.
        foreach (int length in new[] { 0, 1, 31, 32, 33, 255, 256, 257, 4096, 65537 })
        {
            byte[] payload = BinaryPayload(length);
            byte[] snapshot = (byte[])payload.Clone();
            IEnumerable<byte> throughInterface = payload;
            byte[] expected = ExpectedDigest(algorithm, payload);
            ErrorDetection detector = CreateDetector(algorithm);

            Assert.Equal(expected, detector.Compute(payload).ToArray());
            Assert.Equal(expected, detector.Compute(throughInterface).ToArray());
            if (expected.Length == 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => NumericDigest(algorithm, payload));
                Assert.Throws<ArgumentOutOfRangeException>(() => NumericDigest(algorithm, throughInterface));
                Assert.Throws<ArgumentOutOfRangeException>(() => detector.Encode(payload));
                Assert.Throws<ArgumentOutOfRangeException>(() => EncodeExtension(algorithm, throughInterface));
            }
            else
            {
                byte[] frame = payload.Concat(expected).ToArray();
                Assert.Equal(expected, NumericDigest(algorithm, payload));
                Assert.Equal(expected, NumericDigest(algorithm, throughInterface));
                Assert.Equal(frame, detector.Encode(payload).ToArray());
                Assert.Equal(frame, EncodeExtension(algorithm, throughInterface).ToArray());
                Assert.Equal(length == 0, detector.IsError(frame));
            }
            Assert.Equal(snapshot, payload);
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void ArrayDigest_IsCalculatedImmediatelyAndCanBeEnumeratedAgainWithoutReadingInput(Algorithm algorithm)
    {
        byte[] payload = BinaryPayload(257);
        byte[] expected = ExpectedDigest(algorithm, payload);

        IEnumerable<byte> digest = CreateDetector(algorithm).Compute(payload);
        Array.Fill(payload, (byte)0xFF);

        Assert.Equal(expected, digest.ToArray());
        Assert.Equal(expected, digest.ToArray());
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndLengths))]
    public void NonArrayIList_PreservesCountGuardsAndForeachInsteadOfUsingIndexer(Algorithm algorithm, int length)
    {
        byte[] payload = BinaryPayload(length);
        var source = new EnumerationOnlyList(payload);

        Assert.Equal(ExpectedDigest(algorithm, payload), CreateDetector(algorithm).Compute(source).ToArray());

        Assert.Equal(CollectionCountReads(algorithm, length), source.CountReads);
        Assert.Equal(0, source.IndexerReads);
        Assert.Equal(0, source.CopyToCalls);
        Assert.Equal(length > 0 || algorithm == Algorithm.Crc16Modbus ? 1 : 0,
            source.Reads.EnumeratorCount);
        Assert.Equal(length, source.Reads.CurrentCount);
        Assert.Equal(source.Reads.EnumeratorCount, source.Reads.DisposeCount);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndLengths))]
    public void NonArrayRepeatableEnumerable_PreservesExistingGuardPassesAndDisposal(Algorithm algorithm, int length)
    {
        byte[] payload = BinaryPayload(length);
        var source = new ObservedEnumerable(payload);

        Assert.Equal(ExpectedDigest(algorithm, payload), CreateDetector(algorithm).Compute(source).ToArray());

        int passes = length == 0 ? 1 : CalculationEnumeration(algorithm);
        Assert.Equal(passes, source.EnumeratorCount);
        Assert.Equal(passes * (length + 1), source.MoveNextCount);
        Assert.Equal(length, source.CurrentCount);
        Assert.Equal(source.EnumeratorCount, source.DisposeCount);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndFailures))]
    public void NonArrayEnumerable_SourceFailuresRemainEagerAndDisposeOpenedEnumerators(
        Algorithm algorithm, FailureStage stage)
    {
        var source = new ObservedEnumerable(BinaryPayload(32));
        source.FailOn(stage == FailureStage.CountMoveNext ? 1 : CalculationEnumeration(algorithm),
            stage == FailureStage.CalculationCurrent);

        // Source access stays eager even if the returned digest is never read.
        var failure = Assert.Throws<InvalidOperationException>(() => CreateDetector(algorithm).Compute(source));

        Assert.Same(source.InjectedFailure, failure);
        Assert.Equal(source.EnumeratorCount, source.DisposeCount);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void NonArrayIList_SourceFailuresUseEnumeratorAndPreserveDisposal(Algorithm algorithm)
    {
        foreach (bool currentFailure in new[] { false, true })
        {
            var source = new EnumerationOnlyList(BinaryPayload(32));
            source.Reads.FailOn(1, currentFailure);

            var failure = Assert.Throws<InvalidOperationException>(() => CreateDetector(algorithm).Compute(source));

            Assert.Same(source.Reads.InjectedFailure, failure);
            Assert.Equal(CollectionCountReads(algorithm, 32), source.CountReads);
            Assert.Equal(0, source.IndexerReads);
            Assert.Equal(0, source.CopyToCalls);
            Assert.Equal(1, source.Reads.DisposeCount);
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void NullAndEmptyInput_PreserveLegacyGuardAndModbusInitialValue(Algorithm algorithm)
    {
        ErrorDetection detector = CreateDetector(algorithm);
        if (algorithm == Algorithm.Crc16Modbus)
            Assert.Throws<NullReferenceException>(() => detector.Compute(null!));
        else
        {
            var failure = Assert.Throws<ArgumentNullException>(() => detector.Compute(null!));
            Assert.Equal("source", failure.ParamName);
        }

        byte[] emptyDigest = detector.Compute(Array.Empty<byte>()).ToArray();
        if (IsCrc8(algorithm))
            Assert.Empty(emptyDigest);
        else
            Assert.Equal(NativeBytes(algorithm == Algorithm.Crc16Modbus ? (ushort)0xFFFF : (ushort)0), emptyDigest);
    }

    [Theory]
    [InlineData(Algorithm.Crc8Basic, Algorithm.Crc8Classic)]
    [InlineData(Algorithm.Crc16Basic, Algorithm.Crc16Classic)]
    public void BasicAliases_PreserveValuesAndDetectionNames(Algorithm basic, Algorithm classic)
    {
        byte[] payload = BinaryPayload(257);

        Assert.Equal(CreateDetector(classic).Compute(payload), CreateDetector(basic).Compute(payload));
        Assert.Equal(CreateDetector(classic).Encode(payload), EncodeExtension(basic, payload));
        Assert.Equal("Basic", CreateDetector(basic).GetDetectionType());
        Assert.Equal("Classic", CreateDetector(classic).GetDetectionType());
    }

    private static bool IsCrc8(Algorithm algorithm)
        => algorithm is Algorithm.Crc8Basic or Algorithm.Crc8Classic or Algorithm.Crc8Maxim;

    private static int CalculationEnumeration(Algorithm algorithm)
        => algorithm == Algorithm.Crc16Modbus ? 1 : IsCrc8(algorithm) ? 3 : 2;

    private static int CollectionCountReads(Algorithm algorithm, int length)
        => algorithm == Algorithm.Crc16Modbus ? 0 : IsCrc8(algorithm) && length > 0 ? 2 : 1;

#pragma warning disable CS0618 // Obsolete aliases remain supported public options.
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
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };
#pragma warning restore CS0618

    private static ErrorDetection CreateDetector(Algorithm algorithm)
        => IsCrc8(algorithm) ? new CRC8(Crc8Type(algorithm)) : new CRC16(Crc16Type(algorithm));

    private static byte[] ExpectedDigest(Algorithm algorithm, byte[] payload)
    {
        if (IsCrc8(algorithm))
            return payload.Length == 0 ? Array.Empty<byte>() : new[] { CrcReference.Compute8(payload, Crc8Type(algorithm)) };
        return NativeBytes(CrcReference.Compute16(payload, Crc16Type(algorithm)));
    }

    private static byte[] NumericDigest(Algorithm algorithm, IEnumerable<byte> payload)
        => IsCrc8(algorithm) ? new[] { payload.CRC8(Crc8Type(algorithm)) } : NativeBytes(payload.CRC16(Crc16Type(algorithm)));

    private static IEnumerable<byte> EncodeExtension(Algorithm algorithm, IEnumerable<byte> payload)
        => IsCrc8(algorithm) ? payload.WithCRC8(Crc8Type(algorithm)) : payload.WithCRC16(Crc16Type(algorithm));

    private static byte[] NativeBytes(ushort value)
        => BitConverter.IsLittleEndian
            ? new[] { (byte)value, (byte)(value >> 8) }
            : new[] { (byte)(value >> 8), (byte)value };

    private static byte[] BinaryPayload(int length)
        => Enumerable.Range(0, length)
            .Select(index => (byte)((index * 73 + (index >> 3) * 29 + 0xA5) & 0xFF))
            .ToArray();

    // This is deliberately an IList but not an array. Its enumeration is the
    // supported read path; rejecting indexer/CopyTo use catches an accidental
    // expansion of the new byte[] optimization to other collections.
    private sealed class EnumerationOnlyList(byte[] values) : IList<byte>
    {
        public ObservedEnumerable Reads { get; } = new(values);
        public int CountReads { get; private set; }
        public int IndexerReads { get; private set; }
        public int CopyToCalls { get; private set; }
        public bool IsReadOnly => true;
        public int Count
        {
            get
            {
                CountReads++;
                return values.Length;
            }
        }

        public byte this[int index]
        {
            get
            {
                IndexerReads++;
                throw new InvalidOperationException("The non-array IList must retain foreach traversal.");
            }
            set => throw new NotSupportedException();
        }

        public void CopyTo(byte[] array, int arrayIndex)
        {
            CopyToCalls++;
            throw new InvalidOperationException("The non-array IList must not be materialized.");
        }

        public IEnumerator<byte> GetEnumerator() => Reads.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public bool Contains(byte item) => Array.IndexOf(values, item) >= 0;
        public int IndexOf(byte item) => Array.IndexOf(values, item);
        public void Add(byte item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public void Insert(int index, byte item) => throw new NotSupportedException();
        public bool Remove(byte item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();
    }

    private sealed class ObservedEnumerable(byte[] values) : IEnumerable<byte>
    {
        private int failureEnumeration;
        private bool failInCurrent;
        public int EnumeratorCount { get; private set; }
        public int MoveNextCount { get; private set; }
        public int CurrentCount { get; private set; }
        public int DisposeCount { get; private set; }
        public InvalidOperationException InjectedFailure { get; } = new("Injected CRC source failure.");
        private byte[] Values => values;

        public void FailOn(int enumeration, bool current)
        {
            failureEnumeration = enumeration;
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
                    if (owner.failInCurrent && number == owner.failureEnumeration && index == 7)
                        throw owner.InjectedFailure;
                    return owner.Values[index];
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                owner.MoveNextCount++;
                index++;
                if (!owner.failInCurrent && number == owner.failureEnumeration && index == 7)
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
            }
        }
    }
}
