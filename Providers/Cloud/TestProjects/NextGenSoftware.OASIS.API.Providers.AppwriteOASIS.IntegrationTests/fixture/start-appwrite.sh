#!/usr/bin/env bash
# Disposable self-hosted Appwrite for the AppwriteOASIS native storage suite.
#
#   start-appwrite.sh start <env-file>   start Appwrite on 127.0.0.1:18080, provision a project, API key,
#                                        database and the oasis_async/oasis_sync key-value collections,
#                                        then write OASIS_APPWRITE_* variables to <env-file>.
#   start-appwrite.sh stop               remove every container, volume and network this script created.
#
# Everything binds to 127.0.0.1. All secrets are generated per run and never leave the env file.
set -euo pipefail

APPWRITE_IMAGE="appwrite/appwrite:1.7.4"
MARIADB_IMAGE="mariadb:10.11.11"
REDIS_IMAGE="redis:7.2.4-alpine"
NAME="oasis-appwrite"
PORT="${OASIS_APPWRITE_PORT:-18080}"
ENDPOINT="http://127.0.0.1:${PORT}/v1"

stop() {
  docker rm -fv "$NAME-http" "$NAME-worker-databases" "$NAME-mariadb" "$NAME-redis" >/dev/null 2>&1 || true
  docker network rm "$NAME" >/dev/null 2>&1 || true
}

wait_for() {
  for _ in $(seq 1 120); do
    if eval "$1" >/dev/null 2>&1; then return 0; fi
    sleep 2
  done
  echo "Appwrite fixture: '$1' never became ready" >&2
  docker logs --tail 60 "$NAME-http" >&2 || true
  return 1
}

