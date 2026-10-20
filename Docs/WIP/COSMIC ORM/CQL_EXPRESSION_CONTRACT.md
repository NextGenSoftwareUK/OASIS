# CQL expression contract: first implementation milestone

Latest shared scalar-property increment (20 October 2026): exact trusted `CustomOASISProperty` scalar members now compile to literal metadata paths without invoking getters or attribute constructors. Inherited/overridden markers are supported; unmarked, read-only, canonical-shadowing and StoreAsJsonString members remain explicit unsupported paths. The seven original positive failures now pass with five safety controls unchanged, inside complete Core 1,536/1,536. MongoDB and SQLite execute these same paths natively; [acceptance and remaining default/schema semantics](CQL_MONGO_NATIVE.md).

For the current usage overview, start with [CQL getting started](CQL_GETTING_STARTED.md). The checkpoint sections below retain their original scope and evidence; later native/manager implementation supersedes earlier statements that execution is unimplemented. Complete WEB4 authorization/routing, additional adapters and the remaining CQL roadmap are still open.

20 October 2026. Predicate compilation and immutable typed query composition are implemented; database query execution remains incomplete. It performs no provider I/O and does not satisfy the complete CQL roadmap.

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

## Typed composition and scope

`CosmicQuery<T>.Create()` defaults to explicit `CurrentDefault` intent. `CosmicQueryScope.Named(...)` returns an `OASISResult` and rejects null/empty/duplicate/undefined providers, routing sentinel values and more than 64 identities. Named provider order is retained, with a detached immutable copy. `AllEligible` is separate planner intent; it does not authorize every registered provider.

`Where(expression)` returns an `OASISResult<CosmicQuery<T>>`, freezes the expression immediately and appends an AND predicate to a new query. Previous queries remain unchanged. Compiler errors preserve their code and exception; at most 32 predicates can be composed, each subject to the compiler limits. `WithScope` produces a new query and requires a non-null scope. Scope is unresolved until planning; no provider registration, capability, tenant authorization or I/O is implied.

Five additional composition cases pass in the full **1,303/1,303** Core gate, zero skips (`TestResults/CosmicMongoCurrent/cql-composition-core-verified.trx`). Both target frameworks build with zero errors. This extends the earlier compiler evidence, without certifying execution.

## Capability planning boundary (pure admission verified)

`CosmicQueryCapabilities` describes admitted logical operators, metadata/null support, exact CLR path types and comparison operators. Composition across multiple `Where` clauses requires AND capability. Nullable types remain distinct from their underlying type. Capability arrays are detached and immutable. An exact canonical-field profile additionally binds each field to its declared CLR type and admitted operators; type-wide profiles do not imply adapter certification. SQLite publishes its exact profile from the same field map used by native translation.

`CosmicQueryPlanner.Plan` operates on a supplied, already authorized eligible target snapshot. It performs no registration discovery, policy evaluation or storage I/O. Current-default scope resolves to exactly one listed target, named scope preserves requested order, and all-eligible scope selects the entire snapshot. Missing targets or an unsupported selected target reject the whole plan; no target is silently dropped, no client evaluation is substituted, and an empty target selection is an error. Target snapshots are bounded to 64 unique identities.

Five capability/planning cases pass in full Core **1,308/1,308**, zero skips (`TestResults/CosmicMongoCurrent/cql-planning-core-verified.trx`); both target frameworks build with zero errors.

These declarations are admission prerequisites. Adapters must still validate exact provider-native translation semantics, and the WEB4 execution boundary must obtain and enforce actor/tenant/visibility/residency/encryption/cost authorization. A caller-supplied capability profile is not authorization or native certification. Native translation, parameter binding, execution and paging remain required.

## Required next work

