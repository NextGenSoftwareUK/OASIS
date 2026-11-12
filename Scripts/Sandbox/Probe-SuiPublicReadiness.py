"""Read-only GraphQL object qualification, no JSON-RPC or SDK fallback."""
import hashlib
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from urllib.request import Request, build_opener, HTTPRedirectHandler

ENDPOINT = "https://graphql.mainnet.sui.io/graphql"
GENESIS = "4btiuiMPvEENsttpZC7CZ53DruC3MAgfznDbASZ7DR6S"
OBJECTS = ["0x" + format(value, "064x") for value in (5, 6, 7)]
ALPHABET = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ValueError("Redirects are not accepted")


def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON field")
        result[key] = value
    return result


def address(value):
    if not isinstance(value, str) or not re.fullmatch(r"0x[0-9a-f]{64}", value):
        raise ValueError("Canonical object address required")
    return value


def digest(value):
    if not isinstance(value, str) or not 32 <= len(value) <= 44 or any(c not in ALPHABET for c in value):
        raise ValueError("Canonical Base58 digest required")
    number = 0
    for character in value:
        number = number * 58 + ALPHABET.index(character)
    raw = b"\0" * (len(value) - len(value.lstrip("1"))) + number.to_bytes((number.bit_length() + 7) // 8, "big")
    if len(raw) != 32:
        raise ValueError("32-byte digest required")
    return value


def uint53(value):
    if type(value) is not int or not 0 <= value <= (1 << 53) - 1:
        raise ValueError("Exact UInt53 integer required")
    return str(value)


def request(keys):
    keys = list(keys)
    if not 1 <= len(keys) <= 8 or len(set(keys)) != len(keys):
        raise ValueError("One to eight unique configured objects required")
    variables = {"k" + str(i): address(key) for i, key in enumerate(keys)}
    parameters = ",".join("$" + key + ":SuiAddress!" for key in variables)
    fields = " ".join("r" + str(i) + ":object(address:$k" + str(i) + "){address version digest}" for i in range(len(keys)))
    return {"query": "query(" + parameters + "){chainIdentifier genesis:checkpoint(sequenceNumber:0){digest} view:checkpoint{sequenceNumber digest query{" + fields + "}}}",
            "variables": variables}


def validate(response, keys):
    if not isinstance(response, dict) or "errors" in response:
        raise ValueError("Complete GraphQL response without errors required")
    data = response["data"]
    if digest(data["chainIdentifier"]) != GENESIS or digest(data["genesis"]["digest"]) != GENESIS:
        raise ValueError("Configured API-reported Mainnet genesis differs")
    view = data["view"]
    sequence = uint53(view["sequenceNumber"])
    checkpoint_digest = digest(view["digest"])
    rows = view["query"]
    if set(rows) != {"r" + str(i) for i in range(len(keys))}:
        raise ValueError("Complete exact selection aliases required")
    result = []
    for i, key in enumerate(keys):
        row = rows["r" + str(i)]
        if not isinstance(row, dict) or address(row["address"]) != key:
            raise ValueError("Selected object missing or identity differs")
        result.append({"objectId": key, "objectVersion": uint53(row["version"]),
                       "objectDigest": digest(row["digest"]), "checkpointSequence": sequence,
                       "checkpointDigest": checkpoint_digest})
    return result


def main():
    folder = Path(__file__).resolve().parents[2] / "TestResults/SuiPublicSourceCandidateCurrent"
    folder.mkdir(parents=True, exist_ok=True)
    report = {"utc": datetime.now(timezone.utc).isoformat(), "endpoint": ENDPOINT, "genesisDigest": GENESIS,
              "sourceWrites": 0, "scope": "API-reported checkpoint-scoped object metadata; no independent BCS/digest/state/consensus proof or cross-provider snapshot"}
    try:
        payload = json.dumps(request(OBJECTS), separators=(",", ":")).encode()
        http = Request(ENDPOINT, data=payload, headers={"Accept": "application/json", "Content-Type": "application/json"}, method="POST")
        with build_opener(NoRedirect()).open(http, timeout=20) as response:
            raw = response.read(262145)
        if len(raw) > 262144:
            raise ValueError("Response exceeds256KiB")
        rows = validate(json.loads(raw, object_pairs_hook=unique), OBJECTS)
        report.update(passed=True, requests=1, responseBytes=len(raw), responseSha256=hashlib.sha256(raw).hexdigest(), records=rows)
    except Exception as error:
        report.update(passed=False, errorType=type(error).__name__, httpStatus=getattr(error, "code", None))
    (folder / "readiness.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"passed": report["passed"], "records": len(report.get("records", [])), "errorType": report.get("errorType")}))
    return 0 if report["passed"] else 1


if __name__ == "__main__":
    raise SystemExit(main())
