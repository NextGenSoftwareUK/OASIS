"""Native volatile-cache proof with explicit process-generation admission/retirement."""
import argparse
import sys
import fcntl
import hashlib
import json
import os
from pathlib import Path
import socket
import subprocess
import time
import uuid
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
STATE = Path("/var/tmp/oasis-memcached-cql")
RESULTS = ROOT / "TestResults/MemcachedCqlCurrent"
DAEMON = Path("/var/tmp/oasis-runtimes/memcached-1.6.40/usr/bin/memcached")
DIGEST = "a1d63f0865bafd0fb1c64022f90fd9be5a58a095fa4856ef56d50aa20cabfb6e"
DOTNET = "/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet"
RUNNER = "/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll"
ASSEMBLY = ROOT / "Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MemcachedOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MemcachedOASIS.IntegrationTests.dll"
assert hashlib.sha256(DAEMON.read_bytes()).hexdigest() == DIGEST
STATE.mkdir(exist_ok=True)
def durable(path, record):
    with path.open("x") as output:
        json.dump(record, output, indent=2)
        output.flush()
        os.fsync(output.fileno())
def stats():
    with socket.create_connection(("127.0.0.1", 11239), timeout=3) as client:
        client.sendall(b"stats\r\n")
        body = b""
        while not body.endswith(b"END\r\n"):
            block = client.recv(8192)
            if not block or len(body) + len(block) > 65536:
                raise RuntimeError("Incomplete/oversized native stats response")
            body += block
        return dict(line.decode().split()[1:3] for line in body.splitlines() if line.startswith(b"STAT "))
