#!/usr/bin/env bash
set -euo pipefail

# Simple helper for exercising driver-signal endpoints via curl.
#
# Usage:
#   BASE_URL=http://localhost:4205/api \
#   SERVICE_TOKEN=dev-token \
#   DRIVER_ID=64b2... \
#   BOOKING_ID=65aa... \
#   ./scripts/driverSignalTest.sh action accept
#
# Commands:
#   action <accept|start|complete|cancel|reject> [reason]
#   location <latitude> <longitude> [speed] [bearing]
#   pathpulse [telemetry|action]
#   metrics

BASE_URL="${BASE_URL:-http://localhost:4205/api}"
SERVICE_TOKEN="${SERVICE_TOKEN:-}"
PATHPULSE_SECRET="${PATHPULSE_SECRET:-changeme}"
DRIVER_ID="${DRIVER_ID:-}"
BOOKING_ID="${BOOKING_ID:-}"
TRACE_ID_PREFIX="${TRACE_ID_PREFIX:-cli-test}"

if [[ -z "$SERVICE_TOKEN" ]]; then
  echo "SERVICE_TOKEN env var is required" >&2
  exit 1
fi

if [[ -z "$DRIVER_ID" || -z "$BOOKING_ID" ]]; then
  echo "DRIVER_ID and BOOKING_ID env vars are required" >&2
  exit 1
fi

command="${1:-}"
shift || true

trace_id() {
  echo "${TRACE_ID_PREFIX}-$(date +%s%3N)"
}

post_driver_action() {
  local action="$1"
  local reason="${2:-}"
  local payload

  payload=$(jq -n \
    --arg driverId "$DRIVER_ID" \
    --arg bookingId "$BOOKING_ID" \
    --arg action "$action" \
    --arg traceId "$(trace_id)" \
    --arg reason "$reason" \
    '{
      driverId: $driverId,
      bookingId: $bookingId,
      action: $action,
      source: "cli",
      traceId: $traceId,
      meta: { reason: ($reason // null) }
    }')

  curl -sS -X POST \
    "${BASE_URL}/driver-actions" \
    -H "Content-Type: application/json" \
    -H "x-service-token: ${SERVICE_TOKEN}" \
    -d "$payload" | jq
}

post_driver_location() {
  local lat="$1"
  local lng="$2"
  local speed="${3:-null}"
  local bearing="${4:-null}"
  local payload

  payload=$(jq -n \
    --arg driverId "$DRIVER_ID" \
    --arg bookingId "$BOOKING_ID" \
    --argjson latitude "$lat" \
    --argjson longitude "$lng" \
    --argjson speed "${speed}" \
    --argjson bearing "${bearing}" \
    --arg traceId "$(trace_id)" \
    '{
      driverId: $driverId,
      bookingId: $bookingId,
      source: "cli",
      traceId: $traceId,
      location: {
        latitude: $latitude,
        longitude: $longitude,
        speed: ($speed | if . == null then null else . end),
        bearing: ($bearing | if . == null then null else . end)
      }
    }')

  curl -sS -X POST \
    "${BASE_URL}/driver-location" \
    -H "Content-Type: application/json" \
    -H "x-service-token: ${SERVICE_TOKEN}" \
    -d "$payload" | jq
}

send_pathpulse_event() {
  local mode="${1:-telemetry}"
  local timestamp
  timestamp=$(date -u +%s)

  local body
  if [[ "$mode" == "telemetry" ]]; then
    body=$(jq -n \
      --arg driver "$DRIVER_ID" \
      --arg booking "$BOOKING_ID" \
      --arg ts "$(date -Is)" \
      '{
        eventType: "telemetry",
        driverExternalId: $driver,
        bookingExternalId: $booking,
        timestamp: $ts,
        telemetry: {
          latitude: -26.11,
          longitude: 28.02,
          speed: 12.5,
          bearing: 180
        }
      }')
  else
    body=$(jq -n \
      --arg driver "$DRIVER_ID" \
      --arg booking "$BOOKING_ID" \
      --arg ts "$(date -Is)" \
      --arg actionType "accept" \
      '{
        eventType: "action",
        driverExternalId: $driver,
        bookingExternalId: $booking,
        timestamp: $ts,
        action: {
          type: $actionType,
          meta: {
            reason: "cli-demo"
          }
        }
      }')
  fi

  local signature
  signature=$(printf "%s.%s" "$timestamp" "$body" | openssl dgst -sha256 -hmac "$PATHPULSE_SECRET" | awk '{print $2}')

  curl -sS -X POST \
    "${BASE_URL}/driver-webhooks/pathpulse" \
    -H "Content-Type: application/json" \
    -H "x-pathpulse-timestamp: ${timestamp}" \
    -H "x-pathpulse-signature: ${signature}" \
    -d "$body" | jq
}

fetch_metrics() {
  curl -sS "${BASE_URL}/metrics/driver-signal" | jq
}

case "$command" in
  action)
    post_driver_action "${1:-accept}" "${2:-}"
    ;;
  location)
    if [[ $# -lt 2 ]]; then
      echo "Usage: location <lat> <lng> [speed] [bearing]" >&2
      exit 1
    fi
    post_driver_location "$1" "$2" "${3:-null}" "${4:-null}"
    ;;
  pathpulse)
    send_pathpulse_event "${1:-telemetry}"
    ;;
  metrics)
    fetch_metrics
    ;;
  *)
    cat <<EOF
Unknown command: ${command}
Commands:
  action <type> [reason]
  location <lat> <lng> [speed] [bearing]
  pathpulse [telemetry|action]
  metrics
EOF
    exit 1
    ;;
esac


