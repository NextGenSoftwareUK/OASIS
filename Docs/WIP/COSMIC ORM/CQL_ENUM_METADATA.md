# CQL enum metadata

## Contract

SQLite explicitly registers `HolonType`, `ProviderType` and their nullable forms as metadata scalar domains. These declarations support comparisons, membership, field comparisons and ordering through the common Core query model. Other enum types remain unsupported by the production profile; a caller-supplied broader profile cannot enable their native execution. Broader enum enrollment and application schema registration remain roadmap work.

The native domain consists of JSON integers whose values are defined by the declared enum. Aliases share one numeric identity. Undefined integers, enum-name strings, booleans, fractional values, arrays and objects are outside the domain. A numeric value can belong to multiple enum domains because the existing JSON representation has no CLR enum type tag. A declared typed path supplies its interpretation. Persisted unknown values remain intact in the record; querying does not rewrite them.

The standard metadata missing/null contract applies. Nonnullable typed comparisons do not match missing or invalid-domain values, including for inequality. Nullable equality to null matches explicit JSON null only. Nullable membership may include explicit null. NOT complements the complete predicate, so it also includes missing/invalid-domain records unless another predicate restricts them. Fields must have the same declared underlying CLR enum type to compare; numeric overlap does not permit comparing different enum types.

Core already validates enum literals and membership values without executing application getters or delegates. SQLite's native guard uses trusted enum-schema numeric constants and is shared by all four operation families. No client evaluation, deserialization callback, arbitrary SQL or enum-name coercion is introduced. `EnumValue<TEnum>` objects stored as objects are not numeric enum scalars.

## Example

```csharp
var kinds = new HolonType?[] { null, HolonType.Quest };
var query = CosmicQuery<Holon>.Create()
    .Where(h => kinds.Contains(CosmicQueryMetadata.Value<HolonType?>(h, "kind")))
    .OrderBy(h => CosmicQueryMetadata.Value<HolonType?>(h, "kind"));
if (query.IsError) throw new InvalidOperationException(query.Message);
```

Plan against an already authorized SQLite target and execute through HolonManager as shown in [the CQL guide](CQL_GETTING_STARTED.md). An adapter capability is not an authorization grant.

## Verified SQLite acceptance

Both new native cases fail against the prior provider because enum metadata capability is missing (`TestResults/SQLiteCurrent/cql-enum-domains-red.trx`). The fixed focused gate passes7/7, zero skips (`cql-enum-domains-focused.trx`). It exercises both enum types, boundary defined values, undefined integer gaps, all six comparisons, nullable membership, field comparisons and descending ordering. Existing unsupported-type tests now use the still-unregistered `KarmaSourceType` so forged-plan rejection remains tested. Decimal, double, canonical audit-date and mixed coercion rejection remain covered.

The complete SQLite suite passes1176/1176 with zero failures/skips in2m4s (`TestResults/SQLiteCurrent/cql-enum-domains-full.trx`). Eight declared Core/Shared/SQLite producer and consumer copies match; report hash, counters and full assembly identities are retained in `TestResults/SQLiteCurrent/cql-enum-domain-artifacts.json`. SQLite SHA256 is `D920404790458E53B634AE7BD83FEEC7A363FE9CA2402B3169913117527B830C`; Shared is `942F1FDD6CBE124752C3F21538B63FDEDB67167B350588ACD85FB1DF988823F1`; Core is `EB9EFAD0C29B70A546ADD34AD651F9EB21F7A86341CD30693251729EBD0759AC`. The owned disposable database is absent after teardown.

No Core or Shared source change belongs to this increment. MongoDB enum enrollment and broader enum/application schema registration remain open; integer enrollment alone does not grant enum metadata support.
