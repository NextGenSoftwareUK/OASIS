#!/bin/bash
# Example: ./deploy.sh development true

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
