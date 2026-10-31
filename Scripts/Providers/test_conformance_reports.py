"""Pure report validation controls; no files, SDKs, services or credentials."""
import sys
sys.dont_write_bytecode=True
import unittest
import hashlib,json,tempfile
from pathlib import Path
from unittest.mock import patch
import record_conformance_results as recorder
from record_conformance_results import validate_report
class ConformanceReportTests(unittest.TestCase):
    def test_registered_report_uses_registry_and_rejects_tampering(self):
        with tempfile.TemporaryDirectory(prefix='oasis-report-control-') as temporary:
            root=Path(temporary);doc=root/'Docs';doc.mkdir()
            for relative in ('Docs/legacy.json','TestResults/current.json'):
                path=root/relative;path.parent.mkdir(parents=True,exist_ok=True)
                raw=b'{"runs": []}\r\n';path.write_bytes(raw)
                entry=dict(provider='RedisOASIS',report=relative,sha256=hashlib.sha256(raw).hexdigest())
                (doc/'CONFORMANCE_RESULTS.json').write_text(json.dumps(dict(results=[entry])))
                with patch.object(recorder,'ROOT',root),patch.object(recorder,'DOC',doc):
                    self.assertEqual(raw,recorder.registered_report('RedisOASIS'))
                    path.write_bytes(b'changed')
                    with self.assertRaisesRegex(ValueError,'hash mismatch'):recorder.registered_report('RedisOASIS')

    def test_registration_writes_only_testresults_and_updates_hash(self):
        with tempfile.TemporaryDirectory(prefix='oasis-report-control-') as temporary:
            root=Path(temporary);doc=root/'Docs';doc.mkdir()
            evidence=root/'TestResults/HyperDrive-Providers/NativeEvidence'
            (doc/'PROVIDER_MATRIX.md').write_text('| [RedisOASIS](source) | Key-value |\n')
            registry=doc/'CONFORMANCE_RESULTS.json'
            registry.write_text(json.dumps(dict(currentCoreSuite='core-storage-v2',results=[])))
            for mode in ('async','sync'):
                path=root/'TestResults/ProviderConformanceCurrent/RedisOASIS'/mode/'report.json'
                path.parent.mkdir(parents=True);report=self.report();report['Synchronous']=mode=='sync'
                path.write_text(json.dumps(report))
            with patch.object(recorder,'ROOT',root),patch.object(recorder,'DOC',doc),patch.object(recorder,'EVIDENCE',evidence):
                recorder.register(['RedisOASIS'])
                raw=recorder.registered_report('RedisOASIS')
            entry=json.loads(registry.read_text())['results'][0]
            self.assertTrue(entry['report'].startswith('TestResults/HyperDrive-Providers/'))
            self.assertEqual(52,entry['passed'])
            self.assertEqual(hashlib.sha256(raw).hexdigest(),entry['sha256'])
            self.assertFalse((doc/'NativeEvidence').exists())

    def report(self):
        return dict(SuiteVersion='core-storage-v2',Provider='RedisOASIS',Mode='native',Synchronous=False,
            Passed=[str(i) for i in range(26)],Succeeded=True,CurrentCase='complete',FixtureCleanupComplete=True,CleanupScope='point-and-raw-collection-interface-absence',
            RootId='root',ChildId='child',ImportId='import',BatchIds=['batch1','batch2'],
            DeletionKinds={id:'soft' for id in ['root','child','import','batch1','batch2']},
            HardCleanedFixtures=['root','child','import','batch1','batch2'])
    def test_complete_soft_and_hard_reports(self):
        r=self.report();validate_report(r,'RedisOASIS','async','core-storage-v2')
        r['DeletionKinds']={id:'absent' for id in r['DeletionKinds']};r['HardCleanedFixtures']=[]
        validate_report(r,'RedisOASIS','async','core-storage-v2')

    def test_contract_diagnostics_are_optional_bounded_and_never_on_success(self):
        r=self.report();r['FailureContract']='single read acknowledgement/count'
        with self.assertRaises(ValueError):validate_report(r,'RedisOASIS','async','core-storage-v2')
        r['Succeeded']=False
        validate_report(r,'RedisOASIS','async','core-storage-v2')
        for value in [None,{},'x'*161,'line\nbreak','table|injection']:
            r['FailureContract']=value
            with self.subTest(value=value),self.assertRaises(ValueError):validate_report(r,'RedisOASIS','async','core-storage-v2')
    def test_old_suite_or_missing_cleanup_or_fake_totals_rejected(self):
        variants=[('SuiteVersion','core-storage-v1'),('FixtureCleanupComplete',False),('HardCleanedFixtures',[]),('Passed',['same']*26),('CurrentCase','fixture-hard-cleanup-child'),('DeletionKinds',{}),('ExpectedDefaultDeletion','absent'),('CleanupScope','physical-ledger-erase')]
        for field,value in variants:
            with self.subTest(field=field):
                r=self.report();r[field]=value
                with self.assertRaises(ValueError):validate_report(r,'RedisOASIS','async','core-storage-v2')
    def test_configuration_blocker_is_not_fabricated_success(self):
        r=self.report();r.update(Passed=[],Succeeded=False,CurrentCase='configuration',FixtureCleanupComplete=False,DeletionKinds={},HardCleanedFixtures=[])
        validate_report(r,'RedisOASIS','async','core-storage-v2')
if __name__=='__main__':unittest.main()
