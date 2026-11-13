import json
import os
from pathlib import Path
import secrets
import subprocess
import time
import urllib.error
import urllib.request

from cosmic_linux_mongo_service import isolated_ci_mongo

PROJECT_NAME = "NextGenSoftware.OASIS.API.Providers.CouchDBOASIS.IntegrationTests"
PROJECT = Path("Providers/Storage/TestProjects") / PROJECT_NAME / (PROJECT_NAME + ".csproj")
IMAGE = "docker.io/apache/couchdb@sha256:701bf53dd95b8742cd4120fb8f5aea8f14c0d8a77cfa807e86caaa537f822ee1"
ENDPOINT = "http://127.0.0.1:15984"
STATE = Path("/var/tmp/oasis-couchdb-cql")


def docker(*args):
    return subprocess.check_output(["docker", *args], text=True).strip()


def test(test_class, env, *, build=False):
    command = ["dotnet", "test", str(PROJECT), "--filter", "FullyQualifiedName~" + test_class,
               "/m:2", "/nr:false", "/p:UseSharedCompilation=false"]
    if not build:
        command.append("--no-build")
    subprocess.run(command, env=env, check=True)


def start(container):
    docker("start", container)
    deadline = time.monotonic() + 45
    while True:
        if docker("inspect", "--format", "{{.State.Running}}", container) != "true":
            raise RuntimeError("Owned CouchDB container exited before readiness")
        try:
            with urllib.request.urlopen(ENDPOINT, timeout=3) as response:
                info = json.load(response)
            break
        except urllib.error.HTTPError as error:
            if error.code != 503:
                raise
            last_error = error
        except (urllib.error.URLError, ConnectionError, TimeoutError) as error:
            last_error = error
        if time.monotonic() >= deadline:
            raise RuntimeError("Owned CouchDB readiness deadline exceeded") from last_error
        time.sleep(1)
    if info["version"] != "3.5.1" or not info["uuid"]:
        raise RuntimeError("CouchDB native identity was not admitted")
    return info["uuid"]


def main():
    if os.environ.get("GITHUB_ACTIONS") != "true":
        raise RuntimeError("This fixture requires a disposable GitHub Actions runner")
    STATE.mkdir(mode=0o700)
    password = secrets.token_urlsafe(24)
    username = "admin"
    (STATE / "credentials.json").write_text(json.dumps({"username": username, "password": password}))
    env = {**os.environ, "COSMIC_COUCHDB_ROOT": str(STATE)}
    container = docker("create", "--publish", "127.0.0.1:15984:5984",
                       "--env", "COUCHDB_USER=" + username, "--env", "COUCHDB_PASSWORD=" + password, IMAGE)
    try:
        identity = start(container)
        (STATE / "environment.json").write_text(json.dumps({"containerId": container, "uuid": identity}))
        test("CouchDbNativeCqlTests", env, build=True)
        docker("stop", container)
        if start(container) != identity:
            raise RuntimeError("CouchDB identity changed after restart")
        test("CouchDbNativeCqlTests", env)
        with isolated_ci_mongo() as mongo:
            test("LinuxMongoDemoEnvironmentTests", {**env, **mongo})
            test("CouchDBMongoCqlNativeTests", {**env, **mongo})
        test("CouchDbNativeCqlTests", env)
    finally:
        docker("rm", "-f", "-v", container)


if __name__ == "__main__":
    main()
