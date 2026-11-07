"""Validate a qualified Waku artifact and print its WEB4 descriptor."""

import argparse
import hashlib
import json
from pathlib import Path
import re
import uuid


ARCHIVE = "store-qualified.sqlite3"
RECEIPT = "archive-qualified.json"
MAX_ARCHIVE_BYTES = 8 * 1024 * 1024


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate Waku receipt field")
        result[key] = value
    return result


def descriptor(directory, owner_text):
    owner = uuid.UUID(owner_text)
    if not owner.int or str(owner) != owner_text:
        raise ValueError("A canonical non-empty teaching owner is required")

    root = directory.resolve(strict=True)
    receipt = root / RECEIPT
    archive = root / ARCHIVE
    for file in (receipt, archive):
        if file.resolve(strict=True) != file or not file.is_file() or file.is_symlink():
            raise ValueError("Expected regular files in the qualified Waku artifact")

    with receipt.open("rb") as stream:
        raw = stream.read(2049)
    if len(raw) > 2048:
        raise ValueError("Waku receipt exceeds its size limit")
    record = json.loads(raw, object_pairs_hook=unique_object)
    if not isinstance(record, dict) or set(record) != {"archive", "sha256", "bytes", "messageHashes"}:
        raise ValueError("Unexpected Waku receipt fields")
    hashes = record["messageHashes"]
    if (record["archive"] != ARCHIVE or type(record["bytes"]) is not int
            or not 0 < record["bytes"] <= MAX_ARCHIVE_BYTES
            or not isinstance(record["sha256"], str)
            or not re.fullmatch(r"[a-f0-9]{64}", record["sha256"])
            or not isinstance(hashes, list) or len(hashes) != 3
            or any(not isinstance(value, str) or not re.fullmatch(r"0x[a-f0-9]{64}", value) for value in hashes)
            or len(set(hashes)) != 3):
        raise ValueError("Invalid Waku archive metadata or reviewed hashes")

    with archive.open("rb") as stream:
        contents = stream.read(record["bytes"] + 1)
    if len(contents) != record["bytes"] or hashlib.sha256(contents).hexdigest() != record["sha256"]:
        raise ValueError("Waku archive differs from the qualified receipt")
    if not contents.startswith(b"SQLite format 3\x00"):
        raise ValueError("The qualified Waku archive must be a SQLite database")

    return {"sourceOwner": owner_text, "messageHashes": hashes}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    parser.add_argument("owner")
    args = parser.parse_args()
    try:
        print(json.dumps(descriptor(args.directory, args.owner), separators=(",", ":")))
    except (OSError, ValueError) as error:
        parser.exit(1, f"Waku artifact rejected: {error}\n")


if __name__ == "__main__":
    main()
