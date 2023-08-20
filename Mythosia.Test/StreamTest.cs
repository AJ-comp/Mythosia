using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia.Test
{
    public class StreamTest
    {
        public void StartTest()
        {
            TestA source = new();
            var tt = source.SerializeUsingMarshal();

            TestA target = new()
            {
                data = 0x00,
                dd = 0x0000
            };
            target.DeSerializeUsingMarshal(tt);
        }

    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TestA
    {
        public byte data = 0xff;
        public short dd = 0x0f0f;
    }


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class TestB
    {

    }
}
