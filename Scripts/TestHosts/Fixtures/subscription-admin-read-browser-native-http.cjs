'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors, adminExpectedSubscriptions }) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.equal(adminExpectedSubscriptions, 104);
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const analytics = '/api/subscription/admin/analytics';
  const tabs = {
    subscriptions: '/api/subscription/admin/subscriptions', invoices: '/api/subscription/admin/invoices',
    overages: '/api/subscription/overages/admin/statements', usage: '/api/subscription/admin/usage/events',
    audit: '/api/subscription/admin/usage/audit'
  };
  const requests = [], errors = [], forbidden = [];
  let phase = 'launch';
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      if (url.pathname.startsWith('/api/')) forbidden.push(url.origin + url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(actor => { window.fixtureSubscriptionActor = actor; }, actors[0]);
    const page = await context.newPage(); page.setDefaultTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.origin === base.origin && url.pathname.startsWith('/api/')) requests.push({ path: url.pathname, method: request.method() });
    });
    const response = apiPath => page.waitForResponse(value => new URL(value.url()).pathname === apiPath && value.request().method() === 'GET');
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        localStorage.setItem('avatar', JSON.stringify({ id: value.id }));
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function open(expected = 200) {
      const [result] = await Promise.all([response(analytics), page.locator('#fixture-open-admin').click()]);
      assert.equal(result.status(), expected);
      if (expected === 200) {
        const body = await result.json();
        assert.equal(body.isError, false);
        assert.equal(body.result.customers, adminExpectedSubscriptions);
        assert.equal(body.result.revenueByCurrency.GBP, 38);
        await page.waitForFunction(() => document.getElementById('admin-kpis').textContent.includes('104'));
      } else await page.waitForFunction(() => document.getElementById('subscription-admin-status').className.includes('is-error'));
      await page.waitForLoadState('networkidle');
      return result;
    }
    function financialWrites() { return requests.filter(value => value.method !== 'GET'); }
    async function cleared() {
      assert.equal(await page.locator('#subscription-admin-modal-block').evaluate(el => el.classList.contains('is-selected')), false);
      assert.equal(await page.locator('#admin-kpis').textContent(), '');
      assert.equal(await page.locator('#admin-subscriptions').textContent(), '');
      assert.equal(await page.locator('#admin-invoices').textContent(), '');
      assert.equal(await page.locator('#subscription-admin-status').textContent(), '');
      assert.equal(await page.locator('#subscription-admin-modal-block').evaluate(el =>
        Array.from(el.querySelectorAll('input:not([type="date"]), textarea')).every(field => field.value === '')), true);
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-admin').disabled);
    phase = 'authoritative-stored-wizard-analytics';
    await open();
    assert.equal(requests.length, 1);
    phase = 'actual-subscription-pagination';
    const [first] = await Promise.all([response(tabs.subscriptions), page.locator('[data-admin-tab="subscriptions"]').click()]);
    assert.equal(first.status(), 200);
    const firstBody = await first.json(); assert.equal(firstBody.result.items.length, 100);
    await page.waitForFunction(() => document.querySelectorAll('#admin-subscriptions tbody tr').length === 100);
    let releaseMore, observeMore;
    const moreGate = new Promise(resolve => { releaseMore = resolve; });
    const moreSeen = new Promise(resolve => { observeMore = resolve; });
    let pauseMore = true;
    await page.route(url => url.origin === base.origin && url.pathname === tabs.subscriptions, async route => {
      if (pauseMore && new URL(route.request().url()).searchParams.has('cursor')) { pauseMore = false; observeMore(); await moreGate; }
      await route.continue();
    });
    const secondResult = response(tabs.subscriptions);
    await page.locator('#admin-subscriptions-more').click(); await moreSeen;
    assert.equal(await page.locator('#admin-subscriptions-more').isDisabled(), true);
    await page.evaluate(() => document.getElementById('admin-subscriptions-more').onclick());
    assert.equal(requests.filter(value => value.path === tabs.subscriptions).length, 2);
    releaseMore(); const second = await secondResult;
    assert.equal(second.status(), 200); assert.equal((await second.json()).result.items.length, 4);
    await page.waitForFunction(() => document.querySelectorAll('#admin-subscriptions tbody tr').length === 104);
    assert.equal(await page.locator('#admin-subscriptions-more').count(), 0);
    phase = 'native-invoices-overages-usage-audit';
    for (const tab of ['invoices', 'overages', 'usage', 'audit']) {
      const [result] = await Promise.all([response(tabs[tab]), page.locator('[data-admin-tab="' + tab + '"]').click()]);
      assert.equal(result.status(), 200);
      const body = await result.json();
      if (tab === 'overages') assert.deepEqual(body, []);
      else { assert.equal(body.isError, false); assert.equal(body.result.items.length, tab === 'invoices' ? 2 : 0); }
      await page.locator('#admin-' + tab + ' table').waitFor();
    }
    assert.equal(financialWrites().length, 0);
    phase = 'pending-actual-analytics-and-account-change';
    await page.locator('[data-admin-tab="analytics"]').click();
    await page.evaluate(() => {
      document.querySelectorAll('#subscription-admin-modal-block input, #subscription-admin-modal-block textarea').forEach(field => {
        if (field.type === 'text' || field.tagName === 'TEXTAREA') field.value = 'owned-admin-private-draft';
      });
    });
    let release, observe;
    const gate = new Promise(resolve => { release = resolve; });
    const seen = new Promise(resolve => { observe = resolve; });
    let pause = true;
    await page.route(url => url.origin === base.origin && url.pathname === analytics, async route => {
      if (pause) { pause = false; observe(); await gate; }
      await route.continue();
    });
    await page.locator('#admin-refresh').click(); await seen;
    await actor(1); await cleared();
    const completed = response(analytics); release();
    assert.equal((await completed).status(), 200);
    await page.waitForLoadState('networkidle'); await cleared();
    phase = 'ordinary-account-all-protected-read-denials';
    await page.evaluate(({ current, stale }) => {
      localStorage.setItem('avatar', JSON.stringify({ id: current.id, jwtToken: stale.token }));
    }, { current: actors[1], stale: actors[0] });
    await open(403);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /authorized account/);
    for (const tab of Object.keys(tabs)) {
      const [result] = await Promise.all([response(tabs[tab]), page.locator('[data-admin-tab="' + tab + '"]').click()]);
      assert.equal(result.status(), 403);
      await page.waitForFunction(() => document.getElementById('subscription-admin-status').textContent.includes('authorized account'));
      assert.equal(await page.locator('#admin-' + tab).textContent(), '');
    }
    phase = 'genuine-expired-signed-token';
    await actor(2); await cleared(); await open(401);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /Sign in again/);
    await page.evaluate(() => {
      document.getElementById('admin-user-id').value = 'owned-admin-private-draft';
      window.dispatchEvent(new CustomEvent('oasis-logout'));
    });
    await cleared();
    assert.equal(financialWrites().length, 0); assert.deepEqual(errors, []); assert.deepEqual(forbidden, []);
    return { subscriptionAdminReadsBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      authoritativeStoredWizardInput: true, initialRequests: 1, subscriptionsRendered: 104, paginationRequests: 2,
      sixReadTabsVerified: true, ordinaryDenials: 6, expiredSignedTokenDenied: true, empty403Handled: true,
      privateInputsAndResultsCleared: true, onePendingRead: true, financialWriteRequests: 0,
      canonicalSdkTokenWithoutCachedJwt: true, cachedWizardJwtIgnored: true,
      outboxCatalogueActionsVerified: false, genuineHostedWizardVerified: false, productionStartupVerified: false };
  } catch (error) { console.error(JSON.stringify({ phase, pageErrors: errors, requests: requests.length })); throw error; }
  finally { await context.close(); }
}
module.exports = { verify };
