"""Materialize the exact pinned native OS source for an offline Kelvin update."""
import json
import os
from pathlib import Path
import platform
import shutil

from qualify_urbit_desk import blob_sha, checkout, git, sha


def main():
    assert platform.system() == 'Linux' and os.environ.get('GITHUB_ACTIONS') == 'true'
    root = Path(__file__).resolve().parents[2]
    inputs = json.loads((root / 'Scripts/TestHosts/urbit-runtime-inputs.json').read_text())
    out = root / 'TestResults/UrbitKernelCurrent'
    out.mkdir(parents=True, exist_ok=False)
    scratch = out / 'owned-source-checkouts'
    scratch.mkdir()
    cache = root / 'LocalState/COSMIC/CloudUrbitKernel408'
    assert not cache.exists()
    base = cache / 'base'
    base.mkdir(parents=True)
    report = dict(source=os.environ['GITHUB_SHA'], kernelCommit=inputs['kernelCommit'],
                  sourceAssemblyVerified=False, nativeKernelUpdated=False,
                  contactsQualified=False, cqlQualified=False, liveDeployment=False)
    (out / 'intent.json').write_text(json.dumps(report, indent=2))
    try:
        directory = checkout(scratch, 'kernel', 'urbit/urbit', inputs['kernelCommit'], ['pkg/'])
        tree = git(directory, 'ls-tree', '-rz', '--full-tree', inputs['kernelCommit'], '--', 'pkg/')
        entries = {}
        for item in tree.split(b'\0'):
            if item:
                header, path = item.split(b'\t', 1)
                mode, kind, oid = header.decode().split()
                assert kind == 'blob' and mode in ('100644', '100755', '120000')
                entries[path.decode()] = (mode, oid)
        # This commit's bounded package tree includes all arvo symlink targets.
        assert len(entries) == 1431
        package_bytes = 0
        for path, (mode, oid) in entries.items():
            source = directory / path
            payload = os.readlink(source).encode() if mode == '120000' else source.read_bytes()
            assert len(payload) <= 2 * 1024 * 1024 and blob_sha(payload) == oid
            package_bytes += len(payload)
        assert package_bytes == 9101886
        selected = {p: e for p, e in entries.items() if p.startswith('pkg/arvo/')}
        assert len(selected) == 539
        manifest = {}
        symlinks = 0
        directory_aliases = set()

        def materialize(source, relative, source_oid, depth=0):
            assert depth <= 12, 'Source alias recursion exceeds contract.'
            target = source.resolve(strict=True)
            assert target.is_relative_to((directory / 'pkg').resolve()), 'Source alias escapes pinned package tree.'
            if target.is_dir():
                for child in sorted(target.iterdir()):
                    materialize(child, str(Path(relative) / child.name), source_oid, depth + 1)
                return
            assert target.is_file()
            target_path = str(target.relative_to(directory))
            assert target_path in entries and entries[target_path][0] != '120000'
            payload = target.read_bytes()
            assert blob_sha(payload) == entries[target_path][1]
            assert relative not in manifest and len(manifest) < 3000
            destination = base / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(payload)
            manifest[relative] = dict(bytes=len(payload), sha256=sha(payload),
                                      sourceGitBlob=source_oid, resolvedPath=target_path)

        for path, (mode, oid) in selected.items():
            source = directory / path
            relative = str(Path(path).relative_to('pkg/arvo'))
            if source.resolve(strict=True).is_dir():
                assert mode == '120000'
                directory_aliases.add(path)
            materialize(source, relative, oid)
            symlinks += mode == '120000'
        assert directory_aliases == {'pkg/arvo/lib/verb', 'pkg/arvo/lib/wasm', 'pkg/arvo/mar/verb', 'pkg/arvo/sur/wasm'}
        assert sum(e['bytes'] for e in manifest.values()) <= 16 * 1024 * 1024
        assert symlinks == 151 and (base / 'sys.kelvin').read_text().strip() == '[%zuse 408]'
        payload = json.dumps(manifest, sort_keys=True, indent=2)
        (cache / 'files.json').write_text(payload)
        (out / 'files.json').write_text(payload)
        report.update(sourceAssemblyVerified=True, fileCount=len(manifest), symlinksMaterialized=symlinks,
                      directoryAliases=sorted(directory_aliases),
                      sourceBytes=sum(e['bytes'] for e in manifest.values()), filesManifestSha256=sha(payload.encode()))
        with open(os.environ['GITHUB_OUTPUT'], 'a') as stream:
            stream.write('assembled=true\n')
    finally:
        assert scratch.resolve().parent == out.resolve() and not scratch.is_symlink()
        shutil.rmtree(scratch)
        report['temporarySourceCheckoutRemoved'] = not scratch.exists()
        (out / 'qualification.json').write_text(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
