#!/usr/bin/env bash

set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  ./TimoRides/push-to-timo.sh backend [branch] [remote]
  ./TimoRides/push-to-timo.sh android [branch] [remote]

Defaults:
  branch -> main
  backend remote -> git@github.com:timo-org/ride-scheduler-be.git
  android remote -> git@github.com:timo-org/Timo-Android-App.git

The script creates a temporary subtree split of the requested project and
pushes it to the provided remote without altering the current working tree.
EOF
}

if [[ $# -lt 1 ]]; then
  usage
  exit 1
fi

TARGET="${1}"
BRANCH="${2:-main}"

case "${TARGET}" in
  backend)
    PREFIX="TimoRides/ride-scheduler-be"
    REMOTE="${3:-git@github.com:timo-org/ride-scheduler-be.git}"
    ;;
  android)
    PREFIX="TimoRides/Timo-Android-App"
    REMOTE="${3:-git@github.com:timo-org/Timo-Android-App.git}"
    ;;
  *)
    echo "Unknown target '${TARGET}'. Use 'backend' or 'android'."
    usage
    exit 1
    ;;
esac

if [[ ! -d "${PREFIX}" ]]; then
  echo "Path '${PREFIX}' does not exist. Run from repository root."
  exit 1
fi

echo "Creating subtree split for '${PREFIX}'..."
COMMIT_HASH=$(git subtree split --prefix="${PREFIX}" HEAD)

echo "Pushing ${PREFIX} (${COMMIT_HASH}) to ${REMOTE} branch ${BRANCH}..."
git push "${REMOTE}" "${COMMIT_HASH}:${BRANCH}"

echo "Done."

