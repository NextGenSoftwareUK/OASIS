"""Enroll only the originally acknowledged Dapr source; never create or repair it at startup."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import uuid


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + '.py'))
    value = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(value)
    return value


preparation = load('Prepare-HostedExactKeyConfiguration')
configuration = preparation.configuration
creation = load('Provision-HostedDaprSource')
mount = load('Publish-HostedLocalFileConfiguration')
PREFIX = 'dapr-owned-public'
PROVIDER = 'DaprOASIS'
RUNTIME = ('OASIS_SANDBOX_DAPR', 'CQL_DAPR_QUERY_STORE', 'CQL_DAPR_QUERY_GRPC_ENDPOINT',
           'CQL_DAPR_QUERY_SIDECAR_TOKEN', 'CQL_DAPR_QUERY_PROBE_ID')
KEYS = RUNTIME + ('PREPARED_STORAGE_ADMISSION_JSON', 'CQL_PUBLIC_READ_CATALOGUES_JSON',
                  'CQL_HOSTED_QUERY_EXAMPLES__DaprOASIS')


def qualified_source():
    payload = configuration.verification.protected_token((creation.STATE / 'source-plan.dpapi').read_bytes(), decrypt=True)
    plan = json.loads(payload)
    ack = json.loads((creation.STATE / 'create.acknowledged.json').read_bytes())
    digest = hashlib.sha256(payload).hexdigest()
    if ack.get('planSha256') != digest or ack.get('replayAllowed') is not False or ack.get('provider') != PROVIDER:
        raise RuntimeError('The original Dapr creation acknowledgement is required.')
    creation.receipt(creation.MARKER + json.dumps(ack['native']).encode(), plan, digest, ack['deployment'], ack['instance'])
    descriptor = {'Actor': plan['prefix'] + '0', 'LiveIds': [plan['prefix'] + suffix for suffix in ('e', '1', '2')],
                  'DeletedId': plan['prefix'] + '3', 'MissingId': plan['prefix'] + '4',
                  'Store': 'cosmic-teaching', 'GrpcEndpoint': 'http://127.0.0.1:54862/'}
    return plan, descriptor


def candidate(before, plan, review):
    if before.get(RUNTIME[0]) not in (None, 'false') or any(key in before for key in RUNTIME[1:]):
        raise RuntimeError('Existing Dapr runtime settings require reconciliation.')
    updates = dict(zip(RUNTIME, ('true', 'cosmic-teaching', 'http://127.0.0.1:54862/',
                                 plan['sidecarToken'], plan['prefix'] + 'e')))
    after = preparation.candidate(before | updates, PROVIDER, review)
    changed = {key for key in before.keys() | after.keys() if before.get(key) != after.get(key)}
    if changed != set(KEYS) or any(len(after[key].encode()) > 32768 for key in KEYS):
        raise RuntimeError('Exactly eight bounded Dapr settings may change.')
    return after


def reviewed_candidate(before):
    plan, descriptor = qualified_source()
    catalogue = configuration.STATE / (PREFIX + '-descriptor.json')
    if json.loads(catalogue.read_bytes()) != descriptor:
        raise RuntimeError('Dapr catalogue differs from the original source generation.')
    review = preparation.verifier.reviewed_exact_key_source(PROVIDER, catalogue)
    return candidate(before, plan, review)


def prepare():
    _, descriptor = qualified_source()
    configuration.STATE.mkdir(parents=True, exist_ok=True)
    catalogue = configuration.STATE / (PREFIX + '-descriptor.json')
    encoded = configuration.compact(descriptor).encode()
    if catalogue.exists():
        if catalogue.read_bytes() != encoded:
            raise RuntimeError('Original Dapr catalogue changed.')
    else:
        configuration.write_new(catalogue, encoded)
    before = configuration.variables()
    after = reviewed_candidate(before)
    for label, value in (('before', before), ('candidate', after)):
        file = configuration.STATE / f'{PREFIX}-{label}-variables.dpapi'
        if file.exists():
            if configuration.protected_read(file.name) != value:
                raise RuntimeError('Protected Dapr settings changed; reconcile original snapshots.')
        else:
            configuration.write_new(file, configuration.verification.protected_token(configuration.compact(value).encode()))
    print('Eight protected Dapr settings prepared; no live setting changed.')


def verify_mount(expected=None):
    if not re.fullmatch(r'sha256:[a-f0-9]{64}', os.environ.get('COSMIC_CQL_EXPECTED_DEPLOYMENT_IMAGE_DIGEST', '')):
        raise RuntimeError('The exact reviewed deployment image digest is required.')
    return mount.verify_mount(initial_bootstrap=True, expected_deployment=expected)


def apply():
    before = configuration.protected_read(PREFIX + '-before-variables.dpapi')
    after = configuration.protected_read(PREFIX + '-candidate-variables.dpapi')
    if reviewed_candidate(before) != after:
        raise RuntimeError('Protected Dapr settings differ from the original source.')
    deployment = verify_mount()

    def require_deployment():
        if mount.verify_release() != deployment:
            raise RuntimeError('Reviewed deployment changed before Dapr settings staging.')

    configuration.apply_values(PREFIX, KEYS, reviewed_snapshots=(before, after), before_mutation=require_deployment)


def verify_settings():
    expected = os.environ.get('COSMIC_CQL_SETTINGS_DEPLOYMENT_ID')
    instance = os.environ.get('COSMIC_CQL_REVIEWED_INSTANCE_IDS', '')
    if not expected or str(uuid.UUID(instance)) != instance:
        raise RuntimeError('Exact settings deployment and active instance required.')
    deployment = verify_mount(expected)
    before = configuration.protected_read(PREFIX + '-before-variables.dpapi')
    after = configuration.protected_read(PREFIX + '-candidate-variables.dpapi')
    if reviewed_candidate(before) != after or configuration.variables() != after:
        raise RuntimeError('Complete live Dapr settings differ from the protected candidate.')
    for key in KEYS:
        ack = json.loads((configuration.STATE / f'{PREFIX}-{key}.acknowledged.json').read_bytes())
        if (ack.get('key') != key or ack.get('verified') is not True or ack.get('deploymentTriggered') is not False
                or ack.get('sha256') != hashlib.sha256(after[key].encode()).hexdigest()):
            raise RuntimeError('Dapr setting acknowledgement differs.')
    script = r"""
