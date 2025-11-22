/**
 * Viewing key utilities for privacy and auditability
 */

export interface ViewingKeyData {
  id: string;
  address: string;
  keyHash: string; // Never store full key, only hash
  purpose: 'audit' | 'compliance' | 'personal';
  createdAt: string;
  lastUsed?: string;
}

/**
 * Validate viewing key hash format
 */
export function isValidViewingKeyHash(hash: string): boolean {
  // Viewing key hashes should be 64 character hex strings
  return /^[a-f0-9]{64}$/i.test(hash);
}

/**
 * Mask viewing key for display (never show full key)
 */
export function maskViewingKey(keyHash: string): string {
  if (!keyHash || keyHash.length < 8) {
    return '****';
  }
  return `${keyHash.substring(0, 4)}...${keyHash.substring(keyHash.length - 4)}`;
}

/**
 * Format viewing key purpose for display
 */
export function formatViewingKeyPurpose(purpose: 'audit' | 'compliance' | 'personal'): string {
  const purposeMap = {
    audit: 'Audit',
    compliance: 'Compliance',
    personal: 'Personal',
  };
  return purposeMap[purpose];
}

/**
 * Check if viewing key is expired (older than 1 year)
 */
export function isViewingKeyExpired(createdAt: string): boolean {
  const created = new Date(createdAt);
  const oneYearAgo = new Date();
  oneYearAgo.setFullYear(oneYearAgo.getFullYear() - 1);
  return created < oneYearAgo;
}

/**
 * Get viewing key export format (for auditors)
 */
export function formatViewingKeyForExport(key: ViewingKeyData): string {
  return JSON.stringify(
    {
      id: key.id,
      address: key.address,
      keyHash: key.keyHash,
      purpose: key.purpose,
      createdAt: key.createdAt,
      lastUsed: key.lastUsed,
    },
    null,
    2
  );
}

