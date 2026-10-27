// One signed submission policy for admitted actions. Preserve native Failure
// receipts and never let the SDK re-sign with another nonce automatically.
export async function submitNativeAction(account,receiverId,action,submitOriginal){
  if(submitOriginal!==undefined){
    if(typeof submitOriginal!=='function')throw new Error('Owned original submission capability required.');
    return await submitOriginal(account,receiverId,action);
  }
  return await account.signAndSendTransaction({receiverId,actions:[action],
    waitUntil:'FINAL',throwOnFailure:false,retries:0});
}
