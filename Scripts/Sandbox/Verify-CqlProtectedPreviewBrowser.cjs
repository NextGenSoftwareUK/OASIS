/* Protected-preview probe using installed Chrome/ws. Default blocks backends;
 * explicit SQL mode permits one demo session, discovery, three reads and revoke. */
'use strict';
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const { spawn } = require('node:child_process');
const https = require('node:https');
const root = path.resolve(__dirname, '../..');
const parent = path.join(root, 'LocalState/COSMIC');
const work = path.join(parent, 'RecoveryLaptopHostedBrowserCurrent');
const profile = path.join(work, 'profile');
const marker = path.join(work, 'owner.json');
const ledger = path.join(root, 'Docs/WIP/COSMIC ORM/RECOVERY_LAPTOP_CLEANUP_LEDGER.md');
const previewOrigin = 'https://web4-site-hzn75ad0v-oasis-team.vercel.app';
const candidateWebsiteSource = 'bf795c7763a827aafb0380f8b11f559f36f66f8c';
const expectedAssetHashes = {
  "/cql-text.js": "8051309d023f5ac61ba37fbf8af69a9a315f7fe7d5a56e52093c6ee96a27ff18",
  "/cql-playground.js": "331918b92ba02e7cf733b7b6213fd7ee1466af960beaed57c9eb3049b9dbb4e0"
};
const sqlFlow = process.env.CQL_BROWSER_SQL_FLOW === '1';
const selectedProviders = (process.env.CQL_BROWSER_PROVIDERS || 'MongoDBOASIS,SQLLiteDBOASIS,Neo4jOASIS').split(',');
if (new Set(selectedProviders).size !== selectedProviders.length || selectedProviders.some(provider =>
    !['MongoDBOASIS', 'SQLLiteDBOASIS', 'Neo4jOASIS'].includes(provider))) throw new Error('Select one to three distinct admitted SQL providers.');
