"""One owner for composing qualified readers and publishing their guarded settings."""
import importlib.util
import os
from pathlib import Path


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class PublicReaderBatch:
    def __init__(self, prefix, publisher_names):
        self.prefix = prefix
        self.publishers = tuple(load(name) for name in publisher_names)
        if not self.publishers:
            raise ValueError('An explicit nonempty reader batch is required.')
        self.configuration = self.publishers[0].configuration
        self.keys = tuple(dict.fromkeys(key for publisher in self.publishers for key in publisher.KEYS))
        self.expected_keys = len(self.publishers) * 2 + 1

    def candidate(self, before):
        after = before
        for publisher in self.publishers:
            after = publisher.candidate(after, publisher.qualified_source())
        changed = {key for key in before.keys() | after.keys() if before.get(key) != after.get(key)}
        if changed != set(self.keys) or len(self.keys) != self.expected_keys:
            raise RuntimeError('Exact source/preset partitions and one shared admission are required.')
        if any(key not in after or after[key] != value for key, value in before.items() if key not in self.keys):
            raise RuntimeError('Existing settings changed outside the reviewed batch.')
        if any(len(after[key].encode()) > 32768 for key in self.keys):
            raise RuntimeError('A composed setting exceeds its byte budget.')
        return after

    def prepare(self):
        configuration = self.configuration
        before = configuration.variables()
        after = self.candidate(before)
        configuration.STATE.mkdir(parents=True, exist_ok=True)
        for label, value in (('before', before), ('candidate', after)):
            name = f'{self.prefix}-{label}-variables.dpapi'
            if (configuration.STATE / name).exists():
                if configuration.protected_read(name) != value:
                    raise RuntimeError('Existing batch preparation differs; reconcile before continuing.')
            else:
                configuration.write_new(configuration.STATE / name,
                    configuration.verification.protected_token(configuration.compact(value).encode()))
        print(f'{len(self.publishers)} qualified readers composed into {len(self.keys)} protected settings; no service mutation.')

    def apply(self):
        configuration = self.configuration
        before = configuration.protected_read(self.prefix + '-before-variables.dpapi')
        after = configuration.protected_read(self.prefix + '-candidate-variables.dpapi')
        if self.candidate(before) != after:
            raise RuntimeError('Protected batch differs from original qualified sources.')
        guard = load('Prepare-TenSourcePublicBatch')
        release_ref = os.environ.get('COSMIC_CQL_REVIEWED_RELEASE_REF', 'HEAD')
        deployment = guard.require_matching_release(configuration, release_ref=release_ref)

        def require_release():
            if guard.require_matching_release(configuration, release_ref=release_ref) != deployment:
                raise RuntimeError('Reviewed batch deployment changed before settings mutation.')

        configuration.apply_values(self.prefix, self.keys, reviewed_snapshots=(before, after), before_mutation=require_release)
