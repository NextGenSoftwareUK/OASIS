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
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const billingPath = '/api/subscription/app-owner/owned-app-a/payg';
  const payPath = billingPath + '/pay-now';
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
    const page = await context.newPage();
    page.setDefaultTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      if (new URL(request.url()).pathname === payPath && request.method() === 'POST') writes.push(request.postDataJSON());
    });
    const response = pathname => page.waitForResponse(value => new URL(value.url()).pathname === pathname);
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function popup(appId) {
      const [apps] = await Promise.all([response('/api/subscription/app-owner/apps'),
        page.locator('#fixture-open-subscription').click()]);
      assert.equal(apps.status(), 200);
      if (!appId) { assert.deepEqual((await apps.json()).result, []); return; }
      await page.locator('.uo-open[data-app="' + appId + '"]').click();
      await page.locator('#ap-pay-now').waitFor();
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    await popup('owned-app-a');
    phase = 'uninvoiced-empty';
    const initialInvoice = await page.locator('.ap-recover').getAttribute('data-statement');
    assert.match(initialInvoice, /^ovga_[0-9a-f]{64}$/);
    const [empty] = await Promise.all([response(payPath), page.locator('#ap-pay-now').click()]);
    assert.equal(empty.status(), 200);
    assert.equal((await empty.json()).result.noCharges, true);
    await page.waitForFunction(() => document.getElementById('ap-payment-msg').textContent.startsWith('No uninvoiced app charges'));
    assert.match(writes[0].billingRequestId, /^[0-9a-f-]{36}$/);
    assert.equal(writes[0].month, new Date().toISOString().slice(0, 7));
    assert.equal(writes[0].statementId, undefined);
    phase = 'existing-zero-invoice';
    const [recovered] = await Promise.all([response(payPath), page.locator('.ap-recover').click()]);
    assert.equal(recovered.status(), 200);
    const recoveredBody = (await recovered.json()).result;
    assert.equal(recoveredBody.statement.id, initialInvoice);
    assert.equal(recoveredBody.statement.state, 'paid');
    assert.equal(recoveredBody.statement.amountUsd, 0);
    assert.equal(JSON.stringify(recoveredBody).includes('owned-private'), false);
    assert.deepEqual(writes[1], { statementId: initialInvoice });
    await page.waitForFunction(() => document.getElementById('ap-payment-msg').textContent.includes('paid'));
    assert.equal(await page.locator('.ap-recover').count(), 0);
    phase = 'foreign-and-no-owner';
    await page.locator('#subscription-close-btn').click();
    for (const index of [1, 2]) {
      await actor(index);
      const code = await page.evaluate(async ({ endpoint, id }) =>
        (await window.oasisClient.http.post(endpoint, { body: { statementId: id } })).statusCode,
      { endpoint: payPath, id: initialInvoice });
      assert.equal(code, 403);
    }
    await popup(null);
    await page.locator('#subscription-close-btn').click();
    await actor(0);
    await popup('owned-app-a');
    phase = 'one-writer-and-stale-session';
    let release, observe, pause = true;
    const held = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observe = resolve; });
    await page.route(url => url.origin === base.origin && url.pathname === payPath, async route => {
      if (pause && route.request().method() === 'POST') { pause = false; observe(); await held; }
      await route.continue();
    });
    const count = writes.length;
    await page.locator('#ap-pay-now').click();
    await seen;
    assert.equal(await page.locator('#ap-on').isDisabled(), true);
    assert.equal(await page.locator('#ap-billing-month').isDisabled(), true);
    await page.evaluate(() => document.getElementById('ap-pay-now').onclick());
    assert.equal(writes.length, count + 1);
    await actor(1);
    const completed = response(payPath);
    release();
    assert.equal((await completed).status(), 200);
    await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#sub-usage-panel').textContent(), '');
    await page.locator('#subscription-close-btn').click();
    await popup('owned-app-b');
    assert.equal(await page.locator('.ap-recover').count(), 1);
    assert.deepEqual(errors, []);
    assert.deepEqual(forbidden, []);
    return { appPayNowBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      uninvoicedEmptyVerified: true, existingZeroStatementCollected: true, currentMonthVerified: true,
      singlePendingWriterVerified: true, privateIdsRedacted: true, foreignOwnerRefused: true,
      fixtureScope: 'Owned persisted statement INPUT fixtures only', stripeCollectionVerified: false,
      freshOperationAllocationVerified: false, nonzeroPaymentVerified: false, productionStartupVerified: false };
  } catch (error) {
    console.error(JSON.stringify({ phase, pageErrors: errors, paymentRequests: writes.length }));
    throw error;
  } finally { await context.close(); }
}
module.exports = { verify };
