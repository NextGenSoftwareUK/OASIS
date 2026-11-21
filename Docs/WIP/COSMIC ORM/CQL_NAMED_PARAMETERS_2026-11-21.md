# Named SQL parameters — 21 November 2026

Website sourcea90ccc58d4d5f5377e2779880b9e9a4e86a2d872 on existing work/cosmic-cql adds named scalar parameters to CqlText.compile(text, discoveredProfiles, parameters). The editor exposes optional JSON below the SQL textarea; Preview request and Run query both pass those values through the same compiler and existing typed WEB4 request. Existing WEB6 review flows keep their ordinary parameter-free path.

Example query:

    SELECT Id, Name FROM MongoDBOASIS WHERE Name = @name AND Version >= @minimum LIMIT 5

Parameter JSON:

    {"name":"Coffee","minimum":2}

Names are case-sensitive. Supply scalar JSON strings, booleans, null or finite safe numbers. Preserve exact numeric digits with an object such as {"type":"INT64","value":"9007199254740993"} or {"type":"DECIMAL","value":"0.1234567890123456789012345678"}. Exact UINT64/SINGLE/DOUBLE spellings are admitted too; the existing field-domain parser still owns range/type/null grammar. A null parameter does not turn equality into IS NULL.

Parameters become individual literal tokens during lexing, not interpolated SQL fragments or a second parse. Text quotes are escaped as literal data and parameter markers inside strings/comments are untouched. Field/provider/alias syntax remains the existing grammar. Up to64 names,512characters/string and16KiB total bound-value text; the UI JSON itself is limited16KiB. Unknown used names, lossy integers, unsupported objects and accessors refuse before producing a request. Parameters do not change permissions, provider admission, filtering budgets or enable writes.

Focused RAM-only Node qualification:3passed/0failed;55existing Ask cases excluded by name filter. Actual compiler request equivalence exercised MongoDB/SQLite/Neo4j selections for quoted adversarial text, exactInt64 and Decimal, plus missing/lossy/accessor refusals and editor JSON preview/malformed-JSON refusal. Production JS syntax parsed. No local test output/files/archive/environment, native database or live API call. This is source/compiler/UI-controller acceptance, not rendered browser, hosted native or production publication acceptance.

No backend deployment change is required for the emitted existing typed requests. Current public production site and reviewed older preview remain unchanged; new source needs the existing authorized preview/publication workflow and changed-flow browser verification. Full SQL/EF/NHibernate/provider parity remains open.

## Review-only ready-provider example

Website7632ab2566ca90cc2f290a10104f10f4d285673d adds Load parameter example. It uses the explicitly selected ready MongoDB/SQLite/Neo4j source, fills a bounded Id/Name/Version query with Version>=@minimum and IsActive=@active, and fills JSON minimum1/activetrue. It does not fetch, compile or execute until the visitor chooses Preview/Run. Other/unavailable providers produce an explicit message and leave the existing editor unchanged.

One new actual-controller/compiler check passed;56existing Ask cases excluded. It verifies selected Neo provider, exact JSON values, review-only load, successful bound preview, unsupported-provider refusal preserving the editor, zero WEB6 calls. Previous three parameter controls retained without replay. Browser/live/publication acceptance remains open.
