#!/usr/bin/env bash
# Linux: build and deploy STAR API. Forwards to cross-platform Unix script.
# Usage: ./build-and-deploy-star-api-linux.sh [ -ForceBuild ]
exec "$(dirname "${BASH_SOURCE[0]}")/build-and-deploy-star-api-unix.sh" "$@"
