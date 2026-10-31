"""Preserve exact reviewed LocalFile acknowledgement failures; no fixture deletion."""
import argparse, hashlib, json
from pathlib import Path
from record_conformance_results import ROOT, registered_report


def retire(check_only=False,suite='core-storage-v1',stage='avatar-save'):
    aggregate=json.loads(registered_report('LocalFileOASIS'))
    if aggregate['suite']!=suite or len(aggregate['runs'])!=2:
        raise ValueError('Exactly the registered failed pair is required')
    plans=[]
    for mode in ('async','sync'):
        work=ROOT/'TestResults/ProviderConformanceCurrent/LocalFileOASIS'/mode
        source=work/'report.json';raw=source.read_bytes();report=json.loads(raw)
        expected=[r for r in aggregate['runs'] if r['Synchronous']==(mode=='sync')]
        if expected!=[report] or report['Succeeded'] or report['CurrentCase']!=stage:
            raise ValueError('Original failed report changed')
        data=work/'data';files=list(data.rglob('*'))
        if data.resolve()!=data.absolute() or source.resolve()!=source.absolute():
            raise ValueError('Fixture paths must not traverse symbolic links')
        payloads=[p for p in files if p.is_file()]
        avatar=data/'Avatars'/(report['OwnerId']+'.json')
        if any(p.is_symlink() for p in files) or data.is_symlink():raise ValueError('Linked fixture path')
        avatar_raw=None
        if stage=='avatar-save':
            if payloads!=[avatar] or report['Passed']!=['activate']:raise ValueError('Only the original single synthetic avatar may be preserved')
            avatar_raw=avatar.read_bytes();row=json.loads(avatar_raw)
            if row['Id']!=report['OwnerId'] or row['Username']!='conformance-'+report['OwnerId'].replace('-',''):
                raise ValueError('Synthetic avatar identity mismatch')
        elif payloads or len(report['Passed'])!=23 or len(report['DeletionKinds'])!=5 or set(report['DeletionKinds'].values())!={'absent'}:
            raise ValueError('Delete acknowledgement failure requires independently empty fixture directory')
        label=suite+('-avatar-delete' if stage=='avatar-delete' else '')
        targets=(work/('report-'+label+'.json'),work/('data-'+label),work/(label+'-preservation.json'))
        if any(p.exists() for p in targets) or (work/'report.json.next').exists():
            raise ValueError('Existing preservation/interrupted report requires review')
        plans.append((source,data,targets,raw,avatar_raw,avatar.relative_to(data)))
    if check_only:
        print('Both exact failed reports and fixtures verified; no mutation');return
    for source,data,(report_target,data_target,receipt),raw,avatar_raw,relative in plans:
        source.rename(report_target);data.rename(data_target)
        if report_target.read_bytes()!=raw:raise ValueError('Preserved report bytes changed')
        if avatar_raw is not None and (data_target/relative).read_bytes()!=avatar_raw:
            raise ValueError('Preserved synthetic avatar bytes changed')
        proof={'scope':'old synthetic failure preserved, not replayed or resolved','files':[
            {'path':str(report_target.relative_to(ROOT)),'sha256':hashlib.sha256(raw).hexdigest()}]}
        if avatar_raw is not None:proof['files'].append({'path':str((data_target/relative).relative_to(ROOT)),'sha256':hashlib.sha256(avatar_raw).hexdigest()})
        with receipt.open('x',encoding='utf-8') as output:json.dump(proof,output,indent=2);output.write('\n')
    print('Original failed pair preserved by moves; same environment ready for current-suite fixtures')


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--check-only',action='store_true')
    parser.add_argument('--suite',choices=['core-storage-v1','core-storage-v2'],default='core-storage-v1')
    parser.add_argument('--stage',choices=['avatar-save','avatar-delete'],default='avatar-save')
    args=parser.parse_args();retire(args.check_only,args.suite,args.stage)
