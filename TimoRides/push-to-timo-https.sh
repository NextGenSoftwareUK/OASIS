#!/usr/bin/env bash

# Alternative push script using HTTPS (no SSH passphrase needed)
# Requires GitHub Personal Access Token instead

set -euo pipefail

usage() {
  cat <<'EOF'
Usage:
  ./TimoRides/push-to-timo-https.sh backend [branch] [token]
  ./TimoRides/push-to-timo-https.sh android [branch] [token]

Defaults:
  branch -> main
  token -> read from GITHUB_TOKEN env var or prompt

This script uses HTTPS instead of SSH, so no passphrase needed.
You'll need a GitHub Personal Access Token with 'repo' permissions.

Get token from: https://github.com/settings/tokens
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
    REMOTE="https://github.com/timo-org/ride-scheduler-be.git"
    ;;
  android)
    PREFIX="TimoRides/Timo-Android-App"
    REMOTE="https://github.com/timo-org/Timo-Android-App.git"
    ;;
  *)
    echo "Unknown target '${TARGET}'. Use 'backend' or 'android'."
    usage
    exit 1
    ;;
esac

# Get token
if [[ -n "${3:-}" ]]; then
  GITHUB_TOKEN="${3}"
elif [[ -n "${GITHUB_TOKEN:-}" ]]; then
  GITHUB_TOKEN="${GITHUB_TOKEN}"
else
  echo "Enter your GitHub Personal Access Token:"
  read -s GITHUB_TOKEN
  echo ""
fi

if [[ -z "${GITHUB_TOKEN}" ]]; then
  echo "Error: GitHub token required"
  echo "Get one from: https://github.com/settings/tokens"
  exit 1
fi

# Embed token in URL (use token as username, password not needed)
case "${TARGET}" in
  backend)
    REMOTE_WITH_TOKEN="https://${GITHUB_TOKEN}@github.com/timo-org/ride-scheduler-be.git"
    ;;
  android)
    REMOTE_WITH_TOKEN="https://${GITHUB_TOKEN}@github.com/timo-org/Timo-Android-App.git"
    ;;
esac

if [[ ! -d "${PREFIX}" ]]; then
  echo "Path '${PREFIX}' does not exist. Run from repository root."
  exit 1
fi

echo "Creating subtree split for '${PREFIX}'..."
COMMIT_HASH=$(git subtree split --prefix="${PREFIX}" HEAD)

echo "Pushing ${PREFIX} (${COMMIT_HASH}) to ${REMOTE} branch ${BRANCH}..."
# Use force-with-lease for safety (allows overwrite if remote hasn't changed)
git push "${REMOTE_WITH_TOKEN}" "${COMMIT_HASH}:${BRANCH}" --force-with-lease || git push "${REMOTE_WITH_TOKEN}" "${COMMIT_HASH}:${BRANCH}" --force

echo "Done."

