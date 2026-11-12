"""Inspect an original uncertain Dapr save, then explicitly accept proven recovery; never resend it."""
import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
from runpy import run_path
import re
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]
STATE = ROOT / 'LocalState/COSMIC/HostedDaprSourceCurrent'
MARKER = b'COSMIC_DAPR_RECOVERY_PROOF '
REMOTE = r"""
const fs=require('node:fs'),crypto=require('node:crypto'),hash=b=>crypto.createHash('sha256').update(b).digest('hex');
const manifest=JSON.parse(Buffer.from(process.argv[4],'base64').toString('utf8'));
for(const [name,sha]of Object.entries(manifest))if(hash(fs.readFileSync('/app/'+name))!==sha)throw Error('Reviewed Dapr recovery implementation differs');
const modes={inspect:'inspectPendingSource','inspect-corrected-device':'inspectCorrectedDeviceSource',acknowledge:'acknowledgeRecoveredSource'},mode=process.argv[3];
if(!modes[mode])throw Error('Explicit Dapr recovery operation required');
const helper=require('/app/provision-sandbox-dapr.cjs');
(async()=>{let text='';for await(const chunk of process.stdin){text+=chunk.toString('utf8');if(Buffer.byteLength(text)>8192)throw Error('Plan budget exceeded');}
const plan=helper.reviewCommand([process.argv[1],process.argv[2]],process.env,text);
const value=await helper[modes[mode]](...(mode==='inspect-corrected-device'?[plan,process.argv[5]]:[plan]));
const recovery=mode!=='acknowledge'?{originalOperation:value.originalOperation,recoveries:value.recoveries,
businessWrites:value.businessWrites,originalSaveResponseAcknowledged:value.originalSaveResponseAcknowledged,
inspectionSha256:hash(JSON.stringify(value)),originalStorageSha256:hash(JSON.stringify(value.originalStorage))}:value.reconciliation;
process.stdout.write('COSMIC_DAPR_RECOVERY_PROOF '+JSON.stringify({schema:1,mode,planSha256:hash(text),
deployment:process.env.RAILWAY_DEPLOYMENT_ID,instance:process.env.RAILWAY_REPLICA_ID,
sourceOwner:value.sourceOwner,records:mode!=='acknowledge'?value.records.length:value.records,...recovery})+'\n');
})().catch(error=>{console.error(error.message);process.exitCode=1;});
"""


def receipt(stdout, mode, plan, digest, deployment, instance):
    lines = [line[len(MARKER):] for line in stdout.splitlines() if line.startswith(MARKER)]
    if len(lines) != 1 or len(lines[0]) > 4096:
        raise RuntimeError('Dapr recovery receipt missing or ambiguous; do not repeat the operation.')
    value = json.loads(lines[0])
    expected = {'schema': 1, 'mode': mode, 'planSha256': digest, 'deployment': deployment, 'instance': instance,
                'sourceOwner': plan['prefix'] + '0', 'records': 4, 'recoveries': 2,
                'businessWrites': 0, 'originalSaveResponseAcknowledged': False}
    if (not isinstance(value, dict) or set(value) != set(expected) | {'originalOperation', 'inspectionSha256', 'originalStorageSha256'}
            or any(value.get(key) != item for key, item in expected.items())
            or any(type(value.get(key)) is not int for key in ('schema', 'records', 'recoveries', 'businessWrites'))
            or value.get('originalSaveResponseAcknowledged') is not False
            or any(not re.fullmatch(r'[a-f0-9]{64}', value.get(key, '')) for key in ('inspectionSha256', 'originalStorageSha256'))
            or str(uuid.UUID(value['originalOperation'])) != value['originalOperation']):
        raise RuntimeError('Dapr recovery proof differs from the original operation.')
    return value


