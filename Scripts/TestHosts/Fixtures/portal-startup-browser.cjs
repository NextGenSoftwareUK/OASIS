'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs/promises'),path=require('node:path');
const http=require('node:http'),https=require('node:https'),{execFileSync}=require('node:child_process');
const {createOwnedAvatar}=require('./startup-authentication.cjs');
let stage='preparation';
const httpFacts=[];
async function main(){
 assert.equal(process.env.GITHUB_ACTIONS,'true');
 const api=new URL(process.env.OASIS_STARTUP_API_URL);assert.equal(api.hostname,'127.0.0.1');assert.equal(api.protocol,'http:');
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
 const proxy=https.createServer({key:await fs.readFile(key),cert:await fs.readFile(cert)},(req,res)=>{
  const forward=http.request(new URL(req.url,api),{method:req.method,headers:{...req.headers,host:api.host}},upstream=>{
   res.writeHead(upstream.statusCode,upstream.headers);upstream.pipe(res);
  });forward.on('error',()=>res.destroy());req.pipe(forward);
 });
 let browser,context;
 try{
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));await new Promise(resolve=>proxy.listen(0,'127.0.0.1',resolve));
  const origin='http://127.0.0.1:'+server.address().port,proxyOrigin='https://127.0.0.1:'+proxy.address().port;
  stage='owned registration';const account=await createOwnedAvatar(api.href);
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
   }catch{fact.json=false;}
  });
  stage='page and Beam In';await page.goto(origin+'/',{waitUntil:'domcontentloaded'});
  await page.locator('.js-modal-trigger[data-signin="login"]:visible').first().click();
  await page.locator('#login-email').fill(account.username);await page.locator('#login-password').fill(account.password);
  await page.locator('#login-submit').click();
  await page.waitForFunction(()=>localStorage.getItem('loggedIn')==='true');
  stage='dashboard';await page.locator('#dashboard').waitFor({state:'visible'});
  assert.equal(await page.locator('#dash-name').textContent(),account.username);
  await page.waitForFunction(()=>/^\d[\d,]*$/.test(document.getElementById('dash-card-holons').textContent)&&
   /^\d[\d,]*$/.test(document.getElementById('dash-card-inventory').textContent)&&
   document.getElementById('dash-card-plan').textContent==='No active plan');
  stage='subscription plans';await page.locator('#dash-card-plan').click();
  await page.locator('#sub-plans-list .sub-plan-card').first().waitFor({state:'visible'});
  stage='my subscription';await page.locator('#subscription-modal-block [data-tab="mysubs"]').click();
  await page.waitForFunction(()=>document.getElementById('sub-mysubs-list').textContent.includes('No active subscriptions'));
  assert.equal(new URL(page.url()).searchParams.get('tab'),'mysubs');
  stage='authenticated refresh';await page.reload({waitUntil:'domcontentloaded'});
  await page.locator('#subscription-modal-block.is-selected').waitFor({state:'visible'});
  await page.waitForFunction(()=>document.getElementById('sub-mysubs-list').textContent.includes('No active subscriptions'));
  assert.equal(await page.evaluate(()=>localStorage.getItem('loggedIn')),'true');
  for(const endpoint of ['/api/avatar/authenticate','/api/avatar/refresh-token','/api/subscription/plans','/api/subscription/subscriptions/me'])
   assert.ok(reads.some(read=>read.path===endpoint&&read.status===200),'Acknowledged API '+endpoint);
  assert.deepEqual(errors,[]);
  console.log('PORTAL_STARTUP_BROWSER_PROOF: actual page Beam In, native dashboard counts/plan, subscription plans/list and authenticated deep-link refresh passed through shipping WEB4 Startup. WEB5/WEB6 requests blocked; cross-service functionality and hosted Stripe excluded.');
  console.log('Other-service requests blocked before dispatch: '+otherServices.length);
 }finally{
  if(context)await context.close();if(browser)await browser.close();
  await Promise.all([new Promise(resolve=>server.close(resolve)),new Promise(resolve=>proxy.close(resolve))]);
  await fs.rm(key,{force:true});await fs.rm(cert,{force:true});
  console.log('Owned portal browser, static/proxy listeners and temporary TLS files closed/removed.');
 }
}
main().catch(()=>{console.error('PORTAL_STARTUP_HTTP_FACTS:'+JSON.stringify(httpFacts));console.error('PORTAL_STARTUP_BROWSER_FAILURE stage='+stage+'; response bodies and credentials withheld.');process.exitCode=1;});
