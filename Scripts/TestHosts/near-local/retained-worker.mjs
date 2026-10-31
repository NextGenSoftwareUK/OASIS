import { createRequire } from "node:module";
import { Worker } from "near-workspaces";

const require=createRequire(import.meta.url);
const workspaceSdk=createRequire(require.resolve("near-workspaces"))("near-api-js");

export async function createRetainedWorker(rpcAddr,rootAccountId){
  // CustomnetManager.defaultKeyStore returns a new object on each access. Supply one
  // explicit store, using the SAME SDK version as near-workspaces, so keys persist.
  return Worker.init({network:"custom",rpcAddr,rootAccountId,keyStore:new workspaceSdk.keyStores.InMemoryKeyStore()});
}
