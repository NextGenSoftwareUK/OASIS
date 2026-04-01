#!/usr/bin/env bash
# Remove all quests for an avatar via STAR WebAPI: DELETE /api/quests/{id}
#
# Defaults match local dev:
#   base-url: http://localhost:8888
#   username: dellams
#   password: test!
#
# Usage:
#   ./remove_all_quests.sh
#   ./remove_all_quests.sh --base-url http://localhost:8888 -u dellams -p 'test!'
#   ./remove_all_quests.sh --jwt "$STAR_JWT"
#   ./remove_all_quests.sh --jwt "$STAR_JWT" --avatar-id e4a72f2e-ca27-4a1d-ac7f-a3d1e753dc27
#
# IMPORTANT — same avatar as the game:
#   Quests are scoped by avatar. JWT alone may resolve to a *different* AvatarId than ODOOM/OQuake
#   (those clients often send X-Avatar-Id). If you delete with the wrong context, the list can look
#   empty or partial while the game still shows quests — or you only remove one duplicate set.
#   Pass --avatar-id (or STAR_AVATAR_ID) matching oasisstar.json / DemoQuestSeed "Avatar ID for quests" output.
#
# Notes:
# - Deletes objectives/sub-quests by deleting the parent quest rows too.
# - Orders deletions children-first when ParentQuestId is present.
# - Set OASIS_SCRIPT_NO_PAUSE=1 for CI/non-interactive runs.

set -euo pipefail

AUTH_TMP=""
LIST_TMP=""

_oasis_remove_quests_on_exit() {
  rm -f "${AUTH_TMP:-}" "${LIST_TMP:-}"
  if [[ "${OASIS_SCRIPT_NO_PAUSE:-}" == "1" ]]; then
    return 0
  fi
  echo ""
  echo "========================================"
  echo "  Press any key to exit"
  echo "========================================"
  if [[ -r /dev/tty ]]; then
    read -r -n1 -s _ </dev/tty 2>/dev/null || true
  else
    read -r -n1 -s _ 2>/dev/null || sleep 15
  fi
  echo ""
}
trap _oasis_remove_quests_on_exit EXIT

usage() {
  sed -n '1,22p' "$0" | tail -n +2
  exit "${1:-0}"
}

BASE_URL="${STAR_BASE_URL:-http://localhost:8888}"
USERNAME="${STAR_USERNAME:-dellams}"
PASSWORD="${STAR_PASSWORD:-test!}"
JWT="${STAR_JWT:-}"
AVATAR_ID="${STAR_AVATAR_ID:-}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --base-url)
      BASE_URL="${2:?}"
      shift 2
      ;;
    -u|--user)
      USERNAME="${2:?}"
      shift 2
      ;;
    -p|--pass)
      PASSWORD="${2:?}"
      shift 2
      ;;
    --jwt)
      JWT="${2:?}"
      shift 2
      ;;
    --avatar-id)
      AVATAR_ID="${2:?}"
      shift 2
      ;;
    -h|--help)
      usage 0
      ;;
    *)
      echo "Unknown option: $1" >&2
      usage 2
      ;;
  esac
done

if ! command -v curl >/dev/null 2>&1; then
  echo "ERROR: curl is required." >&2
  exit 1
fi
if ! command -v python3 >/dev/null 2>&1; then
  echo "ERROR: python3 is required for JSON parsing." >&2
  exit 1
fi

BASE_URL="${BASE_URL%/}"

extract_jwt_from_auth_json() {
  python3 - "$1" <<'PY'
import json, sys
with open(sys.argv[1], encoding="utf-8") as f:
    d = json.load(f)
def walk(o):
    if isinstance(o, dict):
        for k, v in o.items():
            lk = k.lower()
            if lk in ("jwttoken", "token", "accesstoken", "access_token", "jwt") and isinstance(v, str) and v.count(".") >= 2:
                return v
            r = walk(v)
            if r:
                return r
    elif isinstance(o, list):
        for x in o:
            r = walk(x)
            if r:
                return r
    return None
print(walk(d) or "")
PY
}

