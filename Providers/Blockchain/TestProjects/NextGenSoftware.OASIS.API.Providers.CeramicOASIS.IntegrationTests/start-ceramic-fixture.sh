# Starts the disposable native Ceramic fixture for CeramicNativeStorageConformanceTests. Source it from bash with the
# repository root as the working directory and $fixture set to an existing "$RUNNER_TEMP/oasis-native.*" directory:
#   source "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CeramicOASIS.IntegrationTests/start-ceramic-fixture.sh"
# It sets $container (the ceramic-one container id) and $relays (the storage service pids) for the caller's cleanup, and
# exports OASIS_CERAMIC_STORAGE_TOKEN, OASIS_CERAMIC_ASYNC_SERVICE and OASIS_CERAMIC_SYNC_SERVICE.
#
# ceramic-one 0.58.0 (the current rust-ceramic release). 3Box Labs no longer publishes the ceramic-one image to
# public.ecr.aws, so this pins the community build of the same release by digest.
ceramic_image="docker.io/dclimate/ceramic-one@sha256:0fcad81010643b32a62ea7e1891842cbfaa880e92b54ff56eec57a5dfd095ef4"
ceramic_url="http://127.0.0.1:15101"
service_dir="Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CeramicOASIS/StorageService"

[[ -n "${fixture:-}" && -d "$fixture" && "$fixture" == "$RUNNER_TEMP"/oasis-native.* ]] || { echo "\$fixture must be an existing \$RUNNER_TEMP/oasis-native.* directory"; return 1; }
[[ -f "$service_dir/storage-service.mjs" ]] || { echo "Run from the repository root"; return 1; }

ceramic_wait() { for attempt in $(seq 1 90); do if eval "$1" >/dev/null 2>&1; then return 0; fi; sleep 2; done; echo "Never became ready: $1"; return 1; }

container="$(docker run -d -p 127.0.0.1:15101:5101 "$ceramic_image" --network in-memory --bind-address 0.0.0.0:5101 \
  --store-dir /data --p2p-key-dir /data)" || return 1
ceramic_wait "curl -fsS $ceramic_url/ceramic/liveness" || return 1

(cd "$service_dir" && npm ci --no-audit --no-fund --ignore-scripts) || return 1
export OASIS_CERAMIC_STORAGE_TOKEN="$(openssl rand -hex 24)"
relays=""
for mode in async sync; do
  port=$([[ $mode == async ]] && echo 18931 || echo 18932)
  node "$service_dir/storage-service.mjs" "$fixture/ceramic-$mode" "$port" "$ceramic_url" > "$fixture/ceramic-$mode.log" 2>&1 &
  relays="$relays $!"
  ceramic_wait "curl -fsS -H 'Authorization: Bearer $OASIS_CERAMIC_STORAGE_TOKEN' http://127.0.0.1:$port/health" || { cat "$fixture/ceramic-$mode.log"; return 1; }
done
export OASIS_CERAMIC_ASYNC_SERVICE="http://127.0.0.1:18931" OASIS_CERAMIC_SYNC_SERVICE="http://127.0.0.1:18932"
