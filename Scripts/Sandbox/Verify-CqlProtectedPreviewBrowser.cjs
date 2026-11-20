/* Read-only protected-preview probe. Uses installed Chrome and an explicitly
 * supplied existing ws module; installs nothing and blocks backend requests. */
'use strict';
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { spawn } = require('node:child_process');
const root = path.resolve(__dirname, '../..');
const parent = path.join(root, 'LocalState/COSMIC');
const work = path.join(parent, 'RecoveryLaptopHostedBrowserCurrent');
const profile = path.join(work, 'profile');
const marker = path.join(work, 'owner.json');
const ledger = path.join(root, 'Docs/WIP/COSMIC ORM/RECOVERY_LAPTOP_CLEANUP_LEDGER.md');
const origin = 'https://web4-site-kpgpby09s-oasis-team.vercel.app';
const url = origin + '/web4-sandbox.html';
const owner = 'cosmic-cql-protected-preview-readonly';
const token = process.env.VERCEL_OIDC_TOKEN;
if (!token) throw new Error('Project-scoped development OIDC token is required.');
if (!process.env.CQL_BROWSER_WEBSOCKET_MODULE) throw new Error('Supply the verified existing ws module path.');
const WebSocket = require(process.env.CQL_BROWSER_WEBSOCKET_MODULE);
if (!fs.realpathSync(parent).toLowerCase().startsWith(fs.realpathSync(root).toLowerCase() + path.sep)) {
  throw new Error('Browser work parent resolves outside the workspace.');
}
if (fs.existsSync(work)) throw new Error('Owned browser work directory already exists; reconcile its ownership/process before reuse.');
fs.appendFileSync(ledger, '\nPROTECTED-PREVIEW-BROWSER: owned ' + work +
  '; existing Chrome/ws, no install. Block all backend requests. Remove profile after owned Chrome exits; retain preview.png only until visual review, then remove exact owned directory. OIDC header stays in memory; no token in arguments/files/reports.\n');
