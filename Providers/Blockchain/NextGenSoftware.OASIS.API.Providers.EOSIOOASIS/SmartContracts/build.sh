#! /bin/bash

#contract

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