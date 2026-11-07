"""Guarded enrollment of the retained, Linux-qualified read-only DuckDB source."""
import argparse
import base64
import gzip
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import uuid

spec = importlib.util.spec_from_file_location('exact_key', Path(__file__).with_name('Prepare-HostedExactKeyConfiguration.py'))
preparation = importlib.util.module_from_spec(spec)
spec.loader.exec_module(preparation)
configuration = preparation.configuration
mount = preparation.load('Publish-HostedLocalFileConfiguration')
SOURCE = configuration.ROOT / 'LocalState/COSMIC/DuckDbCql'
PREFIX = 'duckdb-public'
PROVIDER = 'DuckDBOASIS'
DIRECTORY = '/data/COSMIC/SandboxDuckDB'
CONNECTION = f'Data Source={DIRECTORY}/source.duckdb;access_mode=READ_ONLY'
KEYS = ('OASIS_SANDBOX_DUCKDB', 'OASIS_SANDBOX_DUCKDB_BOOTSTRAP_JSON', 'CQL_DUCKDB_QUERY_CONNECTION_STRING',
        'PREPARED_STORAGE_ADMISSION_JSON', 'CQL_PUBLIC_READ_CATALOGUES_JSON', 'CQL_HOSTED_QUERY_EXAMPLES__DuckDBOASIS')


def qualified_source():
    archive = SOURCE / 'source.duckdb'
    seed_path = SOURCE / 'demo-seed.json'
    if archive.is_symlink() or seed_path.is_symlink() or (SOURCE / 'source.duckdb.wal').exists():
        raise RuntimeError('The retained DuckDB source must be standalone without symlinks or WAL.')
    if not 16 <= archive.stat().st_size <= 8388608 or not 1 <= seed_path.stat().st_size <= 4096:
        raise RuntimeError('The retained DuckDB source exceeds its qualification bounds.')
    raw, seed_bytes = archive.read_bytes(), seed_path.read_bytes()
    proof = json.loads((SOURCE / 'hosted-runtime-qualified.json').read_bytes())
    digest = hashlib.sha256(raw).hexdigest()
    if (proof.get('sourceSha256') != digest or proof.get('seedSha256') != hashlib.sha256(seed_bytes).hexdigest()
            or proof.get('nativeCounts') != [3, 2, 0] or proof.get('activations') != 2
            or proof.get('sourceUnchanged') is not True or proof.get('walAbsent') is not True
            or proof.get('conclusion') != 'success' or proof.get('passed') != 1 or proof.get('skipped') != 0
            or proof.get('sdkVersion') != '1.1.3' or proof.get('platform') != 'linux-x64'):
        raise RuntimeError('Exact retained-source Linux read-only qualification is required.')
    seed = json.loads(seed_bytes)
    if set(seed) != {'Actor', 'Ids', 'DeletedId'}:
        raise RuntimeError('The original DuckDB seed identity manifest differs.')
    descriptor = {'Actor': seed['Actor'], 'LiveIds': seed['Ids'], 'DeletedId': seed['DeletedId'],
                  'MissingId': str(uuid.uuid5(uuid.NAMESPACE_URL, 'oasis-duckdb-missing:' + digest))}
    bundle = configuration.compact({'schema': 1,
        'archiveGzipBase64': base64.b64encode(gzip.compress(raw, mtime=0)).decode(),
        'bytes': len(raw), 'sha256': digest})
    return descriptor, bundle


def candidate(before):
    descriptor, bundle = qualified_source()
    path = configuration.STATE / (PREFIX + '-descriptor.json')
    if json.loads(path.read_bytes()) != descriptor:
        raise RuntimeError('The reviewed DuckDB catalogue differs from its qualified seed.')
    review = preparation.verifier.reviewed_exact_key_source(PROVIDER, path)
    if any(key in before for key in KEYS[:3]):
        raise RuntimeError('Existing DuckDB runtime settings require reconciliation.')
    observed = subprocess.run(['node', '-e',
        "const fs=require('node:fs'); const value=require('./Docker/host-sandbox-duckdb.cjs')"
        ".captureBundle(fs.readFileSync(0,'utf8')); process.stdout.write(value.digest);"],
        cwd=configuration.ROOT, input=bundle.encode(), capture_output=True, timeout=30)
    if observed.returncode or observed.stdout.decode() != hashlib.sha256(bundle.encode()).hexdigest():
        raise RuntimeError('The shipping DuckDB runtime rejected its original source bundle.')
    after = preparation.candidate(before | {KEYS[0]: 'true', KEYS[1]: bundle, KEYS[2]: CONNECTION}, PROVIDER, review)
    changed = {key for key in before.keys() | after.keys() if before.get(key) != after.get(key)}
    if changed != set(KEYS) or any(len(after[key].encode()) > 32768 for key in KEYS):
        raise RuntimeError('Exactly six bounded DuckDB settings may change.')
    return after


