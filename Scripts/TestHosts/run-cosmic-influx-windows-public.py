"""One disposable pinned Windows InfluxDB server. Run against an already-built fixture."""
import hashlib
import json
import os
from pathlib import Path
import secrets
import shutil
import socket
import ssl
import subprocess
import time
import urllib.error
import urllib.parse
import urllib.request
import uuid
from xml.etree import ElementTree

root = Path("LocalState/COSMIC/HostedInfluxCqlQualification/influxdb-win-x64-2.7.12").resolve(strict=True)
receipt = json.loads((root / "runtime-receipt.json").read_text(encoding="utf-8-sig"))
server = (root / "influxd.exe").resolve(strict=True)
with server.open("rb") as executable:
    assert hashlib.file_digest(executable, "sha256").hexdigest().upper() == receipt["serverSha256"]
openssl = Path("C:/Program Files/Git/usr/bin/openssl.exe")
project = Path("Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS.IntegrationTests.csproj")
scratch = root / "public-native-final"
scratch.mkdir()
data = scratch / "native-data"
data.mkdir()
cert, key = scratch / "fixture.crt", scratch / "fixture.key"
organization, bucket = "cosmic-teaching", "cosmic-public"
owner = secrets.token_urlsafe(48)
catalogue = {"Actor": str(uuid.uuid4()), "LiveIds": [str(uuid.uuid4()) for _ in range(3)],
             "DeletedId": str(uuid.uuid4()), "MissingId": str(uuid.uuid4())}
(scratch / "catalogue.json").write_text(json.dumps(catalogue), encoding="utf-8")
process = None

class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args):
        return None

