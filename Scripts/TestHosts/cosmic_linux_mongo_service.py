"""Reuse the admitted loopback-only Linux Mongo environment; never provision/adopt here."""
from contextlib import contextmanager
import fcntl
import hashlib
import json
import os
from pathlib import Path
import socket
import subprocess
import time

@contextmanager
def owned_linux_mongo(results):
    state = Path("/var/tmp/oasis-cosmic-mongo")
    data = state / "data"
    daemon = Path("/var/tmp/oasis-runtimes/mongodb-8.3.13/mongod")
    digest = "6e449a554aca8ba29c91c6d640d47a6ed32c1856384925b707ca684c3743570c"
    assert hashlib.sha256(daemon.read_bytes()).hexdigest() == digest
    assert json.loads((state / "store.json").read_text()) == dict(data=str(data), runtimeSha256=digest, port=27039, replica="cosmicLinuxDemoRs")
    assert (data / "WiredTiger").is_file() and (state / "replica-init.json").is_file()
    assert not (state / "replica-init.pending.json").exists(), "Original replica admission pending"
    with (state / "environment.lock").open("a") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        with socket.socket() as reservation:
            reservation.bind(("127.0.0.1", 27039))
        with (results / "mongo-console.log").open("w") as output:
            process = subprocess.Popen([str(daemon), "--dbpath", str(data), "--bind_ip", "127.0.0.1", "--port", "27039",
                                        "--replSet", "cosmicLinuxDemoRs", "--wiredTigerCacheSizeGB", "0.25", "--oplogSize", "32",
                                        "--logpath", str(results / "mongo-service.log"), "--logappend"], stdout=output, stderr=subprocess.STDOUT)
            try:
                deadline = time.monotonic() + 30
                while True:
                    if process.poll() is not None:
                        raise RuntimeError("Owned Mongo exited before readiness")
                    listeners = [line.split() for line in Path("/proc/net/tcp").read_text().splitlines()[1:]
                                 if line.split()[1].endswith(":" + format(27039, "04X")) and line.split()[3] == "0A"]
                    if listeners:
                        owned = {os.readlink(path) for path in Path(f"/proc/{process.pid}/fd").iterdir()}
                        assert len(listeners) == 1 and listeners[0][1].startswith("0100007F:") and "socket:[" + listeners[0][9] + "]" in owned
                        break
                    if time.monotonic() > deadline:
                        raise RuntimeError("Owned Mongo readiness deadline exceeded")
                    time.sleep(0.2)
                yield dict(OASIS_COSMIC_LINUX_MONGO_STATE=str(state),
                    OASIS_MONGO_REPLICA_SET_CONNECTION="mongodb://127.0.0.1:27039/?replicaSet=cosmicLinuxDemoRs&serverSelectionTimeoutMS=3000")
            finally:
                if process.poll() is None:
                    process.terminate()
                    process.wait(timeout=30)
                assert process.returncode == 0, "Owned Mongo shutdown failed; preserve store/log"


@contextmanager
def isolated_ci_mongo():
    """Own one disposable replica set for an ephemeral Linux CI runner."""
    state = Path("/var/tmp/oasis-cosmic-mongo")
    state.mkdir(mode=0o700)
    container = subprocess.check_output([
        "docker", "create", "--publish", "127.0.0.1:27039:27039",
        "mongo:8.0.15", "--port", "27039", "--replSet", "cosmicLinuxDemoRs", "--bind_ip_all",
    ], text=True).strip()
    try:
        subprocess.run(["docker", "start", container], check=True)
        deadline = time.monotonic() + 45
        while True:
            if subprocess.check_output(["docker", "inspect", "--format", "{{.State.Running}}", container], text=True).strip() != "true":
                raise RuntimeError("Owned CI MongoDB exited before readiness")
            probe = subprocess.run([
                "docker", "exec", container, "mongosh", "--quiet", "--port", "27039",
                "--eval", "quit(db.adminCommand({ping:1}).ok === 1 ? 0 : 1)",
            ], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=5)
            if probe.returncode == 0:
                break
            if time.monotonic() >= deadline:
                raise RuntimeError("Owned CI MongoDB readiness deadline exceeded")
            time.sleep(1)
        (state / "store.json").write_text(json.dumps({"containerId": container, "port": 27039, "replica": "cosmicLinuxDemoRs"}))
        yield {
            "OASIS_COSMIC_LINUX_MONGO_STATE": str(state),
            "OASIS_MONGO_REPLICA_SET_CONNECTION": "mongodb://127.0.0.1:27039/?replicaSet=cosmicLinuxDemoRs&serverSelectionTimeoutMS=3000",
        }
    finally:
        subprocess.run(["docker", "rm", "-f", "-v", container], check=True)
