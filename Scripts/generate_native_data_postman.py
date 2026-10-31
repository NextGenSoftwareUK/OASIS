#!/usr/bin/env python3
"""Generate the WEB4 Native Data / Sandbox / CQL Postman collection without embedding any credentials.

Run: py -3 Scripts/generate_native_data_postman.py
"""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "Docs/Devs/API Documentation/WEB4 OASIS API/WEB4-NativeData-Sandbox-CQL.postman_collection.json"

MONGO_CONNECTION = {"uri": "{{mongoUri}}", "database": "{{mongoDatabase}}"}
MAPPING = {"sourceName": "{{sourceName}}", "nameField": "Name", "excludedFields": ["internalNotes"]}


def item(name, method, path, body=None, description=None, tests=None, headers=None, bearer=True):
    header = [{"key": "Content-Type", "value": "application/json"}] + (headers or [])
    if bearer:
        header.append({"key": "Authorization", "value": "Bearer {{bearerToken}}"})
    request = {"method": method, "header": header, "url": "{{web4BaseUrl}}" + path}
    if description:
        request["description"] = description
    if body is not None:
        request["body"] = {"mode": "raw", "raw": json.dumps(body, indent=2), "options": {"raw": {"language": "json"}}}
    result = {"name": name, "request": request}
    if tests:
        result["event"] = [{"listen": "test", "script": {"type": "text/javascript", "exec": tests}}]
    return result


def folder(name, items, description=None):
    result = {"name": name, "item": items}
    if description:
        result["description"] = description
    return result


def status_ok(label="OK"):
    return [f"pm.test('{label}', () => pm.expect(pm.response.code).to.be.oneOf([200, 201]));"]


