# Neo4j grouped input normalization

19 November 2026. Source prepared; CI qualification pending.

The grouped compiler previously expanded the same typed projection once per aggregate, including ordinary and distinct counts of the same field. The measured Single query contained20,283 UTF8 bytes and26 parameters. Reuse each identical input state/value expression within one compilation. Identity includes metadata/canonical distinction, ordinal path segments, scalar kind and exact declared CLR type including nullable/enum identity. The cache lives only during compilation and holds no data. Per-column reducer aliases, result packets, input/type proofs, payload budgets and off-page error checks remain independent.

New native case checks ordinary/distinct counts, sum/min/max/mean, canonical versus metadata Name, ordinal stock/Stock separation, and required/null rejection despite an empty output page using four owned records. Re-run the affected cold/warm/profile diagnostic because shipping compiler source changed; require its actual native query below16,000 bytes (over21percent smaller than the accepted baseline), identical results, no PROFILE updates and exact cleanup. Cold/warm latency remains measured evidence, not a preset speed claim or a warm-up workaround.

No Core/DNA/worker/pin changes, shared Atlas requests, PIPER changes, local builds or deployment are part of this source publication.
