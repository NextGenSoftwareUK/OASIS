"""Protected enrollment of the qualified owned NATS source; no native source writes."""
import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import secrets
import subprocess


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


native = load('nats_public', Path(__file__).with_name('Publish-HostedNatsConfiguration.py'))
configuration = native.configuration
mount = load('nats_mount', Path(__file__).with_name('Publish-HostedLocalFileConfiguration.py'))
transport = load('nats_transport', configuration.ROOT / 'Scripts/TestHosts/Prepare-NatsQualifiedBundle.py')
SOURCE = configuration.ROOT / 'LocalState/COSMIC/NatsOwnedTlsQualifiedCurrent'
RESTART_PROOF = configuration.ROOT / 'LocalState/COSMIC/RecoveryLaptopResume20261107/nats-owned-restart-ci-qualified.json'
PREFIX = 'nats-owned-public'
EXTRA = ('OASIS_SANDBOX_NATS', 'OASIS_SANDBOX_NATS_BOOTSTRAP_JSON',
         'CQL_NATS_PUBLIC_SOURCE_USERNAME', 'CQL_NATS_PUBLIC_SOURCE_PASSWORD')
KEYS = tuple(native.KEYS) + EXTRA


def qualified_source():
    descriptor, bundle = transport.build_bundle(SOURCE)
    proof = json.loads(RESTART_PROOF.read_bytes())
    if (proof.get('run') != 42397239148 or proof.get('job') != 128477134308
            or proof.get('commit') != '3ba58eca22c4bcb8e05f406d8f7076c9623e9dc8'
            or proof.get('artifactSha256') != '8fac576e54df6803ff7577ef1fd34fefcbb6cc6953125a11290623280aa3a5b2'
            or proof.get('nativeCounts') != [3, 2] or proof.get('starts') != 2
            or proof.get('sourceSeeded') is not False or proof.get('sourceSaved') is not False
            or not all(proof.get(key) is True for key in ('valuesAndRevisionsUnchanged',
                'tlsIdentityUnchanged', 'restrictedPermissionsVerified', 'nativeStopped'))):
        raise RuntimeError('The exact restored-native TLS restart qualification is required.')
    return descriptor, bundle


def candidate(before, password):
    if not isinstance(password, str) or not re.fullmatch(r'[A-Za-z0-9_-]{43}', password):
        raise ValueError('An explicit bounded reader credential is required.')
    if any(key in before for key in EXTRA):
        raise RuntimeError('Existing owned NATS runtime settings require reconciliation.')
    descriptor, bundle = qualified_source()
    after = native.candidate(before, json.loads(descriptor), owned=True)
    # Preserve the exact descriptor byte binding from the accepted native source.
    after[native.KEYS[0]] = descriptor
    after.update(dict(zip(EXTRA, ('true', bundle, 'cql_reader', password))))
    observed = subprocess.run(['node', '-e',
        "const fs=require('node:fs');const v=JSON.parse(fs.readFileSync(0,'utf8'));"
        "const b=require('./Docker/host-sandbox-nats.cjs').captureBundle(v.bundle,v.descriptor);"
        "process.stdout.write(b.digest);"], cwd=configuration.ROOT,
        input=configuration.compact({'bundle': bundle, 'descriptor': descriptor}).encode(),
        capture_output=True, timeout=30)
    if observed.returncode or observed.stdout.decode() != hashlib.sha256(bundle.encode()).hexdigest():
        raise RuntimeError('The shipping NATS runtime rejected the qualified native source.')
    if ({key for key in before.keys() | after.keys() if before.get(key) != after.get(key)} != set(KEYS)
            or any(len(after[key].encode()) > 32768 for key in KEYS)):
        raise RuntimeError('Exactly eight bounded NATS settings may change.')
    return after


