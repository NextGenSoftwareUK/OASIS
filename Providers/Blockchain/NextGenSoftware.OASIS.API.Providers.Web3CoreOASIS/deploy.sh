#!/bin/bash
# Example: ./deploy.sh development true


# OASIS: pause before exit when run from GUI (CI: OASIS_SCRIPT_NO_PAUSE=1)
if [[ "${OASIS_SCRIPT_NO_PAUSE:-}" != "1" ]]; then
  _OASIS_TD="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
  while [[ "$_OASIS_TD" != "/" ]]; do
    if [[ -f "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh" ]]; then
      # shellcheck disable=SC1091
      source "$_OASIS_TD/Scripts/include/pause_on_exit.inc.sh"
      break
    fi
    _OASIS_TD="$(dirname "$_OASIS_TD")"
  done
fi

NETWORK=$1
RUN_TESTS=$2

if [ -z "$NETWORK" ]; then
  echo "Usage: ./deploy.sh [network] [run_tests]"
  exit 1
fi

if [ "$NETWORK" == "development" ]; then
  npx hardhat node &
  HARDHAT_NODE_PID=$!
  sleep 5
fi

npx hardhat run --network $NETWORK scripts/deploy.js

if [ "$RUN_TESTS" == "true" ]; then
  npx hardhat test --network $NETWORK
fi

if [ "$NETWORK" == "development" ]; then
  kill $HARDHAT_NODE_PID
fi
