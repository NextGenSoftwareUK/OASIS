"""Prove archival admission preserves unknown data and preflights both modes."""
import json,tempfile,unittest
from pathlib import Path
from unittest.mock import patch
import retire_localfile_v1_reports as retirement


class RetirementTests(unittest.TestCase):
    def fixture(self,root):
        reports=[]
        for index,mode in enumerate(('async','sync')):
            owner='00000000-0000-0000-0000-'+str(index+1).zfill(12)
            report=dict(Synchronous=mode=='sync',OwnerId=owner,Succeeded=False,
                        CurrentCase='avatar-save',Passed=['activate'])
            reports.append(report);work=root/'TestResults/ProviderConformanceCurrent/LocalFileOASIS'/mode
            folder=work/'data/Avatars';folder.mkdir(parents=True)
            (folder/(owner+'.json')).write_text(json.dumps(dict(Id=owner,Username='conformance-'+owner.replace('-',''))))
            (work/'report.json').write_text(json.dumps(report))
        return dict(suite='core-storage-v1',runs=reports)

    def test_check_only_and_preservation_leave_payload_bytes_unchanged(self):
        with tempfile.TemporaryDirectory(prefix='oasis-retirement-control-') as temp:
            root=Path(temp);aggregate=self.fixture(root)
            before={p.relative_to(root):p.read_bytes() for p in root.rglob('*') if p.is_file()}
            with patch.object(retirement,'ROOT',root),patch.object(retirement,'registered_report',return_value=json.dumps(aggregate).encode()):
                retirement.retire(check_only=True)
                self.assertEqual(before,{p.relative_to(root):p.read_bytes() for p in root.rglob('*') if p.is_file()})
                retirement.retire()
            for path,raw in before.items():
                destination=root/path.as_posix().replace('data/','data-core-storage-v1/').replace('report.json','report-core-storage-v1.json')
                self.assertEqual(raw,destination.read_bytes())
                self.assertFalse((root/path).exists())

    def test_unknown_second_mode_file_blocks_all_moves(self):
        with tempfile.TemporaryDirectory(prefix='oasis-retirement-control-') as temp:
            root=Path(temp);aggregate=self.fixture(root)
            unknown=root/'TestResults/ProviderConformanceCurrent/LocalFileOASIS/sync/data/unknown.json'
            unknown.write_text('user data')
            with patch.object(retirement,'ROOT',root),patch.object(retirement,'registered_report',return_value=json.dumps(aggregate).encode()):
                with self.assertRaises(ValueError):retirement.retire()
            self.assertEqual('user data',unknown.read_text())
            self.assertTrue((root/'TestResults/ProviderConformanceCurrent/LocalFileOASIS/async/report.json').exists())
            self.assertFalse(list(root.rglob('*preservation.json')))


if __name__=='__main__':unittest.main()
