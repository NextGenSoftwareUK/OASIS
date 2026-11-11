"""Run the existing native Redis CQL and federation tests on disposable CI."""
import base64
import os
from pathlib import Path
import secrets
import subprocess
import time
import xml.etree.ElementTree as ET

from cosmic_linux_mongo_service import isolated_ci_mongo

PROJECT = "Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests.csproj"
IMAGE = "redis@sha256:7b1753b4c5ad8458906c272a8132b8c15f5557b000379eb115fb239706a384b7"


def docker(*args):
    return subprocess.check_output(["docker", *args], text=True).strip()


def test(label, test_class, results, env, *, build=False):
    command = ["dotnet", "test", PROJECT, "--filter", "FullyQualifiedName~" + test_class,
               "--results-directory", str(results), "--logger", "trx;LogFileName=" + label + ".trx",
               "/m:2", "/nr:false", "/p:UseSharedCompilation=false"]
    if not build:
        command.append("--no-build")
    subprocess.run(command, env=env, check=True)
    counters = ET.parse(results / (label + ".trx")).find(".//{*}Counters").attrib
    assert counters["total"] == counters["passed"] == "1" and counters["notExecuted"] == "0"


def start(container, password):
    docker("start", container)
    deadline = time.monotonic() + 30
    while time.monotonic() < deadline:
        if docker("inspect", "--format", "{{.State.Running}}", container) != "true":
            raise RuntimeError("Owned Redis container exited during startup")
        probe = subprocess.run(["docker", "exec", "-e", "REDISCLI_AUTH", container, "redis-cli", "ping"],
                               env={**os.environ, "REDISCLI_AUTH": password}, text=True, capture_output=True, timeout=5)
        if probe.returncode == 0 and probe.stdout.strip() == "PONG":
            return
        time.sleep(1)
    raise RuntimeError("Owned Redis did not accept authenticated commands")


def main():
    if os.environ.get("GITHUB_ACTIONS") != "true":
        raise RuntimeError("This fixture requires disposable GitHub Actions")
    os.umask(0o077)
    state = Path(os.environ["RUNNER_TEMP"]) / "redis-cql"
    state.mkdir()
    data = state / "data"
    data.mkdir()
    results = state / "results"
    results.mkdir()
    password = secrets.token_hex(32)
    config = state / "redis.conf"
    config.write_text("bind 0.0.0.0\nprotected-mode yes\nappendonly yes\nappendfsync always\n"
                      "dir /data\nrequirepass " + password + "\n")
    env = {**os.environ, "OASIS_REDIS_CQL_STATE": str(state), "OASIS_REDIS_NATIVE_PASSWORD": password,
           "OASIS_REDIS_STORAGE_ENCRYPTION_KEY": base64.b64encode(secrets.token_bytes(32)).decode()}
    container = docker("create", "--user", f"{os.getuid()}:{os.getgid()}",
                       "--publish", "127.0.0.1:18679:6379",
                       "--mount", f"type=bind,source={data},target=/data",
                       "--mount", f"type=bind,source={config},target=/redis.conf,readonly",
                       IMAGE, "redis-server", "/redis.conf")
    try:
        start(container, password)
        test("native", "RedisCqlNativeTests", results, env, build=True)
        docker("stop", container)
        start(container, password)
        test("restart", "RedisCqlNativeTests", results, env)
        with isolated_ci_mongo() as mongo:
            test("mongo-init", "LinuxMongoDemoEnvironmentTests", results, {**env, **mongo})
            test("federation", "RedisMongoCqlNativeTests", results, {**env, **mongo})
        test("source-recheck", "RedisCqlNativeTests", results, env)
    finally:
        docker("rm", "-f", "-v", container)


if __name__ == "__main__":
    main()
