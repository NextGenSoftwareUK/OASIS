import { HttpProvider } from '@polkadot/api';
import { canonicalBase64, verifyNativePreparation } from './verify-preparation.mjs';

// A capability for one exact original extrinsic, not a writable metadata/API provider.
export function originalSubmission(endpoint, signedTransactionBase64) {
  const bytes = canonicalBase64(signedTransactionBase64);
  const signedHex = '0x' + bytes.toString('hex');
  const provider = new HttpProvider(endpoint, {}, 0);
  let used = false;
  return async () => {
    if (used) throw new Error('Original submission capability has already been consumed; reconcile before any replay.');
    used = true; // Consume BEFORE I/O, including timeout, rejection and lost response.
    return provider.send('author_submitExtrinsic', [signedHex], false);
  };
}

export async function submitOriginalPreparation(api, preparation, persist, submit, verify = verifyNativePreparation) {
  if (typeof persist !== 'function' || typeof submit !== 'function') throw new Error('Owned persistence and exact-original submission capabilities are required.');
  // Freeze the whole original across asynchronous validation and persistence.
  const original = structuredClone(preparation);
  const identity = await verify(api, original);
  await persist(original);
  try {
    const hash = await submit();
    if (hash !== identity.transactionHash) throw new Error('Submission response does not match the original signed transaction hash.');
    return { transactionHash: hash, persisted: true, submitted: true, poolAccepted: true,
      finalized: false, reconciliationRequired: true };
  } catch (cause) {
    const error = new Error('Original submission outcome requires reconciliation; do not regenerate or resubmit.', { cause });
    error.originalTransactionId = identity.transactionHash;
    error.reconciliationRequired = true;
    throw error;
  }
}
