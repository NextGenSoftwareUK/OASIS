#!/usr/bin/env bash
set -euo pipefail

# One approved, reusable runtime. Never unpack the whole multi-tool release.
runtime_root=/var/tmp/oasis-runtimes/sui-1.81.1
archive="$runtime_root/runtime.tgz"
expected_sha=d921910fdedf5ae2bc3f08a7236c57819a457ce53a8cc60c85f3653c7831b5d4
release_url=https://github.com/MystenLabs/sui/releases/download/mainnet-v1.81.1/sui-mainnet-v1.81.1-ubuntu-x86_64.tgz
mkdir -p "$runtime_root"
if [[ "$(realpath "$runtime_root")" != /var/tmp/oasis-runtimes/sui-1.81.1 ]]; then
    echo 'Unexpected resolved Sui runtime directory' >&2
    exit 1
fi
exec 9>"$runtime_root/install.lock"
flock -n 9 || { echo 'Sui runtime installation is already active' >&2; exit 1; }
verify_runtime() {
    version=$("$runtime_root/sui" --version)
    [[ "$version" == "sui 1.81.1-"* ]] || { echo 'Installed Sui does not match the approved version' >&2; exit 1; }
    echo "$version"
}
cleanup_archive() {
    if [[ -f "$archive" ]]; then
        [[ "$(realpath "$archive")" == /var/tmp/oasis-runtimes/sui-1.81.1/runtime.tgz ]]
        [[ "$(sha256sum "$archive" | cut -d ' ' -f 1)" == "$expected_sha" ]]
        rm -- "$archive"
    fi
}
if [[ -x "$runtime_root/sui" ]]; then
    verify_runtime
    cleanup_archive
    exit 0
fi
df -h "$runtime_root"
# Resume the same immutable release archive after an interrupted download.
cached_sha=''
if [[ -f "$archive" ]]; then cached_sha=$(sha256sum "$archive" | cut -d ' ' -f 1); fi
if [[ "$cached_sha" != "$expected_sha" ]]; then
    curl --fail --silent --show-error --location --continue-at - --output "$archive" "$release_url"
fi
actual_sha=$(sha256sum "$archive" | cut -d ' ' -f 1)
[[ "$actual_sha" == "$expected_sha" ]] || { echo 'Official Sui release digest mismatch' >&2; exit 1; }
member=$(tar -tzf "$archive" | awk '/(^|\/)sui$/ {print}')
[[ -n "$member" && "$member" != *$'\n'* && "$member" != /* && "/$member/" != */../* ]] \
    || { echo 'Release must contain exactly one safe sui executable member' >&2; exit 1; }
components=$(awk -F/ '{print NF-1}' <<< "$member")
tar -xzf "$archive" --no-same-owner --directory "$runtime_root" --strip-components="$components" "$member"
chmod +x "$runtime_root/sui"
verify_runtime
# Delete only the validated, owned download archive after successful extraction.
cleanup_archive
du -h "$runtime_root/sui"
df -h "$runtime_root"