def prepare():
    descriptor, _ = qualified_source()
    configuration.STATE.mkdir(parents=True, exist_ok=True)
    path = configuration.STATE / (PREFIX + '-descriptor.json')
    content = configuration.compact(descriptor).encode()
    if path.exists():
        if path.read_bytes() != content:
            raise RuntimeError('The retained DuckDB descriptor differs.')
    else:
        configuration.write_new(path, content)
    before = configuration.variables()
    after = candidate(before)
    for label, value in (('before', before), ('candidate', after)):
        path = configuration.STATE / f'{PREFIX}-{label}-variables.dpapi'
        if path.exists():
            if configuration.protected_read(path.name) != value:
                raise RuntimeError('Existing DuckDB configuration preparation differs.')
        else:
            configuration.write_new(path, configuration.verification.protected_token(configuration.compact(value).encode()))
    print('Six protected DuckDB settings prepared; original native source is unchanged.')


def verify_mount(expected=None):
    if not re.fullmatch(r'sha256:[a-f0-9]{64}', os.environ.get('COSMIC_CQL_EXPECTED_DEPLOYMENT_IMAGE_DIGEST', '')):
        raise RuntimeError('An explicit reviewed DuckDB deployment image digest is required.')
    return mount.verify_mount(initial_bootstrap=True, expected_deployment=expected)


def apply():
    before = configuration.protected_read(PREFIX + '-before-variables.dpapi')
    after = configuration.protected_read(PREFIX + '-candidate-variables.dpapi')
    if candidate(before) != after:
        raise RuntimeError('Protected DuckDB settings differ from their original qualification.')
    deployment = verify_mount()

    def require_deployment():
        if mount.verify_release() != deployment:
            raise RuntimeError('Reviewed deployment changed before DuckDB settings staging.')

    configuration.apply_values(PREFIX, KEYS, reviewed_snapshots=(before, after), before_mutation=require_deployment)


def verify_settings():
    expected = os.environ.get('COSMIC_CQL_SETTINGS_DEPLOYMENT_ID')
    if not expected:
        raise RuntimeError('An exact DuckDB settings deployment ID is required.')
    deployment = verify_mount(expected)
    after = configuration.protected_read(PREFIX + '-candidate-variables.dpapi')
    for key in KEYS:
        ack = json.loads((configuration.STATE / f'{PREFIX}-{key}.acknowledged.json').read_bytes())
        if (ack.get('key') != key or ack.get('verified') is not True or ack.get('deploymentTriggered') is not False
                or ack.get('sha256') != hashlib.sha256(after[key].encode()).hexdigest()):
            raise RuntimeError('A DuckDB setting acknowledgement differs.')
    if configuration.variables() != after:
        raise RuntimeError('Live DuckDB configuration differs from the complete protected candidate.')
    instance = os.environ.get('COSMIC_CQL_REVIEWED_INSTANCE_IDS', '')
    if not re.fullmatch(r'[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}', instance):
        raise RuntimeError('One exact active DuckDB instance is required.')
    bundle = json.loads(after[KEYS[1]])
    receipt = {'schema': 1, 'digest': hashlib.sha256(after[KEYS[1]].encode()).hexdigest(),
               'sha256': bundle['sha256'], 'bytes': bundle['bytes']}
    inspection = "const fs=require('node:fs'); const crypto=require('node:crypto'); const p='/data/COSMIC/SandboxDuckDB/'; " \
        "const names=['source.duckdb','bootstrap.acknowledged.json']; for(const n of names){const s=fs.lstatSync(p+n);" \
        "if(!s.isFile()||s.isSymbolicLink())throw Error('Non-regular DuckDB source');}" \
        "if(fs.realpathSync(p.slice(0,-1))!==p.slice(0,-1)||fs.existsSync(p+'source.duckdb.wal'))throw Error('DuckDB path/WAL differs');" \
        "const s=fs.statSync(p+'source.duckdb'); if(s.size>8388608||fs.statSync(p+'bootstrap.acknowledged.json').size>4096)throw Error('DuckDB byte budget');" \
        "process.stdout.write(JSON.stringify({receipt:JSON.parse(fs.readFileSync(p+'bootstrap.acknowledged.json','utf8'))," \
        "sha256:crypto.createHash('sha256').update(fs.readFileSync(p+'source.duckdb')).digest('hex'),bytes:s.size}));"
    observed = subprocess.run([str(configuration.CLI), 'ssh', *configuration.COMMON, '-d', instance, '--',
        'node', '-e', inspection], capture_output=True, timeout=30)
    if observed.returncode or len(observed.stdout) > 4096:
        raise RuntimeError('Could not inspect the original mounted DuckDB source.')
    if json.loads(observed.stdout) != {'receipt': receipt, 'sha256': bundle['sha256'], 'bytes': bundle['bytes']}:
        raise RuntimeError('Mounted DuckDB source differs from its original native qualification.')
    if mount.verify_release() != deployment:
        raise RuntimeError('Reviewed DuckDB deployment changed during inspection.')
    return deployment


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=('prepare', 'apply', 'verify-mount', 'verify-settings'))
    args = parser.parse_args()
    if args.operation == 'prepare':
        prepare()
    elif args.operation == 'apply':
        apply()
    elif args.operation == 'verify-mount':
        print('Reviewed DuckDB deployment/mount verified', verify_mount())
    else:
        print('Reviewed DuckDB settings/source verified', verify_settings())
