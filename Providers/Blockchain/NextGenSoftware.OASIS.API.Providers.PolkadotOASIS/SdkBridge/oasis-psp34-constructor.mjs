// Explicit oasis-psp34 v0.1.0 constructor, not an inferred PSP34 standard ABI.
// Constructor ownership comes from the native caller; no owner override exists.
const fields = ['name', 'symbol', 'description', 'image'];
export function encodeOasisPsp34Constructor(registry, metadata) {
  if (!metadata || typeof metadata !== 'object' || Array.isArray(metadata)
      || Object.keys(metadata).length !== fields.length
      || Object.keys(metadata).some(key => !fields.includes(key)))
    throw new Error('Explicit name/symbol/description/image byte fields required.');
  const values = fields.map(key => {
    const value = metadata[key];
    if (!(value instanceof Uint8Array) || value.length > 8192)
      throw new Error('OASIS PSP34 constructor metadata requires at most8192 bytes per field.');
    return '0x' + Buffer.from(value).toString('hex');
  });
  const encoded = registry.createType('(Bytes,Bytes,Bytes,Bytes)', values).toU8a();
  return '0x00000001' + Buffer.from(encoded).toString('hex');
}
