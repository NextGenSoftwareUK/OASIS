import {readFileSync} from 'node:fs';
// Controlled process response only: no SDK, key derivation, signing or network.
const input=JSON.parse(readFileSync(0,'utf8'));
if(!['call','transfer'].includes(input.operation)||input.accountId!=='sender.near')throw new Error('Unexpected controlled mutation');
process.stdout.write(process.env.OASIS_NEAR_MUTATION_RESPONSE);
