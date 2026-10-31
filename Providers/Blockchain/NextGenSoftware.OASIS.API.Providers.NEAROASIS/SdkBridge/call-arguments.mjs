// Preserve the C# JSON bytes through the SDK's Uint8Array argument path.
// Parsing validates syntax only; never stringify the parsed JavaScript numbers.
export function decodeCallArguments(encoded){
  if(typeof encoded!=='string'||encoded.length===0)throw new Error('Canonical base64 JSON arguments are required');
  const bytes=Buffer.from(encoded,'base64');
  if(bytes.toString('base64')!==encoded)throw new Error('Noncanonical base64 call arguments');
  const json=new TextDecoder('utf-8',{fatal:true}).decode(bytes);
  JSON.parse(json);
  return bytes;
}
