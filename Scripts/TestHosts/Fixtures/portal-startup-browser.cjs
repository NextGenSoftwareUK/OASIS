'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs/promises'),path=require('node:path');
const http=require('node:http'),https=require('node:https'),{execFileSync}=require('node:child_process');
const {createOwnedAvatar}=require('./startup-authentication.cjs');
let stage='preparation';
const httpFacts=[];
async function main(){
 assert.equal(process.env.GITHUB_ACTIONS,'true');
 const api=new URL(process.env.OASIS_STARTUP_API_URL);assert.equal(api.hostname,'127.0.0.1');assert.equal(api.protocol,'http:');
 const api5=process.env.OASIS_STARTUP_WEB5_URL?new URL(process.env.OASIS_STARTUP_WEB5_URL):null;
 const popupGroup=process.env.OASIS_STARTUP_POPUP_GROUP||'none';
 assert.ok(['none','nft','data','data-crud','map-route','messaging','data-messaging','nft-messaging-data'].includes(popupGroup));
 const selectedPopups=popupGroup==='nft-messaging-data'?['NFT','Data','Messaging']:
  popupGroup==='data-messaging'?['Data','Messaging']:
  popupGroup==='none'?[]:[{nft:'NFT',data:'Data','data-crud':'DataWrite','map-route':'MapRoute',messaging:'Messaging'}[popupGroup]];
 const messageText='Owned portal message '+process.env.GITHUB_RUN_ID+'-'+process.env.GITHUB_RUN_ATTEMPT;
 let recipient,recipientToken,sentMessage=0,dataReads=0;
 let routeId,routeCreates=0,routeUpdates=0;
 let savedHolonId,holonSaves=0,holonDeletes=0,pointReads=0;
 const dataName='Owned portal record '+process.env.GITHUB_RUN_ID+'-'+process.env.GITHUB_RUN_ATTEMPT;
 if(api5){assert.equal(api5.hostname,'127.0.0.1');assert.equal(api5.protocol,'http:');}
 const root=await fs.realpath(process.env.OASIS_STARTUP_PORTAL_ROOT),owned=await fs.realpath(process.env.OASIS_PROFILE_BROWSER_RUNTIME);
 const {chromium}=require(path.join(owned,'node_modules/playwright-core'));
 const cert=path.join(owned,'owned-localhost.crt'),key=path.join(owned,'owned-localhost.key');
 execFileSync('openssl',['req','-x509','-newkey','rsa:2048','-nodes','-days','1','-subj','/CN=127.0.0.1',
  '-addext','subjectAltName=IP:127.0.0.1','-keyout',key,'-out',cert],{stdio:'ignore'});
 const types={'.html':'text/html','.js':'application/javascript','.css':'text/css','.svg':'image/svg+xml','.png':'image/png','.jpg':'image/jpeg','.woff2':'font/woff2'};
 const server=http.createServer(async(req,res)=>{
  try{
   const url=new URL(req.url,'http://127.0.0.1');
   const name=['/','/beamin','/subscription'].includes(url.pathname)?'portal.html':decodeURIComponent(url.pathname).replace(/^\//,'');
   const file=await fs.realpath(path.resolve(root,name));if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
   const body=await fs.readFile(file);res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream'});res.end(body);
  }catch{res.writeHead(404);res.end();}
 });
 // Transparent transport proxy: production Startup supplies every API status, header and body.
 function createProxy(target){return https.createServer({key:keyBytes,cert:certBytes},(req,res)=>{
  const forward=http.request(new URL(req.url,target),{method:req.method,headers:{...req.headers,host:target.host,'x-forwarded-proto':'https'}},upstream=>{
   res.writeHead(upstream.statusCode,upstream.headers);upstream.pipe(res);
  });forward.on('error',()=>res.destroy());req.pipe(forward);
 });}
 const keyBytes=await fs.readFile(key),certBytes=await fs.readFile(cert);
 const proxy=createProxy(api),proxy5=api5?createProxy(api5):null;
 let browser,context;
 try{
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));await new Promise(resolve=>proxy.listen(0,'127.0.0.1',resolve));
  if(proxy5)await new Promise(resolve=>proxy5.listen(0,'127.0.0.1',resolve));
  const origin='http://127.0.0.1:'+server.address().port,proxyOrigin='https://127.0.0.1:'+proxy.address().port;
  const proxy5Origin=proxy5?'https://127.0.0.1:'+proxy5.address().port:null;
  stage='owned registration';const account=await createOwnedAvatar(api.href);
  async function admitFree(owned){
   const signed=await owned.call('free account sign-in','POST','/api/avatar/authenticate',{username:owned.username,password:owned.password},undefined,true);
   await owned.call('free subscription','POST','/api/subscription/checkout/session',{planId:'free',successUrl:origin+'/subscription?tab=mysubs',cancelUrl:origin+'/'},signed.jwtToken);
   const subs=await owned.call('free readback','GET','/api/subscription/subscriptions/me',undefined,signed.jwtToken);
   assert.ok(Array.isArray(subs)&&subs.some(s=>s.planId==='free'&&s.status==='active'),'Persisted free subscription');
   return signed.jwtToken;
  }
  if(api5)await admitFree(account);
  if(selectedPopups.length){
   assert.equal(api5,null,'WEB4-only popup journey');await admitFree(account);
   if(selectedPopups.includes('Messaging')){
    recipient=await createOwnedAvatar(api.href);recipientToken=await admitFree(recipient);
   }
  }
  browser=await chromium.launch({channel:'chrome',headless:true});
  context=await browser.newContext({serviceWorkers:'block',ignoreHTTPSErrors:true});
  const errors=[],otherServices=[],reads=[];
  await context.route('**/*',async route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.origin==='https://api.web4.oasisomniverse.one'){
    const mutation=request.method()!=='GET'&&request.method()!=='OPTIONS';
    let ownedOperation=false;
    if(popupGroup==='map-route'){
     if(request.method()==='POST'&&url.pathname==='/api/map/routes'){
      assert.equal(++routeCreates,1,'One explicit owned GPX publication');const body=request.postDataJSON();
      routeId=body.route.id;assert.match(routeId,/^[0-9a-f-]{36}$/i);
      assert.equal(body.route.sourceApp,'Our World');assert.equal(body.route.sourceRouteId,'owned-walk');
      assert.equal(body.route.isPublic,false);assert.equal(body.route.segments.length,1);assert.equal(body.route.segments[0].length,2);
      assert.equal(body.route.segments[0][0].utc,'2026-11-21T10:00:00.1234567Z');
      assert.equal(Object.keys(body.expectedRevisions).length,2);assert.ok(Object.values(body.expectedRevisions).every(v=>v===null));ownedOperation=true;
     }else if(request.method()==='PUT'&&routeId&&url.pathname==='/api/map/routes/'+routeId+'/access'){
      assert.equal(++routeUpdates,1,'One explicit owned rename');const body=request.postDataJSON();
      assert.equal(body.name,'Renamed owned walk');assert.equal(body.isPublic,false);
      assert.equal(body.providerType,'MongoDBOASIS');assert.equal(Object.keys(body.expectedRevisions).length,2);ownedOperation=true;
     }
    }
    if(popupGroup==='data-crud'){
     if(request.method()==='POST'&&url.pathname==='/api/data/load-holons-for-parent'){
      assert.ok(++dataReads<=4,'Bounded actual Data browse');
      assert.deepEqual(request.postDataJSON(),{id:account.avatar.id,holonType:'All',loadChildren:false,recursive:false});ownedOperation=true;
     }else if(request.method()==='POST'&&url.pathname==='/api/data/save-holon'){
      assert.equal(++holonSaves,1,'One explicit owned create');const body=request.postDataJSON();
      assert.equal(body.holon.name,dataName);assert.equal(body.holon.description,'Owned create read delete journey');
      assert.equal(body.holon.parentHolonId,account.avatar.id);assert.equal(body.saveChildren,false);
      assert.ok(!body.providerType||body.providerType==='Default','Use actual default provider');
      assert.match(body.operationId,/^[0-9a-f-]{36}$/i);ownedOperation=true;
     }else if(request.method()==='POST'&&url.pathname==='/api/data/load-holon'){
      assert.equal(++pointReads,1,'One actual point read');assert.equal(request.postDataJSON().id,savedHolonId);ownedOperation=true;
     }else if(request.method()==='DELETE'&&url.pathname==='/api/data/delete-holon'){
      assert.equal(++holonDeletes,1,'One explicit owned deletion');const body=request.postDataJSON();
      assert.equal(body.id,savedHolonId);assert.equal(body.softDelete,false);
      assert.equal(body.providerType,'MongoDBOASIS');assert.deepEqual(Object.keys(body.expectedRevisions),[savedHolonId]);
      assert.match(body.expectedRevisions[savedHolonId],/^[0-9a-f-]{36}$/i);ownedOperation=true;
     }
    }
    if(selectedPopups.length&&request.method()==='POST'){
     if(selectedPopups.includes('Data')&&url.pathname==='/api/data/load-holons-for-parent'){
      assert.equal(++dataReads,1,'One actual Data browse');
      assert.deepEqual(request.postDataJSON(),{id:account.avatar.id,holonType:'All',loadChildren:false,recursive:false});
      ownedOperation=true;
     }else if(selectedPopups.includes('Messaging')&&url.pathname==='/api/messaging/send-message-to-avatar/'+recipient.avatar.id){
      assert.equal(++sentMessage,1,'One explicit owned message send');
      assert.equal(request.postDataJSON(),messageText);ownedOperation=true;
     }
    }
    if(!url.pathname.startsWith('/api/')||(mutation&&!ownedOperation&&!['/api/avatar/authenticate','/api/avatar/refresh-token'].includes(url.pathname)))
     throw new Error('Unadmitted browser API operation.');
    return route.continue({url:proxyOrigin+url.pathname+url.search});
   }
   if(api5&&url.origin==='https://api.starnet.oasisomniverse.one'){
    if(request.method()!=='GET'&&request.method()!=='OPTIONS'){otherServices.push('unadmitted WEB5 mutation');return route.abort();}
    return route.continue({url:proxy5Origin+url.pathname+url.search});
   }
   if(url.origin===origin||['data:','blob:'].includes(url.protocol)||['fonts.googleapis.com','fonts.gstatic.com'].includes(url.hostname))return route.continue();
   otherServices.push(request.method()+' '+url.origin+url.pathname);return route.abort();
  });
  const page=await context.newPage();page.on('pageerror',()=>errors.push('page-error'));
  page.on('response',async response=>{
   const url=new URL(response.url());if(!url.pathname.startsWith('/api/'))return;
   const fact={path:url.pathname,status:response.status()};reads.push(fact);
   if(httpFacts.length<64)httpFacts.push(fact);
   try{const body=await response.json(),result=typeof body.isError==='boolean'?body:body.result;
    fact.isError=result?.isError;const code=result?.errorCode;
    if(/^\/api\/(nft|oLand|data|messaging)\//i.test(url.pathname)){
     const value=result?.result;fact.resultShape=Array.isArray(value)?'array':value===null?'null':typeof value;
     if(Array.isArray(value))fact.resultCount=value.length;
    }
    if(typeof code==='string'&&/^[A-Z0-9_]{1,80}$/.test(code))fact.errorCode=code;
    if(code==='HYPERDRIVE_ROUTE_FAILED'&&typeof result.message==='string'){
     const message=result.message.replace(/(?:https?|mongodb(?:\+srv)?):\/\/[^\s]+/gi,'[URI]')
      .replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g,'[JWT]')
      .replace(/[a-f0-9]{32,}/gi,'[identifier]').slice(0,320);
     fact.routingMessage=/password|secret|credential|connectionstring|refresh.?token/i.test(message)?'[sensitive routing detail withheld]':message;
    }
   }catch{fact.json=false;}
  });
  stage='page and Beam In';await page.goto(origin+'/',{waitUntil:'domcontentloaded'});
  await page.locator('.js-modal-trigger[data-signin="login"]:visible').first().click();
  await page.locator('#login-email').fill(account.username);await page.locator('#login-password').fill(account.password);
  await page.locator('#login-submit').click();
  await page.waitForFunction(()=>localStorage.getItem('loggedIn')==='true');
  stage='dashboard';await page.locator('#dashboard').waitFor({state:'visible'});
  assert.equal(await page.locator('#dash-name').textContent(),account.username);
  if(selectedPopups.length){
   const outcomes=[];
   async function checkPopup(name,closeSelector,work){
    if(!selectedPopups.includes(name))return;
    let passed=false,closePassed=true,observedStage=name;
    try{await work();passed=true;observedStage=stage;}
    catch{observedStage=stage;}
    try{
     if(await page.locator(closeSelector).isVisible()){
      await page.locator(closeSelector).click();await page.waitForURL(url=>url.pathname==='/');
     }
    }catch{closePassed=false;passed=false;if(observedStage===name)observedStage=name+' close';}
    outcomes.push({name,passed,stage:observedStage,closePassed});
   }
   await checkPopup('NFT','#nft-modal-close-btn',async()=>{
   stage='NFT catalogue';await page.locator('#dash-card-nfts').click();
   await page.waitForFunction(()=>['standard','geo','oland'].every(kind=>
    !document.getElementById('nft-empty-'+kind).hidden&&document.getElementById('nft-error-'+kind).hidden));
   stage='NFT grouping';await page.locator('#nft-group-by-collection').check();
   assert.equal(await page.locator('#nft-empty-standard').isVisible(),true);
   assert.equal(await page.locator('#nft-error-standard').isVisible(),false);
   stage='NFT tabs';
   for(const tab of ['geo','oland']){
    await page.locator('#nft-modal-block [data-tab="'+tab+'"]').click();
    await page.locator('#nft-tab-'+tab).waitFor({state:'visible'});
   }
   });
   await checkPopup('Data','#data-modal-close-btn',async()=>{
   stage='Data browse';const dataRead=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/load-holons-for-parent');
   await page.locator('#dash-card-holons').click();
   const raw=await (await dataRead).json();assert.equal(raw.result.isError,false,'Real Data result');
   assert.ok(Array.isArray(raw.result.result),'Real Data list');
   await page.waitForFunction(n=>document.getElementById('data-modal-status').hidden&&
    document.querySelectorAll('#data-browse-grid .data-holon-card').length===n,raw.result.result.length);
   assert.equal(dataReads,1);
   });
   await checkPopup('DataWrite','#data-modal-close-btn',async()=>{
    stage='Data create form';await page.locator('#dash-card-holons').click();
    await page.waitForFunction(()=>document.getElementById('data-modal-status').hidden);
    await page.locator('#data-modal-block [data-tab="save"]').click();
    await page.locator('#data-save-name').fill(dataName);await page.locator('#data-save-desc').fill('Owned create read delete journey');
    assert.equal(await page.locator('#data-save-provider').inputValue(),'','Keep default provider selection');
    stage='Data create';const create=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/save-holon'&&r.request().method()==='POST');
    await page.locator('#data-save-btn').click();const created=await (await create).json();
    assert.equal(created.result.isError,false,'Real conditional create');savedHolonId=created.result.result.id;
    assert.match(savedHolonId,/^[0-9a-f-]{36}$/i);assert.equal(created.result.result.name,dataName);
    stage='Data point read';await page.locator('#data-modal-block [data-tab="load"]').click();
    await page.locator('#data-load-id').fill(savedHolonId);
    const point=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/load-holon'&&r.request().method()==='POST');
    await page.locator('#data-load-btn').click();const loaded=await (await point).json();
    assert.equal(loaded.result.isError,false);assert.equal(loaded.result.result.id,savedHolonId);assert.equal(loaded.result.result.description,'Owned create read delete journey');
    await page.locator('#data-load-result .data-holon-card').waitFor({state:'visible'});
    stage='Data delete confirmation';await page.locator('#data-modal-block [data-tab="browse"]').click();
    await page.locator('#data-browse-grid .data-card-delete[data-id="'+savedHolonId+'"]').click();
    await page.locator('#data-confirm-overlay').waitFor({state:'visible'});
    const deletion=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/delete-holon'&&r.request().method()==='DELETE');
    const after=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/load-holons-for-parent'&&r.request().method()==='POST');
    stage='Data delete';await page.locator('#data-confirm-delete-btn').click();const deleted=await (await deletion).json();
    assert.equal(deleted.result.isError,false);assert.equal(deleted.result.result.id,savedHolonId,'Acknowledged deleted holon identity');
    const remaining=await (await after).json();assert.equal(remaining.result.isError,false);assert.ok(Array.isArray(remaining.result.result));
    assert.ok(!remaining.result.result.some(row=>row.id===savedHolonId));
    await page.waitForFunction(id=>document.getElementById('data-modal-status').hidden&&
     !document.querySelector('#data-browse-grid [data-holon-idx="'+id+'"]'),savedHolonId);
    assert.equal(holonSaves,1);assert.equal(pointReads,1);assert.equal(holonDeletes,1);
   });
   await checkPopup('MapRoute','#map-close-btn',async()=>{
    stage='Map routes open';await page.locator('#dashboard button.dash-panel-link').filter({hasText:'View Map'}).click();
    await page.locator('#map-modal-block [data-tab="routes"]').click();
    stage='Map GPX import';
    const gpx='<?xml version="1.0"?><gpx xmlns="http://www.topografix.com/GPX/1/1" version="1.1" creator="Owned fixture"><trk><name>Owned walk</name><extensions><route xmlns="https://oasisomniverse.one/schemas/map-route/v1" sourceApp="Our World" sourceRouteId="owned-walk"/></extensions><trkseg><trkpt lat="21.1" lon="-86.8"><time>2026-11-21T10:00:00.1234567Z</time></trkpt><trkpt lat="21.1001" lon="-86.8001"><time>2026-11-21T10:01:00Z</time></trkpt></trkseg></trk></gpx>';
    await page.locator('#map-route-file').setInputFiles({name:'owned-walk.gpx',mimeType:'application/gpx+xml',buffer:Buffer.from(gpx)});
    await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.includes('2 points in 1 connected segments'));
    assert.equal(await page.locator('#map-route-public').isChecked(),false);
    stage='Map route publish';await page.locator('#map-route-save').click();
    await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.startsWith('Private to its owner.'));
    assert.ok(routeId);assert.equal(await page.locator('#map-route-id').inputValue(),routeId);
    stage='Map route rename';await page.locator('#map-route-name').fill('Renamed owned walk');await page.locator('#map-route-save').click();
    await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.startsWith('Private to its owner.')&&document.getElementById('map-route-name').value==='Renamed owned walk'&&!document.getElementById('map-route-save').disabled);
    stage='Map saved-route reopen';await page.locator('#map-route-clear').click();
    await page.locator('#map-route-id').fill(routeId);await page.locator('#map-route-open').click();
    await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.includes('Our World / owned-walk.')&&document.getElementById('map-route-name').value==='Renamed owned walk');
    await page.locator('#map-routes-refresh').click();await page.waitForFunction(()=>document.getElementById('map-saved-routes').textContent.includes('Renamed owned walk'));
    assert.equal(routeCreates,1);assert.equal(routeUpdates,1);
   });
   await checkPopup('Messaging','#messaging-modal-close-btn',async()=>{
   stage='Messaging send';await page.locator('#dash-card-messages').click();
   await page.waitForFunction(()=>document.getElementById('msg-conversations-list').textContent.includes('No conversations yet'));
   await page.locator('#messaging-modal-block [data-tab="send"]').click();
   await page.locator('#msg-to-avatar').fill(recipient.avatar.id);await page.locator('#msg-body').fill(messageText);
   await page.locator('#msg-send-btn').click();
   await page.waitForFunction(()=>document.getElementById('messaging-modal-status').textContent==='Message sent.');
   stage='Messaging recipient readback';
   const inbox=await recipient.call('recipient inbox','GET','/api/messaging/messages?limit=50&offset=0',undefined,recipientToken);
   assert.ok(Array.isArray(inbox));
   const delivered=inbox.filter(m=>m.content===messageText&&m.fromAvatarId===account.avatar.id&&m.toAvatarId===recipient.avatar.id);
   assert.equal(delivered.length,1,'Exactly one persisted recipient message');assert.ok(delivered[0].id);
   stage='Messaging conversation';await page.locator('#messaging-modal-block [data-tab="conversations"]').click();
   await page.locator('[data-msg-open="'+recipient.avatar.id+'"]').click();
   await page.waitForFunction(content=>document.getElementById('msg-thread-messages')?.textContent.includes(content),messageText);
   assert.equal(sentMessage,1);
   });
   if(errors.length)outcomes.forEach(outcome=>{outcome.passed=false;});
   console.log('PORTAL_STARTUP_POPUP_OUTCOMES:'+JSON.stringify(outcomes));
   assert.deepEqual(errors,[]);assert.ok(outcomes.every(outcome=>outcome.passed),'Every selected popup must pass');
   console.log('PORTAL_STARTUP_BROWSER_PROOF: selected real NFT/Data/message journeys passed through shipping WEB4 Startup/owned Mongo; independent failures are retained and still fail qualification. No external mint, paid Stripe, WEB6 or hosted claim.');
   return;
  }
  if(process.env.OASIS_STARTUP_POPUP_BREADTH==='true'){
   assert.equal(api5,null,'WEB4-only new-popup scenario');
   stage='Files popup';await page.locator('#dash-card-files').click();
   await page.waitForFunction(()=>document.getElementById('files-stat-count').textContent==='0'&&
    document.getElementById('files-list').textContent.includes('No files stored yet'));
   await page.locator('#files-close-btn').click();await page.waitForURL(url=>url.pathname==='/');
   stage='Wallet popup';await page.locator('#dash-card-wallets').click();
   await page.waitForFunction(()=>document.getElementById('wallet-stat-wallets').textContent==='0'&&
    !document.getElementById('wallet-grid-empty').hidden&&document.getElementById('wallet-modal-status').hidden);
   await page.locator('#wallet-modal-close-btn').click();await page.waitForURL(url=>url.pathname==='/');
   stage='Karma popup';await page.locator('#dash-card-karma').click();
   await page.waitForFunction(()=>document.getElementById('karma-records-list').textContent.includes('No karma records found')&&
    document.querySelectorAll('#karma-stats-grid .karma-stat-card').length===4&&
    document.getElementById('karma-modal-status').hidden);
   await page.locator('#karma-modal-block [data-tab="stats"]').click();
   await page.locator('#karma-tab-stats').waitFor({state:'visible'});
   await page.locator('#karma-modal-close-btn').click();await page.waitForURL(url=>url.pathname==='/');
   assert.deepEqual(errors,[]);
   assert.ok(reads.some(read=>read.path.includes('/api/wallet/avatar/')&&read.isError===false));
   assert.ok(reads.some(read=>read.path.includes('/api/files/')&&read.isError===false));
   assert.ok(reads.some(read=>read.path.includes('/api/karma/')&&read.isError===false));
   console.log('PORTAL_STARTUP_BROWSER_PROOF: actual page Files/Wallet/Karma reads, empty lists, Karma stats tab and normal closes through shipping WEB4 Startup/native owned Mongo. Prior subscription/Games/OAPP cohorts not selected; writes, external chain execution, paid Stripe, WEB6 and hosted excluded.');
   return;
  }
  if(api5){
   stage='WEB5 Games dashboard';await page.locator('#dash-card-games').scrollIntoViewIfNeeded();
   await page.waitForFunction(()=>document.getElementById('dash-card-games').textContent==='0');
   await page.locator('#dash-card-games').click();
   await page.waitForFunction(()=>document.getElementById('games-stat-total').textContent==='0');
   await page.locator('#games-modal-block [data-tab="mine"]').click();
   await page.waitForFunction(()=>document.getElementById('games-stat-mine').textContent==='0');
   await page.locator('#games-close-btn').click();await page.waitForURL(url=>url.pathname==='/');
   stage='WEB5 OAPPs';await page.locator('#dashboard [onclick*="openOAPPsModal"]').first().click();
   await page.waitForFunction(()=>document.getElementById('oapps-my-list').textContent.includes('no OAPPs'));
   assert.deepEqual(errors,[]);
   for(const endpoint of ['/api/games','/api/games/load-all-for-avatar','/api/oapps/load-all-for-avatar'])
    assert.ok(reads.some(read=>read.path===endpoint&&read.status===200),'Actual WEB5 catalogue read');
   console.log('PORTAL_STARTUP_BROWSER_PROOF: actual page Beam In and WEB5 Games/OAPPs through real shipping WEB5 entry point and enabled WEB4 usage ledger/native owned Mongo. Fresh empty catalogue scope; populated component evidence retained. Paid Stripe/WEB6/hosted excluded.');
   return;
  }
  await page.waitForFunction(()=>/^\d[\d,]*$/.test(document.getElementById('dash-card-holons').textContent)&&
   /^\d[\d,]*$/.test(document.getElementById('dash-card-inventory').textContent)&&
   document.getElementById('dash-card-plan').textContent==='No active plan');
  stage='subscription plans';await page.locator('#dash-card-plan').click();
  // This isolated image intentionally has no Stripe secrets/prices. Prove the real
  // failure is visible, then continue independent account screens; no paid-plan acceptance.
  await page.locator('#sub-plans-list [data-sub-plans-retry]').waitFor({state:'visible'});
  assert.ok((await page.locator('#sub-plans-list').textContent()).includes('Could not load subscription plans'));
  assert.ok(reads.some(read=>read.path==='/api/subscription/plans'&&read.status===503));
  stage='my subscription';await page.locator('#subscription-modal-block [data-tab="mysubs"]').click();
  await page.waitForFunction(()=>document.getElementById('sub-mysubs-list').textContent.includes('No active subscriptions'));
  assert.equal(new URL(page.url()).searchParams.get('tab'),'mysubs');
  stage='orders';await page.locator('#subscription-modal-block [data-tab="orders"]').click();
  await page.waitForFunction(()=>document.getElementById('sub-orders-list').textContent.includes('No orders found'));
  await page.locator('#subscription-modal-block [data-tab="mysubs"]').click();
  stage='authenticated refresh';await page.reload({waitUntil:'domcontentloaded'});
  await page.locator('#subscription-modal-block.is-selected').waitFor({state:'visible'});
  await page.waitForFunction(()=>document.getElementById('sub-mysubs-list').textContent.includes('No active subscriptions'));
  assert.equal(await page.evaluate(()=>localStorage.getItem('loggedIn')),'true');
  stage='explicit session renewal';
  assert.equal(await page.evaluate(()=>window.refreshPortalSession()),true,'Native session renewal acknowledged');
  for(const endpoint of ['/api/avatar/authenticate','/api/avatar/refresh-token','/api/subscription/orders/me','/api/subscription/subscriptions/me'])
   assert.ok(reads.some(read=>read.path===endpoint&&read.status===200),'Acknowledged API '+endpoint);
  assert.deepEqual(errors,[]);
  console.log('PORTAL_STARTUP_BROWSER_PROOF: actual page Beam In, native dashboard counts/plan, explicit missing-Stripe Plans error, subscription list/orders and authenticated deep-link reload and explicit native session renewal passed through shipping WEB4 Startup. WEB5/WEB6 requests blocked; cross-service functionality, available paid catalogue and hosted Stripe excluded.');
  console.log('Other-service requests blocked before dispatch: '+otherServices.length);
 }finally{
  if(context)await context.close();if(browser)await browser.close();
  await Promise.all([new Promise(resolve=>server.close(resolve)),new Promise(resolve=>proxy.close(resolve))]);
  if(proxy5)await new Promise(resolve=>proxy5.close(resolve));
  await fs.rm(key,{force:true});await fs.rm(cert,{force:true});
  console.log('Owned portal browser, static/proxy listeners and temporary TLS files closed/removed.');
 }
}
main().catch(()=>{console.error('PORTAL_STARTUP_HTTP_FACTS:'+JSON.stringify(httpFacts));console.error('PORTAL_STARTUP_BROWSER_FAILURE stage='+stage+'; response bodies and credentials withheld.');process.exitCode=1;});
