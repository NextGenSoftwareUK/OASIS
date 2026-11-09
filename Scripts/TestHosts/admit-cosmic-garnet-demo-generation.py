"""Explicitly admit a new synthetic fixture; never recover/replay the lost first dataset."""
import fcntl
import hashlib
import json
import os
from pathlib import Path
import uuid

ORIGINAL = Path("/var/tmp/oasis-garnet-cql")
NEW = ORIGINAL / "generations/2"
PROBE = Path("/var/tmp/oasis-garnet-storage-probe/empty-checkpoint-admission.json")
DIGEST = "4e38420e097c7c72c5cba0a9a8fa864ced165d48f5ec7ad79387e0b5ac2b3750"

def original_files():
    return {str(path.relative_to(ORIGINAL)): hashlib.sha256(path.read_bytes()).hexdigest()
            for path in ORIGINAL.rglob("*") if path.is_file() and path.relative_to(ORIGINAL).parts[0] != "generations"}

def durable(path, value, private=False):
    descriptor = os.open(path, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600 if private else 0o644)
    with os.fdopen(descriptor, "w") as output:
        json.dump(value, output, indent=2); output.flush(); os.fsync(output.fileno())

with (ORIGINAL / "runner.lock").open("a") as lock:
    fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
    if NEW.exists():
        record = json.loads((NEW / "fixture-admission.json").read_text())
        assert record["originalFiles"] == original_files()
        assert record["probeSha256"] == hashlib.sha256(PROBE.read_bytes()).hexdigest()
        print("Reused explicitly admitted second fixture; no source initialization/replay performed")
    else:
        before = original_files()
        assert (ORIGINAL / "runner.pending.json").is_file() and (ORIGINAL / "checkpoint.pending.json").is_file()
        assert not any(path.is_file() for path in (ORIGINAL / "data").rglob("*"))
        pid = json.loads((ORIGINAL / "active-process.json").read_text())
        assert not Path("/proc/" + str(pid["pid"])).exists(), "Original process must be authoritatively terminal"
        probe = json.loads(PROBE.read_text())
        assert probe["acknowledged"] and probe["emptyBeforeAndAfter"] and probe["originalFilesUnchanged"]
        assert probe["runtimeSha256"] == DIGEST
        old = json.loads((ORIGINAL / "demo-seed.json").read_text())
        seed = dict(Actor=str(uuid.uuid4()), Ids=[str(uuid.uuid4()) for _ in range(3)], DeletedId=str(uuid.uuid4()))
        assert seed["Actor"] != old["Actor"]
        assert not set(seed["Ids"] + [seed["DeletedId"]]) & set(old["Ids"] + [old["DeletedId"]])
        NEW.mkdir(parents=True, mode=0o700)
        durable(NEW / "fixture-admission.pending.json", dict(generation=2, originalFiles=before,
                    purpose="new synthetic demo dataset; original uncheckpointed source remains lost, not recovered"))
        (NEW / "data").mkdir(mode=0o700)
        config = json.loads((ORIGINAL / "garnet.conf").read_text())
        config["CheckpointDir"] = str(NEW / "data/checkpoints")
        durable(NEW / "garnet.conf", config, private=True)
        durable(NEW / "admitted-seed.json", seed)
        credentials = {name: hashlib.sha256((ORIGINAL / name).read_bytes()).hexdigest() for name in ["native-password", "storage-key"]}
        durable(NEW / "store.json", dict(runtimeSha256=DIGEST, port=16392, database=0, data=str(NEW / "data"),
                    credentialRoot=str(ORIGINAL), credentialFiles=credentials,
                    files={"garnet.conf": hashlib.sha256((NEW / "garnet.conf").read_bytes()).hexdigest()}))
        record = dict(generation=2, purpose="explicit independent synthetic demo dataset", oldSourceDisposition="known lost; original receipts/intents retained unchanged",
                      originalFiles=before, probeSha256=hashlib.sha256(PROBE.read_bytes()).hexdigest(), runtimeSha256=DIGEST,
                      seedSha256=hashlib.sha256((NEW / "admitted-seed.json").read_bytes()).hexdigest())
        assert before == original_files()
        durable(NEW / "fixture-admission.json", record)
        (NEW / "fixture-admission.pending.json").unlink()
        print("Admitted generation2 with fresh synthetic identities; original source files and pending gates unchanged")
