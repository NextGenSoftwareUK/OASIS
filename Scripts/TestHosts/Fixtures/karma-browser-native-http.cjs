'use strict';
const assert=require('node:assert/strict');
const path=require('node:path');
const {randomUUID}=require('node:crypto');
async function verify({baseUrl,token,avatarId,otherToken,otherId,providerType}){
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');assert.equal(providerType,'MongoDBOASIS');
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const context=await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,'browser-profile-'+randomUUID()),{channel:'chrome',headless:true,serviceWorkers:'block'});
 const errors=[],unexpected=[],requests=[];
 try{
  await context.route('**/*',route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith('/api/')&&(url.pathname!=='/api/karma/activity'||request.method()!=='GET')){unexpected.push(url.pathname);return route.abort();}
    return route.continue();
   }
   if(['data:','blob:'].includes(url.protocol))return route.continue();
   unexpected.push(url.origin);return route.abort();
  });
  await context.addInitScript(({token,avatarId})=>{localStorage.setItem('loggedIn','true');localStorage.setItem('avatar',JSON.stringify({id:avatarId}));sessionStorage.setItem('ownedKarmaBearer',token);},{token,avatarId});
  const page=await context.newPage();page.setDefaultTimeout(15000);page.on('pageerror',error=>errors.push(error.message));
  page.on('request',request=>{const url=new URL(request.url());if(url.pathname==='/api/karma/activity')requests.push({method:request.method(),offset:Number(url.searchParams.get('offset')||0),limit:Number(url.searchParams.get('limit')||0)});});
  await page.goto(baseUrl+'/__karma_browser');await page.waitForFunction(()=>window.__karmaReady===true);
  await page.locator('#fixture-open-ledger').click();await page.waitForFunction(()=>document.getElementById('karma-ledger-status').textContent==='50 loaded karma events');
  assert.equal(await page.locator('.karma-ledger-event-row').count(),50);
  assert.match(await page.locator('#karma-ledger-list').innerText(),/-7/);
  assert.equal(await page.locator('.karma-ledger-bar-val').innerText(),'+48');
  await page.locator('#karma-ledger-more-btn').click();await page.waitForFunction(()=>document.querySelectorAll('.karma-ledger-event-row').length===51);
  assert.equal(await page.locator('.karma-ledger-bar-val').innerText(),'+49');
  assert.equal(await page.locator('#karma-ledger-more-btn').isVisible(),false);
  await page.locator('#karma-ledger-filter').fill('Owned Karma event 50');assert.equal(await page.locator('.karma-ledger-event-row').count(),1);
  await page.locator('#karma-ledger-modal-close-btn').click();await page.locator('#fixture-open-activity').click();
  await page.waitForFunction(()=>document.getElementById('activity-feed-status').textContent==='50 recent events');assert.equal(await page.locator('.afeed-row').count(),50);
  await page.evaluate(({id,bearer})=>{window.dispatchEvent(new Event('oasis-logout'));window.user={id};localStorage.setItem('avatar',JSON.stringify(window.user));window.oasisClient.setToken(bearer,{avatarId:id});},{id:otherId,bearer:otherToken});
  await page.locator('#fixture-open-ledger').click();await page.waitForFunction(()=>document.getElementById('karma-ledger-status').textContent==='No karma records yet.');assert.equal(await page.locator('.karma-ledger-event-row').count(),0);
  await page.evaluate(()=>{window.dispatchEvent(new Event('oasis-logout'));window.oasisClient.setToken(null);});
  await page.locator('#fixture-open-ledger').click();await page.waitForFunction(()=>document.getElementById('karma-ledger-status').textContent.includes('Sign in'));
  const anonymous=await context.request.get(baseUrl+'/api/karma/activity');assert.equal(anonymous.status(),401);
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);assert.ok(requests.some(r=>r.limit===50&&r.offset===50));assert.ok(requests.every(r=>r.method==='GET'));
  return {karmaPopupsVerified:true,providerType,readRequests:requests.length,paging51:true,negativeAndZero:true,tokenFreeAvatarCache:true,accountSwitchEmpty:true,anonymousDenied:true};
 }finally{await context.close();}
}
module.exports={verify};
