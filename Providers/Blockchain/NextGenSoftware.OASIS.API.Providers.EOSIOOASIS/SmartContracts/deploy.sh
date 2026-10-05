#! /bin/bash

#contract
if [[ "$1" == "oasis" ]]; then
    contract=oasis
else
    echo "need contract"
    exit 0
fi

#oasis account
account=$2

#network
if [[ "$3" == "mainnet" ]]; then 
    url=http://api.tlos.goodblock.io
    network="Telos Mainnet"
elif [[ "$3" == "testnet" ]]; then
    url=https://testnet.telos.caleos.io
    network="Telos Testnet"
elif [[ "$3" == "local" ]]; then
    url=http://127.0.0.1:8888
    network="Local"
else
    echo "need network"
    exit 0
fi

echo ">>> Deploying $contract contract to $account on $network..."

# eosio v1.8.0
cleos -u $url set contract $account ./build/$contract/ $contract.wasm $contract.abi -p $account