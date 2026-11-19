'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors }) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, acceptDownloads: false, serviceWorkers: 'block' });
  const endpoint = '/api/subscription/admin/plan-grant';
  const requests = [], errors = [], forbidden = [];
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
      if (new URL(request.url()).pathname === endpoint) requests.push(request);
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
      await Promise.all([response('/api/subscription/admin/subscriptions'), page.locator('[data-admin-tab="subscriptions"]').click()]);
      await page.waitForLoadState('networkidle');
    }
    const expiry = new Date(Date.now() + 2 * 86400000).toISOString().slice(0, 16);
    async function fill(userId, planId, reason) {
      await page.locator('#admin-grant-avatar').fill(userId);
      await page.locator('#admin-grant-plan').selectOption(planId);
      await page.locator('#admin-grant-expiry').fill(expiry);
      await page.locator('#admin-grant-reason').fill(reason);
    }
    async function submit(expected) {
      const [result] = await Promise.all([response(endpoint), page.locator('#admin-grant-submit').click()]);
      assert.equal(result.status(), expected);
      await page.waitForFunction(() => !document.getElementById('admin-grant-submit').disabled);
      return result;
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-admin').disabled);
    await open();
    phase = 'active-stripe-conflict';
    await fill(actors[1].id, 'enterprise', 'Owned browser conflict attempt');
    await submit(409);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /active Stripe subscription/);
    phase = 'grant-and-one-pending-write';
    await fill(actors[2].id, 'enterprise', 'Owned browser enterprise grant');
    let release, observe;
    const gate = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observe = resolve; });
    let hold = true;
    await page.route(url => url.pathname === endpoint, async route => {
      if (hold) { hold = false; observe(); await gate; }
      await route.continue();
    });
    const granted = response(endpoint);
    await page.locator('#admin-grant-submit').click(); await seen;
    assert.equal(await page.locator('#admin-grant-submit').isDisabled(), true);
    await page.evaluate(() => document.getElementById('admin-plan-grant-form').onsubmit({ preventDefault() {} }));
    assert.equal(requests.length, 2);
    release(); const result = await granted;
    assert.equal(result.status(), 200);
    const body = await result.json();
    assert.equal(body.isError, false); assert.equal(body.result.userId, actors[2].id);
    assert.equal(body.result.planId, 'enterprise');
    assert.equal(Date.parse(body.result.currentPeriodEnd), Date.parse(result.request().postDataJSON().expiresAtUtc));
    await page.waitForFunction(() => document.getElementById('subscription-admin-status').textContent.includes('Plan granted'));
    assert.equal(await page.locator('#admin-grant-reason').inputValue(), '');
    phase = 'delayed-authorized-grant-session-change';
    await fill(actors[0].id, 'gold', 'Owned browser delayed grant');
    let releaseLate, observeLate;
    const lateGate = new Promise(resolve => { releaseLate = resolve; });
    const lateSeen = new Promise(resolve => { observeLate = resolve; });
    let pause = true;
    await page.route(url => url.pathname === endpoint, async route => {
      if (pause) { pause = false; observeLate(); await lateGate; }
      await route.continue();
    });
    const lateResult = response(endpoint);
    await page.locator('#admin-grant-submit').click(); await lateSeen;
    await actor(1);
    const cleared = async () => {
      assert.equal(await page.locator('#subscription-admin-modal-block').evaluate(el => el.classList.contains('is-selected')), false);
      assert.equal(await page.locator('#admin-grant-avatar').inputValue(), '');
      assert.equal(await page.locator('#admin-grant-reason').inputValue(), '');
      assert.equal(await page.locator('#admin-grant-expiry').inputValue(), '');
      assert.equal(await page.locator('#subscription-admin-status').textContent(), '');
    };
    await cleared(); releaseLate(); assert.equal((await lateResult).status(), 200);
    await page.waitForLoadState('networkidle'); await cleared();
    phase = 'ordinary-user-denial';
    await open(); await fill(actors[2].id, 'gold', 'Owned browser unauthorized attempt');
    await submit(403);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /authorized account/);
    phase = 'expired-signed-token-denial';
    await actor(2); await open(); await fill(actors[2].id, 'gold', 'Owned browser expired attempt');
    await submit(401);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /Sign in again/);
    await page.evaluate(() => window.dispatchEvent(new CustomEvent('oasis-logout'))); await cleared();
    assert.equal(requests.length, 5); assert.deepEqual(errors, []); assert.deepEqual(forbidden, []);
    return { subscriptionAdminGrantBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      actualGrantPosts: 5, successfulGrants: 2, activeStripeConflict: true, ordinaryAndExpiredDenied: true,
      onePendingWrite: true, privateGrantInputsCleared: true, genuineStripeOrHostedWizardVerified: false };
  } catch (error) { console.error(JSON.stringify({ phase, pageErrors: errors, grantRequests: requests.length })); throw error; }
  finally { await context.close(); }
}
module.exports = { verify };
