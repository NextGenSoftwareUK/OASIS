"""Exercise the pinned Waku release with two isolated, loopback-only peers."""

import argparse
import base64
import contextlib
import json
from pathlib import Path
import re
import subprocess
import time
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urlencode
from urllib.request import Request, urlopen


STORE = "http://127.0.0.1:28645"
RELAY = "http://127.0.0.1:28646"
PUBSUB = "/waku/2/rs/123/1"
CONTENT = "/oasis-cql/1/store-fixture/proto"
HASH = re.compile(r"0x[0-9a-f]{64}")


def request(url, body=None):
    headers = {"Accept": "application/json"}
    data = None
    if body is not None:
        headers["Content-Type"] = "application/json"
        data = json.dumps(body).encode("utf-8")
    with urlopen(Request(url, data=data, headers=headers), timeout=4) as response:
        content = response.read(65_537)
        if len(content) > 65_536:
            raise RuntimeError("Waku fixture response exceeds 64 KiB")
        return json.loads(content) if response.headers.get_content_type() == "application/json" else None


def wait_for(process, probe):
    deadline = time.monotonic() + 20
    while time.monotonic() < deadline:
        if process.poll() is not None:
            raise RuntimeError("The owned Waku node exited before readiness")
        try:
            result = probe()
            if result:
                return result
        except HTTPError as error:
            if error.code != 503:
                raise
        except (URLError, TimeoutError):
            pass
        time.sleep(0.25)
    raise RuntimeError("Waku fixture readiness expired")


@contextlib.contextmanager
def node(executable, work, name, rest, tcp, *configuration):
    log = work / f"{name}.log"
    with log.open("xb") as output:
        process = subprocess.Popen(
            [str(executable), "--listen-address=127.0.0.1", f"--tcp-port={tcp}",
             "--rest=true", "--rest-address=127.0.0.1", f"--rest-port={rest}",
             "--rest-admin=true", "--cluster-id=123", "--shard=1", "--relay=true",
             "--discv5-discovery=false", "--peer-exchange=false", "--rln-relay=false",
             *configuration],
            cwd=work,
            stdout=output,
            stderr=subprocess.STDOUT,
        )
        try:
            yield process
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=8)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()
            output.flush()
            with log.open("rb") as stream:
                stream.seek(max(0, log.stat().st_size - 4096))
                print(stream.read().decode("utf-8", errors="replace"), flush=True)


def stored(hashes=None):
    query = {"includeData": "true", "pageSize": 8}
    if hashes is None:
        query.update(pubsubTopic=PUBSUB, contentTopics=CONTENT)
    else:
        query["hashes"] = ",".join(hashes)
    result = request(STORE + "/store/v3/messages?" + urlencode(query))
    if result["statusCode"] != 200 or result.get("paginationCursor"):
        raise RuntimeError("Waku did not return a complete Store result")
    rows = result["messages"]
    if any(row["pubsubTopic"] != PUBSUB or row["message"]["contentTopic"] != CONTENT for row in rows):
        raise RuntimeError("Waku returned a message outside the owned fixture topics")
    return rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("executable", type=Path)
    parser.add_argument("workspace", type=Path)
    args = parser.parse_args()
    executable = args.executable.resolve(strict=True)
    work = args.workspace.resolve(strict=True)
    if executable.parent != work and work not in executable.parents:
        raise ValueError("The Waku binary must belong to the disposable runner directory")

    with node(executable, work, "store", 28645, 28600, "--store=true",
              "--store-message-db-url=sqlite://store.sqlite3") as store:
        info = wait_for(store, lambda: request(STORE + "/debug/v1/info"))
        address, = [value for value in info["listenAddresses"]
                    if value.startswith("/ip4/127.0.0.1/tcp/28600/p2p/")]
        with node(executable, work, "relay", 28646, 28601, f"--staticnode={address}") as relay:
            wait_for(relay, lambda: request(RELAY + "/admin/v1/peers/mesh/on/1")["peers"])
            payloads = {base64.b64encode(f"OASIS fixture {i}".encode()).decode() for i in range(3)}
            for payload in sorted(payloads):
                request(RELAY + "/relay/v1/messages/" + quote(PUBSUB, safe=""),
                        {"payload": payload, "contentTopic": CONTENT, "timestamp": time.time_ns(), "version": 0})
            rows = wait_for(store, lambda: result if len(result := stored()) == 3 else None)
            hashes = {row["messageHash"] for row in rows}
            if len(hashes) != 3 or any(not HASH.fullmatch(value) for value in hashes):
                raise RuntimeError("Expected three unique canonical Waku message hashes")
            if {row["message"]["payload"] for row in rows} != payloads:
                raise RuntimeError("Waku returned a payload outside the owned fixture")
            for selected in (sorted(hashes), sorted(hashes)[:2], ["0x" + "01" * 32]):
                found = stored(selected)
                if {row["messageHash"] for row in found} != hashes.intersection(selected):
                    raise RuntimeError("Waku Store did not honor its exact hash filter")
            print("Verified native Waku Store 3/2/0 via two isolated loopback peers.")


if __name__ == "__main__":
    main()
