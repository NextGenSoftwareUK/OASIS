#!/usr/bin/env bash
set -euo pipefail

: "${RUNNER_TEMP:?Use a disposable Linux CI runner.}"
[[ "${GITHUB_ACTIONS:-}" == true ]]
umask 077
fixture="$(mktemp -d "$RUNNER_TEMP/oasis-activitypub.XXXXXXXX")"
compose=(docker compose --project-name "oasis-activitypub-$GITHUB_RUN_ID" --project-directory "$fixture" --file "$fixture/compose.yml")
ca=/usr/local/share/ca-certificates/oasis-activitypub-ci.crt
ca_installed=false
host_marker="# oasis-activitypub-$GITHUB_RUN_ID"
host_entry_added=false
cleanup() {
  local status=$?
  trap - EXIT
  if [[ -f "$fixture/compose.yml" ]]; then
    if (( status != 0 )); then "${compose[@]}" logs --tail 30 web db tls >&2 || true; fi
    "${compose[@]}" down --volumes --remove-orphans || status=1
  fi
  if [[ "$ca_installed" == true ]]; then
    sudo rm -- "$ca" || status=1
    sudo update-ca-certificates --fresh >/dev/null || status=1
  fi
  if [[ "$host_entry_added" == true ]]; then
    sudo sed -i "\\|^127\\.0\\.0\\.1 mastodon\\.oasis\\.test $host_marker$|d" /etc/hosts || status=1
  fi
  if [[ "$fixture" == "$RUNNER_TEMP"/oasis-activitypub.* ]]; then
    rm -rf -- "$fixture"
    if [[ ! -e "$fixture" ]]; then echo "Removed the owned Mastodon fixture directory."; else status=1; fi
  fi
  exit "$status"
}
trap cleanup EXIT

cat >"$fixture/mastodon.env" <<EOF
LOCAL_DOMAIN=mastodon.oasis.test
DB_HOST=db
DB_NAME=mastodon
DB_USER=mastodon
DB_PASS=$(openssl rand -hex 24)
REDIS_HOST=redis
SECRET_KEY_BASE=$(openssl rand -hex 64)
OTP_SECRET=$(openssl rand -hex 64)
ACTIVE_RECORD_ENCRYPTION_PRIMARY_KEY=$(openssl rand -hex 16)
ACTIVE_RECORD_ENCRYPTION_DETERMINISTIC_KEY=$(openssl rand -hex 16)
ACTIVE_RECORD_ENCRYPTION_KEY_DERIVATION_SALT=$(openssl rand -hex 16)
ES_ENABLED=false
EOF
grep '^DB_PASS=' "$fixture/mastodon.env" | sed 's/^DB_PASS=/POSTGRES_PASSWORD=/' >"$fixture/postgres.env"

openssl req -x509 -newkey rsa:2048 -nodes -days 1 -subj '/CN=mastodon.oasis.test' \
  -addext 'subjectAltName=DNS:mastodon.oasis.test' \
  -keyout "$fixture/tls.key" -out "$fixture/tls.crt" 2>"$fixture/openssl.log"
[[ ! -e "$ca" ]]
sudo install -m 0644 "$fixture/tls.crt" "$ca"
ca_installed=true
sudo update-ca-certificates >/dev/null
echo "127.0.0.1 mastodon.oasis.test $host_marker" | sudo tee -a /etc/hosts >/dev/null
host_entry_added=true

cat >"$fixture/nginx.conf" <<'EOF'
server {
    listen 443 ssl;
    ssl_certificate /fixture/tls.crt;
    ssl_certificate_key /fixture/tls.key;
    location / {
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-Proto https;
        proxy_pass http://web:3000;
    }
}
EOF
cat >"$fixture/compose.yml" <<'EOF'
services:
  db:
    image: postgres:16.6-alpine
    env_file: postgres.env
    environment:
      POSTGRES_USER: mastodon
      POSTGRES_DB: mastodon
    healthcheck:
      test: [CMD, pg_isready, -U, mastodon]
      interval: 2s
      timeout: 3s
      retries: 30
  redis:
    image: redis:7.4.2-alpine
    healthcheck:
      test: [CMD, redis-cli, ping]
      interval: 2s
      timeout: 3s
      retries: 30
  web:
    image: ghcr.io/mastodon/mastodon:v4.4.28
    env_file: mastodon.env
    command: bundle exec puma -C config/puma.rb
  tls:
    image: nginx:1.28.0-alpine
    ports: ["127.0.0.1:443:443"]
    volumes:
      - ./nginx.conf:/etc/nginx/conf.d/default.conf:ro
      - ./tls.crt:/fixture/tls.crt:ro
      - ./tls.key:/fixture/tls.key:ro
EOF

"${compose[@]}" up --detach --wait db redis
"${compose[@]}" run --rm -T --no-deps web bundle exec rails db:schema:load db:seed
token="$("${compose[@]}" run --rm -T --no-deps web bundle exec rails runner - <<'RUBY'
def create_user(username)
  user = User.new(email: "#{username}@oasis.test", password: SecureRandom.hex,
                  agreement: true, confirmed_at: Time.now.utc,
                  bypass_registration_checks: true, approved: true)
  user.account = Account.new(username: username)
  user.save!
  user
end

reader = create_user('oasisreader')
42.times { |index| create_user(format('oasisfixture%02d', index)) }
app = Doorkeeper::Application.create!(name: 'OASIS CI', redirect_uri: 'urn:ietf:wg:oauth:2.0:oob', scopes: 'read:search')
token = Doorkeeper::AccessToken.create!(application: app, resource_owner_id: reader.id, scopes: 'read:search')
puts "OASIS_FIXTURE_TOKEN=#{token.plaintext_token}"
RUBY
)"
token="$(sed -n 's/^OASIS_FIXTURE_TOKEN=//p' <<<"$token")"
[[ "$token" =~ ^[A-Za-z0-9_-]+$ ]]
echo "::add-mask::$token"
"${compose[@]}" up --detach web tls
origin=https://mastodon.oasis.test
curl --fail --silent --show-error --retry 30 --retry-all-errors --retry-delay 2 --max-time 5 "$origin/health" >/dev/null
response="$(curl --fail --silent --show-error --max-time 15 --header "Authorization: Bearer $token" \
  "$origin/api/v2/search?q=oasisfixture&type=accounts&limit=40&offset=40&resolve=false")"
jq -e '.accounts | length == 2' <<<"$response" >/dev/null
echo 'Native authenticated search returned the two expected second-page accounts.'
export OASIS_ACTIVITYPUB_SEARCH_LIVE=1 OASIS_ACTIVITYPUB_SEARCH_INSTANCE="$origin"
export OASIS_ACTIVITYPUB_SEARCH_QUERY=oasisfixture OASIS_ACTIVITYPUB_SEARCH_TOKEN="$token"
pwsh -NoProfile -Command '
  ./Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner CI-ActivityPubSearch -Operation {
    dotnet test "Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests.csproj" --filter "FullyQualifiedName~AuthorizedMastodonAccountSearchTests" /p:ActivityPubDirectProviderOnly=true /m:2 /nr:false /p:UseSharedCompilation=false --logger "console;verbosity=normal"
    if ($LASTEXITCODE -ne 0) { throw "Native Mastodon authenticated search verification failed." }
  }'
