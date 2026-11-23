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
 const popupGroup=process.env.OASIS_STARTUP_POPUP_GROUP||'none';
 assert.ok(['none','tools-followup','tools','web5-breadth','community','storage','hyperdrive','eggs','nft','data','data-crud','data-edit','files-write','map-route','messaging','messaging-read','chat','chat-directory','directory-message','clan','portal-breadth','chat-hyperdrive','data-messaging','nft-messaging-data'].includes(popupGroup));
 const selectedPopups=popupGroup==='tools-followup'?['Developer','GlobalSearch']:popupGroup==='tools'?['Designer','Developer']:popupGroup==='web5-breadth'?['HolonBrowser','Celestial','GlobalSearch']:popupGroup==='community'?['Activity','Social','Chain']:popupGroup==='portal-breadth'?['Chat','DirectoryMessage','Clan']:popupGroup==='chat-directory'?['Chat','DirectoryMessage']:popupGroup==='chat-hyperdrive'?['Chat','HyperDrive']:popupGroup==='nft-messaging-data'?['NFT','Data','Messaging']:
  popupGroup==='data-messaging'?['Data','Messaging']:
  popupGroup==='none'?[]:[{storage:'Storage',hyperdrive:'HyperDrive',eggs:'Eggs',nft:'NFT',data:'Data','data-crud':'DataWrite','data-edit':'DataWrite','files-write':'FilesWrite','map-route':'MapRoute',messaging:'Messaging','messaging-read':'MessagingRead',chat:'Chat','directory-message':'DirectoryMessage',clan:'Clan'}[popupGroup]];
 const messageText='Owned portal message '+process.env.GITHUB_RUN_ID+'-'+process.env.GITHUB_RUN_ATTEMPT;
 let storageWrites=0; const storageAppId=require('node:crypto').randomUUID();
 let routingSaves=0;
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
  async function admitFree(owned){
   const signed=await owned.call('free account sign-in','POST','/api/avatar/authenticate',{username:owned.username,password:owned.password},undefined,true);
   await owned.call('free subscription','POST','/api/subscription/checkout/session',{planId:'free',successUrl:origin+'/subscription?tab=mysubs',cancelUrl:origin+'/'},signed.jwtToken);
   const subs=await owned.call('free readback','GET','/api/subscription/subscriptions/me',undefined,signed.jwtToken);
   assert.ok(Array.isArray(subs)&&subs.some(s=>s.planId==='free'&&s.status==='active'),'Persisted free subscription');
   return signed.jwtToken;
  }
  if(api5&&!selectedPopups.length)await admitFree(account);
  if(selectedPopups.length){
   assert.equal(!!api5,popupGroup==='web5-breadth','Explicit matching backend selection');ownedToken=await admitFree(account);
   if(selectedPopups.includes('Messaging')||selectedPopups.includes('MessagingRead')||selectedPopups.includes('Chat')||selectedPopups.includes('DirectoryMessage')||selectedPopups.includes('Clan')||selectedPopups.includes('Social')||selectedPopups.includes('GlobalSearch')){
    recipient=await createOwnedAvatar(api.href);recipientToken=await admitFree(recipient);
   }
  }
  if(popupGroup==='community'){
   assert.equal(await recipient.call('owned social contact setup','POST','/api/messaging/send-message-to-avatar/'+account.avatar.id,messageText,recipientToken),true);
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
  browser=await chromium.launch({channel:'chrome',headless:true});
  context=await browser.newContext({serviceWorkers:'block',ignoreHTTPSErrors:true});
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
    if(popupGroup==='storage'&&['PUT','DELETE'].includes(request.method())&&url.pathname==='/api/settings/holon-storage-policy'){
     ++storageWrites;assert.ok(storageWrites<=4,'Bounded avatar/app policy writes');
     assert.equal(url.searchParams.get('applicationId'),storageWrites===2||storageWrites===3?storageAppId:null);
     assert.equal(request.method(),storageWrites<=2?'PUT':'DELETE');
     if(request.method()==='PUT')assert.deepEqual(request.postDataJSON(),{schemaVersion:1,mode:storageWrites===1?'Materialized':'Virtual',readCache:'None',maximumCacheBytes:0,maximumCacheAgeSeconds:0,materializeOnExplicitSave:storageWrites===1});
     ownedOperation=true;
    }
    if(popupGroup==='hyperdrive'&&request.method()==='PUT'&&url.pathname==='/api/settings/hyperdrive-settings'){
     assert.equal(++routingSaves,1,'One explicit owned routing preference save');
     const body=request.postDataJSON();for(const flag of ['autoFailoverEnabled','autoReplicationEnabled','autoLoadBalancingEnabled'])assert.equal(body[flag],false);
     for(const field of ['autoFailoverProviders','autoReplicationProviders','loadBalancingProviders'])assert.equal(body[field],null);
     ownedOperation=true;
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
  const page=await context.newPage();page.on('pageerror',()=>errors.push('page-error'));
  page.on('response',async response=>{
   const url=new URL(response.url());if(!url.pathname.startsWith('/api/'))return;
   const fact={path:url.pathname,status:response.status()};reads.push(fact);
   if(httpFacts.length<64)httpFacts.push(fact);
   try{const body=await response.json(),result=typeof body.isError==='boolean'?body:body.result;
    fact.isError=result?.isError;const code=result?.errorCode;
    if(popupGroup==='storage'&&url.pathname==='/api/settings/holon-storage-policy'){const value=result?.result;fact.storageShape=Object.fromEntries(['applicationId','avatarPolicy','applicationPolicy','effectivePolicy','memoryCacheSupported'].map(key=>[key,{present:Object.hasOwn(value||{},key),type:value?.[key]===null?'null':typeof value?.[key]}]));}
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
  stage='page and Beam In';await page.goto(origin+'/',{waitUntil:'domcontentloaded'});
  await page.locator('.js-modal-trigger[data-signin="login"]:visible').first().click();
  await page.locator('#login-email').fill(account.username);await page.locator('#login-password').fill(account.password);
  await page.locator('#login-submit').click();
  await page.waitForFunction(()=>localStorage.getItem('loggedIn')==='true');
  stage='dashboard';await page.locator('#dashboard').waitFor({state:'visible'});
  assert.equal(await page.locator('#dash-name').textContent(),account.username);
  if(selectedPopups.length){
   const outcomes=[];
   async function checkPopup(name,closeSelector,work){
    if(!selectedPopups.includes(name))return;
    let passed=false,closePassed=true,observedStage=name;
    try{await work();passed=true;observedStage=stage;}
    catch(error){observedStage=stage;if(name==='Storage'){const feedback=await page.locator('#hd-storage-feedback').textContent();console.error('PORTAL_STARTUP_STORAGE_FAILURE:'+JSON.stringify({stage,feedback:/^(WEB4 returned|The saved policy|Load this scope|Loading storage settings)/.test(feedback||'')?feedback:'Unclassified storage error'}));}if(name==='Clan'){
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
      await page.locator(closeSelector).click();await page.waitForURL(url=>url.pathname==='/');
     }
    }catch{closePassed=false;passed=false;if(observedStage===name)observedStage=name+' close';}
    outcomes.push({name,passed,stage:observedStage,closePassed});
   }
   await checkPopup('Designer','#holon-designer-modal-close-btn',async()=>{
    stage='Designer schema';await page.evaluate(()=>window.openHolonDesignerModal());
    await page.locator('#holon-designer-name').fill('OwnedSchema');await page.locator('#holon-designer-field-name').fill('RouteLabel');await page.locator('#holon-designer-field-type').selectOption('string');await page.locator('#holon-designer-add-field-btn').click();
    assert.equal(await page.locator('#holon-designer-fields-list .holon-designer-field-row').count(),5);
    assert.ok((await page.locator('#holon-designer-canvas').textContent()).includes('RouteLabel'));
    stage='Designer JSON export';const [download]=await Promise.all([page.waitForEvent('download'),page.locator('#holon-designer-export-btn').click()]);
    try{
     assert.equal(download.suggestedFilename(),'OwnedSchema-schema.json');const stream=await download.createReadStream(),chunks=[];for await(const chunk of stream)chunks.push(chunk);
     const schema=JSON.parse(Buffer.concat(chunks).toString('utf8'));assert.equal(schema.holonType,'OwnedSchema');assert.deepEqual(schema.fields.RouteLabel,{type:'string',required:false});assert.deepEqual(schema.fields.Id,{type:'Guid',required:true});assert.equal(Object.keys(schema.fields).length,5);
    }finally{await download.delete();}
    stage='Designer remove field';await page.locator('.holon-designer-field-row').filter({hasText:'RouteLabel'}).locator('button').click();assert.equal(await page.locator('#holon-designer-fields-list .holon-designer-field-row').count(),4);
   });
   await checkPopup('Developer','#dev-portal-close-btn',async()=>{
    stage='Developer resources';await page.evaluate(()=>window.openDevPortalModal());await page.locator('#dev-portal-modal-block .dev-portal-link').first().waitFor({state:'visible'});
    const links=await page.locator('#dev-portal-modal-block a[href]').evaluateAll(nodes=>nodes.map(node=>({href:node.href,target:node.target,rel:node.rel})));
    assert.ok(links.length>0);assert.ok(links.every(link=>new URL(link.href).origin===origin||['https:','mailto:'].includes(new URL(link.href).protocol)));assert.ok(links.filter(link=>link.target==='_blank').every(link=>link.rel.split(/\s+/).includes('noopener')));
   });
   await checkPopup('HolonBrowser','#holon-browser-modal-close-btn',async()=>{
    stage='Holon Browser read';await page.evaluate(()=>window.openHolonBrowserModal());
    await page.waitForFunction(()=>document.getElementById('hbrow-status').textContent==='No STARNET holons yet.');
    stage='Holon Browser filters';await page.locator('#hbrow-search').fill('no matching holons');await page.locator('#hbrow-sort-sel').selectOption('name-asc');
    assert.ok((await page.locator('#hbrow-list').textContent()).includes('No holons match.'));
   });
   await checkPopup('Celestial','#celestial-map-modal-close-btn',async()=>{
    stage='Celestial real lists';await page.evaluate(()=>window.openCelestialMapModal());
    await page.waitForFunction(()=>document.getElementById('celestial-map-status').textContent==='0 spaces · 0 bodies · returned memberships only');
   });
   await checkPopup('GlobalSearch','#global-search-modal-close-btn',async()=>{
    stage='Global avatar search';await page.evaluate(()=>window.openGlobalSearchModal());await page.locator('#global-search-modal-block [data-type="Avatar"]').click();
    const [response]=await Promise.all([page.waitForResponse(r=>new URL(r.url()).pathname==='/api/avatar/search-directory'),page.locator('#gs-input').fill(recipient.username)]);
    const raw=await response.json(),envelope=typeof raw.isError==='boolean'?raw:raw.result;assert.equal(envelope.isError,false);const records=envelope.result;assert.ok(Array.isArray(records));assert.equal(records.length,1);assert.equal(records[0].id,recipient.avatar.id);
    await page.waitForFunction(()=>document.getElementById('gs-status-bar').textContent==='1 result');
    assert.ok((await page.locator('#gs-results').textContent()).includes(records[0].name||records[0].username||records[0].id));
    stage='Global clear';await page.locator('#gs-input').fill('');await page.waitForFunction(()=>document.getElementById('gs-results').textContent==='Start typing to search across the OASIS…');
   });
   await checkPopup('Activity','#activity-feed-modal-close-btn',async()=>{
    stage='Activity read';const [response]=await Promise.all([page.waitForResponse(r=>new URL(r.url()).pathname==='/api/karma/activity'),page.evaluate(()=>window.openActivityFeedModal())]);
    const reply=await response.json();assert.equal(reply.isError,false);assert.ok(Array.isArray(reply.result));assert.equal(reply.result.length,0);
    await page.waitForFunction(()=>document.getElementById('activity-feed-status').textContent==='No activity yet.');
    assert.equal(await page.locator('#activity-feed-live-btn').textContent(),'Start live updates');
    stage='Activity filter';await page.locator('#activity-feed-search').fill('no matching events');assert.equal(await page.locator('#activity-feed-list .afeed-row').count(),0);
   });
   await checkPopup('Social','#social-graph-modal-close-btn',async()=>{
    stage='Social contacts';await page.evaluate(()=>window.openSocialGraphModal());
    await page.waitForFunction(()=>document.getElementById('social-graph-status').textContent==='1 messaging contacts · 1 recent messages checked');
    assert.equal(await page.locator('#social-graph-svg circle').count(),2);assert.equal(await page.locator('#social-graph-svg line').count(),1);
    assert.equal(await page.locator('#social-graph-more-btn').isHidden(),true);
    stage='Social compose';await page.locator('#social-graph-svg [data-contact-id="'+recipient.avatar.id+'"]').press('Enter');
    await page.locator('#msg-to-avatar').waitFor({state:'visible'});assert.equal(await page.locator('#msg-to-avatar').inputValue(),recipient.avatar.id);
    assert.equal(reads.filter(r=>r.path.startsWith('/api/messaging/send-message-to-avatar/')).length,0,'Opening a contact does not send');
    stage='Social return';await page.locator('#messaging-modal-close-btn').click();await page.locator('#social-graph-modal-close-btn').waitFor({state:'visible'});
   });
   await checkPopup('Chain','#chain-explorer-modal-close-btn',async()=>{
    stage='Chain recorded activity';await page.evaluate(()=>window.openChainExplorerModal());
    await page.waitForFunction(()=>document.getElementById('chain-explorer-status').textContent==='No provider activity yet.');
    assert.ok((await page.locator('#chain-explorer-tbody').textContent()).includes('No matching recorded activity.'));
    stage='Chain filter';await page.locator('#chain-explorer-filter').fill('no match');assert.equal(await page.locator('#chain-explorer-provider-select option').count(),1);
    assert.equal(await page.locator('#chain-explorer-prev-btn').isDisabled(),true);assert.equal(await page.locator('#chain-explorer-next-btn').isDisabled(),true);
   });
   await checkPopup('Eggs','#eggs-modal-close-btn',async()=>{
    const tabs=[['my','my-eggs','my-grid','No eggs returned.'],['all','get-all-eggs','all-grid','No eggs returned.'],['quests','get-current-egg-quests','quests-list','No current egg quests returned.'],['leaderboard','get-current-egg-quest-leader-board','leaderboard-list','No egg quest leaderboard entries returned.']];
    for(const [tab,path,target,empty] of tabs){
     stage='Eggs '+tab;
     const [response]=await Promise.all([page.waitForResponse(r=>new URL(r.url()).pathname==='/api/eggs/'+path&&r.request().method()==='GET'),
      tab==='my'?page.evaluate(()=>window.openEggsModal()):page.locator('#eggs-modal-block [data-tab="'+tab+'"]').click()]);
     assert.equal(response.status(),200);const reply=await response.json();assert.equal(reply.isError,false);assert.ok(Array.isArray(reply.result));assert.equal(reply.result.length,0);
     await page.waitForFunction(({target,empty})=>document.getElementById('eggs-'+target)?.textContent.includes(empty),{target,empty});
     assert.equal(await page.locator('#eggs-tab-'+tab).isVisible(),true);assert.equal(await page.locator('#eggs-status').isHidden(),true);
    }
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
   await checkPopup('Storage','#hd-close-btn',async()=>{
    stage='Storage load avatar';await page.evaluate(()=>window.openHyperdriveModal('storage'));
    await page.waitForFunction(()=>!document.getElementById('hd-storage-save').disabled);
    const field=name=>page.locator('#hd-storage-form [name="'+name+'"]');
    const savedText='Storage settings saved. Existing records have not been moved or hidden.';
    async function savePolicy(mode,applicationId){
     await field('mode').selectOption(mode);await page.locator('#hd-storage-save').click();
     await page.waitForFunction(expected=>document.getElementById('hd-storage-feedback').textContent===expected,savedText);
     return account.call('owned storage readback','GET','/api/settings/holon-storage-policy'+(applicationId?'?applicationId='+applicationId:''),undefined,ownedToken);
    }
    stage='Storage save avatar';let snapshot=await savePolicy('Materialized');
    assert.equal(snapshot.avatarPolicy.mode,'Materialized');assert.equal(snapshot.effectivePolicy.mode,'Materialized');
    stage='Storage application inheritance';await field('scope').selectOption('application');await field('applicationId').fill(storageAppId);await page.locator('#hd-storage-load').click();
    await page.waitForFunction(()=>!document.getElementById('hd-storage-save').disabled&&document.getElementById('hd-storage-effective').textContent.includes('inherited'));
    stage='Storage save application';snapshot=await savePolicy('Virtual',storageAppId);
    assert.equal(snapshot.applicationId,storageAppId);assert.equal(snapshot.avatarPolicy.mode,'Materialized');assert.equal(snapshot.applicationPolicy.mode,'Virtual');assert.equal(snapshot.effectivePolicy.mode,'Virtual');
    stage='Storage clear application';snapshot=await savePolicy('',storageAppId);
    assert.equal(snapshot.applicationPolicy??null,null);assert.equal(snapshot.effectivePolicy.mode,'Materialized');
    stage='Storage reload avatar';await field('scope').selectOption('avatar');await page.locator('#hd-storage-load').click();
    await page.waitForFunction(()=>!document.getElementById('hd-storage-save').disabled&&document.querySelector('#hd-storage-form [name="mode"]').value==='Materialized');
    stage='Storage clear avatar';snapshot=await savePolicy('');assert.equal(snapshot.avatarPolicy??null,null);assert.equal(snapshot.effectivePolicy??null,null);assert.equal(storageWrites,4);
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
    if(popupGroup==='hyperdrive'){
     stage='HyperDrive save preferences';
     for(const flag of ['autoFailoverEnabled','autoReplicationEnabled','autoLoadBalancingEnabled'])await page.locator('#hd-settings-form [name="'+flag+'"]').selectOption('false');
     for(const field of ['autoFailoverProviders','autoReplicationProviders','loadBalancingProviders'])await page.locator('#hd-settings-form [name="'+field+'"]').fill('');
     await page.locator('#hd-settings-form button[type="submit"]').click();
     await page.waitForFunction(()=>document.getElementById('hd-settings-feedback').textContent==='Routing preferences saved.');
     const persisted=await account.call('owned routing readback','GET','/api/settings/hyperdrive-settings',undefined,ownedToken);
     for(const flag of ['autoFailoverEnabled','autoReplicationEnabled','autoLoadBalancingEnabled']){assert.equal(persisted.avatarOverrides[flag],false);assert.equal(persisted.effective[flag],false);}
     assert.equal(routingSaves,1);
     stage='HyperDrive deliveries';await page.locator('#hd-modal-block [data-tab="deliveries"]').click();
     await page.waitForFunction(()=>document.getElementById('hd-deliveries-status').textContent==='No hosted deliveries yet.');
    }

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