def prepare():
    configuration.STATE.mkdir(parents=True, exist_ok=True)
    before = configuration.variables()
    candidate_name = PREFIX + '-candidate-variables.dpapi'
    prior = configuration.STATE / candidate_name
    password = configuration.protected_read(candidate_name)[EXTRA[3]] if prior.exists() else secrets.token_urlsafe(32)
    after = candidate(before, password)
    for label, value in (('before', before), ('candidate', after)):
        name = f'{PREFIX}-{label}-variables.dpapi'
        if (configuration.STATE / name).exists():
            if configuration.protected_read(name) != value:
                raise RuntimeError('Existing protected NATS preparation differs; reconcile it.')
        else:
            configuration.write_new(configuration.STATE / name,
                configuration.verification.protected_token(configuration.compact(value).encode()))
    print('Eight protected NATS settings prepared; no deployment or native source mutation.')


def verify_mount():
    if not re.fullmatch(r'sha256:[a-f0-9]{64}', os.environ.get('COSMIC_CQL_EXPECTED_DEPLOYMENT_IMAGE_DIGEST', '')):
        raise RuntimeError('An exact reviewed NATS image digest is required.')
    return mount.verify_mount(initial_bootstrap=True)


def apply():
    before = configuration.protected_read(PREFIX + '-before-variables.dpapi')
    after = configuration.protected_read(PREFIX + '-candidate-variables.dpapi')
    if candidate(before, after[EXTRA[3]]) != after:
        raise RuntimeError('Protected NATS source/settings differ from qualification.')
    deployment = verify_mount()

    def require_deployment():
        if mount.verify_release() != deployment:
            raise RuntimeError('Reviewed NATS deployment changed before settings mutation.')

    configuration.apply_values(PREFIX, KEYS, reviewed_snapshots=(before, after), before_mutation=require_deployment)


