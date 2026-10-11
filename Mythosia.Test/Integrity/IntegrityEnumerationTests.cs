using System.Collections;
using Mythosia.Integrity;
using Mythosia.Integrity.Checksum;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class IntegrityEnumerationTests
{
    public enum Algorithm
    {
        ChecksumXor,
        ChecksumNmea,
        ChecksumModulo256,
        ChecksumTwosComplement,
        Crc16Dnp,
        Crc32Classic,
        Crc32Basic
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
        foreach (int length in new[] { 0, 1, 2, 3, 255, 256, 257, 1024, 8192 })
            yield return new object[] { algorithm, length };
    }

    public static IEnumerable<object[]> AlgorithmsAndFailures()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
        foreach (FailureStage stage in Enum.GetValues<FailureStage>())
            yield return new object[] { algorithm, stage };
    }

    public static IEnumerable<object[]> AlgorithmsAndIndexedLengths()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
        foreach (int length in new[] { 1, 16, 4096 })
            yield return new object[] { algorithm, length };
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndLengths))]
    public void Compute_RepeatableEnumerableUsesLinearReadBudgetAndMatchesIndependentOracle(
        Algorithm algorithm, int length)
    {
        byte[] payload = BinaryPayload(length);
        var source = new CountingEnumerable(payload);
        byte[] expected = ReferenceDigest(algorithm, payload);

        Assert.Equal(expected, CreateDetector(algorithm).Compute(source).ToArray());

        AssertLinearReadsAndDisposal(source, length);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndIndexedLengths))]
    public void Compute_IndexedSourceCachesCountAndReadsDirectlyWithoutEnumerating(
        Algorithm algorithm, int length)
    {
        byte[] payload = BinaryPayload(length);
        var source = new ReadOnlyIndexedSource(payload);
        byte[] expected = ReferenceDigest(algorithm, payload);

        Assert.Equal(expected, CreateDetector(algorithm).Compute(source).ToArray());

        Assert.InRange(source.CountCallCount, 1, 2);
        Assert.Equal(length, source.IndexerReadCount);
        Assert.Equal(0, source.EnumeratorCount);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndLengths))]
    public void NumericExtensions_UseLinearReadBudgetAndPreserveLegacyValues(
        Algorithm algorithm, int length)
    {
        byte[] payload = BinaryPayload(length);
        var source = new CountingEnumerable(payload);
        byte[] expected = ReferenceDigest(algorithm, payload);

        if (expected.Length == 0)
            Assert.Throws<ArgumentOutOfRangeException>(() => NumericExtensionBytes(algorithm, source));
        else
            Assert.Equal(expected, NumericExtensionBytes(algorithm, source));

        AssertLinearReadsAndDisposal(source, length);

        if (!IsChecksum(algorithm))
            return;

        var legacySource = new CountingEnumerable(payload);
#pragma warning disable CS0618 // Retain the supported obsolete checksum extensions.
        CheckSum8Type legacyType = (CheckSum8Type)ChecksumType(algorithm);
        if (expected.Length == 0)
            Assert.Throws<ArgumentOutOfRangeException>(() => legacySource.CheckSum8(legacyType));
        else
            Assert.Equal(expected[0], legacySource.CheckSum8(legacyType));
#pragma warning restore CS0618
        AssertLinearReadsAndDisposal(legacySource, length);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Compute_FinishesReadingBeforeReturnAndResultCanBeEnumeratedRepeatedly(Algorithm algorithm)
    {
        byte[] payload = BinaryPayload(257);
        byte[] expected = ReferenceDigest(algorithm, payload);
        var source = new CountingEnumerable(payload);

        IEnumerable<byte> result = CreateDetector(algorithm).Compute(source);
        var countersAtReturn = source.Counters;
        AssertLinearReadsAndDisposal(source, payload.Length);

        // The result already represents the original input when Compute returns.
        Array.Fill(payload, (byte)0xFF);
        Assert.Equal(expected, result.ToArray());
        Assert.Equal(expected, result.ToArray());
        Assert.Equal(countersAtReturn, source.Counters);
    }

    [Theory]
    [MemberData(nameof(AlgorithmsAndFailures))]
    public void Compute_SourceFailurePropagatesImmediatelyAndDisposesEnumerator(
        Algorithm algorithm, FailureStage stage)
    {
        var source = new CountingEnumerable(BinaryPayload(32), stage);

        // Invoke without enumerating the returned digest: source failures remain eager.
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateDetector(algorithm).Compute(source));

        Assert.Same(source.InjectedFailure, exception);
        Assert.Equal(source.EnumeratorCount, source.DisposeCount);
        Assert.InRange(source.EnumeratorCount, 1, 2);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Compute_NullInputPreservesLinqSourceParameterName(Algorithm algorithm)
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => CreateDetector(algorithm).Compute(null!));

        Assert.Equal("source", exception.ParamName);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(99)]
    [InlineData(int.MaxValue)]
    public void InvalidTypes_PreserveEmptyDigestAndExistingInputValidation(int invalidType)
    {
        byte[] payload = BinaryPayload(3);
        var checksumSource = new CountingEnumerable(payload);
        var crc16Source = new CountingEnumerable(payload);
        var crc32Source = new CountingEnumerable(payload);
        var checksum = new Checksum8((Checksum8Type)invalidType);
        var crc16 = new CRC16((CRC16Type)invalidType);
        var crc32 = new CRC32((CRC32Type)invalidType);

        Assert.Empty(checksum.Compute(checksumSource));
        AssertLinearReadsAndDisposal(checksumSource, payload.Length);
        Assert.Empty(crc16.Compute(crc16Source));
        Assert.Empty(crc32.Compute(crc32Source));
        Assert.Equal(0, crc16Source.EnumeratorCount);
        Assert.Equal(0, crc32Source.EnumeratorCount);

        Assert.Throws<ArgumentNullException>(() => checksum.Compute(null!));
        Assert.Empty(crc16.Compute(null!));
        Assert.Empty(crc32.Compute(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => checksum.Encode(payload));
        Assert.Equal(payload, crc16.Encode(payload).ToArray());
        Assert.Equal(payload, crc32.Encode(payload).ToArray());
        Assert.Throws<InvalidOperationException>(() => payload.CRC16((CRC16Type)invalidType));
        Assert.Throws<ArgumentOutOfRangeException>(() => payload.CRC32((CRC32Type)invalidType));
    }

    private static byte[] BinaryPayload(int length)
        => Enumerable.Range(0, length)
            .Select(index => (byte)((index * 73 + (index >> 3) * 29 + 0xA5) & 0xFF))
            .ToArray();

    private static bool IsChecksum(Algorithm algorithm)
        => algorithm is Algorithm.ChecksumXor or Algorithm.ChecksumNmea or
            Algorithm.ChecksumModulo256 or Algorithm.ChecksumTwosComplement;

    private static Checksum8Type ChecksumType(Algorithm algorithm) => algorithm switch
    {
        Algorithm.ChecksumXor => Checksum8Type.Xor,
        Algorithm.ChecksumNmea => Checksum8Type.NMEA,
        Algorithm.ChecksumModulo256 => Checksum8Type.Modulo256,
        Algorithm.ChecksumTwosComplement => Checksum8Type.TwosComplement,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static ErrorDetection CreateDetector(Algorithm algorithm)
    {
        if (IsChecksum(algorithm))
            return new Checksum8(ChecksumType(algorithm));

#pragma warning disable CS0618 // The CRC32 Basic alias is part of the preserved API.
        return algorithm switch
        {
            Algorithm.Crc16Dnp => new CRC16(CRC16Type.DNP),
            Algorithm.Crc32Classic => new CRC32(CRC32Type.Classic),
            Algorithm.Crc32Basic => new CRC32(CRC32Type.Basic),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
#pragma warning restore CS0618
    }

    private static byte[] NumericExtensionBytes(Algorithm algorithm, IEnumerable<byte> source)
    {
        if (IsChecksum(algorithm))
            return new[] { source.Checksum8(ChecksumType(algorithm)) };

#pragma warning disable CS0618
        return algorithm switch
        {
            Algorithm.Crc16Dnp => BitConverter.GetBytes(source.CRC16(CRC16Type.DNP)),
            Algorithm.Crc32Classic => BitConverter.GetBytes(source.CRC32(CRC32Type.Classic)),
            Algorithm.Crc32Basic => BitConverter.GetBytes(source.CRC32(CRC32Type.Basic)),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
#pragma warning restore CS0618
    }

    private static byte[] ReferenceDigest(Algorithm algorithm, byte[] payload)
    {
        if (!IsChecksum(algorithm))
        {
            if (algorithm == Algorithm.Crc16Dnp)
            {
                // Preserve DNP's empty early return and existing byte-swapped value.
                ushort value = payload.Length == 0 ? (ushort)0 :
                    CrcReference.ToLegacyDnpRepresentation(CrcReference.Compute16(payload, CRC16Type.DNP));
                return BitConverter.GetBytes(value);
            }

            return BitConverter.GetBytes(CrcReference.Compute32(payload, CRC32Type.Classic));
        }

        if (payload.Length == 0)
            return Array.Empty<byte>();

        long sum = payload.Sum(value => (long)value);
        byte xor = payload.Aggregate((byte)0, (accumulator, value) => (byte)(accumulator ^ value));
        byte checksum = algorithm switch
        {
            Algorithm.ChecksumXor or Algorithm.ChecksumNmea => xor,
            Algorithm.ChecksumModulo256 => (byte)(sum % 256),
            Algorithm.ChecksumTwosComplement => (byte)((256 - sum % 256) % 256),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
        return new[] { checksum };
    }

    private static void AssertLinearReadsAndDisposal(CountingEnumerable source, int length)
    {
        // The existing empty guards may make a separate Count pass. Accept one
        // or two passes; reject growth with the number of input bytes.
        Assert.InRange(source.EnumeratorCount, 1, 2);
        Assert.InRange(source.MoveNextCount, 0, 2 * (length + 1));
        Assert.InRange(source.CurrentCount, 0, length);
        Assert.Equal(source.EnumeratorCount, source.DisposeCount);
    }

    // A custom indexed source isolates the ICollection/IList path from byte[]
    // optimizations. Expensive Count getters must be cached; an indexed input
    // must retain direct access without allocating an enumerator.
    private sealed class ReadOnlyIndexedSource : IList<byte>
    {
        private readonly byte[] values;

        public ReadOnlyIndexedSource(byte[] values) => this.values = values;

        public int CountCallCount { get; private set; }
        public int IndexerReadCount { get; private set; }
        public int EnumeratorCount { get; private set; }
        public bool IsReadOnly => true;

        public int Count
        {
            get
            {
                CountCallCount++;
                if (CountCallCount > 2)
                    throw new InvalidOperationException("Indexed input Count was not cached.");

                return values.Length;
            }
        }

        public byte this[int index]
        {
            get
            {
                if (index != IndexerReadCount)
                    throw new InvalidOperationException("Indexed input was not read once in sequence.");

                IndexerReadCount++;
                return values[index];
            }
            set => throw new NotSupportedException();
        }

        public bool Contains(byte item) => Array.IndexOf(values, item) >= 0;
        public int IndexOf(byte item) => Array.IndexOf(values, item);
        public void CopyTo(byte[] array, int arrayIndex) => values.CopyTo(array, arrayIndex);
        public void Add(byte item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public void Insert(int index, byte item) => throw new NotSupportedException();
        public bool Remove(byte item) => throw new NotSupportedException();
        public void RemoveAt(int index) => throw new NotSupportedException();

        public IEnumerator<byte> GetEnumerator()
        {
            EnumeratorCount++;
            throw new InvalidOperationException("Indexed input must not allocate an enumerator.");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    // Intentionally implements only IEnumerable: Count and ElementAt cannot use
    // a collection count or indexer. Budgets make quadratic regressions fail
    // deterministically without timing assertions or a long-running benchmark.
    private sealed class CountingEnumerable : IEnumerable<byte>
    {
        private readonly byte[] values;
        private readonly FailureStage? failureStage;

        public CountingEnumerable(byte[] values, FailureStage? failureStage = null)
        {
            this.values = values;
            this.failureStage = failureStage;
        }

        public int EnumeratorCount { get; private set; }
        public int MoveNextCount { get; private set; }
        public int CurrentCount { get; private set; }
        public int DisposeCount { get; private set; }
        public InvalidOperationException InjectedFailure { get; } = new("Injected source read failure.");
        public (int Enumerators, int MoveNext, int Current, int Disposed) Counters
            => (EnumeratorCount, MoveNextCount, CurrentCount, DisposeCount);

        public IEnumerator<byte> GetEnumerator()
        {
            EnumeratorCount++;
            if (EnumeratorCount > 2)
                throw new InvalidOperationException("Input was enumerated more than twice.");

            return new Enumerator(this, EnumeratorCount);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator : IEnumerator<byte>
        {
            private readonly CountingEnumerable owner;
            private readonly int enumerationNumber;
            private int index = -1;
            private bool disposed;

            public Enumerator(CountingEnumerable owner, int enumerationNumber)
            {
                this.owner = owner;
                this.enumerationNumber = enumerationNumber;
            }

            public byte Current
            {
                get
                {
                    owner.CurrentCount++;
                    if (owner.failureStage == FailureStage.CalculationCurrent &&
                        enumerationNumber == 2 && index == 9)
                        throw owner.InjectedFailure;

                    return owner.values[index];
                }
            }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                owner.MoveNextCount++;
                if (owner.MoveNextCount > 2 * (owner.values.Length + 1))
                    throw new InvalidOperationException("Input exceeded a linear read budget.");

                int nextIndex = index + 1;
                bool countFailure = owner.failureStage == FailureStage.CountMoveNext && enumerationNumber == 1;
                bool calculationFailure = owner.failureStage == FailureStage.CalculationMoveNext && enumerationNumber == 2;
                if ((countFailure || calculationFailure) && nextIndex == 9)
                    throw owner.InjectedFailure;

                index = nextIndex;
                return index < owner.values.Length;
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
