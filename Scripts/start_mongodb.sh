#!/usr/bin/env bash
# Start MongoDB (required for OASIS WEB4/WEB5 APIs that use Mongo at localhost:27017).
# Optionally enable MongoDB to start automatically on Linux boot.

set -e

MONGO_PORT=27017

echo ""
echo "========================================"
echo "MongoDB for OASIS (localhost:$MONGO_PORT)"
echo "========================================"
echo ""

# Check if already running
if command -v mongosh &>/dev/null; then
  if mongosh --quiet --eval "db.adminCommand('ping')" 1>/dev/null 2>&1; then
    echo "MongoDB is already running on port $MONGO_PORT."
    exit 0
  fi
elif command -v nc &>/dev/null && nc -z 127.0.0.1 $MONGO_PORT 2>/dev/null; then
  echo "MongoDB appears to be listening on port $MONGO_PORT."
  exit 0
fi

# Prefer systemd (Ubuntu/Debian, Fedora, etc.)
if command -v systemctl &>/dev/null; then
  MONGO_SVC=""
  for s in mongod mongodb; do
    if systemctl list-unit-files --type=service 2>/dev/null | grep -q "^${s}\.service"; then
      MONGO_SVC="$s"
      break
    fi
  done
  if [[ -n "$MONGO_SVC" ]]; then
    echo "Starting MongoDB via systemd (${MONGO_SVC})..."
    sudo systemctl start "$MONGO_SVC"
    echo "MongoDB started."
    if [[ "${ENABLE_MONGO_ON_BOOT:-}" == "1" ]] || [[ "$*" == *--enable-boot* ]]; then
      echo "Enabling MongoDB to start on boot..."
      sudo systemctl enable "$MONGO_SVC"
      echo "Done. MongoDB will start automatically on boot."
    else
      echo ""
      echo "To start MongoDB automatically on boot, run:"
      echo "  sudo systemctl enable $MONGO_SVC"
      echo "Or run this script with: ENABLE_MONGO_ON_BOOT=1 $0"
      echo "Or: $0 --enable-boot"
    fi
    exit 0
  fi
fi

# Fallback: try to run mongod directly (e.g. if installed but not as a service)
if command -v mongod &>/dev/null; then
  echo "Starting mongod in the foreground (Ctrl+C to stop)..."
  echo "For production, install the MongoDB package that registers a systemd service."
  exec mongod --bind_ip 127.0.0.1 --port $MONGO_PORT
fi

echo "MongoDB is not installed or not in PATH."
echo ""
echo "Install and start MongoDB on Ubuntu/Debian:"
echo "  1. Add the official MongoDB repo and install:"
echo "     https://www.mongodb.com/docs/manual/tutorial/install-mongodb-on-ubuntu/"
echo "  2. Or use the default Ubuntu package:"
echo "     sudo apt update && sudo apt install -y mongodb"
echo "  3. Start the service:"
echo "     sudo systemctl start mongod   # or: mongodb"
echo "  4. Enable start on boot:"
echo "     sudo systemctl enable mongod"
echo ""
echo "Quick one-liner (Ubuntu, if mongodb package exists):"
echo "  sudo apt install -y mongodb && sudo systemctl start mongodb && sudo systemctl enable mongodb"
exit 1