def verify_settings():
    expected = os.environ.get('COSMIC_CQL_SETTINGS_DEPLOYMENT_ID')
    instance = os.environ.get('COSMIC_CQL_REVIEWED_INSTANCE_IDS', '')
    if not expected or not re.fullmatch(r'[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}', instance):
        raise RuntimeError('One exact NATS settings deployment and active instance are required.')
    deployment = verify_mount()
    if deployment != expected:
        raise RuntimeError('The active NATS settings deployment differs.')
    before = configuration.protected_read(PREFIX + '-before-variables.dpapi')
    after = configuration.protected_read(PREFIX + '-candidate-variables.dpapi')
    if candidate(before, after[EXTRA[3]]) != after:
        raise RuntimeError('Protected NATS settings no longer match qualification.')
    for key in KEYS:
        ack = json.loads((configuration.STATE / f'{PREFIX}-{key}.acknowledged.json').read_bytes())
        if (ack.get('key') != key or ack.get('verified') is not True or ack.get('deploymentTriggered') is not False
                or ack.get('sha256') != hashlib.sha256(after[key].encode()).hexdigest()):
            raise RuntimeError('A NATS setting acknowledgement differs.')
    if configuration.variables() != after:
        raise RuntimeError('Live NATS configuration differs from the complete protected candidate.')
    # Credentials travel only through private stdin; they are never shell arguments
    # or inspection output. This reader cannot initialize or repair mounted state.
    inspection = r"""
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const expected=JSON.parse(fs.readFileSync(0,'utf8')),root='/data/COSMIC/SandboxNATS';
const sha=b=>crypto.createHash('sha256').update(b).digest('hex');
for(const [key,value] of Object.entries(expected))if(process.env[key]!==value)throw Error('Active NATS environment differs');
if(fs.realpathSync(root)!==root)throw Error('NATS source redirected');
const runtime=require('/app/host-sandbox-nats.cjs');
const bundle=runtime.captureBundle(expected.OASIS_SANDBOX_NATS_BOOTSTRAP_JSON,expected.CQL_NATS_PUBLIC_SOURCE_JSON);
function read(name,max=16384){const file=path.join(root,name),stat=fs.lstatSync(file);
if(!stat.isFile()||stat.isSymbolicLink()||stat.size>max)throw Error('NATS mounted file differs');return fs.readFileSync(file);}
const receipt=JSON.parse(read('bootstrap.acknowledged.json'));
if(JSON.stringify(receipt)!==JSON.stringify({schema:1,version:'2.15.2',digest:bundle.digest,sourceSha256:bundle.sourceSha256}))throw Error('NATS source receipt differs');
for(const entry of bundle.files){const file=path.join(root,'store',entry.path);
if(fs.realpathSync(file)!==file||!fs.lstatSync(file).isFile())throw Error('NATS native source missing or redirected');}
const configuration=JSON.stringify(runtime.serverConfiguration(root,expected.CQL_NATS_PUBLIC_SOURCE_PASSWORD));
if(read('server.conf').toString()!==configuration)throw Error('NATS reader permissions differ');
const tls=JSON.parse(read('tls.acknowledged.json'));
if(tls.schema!==1||!tls.hashes||Object.keys(tls.hashes).sort().join(',')!=='ca.pem,server.conf,server.key')throw Error('NATS TLS receipt differs');
for(const file of ['ca.pem','server.key','server.conf']){
if(sha(read(file))!==tls.hashes[file])throw Error('NATS TLS identity changed');
if(file!=='ca.pem'&&(fs.statSync(path.join(root,file)).mode&0o077)!==0)throw Error('NATS private file permissions differ');}
const cert=new crypto.X509Certificate(read('ca.pem'));
if(!cert.checkIP('127.0.0.1')||!cert.verify(crypto.createPublicKey(read('server.key')))||Date.parse(cert.validTo)<Date.now()+86400000)throw Error('NATS certificate differs');
if(require('node:child_process').execFileSync('/usr/local/bin/nats-server',['--version'],{encoding:'utf8'}).trim()!=='nats-server: v2.15.2')throw Error('NATS native version differs');
process.stdout.write(JSON.stringify({schema:1,digest:bundle.digest,sourceSha256:bundle.sourceSha256,tlsCertificateSha256:sha(read('ca.pem')),verified:true}));
"""
    result = subprocess.run([str(configuration.CLI), 'ssh', *configuration.COMMON, '-d', instance,
        '--', 'node', '-e', inspection], input=configuration.compact({key: after[key] for key in KEYS}).encode(),
        capture_output=True, timeout=30)
    if result.returncode or len(result.stdout) > 2048:
        raise RuntimeError('Mounted NATS source inspection failed; no source repair was attempted.')
    observed = json.loads(result.stdout)
    if (observed.get('schema') != 1 or observed.get('verified') is not True
            or observed.get('digest') != hashlib.sha256(after[EXTRA[1]].encode()).hexdigest()
            or observed.get('sourceSha256') != hashlib.sha256(after[native.KEYS[0]].encode()).hexdigest()
            or not re.fullmatch(r'[a-f0-9]{64}', observed.get('tlsCertificateSha256', ''))):
        raise RuntimeError('Mounted NATS source binding differs from qualification.')
    if mount.verify_release() != deployment:
        raise RuntimeError('Reviewed NATS deployment changed during inspection.')
    receipt_name = f'{PREFIX}-{deployment}-mounted-source.acknowledged.json'
    receipt_path = configuration.STATE / receipt_name
    if receipt_path.exists():
        if json.loads(receipt_path.read_bytes()) != observed:
            raise RuntimeError('Previously verified NATS mounted identity changed.')
    else:
        configuration.write_new(receipt_path, configuration.compact(observed).encode())
    return deployment


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=('prepare', 'apply', 'verify-mount', 'verify-settings'))
    action = parser.parse_args().operation
    if action == 'prepare':
        prepare()
    elif action == 'apply':
        apply()
    elif action == 'verify-mount':
        print('Reviewed NATS deployment/mount verified', verify_mount())
    else:
        print('Reviewed NATS settings/mounted source verified', verify_settings())
