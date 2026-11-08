"""Qualify native control, source commit and current contacts initialization."""
import hashlib
import json
import os
from pathlib import Path
import platform
import re
import shutil
import stat
import subprocess
import time

from owned_docker_resources import missing_resource
from urbit_conn import thread


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def main(on_initialized=None):
    assert platform.system() == 'Linux' and os.environ.get('GITHUB_ACTIONS') == 'true'
    root = Path(__file__).resolve().parents[2]
    assets = root / 'LocalState/COSMIC/CloudUrbitRuntimeAssets/v4.6-408k-2'
    sources = root / 'LocalState/COSMIC/CloudUrbitContactsDesk'
    kernel = root / 'LocalState/COSMIC/CloudUrbitKernel408'
    inputs = json.loads((root / 'Scripts/TestHosts/urbit-runtime-inputs.json').read_text())
    assert digest(assets / 'urbit') == '47ad302e8934271dfc00dcdab1d553c6d6460b105e3099ff47e0e4e4231bb26d'
    assert digest(assets / 'solid.pill') == inputs['pillSha256']
    assert digest(sources / 'files.json') == '54eff9a62fc2337d472767cd61e5ccd0d1743bba243b0da614d016916a86d464'
    manifest = json.loads((sources / 'files.json').read_text())
    desk = sources / 'desk'
    assert sorted(str(p.relative_to(desk)) for p in desk.rglob('*') if p.is_file()) == sorted(manifest)
    for path, entry in manifest.items():
        source = desk / path
        assert not source.is_symlink() and source.stat().st_size == entry['bytes'] and digest(source) == entry['sha256']
    assert digest(kernel / 'files.json') == '09f7f0e3d6798499b5fb87f3037bb8b6f4ee0dd52badef5ea26f8392b306f7fe'
    kernel_manifest = json.loads((kernel / 'files.json').read_text())
    base_source = kernel / 'base'
    assert sorted(str(p.relative_to(base_source)) for p in base_source.rglob('*') if p.is_file()) == sorted(kernel_manifest)
    for path, entry in kernel_manifest.items():
        source = base_source / path
        assert not source.is_symlink() and source.stat().st_size == entry['bytes'] and digest(source) == entry['sha256']
    run, attempt = os.environ['GITHUB_RUN_ID'], os.environ['GITHUB_RUN_ATTEMPT']
    assert run.isdecimal() and attempt.isdecimal()
    owner = 'urbit-contacts-' + run + '-' + attempt
    label = 'oasis.urbit.contacts.owner'
    out = root / 'TestResults/UrbitContactsInstallCurrent'
    out.mkdir(parents=True, exist_ok=False)
    data = out / 'owned-pier'
    data.mkdir()
    report = dict(source=os.environ['GITHUB_SHA'], owner=owner, nativeControlQualified=False,
                  nativeKernel408Verified=False,
                  currentSourceCommitted=False, contactsAgentInitialized=False,
                  nativeContactsQualified=False, cqlQualified=False, liveDeployment=False,
                  acknowledgements=[])
    (out / 'intent.json').write_text(json.dumps(report, indent=2))

    def docker(*args, check=True, timeout=60):
        result = subprocess.run(['docker', *args], capture_output=True, text=True, timeout=timeout)
        if check:
            result.check_returncode()
        return result

    def inspect():
        result = docker('container', 'inspect', owner, check=False)
        if missing_resource('container', owner, result.returncode, result.stderr):
            return None
        result.check_returncode()
        state = json.loads(result.stdout)[0]
        assert state['Config']['Labels'].get(label) == owner
        return state

    def ready(predicate, seconds=120):
        deadline = time.monotonic() + seconds
        while not predicate():
            state = inspect()
            assert state is not None and state['State']['Running'], 'Native ship stopped before readiness.'
            assert time.monotonic() < deadline, 'Native readiness deadline exceeded.'
            time.sleep(1)

    connection = data / 'zod/.urb/conn.sock'
    binary = assets / 'urbit'

    def execute(name, body, expected='0', timeout=120):
        result = thread(binary, connection, body, timeout)
        (out / (name + '.txt')).write_text(result)
        # A poke acknowledgement alone never sets source/agent readiness flags.
        canonical = re.sub(r'\s+', ' ', result).strip()
        assert canonical == '[1 %avow 0 %noun ' + expected + ']', 'Native thread rejected: ' + name
        report['acknowledgements'].append(name)

    prefix = '=/  m  (strand ,vase)  ;<  =bowl  bind:m  get-bowl  '
    # Vere cue prints untyped small atoms numerically. Choose one explicit
    # numeric sentinel instead of guessing a textual aura for a term.
    suffix = '(pure:m !>(0))'

    def replace_mounted(name, source):
        mounted = data / 'zod' / name
        ready(lambda: (mounted / 'sys.kelvin').is_file())
        assert mounted.resolve() == data.resolve() / 'zod' / name and not mounted.is_symlink()
        for child in mounted.iterdir():
            assert child.resolve().is_relative_to(mounted.resolve()) and not child.is_symlink()
            if child.is_dir():
                shutil.rmtree(child)
            else:
                child.unlink()
        shutil.copytree(source, mounted, dirs_exist_ok=True)

    touched = False
    completed = False

    def create_container(initial):
        command = ['/assets/urbit', '-L', '-t', '--loom', '32']
        command += ['-F', 'zod', '-c', '/pier/zod', '-B', '/assets/solid.pill'] if initial else ['/pier/zod']
        docker('create', '--name', owner, '--label', label + '=' + owner,
               '--user', str(os.getuid()) + ':' + str(os.getgid()),
               '--network', 'none', '--memory', '3g', '--memory-swap', '3g', '--cpus', '2',
               '--pids-limit', '128', '--cap-drop', 'ALL', '--security-opt', 'no-new-privileges',
               '--read-only', '--workdir', '/pier', '--env', 'HOME=/pier',
               '--tmpfs', '/tmp:rw,size=64m,mode=1777',
               '--mount', 'type=bind,source=' + str(assets) + ',target=/assets,readonly',
               '--mount', 'type=bind,source=' + str(data) + ',target=/pier',
               inputs['containerImage'], *command, timeout=180)

    def restart_owned_pier():
        before = connection.stat().st_mtime_ns
        docker('stop', '--time', '30', owner)
        state = inspect()
        assert state['State']['ExitCode'] == 0 and not state['State']['OOMKilled']
        report['restartShutdownExitCode'] = 0
        logs = docker('logs', owner)
        (out / 'before-restart.log').write_text(logs.stdout + logs.stderr)
        docker('rm', '-v', owner)
        assert inspect() is None
        create_container(False)
        docker('start', owner)
        ready(lambda: connection.exists() and stat.S_ISSOCK(connection.stat().st_mode)
              and connection.stat().st_mtime_ns != before)
        execute('restart-control', prefix + suffix)

    try:
        assert inspect() is None
        touched = True
        create_container(True)
        docker('start', owner)
        ready(lambda: connection.exists() and stat.S_ISSOCK(connection.stat().st_mode))
        execute('control', prefix + suffix)
        report['nativeControlQualified'] = True
        execute('mount-base', prefix + '=/  =path  [(scot %p our.bowl) %base (scot %da now.bowl) ~]  ;<  ~  bind:m  (poke [our.bowl %hood] kiln-mount+!>([path %base]))  ' + suffix)
        replace_mounted('base', base_source)
        execute('commit-base', prefix + ';<  ~  bind:m  (poke [our.bowl %hood] kiln-commit+!>([%base |]))  ' + suffix, timeout=600)
        # Readiness probes compile anew on the running OS. A Hoon loop compiled
        # before the upgrade would keep the old zuse constant in its code.
        deadline = time.monotonic() + 600
        kernel_probes = []
        while True:
            remaining = deadline - time.monotonic()
            assert remaining > 0, 'Native OS readiness deadline exceeded.'
            result = thread(binary, connection, prefix + '(pure:m !>(zuse))', timeout=min(600, remaining))
            canonical = re.sub(r'\s+', ' ', result).strip()
            kernel_probes.append(canonical)
            assert canonical in ('[1 %avow 0 %noun 408]', '[1 %avow 0 %noun 409]'), 'Native OS readiness probe failed.'
            report['nativeKernelVersion'] = 408 if canonical == '[1 %avow 0 %noun 408]' else 409
            report['kernelReadinessProbeCount'] = len(kernel_probes)
            if report['nativeKernelVersion'] == 408:
                break
            assert time.monotonic() < deadline, 'Native OS did not reach Kelvin408.'
            time.sleep(2)
        (out / 'kernel-readiness.json').write_text(json.dumps(kernel_probes, indent=2))
        report['nativeKernel408Verified'] = True
        execute('create-groups', prefix + ';<  ~  bind:m  (poke [our.bowl %hood] kiln-merge+!>([%groups our.bowl %base da+now.bowl %init]))  ' + suffix)
        execute('mount-groups', prefix + '=/  =path  [(scot %p our.bowl) %groups (scot %da now.bowl) ~]  ;<  ~  bind:m  (poke [our.bowl %hood] kiln-mount+!>([path %groups]))  ' + suffix)
        replace_mounted('groups', desk)
        execute('commit-groups', prefix + ';<  ~  bind:m  (poke [our.bowl %hood] kiln-commit+!>([%groups |]))  ' + suffix, timeout=600)
        execute('verify-source', prefix + ';<  exists=?  bind:m  (scry ? %cu /groups/app/contacts/hoon)  ?>  exists  ' + suffix)
        report['currentSourceCommitted'] = True
        execute('install-groups', prefix + ';<  ~  bind:m  (poke [our.bowl %hood] kiln-install+!>([%groups our.bowl %groups]))  ' + suffix, timeout=600)
        # Await one native predicate within a single operation, not repeated pokes.
        body = prefix + '=|  attempts=@ud  |-  ?:  .^(? %gu /(scot %p our.bowl)/contacts/(scot %da now.bowl)/$)  ' + suffix
        body += '  ?>  (lth attempts 120)  ;<  ~  bind:m  (sleep ~s1)  $(attempts +(attempts))'
        execute('contacts-ready', body, timeout=180)
        report['contactsAgentInitialized'] = True
        if on_initialized is not None:
            on_initialized(dict(root=root, out=out, report=report, execute=execute,
                                prefix=prefix, suffix=suffix, restart=restart_owned_pier))
        completed = True
    finally:
        if touched:
            state = inspect()
            if state is not None:
                try:
                    logs = docker('logs', owner)
                    log = logs.stdout + logs.stderr
                    assert len(log.encode()) <= 2 * 1024 * 1024
                    (out / 'native.log').write_text(log)
                finally:
                    if state['State']['Running']:
                        docker('stop', '--time', '30', owner)
                    state = inspect()
                    report['exitCode'] = state['State']['ExitCode']
                    report['oomKilled'] = state['State']['OOMKilled']
                    report['networkMode'] = state['HostConfig']['NetworkMode']
                    docker('rm', '-v', owner)
            assert inspect() is None
            report['ownedContainerRemoved'] = True
        assert data.resolve().parent == out.resolve() and not data.is_symlink()
        shutil.rmtree(data)
        report['ownedPierRemoved'] = not data.exists()
        (out / 'qualification.json').write_text(json.dumps(report, indent=2))
    if completed:
        assert report['exitCode'] == 0 and not report['oomKilled'] and report['networkMode'] == 'none', 'Native lifecycle did not finish cleanly.'


if __name__ == '__main__':
    main()
