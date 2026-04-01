#!/usr/bin/env bash
# Source from Scripts/*.sh so file-manager / double-click runs show output before the window closes.
# Skip pause in CI/automation: OASIS_SCRIPT_NO_PAUSE=1

if [[ "${OASIS_SCRIPT_NO_PAUSE:-}" == "1" ]]; then
  :
else
  _oasis_press_enter_to_exit() {
    echo ""
    echo "========================================"
    echo "  Press Enter to exit"
    echo "========================================"
    if [[ -r /dev/tty ]]; then
      read -r _ </dev/tty 2>/dev/null || true
    else
      read -r _ 2>/dev/null || sleep 15
    fi
  }
  trap _oasis_press_enter_to_exit EXIT
fi
