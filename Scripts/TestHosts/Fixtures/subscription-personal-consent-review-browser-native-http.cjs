'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors, personalConsentReviewControl }) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.match(personalConsentReviewControl, /^[0-9A-F]{64}$/);
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const usagePath = '/api/subscription/usage';
  const pricesPath = '/api/subscription/overages/prices';
  const statementsPath = '/api/subscription/overages/statements';
  const consentPath = '/api/subscription/toggle-pay-as-you-go';
  const requests = [], writes = [], errors = [], forbidden = [];
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
      const url = new URL(request.url());
      if (url.origin === base.origin && url.pathname.startsWith('/api/')) requests.push({ path: url.pathname, method: request.method() });
      if (url.pathname === consentPath && request.method() === 'POST') writes.push(request.postDataJSON());
    });
    const response = (apiPath, method = 'GET') => page.waitForResponse(value =>
      new URL(value.url()).pathname === apiPath && value.request().method() === method);
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function open() {
      const [result] = await Promise.all([response(usagePath), page.locator('#fixture-open-subscription').click()]);
      assert.equal(result.status(), 200);
      const body = await result.json();
      assert.equal(JSON.stringify(body).includes('owned-consent-review-private'), false);
      await page.locator('#sub-usage-panel .modal-kv-grid').waitFor();
      await page.waitForLoadState('networkidle');
      return body;
    }
    async function reload() {
      await page.reload({ waitUntil: 'domcontentloaded' });
      await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
      return open();
    }
    async function control(stage) {
      const result = await page.request.post(baseUrl + '/__owned-consent-review/' + stage,
        { headers: { 'X-Owned-Consent-Review-Control': personalConsentReviewControl } });
      assert.equal(result.status(), 200);
    }
    async function reviewRequired() {
      assert.match(await page.locator('#sub-usage-panel').textContent(), /needs fresh authorization/);
      assert.equal(await page.locator('#sub-overage-enable').textContent(), 'Review and authorize overages');
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    phase = 'legacy-consent-requires-review';
    const legacy = await open();
    assert.equal(legacy.payAsYouGoEnabled, true);
    assert.equal(legacy.hasBoundPersonalConsent, false);
    await reviewRequired();
    for (const apiPath of [usagePath, pricesPath, statementsPath])
      assert.equal(requests.filter(value => value.path === apiPath && value.method === 'GET').length, 1);
    await page.locator('#sub-overage-enable').click();
    assert.match(await page.locator('#subscription-status').textContent(), /Review and agree/);
    assert.equal(writes.length, 0);
    phase = 'real-repository-binding-from-owned-readiness-input';
    await control('bind');
    const bound = await reload();
    assert.equal(bound.hasBoundPersonalConsent, true);
    assert.match(await page.locator('#sub-usage-panel').textContent(), /saved consent matches the current pricing version/);
    assert.equal(await page.locator('#sub-overage-enable').textContent(), 'Update cap and consent');
    phase = 'version-mismatch';
    await control('stale');
    const stale = await reload();
    assert.equal(stale.hasBoundPersonalConsent, true);
    assert.notEqual(stale.paidOverageTermsVersion, bound.paidOverageTermsVersion);
    await reviewRequired();
    await control('restore');
    await reload();
    phase = 'hash-mismatch';
    await control('stale-hash');
    const staleHash = await reload();
    assert.equal(staleHash.hasBoundPersonalConsent, true);
    assert.equal(staleHash.paidOverageTermsVersion, bound.paidOverageTermsVersion);
    assert.notEqual(staleHash.paidOverageTermsSha256, bound.paidOverageTermsSha256);
    await reviewRequired();
    await control('restore-hash');
    await reload();
    phase = 'real-opt-out-pending-writer-and-late-session';
    let release, observe;
    const gate = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observe = resolve; });
    let pause = true;
    await page.route(url => url.origin === base.origin && url.pathname === consentPath, async route => {
      if (pause && route.request().method() === 'POST') { pause = false; observe(); await gate; }
      await route.continue();
    });
    const dispatched = page.waitForRequest(value => new URL(value.url()).pathname === consentPath && value.method() === 'POST');
    await page.locator('#sub-overage-disable').click();
    await dispatched; await seen;
    assert.equal(await page.locator('#sub-overage-disable').isDisabled(), true);
    await page.evaluate(() => document.getElementById('sub-overage-disable').onclick());
    assert.equal(writes.length, 1);
    assert.deepEqual(writes[0], { enabled: false });
    await actor(1);
    const completed = response(consentPath, 'POST');
    release();
    assert.equal((await completed).status(), 200);
    await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#sub-usage-panel').textContent(), '');
    await page.locator('#subscription-close-btn').click();
    const other = await open();
    assert.equal(other.payAsYouGoEnabled, false);
    assert.equal(other.hasBoundPersonalConsent, false);
    assert.equal(await page.locator('#sub-overage-enable').count(), 0);
    await page.locator('#subscription-close-btn').click();
    await actor(2);
    const missing = await open();
    assert.equal(missing.payAsYouGoEnabled, false);
    assert.equal(await page.locator('#sub-overage-enable').count(), 0);
    await page.locator('#subscription-close-btn').click();
    await actor(0);
    const disabled = await open();
    assert.equal(disabled.payAsYouGoEnabled, false);
    assert.equal(disabled.hasBoundPersonalConsent, false);
    assert.equal(await page.locator('#sub-overage-enable').textContent(), 'Enable paid overages');
    assert.equal(await page.locator('#sub-overage-disable').count(), 0);
    assert.equal(writes.length, 1);
    assert.deepEqual(errors, []); assert.deepEqual(forbidden, []);
    return { personalConsentReviewBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      legacyRequiresReview: true, boundVersionAndHashVerified: true, versionMismatchRequiresReview: true,
      hashMismatchRequiresReview: true, uncheckedConsentMakesNoRequest: true, onePendingWriter: true,
      initialBillingReads: 3, extraConsentRequests: 0, noPrivateRoutingInUsage: true,
      originalAccountAndReadiness: 'Owned INPUT fixtures', genuineStripeVerification: false,
      nonzeroPaymentVerified: false, productionStartupVerified: false };
  } catch (error) {
    console.error(JSON.stringify({ phase, pageErrors: errors, consentWrites: writes.length }));
    throw error;
  } finally { await context.close(); }
}
module.exports = { verify };
