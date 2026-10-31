// One-time local fixture configuration, not a production network specification or SDK install.
import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync, writeFileSync } from 'node:fs';

const binary = '/var/tmp/oasis-runtimes/substrate-contracts-node-0.42.0/substrate-contracts-node';
const target = '\\\\wsl.localhost\\Ubuntu\\var\\tmp\\oasis-polkadot-evidence\\chain-spec.json';
const expected = 'b5e30ad4bcf37ab0a875f88c690460cf68a0847c03e74045d15bb16513892991';
const actual = execFileSync('wsl.exe', ['-d', 'Ubuntu', '--', 'sha256sum', binary], { encoding: 'utf8' }).split(/\s+/)[0];
if (actual !== expected) throw new Error('Retained pinned runtime differs; do not replace or reset its ledger.');
const properties = { tokenDecimals: 12, tokenSymbol: 'UNIT', ss58Format: 42 };
if (existsSync(target)) {
  const retained = JSON.parse(readFileSync(target, 'utf8'));
  if (JSON.stringify(retained.properties) !== JSON.stringify(properties) || retained.id !== 'dev'
      || retained.relay_chain !== 'rococo-local' || retained.para_id !== 1000)
    throw new Error('Existing local chain specification differs; preserve and investigate, never overwrite.');
  console.log('Existing local chain specification reused.');
} else {
  const specification = JSON.parse(execFileSync('wsl.exe', ['-d', 'Ubuntu', '--', binary,
    'build-spec', '--chain', 'dev', '--raw', '--disable-default-bootnode'],
    { encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 }));
  if (specification.id !== 'dev' || specification.chainType !== 'Development') throw new Error('Unexpected generated chain identity.');
  specification.properties = properties; // Local UNIT denomination is explicitly defined, never inferred from a live chain.
  // v0.42 custom-file loader requires parachain extension fields even for Development;
  // its Development service branch does not start a relay chain.
  specification.relay_chain = 'rococo-local';
  specification.para_id = 1000;
  writeFileSync(target, JSON.stringify(specification), { flag: 'wx' });
  console.log('Created one local chain specification with explicit UNIT precision; genesis runtime/state unchanged.');
}