quest_ids_children_first_from_list_json() {
  python3 - "$1" <<'PY'
import json, sys
with open(sys.argv[1], encoding="utf-8") as f:
    d = json.load(f)

def unwrap_result(o):
    if isinstance(o, dict):
        return o.get("result", o.get("Result", o))
    return o

rows = unwrap_result(d)
if not isinstance(rows, list):
    sys.exit(0)

by_id = {}
parent = {}
for q in rows:
    if not isinstance(q, dict):
        continue
    qid = str(q.get("id") or q.get("Id") or "").strip()
    if not qid:
        continue
    pid = str(q.get("parentQuestId") or q.get("ParentQuestId") or "").strip()
    by_id[qid] = True
    parent[qid] = pid

def depth(qid):
    seen = set()
    d = 0
    cur = qid
    while True:
        p = parent.get(cur, "")
        if not p or p in seen or p not in by_id:
            return d
        d += 1
        seen.add(p)
        cur = p

ids = sorted(by_id.keys(), key=lambda x: depth(x), reverse=True)
print("\n".join(ids))
PY
}

api_result_is_error() {
  python3 - "$1" <<'PY'
import json, sys
try:
    with open(sys.argv[1], encoding="utf-8") as f:
        d = json.load(f)
except Exception:
    print("unknown")
    raise SystemExit(0)
if isinstance(d, dict):
    v = d.get("IsError", d.get("isError", None))
    if v is True:
        print("true")
    elif v is False:
        print("false")
    else:
        print("unknown")
else:
    print("unknown")
PY
}

api_result_message() {
  python3 - "$1" <<'PY'
import json, sys
try:
    with open(sys.argv[1], encoding="utf-8") as f:
        d = json.load(f)
except Exception:
    print("")
    raise SystemExit(0)
if isinstance(d, dict):
    m = d.get("Message", d.get("message", ""))
    print(m if isinstance(m, str) else "")
else:
    print("")
PY
}

if [[ -z "$JWT" ]]; then
  AUTH_TMP="$(mktemp)"
  HTTP_CODE="$(RQ_USER="$USERNAME" RQ_PASS="$PASSWORD" python3 -c 'import json,os; print(json.dumps({"username":os.environ["RQ_USER"],"password":os.environ["RQ_PASS"]}))' | curl -sS -o "$AUTH_TMP" -w '%{http_code}' -X POST "$BASE_URL/api/avatar/authenticate" \
    -H 'Content-Type: application/json' \
    -d @-)"
  if [[ "$HTTP_CODE" != "200" ]]; then
    echo "ERROR: Authenticate failed (HTTP $HTTP_CODE). Body:" >&2
    cat "$AUTH_TMP" >&2
    exit 1
  fi
  JWT="$(extract_jwt_from_auth_json "$AUTH_TMP")"
  rm -f "$AUTH_TMP"
  AUTH_TMP=""
  if [[ -z "$JWT" ]]; then
    echo "ERROR: Could not find JWT in authenticate response." >&2
    exit 1
  fi
fi

CURL_AUTH_HEADERS=(-H "Authorization: Bearer $JWT")
if [[ -n "$AVATAR_ID" ]]; then
  CURL_AUTH_HEADERS+=(-H "X-Avatar-Id: $AVATAR_ID")
fi

if [[ -z "$AVATAR_ID" ]]; then
  echo "WARNING: X-Avatar-Id not set (use --avatar-id or STAR_AVATAR_ID). If quests remain after this script," >&2
  echo "         use the same avatar GUID as ODOOM/oasisstar.json and re-run." >&2
  echo "" >&2
fi

TOTAL_DELETED=0
PASS=1
declare -A SKIP_GHOST_IDS=()

