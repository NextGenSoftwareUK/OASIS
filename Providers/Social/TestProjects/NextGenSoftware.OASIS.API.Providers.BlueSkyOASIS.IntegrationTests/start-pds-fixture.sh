#!/usr/bin/env bash
# Starts a disposable, self-contained AT Protocol PDS for the BlueSkyOASIS native storage suite.
#
#   start-pds-fixture.sh <account-password>
#
# Containers (all pinned by digest, sharing one network namespace, nothing federated):
#   postgres  - storage for the local PLC directory
#   plc       - official did:plc directory server (the PDS registers each account's DID here)
#   pds       - official Bluesky PDS on http://127.0.0.1:12583 (no crawlers, no AppView, rate limits on)
# Accounts: oasis-async.test and oasis-sync.test, both with <account-password>, created with
# admin-issued invite codes. Nothing touches the public Bluesky network or plc.directory.
#
# On success prints the container ids (space separated, pds last) on stdout; on failure removes
# everything it started. Restart only the pds container (docker restart <last id>) to check persistence.
set -euo pipefail

password="${1:?usage: start-pds-fixture.sh <account-password>}"
POSTGRES_IMAGE="docker.io/library/postgres@sha256:f9cf3e1f0066f0eb0c48707d864f0ac5941e9ff454a072077d97a4d278d07d73" # 17-alpine
PLC_IMAGE="ghcr.io/bluesky-social/did-method-plc@sha256:c78a4e972c356610851078757f1ebe4f359634c24909209d740c1dbe74395b83"
PDS_IMAGE="ghcr.io/bluesky-social/pds@sha256:b7dcc38b5ec26d03c096c90d4385c4b29c4fdd22daf3b0b36e159f61a81d55c0" # 0.4 (@atproto/pds 0.5.65)
PDS="http://127.0.0.1:12583"

started=()
fail() {
  echo "PDS fixture failed: $*" >&2
  for id in "${started[@]}"; do docker logs --tail 30 "$id" >&2 || true; done
  [[ ${#started[@]} -gt 0 ]] && docker rm -fv "${started[@]}" >/dev/null
  exit 1
}
wait_for() { for _ in $(seq 1 90); do if eval "$1" >/dev/null 2>&1; then return 0; fi; sleep 2; done; fail "$2 never became ready"; }

admin="$(openssl rand -hex 16)"
rotation_key="$(openssl ecparam -name secp256k1 -genkey -noout -outform DER | tail -c +8 | head -c 32 | od -An -tx1 | tr -d ' \n')"
[[ "$rotation_key" =~ ^[0-9a-f]{64}$ ]] || fail "could not generate the PLC rotation key"

db="$(docker run -d -p 127.0.0.1:12583:2583 -e POSTGRES_PASSWORD="$admin" "$POSTGRES_IMAGE")" || fail "postgres did not start"
started+=("$db")
wait_for "docker exec $db pg_isready -U postgres -h 127.0.0.1" postgres

plc="$(docker run -d --network "container:$db" -e PORT=2582 -e PLC_VERSION=0.0.1 \
  -e DB_CREDS_JSON="{\"password\":\"$admin\"}" -e DB_MIGRATE_CREDS_JSON="{\"password\":\"$admin\"}" "$PLC_IMAGE")" || fail "plc did not start"
started+=("$plc")
wait_for "docker exec $db wget -qO- http://127.0.0.1:2582/_health" plc

pds="$(docker run -d --network "container:$db" -v /pds \
  -e PDS_HOSTNAME=localhost -e PDS_PORT=2583 -e PDS_DATA_DIRECTORY=/pds -e PDS_BLOBSTORE_DISK_LOCATION=/pds/blocks \
  -e PDS_JWT_SECRET="$(openssl rand -hex 32)" -e PDS_ADMIN_PASSWORD="$admin" \
  -e PDS_PLC_ROTATION_KEY_K256_PRIVATE_KEY_HEX="$rotation_key" -e PDS_DID_PLC_URL=http://127.0.0.1:2582 \
  -e PDS_CRAWLERS= -e PDS_RATE_LIMITS_ENABLED=true -e PDS_INVITE_REQUIRED=true -e PDS_DEV_MODE=true "$PDS_IMAGE")" || fail "pds did not start"
started+=("$pds")
wait_for "curl -fsS $PDS/xrpc/_health" pds

for mode in async sync; do
  invite="$(curl -fsS -u "admin:$admin" -H 'Content-Type: application/json' -d '{"useCount":1}' \
    "$PDS/xrpc/com.atproto.server.createInviteCode" | jq -er .code)" || fail "createInviteCode ($mode)"
  body="$(jq -nc --arg h "oasis-$mode.test" --arg e "oasis-$mode@oasis.example.com" --arg p "$password" --arg i "$invite" \
    '{handle:$h,email:$e,password:$p,inviteCode:$i}')"
  response="$(curl -sS -H 'Content-Type: application/json' -d "$body" "$PDS/xrpc/com.atproto.server.createAccount")"
  jq -e '.did | startswith("did:plc:")' <<<"$response" >/dev/null || fail "createAccount ($mode): $response"
done

echo "${started[*]}"
