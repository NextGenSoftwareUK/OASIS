'use strict';
const assert=require('node:assert/strict'),path=require('node:path');
async function verify({baseUrl,token,avatarId,gameId,oappId,itemId}) {
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const browser=await chromium.launch({channel:'chrome',headless:true}),context=await browser.newContext({serviceWorkers:'block'});
 const errors=[],unexpected=[],requests=[],responseFacts=[];
 const allowed=new Set(['/api/games','/api/games/load-all-for-avatar','/api/oapps/load-all-for-avatar','/api/oapps','/api/oapps/search','/api/avatar/inventory','/api/inventoryitems']);
 try{
  await context.route('**/*',route=>{const req=route.request(),url=new URL(req.url());
   if(url.origin===base.origin){if(url.pathname.startsWith('/api/')){
    requests.push(url.pathname);if(req.method()!=='GET'||!allowed.has(url.pathname)){unexpected.push(req.method()+' '+url.pathname);return route.abort();}}
    return route.continue();}
   if(['data:','blob:'].includes(url.protocol)||['https://fonts.googleapis.com','https://fonts.gstatic.com'].includes(url.origin))return route.continue();
   unexpected.push(url.origin+url.pathname);return route.abort();
  });
  await context.addInitScript(({token,avatarId})=>{localStorage.setItem('avatar',JSON.stringify({id:avatarId}));sessionStorage.setItem('ownedCatalogueBearer',token);},{token,avatarId});
  const page=await context.newPage();page.setDefaultTimeout(15000);page.on('pageerror',e=>errors.push(e.message));
  page.on('response',async response=>{
   const route=new URL(response.url()).pathname;if(!allowed.has(route)||responseFacts.length>=14)return;
   try{const body=await response.json();const message=String(body.message||'');responseFacts.push({route,http:response.status(),isError:body.isError,
    resultArray:Array.isArray(body.result),count:Array.isArray(body.result)?body.result.length:null,
    message:/secret|password|token|connectionstring|credential/i.test(message)?'[redacted]':message.replace(/[a-z]+:\/\/\S+/gi,'[endpoint]').slice(0,200)});}
   catch{responseFacts.push({route,http:response.status(),json:false});}
  });
  await page.goto(baseUrl+'/__storefront_browser');await page.waitForFunction(()=>window.__storefrontReady===true);
  const gamesResponse=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/games');
  await page.locator('#fixture-games').click();
  const gamesHttp=await gamesResponse;
  const gamesBody=await gamesHttp.json();
  const safeMessage=value=>String(value||'').replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g,'[redacted]').replace(/[a-z]+:\/\/\S+/gi,'[endpoint]').slice(0,300);
  const facts={http:gamesHttp.status(),isError:gamesBody.isError,errorCode:gamesBody.errorCode,
   message:/secret|password|token|connectionstring|credential/i.test(gamesBody.message||'')?'[redacted]':safeMessage(gamesBody.message),
   resultArray:Array.isArray(gamesBody.result),count:Array.isArray(gamesBody.result)?gamesBody.result.length:null,
   firstId:Array.isArray(gamesBody.result)?gamesBody.result[0]?.id:null};
  assert.equal(gamesHttp.ok(),true,JSON.stringify(facts));
  assert.equal(gamesBody.isError,false,JSON.stringify(facts));
  assert.ok(Array.isArray(gamesBody.result)&&gamesBody.result.some(row=>row.id===gameId),JSON.stringify(facts));
  await page.locator('#games-all-list article').waitFor();
  await page.locator('#games-all-list summary').click();const game=await page.locator('#games-all-list article').textContent();
  for(const text of [gameId,'Owned developer','Owned publisher','Windows, Linux','Avatar SSO'])assert.ok(game.includes(text),text);
  assert.equal(await page.locator('#games-all-list a').getAttribute('href'),'https://game.example/play');
  await page.locator('#games-modal-block [data-tab="mine"]').click();await page.locator('#games-mine-list article').waitFor();
  await page.locator('#games-filter').fill('no matching game');assert.match(await page.locator('#games-mine-list').textContent(),/No matches/);
  await page.locator('#games-filter').fill('Owned storefront');assert.equal(await page.locator('#games-mine-list article').count(),1);
  await page.locator('#fixture-oapps').click();await page.locator('#oapps-my-list article').waitFor();
  await page.locator('#oapps-my-list summary').click();assert.ok((await page.locator('#oapps-my-list article').textContent()).includes(oappId));
  assert.equal(await page.locator('#oapps-my-list a').getAttribute('href'),'https://app.example/open');
  await page.locator('#oapps-modal-block [data-tab="all"]').click();await page.locator('#oapps-all-list article').waitFor();
  await page.locator('#oapps-modal-block [data-tab="search"]').click();await page.locator('#oapps-search-input').fill('Owned storefront OAPP');
  await page.locator('#oapps-search-btn').click();await page.locator('#oapps-search-results article').waitFor();
  await page.locator('#fixture-inventory').click();await page.waitForFunction(()=>document.getElementById('inv-mine-list').textContent.includes('empty'));
  await page.locator('#inventory-modal-block [data-tab="all"]').click();await page.locator('#inv-all-list article').waitFor();
  await page.locator('#inv-all-list [data-inv-item]').click();await page.locator('#inv-detail').waitFor({state:'visible'});
  assert.ok((await page.locator('#inv-detail-body').textContent()).includes(itemId));
  await page.locator('#inv-detail-close').click();assert.equal(await page.locator('#inv-detail').isHidden(),true);
  await page.evaluate(()=>{window.oasisClient.setToken(null);window.starClient.setToken(null);window.dispatchEvent(new Event('oasis-logout'));});
  for(const id of ['games-all-list','games-mine-list','oapps-my-list','oapps-all-list','oapps-search-results','inv-all-list','inv-mine-list','inv-detail-body'])
   assert.equal(await page.locator('#'+id).textContent(),'');
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);assert.equal(requests.length,7);
  return {storefrontBrowserVerified:true,games:true,oapps:true,inventory:true,zeroBrowserMutations:true};
 }catch(error){throw new Error(error.message+' | requests='+JSON.stringify(requests)+' | responses='+JSON.stringify(responseFacts)+' | browserErrors='+JSON.stringify(errors)+' | blocked='+JSON.stringify(unexpected));}
 finally{await context.close();await browser.close();}
}
module.exports={verify};