"""Preserve an unadmitted failed generation; never acknowledge or replay its unknown save."""
import argparse
import base64
import hashlib
import json
import os
import re
from pathlib import Path
from runpy import run_path
import subprocess
import uuid

ROOT = Path(__file__).resolve().parents[2]
STATE_ROOT = ROOT / 'LocalState/COSMIC'
STATE = STATE_ROOT / 'HostedDaprSourceCurrent'
RECOVERY = STATE_ROOT / 'RecoveryLaptopResume20261107'
SCRIPT = 'Scripts/Sandbox/DaprSourceRetirement.cjs'
MARKER = b'COSMIC_DAPR_RETIREMENT '


def validate_receipt(value, plan, deployment, instance, store_hash, original_operation):
    required = {'schema', 'originalOperation', 'sourceOwner', 'originalStorageSha256',
                'originalSaveResponseAcknowledged', 'originalSaveOutcome', 'priorGenerationAdmitted',
                'businessWrites', 'retiredPath', 'files', 'sourceFilesSha256', 'deployment', 'instance'}
    operation = value.get('originalOperation', '')
    if (set(value) != required or str(uuid.UUID(operation)) != operation or operation != original_operation
            or value.get('schema') != 1 or type(value.get('schema')) is not int
            or value.get('sourceOwner') != plan['prefix'] + '0'
            or value.get('originalStorageSha256') != store_hash
            or value.get('originalSaveResponseAcknowledged') is not False
            or value.get('originalSaveOutcome') != 'unknown' or value.get('priorGenerationAdmitted') is not False
            or type(value.get('businessWrites')) is not int or value['businessWrites'] != 0
            or type(value.get('files')) is not int or not 1 <= value['files'] <= 256
            or not re.fullmatch(r'[a-f0-9]{64}', value.get('sourceFilesSha256', ''))
            or value.get('retiredPath') != '/data/COSMIC/RetiredDaprSources/' + operation + '/source'
            or value.get('deployment') != deployment or value.get('instance') != instance):
        raise RuntimeError('Exact unadmitted-generation preservation receipt required.')
    return value


