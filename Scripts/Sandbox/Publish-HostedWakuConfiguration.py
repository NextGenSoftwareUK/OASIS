"""Prepare and explicitly apply the retained Waku archive's six reviewed settings."""
import argparse
import base64
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess

spec = importlib.util.spec_from_file_location('exact_key', Path(__file__).with_name('Prepare-HostedExactKeyConfiguration.py'))
preparation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preparation)
configuration = preparation.configuration
mount = preparation.load('Publish-HostedLocalFileConfiguration')
archive_verifier = preparation.load('Verify-WakuQualifiedSource')
SOURCE = configuration.ROOT / 'LocalState/COSMIC/WakuQualifiedSourceCurrent'
PREFIX = 'waku-public'
PROVIDER = 'WakuOASIS'
KEYS = ('OASIS_SANDBOX_WAKU', 'OASIS_SANDBOX_WAKU_BOOTSTRAP_JSON', 'CQL_WAKU_STORE_QUERY_SOURCE_JSON',
        'PREPARED_STORAGE_ADMISSION_JSON', 'CQL_PUBLIC_READ_CATALOGUES_JSON', 'CQL_HOSTED_QUERY_EXAMPLES__WakuOASIS')


def candidate(before):
    descriptor_path = configuration.STATE / (PREFIX + '-descriptor.json')
    review = preparation.verifier.reviewed_exact_key_source(PROVIDER, descriptor_path)
    descriptor = json.loads(descriptor_path.read_bytes())
    source = archive_verifier.descriptor(SOURCE, descriptor['Actor'])
    if source['messageHashes'] != descriptor['MessageHashes']:
        raise RuntimeError('The reviewed Waku descriptor differs from its original archive.')
    proof = json.loads((SOURCE / 'hosted-runtime-qualified.json').read_bytes())
    receipt = json.loads((SOURCE / 'archive-qualified.json').read_bytes())
    if (proof.get('archiveSha256') != receipt['sha256'] or proof.get('originalArchiveBytes') != receipt['bytes']
            or proof.get('nativeRows') != 3 or proof.get('nativeStarts') != 2
            or proof.get('originalSourceUnchanged') is not True or proof.get('acknowledgementUnchanged') is not True):
        raise RuntimeError('Original-source native restore and restart qualification is required.')
    bundle = configuration.compact({'schema': 1,
        'archiveBase64': base64.b64encode((SOURCE / 'store-qualified.sqlite3').read_bytes()).decode('ascii'),
        'receipt': receipt})
    binding = review['sourceBinding']
    observed = subprocess.run(['node', '-e',
        "const fs=require('node:fs'); const input=JSON.parse(fs.readFileSync(0,'utf8')); "
        "const value=require('./Docker/host-sandbox-waku.cjs').captureBundle(input.bundle,input.descriptor); "
        "process.stdout.write(value.digest);"], cwd=configuration.ROOT,
        input=configuration.compact({'bundle': bundle, 'descriptor': binding[KEYS[2]]}).encode(),
        capture_output=True, timeout=30)
    if observed.returncode or observed.stdout.decode() != hashlib.sha256(bundle.encode()).hexdigest():
        raise RuntimeError('The shipping Waku runtime rejected the retained archive bundle.')
    if any(key in before for key in (KEYS[0], KEYS[1], KEYS[2])):
        raise RuntimeError('Existing Waku runtime settings require reconciliation.')
    after = preparation.candidate(before | {KEYS[0]: 'true', KEYS[1]: bundle} | binding, PROVIDER, review)
    changed = {key for key in before.keys() | after.keys() if before.get(key) != after.get(key)}
    if changed != set(KEYS) or any(len(after[key].encode()) > (131072 if key == KEYS[1] else 32768) for key in KEYS):
        raise RuntimeError('Exactly six bounded Waku settings may change.')
    return after


