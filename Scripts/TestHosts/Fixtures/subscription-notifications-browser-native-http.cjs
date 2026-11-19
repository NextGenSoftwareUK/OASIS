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
  const endpoint = '/api/subscription/usage/notifications';
  const received = [], writes = [], errors = [], forbidden = [];
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
    async function open() {
      const [result] = await Promise.all([response('GET'), page.locator('#fixture-open-subscription').click()]);
      assert.equal(result.status(), 200);
      await page.locator('#uw-save').waitFor();
      return (await result.json()).result;
    }
    async function fields(thresholds, mode = 'off') {
      await page.locator('#uw-thresholds').fill(thresholds);
      await page.locator('#uw-mode').selectOption(mode);
      await page.locator('#uw-email').uncheck();
    }
    async function save() {
      const [result] = await Promise.all([response('POST'), page.locator('#uw-save').click()]);
      assert.equal(result.status(), 200);
      const body = await result.json();
      assert.equal(body.isSaved, true);
      await page.waitForFunction(() => document.getElementById('uw-msg')?.textContent === 'Saved.');
      return body.result;
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    phase = 'initial-defaults';
    const initial = await open();
    assert.equal(initial.isDefault, true);
    assert.equal(await page.locator('#uw-thresholds').inputValue(), '');
    assert.equal(await page.locator('#uw-thresholds').getAttribute('placeholder'), initial.thresholds.join(', '));
    phase = 'invalid-input';
    for (const invalid of ['50.5,80', '70oops', '0', '101', '70,,90']) {
      await fields(invalid);
      await page.locator('#uw-save').click();
      await page.waitForFunction(() => document.getElementById('uw-msg').textContent.includes('whole percent'));
    }
    assert.equal(writes.length, 0);
    phase = 'single-pending-save';
    let release, observed;
    const held = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observed = resolve; });
    const hold = async route => { observed(); await held; await route.continue(); };
    await page.route(url => url.origin === base.origin && url.pathname === endpoint, async route => {
      if (route.request().method() === 'POST') return hold(route);
      await route.continue();
    });
    await fields('90,70,70');
    await page.locator('#uw-save').click();
    await seen;
    assert.equal(await page.locator('#uw-save').isDisabled(), true);
    await page.evaluate(() => document.getElementById('uw-save').onclick());
    assert.equal(writes.length, 1);
    release();
    await page.waitForFunction(() => document.getElementById('uw-msg').textContent === 'Saved.');
    assert.equal(await page.locator('#uw-thresholds').inputValue(), '70, 90');
    await page.unrouteAll({ behavior: 'wait' });
    await page.locator('#subscription-close-btn').click();
    const persisted = await open();
    assert.deepEqual(persisted.thresholds, [70, 90]);
    assert.equal(persisted.warningsOverride, false);
    phase = 'inherit-defaults';
    await fields('', 'default');
    assert.equal((await save()).isDefault, true);
    assert.equal(await page.locator('#uw-thresholds').inputValue(), '');
    await page.locator('#subscription-close-btn').click();
    await actor(1);
    assert.equal((await open()).isDefault, true);
    await fields('60', 'on');
    await save();
    phase = 'server-validation-and-identity';
    const direct = await page.evaluate(async ({ endpoint, foreign }) => {
      const invalid = await window.oasisClient.http.post(endpoint, { body: { thresholds: [-1], emailRequested: false } });
      const saved = await window.oasisClient.http.post(endpoint, { body: { subject: 'user:' + foreign, userId: foreign,
        thresholds: [45, 95], warningsEnabled: true, emailRequested: false } });
      return { invalid: invalid.statusCode, saved: saved.statusCode };
    }, { endpoint, foreign: actors[0].id });
    assert.deepEqual(direct, { invalid: 400, saved: 200 });
    await page.locator('#subscription-close-btn').click();
    const second = await open();
    assert.equal(second.subject, 'user:' + actors[1].id);
    assert.deepEqual(second.thresholds, [45, 95]);
    await page.locator('#subscription-close-btn').click();
    await actor(2);
    assert.equal((await open()).isDefault, true);
    await page.locator('#subscription-close-btn').click();
    await actor(0);
    await open();
    phase = 'late-session-save';
    let releaseLate, observeLate;
    const lateHeld = new Promise(resolve => { releaseLate = resolve; });
    const lateSeen = new Promise(resolve => { observeLate = resolve; });
    await page.route(url => url.origin === base.origin && url.pathname === endpoint, async route => {
      if (route.request().method() === 'POST') { observeLate(); await lateHeld; }
      await route.continue();
    });
    await fields('35');
    await page.locator('#uw-save').click();
    await lateSeen;
    await actor(1);
    const start = received.length;
    releaseLate();
    const late = received.slice(start).find(value => value.request().method() === 'POST') || await response('POST');
    assert.equal(late.status(), 200);
    assert.equal((await late.json()).result.subject, 'user:' + actors[0].id);
    await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#sub-usage-panel').textContent(), '');
    await page.locator('#subscription-close-btn').click();
    assert.deepEqual((await open()).thresholds, [45, 95]);
    assert.equal(writes.length, 6);
    assert.deepEqual(errors, []);
    assert.deepEqual(forbidden, []);
    return { notificationPreferencesBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      notificationWriteRequests: writes.length, committedPreferenceWrites: 5, warningBannerVerified: false,
      emailDeliveryVerified: false, appOwnerControlsVerified: false, productionStartupVerified: false, stripeCheckoutVerified: false };
  } catch (error) {
    console.error(JSON.stringify({ phase, pageErrors: errors,
      responses: received.map(value => ({ status: value.status(), method: value.request().method() })) }));
    throw error;
  } finally { await context.close(); }
}
module.exports = { verify };
