using Xunit;

namespace Mythosia.Test
{
    public class NumericTest
    {
        [Fact]
        public void StartTest()
        {
            double dt = 1235.2139414252;
            Assert.True(dt.ToSIPrefix(SIPrefixUnit.Auto, 7) == "1.2352139 k");
            Assert.True(dt.ToSIPrefix(SIPrefixUnit.Kilo, 7) == "1.2352139 k");
            Assert.True(dt.ToSIPrefix(SIPrefixUnit.Mega) == "0 M");
            Assert.True(dt.ToSIPrefix(SIPrefixUnit.Giga, 10) == "0.0000012352 G");
            Assert.True(dt.ToSIPrefix(SIPrefixUnit.Tera, 10) == "0.0000000012 T");

            var dd = 0.3123412355;
            Assert.True(dd.ToSIPrefix(SIPrefixUnit.Mili, 5) == "312.34124 m");
            Assert.True(dd.ToSIPrefix(SIPrefixUnit.Micro, 5) == "312341.2355μ");
            Assert.True(dd.ToSIPrefix(SIPrefixUnit.Nano, 5) == "312341235.5 n");
            Assert.True(dd.ToSIPrefix(SIPrefixUnit.Pico, 5) == "312341235500 p");

            var p = 0.00000000000000000000012314;
            Assert.True(p.ToSIPrefix(SIPrefixUnit.Yocto, 5) == "123.14 y");
        }
    }
}
