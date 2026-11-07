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


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('operation', choices=('prepare', 'apply', 'verify-mount'))
    action = parser.parse_args().operation
    if action == 'prepare':
        prepare()
    elif action == 'apply':
        apply()
    else:
        print('Reviewed NATS deployment/mount verified', verify_mount())
