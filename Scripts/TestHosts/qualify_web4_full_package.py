"""Inspect an immutable full WEB4 image in an owned cloud container; no API acceptance claim."""
import argparse, hashlib, json, os, re, subprocess
from pathlib import Path
from owned_docker_resources import missing_resource


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--source', required=True)
    p.add_argument('--digest', required=True)
    p.add_argument('--require-tron-reader', action='store_true')
    o = p.parse_args()
    assert os.environ.get('GITHUB_ACTIONS') == 'true', 'Cloud-only image qualification.'
    assert re.fullmatch('[a-f0-9]{40}', o.source)
    assert re.fullmatch('sha256:[a-f0-9]{64}', o.digest)
    root = Path(__file__).resolve().parents[2]
    assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip() == o.source
    run = os.environ['GITHUB_RUN_ID']; attempt = os.environ['GITHUB_RUN_ATTEMPT']
    assert run.isdecimal() and attempt.isdecimal()
    name = 'oasis-web4-package-' + run + '-' + attempt
    image = 'ghcr.io/nextgensoftwareuk/oasis-web4@' + o.digest
    tag = 'ghcr.io/nextgensoftwareuk/oasis-web4:' + o.source + '-teaching'
    out = root / 'TestResults/Web4FullPackage'
    out.mkdir(parents=True, exist_ok=False)

    def docker(*args, check=True, timeout=180):
        r = subprocess.run(['docker', *args], text=True, capture_output=True, timeout=timeout)
        if check and r.returncode:
            raise RuntimeError('Package operation failed: ' + r.stderr[-2000:])
        return r

    def absent():
        r = docker('container', 'inspect', name, check=False, timeout=15)
        if missing_resource('container', name, r.returncode, r.stderr):
            return True
        if r.returncode:
            raise RuntimeError('Package container state is unknown.')
        return False

    assert absent(), 'Never adopt existing containers.'
    docker('pull', image, timeout=600)
    docker('pull', tag, timeout=600)
    metadata = json.loads(docker('image', 'inspect', image).stdout)[0]
    tagged = json.loads(docker('image', 'inspect', tag).stdout)[0]
    assert image in metadata['RepoDigests'] and tagged['Id'] == metadata['Id']
    mapping = {'Docker/host-sandbox-openvino.cjs': '/app/host-sandbox-openvino.cjs',
               'Docker/host-sandbox-ipfs.cjs': '/app/host-sandbox-ipfs.cjs',
               'Scripts/TestHosts/openvino-model-server-pins.json': '/app/openvino-model-server-pins.json'}
    expected = {target: hashlib.sha256((root / source).read_bytes()).hexdigest()
                for source, target in mapping.items()}
    probe = r'''
const fs=require('node:fs'), crypto=require('node:crypto'), assert=require('node:assert/strict');
const expected=JSON.parse(process.argv[1]), requireTron=JSON.parse(process.argv[2]);
for(const [file,hash] of Object.entries(expected)) assert.equal(crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex'),hash);
for(const name of ['NextGenSoftware.OASIS.API.ONODE.WebAPI','NextGenSoftware.OASIS.API.Core','NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS','NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS','NextGenSoftware.OASIS.API.Providers.PolygonIDOASIS','NextGenSoftware.OASIS.API.Providers.SEEDSOASIS']) assert(fs.statSync('/app/'+name+'.dll').size>0,name);
if(requireTron){const reader=fs.readFileSync('/app/NextGenSoftware.OASIS.API.Providers.TRONOASIS.dll');for(const symbol of ['TronPublicHeaderMetadataSource','FromAuthenticatedJson'])assert(reader.includes(Buffer.from(symbol)),symbol);}
const pins=require('/app/openvino-model-server-pins.json');
for(const v of pins.versions)for(const [name,p]of Object.entries(pins.files)){const b=fs.readFileSync('/models/'+pins.modelName+'/'+v+'/'+name);assert.equal(b.length,p.bytes);assert.equal(crypto.createHash('sha256').update(b).digest('hex'),p.sha256);}
assert.equal(fs.realpathSync('/ovms'),'/opt/ovms-rootfs/ovms');
assert(fs.statSync('/opt/ovms-rootfs/usr/lib/x86_64-linux-gnu/ld-linux-x86-64.so.2').size>0);
console.log(JSON.stringify({packagedSourceHashes:true,assemblies:requireTron?7:6,tronReaderPackaged:requireTron,modelVersions:pins.versions,modelHashes:true,apiQualified:false}));
'''
    try:
        docker('create', '--name', name, '--label', 'oasis.package.owner=' + name,
               '--network', 'none', '--read-only', '--cpus', '1', '--memory', '256m',
               '--entrypoint', 'node', image, '-e', probe, json.dumps(expected), json.dumps(o.require_tron_reader))
        r = docker('start', '--attach', name, timeout=30)
        ack = json.loads(r.stdout)
        state = json.loads(docker('inspect', name).stdout)[0]
        assert state['Image'] == metadata['Id'] and state['State']['ExitCode'] == 0
        assert not state['State']['Running'] and not state['State']['OOMKilled']
        assert ack['apiQualified'] is False and ack['assemblies'] == (7 if o.require_tron_reader else 6)
        assert ack['tronReaderPackaged'] is o.require_tron_reader
        (out / 'acknowledged.json').write_text(json.dumps({'source': o.source, 'image': image,
            'tag': tag, 'imageId': metadata['Id'], 'expectedHashes': expected, 'checks': ack,
            'liveDeployment': False}, indent=2))
    finally:
        r = docker('container', 'inspect', name, check=False)
        if r.returncode == 0:
            state = json.loads(r.stdout)[0]
            assert state['Config']['Labels']['oasis.package.owner'] == name
            if state['State']['Running']:
                docker('stop', '--time', '10', name, timeout=15)
            docker('rm', '--volumes', name)
        else:
            assert missing_resource('container', name, r.returncode, r.stderr), 'Container state unknown.'
        assert absent()
        (out / 'cleanup.json').write_text(json.dumps({'ownedContainerAbsent': True, 'dataWrites': 0}))


if __name__ == '__main__':
    main()
