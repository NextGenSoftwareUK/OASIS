/* Real Linux CI browser against frozen WEB4 assets and one bounded public-provider read.
 * Never runs on the user's Windows desktop. No database fixture, model or paid request. */
'use strict';
const fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const {spawn,execFileSync}=require('node:child_process');
const {isDeepStrictEqual}=require('node:util');
const origin='https://web4.oasisomniverse.one',api='https://dev.api.web4.oasisomniverse.one';
const provider='HoloOASIS';
const assetNames=['web4-sandbox.html','web4-session.js','web4-tabs.js','cql-demo-preparation.js','cql-text.js','lossless-metadata.js','cql-playground.js','cql-ask.js'];
const guid=/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i;
function validateRead(query){
  const body=query?.body,allowed=new Set(['provider','filters','orderBy','skip','take','includeMetadata','maximumDurationMilliseconds']);
  if(query?.path!=='data/query-cql'||body?.provider!==provider||Object.keys(body).some(k=>!allowed.has(k))||
      body.orderBy!=='None'||body.skip!==0||body.includeMetadata!==true||
      body.maximumDurationMilliseconds!==undefined&&(!Number.isInteger(body.maximumDurationMilliseconds)||body.maximumDurationMilliseconds<1||body.maximumDurationMilliseconds>30000)||!Number.isInteger(body.take)||body.take<1||body.take>10||
      !Array.isArray(body.filters)||body.filters.length!==1)throw Error('Public reader scope changed');
  const filter=body.filters[0];
  const ids=filter.operator==='Equal'?[filter.value]:filter.operator==='In'?filter.value:null;
  if(filter.field!=='Id'||filter.scalar!=='Guid'||Object.keys(filter).some(k=>!['field','scalar','operator','value'].includes(k))||
      !Array.isArray(ids)||ids.length<1||ids.length>5||ids.length>body.take||ids.some(id=>typeof id!=='string'||!guid.test(id))||
      new Set(ids.map(id=>id.toLowerCase())).size!==ids.length)throw Error('Public example requires bounded unique GUID identities');
  return ids.map(id=>id.toLowerCase());
}
function capturesResponse(method,pathname){
  return method==='POST'&&['/api/sandbox/session','/api/data/query-cql','/api/avatar/revoke-token'].includes(pathname);
}
function responseEnvelope(pathname,payload,client){
  if(pathname==='/api/sandbox/session')return payload;
  if(pathname==='/api/avatar/revoke-token')return payload?.result;
  if(pathname==='/api/data/query-cql')return client.unwrap(payload);
  throw Error('Unadmitted response route');
}
async function run(){
  if(process.platform!=='linux'||process.env.GITHUB_ACTIONS!=='true'||typeof WebSocket!=='function')
    throw Error('This qualification requires the existing Linux CI browser runtime');
  const site=fs.realpathSync(process.env.CQL_BROWSER_SITE_ROOT);
  const website=process.env.CQL_BROWSER_WEBSITE_SOURCE,backend=process.env.CQL_BROWSER_API_SOURCE;
  if(!/^[0-9a-f]{40}$/.test(website||'')||!/^[0-9a-f]{40}$/.test(backend||''))throw Error('Exact source revisions required');
  if(execFileSync('git',['rev-parse','HEAD'],{cwd:site,encoding:'utf8'}).trim()!==website)throw Error('Website checkout differs');
  execFileSync('git',['diff','--exit-code'],{cwd:site,stdio:'pipe'});
  const temporary=fs.realpathSync(process.env.RUNNER_TEMP),work=path.join(temporary,'cosmic-cql-browser-current');
  const reportDir=path.resolve(process.env.CQL_BROWSER_REPORT_DIRECTORY);
  if(fs.existsSync(work))throw Error('Preserve existing browser intent; do not replay');
  fs.mkdirSync(work);fs.mkdirSync(reportDir,{recursive:true});
  if(fs.realpathSync(work)!==work||path.dirname(work)!==temporary)throw Error('Owned browser path changed');
  const profile=path.join(work,'profile'),assets=new Map();
  for(const name of assetNames){
    const filename=path.join(site,name);
    if(fs.realpathSync(filename)!==filename)throw Error('Asset source resolves outside its exact file');
    const body=fs.readFileSync(filename);if(body.length>2097152)throw Error('Asset size exceeds bound');
    assets.set('/'+name,{body,sha256:crypto.createHash('sha256').update(body).digest('hex')});
  }
  const live=process.env.CQL_BROWSER_LIVE_FLOW==='1';
  const client=require(path.join(site,'cql-playground.js'));
  const report={websiteSource:website,declaredApiSource:backend,apiSourceVerifiedInsideRunner:false,
    approvedOrigin:origin,provider,liveRequested:live,publicWebsitePublicationVerified:false,modelCalls:0,applicationHolonWrites:0,
    apiRequests:{},blockedApi:[],scriptExceptions:[],assetHashes:{},populatedReadVerified:false,fullBrowserDataFlowVerified:false};
  const executable=fs.realpathSync(process.env.CQL_BROWSER_CHROME);
  const env={};for(const key of ['PATH','HOME','TMPDIR','LANG','DISPLAY','XDG_RUNTIME_DIR','CHROME_DEVEL_SANDBOX'])if(process.env[key])env[key]=process.env[key];
  const browser=spawn(executable,['--headless=new','--disable-gpu','--renderer-process-limit=2','--no-first-run',
    '--no-default-browser-check','--disable-background-networking','--disable-component-update','--disable-sync',
    '--disable-extensions','--remote-debugging-address=127.0.0.1','--remote-debugging-port=0','--user-data-dir='+profile,'about:blank'],
    {env,stdio:['ignore','ignore','pipe']});
  fs.writeFileSync(path.join(work,'owner.json'),JSON.stringify({owner:'cosmic-cql-linux-ci',pid:browser.pid,work,profile}));
  let socket,session,sequence=0,eventFailure=null,reviewed,expectedIds,readResult,querySentAt=0,lastApi=0;
  const pending=new Map(),ready=new Map(),waiters=new Map(),responses=new Map(),loadedReads=[],networkMethods=new Map();
  const sleep=ms=>new Promise(resolve=>setTimeout(resolve,ms));
  function send(method,params={},sessionId){
    return new Promise((resolve,reject)=>{
      const id=++sequence,timer=setTimeout(()=>{pending.delete(id);reject(Error('CDP deadline '+method));},45000);
      pending.set(id,{resolve,reject,timer,method});socket.send(JSON.stringify({id,method,params,...(sessionId?{sessionId}:{})}));
    });
  }
  async function evaluate(expression){
    const value=await send('Runtime.evaluate',{expression,awaitPromise:true,returnByValue:true},session);
    if(value.exceptionDetails)throw Error('Browser scenario evaluation failed');
    return value.result.value;
  }
  async function waitFor(check,label,timeout=35000,ignoreScenarioFailure=false){
    const end=Date.now()+timeout;
    while(Date.now()<end){if(eventFailure&&!ignoreScenarioFailure)throw Error(eventFailure);if(await check())return;await sleep(200);}
    throw Error(label+' deadline');
  }
  const limits={'POST /api/sandbox/session':1,'GET /api/data/query-cql/providers':2,
    'POST /api/data/query-cql':1,'POST /api/avatar/revoke-token':1};
  let pacing=Promise.resolve();
  async function event(message){
    const p=message.params||{},sid=message.sessionId;
    if(message.method==='Target.attachedToTarget'){
      if(p.targetInfo.type!=='page'){await send('Target.closeTarget',{targetId:p.targetInfo.targetId});return;}
      await send('Fetch.enable',{patterns:[{urlPattern:'*',requestStage:'Request'}]},p.sessionId);
      await send('Network.enable',{},p.sessionId);await send('Runtime.enable',{},p.sessionId);await send('Page.enable',{},p.sessionId);
      await send('Emulation.setDeviceMetricsOverride',{width:1280,height:900,deviceScaleFactor:1,mobile:false},p.sessionId);
      ready.set(p.targetInfo.targetId,p.sessionId);
      if(waiters.has(p.targetInfo.targetId)){waiters.get(p.targetInfo.targetId)(p.sessionId);waiters.delete(p.targetInfo.targetId);}
      await send('Runtime.runIfWaitingForDebugger',{},p.sessionId);
    }else if(message.method==='Fetch.requestPaused'){
      const address=new URL(p.request.url),key=p.request.method+' '+address.pathname;
      if(address.origin===origin&&p.request.method==='GET'&&assets.has(address.pathname)){
        const asset=assets.get(address.pathname);report.assetHashes[address.pathname]=asset.sha256;
        await send('Fetch.fulfillRequest',{requestId:p.requestId,responseCode:200,
          responseHeaders:[{name:'content-type',value:address.pathname.endsWith('.html')?'text/html; charset=utf-8':'application/javascript; charset=utf-8'}],
          body:asset.body.toString('base64')},sid);return;
      }
      const preflight=p.request.method==='OPTIONS'&&Object.keys(limits).some(k=>k.endsWith(' '+address.pathname));
      if(live&&address.origin===api&&(limits[key]||preflight)){
        if(!preflight){
          const count=(report.apiRequests[key]||0)+1;if(count>limits[key])throw Error('API request cap reached');
          report.apiRequests[key]=count;
          if(address.pathname==='/api/data/query-cql'){
            const body=JSON.parse(p.request.postData||'null');
            if(!reviewed||!isDeepStrictEqual(body,reviewed.body))throw Error('Browser request differs from reviewed public example');
          }
        }
        pacing=pacing.then(async()=>{await sleep(Math.max(0,1000-(Date.now()-lastApi)));lastApi=Date.now();
          if(key==='POST /api/data/query-cql')querySentAt=performance.now();
          await send('Fetch.continueRequest',{requestId:p.requestId},sid);});
        await pacing;return;
      }
      if(address.origin===api)report.blockedApi.push(key);
      await send('Fetch.failRequest',{requestId:p.requestId,errorReason:'BlockedByClient'},sid);
    }else if(message.method==='Runtime.exceptionThrown'){
      report.scriptExceptions.push({line:p.exceptionDetails?.lineNumber,type:p.exceptionDetails?.exception?.className||'ScriptException'});
    }else if(message.method==='Network.requestWillBeSent'&&p.request.url.startsWith(api+'/api/')){
      networkMethods.set(p.requestId,p.request.method);
    }else if(message.method==='Network.responseReceived'&&p.response.url.startsWith(api+'/api/')){
      const pathname=new URL(p.response.url).pathname;
      if(capturesResponse(networkMethods.get(p.requestId),pathname))
        responses.set(p.requestId,{path:pathname,status:p.response.status});
    }else if(message.method==='Network.loadingFinished'&&responses.has(p.requestId)){
      const observation=responses.get(p.requestId);responses.delete(p.requestId);
      const wire=await send('Network.getResponseBody',{requestId:p.requestId},sid);
      const raw=JSON.parse(wire.base64Encoded?Buffer.from(wire.body,'base64').toString():wire.body);
      const payload=responseEnvelope(observation.path,raw,client);
      observation.acknowledged=observation.status===200&&payload?.isError===false;
      if(observation.path==='/api/data/query-cql'){
        const rows=payload?.result?.rows;
        if(!observation.acknowledged||payload.isLoaded!==true||payload.result.provider!==provider||!Array.isArray(rows)||
           rows.length!==expectedIds.length||!isDeepStrictEqual(rows.map(row=>row.id.toLowerCase()).sort(),expectedIds.slice().sort()))
          throw Error('Actual public provider response did not match exact reviewed identities');
        readResult=rows;report.queryElapsedMilliseconds=performance.now()-querySentAt;
        report.returnedRows=rows.length;report.providerReturned=payload.result.provider;
      }
      loadedReads.push(observation);
    }
  }
  try{
    const endpoint=await new Promise((resolve,reject)=>{
      let text='';const timer=setTimeout(()=>reject(Error('Owned Chrome startup deadline')),20000);
      browser.once('error',()=>{clearTimeout(timer);reject(Error('Owned Chrome launch failed'));});
      browser.once('exit',code=>{clearTimeout(timer);reject(Error('Owned Chrome exited before readiness '+code));});
      browser.stderr.on('data',chunk=>{text=(text+chunk.toString()).slice(-131072);
        const match=/DevTools listening on (ws:\/\/127\.0\.0\.1:\d+\/devtools\/browser\/[a-zA-Z0-9-]+)/.exec(text);
        if(match){clearTimeout(timer);resolve(match[1]);}});
    });
    socket=new WebSocket(endpoint);
    await new Promise((resolve,reject)=>{socket.addEventListener('open',resolve,{once:true});socket.addEventListener('error',()=>reject(Error('Browser websocket connection failed')),{once:true});});
    socket.addEventListener('message',message=>{
      const value=JSON.parse(message.data);
      if(value.id&&pending.has(value.id)){
        const item=pending.get(value.id);pending.delete(value.id);clearTimeout(item.timer);
        value.error?item.reject(Error('CDP '+item.method+' '+value.error.code+': '+String(value.error.message).replace(/[\r\n]/g,' ').slice(0,180))):item.resolve(value.result);
      }else event(value).catch(error=>{eventFailure=error.message;});
    });
    report.browserVersion=(await send('Browser.getVersion')).product;
    await send('Target.setAutoAttach',{autoAttach:true,waitForDebuggerOnStart:true,flatten:true});
    const context=await send('Target.createBrowserContext',{disposeOnDetach:true});
    const target=await send('Target.createTarget',{url:'about:blank',browserContextId:context.browserContextId});
    session=ready.get(target.targetId)||await new Promise((resolve,reject)=>{
      const timer=setTimeout(()=>reject(Error('Guarded page readiness deadline')),15000);
      waiters.set(target.targetId,id=>{clearTimeout(timer);resolve(id);});
    });
    await send('Page.navigate',{url:origin+'/web4-sandbox.html'},session);
    await waitFor(()=>evaluate("document.readyState==='complete'&&!!window.cqlPlayground&&!!window.cqlAsk"),'Real page initialization');
    if(Object.keys(report.assetHashes).length!==assetNames.length)throw Error('Not all exact source assets loaded');
    await evaluate("document.querySelector('button[data-tab=\"cql\"]').click();true");
    for(const name of ['derived','staged-set']){
      const loaded=await evaluate("(()=>{const b=document.getElementById('cql-load-"+name+"-example');b.click();const t=document.getElementById('cql-text');return t.value.trim()===document.getElementById('cql-"+name+"-example').textContent.trim()&&t.getClientRects().length>0;})()");
      if(!loaded)throw Error('Displayed advanced example did not populate the visible real editor');
    }
    report.advancedExampleControlsRendered=true;
    if(live){
      await evaluate("document.getElementById('demo-btn').click();true");
      await waitFor(()=>evaluate("!!session.identity()&&!document.getElementById('cql-run').disabled"),'Temporary session and discovery');
      const available=await evaluate("window.cqlPlayground.availableTextProviders().includes('HoloOASIS')");
      if(!available)throw Error('Shipping HoloOASIS unavailable; no replacement provider');
      await evaluate("(()=>{const s=document.getElementById('cql-provider');s.value='HoloOASIS';s.dispatchEvent(new Event('change'));document.getElementById('cql-load-text-example').click();return true;})()");
      reviewed=await evaluate("window.cqlPlayground.compileAvailableText(document.getElementById('cql-text').value)");
      report.reviewedScope={path:reviewed?.path,provider:reviewed?.body?.provider,keys:Object.keys(reviewed?.body||{}),
        take:reviewed?.body?.take,maximumDurationMilliseconds:reviewed?.body?.maximumDurationMilliseconds};
      expectedIds=validateRead(reviewed);
      await evaluate("document.getElementById('cql-run-text').click();true");
      await waitFor(()=>Boolean(readResult),'Real native public query response');
      const expected=readResult.map(row=>[provider,String(row.name??''),String(row.id)]);
      await waitFor(()=>evaluate("(()=>{const rows=[...document.querySelectorAll('#cql-results table tbody tr')];return JSON.stringify(rows.map(r=>[r.cells[0].textContent,r.cells[1].textContent,r.cells[4].textContent]))==="+JSON.stringify(JSON.stringify(expected))+";})()"),'Rendered actual native rows');
      await evaluate("document.getElementById('cql-results').scrollIntoView({block:'center'});true");
      if(!await evaluate("(()=>{const r=document.getElementById('cql-results').getBoundingClientRect();return r.width>0&&r.height>0&&r.bottom>0&&r.top<innerHeight;})()"))throw Error('Populated table is not visible');
      report.populatedReadVerified=true;
    }
    if(report.scriptExceptions.length)throw Error('Page emitted an unhandled script exception');
    const screenshot=await send('Page.captureScreenshot',{format:'png',captureBeyondViewport:false},session);
    const bytes=Buffer.from(screenshot.data,'base64');if(bytes.length>2097152)throw Error('Screenshot exceeds bound');
    fs.writeFileSync(path.join(reportDir,'browser.png'),bytes);report.screenshotSha256=crypto.createHash('sha256').update(bytes).digest('hex');
  }catch(error){report.error=error.message;process.exitCode=1;}
  finally{
    if(live&&report.apiRequests['POST /api/sandbox/session']){
      try{
        if(!session||!socket||socket.readyState!==WebSocket.OPEN)throw Error('Session cleanup transport unavailable');
        await evaluate("(async()=>{await session.signOut();return !session.identity()})()");
        await waitFor(()=>loadedReads.some(x=>x.path==='/api/avatar/revoke-token'),'Session revocation acknowledgement',15000,true);
        report.demoSessionRevoked=loadedReads.some(x=>x.path==='/api/avatar/revoke-token'&&x.acknowledged);
        if(!report.demoSessionRevoked)throw Error('Session revocation not acknowledged');
      }catch(error){report.cleanupError=error.message;process.exitCode=1;}
    }
    if(socket?.readyState===WebSocket.OPEN){try{await send('Browser.close');}catch{report.browserCloseTransportEnded=true;}socket.close();}
    const exited=()=>browser.exitCode!==null||browser.signalCode!==null;
    for(let n=0;n<30&&!exited();n++)await sleep(100);
    if(!exited()){browser.kill('SIGTERM');for(let n=0;n<30&&!exited();n++)await sleep(100);}
    if(!exited()){browser.kill('SIGKILL');for(let n=0;n<30&&!exited();n++)await sleep(100);}
    report.ownedBrowserStopped=exited();
    if(report.ownedBrowserStopped){
      if(fs.existsSync(profile)){if(fs.realpathSync(profile)!==profile)throw Error('Owned profile path changed');fs.rmSync(profile,{recursive:true});}
      report.profileAbsent=!fs.existsSync(profile);
      if(report.profileAbsent){fs.unlinkSync(path.join(work,'owner.json'));fs.rmdirSync(work);}
    }
    for(const item of pending.values())clearTimeout(item.timer);
    report.fullBrowserDataFlowVerified=live&&report.populatedReadVerified&&report.demoSessionRevoked===true&&report.ownedBrowserStopped&&report.profileAbsent&&!report.error&&!report.cleanupError&&!eventFailure;
    if(eventFailure)report.eventFailure=eventFailure;
    if(!report.ownedBrowserStopped||!report.profileAbsent||live&&!report.fullBrowserDataFlowVerified)process.exitCode=1;
    report.apiResponses=loadedReads.map(({path,status,acknowledged})=>({path,status,acknowledged}));
    fs.writeFileSync(path.join(reportDir,'browser.json'),JSON.stringify(report,null,2)+'\n');
    console.log(JSON.stringify({passed:!process.exitCode,live:report.fullBrowserDataFlowVerified,provider:report.provider,rows:report.returnedRows,
      ownedBrowserStopped:report.ownedBrowserStopped,profileAbsent:report.profileAbsent,error:report.error||report.cleanupError||report.eventFailure}));
  }
}
module.exports={validateRead,capturesResponse,responseEnvelope};
if(require.main===module)run().catch(error=>{console.error('Browser qualification stopped: '+error.message);process.exitCode=1;});
