#!/usr/bin/env bash
# Boots one offline fake ship (~zod, -F -L) from the pinned Vere runtime and solid pill in a
# container, installs the %oasis desk (the %oasis-kv Gall agent UrbitOASIS stores into) and
# prints shell assignments for the caller to eval:
#   container="$container <id>"; export OASIS_URBIT_URL=... OASIS_URBIT_CODE=...
# A fake ship never contacts the live Urbit network. The runtime and pill are downloaded once into
# $OASIS_URBIT_CACHE (default ~/.cache/oasis-urbit) and verified by size and SHA-256 on every run.
# The pier lives in <fixture-dir>/urbit/zod; `docker restart` of the container reopens it.
#
# Usage: start-urbit-fixture.sh <fixture-dir> [http-port]
set -euo pipefail

fixture="$(cd "${1:?fixture directory required}" && pwd)"
port="${2:-18080}"
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo="$(cd "$here/../../../../.." && pwd)"
desk_source="$repo/Providers/Other/NextGenSoftware.OASIS.API.Providers.UrbitOASIS/UrbitDesk"
inputs="$repo/Scripts/TestHosts/urbit-runtime-inputs.json"
image="$(jq -er .containerImage "$inputs")"
cache="${OASIS_URBIT_CACHE:-$HOME/.cache/oasis-urbit}/$(jq -er .runtimeTag "$inputs")-$(jq -er .kernelRelease "$inputs")"
pier="$fixture/urbit/zod"
[[ ! -e "$fixture/urbit" ]] || { echo "Refusing to reuse an existing pier in $fixture/urbit" >&2; exit 1; }
mkdir -p "$cache" "$fixture/urbit"

fetch() { # url target bytes sha256
  if [[ ! -f "$2" ]]; then
    curl -fsSL --retry 3 -o "$2.partial" "$1"
    mv -- "$2.partial" "$2"
  fi
  [[ "$(stat -c %s "$2")" == "$3" ]] || { echo "Size mismatch for $2" >&2; exit 1; }
  echo "$4  $2" | sha256sum -c --quiet - >&2 || { echo "SHA-256 mismatch for $2" >&2; exit 1; }
}

fetch "$(jq -er .runtimeArchiveUrl "$inputs")" "$cache/runtime.tgz" "$(jq -er .runtimeArchiveBytes "$inputs")" "$(jq -er .runtimeArchiveSha256 "$inputs")"
pill_url="https://media.githubusercontent.com/media/urbit/urbit/$(jq -er .kernelCommit "$inputs")/$(jq -er .pillPath "$inputs")"
fetch "$pill_url" "$cache/solid.pill" "$(jq -er .pillBytes "$inputs")" "$(jq -er .pillSha256 "$inputs")"
urbit="$cache/urbit"
if [[ ! -x "$urbit" ]]; then
  member="$(tar -tzf "$cache/runtime.tgz" | grep -Ev '/$')"
  [[ "$(wc -l <<<"$member")" == 1 ]] || { echo "Runtime archive must hold exactly one file" >&2; exit 1; }
  tar -xzf "$cache/runtime.tgz" -O "$member" >"$cache/urbit.partial"
  chmod 755 "$cache/urbit.partial" && mv -- "$cache/urbit.partial" "$urbit"
fi
"$urbit" -R | grep -qx 'urbit 4.6' || { echo "Unexpected Vere version" >&2; exit 1; }

ship() { # docker run options..., then urbit arguments after --
  local options=() ; while [[ "$1" != -- ]]; do options+=("$1"); shift; done; shift
  docker run "${options[@]}" --user "$(id -u):$(id -g)" --env HOME=/pier --workdir /pier \
    -v "$cache:/assets:ro" -v "$fixture/urbit:/pier" "$image" /assets/urbit "$@"
}
thread() { python3 -I "$here/urbit-thread.py" "$urbit" "$pier" "$@"; }

# -x: boot from the pill, commit the event log and exit (about a minute).
ship --rm --network none -- -F zod -B /assets/solid.pill -c /pier/zod -t -x -L --loom 32 >"$fixture/urbit-boot.log" 2>&1 \
  || { tail -n 40 "$fixture/urbit-boot.log" >&2; exit 1; }
id="$(ship -d -p "127.0.0.1:$port:8080" -- -t -L --http-port 8080 --loom 32 /pier/zod)"
echo "container=\"\${container:-} $id\""
for _ in $(seq 1 120); do
  if [[ -S "$pier/.urb/conn.sock" ]] && curl -fsS -o /dev/null "http://127.0.0.1:$port/~/login" 2>/dev/null; then break; fi
  [[ "$(docker inspect -f '{{.State.Running}}' "$id")" == true ]] || { docker logs --tail 40 "$id" >&2; exit 1; }
  sleep 1
done
thread 0 '(pure:m !>(0))' >/dev/null

# %oasis = %base (marks, strandio, default-agent) + the committed %oasis-kv sources.
thread 0 ';<  ~  bind:m  (poke [our.bowl %hood] kiln-merge+!>([%oasis our.bowl %base da+now.bowl %init]))  (pure:m !>(0))' >/dev/null
thread 0 '=/  =path  [(scot %p our.bowl) %oasis (scot %da now.bowl) ~]  ;<  ~  bind:m  (poke [our.bowl %hood] kiln-mount+!>([path %oasis]))  (pure:m !>(0))' >/dev/null
for _ in $(seq 1 60); do [[ -f "$pier/oasis/sys.kelvin" ]] && break; sleep 1; done
[[ -f "$pier/oasis/sys.kelvin" ]] || { echo "%oasis desk was never mounted" >&2; exit 1; }
cp -R "$desk_source/." "$pier/oasis/"
thread 0 ';<  ~  bind:m  (poke [our.bowl %hood] kiln-commit+!>([%oasis |]))  (pure:m !>(0))' >/dev/null
thread 0 ';<  ok=?  bind:m  (scry ? %cu /oasis/app/oasis-kv/hoon)  ?>  ok  (pure:m !>(0))' >/dev/null
thread 0 ';<  ~  bind:m  (poke [our.bowl %hood] kiln-install+!>([%oasis our.bowl %oasis]))  (pure:m !>(0))' >/dev/null
thread 0 '=|  n=@ud  |-  ?:  .^(? %gu /(scot %p our.bowl)/oasis-kv/(scot %da now.bowl)/$)  (pure:m !>(0))  ?>  (lth n 300)  ;<  ~  bind:m  (sleep ~s1)  $(n +(n))' >/dev/null
code="$(thread '%([a-z-]+)' '(pure:m !>((crip (slag 1 (scow %p .^(@p %j /(scot %p our.bowl)/code/(scot %da now.bowl)/(scot %p our.bowl)))))))')"
[[ "$code" =~ ^[a-z-]{27}$ ]] || { echo "Unexpected +code shape" >&2; exit 1; }
echo "export OASIS_URBIT_URL=http://127.0.0.1:$port OASIS_URBIT_CODE=$code"
