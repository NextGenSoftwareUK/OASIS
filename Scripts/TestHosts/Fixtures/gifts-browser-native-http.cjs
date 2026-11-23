'use strict';
const assert=require('node:assert/strict'),path=require('node:path');
async function verify({baseUrl,token,avatarId,recipientId,recipientToken}) {
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const browser=await chromium.launch({channel:'chrome',headless:true});
 const errors=[],unexpected=[],writes=[];
 async function actor(id,bearer){
  const context=await browser.newContext({serviceWorkers:'block'});
  await context.route('**/*',route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith('/api/')){
     const permitted=request.method()==='GET'&&['/api/gifts/my-gifts','/api/gifts/stats','/api/gifts/history'].includes(url.pathname)||
      request.method()==='POST'&&/^\/api\/gifts\/(?:send-gift|receive-gift|open-gift)\/[0-9a-f-]{36}$/i.test(url.pathname);
     if(!permitted){unexpected.push(request.method()+' '+url.pathname);return route.abort();}
     if(request.method()==='POST')writes.push(url.pathname);
    }
    return route.continue();
   }
   if(['data:','blob:'].includes(url.protocol)||['https://fonts.googleapis.com','https://fonts.gstatic.com'].includes(url.origin))return route.continue();
   unexpected.push(url.origin+url.pathname);return route.abort();
  });
  await context.addInitScript(({id,bearer})=>{
   localStorage.setItem('avatar',JSON.stringify({id}));sessionStorage.setItem('ownedGiftsBearer',bearer);
  },{id,bearer});
  const page=await context.newPage();page.setDefaultTimeout(15000);page.on('pageerror',e=>errors.push(e.message));
  await page.goto(baseUrl+'/__gifts_browser');await page.waitForFunction(()=>window.__giftsReady===true);
  await page.locator('#fixture-open').click();
  await page.waitForFunction(()=>!document.getElementById('gifts-inbox-list').textContent.includes('Loading'));
  return {context,page};
 }
 let sender,recipient;
 try{
  sender=await actor(avatarId,token);
  await sender.page.locator('[data-tab="send"]').click();
  await sender.page.locator('#gifts-to-id').fill(recipientId);
  await sender.page.locator('#gifts-message').fill('Owned real browser gift');
  const sentReply=sender.page.waitForResponse(r=>new URL(r.url()).pathname==='/api/gifts/send-gift/'+recipientId&&r.request().method()==='POST');
  await sender.page.locator('#gifts-send-btn').click();
  const sent=await (await sentReply).json();assert.equal(sent.isError,false);const gift=sent.result;
  assert.equal(gift.fromAvatarId,avatarId);assert.equal(gift.toAvatarId,recipientId);assert.match(gift.id,/^[0-9a-f-]{36}$/i);
  await sender.page.waitForFunction(()=>document.getElementById('gifts-message').value==='');
  recipient=await actor(recipientId,recipientToken);
  await recipient.page.locator('[data-gift-receive="'+gift.id+'"]').click();
  await recipient.page.locator('[data-gift-open="'+gift.id+'"]').click();
  await recipient.page.waitForFunction(()=>document.getElementById('gifts-inbox-list').textContent.includes('Opened'));
  await recipient.page.locator('[data-tab="history"]').click();
  await recipient.page.waitForFunction(()=>document.getElementById('gifts-history-list').textContent.includes('Opened'));
  for(const item of [sender,recipient]){
   await item.page.evaluate(()=>{window.oasisClient.setToken(null);localStorage.removeItem('avatar');window.dispatchEvent(new Event('oasis-logout'));});
   assert.equal(await item.page.locator('#gifts-inbox-list').textContent(),'');
   assert.equal(await item.page.locator('#gifts-history-list').textContent(),'');
   assert.equal(await item.page.locator('#gifts-send-btn').isEnabled(),true);
  }
  assert.deepEqual(writes,['/api/gifts/send-gift/'+recipientId,'/api/gifts/receive-gift/'+gift.id,'/api/gifts/open-gift/'+gift.id]);
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);
  return {giftsBrowserVerified:true,sendReceiveOpenHistory:true,logoutClears:true};
 }finally{if(sender)await sender.context.close();if(recipient)await recipient.context.close();await browser.close();}
}
module.exports={verify};