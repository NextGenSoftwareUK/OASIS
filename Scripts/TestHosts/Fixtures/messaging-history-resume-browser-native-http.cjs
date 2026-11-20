'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const {randomUUID} = require('node:crypto');

async function verify({baseUrl, token, avatarId, olderRecipientId, sessionId, providerType}) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.equal(base.protocol, 'http:');
  assert.equal(providerType, 'MongoDBOASIS');
  const {chromium} = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const profile = path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT, 'browser-profile-' + randomUUID());
  const context = await chromium.launchPersistentContext(profile, {channel: 'chrome', headless: true,
    viewport: {width: 1440, height: 1000}, serviceWorkers: 'block'});
  const errors = [], failedResponses = [], writes = [], reads = [], unexpected = [];
  let page;
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || ['data:', 'blob:'].includes(url.protocol)) return route.continue();
      if (url.pathname.startsWith('/api/')) unexpected.push(url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(({token, avatarId}) => {
      localStorage.setItem('avatar', JSON.stringify({id: avatarId, jwtToken: token}));
      localStorage.setItem('loggedIn', 'true');
    }, {token, avatarId});
    page = await context.newPage();
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => {
      const url = new URL(response.url());
      if (url.origin === base.origin && url.pathname.startsWith('/api/') && response.status() >= 400)
        failedResponses.push({path: url.pathname, status: response.status()});
    });
    page.on('request', request => {
      const url = new URL(request.url());
      if (url.origin !== base.origin || !url.pathname.startsWith('/api/')) return;
      if (request.method() !== 'GET') writes.push({method: request.method(), path: url.pathname});
      else reads.push({path: url.pathname, offset: url.searchParams.get('offset')});
    });
    await page.goto(base.origin + '/__messaging_browser');
    await page.waitForFunction(() => window.__messagingReady === true);
    await page.locator('#fixture-open-messaging').click();
    await page.waitForFunction(() => document.getElementById('msg-stat-unread').textContent === '50');
    assert.equal(await page.locator('[data-msg-open]').count(), 1);
    assert.equal(await page.locator('[data-msg-open="' + olderRecipientId + '"]').count(), 0);
    await page.locator('#msg-conversations-more').click();
    await page.waitForFunction(() => document.getElementById('msg-stat-unread').textContent === '52');
    assert.equal(await page.locator('[data-msg-open="' + olderRecipientId + '"]').count(), 1);
    assert.equal(await page.locator('#msg-conversations-more').isVisible(), false);
    await page.locator('[data-tab="notifications"]').click();
    await page.waitForFunction(() => document.querySelectorAll('.msg-notification-row').length === 20);
    await page.locator('#msg-notifications-more').click();
    await page.waitForFunction(() => document.querySelectorAll('.msg-notification-row').length === 40);
    await page.locator('#msg-notifications-more').click();
    await page.waitForFunction(() => document.querySelectorAll('.msg-notification-row').length === 52);
    assert.equal(await page.locator('#msg-notifications-more').isVisible(), false);
    await page.locator('[data-tab="chat"]').click();
    await page.locator('[data-chat-resume="' + sessionId + '"]').waitFor({state: 'visible'});
    assert.equal(await page.locator('#msg-stat-chats').innerText(), '1');
    await page.locator('[data-chat-resume="' + sessionId + '"]').click();
    await page.waitForFunction(() => document.querySelectorAll('#msg-chat-messages .msg-chat-bubble').length === 50);
    assert.doesNotMatch(await page.locator('#msg-chat-messages').innerText(), /Owned chat message 0\b/);
    await page.locator('#msg-chat-older-btn').click();
    await page.waitForFunction(() => document.querySelectorAll('#msg-chat-messages .msg-chat-bubble').length === 51);
    assert.match(await page.locator('#msg-chat-messages').innerText(), /Owned chat message 0\b/);
    assert.equal(await page.locator('#msg-chat-older-btn').isVisible(), false);
    await page.locator('[data-tab="notifications"]').click();
    const latest = page.waitForResponse(response => {
      const url = new URL(response.url());
      return url.pathname === '/api/chat/history/' + sessionId && url.searchParams.get('offset') === '0';
    });
    await page.locator('[data-tab="chat"]').click();
    await (await latest).finished();
    assert.equal(await page.locator('#msg-chat-messages .msg-chat-bubble').count(), 51);
    assert.match(await page.locator('#msg-chat-messages').innerText(), /Owned chat message 0\b/);
    await page.locator('#msg-chat-choose-btn').click();
    await page.locator('[data-chat-resume="' + sessionId + '"]').waitFor({state: 'visible'});
    await page.locator('[data-chat-resume="' + sessionId + '"]').click();
    await page.waitForFunction(() => document.querySelectorAll('#msg-chat-messages .msg-chat-bubble').length === 50);
    await page.reload();
    await page.waitForFunction(() => window.__messagingReady === true);
    await page.locator('#fixture-open-messaging').click();
    await page.locator('[data-tab="chat"]').click();
    await page.locator('[data-chat-resume="' + sessionId + '"]').waitFor({state: 'visible'});
    await page.locator('[data-chat-resume="' + sessionId + '"]').click();
    await page.waitForFunction(() => document.querySelectorAll('#msg-chat-messages .msg-chat-bubble').length === 50);
    assert.match(await page.locator('#msg-chat-messages').innerText(), /Owned chat message 50\b/);
    for (const [route, offsets] of [
      ['/api/messaging/messages', ['0', '50']],
      ['/api/messaging/notifications', ['0', '20', '40']],
      ['/api/chat/history/' + sessionId, ['0', '50']]
    ]) for (const offset of offsets)
      assert.ok(reads.some(read => read.path === route && read.offset === offset), route + ' offset ' + offset);
    assert.ok(reads.some(read => read.path === '/api/chat/active-sessions'));
    assert.deepEqual(writes, []);
    assert.deepEqual(errors, []);
    assert.deepEqual(failedResponses, []);
    assert.deepEqual(unexpected, []);
    return {messagingHistoryResumeVerified: true, providerType, sessionId, messagePages: 2,
      notificationPages: 3, chatPages: 2, pageReloadVerified: true, apiWrites: 0};
  } catch (error) {
    if (page && !page.isClosed()) {
      const state = await page.evaluate(() => Object.fromEntries(
        ['messaging-modal-status', 'msg-stat-unread', 'msg-stat-notifs', 'msg-stat-chats', 'msg-active-sessions', 'msg-chat-messages']
          .map(id => [id, (document.getElementById(id)?.textContent || '').slice(0, 400)])));
      console.error(JSON.stringify({state, failedResponses, writes, reads}));
    }
    throw error;
  } finally { await context.close(); }
}
module.exports = {verify};
