import importlib.util
import hashlib
import json
from pathlib import Path
import unittest
from unittest.mock import patch
from tempfile import TemporaryDirectory
spec=importlib.util.spec_from_file_location('gate',Path(__file__).with_name('Verify-HostedHyperdriveCandidateRouting.py'))
gate=importlib.util.module_from_spec(spec);spec.loader.exec_module(gate)
class CandidateRoutingTests(unittest.TestCase):
    def test_each_eligible_provider_and_rejection_of_other_serving_provider(self):
        expected={'id':'record','name':'coffee','version':1,'createdByAvatarId':'owner'}
        for eligible in gate.routing.PROVIDERS:
            route={'RequestedProvider':'Default','InitialProvider':eligible,'ServingProvider':eligible,'RequestedStrategy':'Auto','Succeeded':True}
            response={'result':expected,'metaData':{'hyperDriveRoute':json.dumps(route)}}
            self.assertEqual(gate.verify_candidate(response,expected,eligible),route)
            for other in gate.routing.PROVIDERS-{eligible}:
                route['ServingProvider']=other
                response['metaData']['hyperDriveRoute']=json.dumps(route)
                with self.assertRaises(ValueError):gate.verify_candidate(response,expected,eligible)
    def test_unreviewed_candidate_is_rejected_before_response_processing(self):
        with self.assertRaises(ValueError):gate.verify_candidate({}, {}, 'SolanaOASIS')

    def test_localfile_requires_separate_scope_and_exact_route(self):
        provider = "LocalFileOASIS"
        expected = {"id": "record", "name": "coffee", "version": 0, "createdByAvatarId": "owner"}
        route = {"RequestedProvider": "Default", "InitialProvider": provider, "ServingProvider": provider,
                 "RequestedStrategy": "Auto", "Succeeded": True}
        response = {"result": expected, "metaData": {"hyperDriveRoute": json.dumps(route)}}
        with self.assertRaises(ValueError):
            gate.verify_candidate(response, expected, provider)
        self.assertEqual(route, gate.verify_candidate(response, expected, provider, providers={provider}))
        route["InitialProvider"] = "SQLLiteDBOASIS"
        response["metaData"]["hyperDriveRoute"] = json.dumps(route)
        with self.assertRaises(ValueError):
            gate.verify_candidate(response, expected, provider, providers={provider})
    def test_localfile_receipt_requires_stable_release(self):
        provider = "LocalFileOASIS"
        expected = {"id": "record", "name": "coffee", "version": 0, "createdByAvatarId": "owner"}
        route = {"RequestedProvider": "Default", "InitialProvider": provider, "ServingProvider": provider,
                 "RequestedStrategy": "Auto", "Succeeded": True}
        response = {"result": expected, "metaData": {"hyperDriveRoute": json.dumps(route)}}
        with TemporaryDirectory() as directory:
            root = Path(directory)
            state = root / "LocalState/COSMIC/HostedLocalFilePublicDatasetCurrent"
            state.mkdir(parents=True)
            raw = json.dumps({"Id": "record", "Name": "coffee", "Version": 0,
                              "CreatedByAvatarId": "owner", "IsPublic": True, "IsDeleted": False}).encode()
            digest = hashlib.sha256(raw).hexdigest()
            (state / "source").mkdir()
            record = state / "source/record.json"
            record.write_bytes(raw)
            (state / "point-routing-reviewed-record.json").write_text(json.dumps({
                "schema": 1, "provider": provider, "expected": expected, "sourceEvidenceSha256": digest}))
            (state / "local-source.json").write_text(json.dumps({
                "sourceOwner": "owner", "records": [{"id": "record", "sha256": digest}]}))
            with (patch.object(gate, "ROOT", root),
                  patch.object(gate.flow, "acknowledged_readiness_visitor", return_value=root) as visitor,
                  patch.object(gate.flow, "session", return_value=({}, "token")) as session,
                  patch.object(gate.flow, "call", side_effect=[
                      {"dailyCallsRemaining": 0}, {"dailyCallsRemaining": 1}, response,
                      {"dailyCallsRemaining": -1}, response]) as request,
                  patch.object(gate.flow, "require_readiness_release", side_effect=["one", "one", "two", "one", "one", "one"])):
                record.write_bytes(raw + b" ")
                with self.assertRaisesRegex(ValueError, "digest changed"):
                    gate.verify_localfile(root)
                visitor.assert_not_called()
                request.assert_not_called()
                record.write_bytes(raw)
                with self.assertRaisesRegex(RuntimeError, "one remaining daily call"):
                    gate.verify_localfile(root)
                request.assert_called_once_with("subscription/usage/current", "token", method="GET")
                with self.assertRaisesRegex(RuntimeError, "deployment changed"):
                    gate.verify_localfile(root)
                receipt = state / "point-routing-one.acknowledged.json"
                self.assertFalse(receipt.exists())
                gate.verify_localfile(root)
                self.assertEqual(route, json.loads(receipt.read_bytes())["route"])
                with self.assertRaisesRegex(RuntimeError, "Completed"):
                    gate.verify_localfile(root)
                self.assertEqual(5, request.call_count)
                session.assert_called_with(root, "visitor", allow_create=False, allow_renewal=False)

if __name__=='__main__':unittest.main()
