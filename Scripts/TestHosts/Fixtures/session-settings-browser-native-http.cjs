"use strict";
const assert=require("node:assert/strict"),path=require("node:path");
async function verify({baseUrl,token,avatarId,currentSessionId,independentSessionId}) {
 const base=new URL(baseUrl);assert.equal(base.hostname,"127.0.0.1");assert.equal(base.protocol,"http:");
 assert.match(avatarId,/^[0-9a-f-]{36}$/i);assert.match(independentSessionId,/^[0-9a-f]{64}$/);assert.match(currentSessionId,/^[0-9a-f]{64}$/);assert.notEqual(currentSessionId,independentSessionId);
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,"node_modules/playwright-core"));
 const browser=await chromium.launch({channel:"chrome",headless:true}),context=await browser.newContext({serviceWorkers:"block"});
 const reads=["/api/avatar/mfa/status","/api/avatar/"+avatarId+"/sessions","/api/avatar/holon-seal","/api/avatar/version-purge"];
 const selected="/api/avatar/"+avatarId+"/sessions/logout",global="/api/avatar/"+avatarId+"/sessions/logout-all";
 const writes=[],unexpected=[],errors=[];let stage="setup";
 try {
  await context.route("**/*",route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith("/api/")){
     const method=request.method();
     if(method==="POST"&&(url.pathname===selected||url.pathname===global))writes.push({path:url.pathname,body:request.postData()});
     else if(!(method==="GET"&&reads.includes(url.pathname))){unexpected.push(method+" "+url.pathname);return route.abort();}
    }
    return route.continue();
   }
   if(["data:","blob:"].includes(url.protocol)||["https://fonts.googleapis.com","https://fonts.gstatic.com"].includes(url.origin))return route.continue();
   unexpected.push(url.origin);return route.abort();
  });
  await context.addInitScript(({origin,token,avatarId})=>{
   if(location.origin===origin&&!sessionStorage.getItem("ownedSessionSeeded")){
    sessionStorage.setItem("ownedSessionSeeded","true");
    localStorage.setItem("loggedIn","true");localStorage.setItem("avatar",JSON.stringify({id:avatarId,jwtToken:token}));
   }
  },{origin:base.origin,token,avatarId});
  const page=await context.newPage();page.setDefaultTimeout(15000);
  page.on("pageerror",error=>errors.push(error.name));page.on("dialog",dialog=>dialog.accept());
  await page.goto(baseUrl+"/__session_settings_browser",{waitUntil:"domcontentloaded"});
  await page.locator("#fixture-open-settings").click();
  await page.waitForFunction(()=>document.getElementById("settings-sessions-status").textContent.startsWith("2 active /"));
  stage="selected sign-out";
  // The independent credential is issued last; the real API orders records newest first.
  await page.locator("[data-session-revoke]").first().click();
  await page.waitForFunction(()=>document.getElementById("settings-sessions-status").textContent==="Selected session signed out. Your current session remains active.");
  assert.equal(writes.length,1);assert.equal(writes[0].path,selected);
  assert.deepEqual(JSON.parse(writes[0].body),[independentSessionId]);
  assert.equal(await page.evaluate(()=>localStorage.getItem("loggedIn")),"true");
  const remaining=await page.evaluate(async id=>{
   const reply=await window.oasisClient.http.request("GET","api/avatar/"+id+"/sessions");
   if(reply?.isError!==false)throw new Error("The real remaining sessions read failed.");
   return reply.result;
  },avatarId);
  assert.equal(remaining.activeSessions,1);
  assert.deepEqual(remaining.sessions.filter(row=>row.isActive).map(row=>row.id),[currentSessionId]);
  stage="global sign-out";
  await page.locator("#settings-signout-all").click();
  await page.waitForFunction(()=>localStorage.getItem("loggedIn")==="false"&&localStorage.getItem("avatar")===null);
  await page.locator("#fixture-session-state").filter({hasText:"Signed out"}).waitFor();
  assert.equal(await page.evaluate(()=>!!window.oasisClient.http.tokenStore.getToken()),false);
  assert.deepEqual(writes.map(write=>write.path),[selected,global]);
  assert.deepEqual(unexpected,[]);assert.deepEqual(errors,[]);
  return {sessionSettingsBrowserVerified:true,selectedIndependentSession:true,currentSessionPreserved:true,globalSignOut:true,localCredentialsCleared:true,reloadedSignedOut:true,posts:writes.length};
 } catch(error){throw new Error("Settings session browser failed at "+stage+" ("+error.name+").");}
 finally{await context.close();await browser.close();}
}
module.exports={verify};
