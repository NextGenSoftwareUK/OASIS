import socket
import unittest
from owned_overpass_proxy import OwnedOverpassProxy

class OwnedCatalogueBoundaryTest(unittest.TestCase):
    def test_unadmitted_client_and_other_targets_never_open_upstream(self):
        proxy = OwnedOverpassProxy('127.0.0.1')
        def refused(target):
            with socket.create_connection(proxy.server.server_address, timeout=2) as stream:
                stream.sendall(('CONNECT '+target+' HTTP/1.1\r\nHost: '+target+'\r\n\r\n').encode())
                self.assertIn(b' 403 ', stream.recv(4096).split(b'\r\n')[0])
        with proxy:
            refused('overpass-api.de:443')
            proxy.admit('127.0.0.1')
            for target in ('example.com:443','overpass-api.de:80','cluster.mongodb.net:27017'):
                refused(target)
            self.assertEqual(0, proxy.proof()['connections'])
            self.assertEqual(4, proxy.proof()['deniedConnections'])
        self.assertTrue(proxy.proof()['proxyStopped'])
        self.assertEqual(0, proxy.proof()['activeTunnels'])

if __name__ == '__main__': unittest.main()
