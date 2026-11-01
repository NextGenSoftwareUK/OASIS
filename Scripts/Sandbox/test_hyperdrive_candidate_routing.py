import importlib.util
import json
from pathlib import Path
import unittest
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
if __name__=='__main__':unittest.main()
