"""Controlled acceptance-audit failures; no network, accounts or configuration."""
import copy
import importlib.util
from pathlib import Path
import unittest

spec = importlib.util.spec_from_file_location("clickhouse_audit", Path(__file__).with_name("Audit-HostedClickHouseAcceptance.py"))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ClickHouseAuditTests(unittest.TestCase):
    def fixture(self):
        names = ["France","Germany","Italy"]
        source = {"apiUrl":"https://play.clickhouse.com/", "countries":names, "sourceOwner":"source-owner"}
        rows = [{"id":"public-"+str(i),"name":name,"description":"Country polygon summary",
            "isPublic":True,"createdByAvatarId":"source-owner","metadata":{
                "ClickHouseCountryName":name,"ClickHousePolygonCount":"18446744073709551615",
                "ClickHouseTable":"blogs.countries","ClickHouseObservationPolicy":"operator-reported-public-country-summary",
                "oasis.native.source":"clickhouse-public-countries:https://play.clickhouse.com/:blogs.countries",
                "oasis.native.key":name}} for i,name in enumerate(names)]
        response = lambda values: {"isError": False, "result": {"rows": values}}
        native = {"avatar": {"id": "visitor"}, "native": [response(rows), response(rows[:2])]}
        private = [{"id": "private-" + str(i), "name": row["name"], "description": row["description"],
            "isPublic": False, "createdByAvatarId": "visitor", "version": 1,
            "metadata": {"ClickHouseCountryName": row["metadata"]["ClickHouseCountryName"], "COSMIC.Demo.SourceHolonId": row["id"]}}
            for i, row in enumerate(rows[:2])]
        federation = lambda selected: response([{"selected": {"provider": "ClickHouseOASIS", "row": row}} for row in selected] +
            [{"selected": {"provider": "MongoDBOASIS", "row": row}} for row in private])
        flow = {"provider": "ClickHouseOASIS", "prepared": {"saved": 2, "existing": 0},
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
        for fault in ("endpoint", "height", "consensus", "payload", "public-owner", "timestamp", "replica", "version", "private-leak", "operation", "pending", "resave", "freshness", "wrong-height", "native-missing", "genesis"):
            with self.subTest(fault=fault):
                source, native, flow, isolation, acks, pending = self.fixture()
                if fault == "endpoint": native["native"][0]["result"]["rows"][2]["metadata"]["oasis.native.source"] = "wrong"
                if fault == "height": native["native"][0]["result"]["rows"][2]["metadata"]["ClickHousePolygonCount"] = "unknown"
                if fault == "consensus": native["native"][0]["result"]["rows"][2]["metadata"]["ClickHouseObservationPolicy"] = "wrong"
                if fault == "payload": native["native"][0]["result"]["rows"][2]["metadata"]["coordinates"] = "excluded"
                if fault == "public-owner": native["native"][0]["result"]["rows"][0]["createdByAvatarId"] = "other"
                if fault == "timestamp": native["native"][0]["result"]["rows"][0]["metadata"]["ClickHousePolygonCount"] = "18446744073709551616"
                if fault == "replica": flow["replicas"]["Neo4jOASIS"]["result"]["rows"][0]["description"] = "wrong"
                if fault == "version": flow["mongoRows"][0]["version"] = 2
                if fault == "private-leak": isolation["foreignPrivateIds"]["result"] = {"count": 1, "rows": [private_row for private_row in flow["mongoRows"][:1]]}
                if fault == "operation": acks[0]["operationId"] = "wrong"
                if fault == "pending": pending["unsettled"] = True
                if fault == "resave": flow["repeated"]["saved"] = 1
                if fault == "freshness": native["native"][0]["result"]["rows"][0]["metadata"]["ClickHousePolygonCount"] = "0"
                if fault == "wrong-height": native["native"][0]["result"]["rows"][0]["metadata"]["ClickHouseTable"] = "wrong"
                if fault == "native-missing": native["native"][0]["result"]["rows"].pop()
                if fault == "genesis": native["native"][0]["result"]["rows"][0]["metadata"]["ClickHousePolygonCount"] = "01"
                with self.assertRaises(ValueError): module.audit(source, native, flow, isolation, acks, pending)


if __name__ == "__main__":
    unittest.main()
