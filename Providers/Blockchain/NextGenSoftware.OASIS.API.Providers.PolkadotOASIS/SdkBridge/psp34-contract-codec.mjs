// Published PSP34 ABI: https://github.com/w3f/PSPs/blob/master/PSPs/psp-34.md
// Pure SCALE encoding/decoding only; this module neither queries nor submits.
const definitions = {
  OasisPsp34Id: { _enum: { U8: 'u8', U16: 'u16', U32: 'u32', U64: 'u64', U128: 'u128', Bytes: 'Bytes' } },
  OasisPsp34Error: { _enum: { Custom: 'String', SelfApprove: 'Null', NotApproved: 'Null',
    TokenExists: 'Null', TokenNotExists: 'Null', SafeTransferCheckFailed: 'String' } },
  OasisPsp34LangError: { _enum: { CouldNotReadInput: 1 } }
};
const methods = Object.freeze({
  collectionId: ['ffa27a5f', [], 'OasisPsp34Id'],
  balanceOf: ['cde7e55f', ['account'], 'u32'],
  ownerOf: ['1168624d', ['id'], 'Option<AccountId32>'],
  allowance: ['4790f55a', ['account', 'account', 'optionalId'], 'bool'],
  approve: ['1932a8b0', ['account', 'optionalId', 'bool'], 'Result<(),OasisPsp34Error>'],
  transfer: ['3128d61b', ['account', 'id', 'bytes'], 'Result<(),OasisPsp34Error>'],
  totalSupply: ['628413fe', [], 'u128'],
  getAttribute: ['f19d48d1', ['id', 'bytes'], 'Option<Bytes>']
});
const types = { account: 'AccountId32', id: 'OasisPsp34Id', optionalId: 'Option<OasisPsp34Id>', bool: 'bool', bytes: 'Bytes' };
function specification(name) {
  if (!Object.hasOwn(methods, name)) throw new Error('Unknown PSP34 method; no inferred custom ABI.');
  return methods[name];
}
function binary(value) {
  if (!(value instanceof Uint8Array)) throw new Error('PSP34 binary input requires explicit Uint8Array.');
  return '0x' + Buffer.from(value).toString('hex');
}
function id(value) {
  if (!value || typeof value !== 'object' || Array.isArray(value) || Object.keys(value).length !== 1)
    throw new Error('PSP34 token identity requires exactly one explicit variant.');
  const [variant] = Object.keys(value);
  if (variant === 'Bytes') return { Bytes: binary(value.Bytes) };
  const bits = { U8: 8n, U16: 16n, U32: 32n, U64: 64n, U128: 128n }[variant];
  const number = value[variant];
  if (!bits || typeof number !== 'string' || !/^(0|[1-9][0-9]*)$/.test(number)
      || number.length > 39 || BigInt(number) >= 1n << bits)
    throw new Error('PSP34 numeric identity must be a canonical decimal string within its declared variant.');
  return { [variant]: number };
}
function normalize(kind, value) {
  if (kind === 'account') {
    if (typeof value !== 'string' || !/^0x[0-9a-fA-F]{64}$/.test(value))
      throw new Error('PSP34 ABI requires canonical32byte account identity, not an inferred SS58 network.');
    return value.toLowerCase();
  }
  if (kind === 'id') return id(value);
  if (kind === 'optionalId') return value === null ? null : id(value);
  if (kind === 'bytes') return binary(value);
  if (typeof value !== 'boolean') throw new Error('PSP34 approval requires an explicit boolean.');
  return value;
}
export function registerPsp34Types(registry) { registry.register(definitions); }
export function encodePsp34Message(registry, name, args) {
  const [selector, kinds] = specification(name);
  return '0x' + selector + Buffer.from(encodePsp34Arguments(registry, kinds, args)).toString('hex');
}
// Shared primitive argument encoder for the published ABI and reviewed OASIS
// extensions. Custom selectors are deliberately absent from the standard map.
export function encodePsp34Arguments(registry, kinds, args) {
  if (!Array.isArray(kinds) || kinds.some(kind => !Object.hasOwn(types, kind))) throw new Error('Unknown PSP34 argument type.');
  if (!Array.isArray(args) || args.length !== kinds.length) throw new Error('Incorrect PSP34 argument count.');
  registerPsp34Types(registry);
  // Encode each argument independently: a raw Bytes value must not be mistaken
  // for already SCALE-encoded input by the registry's overloaded constructor.
  const encoded = kinds.map((kind, index) => registry.createType(types[kind], normalize(kind, args[index])).toU8a());
  return Buffer.concat(encoded.map(value => Buffer.from(value)));
}
export function decodePsp34Result(registry, name, bytes) {
  const [, , type] = specification(name);
  if (!(bytes instanceof Uint8Array) || bytes.length === 0) throw new Error('Missing PSP34 return bytes.');
  registerPsp34Types(registry);
  const decoded = registry.createType(`Result<${type},OasisPsp34LangError>`, bytes);
  if (!Buffer.from(decoded.toU8a()).equals(Buffer.from(bytes))) throw new Error('Noncanonical or trailing PSP34 return bytes.');
  if (decoded.isErr) throw new Error(`PSP34 language failure: ${decoded.asErr.toString()}`);
  if (type.startsWith('Result<') && decoded.asOk.isErr)
    throw new Error(`PSP34 application failure: ${decoded.asOk.asErr.toString()}`);
  return decoded.asOk;
}
