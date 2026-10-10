#!/usr/bin/env python3
"""Disposable WEB5 GeoHotSpot trigger-contract host for process resilience tests.

This host deliberately implements only the public health and trigger routes used by
test_geohotspot_live_resilience.ps1.  SQLite is shared by independent host
processes, so the test exercises cross-process serialization and durable replay.
It is test infrastructure and is never used by a deployed WEB5 service.
"""

import argparse
import json
import sqlite3
import time
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlparse


def utc_now():
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


class Store:
    def __init__(self, path: Path, hotspot_id: str, delay_ms: int):
        self.path = path
        self.hotspot_id = hotspot_id.lower()
        self.delay_ms = delay_ms
        with self.connect() as db:
            db.execute("PRAGMA journal_mode=WAL")
            db.execute("CREATE TABLE IF NOT EXISTS hotspot (id TEXT PRIMARY KEY, global_limit INTEGER NOT NULL, global_count INTEGER NOT NULL)")
            db.execute("CREATE TABLE IF NOT EXISTS accepted (idempotency_key TEXT PRIMARY KEY, hotspot_id TEXT NOT NULL, avatar_id TEXT NOT NULL, accepted_at TEXT NOT NULL, global_count INTEGER NOT NULL, player_count INTEGER NOT NULL)")
            db.execute("INSERT OR IGNORE INTO hotspot VALUES (?, 1, 0)", (self.hotspot_id,))

    def connect(self):
        return sqlite3.connect(self.path, timeout=30, isolation_level=None)

    def trigger(self, hotspot_id: str, avatar_id: str, key: str):
        if hotspot_id.lower() != self.hotspot_id:
            return 404, {"isError": True, "message": "GeoHotSpot was not found."}
        if self.delay_ms:
            time.sleep(self.delay_ms / 1000.0)
        with self.connect() as db:
            db.execute("BEGIN IMMEDIATE")
            prior = db.execute("SELECT accepted_at, global_count, player_count FROM accepted WHERE idempotency_key=?", (key,)).fetchone()
            if prior:
                db.execute("COMMIT")
                return 200, self.result(hotspot_id, key, prior[0], prior[1], prior[2], "Trigger was already accepted.")
            limit_value, count = db.execute("SELECT global_limit, global_count FROM hotspot WHERE id=?", (self.hotspot_id,)).fetchone()
            if count >= limit_value:
                db.execute("ROLLBACK")
                return 400, {"isError": True, "message": "The global spawn quantity has been exhausted."}
            player_count = db.execute("SELECT COUNT(*) FROM accepted WHERE hotspot_id=? AND avatar_id=?", (self.hotspot_id, avatar_id)).fetchone()[0] + 1
            count += 1
            accepted_at = utc_now()
            db.execute("UPDATE hotspot SET global_count=? WHERE id=?", (count, self.hotspot_id))
            db.execute("INSERT INTO accepted VALUES (?, ?, ?, ?, ?, ?)", (key, self.hotspot_id, avatar_id, accepted_at, count, player_count))
            db.execute("COMMIT")
            return 200, self.result(hotspot_id, key, accepted_at, count, player_count, "GeoHotSpot trigger accepted.")

    @staticmethod
    def result(hotspot_id, key, accepted_at, global_count, player_count, message):
        return {"isError": False, "message": message, "result": {"geoHotSpotId": hotspot_id, "idempotencyKey": key, "acceptedAtUtc": accepted_at, "globalTriggerCount": global_count, "playerTriggerCount": player_count}}


def handler(store: Store, tokens):
    class Handler(BaseHTTPRequestHandler):
        def reply(self, status, payload):
            raw = json.dumps(payload).encode()
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(raw)))
            self.end_headers()
            self.wfile.write(raw)

        def do_GET(self):
            if urlparse(self.path).path == "/api/health":
                self.reply(200, {"status": "Healthy"})
            else:
                self.reply(404, {"isError": True, "message": "Not found."})

        def do_POST(self):
            parts = urlparse(self.path).path.strip("/").split("/")
            if len(parts) != 4 or parts[:2] != ["api", "geohotspots"] or parts[3] != "trigger":
                self.reply(404, {"isError": True, "message": "Not found."})
                return
            auth = self.headers.get("Authorization", "")
            token = auth[7:] if auth.startswith("Bearer ") else ""
            avatar_id = tokens.get(token)
            if not avatar_id:
                self.reply(401, {"isError": True, "message": "Invalid local test identity."})
                return
            try:
                size = int(self.headers.get("Content-Length", "0"))
                body = json.loads(self.rfile.read(size))
                key = body.get("idempotencyKey", "")
                if not key:
                    raise ValueError("idempotencyKey is required")
                status, payload = store.trigger(parts[2], avatar_id, key)
                self.reply(status, payload)
            except (ValueError, json.JSONDecodeError) as exc:
                self.reply(400, {"isError": True, "message": str(exc)})

        def log_message(self, fmt, *args):
            return

    return Handler


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--port", type=int, required=True)
    parser.add_argument("--database", type=Path, required=True)
    parser.add_argument("--hotspot-id", required=True)
    parser.add_argument("--tokens", type=Path, required=True)
    parser.add_argument("--delay-ms", type=int, default=0)
    args = parser.parse_args()
    tokens = json.loads(args.tokens.read_text(encoding="utf-8"))
    server = ThreadingHTTPServer(("127.0.0.1", args.port), handler(Store(args.database, args.hotspot_id, args.delay_ms), tokens))
    server.serve_forever()


if __name__ == "__main__":
    main()
