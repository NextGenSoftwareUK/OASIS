'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs/promises');
const path = require('node:path');
const {randomUUID} = require('node:crypto');

async function verify({baseUrl, token, avatarId, providerType}) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.equal(base.protocol, 'http:');
  assert.equal(providerType, 'MongoDBOASIS');
  const {chromium} = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const profile = path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT, 'browser-profile-' + randomUUID());
  const context = await chromium.launchPersistentContext(profile, {channel: 'chrome', headless: true,
    viewport: {width: 1440, height: 1000}, serviceWorkers: 'block', acceptDownloads: true,
    downloadsPath: path.join(profile, 'downloads')});
  const errors = [], writes = [], unexpected = [];
  const failedResponses = [];
  let page;
  const expected = Buffer.from('Actual portal file native round trip.\n', 'utf8');
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || ['data:', 'blob:'].includes(url.protocol)) return route.continue();
      if (url.pathname.startsWith('/api/')) unexpected.push(url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(({token, avatarId}) => {
      localStorage.setItem('loggedIn', 'true');
      localStorage.setItem('avatar', JSON.stringify({id: avatarId, jwtToken: token}));
    }, {token, avatarId});
    page = await context.newPage();
    page.setDefaultTimeout(15000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => {
      const url = new URL(response.url());
      if (url.pathname.startsWith('/api/') && response.status() >= 400)
        failedResponses.push({path: url.pathname, status: response.status()});
    });
    page.on('dialog', dialog => dialog.accept());
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.pathname.startsWith('/api/') && request.method() !== 'GET')
        writes.push({method: request.method(), path: url.pathname, query: url.searchParams, body: request.postData()});
    });
    await page.goto(baseUrl + '/__files_notifications_browser');
    await page.waitForFunction(() => window.__filesNotificationsReady === true);
    await page.locator('#fixture-open-files').click();
    await page.waitForFunction(() => document.getElementById('files-stat-count').textContent === '0');
    await page.locator('#files-modal-block [data-tab="upload"]').click();
    await page.locator('#files-upload-input').setInputFiles({name: 'selected.txt', mimeType: 'text/plain', buffer: expected});
    await page.locator('#files-upload-name').fill('portal-owned.txt');
    await page.locator('#files-upload-desc').fill('Owned native file proof');
    await page.locator('#files-upload-btn').click();
    await page.waitForFunction(() => document.getElementById('files-stat-count').textContent === '1');
    assert.match(await page.locator('#files-list').innerText(), /portal-owned\.txt/);
    assert.match(await page.locator('#files-list').innerText(), /Owned native file proof/);
    await page.locator('#files-close-btn').click();
    await page.locator('#fixture-open-files').click();
    await page.waitForFunction(() => document.getElementById('files-stat-count').textContent === '1' &&
      document.querySelector('#files-list [data-file-download]'));
    const fileId = await page.locator('[data-file-download]').getAttribute('data-file-download');
    assert.match(fileId, /^[0-9a-f-]{36}$/i);
    const downloaded = page.waitForEvent('download');
    await page.locator('[data-file-download]').click();
    const download = await downloaded;
    try {
      assert.equal(download.suggestedFilename(), 'portal-owned.txt');
      const target = await download.path();
      assert.ok(path.resolve(target).startsWith(path.resolve(profile) + path.sep));
      assert.deepEqual(await fs.readFile(target), expected);
    } finally { await download.delete(); }
    await page.locator('[data-file-delete]').click();
    await page.waitForFunction(() => document.getElementById('files-stat-count').textContent === '0');
    await page.locator('#files-close-btn').click();
    await page.locator('#notif-bell-btn').click();
    await page.waitForFunction(() => document.querySelectorAll('#notif-list [data-notification-id]').length === 50);
    await page.locator('#notif-more-btn').click();
    await page.waitForFunction(() => document.querySelectorAll('#notif-list [data-notification-id]').length === 51);
    await page.locator('#notif-list [data-notification-id]').first().click();
    await page.waitForFunction(() => document.querySelectorAll('#notif-list [data-notification-id]').length === 50);
    await page.locator('#notif-mark-all-btn').click();
    await page.waitForFunction(() => document.getElementById('notif-status').textContent === 'Notifications marked as read.' &&
      document.querySelectorAll('#notif-list [data-notification-id]').length === 0);
    await page.locator('#notif-tab-all').click();
    assert.equal(await page.locator('#notif-list .notif-row--read').count(), 51);
    await page.locator('#notif-refresh-btn').click();
    await page.waitForFunction(() => document.getElementById('notif-status').textContent === '50 notifications loaded.' &&
      document.querySelectorAll('#notif-list .notif-row--read').length === 50);
    await page.locator('#notif-more-btn').click();
    await page.waitForFunction(() => document.querySelectorAll('#notif-list .notif-row--read').length === 51);
    await page.locator('#notif-panel-close-btn').click();
    assert.equal(await page.locator('#notif-panel').isVisible(), false);
    assert.equal(writes.length, 4);
    const upload = writes.find(write => write.path === '/api/files/upload-file');
    assert.equal(upload.method, 'POST');
    assert.equal(JSON.parse(upload.body), expected.toString('base64'));
    assert.equal(upload.query.get('fileName'), 'portal-owned.txt');
    assert.equal(upload.query.get('description'), 'Owned native file proof');
    assert.equal(writes.filter(write => write.method === 'DELETE' && write.path === '/api/files/delete-file/' + fileId).length, 1);
    const marks = writes.filter(write => write.path === '/api/messaging/mark-notifications-read');
    assert.equal(marks.length, 2);
    marks.forEach(write => assert.equal(write.method, 'POST'));
    assert.deepEqual(marks.map(write => JSON.parse(write.body).length), [1, 50]);
    assert.deepEqual(errors, []);
    assert.deepEqual(unexpected, []);
    assert.deepEqual(failedResponses, []);
    return {filesPopupVerified: true, notificationsPanelVerified: true, providerType, uploadRequests: 1,
      downloadRequests: 1, deleteRequests: 1, notificationWrites: 2, browserDownloadDeleted: true};
  } catch (error) {
    if (page && !page.isClosed()) {
      const state = await page.evaluate(() => Object.fromEntries(
        ['files-status', 'files-stat-count', 'files-list', 'notif-status'].map(id =>
          [id, (document.getElementById(id)?.textContent || '').slice(0, 500)])));
      console.error(JSON.stringify({popupState: state, failedResponses,
        writeShapes: writes.map(write => ({method: write.method, path: write.path, jsonPrefix: write.body?.[0] || null}))}));
    }
    throw error;
  } finally { await context.close(); }
}
module.exports = {verify};
