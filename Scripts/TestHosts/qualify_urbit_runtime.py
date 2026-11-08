"""Pin/cache official runtime assets and cold-boot one offline owned development ship."""
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import platform
import re
import signal
import shutil
import subprocess
import tarfile
import time
import urllib.error
import urllib.parse
import urllib.request

from owned_docker_resources import missing_resource


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


def download_release(url, target, size, expected):
    if target.exists():
        assert target.stat().st_size == size and digest(target) == expected, 'Refuse corrupt cached asset.'
        return
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
    current = url
    response = None
    for _ in range(3):
        parsed = urllib.parse.urlparse(current)
        assert parsed.scheme == 'https' and parsed.username is None and parsed.password is None
        assert parsed.hostname == 'github.com' or parsed.hostname.endswith('.githubusercontent.com')
        try:
            response = opener.open(current, timeout=20)
            break
        except urllib.error.HTTPError as error:
            if error.code not in (301, 302, 303, 307, 308):
                raise
            current = urllib.parse.urljoin(current, error.headers['Location'])
            error.close()
    assert response is not None, 'Release redirect bound exceeded.'
    temporary = target.with_suffix(target.suffix + '.partial')
    assert not temporary.exists(), 'Preserve an unfinished asset observation.'
    try:
        started = time.monotonic()
        count = 0
        with response, temporary.open('xb') as stream:
            assert response.status == 200
            while chunk := response.read(65536):
                count += len(chunk)
                assert count <= size and time.monotonic() - started <= 90, 'Runtime transfer bound exceeded.'
                stream.write(chunk)
        assert count == size and digest(temporary) == expected, 'Runtime release digest differs.'
        temporary.rename(target)
    finally:
        if temporary.exists():
            temporary.unlink()


