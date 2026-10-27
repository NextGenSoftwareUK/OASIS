"""Archive only unchanged zero-operation configuration failures before native setup."""
import argparse,hashlib,json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
DOC=ROOT/'Docs/WIP/HyperDrive-Providers/NativeEvidence'

def archive(names,check_only=False):
    plans=[]
    for name in names:
        if name not in {'AWSOASIS','DynamoDBOASIS','AWSS3OASIS'}:raise ValueError('Unreviewed provider')
        evidence=DOC/('conformance-'+name.lower()+'-current.json');raw=evidence.read_bytes();runs=json.loads(raw)['runs']
        if len(runs)!=2:raise ValueError('Original pair required')
        targets=[]
        for mode in ('async','sync'):
            path=ROOT/'TestResults/ProviderConformanceCurrent'/name/mode/'report.json'
            report=json.loads(path.read_bytes());expected=[r for r in runs if r['Synchronous']==(mode=='sync')]
            if len(expected)!=1 or report!=expected[0] or report['Provider']!=name or report['Mode']!='native' or report['CurrentCase']!='configuration' or report['Passed'] or report['Succeeded']:
                raise ValueError('Only exact zero-operation configuration failures may be archived')
            destination=path.with_name('report-original-configuration.json')
            if destination.exists() or path.with_name('report.json.next').exists():raise ValueError('Existing or interrupted evidence requires review')
            targets.append((path,destination,hashlib.sha256(path.read_bytes()).hexdigest()))
        initial=DOC/('conformance-'+name.lower()+'-initial-configuration.json')
        if initial.exists():raise ValueError('Initial configuration archive already exists')
        plans.append((initial,raw,targets))
    if check_only:
        print('Unchanged zero-operation configuration reports verified; no mutation');return
    for initial,raw,targets in plans:
        with initial.open('xb') as out:out.write(raw)
        for path,destination,digest in targets:
            path.rename(destination)
            if hashlib.sha256(destination.read_bytes()).hexdigest()!=digest:raise ValueError('Archived report changed')
    print('Original configuration evidence preserved; no database operation')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--providers',nargs='+',required=True);parser.add_argument('--check-only',action='store_true')
    args=parser.parse_args();archive(args.providers,args.check_only)
