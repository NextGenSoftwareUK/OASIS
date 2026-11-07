"""One explicit native source operation on the reviewed existing sandbox volume."""
import argparse
import base64
import hashlib
import json
from pathlib import Path
import re
from runpy import run_path
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / "LocalState/COSMIC/HostedTeachingSourceCurrent"
MARKER = b"COSMIC_TEACHING_PROOF "
FUNCTIONS = {"create": "createSource", "verify": "verifySource"}


def receipt(stdout, mode, expected_hash, provider):
    lines = [line[len(MARKER):] for line in stdout.splitlines() if line.startswith(MARKER)]
    if len(lines) != 1 or len(lines[0]) > 4096:
        raise RuntimeError("Native receipt missing or ambiguous; inspect the original journal without replay.")
    value = json.loads(lines[0])
    fields = {"schema", "planSha256", "address"}
    counts = {"publicRecords": 3, "privateRecords": 1, "deletedRecords": 1, "missingRecords": 1}
    if mode == "verify":
        fields |= counts.keys()
    if (not isinstance(value, dict) or set(value) != fields or type(value["schema"]) is not int
            or value["schema"] != 1 or value["planSha256"] != expected_hash
            or mode not in FUNCTIONS):
        raise RuntimeError("Native receipt differs from the original operation.")
    if mode == "verify" and any(type(value[key]) is not int or value[key] != count for key, count in counts.items()):
        raise RuntimeError("Native verification has incomplete record coverage.")
    address = value["address"]
    if ((provider == "GUNOASIS" and address is not None)
            or (provider == "OrbitDBOASIS" and (not isinstance(address, str)
                or not re.fullmatch(r"/orbitdb/z[1-9A-HJ-NP-Za-km-z]{40,80}", address)))
            or provider not in ("GUNOASIS", "OrbitDBOASIS")):
        raise RuntimeError("Native database identity differs from the reviewed provider.")
    return value


REMOTE = """
if(process.env.RAILWAY_DEPLOYMENT_ID!==process.argv[4]||process.env.RAILWAY_REPLICA_ID!==process.argv[5])
  throw Error('Reviewed deployment/replica changed');
if(process.env.OASIS_SANDBOX_GUN==='true'||process.env.OASIS_SANDBOX_ORBITDB==='true')
  throw Error('Native readers must remain off during source provisioning');
eval(Buffer.from(process.argv[1],'base64').toString('utf8'));
const plan=JSON.parse(Buffer.from(process.argv[2],'base64').toString('utf8'));
if(!['createSource','verifySource'].includes(process.argv[3]))throw Error('Unknown source operation');
module.exports[process.argv[3]](plan).then(proof=>{
  process.stdout.write('COSMIC_TEACHING_PROOF '+JSON.stringify(proof)+'\\n',()=>process.exit(0));
},error=>{console.error(error);process.exit(1)});
"""


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("provider", choices=("GUNOASIS", "OrbitDBOASIS"))
    parser.add_argument("mode", choices=FUNCTIONS)
    parser.add_argument("--deployment-id", required=True)
    parser.add_argument("--instance-id", required=True)
    args = parser.parse_args()
    if any(str(uuid.UUID(value)) != value for value in (args.deployment_id, args.instance_id)):
        parser.error("Canonical reviewed deployment and instance IDs are required.")
    configuration = run_path(str(Path(__file__).with_name("Publish-HostedSolidConfiguration.py")))
    mount = run_path(str(Path(__file__).with_name("Publish-HostedLocalFileConfiguration.py")))
    environment = mount["os"].environ
    if environment.get("COSMIC_CQL_REVIEWED_INSTANCE_IDS") != args.instance_id:
        raise RuntimeError("Set the exact reviewed instance before source provisioning.")
    mount["verify_mount"](initial_bootstrap=True, expected_deployment=args.deployment_id)
    settings = configuration["variables"]()
    if (settings.get("OASIS_INCLUDE_TEACHING_SIDECARS") != "true"
            or any(settings.get(key) not in (None, "false") for key in ("OASIS_SANDBOX_GUN", "OASIS_SANDBOX_ORBITDB"))):
        raise RuntimeError("The qualified native package is required and native readers must remain off.")
    state = STATE / args.provider
    helper = ROOT / "Scripts/Sandbox/hosted-teaching-source.cjs"
    review = "const h=require(process.argv[1]);process.stdout.write(JSON.stringify(h.review(JSON.parse(require('node:fs').readFileSync(0,'utf8')))))"
    original = (state / "source-plan.json").read_bytes()
    validated = subprocess.run(["node", "--eval", review, str(helper)], input=original,
                               capture_output=True, check=True, timeout=15).stdout
    plan = json.loads(validated)
    if plan["provider"] != args.provider:
        raise RuntimeError("The source plan names another provider.")
    plan_hash = hashlib.sha256(validated).hexdigest()
    helper_bytes = helper.read_bytes()
    intent = {"provider": args.provider, "mode": args.mode, "planSha256": plan_hash,
              "deployment": args.deployment_id, "instance": args.instance_id,
              "helperSha256": hashlib.sha256(helper_bytes).hexdigest()}
    write = configuration["write_new"]
    write(state / (args.mode + ".intent.json"), json.dumps(intent).encode())
    if mount["verify_release"]() != args.deployment_id or configuration["variables"]() != settings:
        raise RuntimeError("Reviewed release or settings changed; no native operation started.")
    command = [str(configuration["CLI"]), "ssh", *configuration["COMMON"], "-d", args.instance_id,
               "--", "node", "--eval", REMOTE, base64.b64encode(helper_bytes).decode(),
               base64.b64encode(validated).decode(), FUNCTIONS[args.mode], args.deployment_id, args.instance_id]
    try:
        result = subprocess.run(command, capture_output=True, timeout=60)
        outcome = {"returncode": result.returncode, "stdout": base64.b64encode(result.stdout).decode(),
                   "stderr": base64.b64encode(result.stderr).decode()}
    except subprocess.TimeoutExpired as error:
        outcome = {"uncertain": True, "stdout": base64.b64encode(error.stdout or b"").decode(),
                   "stderr": base64.b64encode(error.stderr or b"").decode()}
        write(state / (args.mode + ".outcome.dpapi"),
              configuration["verification"].protected_token(json.dumps(outcome).encode()))
        raise RuntimeError("Native operation timed out; reconcile the original journal without replay.") from None
    write(state / (args.mode + ".outcome.dpapi"),
          configuration["verification"].protected_token(json.dumps(outcome).encode()))
    if result.returncode:
        raise RuntimeError("Native operation failed; protected output and original intent retained. Do not replay.")
    proof = receipt(result.stdout, args.mode, plan_hash, args.provider)
    write(state / (args.mode + ".acknowledged.json"), json.dumps(intent | {"native": proof}).encode())
    print(json.dumps({"provider": args.provider, "mode": args.mode, "planSha256": plan_hash,
                      "acknowledged": True, "sourceVerified": args.mode == "verify"}))


if __name__ == "__main__":
    main()
