"""Run the actual ENS CLI once, preserving configuration, intent and outcome."""
import hashlib
import json
from pathlib import Path
import subprocess
import uuid


ROOT = Path(__file__).resolve().parents[2]


def write_new(path, value):
    with path.open("x", encoding="utf-8") as stream:
        json.dump(value, stream, sort_keys=True, separators=(",", ":"))
        stream.flush()
        import os
        os.fsync(stream.fileno())


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    if ROOT != Path("C:/Source/OASIS-cosmic"):
        raise RuntimeError("Assigned worktree required.")
    names = ["nick.eth", "vitalik.eth", "ur.integration-tests.eth"]
    qualification = ROOT / "LocalState/COSMIC/EnsPublicQualificationCurrent/qualification.acknowledged.json"
    proof = json.loads(qualification.read_text(encoding="utf-8"))
    if [row["Name"] for row in proof["Records"]] != names or proof["Records"][-1]["Address"] != "0x" + "2" * 40:
        raise RuntimeError("Original three-name qualification required.")
    binary = ROOT / "Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll"
    if not binary.is_file():
        raise RuntimeError("Build actual console before allocating source identity.")
    state = ROOT / "LocalState/COSMIC/HostedEnsPublicSourceCurrent"
    state.mkdir(exist_ok=False)
    evidence = ROOT / "TestResults/EnsPublicSourceCurrent"
    evidence.mkdir(exist_ok=True)
    source = state / "source.json"
    write_new(source, {"schema": 1, "rpcUrl": "https://ethereum-rpc.publicnode.com/",
                       "names": names, "sourceOwner": str(uuid.uuid4())})
    identity = {"schema": 1, "sourceSha256": digest(source), "binarySha256": digest(binary),
                "qualificationSha256": digest(qualification), "writes": False,
                "maximumInvocations": 1, "processTimeoutSeconds": 140}
    write_new(state / "console.intent.json", identity)
    try:
        with (evidence / "console.stdout.log").open("xb") as stdout, (evidence / "console.stderr.log").open("xb") as stderr:
            result = subprocess.run(["dotnet", str(binary), "--ens-public", str(source)],
                                    cwd=ROOT, stdout=stdout, stderr=stderr, timeout=140, check=False)
        output = (evidence / "console.stdout.log").read_text(encoding="utf-8")
        if result.returncode != 0 or "returned holons: 3" not in output or "returned holons: 2" not in output or output.count("Provider: ENSOASIS") != 2:
            raise RuntimeError(f"Actual ENS console did not prove 3/2; exit={result.returncode}.")
        write_new(state / "console.acknowledged.json", {**identity, "full": 3, "subset": 2,
                    "stdoutSha256": digest(evidence / "console.stdout.log"), "exitCode": result.returncode})
        print("Actual ENS manager/LINQ console passed: 3 full / 2 filtered; no source writes.")
    except Exception as error:
        write_new(state / "console.failure.observed.json", {**identity, "errorType": type(error).__name__,
                   "message": str(error), "replayAllowed": False})
        raise


if __name__ == "__main__":
    main()
