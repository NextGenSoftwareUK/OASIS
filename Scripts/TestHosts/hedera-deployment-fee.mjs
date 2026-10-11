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