fs.mkdirSync(work);
fs.writeFileSync(marker, JSON.stringify({ owner, work, profile, createdUtc: new Date().toISOString(), pid: null }, null, 2));
const osEnvironment = {};
for (const key of ['SystemRoot', 'SystemDrive', 'WINDIR', 'COMSPEC', 'PATH', 'PATHEXT', 'TEMP', 'TMP', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA', 'PROGRAMFILES', 'PROGRAMFILES(X86)']) {
  if (process.env[key]) osEnvironment[key] = process.env[key];
}
const browser = spawn('C:/Program Files/Google/Chrome/Application/chrome.exe', [
  '--headless=new', '--incognito', '--disable-gpu', '--renderer-process-limit=2',
  '--no-first-run', '--no-default-browser-check', '--disable-background-networking',
  '--disable-component-update', '--disable-sync', '--disable-extensions',
  '--remote-debugging-address=127.0.0.1', '--remote-debugging-port=0',
  '--user-data-dir=' + profile, 'about:blank'
], { env: osEnvironment, windowsHide: true, stdio: ['ignore', 'ignore', 'pipe'] });
fs.writeFileSync(marker, JSON.stringify({ owner, work, profile, pid: browser.pid, createdUtc: new Date().toISOString() }, null, 2));
let socket, sequence = 0;
const pending = new Map(), ready = new Map(), readyWaiters = new Map(), loadWaiters = new Map();
const blocked = [], exceptions = [], protocolErrors = [];
const report = { owner, url, applicationDataRequestsSent: 0, filesInstalled: 0, configurationWrites: 0 };
function send(method, params = {}, sessionId) {
  const id = ++sequence;
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error('CDP deadline: ' + method)); }, 15000);
    pending.set(id, { resolve, reject, timer });
    socket.send(JSON.stringify({ id, method, params, ...(sessionId ? { sessionId } : {}) }));
  });
}
function forTarget(id) {
  if (ready.has(id)) return Promise.resolve(ready.get(id));
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { readyWaiters.delete(id); reject(new Error('Guarded target readiness deadline.')); }, 15000);
    readyWaiters.set(id, value => { clearTimeout(timer); resolve(value); });
  });
}
async function event(message) {
  const p = message.params || {};
  if (message.method === 'Target.attachedToTarget') {
    if (p.targetInfo.type !== 'page') {
      await send('Target.closeTarget', { targetId: p.targetInfo.targetId });
      return;
    }
    await send('Fetch.enable', { patterns: [{ urlPattern: '*', requestStage: 'Request' }] }, p.sessionId);
    await send('Runtime.enable', {}, p.sessionId);
    await send('Page.enable', {}, p.sessionId);
    await send('Emulation.setDeviceMetricsOverride', { width: 1280, height: 900, deviceScaleFactor: 1, mobile: false }, p.sessionId);
    ready.set(p.targetInfo.targetId, p.sessionId);
    if (readyWaiters.has(p.targetInfo.targetId)) { readyWaiters.get(p.targetInfo.targetId)(p.sessionId); readyWaiters.delete(p.targetInfo.targetId); }
    await send('Runtime.runIfWaitingForDebugger', {}, p.sessionId);
  } else if (message.method === 'Fetch.requestPaused') {
    const request = p.request, target = new URL(request.url);
    const staticPath = /\.(?:html|js|mjs|css|png|jpg|jpeg|svg|ico|woff2?|ttf)$/i.test(target.pathname);
    const read = request.method === 'GET' || request.method === 'HEAD';
    const ownStatic = target.origin === origin && staticPath && !/^\/(?:api|v1)\//.test(target.pathname);
    const font = ['fonts.googleapis.com', 'fonts.gstatic.com'].includes(target.hostname);
    if (read && target.protocol === 'https:' && (ownStatic || font)) {
      const headers = Object.entries(request.headers).filter(([name]) => name.toLowerCase() !== 'x-vercel-trusted-oidc-idp-token')
        .map(([name, value]) => ({ name, value: String(value) }));
      if (ownStatic) headers.push({ name: 'x-vercel-trusted-oidc-idp-token', value: token });
      await send('Fetch.continueRequest', { requestId: p.requestId, headers }, message.sessionId);
    } else {
      blocked.push({ method: request.method, origin: target.origin, path: target.pathname.slice(0, 180) });
      await send('Fetch.failRequest', { requestId: p.requestId, errorReason: 'BlockedByClient' }, message.sessionId);
    }
  } else if (message.method === 'Runtime.exceptionThrown') {
    exceptions.push({ line: p.exceptionDetails?.lineNumber, type: p.exceptionDetails?.exception?.className || 'ScriptException' });
  } else if (message.method === 'Page.loadEventFired' && loadWaiters.has(message.sessionId)) {
    loadWaiters.get(message.sessionId)(); loadWaiters.delete(message.sessionId);
  }
}
(async () => {
  try {
    const endpoint = await new Promise((resolve, reject) => {
      let output = '';
      const timer = setTimeout(() => reject(new Error('Owned Chrome startup deadline.')), 15000);
      browser.once('error', error => { clearTimeout(timer); reject(error); });
      browser.stderr.on('data', value => {
        output = (output + value.toString()).slice(-12000);
        const match = output.match(/DevTools listening on (ws:\/\/127\.0\.0\.1:\d+\/[^\s]+)/);
        if (match) { clearTimeout(timer); resolve(match[1]); }
      });
    });
    socket = new WebSocket(endpoint);
    await new Promise((resolve, reject) => { socket.once('open', resolve); socket.once('error', reject); });
    socket.on('close', () => {
      for (const waiter of pending.values()) { clearTimeout(waiter.timer); waiter.reject(new Error('CDP transport closed.')); }
      pending.clear();
    });
    socket.on('message', raw => {
      const message = JSON.parse(raw.toString());
      if (message.id && pending.has(message.id)) {
        const waiter = pending.get(message.id); pending.delete(message.id); clearTimeout(waiter.timer);
        if (message.error) waiter.reject(new Error('CDP ' + message.error.code)); else waiter.resolve(message.result);
      } else event(message).then(undefined, error => { protocolErrors.push(error.message); });
    });
    await send('Target.setAutoAttach', { autoAttach: true, waitForDebuggerOnStart: true, flatten: true });
    const context = await send('Target.createBrowserContext', { disposeOnDetach: true });
    const target = await send('Target.createTarget', { url: 'about:blank', browserContextId: context.browserContextId });
    const session = await forTarget(target.targetId);
    const loaded = new Promise((resolve, reject) => {
      const timer = setTimeout(() => { loadWaiters.delete(session); reject(new Error('Preview load deadline.')); }, 25000);
      loadWaiters.set(session, () => { clearTimeout(timer); resolve(); });
    });
    await send('Page.navigate', { url }, session);
    await loaded;
    const result = await send('Runtime.evaluate', { expression: `JSON.stringify({title:document.title,ready:document.readyState,ask:document.body.innerText.toLowerCase().includes('ask your data'),virtual:document.body.innerText.toLowerCase().includes('virtual'),editorControls:[...document.querySelectorAll('textarea,select,input')].map(e=>({tag:e.tagName,id:e.id,type:e.type})).filter(e=>/cql|sql|ask|mapping|virtual/i.test(e.id)).slice(0,40)})`, returnByValue: true }, session);
    if (result.exceptionDetails) throw new Error('DOM inspection failed.');
    Object.assign(report, JSON.parse(result.result.value));
    const screenshot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false }, session);
    const png = Buffer.from(screenshot.data, 'base64');
    fs.writeFileSync(path.join(work, 'preview.png'), png);
    report.screenshot = path.join(work, 'preview.png'); report.screenshotBytes = png.length;
    report.screenshotSha256 = crypto.createHash('sha256').update(png).digest('hex');
    report.blockedRequests = blocked; report.scriptExceptions = exceptions; report.protocolErrors = protocolErrors;
    report.renderedProbe = true; report.fullBrowserDataFlowVerified = false;
  } catch (error) {
    report.error = error.message; process.exitCode = 1;
  } finally {
    if (socket && socket.readyState === WebSocket.OPEN) {
      try { await send('Browser.close'); } catch (error) { report.closeTransport = error.message; }
      socket.terminate();
    }
    if (browser.exitCode === null && browser.signalCode === null) {
      await new Promise(resolve => { const timer = setTimeout(resolve, 5000); browser.once('exit', () => { clearTimeout(timer); resolve(); }); });
    }
    if (browser.exitCode === null && browser.signalCode === null) {
      const exited = new Promise(resolve => {
        const timer = setTimeout(resolve, 5000);
        browser.once('exit', () => { clearTimeout(timer); resolve(); });
      });
      browser.kill(); report.ownedBrowserForcedStop = true;
      await exited;
    }
    report.ownedBrowserStopped = browser.exitCode !== null || browser.signalCode !== null;
    if (report.ownedBrowserStopped) browser.stderr.destroy();
    const ownership = JSON.parse(fs.readFileSync(marker, 'utf8'));
    if (report.ownedBrowserStopped && ownership.owner === owner && path.resolve(ownership.work) === work && fs.realpathSync(work).toLowerCase().startsWith(fs.realpathSync(parent).toLowerCase() + path.sep)) {
      try { if (fs.existsSync(profile)) fs.rmSync(profile, { recursive: true }); report.profileAbsent = !fs.existsSync(profile); }
      catch (error) { report.profileCleanupError = error.code; }
    }
    fs.appendFileSync(ledger, '\nPROTECTED-PREVIEW-BROWSER-OUTCOME: ' + JSON.stringify({ work, browserStopped: report.ownedBrowserStopped, profileAbsent: report.profileAbsent, cleanupError: report.profileCleanupError, screenshot: report.screenshot, screenshotBytes: report.screenshotBytes, retainedForVisualReview: true }) + '\n');
    console.log('CQL_BROWSER_JSON=' + JSON.stringify(report));
  }
})();
