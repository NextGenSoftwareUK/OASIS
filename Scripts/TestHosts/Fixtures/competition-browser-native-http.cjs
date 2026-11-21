'use strict';
const assert=require('node:assert/strict'),path=require('node:path');
async function verify({baseUrl,token,avatarId,tournamentId,leagueName,tournamentName}){
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');
 for(const id of [avatarId,tournamentId])assert.match(id,/^[0-9a-f-]{36}$/i);
 assert.ok(leagueName&&tournamentName);
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const browser=await chromium.launch({channel:'chrome',headless:true});
 const context=await browser.newContext({serviceWorkers:'block'}),errors=[],unexpected=[],requests=[];
 let stage='browser setup';
 const leaderboard='/api/competition/leaderboard/Karma/Yearly',stats='/api/competition/stats/Karma/Yearly';
 const tournaments='/api/competition/tournaments',join=tournaments+'/'+tournamentId+'/join';
 try{
  await context.route('**/*',async route=>{
   const req=route.request(),url=new URL(req.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith('/api/')){
     requests.push({path:url.pathname,method:req.method(),competitionType:url.searchParams.get('competitionType')});
     const allowed=req.method()==='GET'&&[leaderboard,stats,tournaments].includes(url.pathname)||
      req.method()==='POST'&&url.pathname===join&&url.searchParams.get('competitionType')==='Karma';
     if(!allowed){unexpected.push(req.method()+' '+url.pathname);return route.abort();}
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
  stage='open fixture';
  await page.goto(baseUrl+'/__competition_browser');await page.waitForFunction(()=>window.__competitionReady===true);
  async function clickRead(selector,route){
   await Promise.all([page.waitForResponse(response=>new URL(response.url()).pathname===route&&response.request().method()==='GET'&&response.status()===200),page.locator(selector).click()]);
  }
  stage='open leaderboard';
  await clickRead('#fixture-open-competition',leaderboard);
  await page.waitForFunction(()=>document.getElementById('comp-leaderboard-list').textContent.includes('0 pts'));
  assert.equal(await page.locator('#comp-leaderboard-list .modal-item-row').count(),1);
  stage='refresh leaderboard';
  await clickRead('#comp-load-btn',leaderboard);
  await page.waitForFunction(()=>document.getElementById('comp-leaderboard-list').textContent.includes('0 pts'));
  stage='rank and league';
  await clickRead('#competition-modal-block [data-tab="myrank"]',stats);
  await page.waitForFunction(name=>document.getElementById('comp-stat-league').textContent===name,leagueName);
  assert.equal(await page.locator('#comp-stat-rank').textContent(),'1');
  stage='tournament catalogue';
  await clickRead('#competition-modal-block [data-tab="tournaments"]',tournaments);
  await page.waitForFunction(name=>document.getElementById('comp-tournaments-list').textContent.includes(name),tournamentName);
  assert.equal(await page.locator('#comp-stat-tournaments').textContent(),'1');
  stage='tournament join';
  await clickRead('[data-join-tournament="'+tournamentId+'"]',tournaments);
  await page.waitForFunction(()=>document.getElementById('comp-tournaments-list').textContent.includes('Registered'));
  assert.equal(await page.locator('[data-join-tournament]').count(),0);
  assert.equal(requests.filter(r=>r.method==='POST'&&r.path===join).length,1);
  stage='close and reopen';
  await page.locator('#competition-close-btn').click();
  assert.equal(await page.locator('.js-modal').isVisible(),false);
  await clickRead('#fixture-open-competition',leaderboard);
  await page.waitForFunction(()=>document.getElementById('comp-leaderboard-list').textContent.includes('0 pts'));
  await page.evaluate(()=>{window.starClient.setToken(null);window.dispatchEvent(new Event('oasis-logout'));});
  for(const id of ['comp-leaderboard-list','comp-myrank-panel','comp-tournaments-list'])assert.equal(await page.locator('#'+id).textContent(),'');
  assert.equal(await page.locator('.js-modal').isVisible(),false);
  assert.equal(requests.length,7);assert.equal(requests.filter(r=>r.method==='GET').length,6);
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);
  return {competitionPopupVerified:true,leaderboard:true,rank:true,tournaments:true,joinAcknowledged:true,reopen:true,logoutClears:true};
 }catch(error){
  const page=context.pages()[0];
  const status=page?await page.locator('#competition-status').textContent().catch(()=>null):null;
  error.message='Competition stage='+stage+'; status='+status+'; requests='+JSON.stringify(requests)+'; '+error.message;
  throw error;
 }finally{await context.close();await browser.close();}
}
module.exports={verify};
