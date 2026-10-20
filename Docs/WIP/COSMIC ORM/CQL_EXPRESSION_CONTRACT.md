# CQL expression contract: first implementation milestone

20 October 2026. This is a predicate compiler, not a working database query API. It performs no provider I/O and does not satisfy the complete CQL roadmap.

## Implemented

`CosmicQueryExpression.Compile<T>(Expression<Func<T, bool>>)` returns an `OASISResult<CosmicQueryPredicate>`. The immutable representation supports AND/OR/NOT, boolean constants and fields, equality/inequality, ordered comparisons and null equality tests over canonical scalar holon fields. Reversed comparisons normalize to field/operator/literal form.

Captured primitive values are frozen during compilation. String values remain literals; no native query text is constructed. The compiler does not compile or execute delegates, property getters, arbitrary methods or user comparison operators. Unsupported syntax returns `CQL_EXPRESSION_UNSUPPORTED`; invalid bounds or scalar values return `CQL_EXPRESSION_INVALID`.

Metadata uses an explicit typed path marker:

```csharp
var minimum = 7;
var compiled = CosmicQueryExpression.Compile<Holon>(h =>
    h.Version >= minimum &&
    (h.Name == "example" ||
     CosmicQueryMetadata.Value<decimal>(h, "stats", "rating") > 10.5m));
```

The marker is valid only inside a CQL expression. This example produces a detached predicate; it does not execute a query. Metadata segments remain separate values rather than provider-native path fragments.

Scalar kinds are null, string, boolean, signed integer, decimal, finite floating point, GUID and UTC DateTime. Paths also retain their exact declared CLR value type, including nullable and numeric-width information, for later capability admission. Enum comparisons validate defined values; nullable metadata null tests and interface roots are covered. Numeric widening is admitted conservatively only when lossless. Lossy field casts, arbitrary root properties and shadowed canonical properties are rejected rather than translated with different meaning.

Current limits: 32 traversal levels, a 256-visit compiler work budget, 4,096 characters per string literal, and one to sixteen metadata segments of at most 128 characters. These are initial admission limits, not provider capacity claims.

## Verification

Nine compiler cases pass within the full Core **1,298/1,298**, zero failures/skips (`TestResults/CosmicMongoCurrent/cql-expression-final-verified.trx`). Both net10.0 and netstandard2.1 build successfully. Tests cover closure mutation after capture, literal SQL-like text, immutable metadata paths/decimal precision, reversed/null/enum/boolean comparisons, interface roots, nullable metadata, rejected getters/methods/shadow properties, lossy casts, invalid dates/nonfinite numbers and bounded deep expressions.

The initial enum expression shape and netstandard compatibility-name collision were fixed at the compiler boundary; no compatibility shim or provider fallback was added. Exact source/assembly evidence is in the integration boundary.

## Required next work

- Typed query composition and explicit current/named/all-eligible provider scope.
- Additional admitted operators, including string/inclusion, field comparisons, relationship predicates and custom persisted property mappings.
- Versioned query envelopes, capability-aware plans and explanations, authorized tenant/visibility/residency/encryption/cost admission.
- Native parameterized adapters, beginning with the selected stores; unsupported pushdown must fail before I/O. Bounded client evaluation requires explicit opt-in.
- Ordering/projection/paging/aggregates, stable pagination policy, async execution and cancellation.
- Federated identity/version deduplication, provenance, conflict/freshness policy and explicit partial failures.
- Equivalent native-provider results, injection/resource/security tests and runnable end-to-end examples.

No provider scope, authorization enforcement, query execution or native certification is implied by compiler success. Committed per-save deltas and the earlier unfinished roadmap stages remain independent required work.
