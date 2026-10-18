#!/usr/bin/env bash
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
runtime="${repo}/TestResults/AntelopeSeedsRuntime"
spring_version="1.2.2"
spring_sha256="531ed1c965f94dd7732f5e900401f132170769c45b7589819a0ba1336627687a"
spring_deb="${runtime}/antelope-spring_${spring_version}_amd64.deb"
spring_url="https://github.com/AntelopeIO/spring/releases/download/v${spring_version}/antelope-spring_${spring_version}_amd64.deb"
node_pid=""
wallet_pid=""

cleanup() {
    [[ -z "${wallet_pid}" ]] || kill "${wallet_pid}" 2>/dev/null || true
    [[ -z "${node_pid}" ]] || kill "${node_pid}" 2>/dev/null || true
    rm -f /tmp/oasis-seeds-keosd.sock
}
trap cleanup EXIT

mkdir -p "${runtime}"
if [[ ! -f "${spring_deb}" ]]; then
    curl --fail --location --silent --show-error "${spring_url}" --output "${spring_deb}"
fi
echo "${spring_sha256}  ${spring_deb}" | sha256sum --check --status

rm -rf "${runtime}/spring" "${runtime}/node" "${runtime}/wallet"
mkdir -p "${runtime}/spring" "${runtime}/node/data" "${runtime}/node/config" "${runtime}/wallet"
dpkg-deb --extract "${spring_deb}" "${runtime}/spring"
nodeos="${runtime}/spring/usr/bin/nodeos"
cleos="${runtime}/spring/usr/bin/cleos"
keosd="${runtime}/spring/usr/bin/keosd"

"${nodeos}" -e -p eosio \
    --data-dir "${runtime}/node/data" \
    --config-dir "${runtime}/node/config" \
    --plugin eosio::chain_api_plugin \
    --plugin eosio::http_plugin \
    --http-server-address 127.0.0.1:8888 \
    --http-validate-host=false \
    --contracts-console \
    --resource-monitor-not-shutdown-on-threshold-exceeded \
    --signature-provider EOS6MRyAjQq8ud7hVNYcfnVPJqcVpscN5So8BhtHuGYqET5GDW5CV=KEY:5KQwrPbwdL6PhXujxW37FSSQZ1JiwsST4cqQzDeyXtP79zkvFD3 \
    >"${runtime}/nodeos.log" 2>&1 &
node_pid=$!

"${keosd}" \
    --wallet-dir "${runtime}/wallet" \
    --http-server-address 127.0.0.1:8902 \
    --unix-socket-path /tmp/oasis-seeds-keosd.sock \
    >"${runtime}/keosd.log" 2>&1 &
wallet_pid=$!

for _ in $(seq 1 120); do
    node_ready=false
    wallet_ready=false
    head_block="$(curl --silent --fail http://127.0.0.1:8888/v1/chain/get_info -d '{}' 2>/dev/null | python3 -c 'import json,sys; print(json.load(sys.stdin).get("head_block_num", 0))' 2>/dev/null || echo 0)"
    [[ "${head_block}" -gt 1 ]] && node_ready=true
    (echo >/dev/tcp/127.0.0.1/8902) >/dev/null 2>&1 && wallet_ready=true
    if [[ "${node_ready}" == true && "${wallet_ready}" == true ]]; then break; fi
    sleep 0.5
done
curl --silent --fail http://127.0.0.1:8888/v1/chain/get_info -d '{}' >/dev/null
(echo >/dev/tcp/127.0.0.1/8902) >/dev/null 2>&1

"${cleos}" --wallet-url http://127.0.0.1:8902 wallet create --name test --file "${runtime}/wallet-password.txt"
"${cleos}" --wallet-url http://127.0.0.1:8902 wallet import --name test --private-key 5KQwrPbwdL6PhXujxW37FSSQZ1JiwsST4cqQzDeyXtP79zkvFD3
contract="${repo}/Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EOSIOOASIS/SmartContracts/build/oasis"
"${cleos}" --url http://127.0.0.1:8888 --wallet-url http://127.0.0.1:8902 set contract eosio "${contract}" oasis.wasm oasis.abi -p eosio@active

export OASIS_ANTELOPE_ENDPOINT="http://127.0.0.1:8888/"
export OASIS_ANTELOPE_ACCOUNT="eosio"
export OASIS_ANTELOPE_CHAIN_ID="$(curl --silent --fail http://127.0.0.1:8888/v1/chain/get_info -d '{}' | python3 -c 'import json,sys; print(json.load(sys.stdin)["chain_id"])')"
export OASIS_ANTELOPE_PRIVATE_KEY="5KQwrPbwdL6PhXujxW37FSSQZ1JiwsST4cqQzDeyXtP79zkvFD3"
export WSLENV="${WSLENV:+${WSLENV}:}OASIS_ANTELOPE_ENDPOINT:OASIS_ANTELOPE_ACCOUNT:OASIS_ANTELOPE_CHAIN_ID:OASIS_ANTELOPE_PRIVATE_KEY"
project="$(wslpath -w "${repo}/Providers/Other/TestProjects/NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.IntegrationTests.csproj")"
"/mnt/c/Program Files/dotnet/dotnet.exe" test "${project}" -c Release --logger "trx;LogFileName=seeds.trx" --results-directory "$(wslpath -w "${runtime}")" -v:minimal
