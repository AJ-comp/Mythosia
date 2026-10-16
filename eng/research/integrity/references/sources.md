# Research source provenance

These links, revisions, and notices were recorded in the original local research
files or the maintained test fixtures. A missing pin or license is identified
explicitly rather than inferred. Full upstream implementations, PDFs, package
caches, and captures are not copied into this archive.

## xxHash input/answer facts

`../xxhash/vectors.json` preserves the 155 selected input/seed/expected-value facts
previously saved in `TestResults/xxhash-research/vectors.json`. Case names come from
the matching tracked `Mythosia.Test/Integrity/XxHashPublishedVectors.cs` rows. The
rendering helper uses this offline snapshot rather than parsing downloaded C++ and
Go source files. The expected hashes are not calculated by the Mythosia library.

| Source | Recorded revision | License evidence and retained material |
| --- | --- | --- |
| [jmacheta/checksum xxHash vectors](https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/xxhash_reference_vectors.hpp#L25-L266), [input generator](https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/include/test_data.hpp#L16-L23) | `39358d88bb755c74ca0f50096eb607a564155755` | MIT; the original notice is retained in `jmacheta.LICENSE.txt` and emitted with review fixtures. Only selected published input/answer facts are kept. |
| [pierrec XXH32 fixtures](https://github.com/pierrec/xxHash/blob/36da3d21705c750db3ecfad1e46dc667baeb0ca9/xxHash32/xxHash32_test.go#L17-L32), [XXH64 fixtures](https://github.com/pierrec/xxHash/blob/36da3d21705c750db3ecfad1e46dc667baeb0ca9/xxHash64/xxHash64_test.go#L17-L32) | `36da3d21705c750db3ecfad1e46dc667baeb0ca9` | BSD notice; retained in `pierrec.LICENSE.txt` and emitted with review fixtures. Only published message/hash facts are kept. |
| [Official xxHash sanity facts](https://github.com/Cyan4973/xxHash/blob/e626a72bc2321cd320e953a0ccf1584cad60f363/cli/xsum_sanity_check.c#L101-L123), [input construction](https://github.com/Cyan4973/xxHash/blob/e626a72bc2321cd320e953a0ccf1584cad60f363/cli/xsum_sanity_check.c#L44-L60) | `e626a72bc2321cd320e953a0ccf1584cad60f363` | The downloaded CLI source identifies GPL v2 or later. Its implementation and harness are excluded; only transcribed numeric input/hash facts remain. |
| [xxHash algorithm specification](https://github.com/Cyan4973/xxHash/blob/v0.8.3/doc/xxhash_spec.md) | `v0.8.3` | Reference link from the historical notes; full specification is excluded. |
| [python-xxhash](https://github.com/ifduyue/python-xxhash) | Installed package metadata records version `3.6.0`; the downloaded `python32.py` test file has no recorded commit pin. | Package metadata records BSD. The external test file and installed package are excluded; the retained tools do not require them. |

## Fletcher-4 and OSPF

- [OpenZFS Fletcher implementation](https://github.com/openzfs/zfs/blob/34f96a15c73eab27dd6ad17bb5f1263bf26e37d7/module/zcommon/zfs_fletcher.c),
  [send-record mapping](https://github.com/openzfs/zfs/blob/34f96a15c73eab27dd6ad17bb5f1263bf26e37d7/module/zfs/dmu_send.c#L257-L284),
  [record definition](https://github.com/openzfs/zfs/blob/34f96a15c73eab27dd6ad17bb5f1263bf26e37d7/include/sys/zfs_ioctl.h#L374-L387),
  and [published send fixture](https://github.com/openzfs/zfs/blob/34f96a15c73eab27dd6ad17bb5f1263bf26e37d7/tests/zfs-tests/tests/functional/rsend/dedup.zsend.bz2):
  revision `34f96a15c73eab27dd6ad17bb5f1263bf26e37d7`. Downloaded source headers
  identify CDDL-1.0. Full source files are excluded from this archive. Existing
  published data and the CDDL notice remain in
  `Mythosia.Test/Integrity/Fletcher4PublishedVectors.cs` and its `TestData` directory.
- [RFC 2328 section 12.1.7](https://www.rfc-editor.org/rfc/rfc2328.html#section-12.1.7)
  and [appendix B](https://www.rfc-editor.org/rfc/rfc2328.html#appendix-B) describe
  the OSPF LSA checksum. The standard is linked, not reproduced.
- [tcpdump capture corpus](https://github.com/the-tcpdump-group/tcpdump/tree/3a2f6acd865e5b845d9f63caf5f2c23cb73a2bc9/tests):
  revision `3a2f6acd865e5b845d9f63caf5f2c23cb73a2bc9`. The existing fixture records a
  BSD notice, selected LSA byte sequences, original filenames, frame offsets, and
  capture SHA-256 values in `Mythosia.Test/Integrity/OspfLsaPublishedVectors.cs`.
  Full captures are excluded here. For example,
  [ospf-nssa-bitnt.pcap](https://github.com/the-tcpdump-group/tcpdump/blob/3a2f6acd865e5b845d9f63caf5f2c23cb73a2bc9/tests/ospf-nssa-bitnt.pcap)
  has SHA-256 `50C00FD8053B43B3AACF996C75FC3E7023B20D13AB1721E9E264A95E018B644F`.
- Other discarded source-index caches record
  [Wireshark](https://github.com/wireshark/wireshark/tree/b97ab61b7fdcdb15c3c8b184fac6d0526cf4cb4e)
  at `b97ab61b7fdcdb15c3c8b184fac6d0526cf4cb4e` and
  [Scapy](https://github.com/secdev/scapy/tree/8e08cbf759de6709a5b4af6bea3655d293129bb4)
  at `8e08cbf759de6709a5b4af6bea3655d293129bb4`. No files from these caches are
  copied. Their per-file licenses were not captured in the retained research notes.

## CRC references

| Source | Recorded version or revision | Preservation decision |
| --- | --- | --- |
| [Sensirion SHT4x datasheet](https://sensirion.com/resource/datasheet/sht4x) | Historical notes record v7.3, June 2026, section 4.4/table 7. The URL may resolve to a later edition. | Link and model parameters only; downloaded full PDF and extracted pages are excluded. |
| [AUTOSAR CRC Library](https://www.autosar.org/fileadmin/standards/R24-11/CP/AUTOSAR_CP_SWS_CRCLibrary.pdf) | CP R24-11; downloaded PDF SHA-256 `496f7e2885c308f382aa2b9f9bf3f7b472546731fcde384fae622ace3cc1b92f`. | Link and factual parameters only; full PDF is excluded. Existing tests retain numeric facts with their source IDs. |
| [jmacheta CRC vectors](https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/include/crc_reference_vectors.hpp#L25-L45), [fixture consumer](https://github.com/jmacheta/checksum/blob/39358d88bb755c74ca0f50096eb607a564155755/tests/unit_tests/ut_crc_vectors.cpp#L29-L44) | `39358d88bb755c74ca0f50096eb607a564155755`, MIT notice recorded with the existing fixture. | Downloaded C++ excluded; published facts remain in `Mythosia.Test/Integrity/RemainingCrcVariantsPublishedVectors.cs`. |
| [autosar-e2e CRC tests](https://github.com/zariiii9003/autosar-e2e/blob/263c08172acfcb87cd3f9ad37dbdbddd8f5da075/test/test_crc.py#L38-L67), [Profile 2 tests](https://github.com/zariiii9003/autosar-e2e/blob/263c08172acfcb87cd3f9ad37dbdbddd8f5da075/test/test_p02.py#L5-L66) | `263c08172acfcb87cd3f9ad37dbdbddd8f5da075`, MIT notice recorded with the existing fixture. | Downloaded Python/C sources excluded; published facts and notices remain in `Crc8AutosarH2FPublishedVectors.cs`. |
| [js-crc AUTOSAR tests](https://github.com/taichunmin/js-crc/blob/609858d9743ea2884715c810932c0aa3daf084bc/lib/crc8.test.ts#L48-L56) | `609858d9743ea2884715c810932c0aa3daf084bc`, MIT notice recorded with the existing fixture. | Downloaded TypeScript source excluded; facts and notice remain with the existing test fixture. |
| [CRC-Forge tests](https://github.com/CodeName-666/CRC-Forge/blob/b54bea45bb929da4379a6f0bbd2b8e5a4ec4a277/test/test_crc/test_main.cpp#L72-L85) | `b54bea45bb929da4379a6f0bbd2b8e5a4ec4a277`, MIT notice recorded with the existing fixture. | Downloaded C++ source excluded; facts and notice remain with the existing test fixture. |

Additional reference links preserved from the historical notes are the
[CRC RevEng catalogue](https://reveng.sourceforge.io/crc-catalogue/all.htm),
[USB CRC explanation](https://www.usb.org/sites/default/files/crcdes.pdf),
[MAVLink serialization guide](https://mavlink.io/en/guide/serialization.html#checksum),
[TSDuck MPEG CRC documentation](https://tsduck.io/docs/tsduck.html#tscrc32), and
[bzip2 manual](https://www.sourceware.org/bzip2/manual/manual.html). No revision or
redistribution permission was recorded for those web-document snapshots, so their
full contents are not included.
