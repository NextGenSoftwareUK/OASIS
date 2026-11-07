import { join } from "node:path";
import { createOrbitDB, KeyValueIndexed, isValidAddress } from "@orbitdb/core";
import { gossipsub } from "@libp2p/gossipsub";
import { identify } from "@libp2p/identify";
import { LevelBlockstore } from "blockstore-level";
import { createHelia } from "helia";
import { createLibp2p } from "libp2p";

const scope = "oasis-cql-public-teaching-v1";

export async function openTeachingStore(directory, address) {
  if (address !== undefined && !isValidAddress(address))
    throw new Error("Use a reviewed OrbitDB database address.");
  const blockstore = new LevelBlockstore(join(directory, "blocks"));
  const libp2p = await createLibp2p({
    services: {
      pubsub: gossipsub({ allowPublishToZeroTopicPeers: true }),
      identify: identify(),
    },
  });
  let ipfs;
  let orbitdb;
  const close = async () => {
    try {
      await orbitdb?.stop();
    } finally {
      if (ipfs) await ipfs.stop();
      else await libp2p.stop();
    }
  };
  try {
    ipfs = await createHelia({ libp2p, blockstore, routers: [] });
    orbitdb = await createOrbitDB({ ipfs, directory: join(directory, "orbitdb") });
    const options = { Database: KeyValueIndexed(), sync: false };
    const db = address === undefined
      ? await orbitdb.open("oasis.teaching.holons", { ...options, type: "keyvalue", meta: { scope } })
      : await orbitdb.open(address, options);
    if (db.type !== "keyvalue" || db.meta?.scope !== scope || (address !== undefined && db.address !== address))
      throw new Error("OrbitDB teaching manifest differs from the reviewed source.");
    return { db, close };
  } catch (error) {
    await close();
    throw error;
  }
}
