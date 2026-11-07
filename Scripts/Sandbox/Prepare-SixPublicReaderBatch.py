"""Compose explicitly qualified public readers; apply only after exact release qualification."""
import sys
from HostedPublicReaderBatch import PublicReaderBatch, load

BATCH = PublicReaderBatch('six-public-readers', ('Publish-HostedMidenPublicConfiguration', 'Publish-HostedEspressoPublicConfiguration', 'Publish-HostedEigenLayerPublicConfiguration', 'Publish-HostedFhenixPublicConfiguration', 'Publish-HostedAxelarBlockConfiguration', 'Publish-HostedRitualBlockConfiguration'))
PUBLISHERS = BATCH.publishers
configuration = BATCH.configuration
PREFIX = BATCH.prefix
KEYS = BATCH.keys
candidate = BATCH.candidate
prepare = BATCH.prepare
apply = BATCH.apply

if __name__ == '__main__':
    if sys.argv[1:] == ['prepare']:
        prepare()
    elif sys.argv[1:] == ['apply']:
        apply()
    else:
        raise SystemExit('Use prepare, or apply only after the exact batch image/API qualification.')