- Integrate the pure scope/capability planner with an authenticated, authorized WEB4 target snapshot and certified native adapters.
- Additional admitted operators, including inclusion, field comparisons, relationship predicates, custom persisted property mappings and remaining string/collation modes.
- Versioned query envelopes, capability-aware plans and explanations, authorized tenant/visibility/residency/encryption/cost admission.
- Native parameterized adapters, beginning with the selected stores; unsupported pushdown must fail before I/O. Bounded client evaluation requires explicit opt-in.
- Ordering/projection/paging/aggregates, stable pagination policy, async execution and cancellation.
- Federated identity/version deduplication, provenance, conflict/freshness policy and explicit partial failures.
- Equivalent native-provider results, injection/resource/security tests and runnable end-to-end examples.

Provider scope intent is implemented; authorization enforcement, query execution and native certification remain unimplemented. Committed per-save deltas and the earlier unfinished roadmap stages remain independent required work.

## Native adapter checkpoint

The [SQLite native adapter](CQL_SQLITE_NATIVE.md) now has eight focused native cases passing, including independent CLR result comparison, unsupported-before-I/O checks, cancellation, injection-like text and result budgets. Full SQLite823/823 passes127s, zero skips. It is a direct provider seam, without WEB4 authorization or manager integration; complete CQL acceptance remains open.

Exact field capability checkpoint: full Core1,310/1,310 and SQLite824/824 pass with zero skips. SQLite publishes `CqlCapabilities`; both its native grammar and exact planner profile derive from one field/operator map. Unmapped fields reject in planning rather than inheriting support merely from another field with the same CLR type. The native translator also rejects overbroad caller profiles before I/O. These gates do not authorize caller-created plans or close WEB4 integration.

Metadata presence/null checkpoint: `CosmicQueryMetadata.Exists(h, "key")` and `IsNull(h, "key")` compile into immutable predicates and execute natively in SQLite, including literal nested object keys. Exists includes explicit null; IsNull matches explicit JSON null only, with missing false for both. Separate capability flags do not admit scalar metadata Value<T> translation. FullCore1,313/1,313, focusedSQLite17/17 and fullSQLite832/832 pass, zero skips. Native scalar metadata comparisons, WEB4 policy integration and remaining CQL acceptance stay open.

Shared execution checkpoint: [the native adapter contract and executor](CQL_EXECUTION_PIPELINE.md) preflight every selected adapter before I/O, enforce total row budgets, preserve source/error provenance and detach canonical results. FullCore1,320/1,320 and SQLite833/833 pass, zero skips. This is an orchestration seam; authenticated WEB4 selection, typed manager integration and federation policies remain required.

Typed manager checkpoint: `HolonManager.ExecuteQueryAsync<T>(plan, adapters, maximumResults, cancellationToken)` returns V2 typed leaf entities with private clean tracking, strict metadata hydration and retained provider groups. FullCore1,337/1,337 and SQLite845/845 pass, zero skips, including no-op-save revision preservation and no partial typed publication. The plan/adapter snapshot still requires trusted authorization; automatic entity-kind mapping, relationship options and complete WEB4 policy/routing remain open. See [execution pipeline](CQL_EXECUTION_PIPELINE.md).

Ordinal string checkpoint: Contains(string), Contains(string,StringComparison.Ordinal) and explicit-ordinal StartsWith/EndsWith compile to immutable string nodes with per-field operator admission. Null patterns, culture-sensitive prefix/suffix defaults, other comparison modes, invalid Unicode and oversized patterns reject before I/O. String matching treats null receivers as false; NOT follows two-valued CQL semantics. SQLite supports Name/Description through bound BLOB operations, preserving Unicode sequences/NUL and literal wildcard text. Empty patterns match non-null values. FullCore1,341/1,341 and SQLite874/874 pass, zero skips; scalar metadata strings remain unsupported by SQLite. See [native contract](CQL_SQLITE_NATIVE.md).

