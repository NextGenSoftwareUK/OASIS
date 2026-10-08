import { spawnSync } from "node:child_process";
import { readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { Worker } from "near-workspaces";

const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = process.env.OASIS_REPO_ROOT_WSL ?? resolve(here, "../../..");
const toWindows = path => path.replace(/^\/mnt\/([a-z])/i, (_, drive) => `${drive.toUpperCase()}:`).replaceAll("/", "\\");
const wasm = readFileSync(resolve(here, "build/oasis_storage.wasm"));
const worker = await Worker.init();

try {
  const contract = await worker.rootAccount.createSubAccount("oasis");
  await contract.deploy(wasm);
  await contract.call(contract, "init", { owner_id: contract.accountId });
  const key = await contract.getKey();
  if (!key) throw new Error("NEAR sandbox did not provide the contract account key");
  const statusResponse = await fetch(worker.provider.connection.url, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ jsonrpc: "2.0", id: "oasis-near-evidence", method: "status", params: [] }),
  });
  if (!statusResponse.ok) throw new Error(`NEAR sandbox status failed with HTTP ${statusResponse.status}`);
  const status = await statusResponse.json();
  const networkId = status?.result?.chain_id;
  if (!networkId) throw new Error("NEAR sandbox status did not contain chain_id");

  // WSL does not propagate arbitrary environment variables to Windows executables.
  // Pass the disposable sandbox credentials through a temporary JSON file instead
  // of exposing the private key in the process command line.
  const settingsPath = process.env.OASIS_NEAR_SETTINGS_PATH_WSL;
  if (!settingsPath) throw new Error("OASIS_NEAR_SETTINGS_PATH_WSL is required");
  writeFileSync(settingsPath, JSON.stringify({
    NEAROASIS_RPCENDPOINT: worker.provider.connection.url,
    NEAROASIS_NETWORKID: networkId,
    NEAROASIS_CHAINID: networkId,
    NEAROASIS_CONTRACTADDRESS: contract.accountId,
    NEAROASIS_ACCOUNTID: contract.accountId,
    NEAROASIS_PRIVATEKEY: key.toString(),
    OASIS_NEAR_NODE: "C:\\Program Files\\nodejs\\node.exe",
    OASIS_NEAR_SDK_BRIDGE: toWindows(resolve(repoRoot, "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/bridge.mjs")),
  }), { mode: 0o600 });

  const result = spawnSync("/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe", [
    "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File",
    toWindows(resolve(repoRoot, "Scripts/TestHosts/near-local/run-dotnet-evidence.ps1")),
    "-SettingsPath", toWindows(settingsPath),
    "-RepositoryRoot", toWindows(repoRoot),
  ], { stdio: "inherit" });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exitCode = result.status ?? 1;
} finally {
  await worker.tearDown();
}
