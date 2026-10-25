# CQL join HTTP contract — compiler checkpoint

Date:25 October2026. Status: **request model/compiler verified; route not registered**.
This preserves the full remaining transport acceptance scope.

## Intent

The proposed `POST /api/data/query-cql/join` request selects one host-eligible
provider (or its current default), an `Inner`/`Left` kind, independent `Left`/`Right`
source filters/order/windows,1–8 typed key pairs and1–32 selected columns.
`CqlJoinV1Request` and nested objects reject unknown members. Actors and provider
eligibility come from admission; sources cannot override them. Requests contain
bounded descriptions, never C#, SQL, Cypher, expression strings or provider plans.

Each key has an alias and explicit left/right `CqlScalarV1Selection` paths.
Output columns name `Left`, `Right` or `RightPresence`. A path column requires a
selection; `AllowMissing` applies only to right outputs. `WhenMissing` applies only
to presence. Right required value types on a left join need explicit absence
lifting. Source metadata nullability and right-row absence are independent.

Maximum input rows are1–1000 per side, pair work1–10000, output rows1–100.
Left/right/output payload limits are each1MiB. These limits reject overflow rather
than implement a result page. Source Skip/Take have the existing CQL source
semantics. Result composition/paging and distributed joins remain separate work.

## Verified compiler controls

The linked production sources pass18 new controls in full Core2,771, zero skips.
Both actor/provider bindings, independent windows/budgets, required versus lifted
right paths, malformed/ambiguous shapes, unknown JSON members and mutable request
isolation are covered. Core runtime named bindings reuse the common join owner.

## Required before exposure

- Register one authorized controller/endpoint action and exact query-route/tariff leaf.
- Reuse existing prepared admission/provider eligibility and physical query-attempt owner.
- Plan against actual adapter capabilities before dispatch; reject unsupported graph/snapshot domains.
- Serialize bounded columns/rows with existing exact scalar wire tags and independent right presence.
- Cover unauthorized/foreign actor/provider, malformed/oversized requests, cancellation and budgets.
- Run signed native SDK/HTTP acceptance on MongoDB, SQLite and Neo4j, with receipt/replay checks.
- Update commented C#/HTTP/browser/Swagger examples, runbook, capability matrix and evidence.

SDK flat joins are already available on all three stores. This compiler checkpoint
does not claim the planned HTTP endpoint exists or close the full CQL roadmap.
