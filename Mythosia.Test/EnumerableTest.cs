using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia.Test
{
    public class EnumerableTest
    {
        public void StartTest(IEnumerable<byte> testList)
        {
            Console.WriteLine($"Start test for testlist [{testList.ToDecimalString()}]");
            Console.WriteLine($"[prefixed]");
            Console.WriteLine($"hex string is [{testList.ToPrefixedHexString()}]");
            Console.WriteLine();
            Console.WriteLine($"[prefixed with separated]");
            Console.WriteLine($"hex string is [{testList.ToPrefixedHexString(true)}]");
            Console.WriteLine();
            Console.WriteLine($"[unprefixed]");
            Console.WriteLine($"hex string is [{testList.ToUnPrefixedHexString()}]");
            Console.WriteLine();
            Console.WriteLine($"[unprefixed without connector]");
            Console.WriteLine($"hex string is [{testList.ToUnPrefixedHexString("")}]");
        }
    }
}
