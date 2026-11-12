import assert from "node:assert/strict";
import { spawn } from "node:child_process";
import { randomBytes } from "node:crypto";
import { once } from "node:events";
import { readFile } from "node:fs/promises";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { Worker } from "near-workspaces";

assert.equal(process.env.OASIS_NEAR_FRESH_MANAGED_LIFECYCLE, "1");
const reportRoot = process.env.OASIS_NEAR_FRESH_REPORT_ROOT;
assert.ok(reportRoot);
const port = 18830;
const endpoint = "http://127.0.0.1:" + port;
const worker = await Worker.init({ network: "sandbox", port, rpcAddr: endpoint });
try {
  const owner = worker.rootAccount;
  const signer = await owner.getKey();
  assert.ok(signer);
  const contract = await owner.devDeploy("build/oasis_storage.wasm");
  await owner.call(contract, "init", { owner_id: owner.accountId });
  assert.deepEqual(await contract.view("entries", {}), []);
  const response = await fetch(endpoint + "/status", { signal: AbortSignal.timeout(5000) });
  assert.ok(response.ok);
  const status = await response.json();
  assert.ok(status.chain_id && !["mainnet", "testnet"].includes(status.chain_id));
  const privateKey = signer.toString();
  const storageKey = randomBytes(32).toString("base64");
  console.log("::add-mask::" + privateKey);
  console.log("::add-mask::" + storageKey);
  const projectRoot = fileURLToPath(new URL("../../../", import.meta.url));
  const script = `
    $ErrorActionPreference = 'Stop'
    ./Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner CI-NearStorage -Operation {
      $directory = "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests"
      $project = "$directory/NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests.csproj"
      dotnet build $project /p:NearDirectProviderOnly=true /m:2 /nr:false /p:UseSharedCompilation=false
      if ($LASTEXITCODE -ne 0) { throw "Native NEAR integration build failed." }
      npm ci --prefix "$directory/bin/Debug/net10.0/NearSdkBridge" --no-audit --no-fund
      if ($LASTEXITCODE -ne 0) { throw "Locked NEAR SDK installation failed." }
      dotnet test $project --no-build --filter "FullyQualifiedName~NearSharedLifecycleTests" /p:NearDirectProviderOnly=true /m:2 /nr:false /p:UseSharedCompilation=false --results-directory $env:OASIS_NEAR_LIFECYCLE_REPORT_ROOT --logger "trx;LogFileName=near-native.trx" --logger "console;verbosity=normal"
      if ($LASTEXITCODE -ne 0) { throw "Native NEAR storage verification failed." }
      [xml]$trx = Get-Content -LiteralPath (Join-Path $env:OASIS_NEAR_LIFECYCLE_REPORT_ROOT 'near-native.trx')
      if ($trx.TestRun.ResultSummary.Counters.passed -ne '3') { throw 'All three NEAR native cases must pass.' }
    }
  `;
  const child = spawn("pwsh", ["-NoProfile", "-Command", script], {
    cwd: projectRoot,
    stdio: "inherit",
    env: {
      ...process.env,
      NEAROASIS_RPCENDPOINT: endpoint,
      NEAROASIS_NETWORKID: status.chain_id,
      NEAROASIS_CHAINID: status.chain_id,
      NEAROASIS_CONTRACTADDRESS: contract.accountId,
      NEAROASIS_ACCOUNTID: owner.accountId,
      NEAROASIS_PRIVATEKEY: privateKey,
      OASIS_NEAR_STORAGE_ENCRYPTION_KEY: storageKey,
      OASIS_NEAR_LIFECYCLE_REPORT_ROOT: reportRoot,
    },
  });
  const [code, signal] = await once(child, "exit");
  assert.equal(code, 0, "Managed NEAR tests failed: " + (signal ?? code));
  for (const mode of ["async", "sync"]) {
    const report = JSON.parse(await readFile(join(reportRoot, mode, "report.json"), "utf8"));
    assert.equal(report.Succeeded, true);
    assert.equal(report.FixtureCleanupComplete, true);
    assert.equal(report.Passed.length, 26);
  }
  assert.deepEqual(await contract.view("entries", {}), []);
  console.log("Both NEAR storage modes and the managed avatar history regression passed with an empty native contract.");
} finally {
  await worker.tearDown();
}
