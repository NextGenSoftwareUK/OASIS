// One SDK invocation. This does not certify SDK-internal transport retries or durable pre-sign admission.
export async function submitCheckedRecord(provider,createAccount,input,createAction) {
  let account,action;
  try {
  if(!input.networkId||!input.contractId)throw new Error('Explicit network and contract identity required');
  if(await provider.getNetworkId()!==input.networkId)throw new Error('NEAR checked-write network mismatch');
  if(await provider.callFunction({contractId:input.contractId,method:'record_write_protocol',args:{},blockQuery:{finality:'final'}})!==1)
    throw new Error('NEAR conditional record protocol1 required; no unconditional fallback');
  account=createAccount();action=createAction(input.args);
  } catch(cause) {
    const error=cause instanceof Error?cause:new Error(String(cause));
    error.checkedWritePhase='before-submission';throw error;
  }
  const outcome=await account.signAndSendTransaction({receiverId:input.contractId,
    actions:[action],waitUntil:'FINAL',throwOnFailure:false});
  // Preserve native failures as receipts, not exceptions which discard the acknowledged identity.
  // Consumers must validate FINAL/status and reconcile malformed/unknown acknowledgements.
  return {transactionHash:outcome.transaction_outcome?.id??null,
    finalExecutionStatus:outcome.final_execution_status??null,nativeStatus:outcome.status??null};
}
