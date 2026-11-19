'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors, outboxOperations }) {
  const base = new URL(baseUrl); assert.equal(base.hostname, '127.0.0.1');
  const services = ['WEB5', 'WEB6', 'WEB7', 'WEB8', 'WEB9', 'WEB10'];
  assert.deepEqual(Object.keys(outboxOperations), services);
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, acceptDownloads: false, serviceWorkers: 'block' });
  const list = '/api/subscription/admin/outboxes', requeue = list + '/requeue';
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
      if (new URL(request.url()).pathname === requeue) writes.push(request.postDataJSON());
    });
    const response = pathname => page.waitForResponse(value => new URL(value.url()).pathname === pathname);
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        localStorage.setItem('avatar', JSON.stringify({ id: value.id }));
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function open() {
      await Promise.all([response('/api/subscription/admin/analytics'), page.locator('#fixture-open-admin').click()]);
      const [loaded] = await Promise.all([response(list), page.locator('[data-admin-tab="outboxes"]').click()]);
      await page.waitForLoadState('networkidle'); return loaded;
    }
    async function post(body, expected) {
      const status = await page.evaluate(async input => {
        const result = await fetch('/api/subscription/admin/outboxes/requeue', { method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: 'Bearer ' + window.oasisClient.http.tokenStore.getToken() },
          body: JSON.stringify(input) });
        return result.status;
      }, body);
      assert.equal(status, expected);
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-admin').disabled);
    phase = 'real-six-service-shared-database-list';
    const initial = await open(); assert.equal(initial.status(), 200);
    const groups = (await initial.json()).result;
    for (const service of services) {
      assert.equal(groups[service].length, 1);
      assert.equal(groups[service][0].service, service); assert.equal(groups[service][0].id, outboxOperations[service]);
    }
    await page.waitForFunction(() => document.querySelectorAll('#admin-outboxes tbody tr').length === 6);
    const button = '.admin-requeue[data-service="WEB5"]';
    const originalButton = await page.locator(button).elementHandle();
    phase = 'one-pending-requeue-across-real-refresh';
    let release, observe;
    const gate = new Promise(resolve => { release = resolve; }), seen = new Promise(resolve => { observe = resolve; });
    let pause = true;
    await page.route(url => url.pathname === requeue, async route => {
      if (pause) { pause = false; observe(); await gate; }
      await route.continue();
    });
    page.once('dialog', dialog => dialog.accept('Owned browser requeue after service review'));
    const requeued = response(requeue); await page.locator(button).click(); await seen;
    assert.equal(await page.locator(button).isDisabled(), true);
    await page.evaluate(selector => document.getElementById('admin-outboxes').onclick({ target: document.querySelector(selector) }), button);
    assert.equal(writes.length, 1);
    await Promise.all([response(list), page.locator('#admin-outbox-refresh').click()]);
    await page.waitForFunction(element => !element.isConnected, originalButton);
    await page.waitForFunction(selector => document.querySelector(selector)?.disabled, button);
    await page.evaluate(selector => document.getElementById('admin-outboxes').onclick({ target: document.querySelector(selector) }), button);
    assert.equal(writes.length, 1);
    release(); assert.equal((await requeued).status(), 200);
    await page.waitForFunction(() => document.querySelector('.admin-requeue[data-service="WEB5"]') === null);
    await page.waitForFunction(() => document.getElementById('subscription-admin-status').textContent.includes('settlement requeued'));
    assert.match(await page.locator('#subscription-admin-status').textContent(), /settlement requeued/);
    assert.equal(writes[0].service, 'WEB5'); assert.equal(writes[0].operationId, outboxOperations.WEB5);
    phase = 'native-action-replay-and-wrong-service-denial';
    await post(writes[0], 200);
    await post({ ...writes[0], reason: 'Owned changed replay intent' }, 409);
    await post({ ...writes[0], service: 'WEB6' }, 409);
    phase = 'cancel-is-not-a-write';
    const prior = writes.length;
    page.once('dialog', dialog => dialog.dismiss());
    await page.locator('.admin-requeue[data-service="WEB6"]').click();
    await page.waitForFunction(() => !document.querySelector('.admin-requeue[data-service="WEB6"]').disabled);
    assert.equal(writes.length, prior);
    phase = 'late-real-list-after-account-change';
    let releaseRead, observeRead;
    const readGate = new Promise(resolve => { releaseRead = resolve; });
    const readSeen = new Promise(resolve => { observeRead = resolve; }); let pauseRead = true;
    await page.route(url => url.pathname === list, async route => {
      if (pauseRead) { pauseRead = false; observeRead(); await readGate; }
      await route.continue();
    });
    const delayed = response(list); await page.locator('#admin-outbox-refresh').click(); await readSeen;
    await actor(1); releaseRead(); assert.equal((await delayed).status(), 200);
    await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#admin-outboxes').textContent(), '');
    assert.equal(await page.locator('#subscription-admin-status').textContent(), '');
    phase = 'ordinary-and-expired-denials';
    assert.equal((await open()).status(), 403); await post({ ...writes[0], actionId: randomUUID() }, 403);
    await actor(2); assert.equal((await open()).status(), 401); await post({ ...writes[0], actionId: randomUUID() }, 401);
    await page.evaluate(() => window.dispatchEvent(new CustomEvent('oasis-logout')));
    assert.equal(await page.locator('#admin-outboxes').textContent(), '');
    assert.equal(writes.length, 6); assert.deepEqual(errors, []); assert.deepEqual(forbidden, []);
    return { subscriptionAdminOutboxBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      serviceGroupsVerified: 6, sharedDatabaseServiceIsolation: true, onePendingRequeueAcrossRefresh: true,
      duplicateSubmissionsPrevented: true, exactActionReplayAcknowledged: true, changedReplayAndWrongServiceDenied: true,
      cancelWrites: 0, requeuePostAttempts: 6, ordinaryAndExpiredDenied: true, noProviderExecution: true,
      genuineHostedWizardOrSharedWorkerVerified: false, stripeVerified: false };
  } catch (error) { console.error(JSON.stringify({ phase, pageErrors: errors, requeuePosts: writes.length })); throw error; }
  finally { await context.close(); }
}
module.exports = { verify };
