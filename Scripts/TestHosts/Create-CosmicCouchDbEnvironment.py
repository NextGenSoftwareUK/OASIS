"""Create one retained, authenticated, loopback-only CouchDB demo environment."""
from pathlib import Path
import base64, json, os, pwd, secrets, subprocess, time, urllib.error, urllib.request

STATE=Path('/var/tmp/oasis-couchdb-cql')
IMAGE='docker.io/apache/couchdb@sha256:701bf53dd95b8742cd4120fb8f5aea8f14c0d8a77cfa807e86caaa537f822ee1'
NAME='oasis-couchdb-cql'
OWNER=pwd.getpwnam('dellams').pw_uid

def command(*args):
    return subprocess.check_output(['podman',*args],text=True).strip()

def private_json(path,value):
    with path.open('x') as file:
        os.chmod(path,0o600);json.dump(value,file);file.flush();os.fsync(file.fileno())
    os.chown(path,OWNER,OWNER)

def main():
    assert os.geteuid()==0,'Use existing rootful Podman owner'
    assert not STATE.exists(),'Original environment exists; reuse or reconcile it, never recreate'
    assert subprocess.run(['podman','container','exists',NAME]).returncode==1,'Named container already exists; do not adopt'
    image=json.loads(command('image','inspect',IMAGE))[0]
    assert image['Id']=='df2aa2a1f3b5c7b5b8ecd51132941c204a0f1da91219c3280a83feb6260c7e11'
    STATE.mkdir(mode=0o700);os.chown(STATE,OWNER,OWNER)
    pending=STATE/'environment.pending.json'
    private_json(pending,{'operation':'create-native-couchdb','image':IMAGE,'name':NAME})
    config=STATE/'config';data=STATE/'data'
    for path in [config,data]:path.mkdir(mode=0o700);os.chown(path,5984,5984)
    username='cosmic_demo';password=secrets.token_hex(32)
    private_json(STATE/'credentials.json',{'username':username,'password':password})
    ini=config/'10-oasis.ini'
    with ini.open('x') as file:
        os.chmod(ini,0o600)
        file.write('[admins]\n'+username+' = '+password+'\n[couchdb]\nsingle_node = true\nn = 1\n[chttpd]\nbind_address = 0.0.0.0\nrequire_valid_user = true\n')
        file.flush();os.fsync(file.fileno())
    os.chown(ini,5984,5984)
    container=command('create','--name',NAME,'--label','oasis.task=cosmic-cql',
        '--publish','127.0.0.1:15984:5984','--cpus','2','--memory','512m','--pids-limit','256',
        '--env','ERL_FLAGS=+S 2:2 +SDcpu 1 +SDio 1',
        '--volume',str(data)+':/opt/couchdb/data',
        '--volume',str(config)+':/opt/couchdb/etc/local.d',IMAGE)
    private_json(STATE/'container.ack.json',{'id':container,'imageId':image['Id']})
    auth='Basic '+base64.b64encode((username+':'+password).encode()).decode()
    def get(path):
        request=urllib.request.Request('http://127.0.0.1:15984'+path,headers={'Authorization':auth})
        with urllib.request.urlopen(request,timeout=2) as response:return json.load(response)
    observations=[]
    for phase in ['first','restart']:
        command('start',container)
        try:
            for _ in range(90):
                status=json.loads(command('inspect',container))[0]['State']
                assert status['Running'],'Owned node exited; preserve native logs/config/source'
                try:
                    if get('/_up').get('status')=='ok':break
                except (ConnectionError,urllib.error.URLError):time.sleep(.25)
            else:raise RuntimeError('Native readiness deadline')
            info=get('/');assert info['version']=='3.5.1'
            try:
                urllib.request.urlopen('http://127.0.0.1:15984/_all_dbs',timeout=2)
                raise AssertionError('Anonymous database listing must reject')
            except urllib.error.HTTPError as error:assert error.code==401
            databases=get('/_all_dbs');assert all(name.startswith('_') for name in databases),'Unexpected business source'
            observations.append({'phase':phase,'version':info['version'],'uuid':info['uuid'],'databases':databases})
        finally:
            command('stop','--time','20',container)
            status=json.loads(command('inspect',container))[0]['State']
            assert not status['Running'] and status['ExitCode']==0,'Owned teardown failed; preserve source'
    assert observations[0]['uuid']==observations[1]['uuid'],'Original native server identity changed'
    private_json(STATE/'environment.json',{'containerId':container,'image':IMAGE,'imageId':image['Id'],
        'port':15984,'host':'127.0.0.1','observations':observations})
    pending.unlink()
    print(json.dumps({'passed':True,'containerId':container,'imageId':image['Id'],'observations':observations,
        'stateBytes':sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file()),'stopped':True}))

if __name__=='__main__':main()
