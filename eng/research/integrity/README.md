# Integrity research archive

This directory preserves hand-written research notes and development helpers that
previously lived under ignored `TestResults` directories. The notes capture the
implementation work that introduced these APIs; their `unreleased` labels belong
to that historical snapshot, not the current publication status. Use the maintained [Integrity README](../../../Mythosia.Integrity/README.md)
and [package validation guide](../../IntegrityPackageValidation.md) for current behavior.

The scripts require Python 3.10 or newer and use only the standard library. Paths
are resolved from the script location, so commands also work from another working
directory. Generated copies go under `TestResults/Research/integrity/`, which stays
ignored. None of these scripts overwrites maintained product or test sources.

From the repository root:

```text
python eng/research/integrity/xxhash/check_xxhash_math.py
python eng/research/integrity/checksum/write_checksum_expected.py
python eng/research/integrity/xxhash/write_vectors.py
python eng/research/integrity/xxhash/check_files.py
python eng/research/integrity/xxhash/write_product.py
```

- `check_xxhash_math.py` independently checks the nine-byte xxHash example using scalar math.
- `write_checksum_expected.py` reproduces the three checksum examples and compares
  them with the preserved expected-output snapshot. `checksum/expectedstdout.json`
  separately preserves the remaining CRC examples and their original code hashes.
- `write_vectors.py` renders the 155 published input/answer facts in `xxhash/vectors.json`
  into a review copy of `XxHashPublishedVectors.cs`. The JSON includes the original
  case names and needs no downloaded upstream source or Python hash package.
- `check_files.py` writes normalized review copies of the maintained xxHash sources.
- `write_product.py` reproduces a historical prototype. Its output is a reference
  for comparing old implementation ideas; it is not a generator for updating the
  maintained implementation. Review any proposed changes manually.

The optional `checksum/extract_ospf.py` preserves the public-capture extraction
helper. It requires an explicitly supplied directory of downloaded `.pcap` or
`.pcapng` files; no capture download is performed. Relative input paths are resolved
from the repository root. The original capture URLs and existing frozen test
vectors are recorded in [references/sources.md](references/sources.md).

```text
python eng/research/integrity/checksum/extract_ospf.py path/to/public-captures
```

## Original locations

| Ignored original | Preserved location |
| --- | --- |
| `TestResults/AdditionalAlgorithms/docs/checksum-families.md`, `remaining-crc.md` | `notes/` with a historical-status note |
| `TestResults/AdditionalAlgorithms/docs/check_xxhash_math.py` | `xxhash/check_xxhash_math.py` |
| `TestResults/AdditionalAlgorithms/docs/write_checksum_expected.py`, `*.json` | `checksum/` |
| `TestResults/xxhash-research/check_files.py`, `write_product.py`, `write_vectors.py` | `xxhash/`, with outputs redirected to ignored review copies |
| `TestResults/xxhash-research/vectors.json` | `xxhash/vectors.json`, with case names preserved from the tracked published fixture |
| `TestResults/xxhash-research/jm_license.txt`, `pierrec_license.txt` | `references/*.LICENSE.txt` |
| `TestResults/FletcherSources/extract_ospf.py` | `checksum/extract_ospf.py` |

Downloaded full PDF specifications, C/C++/Go/Python source trees, the installed
`python-xxhash` package, raw packet captures, temporary consumers, and duplicate
generated test files are not vendored here. Their useful provenance is preserved
in [references/sources.md](references/sources.md); published facts and any notices
already required by the maintained tests remain with those tests. The original
ignored directories are not required to build/test or run these helpers. The
optional capture extractor takes separately supplied external inputs. Historical benchmark runs, CI
outputs, and deployment screenshots are separate records rather than inputs to
these research tools.
