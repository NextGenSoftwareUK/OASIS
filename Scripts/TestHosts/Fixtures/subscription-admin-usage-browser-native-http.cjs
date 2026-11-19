'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors, usageTodayDate, usageHistoricalDate }) {
  const base = new URL(baseUrl); assert.equal(base.hostname, '127.0.0.1'); assert.equal(actors.length, 3);
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const errors = [], external = [], writes = [], releases = [];
  const appPath = id => '/api/subscription/admin/app-accounts/' + id + '/usage/series';
  const topPath = id => '/api/subscription/admin/app-accounts/' + id + '/top-users';
  const userPath = id => '/api/subscription/admin/users/' + id + '/usage/series';
  let phase = 'launch';
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      if (url.pathname.startsWith('/api/')) external.push(url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(actor => { window.fixtureSubscriptionActor = actor; }, actors[0]);
    const page = await context.newPage(); page.setDefaultTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => { if (new URL(request.url()).pathname.startsWith('/api/') && request.method() !== 'GET') writes.push(request.method()); });
    const response = pathname => page.waitForResponse(value => new URL(value.url()).pathname === pathname)
      .then(value => ({ value }), error => ({ error }));
    async function take(observation) { const result = await observation; if (result.error) throw result.error; return result.value; }
    const failed = pathname => page.waitForEvent('requestfailed', { predicate: request => new URL(request.url()).pathname === pathname })
      .then(value => ({ value }), error => ({ error }));
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        localStorage.setItem('avatar', JSON.stringify({ id: value.id }));
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function open(expected = 200) {
      const read = response('/api/subscription/admin/analytics'); await page.locator('#fixture-open-admin').click();
      assert.equal((await take(read)).status(), expected); await page.locator('[data-admin-tab="appusage"]').click();
    }
    async function range(date) { await page.locator('#admin-app-from').fill(date); await page.locator('#admin-app-to').fill(date); }
    async function app(id, expected = 200, expectedTop = expected) {
      await page.locator('#admin-app-id').fill(id);
      const series = response(appPath(id)), top = response(topPath(id)); await page.locator('#admin-app-load').click();
      const [a, b] = await Promise.all([take(series), take(top)]); assert.equal(a.status(), expected); assert.equal(b.status(), expectedTop);
      return expected === 200 ? { data: (await a.json()).result, top: expectedTop === 200 ? (await b.json()).result : null } : null;
    }
    async function user(id, expected = 200) {
      await page.locator('#admin-user-id').fill(id); const read = response(userPath(id)); await page.locator('#admin-user-load').click();
      const result = await take(read); assert.equal(result.status(), expected); return expected === 200 ? (await result.json()).result : null;
    }
    async function graph(host, requests) {
      await page.waitForFunction(value => Array.from(document.querySelectorAll(value.host + ' svg title'))
        .some(element => element.textContent.endsWith(': ' + value.requests + ' requests')), { host, requests });
    }
    async function currentToday(host, requests) {
      await page.waitForFunction(value => Array.from(document.querySelectorAll(value.host + ' .usage-progress'))
        .some(element => { const count = element.textContent.match(/Today:?\s+([0-9,]+)\s+(?:of|used)/);
          return count && Number(count[1].replaceAll(',', '')) === value.requests; }), { host, requests });
    }
    async function sdk(pathname) { return page.evaluate(path => window.oasisClient.http.get(path), pathname); }
    async function hold(pathname) {
      let release, pending = true; const gate = new Promise(resolve => { release = resolve; });
      releases.push(release);
      await page.route(url => url.pathname === pathname, async route => {
        if (pending) { pending = false; await gate; }
        // The production AbortController may already have cancelled this held request.
        if (route.request().failure()) return;
        await route.continue();
      });
      return { release };
    }
    async function emptyOutput(host) { await page.waitForFunction(id => document.getElementById(id).textContent === '', host); }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-admin').disabled);
    phase = 'real-sharded-app-and-net-top-users'; await open(); await range(usageTodayDate);
    const current = await app('owned-app-a');
    assert.equal(current.data.pool.poolActive, true); assert.equal(current.data.pool.usage.monthlyRequests, 2);
    assert.equal(current.data.series.totalRequests, 2); assert.equal(current.data.series.todayRequests, 2);
    assert.equal(current.data.series.days[0].units, 14); assert.equal(current.top.length, 2);
    for (const [id, units, usd] of [[actors[0].id, 8, 0.008], [actors[1].id, 6, 0.006]]) {
      const row = current.top.find(value => value.userId === id); assert.ok(row); assert.equal(row.requests, 1);
      assert.equal(row.units, units); assert.equal(row.settledCostUsd, usd);
    }
    await graph('#admin-app-usage', 2); await currentToday('#admin-app-usage', 2);
    const kpis = await page.locator('#admin-app-usage .admin-kpi').evaluateAll(rows => Object.fromEntries(rows.map(row =>
      [row.querySelector('span').textContent, row.querySelector('strong').textContent])));
    assert.equal(kpis['Used this month'], '2'); assert.equal(kpis['Used today'], '2');
    assert.equal(kpis['Pool active'], 'yes'); assert.equal(kpis.Plan, 'silver');
    assert.equal(kpis['Monthly pool size'], String(current.data.pool.limits.monthlyRequestLimit));
    await page.waitForFunction(() => document.querySelectorAll('#admin-app-topusers tbody tr').length === 2);
    const rendered = await page.locator('#admin-app-topusers tbody tr').evaluateAll(rows => rows.map(row => Array.from(row.cells, cell => cell.textContent)));
    assert.equal(rendered.some(cells => cells[0] === actors[2].id), false);
    for (const [id, units] of [[actors[0].id, 8], [actors[1].id, 6]]) {
      const cells = rendered.find(row => row[0] === id); assert.ok(cells);
      assert.deepEqual(cells.slice(0, 3), [id, '1', String(units)]); assert.match(cells[3], /0\.01/);
    }
    phase = 'historical-window-keeps-current-today'; await range(usageHistoricalDate);
    const historical = await app('owned-app-a'); assert.equal(historical.data.series.totalRequests, 1);
    assert.equal(historical.data.series.days[0].units, 777); assert.equal(historical.data.series.todayRequests, 2);
    assert.equal(historical.data.series.monthRequests, 2); await graph('#admin-app-usage', 1); await currentToday('#admin-app-usage', 2);
    const oldUser = await user(actors[0].id); assert.equal(oldUser.totalRequests, 0); assert.equal(oldUser.todayRequests, 7);
    await graph('#admin-user-usage', 0); await currentToday('#admin-user-usage', 7);
    const bounds = '?from=' + usageHistoricalDate + '&to=' + usageHistoricalDate;
    const self = await sdk('/api/subscription/usage/series' + bounds); assert.equal(self.isError, false); assert.equal(self.result.todayRequests, 7);
    const owner = await sdk('/api/subscription/app-owner/owned-app-a/usage' + bounds);
    assert.equal(owner.isError, false); assert.equal(owner.result.series.todayRequests, 2); assert.equal(owner.result.series.totalRequests, 1);
    phase = 'actual-user-chart-and-read-errors'; await range(usageTodayDate);
    const canonicalUser = await user(actors[1].id.toUpperCase());
    assert.equal(canonicalUser.subject, actors[1].id); assert.equal(canonicalUser.totalRequests, 11);
    await graph('#admin-user-usage', 11);
    await app('owned-admin-missing', 404);
    await page.waitForFunction(() => /not found/i.test(document.getElementById('admin-app-usage').textContent));
    await user('not-a-guid', 400);
    await page.waitForFunction(() => /valid avatar/i.test(document.getElementById('admin-user-usage').textContent));
    const tooOld = new Date(usageTodayDate + 'T00:00:00Z'); tooOld.setUTCDate(tooOld.getUTCDate() - 120);
    await page.locator('#admin-app-from').fill(tooOld.toISOString().slice(0, 10));
    await app('owned-app-a', 400, 200);
    await page.waitForFunction(() => /at most 92 days/i.test(document.getElementById('admin-app-usage').textContent));
    phase = 'superseded-app-read-cancelled'; await range(usageTodayDate);
    const appGate = await hold(appPath('owned-app-a'));
    await page.locator('#admin-app-id').fill('owned-app-a');
    await Promise.all([page.waitForRequest(value => new URL(value.url()).pathname === appPath('owned-app-a')), page.locator('#admin-app-load').click()]);
    const cancelledApp = failed(appPath('owned-app-a'));
    const startedB = page.waitForRequest(value => new URL(value.url()).pathname === appPath('owned-app-b'))
      .then(value => ({ value }), error => ({ error }));
    const foreignRead = app('owned-app-b').then(value => ({ value }), error => ({ error }));
    await take(startedB); appGate.release();
    const foreign = await take(foreignRead); assert.equal(foreign.data.series.totalRequests, 1); assert.equal(foreign.data.series.days[0].units, 500);
    assert.match((await take(cancelledApp)).failure().errorText, /ERR_ABORTED/); await graph('#admin-app-usage', 1);
    await page.waitForFunction(() => document.querySelector('#admin-app-usage h3')?.textContent.includes('owned-app-b'));
    phase = 'account-change-cancels-private-user-read';
    const userGate = await hold(userPath(actors[0].id)); await page.locator('#admin-user-id').fill(actors[0].id);
    await Promise.all([page.waitForRequest(value => new URL(value.url()).pathname === userPath(actors[0].id)), page.locator('#admin-user-load').click()]);
    const cancelledUser = failed(userPath(actors[0].id));
    await actor(1); userGate.release(); assert.match((await take(cancelledUser)).failure().errorText, /ERR_ABORTED/);
    await emptyOutput('admin-app-usage'); await emptyOutput('admin-user-usage');
    const ordinarySelf = await sdk('/api/subscription/usage/series' + bounds);
    assert.equal(ordinarySelf.isError, false); assert.equal(ordinarySelf.result.todayRequests, 11);
    const ordinaryOwner = await sdk('/api/subscription/app-owner/owned-app-b/usage' + bounds);
    assert.equal(ordinaryOwner.isError, false); assert.equal(ordinaryOwner.result.series.todayRequests, 1);
    assert.equal(ordinaryOwner.result.series.totalRequests, 0);
    phase = 'ordinary-and-expired-administrator-refusal';
    for (const [index, status] of [[1, 403], [2, 401]]) {
      if (index === 2) await actor(index); await open(status); await range(usageTodayDate);
      await app('owned-app-a', status); await user(actors[0].id, status);
    }
    assert.deepEqual(writes, []); assert.deepEqual(errors, []); assert.deepEqual(external, []);
    return { subscriptionAdminUsageBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      historicalTodayCountsCorrect: true, adminSelfAndOwnerCallersVerified: true, shardedPoolAndCorrectedTopUsersVerified: true,
      supersededReadCancelled: true, accountChangedReadCancelled: true, businessWrites: 0,
      existingLedgerInputsOnly: true, nativeMongoReadVerified: true, paidInferenceProviderExecutionAccepted: false,
      hostedOrStripeAccepted: false, fullPortalAccepted: false };
  } catch (error) { console.error(JSON.stringify({ phase, pageErrors: errors, businessWrites: writes.length })); throw error; }
  finally { releases.forEach(release => release()); await context.close(); }
}
module.exports = { verify };
