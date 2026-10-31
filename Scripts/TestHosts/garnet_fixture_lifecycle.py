"""Fixture teardown must never discard uncheckpointed acknowledged native state."""
import json
from pathlib import Path

def stop_owned_garnet(state: Path, process):
    has_source = (state / "demo-seed.json").exists() or (state / "demo-seed.pending.json").exists() or any(state.glob("source-write-*.ack.json"))
    checkpoint = state / "checkpoint.json"
    acknowledged = checkpoint.exists() and json.loads(checkpoint.read_text()).get("acknowledged") is True
    if has_source and ((state / "checkpoint.pending.json").exists() or not acknowledged):
        condition = "still live; preserve its owned PID/socket for reconciliation" if process.poll() is None else "already unavailable; preserve receipts, do not replay/reseed"
        raise RuntimeError("Uncheckpointed native source is " + condition)
    requested_stop = process.poll() is None
    if requested_stop:
        process.terminate()
        process.wait(timeout=20)
    if process.returncode != 0 and not (requested_stop and process.returncode == -15):
        raise RuntimeError("Owned Garnet termination failed; preserve process/source evidence")