const fs=require('node:fs');const input=JSON.parse(fs.readFileSync(0,'utf8'));
if(process.env.RAILWAY_DEPLOYMENT_ID!==process.argv[1]||process.env.RAILWAY_REPLICA_ID!==process.argv[2])throw Error('Reviewed instance changed');
for(const [key,value] of Object.entries(input))if(process.env[key]!==value)throw Error('Active Dapr setting differs');
const runtime=require('/app/host-sandbox-dapr.cjs');
(async()=>{const {source}=await runtime.readSource(input,'/data/COSMIC/SandboxDapr');
await runtime.verifyRows(source,input.CQL_DAPR_QUERY_SIDECAR_TOKEN,AbortSignal.timeout(20000));
process.stdout.write(JSON.stringify({verified:true,records:source.records.length,sourceOwner:source.sourceOwner}));
})().catch(()=>{process.stderr.write('Dapr original source verification failed');process.exitCode=1;});
"""
    result = subprocess.run([str(configuration.CLI), 'ssh', *configuration.COMMON, '-d', instance,
                             '--', 'node', '--eval', script, deployment, instance],
                            input=configuration.compact({key: after[key] for key in KEYS}).encode(),
                            capture_output=True, timeout=35)
    _, descriptor = qualified_source()
    if (result.returncode or len(result.stdout) > 2048 or json.loads(result.stdout) !=
            {'verified': True, 'records': 4, 'sourceOwner': descriptor['Actor']}):
        raise RuntimeError('Mounted Dapr source verification failed; no repair or reseed attempted.')
    if mount.verify_release() != deployment:
        raise RuntimeError('Reviewed deployment changed during Dapr inspection.')
    print('Exact Dapr settings and original mounted records verified; no saves performed.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('mode', choices=('prepare', 'apply', 'verify-settings'))
    args = parser.parse_args()
    {'prepare': prepare, 'apply': apply, 'verify-settings': verify_settings}[args.mode]()