const origin = sqlFlow ? 'https://web4.oasisomniverse.one' : previewOrigin;
const url = origin + '/web4-sandbox.html';
const owner = 'cosmic-cql-protected-preview-readonly';
const token = process.env.VERCEL_OIDC_TOKEN;
if (!token) throw new Error('Project-scoped development OIDC token is required.');
if (!process.env.CQL_BROWSER_WEBSOCKET_MODULE) throw new Error('Supply the verified existing ws module path.');
const WebSocket = require(process.env.CQL_BROWSER_WEBSOCKET_MODULE);
if (!fs.realpathSync(parent).toLowerCase().startsWith(fs.realpathSync(root).toLowerCase() + path.sep)) {
  throw new Error('Browser work parent resolves outside the workspace.');
}
if (fs.existsSync(work)) {
  const prior = JSON.parse(fs.readFileSync(marker, 'utf8'));
  if (prior.owner !== owner || path.resolve(prior.work) !== work || path.resolve(prior.profile) !== profile)
    throw new Error('Existing browser work ownership mismatch.');
  if (prior.pid) {
    try { process.kill(prior.pid, 0); throw new Error('Previous owned browser PID is still present.'); }
    catch (error) { if (error.code !== 'ESRCH') throw error; }
  }
} else fs.mkdirSync(work);
fs.appendFileSync(ledger, '\nPROTECTED-PREVIEW-BROWSER: owned ' + work +
  '; existing Chrome/ws, no install. SQL mode=' + sqlFlow + '. Reuse only after prior owned PID is absent; remove only the owned profile after verified browser exit. OIDC header stays in memory; no token in arguments/files/reports.\n');
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
const assets = new Map(), apiCounts = new Map(), scalarResponses = [];
const scalarValues = []; // Compare rendered output in memory; do not include raw values in reports.
const assetHashes = new Map();
const scalarRequests = new Set();
const completedSqlProviders = [];
const authRequests = new Map(), authResponses = [];
const authTransportRequests = new Map();
const catalogueRequests = new Set();
let neoDiscovery;
let pageSession;
function candidateAsset(target) {
  const address = new URL(target.pathname + target.search, previewOrigin);
  if (!assets.has(address.href)) assets.set(address.href, new Promise((resolve, reject) => {
    const request = https.get(address, { headers: { 'x-vercel-trusted-oidc-idp-token': token } }, response => {
      if (response.statusCode !== 200) { response.resume(); reject(new Error('Candidate asset HTTP ' + response.statusCode)); return; }
      const chunks = []; let bytes = 0;
      response.on('data', chunk => { bytes += chunk.length; if (bytes > 2097152) request.destroy(new Error('Candidate asset exceeds bound.')); else chunks.push(chunk); });
      response.on('error', reject);
      response.on('end', () => {
        const body = Buffer.concat(chunks);
        const hash = crypto.createHash('sha256').update(body).digest('hex');
        if (expectedAssetHashes[address.pathname] && expectedAssetHashes[address.pathname] !== hash) {
          reject(new Error('Candidate source hash mismatch: ' + address.pathname)); return;
        }
        assetHashes.set(address.pathname, hash);
        resolve({ body, type: response.headers['content-type'] || 'application/octet-stream' });
      });
    });
    request.setTimeout(15000, () => request.destroy(new Error('Candidate asset deadline: ' + address.pathname)));
    request.on('error', reject);
  }));
  return assets.get(address.href);
}
async function waitForOwnedBrowserExit(timeout = 15000) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) {
    if (browser.exitCode !== null || browser.signalCode !== null) return true;
    try { process.kill(browser.pid, 0); }
    catch (error) { if (error.code === 'ESRCH') return true; throw error; }
    await new Promise(resolve => setTimeout(resolve, 250));
  }
  return browser.exitCode !== null || browser.signalCode !== null;
}
async function evaluate(expression) {
  const result = await send('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true }, pageSession);
  if (result.exceptionDetails) throw new Error('Browser scenario evaluation failed.');
  return result.result.value;
}
async function waitFor(check, label, timeout = 20000) {
  const deadline = Date.now() + timeout;
  while (Date.now() < deadline) { if (await check()) return; await new Promise(resolve => setTimeout(resolve, 250)); }
  throw new Error(label + ' deadline.');
}
function send(method, params = {}, sessionId) {
  const id = ++sequence;
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => { pending.delete(id); reject(new Error('CDP deadline: ' + method)); }, ['Runtime.evaluate', 'Page.navigate'].includes(method) ? 45000 : 15000);
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
    if (sqlFlow) await send('Network.enable', {}, p.sessionId);
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
    const apiLimits = { 'POST /api/sandbox/session': 1, 'POST /api/avatar/revoke-token': 1,
      'GET /api/data/query-cql/providers': 2, 'POST /api/data/query-cql/scalar': selectedProviders.length };
    const apiKey = request.method + ' ' + target.pathname;
    if (sqlFlow && target.origin === 'https://dev.api.web4.oasisomniverse.one' &&
        (apiLimits[apiKey] || request.method === 'OPTIONS' && Object.keys(apiLimits).some(key => key.endsWith(' ' + target.pathname)))) {
      if (request.method !== 'OPTIONS') {
        const count = (apiCounts.get(apiKey) || 0) + 1;
        if (count > apiLimits[apiKey]) throw new Error('Live browser request cap exceeded.');
        apiCounts.set(apiKey, count);
        if (target.pathname === '/api/data/query-cql/scalar') report.applicationDataRequestsSent++;
      }
      await send('Fetch.continueRequest', { requestId: p.requestId }, message.sessionId);
    } else if (sqlFlow && read && ownStatic) {
      const asset = await candidateAsset(target);
      await send('Fetch.fulfillRequest', { requestId: p.requestId, responseCode: 200,
        responseHeaders: [{ name: 'Content-Type', value: asset.type }, { name: 'Cache-Control', value: 'no-store' }],
        body: asset.body.toString('base64') }, message.sessionId);
    } else if (read && target.protocol === 'https:' && (ownStatic || font)) {
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
  } else if (sqlFlow && message.method === 'Network.requestWillBeSent' &&
      ['https://dev.api.web4.oasisomniverse.one/api/sandbox/session', 'https://dev.api.web4.oasisomniverse.one/api/avatar/revoke-token'].includes(p.request.url)) {
    authTransportRequests.set(p.requestId, new URL(p.request.url).pathname);
  } else if (sqlFlow && message.method === 'Network.loadingFailed' && authTransportRequests.has(p.requestId)) {
    authResponses.push({ path: authTransportRequests.get(p.requestId), transportFailed: true,
      transportError: /^net::ERR_[A-Z0-9_]+$/.test(p.errorText || '') ? p.errorText : 'browser-request-failed' });
    authTransportRequests.delete(p.requestId);
  } else if (sqlFlow && message.method === 'Network.responseReceived' &&
      p.response.url.startsWith('https://dev.api.web4.oasisomniverse.one/api/data/query-cql/providers?')) {
    catalogueRequests.add(p.requestId);
  } else if (sqlFlow && message.method === 'Network.loadingFinished' && catalogueRequests.delete(p.requestId)) {
    const response = await send('Network.getResponseBody', { requestId: p.requestId }, message.sessionId);
    const payload = JSON.parse(response.base64Encoded ? Buffer.from(response.body, 'base64').toString('utf8') : response.body);
    if (!Array.isArray(payload.result)) throw new Error('Unexpected provider catalogue contract.');
    const neo = payload.result.find(row => row.provider === 'Neo4jOASIS');
    if (neo) neoDiscovery = { provider: neo.provider, available: neo.available,
      queryOnly: neo.queryOnly, federationAvailable: neo.federationAvailable,
      activationGeneration: neo.activationGeneration,
      diagnosticCodes: Object.values(neo).filter(value => typeof value === 'string' && /^CQL_[A-Z0-9_]{1,100}$/.test(value)) };
  } else if (sqlFlow && message.method === 'Network.responseReceived' &&
      ['https://dev.api.web4.oasisomniverse.one/api/sandbox/session', 'https://dev.api.web4.oasisomniverse.one/api/avatar/revoke-token'].includes(p.response.url)) {
    const observation = { path: new URL(p.response.url).pathname, http: p.response.status };
    authRequests.set(p.requestId, observation); authResponses.push(observation);
  } else if (sqlFlow && message.method === 'Network.loadingFinished' && authRequests.has(p.requestId)) {
    const observation = authRequests.get(p.requestId); authRequests.delete(p.requestId);
    const response = await send('Network.getResponseBody', { requestId: p.requestId }, message.sessionId);
    try {
      const payload = JSON.parse(response.base64Encoded ? Buffer.from(response.body, 'base64').toString('utf8') : response.body);
      observation.errorEnvelope = payload.isError === true;
      const code = payload.errorCode ?? payload.result?.errorCode;
      if (typeof code === 'string' && /^[A-Z0-9_]{1,100}$/.test(code)) observation.errorCode = code;
      const message = typeof payload.message === 'string' ? payload.message : '';
      for (const [prefix, stage] of [['Could not create a sandbox avatar:', 'registration'],
        ['This node cannot host the sandbox:', 'provider-routing'], ['Could not verify the sandbox avatar:', 'verification'],
        ['Could not sign in the sandbox avatar:', 'authentication']]) {
        if (message.startsWith(prefix)) observation.failureStage = stage;
      }
      if (observation.http === 429) observation.failureStage = 'session-admission';
    } catch { observation.jsonEnvelope = false; }
  } else if (sqlFlow && message.method === 'Network.responseReceived' &&
      p.response.url === 'https://dev.api.web4.oasisomniverse.one/api/data/query-cql/scalar') {
    scalarRequests.add(p.requestId);
  } else if (sqlFlow && message.method === 'Network.loadingFinished' && scalarRequests.delete(p.requestId)) {
    const body = await send('Network.getResponseBody', { requestId: p.requestId }, message.sessionId);
    const value = JSON.parse(body.base64Encoded ? Buffer.from(body.body, 'base64').toString('utf8') : body.body);
    scalarValues.push(value.result?.value);
    scalarResponses.push({ acknowledged: value.isError === false, resultPresent: value.result !== undefined && value.result !== null,
      nonnegativeIntegerCount: value.result?.version === 'cql-scalar-v1' &&
        value.result.provider === selectedProviders[scalarResponses.length] &&
        value.result.operation === 'LongCount' && value.result.valueType === 'Int64' &&
        typeof value.result.value === 'string' && /^(0|[1-9][0-9]{0,18})$/.test(value.result.value) &&
        BigInt(value.result.value) <= 9223372036854775807n });
  } else if ((sqlFlow ? message.method === 'Page.domContentEventFired' : message.method === 'Page.loadEventFired') && loadWaiters.has(message.sessionId)) {
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
      } else event(message).then(undefined, async error => {
        protocolErrors.push(error.message);
        if (message.method === 'Fetch.requestPaused') {
          try { await send('Fetch.failRequest', { requestId: message.params.requestId, errorReason: 'Failed' }, message.sessionId); }
          catch (releaseError) { protocolErrors.push('Failed request release: ' + releaseError.message); }
        }
      });
    });
    await send('Target.setAutoAttach', { autoAttach: true, waitForDebuggerOnStart: true, flatten: true });
    const context = await send('Target.createBrowserContext', { disposeOnDetach: true });
    const target = await send('Target.createTarget', { url: 'about:blank', browserContextId: context.browserContextId });
    const session = await forTarget(target.targetId);
    pageSession = session;
    const loaded = new Promise((resolve, reject) => {
      const timer = setTimeout(() => { loadWaiters.delete(session); reject(new Error('Preview load deadline.')); }, 45000);
      loadWaiters.set(session, () => { clearTimeout(timer); resolve(); });
    });
    await Promise.all([send('Page.navigate', { url }, session), loaded]);
    const result = await send('Runtime.evaluate', { expression: `JSON.stringify({title:document.title,ready:document.readyState,ask:document.body.innerText.toLowerCase().includes('ask your data'),virtual:document.body.innerText.toLowerCase().includes('virtual'),editorControls:[...document.querySelectorAll('textarea,select,input')].map(e=>({tag:e.tagName,id:e.id,type:e.type})).filter(e=>/cql|sql|ask|mapping|virtual/i.test(e.id)).slice(0,40)})`, returnByValue: true }, session);
    if (result.exceptionDetails) throw new Error('DOM inspection failed.');
    Object.assign(report, JSON.parse(result.result.value));
    if (sqlFlow) {
      report.candidateAssetsAtApprovedOrigin = true;
      report.candidateWebsiteSource = candidateWebsiteSource;
      report.candidateScriptHashesVerified = Object.entries(expectedAssetHashes)
        .every(([name, hash]) => assetHashes.get(name) === hash);
      if (!report.candidateScriptHashesVerified) throw new Error('The exact candidate scripts were not loaded.');
      report.publicWebsitePublicationVerified = false;
      await evaluate('(async()=>{await startDemo();return !!session.identity()})()').then(ok => { if (!ok) throw new Error('Demo session not established.'); });
      await waitFor(() => evaluate("!document.getElementById('cql-run').disabled"), 'Provider discovery and query readiness');
      for (const provider of selectedProviders) {
        const available = await evaluate(`window.cqlPlayground.availableTextProviders().includes(${JSON.stringify(provider)})`);
        if (!available) { report.providerUnavailable = provider; throw new Error('Provider unavailable in current catalogue: ' + provider); }
        const before = scalarResponses.length;
        await evaluate(`document.getElementById('cql-text').value=${JSON.stringify('SELECT COUNT(*) Total FROM ')}+${JSON.stringify(provider)};document.getElementById('cql-run-text').click();true`);
        await waitFor(() => scalarResponses.length > before, provider + ' SQL response');
        if (!scalarResponses[before].acknowledged || !scalarResponses[before].resultPresent) throw new Error(provider + ' SQL request was not acknowledged.');
        if (!scalarResponses[before].nonnegativeIntegerCount) throw new Error(provider + ' COUNT response was not a nonnegative integer.');
        const expectedNote = provider + ' · Total: ' + scalarValues[before];
        await waitFor(() => evaluate(`document.getElementById('cql-note').textContent === ${JSON.stringify(expectedNote)}`), provider + ' rendered SQL alias and scalar');
        scalarResponses[before].renderedAliasAndValueVerified = true;
        completedSqlProviders.push(provider);
        await new Promise(resolve => setTimeout(resolve, 600));
      }
      report.sqlEditorRequestsAcknowledged = scalarResponses;
      report.sqlEditorApiJourneyVerified = true;
      report.scalarResultRenderingVerified = true;
      report.populatedSqlResultsVerified = false;
      report.fullBrowserDataFlowVerified = false;
    }
    const screenshot = await send('Page.captureScreenshot', { format: 'png', captureBeyondViewport: false }, session);
    const png = Buffer.from(screenshot.data, 'base64');
    fs.writeFileSync(path.join(work, 'preview.png'), png);
    report.screenshot = path.join(work, 'preview.png'); report.screenshotBytes = png.length;
    report.screenshotSha256 = crypto.createHash('sha256').update(png).digest('hex');
    report.blockedRequests = blocked; report.scriptExceptions = exceptions; report.protocolErrors = protocolErrors;
    report.renderedProbe = true;
    if (!sqlFlow) report.fullBrowserDataFlowVerified = false;
  } catch (error) {
    report.error = error.message; process.exitCode = 1;
    report.scriptExceptions = exceptions; report.protocolErrors = protocolErrors; report.blockedRequests = blocked;
    if (pageSession && socket?.readyState === WebSocket.OPEN) {
      try { report.editorFailure = await evaluate("({note:document.getElementById('cql-note')?.textContent,runDisabled:document.getElementById('cql-run')?.disabled,provider:document.getElementById('cql-provider')?.value,playground:!!window.cqlPlayground,askInitialized:!!window.cqlAsk})"); }
      catch (inspectionError) { report.editorInspectionError = inspectionError.message; }
    }
  } finally {
    report.completedSqlProviders = completedSqlProviders;
    report.sqlResponses = scalarResponses;
    if (neoDiscovery) report.neoDiscovery = neoDiscovery;
    if (sqlFlow && pageSession && socket?.readyState === WebSocket.OPEN) {
      report.demoSessionRequested = apiCounts.has('POST /api/sandbox/session');
      if (report.demoSessionRequested) {
        try { report.demoSessionRevoked = await evaluate('(async()=>{if(!session.identity())return false;await session.signOut();return !session.identity()})()'); }
        catch (error) { report.demoSessionRevoked = false; report.revocationError = error.message; process.exitCode = 1; }
      } else report.demoSessionRevocationRequired = false;
      report.apiRequestCounts = Object.fromEntries(apiCounts);
      report.authResponses = authResponses; // Status/error code only: never retain avatar, cookie or token payloads.
      report.candidateAssetHashes = Object.fromEntries(assetHashes);
    }
    if (socket && socket.readyState === WebSocket.OPEN) {
      try { await send('Browser.close'); } catch (error) { report.closeTransport = error.message; }
      socket.terminate();
    }
    report.ownedBrowserStopped = await waitForOwnedBrowserExit();
    if (!report.ownedBrowserStopped) {
      browser.kill(); report.ownedBrowserForcedStop = true;
      report.ownedBrowserStopped = await waitForOwnedBrowserExit();
    }
    if (report.ownedBrowserStopped) browser.stderr.destroy();
    const ownership = JSON.parse(fs.readFileSync(marker, 'utf8'));
    report.ownedWorkVerified = ownership.owner === owner && path.resolve(ownership.work) === work &&
      fs.realpathSync(work).toLowerCase().startsWith(fs.realpathSync(parent).toLowerCase() + path.sep);
    if (report.ownedBrowserStopped && report.ownedWorkVerified) {
      try {
        if (fs.existsSync(profile)) {
          if (fs.realpathSync(profile).toLowerCase() !== profile.toLowerCase()) throw new Error('Owned profile resolution changed.');
          fs.rmSync(profile, { recursive: true });
        }
        report.profileAbsent = !fs.existsSync(profile);
        const remaining = fs.readdirSync(work);
        if (remaining.length === 1 && remaining[0] === 'owner.json') {
          fs.unlinkSync(marker); fs.rmdirSync(work);
        }
      } catch (error) { report.profileCleanupError = error.code || error.message; process.exitCode = 1; }
    }
    report.profileCleanupPending = fs.existsSync(profile);
    fs.appendFileSync(ledger, '\nPROTECTED-PREVIEW-BROWSER-OUTCOME: ' + JSON.stringify({ work, browserStopped: report.ownedBrowserStopped, profileAbsent: report.profileAbsent, cleanupError: report.profileCleanupError, screenshot: report.screenshot, screenshotBytes: report.screenshotBytes, retainedForVisualReview: true }) + '\n');
    console.log('CQL_BROWSER_JSON=' + JSON.stringify(report));
  }
})();
