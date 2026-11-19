'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors }) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.equal(base.protocol, 'http:');
  assert.equal(actors.length, 3);
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const profile = path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT, 'browser-profile-' + randomUUID());
  const context = await chromium.launchPersistentContext(profile, {
    channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce',
    acceptDownloads: false, serviceWorkers: 'block'
  });
  try {
    const forbidden = [];
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      if (url.pathname.startsWith('/api/')) forbidden.push(url.origin + url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(actor => { window.fixtureSubscriptionActor = actor; }, actors[0]);
    const page = await context.newPage();
    const errors = [];
    const writes = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      if (new URL(request.url()).pathname.startsWith('/api/') && request.method() !== 'GET') writes.push(request.method());
    });
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.locator('#fixture-open-subscription').waitFor({ state: 'visible' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    const timings = [];
    const response = endpoint => page.waitForResponse(value => new URL(value.url()).pathname === endpoint);
    const subscriptionPath = '/api/subscription/subscriptions/me';
    const ordersPath = '/api/subscription/orders/me';
    for (let i = 0; i < actors.length; i++) {
      if (i) await page.evaluate(actor => {
        window.oasisClient.setToken(actor.token, { avatarId: actor.id });
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[i]);
      const opening = response(subscriptionPath);
      const started = Date.now();
      await page.locator('#fixture-open-subscription').click();
      const loaded = await opening;
      assert.equal(loaded.status(), 200);
      const subscriptions = (await loaded.json()).result;
      if (i < 2) {
        assert.equal(subscriptions.length, 1);
        assert.equal(subscriptions[0].planId, i ? 'silver' : 'bronze');
        await page.waitForFunction(name => document.getElementById('sub-stat-plan').textContent === name, subscriptions[0].planName);
        assert.match(await page.locator('#sub-mysubs-list').innerText(), i ? /Renews:/ : /Ends:/);
      } else {
        assert.equal(subscriptions.length, 0);
        await page.waitForFunction(() => document.getElementById('sub-mysubs-list').textContent.includes('No active subscriptions'));
        assert.equal(await page.locator('#sub-stat-plan').innerText(), 'No active plan');
      }
      const openMs = Date.now() - started;
      const ordersRead = response(ordersPath);
      const ordersStarted = Date.now();
      await page.locator('#subscription-modal-block .map-tab[data-tab="orders"]').click();
      const ordersResponse = await ordersRead;
      assert.equal(ordersResponse.status(), 200);
      const orders = (await ordersResponse.json()).result;
      if (i < 2) {
        assert.equal(orders.length, 1);
        assert.equal(orders[0].userId, actors[i].id);
        await page.waitForFunction(amount => document.getElementById('sub-orders-list').textContent.includes('GBP ' + amount + ' paid'), i ? 29 : 9);
      } else {
        assert.equal(orders.length, 0);
        await page.waitForFunction(() => document.getElementById('sub-orders-list').textContent.includes('No orders found'));
      }
      timings.push({ openMs, ordersMs: Date.now() - ordersStarted });
      await page.locator('#subscription-close-btn').click();
    }
    await page.evaluate(actor => {
      window.oasisClient.setToken(actor.token, { avatarId: actor.id });
      window.dispatchEvent(new CustomEvent('oasis-login'));
    }, actors[0]);
    let release;
    let observed;
    const held = new Promise(resolve => { release = resolve; });
    const observedRequest = new Promise(resolve => { observed = resolve; });
    let pause = true;
    await page.route(url => url.origin === base.origin && url.pathname === subscriptionPath, async route => {
      if (pause) { pause = false; observed(); await held; }
      await route.continue();
    });
    const lateResponse = response(subscriptionPath);
    await page.locator('#fixture-open-subscription').click();
    await observedRequest;
    await page.evaluate(actor => {
      window.oasisClient.setToken(actor.token, { avatarId: actor.id });
      window.dispatchEvent(new CustomEvent('oasis-login'));
    }, actors[1]);
    release();
    const delayed = await lateResponse;
    assert.equal(delayed.status(), 200);
    await delayed.finished();
    await page.waitForLoadState('networkidle');
    await page.waitForFunction(() => document.getElementById('sub-mysubs-list').textContent === '');
    assert.equal(await page.locator('#sub-stat-plan').innerText(), '—');
    await page.evaluate(() => {
      window.oasisClient.setToken(null);
      window.dispatchEvent(new CustomEvent('oasis-logout'));
    });
    assert.equal(await page.locator('#sub-mcp-config-pre').textContent(), '');
    assert.equal(await page.locator('#sub-mcp-copy-btn').isDisabled(), true);
    assert.deepEqual(forbidden, []);
    assert.deepEqual(errors, []);
    assert.deepEqual(writes, [], 'Reading subscriptions and Orders must not write billing state.');
    return { subscriptionBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      timings, billingWrites: writes.length, productionStartupVerified: false, stripeCheckoutVerified: false };
  } finally { await context.close(); }
}
module.exports = { verify };
