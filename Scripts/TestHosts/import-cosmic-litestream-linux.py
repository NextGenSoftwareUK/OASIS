"""One-time native SQLite backup admission; preserve original identities and failed Windows state."""
import hashlib
import json
import os
from pathlib import Path
import sqlite3

original = Path("/mnt/c/Source/OASIS-cosmic/LocalState/COSMIC/LitestreamCql")
state = Path("/var/tmp/oasis-litestream-cql")
if state.exists():
    raise RuntimeError("Original Linux admission exists; inspect instead of recreating")
assert not (original / "replication-probe.pending.json").exists()
assert (original / "replication-probe.json").is_file()
state.mkdir()
intent = state / "source-import.pending.json"
record = dict(source=str(original / "source.db"), destination=str(state / "source.db"), method="SQLite online backup")
def durable(path, data):
    with path.open("xb") as output:
        output.write(data)
        output.flush()
        os.fsync(output.fileno())
durable(intent, json.dumps(record).encode())
source = sqlite3.connect("file:" + str(original / "source.db") + "?mode=ro", uri=True)
target = sqlite3.connect(state / "source.db")
try:
    source.backup(target)
    assert list(source.iterdump()) == list(target.iterdump()), "Backup must preserve all schema and rows"
    expected = json.loads((original / "source-snapshot.json").read_text())
    observed = [identity + "|" + str(deleted) + "|" + hashlib.sha256(document.encode()).hexdigest().upper()
                for identity, document, deleted in target.execute("SELECT id,data_json,is_deleted FROM oasis_holons ORDER BY id")]
    assert observed == expected, "Original holon hashes/tombstones changed"
    inherited = json.loads((original / "replication-probe.json").read_text())
    assert target.execute("SELECT marker FROM cosmic_cql_replication_probe").fetchall() == [(inherited["marker"],)]
finally:
    target.close()
    source.close()
for name in ["demo-seed.json", "source-snapshot.json"] + [f"source-write-{i}.ack.json" for i in range(4)]:
    durable(state / name, (original / name).read_bytes())
durable(state / "inherited-replication-probe.json", (original / "replication-probe.json").read_bytes())
with (state / "source.db").open("rb") as stream:
    os.fsync(stream.fileno())
record["backupSha256"] = hashlib.sha256((state / "source.db").read_bytes()).hexdigest()
record["seedSha256"] = hashlib.sha256((state / "demo-seed.json").read_bytes()).hexdigest()
durable(state / "source-import.json", json.dumps(record, indent=2).encode())
intent.rename(state / "source-import.intent.json")
fd = os.open(state, os.O_RDONLY | os.O_DIRECTORY)
try:
    os.fsync(fd)
finally:
    os.close(fd)
print(json.dumps(record))
