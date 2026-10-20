# Enabled ONODE COSMIC replication configuration

Updated 19 October 2026. Local implementation; native/application verification in progress. No release or deployment claim.

## Opt-in startup

ONODE Startup registers the configured worker after the shared OASIS initialization service. `COSMIC_HOLON_REPLICATION_JSON` is trusted deployment configuration, not a request-body API. An absent value or exactly `{"enabled":false}` leaves the feature disabled and performs no COSMIC provider resolution. Unknown/duplicate fields, implicit booleans, numeric/default authorities, invalid or empty IDs, empty/boundedness violations and missing/expired UTC grants are rejected during registration before provider I/O.

```json
{
  "enabled": true,
  "workerId": "onode-eu-1",
  "catalogs": [{
    "id": "application-replicas",
    "version": "cc157ef4-0a97-49d4-a7bc-f4eedc6fc969",
    "sourceId": "application-primary",
    "storeIdentity": "primary-sqlite",
    "sourceProvider": "SQLLiteDBOASIS",
    "targets": [{
      "name": "mongo-eu",
      "endpointIdentity": "registered-mongo-eu",
      "provider": "MongoDBOASIS",
      "actors": ["65f7c683-57e0-44ae-8dba-312bd2ee96e0"],
      "expiresAtUtc": "2026-12-01T00:00:00.0000000Z"
    }]
  }]
}
```

Dates above are examples; configured grants must be unexpired at startup. Credentials and provider connection settings remain in the existing provider configuration. Registry lookup currently has one instance per ProviderType, so self replication is rejected and one registered source type cannot declare conflicting source/store bindings. Multiple catalog versions may retain the same source binding. A registered target type must have one endpoint descriptor within this configuration.

## Pinned factory and lifecycle

`HolonReplicationCatalogFactory` resolves every explicit authority and validates actual ProviderType and required capabilities before activating any provider. Sources require atomic replication and provider-bound outbox capture; targets require committed-replica application. Missing/wrong/unsupported bindings fail instead of selecting the default or a failover substitute. It activates only inactive pinned providers, checks acknowledgements and actual active state, and rejects authority drift during activation.

The factory captures one native source store per provider, builds immutable versioned catalogs/actor policies and probes the exact target heads before publishing the catalog snapshot. Source readiness errors or uncertain warnings fail startup. A valid persisted reconciliation head remains a degraded operational state, allowing the running host/operator surface to expose it. Grant expiry is checked before activation, after activation and after readiness validation. Source schema must already exist where required; the factory does not create an alternate queue or repair data.

`ConfiguredHolonReplicationHostedService.StartAsync` first awaits the existing cached shared OASIS initialization task, then awaits this process before returning, so global BackgroundServiceExceptionBehavior.Ignore cannot turn invalid enabled configuration into successful startup. It starts and observes the existing worker, exposes its immutable Catalogs for obtaining the actual versioned Enrollment, and joins shutdown. It does not deactivate/dispose the platform-owned shared providers. Programmatic and configured registration are mutually exclusive; neither can introduce a second owner for the same queues.

## Readiness and boundaries

The existing named check drives `/health/cosmic/ready`. It is503 until the configured worker and native heads are healthy, and for persisted reconciliation or source failure; disabled/unconfigured hosting returns not-configured. The response contains no configuration, diagnostic exceptions, credentials, business payloads or lease tokens.

Endpoint/store identities are trusted descriptors, not cryptographic proof of the provider object's physical endpoint. The registry/provider lifetime owner must keep those objects stable. Actor allowlists do not replace tenant/record/encryption authorization or authenticated source admission. Default ordinary CRUD is not automatically enrolled: source saves must use the configured catalog.Enrollment. Persisted catalog/grant migration, automatic backfill/retargeting, process-loss journals, authenticated reconciliation/resume, retention/queue/metrics, broad graph/history/provider certification, migration/placement, deltas and CQL remain open. Legacy Hyperdrive is unchanged.

## Evidence

The full Shared suite passes685, including28 configuration/factory cases covering initialization ordering, activation drift and both double-registration orders. Native configured-host tests use initially inactive Neo4j/SQLite providers and real queues to verify activation, source capture, authorized automatic creation/update/deletion and receipt replay. Both native configured-host cases pass with zero skips; the full ONODE build passes with zero errors and253 warnings. Final binary hashes and terminated-run evidence are recorded in VERIFICATION.md.
