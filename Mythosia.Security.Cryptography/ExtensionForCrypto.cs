using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Mythosia;

namespace Mythosia.Security.Cryptography
{
    public static class ExtensionForCrypto
    {
        public static IEnumerable<byte> EncryptSEED(this IEnumerable<byte> data, 
                                                                                IEnumerable<byte> seedKey, 
                                                                                bool cbcPad = true)
            => SEED.Encrypt(data.ToArray(), seedKey.ToArray(), cbcPad);

        public static IEnumerable<byte> DecryptSEED(this IEnumerable<byte> data, 
                                                                                IEnumerable<byte> seedKey,
                                                                                bool cbcPad = true)
            => SEED.Decrypt(data.ToArray(), seedKey.ToArray(), cbcPad);
    }
}
