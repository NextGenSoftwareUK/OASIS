# Custom value conversion implementation candidate

23 November 2026. Qualification pending; no Development acceptance or full M12 certification.

The invariant is one explicit application conversion for stored metadata, captured native query parameters and typed returned values. Native providers compare/order their admitted storage scalars; queries never execute a conversion callback to filter source records in memory.

The new model API is Property(selector).HasConversion(new CosmicValueConverter<TModel,TProvider>(toProvider,fromProvider,semantics)).HasMetadataKey(key). Equality is the default declared contract; native ordering requires the explicit Ordering declaration. Converter callbacks are trusted application model code, not client-supplied expressions. Native storage type admission remains the existing compiler's responsibility. Nulls propagate without invoking callbacks and must match both declared domains. Non-null values cannot encode/decode to null.

The immutable model, including named/all-filter bypass copies, retains converters. Save extraction uses the existing metadata owner; typed hydration decodes the exact provider type before conversion and completes all conversions before setters. Query comparisons/membership encode closed values once into native immutable scalar literals. Projection buffers keep their native proof domains; visible cells decode before DTO construction. Set operands require the same model and corresponding converter instances; set output retains mapping for post-set expressions.

Converted arithmetic, computed string/date projections and selected extrema are not silently evaluated against transformed storage units. Their remaining admission/lowering work is explicit; mutable custom comparer policies and arbitrary serialization contracts are still open. Full framework parity is not claimed.

The representative native fixture NativeValueConversionsRoundtripFilterProjectAndTrack uses the real shipping MongoDB, SQLite and Neo4j providers and the existing isolated fixture owner. Each journey expects8 evidence groups,8 timed operations,2 acknowledged writes and1 exact cleanup identity. It exercises int-to-scaled-Int64 and mutable record-to-string mappings, typed roundtrip, native equality/range/order, selected filtering/projection, membership, set composition, foreign caller exclusion, immutable model capture and acknowledged tracked update.

The new workflow qualifies that fixture and builds actual WEB4 once under the existing shared provider build lease. Accepted previous cohorts are retained rather than repeated. No shared Atlas data, application/PIPER data, live settings or public promotions are involved.
