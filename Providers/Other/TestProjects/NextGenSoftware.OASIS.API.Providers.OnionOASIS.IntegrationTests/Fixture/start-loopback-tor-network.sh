#!/usr/bin/env bash
# Starts a private, loopback-only Tor network (TestingTorNetwork) for the OnionOASIS conformance suite:
# three directory authorities, two relays and one client that hosts the onion services and exposes SOCKS5.
# Nothing here contacts the public Tor network: every node trusts only these authorities.
#
# Usage: start-private-tor-network.sh <fixture dir> <socks port> <name=local port>...
# Writes <fixture>/tor/pids (tor process ids) and <fixture>/tor/<name>.onion (one hostname per onion service),
# and returns once the client is fully bootstrapped and every onion service descriptor has been uploaded.
set -euo pipefail
fixture="$1"; socks_port="$2"; shift 2
(( $# > 0 )) || { echo "At least one onion service (name=port) is required." >&2; exit 2; }
root="$fixture/tor"
mkdir -p "$root"
: >"$root/pids"
authorities=(a1 a2 a3)
relays=(r1 r2)
base_or=15101
base_dir=17101

common() {
  cat <<CONF
TestingTorNetwork 1
DataDirectory $root/$1
PidFile $root/$1/pid
Log notice file $root/$1/notice.log
SafeLogging 0
ShutdownWaitLength 0
Address 127.0.0.1
ClientUseIPv6 0
TestingV3AuthInitialVotingInterval 20
TestingV3AuthInitialVoteDelay 4
TestingV3AuthInitialDistDelay 4
V3AuthVotingInterval 20
V3AuthVoteDelay 4
V3AuthDistDelay 4
TestingDirAuthVoteExit *
TestingDirAuthVoteGuard *
TestingDirAuthVoteHSDir *
CONF
  cat "$root/dirauthorities"
}

# Authority identity/signing keys and relay fingerprints.
: >"$root/dirauthorities"
i=0
for node in "${authorities[@]}"; do
  or=$((base_or + i)); dir=$((base_dir + i)); i=$((i + 1))
  mkdir -p "$root/$node/keys"; chmod 700 "$root/$node"
  (cd "$root/$node/keys" && echo "" | tor-gencert --create-identity-key -m 12 -a "127.0.0.1:$dir" --passphrase-fd 0 >/dev/null 2>&1)
  v3ident="$(sed -n 's/^fingerprint //p' "$root/$node/keys/authority_certificate")"
  tor --quiet --ignore-missing-torrc --list-fingerprint --orport 1 \
    --dirserver "x 127.0.0.1:1 ffffffffffffffffffffffffffffffffffffffff" --datadirectory "$root/$node" >/dev/null
  fingerprint="$(cut -d' ' -f2- "$root/$node/fingerprint" | tr -d ' ')"
  echo "DirAuthority $node orport=$or no-v2 v3ident=$v3ident 127.0.0.1:$dir $fingerprint" >>"$root/dirauthorities"
done

start() { tor -f "$root/$1.torrc" --RunAsDaemon 0 >/dev/null 2>&1 & echo "$!" >>"$root/pids"; }

i=0
for node in "${authorities[@]}" "${relays[@]}"; do
  mkdir -p "$root/$node"; chmod 700 "$root/$node"
  {
    common "$node"
    echo "Nickname $node"
    echo "ORPort 127.0.0.1:$((base_or + i)) IPv4Only"
    echo "DirPort 127.0.0.1:$((base_dir + i))"
    echo "SocksPort 0"
    echo "ContactInfo oasis-private-test-network"
    echo "ExitPolicy accept 127.0.0.0/8:*, reject *:*"
    if (( i < ${#authorities[@]} )); then
      echo "AuthoritativeDirectory 1"
      echo "V3AuthoritativeDirectory 1"
    fi
  } >"$root/$node.torrc"
  i=$((i + 1))
  start "$node"
done

mkdir -p "$root/client"; chmod 700 "$root/client"
{
  common client
  echo "SocksPort 127.0.0.1:$socks_port"
  for service in "$@"; do
    name="${service%%=*}"; port="${service#*=}"
    mkdir -p "$root/hs-$name"; chmod 700 "$root/hs-$name"
    echo "HiddenServiceDir $root/hs-$name"
    echo "HiddenServicePort 80 127.0.0.1:$port"
  done
} >"$root/client.torrc"
start client

# Ready once every onion service answers an HTTP request made through the client's SOCKS5 port.
reachable() {
  for service in "$@"; do
    name="${service%%=*}"
    [[ -s "$root/hs-$name/hostname" ]] || return 1
    code="$(curl -sS -m 30 -o /dev/null -w '%{http_code}' --socks5-hostname "127.0.0.1:$socks_port" "http://$(cat "$root/hs-$name/hostname")/" 2>/dev/null || true)"
    [[ "$code" =~ ^[1-5][0-9][0-9]$ ]] || return 1
  done
}
for attempt in $(seq 1 100); do
  if grep -q 'Bootstrapped 100%' "$root/client/notice.log" 2>/dev/null && reachable "$@"; then
    for service in "$@"; do name="${service%%=*}"; cp "$root/hs-$name/hostname" "$root/$name.onion"; done
    echo "Private Tor network ready (SOCKS5 127.0.0.1:$socks_port)."
    exit 0
  fi
  sleep 3
done
echo "The private Tor network did not bootstrap." >&2
tail -n 30 "$root/client/notice.log" >&2 || true
exit 1
