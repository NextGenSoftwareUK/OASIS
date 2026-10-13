export function estimatedTinybars(totalTinycents,rate) {
 if(typeof totalTinycents!=='string'||!/^[1-9][0-9]*$/.test(totalTinycents))throw new Error('A positive exact fee estimate is required.');
 if(!Number.isSafeInteger(rate.hbar_equivalent)||rate.hbar_equivalent<=0||!Number.isSafeInteger(rate.cent_equivalent)||rate.cent_equivalent<=0)
  throw new Error('Exact positive network exchange-rate units are required.');
 const numerator=BigInt(totalTinycents)*BigInt(rate.hbar_equivalent),denominator=BigInt(rate.cent_equivalent);
 return (numerator+denominator-1n)/denominator;
}

export function validateDeploymentBudget(estimate,ceiling,balance) {
 for(const value of [estimate,ceiling,balance])if(typeof value!=='bigint'||value<0n||value>9223372036854775807n)throw new Error('Deployment budget requires exact nonnegative int64 tinybars.');
 if(estimate===0n||ceiling===0n||estimate>ceiling)throw new Error('Explicit fee ceiling is below the deployment estimate.');
 if(balance<ceiling)throw new Error('Available test funding is below the explicit deployment fee ceiling.');
}

// HAPI contract creation's SDK estimate is not an all-in spending cap. Combine
// the non-gas estimate with the independently quoted maximum declared gas.
// The caller must retain quote provenance/expiry; this is not consensus enforcement.
export function quotedDeploymentTinybars(baseTinycents,rate,gasLimit,gasPriceTinybars,serviceExtras) {
 if(!Number.isSafeInteger(gasLimit)||gasLimit<=0)throw new Error('Explicit positive safe-integer gas limit required.');
 if(typeof gasPriceTinybars!=='bigint'||gasPriceTinybars<=0n||gasPriceTinybars>9223372036854775807n)
  throw new Error('Exact positive int64 quoted gas price required.');
 if(!Array.isArray(serviceExtras)||serviceExtras.some(extra=>typeof extra?.name!=='string'))
  throw new Error('Explicit service extras required to establish estimate coverage.');
 if(serviceExtras.some(extra=>extra.name==='GAS'))
  throw new Error('SDK estimate already includes GAS; reconcile its coverage before adding gas again.');
 const base=estimatedTinybars(baseTinycents,rate),gas=BigInt(gasLimit)*gasPriceTinybars;
 if(base<0n||base+gas>9223372036854775807n)throw new Error('Combined deployment quote exceeds int64 tinybars.');
 return {base,gas,total:base+gas};
}
