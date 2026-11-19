'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors }) {
  const base = new URL(baseUrl); assert.equal(base.hostname, '127.0.0.1'); assert.equal(actors.length, 3);
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const ownerPath = '/api/subscription/admin/app-accounts/owned-app-a/owners';
  const errors = [], external = [], posts = [];
  let phase = 'launch';
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      if (url.pathname.startsWith('/api/')) external.push(url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(actor => { window.fixtureSubscriptionActor = actor; }, actors[0]);
    const page = await context.newPage(); page.setDefaultTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      const pathname = new URL(request.url()).pathname;
      if (pathname.startsWith('/api/subscription/admin/app-accounts/') && pathname.endsWith('/owners') && request.method() === 'POST') posts.push(pathname);
    });
    const response = (pathname, method = 'GET') => page.waitForResponse(value =>
      new URL(value.url()).pathname === pathname && value.request().method() === method)
      .then(value => ({ value }), error => ({ error }));
    async function take(observation) { const result = await observation; if (result.error) throw result.error; return result.value; }
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        localStorage.setItem('avatar', JSON.stringify({ id: value.id }));
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function open(expected = 200) {
      const read = response('/api/subscription/admin/analytics');
      await page.locator('#fixture-open-admin').click(); assert.equal((await take(read)).status(), expected);
      await page.locator('[data-admin-tab="appusage"]').click();
    }
    async function form(ids, reason) {
      await page.locator('#admin-app-id').fill('owned-app-a');
      await page.locator('#admin-app-owners').fill(ids);
      await page.locator('#admin-app-owners-reason').fill(reason);
    }
    async function idle() { await page.waitForFunction(() => !document.getElementById('admin-app-owners-save').disabled); }
    async function submit(expected) {
      const pending = response(ownerPath, 'POST'); await page.locator('#admin-app-owners-save').click();
      const result = await take(pending); assert.equal(result.status(), expected); await idle(); return result;
    }
    async function hold() {
      let release, pending = true; const gate = new Promise(resolve => { release = resolve; });
      await page.route(url => url.pathname === ownerPath, async route => {
        if (route.request().method() === 'POST' && pending) { pending = false; await gate; }
        await route.continue();
      });
      return { release };
    }
    async function actual(pathname, body) {
      return page.evaluate(async value => {
        const result = await fetch(value.pathname, { method: value.body === undefined ? 'GET' : 'POST',
          headers: { Authorization: 'Bearer ' + window.oasisClient.http.tokenStore.getToken(), 'Content-Type': 'application/json' },
          body: value.body === undefined ? undefined : JSON.stringify(value.body) });
        const text = await result.text();
        return { status: result.status, body: text ? JSON.parse(text) : null };
      }, { pathname, body });
    }
    async function ownedApps(expected) {
      const result = await page.evaluate(() => window.oasisClient.http.get('/api/subscription/app-owner/apps'));
      assert.equal(result.isError, false); assert.equal(Array.isArray(result.result), true);
      assert.deepEqual(result.result.map(app => app.appId).sort(), expected);
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-admin').disabled);
    phase = 'client-validation'; await open();
    await form('not-an-avatar', 'Owned browser invalid owner input');
    await page.locator('#admin-app-owners-save').click();
    assert.match(await page.locator('#subscription-admin-status').textContent(), /UUID/); assert.equal(posts.length, 0);
    await form(actors[1].id, 'bad'); await page.locator('#admin-app-owners-save').click();
    assert.match(await page.locator('#subscription-admin-status').textContent(), /reason/); assert.equal(posts.length, 0);
    phase = 'one-pending-owner-assignment';
    await form(actors[1].id.toUpperCase() + ', ' + actors[1].id, 'Owned browser owner assignment');
    const gate = await hold(), assigned = response(ownerPath, 'POST');
    await Promise.all([page.waitForRequest(value => new URL(value.url()).pathname === ownerPath), page.locator('#admin-app-owners-save').click()]);
    assert.equal(await page.locator('#admin-app-owners-save').isDisabled(), true);
    await page.evaluate(() => { document.getElementById('admin-app-owners-save').onclick(); });
    await page.locator('[data-admin-tab="analytics"]').click(); await page.locator('[data-admin-tab="appusage"]').click();
    assert.equal(await page.locator('#admin-app-owners-save').isDisabled(), true); assert.equal(posts.length, 1);
    await form(actors[0].id, 'Owned newer unsaved owner reason');
    gate.release(); const saved = await take(assigned); assert.equal(saved.status(), 200); await idle();
    const assignedAccount = (await saved.json()).result;
    assert.deepEqual(assignedAccount.ownerAvatarIds, [actors[1].id]);
    assert.equal(assignedAccount.apiKey == null && assignedAccount.keyHashSha256 == null, true);
    assert.equal(await page.locator('#admin-app-owners').inputValue(), actors[0].id);
    assert.equal(await page.locator('#admin-app-owners-reason').inputValue(), 'Owned newer unsaved owner reason');
    phase = 'canonical-identities-and-server-refusals';
    const canonical = await actual(ownerPath, { ownerAvatarIds: [actors[1].id, actors[1].id.replaceAll('-', '').toUpperCase()], reason: 'Owned browser canonical duplicate assignment' });
    assert.equal(canonical.status, 200); assert.deepEqual(canonical.body.result.ownerAvatarIds, [actors[1].id]);
    assert.equal((await actual(ownerPath, { reason: 'Owned browser omitted owner array' })).status, 400);
    assert.equal((await actual(ownerPath, { ownerAvatarIds: null, reason: 'Owned browser null owner array' })).status, 400);
    assert.equal((await actual(ownerPath, { ownerAvatarIds: [null], reason: 'Owned browser null owner element' })).status, 400);
    assert.equal((await actual(ownerPath, { ownerAvatarIds: [' '], reason: 'Owned browser blank owner element' })).status, 400);
    assert.equal((await actual(ownerPath, { ownerAvatarIds: ['invalid'], reason: 'Owned browser invalid owner array' })).status, 400);
    assert.equal((await actual(ownerPath, { ownerAvatarIds: Array.from({ length: 11 }, () => randomUUID()), reason: 'Owned browser excessive owners' })).status, 400);
    assert.equal((await actual(ownerPath.replace('owned-app-a', 'owned-admin-missing'), { ownerAvatarIds: [], reason: 'Owned browser missing app' })).status, 404);
    phase = 'granted-owner-access-and-ordinary-admin-refusal';
    await actor(1); await ownedApps(['owned-app-a', 'owned-app-b']);
    assert.equal((await actual('/api/subscription/app-owner/owned-app-a/settings')).status, 200);
    await open(403); await form(actors[0].id, 'Owned browser ordinary admin refusal'); await submit(403);
    phase = 'explicit-removal-and-revoked-owner-access';
    await actor(0); await open(); await form('', 'Owned browser owner removal');
    const removed = await submit(200); assert.deepEqual((await removed.json()).result.ownerAvatarIds, []);
    await actor(1); await ownedApps(['owned-app-b']);
    assert.equal((await actual('/api/subscription/app-owner/owned-app-a/settings')).status, 403);
    phase = 'expired-admin-refusal';
    await actor(2); await open(401); await form(actors[1].id, 'Owned browser expired admin refusal'); await submit(401);
    phase = 'original-authorized-write-after-account-change';
    await actor(0); await open(); await form(actors[1].id, 'Owned browser late owner assignment');
    const lateGate = await hold(), late = response(ownerPath, 'POST');
    await Promise.all([page.waitForRequest(value => new URL(value.url()).pathname === ownerPath), page.locator('#admin-app-owners-save').click()]);
    await actor(1); lateGate.release(); const lateResponse = await take(late); assert.equal(lateResponse.status(), 200);
    await lateResponse.finished(); await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#subscription-admin-status').textContent(), '');
    assert.equal(await page.locator('#admin-app-owners').inputValue(), '');
    assert.equal(await page.locator('#admin-app-owners-reason').inputValue(), '');
    await ownedApps(['owned-app-a', 'owned-app-b']);
    assert.equal(posts.length, 13); assert.deepEqual(errors, []); assert.deepEqual(external, []);
    return { subscriptionAdminOwnersBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      ownerPosts: posts.length, committedTransitions: 4, canonicalOwnerUniqueness: true, singlePendingOwnerAction: true,
      editedDraftPreserved: true, actualSdkOwnershipGrantAndRevoke: true, ordinary403AndExpired401: true,
      hostedOrStripeAccepted: false, fullPortalAccepted: false };
  } catch (error) { console.error(JSON.stringify({ phase, ownerPosts: posts.length, pageErrors: errors })); throw error; }
  finally { await context.close(); }
}
module.exports = { verify };
