#!/usr/bin/env sh
set -eu

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <dependency-manifest>" >&2
    exit 2
fi

manifest="$1"
if [ ! -f "${manifest}" ]; then
    echo "Dependency manifest not found: ${manifest}" >&2
    exit 2
fi

# shellcheck disable=SC1090
. "${manifest}"

# Railway receives the parent repository without populated private submodules.
# Check out the exact revisions tested together instead of each repository's
# moving default branch, which can produce an internally inconsistent build.
clone_at() {
    repository="$1"
    destination="$2"
    revision="$3"
    git clone --no-checkout "https://${GITHUB_PAT}@github.com/NextGenSoftwareUK/${repository}.git" "${destination}"
    git -C "${destination}" checkout --detach "${revision}"
}

rm -rf \
    "OASIS Architecture/NextGenSoftware.OASIS.API.Core" \
    "ONODE/NextGenSoftware.OASIS.API.ONODE.Core" \
    "ONODE/ONODEManager" \
    "STAR ODK" \
    "WEB6" \
    "OASIS Omniverse/OGEngineClient" \
    "HoloNET-ORM"

clone_at OASIS-API-Core "OASIS Architecture/NextGenSoftware.OASIS.API.Core" "${OASIS_API_CORE_COMMIT}"
clone_at OASIS-ONODE-Core "ONODE/NextGenSoftware.OASIS.API.ONODE.Core" "${OASIS_ONODE_CORE_COMMIT}"
clone_at ONODEManager "ONODE/ONODEManager" "${ONODE_MANAGER_COMMIT}"
clone_at STAR-ODK "STAR ODK" "${STAR_ODK_COMMIT}"
clone_at OASIS-WEB6 "WEB6" "${OASIS_WEB6_COMMIT}"
clone_at OGEngineClient "OASIS Omniverse/OGEngineClient" "${OGENGINE_CLIENT_COMMIT}"
clone_at HoloNET-ORM "HoloNET-ORM" "${HOLONET_ORM_COMMIT}"
