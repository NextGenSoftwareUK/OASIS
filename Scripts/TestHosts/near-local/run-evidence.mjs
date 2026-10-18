import { spawnSync } from "node:child_process";
import { readFileSync, realpathSync } from "node:fs";
import { createHash } from "node:crypto";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { KeyPair } from "near-workspaces";
import { createRetainedWorker } from "./retained-worker.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const repoRoot = process.env.OASIS_REPO_ROOT_WSL ?? resolve(here, "../../..");
const toWindows = path => path.replace(/^\/mnt\/([a-z])/i, (_, drive) => `${drive.toUpperCase()}:`).replaceAll("/", "\\");
const rpcAddr=process.env.OASIS_NEAR_REUSABLE_RPC_URL;
const expectedChain=process.env.OASIS_NEAR_REUSABLE_CHAIN_ID;
const contractId=process.env.OASIS_NEAR_REUSABLE_CONTRACT_ID;
const signerPath=process.env.OASIS_NEAR_REUSABLE_SIGNER_PATH;
if(!rpcAddr || !expectedChain || !contractId || !signerPath)
  throw new Error("Explicit retained NEAR RPC, chain, contract and signer-path configuration is required; no sandbox initialization/reset/download is permitted.");
const endpoint=new URL(rpcAddr);
if(endpoint.protocol!=="http:" || endpoint.hostname!=="127.0.0.1" || endpoint.username || endpoint.password)
  throw new Error("NEAR evidence requires an explicit loopback HTTP endpoint.");
const signerResolved=realpathSync(signerPath);
if(!signerResolved.startsWith("/var/tmp/oasis-near-evidence/"))
  throw new Error("NEAR evidence signer must belong to the retained test environment.");
const wasm = readFileSync(resolve(here, "build/oasis_storage.wasm"));
const statusResponse=await fetch(rpcAddr,{method:"POST",headers:{"content-type":"application/json"},
  body:JSON.stringify({jsonrpc:"2.0",id:"near-retained-status",method:"status",params:[]})});
if(!statusResponse.ok)throw new Error("Retained NEAR status request failed.");
const status=await statusResponse.json();
if(status.result?.chain_id!==expectedChain)throw new Error("Retained NEAR chain identity differs; refusing signed operations.");
let signer;
let key;
try{
  signer=JSON.parse(readFileSync(signerResolved,"utf8"));
  if(signer.account_id!==contractId)throw new Error("signer account differs");
  key=KeyPair.fromString(signer.secret_key);
}catch{throw new Error("Retained NEAR signer configuration is invalid; no replacement is generated.");}
// CustomnetWorker connects only. SandboxWorker.init deletes an existing home and is forbidden here.
const worker = await createRetainedWorker(rpcAddr,contractId);
await worker.manager.setKey(contractId,key);

try {
  const contract = worker.rootAccount;
  const codeResponse = await fetch(rpcAddr, {
    method: "POST",
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ jsonrpc: "2.0", id: "oasis-near-code", method: "query", params: {request_type:"view_code",account_id:contractId,finality:"final"} }),
  });
  if (!codeResponse.ok) throw new Error("Retained NEAR code lookup failed.");
  const code=await codeResponse.json();
  if(typeof code.result?.code_base64!=="string" || createHash("sha256").update(Buffer.from(code.result.code_base64,"base64")).digest("hex")
      !==createHash("sha256").update(wasm).digest("hex"))
    throw new Error("Retained NEAR contract bytecode differs; no automatic redeployment or initialization is permitted.");
  const networkId=expectedChain;

  // WSL does not propagate arbitrary environment variables to Windows executables.
  // Pass owned test credentials through stdin, never a command line or plaintext file.
  const settings = {
    NEAROASIS_RPCENDPOINT: rpcAddr,
    NEAROASIS_NETWORKID: networkId,
    NEAROASIS_CHAINID: networkId,
    NEAROASIS_CONTRACTADDRESS: contract.accountId,
    NEAROASIS_ACCOUNTID: contract.accountId,
    NEAROASIS_PRIVATEKEY: key.toString(),
    OASIS_NEAR_NODE: "C:\\Program Files\\nodejs\\node.exe",
    OASIS_NEAR_SDK_BRIDGE: toWindows(resolve(repoRoot, "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.NEAROASIS/SdkBridge/bridge.mjs")),
  };

  const result = spawnSync("/mnt/c/Windows/System32/WindowsPowerShell/v1.0/powershell.exe", [
    "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File",
    toWindows(resolve(repoRoot, "Scripts/TestHosts/near-local/run-dotnet-evidence.ps1")),
    "-SettingsFromStdin",
    "-RepositoryRoot", toWindows(repoRoot),
  ], { stdio: ["pipe","inherit","inherit"], input: JSON.stringify(settings) });
  if (result.error) throw result.error;
  if (result.status !== 0) process.exitCode = result.status ?? 1;
} finally {
  await worker.tearDown();
}
