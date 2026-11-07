'use strict';
const path = require('node:path');
const contract = require('./contract.json');
function credential(value) {
  if (typeof value !== 'string' || !/^[a-f0-9]{64}$/.test(value))
    throw Error('An explicit 256-bit hexadecimal native credential is required.');
  return value;
}
function directory(value) {
  if (typeof value !== 'string' || !path.isAbsolute(value) || /[\r\n\0]/.test(value))
    throw Error('An explicit absolute native source directory is required.');
  return value;
}
function garnetArguments(root, aclFile, mode) {
  directory(root); directory(aclFile);
  if (!['provision', 'recover'].includes(mode)) throw Error('Explicit source lifecycle mode required.');
  return ['--bind', '127.0.0.1', '--port', String(contract.ports.redis), '--auth', 'ACL',
    '--acl-file', aclFile, ...contract.garnetArguments, '--checkpointdir', path.join(root, 'store'),
    ...(mode === 'recover' ? ['--recover', '--fail-on-recovery-error'] : [])];
}
function daprArguments(root) {
  directory(root);
  return ['--app-id', contract.appId, '--dapr-listen-addresses', '127.0.0.1',
    '--dapr-http-port', String(contract.ports.http), '--dapr-grpc-port', String(contract.ports.grpc),
    '--dapr-internal-grpc-port', String(contract.ports.internalGrpc), '--enable-metrics=false',
    '--resources-path', path.join(root, 'components')];
}
function readerAcl(password) {
  return `user default off\nuser reader on >${credential(password)} ~* +@read +@connection +command +info\n`;
}
function writerAcl(admin, reader) {
  if (credential(admin) === credential(reader)) throw Error('Provisioning and reader credentials must be distinct.');
  return `user default on >${credential(admin)} ~* +@all\nuser reader on >${credential(reader)} ~* +@read +@connection +command +info\n`;
}
function component(password, principal) {
  credential(password);
  if (!['reader', 'default'].includes(principal)) throw Error('Explicit native principal required.');
  return `apiVersion: dapr.io/v1alpha1\nkind: Component\nmetadata:\n  name: ${contract.store}\nspec:\n  type: state.redis\n  version: v1\n  metadata:\n  - name: redisHost\n    value: "127.0.0.1:${contract.ports.redis}"\n  - name: redisUsername\n    value: "${principal}"\n  - name: redisPassword\n    value: "${password}"\nscopes:\n- ${contract.appId}\n`;
}
module.exports = { contract, credential, garnetArguments, daprArguments, readerAcl, writerAcl, component };