Bounded membership checkpoint: `ids.Contains(h.Id)` and initialized scalar-array variants compile to immutable membership nodes. Arrays are one-dimensional/zero-based, at most128 entries, with element types exactly matching the path. SDK Enumerable/ReadOnlySpan array lowerings normalize without invocation; non-null comparers and arbitrary enumerables reject. Empty sets are false; explicit null entries match null and NOT complements the complete predicate. Equality, string and metadata-path text now share valid-Unicode admission. Six new Core cases pass in full1,347/1,347; native26 focused and full900/900 SQLite cases pass0skips. Mixed numeric comparison kinds require explicit adapter capability; SQLite now admits exact decimal/double comparisons for canonical Int32 fields and Int32-backed enum storage; other numeric path types remain unsupported. Full scalar metadata, broader collection forms and other original acceptance remain open.


## Exact SQLite mixed numeric comparisons — 20 October 2026

SQLite now declares MixedNumericComparison for its admitted canonical Int32 fields (Version and Int32-backed HolonType storage). Decimal/double literals are compared in their original precision against Int32 boundaries, then translated to exact integer bounds using floor/ceiling. Fractional equality is false; fractional inequality matches non-null values. Out-of-range literals reduce to the correct constant/non-null predicate. Only exact integer parameters reach SQLite, preventing REAL affinity from rounding a high-precision decimal into a false match. Unsupported numeric path types still reject explicitly.

Six focused native cases pass, covering 420 comparisons against separately compiled CLR predicates plus the decimal equality regression. Cases include six operators, reversed operands, fractional/negative values, floating-point neighbors, Int32 boundaries and enum storage. Full SQLite regression passes 905/905 with zero skips; the owned fixture is absent after teardown. This does not certify arbitrary numeric metadata, undefined enum literals, other adapters or the unfinished security/graph/federation roadmap.


## Exact scalar metadata capability admission — 20 October 2026

`CosmicQueryMetadataCapability` declares an exact CLR metadata path type, its comparison and string operators, and separate membership/mixed-numeric semantics. The exact `CosmicQueryCapabilities(features, canonicalFields, metadataTypes)` profile freezes these declarations and rejects duplicate/null descriptors. Canonical and metadata capabilities cannot grant operations to one another. Nullable types remain distinct; global feature gates still apply. Existing exact field-only profiles declare no scalar metadata support. The broad type-based constructor remains a declaration mechanism and does not certify native adapters.

Five new cases verify operator/type separation, metadata-only profiles, nullable admission, membership/string/mixed-numeric gates and immutable descriptor ownership. Full Core 1,352/1,352 passes with zero skips, and both target frameworks build with zero errors. This is admission groundwork: SQLite scalar metadata translation is not enabled by this checkpoint. Native missing/null/type semantics and the full original roadmap remain required.

Native compatibility verification on the new Core contract: SQLite 905/905 passes in117s, zero skips, with fixture absent after teardown (`sqlite-cql-metadata-profile-full.trx`). No scalar metadata native support is claimed.


## Native SQLite scalar metadata checkpoint — 20 October 2026

The [typed scalar metadata contract](CQL_SCALAR_METADATA.md) adds native comparisons, membership and ordinal string matching for string, boolean, Int32 and Int64 metadata, including nullable value types. The production profile and translator share exact type/operator declarations. Missing and wrong-type paths remain distinct from explicit JSON null; integer bounds and SQLite runtime types are checked before comparison. Arrays and JSON-encoded strings are not traversed as objects. Shared native string/membership helpers preserve existing canonical field behavior. Unsupported metadata types and mixed numeric metadata comparisons reject before I/O, including with overbroad caller plans.

The compiler also handles method-free built-in nullable scalar literal lifting, fixing the nullable boolean comparison exposed by the original red fixture. Full Core 1,353/1,353 and focused native 43/43 pass with zero skips. The original 26-case failure report is retained. Native decimal/double/GUID/date/enum metadata, authenticated WEB4 policy/routing, broader adapters, graph queries, paging/aggregates/federation and all unfinished original roadmap stages remain required.

Final native regression: SQLite 948/948 passes125s, zero skips (`sqlite-cql-scalar-metadata-full-verified.trx`), with the fixture absent after normal teardown. Full Core1,353/1,353 and focused native43/43 belong to the recorded matching Core/provider epoch.
