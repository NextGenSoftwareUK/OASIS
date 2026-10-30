// Exact-source local test-runtime repairs; never patch an unknown vendor revision.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const runtime = path.join(root, 'LocalState/COSMIC/ArweaveRuntime');
const source = path.join(runtime, 'node_modules/arlocal/bin/app.js');
const manifest = path.join(runtime, 'oasis-runtime-patch.json');
const hash = value => crypto.createHash('sha256').update(value).digest('hex');
const original = 'a3aefb60f5438c79949493b52b4b381661790849933160b82c4558a0cd1163b7';
function prepare() {
  if (require(path.join(runtime,'node_modules/arlocal/package.json')).version !== '1.1.66')
    throw new Error('Pinned ArLocal1.1.66 required; no automatic install.');
  const bytes = fs.readFileSync(source);
  if (fs.existsSync(manifest)) {
    const recorded = JSON.parse(fs.readFileSync(manifest,'utf8'));
    if(recorded.original !== original || recorded.patched !== hash(bytes))
      throw new Error('ArLocal source differs from retained patch; preserve and reconcile.');
    if (recorded.revision === 2) return recorded;
    if (recorded.patched !== 'd7282066e53e8548b0e74c610df5b70200905d09ae5413e173262dcf0587c6aa')
      throw new Error('Unknown prior persistence patch; reconcile without reset.');
    const old = 'yield (0, initialize_1.down)(this.connection, this.persist);';
    const text = bytes.toString('utf8');
    if(text.split(old).length !== 2) throw new Error('Expected one native destructive-down call.');
    const repaired = text.replace(old, 'if (!this.persist) yield (0, initialize_1.down)(this.connection, true);');
    const updated = {...recorded,revision:2,patched:hash(Buffer.from(repaired)),
      fixes:[...recorded.fixes,'persistent shutdown never drops native tables']};
    fs.writeFileSync(source,repaired);
    fs.writeFileSync(manifest,JSON.stringify(updated,null,2));
    return updated;
  }
  if(hash(bytes) !== original) throw new Error('Unknown ArLocal source; no speculative patch.');
  let text = bytes.toString('utf8');
  const listener = 'this.server = this.app.listen(this.port, () => {';
  if(text.split(listener).length !== 2) throw new Error('Expected one native listener.');
  text = text.replace(listener, "this.server = this.app.listen(this.port, '127.0.0.1', () => {");
  const begin = text.indexOf('    stop() {');
  const end = text.indexOf('    getServer() {',begin);
  if(begin < 0 || end < 0) throw new Error('Pinned teardown boundary missing.');
  text = text.slice(0,begin) + `    stop() {
        return __awaiter(this, void 0, void 0, function* () {
            if (this.server && this.server.listening) {
                yield new Promise((resolve, reject) => this.server.close(error => error ? reject(error) : resolve()));
            }
            if (!this.persist) yield (0, initialize_1.down)(this.connection, true);
            yield this.apollo.stop();
            yield this.connection.destroy();
        });
    }
` + text.slice(end);
  const recorded = {version:'1.1.66',revision:2,original,patched:hash(Buffer.from(text)),
    fixes:['loopback-only listener','awaited native teardown with propagated errors','persistent shutdown never drops native tables']};
  fs.writeFileSync(source,text);
  fs.writeFileSync(manifest,JSON.stringify(recorded,null,2),{flag:'wx'});
  return recorded;
}
module.exports = {prepare};
if(require.main === module) console.log(JSON.stringify(prepare()));
