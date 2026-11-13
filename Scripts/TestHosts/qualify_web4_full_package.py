"""Inspect an immutable full WEB4 image in an owned cloud container; no API acceptance claim."""
import argparse, hashlib, json, os, re, subprocess
from pathlib import Path
from owned_docker_resources import missing_resource


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--source', required=True)
    p.add_argument('--digest', required=True)
    p.add_argument('--require-tron-reader', action='store_true')
    p.add_argument('--require-urbit-reader', action='store_true')
    p.add_argument('--require-infura-reader', action='store_true')
    p.add_argument('--profile', choices=['full', 'tron-api', 'maplibre'], default='full')
    o = p.parse_args()
    assert not (o.require_urbit_reader or o.require_infura_reader) or o.profile == 'full', 'Urbit and Infura require the full WEB4 package.'
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
    mapping = {'Docker/host-sandbox-ipfs.cjs': '/app/host-sandbox-ipfs.cjs'}
    if o.profile == 'maplibre':
        mapping['Docker/host-sandbox-waku.cjs'] = '/app/host-sandbox-waku.cjs'
    if o.profile == 'full':
        mapping.update({'Docker/host-sandbox-openvino.cjs': '/app/host-sandbox-openvino.cjs',
                        'Scripts/TestHosts/openvino-model-server-pins.json': '/app/openvino-model-server-pins.json'})
    require_tron = o.require_tron_reader or o.profile == 'tron-api'
    expected = {target: hashlib.sha256((root / source).read_bytes()).hexdigest()
                for source, target in mapping.items()}
    probe = r'''
const fs=require('node:fs'), crypto=require('node:crypto'), assert=require('node:assert/strict');
const expected=JSON.parse(process.argv[1]), requireTron=JSON.parse(process.argv[2]), profile=process.argv[3], requireUrbit=JSON.parse(process.argv[4]), requireInfura=JSON.parse(process.argv[5]);
assert(['full','tron-api','maplibre'].includes(profile));
assert(profile!=='tron-api'||requireTron, 'TRON API package requires its reader');
assert(!(requireUrbit||requireInfura)||profile==='full', 'Urbit and Infura require the full WEB4 package');
for(const [file,hash] of Object.entries(expected)) assert.equal(crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex'),hash);
const names=['NextGenSoftware.OASIS.API.ONODE.WebAPI','NextGenSoftware.OASIS.API.Core'];
if(profile==='maplibre')names.push('NextGenSoftware.OASIS.API.Providers.MapLibreOASIS');
if(profile==='full')names.push('NextGenSoftware.OASIS.API.Providers.IntelOpenVINOOASIS','NextGenSoftware.OASIS.API.Providers.TheSandboxOASIS','NextGenSoftware.OASIS.API.Providers.PolygonIDOASIS','NextGenSoftware.OASIS.API.Providers.SEEDSOASIS');
for(const name of names)assert(fs.statSync('/app/'+name+'.dll').size>0,name);
if(requireTron){const reader=fs.readFileSync('/app/NextGenSoftware.OASIS.API.Providers.TRONOASIS.dll');for(const symbol of ['TronPublicHeaderMetadataSource','FromAuthenticatedJson'])assert(reader.includes(Buffer.from(symbol)),symbol);}
function requireProjectReader(provider, version, readerSymbols, registrationSymbols) {
 const name='NextGenSoftware.OASIS.API.Providers.'+provider, file=name+'.dll';
 const reader=fs.readFileSync('/app/'+file);
 for(const symbol of readerSymbols)assert(reader.includes(Buffer.from(symbol)),symbol);
 const api=fs.readFileSync('/app/NextGenSoftware.OASIS.API.ONODE.WebAPI.dll');
 for(const symbol of registrationSymbols)assert(api.includes(Buffer.from(symbol)),symbol);
 const deps=JSON.parse(fs.readFileSync('/app/NextGenSoftware.OASIS.API.ONODE.WebAPI.deps.json','utf8'));
 const key=name+'/'+version, target=deps.targets[deps.runtimeTarget.name];
 assert.equal(deps.libraries[key].type,'project');
 assert(target[key].runtime[file], provider+' runtime assembly must be in the dependency graph');
 const apiKeys=Object.keys(target).filter(k=>k.startsWith('NextGenSoftware.OASIS.API.ONODE.WebAPI/'));
 assert.equal(apiKeys.length,1);
 assert.equal(target[apiKeys[0]].dependencies[name],version);
}
if(requireUrbit)requireProjectReader('UrbitOASIS','2.0.0',
 ['UrbitContactsV1Client','ReadBookPageAsync','UrbitContactBookQuerySource'],
 ['CqlUrbitReadSourceRegistration','AddUrbitReadSource']);
if(requireInfura)requireProjectReader('InfuraOASIS','2.0.1',
 ['InfuraPublicAccountMetadataSource','CredentialHandler'],
 ['CqlInfuraReadSourceRegistration','AddInfuraReadSource']);
let versions=[];
if(profile==='full'){
const pins=require('/app/openvino-model-server-pins.json');
for(const v of pins.versions)for(const [name,p]of Object.entries(pins.files)){const b=fs.readFileSync('/models/'+pins.modelName+'/'+v+'/'+name);assert.equal(b.length,p.bytes);assert.equal(crypto.createHash('sha256').update(b).digest('hex'),p.sha256);}
assert.equal(fs.realpathSync('/ovms'),'/opt/ovms-rootfs/ovms');
assert(fs.statSync('/opt/ovms-rootfs/usr/lib/x86_64-linux-gnu/ld-linux-x86-64.so.2').size>0);
versions=pins.versions;
}
console.log(JSON.stringify({packagedSourceHashes:true,assemblies:names.length+(requireTron?1:0)+(requireUrbit?1:0)+(requireInfura?1:0),tronReaderPackaged:requireTron,urbitReaderPackaged:requireUrbit,infuraReaderPackaged:requireInfura,modelVersions:versions,modelHashes:profile==='full',apiQualified:false}));
'''
    try:
        docker('create', '--name', name, '--label', 'oasis.package.owner=' + name,
               '--network', 'none', '--read-only', '--cpus', '1', '--memory', '256m',
               '--entrypoint', 'node', image, '-e', probe, json.dumps(expected), json.dumps(require_tron), o.profile, json.dumps(o.require_urbit_reader), json.dumps(o.require_infura_reader))
        r = docker('start', '--attach', name, timeout=30)
        ack = json.loads(r.stdout)
        state = json.loads(docker('inspect', name).stdout)[0]
        assert state['Image'] == metadata['Id'] and state['State']['ExitCode'] == 0
        assert not state['State']['Running'] and not state['State']['OOMKilled']
        assert ack['apiQualified'] is False and ack['assemblies'] == {'tron-api': 2, 'maplibre': 3, 'full': 6}[o.profile] + int(require_tron) + int(o.require_urbit_reader) + int(o.require_infura_reader)
        assert ack['tronReaderPackaged'] is require_tron
        assert ack['urbitReaderPackaged'] is o.require_urbit_reader
        assert ack['infuraReaderPackaged'] is o.require_infura_reader
        assert ack['modelHashes'] is (o.profile == 'full')
        (out / 'acknowledged.json').write_text(json.dumps({'source': o.source, 'image': image,
            'profile': o.profile, 'tag': tag, 'imageId': metadata['Id'], 'expectedHashes': expected, 'checks': ack,
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
