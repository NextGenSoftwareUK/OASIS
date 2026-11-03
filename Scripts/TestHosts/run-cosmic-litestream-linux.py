"""Actual Linux provider + live Litestream replication, preserving original lineage."""
import fcntl
import hashlib
import json
import os
from pathlib import Path
import shlex
import subprocess
import time
import xml.etree.ElementTree as ET

ROOT = Path("/mnt/c/Source/OASIS-cosmic")
STATE = Path("/var/tmp/oasis-litestream-cql")
RESULTS = ROOT / "TestResults/LitestreamCqlCurrent"
DOTNET = Path("/var/tmp/oasis-runtimes/dotnet-10.0.12/dotnet")
SIDECAR = Path("/var/tmp/oasis-runtimes/litestream-0.5.17/litestream")
RUNNER = Path("/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll")
ASSEMBLY = ROOT / "Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.LitestreamOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.LitestreamOASIS.IntegrationTests.dll"
assert hashlib.sha256(SIDECAR.read_bytes()).hexdigest() == "200e4248a4cc83da2ca52babe66774f1a71afa993d31373adb0ce0fe12154647"
assert hashlib.sha256(DOTNET.read_bytes()).hexdigest() == "01d89e0a0191052bfea616cd4ce624c8faf13b05bbddf7f64499c23e2a9d9269"
assert (STATE / "source-import.json").is_file()
assert not (STATE / "source-import.pending.json").exists()
env = os.environ.copy()
env.update(DOTNET_ROOT=str(DOTNET.parent), PATH=str(DOTNET.parent) + os.pathsep + env.get("PATH", ""),
           OASIS_LITESTREAM_CQL_STATE=str(STATE), OASIS_LITESTREAM_CQL_CONNECTION="Data Source=" + str(STATE / "source.db"),
           OASIS_LITESTREAM_REPLICATION_EXEC="1")
def test_args(name, report):
    return [str(DOTNET), str(RUNNER), str(ASSEMBLY), "--TestCaseFilter:FullyQualifiedName~" + name,
            "--ResultsDirectory:" + str(RESULTS), "--Logger:trx;LogFileName=" + report]
def run(args, log):
    with (RESULTS / log).open("w") as output:
        subprocess.run(args, env=env, stdout=output, stderr=subprocess.STDOUT, check=True)
def counters(report, started):
    path = RESULTS / report
    assert path.stat().st_mtime >= started, "Fresh native test report required"
    count = ET.parse(path).find(".//{*}Counters").attrib
    assert count["total"] == "1" and count["passed"] == "1" and count["notExecuted"] == "0", "Exactly one pass without skips required"
def test(name, report, log):
    # Preserve settled evidence; unresolved outcomes must be reconciled first.
    previous = RESULTS / report
    if previous.exists():
        count = ET.parse(previous).find(".//{*}Counters").attrib
        assert count["total"] == "1" and count["passed"] == "1" and count["notExecuted"] == "0", "Preserve unresolved prior report"
        archive = RESULTS / "Archive"
        archive.mkdir(exist_ok=True)
        destination = archive / (hashlib.sha256(previous.read_bytes()).hexdigest().upper() + "-" + report)
        assert not destination.exists(), "Prior report already archived; reconcile explicitly"
        previous.rename(destination)
    started = time.time()
    run(test_args(name, report), log)
    counters(report, started)
def durable(path, value):
    with path.open("x") as output:
        json.dump(value, output, indent=2)
        output.flush()
        os.fsync(output.fileno())
with (STATE / "runner.lock").open("a") as lock:
    fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
    pending = STATE / "replication.pending.json"
    receipt = STATE / "replication.json"
    replica = STATE / "file-replica"
    restored = STATE / "restored.db"
    if pending.exists():
        raise RuntimeError("Original Linux replication outcome pending; preserve without replay")
    test("LitestreamCqlNativeTests", "litestream-linux-native.trx", "linux-native.log")
    if not receipt.exists():
        for path in [replica, restored, STATE / "replication-probe.json", STATE / "replication-probe.pending.json"]:
            if path.exists():
                raise RuntimeError("Unadmitted native replica/probe exists; preserve original")
        durable(pending, dict(source=str(STATE / "source.db"), replica=str(replica), restored=str(restored), runtime="0.5.17"))
        probe_started = time.time()
        run([str(SIDECAR), "replicate", "-exec", shlex.join(test_args("WriteOwnedProbeDuringNativeReplication", "litestream-linux-probe.trx")),
             str(STATE / "source.db"), replica.as_uri()], "linux-replication.log")
        counters("litestream-linux-probe.trx", probe_started)
        log = (RESULTS / "linux-replication.log").read_text()
        if "level=ERROR" in log or "shutdown sync failed" in log:
            raise RuntimeError("Native daemon reported errors; do not accept child success as durable replication")
        assert (STATE / "replication-probe.json").is_file()
        run([str(SIDECAR), "restore", "-o", str(restored), replica.as_uri()], "linux-restore.log")
    else:
        assert replica.is_dir() and restored.is_file(), "Original replica/restore missing; never recreate"
    test("RestoredReplicaPreservesOriginalHolonsAndLiveProbe", "litestream-linux-restore.trx", "linux-restore-test.log")
    if pending.exists():
        pending.rename(receipt)
        fd = os.open(STATE, os.O_RDONLY | os.O_DIRECTORY)
        try:
            os.fsync(fd)
        finally:
            os.close(fd)
print("PASS: actual Linux provider/native source, live LTX probe, clean daemon exit and native restore/CQL; original lineage preserved")
