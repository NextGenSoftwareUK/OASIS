"""Cloud-only verification of the exact native artifacts consumed by WEB4."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--role', choices=['waku', 'nats'], required=True)
    options = parser.parse_args()
    if os.environ.get('GITHUB_ACTIONS') != 'true':
        raise RuntimeError('Native images must be pulled only in the owned cloud job.')
    root = Path(__file__).resolve().parents[2]
    source = (root / 'Docker/Dockerfile.web4').read_text(encoding='utf-8')
    image = re.search(r'^FROM (ghcr\.io/nextgensoftwareuk/oasis-web4@sha256:[a-f0-9]{64}) AS provider_' + options.role + r'_runtime$', source, re.M).group(1)
    role = 'provider_' + options.role
    run = os.environ['GITHUB_RUN_ID']; attempt = os.environ['GITHUB_RUN_ATTEMPT']
    assert re.fullmatch(r'[0-9]+', run) and re.fullmatch(r'[0-9]+', attempt)
    name = 'oasis-native-consumer-' + options.role + '-' + run + '-' + attempt
    result = root / 'TestResults/Web4NativeConsumers' / options.role
    result.mkdir(parents=True, exist_ok=False)

    def docker(*args, check=True, timeout=180):
        completed = subprocess.run(['docker', *args], capture_output=True, text=True, timeout=timeout)
        if check and completed.returncode:
            raise RuntimeError('Owned native artifact operation failed: ' + completed.stderr[-2000:])
        return completed

    def absent():
        observed = docker('container', 'inspect', name, check=False, timeout=15)
        return observed.returncode != 0 and 'no such' in observed.stderr.lower()

    assert absent(), 'Never adopt an existing container.'
    docker('pull', image)
    metadata = json.loads(docker('image', 'inspect', image, timeout=15).stdout)[0]
    assert image in metadata['RepoDigests']
    labels = metadata['Config']['Labels']
    assert labels['one.oasis.native-runtime'] == role
    assert labels['org.opencontainers.image.source'] == 'https://github.com/NextGenSoftwareUK/OASIS'
    producer = labels['org.opencontainers.image.revision']
    assert re.fullmatch(r'[a-f0-9]{40}', producer)
    binary = '/opt/waku/logosdeliverynode' if options.role == 'waku' else '/usr/local/bin/nats-server'
    if options.role == 'waku':
        text = (root / 'Docker/host-sandbox-waku.cjs').read_text(encoding='utf-8')
        version = re.search(r"const version = '([0-9.]+)';", text).group(1)
        expected = 'version / git commit hash: "v' + version + '"'
    else:
        expected = re.search(r"test \"\$\(nats-server --version\)\" = '([^']+)'", source).group(1)
    intent = {'source': os.environ['GITHUB_SHA'], 'role': role, 'image': image,
              'producer': producer, 'container': name, 'nativeOnly': True, 'liveDeployment': False}
    (result / 'intent.json').write_text(json.dumps(intent, indent=2))
    try:
        docker('create', '--name', name, '--label', 'oasis.native.consumer=' + name,
               '--network', 'none', '--read-only', '--cpus', '1', '--memory', '256m',
               '--entrypoint', binary, image, '--version', timeout=30)
        completed = docker('start', '--attach', name, timeout=20)
        output = completed.stdout + completed.stderr
        assert len(output) <= 16384 and expected in output.splitlines()
        state = json.loads(docker('container', 'inspect', name, timeout=15).stdout)[0]
        assert state['Image'] == metadata['Id']
        assert not state['State']['Running'] and not state['State']['OOMKilled'] and state['State']['ExitCode'] == 0
        (result / 'acknowledged.json').write_text(json.dumps(intent | {
            'imageId': metadata['Id'], 'nativeVersionLine': expected,
            'versionOutputSha256': hashlib.sha256(output.encode()).hexdigest(),
            'isolatedReadOnlyProcessPass': True, 'webApiQualified': False}, indent=2))
    finally:
        observed = docker('container', 'inspect', name, check=False, timeout=15)
        if observed.returncode == 0:
            state = json.loads(observed.stdout)[0]
            assert state['Config']['Labels']['oasis.native.consumer'] == name
            if state['State']['Running']:
                docker('stop', '--time', '10', name, timeout=15)
            docker('rm', '--volumes', name, timeout=15)
        else:
            assert 'no such' in observed.stderr.lower(), 'Container outcome unknown.'
        assert absent()
        (result / 'cleanup.json').write_text(json.dumps({'ownedContainerAbsent': True, 'dataWrites': 0}))
    print(json.dumps({'role': role, 'image': image, 'producer': producer, 'passed': True, 'webApiQualified': False}))


if __name__ == '__main__':
    main()
