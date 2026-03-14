#!/usr/bin/env bash
# One command to run the skybox viewer. Starts a server and opens the Muay Thai gym world.
cd "$(dirname "$0")"
PORT=3333
URL="http://localhost:${PORT}/skybox-world-viewer.html"
echo ""
echo "  Skybox viewer: $URL"
echo "  (Credentials built in. Drag to look around. Press Ctrl+C to stop.)"
echo ""
python3 -m http.server "$PORT" &
PID=$!
sleep 1.5
open "$URL" 2>/dev/null || true
wait $PID
