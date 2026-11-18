'use strict';
const assert = require('node:assert/strict');
const { randomUUID } = require('node:crypto');
async function verify(input) {
  const { baseUrl, token, providerType, title } = input;
  const url = new URL(baseUrl);
  assert.equal(url.hostname, '127.0.0.1');
  assert.equal(url.protocol, 'http:');
  const sdkRoot = process.env.OASIS_PROFILE_WEB4_SDK_ROOT;
  assert.ok(sdkRoot, 'Recorded SDK checkout is required.');
  const { OASISClient } = require(sdkRoot);
  const client = new OASISClient({ baseUrl, persistSession: false });
  client.setToken('intentionally-invalid-session-token');
  const opened = await client.profile.getProfile({ providerType, token });
  assert.equal(opened.isError, false, opened.message);
  assert.equal(opened.result.providerType, providerType);
  const before = opened.result.profile;
  const edit = {
    avatarRevision: before.avatarRevision, detailRevision: before.detailRevision,
    providerType: opened.result.providerType, changes: { title, address: null }
  };
  const denied = await client.profile.applyProfile(
    { ...edit, changes: { ...edit.changes, activeQuestId: randomUUID() } },
    { token, operationId: randomUUID() });
  assert.equal(denied.statusCode, 400);
  assert.equal(denied.isError, true);
  const saved = await client.profile.applyProfile(edit, { token, operationId: randomUUID() });
  assert.equal(saved.isError, false, saved.message);
  assert.equal(saved.statusCode, 200);
  assert.equal(saved.result.providerType, providerType);
  assert.equal(saved.result.profile.title, title);
  assert.equal(saved.result.profile.address, '');
  assert.notEqual(saved.result.profile.avatarRevision.versionId, before.avatarRevision.versionId);
  const stale = await client.profile.applyProfile(edit, { token, operationId: randomUUID() });
  assert.equal(stale.isError, true);
  assert.equal(stale.statusCode, 409);
  assert.equal(stale.raw.errorCode, 'AVATAR_PROFILE_REVISION_CONFLICT');
  const reloaded = await client.profile.getProfile({ providerType, token });
  assert.equal(reloaded.isError, false, reloaded.message);
  assert.deepEqual(reloaded.result.profile, saved.result.profile);
  return {
    title: saved.result.profile.title,
    avatarVersionId: saved.result.profile.avatarRevision.versionId,
    detailVersionId: saved.result.profile.detailRevision.versionId
  };
}
module.exports = { verify };
if (require.main === module) {
  const deadline = setTimeout(() => { console.error('Owned SDK fixture exceeded its time limit.'); process.exit(1); }, 30000);
  (async () => {
    let input = '';
    for await (const chunk of process.stdin) {
      input += chunk;
      if (input.length > 8192) throw new Error('Bounded fixture input required.');
    }
    process.stdout.write(JSON.stringify(await verify(JSON.parse(input))));
  })().catch(error => { console.error(error.message); process.exitCode = 1; }).finally(() => clearTimeout(deadline));
}
