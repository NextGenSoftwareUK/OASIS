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
  const settingsPath = '/api/subscription/app-owner/owned-app-a/settings';
  const responses = [], errors = [], forbidden = [], writes = [];
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
      if (new URL(response.url()).pathname.startsWith('/api/subscription/app-owner/')) responses.push(response);
    });
    page.on('request', request => {
      if (new URL(request.url()).pathname.startsWith('/api/subscription/app-owner/') && request.method() === 'POST') writes.push(request);
    });
    const response = (pathname, method) => page.waitForResponse(value => new URL(value.url()).pathname === pathname && value.request().method() === method);
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function open(appId) {
      const [apps] = await Promise.all([response('/api/subscription/app-owner/apps', 'GET'), page.locator('#fixture-open-subscription').click()]);
      assert.equal(apps.status(), 200);
      const values = (await apps.json()).result;
      assert.deepEqual(values.map(app => app.appId), appId ? [appId] : []);
      if (!appId) return;
      const [settings] = await Promise.all([response('/api/subscription/app-owner/' + appId + '/settings', 'GET'),
        page.locator('.uo-open[data-app="' + appId + '"]').click()]);
      assert.equal(settings.status(), 200);
      const value = (await settings.json()).result;
      assert.equal(value.appId, appId);
      await page.locator('#ao-save').waitFor();
      return value;
    }
    async function save() {
      const [result] = await Promise.all([response(settingsPath, 'POST'), page.locator('#ao-save').click()]);
      assert.equal(result.status(), 200);
      const value = await result.json();
      assert.equal(value.isSaved, true);
      assert.equal(value.result.appId, 'owned-app-a');
      assert.equal(value.result.keyHashSha256 == null, true);
      assert.equal(value.result.overrides.sealedHistoryEnabled, true);
      assert.equal(value.result.overrides.versionPurgeEnabled, true);
      assert.equal(value.result.overrides.versionPurgeDays, 45);
      await page.waitForFunction(() => document.getElementById('ao-msg')?.textContent === 'Saved.');
      return value.result;
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    phase = 'owned-settings';
    const initial = await open('owned-app-a');
    assert.equal(initial.overrides.sealedHistoryEnabled, true);
    await page.locator('#ao-view').selectOption('off');
    await page.locator('#ao-warn').selectOption('on');
    await page.locator('#ao-log').selectOption('off');
    const saved = await save();
    assert.equal(saved.overrides.ownerViewEnabled, false);
    assert.equal(saved.overrides.warningsEnabled, true);
    assert.equal(await page.locator('#ao-view').inputValue(), 'off');
    await page.locator('#subscription-close-btn').click();
    const persisted = await open('owned-app-a');
    assert.equal(persisted.overrides.ownerViewEnabled, false);
    assert.equal(persisted.overrides.warningsEnabled, true);
    phase = 'explicit-inheritance';
    await page.locator('#ao-warn').selectOption('default');
    assert.equal((await save()).overrides.warningsEnabled, null);
    phase = 'non-owner';
    await page.locator('#subscription-close-btn').click();
    await actor(1);
    await open('owned-app-b');
    const refused = await page.evaluate(async path => {
      const read = await window.oasisClient.http.get(path);
      const write = await window.oasisClient.http.post(path, { body: { warningsEnabled: true } });
      return [read.statusCode, write.statusCode];
    }, settingsPath);
    assert.deepEqual(refused, [403, 403]);
    await page.locator('#subscription-close-btn').click();
    await actor(2);
    await open(null);
    assert.equal(await page.locator('.uo-open').count(), 0);
    await page.locator('#subscription-close-btn').click();
    await actor(0);
    await open('owned-app-a');
    phase = 'late-save';
    let release, observe;
    const held = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observe = resolve; });
    await page.route(url => url.origin === base.origin && url.pathname === settingsPath, async route => {
      if (route.request().method() === 'POST') { observe(); await held; }
      await route.continue();
    });
    await page.locator('#ao-view').selectOption('on');
    await page.locator('#ao-warn').selectOption('on');
    await page.locator('#ao-save').click();
    await seen;
    assert.equal(await page.locator('#ao-save').isDisabled(), true);
    await page.evaluate(() => document.getElementById('ao-save').onclick());
    assert.equal(writes.length, 4, 'Three owner writes and one refused foreign write; no duplicate pending save.');
    await actor(1);
    const start = responses.length;
    release();
    const late = responses.slice(start).find(value => new URL(value.url()).pathname === settingsPath && value.request().method() === 'POST') || await response(settingsPath, 'POST');
    assert.equal(late.status(), 200);
    await late.finished();
    await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#sub-usage-panel').textContent(), '');
    await page.locator('#subscription-close-btn').click();
    const other = await open('owned-app-b');
    assert.equal(other.overrides.ownerViewEnabled, true);
    assert.equal(other.overrides.warningsEnabled, false);
    assert.deepEqual(errors, []);
    assert.deepEqual(forbidden, []);
    return { appOwnerSettingsBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      committedBrowserSettingsWrites: 3, refusedForeignWriteRequests: 1, hiddenSettingsPreserved: true,
      appUsageVerified: false, appBillingVerified: false, productionStartupVerified: false, stripeCheckoutVerified: false };
  } catch (error) {
    console.error(JSON.stringify({ phase, pageErrors: errors,
      responses: responses.map(value => ({ path: new URL(value.url()).pathname, method: value.request().method(), status: value.status() })) }));
    throw error;
  } finally { await context.close(); }
}
module.exports = { verify };
