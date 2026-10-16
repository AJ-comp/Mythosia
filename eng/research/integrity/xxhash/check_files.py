"""Write normalized review copies of maintained sources; never edit those sources."""

from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[4]
OUTPUT_ROOT = REPOSITORY_ROOT / "TestResults/Research/integrity/xxhash/review"


def main():
    checksum_sources = REPOSITORY_ROOT / "Mythosia.Integrity/Checksum"
    test_sources = REPOSITORY_ROOT / "Mythosia.Test/Integrity"
    sources = sorted(checksum_sources.glob("*XxHash*.cs"))
    sources += sorted(test_sources.glob("XxHash*.cs"))
    if not sources:
        raise SystemExit("No maintained xxHash source files were found.")

    for source in sources:
        content = source.read_text(encoding="utf-8")
        content = content.replace(
            "xxhash_reference_vectors.hpp#L25-L283",
            "xxhash_reference_vectors.hpp#L25-L266",
        )
        normalized = "\n".join(line.rstrip() for line in content.splitlines()) + "\n"
        destination = OUTPUT_ROOT / source.relative_to(REPOSITORY_ROOT)
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(normalized, encoding="utf-8", newline="\n")

    print(f"Wrote {len(sources)} review copies to {OUTPUT_ROOT}")


if __name__ == "__main__":
    main()
