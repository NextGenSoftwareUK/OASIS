export interface RecordMap { get(key: string): string | null; set(key: string, value: string): unknown; }
export interface CheckedRecordWrite {
  key: string;
  expected_value: string | null;
  value: string;
  archive_key?: string;
  archive_value?: string;
}

// Called inside one native receipt by the owner-authorized contract method.
// Validate every condition BEFORE either mutation; no retry/merge/upsert alternative.
export function writeCheckedRecord(records: RecordMap, input: CheckedRecordWrite): void {
  if (!input || typeof input.key !== "string" ||
      !/^(avatar|avatar-detail|holon):[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/.test(input.key) || input.key.endsWith(':00000000-0000-0000-0000-000000000000'))
    throw new Error("Invalid canonical record key");
  if (input.expected_value !== null && (typeof input.expected_value !== "string" || input.expected_value.length === 0))
    throw new Error("Explicit original ciphertext or null is required");
  if (typeof input.value !== "string" || input.value.length === 0) throw new Error("Replacement ciphertext is required");
  const archive = input.archive_key !== undefined || input.archive_value !== undefined;
  if (archive) {
    if (input.expected_value === null || typeof input.archive_key !== "string" ||
        !input.archive_key.startsWith(`history:${input.key}:`) ||
        !/^[0-9]{10}$/.test(input.archive_key.slice(`history:${input.key}:`.length)) ||
        Number(input.archive_key.slice(`history:${input.key}:`.length)) < 1 ||
        Number(input.archive_key.slice(`history:${input.key}:`.length)) > 2147483647 ||
        typeof input.archive_value !== "string" || input.archive_value.length === 0)
      throw new Error("Archive must name a positive version of the same existing record");
  }
  if (records.get(input.key) !== input.expected_value) throw new Error("OASIS_RECORD_CONFLICT");
  if (archive && records.get(input.archive_key!) !== null) throw new Error("OASIS_ARCHIVE_CONFLICT");
  if (archive) records.set(input.archive_key!, input.archive_value!);
  records.set(input.key, input.value);
}
