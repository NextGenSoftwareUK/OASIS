import importlib.util
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('elastic_runner', Path(__file__).with_name('run-cosmic-elasticsearch-windows.py'))
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)


class ShutdownEvidenceTests(unittest.TestCase):
    def test_prior_shutdown_cannot_certify_current_run(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'native.log'
            prior = b'[cosmic-owned-local] stopped\n[cosmic-owned-local] closed\n'
            path.write_bytes(prior + b'current run started\n')
            self.assertFalse(runner.current_node_closed(path, len(prior)))
            with path.open('ab') as f:
                f.write(b'[cosmic-owned-local] stopped\n[cosmic-owned-local] closed\n')
            self.assertTrue(runner.current_node_closed(path, len(prior)))

    def test_truncated_log_cannot_certify_shutdown(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'native.log'
            path.write_bytes(b'new log')
            with self.assertRaisesRegex(RuntimeError, 'lineage changed'):
                runner.current_node_closed(path, 100)

    def test_existing_source_blocks_initialization_before_runtime_or_writes(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            source = root / 'source.json'
            source.write_bytes(b'original source identity')
            with patch.object(runner, 'STATE', root), patch.object(runner, 'verify_runtime') as runtime:
                with self.assertRaisesRegex(RuntimeError, 'initialization cannot replay'):
                    runner.run_owned(SimpleNamespace(initialize=True,retained=False,preflight=False))
                runtime.assert_not_called()
            self.assertEqual(b'original source identity',source.read_bytes())
            self.assertEqual(['source.json'],[p.name for p in root.iterdir()])

    def test_missing_acknowledgement_blocks_retained_run_without_initialization(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            with patch.object(runner, 'STATE', root), patch.object(runner, 'verify_runtime') as runtime:
                with self.assertRaisesRegex(RuntimeError, 'Acknowledged retained'):
                    runner.run_owned(SimpleNamespace(initialize=False,retained=True,preflight=False))
                runtime.assert_not_called()
            self.assertEqual([],list(root.iterdir()))

    def test_ready_health_requires_the_owned_fully_active_index(self):
        runner.validate_source_health({'cluster_name':runner.CLUSTER,'timed_out':False,'status':'green',
            'number_of_nodes':1,'active_primary_shards':1,'active_shards':1,'unassigned_shards':0,'initializing_shards':0})

    def test_http_started_but_unready_or_foreign_health_cannot_dispatch_source_reads(self):
        base={'cluster_name':runner.CLUSTER,'timed_out':False,'status':'green','number_of_nodes':1,
            'active_primary_shards':1,'active_shards':1,'unassigned_shards':0,'initializing_shards':0}
        for field,value in [('status','red'),('timed_out',True),('cluster_name','foreign'),
                            ('active_primary_shards',0),('initializing_shards',1),('unassigned_shards',1)]:
            with self.subTest(field=field), self.assertRaises(RuntimeError):
                runner.validate_source_health(dict(base,**{field:value}))


if __name__ == '__main__':
    unittest.main()
