// Public polkadot-js SCALE registry only. This is the versioned oasis-records
// ABI, not an inferred generic contract ABI or a custom runtime state_call.
const definitions = {
  OasisRecordCell: { value: 'Bytes', revision: 'u64' },
  OasisRecordExpected: { _enum: { Any: 'Null', Missing: 'Null', Revision: 'u64' } },
  OasisRecordError: { _enum: ['Unauthorized', 'InvalidKey', 'ValueTooLarge', 'Conflict', 'Exhausted', 'InvalidPage'] },
  OasisRecordPage: { generation: 'u64', keys: 'Vec<Bytes>', next: 'Option<u64>' },
  OasisRecordLangError: { _enum: { Unused: 0, CouldNotReadInput: 1 } }
};

const messages = Object.freeze({
  new: { selector: '00000001', args: '()', result: null },
  get: { selector: '00000002', args: '(Bytes)', result: 'Option<OasisRecordCell>' },
  put: { selector: '00000003', args: '(Bytes,Bytes,OasisRecordExpected,[u8;32])', result: 'u64' },
  delete: { selector: '00000004', args: '(Bytes,OasisRecordExpected,[u8;32])', result: 'Option<u64>' },
  keys: { selector: '00000005', args: '(Bytes,u64,u8)', result: 'OasisRecordPage' }
});

function message(name) {
  if (!Object.hasOwn(messages, name)) throw new Error('Unknown oasis-records message.');
  return messages[name];
}

export function registerRecordContractTypes(registry) {
  registry.register(definitions);
}

export function encodeRecordMessage(registry, name, args) {
  const spec = message(name);
  const expectedCount = { new: 0, get: 1, put: 4, delete: 3, keys: 3 }[name];
  if (!Array.isArray(args) || args.length !== expectedCount) throw new Error('Incorrect contract argument count.');
  const normalized = [...args];
  // Uint8Array passed directly to a Bytes codec can be interpreted as already
  // SCALE encoded. Hex strings specify the raw byte value unambiguously.
  const bytePositions = { new: [], get: [0], put: [0, 1, 3], delete: [0, 2], keys: [0] }[name];
  for (const index of bytePositions) {
    if (!(args[index] instanceof Uint8Array)) throw new Error('Contract binary arguments require Uint8Array.');
    normalized[index] = '0x' + Buffer.from(args[index]).toString('hex');
  }
  if (name !== 'new') {
    if (args[0].length > 1024 || (name !== 'keys' && args[0].length === 0)) throw new Error('Invalid contract key length.');
  }
  const validU64 = value => typeof value === 'bigint' && value >= 0n && value <= 18446744073709551615n;
  if (name === 'put' || name === 'delete') {
    const expected = args[name === 'put' ? 2 : 1];
    const revision = expected && typeof expected === 'object' && Object.keys(expected).length === 1
      && Object.hasOwn(expected, 'Revision') && validU64(expected.Revision);
    if (expected !== 'Any' && expected !== 'Missing' && !revision) throw new Error('Explicit valid contract expectation required.');
    if (args[name === 'put' ? 3 : 2].length !== 32) throw new Error('Operation tag must contain 32 bytes.');
    if (name === 'put' && args[1].length > 8192) throw new Error('Contract cell exceeds 8192 bytes.');
  }
  if (name === 'keys' && (!validU64(args[1]) || !Number.isInteger(args[2]) || args[2] < 1 || args[2] > 8)) {
    throw new Error('Invalid contract page bounds.');
  }
  const encoded = registry.createType(spec.args, normalized).toU8a();
  return '0x' + spec.selector + Buffer.from(encoded).toString('hex');
}

export function decodeRecordResult(registry, name, bytes) {
  const spec = message(name);
  if (!spec.result) throw new Error('Constructor outcomes require native instantiation evidence.');
  const input = typeof bytes === 'string' && /^0x(?:[0-9a-fA-F]{2})+$/.test(bytes)
    ? Buffer.from(bytes.slice(2), 'hex') : bytes;
  if (!(input instanceof Uint8Array) || input.length === 0) throw new Error('Contract output must be nonempty SCALE bytes.');
  const type = `Result<Result<${spec.result},OasisRecordError>,OasisRecordLangError>`;
  const decoded = registry.createType(type, input);
  // SCALE decoders may consume only a prefix; reject trailing or noncanonical
  // bytes rather than certifying an acknowledgement for a different ABI.
  if (!Buffer.from(decoded.toU8a()).equals(Buffer.from(input))) throw new Error('Noncanonical or trailing contract output.');
  if (decoded.isErr) throw new Error(`Contract language error: ${decoded.asErr.type}`);
  const application = decoded.asOk;
  if (application.isErr) {
    const error = new Error(`Contract rejected operation: ${application.asErr.type}`);
    error.contractError = application.asErr.type;
    throw error;
  }
  return application.asOk;
}
