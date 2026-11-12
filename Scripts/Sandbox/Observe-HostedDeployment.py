"""Observe one existing deployment; never request a build, retry or restart.

Only a small public status tuple is printed/saved. Raw CLI metadata is withheld
because it may contain deployment configuration. Use the same ID after a timeout.
"""
import argparse
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("deployment_id")
    parser.add_argument("--expected-commit", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args()
    if str(uuid.UUID(args.deployment_id)) != args.deployment_id:
        raise ValueError("Canonical deployment ID required.")
    if not re.fullmatch("[0-9a-f]{40}", args.expected_commit):
        raise ValueError("Exact reviewed commit required.")
    output = (ROOT / args.output).resolve()
    if not output.is_relative_to((ROOT / "TestResults").resolve()) or not output.parent.is_dir():
        raise ValueError("Use the existing worktree TestResults directory.")

    spec = importlib.util.spec_from_file_location("hosted_configuration",
        Path(__file__).with_name("Publish-HostedSolidConfiguration.py"))
    host = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(host)
    observed = subprocess.run([str(host.CLI), "deployment", "list", *host.COMMON,
        "--json"], capture_output=True, timeout=45)
    if observed.returncode:
        raise RuntimeError("Observation failed; raw output withheld. Do not request another deployment.")
    deployments = json.loads(observed.stdout)
    if not isinstance(deployments, list):
        raise RuntimeError("Unexpected deployment inventory; no state inferred.")
    matches = [row for row in deployments if row.get("id") == args.deployment_id]
    if len(matches) != 1:
        raise RuntimeError("Exact deployment absent or ambiguous; reconcile before taking action.")
    row = matches[0]
    commit = row.get("meta", {}).get("commitHash")
    if commit != args.expected_commit:
        raise RuntimeError("Deployment belongs to another release; no state inferred.")
    status = row.get("status")
    if not isinstance(status, str) or not status:
        raise RuntimeError("Deployment status missing; no state inferred.")
    summary = {"id": args.deployment_id, "status": status, "commit": commit}
    output.write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(summary))


if __name__ == "__main__":
    main()
