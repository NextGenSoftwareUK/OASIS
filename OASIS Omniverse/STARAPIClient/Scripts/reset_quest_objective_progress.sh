#!/usr/bin/env bash
# Reset in-game progress dictionaries for one or more quests (objective kill counts, pickups, etc.)
# via STAR WebAPI: POST /api/quests/{questId}/progress/reset
#
# Requires: curl, python3 (for JSON/JWT parsing). WEB5 STAR API (default port 8888) and WEB4 must be reachable for password auth.
# On exit, waits for a key so double-click / terminal windows do not vanish immediately. CI/automation: OASIS_SCRIPT_NO_PAUSE=1
#
# Usage:
#   ./reset_quest_objective_progress.sh                        # no args: dellams / test! @ localhost:8888, all quests
#   ./reset_quest_objective_progress.sh [options] <quest-guid> [quest-guid ...]
#   ./reset_quest_objective_progress.sh [options] --all        # same as no GUIDs (explicit)
#
# Options:
#   --base-url URL   STAR WebAPI base (default: http://localhost:8888 or STAR_BASE_URL)
#   -u, --user NAME  Username (default: dellams, or STAR_USERNAME)
#   -p, --pass PASS  Password (default: test!, or STAR_PASSWORD)
#   --jwt TOKEN      Skip login; use this Bearer token (or STAR_JWT)
#   --all            Reset every quest for the avatar (default when no quest GUIDs are passed)
#   -h, --help
#
# Examples:
#   ./reset_quest_objective_progress.sh
#   ./reset_quest_objective_progress.sh --jwt "$JWT" 3fa85f64-5717-4562-b3fc-2c963f66afa6

set -euo pipefail

AUTH_TMP=""
LIST_TMP=""

_oasis_quest_reset_on_exit() {
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
trap _oasis_quest_reset_on_exit EXIT

usage() {
  sed -n '1,25p' "$0" | tail -n +2
  exit "${1:-0}"
}

BASE_URL="${STAR_BASE_URL:-http://localhost:8888}"
USERNAME="${STAR_USERNAME:-dellams}"
# Local dev default matches TEST_INVENTORY / common OASIS demo accounts; override with STAR_PASSWORD or -p.
PASSWORD="${STAR_PASSWORD:-test!}"
JWT="${STAR_JWT:-}"
DO_ALL=0
QUEST_IDS=()

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
    --all)
      DO_ALL=1
      shift
      ;;
    -h|--help)
      usage 0
      ;;
    -*)
      echo "Unknown option: $1" >&2
      usage 2
      ;;
    *)
      QUEST_IDS+=("$1")
      shift
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
path = sys.argv[1]
with open(path, encoding="utf-8") as f:
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

quest_ids_from_list_json() {
  python3 - "$1" <<'PY'
import json, sys
path = sys.argv[1]
with open(path, encoding="utf-8") as f:
    d = json.load(f)

def unwrap_result(o):
    if not isinstance(o, dict):
        return o
    for key in ("result", "Result"):
        if key in o:
            return o[key]
    return o

r = unwrap_result(d)
if isinstance(r, list):
    out = []
    for q in r:
        if not isinstance(q, dict):
            continue
        i = q.get("id") or q.get("Id")
        if i:
            out.append(str(i))
    print("\n".join(out))
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
  # Match StarApiClient: best-effort ignite so managers are ready (ignore failures).
  RQ_USER="$USERNAME" RQ_PASS="$PASSWORD" python3 -c 'import json,os; print(json.dumps({"userName":os.environ["RQ_USER"],"password":os.environ["RQ_PASS"]}))' | \
    curl -sS -o /dev/null -X POST "$BASE_URL/api/star/ignite" \
      -H 'Content-Type: application/json' \
      -d @- || true
fi

if [[ ${#QUEST_IDS[@]} -gt 0 && "$DO_ALL" -eq 1 ]]; then
  echo "ERROR: Do not pass quest GUIDs together with --all." >&2
  exit 2
fi

if [[ ${#QUEST_IDS[@]} -eq 0 ]]; then
  LIST_TMP="$(mktemp)"
  HC="$(curl -sS -o "$LIST_TMP" -w '%{http_code}' -X GET "$BASE_URL/api/quests/all-for-avatar" \
    -H "Authorization: Bearer $JWT" \
    -H 'Accept: application/json')"
  if [[ "$HC" != "200" ]]; then
    echo "ERROR: GET /api/quests/all-for-avatar failed (HTTP $HC). Body:" >&2
    cat "$LIST_TMP" >&2
    exit 1
  fi
  mapfile -t QUEST_IDS < <(quest_ids_from_list_json "$LIST_TMP")
  rm -f "$LIST_TMP"
  LIST_TMP=""
  if [[ ${#QUEST_IDS[@]} -eq 0 ]]; then
    echo "No quest IDs returned from GET /api/quests/all-for-avatar (empty list or unexpected JSON shape)." >&2
    exit 1
  fi
  echo "Resetting objective progress for user '$USERNAME': ${#QUEST_IDS[@]} quest(s)..."
fi

for qid in "${QUEST_IDS[@]}"; do
  if [[ -z "$qid" ]]; then
    continue
  fi
  echo "POST $BASE_URL/api/quests/$qid/progress/reset"
  OUT_TMP="$(mktemp)"
  RESP_CODE="$(curl -sS -o "$OUT_TMP" -w '%{http_code}' -X POST "$BASE_URL/api/quests/$qid/progress/reset" \
    -H "Authorization: Bearer $JWT" \
    -H 'Content-Type: application/json' \
    -d '{}')"
  if [[ "$RESP_CODE" != "200" ]]; then
    echo "  FAILED HTTP $RESP_CODE" >&2
    cat "$OUT_TMP" >&2
    rm -f "$OUT_TMP"
    exit 1
  fi
  echo "  OK (HTTP $RESP_CODE)"
  python3 -c "import json,sys; d=json.load(open(sys.argv[1])); m=d.get('message') or d.get('Message'); print(' ', m if m else '')" "$OUT_TMP" 2>/dev/null || true
  rm -f "$OUT_TMP"
done

echo "Done."
