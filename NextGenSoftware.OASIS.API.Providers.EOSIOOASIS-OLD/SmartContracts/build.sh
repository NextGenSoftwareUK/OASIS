#! /bin/bash

#contract
if [[ "$1" == "oasis" ]]; then
    contract=oasis
else
    echo "need contract"
    exit 0
fi

echo ">>> Building $contract contract..."

# eosio.cdt v1.6.1
# -contract=<string>       - Contract name
# -o=<string>              - Write output to <file>
# -abigen                  - Generate ABI
# -I=<string>              - Add directory to include search path
# -L=<string>              - Add directory to library search path
# -R=<string>              - Add a resource path for inclusion

eosio-cpp -I="./contracts/$contract/include/" -R="./contracts/$contract/resources" -o="./build/$contract/$contract.wasm" -contract="$contract" -abigen ./contracts/$contract/src/$contract.cpp