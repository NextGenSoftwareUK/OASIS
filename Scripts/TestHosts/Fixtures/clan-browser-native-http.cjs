'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const {randomUUID} = require('node:crypto');

async function verify({baseUrl, token, avatarId, memberToken, memberId, memberUsername, foreignClanId, providerType}) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1'); assert.equal(base.protocol, 'http:'); assert.equal(providerType, 'MongoDBOASIS');
  const {chromium} = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const profile = path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT, 'browser-profile-' + randomUUID());
  const context = await chromium.launchPersistentContext(profile, {channel: 'chrome', headless: true,
    viewport: {width: 1440, height: 1000}, serviceWorkers: 'block'});
  const errors = [], writes = [], failedResponses = [], unexpected = [];
  let page;
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || ['data:', 'blob:'].includes(url.protocol)) {
        if (url.pathname.startsWith('/api/') && !url.pathname.startsWith('/api/clan') && url.pathname !== '/api/avatar/search-directory') {
          unexpected.push(url.pathname); return route.abort('blockedbyclient');
        }
        return route.continue();
      }
      if (url.pathname.startsWith('/api/')) unexpected.push(url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(({token, avatarId}) => {
      localStorage.setItem('loggedIn', 'true');
      localStorage.setItem('avatar', JSON.stringify({id: avatarId, jwtToken: token, avatarType: {value: 'User'}}));
    }, {token, avatarId});
    page = await context.newPage(); page.setDefaultTimeout(15000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('dialog', dialog => dialog.accept());
    page.on('response', response => {
      const url = new URL(response.url());
      if (url.pathname.startsWith('/api/') && response.status() >= 400) failedResponses.push({path: url.pathname, status: response.status()});
    });
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.pathname.startsWith('/api/') && request.method() !== 'GET')
        writes.push({method: request.method(), path: url.pathname, body: request.postData()});
    });
    await page.goto(baseUrl + '/__clan_browser');
    await page.waitForFunction(() => window.__clanReady === true);
    await page.locator('#fixture-open-clan').click();
    await page.waitForFunction(() => document.getElementById('clan-stat-total').textContent === '1');
    assert.match(await page.locator('#clan-browse-list').innerText(), /1 members/);
    await page.locator('[data-clan-view="' + foreignClanId + '"]').click();
    await page.waitForFunction(() => document.getElementById('clan-detail-panel').textContent.includes('Owned shared treasury item'));
    assert.equal(await page.locator('#clan-detail-panel .modal-badge').innerText(), '0');
    assert.equal(await page.locator('#clan-edit-form, #clan-add-member-form, #clan-delete-btn').count(), 0);

    await page.locator('#clan-modal-block [data-tab="create"]').click();
    await page.locator('#clan-create-name').fill('Owned new alliance');
    await page.locator('#clan-create-desc').fill('Created through the real popup.');
    const createdResponse = page.waitForResponse(response => new URL(response.url()).pathname === '/api/clan' && response.request().method() === 'POST');
    await page.locator('#clan-create-btn').click();
    const createdBody = await (await createdResponse).json();
    const created = createdBody.result.result;
    assert.match(created.id, /^[0-9a-f-]{36}$/i); const createdClanId = created.id;
    await page.waitForFunction(() => document.getElementById('clan-edit-form') && document.getElementById('clan-edit-name').value === 'Owned new alliance');
    await page.locator('#clan-detail-panel summary').click();
    await page.locator('#clan-edit-name').fill('Owned edited alliance');
    await page.locator('#clan-edit-description').fill('Saved through the actual API.');
    await page.locator('#clan-edit-form button[type="submit"]').click();
    await page.waitForFunction(() => document.querySelector('#clan-detail-panel h3')?.textContent === 'Owned edited alliance');

    async function add() {
      await page.locator('#clan-add-username').fill(memberUsername);
      await page.locator('#clan-add-member-form button[type="submit"]').click();
      await page.waitForFunction(id => document.querySelector('[data-clan-remove="' + id + '"]'), memberId);
    }
    await add();
    await page.locator('[data-clan-remove="' + memberId + '"]').click();
    await page.waitForFunction(id => !document.querySelector('[data-clan-remove="' + id + '"]') && document.getElementById('clan-add-member-form'), memberId);
    await add();
    await page.locator('#clan-close-btn').click(); await page.locator('#fixture-open-clan').click();
    await page.waitForFunction(() => document.getElementById('clan-stat-total').textContent === '2');
    await page.locator('[data-clan-view="' + createdClanId + '"]').click();
    await page.waitForFunction(id => document.querySelector('[data-clan-remove="' + id + '"]'), memberId);

    async function switchAccount(id, bearer) {
      await page.evaluate(({id, bearer}) => {
        window.dispatchEvent(new Event('oasis-logout'));
        window.user = {id, jwtToken: bearer, avatarType: {value: 'User'}};
        localStorage.setItem('avatar', JSON.stringify(window.user));
        window.oasisClient.setToken(bearer, {avatarId: id});
      }, {id, bearer});
      await page.locator('#fixture-open-clan').click();
      await page.waitForFunction(() => document.getElementById('clan-stat-total').textContent === '2');
      await page.locator('[data-clan-view="' + createdClanId + '"]').click();
    }
    await switchAccount(memberId, memberToken);
    await page.locator('#clan-leave-btn').waitFor();
    assert.equal(await page.locator('#clan-edit-form, #clan-add-member-form, #clan-delete-btn').count(), 0);
    await page.locator('#clan-leave-btn').click();
    await page.waitForFunction(() => document.getElementById('clan-stat-mine').textContent === 'Not a member' &&
      !document.getElementById('clan-leave-btn'));
    await switchAccount(avatarId, token);
    await page.locator('#clan-delete-btn').waitFor();
    assert.equal(await page.locator('[data-clan-remove]').count(), 0);
    await page.locator('#clan-delete-btn').click();
    await page.waitForFunction(() => document.getElementById('clan-stat-total').textContent === '1' &&
      document.getElementById('clan-tab-browse').hidden === false);
    assert.equal(await page.locator('[data-clan-view="' + createdClanId + '"]').count(), 0);
    assert.equal(await page.locator('[data-clan-view="' + foreignClanId + '"]').count(), 1);
    assert.equal(writes.length, 7);
    assert.equal(writes.filter(row => row.method === 'POST' && row.path === '/api/clan').length, 1);
    assert.equal(writes.filter(row => row.method === 'PUT' && row.path === '/api/clan/' + createdClanId).length, 1);
    assert.equal(writes.filter(row => row.method === 'POST' && row.path.endsWith('/members/' + memberId)).length, 2);
    assert.equal(writes.filter(row => row.method === 'DELETE' && row.path.endsWith('/members/' + memberId)).length, 2);
    assert.equal(writes.filter(row => row.method === 'DELETE' && row.path === '/api/clan/' + createdClanId).length, 1);
    assert.deepEqual(errors, []); assert.deepEqual(unexpected, []); assert.deepEqual(failedResponses, []);
    return {clanPopupVerified: true, createdClanId, providerType, createRequests: 1, editRequests: 1,
      memberAddRequests: 2, memberRemoveOrLeaveRequests: 2, deleteRequests: 1, treasuryZeroQuantityVerified: true,
      coldReopenVerified: true, ownerAndMemberUiVerified: true, accountSwitchVerified: true};
  } catch (error) {
    if (page && !page.isClosed()) {
      const state = await page.evaluate(() => Object.fromEntries(['clan-status', 'clan-stat-total', 'clan-stat-mine', 'clan-detail-panel']
        .map(id => [id, (document.getElementById(id)?.textContent || '').slice(0, 500)])));
      console.error(JSON.stringify({clanState: state, failedResponses, writes: writes.map(row => ({method: row.method, path: row.path}))}));
    }
    throw error;
  } finally { await context.close(); }
}
module.exports = {verify};
