'use strict';
const assert=require('node:assert/strict'),path=require('node:path'),crypto=require('node:crypto');
function authenticatorCode(text){
 const alphabet='ABCDEFGHIJKLMNOPQRSTUVWXYZ234567';let bits=0,value=0,bytes=[];
 for(const char of text){const digit=alphabet.indexOf(char);if(digit<0)throw new Error('Invalid setup key.');
  value=(value<<5)|digit;bits+=5;if(bits>=8){bits-=8;bytes.push((value>>>bits)&255);value&=(1<<bits)-1;}}
 const key=Buffer.from(bytes),counter=Buffer.alloc(8);counter.writeBigUInt64BE(BigInt(Math.floor(Date.now()/30000)));
 const digest=crypto.createHmac('sha1',key).update(counter).digest(),offset=digest[19]&15;
 const code=String((digest.readUInt32BE(offset)&0x7fffffff)%1000000).padStart(6,'0');key.fill(0);return code;
}
async function verify({baseUrl,token,avatarId}){
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');
 assert.match(avatarId,/^[0-9a-f-]{36}$/i);
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const browser=await chromium.launch({channel:'chrome',headless:true}),context=await browser.newContext({serviceWorkers:'block'});
 const requests=[],errors=[],unexpected=[];let stage='setup',secret='';
 const reads=['/api/avatar/mfa/status','/api/avatar/'+avatarId+'/sessions','/api/avatar/holon-seal','/api/avatar/version-purge'];
 const writes=['/api/avatar/mfa/enrol','/api/avatar/mfa/confirm','/api/avatar/mfa/recovery-codes','/api/avatar/mfa/disable'];
 try{
  await context.route('**/*',route=>{
   const request=route.request(),url=new URL(request.url());
   if(url.origin===base.origin){
    if(url.pathname.startsWith('/api/')){
     const entry={method:request.method(),path:url.pathname};requests.push(entry);
     if(!(entry.method==='GET'&&reads.includes(entry.path)||entry.method==='POST'&&writes.includes(entry.path))){unexpected.push(entry);return route.abort();}
    }
    return route.continue();
   }
   if(['data:','blob:'].includes(url.protocol)||['https://fonts.googleapis.com','https://fonts.gstatic.com'].includes(url.origin))return route.continue();
   unexpected.push({origin:url.origin,path:url.pathname});return route.abort();
  });
  await context.addInitScript(({token,avatarId})=>{
   localStorage.setItem('loggedIn','true');localStorage.setItem('avatar',JSON.stringify({id:avatarId,jwtToken:token}));
  },{token,avatarId});
  const page=await context.newPage();page.setDefaultTimeout(15000);page.on('pageerror',error=>errors.push(error.name));
  await page.goto(baseUrl+'/__mfa_settings_browser');
  await page.locator('#fixture-open-settings').click();
  await page.waitForFunction(()=>document.getElementById('settings-mfa-status').textContent==='Authenticator sign-in is off for your account.');
  stage='open management';await page.locator('#settings-mfa-manage').click();
  stage='enrollment';await page.locator('[data-settings-mfa="enrol"]').click();
  await page.locator('#settings-mfa-enrolment').waitFor({state:'visible'});
  secret=(await page.locator('#settings-mfa-secret').textContent()).trim();
  if(!/^[A-Z2-7]+$/.test(secret))throw new Error('Actual setup key not displayed.');
  stage='confirmation';const confirmationStep=Math.floor(Date.now()/30000);
  await page.locator('#settings-mfa-code').fill(authenticatorCode(secret));
  await page.locator('[data-settings-mfa="confirm"]').click();
  await page.locator('#settings-mfa-recovery-result').waitFor({state:'visible'});
  const originalCodes=(await page.locator('#settings-mfa-recovery-codes').textContent()).trim();
  if(!originalCodes)throw new Error('Actual confirmation did not show recovery codes.');
  await page.waitForFunction(()=>document.getElementById('settings-mfa-status').textContent.startsWith('Authenticator sign-in is on.'));
  stage='recovery replacement';
  // TOTP replay protection requires a new real time step, bounded to at most31seconds.
  if(Math.floor(Date.now()/30000)===confirmationStep)await page.waitForTimeout(30000-Date.now()%30000+250);
  await page.locator('#settings-mfa-code').fill(authenticatorCode(secret));
  await page.locator('[data-settings-mfa="recovery-codes"]').click();
  await page.waitForFunction(old=>document.getElementById('settings-mfa-recovery-codes').textContent.trim()!==old,originalCodes);
  const replacement=(await page.locator('#settings-mfa-recovery-codes').textContent()).trim().split('\n');
  if(!replacement[0]||replacement.join('\n')===originalCodes)throw new Error('Recovery codes were not replaced.');
  stage='disable';await page.waitForFunction(()=>!document.querySelector('[data-settings-mfa="disable"]').disabled);
  await page.locator('#settings-mfa-recovery').fill(replacement[0]);
  await page.locator('[data-settings-mfa="disable"]').click();
  await page.waitForFunction(()=>document.getElementById('settings-mfa-status').textContent==='Authenticator sign-in is off for your account.');
  for(const id of ['settings-mfa-secret','settings-mfa-recovery-codes'])if((await page.locator('#'+id).textContent()).trim())throw new Error('Sensitive setup values remained after disable.');
  stage='close and reopen';await page.locator('#settings-modal-close-btn').click();
  await page.locator('#fixture-open-settings').click();
  await page.waitForFunction(()=>document.getElementById('settings-mfa-status').textContent==='Authenticator sign-in is off for your account.');
  assert.deepEqual(requests.filter(item=>item.method==='POST').map(item=>item.path),writes);
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);
  return {mfaSettingsBrowserVerified:true,enrollment:true,confirmation:true,recoveryReplacement:true,disable:true,reopen:true,secretsCleared:true,requests:requests.length};
 }catch(error){throw new Error('MFA Settings browser failed at '+stage+' ('+error.name+').');}
 finally{secret='';await context.close();await browser.close();}
}
module.exports={verify};