try:
    subprocess.run([openssl, "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-days", "2",
        "-subj", "/CN=localhost", "-addext", "subjectAltName=DNS:localhost",
        "-keyout", key, "-out", cert], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
        check=True, creationflags=subprocess.CREATE_NO_WINDOW)
    with socket.socket() as reservation:
        reservation.bind(("127.0.0.1", 0))
        port = reservation.getsockname()[1]
    assert 49152 <= port <= 65535
    args = [server, "run", "--http-bind-address", f"127.0.0.1:{port}", "--bolt-path", data / "influxd.bolt",
            "--sqlite-path", data / "influxd.sqlite", "--engine-path", data / "engine",
            "--tls-cert", cert, "--tls-key", key, "--reporting-disabled"]
    with (scratch / "server.log").open("xb") as output:
        process = subprocess.Popen(args, cwd=data, stdout=output, stderr=subprocess.STDOUT,
                                   creationflags=subprocess.CREATE_NO_WINDOW)
        origin = f"https://localhost:{port}"
        context = ssl.create_default_context(cafile=str(cert))
        opener = urllib.request.build_opener(NoRedirect(), urllib.request.HTTPSHandler(context=context))

        def request(route, body=None, token=None, content_type="application/json"):
            headers = {"Content-Type": content_type}
            if token:
                headers["Authorization"] = "Token " + token
            req = urllib.request.Request(origin + route, data=body, headers=headers)
            try:
                response = opener.open(req, timeout=12)
            except urllib.error.HTTPError as error:
                response = error
            with response:
                raw = response.read(65537)
                assert len(raw) <= 65536
                return response.status, raw

        until = time.monotonic() + 25
        while True:
            if process.poll() is not None or time.monotonic() >= until:
                raise RuntimeError("InfluxDB did not open its scoped TLS port.")
            try:
                status, health = request("/health")
                if status == 200 and json.loads(health)["status"] == "pass":
                    break
            except (urllib.error.URLError, TimeoutError):
                time.sleep(0.2)
        assert json.loads(health)["version"] == "v2.7.12"

        def encoded(value):
            return json.dumps(value, separators=(",", ":")).encode()
        assert json.loads(request("/api/v2/setup")[1])["allowed"] is True
        status, raw = request("/api/v2/setup", encoded({"username": "cosmic-owner", "password": secrets.token_urlsafe(28),
            "org": organization, "bucket": bucket, "token": owner, "retentionPeriodSeconds": 3600}))
        assert status == 201
        setup = json.loads(raw)
        assert setup["auth"]["token"] == owner
        org_id, bucket_id = setup["org"]["id"], setup["bucket"]["id"]
        status, raw = request("/api/v2/authorizations", encoded({"orgID": org_id, "description": "CQL fixture reader",
            "permissions": [{"action": "read", "resource": {"type": "buckets", "id": bucket_id, "orgID": org_id}}]}), owner)
        assert status == 201
        permission = json.loads(raw)
        assert len(permission["permissions"]) == 1 and permission["permissions"][0]["action"] == "read"
        assert permission["permissions"][0]["resource"]["id"] == bucket_id
        reader = permission["token"]
        write = "/api/v2/write?" + urllib.parse.urlencode({"org": organization, "bucket": bucket, "precision": "ns"})
        assert request(write, b"denial_probe value=1i", reader, "text/plain")[0] in (401, 403)

        points = []
        stamp = time.time_ns()
        for index, identifier in enumerate([*catalogue["LiveIds"], catalogue["DeletedId"]]):
            row = {"id": identifier, "name": f"InfluxDB teaching record {index + 1}",
                   "isPublic": True, "createdByAvatarId": catalogue["Actor"], "children": [], "metaData": {}}
            escaped = encoded(row).decode().replace("\\", "\\\\").replace('"', '\\"')
            deleted = "true" if index == 3 else "false"
            points.append(f'oasis_holons,id={identifier} is_deleted={deleted},data_json="{escaped}" {stamp + index}')
        point_data = "\n".join(points).encode()
        (scratch / "seed.intent.json").write_text(json.dumps({"points": len(points), "sha256": hashlib.sha256(point_data).hexdigest()}))
        assert request(write, point_data, owner, "text/plain")[0] == 204
        (scratch / "seed.ack.json").write_text(json.dumps({"points": len(points), "status": 204}))

        def observe():
            digest = hashlib.sha256()
            for field in ("data_json", "is_deleted"):
                flux = (f'from(bucket: "{bucket}") |> range(start: 0, stop: time(v: 9223372036854775806)) '
                        f'|> filter(fn: (r) => r._field == "{field}") |> group() |> sort(columns: ["id"])')
                status, snapshot = request(f"/api/v2/query?org={organization}", encoded({"query": flux}), reader)
                if status != 200:
                    (scratch / "snapshot.error.json").write_bytes(snapshot)
                    raise RuntimeError(f"Native observation for {field} returned HTTP {status}.")
                assert all(identifier.encode() in snapshot for identifier in [*catalogue["LiveIds"], catalogue["DeletedId"]])
                assert catalogue["MissingId"].encode() not in snapshot
                digest.update(snapshot)
            return digest.hexdigest()

        before = observe()
        env = dict(os.environ, COSMIC_INFLUX_WINDOWS_PORT=str(port), COSMIC_INFLUX_WINDOWS_CERT=str(cert),
                   COSMIC_INFLUX_WINDOWS_CATALOGUE=str(scratch / "catalogue.json"), COSMIC_INFLUX_WINDOWS_READER=reader)
        with (scratch / "native.log").open("xb") as log:
            subprocess.run(["dotnet", "test", str(project), "--no-build", "--no-restore", "/m:2", "/nr:false",
                "/p:UseSharedCompilation=false", "/p:EnableInfluxDbWindowsPublicCqlNativeTests=true", "--filter",
                "TestCategory=InfluxDbWindowsPublicCqlNative", "--results-directory", str(scratch),
                "--logger", "trx;LogFileName=native.trx"], env=env, stdout=log, stderr=subprocess.STDOUT, check=True, timeout=90)
        counts = ElementTree.parse(scratch / "native.trx").find(".//{*}Counters").attrib
        assert counts["total"] == counts["passed"] == "1" and counts["failed"] == "0"
        assert observe() == before
finally:
    if process is not None:
        if process.poll() is None:
            process.terminate()
        process.wait(timeout=15)
    key.unlink(missing_ok=True)
    cert.unlink(missing_ok=True)
    if data.is_dir():
        assert data.resolve(strict=True) == scratch / "native-data"
        assert data.resolve(strict=True).is_relative_to(root)
        shutil.rmtree(data)
with socket.socket() as check:
    check.settimeout(2)
    assert check.connect_ex(("127.0.0.1", port)) != 0
proof = {"nativeTrxSha256": hashlib.sha256((scratch / "native.trx").read_bytes()).hexdigest(),
         "nativeSnapshotSha256": before, "restrictedWriteDenied": True, "listenerClosed": True}
(scratch / "proof.json").write_text(json.dumps(proof, indent=2) + "\n")
print(json.dumps(proof))
