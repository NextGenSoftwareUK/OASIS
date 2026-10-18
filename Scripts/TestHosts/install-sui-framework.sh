#!/usr/bin/env bash
set -euo pipefail
framework=/var/tmp/oasis-runtimes/sui-1.81.1/framework
revision=e04cf9aa915cb6c069092733bebc3648755998e2
mkdir -p "$framework"
[[ "$(realpath "$framework")" == /var/tmp/oasis-runtimes/sui-1.81.1/framework ]]
exec 9>"$framework/install.lock"
flock -n 9 || { echo 'Sui framework installation is active' >&2; exit 1; }
if [[ ! -f "$framework/sui-framework/Move.toml" || ! -f "$framework/move-stdlib/Move.toml" ]]; then
    # Stream the official pinned source archive; retain only the two build dependencies.
    curl --fail --silent --show-error --location "https://codeload.github.com/MystenLabs/sui/tar.gz/$revision" \
        | tar -xz --no-same-owner --directory "$framework" --strip-components=4 --wildcards \
            '*/crates/sui-framework/packages/sui-framework/*' '*/crates/sui-framework/packages/move-stdlib/*'
fi
[[ -f "$framework/sui-framework/Move.toml" && -f "$framework/move-stdlib/Move.toml" ]]
du -sh "$framework"
