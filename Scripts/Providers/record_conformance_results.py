"""Register only actually executed, hash-bound shared-suite results. No old test-total promotion."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
DOC=ROOT/'Docs/WIP/HyperDrive-Providers'
work=ROOT/'TestResults/ProviderConformanceCurrent/LocalFileOASIS'
runs=[]
for mode in ['async','sync']:
 p=work/mode/'report.json'
 if p.exists():runs.append(json.loads(p.read_text(encoding='utf-8-sig')))
if not runs:raise SystemExit('No executed LocalFile conformance reports to register')
assert all(r['SuiteVersion']=='core-storage-v1' and r['Provider']=='LocalFileOASIS' and r['Mode']=='native' for r in runs)
archive=DOC/'NativeEvidence/conformance-localfile-current.json'
archive.write_text(json.dumps({'suite':'core-storage-v1','runs':runs},indent=2)+'\n',encoding='utf-8')
passed=sum(len(r['Passed']) for r in runs)
blockers=[r['CurrentCase']+': '+r['FailureType'] for r in runs if not r['Succeeded']]
if len(runs)!=2:blockers.append('One sync/async mode did not execute')
registry=DOC/'CONFORMANCE_RESULTS.json';data=json.loads(registry.read_text(encoding='utf-8-sig'))
data['results']=[r for r in data['results'] if r['provider']!='LocalFileOASIS']
data['results'].append({'provider':'LocalFileOASIS','suite':'core-storage-v1','mode':'native','passed':passed,'total':52,
 'report':archive.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(archive.read_bytes()).hexdigest(),'blockers':blockers})
registry.write_text(json.dumps(data,indent=2)+'\n',encoding='utf-8')
print(f'Registered LocalFile: {passed}/52 shared contract checks; blockers={len(blockers)}')
