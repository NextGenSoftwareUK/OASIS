"""Verify the pinned zkSync interpreter's explicit checkpoint API before fixture writes."""
from pathlib import Path
import hashlib, json, os, signal, socket, subprocess, time, urllib.error, urllib.request

ROOT = Path('/var/tmp/oasis-zksync-cql')
RESULTS = Path('/mnt/c/Source/OASIS-cosmic/TestResults/ZkSyncCqlCurrent')
BINARY = Path('/var/tmp/oasis-runtimes/anvil-zksync-0.6.11/anvil-zksync')
DIGEST = '4a801b25f543099838fa88df50f0e59f5e5c93b7a22330948ca7ed029f48228b'
CHECKPOINT = ROOT / 'checkpoint.hex'
ACK = ROOT / 'checkpoint.ack.json'
PENDING = ROOT / 'checkpoint.pending.json'


def durable(path, value):
    with path.open('x', encoding='utf-8') as stream:
        stream.write(value)
        stream.flush()
        os.fsync(stream.fileno())


def rpc(method, params=None):
    request = urllib.request.Request('http://127.0.0.1:18548',
        data=json.dumps(dict(jsonrpc='2.0', id=1, method=method, params=params or [])).encode(),
        headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(request, timeout=20) as response:
        value = json.load(response)
    if 'error' in value:
        raise RuntimeError(f'{method}: {value["error"]}')
    return value['result']


def start(logname, load=False):
    # Never adopt an unknown listener or reset an existing environment.
    with socket.socket() as probe:
        # Match the server's reuse policy so closed connections in TIME_WAIT
        # do not masquerade as an active listener during immediate restart.
        probe.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        probe.bind(('127.0.0.1', 18548))
    log = (RESULTS / logname).open('wb')
    load_args = []
    if load:
        payload = CHECKPOINT.read_text()
        recorded = json.loads(ACK.read_text())
        assert hashlib.sha256(payload.encode()).hexdigest() == recorded['sha256']
        binary_checkpoint = ROOT / 'checkpoint.bin'
        raw = bytes.fromhex(payload[2:])
        if binary_checkpoint.exists():
            assert binary_checkpoint.read_bytes() == raw
        else:
            with binary_checkpoint.open('xb') as file:
                file.write(raw); file.flush(); os.fsync(file.fileno())
        load_args = ['--load-state', str(binary_checkpoint)]
    process = subprocess.Popen([str(BINARY), '--host', '127.0.0.1', '--port', '18548',
        '--chain-id', '31339', '--evm-interpreter', '--cache-dir', str(RESULTS/'node-cache'),
        '--log-file-path', str(RESULTS/'node.log'), '--silent', 'true', *load_args, 'run'],
        cwd=ROOT, stdout=log, stderr=subprocess.STDOUT,
        env={**os.environ, 'RAYON_NUM_THREADS': '2'})
    try:
        for _ in range(80):
            if process.poll() is not None:
                raise RuntimeError('Owned native node exited; preserve logs/state')
            try:
                assert rpc('eth_chainId') == hex(31339)
                return process, log
            except (ConnectionError, urllib.error.URLError):
                time.sleep(.25)
        raise RuntimeError('Native readiness deadline exceeded')
    except BaseException:
        stop(process, log)
        raise


def stop(process, log):
    if process.poll() is None:
        process.send_signal(signal.SIGINT)
    process.wait(timeout=30)
    log.close()
    assert process.returncode == 0, 'Native process exited nonzero; inspect startup log and preserve checkpoint'


def main():
    assert hashlib.sha256(BINARY.read_bytes()).hexdigest() == DIGEST
    assert ROOT.is_dir() and not PENDING.exists(), 'Unresolved outcome; reconcile, never replay'
    if not CHECKPOINT.exists():
        assert not ACK.exists(), 'Original checkpoint missing; never replace'
        durable(PENDING, json.dumps({'chain': 31339, 'operation': 'three-block checkpoint probe'}))
        process, log = start('checkpoint-create.log')
        try:
            for _ in range(3):
                rpc('evm_mine')
            block = rpc('eth_blockNumber')
            assert int(block, 16) >= 3
            payload = rpc('anvil_dumpState', [True])
            assert isinstance(payload, str) and payload.startswith('0x') and len(payload) > 2
            durable(CHECKPOINT, payload)
            durable(ACK, json.dumps({'block': block, 'sha256': hashlib.sha256(payload.encode()).hexdigest()}))
            PENDING.unlink()
        finally:
            stop(process, log)
    else:
        assert ACK.exists(), 'Original checkpoint acknowledgement missing'
    process, log = start('checkpoint-reload.log', load=True)
    try:
        recorded = json.loads(ACK.read_text())
        assert rpc('eth_blockNumber') == recorded['block']
        before = 'separate process loaded original checkpoint'
        print(json.dumps({'result': 'passed', 'chain': 31339, 'blockBefore': before,
            'blockRestored': recorded['block'], 'checkpointBytes': CHECKPOINT.stat().st_size,
            'sha256': recorded['sha256']}), flush=True)
    finally:
        stop(process, log)


if __name__ == '__main__':
    main()
