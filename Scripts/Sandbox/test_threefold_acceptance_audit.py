"""Controlled acceptance-audit failures; no network, accounts or configuration."""
import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("threefold_audit", Path(__file__).with_name("Audit-HostedThreeFoldAcceptance.py"))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ThreeFoldAuditTests(unittest.TestCase):
    def fixture(self):
        names = ["1","2","3"]
        source = {"apiUrl":"https://gridproxy.grid.tf/", "nodeIds":names, "sourceOwner":"source-owner"}
        rows = [{"id":"public-"+str(i),"name":name,"description":"Public grid node summary",
            "isPublic":True,"createdByAvatarId":"source-owner","metadata":{
                "ThreeFoldNodeId":name,"ThreeFoldFarmId":"4294967295", "ThreeFoldGridVersion":"3",
                "ThreeFoldChainStateVerified":"false","ThreeFoldObservationPolicy":"operator-reported-public-grid-node",
                "oasis.native.source":"threefold-mainnet-grid-proxy:https://gridproxy.grid.tf/",
                "oasis.native.key":name}} for i,name in enumerate(names)]
        response = lambda values: {"isError": False, "result": {"rows": values}}
        native = {"avatar": {"id": "visitor"}, "native": [response(rows), response(rows[:2])]}
        private = [{"id": "private-" + str(i), "name": row["name"], "description": row["description"],
            "isPublic": False, "createdByAvatarId": "visitor", "version": 1,
            "metadata": {"ThreeFoldNodeId": row["metadata"]["ThreeFoldNodeId"], "COSMIC.Demo.SourceHolonId": row["id"]}}
            for i, row in enumerate(rows[:2])]
        federation = lambda selected: response([{"selected": {"provider": "ThreeFoldOASIS", "row": row}} for row in selected] +
            [{"selected": {"provider": "MongoDBOASIS", "row": row}} for row in private])
        flow = {"provider": "ThreeFoldOASIS", "prepared": {"saved": 2, "existing": 0},
            "repeated": {"saved": 0, "existing": 2}, "mongoRows": private, "ids": [row["id"] for row in private],
            "five": federation(rows), "four": federation(rows[:2]),
            "replicas": {p: response(copy.deepcopy(private)) for p in ("SQLLiteDBOASIS", "Neo4jOASIS")},
            "steps": [{"route": "data/save-holon", "status": 200, "operationId": "operation-" + str(i)} for i in range(2)]}
        isolation = {"avatar": {"id": "foreign"}, "foreignPrivateIds": {"isError": False, "result": {"count": 0, "rows": []}}, "public": response(rows)}
        acks = [{"id": row["id"], "ownerId": "visitor", "isSaved": True, "isPublic": False,
            "version": 1, "operationId": "operation-" + str(i)} for i, row in enumerate(private)]
        return source, native, flow, isolation, acks, {}

    def test_complete_original_cohort(self):
        self.assertEqual(module.audit(*self.fixture())["pendingSaves"], 0)

    def test_mutations_are_rejected(self):
        for fault in ("endpoint", "integer", "policy", "payload", "public-owner", "overflow", "replica", "version", "private-leak", "operation", "pending", "resave", "chain-claim", "zero-node", "native-missing", "leading-zero"):
            with self.subTest(fault=fault):
                source, native, flow, isolation, acks, pending = self.fixture()
                if fault == "endpoint": native["native"][0]["result"]["rows"][2]["metadata"]["oasis.native.source"] = "wrong"
                if fault == "integer": native["native"][0]["result"]["rows"][2]["metadata"]["ThreeFoldFarmId"] = "unknown"
                if fault == "policy": native["native"][0]["result"]["rows"][2]["metadata"]["ThreeFoldObservationPolicy"] = "wrong"
                if fault == "payload": native["native"][0]["result"]["rows"][2]["metadata"]["interfaces"] = "excluded"
                if fault == "public-owner": native["native"][0]["result"]["rows"][0]["createdByAvatarId"] = "other"
                if fault == "overflow": native["native"][0]["result"]["rows"][0]["metadata"]["ThreeFoldFarmId"] = "4294967296"
                if fault == "replica": flow["replicas"]["Neo4jOASIS"]["result"]["rows"][0]["description"] = "wrong"
                if fault == "version": flow["mongoRows"][0]["version"] = 2
                if fault == "private-leak": isolation["foreignPrivateIds"]["result"] = {"count": 1, "rows": [private_row for private_row in flow["mongoRows"][:1]]}
                if fault == "operation": acks[0]["operationId"] = "wrong"
                if fault == "pending": pending["unsettled"] = True
                if fault == "resave": flow["repeated"]["saved"] = 1
                if fault == "chain-claim": native["native"][0]["result"]["rows"][0]["metadata"]["ThreeFoldChainStateVerified"] = "true"
                if fault == "zero-node": native["native"][0]["result"]["rows"][0]["metadata"]["ThreeFoldNodeId"] = "0"
                if fault == "native-missing": native["native"][0]["result"]["rows"].pop()
                if fault == "leading-zero": native["native"][0]["result"]["rows"][0]["metadata"]["ThreeFoldFarmId"] = "01"
                with self.assertRaises(ValueError): module.audit(source, native, flow, isolation, acks, pending)


if __name__ == "__main__":
    unittest.main()