def prepare(owner):
    source = archive_verifier.descriptor(SOURCE, owner)
    descriptor = configuration.compact({'Actor': owner, 'MessageHashes': source['messageHashes']}).encode()
    configuration.STATE.mkdir(parents=True, exist_ok=True)
    path = configuration.STATE / (PREFIX + '-descriptor.json')
    if path.exists():
        if path.read_bytes() != descriptor:
            raise RuntimeError('The retained Waku teaching descriptor differs.')
    else:
        configuration.write_new(path, descriptor)
    before = configuration.variables()
    after = candidate(before)
    for label, value in (('before', before), ('candidate', after)):
        path = configuration.STATE / f'{PREFIX}-{label}-variables.dpapi'
        if path.exists():
            if configuration.protected_read(path.name) != value:
                raise RuntimeError('Existing Waku settings preparation differs.')
        else:
            configuration.write_new(path, configuration.verification.protected_token(configuration.compact(value).encode()))
    print('Six protected Waku settings prepared from the original qualified archive.')


def verify_mount(expected=None):
    if not re.fullmatch(r'sha256:[a-f0-9]{64}', os.environ.get('COSMIC_CQL_EXPECTED_DEPLOYMENT_IMAGE_DIGEST', '')):
        raise RuntimeError('An explicit reviewed Waku deployment image digest is required.')
    return mount.verify_mount(initial_bootstrap=True, expected_deployment=expected)


def apply():
    before = configuration.protected_read(PREFIX + '-before-variables.dpapi')
    after = configuration.protected_read(PREFIX + '-candidate-variables.dpapi')
    if candidate(before) != after:
        raise RuntimeError('The protected Waku candidate differs from its qualification.')
    deployment = verify_mount()

    def require_deployment():
        if mount.verify_release() != deployment:
            raise RuntimeError('The reviewed deployment changed before Waku settings staging.')

    configuration.apply_values(PREFIX, KEYS, reviewed_snapshots=(before, after), before_mutation=require_deployment)


def verify_settings():
    expected = os.environ.get('COSMIC_CQL_SETTINGS_DEPLOYMENT_ID')
    if not expected:
        raise RuntimeError('Set the exact reviewed Waku settings deployment ID.')
    deployment = verify_mount(expected)
    after = configuration.protected_read(PREFIX + '-candidate-variables.dpapi')
    for key in KEYS:
        ack = json.loads((configuration.STATE / f'{PREFIX}-{key}.acknowledged.json').read_bytes())
        if (ack.get('key') != key or ack.get('verified') is not True or ack.get('deploymentTriggered') is not False
                or ack.get('sha256') != hashlib.sha256(after[key].encode()).hexdigest()):
            raise RuntimeError('A Waku settings acknowledgement differs from the reviewed candidate.')
    if configuration.variables() != after:
        raise RuntimeError('Live Waku settings differ from the complete protected candidate.')
    instance = os.environ.get('COSMIC_CQL_REVIEWED_INSTANCE_IDS', '')
    if not re.fullmatch(r'[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}', instance):
        raise RuntimeError('One exact active Waku instance is required.')
    observed = subprocess.run([str(configuration.CLI), 'ssh', *configuration.COMMON, '-d', instance, '--',
        'head', '-c', '4097', '/data/COSMIC/SandboxWaku/bootstrap.acknowledged.json'], capture_output=True, timeout=30)
    descriptor = json.loads(after[KEYS[2]])
    receipt = {'schema': 1, 'digest': hashlib.sha256(after[KEYS[1]].encode()).hexdigest(), 'version': '0.39.2',
               'sourceOwner': descriptor['sourceOwner'], 'messageHashes': descriptor['messageHashes']}
    if observed.returncode or len(observed.stdout) > 4096 or json.loads(observed.stdout) != receipt:
        raise RuntimeError('The live Waku archive receipt differs from its original provisioning acknowledgement.')
    if mount.verify_release() != deployment:
        raise RuntimeError('The reviewed Waku deployment changed during source inspection.')
    return deployment


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=('prepare', 'apply', 'verify-mount', 'verify-settings'))
    parser.add_argument('--source-owner')
    args = parser.parse_args()
    if args.operation == 'prepare':
        if not args.source_owner:
            parser.error('Preparation requires the explicit existing sandbox teaching owner.')
        prepare(args.source_owner)
    elif args.operation == 'apply':
        apply()
    elif args.operation == 'verify-mount':
        print('Reviewed Waku deployment and writable mount verified', verify_mount())
    else:
        print('Reviewed Waku settings and original source receipt verified', verify_settings())
