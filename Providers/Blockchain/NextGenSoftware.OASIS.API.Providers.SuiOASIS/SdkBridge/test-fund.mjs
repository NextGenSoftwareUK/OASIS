import { requestSuiFromFaucetV2 } from '@mysten/sui/faucet';
const host = process.env.OASIS_SUI_TEST_FAUCET;
if (!host || !['127.0.0.1', 'localhost', '[::1]'].includes(new URL(host).hostname))
  throw new Error('Only a configured local test faucet is permitted');
await requestSuiFromFaucetV2({ host, recipient: process.argv[2] });
