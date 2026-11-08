"""One explicit client transport: loopback inside the owned internal network."""
import os


def client_command(state, owner, label, network, command):
    assert state['Config']['Labels'].get(label) == owner
    assert state['State']['Running'] is True and state['State']['Pid'] > 1
    assert state['HostConfig']['NetworkMode'] == network
    assert set(state['NetworkSettings']['Networks']) == {network}
    assert not state['HostConfig'].get('PortBindings')
    assert all(not value for value in state['NetworkSettings']['Ports'].values())
    # Enter only the network namespace, retaining the runner filesystem. Drop
    # back to the runner identity before executing the frozen HTTP client.
    return ['sudo', '-n', 'nsenter', '--target', str(state['State']['Pid']), '--net',
            '--setuid', str(os.getuid()), '--setgid', str(os.getgid()), '--',
            '/usr/bin/env', 'GITHUB_ACTIONS=true', *command]
