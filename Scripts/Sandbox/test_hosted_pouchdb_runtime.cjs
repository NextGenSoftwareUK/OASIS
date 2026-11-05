'use strict';
const test = require('node:test');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const { captureBundle, canonical } = require('../../Docker/host-sandbox-pouchdb.cjs');
const hash = bytes => crypto.createHash('sha256').update(bytes).digest('hex');

function sample() {
  const actor = 'b05b1234-1000-4000-8000-000000000001';
  return { schema: 1, actor, database: 'oasis_holons', records: [1, 2, 3].map(number => {
    const id = 'b05b1234-2000-4000-8000-00000000000' + number;
    const bytes = Buffer.from(JSON.stringify({ Id: id, CreatedByAvatarId: actor, Name: 'PouchDB teaching record ' + number,
      Description: 'Synthetic public native document', IsPublic: true, MetaData: { DemoSource: 'PouchDB' }, Children: [] }));
    return { id, sha256: hash(bytes), jsonBase64: bytes.toString('base64') };
  }) };
}
function body(record, mutate) {
  const row = JSON.parse(Buffer.from(record.jsonBase64, 'base64').toString());
  const bytes = Buffer.from(typeof mutate === 'string' ? mutate : JSON.stringify(mutate(row) ?? row));
  record.jsonBase64 = bytes.toString('base64'); record.sha256 = hash(bytes);
}

test('PouchDB bundle binds three public projections to one stable actor/database/digest', () => {
  const value = sample();
  const first = captureBundle(JSON.stringify(value));
  assert.equal(first.digest, captureBundle(JSON.stringify(value, null, 2)).digest);
  assert.equal(first.records.length, 3);
  assert.ok(Object.isFrozen(first) && Object.isFrozen(first.records) && first.records.every(Object.isFrozen));
  assert.equal(canonical({ z: { b: 'two', a: 'one' }, a: [] }), '{"a":[],"z":{"a":"one","b":"two"}}');
});

const invalid = [
  ['wrong database', value => { value.database = 'foreign'; }],
  ['empty owner', value => { value.actor = '00000000-0000-0000-0000-000000000000'; }],
  ['extra config', value => { value.endpoint = 'https://foreign.invalid'; }],
  ['incomplete rows', value => { value.records.pop(); }],
  ['duplicate identity', value => { value.records[1] = value.records[0]; }],
  ['unsafe identity', value => { value.records[0].id = '../foreign'; }],
  ['changed transfer', value => { value.records[0].sha256 = '0'.repeat(64); }],
  ['extra transfer fields', value => { value.records[0].secret = 'controlled-only'; }],
  ['changed owner', value => { body(value.records[0], row => { row.CreatedByAvatarId = crypto.randomUUID(); }); }],
  ['changed identity', value => { body(value.records[0], row => { row.Id = crypto.randomUUID(); }); }],
  ['private record', value => { body(value.records[0], row => { row.IsPublic = false; }); }],
  ['native revision in packet', value => { body(value.records[0], row => { row._rev = '8-controlled'; }); }],
  ['child graph', value => { body(value.records[0], row => { row.Children = [{ Id: crypto.randomUUID() }]; }); }],
  ['numeric metadata', value => { body(value.records[0], row => { row.MetaData.number = 9007199254740992; }); }],
  ['oversized metadata', value => { body(value.records[0], row => { row.MetaData.value = 'x'.repeat(1025); }); }],
  ['unbounded description', value => { body(value.records[0], row => { row.Description = 'x'.repeat(1025); }); }]
];
for (const [name, mutate] of invalid) test('PouchDB source rejects ' + name + ' before native I/O', () => {
  const value = sample(); mutate(value);
  assert.throws(() => captureBundle(JSON.stringify(value)));
});

test('PouchDB rejects duplicate escaped/case-folded keys rather than taking the last value', () => {
  const value = sample();
  assert.throws(() => captureBundle(JSON.stringify(value).replace('"schema":1', '"schema":1,"sch\\u0065ma":1')), /Duplicate/);
  const row = JSON.parse(Buffer.from(value.records[0].jsonBase64, 'base64').toString());
  body(value.records[0], JSON.stringify(row).replace('"Name":', '"name":"duplicate","Name":'));
  assert.throws(() => captureBundle(JSON.stringify(value)), /Duplicate/);
});

test('PouchDB rejects noncanonical base64, invalid UTF8 and oversized envelopes', () => {
  let value = sample(); value.records[0].jsonBase64 += '\n';
  assert.throws(() => captureBundle(JSON.stringify(value)), /encoding/);
  value = sample(); const bytes = Buffer.from([0xff]);
  value.records[0].jsonBase64 = bytes.toString('base64'); value.records[0].sha256 = hash(bytes);
  assert.throws(() => captureBundle(JSON.stringify(value)));
  assert.throws(() => captureBundle(' '.repeat(131073)), /Bounded/);
});

module.exports = { sample };
