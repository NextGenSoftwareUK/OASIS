import hashlib
import json
import os
from pathlib import Path
import secrets
import socket
import ssl
import subprocess
import time
from xml.etree import ElementTree

root = Path("LocalState/COSMIC/HostedGarnetKeyDbCqlQualification/garnet-win-x64-2.2.5").resolve(strict=True)
receipt = json.loads((root / "runtime-receipt.json").read_text(encoding="utf-8-sig"))
server = (root / receipt["server"]).resolve(strict=True)
assert server.is_relative_to(root)
with server.open("rb") as executable:
    assert hashlib.file_digest(executable, "sha256").hexdigest().upper() == receipt["serverSha256"]
openssl = Path("C:/Program Files/Git/usr/bin/openssl.exe")
project = Path("Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests.csproj")
scratch = root / "encrypted-cql-native"
scratch.mkdir()
cert, key, pfx, acl = [scratch / name for name in ("fixture.crt", "fixture.key", "fixture.pfx", "users.acl")]
admin, reader, cert_password = [secrets.token_hex(24) for _ in range(3)]
env = dict(os.environ, OASIS_GARNET_FIXTURE_CERT_PASSWORD=cert_password)
quiet = dict(stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, check=True, creationflags=subprocess.CREATE_NO_WINDOW)
process = None
try:
    subprocess.run([openssl, "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-days", "2",
        "-subj", "/CN=localhost", "-addext", "subjectAltName=DNS:localhost",
        "-keyout", key, "-out", cert], **quiet)
    subprocess.run([openssl, "pkcs12", "-export", "-inkey", key, "-in", cert, "-out", pfx,
        "-passout", "env:OASIS_GARNET_FIXTURE_CERT_PASSWORD"], env=env, **quiet)
    acl.write_text(f"user default off\nuser fixture-admin on >{admin} ~* +@all\n"
                   f"user teaching-reader on >{reader} ~* +ping +echo +info +get\n", encoding="ascii")
    with socket.socket() as reservation:
        reservation.bind(("127.0.0.1", 0))
        port = reservation.getsockname()[1]
    assert 49152 <= port <= 65535
    args = [server, "--bind", "127.0.0.1", "--port", str(port), "--memory", "64m", "--no-obj", "--lua",
            "--auth", "ACL", "--acl-file", acl, "--tls", "--cert-file-name", pfx, "--cert-password", cert_password,
            "--client-certificate-required=false"]
    with (scratch / "server.log").open("xb") as output:
        process = subprocess.Popen(args, cwd=scratch, stdout=output, stderr=subprocess.STDOUT,
                                   creationflags=subprocess.CREATE_NO_WINDOW)
        context = ssl.create_default_context(cafile=str(cert))
        deadline = time.monotonic() + 20
        while True:
            if process.poll() is not None or time.monotonic() >= deadline:
                raise RuntimeError("Garnet did not open its scoped TLS port.")
            try:
                with socket.create_connection(("127.0.0.1", port), timeout=1) as connection:
                    with context.wrap_socket(connection, server_hostname="localhost"):
                        break
            except (ConnectionRefusedError, TimeoutError):
                time.sleep(0.2)
        env = dict(os.environ, COSMIC_GARNET_WINDOWS_PORT=str(port), COSMIC_GARNET_WINDOWS_CERT=str(cert),
                   COSMIC_GARNET_WINDOWS_ADMIN=admin, COSMIC_GARNET_WINDOWS_READER=reader)
        with (scratch / "test.log").open("xb") as test_log:
            subprocess.run(["dotnet", "test", str(project), "--no-build", "--filter", "TestCategory=GarnetWindowsPublicCqlNative",
                            "--results-directory", str(scratch), "--logger", "trx;LogFileName=native.trx",
                            "/m:2", "/nr:false", "/p:UseSharedCompilation=false"],
                           env=env, stdout=test_log, stderr=subprocess.STDOUT, check=True, timeout=90)
    counters = ElementTree.parse(scratch / "native.trx").find(".//{*}Counters").attrib
    assert counters["total"] == counters["passed"] == "1" and counters["failed"] == "0"
finally:
    if process is not None:
        if process.poll() is None:
            process.terminate()
        process.wait(timeout=10)
    for generated in (key, cert, pfx, acl):
        generated.unlink(missing_ok=True)
with socket.socket() as check:
    check.settimeout(2)
    assert check.connect_ex(("127.0.0.1", port)) != 0
with (scratch / "native.trx").open("rb") as result:
    trx_sha256 = hashlib.file_digest(result, "sha256").hexdigest()
(scratch / "proof.json").write_text(json.dumps({"nativeTrxSha256": trx_sha256, "listenerClosed": True,
                                               "runtimeSha256": receipt["serverSha256"]}, indent=2) + "\n")
print(trx_sha256)