with (STATE / "runner.lock").open("a") as lock:
    fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
    pending = STATE / "active-generation.pending.json"
    for name in ["active-generation.pending.json", "active-generation-cql.json", "active-process.json"]:
        if (STATE / name).exists():
            raise RuntimeError("Original generation outcome pending; inspect without restart/reseed")
    # Preserve settled evidence before admitting a new volatile generation.
    for name in ["memcached-cql-native.trx", "mongo-init.trx", "memcached-mongo-federation.trx"]:
        previous = RESULTS / name
        if previous.exists():
            count = ET.parse(previous).find(".//{*}Counters").attrib
            assert count["total"] == count["passed"] == "1" and count["notExecuted"] == "0", "Preserve unresolved prior report"
            archive = RESULTS / "Archive"
            archive.mkdir(exist_ok=True)
            destination = archive / (hashlib.sha256(previous.read_bytes()).hexdigest().upper() + "-" + name)
            assert not destination.exists(), "Prior report already archived; reconcile explicitly"
            previous.rename(destination)
    for name in ["last-generation.json", "last-generation-cql.json", "last-process.json"]:
        previous = STATE / name
        if previous.exists():
            archive = STATE / "Archive"
            archive.mkdir(exist_ok=True)
            payload = previous.read_bytes()
            destination = archive / (hashlib.sha256(payload).hexdigest() + "-" + name)
            if destination.exists():
                assert destination.read_bytes() == payload, "Retired evidence archive differs"
            else:
                with destination.open("xb") as saved:
                    saved.write(payload); saved.flush(); os.fsync(saved.fileno())
    seed = STATE / "demo-seed.json"
    if not seed.exists():
        durable(seed, dict(Actor=str(uuid.uuid4()), Ids=[str(uuid.uuid4()) for _ in range(3)], DeletedId=str(uuid.uuid4())))
    with socket.socket() as reservation:
        reservation.bind(("127.0.0.1", 11239))
    generation = dict(generation=str(uuid.uuid4()), runtimeSha256=DIGEST, seedSha256=hashlib.sha256(seed.read_bytes()).hexdigest(),
                      port=11239, lifecycle="active-volatile", dataset="canonical synthetic fixture")
    durable(pending, generation)
    passed = False
    with (RESULTS / "native-service.log").open("w") as output:
        process = subprocess.Popen([str(DAEMON), "-l", "127.0.0.1", "-p", "11239", "-U", "0", "-m", "32", "-t", "1"], stdout=output, stderr=subprocess.STDOUT)
        try:
            durable(STATE / "active-process.json", dict(pid=process.pid, startTicks=Path(f"/proc/{process.pid}/stat").read_text().split()[21], runtimeSha256=DIGEST))
            deadline = time.monotonic() + 15
            while True:
                if process.poll() is not None:
                    raise RuntimeError("Owned cache process exited before readiness")
                listeners = [line.split() for line in Path("/proc/net/tcp").read_text().splitlines()[1:]
                             if line.split()[1].endswith(":" + format(11239, "04X")) and line.split()[3] == "0A"]
                if listeners:
                    owned = {os.readlink(path) for path in Path(f"/proc/{process.pid}/fd").iterdir()}
                    assert len(listeners) == 1 and listeners[0][1].startswith("0100007F:") and "socket:[" + listeners[0][9] + "]" in owned
                    break
                if time.monotonic() > deadline:
                    raise RuntimeError("Owned cache readiness deadline exceeded")
                time.sleep(0.1)
            assert stats()["curr_items"] == "0", "Initial owned cache must be empty, no flush/adoption"
            env = os.environ.copy()
            env.update(DOTNET_ROOT=str(Path(DOTNET).parent), OASIS_MEMCACHED_CQL_STATE=str(STATE))
            started = time.time()
            with (RESULTS / "native.log").open("w") as log:
                subprocess.run([DOTNET, RUNNER, str(ASSEMBLY), "--TestCaseFilter:FullyQualifiedName~MemcachedCqlNativeTests",
                                "--ResultsDirectory:" + str(RESULTS), "--Logger:trx;LogFileName=memcached-cql-native.trx"],
                               env=env, stdout=log, stderr=subprocess.STDOUT, check=True)
            report = RESULTS / "memcached-cql-native.trx"
            assert report.stat().st_mtime >= started
            count = ET.parse(report).find(".//{*}Counters").attrib
            assert count["passed"] == count["total"] == "1" and count["notExecuted"] == "0"
            assert stats()["curr_items"] == "5", "Only four records plus owned index may exist"
            assert (STATE / "active-generation-cql.json").is_file()
            if options.federated:
                if (STATE / "mongo.pending.json").exists():
                    raise RuntimeError("Original Mongo fixture pending; preserve without replay")
                with socket.socket() as reservation:
                    reservation.bind(("127.0.0.1", 5119))
                with owned_linux_mongo(RESULTS) as mongo_env:
                    env.update(mongo_env, OASIS_MEMCACHED_MONGO_SERVE="1" if options.serve else "0")
                    for name, filename in [("LinuxMongoDemoEnvironmentTests", "mongo-init"), ("MemcachedMongoCqlNativeTests", "memcached-mongo-federation")]:
                        started = time.time()
                        with (RESULTS / (filename + ".log")).open("w") as log:
                            subprocess.run([DOTNET, RUNNER, str(ASSEMBLY), "--TestCaseFilter:FullyQualifiedName~" + name,
                                            "--ResultsDirectory:" + str(RESULTS), "--Logger:trx;LogFileName=" + filename + ".trx"],
                                           env=env, stdout=log, stderr=subprocess.STDOUT, check=True)
                        report = RESULTS / (filename + ".trx")
                        assert report.stat().st_mtime >= started
                        count = ET.parse(report).find(".//{*}Counters").attrib
                        assert count["passed"] == count["total"] == "1" and count["notExecuted"] == "0"
                    assert not (STATE / "mongo.pending.json").exists(), "Owned Mongo fixture cleanup incomplete"
                assert stats()["curr_items"] == "5", "Pair must preserve only admitted native cache items"
            passed = True
        finally:
            if process.poll() is None:
                process.terminate()
                process.wait(timeout=15)
            if process.returncode != 0:
                raise RuntimeError("Owned cache termination failed; preserve generation/pid evidence")
        if passed:
            generation["lifecycle"] = "retired-clean; volatile data intentionally discarded after verified proof"
            generation["verifiedReportSha256"] = hashlib.sha256((RESULTS / "memcached-cql-native.trx").read_bytes()).hexdigest()
            with (STATE / "last-generation.json").open("w") as receipt:
                json.dump(generation, receipt, indent=2); receipt.flush(); os.fsync(receipt.fileno())
            os.replace(STATE / "active-generation-cql.json", STATE / "last-generation-cql.json")
            os.replace(STATE / "active-process.json", STATE / "last-process.json")
            pending.unlink()
print("PASS: native cache and requested paired proofs; owned Mongo stopped and volatile generation retired cleanly")
