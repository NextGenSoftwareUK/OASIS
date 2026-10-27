"""Archive the reviewed SQL baseline failures without touching database fixtures."""
import argparse, hashlib, json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
DOC=ROOT/'Docs/WIP/HyperDrive-Providers/NativeEvidence'

def retire(check_only=False,suite='core-storage-v1',visibility=False):
    archive=DOC/'conformance-postgresqloasis-current.json'
    original=archive.read_bytes()
    data=json.loads(original)
    case={'core-storage-v1':'detail-id','core-storage-v2':'parent-query'}[suite]
    if visibility and suite!='core-storage-v2':raise ValueError('Visibility evidence is v2 only')
    if data['suite']!=suite or len(data['runs'])!=2:
        raise ValueError('Exactly the original failed suite pair is required')
    moves=[]
    for mode in ('async','sync'):
        source=ROOT/'TestResults/ProviderConformanceCurrent/PostgreSQLOASIS'/mode/'report.json'
        target=source.with_name('report-'+suite+('-visibility' if visibility else '')+'.json')
        raw=source.read_bytes(); report=json.loads(raw)
        expected=[r for r in data['runs'] if r['Synchronous']==(mode=='sync')]
        valid_case=(report['CurrentCase'].startswith('holon-delete-') and report.get('FailureContract')=='declared shipping default deletion contract') if visibility else report['CurrentCase']==case
        if len(expected)!=1 or report!=expected[0] or not valid_case or report['Succeeded']:
            raise ValueError('Original failed report changed; no retirement')
        if target.exists() or source.with_name('report.json.next').exists():
            raise ValueError('Existing archive or interrupted checkpoint must be inspected')
        moves.append((source,target,hashlib.sha256(raw).hexdigest(),len(raw)))
    version=suite.rsplit('-',1)[1]
    if visibility:version+='-visibility'
    preserved=DOC/('conformance-postgresqloasis-initial-'+version+'.json')
    proof=DOC/('postgres-'+version+'-report-retirement.json')
    if (preserved.exists() and preserved.read_bytes()!=original) or proof.exists():raise ValueError('Retirement evidence changed or already exists; inspect before retry')
    if check_only:
        print('Two unchanged failed '+suite+' reports verified; no files or database changed')
        return
    # Persist original aggregate before moving checkpoints; no row deletion or replay.
    if not preserved.exists():
        with preserved.open('xb') as out:out.write(original)
    evidence={'scope':'report archival only; original database rows preserved',
              'initialArchiveSha256':hashlib.sha256(original).hexdigest(),'reports':[]}
    for source,target,digest,size in moves:
        source.rename(target)
        if hashlib.sha256(target.read_bytes()).hexdigest()!=digest:raise ValueError('Archived report hash mismatch')
        evidence['reports'].append({'path':target.relative_to(ROOT).as_posix(),'sha256':digest,'bytes':size})
    with proof.open('x',encoding='utf-8') as out:json.dump(evidence,out,indent=2);out.write('\n')
    print('Original '+suite+' reports archived; all original database rows remain untouched')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--check-only',action='store_true')
    parser.add_argument('--suite',choices=['core-storage-v1','core-storage-v2'],default='core-storage-v1')
    parser.add_argument('--visibility',action='store_true')
    args=parser.parse_args();retire(args.check_only,args.suite,args.visibility)
