"""Build the WEB4 sandbox OASIS_DNA_JSON from the dev WEB4 DNA.

The sandbox is public, so it keeps only its own MongoDB, SQLite and Neo4j providers and drops every
other provider's configuration (chain keys, wallets, cloud credentials). It gets its own JWT secret,
so sandbox tokens are never valid on dev or production, and sends no email.

Usage:
    railway variables --service "WEB4 OASIS API" --environment dev --json > dev-vars.json
    python Scripts/Sandbox/build_sandbox_dna.py dev-vars.json > sandbox-dna.json

Mongo and Neo4j connection strings are Railway variable references, resolved by Railway at deploy time.
"""
import json
import secrets
import sys

SANDBOX_PROVIDERS = ["MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS"]
PROVIDER_LIST = ", ".join(SANDBOX_PROVIDERS)


def build(dev_dna: dict) -> dict:
    dna = json.loads(json.dumps(dev_dna))
    oasis = dna["OASIS"]

    oasis["IsProduction"] = False
    oasis["HyperDriveMode"] = "Legacy"
    oasis["OASISAPIURL"] = "https://sandbox.api.web4.oasisomniverse.one"

    security = oasis["Security"]
    security["SecretKey"] = secrets.token_urlsafe(48)
    security["OASISProviderPrivateKeys"] = {}
    security["RequireApiKey"] = False
    security["ApiKey"] = ""

    email = oasis["Email"]
    email["DisableAllEmails"] = True
    email["SendVerificationEmail"] = False
    for key in ("SmtpPass", "ResendKey"):
        email[key] = ""

    storage = oasis["StorageProviders"]
    for name, value in list(storage.items()):
        if isinstance(value, dict) and name not in SANDBOX_PROVIDERS:
            storage[name] = None
    storage["MongoDBOASIS"] = {"DBName": "OASISAPI_SANDBOX", "ConnectionString": "${{MongoDB.MONGO_URL}}"}
    storage["SQLLiteDBOASIS"] = {"ConnectionString": "Data Source=/app/sandbox/oasis-sandbox.db"}
    storage["Neo4jOASIS"] = {"Username": "neo4j", "Password": "${{Neo4j.NEO4J_PASSWORD}}",
                             "ConnectionString": "bolt://${{Neo4j.RAILWAY_PRIVATE_DOMAIN}}:7687"}
    storage["AutoFailOverEnabled"] = True
    storage["AutoReplicationEnabled"] = True
    storage["AutoLoadBalanceEnabled"] = True
    for key in ("AutoFailOverProviders", "AutoReplicationProviders", "AutoLoadBalanceProviders"):
        storage[key] = PROVIDER_LIST
    for key in [k for k in storage if k.startswith("AutoFailOverProvidersFor")]:
        storage[key] = PROVIDER_LIST

    hyperdrive = oasis.get("OASISHyperDriveConfig")
    if isinstance(hyperdrive, dict):
        hyperdrive["IsEnabled"] = True
        hyperdrive["AutoFailoverEnabled"] = True
        hyperdrive["AutoLoadBalancingEnabled"] = True
        for key in ("EnabledProviders", "AutoFailoverProviders", "AutoReplicationProviders", "LoadBalancingProviders"):
            hyperdrive[key] = list(SANDBOX_PROVIDERS)
        configs = hyperdrive.get("ProviderConfigs")
        if isinstance(configs, dict):
            template = configs.get("MongoDBOASIS")
            hyperdrive["ProviderConfigs"] = {
                p: {**(template or {}), "ProviderType": p} for p in SANDBOX_PROVIDERS
            } if template else configs

    return dna


if __name__ == "__main__":
    variables = json.load(open(sys.argv[1], encoding="utf-8"))
    print(json.dumps(build(json.loads(variables["OASIS_DNA_JSON"])), indent=2))
