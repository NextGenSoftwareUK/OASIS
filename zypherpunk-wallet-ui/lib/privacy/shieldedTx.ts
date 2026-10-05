/**
 * Shielded transaction utilities
 */

/**
 * Check if an address is a shielded address (Zcash z-address)
 */
export function isShieldedAddress(address: string): boolean {
  // Zcash shielded addresses start with 'z' or 'zt'
  return address.startsWith('z') || address.startsWith('zt');
}

/**
 * Check if an address is a transparent address
 */
export function isTransparentAddress(address: string): boolean {
  // Zcash transparent addresses start with 't'
  return address.startsWith('t');
}

/**
 * Validate shielded address format
 */
export function validateShieldedAddress(address: string): boolean {
  if (!address) return false;
  
  // Zcash z-addresses are typically 78 characters
  // zt addresses are for testnet
  if (address.startsWith('zt')) {
    return address.length >= 70 && address.length <= 80;
  }
  
  // Mainnet z-addresses
  if (address.startsWith('z')) {
    return address.length >= 70 && address.length <= 80;
  }
  
  return false;
}

/**
 * Get privacy level recommendation based on amount
 */
export function getRecommendedPrivacyLevel(amount: number): 'low' | 'medium' | 'high' | 'maximum' {
  if (amount >= 1000) {
    return 'maximum';
  } else if (amount >= 100) {
    return 'high';
  } else if (amount >= 10) {
    return 'medium';
  }
  return 'low';
}

/**
 * Calculate number of partial notes based on privacy level
 */
export function getPartialNotesCount(
  privacyLevel: 'low' | 'medium' | 'high' | 'maximum'
): number {
  const counts = {
    low: 1,
    medium: 2,
    high: 3,
    maximum: 5,
  };
  return counts[privacyLevel];
}

/**
 * Format memo for shielded transaction
 */
export function formatShieldedMemo(memo: string): string {
  // Zcash memos are 512 bytes, but we'll limit to reasonable length
  if (!memo) return '';
  return memo.substring(0, 500);
}

/**
 * Check if transaction should use partial notes
 */
export function shouldUsePartialNotes(
  amount: number,
  privacyLevel: 'low' | 'medium' | 'high' | 'maximum'
): boolean {
  // Use partial notes for high-value transactions or high privacy level
  return privacyLevel === 'high' || privacyLevel === 'maximum' || amount >= 100;
}

