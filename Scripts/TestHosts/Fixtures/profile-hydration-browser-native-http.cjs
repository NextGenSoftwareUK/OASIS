'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors, dashboardExpiredToken, dashboardReplacementToken }) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1'); assert.equal(actors.length, 3);
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    acceptDownloads: false, serviceWorkers: 'block' });
  const reads = [], writes = [], errors = [], external = [], gates = [];
  let phase = 'launch';
  const isRead = url => url.pathname.startsWith('/api/avatar/get-avatar-detail-by-id/');
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      external.push(url.pathname); return route.abort('blockedbyclient');
    });
    const page = await context.newPage(); page.setDefaultTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      const url = new URL(request.url());
      if (isRead(url)) reads.push(request.method());
      if (url.pathname.startsWith('/api/') && request.method() !== 'GET') writes.push(request.method());
    });
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => typeof window.hydrateAvatarProfile === 'function');
    async function session(actor, bearer, cached, replaceClient = false) {
      await page.evaluate(({ actor, bearer, cached, replaceClient }) => {
        if (replaceClient) window.oasisClient = new window.OASISClient({ baseUrl: location.origin, persistSession: false });
        window.oasisClient.setToken(bearer, { avatarId: actor.id });
        const profile = { id: actor.id, username: 'owned-hydration-avatar' };
        if (cached !== null) profile.jwtToken = cached;
        localStorage.setItem('avatar', JSON.stringify(profile)); localStorage.setItem('loggedIn', 'true');
      }, { actor, bearer, cached, replaceClient });
    }
    async function begin(name, duplicate = false) {
      await page.evaluate(({ name, duplicate }) => {
        const profile = JSON.parse(localStorage.getItem('avatar'));
        const promise = window.hydrateAvatarProfile(profile);
        if (duplicate) window.sameHydrationPending = promise === window.hydrateAvatarProfile(profile);
        window.pendingHydrations[name] = promise.then(value => ({ ok: true, value }), error => ({ ok: false, message: error.message }))
          .then(result => { window.hydrationResults[name] = result; return result; });
      }, { name, duplicate });
      if (duplicate) assert.equal(await page.evaluate(() => window.sameHydrationPending), true);
    }
    async function finish(name, actor, karma) {
      await page.waitForFunction(name => Object.hasOwn(window.hydrationResults, name), name);
      const result = await page.evaluate(name => window.hydrationResults[name], name);
      assert.equal(result.ok, true);
      if (!actor) { assert.equal(result.value, null); return; }
      assert.equal(result.value.id, actor.id);
      assert.equal(result.value.karma ?? result.value.Karma, karma);
    }
    async function holdNext() {
      let release, pending = true;
      const gate = new Promise(resolve => { release = resolve; });
      const match = url => isRead(url);
      const handler = async route => { if (pending) { pending = false; await gate; } await route.continue(); };
      await page.route(match, handler);
      const owned = { release, dispose: () => page.unroute(match, handler) }; gates.push(owned); return owned;
    }
    async function held(name) {
      const gate = await holdNext();
      const observed = page.waitForRequest(request => isRead(new URL(request.url())));
      await begin(name, true); await observed; return gate;
    }
    phase = 'cached-expired-token-valid-sdk';
    await session(actors[0], actors[0].token, dashboardExpiredToken);
    await begin('initial'); await finish('initial', actors[0], 1000);

    phase = 'bearer-replacement';
    const bearerGate = await held('oldBearer');
    await session(actors[0], dashboardReplacementToken, dashboardExpiredToken);
    await begin('newBearer'); await finish('newBearer', actors[0], 1000);
    bearerGate.release(); await finish('oldBearer', null); await bearerGate.dispose();

    phase = 'client-replacement-same-bearer';
    const clientGate = await held('oldClient');
    await session(actors[0], dashboardReplacementToken, dashboardExpiredToken, true);
    await begin('newClient'); await finish('newClient', actors[0], 1000);
    clientGate.release(); await finish('oldClient', null); await clientGate.dispose();

    phase = 'account-change-missing-cached-token';
    const accountGate = await held('oldAccount');
    await session(actors[1], actors[1].token, null);
    await begin('second'); await finish('second', actors[1], 0);
    accountGate.release(); await finish('oldAccount', null); await accountGate.dispose();
    await session(actors[2], actors[2].token, null);
    await begin('third'); await finish('third', actors[2], 0);

    phase = 'expired-sdk-and-recovery';
    await session(actors[0], dashboardExpiredToken, actors[0].token);
    const refused = page.waitForResponse(response => isRead(new URL(response.url())) && response.status() === 401);
    await begin('expired'); await refused;
    await page.waitForFunction(() => Object.hasOwn(window.hydrationResults, 'expired'));
    assert.equal((await page.evaluate(() => window.hydrationResults.expired)).ok, false);
    await session(actors[0], actors[0].token, actors[0].token);
    await begin('recovered'); await finish('recovered', actors[0], 1000);
    assert.equal(reads.length, 10); assert.equal(await page.evaluate(() => window.profileUpdates.length), 6);
    assert.deepEqual(writes, []); assert.deepEqual(errors, []); assert.deepEqual(external, []);
    return { profileHydrationBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      canonicalSdkBearerVerified: true, clientReplacementVerified: true, duplicatePendingCoalesced: true,
      cachedAvatarJwtRequired: false, expiredSdkRefusedAndRecovered: true, profileReads: reads.length,
      businessWrites: 0, refreshEndpointAccepted: false, hostedAccepted: false, fullPortalAccepted: false };
  } catch (error) { console.error(JSON.stringify({ phase, reads: reads.length, pageErrors: errors })); throw error; }
  finally { gates.forEach(gate => gate.release()); await context.close(); }
}
module.exports = { verify };
