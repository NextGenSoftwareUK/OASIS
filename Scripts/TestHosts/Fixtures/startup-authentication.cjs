'use strict';
// Runs inside the owned, network-isolated shipping API image. Never outputs credentials or responses.
const assert=require('node:assert/strict'),crypto=require('node:crypto');
let stage='preparation';
async function createOwnedAvatar(base,readOwnedVerificationToken){
 const url=new URL(base);assert.equal(url.hostname,'127.0.0.1');assert.equal(url.protocol,'http:');
 assert.equal(url.pathname,'/');base=url.origin;
 const password='Owned!'+crypto.randomBytes(24).toString('hex');
 const username='startup_'+crypto.randomBytes(10).toString('hex');
 async function call(label,method,endpoint,body,token,wrapped=false){
  stage=label;
  const response=await fetch(base+endpoint,{method,signal:AbortSignal.timeout(20000),
   headers:{'Content-Type':'application/json',...(token?{Authorization:'Bearer '+token}:{})},
   ...(body===undefined?{}:{body:JSON.stringify(body)})});
  let json;
  try { json=await response.json(); }
  catch(error){
   console.error('PORTAL_STARTUP_HTTP_FACTS:'+JSON.stringify([{path:endpoint.split('?')[0],status:response.status,json:false}]));
   throw error;
  }
  const result=wrapped?json.result:json;
  if(response.status!==200||result?.isError!==false){
   let message=String(result?.message||json.message||json.title||'No application acknowledgement');
   for(const value of [password,token,body?.password,body?.confirmPassword,body?.token])
    if(typeof value==='string'&&value)message=message.split(value).join('[private]');
   message=message.replace(/(?:https?|mongodb(?:\+srv)?):\/\/[^\s]+/gi,'[URI]')
    .replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g,'[JWT]')
    .replace(/[a-f0-9]{32,}/gi,'[identifier]').slice(0,320);
   if(/password|secret|credential|connectionstring|verificationtoken|resettoken/i.test(message))message='Sensitive failure detail withheld';
   const fact={path:endpoint.split('?')[0],status:response.status,isError:result?.isError===true,routingMessage:message};
   if(typeof result?.errorCode==='string'&&/^[A-Z0-9_]{1,80}$/.test(result.errorCode))fact.errorCode=result.errorCode;
   console.error('PORTAL_STARTUP_HTTP_FACTS:'+JSON.stringify([fact]));
  }
  assert.equal(response.status,200,label+' HTTP status');
  assert.equal(result?.isError,false,label+' application result');
  return result.result;
 }
 const avatar=await call('registration','POST','/api/avatar/register',{
  title:'',firstName:'Owned',lastName:'Startup',avatarType:'Human',email:username+'@startup.invalid',
  username,password,confirmPassword:password,acceptTerms:true,suppressVerificationEmail:true},undefined,true);
 assert.match(avatar?.id||'',/^[0-9a-f-]{36}$/i,'Persisted registration identity');
 // Browser fixtures read the token from their labelled disposable database. Shipping JSON intentionally redacts it.
 const verificationToken=readOwnedVerificationToken?readOwnedVerificationToken(username,avatar.id):avatar.verificationToken;
 assert.equal(typeof verificationToken,'string','Owned verification token');
 assert.ok(verificationToken.length>0,'Verification token present');
 const verified=await call('verification','POST','/api/avatar/verify-email',{token:verificationToken},undefined,true);
 assert.equal(verified,true,'Verification acknowledged');
 return {username,password,avatar,call};
}
async function main(){
 const {username,password,avatar,call}=await createOwnedAvatar('http://127.0.0.1:8080');
 const signed=await call('authentication','POST','/api/avatar/authenticate',{username,password},undefined,true);
 assert.equal(signed?.id,avatar.id,'Authenticated avatar identity');
 assert.equal(typeof signed.jwtToken,'string','Issued bearer');
 assert.ok(signed.jwtToken.length>0,'Issued bearer present');
 await call('sessions','GET','/api/avatar/'+encodeURIComponent(avatar.id)+'/sessions',undefined,signed.jwtToken);
 const subscriptions=await call('subscriptions','GET','/api/subscription/subscriptions/me',undefined,signed.jwtToken);
 assert.ok(Array.isArray(subscriptions),'Real subscription list');
 console.log('STARTUP_AUTH_PROOF: shipping Program/Startup registration, verification, password issuance and protected session/subscription reads passed against owned native Mongo; no synthetic JWT, shared backend or Stripe calls. Full portal browser excluded.');
}
module.exports={createOwnedAvatar};
if(require.main===module)main().catch(()=>{console.error('Owned production Startup authentication check failed at '+stage+'; credential-bearing responses withheld.');process.exitCode=1;});
