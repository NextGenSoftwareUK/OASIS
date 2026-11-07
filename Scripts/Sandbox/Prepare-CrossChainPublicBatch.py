"""Compose retained qualified public readers into one reviewed Development batch."""
import importlib.util
import os
from pathlib import Path
import sys


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


PUBLISHERS = tuple(load('Publish-Hosted' + name + 'PublicConfiguration') for name in
    ('LayerZeroV2', 'Stargate', 'DeBridge', 'Synapse', 'Connext'))
configuration = PUBLISHERS[0].configuration
PREFIX = 'cross-chain-five-public'
KEYS = tuple(dict.fromkeys(key for publisher in PUBLISHERS for key in publisher.KEYS))


def candidate(before):
    after = before
    for publisher in PUBLISHERS:
        after = publisher.candidate(after, publisher.qualified_source())
    changed = {key for key in before.keys() | after.keys() if before.get(key) != after.get(key)}
    if changed != set(KEYS) or len(KEYS) != 11:
        raise RuntimeError('Exactly eleven settings must compose five qualified readers.')
    if any(key not in after or after[key] != value for key, value in before.items() if key not in KEYS):
        raise RuntimeError('Existing settings changed outside the reviewed batch.')
    if any(len(after[key].encode()) > 32768 for key in KEYS):
        raise RuntimeError('A composed setting exceeds its byte budget.')
    return after


def prepare():
    before = configuration.variables()
    after = candidate(before)
    configuration.STATE.mkdir(parents=True, exist_ok=True)
    for label, value in (('before', before), ('candidate', after)):
        name = f'{PREFIX}-{label}-variables.dpapi'
        if (configuration.STATE / name).exists():
            if configuration.protected_read(name) != value:
                raise RuntimeError('Existing batch preparation differs; reconcile before continuing.')
        else:
            configuration.write_new(configuration.STATE / name,
                configuration.verification.protected_token(configuration.compact(value).encode()))
    print('Five qualified public readers composed into eleven protected settings; no service mutation.')


def apply():
    before = configuration.protected_read(PREFIX + '-before-variables.dpapi')
    after = configuration.protected_read(PREFIX + '-candidate-variables.dpapi')
    if candidate(before) != after:
        raise RuntimeError('Protected batch differs from original qualified sources.')
    guard = load('Prepare-TenSourcePublicBatch')
    release_ref = os.environ.get('COSMIC_CQL_REVIEWED_RELEASE_REF', 'HEAD')
    deployment = guard.require_matching_release(configuration, release_ref=release_ref)

    def require_release():
        if guard.require_matching_release(configuration, release_ref=release_ref) != deployment:
            raise RuntimeError('Reviewed batch deployment changed before settings mutation.')

    configuration.apply_values(PREFIX, KEYS, reviewed_snapshots=(before, after), before_mutation=require_release)


if __name__ == '__main__':
    if sys.argv[1:] == ['prepare']:
        prepare()
    elif sys.argv[1:] == ['apply']:
        apply()
    else:
        raise SystemExit('Use prepare, or apply only after the exact batch image/API qualification.')
