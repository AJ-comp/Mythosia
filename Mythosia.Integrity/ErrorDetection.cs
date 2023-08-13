using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Mythosia.Integrity
{
    public abstract class ErrorDetection
    {
        public abstract IEnumerable<byte> Compute(IEnumerable<byte> source);
        public abstract IEnumerable<byte> Encode(IEnumerable<byte> source);
        public abstract IEnumerable<byte> Decode(IEnumerable<byte> sourceWithCRC);
        public abstract bool IsError(IEnumerable<byte> sourceWithCRC);

        public abstract string GetDetectionType();
    }
}
