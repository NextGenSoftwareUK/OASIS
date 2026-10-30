"""Isolated empty native IO admission; never opens/replays the failed source."""
import fcntl
import hashlib
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
sys.dont_write_bytecode = True
from garnet_fixture_lifecycle import stop_owned_garnet

ROOT = Path("/mnt/c/Source/OASIS-cosmic")
ORIGINAL = Path("/var/tmp/oasis-garnet-cql")
PROBE = Path("/var/tmp/oasis-garnet-storage-probe")
RESULTS = ROOT / "TestResults/GarnetCqlCurrent"
DAEMON = Path("/var/tmp/oasis-runtimes/garnet-2.2.5/net10.0/GarnetServer")
DIGEST = "4e38420e097c7c72c5cba0a9a8fa864ced165d48f5ec7ad79387e0b5ac2b3750"
DOTNET = "/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet"
RUNNER = "/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll"
ASSEMBLY = ROOT / "Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests.dll"

def fingerprint(path):
    return {str(file.relative_to(path)): hashlib.sha256(file.read_bytes()).hexdigest()
            for file in path.rglob("*") if file.is_file()}

def durable(path, record):
    with path.open("x") as output:
        json.dump(record, output, indent=2); output.flush(); os.fsync(output.fileno())

assert hashlib.sha256(DAEMON.read_bytes()).hexdigest() == DIGEST
if PROBE.exists():
    raise RuntimeError("Original probe exists; inspect its outcome, never overwrite or restart automatically")
with (ORIGINAL / "runner.lock").open("a") as original_lease:
    fcntl.flock(original_lease, fcntl.LOCK_EX | fcntl.LOCK_NB)
    before = fingerprint(ORIGINAL)
    PROBE.mkdir(mode=0o700)
    (PROBE / "data").mkdir(mode=0o700)
    config = json.loads((ORIGINAL / "garnet.conf").read_text())
    assert config["EnableStorageTier"] is False and "LogDir" not in config
    config.update(Port=16492, CheckpointDir=str(PROBE / "data/checkpoints"))
    descriptor = os.open(PROBE / "garnet.conf", os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
    with os.fdopen(descriptor, "w") as output:
        json.dump(config, output, indent=2); output.flush(); os.fsync(output.fileno())
    durable(PROBE / "probe.pending.json", dict(runtimeSha256=DIGEST, port=16492, purpose="empty native checkpoint only", originalFiles=before))
    with socket.socket() as port:
        port.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        port.bind(("127.0.0.1", 16492))
    env = dict(os.environ, DOTNET_ROOT=str(Path(DOTNET).parent),
               OASIS_GARNET_STORAGE_PROBE_STATE=str(PROBE),
               OASIS_GARNET_NATIVE_PASSWORD=(ORIGINAL / "native-password").read_text())
    passed = False
    with (RESULTS / "empty-checkpoint-service.log").open("w") as log:
        process = subprocess.Popen([str(DAEMON), "--config-import-path", str(PROBE / "garnet.conf")],
                                   env=env, cwd=str(PROBE / "data"), stdout=log, stderr=subprocess.STDOUT)
        try:
            durable(PROBE / "active-process.json", dict(pid=process.pid, startTicks=Path(f"/proc/{process.pid}/stat").read_text().split()[21], runtimeSha256=DIGEST))
            deadline = time.monotonic() + 15
            while True:
                if process.poll() is not None:
                    raise RuntimeError("Owned empty-probe server exited before readiness")
                listeners = [line.split() for line in Path("/proc/net/tcp").read_text().splitlines()[1:]
                             if line.split()[1].endswith(":" + format(16492, "04X")) and line.split()[3] == "0A"]
                if listeners:
                    owned = {os.readlink(path) for path in Path(f"/proc/{process.pid}/fd").iterdir()}
                    assert len(listeners) == 1 and listeners[0][1].startswith("0100007F:") and "socket:[" + listeners[0][9] + "]" in owned
                    break
                if time.monotonic() > deadline:
                    raise RuntimeError("Owned empty-probe readiness deadline exceeded")
                time.sleep(0.1)
            started = time.time()
            with (RESULTS / "empty-checkpoint.log").open("w") as output:
                subprocess.run([DOTNET, RUNNER, str(ASSEMBLY), "--TestCaseFilter:FullyQualifiedName~GarnetEmptyCheckpointTests",
                                "--ResultsDirectory:" + str(RESULTS), "--Logger:trx;LogFileName=garnet-empty-checkpoint.trx"],
                               env=env, stdout=output, stderr=subprocess.STDOUT, check=True)
            report = RESULTS / "garnet-empty-checkpoint.trx"
            assert report.stat().st_mtime >= started
            count = ET.parse(report).find(".//{*}Counters").attrib
            assert count["total"] == count["passed"] == "1" and count["notExecuted"] == "0"
            assert json.loads((PROBE / "empty-checkpoint.json").read_text())["emptyBeforeAndAfter"] is True
            passed = True
        finally:
            # This dedicated probe performs no business-record writes. There is
            # no uncheckpointed holon source to discard during teardown.
            stop_owned_garnet(PROBE, process)
    assert fingerprint(ORIGINAL) == before, "Original failed fixture changed; preserve both environments"
    if passed:
        evidence = json.loads((PROBE / "empty-checkpoint.json").read_text())
        evidence.update(runtimeSha256=DIGEST, originalFilesUnchanged=True,
                        reportSha256=hashlib.sha256((RESULTS / "garnet-empty-checkpoint.trx").read_bytes()).hexdigest(),
                        stoppedExitCode=process.returncode)
        durable(PROBE / "empty-checkpoint-admission.json", evidence)
        # Empty DB was verified on both sides of SAVE, and the native process
        # has stopped. Remove only its exact owned checkpoint tree and config.
        data = PROBE / "data"
        assert data.resolve() == Path("/var/tmp/oasis-garnet-storage-probe/data")
        assert PROBE.resolve() == Path("/var/tmp/oasis-garnet-storage-probe")
        observed = {str(file.relative_to(data / "checkpoints")): hashlib.sha256(file.read_bytes()).hexdigest().upper()
                    for file in (data / "checkpoints").rglob("*") if file.is_file()}
        assert observed == evidence["files"]
        assert all(file.is_relative_to(data / "checkpoints") for file in data.rglob("*") if file.is_file()), "Unexplained files outside native checkpoint directory"
        shutil.rmtree(data)
        (PROBE / "garnet.conf").unlink()
        os.replace(PROBE / "active-process.json", PROBE / "last-process.json")
        (PROBE / "probe.pending.json").unlink()
print("PASS: empty native checkpoint acknowledged; owned process stopped, empty artifacts cleaned, original fixture unchanged")
