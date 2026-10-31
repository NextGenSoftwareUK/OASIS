// Shared strict ink language/application result boundary. The caller registers
// its reviewed ABI types; transport success alone never means contract success.
export function decodeInkScaleResult(registry, type, languageErrorType, bytes, label) {
  if (!(bytes instanceof Uint8Array) || bytes.length === 0) throw new Error(`Missing ${label} return bytes.`);
  const decoded = registry.createType(`Result<${type},${languageErrorType}>`, bytes);
  if (!Buffer.from(decoded.toU8a()).equals(Buffer.from(bytes))) throw new Error(`Noncanonical or trailing ${label} return bytes.`);
  if (decoded.isErr) throw new Error(`${label} language failure: ${decoded.asErr.toString()}`);
  if (type.startsWith('Result<') && decoded.asOk.isErr)
    throw new Error(`${label} application failure: ${decoded.asOk.asErr.toString()}`);
  return decoded.asOk;
}
