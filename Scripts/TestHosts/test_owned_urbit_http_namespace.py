"""Refuse entry into a network namespace outside the owned isolated fixture."""
import copy
import unittest
from unittest.mock import patch

from owned_urbit_http_namespace import client_command


class OwnedNamespaceTests(unittest.TestCase):
    def setUp(self):
        self.owner = 'urbit-owned-123-1'
        self.label = 'oasis.urbit.contacts.owner'
        self.network = self.owner + '-http'
        self.state = {
            'Config': {'Labels': {self.label: self.owner}},
            'State': {'Running': True, 'Pid': 1234},
            'HostConfig': {'NetworkMode': self.network, 'PortBindings': {}},
            'NetworkSettings': {'Networks': {self.network: {}}, 'Ports': {}},
        }

    def command(self, state, native_web4_input=False):
        with patch('os.getuid', return_value=1001, create=True), patch('os.getgid', return_value=1002, create=True), patch('os.path.expanduser', return_value='/home/runner'):
            return client_command(state, self.owner, self.label, self.network, ['/frozen/dotnet', '/frozen/reader.dll'], native_web4_input=native_web4_input)

    def test_owned_client_drops_to_runner_and_enters_only_network(self):
        command = self.command(self.state)
        self.assertEqual(command, ['sudo', '-n', 'nsenter', '--target', '1234', '--net',
            '--setuid', '1001', '--setgid', '1002', '--', '/usr/bin/env',
            'GITHUB_ACTIONS=true', 'HOME=/home/runner', 'DOTNET_CLI_HOME=/home/runner',
            'DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1', 'DOTNET_NOLOGO=1', '/frozen/dotnet', '/frozen/reader.dll'])

    def test_native_input_preserves_only_the_named_private_field(self):
        command = self.command(self.state, native_web4_input=True)
        self.assertEqual(command[2], '--preserve-env=COSMIC_URBIT_NATIVE_WEB4_INPUT')
        self.assertNotIn('--preserve-env', command)
        with self.assertRaises(AssertionError): self.command(self.state, native_web4_input='ALL')

    def test_wrong_owner_is_rejected(self):
        self.state['Config']['Labels'][self.label] = 'another-owner'
        with self.assertRaises(AssertionError): self.command(self.state)

    def test_stopped_process_is_rejected(self):
        self.state['State']['Running'] = False
        with self.assertRaises(AssertionError): self.command(self.state)

    def test_host_or_missing_process_is_rejected(self):
        for pid in [0, 1]:
            with self.subTest(pid=pid):
                self.state['State']['Pid'] = pid
                with self.assertRaises(AssertionError): self.command(self.state)

    def test_different_network_mode_is_rejected(self):
        self.state['HostConfig']['NetworkMode'] = 'host'
        with self.assertRaises(AssertionError): self.command(self.state)

    def test_additional_or_missing_network_is_rejected(self):
        for networks in [{}, {self.network: {}, 'external': {}}]:
            with self.subTest(networks=networks):
                state = copy.deepcopy(self.state)
                state['NetworkSettings']['Networks'] = networks
                with self.assertRaises(AssertionError): self.command(state)

    def test_requested_or_effective_publication_is_rejected(self):
        for section, key in [('HostConfig', 'PortBindings'), ('NetworkSettings', 'Ports')]:
            with self.subTest(section=section):
                state = copy.deepcopy(self.state)
                state[section][key] = {'80/tcp': [{'HostIp': '127.0.0.1', 'HostPort': '12345'}]}
                with self.assertRaises(AssertionError): self.command(state)


if __name__ == '__main__':
    unittest.main()
