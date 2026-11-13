"""Prepare privately, then perform one journalled creation on the exact reviewed Development instance."""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
from runpy import run_path
import secrets
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/HostedDaprSourceCurrent'
FILES = ['provision-sandbox-dapr.cjs', 'host-sandbox-dapr.cjs', 'host-sandbox-ipfs.cjs',
         'DaprRuntime/configuration.cjs', 'DaprRuntime/contract.json', 'DaprRuntime/seed.cjs']
MARKER = b'COSMIC_DAPR_PROOF '
REMOTE = r"""
const fs=require('node:fs'),crypto=require('node:crypto');
const manifest=JSON.parse(Buffer.from(process.argv[3],'base64').toString('utf8'));
for(const [name,sha] of Object.entries(manifest))
 if(crypto.createHash('sha256').update(fs.readFileSync('/app/'+name)).digest('hex')!==sha)
  throw Error('Dapr deployed implementation differs from the reviewed source');
const helper=require('/app/provision-sandbox-dapr.cjs');
(async()=>{let text='';for await(const chunk of process.stdin){text+=chunk.toString('utf8');
 if(Buffer.byteLength(text)>8192)throw Error('Dapr plan exceeds budget');}
const plan=helper.reviewCommand([process.argv[1],process.argv[2]],process.env,text);
const receipt=await helper.provision(plan);
process.stdout.write('COSMIC_DAPR_PROOF '+JSON.stringify({...receipt,
planSha256:crypto.createHash('sha256').update(text).digest('hex'),
deployment:process.env.RAILWAY_DEPLOYMENT_ID,instance:process.env.RAILWAY_REPLICA_ID})+'\n');
})().catch(error=>{console.error(error.message);process.exitCode=1;});
"""


def receipt(stdout, plan, expected_hash, deployment, instance):
    lines = [line[len(MARKER):] for line in stdout.splitlines() if line.startswith(MARKER)]
    if len(lines) != 1 or len(lines[0]) > 4096:
        raise RuntimeError('Dapr receipt missing or ambiguous; reconcile original intent without replay.')
    value = json.loads(lines[0])
    expected = {'schema': 1, 'sourceOwner': plan['prefix'] + '0',
                'ids': [plan['prefix'] + suffix for suffix in ('e', '1', '2')],
                'records': 4, 'recoveryVerified': True, 'planSha256': expected_hash,
                'deployment': deployment, 'instance': instance}
    if (value != expected or type(value.get('records')) is not int or type(value.get('schema')) is not int
            or value.get('recoveryVerified') is not True):
        raise RuntimeError('Dapr native receipt differs; preserve original source without replay.')
    return value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('prepare', 'create'))
    parser.add_argument('--deployment-id')
    parser.add_argument('--instance-id')
    args = parser.parse_args()
    configuration = run_path(str(Path(__file__).with_name('Publish-HostedSolidConfiguration.py')))
    protect = configuration['verification'].protected_token
    write = configuration['write_new']
    STATE.mkdir(parents=True, exist_ok=True)
    plan_file = STATE / 'source-plan.dpapi'
    if args.mode == 'prepare':
        plan = {'schema': 1, 'prefix': str(uuid.uuid4())[:-1],
                'readerPassword': secrets.token_hex(32), 'sidecarToken': secrets.token_hex(32)}
        payload = json.dumps(plan, separators=(',', ':')).encode()
        write(plan_file, protect(payload))
        print(json.dumps({'prepared': True, 'provider': 'DaprOASIS', 'sourceOwner': plan['prefix'] + '0'}))
        return
    for value in (args.deployment_id, args.instance_id):
        if not value or str(uuid.UUID(value)) != value:
            parser.error('Exact canonical deployment and instance IDs are required for creation.')
    if os.environ.get('COSMIC_CQL_REVIEWED_INSTANCE_IDS') != args.instance_id:
        raise RuntimeError('Set the exact reviewed source instance before creation.')
    mount = run_path(str(Path(__file__).with_name('Publish-HostedLocalFileConfiguration.py')))
    mount['verify_mount'](initial_bootstrap=True, expected_deployment=args.deployment_id)
    settings = configuration['variables']()
    if settings.get('OASIS_SANDBOX_DAPR') not in (None, 'false'):
        raise RuntimeError('Dapr readers must remain disabled during creation.')
    payload = protect(plan_file.read_bytes(), decrypt=True)
    plan = json.loads(payload)
    release_ref = os.environ.get('COSMIC_CQL_REVIEWED_RELEASE_REF')
    if not release_ref:
        raise RuntimeError('The exact reviewed release ref is required for deployed implementation hashes.')
    source_sha = subprocess.check_output(['git', 'rev-parse', '--verify', '--end-of-options',
                                          release_ref + '^{commit}'], cwd=ROOT, timeout=15).decode().strip()
    # Hash the reviewed Git objects, not platform-dependent checkout line endings.
    manifest = {name: hashlib.sha256(subprocess.check_output(
        ['git', 'show', source_sha + ':Docker/' + name], cwd=ROOT, timeout=15)).hexdigest() for name in FILES}
    intent = {'schema': 1, 'provider': 'DaprOASIS', 'deployment': args.deployment_id,
              'instance': args.instance_id, 'planSha256': hashlib.sha256(payload).hexdigest(),
              'implementationSha256': manifest, 'sourceCommit': source_sha, 'replayAllowed': False}
    write(STATE / 'create.intent.json', json.dumps(intent).encode())
    if mount['verify_release']() != args.deployment_id or configuration['variables']() != settings:
        raise RuntimeError('Reviewed release/settings changed; no creation started. Reconcile intent.')
    command = [str(configuration['CLI']), 'ssh', *configuration['COMMON'], '-d', args.instance_id,
               '--', 'node', '--eval', REMOTE, args.deployment_id, args.instance_id,
               base64.b64encode(json.dumps(manifest).encode()).decode()]
    try:
        result = subprocess.run(command, input=payload, capture_output=True, timeout=150)
        outcome = {'returncode': result.returncode, 'stdout': base64.b64encode(result.stdout).decode(),
                   'stderr': base64.b64encode(result.stderr).decode()}
    except subprocess.TimeoutExpired as error:
        outcome = {'uncertain': True, 'stdout': base64.b64encode(error.stdout or b'').decode(),
                   'stderr': base64.b64encode(error.stderr or b'').decode()}
        write(STATE / 'create.outcome.dpapi', protect(json.dumps(outcome).encode()))
        raise RuntimeError('Dapr creation outcome unknown; inspect original intent without resending.') from None
    write(STATE / 'create.outcome.dpapi', protect(json.dumps(outcome).encode()))
    if result.returncode:
        raise RuntimeError('Dapr creation failed; protected outcome retained. Do not resend.')
    proof = receipt(result.stdout, plan, intent['planSha256'], args.deployment_id, args.instance_id)
    write(STATE / 'create.acknowledged.json', json.dumps(intent | {'native': proof}).encode())
    print(json.dumps({'provider': 'DaprOASIS', 'acknowledged': True, 'sourceOwner': proof['sourceOwner']}))


if __name__ == '__main__':
    main()
