'use strict';
const assert=require('node:assert/strict'),path=require('node:path');
async function verify({baseUrl,token,avatarId,expectedGeoNFTs,expectedHotspots,expectedQuestLocations}){
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');
 for(const count of [expectedGeoNFTs,expectedHotspots,expectedQuestLocations])assert.ok(Number.isInteger(count)&&count>0);
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const browser=await chromium.launch({channel:'chrome',headless:true});
 const context=await browser.newContext({serviceWorkers:'block'}),errors=[],unexpected=[],requests=[];
 let releaseQuests;const questGate=new Promise(resolve=>{releaseQuests=resolve;});
 const geoNftPath='/api/nft/load-all-geo-nfts-for-avatar/'+avatarId;
 const allowed=new Set(['/api/map/stats',geoNftPath,'/api/geoHotSpots/nearby','/api/quests/all-for-avatar']);
 try{
  await context.route('**/*',async route=>{
   const req=route.request(),url=new URL(req.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith('/api/')){
     requests.push({path:url.pathname,method:req.method()});
     if(req.method()!=='GET'||!allowed.has(url.pathname)){unexpected.push(url.pathname);return route.abort();}
     if(url.pathname==='/api/quests/all-for-avatar')await questGate;
    }
    return route.continue();
   }
   if(['data:','blob:'].includes(url.protocol))return route.continue();
   if(url.origin==='https://fonts.googleapis.com'||url.origin==='https://fonts.gstatic.com')return route.continue();
   unexpected.push(url.origin+url.pathname);return route.abort();
  });
  await context.addInitScript(({token,avatarId})=>{
   localStorage.setItem('avatar',JSON.stringify({id:avatarId}));sessionStorage.setItem('ownedGeoBearer',token);
  },{token,avatarId});
  const page=await context.newPage();page.setDefaultTimeout(20000);page.on('pageerror',error=>errors.push(error.message));
  await page.goto(baseUrl+'/__geo_browser');await page.waitForFunction(()=>window.__mapReady===true);
  await page.locator('#fixture-open-map').click();
  await page.locator('#map-latitude').fill('0');await page.locator('#map-longitude').fill('0');await page.locator('#map-radius').fill('1');
  await page.locator('.map-tab[data-tab="geo"]').click();await page.locator('#map-load-geo').click();
  const partial=expectedGeoNFTs+' GeoNFTs, '+expectedHotspots+' hotspots, 0 quest/objective locations loaded.';
  await page.waitForFunction(text=>{const s=document.getElementById('map-geo-status').textContent;return s.includes(text)&&s.includes('Still loading: quests.');},partial);
  assert.equal(requests.filter(r=>r.path==='/api/quests/all-for-avatar').length,1);
  releaseQuests();
  const complete=expectedGeoNFTs+' GeoNFTs, '+expectedHotspots+' hotspots, '+expectedQuestLocations+' quest/objective locations loaded.';
  await page.waitForFunction(text=>{const s=document.getElementById('map-geo-status').textContent;return s===text;},complete);
  for(const p of [geoNftPath,'/api/geoHotSpots/nearby','/api/quests/all-for-avatar'])assert.equal(requests.filter(r=>r.path===p).length,1);
  assert.equal(await page.locator('#map-layer-geonfts').isChecked(),true);
  assert.equal(await page.locator('#map-layer-hotspots').isChecked(),true);
  assert.equal(await page.locator('#map-layer-quests').isChecked(),true);
  await page.evaluate(()=>{window.dispatchEvent(new Event('oasis-logout'));window.oasisClient.setToken(null);window.starClient.setToken(null);});
  assert.equal(await page.locator('#map-geo-status').textContent(),'');
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);assert.ok(requests.every(r=>r.method==='GET'));
  return {geoAssetsBrowserVerified:true,independentLayerCompletion:true,questRequirementsJoined:true,logoutClears:true,zeroMutations:true};
 }finally{releaseQuests();await context.close();await browser.close();}
}
module.exports={verify};
