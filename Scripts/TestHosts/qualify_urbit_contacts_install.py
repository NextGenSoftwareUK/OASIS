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


def main():
    assert platform.system() == 'Linux' and os.environ.get('GITHUB_ACTIONS') == 'true'
    root = Path(__file__).resolve().parents[2]
    assets = root / 'LocalState/COSMIC/CloudUrbitRuntimeAssets/v4.6-408k-2'
    sources = root / 'LocalState/COSMIC/CloudUrbitContactsDesk'
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
    run, attempt = os.environ['GITHUB_RUN_ID'], os.environ['GITHUB_RUN_ATTEMPT']
    assert run.isdecimal() and attempt.isdecimal()
    owner = 'urbit-contacts-' + run + '-' + attempt
    label = 'oasis.urbit.contacts.owner'
    out = root / 'TestResults/UrbitContactsInstallCurrent'
    out.mkdir(parents=True, exist_ok=False)
    data = out / 'owned-pier'
    data.mkdir()
    report = dict(source=os.environ['GITHUB_SHA'], owner=owner, nativeControlQualified=False,
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

    def execute(name, body, expected='%ok', timeout=120):
        result = thread(binary, connection, body, timeout)
        (out / (name + '.txt')).write_text(result)
        # A poke acknowledgement alone never sets source/agent readiness flags.
        canonical = re.sub(r'\s+', ' ', result).strip()
        assert canonical == '[1 %avow 0 %noun ' + expected + ']', 'Native thread rejected: ' + name
        report['acknowledgements'].append(name)

    prefix = '=/  m  (strand ,vase)  ;<  =bowl  bind:m  get-bowl  '
    suffix = '(pure:m !>(%ok))'
    touched = False
    try:
        assert inspect() is None
        touched = True
        docker('create', '--name', owner, '--label', label + '=' + owner,
               '--network', 'none', '--memory', '3g', '--memory-swap', '3g', '--cpus', '2',
               '--pids-limit', '128', '--cap-drop', 'ALL', '--security-opt', 'no-new-privileges',
               '--read-only', '--workdir', '/pier', '--env', 'HOME=/pier',
               '--tmpfs', '/tmp:rw,size=64m,mode=1777',
               '--mount', 'type=bind,source=' + str(assets) + ',target=/assets,readonly',
               '--mount', 'type=bind,source=' + str(data) + ',target=/pier',
               inputs['containerImage'], '/assets/urbit', '-F', 'zod', '-c', '/pier/zod',
               '-B', '/assets/solid.pill', '-L', '-t', '--loom', '32', timeout=180)
        docker('start', owner)
        ready(lambda: connection.exists() and stat.S_ISSOCK(connection.stat().st_mode))
        execute('control', prefix + suffix)
        report['nativeControlQualified'] = True
        execute('create-groups', prefix + ';<  ~  bind:m  (poke [our.bowl %hood] kiln-merge+!>([%groups our.bowl %base da+now.bowl %init]))  ' + suffix)
        execute('mount-groups', prefix + '=/  =path  [(scot %p our.bowl) %groups (scot %da now.bowl) ~]  ;<  ~  bind:m  (poke [our.bowl %hood] kiln-mount+!>([path %groups]))  ' + suffix)
        mounted = data / 'zod/groups'
        ready(lambda: (mounted / 'sys.kelvin').is_file())
        assert mounted.resolve() == data.resolve() / 'zod/groups' and not mounted.is_symlink()
        # Replace only this run's synthetic mounted desk, preserving its mount root.
        for child in mounted.iterdir():
            assert child.resolve().is_relative_to(mounted.resolve()) and not child.is_symlink()
            if child.is_dir():
                shutil.rmtree(child)
            else:
                child.unlink()
        shutil.copytree(desk, mounted, dirs_exist_ok=True)
        execute('commit-groups', prefix + ';<  ~  bind:m  (poke [our.bowl %hood] kiln-commit+!>([%groups |]))  ' + suffix, timeout=600)
        execute('verify-source', prefix + ';<  exists=?  bind:m  (scry ? %cu /groups/app/contacts/hoon)  ?>  exists  ' + suffix)
        report['currentSourceCommitted'] = True
        execute('install-groups', prefix + ';<  ~  bind:m  (poke [our.bowl %hood] kiln-install+!>([%groups our.bowl %groups]))  ' + suffix, timeout=600)
        # Await one native predicate within a single operation, not repeated pokes.
        body = prefix + '=|  attempts=@ud  |-  ?:  .^(? %gu /(scot %p our.bowl)/contacts/(scot %da now.bowl)/$)  ' + suffix
        body += '  ?>  (lth attempts 120)  ;<  ~  bind:m  (sleep ~s1)  $(attempts +(attempts))'
        execute('contacts-ready', body, timeout=180)
        report['contactsAgentInitialized'] = True
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


if __name__ == '__main__':
    main()
