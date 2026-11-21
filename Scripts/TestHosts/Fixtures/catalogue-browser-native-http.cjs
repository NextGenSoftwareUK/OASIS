'use strict';
const assert=require('node:assert/strict'),path=require('node:path');
async function verify({baseUrl,token,avatarId,missionId,questId,chapterId}){
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');
 for(const id of [avatarId,missionId,questId,chapterId])assert.match(id,/^[0-9a-f-]{36}$/i);
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const browser=await chromium.launch({channel:'chrome',headless:true});
 const context=await browser.newContext({serviceWorkers:'block'}),errors=[],unexpected=[],requests=[];
 const allowed=new Set(['/api/quests/all-for-avatar','/api/quests','/api/quests/search','/api/quests/'+questId,
  '/api/missions/load-all-for-avatar','/api/missions','/api/missions/search','/api/missions/'+missionId]);
 try{
  await context.route('**/*',async route=>{
   const req=route.request(),url=new URL(req.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith('/api/')){
     requests.push({path:url.pathname,method:req.method(),query:url.searchParams.get('query')});
     if(req.method()!=='GET'||!allowed.has(url.pathname)){unexpected.push(url.pathname);return route.abort();}
    }
    return route.continue();
   }
   if(['data:','blob:'].includes(url.protocol)||['https://fonts.googleapis.com','https://fonts.gstatic.com'].includes(url.origin))return route.continue();
   unexpected.push(url.origin+url.pathname);return route.abort();
  });
  await context.addInitScript(({token,avatarId})=>{
   localStorage.setItem('avatar',JSON.stringify({id:avatarId}));sessionStorage.setItem('ownedCatalogueBearer',token);
  },{token,avatarId});
  const page=await context.newPage();page.setDefaultTimeout(20000);page.on('pageerror',error=>errors.push(error.message));
  await page.goto(baseUrl+'/__catalogue_browser');await page.waitForFunction(()=>window.__catalogueReady===true);
  for(const item of [{kind:'quests',id:questId,name:'Owned catalogue quest'},
   {kind:'missions',id:missionId,name:'Owned catalogue mission'}]){
   const singular=item.kind.slice(0,-1),card='#'+item.kind+'-my-list > article';
   await page.locator('#fixture-open-'+item.kind).click();
   await page.waitForFunction(({kind,name})=>document.getElementById(kind+'-my-list').textContent.includes(name),item);
   assert.equal(await page.locator(card).count(),1);
   await page.locator(card+' [data-'+singular+'-index]').click();
   await page.locator('#'+item.kind+'-detail').waitFor({state:'visible'});
   const detail=await page.locator('#'+item.kind+'-detail-body').textContent();
   assert.ok(detail.includes(item.id));assert.ok(detail.includes(item.name));
   if(item.kind==='missions')assert.ok(detail.includes(chapterId));
   await page.locator('#'+item.kind+'-detail-close-btn').click();
   assert.equal(await page.locator('#'+item.kind+'-detail').isHidden(),true);
   await page.locator('.'+item.kind+'-tab[data-tab="all"]').click();
   await page.waitForFunction(({kind,name})=>document.getElementById(kind+'-all-list').textContent.includes(name),item);
   assert.equal(await page.locator('#'+item.kind+'-all-list > article').count(),1);
   await page.locator('.'+item.kind+'-tab[data-tab="search"]').click();
   await page.locator('#'+item.kind+'-search-input').fill(item.name);
   await page.locator('#'+item.kind+'-search-btn').click();
   await page.waitForFunction(({kind,name})=>document.getElementById(kind+'-search-results').textContent.includes(name),item);
   await page.locator('#'+item.kind+'-refresh-btn').click();
   await page.waitForFunction(({kind,name})=>document.getElementById(kind+'-search-results').textContent.includes(name)&&
    !document.getElementById(kind+'-refresh-btn').disabled,item);
   const search=requests.filter(r=>r.path==='/api/'+item.kind+'/search');
   assert.equal(search.length,2);assert.ok(search.every(r=>r.query===item.name));
   assert.equal(await page.locator('#'+item.kind+'-search-results > article').count(),1);
   if(item.kind==='quests'){
    await page.locator('#quests-modal-close-btn').click();
    assert.equal(await page.locator('.js-modal').isVisible(),false);
   }
  }
  await page.evaluate(()=>{window.starClient.setToken(null);window.dispatchEvent(new Event('oasis-logout'));});
  for(const kind of ['quests','missions'])for(const suffix of ['my-list','all-list','search-results','detail-body'])
   assert.equal(await page.locator('#'+kind+'-'+suffix).textContent(),'');
  for(const route of ['/api/quests/'+questId,'/api/missions/'+missionId])assert.equal(requests.filter(r=>r.path===route).length,1);
  assert.equal(requests.length,10);assert.ok(requests.every(r=>r.method==='GET'));
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);
  return {cataloguePopupsVerified:true,quests:true,missions:true,details:true,searchAndRefresh:true,logoutClears:true,zeroMutations:true};
 }finally{await context.close();await browser.close();}
}
module.exports={verify};
