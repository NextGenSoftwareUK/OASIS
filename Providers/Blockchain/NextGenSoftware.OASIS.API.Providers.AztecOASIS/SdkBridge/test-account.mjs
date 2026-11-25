// CI fixture helper: prints the local network's first pre-funded test account (public, well-known test keys).
import { getInitialTestAccountsData } from '@aztec/accounts/testing';
const [account] = await getInitialTestAccountsData();
process.stdout.write(JSON.stringify({ secret: account.secret.toString(), salt: account.salt.toString(), signingKey: account.signingKey.toString(), address: account.address.toString() }) + '\n');
