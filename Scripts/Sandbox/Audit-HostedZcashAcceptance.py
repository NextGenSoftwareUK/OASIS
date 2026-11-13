"""Offline audit of the original Zcash hosted cohort; never sends requests or saves."""
import hashlib
import json
import os
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def require(condition, message):
    if not condition:
        raise ValueError(message)


def audit(source, native, flow, isolation, acknowledgements, pending):
    owner = native["avatar"]["id"]
    names = [block["hash"] for block in source["blocks"]]
    require(len(names) == 3 and len(set(names)) == 3, "Original three-provider sample required.")
    require(flow["provider"] == "ZcashOASIS", "Wrong flow provider.")
    originals = native["native"][0]["result"]["rows"]
    for response, selected in zip(native["native"], (names, names[:2])):
        rows = response["result"]["rows"]
        require(not response["isError"] and len(rows) == len(selected), "Native selection failed.")
        require({row["metadata"]["ZcashBlockHash"] for row in rows} == set(selected), "Wrong native account selection.")
        require(len({row["id"] for row in rows}) == len(rows), "Duplicate source identity.")
        for row in rows:
            metadata = row["metadata"]
            require(row["isPublic"] and row["createdByAvatarId"] == source["sourceOwner"], "Wrong public ownership.")
            require(metadata["oasis.native.source"] == "zecblock-public-summaries:" + source["apiUrl"] + ":" + source["genesisHash"], "Wrong source endpoint.")
            require(metadata["oasis.native.key"] == metadata["ZcashBlockHash"], "Wrong native identity.")
            require(metadata["ZcashProofVerified"] == "false", "Unsupported consensus claim.")
            require(metadata["ZcashObservationPolicy"] == "indexer-reported-public-block-summary", "Wrong observation policy.")
            require(source["genesisHash"] == "00040fe8ec8471911baa1db1266ea15dd06b4a8a5c453883c000b031973dce08"
                    and metadata["ZcashGenesisHash"] == source["genesisHash"], "Wrong Zcash genesis.")
            key = metadata["ZcashBlockHash"]
            require(isinstance(key, str) and re.fullmatch(r"[0-9a-f]{64}", key), "Wrong block hash representation.")
            expected = next(block for block in source["blocks"] if block["hash"] == key)
            require(metadata["ZcashBlockHeight"] == str(expected["height"]), "Height/hash mapping differs.")
            timestamp = metadata["ZcashBlockTimestamp"]
            require(isinstance(timestamp, str) and re.fullmatch(r"0|[1-9][0-9]{0,9}", timestamp)
                    and int(timestamp) <= 4294967295, "Wrong UInt32 timestamp.")
            require(metadata["ZcashFreshness"] in ("fresh", "stale", "unknown"), "Unsupported freshness.")
            require(all(field not in metadata for field in ("transactions", "raw", "private_notes", "shielded_balances")),
                    "Excluded native/private payload leaked.")
    require(len(native["native"]) == 2, "Exactly two native observations required.")
    require(flow["prepared"]["saved"] == 2 and flow["prepared"]["existing"] == 0, "Wrong initial saves.")
    require(flow["repeated"]["saved"] == 0 and flow["repeated"]["existing"] == 2, "Preparation resaved copies.")
    private = flow["mongoRows"]
    ids = set(flow["ids"])
    require(len(private) == 2 and len(ids) == 2 and {row["id"] for row in private} == ids, "Wrong private identities.")
    by_source = {row["id"]: row for row in originals}
    for row in private:
        require(row["createdByAvatarId"] == owner and not row["isPublic"] and row["version"] == 1, "Wrong private ownership/version.")
        original = by_source[row["metadata"]["COSMIC.Demo.SourceHolonId"]]
        require(row["metadata"]["ZcashBlockHash"] == original["metadata"]["ZcashBlockHash"], "Private native key changed.")
        require(row["name"] == original["name"] and row["description"] == original["description"], "Private snapshot changed.")
    for label, selected in (("five", names), ("four", names[:2])):
        rows = flow[label]["result"]["rows"]
        require(not flow[label]["isError"] and len(rows) == len(selected) + 2, "Federation count failed.")
        copies = [row["selected"] for row in rows]
        require({row["row"]["id"] for row in copies if row["provider"] == "MongoDBOASIS"} == ids, "Federation lost private copies.")
        public = [row["row"] for row in copies if row["provider"] == "ZcashOASIS"]
        require(len(public) == len(selected) and {row["metadata"]["ZcashBlockHash"] for row in public} == set(selected), "Federation lost native records.")
    for provider in ("SQLLiteDBOASIS", "Neo4jOASIS"):
        response = flow["replicas"][provider]
        require(not response["isError"], "Replica query failed.")
        rows = response["result"]["rows"]
        require(len(rows) == 2 and {row["id"] for row in rows} == ids, "Replica identity differs.")
        for row in rows:
            require(row == next(value for value in private if value["id"] == row["id"]), "Replica projection differs.")
    denied = isolation["foreignPrivateIds"]
    require(isolation["avatar"]["id"] != owner and not denied["isError"] and
            denied["result"]["count"] == 0 and denied["result"]["rows"] == [], "Foreign private visibility failed.")
    public_response = isolation["public"]
    public_rows = public_response["result"]["rows"]
    require(not public_response["isError"] and len(public_rows) == 3 and
            {row["metadata"]["ZcashBlockHash"] for row in public_rows} == set(names) and
            all(row["isPublic"] for row in public_rows), "Foreign public visibility failed.")
    require(len(acknowledgements) == 2 and {ack["id"] for ack in acknowledgements} == ids and not pending, "Unsettled saves.")
    saves = [step for step in flow["steps"] if step["route"] == "data/save-holon"]
    require(len(saves) == 2 and all(step["status"] == 200 for step in saves), "Wrong normal API save sequence.")
    require({step["operationId"] for step in saves} == {ack["operationId"] for ack in acknowledgements}, "Save operation mismatch.")
    require(all(ack["isSaved"] and ack["ownerId"] == owner and not ack["isPublic"] and ack["version"] == 1 for ack in acknowledgements), "Unacknowledged private saves.")
    return {"provider": "ZcashOASIS", "ownerId": owner, "nativeCounts": [3, 2], "federationCounts": [5, 4],
            "saved": 2, "repeatNewSaves": 0, "replicas": {"SQLLiteDBOASIS": 2, "Neo4jOASIS": 2},
            "pendingSaves": 0, "browserCertified": False, "reliabilityCertified": False}


def main():
    source = ROOT / "LocalState/COSMIC/HostedZcashPublicSourceCurrent/source.json"
    state = ROOT / "LocalState/COSMIC/HostedQueryAcceptance-ZcashOASIS"
    results = ROOT / "TestResults/HostedCqlIntegrationCurrent"
    def read(path):
        return json.loads(path.read_text(encoding="utf-8-sig"))
    result = audit(read(source), read(results / "ZcashOASIS-native-http.json"),
        read(results / "ZcashOASIS-client-http-flow.json"), read(results / "ZcashOASIS-isolation-http.json"),
        [json.loads(line) for line in (state / "save-acknowledgements.jsonl").read_text().splitlines()],
        read(state / "preparation-intents.json"))
    result["sourceSha256"] = hashlib.sha256(source.read_bytes()).hexdigest()
    with (state / "cql-http-acceptance.acknowledged.json").open("x", encoding="utf-8") as stream:
        json.dump(result, stream, sort_keys=True)
        stream.flush()
        os.fsync(stream.fileno())
    print("Original Zcash cohort audit passed; no additional reads or saves.")


if __name__ == "__main__":
    main()
