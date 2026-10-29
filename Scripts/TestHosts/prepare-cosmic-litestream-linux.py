"""Install once the pinned native test runtimes; no SDK or package-manager mutation."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import tarfile
import urllib.request

BASE = Path("/var/tmp/oasis-runtimes")
SPECS = [
    ("dotnet-10.0.12", "aspnetcore-runtime-10.0.12-linux-x64.tar.gz",
     "https://builds.dotnet.microsoft.com/dotnet/aspnetcore/Runtime/10.0.12/aspnetcore-runtime-10.0.12-linux-x64.tar.gz",
     "sha512", "c489fc3a9dcc85f24f2d52e9a81b46fdf51affc8a360d008dbd5e3b7ce524969adc4a54f555ad13e29b822f59ed1cae9785608cbac2282f2c9c4fda45c21c7d1", "dotnet"),
    ("litestream-0.5.17", "litestream-0.5.17-linux-x86_64.tar.gz",
     "https://github.com/benbjohnson/litestream/releases/download/v0.5.17/litestream-0.5.17-linux-x86_64.tar.gz",
     "sha256", "cfb371176d164437ae869f8351cfde49bd1804ae71c61923f75c9cba9c9c006d", "litestream"),
]
for name, archive_name, url, algorithm, expected, executable in SPECS:
    destination = BASE / name
    admission = destination / "cosmic-runtime-admission.json"
    if admission.exists():
        record = json.loads(admission.read_text())
        assert record["archiveDigest"] == expected
        assert hashlib.sha256((destination / executable).read_bytes()).hexdigest() == record["executableSha256"]
        print(name + ": reused verified runtime")
        continue
    if destination.exists():
        raise RuntimeError("Unadmitted runtime directory exists; preserve instead of overlaying: " + str(destination))
    downloads = BASE / ".downloads"
    downloads.mkdir(parents=True, exist_ok=True)
    archive = downloads / archive_name
    if not archive.exists():
        with urllib.request.urlopen(url, timeout=60) as response, archive.open("xb") as output:
            shutil.copyfileobj(response, output)
    with archive.open("rb") as stream:
        observed = hashlib.file_digest(stream, algorithm).hexdigest()
    if observed != expected:
        raise RuntimeError("Official archive digest differs; preserve archive")
    archive_size = archive.stat().st_size
    destination.mkdir()
    with tarfile.open(archive) as package:
        package.extractall(destination, filter="data")
    command = [str(destination / executable), "--list-runtimes" if executable == "dotnet" else "version"]
    version = subprocess.run(command, check=True, text=True, capture_output=True).stdout.strip()
    record = dict(archiveDigest=expected, executableSha256=hashlib.sha256((destination / executable).read_bytes()).hexdigest(),
                  archiveBytes=archive_size, extractedBytes=sum(p.stat().st_size for p in destination.rglob("*") if p.is_file()), version=version)
    with admission.open("x") as output:
        json.dump(record, output, indent=2)
    # This exact verified download is owned and no longer open/in use.
    assert archive.resolve().parent == downloads.resolve()
    archive.unlink()
    print(name + ": " + json.dumps(record))
