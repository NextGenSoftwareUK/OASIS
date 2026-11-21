"""Qualify owned internal Mongo topology/cleanup without booting a native ship."""
import json
import os
from pathlib import Path
import platform
import subprocess

from owned_urbit_mongo import owned_mongo
from owned_docker_resources import missing_resource


def main():
    assert platform.system() == 'Linux' and os.environ.get('GITHUB_ACTIONS') == 'true'
    run, attempt = os.environ['GITHUB_RUN_ID'], os.environ['GITHUB_RUN_ATTEMPT']
    assert run.isdecimal() and attempt.isdecimal()
    owner = 'urbit-contacts-' + run + '-' + attempt
    network = owner + '-http'
    label = 'oasis.urbit.contacts.owner'
    root = Path(__file__).resolve().parents[2]
    out = root / 'TestResults/UrbitMongoTransportCurrent'
    out.mkdir(parents=True, exist_ok=False)
    report = dict(source=os.environ['GITHUB_SHA'], owner=owner, transportOnly=True)

    def docker(*args, check=True):
        result = subprocess.run(['docker', *args], capture_output=True, text=True, timeout=60)
        if check:
            result.check_returncode()
        return result

    def inspect():
        result = docker('network', 'inspect', network, check=False)
        if missing_resource('network', network, result.returncode, result.stderr):
            return None
        result.check_returncode()
        state = json.loads(result.stdout)[0]
        assert state['Labels'].get(label) == owner and state['Internal'] is True
        return state

    assert inspect() is None
    intended = False
    try:
        intended = True
        docker('network', 'create', '--internal', '--label', label + '=' + owner, network)
        with owned_mongo(dict(root=root, out=out, report=report)) as metadata:
            assert metadata['noPublishedPorts'] and metadata['internalNetwork'] and metadata['replicaReady']
            report['ownedReplicaReady'] = True
    finally:
        if intended:
            state = inspect()
            if state is not None:
                assert not state['Containers']
                docker('network', 'rm', network)
            report['ownedNetworkRemoved'] = inspect() is None
        (out / 'qualification.json').write_text(json.dumps(report, indent=2))
    assert report['ownedReplicaReady'] and report['ownedNetworkRemoved']


if __name__ == '__main__':
    main()
