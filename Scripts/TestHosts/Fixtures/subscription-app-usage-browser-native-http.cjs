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
  const endpoint = id => '/api/subscription/app-owner/' + id + '/usage';
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
      if (new URL(request.url()).pathname.startsWith('/api/') && request.method() !== 'GET') writes.push(new URL(request.url()).pathname);
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
    async function open(id, expected = 200) {
      await popup();
      const started = Date.now();
      const [result] = await Promise.all([response(endpoint(id)), page.locator('.uo-open[data-app="' + id + '"]').click()]);
      assert.equal(result.status(), expected);
      const body = await result.json();
      if (expected !== 200) {
        assert.equal(body.isError, true);
        await page.waitForFunction(() => document.querySelector('[data-owner-usage]')?.textContent.includes('turned off'));
        assert.equal(await page.locator('[data-owner-usage] svg').count(), 0);
        return;
      }
      const usage = body.result;
      assert.equal(usage.app.appId, id);
      assert.equal(usage.series.subject, 'app:' + id);
      assert.equal(usage.avatarNamesIncluded, false);
      assert.equal(JSON.stringify(usage).includes('keyHashSha256'), false);
      assert.equal(usage.series.monthRequests, id === 'owned-app-a' ? 2 : 1);
      assert.equal(usage.series.totalRequests, usage.series.monthRequests);
      assert.equal(usage.series.monthRequests, usage.pool.usage.monthlyRequests);
      assert.equal(usage.series.monthlyLimit, usage.pool.limits.monthlyRequestLimit);
      if (id === 'owned-app-a') {
        const users = Object.fromEntries(usage.users.map(user => [user.avatarId, user]));
        assert.deepEqual(Object.keys(users).sort(), actors.slice(0, 2).map(actor => actor.id).sort());
        assert.deepEqual([users[actors[0].id].requests, users[actors[0].id].units, users[actors[0].id].costUsd], [1, 8, 0.008]);
        assert.deepEqual([users[actors[1].id].requests, users[actors[1].id].units, users[actors[1].id].costUsd], [1, 6, 0.006]);
        assert.equal(usage.users.reduce((sum, user) => sum + user.units, 0), 14);
      } else {
        assert.equal(usage.users.length, 1);
        assert.equal(usage.users[0].avatarId, actors[2].id);
        assert.equal(usage.users[0].units, 500);
      }
      await page.locator('[data-owner-usage] svg.usage-chart').waitFor();
      await page.waitForFunction(expected => document.querySelectorAll('[data-owner-usage] tbody tr').length === expected, usage.users.length);
      const rows = await page.locator('[data-owner-usage] tbody tr').evaluateAll(values => values.map(value => [...value.querySelectorAll('td')].map(cell => cell.textContent)));
      for (const user of usage.users) assert.deepEqual(rows.find(row => row[0] === user.avatarId),
        [user.avatarId, String(user.requests), String(user.units), '$' + user.costUsd.toFixed(6)]);
      timings.push({ app: id, appDetailMs: Date.now() - started });
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    phase = 'corrected-and-expired-ledger';
    await open('owned-app-a');
    phase = 'validation';
    const invalid = await page.evaluate(async path => {
      const date = await window.oasisClient.http.get(path + '?from=not-a-date');
      const range = await window.oasisClient.http.get(path + '?from=2000-01-01&to=2026-01-01');
      return [date.statusCode, range.statusCode];
    }, endpoint('owned-app-a'));
    assert.deepEqual(invalid, [400, 400]);
    await page.locator('#subscription-close-btn').click();
    await actor(1);
    phase = 'app-isolation';
    await open('owned-app-b');
    const foreign = await page.evaluate(async path => (await window.oasisClient.http.get(path)).statusCode, endpoint('owned-app-a'));
    assert.equal(foreign, 403);
    await page.locator('#subscription-close-btn').click();
    await actor(2);
    assert.deepEqual(await popup(), []);
    await page.locator('#subscription-close-btn').click();
    await actor(0);
    phase = 'late-session';
    await popup();
    let release, observed, pause = true;
    const held = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observed = resolve; });
    await page.route(url => url.origin === base.origin && url.pathname === endpoint('owned-app-a'), async route => {
      if (pause) { pause = false; observed(); await held; }
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
    await actor(0);
    phase = 'disabled-view';
    const disabled = await page.evaluate(async () => {
      const result = await window.oasisClient.http.post('/api/subscription/app-owner/owned-app-a/settings', { body: { ownerViewEnabled: false } });
      return { status: result.statusCode, ownerViewEnabled: result.result?.overrides?.ownerViewEnabled };
    });
    assert.deepEqual(disabled, { status: 200, ownerViewEnabled: false });
    await open('owned-app-a', 409);
    assert.deepEqual(writes, ['/api/subscription/app-owner/owned-app-a/settings']);
    assert.deepEqual(errors, []);
    assert.deepEqual(forbidden, []);
    return { appOwnerUsageBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      expiredReservationExcluded: true, auditCorrectionIncluded: true, poolShardTotalsVerified: true,
      invalidDateAndRangeRejected: true, disabledViewRefused: true, usageWriteRequests: 0,
      explicitSettingsWrites: 1, timings, appBillingVerified: false, productionStartupVerified: false, stripeCheckoutVerified: false };
  } catch (error) {
    console.error(JSON.stringify({ phase, pageErrors: errors,
      responses: received.slice(-40).map(value => ({ path: new URL(value.url()).pathname, status: value.status() })) }));
    throw error;
  } finally { await context.close(); }
}
module.exports = { verify };
