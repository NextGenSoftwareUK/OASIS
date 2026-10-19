// Every page reads the same finalized block. Concurrent map swap-removals therefore
// cannot skip or duplicate keys across pages. A missing/pruned snapshot is an error.
export async function collectEntries(provider,contractId,prefix='') {
  if(typeof prefix!=='string')throw new Error('Record prefix must be a string');
  const block=await provider.viewBlock({finality:'final'});
  const hash=block?.header?.hash;
  if(typeof hash!=='string'||!hash)throw new Error('Finalized block hash is required for record pagination');
  const result=[],seen=new Set();let offset=0,total=null,bytes=0;
  for(let pageNumber=0;pageNumber<6251;pageNumber++) {
    const page=await provider.callFunction({contractId,method:'entries_page',args:{prefix,offset,limit:16},blockQuery:{blockId:hash}});
    if(!page||!Number.isSafeInteger(page.total)||page.total<0||page.total>100000||
       (total!==null&&page.total!==total)||!Array.isArray(page.rows))throw new Error('Invalid or inconsistent record page');
    total=page.total;
    const scanned=Math.min(16,total-offset);
    const next=offset+scanned<total?offset+scanned:null;
    if(scanned<0||page.next_offset!==next||page.rows.length>scanned)throw new Error('Invalid record page continuation');
    for(const row of page.rows){
      if(!Array.isArray(row)||row.length!==2||typeof row[0]!=='string'||typeof row[1]!=='string'||
         !row[0].startsWith(prefix)||seen.has(row[0]))throw new Error('Invalid, duplicate or out-of-scope record');
      bytes+=Buffer.byteLength(row[0])+Buffer.byteLength(row[1]);
      if(bytes>64*1024*1024)throw new Error('Record enumeration exceeds64MiB safety bound');
      seen.add(row[0]);result.push(row);
    }
    if(next===null)return result;
    offset=next;
  }
  throw new Error('Record enumeration exceeded page bound');
}
