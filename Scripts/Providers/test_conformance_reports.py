"""Pure report validation controls; no files, SDKs, services or credentials."""
import sys
sys.dont_write_bytecode=True
import unittest
from record_conformance_results import validate_report
class ConformanceReportTests(unittest.TestCase):
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