def accepted_inspection(state):
    files = [state / name for name in ('inspect.acknowledged.json', 'inspect-corrected-device.acknowledged.json')
             if (state / name).is_file()]
    if len(files) != 1:
        raise RuntimeError('One acknowledged original-source inspection is required.')
    return json.loads(files[0].read_bytes())


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('inspect', 'inspect-corrected-device', 'acknowledge'))
    parser.add_argument('--prior-inspection-intent-sha256')
    parser.add_argument('--deployment-id', required=True)
    parser.add_argument('--instance-id', required=True)
    args = parser.parse_args()
    prior_hash = args.prior_inspection_intent_sha256
    if args.mode == 'inspect-corrected-device':
        if not re.fullmatch(r'[a-f0-9]{64}', prior_hash or ''):
            parser.error('The read-only reviewed prior inspection intent hash is required.')
    elif prior_hash is not None:
        parser.error('A prior inspection hash is valid only for explicit corrected-device inspection.')
    for value in (args.deployment_id, args.instance_id):
        if str(uuid.UUID(value)) != value:
            parser.error('Exact canonical deployment and instance required.')
    if os.environ.get('COSMIC_CQL_REVIEWED_INSTANCE_IDS') != args.instance_id:
        raise RuntimeError('Set the exact reviewed recovery instance.')
    host = run_path(str(Path(__file__).with_name('Publish-HostedSolidConfiguration.py')))
    mount = run_path(str(Path(__file__).with_name('Publish-HostedLocalFileConfiguration.py')))
    creation = run_path(str(Path(__file__).with_name('Provision-HostedDaprSource.py')))
    protect, write = host['verification'].protected_token, host['write_new']
    payload = protect((STATE / 'source-plan.dpapi').read_bytes(), decrypt=True)
    plan, digest = json.loads(payload), hashlib.sha256(payload).hexdigest()
    original_bytes = (STATE / 'create.intent.json').read_bytes()
    original = json.loads(original_bytes)
    if original.get('planSha256') != digest or original.get('replayAllowed') is not False or (STATE / 'create.acknowledged.json').exists():
        raise RuntimeError('Original unacknowledged source intent required; no replacement or replay.')
    release = os.environ.get('COSMIC_CQL_REVIEWED_RELEASE_REF')
    if not release or not re.fullmatch(r'sha256:[a-f0-9]{64}', os.environ.get('COSMIC_CQL_EXPECTED_DEPLOYMENT_IMAGE_DIGEST', '')):
        raise RuntimeError('Exact reviewed source ref and deployed image digest required.')
    source = subprocess.check_output(['git', 'rev-parse', '--verify', '--end-of-options', release + '^{commit}'], cwd=ROOT, timeout=15).decode().strip()
    manifest = {name: hashlib.sha256(subprocess.check_output(['git', 'show', source + ':Docker/' + name], cwd=ROOT, timeout=15)).hexdigest() for name in creation['FILES']}
    mount['verify_mount'](initial_bootstrap=True, expected_deployment=args.deployment_id)
    settings = host['variables']()
    if settings.get('OASIS_SANDBOX_DAPR') not in (None, 'false'):
        raise RuntimeError('Dapr readers must remain disabled during reconciliation.')
    inspected = None
    if args.mode == 'inspect-corrected-device':
        prior_intent = json.loads((STATE / 'inspect.intent.json').read_bytes())
        prior_outcome = json.loads(protect((STATE / 'inspect.outcome.dpapi').read_bytes(), decrypt=True))
        if (prior_intent.get('planSha256') != digest or prior_outcome.get('uncertain')
                or type(prior_outcome.get('returncode')) is not int or prior_outcome['returncode'] == 0
                or (STATE / 'inspect.acknowledged.json').exists()):
            raise RuntimeError('A terminal failed original inspection with the same plan is required.')
    if args.mode == 'acknowledge':
        inspected = accepted_inspection(STATE)
        if inspected.get('sourceCommit') != source or inspected.get('planSha256') != digest:
            raise RuntimeError('Exact qualified inspection source and plan must match.')
    intent = {'mode': args.mode, 'sourceCommit': source, 'planSha256': digest,
              'originalCreationIntentSha256': hashlib.sha256(original_bytes).hexdigest(),
              'deployment': args.deployment_id, 'instance': args.instance_id, 'implementationSha256': manifest}
    if args.mode == 'inspect-corrected-device':
        intent['priorInspectionIntentSha256'] = prior_hash
        intent['priorLocalInspectionIntentSha256'] = hashlib.sha256((STATE / 'inspect.intent.json').read_bytes()).hexdigest()
    write(STATE / (args.mode + '.intent.json'), json.dumps(intent).encode())
    if mount['verify_release']() != args.deployment_id or host['variables']() != settings:
        raise RuntimeError('Reviewed release/settings changed; no recovery operation sent.')
    command = [str(host['CLI']), 'ssh', *host['COMMON'], '-d', args.instance_id, '--', 'node', '--eval', REMOTE,
               args.deployment_id, args.instance_id, args.mode, base64.b64encode(json.dumps(manifest).encode()).decode()]
    if args.mode == 'inspect-corrected-device':
        command.append(prior_hash)
    try:
        result = subprocess.run(command, input=payload, capture_output=True, timeout=150)
        outcome = {'returncode': result.returncode, 'stdout': base64.b64encode(result.stdout).decode(), 'stderr': base64.b64encode(result.stderr).decode()}
    except subprocess.TimeoutExpired as error:
        outcome = {'uncertain': True, 'stdout': base64.b64encode(error.stdout or b'').decode(), 'stderr': base64.b64encode(error.stderr or b'').decode()}
        write(STATE / (args.mode + '.outcome.dpapi'), protect(json.dumps(outcome).encode()))
        raise RuntimeError('Dapr recovery observation timed out; preserve original journal without repeating.') from None
    write(STATE / (args.mode + '.outcome.dpapi'), protect(json.dumps(outcome).encode()))
    if result.returncode:
        raise RuntimeError('Dapr recovery operation failed; protected outcome retained. Do not repeat.')
    proof = receipt(result.stdout, args.mode, plan, digest, args.deployment_id, args.instance_id)
    if inspected is not None and any(proof[key] != inspected['native'][key] for key in ('originalOperation', 'inspectionSha256', 'originalStorageSha256')):
        raise RuntimeError('Recovered source differs from the original inspected outcome.')
    write(STATE / (args.mode + '.acknowledged.json'), json.dumps(intent | {'native': proof}).encode())
    if args.mode == 'acknowledge':
        native = {'schema': 1, 'sourceOwner': proof['sourceOwner'], 'ids': [plan['prefix'] + suffix for suffix in ('e', '1', '2')],
                  'records': 4, 'recoveryVerified': True, 'planSha256': digest, 'deployment': args.deployment_id, 'instance': args.instance_id}
        creation['receipt'](creation['MARKER'] + json.dumps(native).encode(), plan, digest, args.deployment_id, args.instance_id)
        accepted = original | {'deployment': args.deployment_id, 'instance': args.instance_id, 'native': native,
                               'originalCreationDeployment': original['deployment'], 'originalCreationInstance': original['instance'],
                               'originalCreationIntentSha256': intent['originalCreationIntentSha256'], 'reconciliation': proof}
        write(STATE / 'create.acknowledged.json', json.dumps(accepted).encode())
    print(json.dumps({'mode': args.mode, 'acknowledged': True, 'businessWrites': 0, 'originalSaveResponseAcknowledged': False}))


if __name__ == '__main__':
    main()
