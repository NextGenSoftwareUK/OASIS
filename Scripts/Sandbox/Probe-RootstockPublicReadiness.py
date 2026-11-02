"""One journalled Rootstock RPC qualification, without fallback or source writes."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import urllib.error
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[2]
RPC = "https://public-node.rsk.co/"


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, url):
        return None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--latest-canonical", action="store_true",
        help="Explicitly qualify a different latest/canonical-at-read contract, never a finalized fallback.")
    options = parser.parse_args()
    if not options.latest_canonical:
        parser.error("Only explicit --latest-canonical qualification is supported; original finalized failure is preserved.")
    state = ROOT / "LocalState/COSMIC/HostedRootstockPublicSourceCurrent"
    output = ROOT / "TestResults/RootstockPublicSourceCurrent"
    state.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    if options.latest_canonical:
        original = json.loads((ROOT / "TestResults/EvmBreadthReadinessCurrent/Rootstock.json").read_text(encoding="utf-8-sig"))
        if original.get("passed") is not False:
            raise RuntimeError("Preserved original finalized qualification failure required.")
        output = output / "LatestCanonical"
        output.mkdir(exist_ok=True)
    intent = state / ("latest-canonical-qualification.intent.json" if options.latest_canonical else "public-qualification.intent.json")
    if intent.exists() or (state / "source.json").exists():
        raise RuntimeError("Original qualification/source exists; reconcile it, never automatically replay.")
    with intent.open("x", encoding="utf-8") as stream:
        json.dump({"rpc": RPC, "chainId": "0x1e", "sourceWrites": False, "retryAllowed": False, "blockPolicy": "latest-canonical-at-read" if options.latest_canonical else "finalized"}, stream)
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

    result = {"rpc": RPC, "qualified": False, "sourceWrites": 0, "blockPolicy": "latest-canonical-at-read" if options.latest_canonical else "finalized", "calls": calls}
    try:
        if rpc("eth_chainId", []) != "0x1e":
            raise RuntimeError("Endpoint is not Rootstock chain250; never relabel it as Sonic.")
        genesis = rpc("eth_getBlockByNumber", ["0x0", False])
        if genesis.get("hash") != "0xf88529d4ab262c0f4d042e9d8d3f2472848eaafe1a9b7213f57617eb40a9f9e0":
            raise RuntimeError("Previously observed Rootstock genesis differs.")
        if genesis.get("number") != "0x0" or not re.fullmatch("0x[0-9a-f]{64}", genesis.get("hash", "")):
            raise RuntimeError("Canonical native genesis header required.")
        finalized = rpc("eth_getBlockByNumber", ["latest" if options.latest_canonical else "finalized", False])
        if not re.fullmatch("0x[0-9a-f]{64}", finalized.get("hash", "")) or not re.fullmatch("0x(?:0|[1-9a-f][0-9a-f]*)", finalized.get("number", "")):
            raise RuntimeError("Canonical finalized header required; no latest-tag fallback.")
        addresses = ["0x" + format(i, "040x") for i in range(1, 4)]
        balances = [rpc("eth_getBalance", [address, {"blockHash": finalized["hash"], "requireCanonical": True}]) for address in addresses]
        if any(not isinstance(value, str) or not re.fullmatch("0x(?:0|[1-9a-f][0-9a-f]{0,63})", value) for value in balances):
            raise RuntimeError("Canonical exact native balances required.")
        if options.latest_canonical:
            check = rpc("eth_getBlockByNumber", [finalized["number"], False])
            if check.get("hash") != finalized["hash"] or check.get("number") != finalized["number"]:
                raise RuntimeError("Selected canonical block changed during balance reads; no retry.")
        result.update(qualified=True, genesisHash=genesis["hash"], blockHash=finalized["hash"],
            blockNumber=finalized["number"], balances=balances)
        with (state / "source.json").open("x", encoding="utf-8") as stream:
            json.dump({"schema": 1, "rpcUrl": RPC, "chainId": "0x1e", "addresses": addresses,
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
