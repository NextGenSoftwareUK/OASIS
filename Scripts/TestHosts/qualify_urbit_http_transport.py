"""Cloud-only isolated Docker publication diagnostic; no native ship or secrets."""
import json
import os
from pathlib import Path
import platform
import subprocess

from owned_docker_resources import missing_resource


def main():
    assert platform.system() == 'Linux' and os.environ.get('GITHUB_ACTIONS') == 'true'
    run, attempt = os.environ['GITHUB_RUN_ID'], os.environ['GITHUB_RUN_ATTEMPT']
    assert run.isdecimal() and attempt.isdecimal()
    root = Path(__file__).resolve().parents[2]
    image = json.loads((root / 'Scripts/TestHosts/urbit-runtime-inputs.json').read_text())['containerImage']
    owner = 'urbit-transport-' + run + '-' + attempt
    network = owner + '-http'
    label = 'oasis.urbit.transport.owner'
    out = root / 'TestResults/UrbitHttpTransportCurrent'
    out.mkdir(parents=True, exist_ok=False)
    report = dict(source=os.environ['GITHUB_SHA'], owner=owner, diagnosticOnly=True)

    def docker(*args, check=True):
        result = subprocess.run(['docker', *args], capture_output=True, text=True, timeout=120)
        if check:
            result.check_returncode()
        return result

    def inspect(kind, name):
        result = docker(kind, 'inspect', name, check=False)
        if missing_resource(kind, name, result.returncode, result.stderr):
            return None
        result.check_returncode()
        state = json.loads(result.stdout)[0]
        labels = state['Config']['Labels'] if kind == 'container' else state['Labels']
        assert labels.get(label) == owner
        return state

    assert inspect('container', owner) is None
    assert inspect('network', network) is None
    try:
        report['engineVersion'] = docker('version', '--format', '{{.Server.Version}}').stdout.strip()
        docker('network', 'create', '--internal', '--label', label + '=' + owner, network)
        state = inspect('network', network)
        assert state['Internal'] is True
        report['networkOptions'] = state['Options']
        docker('run', '-d', '--name', owner, '--label', label + '=' + owner,
               '--network', network, '--publish', '127.0.0.1::80',
               '--memory', '64m', '--pids-limit', '32', '--cap-drop', 'ALL',
               '--security-opt', 'no-new-privileges', '--read-only', image,
               'node', '-e', "require('http').createServer((q,s)=>s.end('owned transport')).listen(80,'0.0.0.0')")
        state = inspect('container', owner)
        assert state['State']['Running'] is True
        report['requestedBindings'] = state['HostConfig']['PortBindings']
        report['effectivePorts'] = state['NetworkSettings']['Ports']
        report['dockerPortOutput'] = docker('port', owner, '80/tcp').stdout.strip()
    finally:
        state = inspect('container', owner)
        if state is not None:
            docker('stop', '--time', '10', owner)
            docker('rm', owner)
        report['ownedContainerRemoved'] = inspect('container', owner) is None
        state = inspect('network', network)
        if state is not None:
            assert state['Internal'] is True and not state['Containers']
            docker('network', 'rm', network)
        report['ownedNetworkRemoved'] = inspect('network', network) is None
        (out / 'qualification.json').write_text(json.dumps(report, indent=2))
    assert report['ownedContainerRemoved'] and report['ownedNetworkRemoved']


if __name__ == '__main__':
    main()
