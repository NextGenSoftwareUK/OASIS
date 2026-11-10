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
    echo "usage: build.sh oasis" >&2
    exit 2
fi

cd -- "$(dirname -- "${BASH_SOURCE[0]}")" || exit 1

echo ">>> Building $contract contract..."

CDT_CPP="${CDT_CPP:-$(command -v cdt-cpp || command -v eosio-cpp || true)}"
if [[ -z "$CDT_CPP" ]]; then
    echo "Antelope CDT compiler not found. Install the official Antelope CDT or set CDT_CPP."
    exit 1
fi

mkdir -p "./build/$contract" || exit 1
"$CDT_CPP" -I="./contracts/$contract/include/" -R="./contracts/$contract/resources" -o="./build/$contract/$contract.wasm" -contract="$contract" -abigen "./contracts/$contract/src/$contract.cpp"