while true; do
  LIST_TMP="$(mktemp)"
  HC="$(curl -sS -o "$LIST_TMP" -w '%{http_code}' -X GET "$BASE_URL/api/quests/all-for-avatar" \
    "${CURL_AUTH_HEADERS[@]}" \
    -H 'Accept: application/json')"
  if [[ "$HC" != "200" ]]; then
    echo "ERROR: GET /api/quests/all-for-avatar failed (HTTP $HC). Body:" >&2
    cat "$LIST_TMP" >&2
    exit 1
  fi

  mapfile -t QUEST_IDS < <(quest_ids_children_first_from_list_json "$LIST_TMP")
  rm -f "$LIST_TMP"
  LIST_TMP=""

  if [[ ${#QUEST_IDS[@]} -eq 0 ]]; then
    if [[ "$TOTAL_DELETED" -eq 0 ]]; then
      echo "No quests found for user '$USERNAME'."
    else
      echo "Done. Deleted $TOTAL_DELETED quest(s) total across $((PASS - 1)) pass(es)."
    fi
    exit 0
  fi

  FILTERED_IDS=()
  for qid in "${QUEST_IDS[@]}"; do
    [[ -z "$qid" ]] && continue
    if [[ -n "${SKIP_GHOST_IDS[$qid]:-}" ]]; then
      continue
    fi
    FILTERED_IDS+=("$qid")
  done

  if [[ ${#FILTERED_IDS[@]} -eq 0 ]]; then
    echo "Done. Deleted $TOTAL_DELETED quest(s)."
    if [[ ${#SKIP_GHOST_IDS[@]} -gt 0 ]]; then
      echo "Note: ${#SKIP_GHOST_IDS[@]} ghost quest id(s) were returned by list endpoint but cannot be loaded/deleted (No Holon Found):"
      for gid in "${!SKIP_GHOST_IDS[@]}"; do
        echo "  - $gid"
      done
    fi
    exit 0
  fi

  echo "Pass $PASS: deleting ${#FILTERED_IDS[@]} quest(s) for user '$USERNAME'..."
  PASS_DELETED=0
  for qid in "${FILTERED_IDS[@]}"; do
    [[ -z "$qid" ]] && continue
    echo "DELETE $BASE_URL/api/quests/$qid"
    OUT_TMP="$(mktemp)"
    RESP_CODE="$(curl -sS -o "$OUT_TMP" -w '%{http_code}' -X DELETE "$BASE_URL/api/quests/$qid" \
      "${CURL_AUTH_HEADERS[@]}" \
      -H 'Accept: application/json')"
    if [[ "$RESP_CODE" != "200" ]]; then
      echo "  FAILED HTTP $RESP_CODE" >&2
      cat "$OUT_TMP" >&2
      rm -f "$OUT_TMP"
      exit 1
    fi
    API_ERR="$(api_result_is_error "$OUT_TMP")"
    API_MSG="$(api_result_message "$OUT_TMP")"
    if [[ "$API_ERR" == "true" ]]; then
      if [[ "$API_MSG" == *"No Holon Found"* ]]; then
        echo "  SKIP ghost quest (already missing): $qid"
        SKIP_GHOST_IDS["$qid"]=1
        rm -f "$OUT_TMP"
        continue
      fi
      echo "  FAILED (API IsError=true)${API_MSG:+: $API_MSG}" >&2
      cat "$OUT_TMP" >&2
      rm -f "$OUT_TMP"
      exit 1
    fi
    echo "  OK (HTTP $RESP_CODE)${API_MSG:+: $API_MSG}"
    rm -f "$OUT_TMP"
    PASS_DELETED=$((PASS_DELETED + 1))
    TOTAL_DELETED=$((TOTAL_DELETED + 1))
  done

  echo "Pass $PASS complete: deleted $PASS_DELETED quest(s). Re-checking..."
  PASS=$((PASS + 1))
done
