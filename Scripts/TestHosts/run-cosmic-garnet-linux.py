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
parser.add_argument("--fixture-generation", type=int, choices=[2])
parser.add_argument("--federated", action="store_true")
parser.add_argument("--serve", action="store_true")
options = parser.parse_args()
if options.serve and not options.federated or options.federated and options.fixture_generation != 2:
    raise RuntimeError("Paired/interactive preset requires explicitly admitted generation2 and federation")
sys.dont_write_bytecode = True
from garnet_fixture_lifecycle import stop_owned_garnet
from cosmic_linux_mongo_service import owned_linux_mongo

ROOT = Path("/mnt/c/Source/OASIS-cosmic")
ORIGINAL = Path("/var/tmp/oasis-garnet-cql")
STATE = ORIGINAL / "generations/2" if options.fixture_generation == 2 else ORIGINAL
RESULTS = ROOT / "TestResults/GarnetCqlCurrent"
RUNTIME = Path("/var/tmp/oasis-runtimes/garnet-2.2.5")
DAEMON = RUNTIME / "net10.0/GarnetServer"
DIGEST = "4e38420e097c7c72c5cba0a9a8fa864ced165d48f5ec7ad79387e0b5ac2b3750"
DOTNET = "/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet"
RUNNER = "/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll"
ASSEMBLY = ROOT / "Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests.dll"
assert hashlib.sha256(DAEMON.read_bytes()).hexdigest() == DIGEST
assert not DAEMON.is_symlink()
assert (ORIGINAL / "store.json").is_file(), "Original failed fixture evidence must remain; never recreate it"
assert STATE.is_dir(), "Explicit fixture admission is required"
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
    if options.fixture_generation == 2:
        record = json.loads((STATE / "fixture-admission.json").read_text())
        original = {str(path.relative_to(ORIGINAL)): hashlib.sha256(path.read_bytes()).hexdigest()
                    for path in ORIGINAL.rglob("*") if path.is_file() and path.relative_to(ORIGINAL).parts[0] != "generations"}
        assert record["generation"] == 2 and record["runtimeSha256"] == DIGEST
        assert record["originalFiles"] == original, "Original failed source changed; preserve without replay"
        assert record["seedSha256"] == hashlib.sha256((STATE / "admitted-seed.json").read_bytes()).hexdigest()
    admitted = json.loads(store.read_text())
    assert admitted["runtimeSha256"] == DIGEST and admitted["port"] == 16392 and admitted["database"] == 0
    assert admitted["data"] == str(STATE / "data") and (STATE / "data").is_dir()
    for name, digest in admitted["files"].items():
        assert hashlib.sha256((STATE / name).read_bytes()).hexdigest() == digest
        assert (STATE / name).stat().st_mode & 0o077 == 0
    credential_root = Path(admitted.get("credentialRoot", str(ORIGINAL)))
    assert credential_root == ORIGINAL
    for name, digest in admitted.get("credentialFiles", {}).items():
        assert name in ["native-password", "storage-key"]
        assert hashlib.sha256((credential_root / name).read_bytes()).hexdigest() == digest
        assert (credential_root / name).stat().st_mode & 0o077 == 0
    if (STATE / "demo-seed.json").exists():
        assert (STATE / "checkpoint.json").is_file() and (STATE / "source-snapshot.json").exists(), "Original native checkpoint/source missing; never recreate"
        admitted_files = json.loads((STATE / "last-checkpoint-files.json").read_text())
        assert checkpoint_files() == admitted_files, "Original native checkpoint files changed; preserve without replay"
    env = os.environ.copy()
    env.update(DOTNET_ROOT=str(Path(DOTNET).parent), OASIS_GARNET_CQL_STATE=str(STATE), OASIS_GARNET_FIXTURE_GENERATION=str(options.fixture_generation or 1),
               OASIS_GARNET_NATIVE_PASSWORD=(credential_root / "native-password").read_text(),
               OASIS_GARNET_STORAGE_ENCRYPTION_KEY=base64.b64encode(bytes.fromhex((credential_root / "storage-key").read_text())).decode("ascii"))
    durable(STATE / "runner.pending.json", dict(runtimeSha256=DIGEST, source=str(STATE), stage="native then clean restart"))
    results = {}
    exit_codes = {}
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
                results[stage] = report("GarnetCqlNativeTests.NativeEncryptedGarnet", ("garnet-g2-cql-" if options.fixture_generation == 2 else "garnet-cql-") + stage, env)
                if stage == "restart" and options.federated:
                    if options.serve:
                        with socket.socket() as port:
                            port.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
                            port.bind(("127.0.0.1", 5121))
                    with owned_linux_mongo(RESULTS) as mongo_env:
                        pair_env = dict(env, **mongo_env, OASIS_GARNET_MONGO_SERVE="1" if options.serve else "0")
                        results["mongo-init"] = report("LinuxMongoDemoEnvironmentTests", "garnet-g2-mongo-init", pair_env)
                        results["federation"] = report("GarnetMongoCqlNativeTests", "garnet-g2-mongo-federation", pair_env)
                        assert not (STATE / "mongo.pending.json").exists()
            finally:
                stop_owned_garnet(STATE, process)
            exit_codes[stage] = process.returncode
            assert (STATE / "checkpoint.json").is_file()
            temporary = STATE / "last-checkpoint-files.complete.json"
            durable(temporary, checkpoint_files())
            os.replace(temporary, STATE / "last-checkpoint-files.json")
            (STATE / "active-process.json").unlink()
    # No write replay or source replacement occurred during the restart.
    with (STATE / "last-verification.json").open("w") as receipt:
        json.dump(dict(runtimeSha256=DIGEST, reports=results, stoppedAfterCheckpoint=True, nativeExitCodes=exit_codes), receipt, indent=2)
        receipt.flush(); os.fsync(receipt.fileno())
    (STATE / "runner.pending.json").unlink()
print("PASS: independently identified native Garnet, encrypted queries and checkpoint restart; original source retained")
