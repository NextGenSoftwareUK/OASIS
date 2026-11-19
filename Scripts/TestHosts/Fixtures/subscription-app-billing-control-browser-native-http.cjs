'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors }) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.equal(base.protocol, 'http:');
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const endpoint = '/api/subscription/app-owner/owned-app-a/payg';
  const received = [], errors = [], forbidden = [], writes = [];
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
    page.on('response', response => { if (new URL(response.url()).pathname === endpoint) received.push(response); });
    page.on('request', request => {
      if (new URL(request.url()).pathname === endpoint && request.method() === 'POST') writes.push(request);
    });
    const response = method => page.waitForResponse(value => new URL(value.url()).pathname === endpoint && value.request().method() === method);
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function popup(id) {
      const [apps] = await Promise.all([page.waitForResponse(value => new URL(value.url()).pathname === '/api/subscription/app-owner/apps'),
        page.locator('#fixture-open-subscription').click()]);
      assert.equal(apps.status(), 200);
      if (!id) { assert.deepEqual((await apps.json()).result, []); return; }
      await page.locator('.uo-open[data-app="' + id + '"]').click();
      await page.locator('#ap-off').waitFor();
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    phase = 'legacy-preferences';
    await popup('owned-app-a');
    const initial = (await received.find(value => value.request().method() === 'GET').json()).result;
    assert.equal(initial.featureEnabled, true);
    assert.equal(initial.hasAcceptedCurrentTerms, false);
    assert.equal(initial.settings.enabled, true);
    assert.equal(JSON.stringify(initial).includes('owned-private-'), false);
    assert.equal(await page.locator('#ap-on').textContent(), 'Review and turn on');
    assert.equal(await page.locator('#ap-off').isVisible(), true);
    phase = 'missing-consent';
    const [noConsent] = await Promise.all([response('POST'), page.locator('#ap-on').click()]);
    assert.equal(noConsent.status(), 400);
    await page.waitForFunction(() => document.getElementById('ap-msg').textContent.includes('Consent'));
    phase = 'unverified-payer';
    await page.locator('#ap-consent').check();
    const [unverified] = await Promise.all([response('POST'), page.locator('#ap-on').click()]);
    assert.equal(unverified.status(), 409);
    await page.waitForFunction(() => document.getElementById('ap-msg').textContent.includes('paid subscription'));
    assert.equal(await page.locator('#ap-off').isVisible(), true);
    phase = 'malformed-intent';
    const invalid = await page.evaluate(async ({ endpoint, version, fingerprint }) => {
      const variants = [
        { enabled: true, consent: true, monthlyCapUsd: 0.501, pricingVersion: version, pricingSha256: fingerprint },
        { enabled: true, consent: true, monthlyCapUsd: 5, pricingVersion: version, pricingSha256: '0'.repeat(64) },
        { enabled: false, verifiedPayer: { avatarId: 'forged' } }
      ];
      const results = [];
      for (const body of variants) results.push((await window.oasisClient.http.post(endpoint, { body })).statusCode);
      return results;
    }, { endpoint, version: initial.pricingVersion, fingerprint: initial.pricingSha256 });
    assert.deepEqual(invalid, [400, 409, 400]);
    phase = 'foreign-owner';
    await page.locator('#subscription-close-btn').click();
    await actor(1);
    const foreign = await page.evaluate(async endpoint => (await window.oasisClient.http.post(endpoint, { body: { enabled: false } })).statusCode, endpoint);
    assert.equal(foreign, 403);
    await actor(2);
    await popup(null);
    const noOwner = await page.evaluate(async endpoint => (await window.oasisClient.http.post(endpoint, { body: { enabled: false } })).statusCode, endpoint);
    assert.equal(noOwner, 403);
    await page.locator('#subscription-close-btn').click();
    await actor(0);
    await popup('owned-app-a');
    phase = 'opt-out-session-change';
    let release, observe, pause = true;
    const held = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observe = resolve; });
    await page.route(url => url.origin === base.origin && url.pathname === endpoint, async route => {
      if (pause && route.request().method() === 'POST') { pause = false; observe(); await held; }
      await route.continue();
    });
    const beforeOptOut = writes.length;
    await page.locator('#ap-off').click();
    await seen;
    assert.equal(await page.locator('#ap-on').isDisabled(), true);
    await page.evaluate(() => document.getElementById('ap-off').onclick());
    assert.equal(writes.length, beforeOptOut + 1, 'A pending opt-out must not dispatch a second write.');
    const writeCount = writes.length;
    await actor(1);
    const start = received.length;
    release();
    const saved = received.slice(start).find(value => value.request().method() === 'POST') || await response('POST');
    assert.equal(saved.status(), 200);
    const body = await saved.json();
    assert.equal(body.isSaved, true);
    assert.equal(body.result.payg.enabled, false);
    assert.equal(body.result.payg.monthlyCapUsd, 5);
    assert.equal(body.result.payg.pricingVersion, 'owned-historical-terms');
    assert.equal(JSON.stringify(body).includes('owned-private-'), false);
    await saved.finished();
    await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#sub-usage-panel').textContent(), '');
    assert.equal(writes.length, writeCount);
    await page.locator('#subscription-close-btn').click();
    await popup('owned-app-b');
    assert.equal(await page.locator('#ap-off').isVisible(), true);
    assert.deepEqual(errors, []);
    assert.deepEqual(forbidden, []);
    return { appBillingControlsBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      optOutVerified: true, priorTermsPreserved: true, legacyRequiresFreshConsent: true,
      missingConsentAndInvalidTermsRefused: true, unverifiedPayerRefused: true, foreignOwnerRefused: true,
      privatePayerEvidenceNotExposed: true, committedPreferenceWrites: 1,
      stripeReadinessVerified: false, positiveOptInVerified: false, appAdmissionVerified: false,
      appSettlementVerified: false, stripeCollectionVerified: false, productionStartupVerified: false };
  } catch (error) {
    console.error(JSON.stringify({ phase, pageErrors: errors,
      responses: received.slice(-24).map(value => ({ method: value.request().method(), status: value.status() })) }));
    throw error;
  } finally { await context.close(); }
}
module.exports = { verify };
