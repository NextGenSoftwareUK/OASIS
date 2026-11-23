'use strict';
const assert=require('node:assert/strict'),path=require('node:path');
async function verify({baseUrl,token,avatarId,nodeId}){
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');
 assert.match(avatarId,/^[a-f0-9-]{36}$/i);assert.ok(nodeId&&typeof token==='string');
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const browser=await chromium.launch({channel:'chrome',headless:true});
 const context=await browser.newContext({serviceWorkers:'block',viewport:{width:1280,height:900}});
 const errors=[],unexpected=[],requests=[];
 let stage='preparation';
 try{
  await context.route('**/*',async route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith('/api/')){
     if(!url.pathname.startsWith('/api/v1/onode/')){unexpected.push(url.pathname);return route.abort();}
     requests.push({path:url.pathname,method:request.method()});
    }
    return route.continue();
   }
   if(['data:','blob:'].includes(url.protocol)||
      ['https://fonts.googleapis.com','https://fonts.gstatic.com'].includes(url.origin))return route.continue();
   unexpected.push(url.origin+url.pathname);return route.abort();
  });
  await context.addInitScript(({token,avatarId})=>{
   localStorage.setItem('loggedIn','true');
   localStorage.setItem('avatar',JSON.stringify({id:avatarId,jwtToken:token}));
  },{token,avatarId});
  const page=await context.newPage();page.setDefaultTimeout(20000);
  page.on('pageerror',error=>errors.push(error.name));
  stage='actual popup';await page.goto(base.origin+'/__onode_browser');
  await page.waitForFunction(()=>window.__onodeFixtureReady===true);
  await page.locator('#fixture-open-onode').click();
  await page.locator('#onode-target option').filter({hasText:nodeId}).waitFor({state:'attached'});
  await page.locator('#onode-target').selectOption(nodeId);
  await page.waitForFunction(id=>document.getElementById('onode-info-grid').textContent.includes(id),nodeId);
  async function tab(name){await page.locator('.onode-tab[data-tab="'+name+'"]').click();}
  async function publicView(id,source){
   await page.waitForFunction(({id,source})=>{try{const value=JSON.parse(document.getElementById(id).textContent);return !!value&&(!source||value.source===source);}catch{return false;}},{id,source});
   const value=JSON.parse(await page.locator('#'+id).textContent());
   assert.ok(!JSON.stringify(value).includes(token));return value;
  }
  stage='registered configuration';await tab('config');
  assert.equal((await publicView('onode-config')).nodeId,nodeId);
  stage='saved DNA';await tab('oasisdna');
  const savedDna=await publicView('onode-oasisdna','Saved node OASIS DNA');
  assert.equal(savedDna.source,'Saved node OASIS DNA');
  assert.equal(savedDna.routingPolicy.source,'OASIS.OASISHyperDriveConfig');
  stage='provider membership';await tab('providers');
  await page.locator('#onode-provider-role').waitFor();
  function mongoRow(role){return page.locator('#onode-providers-grid .onode-svc-row').filter({
   has:page.locator('button[data-provider="MongoDBOASIS"]'+(role?'[data-role="'+role+'"]':''))});}
  await mongoRow().getByRole('button',{name:'Remove',exact:true}).click();
  await mongoRow().getByRole('button',{name:'Include',exact:true}).waitFor();
  await page.waitForFunction(()=>document.getElementById('onode-modal-status').classList.contains('onode-status--success'));
  await mongoRow().getByRole('button',{name:'Include',exact:true}).click();
  await mongoRow().getByRole('button',{name:'Remove',exact:true}).waitFor();
  await page.waitForFunction(()=>document.getElementById('onode-modal-status').classList.contains('onode-status--success'));
  assert.ok((await page.locator('#onode-modal-status').textContent()).includes('Live routing has not been changed'));
  const providerName=await mongoRow().locator('.onode-provider-id').boundingBox();
  const providerActions=await mongoRow().locator('.onode-row-actions').boundingBox();
  assert.ok(providerName&&providerActions&&providerName.width>=80&&providerActions.x>=providerName.x+providerName.width);
  stage='replication role edit';await page.locator('#onode-provider-role').selectOption('replication');
  await mongoRow('replication').getByRole('button',{name:'Remove',exact:true}).click();
  await mongoRow('replication').getByRole('button',{name:'Include',exact:true}).waitFor();
  stage='load balancing role read';await page.locator('#onode-provider-role').selectOption('loadBalancing');
  await mongoRow('loadBalancing').getByRole('button',{name:'Remove',exact:true}).waitFor();
  stage='reported services';await tab('services');
  assert.ok((await page.locator('#onode-services-grid').textContent()).length>0);
  stage='reported metrics';await tab('metrics');
  assert.ok((await page.locator('#onode-metrics-grid').textContent()).includes('Peers connected'));
  stage='live updates';
  const liveFrame=page.waitForEvent('websocket',{predicate:socket=>new URL(socket.url()).pathname==='/ws/onode/'+nodeId})
   .then(socket=>socket.waitForEvent('framereceived',{predicate:frame=>{try{return JSON.parse(String(frame.payload)).nodeId===nodeId;}catch{return false;}}}));
  await page.locator('#onode-live-updates').check();await liveFrame;
  await page.waitForFunction(()=>document.getElementById('onode-status-label').textContent==='REGISTERED NODE');
  stage='close';await page.locator('#onode-close-btn').click();
  assert.equal(await page.locator('.js-modal.is-visible').count(),0);
  stage='API-managed loaded configuration';await page.locator('#fixture-open-onode').click();
  await page.locator('#onode-target').selectOption('');
  await tab('oasisdna');const loadedDna=await publicView('onode-oasisdna','Loaded WEB4 OASIS DNA');
  assert.equal(loadedDna.source,'Loaded WEB4 OASIS DNA');
  assert.equal(loadedDna.routingPolicy.source,'OASIS.OASISHyperDriveConfig');
  await page.locator('#onode-close-btn').click();
  assert.equal(await page.locator('.js-modal.is-visible').count(),0);
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);
  assert.ok(requests.some(request=>request.path==='/api/v1/onode/commands'&&request.method==='POST'));
  assert.ok(requests.some(request=>request.path==='/api/v1/onode/public-config'));
  return {actualOnodePopup:true,registeredPublicViews:true,savedRoleWriteReadback:true,
   roleSelection:true,providerLayout:true,reportedServicesMetrics:true,realLiveFrame:true,loadedApiView:true,closed:true,
   apiMocks:false,errors:0};
 }catch(error){throw new Error('Owned ONODE browser failed at '+stage+'; type='+error.name+'; credential-bearing detail withheld.');}
 finally{await context.close();await browser.close();}
}
module.exports={verify};
