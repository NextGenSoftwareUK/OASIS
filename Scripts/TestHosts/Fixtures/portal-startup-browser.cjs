'use strict';
const assert=require('node:assert/strict'),fs=require('node:fs/promises'),path=require('node:path');
const http=require('node:http'),https=require('node:https'),{execFileSync,spawnSync}=require('node:child_process');
const {createOwnedAvatar}=require('./startup-authentication.cjs');
let stage='preparation';
const httpFacts=[];
async function main(){
 assert.equal(process.env.GITHUB_ACTIONS,'true');
 const api=new URL(process.env.OASIS_STARTUP_API_URL);assert.equal(api.hostname,'127.0.0.1');assert.equal(api.protocol,'http:');
 const api5=process.env.OASIS_STARTUP_WEB5_URL?new URL(process.env.OASIS_STARTUP_WEB5_URL):null;
 const popupGroup=process.env.OASIS_STARTUP_POPUP_GROUP||'none';
 assert.ok(['none','map-renderer','map-geo','map-places','onet','web5-diff','web5-errors','provider-migration','web5-depth','nft','data','data-crud','data-edit','files-write','map-route','messaging','messaging-read','chat','chat-directory','directory-message','clan','portal-breadth','chat-hyperdrive','data-messaging','nft-messaging-data','social-activity','account-recovery'].includes(popupGroup));
 const selectedPopups=popupGroup==='map-renderer'?['MapRenderer']:popupGroup==='map-geo'?['MapGeo']:popupGroup==='map-places'?['MapPlaces']:popupGroup==='onet'?['ONET']:popupGroup==='web5-diff'?['HolonDiff']:popupGroup==='web5-errors'?['CatalogueErrors']:popupGroup==='provider-migration'?['Migration']:popupGroup==='web5-depth'?['OappMap','Launcher']:popupGroup==='account-recovery'?['AccountRecovery']:popupGroup==='social-activity'?['SocialGraph','ActivityFeed']:popupGroup==='portal-breadth'?['Chat','DirectoryMessage','Clan']:popupGroup==='chat-directory'?['Chat','DirectoryMessage']:popupGroup==='chat-hyperdrive'?['Chat','HyperDrive']:popupGroup==='nft-messaging-data'?['NFT','Data','Messaging']:
  popupGroup==='data-messaging'?['Data','Messaging']:
  popupGroup==='none'?[]:[{nft:'NFT',data:'Data','data-crud':'DataWrite','data-edit':'DataWrite','files-write':'FilesWrite','map-route':'MapRoute',messaging:'Messaging','messaging-read':'MessagingRead',chat:'Chat','directory-message':'DirectoryMessage',clan:'Clan'}[popupGroup]];
 let mapRendererRecordId;
 let mapGeoVisibleId,mapGeoHiddenId,mapGeoQuestId;
 let expectedPlaceObservation=null,placeObservationCount=0,mapPlaceId=null,mapKarmaBefore=null;const mapVisitIds=[];
 const onetWrites={};
 let diffIdentity;const diffStoredIds=[],diffSealReads=[];
 let migrationRootId,migrationChildId;const migrationReads={MongoDBOASIS:0,SQLLiteDBOASIS:0};let migrationCopies=0;
 let catalogueAppId,catalogueIdentity;const catalogueName='Owned depth catalogue';
 let recoveryToken,recoveryPassword;const recoveryRequests={forgot:0,reset:0,login:0};
 const messageText='Owned portal message '+process.env.GITHUB_RUN_ID+'-'+process.env.GITHUB_RUN_ATTEMPT;
 let clanId,clanCreates=0,clanUpdates=0,clanMembers=0,clanDeletes=0;
 const clanName='Owned portal clan '+process.env.GITHUB_RUN_ID;
 let chatId,chatStarts=0,chatSends=0,chatEnds=0;
 let recipient,recipientToken,ownedToken,incomingMessageId,incomingNotificationIds,markedMessages=0,markedNotifications=0,sentMessage=0,dataReads=0;
 const fileBytes=Buffer.from('Owned portal file roundtrip.\n','utf8');
 let fileId,fileUploads=0,fileDeletes=0;
 let routeId,routeCreates=0,routeUpdates=0;
 let savedHolonId,holonSaves=0,holonDeletes=0,pointReads=0;
 const dataName='Owned portal record '+process.env.GITHUB_RUN_ID+'-'+process.env.GITHUB_RUN_ATTEMPT;
 if(api5){assert.equal(api5.hostname,'127.0.0.1');assert.equal(api5.protocol,'http:');}
 function readOwnedVerificationToken(username,avatarId){
  const owner='oasis_startup_'+process.env.GITHUB_RUN_ID+'_'+process.env.GITHUB_RUN_ATTEMPT;
  assert.match(owner,/^oasis_startup_[0-9]+_[0-9]+$/);
  const mongo=owner+'_mongo',database=owner+'_avatars';
  assert.equal(execFileSync('docker',['inspect','--format','{{ index .Config.Labels "oasis.startup.owner" }}',mongo],{encoding:'utf8',timeout:15000}).trim(),owner,'Owned verification database');
  const query='const row=db.getSiblingDB('+JSON.stringify(database)+').getCollection("Avatar").findOne({Username:'+JSON.stringify(username)+'},{Id:1,VerificationToken:1,_id:0});if(!row)throw new Error("Owned avatar absent");print(JSON.stringify({token:row.VerificationToken??null}));';
  // Token output is captured in memory only; the query never contains a token or password.
  const native=JSON.parse(execFileSync('docker',['exec',mongo,'mongosh','--quiet','--eval',query],{encoding:'utf8',timeout:15000}));
  assert.equal(typeof native.token,'string');assert.ok(native.token.length>=32);return native.token;
 }
 async function prepareOwnedWizard(account,privilegedPath='/api/data/copy-holon/providers'){
  const owner='oasis_startup_'+process.env.GITHUB_RUN_ID+'_'+process.env.GITHUB_RUN_ATTEMPT;
  assert.match(owner,/^oasis_startup_[0-9]+_[0-9]+$/);assert.match(account.username,/^startup_[a-f0-9]{20}$/);
  const mongo=owner+'_mongo',apiName=owner+'_api',database=owner+'_avatars';
  for(const name of [mongo,apiName])assert.equal(execFileSync('docker',['inspect','--format','{{ index .Config.Labels "oasis.startup.owner" }}',name],{encoding:'utf8',timeout:15000}).trim(),owner);
  const ordinary=await account.call('ordinary migration sign-in','POST','/api/avatar/authenticate',{username:account.username,password:account.password},undefined,true);
  const refused=await fetch(api.origin+privilegedPath,{headers:{Authorization:'Bearer '+ordinary.jwtToken},signal:AbortSignal.timeout(20000)});
  assert.equal(refused.status,403,'Ordinary role cannot enumerate admin copy providers');await refused.arrayBuffer();
  stage='Owned Wizard native provider bootstrap';
  try{
   const seeded=execFileSync('docker',['exec',apiName,'dotnet','exec','--depsfile','/app/NextGenSoftware.OASIS.API.ONODE.WebAPI.deps.json','--runtimeconfig','/app/NextGenSoftware.OASIS.API.ONODE.WebAPI.runtimeconfig.json','/app/OwnedMigrationBootstrap.dll',owner,account.username,account.avatar.id],{encoding:'utf8',timeout:30000});
   assert.ok(seeded.split(/\r?\n/).includes('OWNED_WIZARD_NATIVE_PROVIDER_SEED'));
  }catch(error){
   const code=String(error.stderr||'').match(/MIGRATION_BOOTSTRAP_FAILURE:[A-Z_]+/);
   httpFacts.push({path:'/api/data/copy-holon/providers',status:0,isError:true,errorCode:'OWNED_NATIVE_BOOTSTRAP_FAILED',...(code?{routingMessage:code[0]}:{})});throw error;
  }
  execFileSync('docker',['restart','--time','20',apiName],{encoding:'utf8',timeout:30000});
  const probe="(async()=>{let r;try{r=await fetch('http://127.0.0.1:8080/api/health',{signal:AbortSignal.timeout(3000)});}catch(e){if(e.cause?.code==='ECONNREFUSED')process.exit(2);throw e;}if(r.status===503)process.exit(2);if(!r.ok)throw Error('Health status');const b=await r.json();if(b.status!=='healthy'||b.service!=='OASIS API ONODE WebAPI')throw Error('Health identity');})().catch(()=>process.exit(1));";
  const deadline=Date.now()+90000;let ready=false;
  while(Date.now()<deadline){
   const check=spawnSync('docker',['exec',apiName,'node','-e',probe],{encoding:'utf8',timeout:8000});
   if(check.status===0){ready=true;break;}assert.equal(check.status,2,'Owned restart failed outside the readiness contract');
   await new Promise(resolve=>setTimeout(resolve,1000));
  }
  assert.equal(ready,true,'Owned API restart ready; clears pre-grant process caches');
 }
 const root=await fs.realpath(process.env.OASIS_STARTUP_PORTAL_ROOT),owned=await fs.realpath(process.env.OASIS_PROFILE_BROWSER_RUNTIME);
 const {chromium}=require(path.join(owned,'node_modules/playwright-core'));
 const cert=path.join(owned,'owned-localhost.crt'),key=path.join(owned,'owned-localhost.key');
 execFileSync('openssl',['req','-x509','-newkey','rsa:2048','-nodes','-days','1','-subj','/CN=127.0.0.1',
  '-addext','subjectAltName=IP:127.0.0.1','-keyout',key,'-out',cert],{stdio:'ignore'});
 const types={'.html':'text/html','.js':'application/javascript','.css':'text/css','.svg':'image/svg+xml','.png':'image/png','.jpg':'image/jpeg','.woff2':'font/woff2'};
 const server=http.createServer(async(req,res)=>{
  try{
   const url=new URL(req.url,'http://127.0.0.1');
   if(popupGroup==='web5-errors'&&url.pathname==='/owned-sdk-host'){res.writeHead(200,{'Content-Type':'text/html'});res.end('<!doctype html><html><head><meta charset="utf-8"><script src="/assets/js/oasis-web5-sdk.js"></script></head><body></body></html>');return;}
   const name=['/','/beamin','/subscription','/forgot'].includes(url.pathname)?'portal.html':decodeURIComponent(url.pathname).replace(/^\//,'');
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
  stage='owned registration';const account=await createOwnedAvatar(api.href,readOwnedVerificationToken);
  if(['provider-migration','onet'].includes(popupGroup))await prepareOwnedWizard(account,popupGroup==='onet'?'/api/v1/onet/network/status':'/api/data/copy-holon/providers');
  async function admitFree(owned){
   const signed=await owned.call('free account sign-in','POST','/api/avatar/authenticate',{username:owned.username,password:owned.password},undefined,true);
   await owned.call('free subscription','POST','/api/subscription/checkout/session',{planId:'free',successUrl:origin+'/subscription?tab=mysubs',cancelUrl:origin+'/'},signed.jwtToken);
   const subs=await owned.call('free readback','GET','/api/subscription/subscriptions/me',undefined,signed.jwtToken);
   assert.ok(Array.isArray(subs)&&subs.some(s=>s.planId==='free'&&s.status==='active'),'Persisted free subscription');
   return signed.jwtToken;
  }
  if(api5)ownedToken=await admitFree(account);
  if(selectedPopups.length&&popupGroup!=='account-recovery'){
   stage='explicit backend selection';assert.equal(!!api5,['web5-depth','web5-errors','web5-diff','map-geo'].includes(popupGroup),'Explicit matching backend selection');if(!api5)ownedToken=await admitFree(account);
   if(selectedPopups.includes('Messaging')||selectedPopups.includes('MessagingRead')||selectedPopups.includes('Chat')||selectedPopups.includes('DirectoryMessage')||selectedPopups.includes('Clan')||selectedPopups.includes('SocialGraph')){
    recipient=await createOwnedAvatar(api.href,readOwnedVerificationToken);recipientToken=await admitFree(recipient);
   }
  }
  if(popupGroup==='social-activity'){
   assert.equal(await recipient.call('owned social contact','POST','/api/messaging/send-message-to-avatar/'+account.avatar.id,messageText,recipientToken),true);
  }
  if(popupGroup==='messaging-read'){
   assert.equal(await recipient.call('owned incoming message','POST','/api/messaging/send-message-to-avatar/'+account.avatar.id,messageText,recipientToken),true);
   const messages=await account.call('owned incoming read','GET','/api/messaging/messages?limit=50&offset=0',undefined,ownedToken);
   const matches=messages.filter(row=>row.content===messageText&&row.fromAvatarId===recipient.avatar.id&&row.toAvatarId===account.avatar.id);
   assert.equal(matches.length,1);assert.equal(matches[0].isRead,false);incomingMessageId=matches[0].id;
   const notifications=await account.call('owned notifications read','GET','/api/messaging/notifications?limit=20&offset=0',undefined,ownedToken);
   incomingNotificationIds=notifications.filter(row=>row.avatarId===account.avatar.id&&row.isRead===false).map(row=>row.id);
   assert.ok(incomingNotificationIds.length>0);
  }
  browser=await chromium.launch({channel:'chrome',headless:true,...(popupGroup==='map-renderer'?{args:['--use-gl=angle','--use-angle=swiftshader','--enable-unsafe-swiftshader']}: {})});
  context=await browser.newContext({serviceWorkers:'block',ignoreHTTPSErrors:true});
  if(popupGroup==='map-renderer')await context.addInitScript(()=>{window.portalMapStyle={version:8,sources:{},layers:[{id:'background',type:'background',paint:{'background-color':'#102030'}}]};});
  const errors=[],otherServices=[],reads=[];
  await context.route('**/*',async route=>{
   try {
   const request=route.request(),url=new URL(request.url());
   if(url.origin==='https://api.web4.oasisomniverse.one'){
    const mutation=request.method()!=='GET'&&request.method()!=='OPTIONS';
    let ownedOperation=false;
    if(selectedPopups.includes('Clan')){
     if(request.method()==='POST'&&url.pathname==='/api/clan'){
      assert.equal(++clanCreates,1);assert.deepEqual(request.postDataJSON(),{name:clanName,description:'Owned clan journey'});ownedOperation=true;
     }else if(request.method()==='PUT'&&clanId&&url.pathname==='/api/clan/'+clanId){
      assert.equal(++clanUpdates,1);assert.deepEqual(request.postDataJSON(),{name:clanName+' edited',description:'Edited clan journey'});ownedOperation=true;
     }else if(request.method()==='POST'&&clanId&&url.pathname==='/api/clan/'+clanId+'/members/'+recipient.avatar.id){assert.equal(++clanMembers,1);ownedOperation=true;
     }else if(request.method()==='DELETE'&&clanId&&url.pathname==='/api/clan/'+clanId){assert.equal(++clanDeletes,1);ownedOperation=true;}
    }
    if(selectedPopups.includes('Chat')&&request.method()==='POST'){
     if(url.pathname==='/api/chat/start-new-chat-session'){
      assert.equal(++chatStarts,1);assert.deepEqual(request.postDataJSON(),[account.avatar.id,recipient.avatar.id]);ownedOperation=true;
     }else if(chatId&&url.pathname==='/api/chat/send-message/'+chatId){
      assert.equal(++chatSends,1);assert.equal(request.postDataJSON(),messageText);ownedOperation=true;
     }else if(chatId&&url.pathname==='/api/chat/end-chat-session/'+chatId){assert.equal(++chatEnds,1);ownedOperation=true;}
    }
    if(popupGroup==='messaging-read'&&request.method()==='POST'){
     if(url.pathname==='/api/messaging/mark-messages-read'){
      assert.equal(++markedMessages,1);assert.deepEqual(request.postDataJSON(),[incomingMessageId]);ownedOperation=true;
     }else if(url.pathname==='/api/messaging/mark-notifications-read'){
      assert.equal(++markedNotifications,1);assert.deepEqual([...request.postDataJSON()].sort(),[...incomingNotificationIds].sort());ownedOperation=true;
     }
    }
    if(popupGroup==='files-write'){
     if(request.method()==='POST'&&url.pathname==='/api/files/upload-file'){
      assert.equal(++fileUploads,1,'One explicit owned upload');assert.equal(request.postDataJSON(),fileBytes.toString('base64'));
      assert.equal(url.searchParams.get('fileName'),'owned-roundtrip.txt');assert.equal(url.searchParams.get('description'),'Owned file roundtrip');ownedOperation=true;
     }else if(request.method()==='DELETE'&&fileId&&url.pathname==='/api/files/delete-file/'+fileId){
      assert.equal(++fileDeletes,1,'One explicit owned file delete');ownedOperation=true;
     }
    }
    if(popupGroup==='map-route'){
     if(request.method()==='POST'&&url.pathname==='/api/map/routes'){
      assert.equal(++routeCreates,1,'One explicit owned GPX publication');const body=request.postDataJSON();
      routeId=body.route.id;assert.match(routeId,/^[0-9a-f-]{36}$/i);
      assert.equal(body.route.sourceApp,'Our World');assert.equal(body.route.sourceRouteId,'owned-walk');
      assert.equal(body.route.isPublic,false);assert.equal(body.route.segments.length,1);assert.equal(body.route.segments[0].length,2);
      assert.equal(body.route.segments[0][0].utc,'2026-11-21T10:00:00.1234567Z');
      assert.equal(Object.keys(body.expectedRevisions).length,2);assert.ok(Object.values(body.expectedRevisions).every(v=>v===null));ownedOperation=true;
     }else if(request.method()==='PUT'&&routeId&&url.pathname==='/api/map/routes/'+routeId+'/access'){
      assert.equal(++routeUpdates,1,'One explicit owned rename');const body=request.postDataJSON();
      assert.equal(body.name,'Renamed owned walk');assert.equal(body.isPublic,false);
      assert.equal(body.providerType,'MongoDBOASIS');assert.equal(Object.keys(body.expectedRevisions).length,2);ownedOperation=true;
     }
    }
    if(['data-crud','data-edit'].includes(popupGroup)){
     if(request.method()==='POST'&&url.pathname==='/api/data/load-holons-for-parent'){
      assert.ok(++dataReads<=4,'Bounded actual Data browse');
      assert.deepEqual(request.postDataJSON(),{id:account.avatar.id,holonType:'All',loadChildren:false,recursive:false});ownedOperation=true;
     }else if(request.method()==='POST'&&url.pathname==='/api/data/save-holon'){
      ++holonSaves;const body=request.postDataJSON();
      assert.equal(body.holon.parentHolonId,account.avatar.id);assert.equal(body.saveChildren,false);
      if(holonSaves===1){
       assert.equal(body.holon.name,dataName);assert.equal(body.holon.description,'Owned create read delete journey');
       assert.ok(!body.providerType||body.providerType==='Default','Use actual default provider');
      }else{
       assert.equal(popupGroup,'data-edit');assert.equal(holonSaves,2,'One explicit owned update');
       assert.equal(body.holon.id,savedHolonId);assert.equal(body.holon.name,dataName+' edited');
       assert.equal(body.holon.description,'Updated through detail form');assert.equal(body.providerType,'MongoDBOASIS');
       assert.deepEqual(Object.keys(body.expectedRevisions),[savedHolonId]);assert.match(body.expectedRevisions[savedHolonId],/^[0-9a-f-]{36}$/i);
       assert.equal(body.recursive,false);
      }
      assert.match(body.operationId,/^[0-9a-f-]{36}$/i);ownedOperation=true;
     }else if(request.method()==='POST'&&url.pathname==='/api/data/load-holon'){
      assert.ok(++pointReads<=(popupGroup==='data-edit'?2:1),'Bounded actual point reads');assert.equal(request.postDataJSON().id,savedHolonId);ownedOperation=true;
     }else if(request.method()==='DELETE'&&url.pathname==='/api/data/delete-holon'){
      assert.equal(++holonDeletes,1,'One explicit owned deletion');const body=request.postDataJSON();
      assert.equal(body.id,savedHolonId);assert.equal(body.softDelete,false);
      assert.equal(body.providerType,'MongoDBOASIS');assert.deepEqual(Object.keys(body.expectedRevisions),[savedHolonId]);
      assert.match(body.expectedRevisions[savedHolonId],/^[0-9a-f-]{36}$/i);ownedOperation=true;
     }
    }
    if(popupGroup==='map-places'&&request.method()==='POST'&&url.pathname==='/api/places/observe'){
     assert.ok(expectedPlaceObservation,'Only an explicit owned visit intent');assert.deepEqual(request.postDataJSON(),expectedPlaceObservation);
     assert.ok(++placeObservationCount<=3,'Bounded enter departure return sequence');
     const headers=request.headers();assert.match(headers['idempotency-key'],/^[0-9a-f-]{36}$/i);assert.equal(headers['x-cosmic-operation-id'],headers['idempotency-key']);ownedOperation=true;
    }
    if(popupGroup==='onet'&&request.method()==='POST'&&url.pathname.startsWith('/api/v1/onet/')){
     assert.ok(['/api/v1/onet/network/start','/api/v1/onet/network/stop'].includes(url.pathname),'Only owned local ONET lifecycle writes');
     assert.equal(onetWrites[url.pathname]=(onetWrites[url.pathname]||0)+1,1,'One explicit lifecycle command');ownedOperation=true;
    }
    if(popupGroup==='web5-diff'&&request.method()==='GET'&&url.pathname.startsWith('/api/data/holon-seal/verify/')){
     const id=url.pathname.split('/').pop();assert.ok(diffStoredIds.includes(id),'Seal read uses returned stored identity');assert.ok(!diffSealReads.includes(id),'One selected seal read per stored version');diffSealReads.push(id);
    }
    if(popupGroup==='provider-migration'&&request.method()==='POST'){
     const body=request.postDataJSON();
     if(url.pathname==='/api/data/load-holon'){
      assert.equal(body.id,migrationRootId);assert.equal(body.loadChildren,false);assert.equal(body.recursive,false);
      assert.ok(Object.hasOwn(migrationReads,body.providerType));assert.equal(++migrationReads[body.providerType],1);ownedOperation=true;
     }else if(url.pathname==='/api/data/copy-holon'){
      assert.equal(++migrationCopies,1);assert.deepEqual(body,{holonId:migrationRootId,sourceProvider:'MongoDBOASIS',destinationProvider:'SQLLiteDBOASIS'});ownedOperation=true;
     }
    }
    if(popupGroup==='account-recovery'&&request.method()==='POST'){
     if(url.pathname==='/api/avatar/forgot-password'){
      assert.equal(++recoveryRequests.forgot,1);assert.deepEqual(request.postDataJSON(),{email:account.username+'@startup.invalid',returnUrl:origin+'/reset-password.html'});ownedOperation=true;
     }else if(url.pathname==='/api/avatar/reset-password'){
      assert.equal(++recoveryRequests.reset,1);
      assert.deepEqual(request.postDataJSON(),{token:recoveryToken,newPassword:recoveryPassword,confirmNewPassword:recoveryPassword});ownedOperation=true;
     }else if(url.pathname==='/api/avatar/authenticate'){
      assert.equal(++recoveryRequests.login,1);assert.equal(request.postDataJSON().username,account.username);
      assert.equal(request.postDataJSON().password,recoveryPassword);ownedOperation=true;
     }
    }
    if(selectedPopups.length&&request.method()==='POST'){
     if(selectedPopups.includes('Data')&&url.pathname==='/api/data/load-holons-for-parent'){
      assert.equal(++dataReads,1,'One actual Data browse');
      assert.deepEqual(request.postDataJSON(),{id:account.avatar.id,holonType:'All',loadChildren:false,recursive:false});
      ownedOperation=true;
     }else if((selectedPopups.includes('Messaging')||selectedPopups.includes('DirectoryMessage'))&&url.pathname==='/api/messaging/send-message-to-avatar/'+recipient.avatar.id){
      assert.equal(++sentMessage,1,'One explicit owned message send');
      assert.equal(request.postDataJSON(),messageText);ownedOperation=true;
     }
    }
    if(!url.pathname.startsWith('/api/')||(mutation&&!ownedOperation&&!['/api/avatar/authenticate','/api/avatar/refresh-token'].includes(url.pathname)))
     throw new Error('Unadmitted browser API operation.');
    return route.continue({url:proxyOrigin+url.pathname+url.search});
   }
   if(api5&&url.origin==='https://api.starnet.oasisomniverse.one'){
    if(request.method()!=='GET'&&request.method()!=='OPTIONS'){otherServices.push('unadmitted WEB5 mutation');return route.abort();}
    return route.continue({url:proxy5Origin+url.pathname+url.search});
   }
   if(popupGroup==='map-renderer'&&url.origin==='https://unpkg.com'&&['/maplibre-gl@6.19.0/dist/maplibre-gl.mjs','/maplibre-gl@6.19.0/dist/maplibre-gl.css','/maplibre-gl@6.19.0/dist/maplibre-gl-csp-worker.js'].includes(url.pathname))return route.continue();
   if(url.origin===origin||['data:','blob:'].includes(url.protocol)||['fonts.googleapis.com','fonts.gstatic.com'].includes(url.hostname))return route.continue();
   otherServices.push(request.method()+' '+url.origin+url.pathname);return route.abort();
   } catch (error) {
    errors.push('network-admission');
    const match=String(error.stack||'').match(/portal-startup-browser\.cjs:(\d+):/);
    console.error('PORTAL_STARTUP_ADMISSION_FAILURE:'+JSON.stringify({stage,path:new URL(route.request().url()).pathname,
     errorType:['AssertionError','TypeError','Error'].includes(error.name)?error.name:'Error',sourceLine:match?Number(match[1]):0}));
    await route.abort();
   }
  });
  if(popupGroup==='web5-errors'){
   assert.ok(api5);stage='First direct OAPP detail missing record';
   const probePage=await context.newPage();await probePage.goto(origin+'/owned-sdk-host',{waitUntil:'load'});
   const result=await probePage.evaluate(async({base,token,id})=>{
    const client=new window.STARClient({persistSession:false});client.setToken(token);
    const response=await client.oAPPs.getOAPP({id});
    return {status:response.statusCode,isError:response.isError,missing:response.result==null,errorCode:response.raw?.errorCode,message:response.message};
   },{base:proxy5Origin,token:ownedToken,id:require('node:crypto').randomUUID()});
   httpFacts.push({path:'/api/oapps/{id}',status:result.status,isError:result.isError,...(typeof result.errorCode==='string'&&/^[A-Z0-9_]{1,80}$/.test(result.errorCode)?{errorCode:result.errorCode}:{})});
   assert.equal(result.status,400);assert.equal(result.isError,true);assert.equal(result.missing,true);
   assert.equal(result.errorCode,'OASIS_RECORD_NOT_FOUND');assert.equal(result.message,'No Holon Found');
   console.log('PORTAL_STARTUP_POPUP_OUTCOMES:'+JSON.stringify([{name:'CatalogueErrors',passed:true,stage:'Direct detail real missing-record error',closePassed:true}]));
   console.log('PORTAL_STARTUP_BROWSER_PROOF: current shipping WEB5 SDK cold detail read reports real native missing record and declared400, with legacy test-data configuration explicitly enabled. API contract scope only; no new popup or whole-portal credit.');
   return;
  }
  const page=await context.newPage();page.on('pageerror',()=>errors.push('page-error'));
  page.on('response',async response=>{
   const url=new URL(response.url());if(!url.pathname.startsWith('/api/'))return;
   const fact={path:url.pathname,status:response.status()};reads.push(fact);
   if(httpFacts.length<64)httpFacts.push(fact);
   try{const body=await response.json(),result=typeof body.isError==='boolean'?body:body.result;
    fact.isError=result?.isError;const code=result?.errorCode;
    if(/^\/api\/(nft|oLand|data|messaging|map)\//i.test(url.pathname)){
     const value=result?.result;fact.resultShape=Array.isArray(value)?'array':value===null?'null':typeof value;
     if(Array.isArray(value))fact.resultCount=value.length;
    }
    if(popupGroup==='map-route'&&url.pathname==='/api/map/routes'&&response.request().method()==='GET'){const value=result?.result;fact.routePage={items:Array.isArray(value?.items),nextOffsetPresent:Object.hasOwn(value||{},'nextOffset'),nextOffsetNull:value?.nextOffset===null,nextOffsetInteger:Number.isInteger(value?.nextOffset)};}
    if(popupGroup==='map-route'&&url.pathname.startsWith('/api/map/routes/')&&result?.result){
     const detail=result.result,header=detail.header,geometry=detail.geometry;
     fact.routeShape={header:!!header,geometry:!!geometry,authority:typeof detail.authority==='string',revisions:detail.expectedRevisions?Object.keys(detail.expectedRevisions).length:0,headerIdMatches:header?.id===routeId,headerPayload:typeof header?.metaData?.payload,geometryPayload:typeof geometry?.metaData?.payload};
     try{const summary=JSON.parse(header?.metaData?.payload),points=JSON.parse(geometry?.metaData?.payload)?.[0]?.[0];fact.routeShape.summaryPascal=typeof summary.SourceApp==='string';fact.routeShape.summaryCamel=typeof summary.sourceApp==='string';fact.routeShape.latitudePascal=typeof points?.Latitude==='number';fact.routeShape.latitudeCamel=typeof points?.latitude==='number';}catch{}
    }
    if(typeof code==='string'&&/^[A-Z0-9_]{1,80}$/.test(code))fact.errorCode=code;
    if((code==='HYPERDRIVE_ROUTE_FAILED'||selectedPopups.includes('Chat')&&url.pathname.startsWith('/api/chat/end-chat-session/')&&result?.isError===true)&&typeof result.message==='string'){
     const message=result.message.replace(/(?:https?|mongodb(?:\+srv)?):\/\/[^\s]+/gi,'[URI]')
      .replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g,'[JWT]')
      .replace(/[a-f0-9]{32,}/gi,'[identifier]').slice(0,320);
     fact.routingMessage=/password|secret|credential|connectionstring|refresh.?token/i.test(message)?'[sensitive routing detail withheld]':message;
    }
   }catch{fact.json=false;}
  });
  if(popupGroup==='account-recovery'){
   assert.equal(api5,null);
   const owner='oasis_startup_'+process.env.GITHUB_RUN_ID+'_'+process.env.GITHUB_RUN_ATTEMPT;
   assert.match(owner,/^oasis_startup_[0-9]+_[0-9]+$/);
   const mongo=owner+'_mongo',database=owner+'_avatars';
   const nativeOwner=execFileSync('docker',['inspect','--format','{{ index .Config.Labels "oasis.startup.owner" }}',mongo],{encoding:'utf8'}).trim();
   assert.equal(nativeOwner,owner,'Read tokens only from this run owned native fixture');
   function ownedResetState(){
    const query='const row=db.getSiblingDB('+JSON.stringify(database)+').getCollection("Avatar").findOne({Username:'+JSON.stringify(account.username)+'},{ResetToken:1,ResetTokenExpires:1,_id:0});if(!row)throw new Error("Owned avatar absent");print(JSON.stringify({token:row.ResetToken??null,expires:row.ResetTokenExpires??null}));';
    // Native reset secrets stay in memory; never put a token on a command line, in logs or an artifact.
    return JSON.parse(execFileSync('docker',['exec',mongo,'mongosh','--quiet','--eval',query],{encoding:'utf8',timeout:15000}));
   }
   stage='Forgot password form';await page.goto(origin+'/forgot',{waitUntil:'domcontentloaded'});
   await page.locator('#forgot-email').fill(account.username+'@startup.invalid');
   stage='Forgot password acknowledgement';await page.locator('#forgot-submit').click();
   await page.locator('#forgot-msg.forgot-status--success').waitFor({state:'visible'});
   const reset=ownedResetState();assert.equal(typeof reset.token,'string');assert.ok(reset.token.length>=32);
   const remaining=Date.parse(reset.expires)-Date.now();
   assert.ok(remaining>23.9*3600000&&remaining<=24*3600000,'Reset token must match the promised one-day lifetime');recoveryToken=reset.token;
   recoveryPassword='OwnedNew!'+require('node:crypto').randomBytes(24).toString('hex');
   stage='Standalone reset form';await page.goto(origin+'/reset-password.html?token='+encodeURIComponent(recoveryToken),{waitUntil:'domcontentloaded'});
   assert.equal(await page.locator('a.logo').getAttribute('href'),'/portal.html');
   await page.locator('#pw1').fill(recoveryPassword);await page.locator('#pw2').fill(recoveryPassword);
   stage='Persisted password reset';await page.locator('#submit-btn').click();
   await page.locator('#status-box.status--success').waitFor({state:'visible'});
   await page.waitForURL(url=>url.pathname==='/portal.html');
   assert.equal(ownedResetState().token,null,'Reset token consumed in native storage');
   stage='New password login';
   await page.locator('.js-modal-trigger[data-signin="login"]:visible').first().click();
   await page.locator('#login-email').fill(account.username);await page.locator('#login-password').fill(recoveryPassword);
   await page.locator('#login-submit').click();
   await page.waitForFunction(()=>localStorage.getItem('loggedIn')==='true');
   await page.locator('#dashboard').waitFor({state:'visible'});
   assert.equal(await page.locator('#dash-name').textContent(),account.username);
   stage='Old password refusal';
   const refused=await fetch(api.origin+'/api/avatar/authenticate',{method:'POST',signal:AbortSignal.timeout(20000),
    headers:{'Content-Type':'application/json'},body:JSON.stringify({username:account.username,password:account.password})});
   const rejected=await refused.json();assert.equal(rejected.result.isError,true,'Prior password must no longer authenticate');
   assert.equal(recoveryRequests.forgot,1);assert.equal(recoveryRequests.reset,1);assert.equal(recoveryRequests.login,1);
   assert.deepEqual(errors,[]);
   console.log('PORTAL_STARTUP_POPUP_OUTCOMES:'+JSON.stringify([{name:'AccountRecovery',passed:true,stage:'New password login and old password refusal',closePassed:true}]));
   console.log('PORTAL_STARTUP_BROWSER_PROOF: actual forgot form, standalone reset and native password/token mutation, portal-home redirect and new-password login passed. Explicit email-disabled isolated Startup; no email delivery, founder activation or hosted acceptance.');
   return;
  }
  stage='page and Beam In';await page.goto(origin+'/portal.html',{waitUntil:'domcontentloaded'});
  await page.locator('.js-modal-trigger[data-signin="login"]:visible').first().click();
  await page.locator('#login-email').fill(account.username);await page.locator('#login-password').fill(account.password);
  await page.locator('#login-submit').click();
  await page.waitForFunction(()=>localStorage.getItem('loggedIn')==='true');
  stage='dashboard';await page.locator('#dashboard').waitFor({state:'visible'});
  assert.equal(await page.locator('#dash-name').textContent(),account.username);
  if(popupGroup==='provider-migration'){
   stage='Migration actual source graph create';
   migrationRootId=require('node:crypto').randomUUID();migrationChildId=require('node:crypto').randomUUID();const operation=require('node:crypto').randomUUID();
   const created=await context.request.post(proxyOrigin+'/api/data/save-holon',{headers:{Authorization:'Bearer '+ownedToken,'Idempotency-Key':operation},data:{holon:{id:migrationRootId,name:'Owned migration root',children:[{id:migrationChildId,parentHolonId:migrationRootId,name:'Owned migration child'}]},saveChildren:true,recursive:true,providerType:'MongoDBOASIS',operationId:operation,expectedRevisions:{[migrationRootId]:null,[migrationChildId]:null}}});
   const wrapped=await created.json(),saved=wrapped.result;
   httpFacts.push({path:'/api/data/save-holon',status:created.status(),isError:saved?.isError===true,...(typeof saved?.errorCode==='string'&&/^[A-Z0-9_]{1,80}$/.test(saved.errorCode)?{errorCode:saved.errorCode}:{})});
   assert.equal(created.status(),200);assert.equal(saved.isError,false);assert.equal(saved.isSaved,true);assert.equal(saved.result.id,migrationRootId);assert.ok(saved.result.children.some(row=>row.id===migrationChildId));
  }
  if(popupGroup==='web5-diff'){
   const owner='oasis_startup_'+process.env.GITHUB_RUN_ID+'_'+process.env.GITHUB_RUN_ATTEMPT;
   const hex=require('node:crypto').createHash('sha256').update(owner+'_diff').digest('hex').slice(0,32);
   diffIdentity=hex.slice(0,8)+'-'+hex.slice(8,12)+'-'+hex.slice(12,16)+'-'+hex.slice(16,20)+'-'+hex.slice(20);
   const headers={Authorization:'Bearer '+ownedToken};
   stage='Holon cold real history';const empty=await context.request.get(proxy5Origin+'/api/holons/'+diffIdentity+'/versions',{headers});
   const initial=await empty.json();assert.equal(empty.status(),200);assert.equal(initial.isError,false);assert.deepEqual(initial.result,[]);
   stage='Holon real typed create';const operation=require('node:crypto').randomUUID();
   const made=await context.request.post(proxy5Origin+'/api/holons',{headers:{...headers,'Idempotency-Key':operation,'X-COSMIC-Operation-Id':operation},data:{id:diffIdentity,name:'Owned diff Smart Brick',description:'Actual published version comparison'}});
   const created=await made.json();httpFacts.push({path:'/api/holons',status:made.status(),isError:created.isError===true});assert.equal(made.status(),200);assert.equal(created.isError,false);assert.equal(created.result.starnetdna.id,diffIdentity);
   const folder=path.join(owned,'holon-diff-source');await fs.mkdir(folder);const dnaFile=path.join(folder,'HolonDNA.json');
   for(const version of ['1.0.0','2.0.0']){
    stage='Holon real publish '+version;
    let sourcePath='';
    if(version==='2.0.0'){
     const first=await context.request.get(proxy5Origin+'/api/holons/'+diffIdentity+'/version/1.0.0',{headers});const stored=await first.json();assert.equal(first.status(),200);assert.equal(stored.isError,false);
     await fs.writeFile(dnaFile,JSON.stringify({...stored.result.starnetdna,version:'2.0.0',description:'Actual second published version'}));sourcePath=folder;
    }
    const op=require('node:crypto').randomUUID();const published=await context.request.post(proxy5Origin+'/api/holons/'+diffIdentity+'/publish',{headers:{...headers,'Idempotency-Key':op,'X-COSMIC-Operation-Id':op},data:{sourcePath,launchTarget:'',edit:false,registerOnSTARNET:true,generateBinary:false,uploadToCloud:false}});
    const body=await published.json();let diagnostic=String(body.message||'').replace(/(?:mongodb(?:\+srv)?|https?):\/\/[^\s]+/gi,'[URI]').replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g,'[JWT]');if(/password|secret|credential|connectionstring|refresh.?token/i.test(diagnostic))diagnostic='Sensitive diagnostic withheld';httpFacts.push({path:'/api/holons/{id}/publish',status:published.status(),isError:body.isError===true,routingMessage:diagnostic.slice(0,320),...(typeof body.errorCode==='string'&&/^[A-Z0-9_]{1,80}$/.test(body.errorCode)?{errorCode:body.errorCode}:{})});assert.equal(published.status(),200);assert.equal(body.isError,false);assert.equal(body.isSaved,true);assert.equal(body.result.starnetdna.version,version);diffStoredIds.push(body.result.id);
   }
   assert.equal(new Set(diffStoredIds).size,2,'Two independently stored published versions');
  }
  if(popupGroup==='map-renderer'){
   stage='Create actual stored map location';
   mapRendererRecordId=require('node:crypto').randomUUID();
   const operationId=require('node:crypto').randomUUID(),bytes=require('node:crypto').createHash('sha256').update('oasis:map-locations:v1').digest().subarray(0,16);
   const catalogueId=[bytes.readUInt32LE(0).toString(16).padStart(8,'0'),bytes.readUInt16LE(4).toString(16).padStart(4,'0'),bytes.readUInt16LE(6).toString(16).padStart(4,'0'),bytes.subarray(8,10).toString('hex'),bytes.subarray(10).toString('hex')].join('-');
   const created=await context.request.post(proxyOrigin+'/api/data/save-holon',{headers:{Authorization:'Bearer '+ownedToken,'Idempotency-Key':operationId,'X-COSMIC-Operation-Id':operationId},
    data:{holon:{id:mapRendererRecordId,name:'Owned renderer point',description:'Real API location for browser map controls',holonType:'Custom',customHolonType:'OASIS.MapLocation',parentHolonId:catalogueId,
     metaData:{mapLocation:JSON.stringify({Latitude:46.98,Longitude:8.25,Type:6,IsActive:true,Metadata:{sourceApp:'Shared Places CI'}})}},
     saveChildren:false,recursive:false,providerType:'MongoDBOASIS',operationId,expectedRevisions:{[mapRendererRecordId]:null}}});
   const envelope=await created.json(),saved=envelope.result;
   let diagnostic=String(saved?.message||envelope.message||'').replace(/(?:https?|mongodb(?:\+srv)?):\/\/[^\s]+/gi,'[URI]').replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g,'[JWT]');
   if(/password|secret|credential|connectionstring|refresh.?token/i.test(diagnostic))diagnostic='Sensitive detail withheld';
   httpFacts.push({path:'/api/data/save-holon',status:created.status(),isError:saved?.isError===true,routingMessage:diagnostic.slice(0,320)});
   assert.equal(created.status(),200);assert.equal(saved.isError,false);assert.equal(saved.result.id,mapRendererRecordId);
   const response=await context.request.get(proxyOrigin+'/api/map/nearby?latitude=46.98&longitude=8.25&radiusKm=5',{headers:{Authorization:'Bearer '+ownedToken}});
   const nearby=await response.json();assert.equal(response.status(),200);assert.equal(nearby.isError,false);assert.equal(nearby.result.length,1);assert.equal(nearby.result[0].id,mapRendererRecordId);
  }
  if(popupGroup==='map-geo'){
   async function createGeo(path,body){
    const operationId=require('node:crypto').randomUUID();
    const response=await context.request.post(proxy5Origin+path,{headers:{Authorization:'Bearer '+ownedToken,'X-COSMIC-Operation-Id':operationId,'Idempotency-Key':operationId},data:body});
    const result=await response.json();
    httpFacts.push({path,status:response.status(),isError:result.isError===true,...(typeof result.errorCode==='string'&&/^[A-Z0-9_]{1,80}$/.test(result.errorCode)?{errorCode:result.errorCode}:{})});
    assert.equal(response.status(),200);assert.equal(result.isError,false);assert.ok(result.result?.id);return result.result;
   }
   stage='Create actual visible hotspot';
   const visible=await createGeo('/api/geoHotSpots',{id:require('node:crypto').randomUUID(),name:'Owned visible map hotspot',description:'Native catalogue and quest location',lat:46.98,long:8.25,isActive:true,isVisibleOnMap:true});
   mapGeoVisibleId=visible.id;assert.equal(visible.isVisibleOnMap,true);assert.equal(visible.lat,46.98);assert.equal(visible.long,8.25);
   stage='Create actual hidden hotspot';
   const hidden=await createGeo('/api/geoHotSpots',{id:require('node:crypto').randomUUID(),name:'Owned hidden map hotspot',description:'Hidden catalogue record',lat:46.981,long:8.251,isActive:true,isVisibleOnMap:false});
   mapGeoHiddenId=hidden.id;assert.equal(hidden.isVisibleOnMap,false);
   stage='Create actual linked quest';
   const quest=await createGeo('/api/quests',{id:require('node:crypto').randomUUID(),name:'Owned hotspot-linked quest',description:'Actual root quest location',isActive:true,linkedGeoHotSpotId:visible.id});
   mapGeoQuestId=quest.id;assert.equal(quest.linkedGeoHotSpotId,visible.id);
   stage='Independent geo catalogue reads';
   const near=await context.request.get(proxy5Origin+'/api/geoHotSpots/nearby?latitude=46.98&longitude=8.25&radiusKm=5',{headers:{Authorization:'Bearer '+ownedToken}});
   const nearBody=await near.json();assert.equal(near.status(),200);assert.equal(nearBody.isError,false);assert.deepEqual(nearBody.result.map(x=>x.id),[visible.id]);
   const questRead=await context.request.get(proxy5Origin+'/api/quests/all-for-avatar',{headers:{Authorization:'Bearer '+ownedToken}});
   const quests=await questRead.json();assert.equal(questRead.status(),200);assert.equal(quests.isError,false);assert.equal(quests.result.length,1);assert.equal(quests.result[0].id,quest.id);assert.equal(quests.result[0].linkedGeoHotSpotId,visible.id);
  }
  if(popupGroup==='map-places'){
   function safeVisitMessage(value){let text=String(value||'').replace(/(?:mongodb(?:\+srv)?|https?):\/\/[^\s]+/gi,'[URI]').replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g,'[JWT]');return /password|secret|credential|connectionstring|refresh.?token/i.test(text)?'Sensitive detail withheld':text.slice(0,320);}
   stage='Real place catalogue';const [catalogueReply,catalogue]=await Promise.all([page.waitForResponse(r=>new URL(r.url()).pathname==='/api/places/nearby'),page.evaluate(()=>window.oasisClient.places.nearby({category:'Mountain',latitude:46.98,longitude:8.25}))]);
   httpFacts.push({path:'/api/places/nearby',status:catalogueReply.status(),isError:catalogue.isError,routingMessage:safeVisitMessage(catalogue.message)});assert.equal(catalogueReply.status(),200);
   assert.equal(catalogue.isError,false);assert.ok(Array.isArray(catalogue.result));
   const point=catalogue.result.find(row=>row.source==='OpenStreetMap'&&String(row.id).startsWith('osm:node:')&&Number.isFinite(row.latitude)&&Number.isFinite(row.longitude));
   assert.ok(point,'The real catalogue must supply a point venue; no synthetic place substitution');mapPlaceId=point.id;
   const before=await page.evaluate(id=>window.oasisClient.http.get('api/karma/get-karma-stats/'+id),account.avatar.id);
   assert.equal(before.isError,false);mapKarmaBefore=before.result.totalKarma;assert.ok(Number.isSafeInteger(mapKarmaBefore));
   async function observe(sourceApp,latitude,longitude){
    expectedPlaceObservation={id:require('node:crypto').randomUUID(),placeId:point.id,latitude,longitude,observedUtc:new Date().toISOString(),sourceApp};
    const [reply,normalized]=await Promise.all([page.waitForResponse(r=>new URL(r.url()).pathname==='/api/places/observe'&&r.request().method()==='POST'),page.evaluate(body=>window.oasisClient.places.observe(body),expectedPlaceObservation)]);
    const raw=await reply.json();httpFacts.push({path:'/api/places/observe',status:reply.status(),isError:raw.isError===true,routingMessage:safeVisitMessage(raw.message)});
    assert.equal(reply.status(),200);assert.equal(normalized.isError,false);assert.equal(raw.isError,false);assert.notEqual(raw.isWarning,true,'Visit and avatar audit must both acknowledge');
    expectedPlaceObservation=null;return raw.result;
   }
   stage='Our World compatible real observation';const first=await observe('Our World',point.latitude,point.longitude);
   assert.equal(first.oApp,'Our World');assert.equal(first.placeId,point.id);assert.equal(first.karma,100);assert.equal(first.firstVisit,true);mapVisitIds.push(first.id);
   stage='Real place departure';const departure=await observe('Our World',point.latitude+0.02,point.longitude);assert.ok(departure==null);
   stage='Other app real return observation';const second=await observe('Shared Places CI',point.latitude,point.longitude);
   assert.equal(second.oApp,'Shared Places CI');assert.equal(second.placeId,point.id);assert.equal(second.karma,50);assert.equal(second.firstVisit,false);mapVisitIds.push(second.id);
   assert.equal(placeObservationCount,3);assert.equal(new Set(mapVisitIds).size,2);
   stage='Cross app native visit readback';const visits=await page.evaluate(()=>window.oasisClient.places.visits({offset:0,limit:50}));
   assert.equal(visits.isError,false);assert.equal(visits.result.total,2);assert.deepEqual(visits.result.items.map(row=>row.oApp).sort(),['Our World','Shared Places CI']);
   assert.deepEqual(visits.result.items.map(row=>row.id).sort(),mapVisitIds.slice().sort());
   const after=await page.evaluate(id=>window.oasisClient.http.get('api/karma/get-karma-stats/'+id),account.avatar.id);
   assert.equal(after.isError,false);assert.equal(after.result.totalKarma,mapKarmaBefore+150);
  }
  if(popupGroup==='web5-depth'){
   stage='Catalogue create actual OAPP graph';const operationId=require('node:crypto').randomUUID();
   const rootId=require('node:crypto').randomUUID(),child={id:require('node:crypto').randomUUID(),parentHolonId:rootId,holonType:'Holon',name:'Owned catalogue child',description:'Actual selected descendant'};
   const native=await context.request.post(proxy5Origin+'/api/oapps',{headers:{Authorization:'Bearer '+ownedToken,'X-COSMIC-Operation-Id':operationId,'Idempotency-Key':operationId},data:{id:rootId,name:catalogueName,description:'Owned populated depth proof',holonType:'OAPP',oappType:'GeneratedCodeOnly',children:[child]}});
   const created=await native.json();httpFacts.push({path:'/api/oapps',status:native.status(),isError:created.isError});assert.equal(native.status(),200);assert.equal(created.isError,false);catalogueAppId=created.result.id;catalogueIdentity=created.result.starnetdna.id;assert.equal(created.result.starnetdna.starnetCategory,'GeneratedCodeOnly','OAPP category is not the general record kind');assert.match(catalogueAppId,/^[0-9a-f-]{36}$/i);assert.match(catalogueIdentity,/^[0-9a-f-]{36}$/i);
   assert.equal(catalogueAppId,rootId,'Typed graph root identity retained');
   assert.ok(Array.isArray(created.result.children)&&created.result.children.some(row=>row.id===child.id),'Graph acknowledgement includes selected child');
   stage='Catalogue native depth comparison';
   for(const deep of [false,true]){
    stage=deep?'Catalogue native deep read':'Catalogue native shallow read';
    const reply=await context.request.get(proxy5Origin+'/api/oapps'+(deep?'?loadChildren=true':''),{headers:{Authorization:'Bearer '+ownedToken}});const body=await reply.json();const app=Array.isArray(body.result)?body.result.find(row=>row.id===catalogueAppId):null;
    httpFacts.push({path:'/api/oapps',status:reply.status(),isError:body.isError===true,resultShape:Array.isArray(body.result)?'array':typeof body.result,resultCount:Array.isArray(body.result)?body.result.length:0,oappShape:{loadChildren:deep,rootPresent:!!app,childrenArray:Array.isArray(app?.children),childPresent:Array.isArray(app?.children)&&app.children.some(row=>row.id===child.id),childCount:Array.isArray(app?.children)?app.children.length:0}});
    assert.equal(reply.status(),200);assert.equal(body.isError,false);assert.ok(Array.isArray(body.result));assert.ok(app);assert.ok(Array.isArray(app.children));assert.equal(app.children.some(row=>row.id===child.id),deep,'Actual descendant loading matches explicit request');
   }
  }

  if(selectedPopups.length){
   const outcomes=[];
   async function checkPopup(name,closeSelector,work){
    if(!selectedPopups.includes(name))return;
    let passed=false,closePassed=true,observedStage=name;
    try{await work();passed=true;observedStage=stage;}
    catch(error){observedStage=stage;const location=String(error.stack||'').match(/portal-startup-browser\.cjs:(\d+):/);console.error('PORTAL_STARTUP_POPUP_FAILURE:'+JSON.stringify({name,stage,errorType:['AssertionError','TimeoutError','TypeError','Error'].includes(error.name)?error.name:'Error',sourceLine:location?Number(location[1]):0}));if(name==='MapRenderer'){
     const rendererState=await page.evaluate(()=>({rendererStatus:document.getElementById('map-renderer-status')?.textContent||'',mapPresent:!!window.__ownedRealMap,glContext:!!window.__ownedRealMap?.getCanvas()?.getContext('webgl2')})).catch(()=>({rendererStatus:'Unavailable',mapPresent:false,glContext:false}));
     function safeRendererText(value){let text=String(value||'').split('\n')[0].replace(/(?:https?|mongodb(?:\+srv)?):\/\/[^\s]+/gi,'[URI]').replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g,'[JWT]');return /password|secret|credential|connectionstring|refresh.?token/i.test(text)?'Sensitive detail withheld':text.slice(0,320);}
     console.error('PORTAL_STARTUP_RENDERER_FAILURE:'+JSON.stringify({stage,errorType:['AssertionError','TimeoutError','TypeError','Error'].includes(error.name)?error.name:'Error',sourceLine:location?Number(location[1]):0,message:safeRendererText(error.message),rendererStatus:safeRendererText(rendererState.rendererStatus),mapPresent:rendererState.mapPresent,glContext:rendererState.glContext,blockedAssetCount:otherServices.filter(row=>row.includes('unpkg.com')).length}));
    }if(name==='Clan'){
     const state=await page.evaluate(()=>({status:document.getElementById('clan-status')?.textContent||'',nameLength:document.getElementById('clan-create-name')?.value.length||0,buttonDisabled:document.getElementById('clan-create-btn')?.disabled===true,activeTab:document.querySelector('#clan-modal-block .map-tab.is-active')?.dataset.tab||'unknown',
      formOwner:document.getElementById('clan-create-btn')?.form?.id||'none',
      formValid:document.getElementById('clan-create-form')?.matches(':valid')===true,
      invalidFields:Array.from(document.getElementById('clan-create-form')?.querySelectorAll(':invalid')||[]).map(node=>node.id),
      bound:document.getElementById('clan-modal-block')?.dataset.clanBound==='true',
      hit:(()=>{const button=document.getElementById('clan-create-btn'),rect=button?.getBoundingClientRect(),hit=rect&&document.elementFromPoint(rect.left+rect.width/2,rect.top+rect.height/2);return hit?{tag:hit.tagName,id:hit.id,classes:String(hit.className).slice(0,120)}:null;})()})).catch(()=>({unavailable:true}));
     if(state.status){state.status=state.status.replace(/(?:https?|mongodb(?:\+srv)?):\/\/[^\s]+/gi,'[URI]').replace(/eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+/g,'[JWT]').slice(0,320);if(/password|secret|credential|connectionstring|refresh.?token/i.test(state.status))state.status='Sensitive detail withheld';}
     const match=String(error.stack||'').match(/portal-startup-browser\.cjs:(\d+):/);state.errorType=['AssertionError','TimeoutError','TypeError','Error'].includes(error.name)?error.name:'Error';state.sourceLine=match?Number(match[1]):0;state.operation=String(error.message||'').startsWith('page.waitForResponse')?'response':String(error.message||'').startsWith('locator.click')?'click':'other';
     console.error('PORTAL_STARTUP_CLAN_FAILURE:'+JSON.stringify(state));
    }}
    try{
     if(await page.locator(closeSelector).isVisible()){
      await page.locator(closeSelector).click();await page.waitForURL(url=>url.pathname==='/portal.html');
     }
    }catch(error){console.error('PORTAL_STARTUP_POPUP_CLOSE_FAILURE:'+JSON.stringify({name,errorType:error.name,path:new URL(page.url()).pathname}));closePassed=false;passed=false;if(observedStage===name)observedStage=name+' close';}
    outcomes.push({name,passed,stage:observedStage,closePassed});
   }
   await checkPopup('MapRenderer','#map-close-btn',async()=>{
    stage='Map real stored location';
    await page.locator('#dashboard button.dash-panel-link').filter({hasText:'View Map'}).click();
    await page.locator('#map-latitude').fill('46.98');await page.locator('#map-longitude').fill('8.25');await page.locator('#map-radius').fill('5');
    await page.locator('#map-refresh-btn').click();
    await page.waitForFunction(()=>document.getElementById('map-target')?.options.length===2);
    stage='Real MapLibre renderer';
    await page.evaluate(async()=>{
     const library=await import('https://unpkg.com/maplibre-gl@6.19.0/dist/maplibre-gl.mjs'),original=library.Map.prototype.addControl;
     library.Map.prototype.addControl=function(...args){window.__ownedRealMap=this;library.Map.prototype.addControl=original;return original.apply(this,args);};
    });
    await page.locator('#map-show-interactive').click();
    await page.waitForFunction(()=>window.__ownedRealMap?.isStyleLoaded()&&window.__ownedRealMap.getLayer('selected-item'));
    assert.equal(await page.evaluate(()=>window.__ownedRealMap.getCanvas().getContext('webgl2')?.drawingBufferWidth>0),true,'Actual WebGL2 context');
    await page.locator('#map-modal-block [data-tab="controls"]').click();
    const target=await page.locator('#map-target option').filter({hasText:'Owned renderer point'}).getAttribute('value');assert.ok(target);
    await page.locator('#map-target').selectOption(target);
    stage='Map actual item selection';await page.locator('#map-select-holon-btn').click();
    await page.waitForFunction(()=>document.querySelector('.maplibregl-popup-content')?.textContent.includes('Owned renderer point'));
    const selected=await page.evaluate(()=>({lat:window.__ownedRealMap.getCenter().lat,lng:window.__ownedRealMap.getCenter().lng,zoom:window.__ownedRealMap.getZoom()}));
    assert.ok(Math.abs(selected.lat-46.98)<1e-6&&Math.abs(selected.lng-8.25)<1e-6);
    stage='Map actual zoom controls';await page.locator('#map-zoom-value').fill('1');
    await page.locator('#map-zoom-in-btn').click();assert.equal(await page.evaluate(()=>window.__ownedRealMap.getZoom()),selected.zoom+1);
    await page.locator('#map-zoom-out-btn').click();assert.equal(await page.evaluate(()=>window.__ownedRealMap.getZoom()),selected.zoom);
    stage='Map actual pan controls';await page.locator('#map-pan-value').fill('100');
    for(const [direction,axis,sign] of [['up','lat',1],['down','lat',-1],['left','lng',-1],['right','lng',1]]){
     const before=await page.evaluate(axis=>window.__ownedRealMap.getCenter()[axis],axis);
     await page.locator('#map-pan-'+direction+'-btn').click();
     const after=await page.evaluate(axis=>window.__ownedRealMap.getCenter()[axis],axis);assert.ok((after-before)*sign>0.001,'Actual '+direction+' pan');
    }
    stage='Map actual target zoom';await page.locator('#map-zoom-holon-btn').click();
    assert.equal(await page.evaluate(()=>window.__ownedRealMap.getZoom()),15);
    stage='Map actual pin highlight';await page.locator('#map-highlight-building-btn').click();
    await page.waitForFunction(id=>window.__ownedRealMap.isSourceLoaded('selected-item')&&window.__ownedRealMap.queryRenderedFeatures({layers:['selected-item']}).some(feature=>feature.properties.identity===id),mapRendererRecordId);
    assert.ok(!reads.some(read=>/^\/api\/map\/(Zoom|Pam|Select|Highlight)/.test(read.path)),'No legacy server camera command');
   });
   if(popupGroup==='map-renderer'){
    assert.equal(await page.evaluate(()=>window.__ownedRealMap.getCanvas().isConnected),false,'Real renderer disposed');
    assert.equal(await page.locator('#map-target option').count(),1,'Private loaded targets cleared');
    assert.equal(await page.locator('#map-zoom-in-btn').isDisabled(),true);
   }
   await checkPopup('MapGeo','#map-close-btn',async()=>{
    stage='Map actual hotspot and quest layers';
    await page.locator('#dashboard button.dash-panel-link').filter({hasText:'View Map'}).click();
    await page.locator('#map-latitude').fill('46.98');await page.locator('#map-longitude').fill('8.25');await page.locator('#map-radius').fill('5');
    await page.locator('#map-modal-block [data-tab="geo"]').click();await page.locator('#map-load-geo').click();
    await page.waitForFunction(()=>document.getElementById('map-geo-status')?.textContent==='0 GeoNFTs, 1 hotspots, 1 quest/objective locations loaded.');
    for(const endpoint of ['/api/geoHotSpots/nearby','/api/quests/all-for-avatar'])
     assert.ok(reads.some(read=>read.path===endpoint&&read.status===200),'Actual map layer API '+endpoint);
    assert.ok(mapGeoVisibleId&&mapGeoHiddenId&&mapGeoQuestId);
   });
   await checkPopup('MapPlaces','#map-close-btn',async()=>{
    stage='Map actual cross app place visits';await page.locator('#dashboard button.dash-panel-link').filter({hasText:'View Map'}).click();
    await page.locator('#map-modal-block [data-tab="places"]').click();
    await page.waitForFunction(()=>{const s=document.getElementById('map-places-list')?.textContent||'';return s.includes('Our World')&&s.includes('Shared Places CI');});
    const rows=page.locator('#map-places-list .map-history-row');assert.equal(await rows.count(),2);
    const text=await page.locator('#map-places-list').textContent();assert.ok(text.includes(mapPlaceId));assert.ok(text.includes('100 karma'));assert.ok(text.includes('50 karma'));
    assert.equal(await page.locator('#map-places-more').isVisible(),false);assert.equal(placeObservationCount,3);
   });
   await checkPopup('ONET','#onet-close-btn',async()=>{
    stage='ONET real initial reads';await page.evaluate(()=>window.openONETModal());
    await page.waitForFunction(()=>['ONLINE','OFFLINE'].includes(document.getElementById('onet-status-label')?.textContent));
    await page.waitForFunction(()=>document.getElementById('onet-stats-grid')?.textContent.includes('Network Running'));
    const before=await context.request.get(proxyOrigin+'/api/v1/onet/network/status',{headers:{Authorization:'Bearer '+ownedToken}});
    assert.equal(before.status(),200);const initial=await before.json();assert.equal(initial.isError,false);assert.equal(initial.result.isRunning,false);
    await page.locator('#onet-modal-block [data-tab="nodes"]').click();
    await page.waitForFunction(()=>document.getElementById('onet-active-onodes')?.textContent==='No ONODEs have synced recently.');
    assert.ok((await page.locator('#onet-nodes-list').textContent()).includes('No nodes found'));
    await page.locator('#onet-modal-block [data-tab="topology"]').click();
    await page.waitForFunction(()=>{try{const t=JSON.parse(document.getElementById('onet-topology').textContent);return Array.isArray(t.nodes)&&Array.isArray(t.connections);}catch{return false;}});
    await page.locator('#onet-modal-block [data-tab="actions"]').click();
    stage='ONET real local start';await page.locator('#onet-start-btn').click();
    await page.waitForFunction(()=>document.getElementById('onet-modal-status').textContent==='Start successful.'&&document.getElementById('onet-status-label').textContent==='ONLINE');
    const started=await context.request.get(proxyOrigin+'/api/v1/onet/network/status',{headers:{Authorization:'Bearer '+ownedToken}});
    assert.equal(started.status(),200);const live=await started.json();assert.equal(live.isError,false);assert.equal(live.result.isRunning,true);
    stage='ONET real local stop';await page.locator('#onet-stop-btn').click();
    await page.waitForFunction(()=>document.getElementById('onet-modal-status').textContent==='Stop successful.'&&document.getElementById('onet-status-label').textContent==='OFFLINE');
    const stopped=await context.request.get(proxyOrigin+'/api/v1/onet/network/status',{headers:{Authorization:'Bearer '+ownedToken}});
    assert.equal(stopped.status(),200);const off=await stopped.json();assert.equal(off.isError,false);assert.equal(off.result.isRunning,false);
    assert.deepEqual(onetWrites,{'/api/v1/onet/network/start':1,'/api/v1/onet/network/stop':1});
   });
   await checkPopup('HolonDiff','#holon-diff-modal-close-btn',async()=>{
    stage='Holon Diff published versions';await page.evaluate(id=>window.openHolonDiffModal(id),diffIdentity);
    await page.waitForFunction(()=>document.getElementById('holon-diff-status')?.textContent==='Comparing published versions 1.0.0 and 2.0.0.');
    assert.equal(await page.locator('#holon-diff-ver-a option').count(),2);
    assert.ok((await page.locator('#holon-diff-pre-a').textContent()).includes('1.0.0'));assert.ok((await page.locator('#holon-diff-pre-b').textContent()).includes('2.0.0'));
    stage='Holon Diff actual stored seal identities';await page.waitForFunction(()=>{const s=document.getElementById('holon-diff-seal');return s?.children.length===2&&!s.textContent.includes('checking sealed history')&&!s.textContent.includes('verification failed');});
    assert.deepEqual([...diffSealReads].sort(),[...diffStoredIds].sort());
    const seal=await page.locator('#holon-diff-seal').textContent();assert.ok(seal.includes('Version 1.0.0:'));assert.ok(seal.includes('Version 2.0.0:'));
   });
   await checkPopup('Migration','#provider-migration-modal-close-btn',async()=>{
    stage='Migration registered real providers';await page.evaluate(()=>window.openProviderMigrationModal());
    await page.locator('#pmig-form:not([hidden])').waitFor({state:'visible'});
    await page.locator('#pmig-holon-id').fill(migrationRootId);await page.locator('#pmig-src').selectOption('MongoDBOASIS');await page.locator('#pmig-dst').selectOption('SQLLiteDBOASIS');
    stage='Migration real source preview';await page.locator('#pmig-preview-source').click();
    await page.waitForFunction(()=>document.getElementById('pmig-preview').textContent.includes('Source: Owned migration root')&&document.getElementById('pmig-preview').textContent.includes('MongoDBOASIS'));
    stage='Migration real destination copy';await page.locator('#pmig-form button[type="submit"]').click();
    await page.waitForFunction(id=>document.getElementById('pmig-status').textContent==='Copy confirmed by WEB4 for '+id+'.',migrationRootId);
    stage='Migration real destination preview';await page.locator('#pmig-preview-destination').click();
    await page.waitForFunction(()=>document.getElementById('pmig-preview').textContent.includes('Destination: Owned migration root')&&document.getElementById('pmig-preview').textContent.includes('SQLLiteDBOASIS'));
    stage='Migration independent graph readback';
    for(const provider of ['MongoDBOASIS','SQLLiteDBOASIS']){
     const read=await context.request.post(proxyOrigin+'/api/data/load-holon',{headers:{Authorization:'Bearer '+ownedToken,'Idempotency-Key':require('node:crypto').randomUUID()},data:{id:migrationRootId,providerType:provider,loadChildren:true,recursive:true}});
     const body=await read.json(),loaded=body.result;
     httpFacts.push({path:'/api/data/load-holon',status:read.status(),isError:loaded?.isError===true,migrationShape:{provider,rootPresent:loaded?.result?.id===migrationRootId,childPresent:Array.isArray(loaded?.result?.children)&&loaded.result.children.some(row=>row.id===migrationChildId),childCount:Array.isArray(loaded?.result?.children)?loaded.result.children.length:0},...(typeof loaded?.errorCode==='string'&&/^[A-Z0-9_]{1,80}$/.test(loaded.errorCode)?{errorCode:loaded.errorCode}:{})});
     assert.equal(read.status(),200);assert.equal(loaded.isError,false);assert.equal(loaded.result.name,'Owned migration root');assert.ok(loaded.result.children.some(row=>row.id===migrationChildId&&row.parentHolonId===migrationRootId));
    }
    assert.equal(migrationCopies,1);assert.deepEqual(migrationReads,{MongoDBOASIS:1,SQLLiteDBOASIS:1});
    stage='Migration persisted native SQLite';
    const owner='oasis_startup_'+process.env.GITHUB_RUN_ID+'_'+process.env.GITHUB_RUN_ATTEMPT;assert.match(owner,/^oasis_startup_[0-9]+_[0-9]+$/);
    const check="const fs=require('fs'),assert=require('assert/strict');const p="+JSON.stringify('/tmp/'+owner+'_migration.sqlite')+";assert.ok(fs.statSync(p).isFile());const fd=fs.openSync(p,'r'),header=Buffer.alloc(16);try{fs.readSync(fd,header,0,16,0);}finally{fs.closeSync(fd);}assert.equal(header.subarray(0,15).toString('ascii'),'SQLite format 3');assert.equal(header[15],0);";
    execFileSync('docker',['exec',owner+'_api','node','-e',check],{encoding:'utf8',timeout:15000});
   });
   await checkPopup('OappMap','#oapp-map-modal-close-btn',async()=>{
    stage='OAPP map explicit depth';const [response]=await Promise.all([page.waitForResponse(r=>new URL(r.url()).pathname.toLowerCase()==='/api/oapps'),page.evaluate(()=>window.openOappMapModal())]);
    assert.equal(new URL(response.url()).searchParams.get('loadChildren'),'true');const raw=await response.json();assert.equal(raw.isError,false);assert.ok(Array.isArray(raw.result));assert.equal(raw.result.length,popupGroup==='web5-depth'?1:0);
    await page.waitForFunction(expected=>document.getElementById('oapp-map-status').textContent===expected,popupGroup==='web5-depth'?'1 OAPPs · returned relationships only':'No OAPPs found.');
    stage='OAPP map filters';await page.locator('#oapp-map-filter').fill('no matching OAPP');assert.equal(await page.locator('#oapp-map-detail').isHidden(),true);await page.locator('#oapp-map-reset-btn').click();
   });
   await checkPopup('Launcher','#oapp-launcher-modal-close-btn',async()=>{
    stage='OAPP launcher shallow catalogue';const [response]=await Promise.all([page.waitForResponse(r=>new URL(r.url()).pathname.toLowerCase()==='/api/oapps'),page.evaluate(()=>window.openOappLauncherModal())]);
    assert.equal(new URL(response.url()).searchParams.get('loadChildren'),null);const raw=await response.json();assert.equal(raw.isError,false);assert.ok(Array.isArray(raw.result));assert.equal(raw.result.length,popupGroup==='web5-depth'?1:0);
    await page.waitForFunction(expected=>document.getElementById('oapp-launcher-status').textContent===expected,popupGroup==='web5-depth'?'1 OAPP':'0 OAPPs');
    if(popupGroup==='web5-depth'){
     stage='OAPP launcher category';assert.ok((await page.locator('#oapp-store-grid').textContent()).includes('OAPP type: GeneratedCodeOnly'));await page.locator('#oapp-launcher-search').fill('GeneratedCodeOnly');assert.equal(await page.locator('#oapp-launcher-status').textContent(),'1 OAPP');
     stage='OAPP launcher actual detail';await page.evaluate(id=>window.openOappLauncherModal(id),catalogueIdentity);await page.waitForFunction(()=>document.getElementById('oapp-launcher-status').textContent==='OAPP details');assert.ok((await page.locator('#oapp-store-grid').textContent()).includes(catalogueName));
     stage='OAPP launcher browse return';await page.locator('#oapp-launcher-browse-btn').click();await page.waitForFunction(()=>document.getElementById('oapp-launcher-status').textContent==='1 OAPP');
    }
    stage='OAPP launcher filter';await page.locator('#oapp-launcher-search').fill('no matching OAPP');assert.equal(await page.locator('#oapp-launcher-browse-btn').isHidden(),true);
   });
   await checkPopup('SocialGraph','#social-graph-modal-close-btn',async()=>{
    stage='Social graph native contacts';
    await page.evaluate(()=>window.openSocialGraphModal());
    await page.waitForFunction(()=>document.getElementById('social-graph-status').textContent.includes('1 messaging contacts'));
    const contact=page.locator('#social-graph-svg [data-contact-id="'+recipient.avatar.id.toLowerCase()+'"]');
    await contact.waitFor({state:'visible'});
    assert.equal(await page.locator('#social-graph-svg circle').count(),2);
    assert.equal(await page.locator('#social-graph-more-btn').isVisible(),false);
    stage='Social graph composer handoff';await contact.focus();await contact.press('Enter');
    await page.locator('#messaging-tab-send').waitFor({state:'visible'});
    assert.equal(await page.locator('#msg-to-avatar').inputValue(),recipient.avatar.id.toLowerCase());
    assert.equal(sentMessage,0,'Contact navigation must never send a message');
    await page.goBack();
    await page.locator('#social-graph-modal-block.is-selected').waitFor({state:'visible'});
   });
   await checkPopup('ActivityFeed','#activity-feed-modal-close-btn',async()=>{
    stage='Activity feed native empty read';
    await page.evaluate(()=>window.openActivityFeedModal());
    await page.waitForFunction(()=>document.getElementById('activity-feed-status').textContent==='No activity yet.');
    assert.equal(await page.locator('#activity-feed-list').textContent(),'');
    assert.equal(await page.locator('#activity-feed-type-sel option').count(),1);
    stage='Activity feed refresh';
    const refreshed=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/karma/activity');
    await page.locator('#activity-feed-refresh-btn').click();
    const raw=await(await refreshed).json(),result=typeof raw.isError==='boolean'?raw:raw.result;
    console.log('PORTAL_STARTUP_ACTIVITY_SHAPE:'+JSON.stringify({topKeys:Object.keys(raw),isErrorType:typeof raw.isError,resultArray:Array.isArray(raw.result),innerKeys:raw.result&&!Array.isArray(raw.result)&&typeof raw.result==='object'?Object.keys(raw.result):[],innerErrorType:typeof raw.result?.isError}));
    assert.equal(result.isError,false);assert.deepEqual(result.result,[]);
    await page.waitForFunction(()=>document.getElementById('activity-feed-status').textContent==='No activity yet.');
   });
   await checkPopup('NFT','#nft-modal-close-btn',async()=>{
   stage='NFT catalogue';await page.locator('#dash-card-nfts').click();
   await page.waitForFunction(()=>['standard','geo','oland'].every(kind=>
    !document.getElementById('nft-empty-'+kind).hidden&&document.getElementById('nft-error-'+kind).hidden));
   stage='NFT grouping';await page.locator('#nft-group-by-collection').check();
   assert.equal(await page.locator('#nft-empty-standard').isVisible(),true);
   assert.equal(await page.locator('#nft-error-standard').isVisible(),false);
   stage='NFT tabs';
   for(const tab of ['geo','oland']){
    await page.locator('#nft-modal-block [data-tab="'+tab+'"]').click();
    await page.locator('#nft-tab-'+tab).waitFor({state:'visible'});
   }
   });
   await checkPopup('Data','#data-modal-close-btn',async()=>{
   stage='Data browse';const dataRead=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/load-holons-for-parent');
   await page.locator('#dash-card-holons').click();
   const raw=await (await dataRead).json();assert.equal(raw.result.isError,false,'Real Data result');
   assert.ok(Array.isArray(raw.result.result),'Real Data list');
   await page.waitForFunction(n=>document.getElementById('data-modal-status').hidden&&
    document.querySelectorAll('#data-browse-grid .data-holon-card').length===n,raw.result.result.length);
   assert.equal(dataReads,1);
   });
   await checkPopup('DataWrite','#data-modal-close-btn',async()=>{
    stage='Data create form';await page.locator('#dash-card-holons').click();
    await page.waitForFunction(()=>document.getElementById('data-modal-status').hidden);
    await page.locator('#data-modal-block [data-tab="save"]').click();
    await page.locator('#data-save-name').fill(dataName);await page.locator('#data-save-desc').fill('Owned create read delete journey');
    assert.equal(await page.locator('#data-save-provider').inputValue(),'','Keep default provider selection');
    stage='Data create';const create=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/save-holon'&&r.request().method()==='POST');
    await page.locator('#data-save-btn').click();const created=await (await create).json();
    assert.equal(created.result.isError,false,'Real conditional create');savedHolonId=created.result.result.id;
    assert.match(savedHolonId,/^[0-9a-f-]{36}$/i);assert.equal(created.result.result.name,dataName);
    stage='Data point read';await page.locator('#data-modal-block [data-tab="load"]').click();
    await page.locator('#data-load-id').fill(savedHolonId);
    const point=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/load-holon'&&r.request().method()==='POST');
    await page.locator('#data-load-btn').click();const loaded=await (await point).json();
    assert.equal(loaded.result.isError,false);assert.equal(loaded.result.result.id,savedHolonId);assert.equal(loaded.result.result.description,'Owned create read delete journey');
    await page.locator('#data-load-result .data-holon-card').waitFor({state:'visible'});
    if(popupGroup==='data-edit'){
     stage='Data edit';await page.locator('#data-load-result .data-holon-card').click();
     await page.locator('#data-detail-edit-name').fill(dataName+' edited');
     await page.locator('#data-detail-edit-description').fill('Updated through detail form');
     await page.locator('#data-detail-edit-save').click();
     await page.waitForFunction(()=>document.getElementById('data-detail-edit-status')?.textContent==='Changes saved.');
     await page.locator('#data-detail-back-btn').click();
     stage='Data edited point read';const updatedRead=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/load-holon'&&r.request().method()==='POST');
     await page.locator('#data-load-btn').click();const updated=await(await updatedRead).json();
     assert.equal(updated.result.isError,false);assert.equal(updated.result.result.id,savedHolonId);
     assert.equal(updated.result.result.name,dataName+' edited');assert.equal(updated.result.result.description,'Updated through detail form');
    }
    stage='Data delete confirmation';await page.locator('#data-modal-block [data-tab="browse"]').click();
    await page.locator('#data-browse-grid .data-card-delete[data-id="'+savedHolonId+'"]').click();
    await page.locator('#data-confirm-overlay').waitFor({state:'visible'});
    const deletion=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/delete-holon'&&r.request().method()==='DELETE');
    const after=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/data/load-holons-for-parent'&&r.request().method()==='POST');
    stage='Data delete';await page.locator('#data-confirm-delete-btn').click();const deleted=await (await deletion).json();
    assert.equal(deleted.result.isError,false);assert.equal(deleted.result.result.id,savedHolonId,'Acknowledged deleted holon identity');
    const remaining=await (await after).json();assert.equal(remaining.result.isError,false);assert.ok(Array.isArray(remaining.result.result));
    assert.ok(!remaining.result.result.some(row=>row.id===savedHolonId));
    await page.waitForFunction(id=>document.getElementById('data-modal-status').hidden&&
     !document.querySelector('#data-browse-grid [data-holon-idx="'+id+'"]'),savedHolonId);
    assert.equal(holonSaves,popupGroup==='data-edit'?2:1);assert.equal(pointReads,popupGroup==='data-edit'?2:1);assert.equal(holonDeletes,1);
   });
   await checkPopup('Clan','#clan-close-btn',async()=>{
    stage='Clan create form';await page.evaluate(()=>window.openClanModal());await page.waitForFunction(()=>document.getElementById('clan-browse-list').textContent.includes('No clans found.'));
    await page.locator('#clan-modal-block [data-tab="create"]').click();await page.locator('#clan-create-name').fill(clanName);await page.locator('#clan-create-desc').fill('Owned clan journey');
    stage='Clan create';const [createResponse]=await Promise.all([page.waitForResponse(r=>new URL(r.url()).pathname==='/api/clan'&&r.request().method()==='POST'),page.locator('#clan-create-btn').click()]);
    const createdRaw=await createResponse.json(),created=typeof createdRaw.isError==='boolean'?createdRaw:createdRaw.result;assert.equal(created.isError,false);clanId=created.result.id;assert.match(clanId,/^[0-9a-f-]{36}$/i);
    assert.equal(created.result.ownerAvatarId,account.avatar.id);assert.ok(created.result.memberIds.includes(account.avatar.id));
    await page.locator('#clan-edit-form').waitFor({state:'attached'});await page.locator('#clan-detail-panel details summary').click();
    stage='Clan edit';await page.locator('#clan-edit-name').fill(clanName+' edited');await page.locator('#clan-edit-description').fill('Edited clan journey');await page.locator('#clan-edit-form button').click();
    await page.waitForFunction(name=>document.querySelector('#clan-detail-panel h3')?.textContent===name,clanName+' edited');
    stage='Clan add member';await page.locator('#clan-add-username').fill(recipient.username);await page.locator('#clan-add-member-form button').click();
    await page.locator('[data-clan-remove="'+recipient.avatar.id+'"]').waitFor({state:'visible'});
    const native=await recipient.call('owned clan member readback','GET','/api/clan/'+clanId,undefined,recipientToken,true);assert.equal(native.name,clanName+' edited');assert.equal(native.description,'Edited clan journey');assert.ok(native.memberIds.includes(recipient.avatar.id));
    stage='Clan delete';page.once('dialog',dialog=>dialog.accept());await page.locator('#clan-delete-btn').click();await page.waitForFunction(()=>document.getElementById('clan-browse-list').textContent.includes('No clans found.')&&document.getElementById('clan-stat-total').textContent==='0');
    assert.equal(clanCreates,1);assert.equal(clanUpdates,1);assert.equal(clanMembers,1);assert.equal(clanDeletes,1);
   });
   await checkPopup('Chat','#messaging-modal-close-btn',async()=>{
    stage='Chat start';await page.locator('#dash-card-messages').click();await page.locator('#messaging-modal-block [data-tab="chat"]').click();
    await page.waitForFunction(()=>document.getElementById('msg-active-sessions').textContent.includes('No active chats.'));
    await page.locator('#msg-chat-recipient').fill(recipient.avatar.id);
    const start=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/chat/start-new-chat-session');await page.locator('#msg-start-session-btn').click();
    const startRaw=await(await start).json(),started=typeof startRaw.isError==='boolean'?startRaw:startRaw.result;
    assert.equal(started.isError,false);chatId=started.result;assert.ok(typeof chatId==='string'&&chatId.length>0);
    await page.locator('#msg-chat-session-wrap').waitFor({state:'visible'});
    stage='Chat send';await page.locator('#msg-chat-input').fill(messageText);await page.locator('#msg-chat-send-btn').click();
    await page.waitForFunction(value=>document.getElementById('msg-chat-messages').textContent.includes(value),messageText);
    stage='Chat participant readback';const history=await recipient.call('participant history','GET','/api/chat/history/'+encodeURIComponent(chatId)+'?limit=50&offset=0',undefined,recipientToken);
    assert.ok(Array.isArray(history));assert.equal(history.filter(row=>row.message===messageText||row.content===messageText).length,1);
    stage='Chat resume';await page.locator('#msg-chat-choose-btn').click();await page.locator('[data-chat-resume="'+chatId+'"]').click();
    await page.waitForFunction(value=>document.getElementById('msg-chat-messages').textContent.includes(value),messageText);
    stage='Chat end';const endResponse=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/chat/end-chat-session/'+chatId);await page.locator('#msg-end-session-btn').click();
    const endRaw=await(await endResponse).json(),ended=typeof endRaw.isError==='boolean'?endRaw:endRaw.result;assert.equal(ended.isError,false);assert.equal(ended.result,true);
    await page.waitForFunction(()=>document.getElementById('msg-no-session').hidden===false&&document.getElementById('msg-active-sessions').textContent.includes('No active chats.'));
    const sessions=await recipient.call('participant ended sessions','GET','/api/chat/active-sessions',undefined,recipientToken);assert.ok(Array.isArray(sessions));assert.ok(!sessions.some(row=>row.id===chatId));
    assert.equal(chatStarts,1);assert.equal(chatSends,1);assert.equal(chatEnds,1);
   });
   await checkPopup('DirectoryMessage','#messaging-modal-close-btn',async()=>{
    stage='Directory search';await page.locator('#dashboard button[onclick*="openSearchModal"]').click();
    await page.locator('#search-avatar-input').fill(recipient.username);await page.locator('#search-avatar-btn').click();
    await page.locator('[data-search-message="'+recipient.avatar.id+'"]').waitFor({state:'visible'});
    stage='Directory compose';await page.locator('[data-search-message="'+recipient.avatar.id+'"]').click();
    await page.locator('#messaging-tab-send').waitFor({state:'visible'});
    assert.equal(await page.locator('#msg-to-avatar').inputValue(),recipient.avatar.id);assert.equal(sentMessage,0,'Directory handoff must not send');
    await page.locator('#msg-body').fill(messageText);await page.locator('#msg-send-btn').click();
    await page.waitForFunction(()=>document.getElementById('messaging-modal-status').textContent==='Message sent.');
    stage='Directory recipient readback';const inbox=await recipient.call('directory recipient native inbox','GET','/api/messaging/messages?limit=50&offset=0',undefined,recipientToken);
    assert.equal(inbox.filter(row=>row.content===messageText&&row.fromAvatarId===account.avatar.id&&row.toAvatarId===recipient.avatar.id).length,1);assert.equal(sentMessage,1);
    stage='Directory return';await page.locator('#messaging-modal-close-btn').click();await page.waitForURL(url=>url.pathname==='/search');
    await page.locator('#search-modal-close-btn').click();await page.waitForURL(url=>url.pathname==='/');
   });
   await checkPopup('HyperDrive','#hd-close-btn',async()=>{
    stage='HyperDrive status';const statusResponse=page.waitForResponse(r=>new URL(r.url()).pathname.toLowerCase()==='/api/hyperdrive/status');
    await page.locator('#dashboard .dash-qa-btn').filter({hasText:'HyperDrive'}).click();
    const raw=await(await statusResponse).json(),response=typeof raw.isError==='boolean'?raw:raw.result;assert.equal(response.isError,false);
    const status=response.result;assert.equal(typeof status.totalProviders,'number');
    await page.waitForFunction(count=>document.getElementById('hd-stat-count').textContent===String(count),status.totalProviders);
    stage='HyperDrive routing panels';for(const tab of ['failover','replication','loadbalance']){await page.locator('#hd-modal-block [data-tab="'+tab+'"]').click();assert.equal(await page.locator('#hd-tab-'+tab).isVisible(),true);}
    stage='HyperDrive routing preferences';await page.locator('#hd-modal-block [data-tab="settings"]').click();await page.locator('#hd-settings-form').waitFor({state:'visible'});
    assert.equal(await page.locator('#hd-settings-fields').isDisabled(),false);
   });
   await checkPopup('MessagingRead','#messaging-modal-close-btn',async()=>{
    stage='Inbox unread state';await page.waitForFunction(()=>document.getElementById('dash-card-messages').textContent==='1 unread');
    await page.locator('#dash-card-messages').click();
    await page.locator('#messaging-modal-block [data-tab="notifications"]').click();
    await page.locator('#msg-notifications-list .msg-notification-row:not(.msg-notification-row--read)').first().waitFor({state:'visible'});
    stage='Inbox mark read';await page.locator('#msg-mark-read-btn').click();
    await page.waitForFunction(()=>document.getElementById('messaging-modal-status').textContent==='Inbox marked as read.');
    await page.waitForFunction(()=>document.querySelectorAll('#msg-notifications-list .msg-notification-row--read').length>0&&!document.querySelector('#msg-notifications-list .msg-notification-row:not(.msg-notification-row--read)'));
    stage='Inbox persisted readback';
    const messages=await account.call('marked native messages','GET','/api/messaging/messages?limit=50&offset=0',undefined,ownedToken);
    assert.equal(messages.find(row=>row.id===incomingMessageId)?.isRead,true);
    const notifications=await account.call('marked native notifications','GET','/api/messaging/notifications?limit=20&offset=0',undefined,ownedToken);
    assert.ok(incomingNotificationIds.every(id=>notifications.some(row=>row.id===id&&row.isRead===true)));
    stage='Inbox dashboard refresh';await page.waitForFunction(()=>document.getElementById('dash-card-messages').textContent==='1');
    assert.equal(markedMessages,1);assert.equal(markedNotifications,1);
   });
   await checkPopup('FilesWrite','#files-close-btn',async()=>{
    stage='Files upload form';await page.locator('#dash-card-files').click();
    await page.waitForFunction(()=>document.getElementById('files-stat-count').textContent==='0');
    await page.locator('#files-modal-block [data-tab="upload"]').click();
    await page.locator('#files-upload-input').setInputFiles({name:'owned-roundtrip.txt',mimeType:'text/plain',buffer:fileBytes});
    await page.locator('#files-upload-desc').fill('Owned file roundtrip');
    stage='Files upload';const uploadResponse=page.waitForResponse(r=>new URL(r.url()).pathname==='/api/files/upload-file');
    await page.locator('#files-upload-btn').click();const raw=await(await uploadResponse).json(),upload=typeof raw.isError==='boolean'?raw:raw.result;
    assert.equal(upload.isError,false,'Actual file upload');fileId=upload.result.id;assert.match(fileId,/^[0-9a-f-]{36}$/i);
    await page.locator('[data-file-download="'+fileId+'"]').waitFor({state:'visible'});
    assert.ok((await page.locator('#files-list').textContent()).includes('Owned file roundtrip'));
    stage='Files download';const downloadEvent=page.waitForEvent('download');await page.locator('[data-file-download="'+fileId+'"]').click();
    const download=await downloadEvent;
    try{assert.equal(download.suggestedFilename(),'owned-roundtrip.txt');const stream=await download.createReadStream();const chunks=[];for await(const chunk of stream)chunks.push(chunk);assert.deepEqual(Buffer.concat(chunks),fileBytes);}finally{await download.delete();}
    stage='Files delete';page.once('dialog',dialog=>dialog.accept());await page.locator('[data-file-delete="'+fileId+'"]').click();
    await page.waitForFunction(()=>document.getElementById('files-stat-count').textContent==='0'&&document.getElementById('files-list').textContent.includes('No files stored yet'));
    assert.equal(fileUploads,1);assert.equal(fileDeletes,1);
   });
   await checkPopup('MapRoute','#map-close-btn',async()=>{
    stage='Map routes open';await page.locator('#dashboard button.dash-panel-link').filter({hasText:'View Map'}).click();
    await page.locator('#map-modal-block [data-tab="routes"]').click();
    stage='Map GPX import';
    const gpx='<?xml version="1.0"?><gpx xmlns="http://www.topografix.com/GPX/1/1" version="1.1" creator="Owned fixture"><trk><name>Owned walk</name><extensions><route xmlns="https://oasisomniverse.one/schemas/map-route/v1" sourceApp="Our World" sourceRouteId="owned-walk"/></extensions><trkseg><trkpt lat="21.1" lon="-86.8"><time>2026-11-21T10:00:00.1234567Z</time></trkpt><trkpt lat="21.1001" lon="-86.8001"><time>2026-11-21T10:01:00Z</time></trkpt></trkseg></trk></gpx>';
    await page.locator('#map-route-file').setInputFiles({name:'owned-walk.gpx',mimeType:'application/gpx+xml',buffer:Buffer.from(gpx)});
    await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.includes('2 points in 1 connected segments'));
    assert.equal(await page.locator('#map-route-public').isChecked(),false);
    stage='Map route publish';await page.locator('#map-route-save').click();
    await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.startsWith('Private to its owner.'));
    assert.ok(routeId);assert.equal(await page.locator('#map-route-id').inputValue(),routeId);
    stage='Map route rename';await page.locator('#map-route-name').fill('Renamed owned walk');await page.locator('#map-route-save').click();
    await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.startsWith('Private to its owner.')&&document.getElementById('map-route-name').value==='Renamed owned walk'&&!document.getElementById('map-route-save').disabled);
    stage='Map saved-route reopen';await page.locator('#map-route-clear').click();
    await page.locator('#map-route-id').fill(routeId);await page.locator('#map-route-open').click();
    await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.includes('Our World / owned-walk.')&&document.getElementById('map-route-name').value==='Renamed owned walk');
    await page.locator('#map-routes-refresh').click();await page.waitForFunction(()=>document.getElementById('map-saved-routes').textContent.includes('Renamed owned walk'));
    assert.equal(routeCreates,1);assert.equal(routeUpdates,1);
   });
   await checkPopup('Messaging','#messaging-modal-close-btn',async()=>{
   stage='Messaging send';await page.locator('#dash-card-messages').click();
   await page.waitForFunction(()=>document.getElementById('msg-conversations-list').textContent.includes('No conversations yet'));
   await page.locator('#messaging-modal-block [data-tab="send"]').click();
   await page.locator('#msg-to-avatar').fill(recipient.avatar.id);await page.locator('#msg-body').fill(messageText);
   await page.locator('#msg-send-btn').click();
   await page.waitForFunction(()=>document.getElementById('messaging-modal-status').textContent==='Message sent.');
   stage='Messaging recipient readback';
   const inbox=await recipient.call('recipient inbox','GET','/api/messaging/messages?limit=50&offset=0',undefined,recipientToken);
   assert.ok(Array.isArray(inbox));
   const delivered=inbox.filter(m=>m.content===messageText&&m.fromAvatarId===account.avatar.id&&m.toAvatarId===recipient.avatar.id);
   assert.equal(delivered.length,1,'Exactly one persisted recipient message');assert.ok(delivered[0].id);
   stage='Messaging conversation';await page.locator('#messaging-modal-block [data-tab="conversations"]').click();
   await page.locator('[data-msg-open="'+recipient.avatar.id+'"]').click();
   await page.waitForFunction(content=>document.getElementById('msg-thread-messages')?.textContent.includes(content),messageText);
   assert.equal(sentMessage,1);
   });
   if(errors.length)outcomes.forEach(outcome=>{outcome.passed=false;});
   console.log('PORTAL_STARTUP_POPUP_OUTCOMES:'+JSON.stringify(outcomes));
   assert.deepEqual(errors,[]);assert.ok(outcomes.every(outcome=>outcome.passed),'Every selected popup must pass');
   console.log('PORTAL_STARTUP_BROWSER_PROOF: selected real NFT/Data/message journeys passed through shipping WEB4 Startup/owned Mongo; independent failures are retained and still fail qualification. No external mint, paid Stripe, WEB6 or hosted claim.');
   return;
  }
  if(process.env.OASIS_STARTUP_POPUP_BREADTH==='true'){
   assert.equal(api5,null,'WEB4-only new-popup scenario');
   stage='Files popup';await page.locator('#dash-card-files').click();
   await page.waitForFunction(()=>document.getElementById('files-stat-count').textContent==='0'&&
    document.getElementById('files-list').textContent.includes('No files stored yet'));
   await page.locator('#files-close-btn').click();await page.waitForURL(url=>url.pathname==='/');
   stage='Wallet popup';await page.locator('#dash-card-wallets').click();
   await page.waitForFunction(()=>document.getElementById('wallet-stat-wallets').textContent==='0'&&
    !document.getElementById('wallet-grid-empty').hidden&&document.getElementById('wallet-modal-status').hidden);
   await page.locator('#wallet-modal-close-btn').click();await page.waitForURL(url=>url.pathname==='/');
   stage='Karma popup';await page.locator('#dash-card-karma').click();
   await page.waitForFunction(()=>document.getElementById('karma-records-list').textContent.includes('No karma records found')&&
    document.querySelectorAll('#karma-stats-grid .karma-stat-card').length===4&&
    document.getElementById('karma-modal-status').hidden);
   await page.locator('#karma-modal-block [data-tab="stats"]').click();
   await page.locator('#karma-tab-stats').waitFor({state:'visible'});
   await page.locator('#karma-modal-close-btn').click();await page.waitForURL(url=>url.pathname==='/');
   assert.deepEqual(errors,[]);
   assert.ok(reads.some(read=>read.path.includes('/api/wallet/avatar/')&&read.isError===false));
   assert.ok(reads.some(read=>read.path.includes('/api/files/')&&read.isError===false));
   assert.ok(reads.some(read=>read.path.includes('/api/karma/')&&read.isError===false));
   console.log('PORTAL_STARTUP_BROWSER_PROOF: actual page Files/Wallet/Karma reads, empty lists, Karma stats tab and normal closes through shipping WEB4 Startup/native owned Mongo. Prior subscription/Games/OAPP cohorts not selected; writes, external chain execution, paid Stripe, WEB6 and hosted excluded.');
   return;
  }
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
