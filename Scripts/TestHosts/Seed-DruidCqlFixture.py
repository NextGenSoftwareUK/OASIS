"""Seed and verify the synthetic fixture on the owned Druid CI router."""
import json
import re
import time
import urllib.request


ORIGIN = "http://127.0.0.1:8888"
ACTOR = "a5808c3e-7031-47e7-92de-48a659568f1b"
TABLE = "oasis_cql_public_holons"


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, msg, headers, newurl):
        return None


HTTP = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())


def request(path, body=None):
    headers = {"Accept": "application/json"}
    payload = None if body is None else json.dumps(body).encode()
    if payload is not None:
        headers["Content-Type"] = "application/json"
    with HTTP.open(urllib.request.Request(ORIGIN + path, data=payload, headers=headers), timeout=15) as response:
        data = response.read(65537)
        if len(data) > 65536:
            raise RuntimeError("Druid fixture response exceeded its limit.")
        return json.loads(data)


def sql(query):
    return request("/druid/v2/sql", {"query": query, "resultFormat": "object", "context": {"timeout": 10000}})


def seed():
    if request("/status").get("version") != "38.0.0":
        raise RuntimeError("Expected the reviewed Druid release.")
    if sql("SELECT 1 AS ready") != [{"ready": 1}]:
        raise RuntimeError("Druid SQL broker is not ready.")
    table_status = f"SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{TABLE}'"
    if sql(table_status):
        raise RuntimeError("The disposable datasource already exists.")
    records = []
    for number in range(1, 6):
        key = f"10000000-0000-4000-8000-{number:012d}"
        doc = {"id": key, "name": f"Druid teaching record {number}", "createdByAvatarId": ACTOR,
               "isPublic": number != 5, "isActive": True, "isDeleted": number == 4}
        records.append({"timestamp": "2026-01-01T00:00:00Z", "id": key,
                        "is_public": int(doc["isPublic"]), "doc": json.dumps(doc, separators=(",", ":"))})
    result = request("/druid/indexer/v1/task", {
        "type": "index_parallel",
        "spec": {
            "dataSchema": {
                "dataSource": TABLE,
                "timestampSpec": {"column": "timestamp", "format": "iso"},
                "dimensionsSpec": {"dimensions": ["id", {"type": "long", "name": "is_public"}, "doc"]},
                "granularitySpec": {"type": "uniform", "segmentGranularity": "DAY", "queryGranularity": "NONE",
                                    "rollup": False, "intervals": ["2026-01-01/2026-01-02"]},
            },
            "ioConfig": {
                "type": "index_parallel",
                "inputSource": {"type": "inline", "data": "\n".join(json.dumps(row) for row in records)},
                "inputFormat": {"type": "json"},
            },
            "tuningConfig": {"type": "index_parallel", "maxNumConcurrentSubTasks": 1},
        },
    })
    task = result.get("task", "")
    if not re.fullmatch(r"[a-zA-Z0-9_.:-]{1,240}", task):
        raise RuntimeError("Druid did not acknowledge an exact owned ingestion task.")
    for _ in range(60):
        status = request(f"/druid/indexer/v1/task/{task}/status")["status"]["status"]
        if status == "SUCCESS":
            break
        if status == "FAILED":
            raise RuntimeError("The owned Druid ingestion task failed.")
        time.sleep(2)
    else:
        raise RuntimeError("Druid ingestion did not complete.")

    for _ in range(90):
        if sql(table_status):
            break
        time.sleep(2)
    else:
        raise RuntimeError("Druid did not publish the reviewed datasource.")
    rows = sql(f'SELECT COUNT(*) AS total, SUM("is_public") AS public_count FROM "{TABLE}"')
    if rows != [{"total": 5, "public_count": 4}]:
        raise RuntimeError("Druid did not publish the exact reviewed teaching rows.")
    print("Druid 38.0.0. Verified five source records, four public flags and successful ingestion.")


if __name__ == "__main__":
    seed()