def archive_local(write):
    ack = json.loads((STATE / 'retirement.acknowledged.json').read_bytes())
    operation = ack['native']['originalOperation']
    if str(uuid.UUID(operation)) != operation:
        raise RuntimeError('Canonical original operation required before local archival.')
    source = STATE.resolve(strict=True)
    destination = STATE_ROOT / ('HostedDaprSourceRetired-' + ack['native']['originalOperation'])
    expected_parent = STATE_ROOT.resolve(strict=True)
    if (STATE.is_symlink() or source != STATE.absolute() or source.parent != expected_parent
            or destination.resolve().parent != expected_parent or not source.is_relative_to(ROOT.resolve())
            or destination.exists() or not destination.name.startswith('HostedDaprSourceRetired-')):
        raise RuntimeError('Exact owned local state/archive paths required; no move performed.')
    write(STATE / 'archive-local.intent.json', json.dumps({'source': str(source), 'destination': str(destination),
          'remoteRetirementSha256': hashlib.sha256((STATE / 'retirement.acknowledged.json').read_bytes()).hexdigest()}).encode())
    source.rename(destination)
    write(destination / 'archive-local.acknowledged.json', json.dumps({'archived': str(destination), 'sourcePreserved': True}).encode())
    print(json.dumps({'localArchive': str(destination), 'sourcePreserved': True, 'newGenerationCreated': False}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('retire-remote', 'archive-local'))
    parser.add_argument('--deployment-id')
    parser.add_argument('--instance-id')
    parser.add_argument('--reviewed-operator-ref')
    args = parser.parse_args()
    host = run_path(str(Path(__file__).with_name('Publish-HostedSolidConfiguration.py')))
    write, protect = host['write_new'], host['verification'].protected_token
    if args.mode == 'archive-local':
        archive_local(write)
        return
    for value in (args.deployment_id, args.instance_id):
        if not value or str(uuid.UUID(value)) != value:
            parser.error('Exact canonical deployment and instance required.')
    if os.environ.get('COSMIC_CQL_REVIEWED_INSTANCE_IDS') != args.instance_id:
        raise RuntimeError('Exact reviewed retirement instance required.')
    operator = json.loads((RECOVERY / 'dapr-retirement-operator-qualified.json').read_bytes())
    if (not args.reviewed_operator_ref or operator.get('source') != args.reviewed_operator_ref
            or operator.get('conclusion') != 'success' or operator.get('platform') != 'linux'
            or operator.get('nodePassed') != 6 or operator.get('pythonPassed') != 3 or operator.get('skipped') != 0):
        raise RuntimeError('Exact qualified retirement operator required.')
    script = subprocess.check_output(['git', 'show', args.reviewed_operator_ref + ':' + SCRIPT], cwd=ROOT, timeout=15)
    if hashlib.sha256(script).hexdigest() != operator.get('scriptSha256'):
        raise RuntimeError('Qualified operator script differs.')
    creation = run_path(str(Path(__file__).with_name('Provision-HostedDaprSource.py')))
    mount = run_path(str(Path(__file__).with_name('Publish-HostedLocalFileConfiguration.py')))
    payload = protect((STATE / 'source-plan.dpapi').read_bytes(), decrypt=True)
    plan, digest = json.loads(payload), hashlib.sha256(payload).hexdigest()
    original = json.loads((STATE / 'create.intent.json').read_bytes())
    failed = json.loads(protect((STATE / 'inspect-corrected-device.outcome.dpapi').read_bytes(), decrypt=True))
    if (original.get('planSha256') != digest or (STATE / 'create.acknowledged.json').exists()
            or (STATE / 'inspect-corrected-device.acknowledged.json').exists()
            or failed.get('uncertain') or type(failed.get('returncode')) is not int or failed['returncode'] == 0):
        raise RuntimeError('Original unadmitted plan and terminal failed inspection required.')
    observed = json.loads((RECOVERY / 'dapr-corrected-device-recovery-failure-inspected.json').read_bytes())
    if observed['ownedProcesses'] or observed['storage'] != observed['intent']['storage']:
        raise RuntimeError('Stopped source and unchanged original AOF required.')
    store_hash = hashlib.sha256(json.dumps(observed['storage'], separators=(',', ':')).encode()).hexdigest()
    release = os.environ.get('COSMIC_CQL_REVIEWED_RELEASE_REF')
    if not release:
        raise RuntimeError('Exact reviewed release required.')
    source = subprocess.check_output(['git', 'rev-parse', '--verify', '--end-of-options', release + '^{commit}'], cwd=ROOT).decode().strip()
    implementation = {name: hashlib.sha256(subprocess.check_output(['git', 'show', source + ':Docker/' + name], cwd=ROOT)).hexdigest() for name in creation['FILES']}
    mount['verify_mount'](initial_bootstrap=True, expected_deployment=args.deployment_id)
    settings = host['variables']()
    if settings.get('OASIS_SANDBOX_DAPR') not in (None, 'false'):
        raise RuntimeError('Dapr must remain disabled before retirement.')
    intent = {'planSha256': digest, 'sourceCommit': source, 'operatorSource': args.reviewed_operator_ref,
              'deployment': args.deployment_id, 'instance': args.instance_id, 'storeSha256': store_hash,
              'originalSaveOutcome': 'unknown', 'replayAllowed': False}
    write(STATE / 'retirement.intent.json', json.dumps(intent).encode())
    if mount['verify_release']() != args.deployment_id or host['variables']() != settings:
        raise RuntimeError('Release/settings changed; no retirement request sent.')
    wire = {'planBase64': base64.b64encode(payload).decode(), 'planSha256': digest,
            'originalOperation': observed['intent']['originalOperation'],
            'storeSha256': store_hash, 'implementation': implementation}
    try:
        result = subprocess.run([str(host['CLI']), 'ssh', *host['COMMON'], '-d', args.instance_id,
            '--', 'node', '--eval', script.decode(), 'retire-hosted-dapr', args.deployment_id, args.instance_id],
            input=json.dumps(wire).encode(), capture_output=True, timeout=90)
        outcome = {'returncode': result.returncode, 'stdout': base64.b64encode(result.stdout).decode(), 'stderr': base64.b64encode(result.stderr).decode()}
    except subprocess.TimeoutExpired as error:
        outcome = {'uncertain': True, 'stdout': base64.b64encode(error.stdout or b'').decode(), 'stderr': base64.b64encode(error.stderr or b'').decode()}
    write(STATE / 'retirement.outcome.dpapi', protect(json.dumps(outcome).encode()))
    if outcome.get('uncertain') or outcome['returncode']:
        raise RuntimeError('Retirement outcome retained; reconcile without repeating.')
    lines = [line[len(MARKER):] for line in result.stdout.splitlines() if line.startswith(MARKER)]
    if len(lines) != 1 or len(lines[0]) > 4096:
        raise RuntimeError('Retirement receipt missing/ambiguous; do not repeat.')
    proof = validate_receipt(json.loads(lines[0]), plan, args.deployment_id, args.instance_id, store_hash,
                             observed['intent']['originalOperation'])
    write(STATE / 'retirement.acknowledged.json', json.dumps(intent | {'native': proof}).encode())
    print(json.dumps({'generationRetired': True, 'sourcePreserved': True, 'businessWrites': 0, 'originalSaveOutcome': 'unknown'}))


if __name__ == '__main__':
    main()
