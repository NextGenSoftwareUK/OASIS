import assert from "node:assert/strict";
import { execFile, fork } from "node:child_process";
import { randomBytes } from "node:crypto";
import { once } from "node:events";
import { mkdir, readFile } from "node:fs/promises";
import { join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { promisify } from "node:util";

const [path, testProject, results] = process.argv.slice(2);
if (!path) throw new Error("An empty disposable fixture directory is required.");
if (Boolean(testProject) !== Boolean(results)) throw new Error("Supply both the built test project and results directory.");
const directory = resolve(path);
await mkdir(directory);
const run = promisify(execFile);
const probe = fileURLToPath(new URL("./probe-indexed.mjs", import.meta.url));
const server = fileURLToPath(new URL("./serve-teaching.mjs", import.meta.url));
await run(process.execPath, [probe, "seed", directory], { timeout: 30000, windowsHide: true });
const { address, actor, ids } = JSON.parse(await readFile(join(directory, "fixture.json"), "utf8"));
const reader = randomBytes(32).toString("hex");
const instance = randomBytes(16).toString("hex");
const port = 54869;
const child = fork(server, [directory, address, String(port)], {
  env: { ...process.env, OASIS_ORBITDB_CQL_READER_TOKEN: reader, OASIS_CQL_TEACHING_INSTANCE: instance },
  windowsHide: true,
});
const exited = once(child, "exit");
let timer;
try {
  await Promise.race([
    once(child, "message").then(([message]) => assert.equal(message, "ready")),
    exited.then(() => { throw new Error("OrbitDB teaching service exited before readiness."); }),
    new Promise((_, reject) => { timer = setTimeout(() => reject(new Error("OrbitDB teaching service readiness timed out.")), 30000); }),
  ]);
  const route = `http://127.0.0.1:${port}/oasis/cql/holon?id=`;
  const get = (suffix, token = reader, method = "GET") => fetch(route + suffix, {
    method, redirect: "error", signal: AbortSignal.timeout(5000),
    headers: token ? { Authorization: `Bearer ${token}` } : {},
  });
  const publicRows = [];
  for (const id of ids.slice(0, 3)) {
    const response = await get(id);
    assert.equal(response.status, 200);
    assert.equal(response.headers.get("X-OASIS-Teaching-Instance"), instance);
    publicRows.push(await response.json());
  }
  assert.deepEqual(publicRows, ids.slice(0, 3).map(id => ({
    id, createdByAvatarId: actor, name: `teaching-${id}`, isPublic: true, isDeleted: false,
  })));
  assert.equal((await get(ids[0], "")).status, 401);
  assert.equal((await get(ids[0], "invalid")).status, 401);
  assert.equal((await get(ids[3])).status, 403);
  assert.equal((await get(ids[0], reader, "POST")).status, 404);
  assert.equal((await get(ids[0] + "&id=" + ids[1])).status, 400);
  for (const id of ids.slice(4)) {
    const response = await get(id);
    assert.equal(response.status, 200);
    assert.equal(await response.json(), null);
  }
  if (testProject) {
    const result = await run("dotnet", ["test", testProject, "--no-build", "--no-restore",
      "--filter", "TestCategory=OrbitDbCqlNative", "--results-directory", results,
      "--logger", "trx;LogFileName=provider-native.trx"], {
      env: { ...process.env, COSMIC_ORBITDB_CQL_FIXTURE: join(directory, "fixture.json"),
        OASIS_ORBITDB_CQL_READER_TOKEN: reader },
      timeout: 60000,
      windowsHide: true,
    });
    console.log(result.stdout.trim());
  }
} finally {
  clearTimeout(timer);
  if (child.connected) child.send("shutdown");
  const [exit] = await exited;
  assert.equal(exit, 0);
}
await run(process.execPath, [probe, "verify", directory], { timeout: 30000, windowsHide: true });
console.log("PASS: real indexed OrbitDB HTTP reads, authorization, child projection and persistence after restart.");
