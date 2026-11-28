"""Owned CONNECT bridge for one bounded real Overpass catalogue check.

Mongo/API containers remain on the existing internal network. This bridge never
forwards plaintext HTTP or a non-Overpass destination and never inspects TLS data.
"""
import http.server
import ipaddress
import select
import socket
import threading
import time


class OwnedOverpassProxy:
    def __init__(self, gateway):
        if not ipaddress.ip_address(gateway).is_private:
            raise ValueError('Owned Docker gateway must be private.')
        self.client = None
        self.lock = threading.Lock()
        self.connections = 0
        self.denied = 0
        self.bytes = 0
        self.first_connection = None
        self.sockets = set()
        self.failures = []
        self.closed = False
        self.deadline_closes = 0
        owner = self

        class Handler(http.server.BaseHTTPRequestHandler):
            protocol_version = 'HTTP/1.1'
            def log_message(self, *args):
                pass  # TLS/catalogue payloads and request headers are never evidence logs.
            def do_CONNECT(self):
                with owner.lock:
                    permitted = (owner.client is not None and self.client_address[0] == owner.client
                        and self.path.lower() == 'overpass-api.de:443' and owner.connections < 2
                        and (owner.first_connection is None or time.monotonic()-owner.first_connection < 90))
                    if permitted:
                        owner.connections += 1
                        if owner.first_connection is None: owner.first_connection = time.monotonic()
                if not permitted:
                    with owner.lock: owner.denied += 1
                    self.send_error(403, 'Destination or owned connection limit refused.')
                    return
                upstream = None
                established = False
                try:
                    addresses = socket.getaddrinfo('overpass-api.de', 443, socket.AF_INET, socket.SOCK_STREAM)
                    if not addresses or any(not ipaddress.ip_address(row[4][0]).is_global for row in addresses):
                        raise ValueError('Overpass must resolve to public IPv4 addresses.')
                    upstream = socket.create_connection((addresses[0][4][0], 443), timeout=10)
                    upstream.settimeout(5)
                    self.connection.settimeout(5)
                    with owner.lock: owner.sockets.update((self.connection, upstream))
                    self.send_response(200, 'Connection Established')
                    self.end_headers()
                    established = True
                    self.close_connection = True
                    deadline = time.monotonic() + 45
                    while time.monotonic() < deadline:
                        ready, _, _ = select.select([self.connection, upstream], [], [], 1)
                        for source in ready:
                            payload = source.recv(32768)
                            if not payload: return
                            with owner.lock:
                                owner.bytes += len(payload)
                                if owner.bytes > 5 * 1024 * 1024:
                                    raise ValueError('Owned catalogue transfer limit exceeded.')
                            target = upstream if source is self.connection else self.connection
                            target.sendall(payload)
                    with owner.lock: owner.deadline_closes += 1
                    return
                except (OSError, ValueError):
                    with owner.lock:
                        if not owner.closed: owner.failures.append('OWNED_CATALOGUE_TRANSPORT_FAILED')
                    if not established:
                        self.send_error(502, 'Real catalogue upstream connection failed.')
                finally:
                    with owner.lock:
                        owner.sockets.discard(self.connection)
                        if upstream is not None: owner.sockets.discard(upstream)
                    if upstream is not None: upstream.close()
            def do_GET(self):
                self.send_error(403, 'Only the owned HTTPS catalogue tunnel is admitted.')
            do_POST = do_GET

        self.server = http.server.ThreadingHTTPServer((gateway, 0), Handler)
        self.server.daemon_threads = True
        self.thread = threading.Thread(target=self.server.serve_forever, name='owned-overpass', daemon=True)
        self.url = 'http://' + gateway + ':' + str(self.server.server_port)

    def __enter__(self):
        self.thread.start()
        return self

    def admit(self, address):
        if not ipaddress.ip_address(address).is_private:
            raise ValueError('Only the owned API container can use the catalogue bridge.')
        with self.lock:
            if self.client is not None: raise ValueError('Owned API client already assigned.')
            self.client = address

    def proof(self):
        with self.lock:
            return {'allowedHost':'overpass-api.de:443','connections':self.connections,'deniedConnections':self.denied,
                'ciphertextBytes':self.bytes,'maximumConnections':2,'maximumCiphertextBytes':5*1024*1024,
                'failures':len(self.failures),'boundedDeadlineCloses':self.deadline_closes,'proxyStopped':self.closed and not self.thread.is_alive(),
                'activeTunnels':len(self.sockets)}

    def __exit__(self, *args):
        with self.lock:
            self.closed = True
            sockets = list(self.sockets)
        for connection in sockets:
            try: connection.shutdown(socket.SHUT_RDWR)
            except OSError: pass  # Closing an already-ended owned connection is an allowed cleanup state.
            connection.close()
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(3)
        deadline = time.monotonic()+3
        while time.monotonic() < deadline:
            with self.lock:
                if not self.sockets: break
            time.sleep(.02)
        if self.thread.is_alive() or self.proof()['activeTunnels']:
            raise RuntimeError('Owned catalogue bridge did not stop.')
