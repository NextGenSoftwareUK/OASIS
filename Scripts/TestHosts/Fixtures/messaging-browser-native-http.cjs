'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const {randomUUID} = require('node:crypto');

async function verify({baseUrl, token, avatarId, recipientId, providerType}) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.equal(base.protocol, 'http:');
  assert.equal(providerType, 'MongoDBOASIS');
  const {chromium} = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const profile = path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT, 'browser-profile-' + randomUUID());
  const context = await chromium.launchPersistentContext(profile, {channel: 'chrome', headless: true,
    viewport: {width: 1440, height: 1000}, serviceWorkers: 'block'});
  const errors = [], failedResponses = [], writes = [], unexpected = [];
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
      if (url.origin === base.origin && url.pathname.startsWith('/api/') && request.method() !== 'GET')
        writes.push({method: request.method(), path: url.pathname, body: request.postData()});
    });
    await page.goto(base.origin + '/__messaging_browser');
    await page.waitForFunction(() => window.__messagingReady === true);
    await page.locator('#fixture-open-messaging').click();
    await page.waitForFunction(() => document.querySelectorAll('[data-msg-open]').length === 1);
    assert.match(await page.locator('#msg-conversations-list').innerText(), /Owned (incoming|outgoing) message/);
    await page.locator('[data-msg-open]').click();
    await page.waitForFunction(() => document.querySelectorAll('#msg-thread-messages .msg-chat-bubble').length === 2);
    await page.locator('[data-msg-action="reply"]').click();
    assert.equal(await page.locator('#msg-to-avatar').inputValue(), recipientId);
    await page.locator('#msg-body').fill('Portal actual direct message');
    await page.locator('#msg-send-btn').click();
    await page.waitForFunction(() => document.getElementById('messaging-modal-status').textContent === 'Message sent.');
    await page.locator('[data-tab="conversations"]').click();
    await page.waitForFunction(() => document.querySelectorAll('[data-msg-open]').length === 1);
    await page.locator('#msg-refresh-btn').click();
    await page.waitForFunction(() => document.querySelectorAll('[data-msg-open]').length === 1);
    await page.locator('[data-tab="notifications"]').click();
    await page.waitForFunction(() => document.querySelectorAll('.msg-notification-row').length === 1);
    await page.locator('#msg-mark-read-btn').click();
    await page.waitForFunction(() => document.getElementById('messaging-modal-status').textContent === 'Inbox marked as read.' &&
      document.querySelectorAll('.msg-notification-row--read').length === 1);
    await page.locator('[data-tab="chat"]').click();
    await page.locator('#msg-chat-recipient').fill(recipientId);
    const started = page.waitForResponse(response => new URL(response.url()).pathname === '/api/chat/start-new-chat-session');
    await page.locator('#msg-start-session-btn').click();
    const startResult = await (await started).json();
    assert.equal(startResult.isError, false);
    const sessionId = startResult.result;
    assert.equal(typeof sessionId, 'string');
    assert.ok(sessionId);
    await page.locator('#msg-chat-session-wrap').waitFor({state: 'visible'});
    await page.locator('#msg-chat-input').fill('Portal actual chat message');
    await page.locator('#msg-chat-send-btn').click();
    await page.waitForFunction(() => document.getElementById('msg-chat-messages').textContent.includes('Portal actual chat message'));
    await page.locator('#msg-end-session-btn').click();
    await page.waitForFunction(() => document.getElementById('messaging-modal-status').textContent === 'Chat session ended.' &&
      document.getElementById('msg-chat-session-wrap').hidden === true);
    await page.locator('#messaging-modal-close-btn').click();
    assert.equal(await page.locator('.js-modal').isVisible(), false);
    assert.equal(writes.length, 6);
    const direct = writes.find(write => write.path === '/api/messaging/send-message-to-avatar/' + recipientId);
    assert.equal(JSON.parse(direct.body), 'Portal actual direct message');
    const chat = writes.find(write => write.path === '/api/chat/send-message/' + sessionId);
    assert.equal(JSON.parse(chat.body), 'Portal actual chat message');
    assert.deepEqual(JSON.parse(writes.find(write => write.path === '/api/chat/start-new-chat-session').body),
      [avatarId, recipientId]);
    for (const route of ['mark-messages-read', 'mark-notifications-read'])
      assert.equal(JSON.parse(writes.find(write => write.path === '/api/messaging/' + route).body).length, 1);
    assert.equal(writes.filter(write => write.path === '/api/chat/end-chat-session/' + sessionId).length, 1);
    assert.deepEqual(errors, []);
    assert.deepEqual(failedResponses, []);
    assert.deepEqual(unexpected, []);
    return {messagingPopupVerified: true, providerType, sessionId, directMessages: 1,
      readWrites: 2, chatStarts: 1, chatMessages: 1, chatEnds: 1};
  } catch (error) {
    if (page && !page.isClosed()) {
      const state = await page.evaluate(() => Object.fromEntries(
        ['messaging-modal-status', 'msg-conversations-list', 'msg-thread-status', 'msg-notifications-list', 'msg-chat-messages']
          .map(id => [id, (document.getElementById(id)?.textContent || '').slice(0, 500)])));
      console.error(JSON.stringify({state, failedResponses, writes: writes.map(write => ({method: write.method, path: write.path}))}));
    }
    throw error;
  } finally { await context.close(); }
}
module.exports = {verify};
