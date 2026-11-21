"use strict";
const assert=require('node:assert/strict'),path=require('node:path');
const {randomUUID}=require('node:crypto');
async function verify({baseUrl,token,avatarId,otherToken,otherId,providerType,locationId}){
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');assert.equal(providerType,'MongoDBOASIS');
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const context=await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,'browser-profile-'+randomUUID()),{channel:'chrome',headless:true,serviceWorkers:'block'});
 const errors=[],unexpected=[],requests=[],responses=[];let page,stage='launch';
 try{
  await context.route('**/*',route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith('/api/')){
     const allowed=request.method()==='GET'&&['/api/map/stats','/api/map/nearby','/api/map/search-locations','/api/map/visit-history'].includes(url.pathname)||request.method()==='POST'&&url.pathname==='/api/map/visit/'+locationId;
     if(!allowed){unexpected.push({path:url.pathname,method:request.method()});return route.abort();}
    }
    return route.continue();
   }
   if(['data:','blob:'].includes(url.protocol))return route.continue();
   if(url.pathname.startsWith('/api/'))unexpected.push({path:url.origin+url.pathname,method:request.method()});return route.abort();
  });
  await context.addInitScript(({token,avatarId})=>{localStorage.setItem('loggedIn','true');localStorage.setItem('avatar',JSON.stringify({id:avatarId}));sessionStorage.setItem('ownedMapBearer',token);},{token,avatarId});
  page=await context.newPage();page.setDefaultTimeout(15000);page.on('pageerror',error=>errors.push(error.message));
  page.on('response',response=>{const url=new URL(response.url());if(url.origin===base.origin&&response.status()>=400)responses.push({path:url.pathname,status:response.status()});});
  page.on('request',request=>{const url=new URL(request.url());if(url.pathname.startsWith('/api/'))requests.push({method:request.method(),path:url.pathname,query:Object.fromEntries(url.searchParams)});});
  stage='host ready';await page.goto(baseUrl+'/__map_browser');await page.waitForFunction(()=>window.__mapReady===true);
  stage='initial statistics';await page.locator('#fixture-open-map').click();await page.waitForFunction(()=>document.getElementById('map-stat-visits').textContent==='0');
  assert.equal(requests.some(x=>x.path==='/api/map/nearby'),false);assert.match(await page.locator('#map-nearby-grid').innerText(),/enter latitude and longitude/);
  stage='zero coordinates';await page.locator('#map-latitude').fill('0');await page.locator('#map-longitude').fill('0');await page.locator('#map-radius').fill('1');await page.locator('#map-refresh-btn').click();
  await page.waitForFunction(()=>document.querySelectorAll('#map-nearby-grid .map-loc-card').length===1);
  assert.match(await page.locator('#map-nearby-grid').innerText(),/Owned browser map destination/);assert.equal(await page.locator('#map-nearby-grid .map-loc-dist').innerText(),'0m');
  stage='actual text search';await page.locator('.map-tab[data-tab="search"]').click();await page.locator('#map-search-input').fill('OWNED BROWSER');await page.locator('#map-search-btn').click();
  await page.waitForFunction(()=>document.querySelectorAll('#map-search-results .map-loc-card').length===1);
  stage='saved visit warning';await page.locator('#map-search-results .map-visit-btn').click();
  await page.waitForFunction(()=>document.getElementById('map-modal-status').classList.contains('map-status--warn')&&document.getElementById('map-modal-status').textContent.includes('Weekly'));
  assert.match(await page.locator('#map-modal-status').innerText(),/visit was saved/i);
  await page.waitForFunction(()=>document.getElementById('map-stat-visits').textContent==='1');
  stage='actual visit history';await page.locator('.map-tab[data-tab="history"]').click();await page.waitForFunction(()=>document.querySelectorAll('.map-history-row').length===1);
  assert.match(await page.locator('#map-history-list').innerText(),new RegExp(locationId,'i'));assert.equal(await page.locator('#map-history-more').isVisible(),false);
  stage='required query coordinates';
  for(const query of ['latitude=0','longitude=0']){const reply=await context.request.get(baseUrl+'/api/map/nearby?'+query,{headers:{Authorization:'Bearer '+token}});assert.equal(reply.status(),400);}
  stage='account switch';await page.evaluate(({id,bearer})=>{window.dispatchEvent(new Event('oasis-logout'));window.user={id};localStorage.setItem('avatar',JSON.stringify(window.user));window.oasisClient.setToken(bearer,{avatarId:id});},{id:otherId,bearer:otherToken});
  assert.equal(await page.locator('#map-latitude').inputValue(),'');assert.equal(await page.locator('#map-search-results .map-loc-card').count(),0);
  await page.locator('#fixture-open-map').click();await page.waitForFunction(()=>document.getElementById('map-stat-visits').textContent==='0');
  await page.locator('.map-tab[data-tab="history"]').click();await page.waitForFunction(()=>document.getElementById('map-history-list').textContent.includes('No visit history yet.'));assert.equal(await page.locator('.map-history-row').count(),0);
  stage='anonymous denial';await page.evaluate(()=>{window.dispatchEvent(new Event('oasis-logout'));window.oasisClient.setToken(null);});const anonymous=await context.request.get(baseUrl+'/api/map/stats');assert.equal(anonymous.status(),401);
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);assert.equal(requests.filter(x=>x.method==='POST').length,1);
  assert.ok(requests.some(x=>x.path==='/api/map/nearby'&&x.query.latitude==='0'&&x.query.longitude==='0'&&x.query.radiusKm==='1'));
  assert.ok(requests.some(x=>x.path==='/api/map/search-locations'&&x.query.query==='OWNED BROWSER'));
  return {mapPopupVerified:true,providerType,oneNativeVisit:true,zeroCoordinates:true,realSearch:true,explicitSavedWarning:true,accountIsolation:true,requiredCoordinates:true,anonymousDenied:true};
 }catch(error){
  const state=page&&!page.isClosed()?await page.evaluate(()=>({ready:window.__mapReady===true,status:document.getElementById('map-modal-status')?.textContent,
   nearby:document.getElementById('map-nearby-grid')?.textContent,history:document.getElementById('map-history-list')?.textContent,visits:document.getElementById('map-stat-visits')?.textContent})):null;
  console.error(JSON.stringify({mapStage:stage,state,errors,unexpected,responses,requests}));throw error;
 }finally{await context.close();}
}
module.exports={verify};
