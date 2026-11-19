'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors, dashboardExpiredToken, dashboardReplacementToken }) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1'); assert.equal(base.protocol, 'http:'); assert.equal(actors.length, 3);
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const endpoint = '/api/subscription/subscriptions/me', reads = [], errors = [], writes = [], external = [], unsupported = new Set();
  let phase = 'launch';
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/v1/')) external.push(url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(value => { window.fixtureDashboard = value; }, { actor: actors[0], expiredToken: dashboardExpiredToken });
    const page = await context.newPage(); page.setDefaultTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.pathname === endpoint) reads.push(request.method());
      if (url.pathname.startsWith('/api/') && request.method() !== 'GET') writes.push(request.method());
    });
    page.on('response', response => {
      const pathname = new URL(response.url()).pathname;
      if (pathname.startsWith('/api/') && response.status() === 404) unsupported.add(pathname);
    });
    const response = bearer => page.waitForResponse(value => new URL(value.url()).pathname === endpoint &&
      value.request().headers().authorization === 'Bearer ' + bearer)
      .then(value => ({ value }), error => ({ error }));
    async function take(pending) { const result = await pending; if (result.error) throw result.error; return result.value; }
    async function plan(expected) { await page.waitForFunction(value => document.getElementById('dash-card-plan').textContent === value, expected); }
    async function frames() { await page.evaluate(() => new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)))); }
    async function hold() {
      let release, pending = true;
      const gate = new Promise(resolve => { release = resolve; });
      await page.route(url => url.pathname === endpoint, async route => {
        if (pending) { pending = false; await gate; }
        await route.continue();
      });
      return { release };
    }
    phase = 'expired-sdk-bearer';
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => window.fixtureDashboardReady === true);
    const expired = response(dashboardExpiredToken); await page.evaluate(() => window.showDashboard());
    assert.equal((await take(expired)).status(), 401); await plan('—');
    phase = 'replacement-and-coalesced-show';
    const firstGate = await hold(), replacement = response(actors[0].token);
    await Promise.all([page.waitForRequest(value => new URL(value.url()).pathname === endpoint), page.evaluate(actor => {
      window.oasisClient.setToken(actor.token, { avatarId: actor.id });
      window.showDashboard(); window.showDashboard();
    }, actors[0])]);
    await frames(); assert.equal(reads.length, 2);
    firstGate.release(); assert.equal((await take(replacement)).status(), 200); await plan('Bronze');
    await page.evaluate(() => { window.showDashboard(); window.showDashboard(); }); await frames(); assert.equal(reads.length, 2);
    phase = 'late-old-account-response';
    const lateGate = await hold(), late = response(dashboardReplacementToken);
    await Promise.all([page.waitForRequest(value => new URL(value.url()).pathname === endpoint), page.evaluate(value => {
      window.oasisClient.setToken(value.token, { avatarId: value.id }); window.showDashboard();
    }, { token: dashboardReplacementToken, id: actors[0].id })]);
    const switched = response(actors[1].token);
    await page.evaluate(actor => {
      window.oasisClient.setToken(actor.token, { avatarId: actor.id });
      localStorage.setItem('avatar', JSON.stringify({ id: actor.id }));
      window.dispatchEvent(new CustomEvent('oasis-login'));
    }, actors[1]);
    assert.equal((await take(switched)).status(), 200); await plan('Silver');
    lateGate.release(); const lateResponse = await take(late); assert.equal(lateResponse.status(), 200);
    await lateResponse.finished(); await page.waitForLoadState('networkidle'); await plan('Silver');
    phase = 'third-actor-no-cached-jwt';
    const empty = response(actors[2].token);
    await page.evaluate(actor => {
      window.oasisClient.setToken(actor.token, { avatarId: actor.id });
      localStorage.setItem('avatar', JSON.stringify({ id: actor.id }));
      window.dispatchEvent(new CustomEvent('oasis-login'));
    }, actors[2]);
    assert.equal((await take(empty)).status(), 200); await plan('No active plan');
    assert.equal(reads.length, 5); assert.deepEqual(writes, []); assert.deepEqual(errors, []); assert.deepEqual(external, []);
    return { dashboardBearerBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      expiredBearerRefused: true, replacementBearerReloads: true, unchangedSessionCoalesced: true,
      cachedAvatarJwtRequired: false, subscriptionReads: reads.length, businessWrites: writes.length,
      unsupportedRoutes: Array.from(unsupported).sort(), allDashboardRoutesAccepted: false,
      actualRefreshEndpointVerified: false, hostedPerformanceVerified: false };
  } catch (error) { console.error(JSON.stringify({ phase, subscriptionReads: reads.length, pageErrors: errors })); throw error; }
  finally { await context.close(); }
}
module.exports = { verify };
