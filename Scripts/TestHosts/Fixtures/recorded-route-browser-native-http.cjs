'use strict';
const assert=require('node:assert/strict'),path=require('node:path');
const {randomUUID}=require('node:crypto');
async function verify({baseUrl,token,avatarId,otherToken,otherId,providerType,ourWorldRouteId}){
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');assert.equal(providerType,'MongoDBOASIS');
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const context=await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,'browser-profile-'+randomUUID()),{channel:'chrome',headless:true,serviceWorkers:'block'});
 const errors=[],unexpected=[],writes=[];let page,stage='launch';
 try{
  await context.grantPermissions(['clipboard-read','clipboard-write'],{origin:base.origin});
  await context.route('**/*',route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith('/api/')){
     const allowed=request.method()==='GET'&&(url.pathname==='/api/map/stats'||/^\/api\/map\/routes(?:\/[0-9a-f-]+)?$/i.test(url.pathname))||
      request.method()==='POST'&&url.pathname==='/api/map/routes'||request.method()==='PUT'&&/^\/api\/map\/routes\/[0-9a-f-]+\/access$/i.test(url.pathname);
     if(!allowed){unexpected.push(request.method()+' '+url.pathname);return route.abort();}
     if(request.method()!=='GET')writes.push({method:request.method(),body:JSON.parse(request.postData())});
    }
    return route.continue();
   }
   if(['data:','blob:'].includes(url.protocol))return route.continue();
   return route.abort();
  });
  await context.addInitScript(({token,avatarId})=>{localStorage.setItem('loggedIn','true');localStorage.setItem('avatar',JSON.stringify({id:avatarId}));sessionStorage.setItem('ownedMapBearer',token);},{token,avatarId});
  page=await context.newPage();page.setDefaultTimeout(15000);page.on('pageerror',error=>errors.push(error.message));
  await page.goto(baseUrl+'/__map_browser'+(ourWorldRouteId?'?mapRoute='+encodeURIComponent(ourWorldRouteId):''));await page.waitForFunction(()=>window.__mapReady===true);
  await page.locator('#fixture-open-map').click();await page.locator('.map-tab[data-tab="routes"]').click();
  if(ourWorldRouteId){
   stage='open actual Our World publication';
   await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.startsWith('Private to its owner.'));
   assert.equal(await page.locator('#map-route-id').inputValue(),ourWorldRouteId);
   assert.equal(await page.locator('#map-route-name').inputValue(),'Our World cloud walk');
   assert.equal(await page.locator('#map-route-public').isChecked(),false);
   assert.match(await page.locator('#map-route-status').innerText(),/Our World \/ owned-http-world-route/);
   assert.equal(await page.locator('#map-route-save').isDisabled(),false);
   assert.equal(writes.length,0);assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);
   return {ourWorldPublicationVerified:true,routeId:ourWorldRouteId,zeroBrowserMutations:true};
  }
  stage='real GPX import';
  const gpx='<?xml version="1.0"?><gpx xmlns="http://www.topografix.com/GPX/1/1" version="1.1"><trk><name>Morning walk</name><extensions><route xmlns="https://oasisomniverse.one/schemas/map-route/v1" sourceApp="Our World" sourceRouteId="owned-browser-walk"/></extensions><trkseg><trkpt lat="0" lon="0"><time>2026-11-21T00:00:00.1234567Z</time></trkpt><trkpt lat="0" lon="0.001"/></trkseg><trkseg><trkpt lat="1" lon="1"/><trkpt lat="1" lon="1.001"/></trkseg></trk></gpx>';
  await page.locator('#map-route-file').setInputFiles({name:'owned-walk.gpx',mimeType:'application/gpx+xml',buffer:Buffer.from(gpx)});
  await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.includes('4 points in 2 connected segments'));
  assert.equal(await page.locator('#map-route-name').inputValue(),'Morning walk');assert.equal(await page.locator('#map-route-public').isChecked(),false);
  stage='private publication';await page.locator('#map-route-save').click();
  await page.waitForFunction(()=>document.getElementById('map-route-save').textContent==='Save name & access'&&!document.getElementById('map-route-save').disabled);
  const routeId=await page.locator('#map-route-id').inputValue();assert.match(routeId,/^[0-9a-f-]{36}$/i);
  assert.match(await page.locator('#map-route-status').innerText(),/Private to its owner/);
  assert.equal(writes.length,1);const create=writes[0].body;assert.equal(create.route.segments[0][0].utc,'2026-11-21T00:00:00.1234567Z');
  assert.equal(create.route.sourceApp,'Our World');assert.equal(create.route.sourceRouteId,'owned-browser-walk');
  const geometryId=Object.keys(create.expectedRevisions).find(id=>id!==routeId);assert.ok(geometryId);
  stage='public rename';await page.locator('#map-route-name').fill('Shared morning walk');await page.locator('#map-route-public').check();await page.locator('#map-route-save').click();
  await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.startsWith('Shared with signed-in OASIS avatars.'));
  assert.equal(writes.length,2);assert.equal(Object.keys(writes[1].body.expectedRevisions).length,2);
  stage='copy access-controlled link';await page.locator('#map-route-copy').click();
  await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.startsWith('Route link copied.'));
  const sharedLink=new URL(await page.evaluate(()=>navigator.clipboard.readText()));assert.equal(sharedLink.pathname,'/map');assert.equal(sharedLink.searchParams.get('mapRoute'),routeId);
  async function actor(id,bearer){await page.evaluate(({id,bearer,routeId})=>{
   window.dispatchEvent(new Event('oasis-logout'));window.user={id};localStorage.setItem('avatar',JSON.stringify(window.user));window.oasisClient.setToken(bearer,{avatarId:id});
   history.replaceState(null,'','/__map_browser?mapRoute='+routeId);
  },{id,bearer,routeId});await page.locator('#fixture-open-map').click();}
  stage='shared reader via deep-link query';await actor(otherId,otherToken);
  await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.startsWith('Shared with signed-in OASIS avatars.'));
  assert.equal(await page.locator('#map-route-save').isDisabled(),true);assert.equal(await page.locator('#map-route-name').inputValue(),'Shared morning walk');
  stage='owner revocation';await actor(avatarId,token);
  await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.startsWith('Shared with signed-in OASIS avatars.'));
  await page.locator('#map-route-public').uncheck();await page.locator('#map-route-save').click();
  await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.startsWith('Private to its owner.'));
  stage='revoked reader';await actor(otherId,otherToken);
  await page.waitForFunction(()=>{const t=document.getElementById('map-route-status').textContent;return t&&t!=='Opening recorded route…'&&!t.startsWith('Shared with');});
  assert.equal(await page.locator('#map-route-name').inputValue(),'');assert.equal(await page.locator('#map-route-save').isDisabled(),true);
  stage='logout clearing';await page.evaluate(()=>window.dispatchEvent(new Event('oasis-logout')));
  assert.equal(await page.locator('#map-route-id').inputValue(),'');assert.equal(await page.locator('#map-route-name').inputValue(),'');assert.equal(await page.locator('#map-saved-routes button').count(),0);
  assert.equal(writes.length,3);assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);
  return {recordedRoutePopupVerified:true,routeId,geometryId,providerType,preciseTimestamp:true,segmentedGpx:true,publicRead:true,ownerRevocation:true,threeAcknowledgedWrites:true};
 }catch(error){
  const state=page&&!page.isClosed()?await page.evaluate(()=>({status:document.getElementById('map-route-status')?.textContent,button:document.getElementById('map-route-save')?.textContent})):null;
  console.error(JSON.stringify({routeStage:stage,state,errors,unexpected,writeCount:writes.length}));throw error;
 }finally{await context.close();}
}
module.exports={verify};
