'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, actors, originalVersion }) {
  const base = new URL(baseUrl); assert.equal(base.hostname, '127.0.0.1');
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, acceptDownloads: false, serviceWorkers: 'block' });
  const catalogue = '/v1/admin/config/catalogue', validation = catalogue + '/validate', publication = catalogue + '/publish';
  const requests = [], errors = [], forbidden = [];
  let phase = 'launch';
  const startedAt = Date.now(), phases = [];
  let publicationControls;
  let denialControls;
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/v1/')) forbidden.push(url.origin + url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(actor => { window.fixtureSubscriptionActor = actor; }, actors[0]);
    const page = await context.newPage(); page.setDefaultTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.pathname.startsWith(catalogue)) requests.push({ path: url.pathname, method: request.method() });
    });
    const observeResponse = pathname => page.waitForResponse(value => new URL(value.url()).pathname === pathname)
      .then(value => ({ value }), error => ({ error }));
    async function take(observation) { const outcome = await observation; if (outcome.error) throw outcome.error; return outcome.value; }
    const response = pathname => take(observeResponse(pathname));
    const count = pathname => requests.filter(value => value.path === pathname).length;
    async function actor(index) {
      await page.evaluate(value => {
        window.oasisClient.setToken(value.token, { avatarId: value.id });
        localStorage.setItem('avatar', JSON.stringify({ id: value.id }));
        window.dispatchEvent(new CustomEvent('oasis-login'));
      }, actors[index]);
    }
    async function open() {
      await Promise.all([response('/api/subscription/admin/analytics'), page.locator('#fixture-open-admin').click()]);
      const [result] = await Promise.all([response(catalogue), page.locator('[data-admin-tab="catalogue"]').click()]);
      await page.waitForLoadState('networkidle'); return result;
    }
    async function draft(value) {
      const editingAt = Date.now();
      const editor = page.locator('#admin-catalogue-json');
      assert.equal(await editor.isVisible(), true, 'Catalogue draft editor must be visible before editing.');
      assert.equal(await editor.isEditable(), true, 'Catalogue draft editor must be editable before editing.');
      const text = JSON.stringify(value, null, 2);
      await editor.click();
      assert.equal(await editor.evaluate(element => document.activeElement === element), true, 'Catalogue editor must own keyboard focus.');
      await page.keyboard.press('ControlOrMeta+A');
      await page.keyboard.insertText(text);
      assert.equal(await editor.inputValue(), text, 'The complete catalogue draft must match the actual browser input.');
      phases.push({ phase: phase + '-draft-edited', elapsedMs: Date.now() - startedAt, editingMs: Date.now() - editingAt });
    }
    async function idle() { await page.waitForFunction(() => !document.getElementById('admin-catalogue-validate').disabled && !document.getElementById('admin-catalogue-publish').disabled); }
    async function submit(endpoint, button, expected) {
      const [result] = await Promise.all([response(endpoint), page.locator(button).click()]);
      assert.equal(result.status(), expected); await idle(); return result;
    }
    async function hold(endpoint) {
      let release, pause = true;
      const gate = new Promise(resolve => { release = resolve; });
      await page.route(url => url.pathname === endpoint, async route => {
        if (pause) { pause = false; await gate; }
        await route.continue();
      });
      return { release };
    }
    await page.goto(baseUrl + '/__subscription_browser', { waitUntil: 'domcontentloaded' });
    await page.waitForFunction(() => !document.getElementById('fixture-open-admin').disabled);
    phase = 'actual-web6-bootstrap'; assert.equal((await open()).status(), 200);
    await idle();
    const original = JSON.parse(await page.locator('#admin-catalogue-json').inputValue());
    assert.equal(original.version, originalVersion); assert.equal(Array.isArray(original.models) && original.models.length > 0, true);
    const candidate = { ...original, version: 'owned-' + randomUUID().slice(0, 8) };
    phase = 'pending-validation-keeps-newer-draft'; await draft(candidate);
    const validationGate = await hold(validation), validated = observeResponse(validation);
    await Promise.all([page.waitForRequest(value => new URL(value.url()).pathname === validation), page.locator('#admin-catalogue-validate').click()]);
    assert.equal(await page.locator('#admin-catalogue-publish').isDisabled(), true);
    const initialGets = count(catalogue);
    await page.evaluate(() => {
      document.getElementById('admin-catalogue-validate').onclick();
      document.getElementById('admin-catalogue-publish').onclick();
      document.getElementById('admin-catalogue-refresh').onclick();
    });
    assert.equal(count(validation), 1); assert.equal(count(publication), 0); assert.equal(count(catalogue), initialGets);
    const newer = { ...candidate, version: candidate.version + '-draft' }; await draft(newer);
    validationGate.release(); assert.equal((await take(validated)).status(), 200); await idle();
    assert.equal(JSON.parse(await page.locator('#admin-catalogue-json').inputValue()).version, newer.version);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /draft changed/);
    phase = 'actual-validation-errors-and-current-version';
    await draft({ ...candidate, models: [] }); await submit(validation, '#admin-catalogue-validate', 400);
    assert.equal(await page.locator('#subscription-admin-status').evaluate(el => el.classList.contains('is-error')), true);
    await draft(candidate); const valid = await submit(validation, '#admin-catalogue-validate', 200);
    assert.equal((await valid.json()).version, candidate.version);
    const noReasonPosts = count(publication); await page.locator('#admin-catalogue-publish').click(); await idle();
    assert.equal(count(publication), noReasonPosts);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /reason is required/);
    phase = 'publish-exact-version-once-and-keep-edits';
    await page.locator('#admin-catalogue-reason').fill('Owned browser catalogue publication');
    const publicationGate = await hold(publication), published = observeResponse(publication);
    const beforePublishGets = count(catalogue);
    publicationControls = await page.evaluate(() => ({
      validateDisabled: document.getElementById('admin-catalogue-validate').disabled,
      publishDisabled: document.getElementById('admin-catalogue-publish').disabled,
      refreshDisabled: document.getElementById('admin-catalogue-refresh').disabled,
      reasonLength: document.getElementById('admin-catalogue-reason').value.length,
      panelHidden: document.getElementById('admin-tab-catalogue').hidden,
      formOwner: document.getElementById('admin-catalogue-publish').form?.id || null,
      status: document.getElementById('subscription-admin-status').textContent.slice(0, 180)
    }));
    await Promise.all([page.waitForRequest(value => new URL(value.url()).pathname === publication), page.locator('#admin-catalogue-publish').click()]);
    phase = 'publication-observed-before-release';
    await page.evaluate(() => { document.getElementById('admin-catalogue-publish').onclick(); });
    assert.equal(count(publication), 1);
    await draft(newer); await page.locator('#admin-catalogue-reason').fill('Owned newer unpublished reason');
    publicationGate.release(); phase = 'actual-native-publication-response';
    const saved = await take(published); assert.equal(saved.status(), 200);
    assert.equal((await saved.json()).activeVersion, candidate.version); await idle();
    await page.waitForLoadState('networkidle');
    assert.equal(count(catalogue), beforePublishGets);
    assert.equal(JSON.parse(await page.locator('#admin-catalogue-json').inputValue()).version, newer.version);
    assert.equal(await page.locator('#admin-catalogue-reason').inputValue(), 'Owned newer unpublished reason');
    assert.match(await page.locator('#subscription-admin-status').textContent(), /newer draft has been kept/);
    phase = 'explicit-refresh-and-immutable-version-conflict';
    const [refreshed] = await Promise.all([response(catalogue), page.locator('#admin-catalogue-refresh').click()]);
    assert.equal(refreshed.status(), 200); await page.waitForLoadState('networkidle');
    await idle();
    assert.equal(JSON.parse(await page.locator('#admin-catalogue-json').inputValue()).version, candidate.version);
    await page.locator('#admin-catalogue-reason').fill('Owned duplicate catalogue publication');
    await submit(publication, '#admin-catalogue-publish', 409);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /immutable/);
    phase = 'loading-cannot-replace-new-draft';
    const readGate = await hold(catalogue), read = observeResponse(catalogue);
    await Promise.all([page.waitForRequest(value => new URL(value.url()).pathname === catalogue), page.locator('#admin-catalogue-refresh').click()]);
    assert.equal(await page.locator('#admin-catalogue-validate').isDisabled(), true);
    assert.equal(await page.locator('#admin-catalogue-publish').isDisabled(), true);
    await draft(newer);
    readGate.release(); assert.equal((await take(read)).status(), 200); await page.waitForLoadState('networkidle'); await idle();
    assert.equal(JSON.parse(await page.locator('#admin-catalogue-json').inputValue()).version, newer.version);
    assert.match(await page.locator('#subscription-admin-status').textContent(), /draft changed while loading/);
    phase = 'late-native-read-after-account-change';
    const lateGate = await hold(catalogue), late = observeResponse(catalogue);
    await Promise.all([page.waitForRequest(value => new URL(value.url()).pathname === catalogue), page.locator('#admin-catalogue-refresh').click()]); await actor(1);
    lateGate.release(); assert.equal((await take(late)).status(), 200); await page.waitForLoadState('networkidle');
    assert.equal(await page.locator('#admin-catalogue-json').inputValue(), '');
    assert.equal(await page.locator('#admin-catalogue-reason').inputValue(), '');
    assert.equal(await page.locator('#subscription-admin-status').textContent(), '');
    phase = 'ordinary-and-expired-real-web6-denials';
    for (const [index, status] of [[1, 403], [2, 401]]) {
      phase = 'real-web6-denials-actor-' + index;
      phases.push({ phase, elapsedMs: Date.now() - startedAt });
      if (index === 2) await actor(index);
      assert.equal((await open()).status(), status);
      denialControls = await page.evaluate(() => {
        const editor = document.getElementById('admin-catalogue-json');
        return { modalVisible: document.querySelector('.js-modal').classList.contains('is-visible'),
          blockSelected: document.getElementById('subscription-admin-modal-block').classList.contains('is-selected'),
          panelHidden: document.getElementById('admin-tab-catalogue').hidden,
          editorDisabled: editor.disabled, editorReadOnly: editor.readOnly,
          editorHasLayout: !!editor.getClientRects().length,
          status: document.getElementById('subscription-admin-status').textContent };
      });
      phases.push({ phase: phase + '-opened', elapsedMs: Date.now() - startedAt });
      await draft(candidate); await submit(validation, '#admin-catalogue-validate', status);
      await page.locator('#admin-catalogue-reason').fill('Owned unauthorized publication');
      await submit(publication, '#admin-catalogue-publish', status);
    }
    await page.evaluate(() => window.dispatchEvent(new CustomEvent('oasis-logout')));
    assert.equal(await page.locator('#admin-catalogue-json').inputValue(), '');
    assert.equal(count(validation), 5); assert.equal(count(publication), 4);
    assert.deepEqual(errors, []); assert.deepEqual(forbidden, []);
    return { subscriptionAdminCatalogueBrowserVerified: true, actorsVerified: 3, lateSessionResponseIgnored: true,
      browserElapsedMs: Date.now() - startedAt, phases,
      publishedVersion: candidate.version, actualWeb6JwtAndStoredWizardAuthority: true,
      singlePendingValidatePublish: true, validationAndReadKeepNewDraft: true, publicationKeepsNewDraft: true,
      noAutomaticPostPublicationRead: true, immutableVersionConflict: true, ordinaryAndExpiredDenied: true,
      refreshControlsDisabledUntilReadComplete: true,
      approvedPricesChanged: false, multiHostPublicationVerified: false, fullProductionStartupVerified: false,
      hostedOrStripeVerified: false };
  } catch (error) { console.error(JSON.stringify({ phase, elapsedMs: Date.now() - startedAt, phases, publicationControls, denialControls, pageErrors: errors, requests: requests.length })); throw error; }
  finally { await context.close(); }
}
module.exports = { verify };
