using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Crc16VariantTests
{
    public static IEnumerable<object[]> PublishedVectors()
    {
        // AUTOSAR Classic Platform R24-11, CRC Routines, SWS_Crc_00078:
        // complete hexadecimal input and numerical CRC-16/CCITT-FALSE output.
        // https://www.autosar.org/fileadmin/standards/R24-11/CP/AUTOSAR_CP_SWS_CRCLibrary.pdf
        yield return new object[] { CRC16Type.CCITTFalse, "00000000", (ushort)0x84C0 };
        yield return new object[] { CRC16Type.CCITTFalse, "F20183", (ushort)0xD374 };
        yield return new object[] { CRC16Type.CCITTFalse, "0FAA0055", (ushort)0x2023 };
        yield return new object[] { CRC16Type.CCITTFalse, "00FF5511", (ushort)0xB8F9 };
        yield return new object[] { CRC16Type.CCITTFalse, "332255AABBCCDDEEFF", (ushort)0xF53F };
        yield return new object[] { CRC16Type.CCITTFalse, "926B55", (ushort)0x0745 };
        yield return new object[] { CRC16Type.CCITTFalse, "FFFFFFFF", (ushort)0x1D0F };

        // ITU-T X.25 (10/1996), Appendix I, printed page 145. Address/control
        // octets exclude HDLC flags and inserted zero bits. The printed FCS
        // octets are least-significant first; these are the numerical answers.
        // https://www.itu.int/rec/dologin_pub.asp?id=T-REC-X.25-199610-I!!PDF-E&lang=e&type=items
        yield return new object[] { CRC16Type.X25, "033F", (ushort)0xEC5B };
        yield return new object[] { CRC16Type.X25, "0173", (ushort)0x5783 };
        yield return new object[] { CRC16Type.X25, "013F", (ushort)0xDFEB };
        yield return new object[] { CRC16Type.X25, "0373", (ushort)0x6433 };

        // ITSO TS 1000-2:2025-03, Annex A.1.1, printed page 57: CRC_B examples.
        // Annex A.1.2 provides the ISO/IEC 14443-3 reference calculation:
        // initial FFFF, reflected 1021 polynomial, final complement, LSB-first FCS.
        // These are the same CRC parameters as CRC-16/X-25.
        // https://www.itso.org.uk/download/42151.2/part-2-customer-media-data-and-customer-media-architecture/
        yield return new object[] { CRC16Type.X25, "000000", (ushort)0xC6CC };
        yield return new object[] { CRC16Type.X25, "0FAAFF", (ushort)0xD1FC };
        yield return new object[] { CRC16Type.X25, "0A123456", (ushort)0xF62C };

        // CRC RevEng catalogue check values for the ASCII octets "123456789".
        // https://reveng.sourceforge.io/crc-catalogue/16.htm#crc.cat.crc-16-ibm-3740
        // https://reveng.sourceforge.io/crc-catalogue/16.htm#crc.cat.crc-16-ibm-sdlc
        yield return new object[] { CRC16Type.CCITTFalse, "313233343536373839", (ushort)0x29B1 };
        yield return new object[] { CRC16Type.X25, "313233343536373839", (ushort)0x906E };

        // Additional fixed answers from pinned upstream tests. Expected values are
        // published literals or CRC fields from published packets, never generated here.
        // Hex fixes exact bytes, including UTF-8 and repeated spaces; no terminal NUL.
        // EMV input ends with ASCII 6304 and excludes only the four ASCII CRC digits.
        // EMV parameter and input-range definitions:
        // https://github.com/mvallim/emv-qrcode/blob/34781d63a7d7180ff3d70e5fe482e88f6f090101/src/main/java/com/emv/qrcode/core/CRC.java
        // https://github.com/dongri/emv-qrcode/blob/ba6df536e538b4aee8f9c962bbfb1fa00050a952/crc16/crc16.go
        // https://github.com/dongri/emv-qrcode/blob/ba6df536e538b4aee8f9c962bbfb1fa00050a952/emv/mpm/emv_mpm.go

        // https://github.com/lammertb/libcrc/blob/7719e2112a9a960b1bba130d02bebdf58e8701f1/test/testcrc.c#L64
        yield return new object[] { CRC16Type.CCITTFalse, "4C616D6D6572742042696573", (ushort)0x4A31 };

        // https://github.com/lammertb/libcrc/blob/7719e2112a9a960b1bba130d02bebdf58e8701f1/test/testcrc.c#L66
        yield return new object[] { CRC16Type.CCITTFalse, "20", (ushort)0xC592 };

        // https://github.com/howeyc/crc16/blob/2b2a61e366a66d3efb279e46176e7291001e0354/crc16_test.go#L59-L60
        yield return new object[] { CRC16Type.CCITTFalse, "7465737464617461", (ushort)0xDC7C };

        // https://github.com/archipel-network/archipel-core/blob/39726d62b2ee44bfa39b8f16df11d835f84228b9/test/unit/test_crc.c#L52
        yield return new object[] { CRC16Type.CCITTFalse, "61736466", (ushort)0xE170 };

        // https://github.com/archipel-network/archipel-core/blob/39726d62b2ee44bfa39b8f16df11d835f84228b9/test/unit/test_crc.c#L53
        yield return new object[] { CRC16Type.CCITTFalse, "6D657373616765", (ushort)0x9CDF };

        // https://github.com/archipel-network/archipel-core/blob/39726d62b2ee44bfa39b8f16df11d835f84228b9/test/unit/test_crc.c#L54
        yield return new object[] { CRC16Type.CCITTFalse, "536F6D65207265616C6C79206C6F6E67206D657373616765", (ushort)0x3B8B };

        // https://github.com/p4lang/behavioral-model/blob/7f4e1daca12b7fe52897687764b7e74ded7dab55/tests/test_calculations.cpp#L440-L450
        yield return new object[] { CRC16Type.CCITTFalse, "0BB81F90", (ushort)0x5D75 };

        // https://github.com/RIOT-OS/RIOT/blob/482bdc9e73a8a3e9c7774238ace19a8d9e6629f9/tests/unittests/tests-checksum/tests-checksum-crc16-ccitt-false.c#L42
        yield return new object[] { CRC16Type.CCITTFalse, "41", (ushort)0xB915 };

        // https://github.com/RIOT-OS/RIOT/blob/482bdc9e73a8a3e9c7774238ace19a8d9e6629f9/tests/unittests/tests-checksum/tests-checksum-crc16-ccitt-false.c#L52
        yield return new object[] { CRC16Type.CCITTFalse,
            "4141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141" +
            "4141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141" +
            "4141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141" +
            "4141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141" +
            "4141414141414141414141414141414141414141414141414141414141414141", (ushort)0xEA0B };

        // https://github.com/RIOT-OS/RIOT/blob/482bdc9e73a8a3e9c7774238ace19a8d9e6629f9/tests/unittests/tests-checksum/tests-checksum-crc16-ccitt-false.c#L78
        yield return new object[] { CRC16Type.CCITTFalse, "12345678", (ushort)0x30EC };

        // https://github.com/DarrenLevine/cppcrc/blob/6360eee1c8966d32b2552ce156b135ec6a3235f1/test/test_cppcrc.cpp#L42
        yield return new object[] { CRC16Type.CCITTFalse,
            "09035E573FA711F1EDF4EAE133966A0022E19383C25607484780CD481938A24C4E8105F9E8020E5F162D2811450E1C4F8463841E2F286E26" +
            "12BCC38211701841", (ushort)0x39CD };

        // https://github.com/mvallim/emv-qrcode/blob/34781d63a7d7180ff3d70e5fe482e88f6f090101/src/test/java/com/emv/qrcode/validators/Crc16ValidatorTest.java#L33
        yield return new object[] { CRC16Type.CCITTFalse,
            "303030323031303130323132323933303030313244313536303030303030303030353130413933464F333233305133313238303031324431" +
            "35363030303030303031303330383132333435363738353230343431313135383032434E3539313442455354205452414E53504F52543630" +
            "30374245494A494E4736343230303030325A4830313034E69C80E4BDB3E8BF90E8BE9330323032E58C97E4BAAC3534303532332E37323533" +
            "3033313536353530323031363233333033303431323334303630332A2A2A303730384136303038363637303930324D453931333230303136" +
            "4130313132323333343439393838373730373038313233343536373836333034", (ushort)0xA13A };

        // https://github.com/mvallim/emv-qrcode/blob/34781d63a7d7180ff3d70e5fe482e88f6f090101/src/test/java/com/emv/qrcode/decoder/mpm/MerchantPresentedModeDecoderTest.java#L249
        yield return new object[] { CRC16Type.CCITTFalse,
            "3030303230313031303231313032313630303034686F676530313034616263643532303434313131353330333135363534303532332E3732" +
            "35353032303135363033353030353730313535383032434E3539313442455354205452414E53504F5254363030374245494A494E47363130" +
            "3731323334353637363239353031303531323334353032303536373839303033303530393837363034303535343332313035303561626364" +
            "6530363035666768696A303730356B6C6D6E6F30383035707172657330393035747576787935303130303030313130313031693634323830" +
            "3030325A4830313032E58C97E4BAAC30323034E69C80E4BDB3E8BF90E8BE9330333034616263643635303230303830333230303136413031" +
            "3132323333343439393838373730373038313233343536373836333034", (ushort)0x4220 };

        // https://github.com/mvallim/emv-qrcode/blob/34781d63a7d7180ff3d70e5fe482e88f6f090101/src/test/java/com/emv/qrcode/decoder/mpm/MerchantPresentedModeDecoderTest.java#L286
        yield return new object[] { CRC16Type.CCITTFalse,
            "303030323031323635373030313442522E474F562E4243422E504958303131343630373031313930303030313034303231374D656E736167" +
            "656D206F7063696F6E616C35323034303030303533303339383635343034312E323235383032425235393230546573746520426174636820" +
            "617475616C697A613630303953414F205041554C4F36323236303532323174394F47457555576C715751695930435532596D4136333034", (ushort)0x8179 };

        // https://github.com/mvallim/emv-qrcode/blob/34781d63a7d7180ff3d70e5fe482e88f6f090101/src/test/java/com/emv/qrcode/decoder/mpm/MerchantPresentedModeDecoderTest.java#L287
        yield return new object[] { CRC16Type.CCITTFalse,
            "303030323031323638333030313462722E676F762E6263622E7069783031333634303034393031642D626438352D343736392D386535322D" +
            "636234633432633530366463303232314A6F726E6164612070616761646F7220353737363835323034303030303533303339383635343035" +
            "37332E36323538303242523539303350697836323038303530346F6F6F6F36333034", (ushort)0x8E87 };

        // https://github.com/mvallim/emv-qrcode/blob/34781d63a7d7180ff3d70e5fe482e88f6f090101/src/test/java/com/emv/qrcode/decoder/mpm/MerchantPresentedModeDecoderTest.java#L288
        yield return new object[] { CRC16Type.CCITTFalse,
            "303030323031323638333030313462722E676F762E6263622E7069783031333634303034393031642D626438352D343736392D386535322D" +
            "636234633432633530366463303232314A6F726E6164612070616761646F7220353737363835323034303030303533303339383635343035" +
            "37332E3632353830324252353930335069783630313653616F204A6F616F2064656C2052656936323038303530346F6F6F6F36333034", (ushort)0x4682 };

        // https://github.com/mvallim/emv-qrcode/blob/34781d63a7d7180ff3d70e5fe482e88f6f090101/src/test/java/com/emv/qrcode/decoder/mpm/MerchantPresentedModeDecoderTest.java#L289
        yield return new object[] { CRC16Type.CCITTFalse,
            "303030323031303431343132333435363738393031323334323635383030313442522E474F562E4243422E50495830313336313233653435" +
            "36372D653132622D313264312D613435362D343236363535343430303030323733303030313242522E434F4D2E4F5554524F303131303031" +
            "3233343536373839353230343030303035333033393836353430363132332E3435353830324252353931374E4F4D4520444F205245434542" +
            "45444F523630303842524153494C4941363130383730303734393030383033393030313242522E434F4D2E4F5554524F3031313930313233" +
            "2E414243442E333435362E5758595A36333034", (ushort)0x549C };

        // https://github.com/mvallim/emv-qrcode/blob/34781d63a7d7180ff3d70e5fe482e88f6f090101/src/test/java/com/emv/qrcode/decoder/mpm/MerchantPresentedModeDecoderTest.java#L290
        yield return new object[] { CRC16Type.CCITTFalse,
            "303030323031323638333030313462722E676F762E6263622E7069783031333634303034393031642D626438352D343736392D386535322D" +
            "636234633432633530366463303232314A6F726E6164612070616761646F7220353737363835323034303030303533303339383635343031" +
            "30353830324252353930335069783630303842524153494C494136323038303530346F6F6F6F36333034", (ushort)0x1F70 };

        // https://github.com/mvallim/emv-qrcode/blob/34781d63a7d7180ff3d70e5fe482e88f6f090101/src/test/java/com/emv/qrcode/decoder/mpm/DecoderMpmTest.java#L52
        yield return new object[] { CRC16Type.CCITTFalse,
            "3030303230313031303231313032313630303034686F676530313034616263643532303434313131353330333135363534303532332E3732" +
            "3535303230323536303335303035383032434E3539313442455354205452414E53504F5254363030374245494A494E473631303731323334" +
            "3536373632393530313035313233343530323035363738393030333035303938373630343035353433323130353035616263646530363035" +
            "666768696A303730356B6C6D6E6F30383035707172657330393033747576313030346162636435303034696A6B6C36343238303030325A48" +
            "30313032E58C97E4BAAC30323034E69C80E4BDB3E8BF90E8BE93303330346162636436353032303038303332303031364130313132323333" +
            "343439393838373730373038313233343536373836333034", (ushort)0xC659 };

        // https://github.com/dongri/emv-qrcode/blob/ba6df536e538b4aee8f9c962bbfb1fa00050a952/emv/mpm/emv_mpm_test.go#L351
        yield return new object[] { CRC16Type.CCITTFalse,
            "30303032303130313032313131353331323033313034313830303532303434364A44424D535A5A5845343442465330333836383030313641" +
            "30303532363632383436363235373730313038333231373034313830323033303032303332356D6368564B796F7A43334D626A5335316836" +
            "3771616C344F73353230343538313235333033343138353830324C41353931354A44424D535A5A5845343442465330363030395669656E74" +
            "69616E6536333034", (ushort)0x3F88 };

        // https://github.com/dongri/emv-qrcode/blob/ba6df536e538b4aee8f9c962bbfb1fa00050a952/emv/mpm/emv_mpm_test.go#L493
        yield return new object[] { CRC16Type.CCITTFalse, "30303032313236333034", (ushort)0x0194 };

        // https://github.com/dongri/emv-qrcode/blob/ba6df536e538b4aee8f9c962bbfb1fa00050a952/emv/mpm/emv_mpm_test.go#L500
        yield return new object[] { CRC16Type.CCITTFalse, "3030303232313036333034", (ushort)0x00C0 };

        // https://github.com/dongri/emv-qrcode/blob/ba6df536e538b4aee8f9c962bbfb1fa00050a952/emv/mpm/emv_mpm_test.go#L507
        yield return new object[] { CRC16Type.CCITTFalse, "303030323632393636333034", (ushort)0x000B };

        // https://github.com/tcltk/tcllib/blob/29bce268ae8d21a688d1a1efb256acc0e030a6a6/modules/crc/crc16.test#L175
        yield return new object[] { CRC16Type.CCITTFalse, "616263", (ushort)0x514A };

        // https://github.com/tcltk/tcllib/blob/29bce268ae8d21a688d1a1efb256acc0e030a6a6/modules/crc/crc16.test#L177
        yield return new object[] { CRC16Type.CCITTFalse, "414243", (ushort)0xF508 };

        // https://github.com/tcltk/tcllib/blob/29bce268ae8d21a688d1a1efb256acc0e030a6a6/modules/crc/crc16.test#L179
        yield return new object[] { CRC16Type.CCITTFalse, "54686973206973206120737472696E67", (ushort)0x4BE9 };

        // https://github.com/tcltk/tcllib/blob/29bce268ae8d21a688d1a1efb256acc0e030a6a6/modules/crc/crc16.test#L54
        yield return new object[] { CRC16Type.CCITTFalse, "2D616263", (ushort)0x17DE };

        // https://github.com/snksoft/crc/blob/03404db21ad4e7182edf4843b51f6252799f7140/crc_test.go#L74
        yield return new object[] { CRC16Type.CCITTFalse, "3132333435363738393031323334353637383930", (ushort)0xDA31 };

        // https://github.com/snksoft/crc/blob/03404db21ad4e7182edf4843b51f6252799f7140/crc_test.go#L75
        yield return new object[] { CRC16Type.CCITTFalse, "496E74726F64756374696F6E206F6E204352432063616C63756C6174696F6E73", (ushort)0xC87E };

        // https://github.com/snksoft/crc/blob/03404db21ad4e7182edf4843b51f6252799f7140/crc_test.go#L76
        yield return new object[] { CRC16Type.CCITTFalse,
            "5768656E65766572206469676974616C20646174612069732073746F726564206F7220696E74657266616365642C206461746120636F7272" +
            "757074696F6E206D69676874206F636375722E2053696E63652074686520626567696E6E696E67206F6620636F6D70757465722073636965" +
            "6E63652C2070656F706C652068617665206265656E207468696E6B696E67206F66207761797320746F206465616C20776974682074686973" +
            "2074797065206F662070726F626C656D2E20466F722073657269616C206461746120746865792063616D6520757020776974682074686520" +
            "736F6C7574696F6E20746F206174746163682061207061726974792062697420746F20656163682073656E7420627974652E205468697320" +
            "73696D706C6520646574656374696F6E206D656368616E69736D20776F726B7320696620616E206F6464206E756D626572206F6620626974" +
            "7320696E20612062797465206368616E6765732C2062757420616E206576656E206E756D626572206F662066616C7365206269747320696E" +
            "206F6E6520627974652077696C6C206E6F74206265206465746563746564206279207468652070617269747920636865636B2E20546F206F" +
            "766572636F6D6520746869732070726F626C656D2070656F706C65206861766520736561726368656420666F72206D617468656D61746963" +
            "616C20736F756E64206D656368616E69736D7320746F20646574656374206D756C7469706C652066616C736520626974732E", (ushort)0xD6ED };

        // PPP is explicitly an alias for X25: poly 1021, reflected, init/xorout FFFF.
        // https://github.com/npat-efault/crc16/blob/4128ccbe47c387ef36362690391e7e8da82057b0/crc16.go#L20-L26
        // https://github.com/npat-efault/crc16/blob/4128ccbe47c387ef36362690391e7e8da82057b0/crc16_test.go#L39-L70
        yield return new object[] { CRC16Type.X25, "61", (ushort)0x82F7 };
        yield return new object[] { CRC16Type.X25, "6162", (ushort)0x33DE };
        yield return new object[] { CRC16Type.X25, "616263", (ushort)0x9E25 };
        yield return new object[] { CRC16Type.X25, "61626364", (ushort)0xA36B };
        yield return new object[] { CRC16Type.X25, "6162636465", (ushort)0x19A5 };
        yield return new object[] { CRC16Type.X25, "616263646566", (ushort)0x04F6 };
        yield return new object[] { CRC16Type.X25, "61626364656667", (ushort)0x757C };
        yield return new object[] { CRC16Type.X25, "6162636465666768", (ushort)0xA6A8 };
        yield return new object[] { CRC16Type.X25, "616263646566676869", (ushort)0x275B };
        yield return new object[] { CRC16Type.X25, "6162636465666768696A", (ushort)0xD055 };
        yield return new object[] { CRC16Type.X25, "44697363617264206D65646963696E65206D6F7265207468616E2074776F207965617273206F6C642E", (ushort)0x7025 };
        yield return new object[] { CRC16Type.X25,
            "48652077686F2068617320612073686164792070617374206B6E6F77732074686174206E69636520677579732066696E697368206C617374" +
            "2E", (ushort)0x2BE0 };
        yield return new object[] { CRC16Type.X25, "4920776F756C646E2774206D617272792068696D207769746820612074656E20666F6F7420706F6C652E", (ushort)0x81D3 };
        yield return new object[] { CRC16Type.X25,
            "46726565212046726565212F4120747269702F746F204D6172732F666F72203930302F656D707479206A6172732F4275726D612053686176" +
            "65", (ushort)0xF471 };
        yield return new object[] { CRC16Type.X25,
            "5468652064617973206F6620746865206469676974616C20776174636820617265206E756D62657265642E20202D546F6D2053746F707061" +
            "7264", (ushort)0xDC73 };
        yield return new object[] { CRC16Type.X25, "4E6570616C207072656D69657220776F6E27742072657369676E2E", (ushort)0x6A62 };
        yield return new object[] { CRC16Type.X25,
            "466F7220657665727920616374696F6E20746865726520697320616E20657175616C20616E64206F70706F7369746520676F7665726E6D65" +
            "6E742070726F6772616D2E", (ushort)0xD860 };
        yield return new object[] { CRC16Type.X25,
            "486973206D6F6E6579206973207477696365207461696E7465643A20277461696E7420796F75727320616E6420277461696E74206D696E65" +
            "2E", (ushort)0xEE04 };
        yield return new object[] { CRC16Type.X25,
            "5468657265206973206E6F20726561736F6E20666F7220616E7920696E646976696475616C20746F2068617665206120636F6D7075746572" +
            "20696E20746865697220686F6D652E202D4B656E204F6C73656E2C2031393737", (ushort)0x1687 };
        yield return new object[] { CRC16Type.X25,
            "4974277320612074696E79206368616E676520746F2074686520636F646520616E64206E6F7420636F6D706C6574656C7920646973677573" +
            "74696E672E202D20426F62204D616E6368656B", (ushort)0x497D };
        yield return new object[] { CRC16Type.X25, "73697A653A2020612E6F75743A2020626164206D61676963", (ushort)0x0073 };
        yield return new object[] { CRC16Type.X25, "546865206D616A6F722070726F626C656D20697320776974682073656E646D61696C2E20202D4D61726B20486F72746F6E", (ushort)0xCCB3 };
        yield return new object[] { CRC16Type.X25,
            "47697665206D65206120726F636B2C20706170657220616E642073636973736F727320616E6420492077696C6C206D6F7665207468652077" +
            "6F726C642E20204343466573746F6F6E", (ushort)0x9464 };
        yield return new object[] { CRC16Type.X25, "49662074686520656E656D792069732077697468696E2072616E67652C207468656E20736F2061726520796F752E", (ushort)0x318E };
        yield return new object[] { CRC16Type.X25,
            "497427732077656C6C2077652063616E6E6F742068656172207468652073637265616D732F546861742077652063726561746520696E206F" +
            "74686572732720647265616D732E", (ushort)0x2CBB };
        yield return new object[] { CRC16Type.X25,
            "596F752072656D696E64206D65206F6620612054562073686F772C206275742074686174277320616C6C2072696768743A20492077617463" +
            "6820697420616E797761792E", (ushort)0xDE8B };
        yield return new object[] { CRC16Type.X25, "4320697320617320706F727461626C652061732053746F6E6568656467652121", (ushort)0xFE32 };
        yield return new object[] { CRC16Type.X25,
            "4576656E206966204920636F756C64206265205368616B657370656172652C2049207468696E6B20492073686F756C64207374696C6C2063" +
            "686F6F736520746F20626520466172616461792E202D20412E204875786C6579", (ushort)0x9186 };
        yield return new object[] { CRC16Type.X25,
            "546865206675676163697479206F66206120636F6E7374697475656E7420696E2061206D697874757265206F662067617365732061742061" +
            "20676976656E2074656D70657261747572652069732070726F706F7274696F6E616C20746F20697473206D6F6C65206672616374696F6E2E" +
            "20204C657769732D52616E64616C6C2052756C65", (ushort)0xD304 };
        yield return new object[] { CRC16Type.X25, "486F772063616E20796F752077726974652061206269672073797374656D20776974686F757420432B2B3F20202D5061756C20476C69636B", (ushort)0xFD73 };

        // https://github.com/DarrenLevine/cppcrc/blob/6360eee1c8966d32b2552ce156b135ec6a3235f1/test/test_cppcrc.cpp#L59
        yield return new object[] { CRC16Type.X25,
            "09035E573FA711F1EDF4EAE133966A0022E19383C25607484780CD481938A24C4E8105F9E8020E5F162D2811450E1C4F8463841E2F286E26" +
            "12BCC38211701841", (ushort)0x7B46 };

        // https://github.com/archipel-network/archipel-core/blob/39726d62b2ee44bfa39b8f16df11d835f84228b9/test/unit/test_crc.c#L30-L33
        yield return new object[] { CRC16Type.X25, "61736466", (ushort)0x1238 };
        yield return new object[] { CRC16Type.X25, "6D657373616765", (ushort)0xA15C };
        yield return new object[] { CRC16Type.X25, "536F6D65207265616C6C79206C6F6E67206D657373616765", (ushort)0xFFCE };
    }

    [Fact]
    public void PublishedCorpusHasAtLeastThirtyDistinctNonemptyInputsPerAlgorithm()
    {
        foreach (CRC16Type type in new[] { CRC16Type.CCITTFalse, CRC16Type.X25 })
        {
            string[] inputs = PublishedVectors().Where(vector => (CRC16Type)vector[0] == type)
                .Select(vector => Convert.ToHexString(Convert.FromHexString((string)vector[1]))).ToArray();
            Assert.True(inputs.Length >= 30, $"{type} requires at least 30 published inputs.");
            Assert.Equal(inputs.Length, inputs.Distinct(StringComparer.Ordinal).Count());
            Assert.All(inputs, input => Assert.NotEmpty(input));
        }
    }

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public async Task PublishedFixedAnswersMatchOracleOneShotEverySplitAndStreams(
        CRC16Type type, string hex, ushort expected)
    {
        byte[] input = Convert.FromHexString(hex);
        Assert.Equal(expected, CrcReference.Compute16(input, type));
        Assert.Equal(expected, input.CRC16(type));
        Assert.Equal(NativeBytes(expected), new CRC16(type).Compute(input));

        var accumulator = new CRC16Accumulator(type);
        for (int split = 0; split <= input.Length; split++)
        {
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, split));
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(input.AsSpan(split));
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }

        using var synchronous = new MemoryStream(input, writable: false);
        using var asynchronous = new MemoryStream(input, writable: false);
        Assert.Equal(expected, synchronous.ComputeCRC16(type));
        Assert.Equal(expected, await asynchronous.ComputeCRC16Async(type));
    }

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public void PublishedAnswersDefineNativeEndianFramesAndRejectEverySingleBitMutation(
        CRC16Type type, string hex, ushort expected)
    {
        byte[] input = Convert.FromHexString(hex);
        byte[] frame = input.Concat(NativeBytes(expected)).ToArray();
        var detector = new CRC16(type);
        Assert.Equal(frame, detector.Encode(input));
        Assert.Equal(frame, input.WithCRC16(type));
        Assert.Equal(input, detector.Decode(frame));
        Assert.False(detector.IsError(frame));

        for (int bit = 0; bit < frame.Length * 8; bit++)
        {
            byte[] damaged = frame.ToArray();
            damaged[bit / 8] ^= (byte)(1 << (bit % 8));
            Assert.True(detector.IsError(damaged));
            Assert.Empty(detector.Decode(damaged));
        }
    }

    [Fact]
    public void X25ProtocolFramesHavePublishedRawResidueWithLittleEndianFcs()
    {
        // RFC 1662, Appendix C.2: PPPGOODFCS16 is the raw, uncomplemented
        // receive register 0xF0B8. The final numerical CRC is its complement.
        // HDLC uses LSB-first FCS octets regardless of machine endianness;
        // Mythosia's native-endian Encode contract is tested separately above.
        // https://www.rfc-editor.org/rfc/rfc1662.html#appendix-C.2
        const ushort rawResidue = 0xF0B8;
        const ushort finalCrcOfFrame = unchecked((ushort)~rawResidue);
        foreach (object[] vector in PublishedVectors())
        {
            if ((CRC16Type)vector[0] != CRC16Type.X25)
                continue;

            byte[] input = Convert.FromHexString((string)vector[1]);
            ushort fcs = (ushort)vector[2];
            byte[] protocolFrame = input.Concat(new[] { (byte)fcs, (byte)(fcs >> 8) }).ToArray();
            Assert.Equal(finalCrcOfFrame, CrcReference.Compute16(protocolFrame, CRC16Type.X25));
            Assert.Equal(finalCrcOfFrame, protocolFrame.CRC16(CRC16Type.X25));
            var accumulator = new CRC16Accumulator(CRC16Type.X25);
            accumulator.Append(protocolFrame);
            Assert.Equal(rawResidue, unchecked((ushort)~accumulator.GetCurrentValue()));
        }
    }

    [Theory]
    [InlineData(CRC16Type.CCITTFalse, 0xFFFF)]
    [InlineData(CRC16Type.X25, 0x0000)]
    public async Task EmptyStateResetAndDigestOnlyFramesHaveExplicitContracts(CRC16Type type, int expectedValue)
    {
        ushort expected = (ushort)expectedValue;
        byte[] empty = Array.Empty<byte>();
        var detector = new CRC16(type);
        Assert.Equal(type, detector.Type);
        Assert.Equal(type.ToString(), detector.GetDetectionType());
        Assert.Equal(expected, CrcReference.Compute16(empty, type));
        Assert.Equal(expected, empty.CRC16(type));
        Assert.Equal(NativeBytes(expected), detector.Compute(empty));
        Assert.Equal(NativeBytes(expected), detector.Encode(empty));

        var accumulator = new CRC16Accumulator(type);
        Assert.Equal(expected, accumulator.GetCurrentValue());
        accumulator.Append(new byte[] { 0x80, 0x00, 0xFF });
        accumulator.Reset();
        accumulator.Append(ReadOnlySpan<byte>.Empty);
        Assert.Equal(expected, accumulator.GetCurrentValue());

        using var source = new MemoryStream();
        Assert.Equal(expected, source.ComputeCRC16(type));
        Assert.Equal(expected, await source.ComputeCRC16Async(type));
        Assert.True(source.CanRead);
        for (int length = 0; length <= sizeof(ushort); length++)
        {
            Assert.True(detector.IsError(NativeBytes(expected).Take(length)));
            Assert.Empty(detector.Decode(NativeBytes(expected).Take(length)));
        }
    }

    [Theory]
    [InlineData(CRC16Type.CCITTFalse, 0x29B1)]
    [InlineData(CRC16Type.X25, 0x906E)]
    public void NewComputePathsConsumeAForwardOnlyEnumerableOnceAndDisposeIt(CRC16Type type, int expectedValue)
    {
        byte[] input = Convert.FromHexString("313233343536373839");
        AssertSinglePass(source => BitConverter.ToUInt16(new CRC16(type).Compute(source).ToArray(), 0));
        AssertSinglePass(source => source.CRC16(type));

        void AssertSinglePass(Func<IEnumerable<byte>, ushort> compute)
        {
            int enumerations = 0;
            int disposals = 0;
            Assert.Equal((ushort)expectedValue, compute(Enumerate()));
            Assert.Equal(1, enumerations);
            Assert.Equal(1, disposals);

            IEnumerable<byte> Enumerate()
            {
                if (++enumerations != 1)
                    throw new InvalidOperationException("The input cannot be enumerated twice.");
                try
                {
                    foreach (byte value in input)
                        yield return value;
                }
                finally
                {
                    disposals++;
                }
            }
        }
    }

    private static byte[] NativeBytes(ushort value)
        => BitConverter.IsLittleEndian
            ? new[] { (byte)value, (byte)(value >> 8) }
            : new[] { (byte)(value >> 8), (byte)value };
}
