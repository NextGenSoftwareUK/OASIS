"""Write the reviewed Pinot teaching fixture into an isolated CI directory."""
import argparse
import json
from pathlib import Path


TABLE = "oasis_cql_public_holons"
ACTOR = "a5808c3e-7031-47e7-92de-48a659568f1b"


def write_fixture(path):
    schema = {
        "schemaName": TABLE,
        "dimensionFieldSpecs": [
            {"name": "id", "dataType": "STRING"},
            {"name": "is_public", "dataType": "INT"},
            {"name": "doc", "dataType": "STRING"},
        ],
        "dateTimeFieldSpecs": [{
            "name": "timestamp", "dataType": "LONG",
            "format": "1:MILLISECONDS:EPOCH", "granularity": "1:MILLISECONDS",
        }],
    }
    table = {
        "tableName": TABLE, "tableType": "OFFLINE",
        "segmentsConfig": {"schemaName": TABLE, "replication": "1", "timeColumnName": "timestamp"},
        "tenants": {"broker": "DefaultTenant", "server": "DefaultTenant"},
        "tableIndexConfig": {"loadMode": "MMAP"},
    }
    for name, document in (("schema.json", schema), ("table.json", table)):
        with (path / name).open("x", encoding="utf-8") as file:
            json.dump(document, file)
    with (path / "records.json").open("x", encoding="utf-8") as file:
        for position in range(1, 6):
            key = f"20000000-0000-4000-8000-{position:012d}"
            doc = {"id": key, "name": f"Pinot teaching record {position}", "createdByAvatarId": ACTOR,
                   "isPublic": position != 5, "isActive": True}
            if position == 4:
                doc["deletedDate"] = "2026-01-01T00:00:00Z"
            row = {"timestamp": 1767225600000, "id": key, "is_public": int(doc["isPublic"]),
                   "doc": json.dumps(doc, separators=(",", ":"))}
            file.write(json.dumps(row) + "\n")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("directory", type=Path)
    args = parser.parse_args()
    write_fixture(args.directory.resolve(strict=True))
