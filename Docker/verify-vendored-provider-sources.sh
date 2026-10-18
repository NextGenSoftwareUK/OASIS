#!/usr/bin/env sh
set -eu
if [ "$#" -ne 1 ]; then
    echo 'Usage: verify-vendored-provider-sources.sh <source-root>' >&2
    exit 2
fi
client="$1/External Libs/net-ipfs-http-client-master/src"
# This source ProjectReference is required by IPFSOASIS; an absent project can be skipped
# by restore and only surface later as missing Ipfs.Http types. Fail before costly restore.
for source in IpfsHttpClient.csproj IpfsClient.cs CoreApi/FileSystemApi.cs; do
    if [ ! -s "${client}/${source}" ]; then
        echo "Required vendored IPFS client source missing: ${client}/${source}. Check the Docker build context; do not substitute a different SDK." >&2
        exit 1
    fi
done
echo 'Required vendored IPFS client source is present.'
