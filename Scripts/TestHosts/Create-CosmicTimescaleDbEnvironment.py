"""Create one original TLS/SCRAM TimescaleDB OSS source, never reset existing state."""
from pathlib import Path
import json, os, pwd, secrets, subprocess, time

STATE=Path('/var/tmp/oasis-timescaledb-cql')
IMAGE='docker.io/timescale/timescaledb@sha256:9192a05d435e360c861cfdb0f4b9f5ae11d33609713163465e46a64fcc577d49'
NAME='oasis-timescaledb-cql'
OWNER=pwd.getpwnam('dellams').pw_uid

def command(*args):return subprocess.check_output(['podman',*args],text=True).strip()
def durable(path,value,owner=OWNER):
    with path.open('x') as file:
        os.fchmod(file.fileno(),0o600);json.dump(value,file);file.flush();os.fsync(file.fileno())
    os.chown(path,owner,owner)
def private_text(path,text,owner):
    with path.open('x') as file:
        os.fchmod(file.fileno(),0o600);file.write(text);file.flush();os.fsync(file.fileno())
    os.chown(path,owner,owner)
def openssl(*args):
    subprocess.run(['openssl',*args],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
def main():
    assert os.geteuid()==0
    assert not STATE.exists(),'Original source/unknown outcome exists; reconcile without recreation'
    assert subprocess.run(['podman','container','exists',NAME]).returncode==1
    image=json.loads(command('image','inspect',IMAGE))[0]
    assert image['Id']=='9d6818ce427d460cb5f6750ea5c97ad7d121e0b76c02425c9020fb6a34034198'
    STATE.mkdir(mode=0o700);os.chown(STATE,OWNER,OWNER)
    pending=STATE/'environment.pending.json'
    durable(pending,{'image':IMAGE,'name':NAME,'port':15434})
    for name in ['data','tls','secrets']:
        path=STATE/name;path.mkdir(mode=0o700);os.chown(path,70,70)
    password=secrets.token_hex(32)
    durable(STATE/'credentials.json',{'username':'oasis_test','password':password,'database':'oasis'})
    private_text(STATE/'secrets/password',password,70)
    private_text(STATE/'secrets/connection.pgpass','localhost:5432:oasis:oasis_test:'+password+'\n',70)
    old_umask=os.umask(0o077)
    try:
        openssl('req','-x509','-newkey','rsa:3072','-nodes','-days','365','-subj','/CN=COSMIC Timescale private CA',
            '-addext','basicConstraints=critical,CA:TRUE','-addext','keyUsage=critical,keyCertSign,cRLSign',
            '-keyout',str(STATE/'ca.key'),'-out',str(STATE/'ca.pem'))
        openssl('req','-new','-newkey','rsa:3072','-nodes','-subj','/CN=localhost',
            '-keyout',str(STATE/'tls/server.key'),'-out',str(STATE/'server.csr'))
        private_text(STATE/'extensions.cnf','subjectAltName=DNS:localhost,IP:127.0.0.1\nkeyUsage=critical,digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\nbasicConstraints=critical,CA:FALSE\n',OWNER)
        openssl('x509','-req','-in',str(STATE/'server.csr'),'-CA',str(STATE/'ca.pem'),'-CAkey',str(STATE/'ca.key'),
            '-CAcreateserial','-days','365','-sha256','-extfile',str(STATE/'extensions.cnf'),'-out',str(STATE/'tls/server.crt'))
        private_text(STATE/'tls/ca.crt',(STATE/'ca.pem').read_text(),70)
        for path in (STATE/'tls').iterdir():os.chmod(path,0o600);os.chown(path,70,70)
        for name in ['ca.key','ca.pem','server.csr','ca.srl']:
            path=STATE/name;os.chmod(path,0o600);os.chown(path,OWNER,OWNER)
    finally:os.umask(old_umask)
    container=command('create','--name',NAME,'--label','oasis.task=cosmic-cql',
        '--publish','127.0.0.1:15434:5432','--cpus','2','--memory','1g','--pids-limit','256',
        '--env','POSTGRES_USER=oasis_test','--env','POSTGRES_DB=oasis','--env','POSTGRES_PASSWORD_FILE=/run/secrets/password',
        '--env','POSTGRES_INITDB_ARGS=--auth-host=scram-sha-256 --auth-local=trust','--env','NO_TS_TUNE=true','--env','TS_TELEMETRY=off',
        '--volume',str(STATE/'data')+':/var/lib/postgresql/data','--volume',str(STATE/'tls')+':/tls:ro',
        '--volume',str(STATE/'secrets')+':/run/secrets:ro',IMAGE,'postgres',
        '-c','shared_preload_libraries=timescaledb','-c','shared_buffers=64MB','-c','max_connections=20',
        '-c','ssl=on','-c','ssl_cert_file=/tls/server.crt','-c','ssl_key_file=/tls/server.key')
    durable(STATE/'container.ack.json',{'id':container,'imageId':image['Id']})
    def sql(query,tls=False):
        arguments=['exec','--user','70']
        if tls:arguments+=['--env','PGPASSFILE=/run/secrets/connection.pgpass']
        arguments += [container,'psql','--no-psqlrc','-v','ON_ERROR_STOP=1','-At']
        arguments += ['dbname=oasis user=oasis_test host=localhost sslmode=verify-full sslrootcert=/tls/ca.crt'] if tls else ['-U','oasis_test','-d','oasis']
        return command(*arguments,'-c',query)
    observations=[]
    for phase in ['first','restart']:
        command('start',container)
        try:
            deadline=time.monotonic()+60
            while time.monotonic()<deadline:
                assert json.loads(command('inspect',container))[0]['State']['Running'],'Owned native server exited; preserve source'
                try:
                    if sql('SELECT 1')=='1':break
                except subprocess.CalledProcessError:time.sleep(.25)
            else:raise RuntimeError('Native readiness deadline')
            extension=sql("SELECT extversion FROM pg_extension WHERE extname='timescaledb'")
            assert extension=='2.31.0'
            assert 'timescaledb' in sql('SHOW shared_preload_libraries').split(',')
            assert sql("SELECT count(*) FROM information_schema.tables WHERE table_schema='cosmic_cql'")=='0'
            assert sql('SELECT ssl FROM pg_stat_ssl WHERE pid=pg_backend_pid()',tls=True)=='t'
            observations.append({'phase':phase,'extension':extension,'postgresVersion':sql('SHOW server_version'),
                'systemIdentifier':sql('SELECT system_identifier::TEXT FROM pg_control_system()'),
                'databaseId':sql("SELECT oid::TEXT FROM pg_database WHERE datname='oasis'"),'remoteVerifyFull':True})
        finally:
            command('stop','--time','30',container)
            status=json.loads(command('inspect',container))[0]['State']
            assert not status['Running'] and status['ExitCode']==0
    for field in ['systemIdentifier','databaseId','extension']:assert observations[0][field]==observations[1][field]
    durable(STATE/'environment.json',{'containerId':container,'imageId':image['Id'],'image':IMAGE,'observations':observations})
    pending.unlink()
    print(json.dumps({'passed':True,'containerId':container,'imageId':image['Id'],'observations':observations,
        'stopped':True,'stateBytes':sum(p.stat().st_size for p in STATE.rglob('*') if p.is_file())}))

if __name__=='__main__':main()
