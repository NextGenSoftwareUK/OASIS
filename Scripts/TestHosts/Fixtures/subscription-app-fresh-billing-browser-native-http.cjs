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
  const endpoint = '/api/subscription/app-owner/owned-app-a/payg/pay-now';
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
      if (new URL(request.url()).pathname === endpoint && request.method() === 'POST') writes.push(request.postDataJSON());
    });
    const response = path => page.waitForResponse(value => new URL(value.url()).pathname === path);
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function popup(appId) {
      const [apps] = await Promise.all([response('/api/subscription/app-owner/apps'), page.locator('#fixture-open-subscription').click()]);
      assert.equal(apps.status(), 200);
      if (!appId) { assert.deepEqual((await apps.json()).result, []); return; }
      await page.locator('.uo-open[data-app="' + appId + '"]').click();
      await page.locator('#ap-pay-now').waitFor();
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    await popup('owned-app-a');
    assert.equal(await page.locator('.ap-recover').count(), 0);
    phase = 'allocate-fresh-settled-operation';
    const [allocated] = await Promise.all([response(endpoint), page.locator('#ap-pay-now').click()]);
    assert.equal(allocated.status(), 200);
    const result = (await allocated.json()).result;
    assert.equal(result.appId, 'owned-app-a');
    assert.equal(result.noCharges, false);
    assert.match(result.statement.id, /^ovga_[0-9a-f]{64}$/);
    assert.equal(result.statement.amountUsd, 0);
    assert.equal(result.statement.state, 'paid');
    assert.equal(JSON.stringify(result).includes('owned-fresh-private'), false);
    assert.match(writes[0].billingRequestId, /^[0-9a-f-]{36}$/);
    await page.waitForFunction(() => document.getElementById('ap-payment-msg').textContent.includes('paid'));
    phase = 'no-second-allocation';
    const [empty] = await Promise.all([response(endpoint), page.locator('#ap-pay-now').click()]);
    assert.equal(empty.status(), 200);
    assert.equal((await empty.json()).result.noCharges, true);
    assert.notEqual(writes[1].billingRequestId, writes[0].billingRequestId);
    await page.waitForFunction(() => document.getElementById('ap-payment-msg').textContent.startsWith('No uninvoiced app charges'));
    await page.locator('#subscription-close-btn').click();
    await actor(1);
    await popup('owned-app-a');
    assert.equal(await page.locator('.ap-recover').count(), 0);
    assert.match(await page.locator('[data-owner-billing]').textContent(), /\(paid\)/);
    assert.equal((await page.locator('[data-owner-billing]').textContent()).includes('owned-fresh-private'), false);
    await page.locator('#subscription-close-btn').click();
    await actor(0);
    await popup('owned-app-a');
    phase = 'late-session';
    let release, observe, pause = true;
    const held = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observe = resolve; });
    await page.route(url => url.origin === base.origin && url.pathname === endpoint, async route => {
      if (pause && route.request().method() === 'POST') { pause = false; observe(); await held; }
      await route.continue();
    });
    const count = writes.length;
    await page.locator('#ap-pay-now').click();
    await seen;
    await page.evaluate(() => document.getElementById('ap-pay-now').onclick());
    assert.equal(writes.length, count + 1);
    await actor(2);
    const complete = response(endpoint);
    release();
    assert.equal((await complete).status(), 200);
    await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#sub-usage-panel').textContent(), '');
    await page.locator('#subscription-close-btn').click();
    await popup(null);
    assert.deepEqual(errors, []);
    assert.deepEqual(forbidden, []);
    return { appFreshUsageBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      freshSettledOperationAllocatedThroughUi: true, secondAllocationRefusedAsEmpty: true,
      originalPayerViewVerified: true, singlePendingWriterVerified: true, privateIdsRedacted: true,
      roundedZeroDueOnly: true, stripeCollectionVerified: false, nonzeroPaymentVerified: false,
      originalPayerReadiness: 'Explicit owned INPUT fixture', productionStartupVerified: false };
  } catch (error) {
    console.error(JSON.stringify({ phase, pageErrors: errors, paymentRequests: writes.length }));
    throw error;
  } finally { await context.close(); }
}
module.exports = { verify };
