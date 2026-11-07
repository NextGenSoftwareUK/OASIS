"""Linux-only disposable native TLS source qualification; never opens retained local fixtures."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import secrets
import signal
import socket
import subprocess
import sys
import tarfile
import time


def write_new(path, value):
    with path.open('x', encoding='utf-8') as stream:
        json.dump(value, stream, separators=(',', ':'))
        stream.flush()
        os.fsync(stream.fileno())


def archive_store(store, target):
    files = sorted(store.rglob('*'))
    if any(path.is_symlink() or not (path.is_file() or path.is_dir()) for path in files):
        raise RuntimeError('Unexpected native source archive entry.')
    if sum(path.stat().st_size for path in files if path.is_file()) > 16777216:
        raise RuntimeError('Native source archive exceeds its retained byte budget.')
    with tarfile.open(target, 'w:gz') as output:
        for path in files:
            output.add(path, arcname=str(path.relative_to(store)), recursive=False)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('binary', type=Path)
    parser.add_argument('consumer', type=Path)
    parser.add_argument('work', type=Path)
    args = parser.parse_args()
    if sys.platform != 'linux':
        raise RuntimeError('This qualification requires the isolated Linux CI runner.')
    binary, consumer, work = args.binary.resolve(strict=True), args.consumer.resolve(strict=True), args.work.resolve()
    version = subprocess.run([str(binary), '--version'], capture_output=True, check=True, timeout=10).stdout.decode().strip()
    if version != 'nats-server: v2.15.2':
        raise RuntimeError('Pinned native NATS version differs.')
    if work.exists():
        raise RuntimeError('Existing qualification state must be reconciled, never reseeded.')
    ca_root = Path('/data/COSMIC/SandboxNATS')
    if ca_root.exists() or ca_root.is_symlink():
        raise RuntimeError('The dedicated native TLS path must be unoccupied.')
    if ca_root.parent.resolve(strict=True) != ca_root.parent:
        raise RuntimeError('Native TLS parent must be a real owned directory.')
    work.mkdir(mode=0o700)
    results = work / 'results'; results.mkdir(mode=0o700)
    store = work / 'store'; store.mkdir(mode=0o700)
    ca_root.mkdir(mode=0o700)
    write_new(work / 'qualification.intent.json', {'schema': 1, 'version': '2.15.2',
        'ownedSource': str(store), 'credentialArtifactsPermitted': False})
    writer_password, reader_password = secrets.token_urlsafe(32), secrets.token_urlsafe(32)
    config = {'host': '127.0.0.1', 'port': 24222, 'server_name': 'cosmic-cql-owned',
        'jetstream': {'store_dir': str(store), 'max_memory_store': 16777216, 'max_file_store': 33554432},
        'tls': {'cert_file': str(ca_root / 'ca.pem'), 'key_file': str(ca_root / 'server.key')},
        'authorization': {'users': [
            {'user': 'writer', 'password': writer_password,
             'permissions': {'publish': {'allow': ['>']}, 'subscribe': {'allow': ['>']}}},
            {'user': 'cql_reader', 'password': reader_password, 'permissions': {
                'publish': {'allow': ['$JS.API.INFO', '$JS.API.STREAM.INFO.KV_cosmic_cql_public',
                    '$JS.API.DIRECT.GET.KV_cosmic_cql_public', '$JS.API.DIRECT.GET.KV_cosmic_cql_public.>',
                    '$JS.API.STREAM.MSG.GET.KV_cosmic_cql_public']},
                'subscribe': {'allow': ['_INBOX.>']}}}]}}
    server = None
    succeeded = False
    try:
        subprocess.run(['openssl', 'req', '-x509', '-newkey', 'rsa:2048', '-sha256', '-nodes',
            '-keyout', str(ca_root / 'server.key'), '-out', str(ca_root / 'ca.pem'), '-days', '2',
            '-subj', '/CN=localhost', '-addext', 'subjectAltName=IP:127.0.0.1,DNS:localhost'],
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, timeout=30, check=True)
        (ca_root / 'server.key').chmod(0o600)
        write_new(ca_root / 'server.conf', config)
        (ca_root / 'server.conf').chmod(0o600)
        environment = {key: os.environ[key] for key in ('PATH', 'HOME', 'TMPDIR') if key in os.environ}
        with (work / 'native-server.log').open('wb') as server_log:
            server = subprocess.Popen([str(binary), '-c', str(ca_root / 'server.conf')],
                stdout=server_log, stderr=subprocess.STDOUT, env=environment)
            deadline = time.monotonic() + 20
            while True:
                if server.poll() is not None:
                    raise RuntimeError('Native TLS server stopped before readiness; private logs retained.')
                try:
                    with socket.create_connection(('127.0.0.1', 24222), timeout=1):
                        break
                except ConnectionRefusedError:
                    if time.monotonic() >= deadline:
                        raise RuntimeError('Native listener startup deadline elapsed.') from None
                    time.sleep(0.1)
            client_environment = dict(os.environ, OASIS_NATS_OWNED_QUALIFICATION=str(results),
                OASIS_NATS_OWNED_WRITER_PASSWORD=writer_password, OASIS_NATS_OWNED_READER_PASSWORD=reader_password)
            with (work / 'native-consumer.log').open('wb') as client_log:
                result = subprocess.run(['dotnet', str(consumer)], env=client_environment,
                    stdout=client_log, stderr=subprocess.STDOUT, timeout=90)
            if result.returncode:
                diagnostic = (work / 'native-consumer.log').read_text(encoding='utf-8', errors='replace')[-12000:]
                for secret in (writer_password, reader_password):
                    diagnostic = diagnostic.replace(secret, '[REDACTED]')
                write_new(results / 'consumer-failure.json', {'exitCode': result.returncode,
                    'diagnostic': diagnostic})
                raise RuntimeError('Real TLS/CQL source qualification failed; no native proof claimed.')
            succeeded = True
    finally:
        if server is not None and server.poll() is None:
            server.send_signal(signal.SIGINT)
            try:
                server.wait(timeout=20)
            except subprocess.TimeoutExpired:
                server.kill(); server.wait(timeout=10)
                succeeded = False
        write_new(work / 'native-stop.acknowledged.json', {'stopped': server is None or server.poll() is not None,
            'exitCode': server.returncode if server else None, 'qualificationPassed': succeeded})
        if ca_root.resolve(strict=True) != ca_root or set(path.name for path in ca_root.iterdir()) - {'server.key', 'ca.pem', 'server.conf'}:
            raise RuntimeError('Private TLS cleanup target differs; no removal attempted.')
        for name in ('server.key', 'ca.pem', 'server.conf'):
            (ca_root / name).unlink(missing_ok=True)
        ca_root.rmdir()
        # Retain partial owned data and mutation journals on failure, without
        # promoting it to a qualified input or retaining credentials/logs.
        archive_store(store, results / 'native-store.tar.gz')
        write_new(results / 'native-outcome.json', {'nativeStopped': server is None or server.poll() is not None,
            'qualificationPassed': succeeded, 'qualifiedInput': succeeded and server.returncode == 0})
    if not succeeded or server.returncode != 0:
        raise RuntimeError('Native qualification or shutdown was not successful.')
    archive = results / 'native-store.tar.gz'
    proof = json.loads((results / 'qualification.json').read_bytes())
    proof.update({'archiveBytes': archive.stat().st_size,
        'archiveSha256': hashlib.sha256(archive.read_bytes()).hexdigest(), 'nativeStopped': True,
        'sourceSha256': hashlib.sha256((results / 'source.json').read_bytes()).hexdigest()})
    write_new(results / 'archive-qualified.json', proof)
    print('PASS: native TLS/CQL3/2, explicit permission denials, untrusted CA rejection, source unchanged and owned server stopped.')


if __name__ == '__main__':
    main()
