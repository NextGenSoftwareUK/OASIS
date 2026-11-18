'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify(input) {
  const { baseUrl, token, providerType, title } = input;
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.equal(base.protocol, 'http:');
  const { OASISClient } = require(process.env.OASIS_PROFILE_WEB4_SDK_ROOT);
  const client = new OASISClient({ baseUrl, persistSession: false });
  const initial = await client.profile.getProfile({ providerType, token });
  assert.equal(initial.isError, false, initial.message);
  const owner = initial.result.profile;
  const runtime = process.env.OASIS_PROFILE_BROWSER_RUNTIME;
  const { chromium } = require(path.join(runtime, 'node_modules/playwright-core'));
  const profilePath = path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT, 'browser-profile-' + randomUUID());
  const context = await chromium.launchPersistentContext(profilePath, {
    channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce',
    acceptDownloads: false, serviceWorkers: 'block'
  });
  try {
    const forbidden = [];
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      if (url.pathname.startsWith('/api/')) forbidden.push(url.origin + url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(({ owner, token }) => {
      if (!localStorage.getItem('avatar')) {
        localStorage.setItem('avatar', JSON.stringify({
          id: owner.avatarId, username: owner.username, email: owner.email,
          firstName: owner.firstName, lastName: owner.lastName, address: owner.address,
          title: 'stale-client-cache', jwtToken: token, xp: 42
        }));
        localStorage.setItem('loggedIn', 'true');
      }
    }, { owner, token });
    const page = await context.newPage();
    const errors = [];
    const writes = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      if (new URL(request.url()).pathname === '/api/avatar/profile' && request.method() === 'PATCH')
        writes.push(JSON.parse(request.postData()));
    });
    await page.goto(baseUrl + '/__profile_browser', { waitUntil: 'domcontentloaded' });
    const response = () => page.waitForResponse(value =>
      new URL(value.url()).pathname === '/api/avatar/profile' && value.request().method() === 'GET');
    const opening = response();
    const started = Date.now();
    await page.locator('#fixture-open-profile').click();
    const opened = await (await opening).json();
    assert.equal(opened.result.providerType, providerType);
    await page.waitForFunction(() => !document.getElementById('av-title').disabled);
    assert.equal(await page.locator('#av-title').inputValue(), opened.result.profile.title || '');
    const openMs = Date.now() - started;
    const username = 'browser-' + randomUUID().replaceAll('-', '');
    const email = username + '@example.test';
    await page.locator('#av-title').fill(title);
    await page.locator('#av-firstName').fill('Browser ' + providerType);
    await page.locator('#av-lastName').fill('Form profile');
    await page.locator('#av-username').fill(username);
    await page.locator('#av-email').fill(email);
    await page.locator('#av-address').fill('');
    const saving = page.waitForResponse(value =>
      new URL(value.url()).pathname === '/api/avatar/profile' && value.request().method() === 'PATCH');
    const saveStarted = Date.now();
    await page.locator('#avatar-modal-save-btn').click();
    const savedResponse = await saving;
    assert.equal(savedResponse.status(), 200);
    const saved = await savedResponse.json();
    await page.waitForFunction(() =>
      document.getElementById('avatar-modal-status').className.includes('--success'));
    assert.equal(writes.length, 1, 'One user save must send one profile PATCH.');
    assert.equal(writes[0].changes.title, title);
    assert.equal(writes[0].avatarId, undefined);
    assert.equal(writes[0].Title, undefined);
    const saveMs = Date.now() - saveStarted;
    assert.equal(saved.result.profile.address, '');
    assert.equal(saved.result.profile.username, username);
    assert.equal(saved.result.profile.email, email);
    assert.equal(await page.evaluate(() => JSON.parse(localStorage.getItem('avatar')).title), title);

    await page.locator('#avatar-modal-close-btn').click();
    await page.reload({ waitUntil: 'domcontentloaded' });
    const reopened = response();
    await page.locator('#fixture-open-profile').click();
    const displayed = await (await reopened).json();
    await page.waitForFunction(() => !document.getElementById('av-title').disabled);
    assert.equal(await page.locator('#av-title').inputValue(), title);
    assert.equal(await page.locator('#av-username').inputValue(), username);
    assert.equal(await page.locator('#av-email').inputValue(), email);

    const current = displayed.result;
    const concurrent = await client.profile.applyProfile({
      avatarRevision: current.profile.avatarRevision, detailRevision: current.profile.detailRevision,
      providerType: current.providerType, changes: { firstName: 'Concurrent ' + title }
    }, { token, operationId: randomUUID() });
    assert.equal(concurrent.isError, false, concurrent.message);
    await page.locator('#av-title').fill('stale-client-edit');
    const conflict = page.waitForResponse(value =>
      new URL(value.url()).pathname === '/api/avatar/profile' && value.request().method() === 'PATCH');
    await page.locator('#avatar-modal-save-btn').click();
    assert.equal((await conflict).status(), 409);
    await page.waitForFunction(() => document.getElementById('av-title').disabled);
    assert.equal(await page.evaluate(() => JSON.parse(localStorage.getItem('avatar')).title), title);
    await page.locator('#avatar-modal-close-btn').click();

    let release;
    let observed;
    const held = new Promise(resolve => { release = resolve; });
    const observedRequest = new Promise(resolve => { observed = resolve; });
    let pause = true;
    await page.route('**/api/avatar/profile', async route => {
      if (pause && route.request().method() === 'GET') {
        pause = false; observed(); await held;
      }
      await route.continue();
    });
    const lateResponse = response();
    await page.locator('#fixture-open-profile').click();
    await observedRequest;
    const otherId = randomUUID();
    await page.evaluate(otherId => {
      localStorage.setItem('avatar', JSON.stringify({ id: otherId, title: 'Other session' }));
      window.oasisClient.setToken('different-session-token', { avatarId: otherId });
    }, otherId);
    release();
    assert.equal((await lateResponse).status(), 200);
    await page.waitForFunction(() =>
      document.getElementById('avatar-modal-status').textContent.includes('session changed'));
    assert.equal(await page.evaluate(() => JSON.parse(localStorage.getItem('avatar')).id), otherId);
    assert.equal(await page.evaluate(() => JSON.parse(localStorage.getItem('avatar')).title), 'Other session');
    assert.equal(await page.locator('#av-title').isDisabled(), true);
    assert.equal(await page.locator('#av-email').inputValue(), '', 'Changed session must clear the previous private contact view.');
    assert.equal(await page.locator('#av-title').inputValue(), '');
    assert.deepEqual(forbidden, [], 'No non-loopback API request is permitted.');
    assert.deepEqual(errors, []);
    const final = await client.profile.getProfile({ providerType, token });
    assert.equal(final.isError, false, final.message);
    assert.equal(final.result.profile.title, title);
    return {
      title, username, email, firstName: final.result.profile.firstName, lastName: final.result.profile.lastName,
      avatarVersionId: final.result.profile.avatarRevision.versionId,
      detailVersionId: final.result.profile.detailRevision.versionId,
      browserVerified: true, openMs, saveMs, profileWrites: writes.length,
      portraitUploadVerified: false, productionStartupVerified: false
    };
  } finally {
    await context.close();
  }
}
module.exports = { verify };
