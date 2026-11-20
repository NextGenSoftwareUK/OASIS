'use strict';
const assert = require('node:assert/strict');
const path = require('node:path');
const { randomUUID, createHash } = require('node:crypto');

async function verify({ baseUrl, token, providerType, imageId, accountSwitch = false, foreignToken, editAfterPortrait = false, replaceClientWhileProfilePending = false }) {
  const base = new URL(baseUrl);
  assert.equal(base.hostname, '127.0.0.1');
  assert.equal(base.protocol, 'http:');
  assert.ok(Number.isInteger(imageId) && imageId >= 1 && imageId <= 3);
  const { OASISClient } = require(process.env.OASIS_PROFILE_WEB4_SDK_ROOT);
  const client = new OASISClient({ baseUrl, persistSession: false });
  const initial = await client.profile.getProfile({ providerType, token });
  assert.equal(initial.isError, false, initial.message);
  const owner = initial.result.profile;
  let foreign;
  if (accountSwitch) {
    assert.equal(typeof foreignToken, 'string');
    const response = await client.profile.getProfile({ providerType, token: foreignToken });
    assert.equal(response.isError, false, response.message);
    foreign = response.result.profile;
    assert.notEqual(foreign.avatarId, owner.avatarId);
  }
  const { chromium } = require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME, 'node_modules/playwright-core'));
  const context = await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-' + randomUUID()), { channel: 'chrome', headless: true, timeout: 30000,
    viewport: { width: 1440, height: 1000 }, reducedMotion: 'reduce', acceptDownloads: false, serviceWorkers: 'block' });
  const writes = [], errors = [], forbidden = [];
  let releaseUpload;
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
    if (replaceClientWhileProfilePending) {
      phase = 'replace-sdk-client-during-real-profile-read';
      await page.locator('#avatar-modal-close-btn').click();
      await page.evaluate(() => {
        const original = window.oasisClient.profile.getProfile.bind(window.oasisClient.profile);
        window.oldProfileReadSettled = false;
        window.oldProfilePublications = [];
        window.addEventListener('avatarUpdated', event => window.oldProfilePublications.push(event.detail));
        window.oasisClient.profile.getProfile = function (...args) {
          const pending = original(...args);
          const finished = () => {
            const channel = new MessageChannel();
            channel.port1.onmessage = () => {
              window.oldProfileReadSettled = true;
              channel.port1.close(); channel.port2.close();
            };
            channel.port2.postMessage(null);
          };
          pending.then(finished, finished);
          return pending;
        };
      });
      const gate = new Promise(resolve => { releaseUpload = resolve; });
      const isProfileRead = value => new URL(value.url()).pathname === '/api/avatar/profile' && value.method() === 'GET';
      const holdRead = async route => { await gate; await route.continue(); };
      await page.route('**/api/avatar/profile', holdRead);
      await Promise.all([
        page.waitForRequest(isProfileRead), page.locator('#fixture-open-profile').click()
      ]);
      const storedBefore = await page.evaluate(() => localStorage.getItem('avatar'));
      await page.evaluate(({ token, id }) => {
        window.oasisClient = new window.OASISClient({ baseUrl: location.origin, persistSession: false });
        window.oasisClient.setToken(token, { avatarId: id });
      }, { token, id: owner.avatarId });
      releaseUpload();
      await page.waitForFunction(() => window.oldProfileReadSettled);
      await page.unroute('**/api/avatar/profile', holdRead);
      assert.equal(await page.locator('#av-title').isDisabled(), true);
      assert.equal(await page.locator('#avatar-modal-save-btn').isDisabled(), true);
      assert.equal(await page.evaluate(() => localStorage.getItem('avatar')), storedBefore);
      assert.deepEqual(await page.evaluate(() => window.oldProfilePublications), []);
      assert.equal(writes.length, 0);
      phase = 'reopen-with-genuine-replacement-sdk';
      await page.locator('#avatar-modal-close-btn').click();
      await open();
    }
    const imageBase64 = await page.evaluate(imageId => {
      const canvas = document.createElement('canvas'); canvas.width = 2; canvas.height = 2;
      const brush = canvas.getContext('2d');
      brush.fillStyle = 'rgb(' + (imageId * 47) + ',91,163)'; brush.fillRect(0, 0, 2, 2);
      return canvas.toDataURL('image/png').split(',')[1];
    }, imageId);
    if (accountSwitch) {
      phase = 'hold-original-upload';
      await page.evaluate(() => {
        window.portraitOriginalUploadSettled = false;
        window.portraitOriginalUploadAcknowledged = false;
        const http = window.oasisClient.http;
        const request = http.request;
        http.request = function (...args) {
          const actual = request.apply(this, args);
          if (args[0] === 'POST' && args[1] === 'api/avatar/upload-avatar-portrait') {
            const observed = acknowledged => {
              // A posted-message task follows the real promise's await continuations.
              // Return the identical shipping promise; never replace a response or transport.
              const channel = new MessageChannel();
              channel.port1.onmessage = () => {
                window.portraitOriginalUploadAcknowledged = acknowledged;
                window.portraitOriginalUploadSettled = true;
                channel.port1.close(); channel.port2.close();
              };
              channel.port2.postMessage(null);
            };
            actual.then(response => observed(response.isError === false && response.result === true), () => observed(false));
          }
          return actual;
        };
      });
      const gate = new Promise(resolve => { releaseUpload = resolve; });
      const match = url => url.pathname === '/api/avatar/upload-avatar-portrait';
      let originalBearerVerified = false;
      await page.route(match, async route => {
        originalBearerVerified = route.request().headers().authorization === 'Bearer ' + token;
        await gate;
        await route.continue();
      });
      // Keep the actual response pending without synthesizing a body or a native result.
      const completion = page.waitForResponse(value => match(new URL(value.url())) && value.request().method() === 'POST')
        .then(async response => {
          assert.equal(response.status(), 200);
          const result = (await response.json()).result;
          assert.ok(result && typeof result === 'object');
          assert.equal(result.isError, false, result.message);
          assert.equal(result.result, true); assert.equal(result.isSaved, true);
          return { ok: true };
        }).catch(error => ({ ok: false, message: error.message }));
      await Promise.all([
        page.waitForRequest(value => match(new URL(value.url())) && value.method() === 'POST'),
        page.locator('#avatar-photo-input').setInputFiles({ name: 'portrait.png', mimeType: 'image/png',
          buffer: Buffer.from(imageBase64, 'base64') })
      ]);
      phase = 'switch-to-real-second-account';
      await page.locator('#avatar-modal-close-btn').click();
      await page.evaluate(({ foreign, foreignToken }) => {
        window.portraitSwitchUpdates = [];
        window.addEventListener('avatarUpdated', event => window.portraitSwitchUpdates.push(event.detail));
        const profile = { id: foreign.avatarId, username: foreign.username, email: foreign.email,
          firstName: foreign.firstName, lastName: foreign.lastName, jwtToken: foreignToken };
        window.oasisClient.setToken(foreignToken, { avatarId: foreign.avatarId });
        localStorage.setItem('avatar', JSON.stringify(profile));
        localStorage.setItem('loggedIn', 'true'); window.user = profile;
      }, { foreign, foreignToken });
      await Promise.all([
        page.waitForResponse(value => new URL(value.url()).pathname ===
          '/api/avatar/get-avatar-portrait-by-username/' + encodeURIComponent(foreign.username)),
        open()
      ]);
      await page.waitForFunction(() => !document.getElementById('avatar-modal-image-large').src.startsWith('data:'));
      const foreignImage = await page.locator('#avatar-modal-image-large').getAttribute('src');
      const foreignStatus = await page.locator('#avatar-modal-status').textContent();
      phase = 'release-original-authenticated-request';
      releaseUpload();
      const finished = await completion;
      assert.equal(finished.ok, true, finished.message);
      assert.ok(originalBearerVerified, 'Queued request must retain its original signed bearer.');
      await page.waitForFunction(() => window.portraitOriginalUploadSettled);
      assert.equal(await page.evaluate(() => window.portraitOriginalUploadAcknowledged), true);
      await page.waitForFunction(({ foreign, imageBase64, foreignImage, foreignStatus }) => {
        const current = JSON.parse(localStorage.getItem('avatar'));
        return current.id === foreign.avatarId && !String(current.avatarImage || current.AvatarImage || '').includes(imageBase64) &&
          document.getElementById('avatar-modal-image-large').getAttribute('src') === foreignImage &&
          document.getElementById('avatar-modal-status').textContent === foreignStatus;
      }, { foreign, imageBase64, foreignImage, foreignStatus });
      assert.equal(writes.length, 1);
      assert.equal(writes[0].body.avatarId, owner.avatarId);
      assert.equal(writes[0].body.username, owner.username); assert.equal(writes[0].body.email, owner.email);
      assert.equal(writes[0].body.imageBase64, imageBase64);
      const updates = await page.evaluate(() => window.portraitSwitchUpdates.map(value => ({
        id: value.id || value.Id || value.avatarId || value.AvatarId,
        image: value.avatarImage || value.AvatarImage || ''
      })));
      assert.ok(updates.every(value => value.id === foreign.avatarId && !value.image.includes(imageBase64)));
      assert.deepEqual(errors, []); assert.deepEqual(forbidden, []);
      return { portraitBrowserVerified: true, providerType, imageBase64, uploadRequests: 1,
        actualFragmentVerified: true, actualSdkHttpVerified: true, reloadWithoutCachedImageVerified: false,
        accountSwitchVerified: true, queuedOriginalUploadVerified: true, oldAvatarUiPublicationSuppressedVerified: true,
        concurrentNativeUpdateVerified: false, productionStartupVerified: false };
    }
    phase = 'actual-file-upload';
    const [uploaded] = await Promise.all([
      page.waitForResponse(value => new URL(value.url()).pathname === '/api/avatar/upload-avatar-portrait' && value.request().method() === 'POST'),
      page.locator('#avatar-photo-input').setInputFiles({ name: 'portrait.png', mimeType: 'image/png',
        buffer: Buffer.from(imageBase64, 'base64') })
    ]);
    assert.equal(uploaded.status(), 200);
    // Legacy AvatarProfileController returns OASISHttpResponseMessage<bool>.Result.
    const confirmation = (await uploaded.json()).result;
    assert.ok(confirmation && typeof confirmation === 'object');
    assert.equal(confirmation.isError, false, confirmation.message);
    assert.equal(confirmation.result, true);
    assert.equal(confirmation.isSaved, true);
    await page.waitForFunction(() => document.getElementById('avatar-modal-status').className.includes('--success'));
    assert.equal(writes.length, 1, 'One selected image must send one upload, without profile or legacy writes.');
    assert.equal(writes[0].method, 'POST'); assert.equal(writes[0].path, '/api/avatar/upload-avatar-portrait');
    assert.equal(writes[0].body.avatarId, owner.avatarId);
    assert.equal(writes[0].body.username, owner.username); assert.equal(writes[0].body.email, owner.email);
    assert.equal(writes[0].body.imageBase64, imageBase64);
    if (editAfterPortrait) {
      phase = 'edit-with-acknowledged-portrait-revision';
      const commit = JSON.parse(confirmation.metaData.avatarProfileCommit);
      assert.equal(commit.providerType, providerType);
      assert.equal(commit.profile.avatarId, owner.avatarId);
      assert.equal(commit.profile.portraitSha256, createHash('sha256').update(imageBase64, 'utf8').digest('base64'));
      assert.notEqual(commit.profile.detailRevision.versionId, owner.detailRevision.versionId);
      assert.equal(Object.hasOwn(commit.profile, 'portrait'), false);
      const newAddress = 'After portrait ' + providerType;
      await page.locator('#av-address').fill(newAddress);
      const [saved] = await Promise.all([
        page.waitForResponse(value => new URL(value.url()).pathname === '/api/avatar/profile' && value.request().method() === 'PATCH'),
        page.locator('#avatar-modal-save-btn').click()
      ]);
      assert.equal(saved.status(), 200);
      const response = await saved.json();
      assert.equal(response.isError, false);
      assert.equal(response.result.profile.address, newAddress);
      assert.equal(writes.length, 2);
      assert.equal(writes[1].method, 'PATCH');
      assert.deepEqual(writes[1].body.detailRevision, commit.profile.detailRevision);
      assert.deepEqual(writes[1].body.avatarRevision, commit.profile.avatarRevision);
      assert.deepEqual(writes[1].body.changes, { address: newAddress });
      await page.waitForFunction(() => !document.getElementById('avatar-modal-save-btn').disabled);
      client.setToken(token, { avatarId: owner.avatarId });
      const current = await client.profile.getProfile({ providerType, token });
      assert.equal(current.isError, false, current.message);
      assert.equal(current.result.profile.address, newAddress);
      const portrait = await client.avatar.getAvatarPortraitByUsername({ username: owner.username });
      assert.equal(portrait.isError, false, portrait.message);
      assert.equal(portrait.result.imageBase64, imageBase64);
      assert.deepEqual(errors, []); assert.deepEqual(forbidden, []);
      return { portraitBrowserVerified: true, providerType, imageBase64, uploadRequests: 1,
        editAfterPortraitVerified: true, newAddress, profilePatchRequests: 1,
        actualFragmentVerified: true, actualSdkHttpVerified: true,
        concurrentNativeUpdateVerified: false, productionStartupVerified: false };
    }
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
    const image = (await reloaded.json()).result;
    assert.ok(image && typeof image === 'object');
    assert.equal(image.isError, false, image.message);
    assert.equal(image.result.imageBase64, imageBase64);
    await page.waitForFunction(imageBase64 => document.getElementById('avatar-modal-image-large').src ===
      'data:image/png;base64,' + imageBase64, imageBase64);
    assert.equal(writes.length, 1); assert.deepEqual(errors, []); assert.deepEqual(forbidden, []);
    return { portraitBrowserVerified: true, providerType, imageBase64, uploadRequests: 1,
      actualFragmentVerified: true, actualSdkHttpVerified: true, reloadWithoutCachedImageVerified: true,
      sameBearerClientReplacementVerified: replaceClientWhileProfilePending,
      accountSwitchVerified: false, concurrentNativeUpdateVerified: false, productionStartupVerified: false };
  } catch (error) {
    throw new Error('Portrait browser ' + providerType + '/' + phase + ': ' + error.message);
  } finally {
    if (releaseUpload) releaseUpload();
    await context.close();
  }
}
module.exports = { verify };
