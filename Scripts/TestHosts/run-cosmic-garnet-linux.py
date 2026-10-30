"""Own one persistent native Garnet source, verify it before/after restart, optionally pair Mongo."""
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
options = parser.parse_args()
sys.dont_write_bytecode = True
from garnet_fixture_lifecycle import stop_owned_garnet

ROOT = Path("/mnt/c/Source/OASIS-cosmic")
STATE = Path("/var/tmp/oasis-garnet-cql")
RESULTS = ROOT / "TestResults/GarnetCqlCurrent"
RUNTIME = Path("/var/tmp/oasis-runtimes/garnet-2.2.5")
DAEMON = RUNTIME / "net10.0/GarnetServer"
DIGEST = "4e38420e097c7c72c5cba0a9a8fa864ced165d48f5ec7ad79387e0b5ac2b3750"
DOTNET = "/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet"
RUNNER = "/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll"
ASSEMBLY = ROOT / "Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests.dll"
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

def checkpoint_files():
    directory = STATE / "data/checkpoints"
    result = {str(path.relative_to(directory)): hashlib.sha256(path.read_bytes()).hexdigest()
              for path in directory.rglob("*") if path.is_file()}
    assert result, "Original native checkpoint must not be empty"
    return result

with (STATE / "runner.lock").open("a") as lease:
    fcntl.flock(lease, fcntl.LOCK_EX | fcntl.LOCK_NB)
    for name in ["runtime-setup.pending.json", "runner.pending.json", "active-process.json", "demo-seed.pending.json", "checkpoint.pending.json", "mongo.pending.json"]:
        if (STATE / name).exists():
            raise RuntimeError("Unresolved original outcome: " + name + "; inspect without replay/reseed")
    store = STATE / "store.json"
    if not store.exists():
        assert set(p.name for p in STATE.iterdir()) == {"runner.lock"}, "Never adopt an unknown source directory"
        durable(STATE / "runtime-setup.pending.json", dict(runtimeSha256=DIGEST, port=16392))
        data = STATE / "data"
        data.mkdir(mode=0o700)
        private_file(STATE / "native-password", secrets.token_hex(32))
        private_file(STATE / "storage-key", secrets.token_hex(32))
        config = json.dumps(dict(Port=16392, Address="127.0.0.1", LogMemorySize="32m", PageSize="512k",
                                 IndexMemorySize="1m", DisableObjects=True, DisablePubSub=True,
                                 AuthenticationMode="Password", Password=(STATE / "native-password").read_text(),
                                 EnableLua=True, LuaTransactionMode=True, Recover=True, FailOnRecoveryError=True,
                                 EnableStorageTier=False, CheckpointDir=str(data / "checkpoints")), indent=2)
        private_file(STATE / "garnet.conf", config)
        durable(store, dict(runtimeSha256=DIGEST, port=16392, database=0, data=str(data),
                            files={name: hashlib.sha256((STATE / name).read_bytes()).hexdigest()
                                   for name in ["garnet.conf", "native-password", "storage-key"]}))
        (STATE / "runtime-setup.pending.json").unlink()
    admitted = json.loads(store.read_text())
    assert admitted["runtimeSha256"] == DIGEST and admitted["port"] == 16392 and admitted["database"] == 0
    assert admitted["data"] == str(STATE / "data") and (STATE / "data").is_dir()
    for name, digest in admitted["files"].items():
        assert hashlib.sha256((STATE / name).read_bytes()).hexdigest() == digest
        assert (STATE / name).stat().st_mode & 0o077 == 0
    if (STATE / "demo-seed.json").exists():
        assert (STATE / "checkpoint.json").is_file() and (STATE / "source-snapshot.json").exists(), "Original native checkpoint/source missing; never recreate"
        admitted_files = json.loads((STATE / "last-checkpoint-files.json").read_text())
        assert checkpoint_files() == admitted_files, "Original native checkpoint files changed; preserve without replay"
    env = os.environ.copy()
    env.update(DOTNET_ROOT=str(Path(DOTNET).parent), OASIS_GARNET_CQL_STATE=str(STATE),
               OASIS_GARNET_NATIVE_PASSWORD=(STATE / "native-password").read_text(),
               OASIS_GARNET_STORAGE_ENCRYPTION_KEY=base64.b64encode(bytes.fromhex((STATE / "storage-key").read_text())).decode("ascii"))
    durable(STATE / "runner.pending.json", dict(runtimeSha256=DIGEST, source=str(STATE), stage="native then clean restart"))
    results = {}
    for stage in ["native", "restart"]:
        with socket.socket() as reservation:
            reservation.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            reservation.bind(("127.0.0.1", 16392))
        with (RESULTS / (stage + "-service.log")).open("w") as output:
            process = subprocess.Popen([str(DAEMON), "--config-import-path", str(STATE / "garnet.conf")], env=env, cwd=str(STATE / "data"), stdout=output, stderr=subprocess.STDOUT)
            try:
                durable(STATE / "active-process.json", dict(pid=process.pid, startTicks=Path(f"/proc/{process.pid}/stat").read_text().split()[21], runtimeSha256=DIGEST))
                deadline = time.monotonic() + 15
                while True:
                    if process.poll() is not None:
                        raise RuntimeError("Owned native Garnet exited before readiness")
                    listeners = [line.split() for line in Path("/proc/net/tcp").read_text().splitlines()[1:]
                                 if line.split()[1].endswith(":" + format(16392, "04X")) and line.split()[3] == "0A"]
                    if listeners:
                        owned = {os.readlink(path) for path in Path(f"/proc/{process.pid}/fd").iterdir()}
                        assert len(listeners) == 1 and listeners[0][1].startswith("0100007F:") and "socket:[" + listeners[0][9] + "]" in owned
                        break
                    if time.monotonic() > deadline:
                        raise RuntimeError("Owned Garnet readiness deadline exceeded")
                    time.sleep(0.1)
                results[stage] = report("GarnetCqlNativeTests.NativeEncryptedGarnet", "garnet-cql-" + stage, env)
            finally:
                stop_owned_garnet(STATE, process)
            assert (STATE / "checkpoint.json").is_file()
            temporary = STATE / "last-checkpoint-files.complete.json"
            durable(temporary, checkpoint_files())
            os.replace(temporary, STATE / "last-checkpoint-files.json")
            (STATE / "active-process.json").unlink()
    # No write replay or source replacement occurred during the restart.
    with (STATE / "last-verification.json").open("w") as receipt:
        json.dump(dict(runtimeSha256=DIGEST, reports=results, stoppedCleanly=True), receipt, indent=2)
        receipt.flush(); os.fsync(receipt.fileno())
    (STATE / "runner.pending.json").unlink()
print("PASS: independently identified native Garnet, encrypted queries and checkpoint restart; original source retained")
