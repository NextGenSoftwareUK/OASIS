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
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const endpoint = id => '/api/subscription/app-owner/' + id + '/payg';
  const received = [], errors = [], forbidden = [], writes = [], timings = [];
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
    page.setDefaultNavigationTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => {
      if (new URL(response.url()).pathname.startsWith('/api/subscription/app-owner/')) received.push(response);
    });
    page.on('request', request => {
      if (new URL(request.url()).pathname.startsWith('/api/') && request.method() !== 'GET') writes.push(request.method());
    });
    const response = pathname => page.waitForResponse(value => new URL(value.url()).pathname === pathname && value.request().method() === 'GET');
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function popup() {
      const [apps] = await Promise.all([response('/api/subscription/app-owner/apps'), page.locator('#fixture-open-subscription').click()]);
      assert.equal(apps.status(), 200);
      return (await apps.json()).result;
    }
    async function open(id, index) {
      await popup();
      const start = Date.now();
      const [result] = await Promise.all([response(endpoint(id)), page.locator('.uo-open[data-app="' + id + '"]').click()]);
      assert.equal(result.status(), 200);
      const billing = (await result.json()).result;
      assert.equal(billing.currency, 'USD');
      assert.equal(billing.pricesConfigured, true);
      assert.equal(typeof billing.featureEnabled, 'boolean');
      assert.equal(billing.pricingVersion, 'app-overages-2026-11-13-v1');
      assert.match(billing.pricingSha256, /^[0-9a-f]{64}$/);
      assert.equal(billing.ratePerRequestUsd, 0.0005);
      assert.equal(billing.settings.enabled, false);
      assert.equal(billing.settings.pricingVersion, 'owned-historical-terms', 'Reading published prices must not rewrite stored consent.');
      assert.equal(billing.settings.monthlyCapUsd, index ? 2 : 5);
      assert.equal(billing.estimate.overRequests, index ? 6000 : 2000);
      assert.equal(billing.estimate.grossUsd, index ? 3 : 1);
      assert.equal(billing.estimate.chargedUsd, index ? 2 : 1);
      assert.equal(billing.estimate.capReached, !!index);
      assert.equal(billing.statements.length, 1, 'Other app and personal statements must be excluded.');
      const statement = billing.statements[0];
      assert.equal(statement.amountUsd, index ? 4.56 : 1.23);
      assert.equal(statement.state, index ? 'open' : 'paid');
      assert.equal(statement.paymentFailed, !!index);
      assert.deepEqual(Object.keys(statement).sort(), ['amountUsd', 'hostedInvoiceUrl', 'id', 'month', 'paymentFailed', 'state']);
      await page.locator('#ap-cap').waitFor();
      assert.equal(Number(await page.locator('#ap-cap').inputValue()), billing.settings.monthlyCapUsd);
      await page.waitForFunction(value => document.getElementById('ap-estimate')?.textContent.includes('$' + value),
        billing.estimate.chargedUsd.toFixed(2));
      const panel = await page.locator('[data-owner-billing]').textContent();
      assert.equal(panel.includes('$' + statement.amountUsd.toFixed(2)), true);
      assert.equal(panel.includes('payment failed'), !!index);
      assert.equal(await page.locator('[data-owner-billing] a').getAttribute('href'), statement.hostedInvoiceUrl);
      assert.equal(await page.locator('#ap-consent').isChecked(), false);
      assert.equal(await page.locator('#ap-on').isDisabled(), !billing.featureEnabled);
      if (!index) await page.waitForFunction(() => document.querySelector('[data-owner-usage]')?.textContent.includes('turned off'));
      timings.push({ app: id, billingPanelMs: Date.now() - start });
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    phase = 'billing-with-usage-disabled';
    await open('owned-app-a', 0);
    await page.locator('#subscription-close-btn').click();
    await actor(1);
    phase = 'other-app-billing';
    await open('owned-app-b', 1);
    const foreign = await page.evaluate(async path => (await window.oasisClient.http.get(path)).statusCode, endpoint('owned-app-a'));
    assert.equal(foreign, 403);
    await page.locator('#subscription-close-btn').click();
    await actor(2);
    assert.deepEqual(await popup(), []);
    const refused = await page.evaluate(async path => (await window.oasisClient.http.get(path)).statusCode, endpoint('owned-app-a'));
    assert.equal(refused, 403);
    await page.locator('#subscription-close-btn').click();
    await actor(0);
    await popup();
    phase = 'late-billing-response';
    let release, observe, pause = true;
    const held = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observe = resolve; });
    await page.route(url => url.origin === base.origin && url.pathname === endpoint('owned-app-a'), async route => {
      if (pause) { pause = false; observe(); await held; }
      await route.continue();
    });
    await page.locator('.uo-open[data-app="owned-app-a"]').click();
    await seen;
    await actor(1);
    const start = received.length;
    release();
    const late = received.slice(start).find(value => new URL(value.url()).pathname === endpoint('owned-app-a')) || await response(endpoint('owned-app-a'));
    assert.equal(late.status(), 200);
    await late.finished();
    await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#sub-usage-panel').textContent(), '');
    await page.locator('#subscription-close-btn').click();
    await open('owned-app-b', 1);
    assert.deepEqual(writes, []);
    assert.deepEqual(errors, []);
    assert.deepEqual(forbidden, []);
    return { appBillingReadBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      publishedTermsVerified: true, estimatesAndCapsVerified: true, statementIsolationVerified: true,
      billingAccessibleWhenUsageViewDisabled: true, billingWriteRequests: 0, timings,
      appConsentVerified: false, appAdmissionVerified: false, appSettlementVerified: false,
      stripeCollectionVerified: false, productionStartupVerified: false, stripeCheckoutVerified: false };
  } catch (error) {
    console.error(JSON.stringify({ phase, pageErrors: errors,
      responses: received.slice(-40).map(value => ({ path: new URL(value.url()).pathname, status: value.status() })) }));
    throw error;
  } finally { await context.close(); }
}
module.exports = { verify };