def main():
    assert os.environ.get('GITHUB_ACTIONS') == 'true' and platform.system() == 'Linux', 'Cloud Linux only.'
    root = Path(__file__).resolve().parents[2]
    inputs = json.loads((Path(__file__).with_name('urbit-runtime-inputs.json')).read_text())
    run, attempt = os.environ['GITHUB_RUN_ID'], os.environ['GITHUB_RUN_ATTEMPT']
    assert run.isdecimal() and attempt.isdecimal()
    owner = 'urbit-runtime-' + run + '-' + attempt
    out = root / 'TestResults/UrbitRuntimeCurrent'
    out.mkdir(parents=True, exist_ok=True)
    cache = root / 'LocalState/COSMIC/CloudUrbitRuntimeAssets/v4.6-408k-2'
    cache.mkdir(parents=True, exist_ok=True)
    report = {'source': os.environ['GITHUB_SHA'], 'owner': owner, 'runtimeTag': inputs['runtimeTag'],
              'kernelRelease': inputs['kernelRelease'], 'kernelCommit': inputs['kernelCommit'],
              'pillSha256': inputs['pillSha256'], 'offlineColdBootPassed': False,
              'contactsQualified': False, 'cqlQualified': False, 'liveDeployment': False}
    with (out / 'intent.json').open('x') as stream:
        json.dump(report, stream, indent=2)
    container, volume = owner, owner + '-data'
    label = 'oasis.urbit.runtime.owner'
    source = out / 'kernel-source'

    def docker(*args, check=True, timeout=60):
        result = subprocess.run(['docker', *args], capture_output=True, text=True, timeout=timeout)
        if check and result.returncode:
            raise RuntimeError('Owned runtime Docker operation failed: ' + result.stderr[-1000:])
        return result

    def inspect(kind, name):
        result = docker(kind, 'inspect', name, check=False)
        if missing_resource(kind, name, result.returncode, result.stderr):
            return None
        if result.returncode:
            raise RuntimeError('Resource state unknown; never infer absence.')
        state = json.loads(result.stdout)[0]
        labels = state['Config']['Labels'] if kind == 'container' else state['Labels']
        assert labels.get(label) == owner, 'Never adopt or remove another resource.'
        return state

    touched = False
    try:
        archive = cache / 'linux-x86_64.tgz'
        download_release(inputs['runtimeArchiveUrl'], archive, inputs['runtimeArchiveBytes'], inputs['runtimeArchiveSha256'])
        with open(os.environ['GITHUB_OUTPUT'], 'a') as stream:
            stream.write('archive_ready=true\n')
        binary = cache / 'urbit'
        with tarfile.open(archive, 'r:gz') as package:
            members = package.getmembers()
            assert len(members) <= 64
            report['archiveMembers'] = [{'name': m.name, 'bytes': m.size} for m in members]
            selected = []
            for member in members:
                path = PurePosixPath(member.name)
                assert not path.is_absolute() and '..' not in path.parts
                assert member.isfile() or member.isdir(), 'No archive links or special files.'
                if member.isfile():
                    with package.extractfile(member) as payload:
                        header = payload.read(20)
                    # One actual x86-64 little-endian ELF executable, independent
                    # of the vendor's archive filename. No script/name fallback.
                    if len(header) == 20 and header[:6] == b'\x7fELF\x02\x01' and int.from_bytes(header[18:20], 'little') == 62:
                        selected.append(member)
            assert len(selected) == 1 and selected[0].size <= 64 * 1024 * 1024, 'Exactly one bounded native executable required.'
            report['selectedExecutable'] = selected[0].name
            with package.extractfile(selected[0]) as payload:
                expected_binary = hashlib.file_digest(payload, 'sha256').hexdigest()
            if binary.exists():
                assert digest(binary) == expected_binary, 'Refuse changed cached executable.'
            else:
                with package.extractfile(selected[0]) as stream, binary.open('xb') as output:
                    shutil.copyfileobj(stream, output)
                binary.chmod(0o755)
        report['binarySha256'] = digest(binary)
        version = subprocess.run([str(binary), '-R'], capture_output=True, text=True, timeout=20, check=True)
        assert re.search(r'^urbit 4\.6\s*$', version.stdout, re.MULTILINE), 'Release runtime version differs.'
        (out / 'runtime-version.txt').write_text(version.stdout)
        pill = cache / 'solid.pill'
        if not pill.exists():
            assert not source.exists(), 'Do not overwrite dependency checkout.'
            environment = dict(os.environ, GIT_LFS_SKIP_SMUDGE='1', GIT_TERMINAL_PROMPT='0')
            def git(*args, timeout=120, check=True, input_text=None, output=None):
                command = ['git', '-c', 'lfs.transfer.maxretries=0', '-C', str(source), *args]
                command_environment = dict(environment)
                if args[:2] == ('lfs', 'smudge'):
                    command_environment['GIT_LFS_SKIP_SMUDGE'] = '0'
                with subprocess.Popen(command, env=command_environment, stdout=output if output is not None else subprocess.PIPE,
                                      stdin=subprocess.PIPE if input_text is not None else subprocess.DEVNULL,
                                      stderr=subprocess.PIPE, text=True, start_new_session=True) as process:
                    try:
                        stdout, stderr = process.communicate(input=input_text, timeout=timeout)
                    except subprocess.TimeoutExpired:
                        # Git/LFS spawn helpers. Stop the entire owned session before
                        # deleting its checkout, rather than only killing the parent.
                        os.killpg(process.pid, signal.SIGKILL)
                        stdout, stderr = process.communicate()
                        report['timedOutGitOperation'] = list(args)
                        report['timedOutGitDiagnostic'] = stderr[-8192:]
                        report['ownedGitProcessGroupStopped'] = True
                        raise
                    result = subprocess.CompletedProcess(command, process.returncode, stdout, stderr)
                    if check:
                        result.check_returncode()
                    return result
            source.mkdir()
            git('init')
            git('remote', 'add', 'origin', inputs['kernelRepository'])
            git('fetch', '--filter=blob:none', '--depth=1', 'origin', inputs['kernelCommit'])
            assert git('rev-parse', 'FETCH_HEAD').stdout.strip() == inputs['kernelCommit']
            assert git('ls-tree', '--name-only', inputs['kernelCommit'], '--', '.lfsconfig').stdout.strip() == '', 'Review custom LFS distribution before fetching.'
            pointer = git('show', inputs['kernelCommit'] + ':' + inputs['pillPath']).stdout
            assert 'oid sha256:' + inputs['pillSha256'] in pointer and 'size ' + str(inputs['pillBytes']) in pointer
            git('lfs', 'version')
            # Resolve precisely the already-verified pointer. Ref-level LFS fetch
            # scans the repository tree, unnecessary for this one-object contract.
            with (source / 'selected-pill').open('xb') as payload:
                git('lfs', 'smudge', inputs['pillPath'], input_text=pointer, output=payload, timeout=180)
            selected_pill = source / 'selected-pill'
            assert selected_pill.stat().st_size == inputs['pillBytes'] and digest(selected_pill) == inputs['pillSha256']
            oid = inputs['pillSha256']
            object_path = source / '.git/lfs/objects' / oid[:2] / oid[2:4] / oid
            assert object_path.is_file() and object_path.stat().st_size == inputs['pillBytes'] and digest(object_path) == oid
            shutil.copyfile(object_path, pill)
        assert pill.stat().st_size == inputs['pillBytes'] and digest(pill) == inputs['pillSha256']
        report['assetsVerified'] = True
        report['cachedAssetBytes'] = sum(p.stat().st_size for p in cache.iterdir() if p.is_file())
        with open(os.environ['GITHUB_OUTPUT'], 'a') as stream:
            stream.write('assets_ready=true\n')
        assert inspect('container', container) is None and inspect('volume', volume) is None
        touched = True
        docker('volume', 'create', '--label', label + '=' + owner, volume)
        docker('create', '--name', container, '--label', label + '=' + owner,
               '--network', 'none', '--memory', '3g', '--memory-swap', '3g', '--cpus', '2', '--pids-limit', '128',
               '--cap-drop', 'ALL', '--security-opt', 'no-new-privileges', '--read-only',
               '--workdir', '/pier', '--env', 'HOME=/pier',
               '--tmpfs', '/tmp:rw,size=64m,mode=1777',
               '--mount', 'type=bind,source=' + str(cache) + ',target=/assets,readonly',
               '--mount', 'type=volume,source=' + volume + ',target=/pier',
               inputs['containerImage'], '/assets/urbit', '-F', 'zod', '-c', '/pier/zod',
               '-B', '/assets/solid.pill', '-L', '-t', '-x', '--loom', '32', timeout=180)
        docker('start', container)
        wait = docker('wait', container, timeout=600)
        state = inspect('container', container)
        logs = docker('logs', container)
        log = logs.stdout + logs.stderr
        assert len(log.encode()) <= 2 * 1024 * 1024, 'Native boot log exceeded budget.'
        (out / 'boot.log').write_text(log)
        report['exitCode'] = state['State']['ExitCode']
        report['oomKilled'] = state['State']['OOMKilled']
        report['networkMode'] = state['HostConfig']['NetworkMode']
        assert wait.stdout.strip() == '0' and report['exitCode'] == 0 and not report['oomKilled']
        assert report['networkMode'] == 'none'
        report['offlineColdBootPassed'] = True
    finally:
        if touched:
            state = inspect('container', container)
            if state is not None:
                if state['State']['Running']:
                    docker('stop', '--time', '20', container)
                docker('rm', '-v', container)
            if inspect('volume', volume) is not None:
                docker('volume', 'rm', volume)
            assert inspect('container', container) is None and inspect('volume', volume) is None
            report['ownedCleanupVerified'] = True
        if source.exists():
            assert source.resolve().parent == out.resolve() and not source.is_symlink()
            shutil.rmtree(source)
            report['temporarySourceCheckoutRemoved'] = True
        (out / 'qualification.json').write_text(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
