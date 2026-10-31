#!/usr/bin/env bash
set -euo pipefail
# Existing TRE ledger only: configuration was finalized on its first startup.
[[ -s /tron/FullNode/fullnode.conf && -d /tron/FullNode/output-directory ]] || {
  echo 'Retained TRON configuration/ledger missing; refusing initialization' >&2; exit 1;
}
cd /tron/FullNode
exec java -XX:+UseContainerSupport -jar FullNode.jar -c fullnode.conf --debug --p2p-disable true --witness
