"""Own one persistent native Dragonfly source, verify it before/after restart, optionally pair Mongo."""
import argparse
import base64
import fcntl
import hashlib
import json
import os
from pathlib import Path
import secrets
import socket
import subprocess
import sys
import time
import xml.etree.ElementTree as ET

parser = argparse.ArgumentParser()
parser.add_argument("--federated", action="store_true")
parser.add_argument("--serve", action="store_true")
options = parser.parse_args()
if options.serve and not options.federated:
    raise RuntimeError("Serve requires explicit federation")
sys.dont_write_bytecode = True
from cosmic_linux_mongo_service import owned_linux_mongo

ROOT = Path("/mnt/c/Source/OASIS-cosmic")
STATE = Path("/var/tmp/oasis-dragonfly-cql")
RESULTS = ROOT / "TestResults/DragonflyCqlCurrent"
RUNTIME = Path("/var/tmp/oasis-runtimes/dragonfly-2.0.2")
DAEMON = RUNTIME / "dragonfly-x86_64"
DIGEST = "2cf7bea7e142b0360421ed5083e54ee49e0effc41f7ced5ebdcaf88f8ff2e116"
DOTNET = "/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet"
RUNNER = "/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll"
ASSEMBLY = ROOT / "Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.DragonflyOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.DragonflyOASIS.IntegrationTests.dll"
assert hashlib.sha256(DAEMON.read_bytes()).hexdigest() == DIGEST
assert not DAEMON.is_symlink()
STATE.mkdir(exist_ok=True, mode=0o700)
RESULTS.mkdir(exist_ok=True, parents=True)

def durable(path, record):
    with path.open("x") as output:
        json.dump(record, output, indent=2)
        output.flush(); os.fsync(output.fileno())

