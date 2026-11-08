"""Owned cloud Mongo replica for explicitly named WEB4 qualification jobs."""
import argparse,json,os,subprocess,time
from pathlib import Path
from qualify_web4_combined_startup import missing_resource


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--operation',choices=['start','stop'],required=True);p.add_argument('--profile',choices=['gomap','tron'],default='gomap');o=p.parse_args()
    assert os.environ.get('GITHUB_ACTIONS')=='true','Cloud-only database qualification.'
    run=os.environ['GITHUB_RUN_ID'];attempt=os.environ['GITHUB_RUN_ATTEMPT'];assert run.isdecimal() and attempt.isdecimal()
    owner=o.profile+'-web4-'+run+'-'+attempt;name=o.profile+'-cql-mongo';label='oasis.'+o.profile+'.web4.owner'
    out=Path(__file__).resolve().parents[2]/'TestResults'/({'gomap':'GOMapWeb4Current','tron':'TronHeaderWeb4Current'}[o.profile]);out.mkdir(parents=True,exist_ok=True)
    def docker(*args,check=True,timeout=30):
        r=subprocess.run(['docker',*args],capture_output=True,text=True,timeout=timeout)
        if check and r.returncode:raise RuntimeError('Owned Mongo operation failed: '+r.stderr[-1500:])
        return r
    def absent(kind,value):
        r=docker(kind,'inspect',value,check=False)
        if missing_resource(kind,value,r.returncode,r.stderr):return True
        if r.returncode:raise RuntimeError('Mongo resource absence unknown: '+r.stderr[-1000:])
        return False
    if o.operation=='stop':
        if not absent('container',name):
            state=json.loads(docker('container','inspect',name).stdout)[0]
            assert state['Config']['Labels'][label]==owner
            volumes=[x['Name'] for x in state.get('Mounts',[]) if x['Type']=='volume']
            if state['State']['Running']:docker('stop','--time','20',name,timeout=30)
            docker('rm','--volumes',name)
            assert absent('container',name)
            for volume in volumes:assert absent('volume',volume)
        (out/'mongo-cleanup.json').write_text(json.dumps({'owner':owner,'ownedContainerAbsent':True,'anonymousVolumesAbsent':True}))
        return
    assert absent('container',name),'Never adopt an existing Mongo container.'
    with (out/'mongo-intent.json').open('x') as f:json.dump({'owner':owner,'container':name,'image':'mongo:8.0.15','loopbackPort':27029},f)
    docker('create','--name',name,'--label',label+'='+owner,'--publish','127.0.0.1:27029:27029',
        '--memory','1g','mongo:8.0.15','--port','27029','--replSet','oasisRecoveryRs','--bind_ip_all',timeout=180)
    docker('start',name)
    def shell(statement):return docker('exec',name,'mongosh','--port','27029','--quiet','--eval',statement,check=False,timeout=10)
    deadline=time.monotonic()+45
    while True:
        r=shell('db.adminCommand({ping:1}).ok')
        if r.returncode==0:break
        if 'ECONNREFUSED' not in r.stdout+r.stderr and 'connection refused' not in (r.stdout+r.stderr).lower():
            raise RuntimeError('Mongo readiness failed outside connection-refusal contract.')
        if time.monotonic()>deadline:raise RuntimeError('Owned Mongo startup deadline exceeded.')
        time.sleep(1)
    r=shell('rs.initiate({_id:"oasisRecoveryRs",members:[{_id:0,host:"127.0.0.1:27029"}]})');assert r.returncode==0
    deadline=time.monotonic()+45
    while True:
        r=shell('if(!db.hello().isWritablePrimary)quit(2)')
        if r.returncode==0:break
        if r.returncode!=2:raise RuntimeError('Owned Mongo primary check failed outside not-yet-primary contract.')
        if time.monotonic()>deadline:raise RuntimeError('Owned Mongo primary deadline exceeded.')
        time.sleep(1)
    (out/'mongo-ready.json').write_text(json.dumps({'owner':owner,'loopbackReplicaReady':True,'port':27029}))

if __name__=='__main__':main()
