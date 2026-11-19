'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors, adminAccountsControl }) {
  const base = new URL(baseUrl); assert.equal(base.hostname, '127.0.0.1');
  assert.equal(typeof adminAccountsControl, 'string');
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, acceptDownloads: false, serviceWorkers: 'block' });
  const endpoint = '/api/subscription/admin/app-accounts', appId = 'owned-admin-browser';
  const rotatePath = endpoint + '/' + appId + '/rotate-key', updatePath = endpoint + '/' + appId;
  const errors = [], forbidden = [], writes = [];
  let phase = 'launch';
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      if (url.pathname.startsWith('/api/')) forbidden.push(url.origin + url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(actor => { window.fixtureSubscriptionActor = actor; }, actors[0]);
    const page = await context.newPage(); page.setDefaultTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.pathname.startsWith(endpoint) && request.method() === 'POST') writes.push(url.pathname);
    });
    const response = (pathname, method = 'GET') => page.waitForResponse(value =>
      new URL(value.url()).pathname === pathname && value.request().method() === method);
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        localStorage.setItem('avatar', JSON.stringify({ id: value.id }));
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function open() {
      await Promise.all([response('/api/subscription/admin/analytics'), page.locator('#fixture-open-admin').click()]);
      await Promise.all([response(endpoint), page.locator('[data-admin-tab="appaccounts"]').click()]);
      await page.waitForLoadState('networkidle');
    }
    async function form(mode, reason, state) {
      await page.locator('#admin-account-action').selectOption(mode);
      await page.locator('#admin-account-id').fill(appId);
      if (mode === 'create') await page.locator('#admin-account-name').fill('Owned browser app');
      if (state) await page.locator('#admin-account-state').selectOption(state);
      await page.locator('#admin-account-reason').fill(reason);
    }
    async function submit(pathname, expected) {
      const [result] = await Promise.all([response(pathname, 'POST'), page.locator('#admin-account-submit').click()]);
      assert.equal(result.status(), expected);
      await page.waitForFunction(() => !document.getElementById('admin-account-submit').disabled);
      return result;
    }
    async function keyCleared() {
      assert.equal(await page.locator('#admin-account-key').inputValue(), '');
      assert.equal(await page.locator('#admin-account-key-app').textContent(), '');
      assert.equal(await page.locator('#admin-account-secret').isHidden(), true);
    }
    async function credential(key, accepted) {
      // Observes the real repository reader through a nonce-guarded test-assembly endpoint, not a substitute business API.
      const actual = await page.evaluate(async input => {
        const result = await fetch('/__owned-app-credential', { method: 'POST',
          headers: { 'Content-Type': 'application/json', 'X-Owned-App-Control': input.control },
          body: JSON.stringify({ appId: input.appId, apiKey: input.key }) });
        if (!result.ok) throw new Error('Owned credential observation failed.');
        return (await result.json()).accepted;
      }, { appId, key, control: adminAccountsControl });
      assert.equal(actual, accepted);
    }
    async function storagePrivate(keys) {
      assert.equal(await page.evaluate(values => {
        const stored = JSON.stringify([Object.entries(localStorage), Object.entries(sessionStorage)]);
        return values.some(value => stored.includes(value));
      }, keys), false);
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-admin').disabled);
    await open();
    assert.equal(await page.locator('#admin-appaccounts tbody tr').count(), 0);
    phase = 'create-once-through-real-form';
    await form('create', 'Owned browser create');
    let release, observe;
    const gate = new Promise(resolve => { release = resolve; }), seen = new Promise(resolve => { observe = resolve; });
    let pauseCreate = true;
    await page.route(url => url.pathname === endpoint, async route => {
      if (pauseCreate && route.request().method() === 'POST') { pauseCreate = false; observe(); await gate; }
      await route.continue();
    });
    const created = response(endpoint, 'POST'); await page.locator('#admin-account-submit').click(); await seen;
    assert.equal(await page.locator('#admin-account-submit').isDisabled(), true);
    await page.evaluate(() => document.getElementById('admin-account-form').onsubmit({ preventDefault() {} }));
    assert.equal(writes.length, 1); release();
    assert.equal((await created).status(), 200);
    await page.waitForFunction(() => !document.getElementById('admin-account-secret').hidden);
    const key1 = await page.locator('#admin-account-key').inputValue();
    assert.equal(key1.startsWith('oak_'), true);
    assert.equal(await page.locator('#admin-account-key-app').textContent(), appId);
    await credential(key1, true); await storagePrivate([key1]);
    await page.locator('#admin-account-key-dismiss').click(); await keyCleared();
    phase = 'canonical-duplicate-create-conflict';
    await form('create', 'Owned browser duplicate create'); await submit(endpoint, 409); await keyCleared();
    phase = 'rotate-real-key-and-reject-old-key';
    await form('rotate', 'Owned browser rotate'); const rotation = await submit(rotatePath, 200);
    const rotated = (await rotation.json()).result;
    assert.equal(rotated.account.keyHashSha256 == null, true);
    const key2 = rotated.apiKey; assert.equal(key2 === key1, false);
    await page.waitForFunction(() => !document.getElementById('admin-account-secret').hidden);
    assert.equal((await page.locator('#admin-account-key').inputValue()) === key2, true);
    await credential(key1, false); await credential(key2, true);
    phase = 'finite-plan-pool-invariant-and-real-settings-update';
    await form('update', 'Owned browser invalid finite plan');
    await page.locator('#admin-account-plan').selectOption('silver');
    await page.locator('#admin-account-pooled').selectOption('false');
    await submit(updatePath, 400);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /pooled quota/);
    await form('update', 'Owned browser pooled plan');
    await page.locator('#admin-account-plan').selectOption('silver');
    await page.locator('#admin-account-pooled').selectOption('true');
    await page.locator('#admin-account-user-plan').selectOption('bronze');
    const updated = (await (await submit(updatePath, 200)).json()).result;
    assert.equal(updated.planId, 'silver'); assert.equal(updated.pooledQuota, true); assert.equal(updated.perUserPlanId, 'bronze');
    phase = 'disable-and-reactivate';
    await form('update', 'Owned browser disable', 'disabled'); await keyCleared();
    await submit(updatePath, 200); await credential(key2, false);
    await form('update', 'Owned browser reactivate', 'active'); await submit(updatePath, 200); await credential(key2, true);
    phase = 'delayed-rotation-after-tab-return-does-not-redisclose';
    await form('rotate', 'Owned browser delayed rotation');
    let releaseRotation, observeRotation;
    const rotationGate = new Promise(resolve => { releaseRotation = resolve; });
    const rotationSeen = new Promise(resolve => { observeRotation = resolve; });
    let pauseRotation = true;
    await page.route(url => url.pathname === rotatePath, async route => {
      if (pauseRotation) { pauseRotation = false; observeRotation(); await rotationGate; }
      await route.continue();
    });
    const lateRotation = response(rotatePath, 'POST'); await page.locator('#admin-account-submit').click(); await rotationSeen;
    await page.locator('[data-admin-tab="analytics"]').click();
    await Promise.all([response(endpoint), page.locator('[data-admin-tab="appaccounts"]').click()]);
    releaseRotation(); const completedRotation = await lateRotation; assert.equal(completedRotation.status(), 200);
    const key3 = (await completedRotation.json()).result.apiKey;
    assert.equal(typeof key3 === 'string' && key3.startsWith('oak_') && key3 !== key2, true);
    await page.waitForLoadState('networkidle'); await keyCleared();
    await credential(key2, false); await credential(key3, true); await storagePrivate([key1, key2, key3]);
    phase = 'actual-read-and-account-change';
    let releaseRead, observeRead;
    const readGate = new Promise(resolve => { releaseRead = resolve; });
    const readSeen = new Promise(resolve => { observeRead = resolve; }); let pauseRead = true;
    await page.route(url => url.pathname === endpoint, async route => {
      if (pauseRead && route.request().method() === 'GET') { pauseRead = false; observeRead(); await readGate; }
      await route.continue();
    });
    const lateRead = response(endpoint); await page.locator('#admin-accounts-refresh').click(); await readSeen;
    await actor(1); releaseRead(); assert.equal((await lateRead).status(), 200);
    await page.waitForLoadState('networkidle'); await keyCleared();
    assert.equal(await page.locator('#admin-appaccounts').textContent(), '');
    assert.equal(await page.locator('#admin-account-id').inputValue(), '');
    assert.equal(await page.locator('#subscription-admin-status').textContent(), '');
    phase = 'ordinary-and-expired-admin-denials';
    await open(); await form('create', 'Owned browser ordinary refusal'); await submit(endpoint, 403);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /authorized account/); await keyCleared();
    await actor(2); await open(); await form('create', 'Owned browser expired refusal'); await submit(endpoint, 401);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /Sign in again/); await keyCleared();
    await page.evaluate(() => window.dispatchEvent(new CustomEvent('oasis-logout'))); await keyCleared();
    assert.equal(writes.length, 10); assert.deepEqual(errors, []); assert.deepEqual(forbidden, []);
    return { subscriptionAdminAccountsBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      administratorWrites: 10, successfulAuditTransitions: 6, actualRepositoryCredentialReads: 7,
      finitePlanWithoutPoolRejected: true, pooledPlanAndPerUserSettingsVerified: true,
      duplicateCreateConflict: true, rotationRejectsOldKeys: true, disabledCredentialRejected: true,
      reactivatedCredentialAccepted: true, onePendingWriter: true, lateKeyRedisclosurePrevented: true,
      keysAbsentFromStorageAndPublicEvidence: true, genuineHostedWizardVerified: false,
      shippingUsageAdmissionWithAppKeyVerified: false, stripeVerified: false };
  } catch (error) { console.error(JSON.stringify({ phase, pageErrors: errors, administratorWrites: writes.length })); throw error; }
  finally { await context.close(); }
}
module.exports = { verify };
