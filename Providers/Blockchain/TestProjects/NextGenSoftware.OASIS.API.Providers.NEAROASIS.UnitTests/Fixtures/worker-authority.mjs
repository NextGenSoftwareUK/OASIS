import {readFileSync} from 'node:fs';
// Controlled IPC authority test only; no SDK/network/signing/persistence.
const input=JSON.parse(readFileSync(0,'utf8'));
const signing=['transfer','call','putRecordChecked','prepareNativeAction'].includes(input.operation);
if(signing){if(input.privateKey!=='explicit-signing-key')throw new Error('Signing operation lost its explicitly selected authority');}
else if(Object.hasOwn(input,'privateKey'))throw new Error('Read-only worker received configured signing authority');
if(input.operation==='derivePublicKey'&&input.secretKey!=='explicit-derivation-key')throw new Error('Explicit derivation input lost');
if(input.operation==='deriveMnemonic'&&input.seedPhrase!=='explicit-derivation-seed')throw new Error('Explicit mnemonic input lost');
if(Object.hasOwn(process.env,'NEAROASIS_PRIVATEKEY')||Object.hasOwn(process.env,'OASIS_NEAR_STORAGE_ENCRYPTION_KEY'))throw new Error('SDK worker inherited unrelated NEAR credentials');
process.stdout.write(JSON.stringify({ok:true,result:{processed:true}}));
