'use strict';
const assert=require('node:assert/strict'),path=require('node:path'),zlib=require('node:zlib');
const {randomUUID,createHash}=require('node:crypto');
function routePixels(png){
 assert.equal(png.subarray(0,8).toString('hex'),'89504e470d0a1a0a');
 let width,height,channels;const chunks=[];
 for(let at=8;at<png.length;){const length=png.readUInt32BE(at),type=png.toString('ascii',at+4,at+8),data=png.subarray(at+8,at+8+length);
  if(type==='IHDR'){width=data.readUInt32BE(0);height=data.readUInt32BE(4);assert.equal(data[8],8);assert.equal(data[12],0);channels=data[9]===2?3:data[9]===6?4:0;assert.ok(channels);}
  if(type==='IDAT')chunks.push(data);at+=length+12;
 }
 assert.ok(width>200&&height>100&&width*height<4000000);
 const raw=zlib.inflateSync(Buffer.concat(chunks)),stride=width*channels;assert.equal(raw.length,(stride+1)*height);
 let previous=Buffer.alloc(stride),count=0,offset=0;
 for(let y=0;y<height;y++){const filter=raw[offset++],row=Buffer.alloc(stride);assert.ok(filter<=4);
  for(let x=0;x<stride;x++){const a=x>=channels?row[x-channels]:0,b=previous[x],c=x>=channels?previous[x-channels]:0;let predict=0;
   if(filter===1)predict=a;if(filter===2)predict=b;if(filter===3)predict=Math.floor((a+b)/2);
   if(filter===4){const p=a+b-c,pa=Math.abs(p-a),pb=Math.abs(p-b),pc=Math.abs(p-c);predict=pa<=pb&&pa<=pc?a:pb<=pc?b:c;}
   row[x]=(raw[offset++]+predict)&255;
  }
  for(let x=0;x<stride;x+=channels)if(row[x]<=30&&row[x+1]>=150&&row[x+1]<=205&&row[x+2]>=190&&row[x+2]<=235)count++;
  previous=row;
 }
 return count;
}
async function verify({baseUrl,token,avatarId,providerType}){
 const base=new URL(baseUrl);assert.equal(base.hostname,'127.0.0.1');assert.equal(base.protocol,'http:');assert.equal(providerType,'MongoDBOASIS');
 const {chromium}=require(path.join(process.env.OASIS_PROFILE_BROWSER_RUNTIME,'node_modules/playwright-core'));
 const context=await chromium.launchPersistentContext(path.join(process.env.OASIS_PROFILE_SDK_HANDOFF_ROOT,'browser-profile-'+randomUUID()),{
  channel:'chrome',headless:true,serviceWorkers:'block',args:['--use-angle=swiftshader','--enable-unsafe-swiftshader']});
 const errors=[],unexpected=[],mutations=[];let page,stage='launch';
 try{
  await context.route('**/*',route=>{const request=route.request(),url=new URL(request.url());
   if(url.origin===base.origin){if(url.pathname.startsWith('/api/')){
    if(request.method()!=='GET'){mutations.push(url.pathname);return route.abort();}
    if(url.pathname!=='/api/map/stats'){unexpected.push(url.pathname);return route.abort();}
   }return route.continue();}
   if(url.origin==='https://unpkg.com'&&url.pathname.startsWith('/maplibre-gl@6.19.0/'))return route.continue();
   if(['data:','blob:'].includes(url.protocol))return route.continue();
   unexpected.push(url.origin+url.pathname);return route.abort();
  });
  await context.addInitScript(({token,avatarId})=>{localStorage.setItem('avatar',JSON.stringify({id:avatarId}));sessionStorage.setItem('ownedMapBearer',token);},{token,avatarId});
  page=await context.newPage();page.setDefaultTimeout(20000);page.on('pageerror',error=>errors.push(error.message));
  await page.goto(baseUrl+'/__map_browser?renderer=1');await page.waitForFunction(()=>window.__mapReady===true);
  await page.locator('#fixture-open-map').click();await page.locator('.map-tab[data-tab="routes"]').click();
  await page.locator('#map-route-file').setInputFiles({name:'renderer-walk.gpx',mimeType:'application/gpx+xml',buffer:Buffer.from(
   '<gpx xmlns="http://www.topografix.com/GPX/1/1" version="1.1"><trk><name>Renderer walk</name><trkseg><trkpt lat="0" lon="-1"/><trkpt lat="0.1" lon="0"/></trkseg><trkseg><trkpt lat="0.1" lon="0.1"/><trkpt lat="0.2" lon="1"/></trkseg></trk></gpx>')});
  await page.waitForFunction(()=>document.getElementById('map-route-status').textContent.includes('4 points in 2 connected segments'));
  stage='actual MapLibre module/worker/WebGL initialization';await page.locator('#map-show-interactive').click();
  await page.waitForFunction(()=>document.getElementById('map-renderer-status').textContent.startsWith('Blue: nearby.'));
  await page.locator('#map-fit-loaded').click();const canvas=page.locator('#map-interactive canvas');assert.equal(await canvas.isVisible(),true);
  assert.equal(await canvas.evaluate(node=>{const gl=node.getContext('webgl2')||node.getContext('webgl');return !!gl&&!gl.isContextLost();}),true);
  stage='visible native route pixels';let first,cyan=0;
  for(let attempt=0;attempt<50;attempt++){first=await canvas.screenshot();cyan=routePixels(first);if(cyan>20)break;await page.waitForTimeout(100);}
  assert.ok(cyan>20,'Actual WebGL canvas must draw the cyan route geometry.');
  stage='actual navigation redraw';const before=createHash('sha256').update(first).digest('hex');await page.locator('#map-interactive .maplibregl-ctrl-zoom-in').click();
  let changed=false;for(let attempt=0;attempt<30;attempt++){const next=await canvas.screenshot();if(createHash('sha256').update(next).digest('hex')!==before&&routePixels(next)>20){changed=true;break;}await page.waitForTimeout(100);}
  assert.equal(changed,true,'Navigation must redraw route pixels.');
  stage='close disposal';await page.locator('#map-close-btn').click();assert.equal(await page.locator('#map-interactive canvas').count(),0);
  assert.deepEqual(errors,[]);assert.deepEqual(unexpected,[]);assert.deepEqual(mutations,[]);
  return {mapRendererVerified:true,actualWebGl:true,routePixels:cyan,navigationRedraw:true,zeroApiMutations:true,externalBasemapRequests:0,screenshotFilesWritten:0};
 }catch(error){const state=page&&!page.isClosed()?await page.evaluate(()=>({status:document.getElementById('map-renderer-status')?.textContent})):null;
  console.error(JSON.stringify({rendererStage:stage,state,errors,unexpected,mutations}));throw error;
 }finally{await context.close();}
}
module.exports={verify};
