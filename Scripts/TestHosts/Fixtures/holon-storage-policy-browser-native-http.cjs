'use strict';
const assert=require('node:assert/strict');
const path=require('node:path');
const {randomUUID}=require('node:crypto');

async function verify({baseUrl,token,providerType,avatarId,applicationId}) {
  const base=new URL(baseUrl);
  assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');
  assert.ok(['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS'].includes(providerType));
  assert.match(applicationId,/^[0-9a-f-]{36}$/);
  const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
  const context=await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-'+randomUUID()),{channel:'chrome',headless:true,timeout:30000,
    viewport:{width:1440,height:1000},acceptDownloads:false,serviceWorkers:'block'});
  const writes=[],reads=[],errors=[],forbidden=[];
  const replies=[],replyReads=[];
  let page,phase='initial load';
  try {
    await context.route('**/*',route=>{
      const url=new URL(route.request().url());
      if(url.origin===base.origin||url.protocol==='data:')return route.continue();
      if(url.pathname.startsWith('/api/'))forbidden.push(url.pathname);
      return route.abort('blockedbyclient');
    });
    await context.addInitScript(({token,avatarId})=>{
      localStorage.setItem('loggedIn','true');
      localStorage.setItem('avatar',JSON.stringify({id:avatarId,jwtToken:token}));
    },{token,avatarId});
    page=await context.newPage();page.setDefaultTimeout(10000);
    page.on('pageerror',error=>errors.push(error.message));
    page.on('request',request=>{
      const url=new URL(request.url());if(!url.pathname.startsWith('/api/'))return;
      const item={method:request.method(),path:url.pathname,app:url.searchParams.get('applicationId')};
      if(request.method()==='GET')reads.push(item);
      else writes.push({...item,body:request.method()==='PUT'?request.postDataJSON():null});
    });
    page.on('response',response=>{
      if(new URL(response.url()).pathname!=='/api/settings/holon-storage-policy')return;
      replyReads.push((async()=>{
        const value=await response.json();
        const selectedPolicy=p=>p===null?null:p&&typeof p==='object'
          ? {keys:Object.keys(p),schemaVersion:p.schemaVersion,mode:p.mode,readCache:p.readCache,maximumCacheBytes:p.maximumCacheBytes,
            maximumCacheAgeSeconds:p.maximumCacheAgeSeconds,materializeOnExplicitSave:p.materializeOnExplicitSave}
          : {type:typeof p};
        replies.push({method:response.request().method(),status:response.status(),isError:value.isError,
          errorCode:value.errorCode,result:typeof value.result==='boolean'?value.result:{
            keys:Object.keys(value.result||{}),applicationId:value.result?.applicationId,avatarPolicy:selectedPolicy(value.result?.avatarPolicy),
            applicationPolicy:selectedPolicy(value.result?.applicationPolicy),
            effectivePolicy:selectedPolicy(value.result?.effectivePolicy),memoryCacheSupported:value.result?.memoryCacheSupported}});
      })().catch(error=>replies.push({method:response.request().method(),status:response.status(),responseBodyReadError:error.name})));
    });
    await page.goto(baseUrl+'/__profile_browser?panel=storage');
    await page.locator('#fixture-open-hyperdrive').click();
    const form=page.locator('#hd-storage-form');
    const field=name=>form.locator('[name="'+name+'"]');
    const ready=()=>page.waitForFunction(()=>!document.getElementById('hd-storage-save').disabled&&
      document.getElementById('hd-storage-feedback').textContent==='');
    const save=async()=>{
      await page.locator('#hd-storage-save').click();
      await page.waitForFunction(()=>document.getElementById('hd-storage-feedback').textContent===
        'Storage settings saved. Existing records have not been moved or hidden.');
    };
    await ready();assert.equal(await field('mode').inputValue(),'');
    assert.equal(await field('readCache').locator('option[value="Memory"]').isDisabled(),true);
    await field('mode').selectOption('Hybrid');
    phase='save avatar Hybrid';await field('materializeOnExplicitSave').uncheck();await save();
    assert.equal(await field('mode').inputValue(),'Hybrid');
    assert.equal(await field('materializeOnExplicitSave').isChecked(),false);
    phase='load application inheritance';await field('scope').selectOption('application');await field('applicationId').fill(applicationId);
    await page.locator('#hd-storage-load').click();await ready();
    assert.equal(await field('mode').inputValue(),'');
    assert.match(await page.locator('#hd-storage-effective').innerText(),/Hybrid.*inherited/);
    phase='save application Virtual';await field('mode').selectOption('Virtual');await save();
    assert.equal(await field('mode').inputValue(),'Virtual');
    phase='reset application';await field('mode').selectOption('');await save();
    assert.match(await page.locator('#hd-storage-effective').innerText(),/Hybrid.*inherited/);
    phase='reload avatar';await field('scope').selectOption('avatar');await page.locator('#hd-storage-load').click();await ready();
    assert.equal(await field('mode').inputValue(),'Hybrid');
    phase='reset avatar';await field('mode').selectOption('');await save();
    assert.match(await page.locator('#hd-storage-effective').innerText(),/No saved override/);
    phase='reopen';await page.locator('#hd-close-btn').click();await page.locator('#fixture-open-hyperdrive').click();await ready();
    assert.equal(await field('mode').inputValue(),'');
    assert.equal(writes.length,4);assert.deepEqual(writes.map(r=>r.method),['PUT','PUT','DELETE','DELETE']);
    assert.deepEqual(writes.map(r=>r.app),[null,applicationId,applicationId,null]);
    assert.deepEqual(writes[0].body,{schemaVersion:1,mode:'Hybrid',readCache:'None',
      maximumCacheBytes:0,maximumCacheAgeSeconds:0,materializeOnExplicitSave:false});
    assert.equal(writes[1].body.mode,'Virtual');assert.equal(writes[1].body.materializeOnExplicitSave,false);
    assert.equal(reads.length,8);
    assert.ok([...reads,...writes].every(r=>r.path==='/api/settings/holon-storage-policy'));
    assert.deepEqual(forbidden,[]);assert.deepEqual(errors,[]);
    await page.locator('#hd-close-btn').click();
    return {holonStoragePolicyBrowserVerified:true,providerType,saveRequests:writes.length,reads:reads.length,
      avatarApplicationInheritanceAndResetVerified:true};
  } catch(error) {
    await Promise.allSettled(replyReads);
    const feedback=page?await page.locator('#hd-storage-feedback').textContent({timeout:1000}):'page not created';
    throw new Error('Storage phase '+phase+' failed; feedback='+JSON.stringify(feedback)+
      '; writes='+writes.length+'; reads='+reads.length+'; replies='+JSON.stringify(replies)+'; '+error.message);
  } finally {await context.close();}
}
module.exports={verify};
