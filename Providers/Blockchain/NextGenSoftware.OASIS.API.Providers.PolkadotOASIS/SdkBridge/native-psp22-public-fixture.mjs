// Fixed cohorts reuse one token/escrow/environment. Historical originals stay immutable.
export function publicPsp22Fixture() {
  const cohort=process.env.OASIS_POLKADOT_TOKEN_PUBLIC_COHORT ?? '1';
  if(!['1','2'].includes(cohort))throw new Error('Explicit retained public PSP22 cohort required.');
  return Object.freeze({prefix:cohort==='1'?'psp22-public':'psp22-public-postfix',
    lockId:cohort==='1'?'e7b3473198cc497d9f115d5b8dd87fb1':'e7b3473198cc497d9f115d5b8dd87fb2'});
}
