"""Verify native replica initialization in the existing owned Mongo environment, without provisioning."""
from pathlib import Path
import hashlib
import json
import os
import signal
import subprocess
import time
import xml.etree.ElementTree as ET
from cosmic_linux_mongo_service import owned_linux_mongo

ROOT = Path(__file__).resolve().parents[2]
RESULTS = ROOT / "TestResults/Web4SandboxCqlCurrent"
DOTNET_ROOT = Path("/var/tmp/oasis-runtimes/dotnet-10.0.12")
VSTEST = Path("/mnt/c/Users/david/.nuget/packages/microsoft.testplatform.portable/18.11.0/tools/net8.0/vstest.console.dll")
DLL = ROOT / "Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests.dll"


def main():
    assert os.geteuid() != 0, "Reuse the admitted source owner, not root"
    assert (DOTNET_ROOT / "dotnet").is_file() and VSTEST.is_file() and DLL.is_file()
    RESULTS.mkdir(parents=True, exist_ok=True)
    started = time.time()
    with owned_linux_mongo(RESULTS) as environment:
        with (RESULTS / "mongo-replica-initialization.log").open("w") as log:
            process = subprocess.Popen([
                str(DOTNET_ROOT / "dotnet"), str(VSTEST), str(DLL),
                "--TestCaseFilter:FullyQualifiedName~MongoReplicaInitializationTests|FullyQualifiedName~MongoReceiptMembershipMigrationTests|FullyQualifiedName~NativePrimaryOutboxCommitsGraphReceiptAndEveryFrozenTargetTogether|FullyQualifiedName~MongoBaselineCompletionTests",
                "--ResultsDirectory:" + str(RESULTS), "--Logger:trx;LogFileName=mongo-replica-initialization.trx"
            ], cwd=RESULTS, env={**os.environ, **environment, "DOTNET_ROOT": str(DOTNET_ROOT)},
                stdout=log, stderr=subprocess.STDOUT, start_new_session=True)
            try:
                assert process.wait(timeout=240) == 0, "Native initialization failed; preserve unknown source outcomes"
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGTERM)
                process.wait(timeout=30)
                raise
        report = RESULTS / "mongo-replica-initialization.trx"
        assert report.stat().st_mtime >= started
        counts = ET.parse(report).find(".//{*}Counters").attrib
        assert counts["passed"] == "7" and counts["failed"] == "0" and counts["notExecuted"] == "0"
        print(json.dumps({"counts": counts, "sha256": hashlib.sha256(report.read_bytes()).hexdigest()}), flush=True)
    print("Owned Mongo stopped; exact source/target fixture databases removed by their checked owner markers.", flush=True)


if __name__ == "__main__":
    main()