def private_file(path, value):
    descriptor = os.open(path, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(descriptor, "w") as output:
        output.write(value); output.flush(); os.fsync(output.fileno())

def report(test, filename, env):
    started = time.time()
    with (RESULTS / (filename + ".log")).open("w") as output:
        subprocess.run([DOTNET, RUNNER, str(ASSEMBLY), "--TestCaseFilter:FullyQualifiedName~" + test,
                        "--ResultsDirectory:" + str(RESULTS), "--Logger:trx;LogFileName=" + filename + ".trx"],
                       env=env, stdout=output, stderr=subprocess.STDOUT, check=True)
    path = RESULTS / (filename + ".trx")
    assert path.stat().st_mtime >= started
    count = ET.parse(path).find(".//{*}Counters").attrib
    assert count["passed"] == count["total"] == "1" and count["notExecuted"] == "0"
    return hashlib.sha256(path.read_bytes()).hexdigest()

with (STATE / "runner.lock").open("a") as lease:
    fcntl.flock(lease, fcntl.LOCK_EX | fcntl.LOCK_NB)
    for name in ["runtime-setup.pending.json", "runner.pending.json", "active-process.json", "demo-seed.pending.json", "checkpoint.pending.json", "mongo.pending.json"]:
        if (STATE / name).exists():
            raise RuntimeError("Unresolved original outcome: " + name + "; inspect without replay/reseed")
    # Preserve completed integration evidence before starting the owned source.
    for name in ["dragonfly-cql-native.trx", "dragonfly-checkpoint-native.trx", "dragonfly-cql-restart.trx", "dragonfly-checkpoint-restart.trx", "mongo-init.trx", "dragonfly-mongo-federation.trx"]:
        previous = RESULTS / name
        if previous.exists():
            count = ET.parse(previous).find(".//{*}Counters").attrib
            assert count["total"] == count["passed"] == "1" and count["notExecuted"] == "0", "Preserve unresolved prior report"
            archive = RESULTS / "Archive"
            archive.mkdir(exist_ok=True)
            destination = archive / (hashlib.sha256(previous.read_bytes()).hexdigest().upper() + "-" + name)
            assert not destination.exists(), "Prior report already archived; reconcile explicitly"
            previous.rename(destination)
    previous = STATE / "last-verification.json"
    if previous.exists():
        payload = previous.read_bytes()
        archive = STATE / "Archive"
        archive.mkdir(exist_ok=True)
        destination = archive / (hashlib.sha256(payload).hexdigest() + "-last-verification.json")
        if destination.exists():
            assert destination.read_bytes() == payload, "Previous verification archive differs"
        else:
            with destination.open("xb") as saved:
                saved.write(payload); saved.flush(); os.fsync(saved.fileno())
    store = STATE / "store.json"
    if not store.exists():
        assert set(p.name for p in STATE.iterdir()) == {"runner.lock"}, "Never adopt an unknown source directory"
        durable(STATE / "runtime-setup.pending.json", dict(runtimeSha256=DIGEST, port=16391))
        data = STATE / "data"
        data.mkdir(mode=0o700)
        private_file(STATE / "native-password", secrets.token_hex(32))
        private_file(STATE / "storage-key", secrets.token_hex(32))
        config = "\n".join([
            "--bind=127.0.0.1", "--port=16391", "--admin_port=0", "--proactor_threads=1",
            "--requirepass=" + (STATE / "native-password").read_text(),
            "--dir=" + str(data), "--dbfilename=source.rdb", "--df_snapshot_format=false",
            "--maxmemory=256mb", "--cache_mode=false", "--epoll_file_threads=1",
            "--version_check=false", "--logtostderr=true", ""
        ])
        private_file(STATE / "dragonfly.conf", config)
        durable(store, dict(runtimeSha256=DIGEST, port=16391, database=0, data=str(data),
                            files={name: hashlib.sha256((STATE / name).read_bytes()).hexdigest()
                                   for name in ["dragonfly.conf", "native-password", "storage-key"]}))
        (STATE / "runtime-setup.pending.json").unlink()
    admitted = json.loads(store.read_text())
    assert admitted["runtimeSha256"] == DIGEST and admitted["port"] == 16391 and admitted["database"] == 0
    assert admitted["data"] == str(STATE / "data") and (STATE / "data").is_dir()
    for name, digest in admitted["files"].items():
        assert hashlib.sha256((STATE / name).read_bytes()).hexdigest() == digest
        assert (STATE / name).stat().st_mode & 0o077 == 0
    if (STATE / "demo-seed.json").exists():
        assert (STATE / "data/source.rdb").is_file() and (STATE / "checkpoint.json").is_file() and (STATE / "source-snapshot.json").exists(), "Original native source missing; never recreate"
        stopped = json.loads((STATE / "last-native-snapshot.json").read_text())
        assert stopped["runtimeSha256"] == DIGEST
        assert stopped["fileSha256"] == hashlib.sha256((STATE / "data/source.rdb").read_bytes()).hexdigest()
        assert stopped["sourceSnapshotSha256"] == hashlib.sha256((STATE / "source-snapshot.json").read_bytes()).hexdigest()
    env = os.environ.copy()
    env.update(DOTNET_ROOT=str(Path(DOTNET).parent), OASIS_DRAGONFLY_CQL_STATE=str(STATE),
               OASIS_DRAGONFLY_NATIVE_PASSWORD=(STATE / "native-password").read_text(),
               OASIS_DRAGONFLY_STORAGE_ENCRYPTION_KEY=base64.b64encode(bytes.fromhex((STATE / "storage-key").read_text())).decode("ascii"))
    durable(STATE / "runner.pending.json", dict(runtimeSha256=DIGEST, source=str(STATE), stage="native then clean restart"))
    results = {}
    for stage in ["native", "restart"]:
        with socket.socket() as reservation:
            reservation.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            reservation.bind(("127.0.0.1", 16391))
        startup_digest = hashlib.sha256((STATE / "data/source.rdb").read_bytes()).hexdigest() if (STATE / "data/source.rdb").exists() else None
        stage_verified = False
        with (RESULTS / (stage + "-service.log")).open("w") as output:
            process = subprocess.Popen([str(DAEMON), "--flagfile=" + str(STATE / "dragonfly.conf")], stdout=output, stderr=subprocess.STDOUT)
            try:
                durable(STATE / "active-process.json", dict(pid=process.pid, startTicks=Path(f"/proc/{process.pid}/stat").read_text().split()[21], runtimeSha256=DIGEST, startupSnapshotSha256=startup_digest))
                deadline = time.monotonic() + 15
                while True:
                    if process.poll() is not None:
                        raise RuntimeError("Owned native Dragonfly exited before readiness")
                    listeners = [line.split() for line in Path("/proc/net/tcp").read_text().splitlines()[1:]
                                 if line.split()[1].endswith(":" + format(16391, "04X")) and line.split()[3] == "0A"]
                    if listeners:
                        owned = {os.readlink(path) for path in Path(f"/proc/{process.pid}/fd").iterdir()}
                        assert len(listeners) == 1 and listeners[0][1].startswith("0100007F:") and "socket:[" + listeners[0][9] + "]" in owned
                        break
                    if time.monotonic() > deadline:
                        raise RuntimeError("Owned Dragonfly readiness deadline exceeded")
                    time.sleep(0.1)
                results[stage] = report("DragonflyCqlNativeTests.NativeEncryptedDragonfly", "dragonfly-cql-" + stage, env)
                results[stage + "-checkpoint"] = report("DragonflySnapshotTests", "dragonfly-checkpoint-" + stage, env)
                if stage == "restart" and options.federated:
                    if options.serve:
                        with socket.socket() as reservation:
                            # Match normal server restart semantics without accepting a live listener.
                            reservation.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
                            reservation.bind(("127.0.0.1", 5122))
                    with owned_linux_mongo(RESULTS) as mongo_env:
                        pair_env = dict(env, **mongo_env, OASIS_DRAGONFLY_MONGO_SERVE="1" if options.serve else "0")
                        results["mongo-init"] = report("LinuxMongoDemoEnvironmentTests", "mongo-init", pair_env)
                        results["federation"] = report("DragonflyMongoCqlNativeTests", "dragonfly-mongo-federation", pair_env)
                        assert not (STATE / "mongo.pending.json").exists()
                stage_verified = True
            finally:
                if process.poll() is None:
                    process.terminate()
                    process.wait(timeout=20)
                if process.returncode != 0:
                    raise RuntimeError("Owned Dragonfly shutdown failed; preserve process/source evidence")
            assert stage_verified
            # SIGTERM performs a native snapshot, so its metadata may change.
            # Admit only a clean stop of the process whose complete logical source
            # was verified, and bind exact resulting file bytes for the next start.
            record = dict(runtimeSha256=DIGEST, stage=stage, pid=process.pid,
                          fileSha256=hashlib.sha256((STATE / "data/source.rdb").read_bytes()).hexdigest(),
                          sourceSnapshotSha256=hashlib.sha256((STATE / "source-snapshot.json").read_bytes()).hexdigest(),
                          reports=dict(results), stoppedCleanly=True)
            temporary = STATE / "last-native-snapshot.complete.json"
            durable(temporary, record)
            os.replace(temporary, STATE / "last-native-snapshot.json")
            (STATE / "active-process.json").unlink()
    # No write replay or source replacement occurred during the restart.
    with (STATE / "last-verification.json").open("w") as receipt:
        json.dump(dict(runtimeSha256=DIGEST, reports=results, stoppedCleanly=True), receipt, indent=2)
        receipt.flush(); os.fsync(receipt.fileno())
    (STATE / "runner.pending.json").unlink()
print("PASS: independently identified native Dragonfly, encrypted queries and clean restart; original source retained")
