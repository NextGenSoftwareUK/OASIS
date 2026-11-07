"""Convert the retained, hash-qualified native NATS artifact into bounded file transport."""
import base64
import hashlib
import io
import json
from pathlib import Path
import re
import sys
import tarfile

def build_bundle(source):
    descriptor = (source / 'source.json').read_bytes()
    archive = (source / 'native-store.tar.gz').read_bytes()
    proof = json.loads((source / 'archive-qualified.json').read_bytes())
    if (hashlib.sha256(descriptor).hexdigest() != '5a8db3e17c3240289cbbb4e521cb63f8f1fb051722b8063e70664794f957709b'
            or hashlib.sha256(archive).hexdigest() != 'a9daaeef39473742cf869b0a9673686523f2771766815f07dbd3c5ba7f29ad37'
            or proof['nativeCounts'] != [3, 2]
            or not all(proof[key] is True for key in ('deniedWrite', 'deniedForeignBucket',
                'untrustedCaRejected', 'sourceUnchanged', 'sourceSeededOnce', 'nativeStopped'))):
        raise ValueError('The accepted native source qualification differs.')
    files = []
    names = set()
    with tarfile.open(fileobj=io.BytesIO(archive), mode='r:gz') as bundle:
        for member in bundle:
            if member.isdir():
                continue
            if (not member.isfile() or member.name in names or member.size > 1048576
                    or not re.fullmatch(r'jetstream/\$G/streams/KV_cosmic_cql_(public|foreign)/(meta\.inf|meta\.sum|msgs/([1-9][0-9]*\.blk|index\.db))', member.name)):
                raise ValueError('Unexpected qualified native source entry.')
            names.add(member.name)
            data = bundle.extractfile(member).read()
            files.append({'path': member.name, 'base64': base64.b64encode(data).decode(),
                'sha256': hashlib.sha256(data).hexdigest()})
    transport = json.dumps({'schema': 1, 'sourceSha256': hashlib.sha256(descriptor).hexdigest(),
        'files': files}, separators=(',', ':'))
    if len(transport.encode()) > 32768:
        raise ValueError('Qualified source exceeds its transport budget.')
    return descriptor.decode(), transport


if __name__ == "__main__":
    source, destination = map(Path, sys.argv[1:])
    _, transport = build_bundle(source)
    with destination.open("x", encoding="utf-8") as output:
        output.write(transport)
    print("PASS: accepted native source hashes verified; no native seed or save performed.")
