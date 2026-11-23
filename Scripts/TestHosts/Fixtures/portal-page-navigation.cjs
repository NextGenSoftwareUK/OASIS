'use strict';
const assert=require('node:assert/strict'),http=require('node:http'),fs=require('node:fs/promises'),path=require('node:path');
async function main(){
 assert.equal(process.env.GITHUB_ACTIONS,'true','Cloud-only browser check.');
 const root=await fs.realpath(process.env.OASIS_PORTAL_ROOT);
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const types={'.html':'text/html','.js':'application/javascript','.css':'text/css','.svg':'image/svg+xml','.png':'image/png','.jpg':'image/jpeg','.woff2':'font/woff2'};
 const server=http.createServer(async(req,res)=>{
  try{
   const url=new URL(req.url,'http://127.0.0.1');
   const relative=['/','/beamin','/signup','/forgot'].includes(url.pathname)?'portal.html':decodeURIComponent(url.pathname).replace(/^\//,'');
   const file=await fs.realpath(path.resolve(root,relative));
   if(!file.startsWith(root+path.sep)){res.writeHead(403);res.end();return;}
   const body=await fs.readFile(file);res.writeHead(200,{'Content-Type':types[path.extname(file)]||'application/octet-stream'});res.end(body);
  }catch{res.writeHead(404);res.end();}
 });
 let browser,context;
 try{
  await new Promise(resolve=>server.listen(0,'127.0.0.1',resolve));
  const origin='http://127.0.0.1:'+server.address().port;
  browser=await chromium.launch({channel:'chrome',headless:true});context=await browser.newContext({serviceWorkers:'block'});
  const errors=[],blocked=[],missing=[];
  await context.route('**/*',async route=>{
   const req=route.request(),url=new URL(req.url());
   if(req.method()!=='GET'||/^\/(api|graphql)(\/|$)/i.test(url.pathname)){
    blocked.push(req.method()+' '+url.origin+url.pathname);return route.abort();
   }
   if(url.origin===origin||['data:','blob:'].includes(url.protocol)||['fonts.googleapis.com','fonts.gstatic.com'].includes(url.hostname))return route.continue();
   blocked.push(req.method()+' '+url.origin+url.pathname);return route.abort();
  });
  await context.addInitScript(()=>window.addEventListener('portal-components-ready',()=>{window.__pageComponentsObserved=true;}));
  const page=await context.newPage();page.on('pageerror',error=>errors.push(error.message));
  page.on('response',response=>{const url=new URL(response.url());if(url.origin===origin&&response.status()>=400)missing.push(response.status()+' '+url.pathname);});
  await page.goto(origin+'/',{waitUntil:'domcontentloaded'});
  await page.waitForFunction(()=>window.__pageComponentsObserved===true);
  assert.equal(await page.locator('.js-modal-block[data-type="login"]').count(),1);
  await page.locator('.js-modal-trigger[data-signin="login"]:visible').first().click();
  await page.waitForURL('**/beamin');
  await page.locator('.js-modal-block[data-type="login"].is-selected').waitFor({state:'visible'});
  await page.locator('.js-modal-block[data-type="login"] .js-modal-trigger[data-signin="signup"]').click();
  await page.waitForURL('**/signup');
  await page.locator('.js-modal-block[data-type="signup"].is-selected').waitFor({state:'visible'});
  await page.reload({waitUntil:'domcontentloaded'});
  await page.locator('.js-modal-block[data-type="signup"].is-selected').waitFor({state:'visible'});
  assert.notEqual(await page.evaluate(()=>localStorage.getItem('loggedIn')),'true');
  assert.deepEqual(errors,[]);assert.deepEqual(blocked,[]);assert.deepEqual(missing,[]);
  console.log('PORTAL_PAGE_PROOF: actual portal.html/scripts/fragments, anonymous Beam In/Sign Up navigation and deep-link refresh passed; zero API requests. Authenticated dashboard, APIs, Startup/SSO and payments excluded.');
 }finally{
  if(context)await context.close();if(browser)await browser.close();
  await new Promise(resolve=>server.close(resolve));
  console.log('Owned portal static server and browser closed.');
 }
}
main().catch(error=>{console.error(error.message);process.exitCode=1;});
