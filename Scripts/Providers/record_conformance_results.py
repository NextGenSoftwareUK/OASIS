"""Register executed shared-suite reports; package/unit evidence never becomes a provider pass."""
import argparse,hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
DOC=ROOT/'Docs/WIP/HyperDrive-Providers'

def validate_report(r,name,mode,suite,capability=False):
    if r['SuiteVersion']!=suite or r['Provider']!=name or r['Mode'] not in {'native','controlled'}:raise ValueError('Report identity/suite/mode mismatch')
    if not capability and r['Synchronous']!=(mode=='sync'):raise ValueError('Report sync mode mismatch')
    contract=r.get('FailureContract','')
    if not isinstance(contract,str) or len(contract)>160 or any(ord(ch)<32 or ch=='|' for ch in contract):raise ValueError('Invalid contract diagnostic')
    if r['Succeeded'] and contract:raise ValueError('Successful report cannot retain a failed contract')
    passed=r['Passed'];maximum=5 if capability else 26
    if len(passed)>maximum or len(set(passed))!=len(passed):raise ValueError('Invalid or duplicate passed checks')
    if suite=='core-storage-v2' and r['Succeeded']:
        ids={r['RootId'],r['ChildId'],r['ImportId'],*r['BatchIds']};kinds=r.get('DeletionKinds',{})
        if len(passed)!=26 or r['CurrentCase']!='complete' or not r.get('FixtureCleanupComplete'):raise ValueError('Incomplete successful storage report')
        if r.get('CleanupScope')!='point-and-raw-collection-interface-absence':raise ValueError('Explicit interface cleanup scope required')
        if len(ids)!=5 or set(kinds)!=ids or any(kind not in {'absent','soft'} for kind in kinds.values()):raise ValueError('Deletion lifecycle evidence required')
        expected=r.get('ExpectedDefaultDeletion')
        if expected is not None and (expected not in {'soft','absent'} or any(kind!=expected for kind in kinds.values())):raise ValueError('Declared shipping deletion contract mismatch')
        cleaned=r.get('HardCleanedFixtures',[])
        if len(cleaned)!=len(set(cleaned)) or set(cleaned)!={id for id,kind in kinds.items() if kind=='soft'}:raise ValueError('Exact soft-fixture cleanup evidence required')

def register(names):
    roles={}
    for line in (DOC/'PROVIDER_MATRIX.md').read_text(encoding='utf-8').splitlines():
        if line.startswith('| ['):roles[line.split(']')[0][3:]]=line.split('|')[2].strip()
    registry=DOC/'CONFORMANCE_RESULTS.json';data=json.loads(registry.read_text(encoding='utf-8-sig'))
    for name in names:
        if name not in roles or not re.fullmatch(r'[A-Za-z0-9_.-]+',name):raise ValueError('Unknown provider identity')
        capability=roles[name]=='Non-storage';suite='capability-v1' if capability else data.get('currentCoreSuite','core-storage-v1')
        modes=['async'] if capability else ['async','sync'];runs=[];blockers=[]
        for mode in modes:
            p=ROOT/'TestResults/ProviderConformanceCurrent'/name/mode/'report.json'
            if not p.exists():blockers.append(mode+': no executed report');continue
            r=json.loads(p.read_text(encoding='utf-8-sig'))
            validate_report(r,name,mode,suite,capability)
            runs.append(r)
            if not r['Succeeded']:
                blocker=mode+': '+r['CurrentCase']+': '+r['FailureType']
                if r.get('FailureContract'):blocker+=' ['+r['FailureContract']+']'
                blockers.append(blocker)
        if not runs:raise ValueError('No executed reports for '+name)
        if len({r['Mode'] for r in runs})!=1:raise ValueError('Do not combine native and controlled provider results')
        stem='localfile' if name=='LocalFileOASIS' else name.lower()
        archive=DOC/'NativeEvidence'/('conformance-'+stem+'-current.json')
        archive.write_text(json.dumps({'suite':suite,'runs':runs},indent=2)+'\n',encoding='utf-8')
        passed=sum(len(r['Passed']) for r in runs);total=(5 if capability else 26)*len(modes)
        execution='blocked-before-native' if all(r['CurrentCase']=='configuration' and not r['Passed'] for r in runs) else runs[0]['Mode']
        data['results']=[r for r in data['results'] if r['provider']!=name]
        data['results'].append({'provider':name,'suite':suite,'mode':execution,'passed':passed,'total':total,
            'report':archive.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'blockers':blockers})
        print(f'Registered {name}: {passed}/{total}, {execution}; blockers={len(blockers)}')
    registry.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--providers',nargs='+',default=['LocalFileOASIS']);register(parser.parse_args().providers)
