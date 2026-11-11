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

source_root="$(pwd -P)/contracts/$contract"
output_root="$(pwd -P)/build/$contract"
mkdir -p "$output_root" || exit 1
# CDT looks up its generated dispatcher by the absolute source filename.
"$CDT_CPP" -I "$source_root/include" -R "$source_root/resources" -o "$output_root/$contract.wasm" -contract "$contract" -abigen "$source_root/src/$contract.cpp" || exit 1

# The managed provider requires all three avatar-detail mutation actions.
python3 - "$output_root/$contract.abi" <<'PY'
import json
import sys

with open(sys.argv[1], encoding="utf-8") as source:
    abi = json.load(source)
required = {"setdetail", "softdetail", "harddetail"}
missing = required - {action["name"] for action in abi["actions"]}
if missing:
    raise SystemExit("Generated ABI is missing actions: " + ", ".join(sorted(missing)))
PY