def collection():
    sign_in = item("Sign in (stores bearerToken)", "POST", "/api/avatar/authenticate",
                   {"username": "{{username}}", "password": "{{password}}"}, bearer=False,
                   description="Sign in with your own avatar. Put username and password in your Postman environment, never in the collection.",
                   tests=["const body = pm.response.json();",
                          "const avatar = (body.result && body.result.result) || body.result || {};",
                          "if (avatar.jwtToken) pm.collectionVariables.set('bearerToken', avatar.jwtToken);",
                          "pm.test('Signed in', () => pm.expect(avatar.jwtToken).to.be.a('string'));"])

    sandbox = folder("Sandbox (only where DNA OASIS.Sandbox.Enabled is true; otherwise 404)", [
        item("Start a demo session (creates a throwaway avatar and signs it in)", "POST", "/api/sandbox/session", bearer=False,
             description="Creates a verified demo avatar named <prefix><8 hex> that is deleted with all its data after DNA OASIS.Sandbox.DemoAvatarRetentionMinutes. "
                         "Limited per address per hour and per day (HTTP 429).",
             tests=["const body = pm.response.json();",
                    "const avatar = body.result || {};",
                    "if (avatar.jwtToken) pm.collectionVariables.set('bearerToken', avatar.jwtToken);",
                    "pm.test('Demo session', () => pm.expect(pm.response.code).to.equal(200));"]),
        item("Providers this avatar has taken offline", "GET", "/api/sandbox/faults"),
        item("Requests each offline provider has refused", "GET", "/api/sandbox/refusals"),
        item("Take a provider offline (5-120 seconds)", "POST", "/api/sandbox/providers/{{provider}}/offline?seconds=30",
             description="Only for your own requests; nobody else is affected."),
        item("Bring a provider back online", "POST", "/api/sandbox/providers/{{provider}}/online"),
        item("Write the demo shop into the sandbox native-apps databases", "POST", "/api/sandbox/native-demo",
             description="Idempotent. Writes customers, products and orders into the demo Mongo, SQLite and Neo4j stores so Native Data can import them."),
    ])

    native_get = [
        item("Providers that can import a connection you supply", "GET", "/api/native-data/providers", tests=status_ok()),
    ]
    demo = folder("Demo store (no connection: Wizard avatar, or the sandbox)", [
        item("Sources of the server's configured demo store", "GET", "/api/native-data/{{provider}}/sources"),
    ])
    own = folder("Your own database (send a connection)", [
        item("List sources", "POST", "/api/native-data/{{provider}}/sources", {"connection": MONGO_CONNECTION},
             description="Nothing is saved. MongoDB shown: uri + database. Other providers take their own settings (see Docs/Devs/NATIVE_DATA_IMPORT.md): "
                         "Neo4j uri/username/password, SQLite fileName/fileBase64, Cosmos endpoint/key/database, SOLID podUrl/containers, "
                         "ActivityPub instanceUrl/accessToken, IPFS apiUrl/paths, Solana rpcUrl/programs."),
        item("Preview the first records and the holons they would become", "POST", "/api/native-data/{{provider}}/sources/{{sourceName}}/preview?count=5",
             {"connection": MONGO_CONNECTION, "mapping": MAPPING}, description="Saves nothing."),
        item("Import one source", "POST", "/api/native-data/{{provider}}/sources/{{sourceName}}/import",
             {"connection": MONGO_CONNECTION, "mapping": MAPPING, "pageSize": 200, "maximumRecords": 1000},
             description="Running it again updates the same holons instead of creating new ones."),
        item("Import every source", "POST", "/api/native-data/{{provider}}/import-all",
             {"connection": MONGO_CONNECTION, "mappings": {"{{sourceName}}": MAPPING}, "pageSize": 200, "maximumRecordsPerSource": 1000}),
    ])
    saved = folder("Saved connections (encrypted with DNA OASIS.NativeImport.ConnectionEncryptionKey; refused if no key is set)", [
        item("List saved connection names", "GET", "/api/native-data/{{provider}}/connections",
             description="Names only. The settings and secrets are never returned."),
        item("Save a connection", "PUT", "/api/native-data/{{provider}}/connections/{{connectionName}}", MONGO_CONNECTION,
             description="Body is the connection settings for that provider. Name: 1-64 letters, digits, spaces, '-' or '_'. Limited by DNA OASIS.NativeImport.MaximumSavedConnectionsPerAvatar."),
        item("List sources using a saved connection", "POST", "/api/native-data/{{provider}}/sources", {"connectionName": "{{connectionName}}"},
             description="Send connectionName instead of connection. Sending both is refused (NATIVE_IMPORT_CONNECTION_AMBIGUOUS)."),
        item("Preview using a saved connection", "POST", "/api/native-data/{{provider}}/sources/{{sourceName}}/preview?count=5",
             {"connectionName": "{{connectionName}}"}),
        item("Import using a saved connection", "POST", "/api/native-data/{{provider}}/sources/{{sourceName}}/import",
             {"connectionName": "{{connectionName}}", "pageSize": 200, "maximumRecords": 1000}),
        item("Import every source using a saved connection", "POST", "/api/native-data/{{provider}}/import-all",
             {"connectionName": "{{connectionName}}"}),
        item("Delete a saved connection", "DELETE", "/api/native-data/{{provider}}/connections/{{connectionName}}"),
    ])
    mappings = folder("Saved mappings (how a source becomes holons)", [
        item("List saved mappings for a provider", "GET", "/api/native-data/{{provider}}/mappings"),
        item("Save a mapping", "PUT", "/api/native-data/{{provider}}/sources/{{sourceName}}/mapping", MAPPING),
        item("Delete a mapping", "DELETE", "/api/native-data/{{provider}}/sources/{{sourceName}}/mapping"),
    ])
    native = folder("Native Data import", native_get + [demo, own, saved, mappings])

    cql_filter = {"field": "Name", "scalar": "String", "operator": "Contains", "value": "coffee"}
    idem = [{"key": "Idempotency-Key", "value": "{{$guid}}", "description": "Required; use a new value for each new intent. Reusing one is refused with 409."}]
    cql = folder("CQL (POST a bounded JSON query; at most 32,768 bytes)", [
        item("Basic query", "POST", "/api/data/query-cql",
             {"provider": "{{provider}}", "filters": [cql_filter], "orderBy": "Name", "descending": False, "skip": 0, "take": 10, "includeMetadata": False},
             headers=idem),
        item("Scalar (count)", "POST", "/api/data/query-cql/scalar",
             {"provider": "{{provider}}", "operation": "LongCount", "filters": [cql_filter]}, headers=idem),
        item("Projection", "POST", "/api/data/query-cql/projection",
             {"provider": "{{provider}}", "filters": [cql_filter], "columns": [{"alias": "name", "field": "Name"}], "take": 10}, headers=idem),
        item("Federation across providers", "POST", "/api/data/query-cql/federation",
             {"scope": "CurrentDefault", "providers": [], "identityMode": "PreserveCopies",
              "source": {"filters": [cql_filter], "orderBy": "Name", "take": 10}, "take": 25}, headers=idem,
             description="The other routes (indexed, indexed/scalar, grouped, join, composition) take the Cql*V1Request shapes in "
                         "ONODE/.../Models/Data and are documented in Docs/Devs/CQL_WEB4_AND_STAR_CLI.md."),
    ])

    return {
        "info": {
            "name": "WEB4 Native Data, Sandbox and CQL",
            "description": "Native data import (including bring-your-own and saved connections), the public sandbox, and CQL. Generated by "
                           "Scripts/generate_native_data_postman.py: edit the script, not this file. No credentials are stored here; set "
                           "username, password, mongoUri and the other variables in a Postman environment.",
            "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json",
        },
        "variable": [
            {"key": "web4BaseUrl", "value": "https://dev.api.web4.oasisomniverse.one"},
            {"key": "bearerToken", "value": ""},
            {"key": "username", "value": ""},
            {"key": "password", "value": ""},
            {"key": "provider", "value": "MongoDBOASIS"},
            {"key": "sourceName", "value": "customers"},
            {"key": "connectionName", "value": "my-database"},
            {"key": "mongoUri", "value": ""},
            {"key": "mongoDatabase", "value": ""},
        ],
        "item": [folder("Sign in", [sign_in]), sandbox, native, cql],
    }


if __name__ == "__main__":
    TARGET.write_text(json.dumps(collection(), indent=2) + "\n", encoding="utf-8")
    print(f"Wrote {TARGET.relative_to(ROOT)}")
