'use strict';
const assert=require('node:assert/strict'),path=require('node:path');
async function verify({baseUrl,token,avatarId}){
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');
 assert.match(avatarId,/^[0-9a-f-]{36}$/i);
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const browser=await chromium.launch({channel:'chrome',headless:true}),context=await browser.newContext({serviceWorkers:'block'});
 const writes=[],unexpected=[],errors=[];let stage='setup';
 const endpoint='/api/subscription/usage/alerts/email';
 // Opening the shipping Usage tab also reads these sibling sections; none may write or use a shared origin.
 const reads=new Set(['/api/subscription/usage','/api/subscription/usage/notifications',
  '/api/subscription/usage/alerts','/api/subscription/app-owner/apps',
  '/api/subscription/overages/prices','/api/subscription/overages/statements']);
 try{
  await context.route('**/*',route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith('/api/')){
     if(request.method()==='POST'&&url.pathname===endpoint)writes.push(url.pathname);
     else if(!(request.method()==='GET'&&reads.has(url.pathname))){unexpected.push(request.method()+' '+url.pathname);return route.abort();}
    }
    return route.continue();
   }
   if(['data:','blob:'].includes(url.protocol)||['https://fonts.googleapis.com','https://fonts.gstatic.com'].includes(url.origin))return route.continue();
   unexpected.push(url.origin+url.pathname);return route.abort();
  });
  await context.addInitScript(({token,avatarId})=>{
   window.fixtureSubscriptionActor={id:avatarId,token:token};
   localStorage.setItem('loggedIn','true');localStorage.setItem('avatar',JSON.stringify({id:avatarId,jwtToken:token}));
  },{token,avatarId});
  const page=await context.newPage();page.setDefaultTimeout(20000);page.on('pageerror',error=>errors.push(error.name));
  await page.goto(baseUrl+'/__warning_email_browser');
  await page.locator('#fixture-open-subscription').click();
  await page.locator('#uw-send-email').waitFor({state:'visible'});
  async function submit(accepted,previous){
   const [response]=await Promise.all([page.waitForResponse(value=>new URL(value.url()).pathname===endpoint&&value.request().method()==='POST'),page.locator('#uw-send-email').click()]);
   assert.equal(response.status(),200);
   const envelope=await response.json();assert.equal(envelope.isError,false);
   assert.equal(envelope.result.accepted,accepted);assert.equal(envelope.result.previouslyAccepted,previous);assert.equal(envelope.result.deferred,0);
   await page.waitForFunction(({accepted,previous})=>document.getElementById('uw-msg').textContent.startsWith(accepted+' accepted for email, '+previous+' already submitted.'),{accepted,previous});
  }
  stage='new provider submission';await submit(1,0);
  stage='already accepted action';await submit(0,1);
  assert.equal(writes.length,2);assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);
  return {warningEmailBrowserVerified:true,newSubmission:true,previouslyAccepted:true,explicitActions:2};
 }catch(error){throw new Error('Warning email browser failed at '+stage+' ('+error.name+').');}
 finally{await context.close();await browser.close();}
}
module.exports={verify};
