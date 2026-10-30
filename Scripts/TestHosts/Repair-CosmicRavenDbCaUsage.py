"""Complete the original pre-admission CA certificate using its original key/dates/serial."""
from pathlib import Path
import argparse, hashlib, json, os, subprocess

STATE = Path('/var/tmp/oasis-ravendb-cql')
assert STATE.resolve() == STATE
assert (STATE / 'environment.pending.json').exists()
assert not (STATE / 'environment.json').exists() and not (STATE / 'database.ack.json').exists()
parser = argparse.ArgumentParser()
parser.add_argument('--complete-receipt', action='store_true')
options = parser.parse_args()
if options.complete_receipt:
    assert (STATE / 'ca-usage-repair.json').stat().st_size == 0
    assert (STATE / 'ca-initial-invalid.pem').exists() and not (STATE / 'ca-corrected.pem').exists()
else:
    assert not (STATE / 'ca-usage-repair.json').exists() and not (STATE / 'ca-initial-invalid.pem').exists()
ack = json.loads((STATE / 'container.ack.json').read_text())
native = json.loads(subprocess.check_output(['podman', 'inspect', ack['id']], text=True))[0]
assert native['Image'] == ack['imageId'] == '933a130ff10d66a6a28b8829c5f6f7af886162fc2425b4012a9f41649072816e'
assert not native['State']['Running'] and native['State']['ExitCode'] == 0

def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()
def public_key(path):
    return subprocess.check_output(['openssl', 'x509', '-in', str(path), '-noout', '-pubkey'])

ca = STATE / 'ca.pem'
if options.complete_receipt:
    before = digest(STATE / 'ca-initial-invalid.pem')
    assert public_key(STATE / 'ca-initial-invalid.pem') == public_key(ca)
    subprocess.run(['openssl', 'verify', '-x509_strict', '-CAfile', str(ca), str(STATE / 'client.pem')],
        check=True, stdout=subprocess.DEVNULL)
else:
    corrected = STATE / 'ca-corrected.pem'
    before = digest(ca)
    unchanged = {name: digest(STATE / name) for name in ['ca.key', 'client.key', 'client.pem', 'client.pfx', 'certs/server.pfx']}
    config = STATE / 'ca-usage-extensions.cnf'
    with config.open('x') as file:
        os.chmod(config, 0o600)
        file.write('basicConstraints=critical,CA:TRUE\nkeyUsage=critical,keyCertSign,cRLSign\nsubjectKeyIdentifier=hash\n')
    subprocess.run(['openssl', 'x509', '-in', str(ca), '-signkey', str(STATE / 'ca.key'),
        '-preserve_dates', '-extfile', str(config), '-out', str(corrected)], check=True,
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    assert public_key(ca) == public_key(corrected), 'Original CA identity must remain unchanged'
    subprocess.run(['openssl', 'verify', '-x509_strict', '-CAfile', str(corrected), str(STATE / 'client.pem')],
        check=True, stdout=subprocess.DEVNULL)
    assert all(digest(STATE / name) == value for name, value in unchanged.items())
    os.chmod(corrected, 0o600); os.chown(corrected, STATE.stat().st_uid, STATE.stat().st_gid)
    ca.rename(STATE / 'ca-initial-invalid.pem'); corrected.rename(ca)
result = {'reason': 'Initial CA missing critical key usage; strict verification retained',
          'originalCertificateSha256': before, 'correctedCertificateSha256': digest(ca),
          'sameCaKey': True, 'leafAndPrivateMaterialUnchanged': True}
with (STATE / 'ca-usage-repair.json').open('w' if options.complete_receipt else 'x') as file:
    os.fchmod(file.fileno(), 0o600); json.dump(result, file); file.flush(); os.fsync(file.fileno())
print(json.dumps(result))
