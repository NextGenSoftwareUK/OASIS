'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID } = require('node:crypto');

async function verify({ baseUrl, token, providerType, imageId }) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.equal(base.protocol, 'http:');
  assert.ok(Number.isInteger(imageId) && imageId >= 1 && imageId <= 3);
  const { OASISClient } = require(process.env.OASIS_PROFILE_WEB4_SDK_ROOT);
  const client = new OASISClient({ baseUrl, persistSession: false });
  const initial = await client.profile.getProfile({ providerType, token });
  assert.equal(initial.isError, false, initial.message);
  const owner = initial.result.profile;
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const writes = [], errors = [], forbidden = [];
  let phase = 'launch';
  try {
    await context.route('**/*', route => {
      const url = new URL(route.request().url());
      if (url.origin === base.origin || url.protocol === 'data:') return route.continue();
      if (url.pathname.startsWith('/api/')) forbidden.push(url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(({ owner, token }) => {
      if (!localStorage.getItem('avatar')) {
        localStorage.setItem('avatar', JSON.stringify({ id: owner.avatarId, username: owner.username,
          email: owner.email, firstName: owner.firstName, lastName: owner.lastName, jwtToken: token }));
        localStorage.setItem('loggedIn', 'true');
      }
    }, { owner, token });
    const page = await context.newPage(); page.setDefaultTimeout(10000);
    page.on('pageerror', error => errors.push(error.message));
    page.on('request', request => {
      if (new URL(request.url()).pathname.startsWith('/api/') && request.method() !== 'GET')
        writes.push({ method: request.method(), path: new URL(request.url()).pathname, body: request.postDataJSON() });
    });
    await page.goto(baseUrl + '/__profile_browser', { waitUntil: 'domcontentloaded' });
    async function open() {
      const [value] = await Promise.all([
        page.waitForResponse(value => new URL(value.url()).pathname === '/api/avatar/profile' && value.request().method() === 'GET'),
        page.locator('#fixture-open-profile').click()
      ]);
      assert.equal(value.status(), 200);
      const result = await value.json();
      assert.equal(result.isError, false);
      assert.equal(result.result.providerType, providerType);
      await page.waitForFunction(() => !document.getElementById('av-title').disabled);
    }
    phase = 'open-actual-profile';
    await open();
    const imageBase64 = await page.evaluate(imageId => {
      const canvas = document.createElement('canvas'); canvas.width = 2; canvas.height = 2;
      const brush = canvas.getContext('2d');
      brush.fillStyle = 'rgb(' + (imageId * 47) + ',91,163)'; brush.fillRect(0, 0, 2, 2);
      return canvas.toDataURL('image/png').split(',')[1];
    }, imageId);
    phase = 'actual-file-upload';
    const [uploaded] = await Promise.all([
      page.waitForResponse(value => new URL(value.url()).pathname === '/api/avatar/upload-avatar-portrait' && value.request().method() === 'POST'),
      page.locator('#avatar-photo-input').setInputFiles({ name: 'portrait.png', mimeType: 'image/png',
        buffer: Buffer.from(imageBase64, 'base64') })
    ]);
    assert.equal(uploaded.status(), 200);
    const confirmation = await uploaded.json();
    assert.equal(confirmation.isError, false, confirmation.message);
    assert.equal(confirmation.result, true);
    assert.equal(confirmation.isSaved, true);
    await page.waitForFunction(() => document.getElementById('avatar-modal-status').className.includes('--success'));
    assert.equal(writes.length, 1, 'One selected image must send one upload, without profile or legacy writes.');
    assert.equal(writes[0].method, 'POST'); assert.equal(writes[0].path, '/api/avatar/upload-avatar-portrait');
    assert.equal(writes[0].body.avatarId, owner.avatarId);
    assert.equal(writes[0].body.username, owner.username); assert.equal(writes[0].body.email, owner.email);
    assert.equal(writes[0].body.imageBase64, imageBase64);
    phase = 'sdk-read';
    client.setToken(token, { avatarId: owner.avatarId });
    const stored = await client.avatar.getAvatarPortraitByUsername({ username: owner.username });
    assert.equal(stored.isError, false, stored.message);
    assert.equal(stored.result.imageBase64, imageBase64);
    phase = 'reload-without-cached-image';
    await page.evaluate(() => {
      const avatar = JSON.parse(localStorage.getItem('avatar'));
      for (const key of ['avatarImage', 'AvatarImage', 'profileImage', 'ProfileImage', 'photo', 'Photo']) delete avatar[key];
      localStorage.setItem('avatar', JSON.stringify(avatar));
    });
    await page.reload({ waitUntil: 'domcontentloaded' });
    const [reloaded] = await Promise.all([
      page.waitForResponse(value => new URL(value.url()).pathname.startsWith('/api/avatar/get-avatar-portrait-by-username/')),
      open()
    ]);
    assert.equal(reloaded.status(), 200);
    const image = await reloaded.json(); assert.equal(image.isError, false, image.message);
    assert.equal(image.result.imageBase64, imageBase64);
    await page.waitForFunction(imageBase64 => document.getElementById('avatar-modal-image-large').src ===
      'data:image/png;base64,' + imageBase64, imageBase64);
    assert.equal(writes.length, 1); assert.deepEqual(errors, []); assert.deepEqual(forbidden, []);
    return { portraitBrowserVerified: true, providerType, imageBase64, uploadRequests: 1,
      actualFragmentVerified: true, actualSdkHttpVerified: true, reloadWithoutCachedImageVerified: true,
      accountSwitchVerified: false, concurrentNativeUpdateVerified: false, productionStartupVerified: false };
  } catch (error) {
    throw new Error('Portrait browser ' + providerType + '/' + phase + ': ' + error.message);
  } finally { await context.close(); }
}
module.exports = { verify };
