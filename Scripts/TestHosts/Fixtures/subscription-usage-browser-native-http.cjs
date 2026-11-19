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
  const profile = path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT, 'browser-profile-' + randomUUID());
  const context = await chromium.launchPersistentContext(profile, {
    channel: 'chrome', headless: true, timeout: 30000, viewport: { width: 1440, height: 1000 },
    reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block'
  });
  const received = [];
  const errors = [];
  let phase = 'launch';
  try {
    const forbidden = [];
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
    const writes = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => {
      if (new URL(response.url()).pathname.startsWith('/api/')) received.push(response);
    });
    page.on('request', request => {
      if (new URL(request.url()).pathname.startsWith('/api/') && request.method() !== 'GET') writes.push(request.method());
    });
    const endpoint = suffix => '/api/subscription/' + suffix;
    const response = suffix => page.waitForResponse(value => new URL(value.url()).pathname === endpoint(suffix));
    async function receivedResponse(suffix, start) {
      return received.slice(start).find(value => new URL(value.url()).pathname === endpoint(suffix)) || await response(suffix);
    }
    phase = 'page';
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-subscription').disabled);
    const timings = [];
    for (let i = 0; i < actors.length; i++) {
      if (i) await page.evaluate(actor => {
        window.oasisClient.setToken(actor.token, { avatarId: actor.id });
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[i]);
      const responseStart = received.length;
      phase = 'actor-' + i + '-summary';
      const started = Date.now();
      const [summaryResponse] = await Promise.all([response('usage'), page.locator('#fixture-open-subscription').click()]);
      assert.equal(summaryResponse.status(), 200);
      const summary = await summaryResponse.json();
      const expectedRequests = i < 2 ? 7 + i * 4 : 0;
      assert.equal(summary.currentMonth.requests, expectedRequests);
      await page.waitForFunction(expected => {
        const rows = [...document.querySelectorAll('#sub-usage-panel .modal-kv')];
        const row = rows.find(value => value.querySelector('.modal-kv-label').textContent === 'Requests this UTC month');
        return row?.querySelector('.modal-kv-value').textContent === String(expected);
      }, expectedRequests);
      const summaryMs = Date.now() - started;
      phase = 'actor-' + i + '-prices-statements';
      const pricesResponse = await receivedResponse('overages/prices', responseStart);
      const statementsResponse = await receivedResponse('overages/statements', responseStart);
      assert.equal(pricesResponse.status(), 200);
      assert.equal(statementsResponse.status(), 200);
      assert.equal(Array.isArray(await statementsResponse.json()), true);
      assert.equal(typeof (await pricesResponse.json()).version, 'string');
      if (i < 2) {
        phase = 'actor-' + i + '-history-estimate';
        const historyResponse = await receivedResponse('usage/series', responseStart);
        const estimateResponse = await receivedResponse('usage/estimate', responseStart);
        assert.equal(historyResponse.status(), 200);
        assert.equal(estimateResponse.status(), 200);
        const history = (await historyResponse.json()).result;
        const estimate = (await estimateResponse.json()).result;
        assert.equal(history.subject, actors[i].id);
        assert.equal(history.totalRequests, expectedRequests);
        assert.equal(history.monthRequests, expectedRequests);
        assert.equal(estimate.estimate.monthRequests, expectedRequests);
        assert.equal(history.monthlyLimit, summary.currentMonth.limit);
        assert.equal(estimate.estimate.includedLimit, summary.currentMonth.limit);
        assert.equal(history.dailyLimit, i ? 100000 : 20000, 'Daily quota must use the stored authoritative karma.');
        await page.waitForFunction(() => document.querySelector('#sub-usage-panel svg.usage-chart'));
        await page.waitForFunction(expected => document.querySelector('#sub-usage-panel .usage-chart title') &&
          [...document.querySelectorAll('#sub-usage-panel .usage-chart title')].some(title => title.textContent.includes(': ' + expected + ' requests')), expectedRequests);
        await page.waitForFunction(() => document.getElementById('sub-usage-panel').textContent.includes('At your current pace'));
      } else {
        await page.waitForFunction(() => document.getElementById('sub-usage-panel').textContent.includes('Activate a plan in Plans'));
        assert.equal(await page.locator('#sub-usage-panel svg.usage-chart').count(), 0);
      }
      timings.push({ summaryMs, historyAndEstimateMs: Date.now() - started });
      await page.locator('#subscription-close-btn').click();
    }
    await page.evaluate(actor => {
      window.oasisClient.setToken(actor.token, { avatarId: actor.id });
      window.dispatchEvent(new CustomEvent('oasis-login'));
    }, actors[0]);
    phase = 'validation';
    const invalid = await page.evaluate(async () => {
      const date = await window.oasisClient.http.get('/api/subscription/usage/series?from=not-a-date');
      const admin = await window.oasisClient.http.get('/api/subscription/admin/users/00000000-0000-0000-0000-000000000001/usage/series');
      return [date.statusCode, admin.statusCode];
    });
    assert.deepEqual(invalid, [400, 403]);
    let release;
    let observed;
    const held = new Promise(resolve => { release = resolve; });
    const observedRequest = new Promise(resolve => { observed = resolve; });
    let pause = true;
    await page.route(url => url.origin === base.origin && url.pathname === endpoint('usage'), async route => {
      if (pause) { pause = false; observed(); await held; }
      await route.continue();
    });
    phase = 'delayed-session';
    const lateStart = received.length;
    await page.locator('#fixture-open-subscription').click();
    await observedRequest;
    await page.evaluate(actor => {
      window.oasisClient.setToken(actor.token, { avatarId: actor.id });
      window.dispatchEvent(new CustomEvent('oasis-login'));
    }, actors[1]);
    release();
    const lateResponse = await receivedResponse('usage', lateStart);
    assert.equal(lateResponse.status(), 200);
    await lateResponse.finished();
    await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#sub-usage-panel').textContent(), '');
    assert.deepEqual(writes, []);
    assert.deepEqual(errors, []);
    assert.deepEqual(forbidden, []);
    return { subscriptionUsageBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      timings, usageWriteRequests: 0, notificationSettingsVerified: false, appOwnerControlsVerified: false,
      productionStartupVerified: false, stripeCheckoutVerified: false };
  } catch (error) {
    console.error(JSON.stringify({ phase, pageErrors: errors,
      responses: received.slice(-32).map(value => ({ path: new URL(value.url()).pathname, status: value.status() })) }));
    throw error;
  } finally { await context.close(); }
}
module.exports = { verify };
