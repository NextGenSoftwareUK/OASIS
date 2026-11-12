"""Actual Urbit + canonical Mongo federation on one owned internal network."""
import re

from owned_urbit_mongo import owned_mongo
from qualify_urbit_contacts_book import load_fixture, qualify_book
from qualify_urbit_contacts_http import native_http
from qualify_urbit_contacts_install import main
from qualify_urbit_web4_native import run_native_case


def federation_phase(context, phase, code, mongo):
    proof = run_native_case(context, phase, code,
        'UrbitNativeWeb4FederationReadsOwnedMongoCopies', 'federation',
        dict(mongoUri=mongo['mongoUri'], mongoDatabase=mongo['database'], owner=mongo['owner']))
    for key, expected in dict(phase=phase, nativeFederationQualified=True, fullCount=5, subsetCount=4,
                              nativePointReadCount=6, unrelatedCanonicalCopyExcluded=True,
                              rejectedRequestsReadNeitherSource=True, sourceDocumentsPreserved=True,
                              copySeedCount=3 if phase == 'beforeRestart' else 0,
                              ownedDatabaseAbsent=phase == 'afterRestart', controlledIdentityAndOutbox=True,
                              productionJwtQualified=False, durableBillingQualified=False,
                              cqlQualified=False, liveDeployment=False).items():
        assert proof[key] == expected, key
    for key in ['canonicalSourceSha256', 'federationProjectionSha256']:
        assert re.fullmatch('[0-9a-f]{64}', proof[key])
    return {key: proof[key] for key in ['nativeFederationQualified', 'canonicalSourceSha256',
                                       'federationProjectionSha256', 'copySeedCount', 'ownedDatabaseAbsent']}


def initialized(context):
    with owned_mongo(context) as mongo:
        qualify_book(context, http_reader=lambda current, phase: native_http(current, phase,
            web4_reader=lambda active, step, code: federation_phase(active, step, code, mongo)))


if __name__ == '__main__':
    main(on_initialized=initialized, http_transport=True, fixture_loader=load_fixture)
