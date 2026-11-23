'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs/promises'),path=require('node:path');
const http=require('node:http'),https=require('node:https'),{execFileSync}=require('node:child_process');
const {createOwnedAvatar}=require('./startup-authentication.cjs');
let stage='preparation';
const httpFacts=[];
async function main(){
 assert.equal(process.env.GITHUB_ACTIONS,'true');
 const api=new URL(process.env.OASIS_STARTUP_API_URL);assert.equal(api.hostname,'127.0.0.1');assert.equal(api.protocol,'http:');
 const api5=process.env.OASIS_STARTUP_WEB5_URL?new URL(process.env.OASIS_STARTUP_WEB5_URL):null;
 if(api5){assert.equal(api5.hostname,'127.0.0.1');assert.equal(api5.protocol,'http:');}
 const root=await fs.realpath(process.env.OASIS_STARTUP_PORTAL_ROOT),owned=await fs.realpath(process.env.OASIS_PROFILE_BROWSER_RUNTIME);
 const {chromium}=require(path.join(owned,'node_modules/playwright-core'));
 const cert=path.join(owned,'owned-localhost.crt'),key=path.join(owned,'owned-localhost.key');
 execFileSync('openssl',['req','-x509','-newkey','rsa:2048','-nodes','-days','1','-subj','/CN=127.0.0.1',
  '-addext','subjectAltName=IP:127.0.0.1','-keyout',key,'-out',cert],{stdio:'ignore'});
 const types={'.html':'text/html','.js':'application/javascript','.css':'text/css','.svg':'image/svg+xml','.png':'image/png','.jpg':'image/jpeg','.woff2':'font/woff2'};
 const server=http.createServer(async(req,res)=>{
  try{
   const url=new URL(req.url,'http://127.0.0.1');
   const name=['/','/beamin','/subscription'].includes(url.pathname)?'portal.html':decodeURIComponent(url.pathname).replace(/^\//,'');
   const file=await fs.realpath(path.resolve(root,name));if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
   const body=await fs.readFile(file);res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream'});res.end(body);
  }catch{res.writeHead(404);res.end();}
 });
 // Transparent transport proxy: production Startup supplies every API status, header and body.
 function createProxy(target){return https.createServer({key:keyBytes,cert:certBytes},(req,res)=>{
  const forward=http.request(new URL(req.url,target),{method:req.method,headers:{...req.headers,host:target.host,'x-forwarded-proto':'https'}},upstream=>{
   res.writeHead(upstream.statusCode,upstream.headers);upstream.pipe(res);
  });forward.on('error',()=>res.destroy());req.pipe(forward);
 });}
 const keyBytes=await fs.readFile(key),certBytes=await fs.readFile(cert);
 const proxy=createProxy(api),proxy5=api5?createProxy(api5):null;
 let browser,context;
 try{
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));await new Promise(resolve=>proxy.listen(0,'127.0.0.1',resolve));
  if(proxy5)await new Promise(resolve=>proxy5.listen(0,'127.0.0.1',resolve));
  const origin='http://127.0.0.1:'+server.address().port,proxyOrigin='https://127.0.0.1:'+proxy.address().port;
  const proxy5Origin=proxy5?'https://127.0.0.1:'+proxy5.address().port:null;
  stage='owned registration';const account=await createOwnedAvatar(api.href);
  if(api5){
   const signed=await account.call('free account sign-in','POST','/api/avatar/authenticate',{username:account.username,password:account.password},undefined,true);
   await account.call('free subscription','POST','/api/subscription/checkout/session',{planId:'free',successUrl:origin+'/subscription?tab=mysubs',cancelUrl:origin+'/'},signed.jwtToken);
   const subs=await account.call('free readback','GET','/api/subscription/subscriptions/me',undefined,signed.jwtToken);
   assert.ok(Array.isArray(subs)&&subs.some(s=>s.planId==='free'&&s.status==='active'),'Persisted free subscription');
  }
  browser=await chromium.launch({channel:'chrome',headless:true});
  context=await browser.newContext({serviceWorkers:'block',ignoreHTTPSErrors:true});
  const errors=[],otherServices=[],reads=[];
  await context.route('**/*',async route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.origin==='https://api.web4.oasisomniverse.one'){
    const mutation=request.method()!=='GET'&&request.method()!=='OPTIONS';
    if(!url.pathname.startsWith('/api/')||(mutation&&!['/api/avatar/authenticate','/api/avatar/refresh-token'].includes(url.pathname)))
     throw new Error('Unadmitted browser API operation.');
    return route.continue({url:proxyOrigin+url.pathname+url.search});
   }
   if(api5&&url.origin==='https://api.starnet.oasisomniverse.one'){
    if(request.method()!=='GET'&&request.method()!=='OPTIONS'){otherServices.push('unadmitted WEB5 mutation');return route.abort();}
    return route.continue({url:proxy5Origin+url.pathname+url.search});
   }
   if(url.origin===origin||['data:','blob:'].includes(url.protocol)||['fonts.googleapis.com','fonts.gstatic.com'].includes(url.hostname))return route.continue();
   otherServices.push(request.method()+' '+url.origin+url.pathname);return route.abort();
  });
  const page=await context.newPage();page.on('pageerror',()=>errors.push('page-error'));
  page.on('response',async response=>{
   const url=new URL(response.url());if(!url.pathname.startsWith('/api/'))return;
   const fact={path:url.pathname,status:response.status()};reads.push(fact);
   if(httpFacts.length<64)httpFacts.push(fact);
   try{const body=await response.json(),result=typeof body.isError==='boolean'?body:body.result;
    fact.isError=result?.isError;const code=result?.errorCode;
    if(typeof code==='string'&&/^[A-Z0-9_]{1,80}$/.test(code))fact.errorCode=code;
    if(code==='HYPERDRIVE_ROUTE_FAILED'&&typeof result.message==='string'){
     const message=result.message.replace(/(?:https?|mongodb(?:\+srv)?):\/\/[^\s]+/gi,'[URI]')
      .replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g,'[JWT]')
      .replace(/[a-f0-9]{32,}/gi,'[identifier]').slice(0,320);
     fact.routingMessage=/password|secret|credential|connectionstring|refresh.?token/i.test(message)?'[sensitive routing detail withheld]':message;
    }
   }catch{fact.json=false;}
  });
  stage='page and Beam In';await page.goto(origin+'/',{waitUntil:'domcontentloaded'});
  await page.locator('.js-modal-trigger[data-signin="login"]:visible').first().click();
  await page.locator('#login-email').fill(account.username);await page.locator('#login-password').fill(account.password);
  await page.locator('#login-submit').click();
  await page.waitForFunction(()=>localStorage.getItem('loggedIn')==='true');
  stage='dashboard';await page.locator('#dashboard').waitFor({state:'visible'});
  assert.equal(await page.locator('#dash-name').textContent(),account.username);
  if(api5){
   stage='WEB5 Games dashboard';await page.locator('#dash-card-games').scrollIntoViewIfNeeded();
   await page.waitForFunction(()=>document.getElementById('dash-card-games').textContent==='0');
   await page.locator('#dash-card-games').click();
   await page.waitForFunction(()=>document.getElementById('games-stat-total').textContent==='0');
   await page.locator('#games-modal-block [data-tab="mine"]').click();
   await page.waitForFunction(()=>document.getElementById('games-stat-mine').textContent==='0');
   await page.locator('#games-close-btn').click();await page.waitForURL(url=>url.pathname==='/');
   stage='WEB5 OAPPs';await page.locator('#dashboard [onclick*="openOAPPsModal"]').first().click();
   await page.waitForFunction(()=>document.getElementById('oapps-my-list').textContent.includes('no OAPPs'));
   assert.deepEqual(errors,[]);
   for(const endpoint of ['/api/games','/api/games/load-all-for-avatar','/api/oapps/load-all-for-avatar'])
    assert.ok(reads.some(read=>read.path===endpoint&&read.status===200),'Actual WEB5 catalogue read');
   console.log('PORTAL_STARTUP_BROWSER_PROOF: actual page Beam In and WEB5 Games/OAPPs through real shipping WEB5 entry point and enabled WEB4 usage ledger/native owned Mongo. Fresh empty catalogue scope; populated component evidence retained. Paid Stripe/WEB6/hosted excluded.');
   return;
  }
  await page.waitForFunction(()=>/^\d[\d,]*$/.test(document.getElementById('dash-card-holons').textContent)&&
   /^\d[\d,]*$/.test(document.getElementById('dash-card-inventory').textContent)&&
   document.getElementById('dash-card-plan').textContent==='No active plan');
  stage='subscription plans';await page.locator('#dash-card-plan').click();
  // This isolated image intentionally has no Stripe secrets/prices. Prove the real
  // failure is visible, then continue independent account screens; no paid-plan acceptance.
  await page.locator('#sub-plans-list [data-sub-plans-retry]').waitFor({state:'visible'});
  assert.ok((await page.locator('#sub-plans-list').textContent()).includes('Could not load subscription plans'));
  assert.ok(reads.some(read=>read.path==='/api/subscription/plans'&&read.status===503));
  stage='my subscription';await page.locator('#subscription-modal-block [data-tab="mysubs"]').click();
  await page.waitForFunction(()=>document.getElementById('sub-mysubs-list').textContent.includes('No active subscriptions'));
  assert.equal(new URL(page.url()).searchParams.get('tab'),'mysubs');
  stage='orders';await page.locator('#subscription-modal-block [data-tab="orders"]').click();
  await page.waitForFunction(()=>document.getElementById('sub-orders-list').textContent.includes('No orders found'));
  await page.locator('#subscription-modal-block [data-tab="mysubs"]').click();
  stage='authenticated refresh';await page.reload({waitUntil:'domcontentloaded'});
  await page.locator('#subscription-modal-block.is-selected').waitFor({state:'visible'});
  await page.waitForFunction(()=>document.getElementById('sub-mysubs-list').textContent.includes('No active subscriptions'));
  assert.equal(await page.evaluate(()=>localStorage.getItem('loggedIn')),'true');
  stage='explicit session renewal';
  assert.equal(await page.evaluate(()=>window.refreshPortalSession()),true,'Native session renewal acknowledged');
  for(const endpoint of ['/api/avatar/authenticate','/api/avatar/refresh-token','/api/subscription/orders/me','/api/subscription/subscriptions/me'])
   assert.ok(reads.some(read=>read.path===endpoint&&read.status===200),'Acknowledged API '+endpoint);
  assert.deepEqual(errors,[]);
  console.log('PORTAL_STARTUP_BROWSER_PROOF: actual page Beam In, native dashboard counts/plan, explicit missing-Stripe Plans error, subscription list/orders and authenticated deep-link reload and explicit native session renewal passed through shipping WEB4 Startup. WEB5/WEB6 requests blocked; cross-service functionality, available paid catalogue and hosted Stripe excluded.');
  console.log('Other-service requests blocked before dispatch: '+otherServices.length);
 }finally{
  if(context)await context.close();if(browser)await browser.close();
  await Promise.all([new Promise(resolve=>server.close(resolve)),new Promise(resolve=>proxy.close(resolve))]);
  if(proxy5)await new Promise(resolve=>proxy5.close(resolve));
  await fs.rm(key,{force:true});await fs.rm(cert,{force:true});
  console.log('Owned portal browser, static/proxy listeners and temporary TLS files closed/removed.');
 }
}
main().catch(()=>{console.error('PORTAL_STARTUP_HTTP_FACTS:'+JSON.stringify(httpFacts));console.error('PORTAL_STARTUP_BROWSER_FAILURE stage='+stage+'; response bodies and credentials withheld.');process.exitCode=1;});