start() {
  local env_file="$1"
  [[ -n "$env_file" ]] || { echo "usage: $0 start <env-file>" >&2; exit 2; }
  stop
  local db_pass root_pass openssl_key secret console_pass
  db_pass="$(openssl rand -hex 16)"; root_pass="$(openssl rand -hex 16)"
  openssl_key="$(openssl rand -hex 32)"; secret="$(openssl rand -hex 32)"; console_pass="$(openssl rand -hex 16)"

  docker network create "$NAME" >/dev/null
  docker run -d --name "$NAME-mariadb" --network "$NAME" --network-alias mariadb \
    -e MYSQL_ROOT_PASSWORD="$root_pass" -e MYSQL_DATABASE=appwrite -e MYSQL_USER=appwrite -e MYSQL_PASSWORD="$db_pass" \
    "$MARIADB_IMAGE" mysqld --innodb-flush-method=fsync >/dev/null
  docker run -d --name "$NAME-redis" --network "$NAME" --network-alias redis "$REDIS_IMAGE" >/dev/null
  wait_for "docker exec $NAME-mariadb mariadb -uappwrite -p$db_pass -e 'SELECT 1' appwrite"

  local common=(--network "$NAME"
    -e _APP_ENV=production -e _APP_LOCALE=en -e _APP_OPENSSL_KEY_V1="$openssl_key"
    -e _APP_DOMAIN=localhost -e _APP_DOMAIN_TARGET=localhost
    -e _APP_DOMAIN_FUNCTIONS=functions.localhost -e _APP_DOMAIN_SITES=sites.localhost -e _APP_OPTIONS_FORCE_HTTPS=disabled
    -e _APP_OPTIONS_ROUTER_PROTECTION=disabled -e _APP_OPTIONS_ABUSE=disabled
    -e _APP_CONSOLE_WHITELIST_ROOT=enabled -e _APP_USAGE_STATS=disabled
    -e _APP_REDIS_HOST=redis -e _APP_REDIS_PORT=6379
    -e _APP_DB_HOST=mariadb -e _APP_DB_PORT=3306 -e _APP_DB_SCHEMA=appwrite -e _APP_DB_USER=appwrite -e _APP_DB_PASS="$db_pass"
    -e _APP_EXECUTOR_SECRET="$secret")
  docker run -d --name "$NAME-http" -p "127.0.0.1:${PORT}:80" "${common[@]}" "$APPWRITE_IMAGE" >/dev/null
  wait_for "curl -fsS $ENDPOINT/health/version"
  docker run -d --name "$NAME-worker-databases" "${common[@]}" --entrypoint worker-databases "$APPWRITE_IMAGE" >/dev/null

  local jar; jar="$(mktemp)"
  trap 'rm -f "$jar"' RETURN
  console() { curl -fsS -b "$jar" -c "$jar" -H 'X-Appwrite-Project: console' -H 'Content-Type: application/json' "$@"; }
  # The first console account is created only after the console schema exists; retry until the API accepts it.
  wait_for "curl -fsS -c $jar -H 'X-Appwrite-Project: console' -H 'Content-Type: application/json' -X POST $ENDPOINT/account \
    -d '{\"userId\":\"unique()\",\"email\":\"fixture@oasis.invalid\",\"password\":\"$console_pass\",\"name\":\"fixture\"}'"
  console -X POST "$ENDPOINT/account/sessions/email" -d "{\"email\":\"fixture@oasis.invalid\",\"password\":\"$console_pass\"}" >/dev/null
  console -X POST "$ENDPOINT/teams" -d '{"teamId":"oasisfixture","name":"oasis-fixture"}' >/dev/null
  console -X POST "$ENDPOINT/projects" -d '{"projectId":"oasisfixture","name":"oasis-fixture","teamId":"oasisfixture","region":"default"}' >/dev/null
  local api_key
  api_key="$(console -X POST "$ENDPOINT/projects/oasisfixture/keys" \
    -d '{"name":"oasis-native-storage","scopes":["databases.read","databases.write","collections.read","collections.write","attributes.read","attributes.write","indexes.read","indexes.write","documents.read","documents.write"]}' | jq -er '.secret')"

  project() { curl -fsS -H 'X-Appwrite-Project: oasisfixture' -H "X-Appwrite-Key: $api_key" -H 'Content-Type: application/json' "$@"; }
  project -X POST "$ENDPOINT/databases" -d '{"databaseId":"oasis","name":"oasis"}' >/dev/null
  for collection in oasis_async oasis_sync; do
    project -X POST "$ENDPOINT/databases/oasis/collections" -d "{\"collectionId\":\"$collection\",\"name\":\"$collection\",\"documentSecurity\":false}" >/dev/null
    project -X POST "$ENDPOINT/databases/oasis/collections/$collection/attributes/string" -d '{"key":"key","size":255,"required":true}' >/dev/null
    project -X POST "$ENDPOINT/databases/oasis/collections/$collection/attributes/string" -d '{"key":"value","size":1073741824,"required":true}' >/dev/null
    wait_for "[[ \$(curl -fsS -H 'X-Appwrite-Project: oasisfixture' -H 'X-Appwrite-Key: $api_key' $ENDPOINT/databases/oasis/collections/$collection/attributes | jq '[.attributes[] | select(.status == \"available\")] | length') == 2 ]]"
    project -X POST "$ENDPOINT/databases/oasis/collections/$collection/indexes" -d '{"key":"key_prefix","type":"key","attributes":["key"],"orders":["ASC"]}' >/dev/null
    wait_for "curl -fsS -H 'X-Appwrite-Project: oasisfixture' -H 'X-Appwrite-Key: $api_key' $ENDPOINT/databases/oasis/collections/$collection/indexes/key_prefix | jq -e '.status == \"available\"'"
  done

  umask 077
  cat > "$env_file" <<EOF
OASIS_APPWRITE_ENDPOINT=$ENDPOINT
OASIS_APPWRITE_PROJECT=oasisfixture
OASIS_APPWRITE_KEY=$api_key
OASIS_APPWRITE_DATABASE=oasis
EOF
  echo "Appwrite fixture ready at $ENDPOINT"
}

case "${1:-}" in
  start) start "${2:-}" ;;
  stop) stop ;;
  *) echo "usage: $0 start <env-file> | stop" >&2; exit 2 ;;
esac
