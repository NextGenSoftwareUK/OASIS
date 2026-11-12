"""One owned replica on the native fixture's existing internal network."""
from contextlib import contextmanager
import ipaddress
import json
import os
import platform
import subprocess
import time

from owned_docker_resources import missing_resource


@contextmanager
def owned_mongo(context):
    assert platform.system() == 'Linux' and os.environ.get('GITHUB_ACTIONS') == 'true'
    owner = context['report']['owner']
    network = owner + '-http'
    label = 'oasis.urbit.mongo.owner'
    name = owner + '-mongo'
    volumes = [name + '-data', name + '-config']
    inputs = json.loads((context['root'] / 'Scripts/TestHosts/urbit-mongo-inputs.json').read_text())
    image = inputs['image']
    assert image.startswith('mongo:8.0.15@sha256:') and len(image.split('@sha256:')[1]) == 64
    assert inputs['port'] == 27029 and inputs['replicaSet'] == 'oasisUrbitRs'

    def docker(*args, check=True, timeout=60):
        result = subprocess.run(['docker', *args], capture_output=True, text=True, timeout=timeout)
        if check:
            result.check_returncode()
        return result

    def inspect(kind, value):
        result = docker(kind, 'inspect', value, check=False)
        if missing_resource(kind, value, result.returncode, result.stderr):
            return None
        result.check_returncode()
        return json.loads(result.stdout)[0]

    net = inspect('network', network)
    assert net is not None and net['Internal'] is True
    assert net['Labels']['oasis.urbit.contacts.owner'] == owner
    assert inspect('container', name) is None, 'Never adopt an existing container.'
    for volume in volumes:
        assert inspect('volume', volume) is None, 'Never adopt an existing volume.'
    with (context['out'] / 'mongo-container-intent.json').open('x') as stream:
        json.dump(dict(owner=owner, container=name, volumes=volumes, image=image, network=network), stream)
    intended_volumes = []
    container_intended = False
    cleanup = dict(owner=owner)
    try:
        for volume in volumes:
            intended_volumes.append(volume)
            docker('volume', 'create', '--label', label + '=' + owner, volume)
        container_intended = True
        docker('create', '--name', name, '--label', label + '=' + owner,
               '--network', network, '--memory', '1g', '--memory-swap', '1g', '--cpus', '1', '--pids-limit', '128',
               '--mount', 'type=volume,source=' + volumes[0] + ',target=/data/db',
               '--mount', 'type=volume,source=' + volumes[1] + ',target=/data/configdb',
               image, '--port', '27029', '--replSet', 'oasisUrbitRs', '--bind_ip_all', timeout=180)
        docker('start', name)
        state = inspect('container', name)
        assert state['Config']['Labels'].get(label) == owner and state['State']['Running']
        assert set(state['NetworkSettings']['Networks']) == {network}
        assert not state['HostConfig'].get('PortBindings')
        address = state['NetworkSettings']['Networks'][network]['IPAddress']
        ip = ipaddress.IPv4Address(address)
        assert any(ip in ipaddress.ip_network(entry['Subnet']) for entry in net['IPAM']['Config'])

        def shell(statement):
            return docker('exec', name, 'mongosh', '--port', '27029', '--quiet', '--norc',
                          '--eval', statement, check=False, timeout=10)

        deadline = time.monotonic() + 45
        while True:
            result = shell('db.adminCommand({ping:1}).ok')
            if result.returncode == 0:
                break
            assert 'ECONNREFUSED' in result.stdout + result.stderr or 'connection refused' in (result.stdout + result.stderr).lower()
            assert time.monotonic() < deadline, 'Owned Mongo listener deadline exceeded.'
            time.sleep(1)
        configuration = dict(_id='oasisUrbitRs', members=[dict(_id=0, host=address + ':27029')])
        result = shell('rs.initiate(' + json.dumps(configuration) + ')')
        result.check_returncode()
        deadline = time.monotonic() + 45
        while True:
            result = shell('if(!db.hello().isWritablePrimary)quit(2)')
            if result.returncode == 0:
                break
            assert result.returncode == 2, 'Unexpected replica readiness failure.'
            assert time.monotonic() < deadline, 'Owned Mongo primary deadline exceeded.'
            time.sleep(1)
        pieces = owner.split('-')
        assert pieces[:2] == ['urbit', 'contacts'] and len(pieces) == 4
        assert pieces[2].isdecimal() and pieces[3].isdecimal()
        database = 'urbit_web4_' + pieces[2] + '_' + pieces[3]
        metadata = dict(owner=owner, container=name, image=image, imageId=state['Image'],
                        network=network, internalNetwork=True, noPublishedPorts=True,
                        mongoUri='mongodb://' + address + ':27029/?replicaSet=oasisUrbitRs&directConnection=true&serverSelectionTimeoutMS=5000&retryReads=false&retryWrites=false',
                        database=database, replicaReady=True)
        with (context['out'] / 'mongo-ready.json').open('x') as stream:
            json.dump(metadata, stream, indent=2)
        yield metadata
    finally:
        try:
            if container_intended:
                state = inspect('container', name)
                cleanup['containerObserved'] = state is not None
                if state is not None:
                    assert state['Config']['Labels'].get(label) == owner
                    assert {item['Name'] for item in state['Mounts'] if item['Type'] == 'volume'} == set(volumes)
                    if state['State']['Running']:
                        docker('stop', '--time', '20', name)
                        state = inspect('container', name)
                    cleanup.update(exitCode=state['State']['ExitCode'], oomKilled=state['State']['OOMKilled'])
                    docker('rm', name)
            cleanup['ownedContainerRemoved'] = inspect('container', name) is None
            for volume in intended_volumes:
                state = inspect('volume', volume)
                if state is not None:
                    assert state['Labels'].get(label) == owner
                    docker('volume', 'rm', volume)
                assert inspect('volume', volume) is None
            cleanup['ownedVolumesRemoved'] = True
        finally:
            (context['out'] / 'mongo-cleanup.json').write_text(json.dumps(cleanup, indent=2))
        assert cleanup.get('ownedContainerRemoved') is True
        if cleanup.get('containerObserved'):
            assert cleanup.get('exitCode') == 0 and cleanup.get('oomKilled') is False
