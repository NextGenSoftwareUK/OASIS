"""Reuse one owned Linux Mongo store for the admitted Litestream teaching preset."""
import argparse
import fcntl
import hashlib
import json
import os
from pathlib import Path
import runpy
import socket
import subprocess
import time

args = argparse.ArgumentParser()
args.add_argument("--serve", action="store_true")
options = args.parse_args()
ROOT = Path("/mnt/c/Source/OASIS-cosmic")
STATE = Path("/var/tmp/oasis-cosmic-mongo")
DATA = STATE / "data"
DAEMON = Path("/var/tmp/oasis-runtimes/mongodb-8.3.13/mongod")
DIGEST = "6e449a554aca8ba29c91c6d640d47a6ed32c1856384925b707ca684c3743570c"
RESULTS = ROOT / "TestResults/LitestreamCqlCurrent"
assert hashlib.sha256(DAEMON.read_bytes()).hexdigest() == DIGEST
# Verify original native source/restore first, never create a replacement fixture.
context = runpy.run_path(str(ROOT / "Scripts/TestHosts/run-cosmic-litestream-linux.py"))
assert not (context["STATE"] / "mongo.pending.json").exists(), "Original Mongo fixture unresolved"
STATE.mkdir(exist_ok=True)
def durable(path, record):
    with path.open("x") as output:
        json.dump(record, output, indent=2)
        output.flush()
        os.fsync(output.fileno())
def listen_inodes():
    found = []
    for line in Path("/proc/net/tcp").read_text().splitlines()[1:]:
        fields = line.split()
        if fields[1].endswith(":" + format(27039, "04X")) and fields[3] == "0A":
            assert fields[1].split(":")[0] == "0100007F", "Mongo must remain loopback-only"
            found.append(fields[9])
    return found
with (STATE / "environment.lock").open("a") as lock:
    fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
    assert not (STATE / "replica-init.pending.json").exists(), "Original replica-init outcome unresolved"
    record = dict(data=str(DATA), runtimeSha256=DIGEST, port=27039, replica="cosmicLinuxDemoRs")
    admission = STATE / "store.json"
    if admission.exists():
        assert json.loads(admission.read_text()) == record and (DATA / "WiredTiger").is_file(), "Original owned data store required"
    else:
        if DATA.exists():
            raise RuntimeError("Unadmitted Linux Mongo data exists; no adoption")
        durable(admission, record)
        DATA.mkdir()
    with socket.socket() as reservation:
        reservation.bind(("127.0.0.1", 27039))
    with socket.socket() as reservation:
        reservation.bind(("127.0.0.1", 5116))
    with (RESULTS / "linux-mongo-console.log").open("w") as output:
        process = subprocess.Popen([str(DAEMON), "--dbpath", str(DATA), "--bind_ip", "127.0.0.1", "--port", "27039",
                                    "--replSet", "cosmicLinuxDemoRs", "--wiredTigerCacheSizeGB", "0.25", "--oplogSize", "32",
                                    "--logpath", str(RESULTS / "linux-mongo-service.log"), "--logappend"], stdout=output, stderr=subprocess.STDOUT)
        try:
            deadline = time.monotonic() + 30
            while True:
                if process.poll() is not None:
                    raise RuntimeError("Owned Mongo exited before readiness; inspect native log")
                inodes = listen_inodes()
                if inodes:
                    owned = {os.readlink(path) for path in Path(f"/proc/{process.pid}/fd").iterdir()}
                    assert len(inodes) == 1 and "socket:[" + inodes[0] + "]" in owned, "Listener owner differs"
                    break
                if time.monotonic() >= deadline:
                    raise RuntimeError("Owned Mongo readiness deadline exceeded")
                time.sleep(0.2)
            context["env"].update(OASIS_COSMIC_LINUX_MONGO_STATE=str(STATE),
                OASIS_MONGO_REPLICA_SET_CONNECTION="mongodb://127.0.0.1:27039/?replicaSet=cosmicLinuxDemoRs&serverSelectionTimeoutMS=3000",
                OASIS_LITESTREAM_MONGO_SERVE="1" if options.serve else "0")
            context["test"]("LinuxMongoDemoEnvironmentTests", "linux-mongo-init.trx", "linux-mongo-init.log")
            context["test"]("LitestreamMongoCqlNativeTests", "litestream-mongo-federation.trx", "litestream-mongo-federation.log")
            assert not (context["STATE"] / "mongo.pending.json").exists(), "Owned fixture cleanup incomplete"
        finally:
            if process.poll() is None:
                process.terminate()
                process.wait(timeout=30)
            assert process.returncode == 0, "Owned Mongo failed to shut down cleanly; preserve store/log"
print("PASS: Linux Litestream/Mongo SDK and HTTP checks; temporary fixture removed, owned Mongo stopped")
