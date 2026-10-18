import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { originalSubmission } from './original-submission.mjs';

for (const outcome of ['accepted', 'rejected', 'lost-response']) {
  test(`exact original HTTP submission is consumed once: ${outcome}`, async () => {
    const requests = [];
    const server = createServer(async (request, response) => {
      let body = '';
      for await (const chunk of request) body += chunk;
      const rpc = JSON.parse(body);
      requests.push(rpc);
      if (outcome === 'lost-response') { request.socket.destroy(); return; }
      response.setHeader('Content-Type', 'application/json');
      response.end(JSON.stringify({ jsonrpc: '2.0', id: rpc.id,
        ...(outcome === 'accepted' ? { result: '0x' + 'a'.repeat(64) } : { error: { code: 1010, message: 'Invalid Transaction' } }) }));
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    try {
      const submit = originalSubmission(`http://127.0.0.1:${server.address().port}`, Buffer.from([1, 2, 3]).toString('base64'));
      const first = submit();
      const reused = submit();
      await assert.rejects(reused, /already been consumed/);
      if (outcome === 'accepted') assert.equal(await first, '0x' + 'a'.repeat(64));
      else await assert.rejects(first);
      await assert.rejects(submit(), /already been consumed/);
      assert.equal(requests.length, 1);
      assert.equal(requests[0].method, 'author_submitExtrinsic');
      assert.deepEqual(requests[0].params, ['0x010203']);
    } finally {
      server.closeAllConnections();
      await new Promise(resolve => server.close(resolve));
    }
  });
}
