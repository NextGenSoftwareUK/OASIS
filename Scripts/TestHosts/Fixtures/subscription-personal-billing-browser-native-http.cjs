'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors, personalControl }) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.match(personalControl, /^[0-9A-F]{64}$/);
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const pay = '/api/subscription/overages/pay-now';
  const statementsPath = '/api/subscription/overages/statements';
  const consent = '/api/subscription/toggle-pay-as-you-go';
  const writes = [], errors = [], forbidden = [];
  let phase = 'launch';
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      if (url.pathname.startsWith('/api/')) forbidden.push(url.origin + url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(actor => { window.fixtureSubscriptionActor = actor; }, actors[0]);
    const page = await context.newPage();
    page.setDefaultTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      if (new URL(request.url()).pathname === pay && request.method() === 'POST') writes.push(request.postDataJSON());
    });
    const response = (path, method = 'POST') => page.waitForResponse(value =>
      new URL(value.url()).pathname === path && value.request().method() === method);
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function open() {
      const [statements] = await Promise.all([response(statementsPath, 'GET'), page.locator('#fixture-open-subscription').click()]);
      assert.equal(statements.status(), 200);
      return statements.json();
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    const initial = await open();
    assert.equal(initial.length, 2);
    const pending = initial.find(value => value.state === 'pending').id;
    const leased = initial.find(value => value.state === 'retry').id;
    await page.locator('#sub-overage-disable').waitFor();
    phase = 'explicit-opt-out';
    const [disabled] = await Promise.all([response(consent), page.locator('#sub-overage-disable').click()]);
    assert.equal(disabled.status(), 200);
    assert.equal((await disabled.json()).success, true);
    await page.waitForFunction(() => !document.getElementById('sub-overage-disable'));
    phase = 'real-zero-due-recovery';
    const [paid] = await Promise.all([response(pay), page.locator('[data-statement="' + pending + '"]').click()]);
    assert.equal(paid.status(), 200);
    const paidBody = await paid.json();
    assert.equal(paidBody.noCharges, false);
    assert.equal(paidBody.statement.id, pending);
    assert.equal(paidBody.statement.state, 'paid');
    assert.equal(JSON.stringify(paidBody).includes('owned-personal-private'), false);
    await page.waitForFunction(id => !document.querySelector('[data-statement="' + id + '"]'), pending);
    phase = 'held-real-collection-lease';
    const [heldLease] = await Promise.all([response(pay), page.locator('[data-statement="' + leased + '"]').click()]);
    assert.equal(heldLease.status(), 200);
    assert.equal((await heldLease.json()).statement.state, 'retry');
    await page.waitForFunction(() => document.getElementById('subscription-status').textContent.includes('retry'));
    assert.match(await page.locator('#subscription-status').getAttribute('class'), /--warn/);
    await page.waitForLoadState('networkidle');
    phase = 'owned-account-fault-stable-request';
    async function control(stage) {
      const result = await page.request.post(baseUrl + '/__owned-personal/account/' + stage,
        { headers: { 'X-Owned-Personal-Control': personalControl } });
      assert.equal(result.status(), 200);
    }
    await control('block');
    const [refused] = await Promise.all([response(pay), page.locator('#sub-overage-pay').click()]);
    assert.equal(refused.status(), 400);
    await page.waitForFunction(() => document.getElementById('subscription-status').className.includes('--error'));
    const originalRequest = writes.at(-1);
    assert.match(originalRequest.billingRequestId, /^[0-9a-f-]{36}$/);
    await control('restore');
    const [empty] = await Promise.all([response(pay), page.locator('#sub-overage-pay').click()]);
    assert.equal(empty.status(), 200);
    assert.equal((await empty.json()).noCharges, true);
    assert.deepEqual(writes.at(-1), originalRequest);
    await page.waitForFunction(() => document.getElementById('subscription-status').textContent.includes('no uninvoiced'));
    assert.match(await page.locator('#subscription-status').getAttribute('class'), /--info/);
    await page.waitForLoadState('networkidle');
    phase = 'one-writer-late-session';
    let release, observe, pause = true;
    const gate = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observe = resolve; });
    await page.route(url => url.origin === base.origin && url.pathname === pay, async route => {
      if (pause && route.request().method() === 'POST') { pause = false; observe(); await gate; }
      await route.continue();
    });
    const count = writes.length;
    const dispatched = page.waitForRequest(value => new URL(value.url()).pathname === pay && value.method() === 'POST');
    await page.locator('#sub-overage-pay').click();
    await dispatched;
    await seen;
    assert.equal(await page.locator('#sub-overage-pay').isDisabled(), true);
    await page.evaluate(() => document.getElementById('sub-overage-pay').onclick());
    assert.equal(writes.length, count + 1);
    assert.notEqual(writes.at(-1).billingRequestId, originalRequest.billingRequestId);
    await actor(1);
    const complete = response(pay);
    release();
    assert.equal((await complete).status(), 200);
    await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#sub-usage-panel').textContent(), '');
    await page.locator('#subscription-close-btn').click();
    const other = await open();
    assert.equal(other.length, 1);
    assert.notEqual(other[0].id, pending);
    await page.locator('#subscription-close-btn').click();
    await actor(2);
    assert.deepEqual(await open(), []);
    assert.deepEqual(errors, []);
    assert.deepEqual(forbidden, []);
    return { personalBillingBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      canonicalOptOutVerified: true, originalZeroInvoicePaid: true, actualHeldLeaseShowsPending: true,
      noGenericPaymentSuccess: true, sameRequestAfterActualAccountFault: true,
      terminalEmptyClearsRequestIdentity: true, subscriberPrivateFieldsRedacted: true,
      onePendingWriter: true, stripeCollectionVerified: false, originalAccountsAndStatements: 'Owned INPUT fixtures',
      nonzeroPaymentVerified: false, productionStartupVerified: false };
  } catch (error) {
    console.error(JSON.stringify({ phase, pageErrors: errors, paymentRequests: writes.length }));
    throw error;
  } finally { await context.close(); }
}
module.exports = { verify };
