'use strict';
const assert=require('node:assert/strict');
const path=require('node:path');
const {randomUUID}=require('node:crypto');

async function verify({baseUrl,token,providerType,avatarId}) {
  const base=new URL(baseUrl);
  assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');
  assert.ok(['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS'].includes(providerType));
  const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
  const context=await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,
    'browser-profile-'+randomUUID()),{channel:'chrome',headless:true,timeout:30000,
    viewport:{width:1440,height:1000},acceptDownloads:false,serviceWorkers:'block'});
  const writes=[],reads=[],errors=[],forbidden=[];
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
    const page=await context.newPage();page.setDefaultTimeout(10000);
    page.on('pageerror',error=>errors.push(error.message));
    page.on('request',request=>{
      const url=new URL(request.url());if(!url.pathname.startsWith('/api/'))return;
      const item={method:request.method(),path:url.pathname};
      if(request.method()==='GET')reads.push(item);
      else writes.push({...item,body:request.postDataJSON()});
    });
    await page.goto(baseUrl+'/__profile_browser');
    await page.locator('#fixture-open-hyperdrive').click();
    const form=page.locator('#hd-settings-form');await form.waitFor({state:'visible'});
    await form.locator('[name="autoFailoverEnabled"]').selectOption('true');
    await form.locator('[name="autoFailoverProviders"]').fill(providerType);
    await form.locator('[name="autoReplicationEnabled"]').selectOption('false');
    await form.locator('[name="autoReplicationProviders"]').fill('');
    await form.locator('[name="autoLoadBalancingEnabled"]').selectOption('false');
    await form.locator('[name="loadBalancingProviders"]').fill('');
    await form.locator('button[type="submit"]').click();
    await page.waitForFunction(()=>document.getElementById('hd-settings-feedback').textContent==='Routing preferences saved.');
    assert.equal(await form.locator('[name="autoFailoverProviders"]').inputValue(),providerType);
    assert.match(await page.locator('#hd-effective-autoFailoverEnabled').innerText(),new RegExp(providerType));
    await page.locator('#hd-close-btn').click();
    assert.equal(await form.isVisible(),false);
    await page.locator('#fixture-open-hyperdrive').click();await form.waitFor({state:'visible'});
    assert.equal(await form.locator('[name="autoFailoverEnabled"]').inputValue(),'true');
    assert.equal(await form.locator('[name="autoFailoverProviders"]').inputValue(),providerType);
    assert.equal(await form.locator('[name="autoReplicationEnabled"]').inputValue(),'false');
    assert.equal(await form.locator('[name="autoLoadBalancingEnabled"]').inputValue(),'false');
    assert.equal(writes.length,1);assert.equal(writes[0].method,'PUT');
    assert.equal(writes[0].path,'/api/settings/hyperdrive-settings');
    assert.equal(writes[0].body.autoFailoverProviders,providerType);
    assert.equal(reads.length,3);assert.ok(reads.every(r=>r.path==='/api/settings/hyperdrive-settings'));
    assert.deepEqual(forbidden,[]);assert.deepEqual(errors,[]);
    await page.locator('#hd-close-btn').click();
    return {hyperDriveSettingsBrowserVerified:true,providerType,saveRequests:writes.length,reads:reads.length};
  } finally {await context.close();}
}
module.exports={verify};
