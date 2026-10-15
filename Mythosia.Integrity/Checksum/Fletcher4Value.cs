using System;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>The four unsigned 64-bit sums of an OpenZFS Fletcher-4 checksum.</summary>
    public readonly struct Fletcher4Value : IEquatable<Fletcher4Value>
    {
        /// <summary>Creates a value from the four sums, in their algorithm order.</summary>
        public Fletcher4Value(ulong a, ulong b, ulong c, ulong d)
        {
            A = a;
            B = b;
            C = c;
            D = d;
        }

        /// <summary>Gets the first sum.</summary>
        public ulong A { get; }
        /// <summary>Gets the second sum.</summary>
        public ulong B { get; }
        /// <summary>Gets the third sum.</summary>
        public ulong C { get; }
        /// <summary>Gets the fourth sum.</summary>
        public ulong D { get; }

        /// <summary>Compares all four sums.</summary>
        public bool Equals(Fletcher4Value other) => A == other.A && B == other.B && C == other.C && D == other.D;
        /// <summary>Compares this checksum with another value.</summary>
        public override bool Equals(object obj) => obj is Fletcher4Value other && Equals(other);
        /// <summary>Gets a hash code for the four sums.</summary>
        public override int GetHashCode() => HashCode.Combine(A, B, C, D);
        /// <summary>Compares all four sums for equality.</summary>
        public static bool operator ==(Fletcher4Value left, Fletcher4Value right) => left.Equals(right);
        /// <summary>Compares all four sums for inequality.</summary>
        public static bool operator !=(Fletcher4Value left, Fletcher4Value right) => !left.Equals(right);
    }

    /// <summary>The byte order of each 32-bit input word, independent of the current platform.</summary>
    public enum Fletcher4ByteOrder
    {
        /// <summary>Reads the least significant byte of each input word first.</summary>
        LittleEndian = 0,
        /// <summary>Reads the most significant byte of each input word first.</summary>
        BigEndian = 1,
    }
}
