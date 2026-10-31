#!/usr/bin/env sh
set -eu
if [ "$#" -ne 1 ]; then
    echo 'Usage: verify-vendored-provider-sources.sh <source-root>' >&2
    exit 2
fi
client="$1/External Libs/net-ipfs-http-client-master/src"
if [ ! -s "$1/External Libs/IpfsSdk.AppReferences.props" ]; then
    echo 'Required IPFS executable SDK composition is missing.' >&2
    exit 1
fi
# This source ProjectReference is required by IPFSOASIS; an absent project can be skipped
# by restore and only surface later as missing Ipfs.Http types. Fail before costly restore.
for source in IpfsHttpClient.csproj IpfsClient.cs CoreApi/FileSystemApi.cs; do
    if [ ! -s "${client}/${source}" ]; then
        echo "Required vendored IPFS client source missing: ${client}/${source}. Check the Docker build context; do not substitute a different SDK." >&2
        exit 1
    fi
done
core="$1/External Libs/net-ipfs-core-0.55.0"
for source in src/IpfsCore.csproj src/Cid.cs src/Base58.cs RuntimeDependencies.props LICENSE; do
    if [ ! -s "${core}/${source}" ]; then
        echo "Required source-built IPFS core missing: ${core}/${source}. Restore the pinned source; do not substitute the incompatible NuGet binary." >&2
        exit 1
    fi
done
echo 'Required vendored IPFS client source is present.'
