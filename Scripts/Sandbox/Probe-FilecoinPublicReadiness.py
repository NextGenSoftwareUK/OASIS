"""One journalled Filecoin FEVM RPC qualification, without fallback or source writes."""
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
    output = ROOT / "TestResults/FilecoinPublicSourceCurrent"
    state.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    intent = state / "public-qualification.intent.json"
    if intent.exists() or (state / "source.json").exists():
        raise RuntimeError("Original qualification/source exists; reconcile it, never automatically replay.")
    with intent.open("x", encoding="utf-8") as stream:
        json.dump({"rpc": RPC, "chainId": "0x13a", "sourceWrites": False, "retryAllowed": False, "blockPolicy": "finalized"}, stream)
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
        genesis = rpc("eth_getBlockByNumber", ["0x0", False])
        if genesis.get("number") != "0x0" or not re.fullmatch("0x[0-9a-f]{64}", genesis.get("hash", "")):
            raise RuntimeError("Canonical native genesis header required.")
        finalized = rpc("eth_getBlockByNumber", ["finalized", False])
        if not re.fullmatch("0x[0-9a-f]{64}", finalized.get("hash", "")) or not re.fullmatch("0x(?:0|[1-9a-f][0-9a-f]*)", finalized.get("number", "")):
            raise RuntimeError("Canonical finalized header required; no latest-tag fallback.")
        addresses = ["0x" + format(i, "040x") for i in range(1, 4)]
        balances = [rpc("eth_getBalance", [address, {"blockHash": finalized["hash"], "requireCanonical": True}]) for address in addresses]
        if any(not isinstance(value, str) or not re.fullmatch("0x(?:0|[1-9a-f][0-9a-f]{0,63})", value) for value in balances):
            raise RuntimeError("Canonical exact native balances required.")
        result.update(qualified=True, genesisHash=genesis["hash"], blockHash=finalized["hash"],
            blockNumber=finalized["number"], balances=balances)
        with (state / "source.json").open("x", encoding="utf-8") as stream:
            json.dump({"schema": 1, "rpcUrl": RPC, "chainId": "0x13a", "addresses": addresses,
                "sourceOwner": str(uuid.uuid4())}, stream, indent=2)
    except Exception as error:
        result.update(errorType=type(error).__name__, failure=str(error))
    with (output / "qualification.json").open("x", encoding="utf-8") as stream:
        json.dump(result, stream, indent=2)
    print(json.dumps(result))
    if not result["qualified"]:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
