"""Explicit native genesis/finalized-balance diagnostic; never a CQL admission or fallback."""
from pathlib import Path
import hashlib
import json
import re
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
RPC = "https://api.node.glif.io/rpc/v1"


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, url):
        return None


def main():
    state = ROOT / "LocalState/COSMIC/HostedFilecoinPublicSourceCurrent"
    output = ROOT / "TestResults/FilecoinPublicSourceCurrent/NativeGenesis"
    original = json.loads((output.parent / "qualification.json").read_text(encoding="utf-8"))
    if original.get("qualified") is not False:
        raise RuntimeError("Original archived-genesis rejection must be preserved.")
    output.mkdir(exist_ok=True)
    intent = state / "native-genesis-qualification.intent.json"
    with intent.open("x", encoding="utf-8") as stream:
        json.dump({"rpc": RPC, "sourceWrites": False, "retryAllowed": False,
            "identityContract": "Filecoin.ChainGetGenesis CID tipset", "cqlAdmission": False}, stream)
    calls = []
    opener = urllib.request.build_opener(NoRedirect())

    def rpc(method, parameters):
        number = len(calls) + 1
        entry = {"method": method, "parameters": parameters}
        calls.append(entry)
        request = urllib.request.Request(RPC, method="POST", headers={"Content-Type": "application/json"},
            data=json.dumps({"jsonrpc": "2.0", "id": number, "method": method, "params": parameters}).encode())
        try:
            response = opener.open(request, timeout=15)
        except urllib.error.HTTPError as error:
            response = error
        with response:
            raw = response.read(262145)
            entry.update(status=response.status, bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest())
            with (output / (str(number) + "-" + method + ".json")).open("xb") as stream:
                stream.write(raw)
            if len(raw) > 262144 or response.status != 200:
                raise RuntimeError("Bounded native RPC response failed at " + method)
        parsed = json.loads(raw)
        if parsed.get("jsonrpc") != "2.0" or type(parsed.get("id")) is not int or parsed["id"] != number or "error" in parsed or parsed.get("result") is None:
            raise RuntimeError("Native RPC result missing or rejected at " + method)
        return parsed["result"]

    result = {"rpc": RPC, "qualified": False, "sourceWrites": 0, "blockPolicy": "finalized", "calls": calls}
    try:
        if rpc("eth_chainId", []) != "0x13a":
            raise RuntimeError("Endpoint is not Filecoin mainnet chain314.")
        genesis = rpc("Filecoin.ChainGetGenesis", [])
        cids = [value["/"] for value in genesis["Cids"]]
        if genesis.get("Height") != 0 or not cids or len(cids) > 16 or len(cids) != len(set(cids)):
            raise RuntimeError("Native genesis tipset identity required.")
        if any(not re.fullmatch("b[a-z2-7]{20,128}", value) for value in cids):
            raise RuntimeError("Canonical native genesis CID encoding required.")
        result["nativeGenesisCids"] = cids
        finalized = rpc("eth_getBlockByNumber", ["finalized", False])
        if not re.fullmatch("0x[0-9a-f]{64}", finalized.get("hash", "")) or not re.fullmatch("0x(?:0|[1-9a-f][0-9a-f]*)", finalized.get("number", "")):
            raise RuntimeError("Canonical finalized header required; no latest-tag fallback.")
        addresses = ["0x" + format(i, "040x") for i in range(1, 4)]
        balances = [rpc("eth_getBalance", [address, {"blockHash": finalized["hash"], "requireCanonical": True}]) for address in addresses]
        if any(not isinstance(value, str) or not re.fullmatch("0x(?:0|[1-9a-f][0-9a-f]{0,63})", value) for value in balances):
            raise RuntimeError("Canonical exact native balances required.")
        result.update(qualified=True, blockHash=finalized["hash"],
            blockNumber=finalized["number"], balances=balances, cqlAdmission=False)
    except Exception as error:
        result.update(errorType=type(error).__name__, failure=str(error))
    with (output / "qualification.json").open("x", encoding="utf-8") as stream:
        json.dump(result, stream, indent=2)
    print(json.dumps(result))
    if not result["qualified"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
