(function (root) {
  'use strict';

  const instructions = 'Translate the question into one read-only CQL statement. Output only the statement, or UNSUPPORTED.';

  const databaseProviders = ['MongoDBOASIS', 'SQLLiteDBOASIS', 'Neo4jOASIS'];
  const databaseInstructions = [
    "For a count of bounded selected-value set occurrences, use WITH combined AS ((SELECT Name AS Label FROM MongoDBOASIS LIMIT 2) UNION ALL (SELECT Name AS Label FROM MongoDBOASIS LIMIT 2)) SELECT COUNT(*) AS Total FROM combined. Each leaf needs an explicit LIMIT up to100. Preserve selected-value equality, all earlier windows and renamed visible aliases; never turn selected sets into SELECT * identity sets. COUNT(*) and COUNT(1) return LongCount; ANY(*) tests existence. Numeric reductions over combined selected values are not supported by this SQL release. Same-provider selected set trees may nest; result filtering, ordering, paging, DISTINCT and alias selection stay in their original sequence. Requires the matching recursive set backend API release.",
      "To count combined full records, use SELECT COUNT(*) AS Total FROM (SELECT * FROM MongoDBOASIS UNION ALL SELECT * FROM MongoDBOASIS LIMIT 10) AS records WHERE Version >= 1. Keep earlier input/combined windows inside the derived table or WITH source. COUNT(1) also counts all visible records; ANY(*) returns existence. UNION ALL counts repeated occurrences; UNION compares entity IDs. Do not replace this with selected-column equality, COUNT(Name), COUNT(DISTINCT Name), numeric reductions or numeric reductions over selected set inputs. Put paging inside the combined source, never after the scalar count. Requires the matching Reduction API release; if unavailable return UNSUPPORTED.",

    "When a question asks for labels or other columns of combined records, preserve entity identity: first combine SELECT * inputs, then select the requested native columns from the resulting derived table or WITH source. Example: SELECT Name AS Label, Id FROM (SELECT * FROM MongoDBOASIS WHERE IsActive = TRUE UNION SELECT * FROM MongoDBOASIS WHERE Version >= 2 ORDER BY Id LIMIT 5) AS records. Distinct records with the same Name remain separate; explicit columns inside UNION instead compare selected values. Preserve earlier combined ordering and paging inside the entity-set source. Outer WHERE, ORDER BY, TOP, LIMIT and OFFSET become separate ordered entity stages after that earlier window and before the displayed columns. Order only direct Name, Description, Version or Id fields or their selected aliases, up to four terms. Keep declared table qualifiers. Explicit selected DISTINCT uses the native selected-stage contract after entity membership; COUNT(*)/COUNT(1) and ANY(*) may reduce bounded combined full records through the native set reduction contract; numeric post-set aggregates remain unsupported; bounded selected-value sets support COUNT(*)/COUNT(1) and ANY(*). Use an explicit combined LIMIT up to100. This requires the matching ordered entity-set stages API release; never rewrite this as selected-value equality or silently move a result filter or window.",
    "For selected stages after entity membership, use bounded WITH steps without changing the original SELECT * set inputs. Example: WITH records AS (SELECT * FROM MongoDBOASIS UNION ALL SELECT * FROM MongoDBOASIS LIMIT 4), chosen AS (SELECT Id, Name AS Label FROM records LIMIT 3) SELECT DISTINCT Label FROM chosen WHERE Label LIKE 'a%' ORDER BY Label LIMIT 2. Later selected filters, ordering, pages, explicit DISTINCT and alias renaming stay after the earlier entity window. Distinct compares only the visible selected tuple when requested; never rewrite entity identity membership as selected-value equality. Use one ready MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS provider and up to16 selected stages. Direct form: SELECT DISTINCT Name AS Label FROM (SELECT * FROM MongoDBOASIS UNION SELECT * FROM MongoDBOASIS LIMIT 4) AS records ORDER BY Label LIMIT 2. Deduplicate explicit selected columns before their ordering and page; DISTINCT * remains unsupported. Numeric aggregates remain unsupported here; nested same-kind set trees preserve each input window. Requires the matching native SelectedStages API release; if unavailable return UNSUPPORTED.",
    "For a supplied UTC DateTime metadata field, select YEAR(CAST(metadata.observed AS DATETIME)) AS ObservedYear, or MONTH, DAY, DAYOFYEAR, HOUR, MINUTE and SECOND with the same exact typed input. These produce nullable Int32 row columns; missing values remain null and invalid source values are errors. Never guess a path or type. Example: SELECT YEAR(CAST(metadata.observed AS DATETIME)) AS ObservedYear FROM MongoDBOASIS ORDER BY ObservedYear DESC LIMIT 5. A bounded WITH source may filter and order the resulting integer alias. Do not generate date arithmetic, local-time conversions, computed date grouping or scalar aggregates. For a selected saved virtual mapping, these seven row functions also use the same supplied DATETIME metadata input: select Id first and retain WITH VIRTUAL MAPPING and its exact mapping ID and provider. Order by Id or selected aliases, LIMIT 1 through 100 and OFFSET below 200. Never discard the mapping or suggest importing data to bypass an unsupported query. This requires the corresponding mixed date-part API release; if it is unavailable, return UNSUPPORTED.",

    "For unique selected tuples from a bounded WITH or derived source, SELECT DISTINCT compares exactly the visible output columns before its ORDER BY and page; order only those visible columns. Earlier source pages remain intact. For a question over an explicitly bounded selected result, use non-recursive WITH steps on one ready MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS provider. Example: WITH sample AS (SELECT Id, Name, Version FROM MongoDBOASIS ORDER BY Id LIMIT 25) SELECT * FROM sample WHERE Version >= 1 ORDER BY Version DESC, Id LIMIT 5. Declare up to eight named steps in dependency order. The initial source needs explicit selected columns and TOP, LIMIT or FETCH up to100; never replace an unbounded source with an implicit page. An equivalent derived table can use FROM (SELECT Name FROM MongoDBOASIS LIMIT 25) AS sample; require an explicit table alias and the same bounded-source rules. Later steps may filter current selected aliases, reorder, page, subset and rename those aliases. Preserve the selected ready provider and supplied field types. Do not use recursive WITH, source joins, aggregates, mappings or relationship predicates in a WITH source. A final parenthesized UNION, UNION ALL, INTERSECT or EXCEPT may combine compatible explicit columns from named or derived sources; each input may preserve its own post-window filters, ordering, paging and column renaming before set equality. This requires the qualified staged-input backend release; result ordering uses final visible left aliases only. WITH named steps and WITH VIRTUAL MAPPING are different contracts; never remove or substitute a selected mapping.",

    "For a known supplied nullable Int32 metadata field, use SELECT COALESCE(CAST(metadata.stock AS INT32), 0) AS Stock FROM MongoDBOASIS ORDER BY Stock LIMIT 5. Missing or null values use the integer replacement; stored zero and other valid Int32 values are preserved, and invalid types are errors. Use only integer replacement values from -2147483648 to 2147483647. Never guess a field path or type. Do not use Int64, UInt64, floating, Decimal or Boolean coalescing, computed inputs or aggregates. For a selected virtual mapping follow the mapped row rules below.",
    "For selected text labels use CASE WHEN IsActive THEN 'Active' ELSE 'Inactive' END AS Status, for example SELECT CASE WHEN IsActive THEN 'Active' ELSE 'Inactive' END AS Status FROM MongoDBOASIS ORDER BY Status LIMIT 5. Use canonical Boolean IsActive or IsPublic, or CASE WHEN CAST(metadata.flag AS BOOLEAN) THEN 'Yes' ELSE 'No' END for a known supplied Boolean metadata path. Never guess a metadata path or its type. Metadata inputs are required; missing, null or invalid Boolean values are errors, not false labels. Both branches must be quoted non-null strings of at most512 characters. Do not generate arbitrary comparisons, multiple WHEN branches, nested CASE, numeric branches, aggregates or canonical Boolean fields inside virtual mapping queries.",
    "For selected text row columns, use COALESCE(Description, 'Unnamed') AS Label or COALESCE(Name, 'Unnamed') AS Label, for example SELECT COALESCE(Description, 'Unnamed') AS Label FROM MongoDBOASIS ORDER BY Label LIMIT 5. Null or missing text uses the literal replacement, empty text remains empty, and stored invalid types are errors. A known text metadata field uses COALESCE(CAST(metadata.label AS STRING), 'Unnamed') AS Label. Use only a known supplied path/type and a non-null quoted replacement of at most512 characters. Do not coalesce nested computed inputs, scalar aggregates or grouped expressions; for a selected virtual mapping follow the mapped row rules below. Numeric coalescing is restricted to the separately described nullable Int32 metadata contract.",
    'Combine two SELECT inputs on the same available MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS provider with UNION, UNION ALL, INTERSECT or EXCEPT. For SELECT *, equality uses Holon IDs. With explicit selected columns, equality uses only the selected values; both sides must select matching types and column counts, and result aliases come from the left. Use UNION for unique values, UNION ALL to preserve occurrences, INTERSECT for shared values and EXCEPT for left-only values. Example: SELECT Name AS Label FROM MongoDBOASIS WHERE IsActive = TRUE UNION SELECT Name FROM MongoDBOASIS WHERE Version >= 2 ORDER BY Label LIMIT 5. ORDER BY and LIMIT/OFFSET after the second input apply to the combined result. Parenthesize an input only when it needs its own ordering or page. Do not mix SELECT * and explicit columns, use cross-provider sets, add custom comparers, combine sets with virtual mappings. Same-kind sets may nest, bounded to depth8 and32 leaves; parenthesize any independent input ordering or paging. INTERSECT binds more tightly than UNION and EXCEPT.',
    'MongoDBOASIS, SQLLiteDBOASIS, and Neo4jOASIS support SELECT *, SELECT Id, Name AS DisplayName, Version from one database, SELECT COUNT(*), ANY(*) to test whether a row exists, MIN(Version), MAX(Version), SUM(Version), and AVG(Version). For a known numeric metadata path and type, use SELECT SUM(CAST(metadata.amount AS DECIMAL)) FROM MongoDBOASIS. Scalar MIN, MAX, SUM and AVG support this syntax with INT32, INT64, UINT64 and DECIMAL. Scalar queries use one database and no TIMEOUT.',
    'For a known metadata field and type on these databases, use SELECT CAST(metadata.stock AS INT64) AS Stock FROM MongoDBOASIS ORDER BY Stock DESC LIMIT 5. Supported types are STRING, BOOLEAN, INT32, INT64, UINT64, DECIMAL, SINGLE, DOUBLE, GUID and DATETIME. Do not guess metadata paths or types. Double-quote a metadata segment containing spaces, dots, or punctuation, such as metadata."catalog.v1"."display name". For unique values, use SELECT DISTINCT CAST(metadata.stock AS INT64) AS Stock FROM MongoDBOASIS ORDER BY Stock LIMIT 5. GROUP BY can use the same typed CAST expression or its selected alias when the alias is not a source field name.',
    'Use GROUP BY for selected scalar keys, for example SELECT Version, COUNT(*) AS Total FROM MongoDBOASIS GROUP BY Version. Include grouping keys only when requested. SELECT COUNT(*) AS Total FROM MongoDBOASIS GROUP BY Version returns counts alone. COUNT(1) means COUNT(*); COUNT(Description) counts non-null values with duplicates. COUNT(DISTINCT Description) or COUNT(DISTINCT CAST(metadata.category AS STRING)) counts unique non-null values. Ungrouped aggregates may use one result alias.',
    'Grouped aggregates are COUNT, MIN, MAX, SUM and AVG. Typed SINGLE and DOUBLE metadata support SUM and AVG. HAVING compares selected aliases or repeats the selected aggregate or typed key expression in its exact type, including text LIKE and NOT LIKE, nullable IS NULL and numeric BETWEEN. For example, SELECT IsActive, COUNT(*) AS Total, AVG(Version) AS Mean, SUM(Version) AS Combined FROM MongoDBOASIS GROUP BY IsActive HAVING Total >= 2 AND Mean >= 1.5 AND Combined >= 3. GROUP BY may list multiple keys. Use selected aliases or repeat selected expressions in ORDER BY before OFFSET. LIMIT and OFFSET page groups. No TIMEOUT or relationship filters in grouped queries.',
    'Use SELECT DISTINCT Version, IsActive FROM MongoDBOASIS ORDER BY Version LIMIT 5 to return unique combinations of explicitly selected fields. DISTINCT works on one MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS source. It takes no aggregate, GROUP BY or HAVING. Order by selected aliases before OFFSET.',
    "For MongoDBOASIS, SQLLiteDBOASIS and Neo4jOASIS, use LIKE with text fields. Example: SELECT * FROM MongoDBOASIS WHERE Name LIKE '%coffee%' LIMIT 5. Put % at the start for suffix matching, at the end for prefix matching, or at both ends for substring matching. Escape a literal % or _ with ESCAPE, for example Name LIKE '%100!%%' ESCAPE '!'. Consecutive % wildcards act as one. On Name or Description, LIKE with only % matches any non-null value. Use NOT LIKE to exclude matching text. It also excludes nulls and missing metadata. Unescaped internal wildcards and _ wildcards are unsupported.",
    'On MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS, a single selected Description alias, selected typed CAST expressions, or multiple selected aliases order matching records in the native provider before applying LIMIT and OFFSET. Full rows on those stores can also use ORDER BY Name, Description, Version or Id, for example SELECT * FROM MongoDBOASIS ORDER BY Name ASC, Id ASC LIMIT 5. For example, SELECT Id, Description AS Detail FROM SQLLiteDBOASIS ORDER BY Detail DESC LIMIT 3. Multi-column example: SELECT Id, Version FROM Neo4jOASIS ORDER BY Version DESC, Id ASC LIMIT 5 OFFSET 1.',
    'Selected columns and all scalar queries use the server timeout. Selected columns require one MongoDBOASIS, SQLLiteDBOASIS, or Neo4jOASIS source.',
    "Use inclusive BETWEEN for numeric and timestamp filters, for example WHERE Version BETWEEN 2 AND 5. A range can participate in a parenthesized Boolean expression; its two comparisons consume two filters.",
    'WHERE accepts Id, ParentHolonId, Name, Description, Version, IsActive, and metadata paths with up to four segments such as metadata.location.city. Use Description IS NULL or Name IS NOT NULL for canonical text. Metadata supports IS PRESENT, IS MISSING, IS NULL, and IS NOT NULL. IS NOT NULL requires the path to exist with a non-null value.',
    "WHERE may combine EXISTS CHILD (conditions) for direct children and EXISTS DESCENDANT (conditions) for bounded descendants with AND, OR, NOT and parentheses. Relationship conditions may themselves contain child or descendant tests. NOT EXISTS requires absence of a visible match; omit its condition parentheses to match any visible related record. Keep at most four relationship predicates and eight Boolean tree levels. Example: SELECT * FROM MongoDBOASIS WHERE (Name = 'Coffee' AND EXISTS CHILD (IsActive = TRUE)) OR (Name = 'Tea' AND NOT EXISTS CHILD) ORDER BY Name LIMIT 5.",
    'Use single-quoted strings (512 characters max), exact numbers, TRUE or FALSE. Use equality, inequality and IN. NOT BETWEEN is exclusive and expands into an OR of the outside numeric or timestamp comparisons. Typed metadata predicates use metadata.path AS TYPE, such as metadata.Sequence AS INT64 >= -9007199254740993. Preserve INT64, UINT64 and DECIMAL digits; use SINGLE or DOUBLE only for known floating values. CAST(metadata.path AS TYPE) selects or deduplicates typed values. Use BOOLEAN = TRUE for known Boolean fields. DATETIME requires a known stored timestamp; use a quoted ISO value with Z or an explicit offset, optionally prefixed TIMESTAMP. Preserve all supplied fractional digits, up to seven, and never invent a timestamp field or reinterpret stored text. Use string equality for stored text. Text also supports CONTAINS, STARTSWITH and ENDSWITH.',
    "AND binds more tightly than OR. Use parentheses to preserve the requested meaning and NOT to negate a complete condition. Independent AND groups can be joined by OR. Keep at most twelve expanded filters, four relationships, 31 Boolean tree nodes and eight levels. Row queries may use ORDER BY Name, Version or Id, LIMIT1–100 and OFFSET0–1000, or OFFSET n ROWS FETCH NEXT n ROWS ONLY. TOP n or TOP(n) can replace LIMIT for rows, selected DISTINCT results, grouped output or joined output; do not combine TOP with another window in the same query. Scalar results do not use TOP. NOT IN with non-null literals on Id, Version, Name, Description or metadata consumes one filter per distinct excluded value; text/metadata add one filter to exclude null/missing values.",
    'Example: SELECT * FROM MongoDBOASIS WHERE IsActive = TRUE AND (metadata.category = \'coffee\' OR metadata.category = \'tea\') ORDER BY Name LIMIT 5',
  ];

  databaseInstructions.push('For the non-null canonical Boolean fields IsActive, IS TRUE, IS FALSE, IS NOT TRUE and IS NOT FALSE are supported and preserve their exact Boolean comparison. Use typed equality for mapped Boolean metadata; do not apply these canonical IS forms to metadata.');

  databaseInstructions.push(
      "For a selected saved virtual mapping, mapped row columns admit COALESCE(CAST(metadata.label AS STRING), 'Unnamed'), CASE WHEN CAST(metadata.flag AS BOOLEAN) THEN 'Yes' ELSE 'No' END, and COALESCE(CAST(metadata.stock AS INT32), 0). Select Id first, preserve the exact selected mapping/provider and use only metadata paths and types supplied by the user. Boolean input is required; text/Int32 nulls use their literal replacements, while invalid stored types are errors. Order by Id or selected aliases, LIMIT 1 through 100 and OFFSET below 200. These mapped computations are row-only: no computed DISTINCT, grouping, aggregate or nested function. Full Holons and live mapped rows share the same selected columns and logical identities.",
    "When a saved virtual mapping is selected, keep its admitted flat AND metadata filters. Do not use nested Boolean WHERE or relationship predicates with WITH VIRTUAL MAPPING.",
    "LENGTH on known text returns .NET UTF-16 units, so a supplementary character counts as two. For known STRING metadata, use SELECT Id, LENGTH(CAST(metadata.name AS STRING)) AS Units FROM MongoDBOASIS WHERE LENGTH(metadata.name) >= 2 ORDER BY Units DESC LIMIT 5. Use only metadata paths and types provided in the question; do not guess them. Canonical LENGTH(Name) and LENGTH(Description) require known non-null text or an explicit IS NOT NULL filter. Outside virtual mappings, COUNT, MIN, MAX, SUM and AVG may select LENGTH of admitted text.",
    "Only when the selected-source context supplies a saved virtual mapping, query real and virtual records together. For row queries, select Id first, optionally followed by direct typed metadata or the admitted computed row columns. Select Id alone when the question requests only logical identities, without reading native property payloads. Use WITH VIRTUAL MAPPING followed by that exact quoted mapping ID. Mapped row metadata types are STRING, BOOLEAN, GUID, DATETIME, INT32, INT64 and finite DOUBLE; LENGTH requires STRING input. Known INT32 metadata filters use CAST(metadata.stock AS INT32) with numeric literals from -2147483648 through 2147483647; do not substitute INT64 for a declared INT32 field. A known mapped GUID supports CAST(metadata.identity AS GUID) = or <> a quoted D-format UUID. Use only the exact field/type supplied in the question; GUID range comparisons, IN and LENGTH(GUID) are unsupported. A known DATETIME metadata field supports row projection and comparisons using CAST(metadata.occurredAt AS DATETIME) with TIMESTAMP literals carrying Z or an explicit timezone offset; these normalize to UTC without losing subsecond precision. Preserve the supplied path and type. Known DATETIME metadata also supports mixed GROUP BY keys and SELECT DISTINCT keys, with exact UTC ordering; DateTime selected-value aggregates remain unsupported. A known DOUBLE metadata field can use CAST(metadata.rate AS DOUBLE) in its selection and comparison; do not substitute DECIMAL or infer the field type. Use zero through eight AND filters on direct metadata; omit WHERE for a bounded unfiltered query. Complete-source budgets still apply and a full branch is refused rather than returned as a complete subset. Use ORDER BY Id or selected raw/computed aliases, LIMIT 1 through 100 and OFFSET below 200. For example: SELECT Id, CAST(metadata.name AS STRING) AS Original, LENGTH(CAST(metadata.name AS STRING)) AS Units FROM MongoDBOASIS WITH VIRTUAL MAPPING '<selected-mapping-id>' WHERE LENGTH(metadata.name) >= 2 ORDER BY Units DESC, Original ASC LIMIT 5. Do not invent IDs, paths or types. To count qualifying real and virtual records, use SELECT COUNT(*) AS Total FROM MongoDBOASIS WITH VIRTUAL MAPPING '<selected-mapping-id>' WHERE metadata.tier = 'gold'. COUNT(1) is also supported. The engine counts the complete deduplicated identity union before paging and refuses an incomplete branch; narrow the filter when the native budget is reached. Count queries return one scalar, with no ORDER BY or row paging; optional LIMIT 1 selects that single result. Mixed mappings also support MIN, MAX, SUM and AVG over known direct INT32, INT64 or finite DOUBLE metadata, or LENGTH(CAST(metadata.name AS STRING)). For example SELECT AVG(CAST(metadata.rate AS DOUBLE)) AS Mean FROM MongoDBOASIS WITH VIRTUAL MAPPING '<selected-mapping-id>' WHERE metadata.tier = 'gold'. Reductions use the complete qualifying identity union with live duplicate winners, skip null values and return one scalar before paging. Integer AVG returns an exact Decimal mean, Double AVG stays Double, empty SUM is zero and other empty nullable reductions return null. Do not invent metadata paths or types. Mixed mappings support MIN and MAX of a known direct STRING metadata field too; text extrema skip missing/null values, return null for empty/all-null inputs, preserve exact text and use Unicode scalar ordering without case folding or normalization. For example SELECT MIN(CAST(metadata.region AS STRING)) AS FirstRegion FROM Neo4jOASIS WITH VIRTUAL MAPPING '<selected-mapping-id>' WHERE metadata.tier = 'gold'. String SUM/AVG are invalid. Mixed mappings also support COUNT of a known direct nullable STRING, BOOLEAN, INT32, INT64 or finite DOUBLE metadata value, and COUNT(DISTINCT ...) of that value; native LENGTH of STRING is supported too. COUNT(value) excludes missing/null values after logical identity reconciliation. COUNT(DISTINCT value) then applies exact ordinal/exact typed equality to the remaining winning values. For example SELECT COUNT(DISTINCT CAST(metadata.region AS STRING)) AS Regions FROM MongoDBOASIS WITH VIRTUAL MAPPING '<selected-mapping-id>' WHERE metadata.tier = 'gold'. Use only known mapping paths/types and preserve the selected source and mapping. Mixed mappings also support GROUP BY over known direct typed metadata keys and SELECT DISTINCT of those keys, with the same provider, mapping and source filters. Grouping reconciles the complete stored/native identity union before grouping, HAVING, selected ordering and one final result page. Grouped aggregates support COUNT(*), COUNT(value), COUNT(DISTINCT value), MIN, MAX, SUM and AVG with admitted direct metadata selections; SUM/AVG require known INT32, INT64 or finite DOUBLE values. Use known STRING, BOOLEAN, GUID, DATETIME, INT32, INT64 or finite DOUBLE keys. For example SELECT CAST(metadata.region AS STRING) AS Region, COUNT(*) AS Total FROM Neo4jOASIS WITH VIRTUAL MAPPING '<selected-mapping-id>' WHERE metadata.tier = 'gold' GROUP BY Region HAVING Total >= 2 ORDER BY Total DESC LIMIT 5. Include only paths and types explicitly supplied in the question. LIMIT/OFFSET page reduced groups, and OFFSET needs selected ORDER BY. Do not select Id just to satisfy a grouped query. COUNT(DISTINCT value) excludes missing/null values within each group after identity reconciliation. Keep ordinary COUNT(value) separate from COUNT(DISTINCT value), including when using their expressions in HAVING and ORDER BY. Computed grouping keys/selectors, other numeric domains, joins, relationship filters and TIMEOUT remain unsupported for mixed mappings. Return UNSUPPORTED when the question requires these."
  );

  const providerInstructions = [
    [['HoloOASIS'], 'HoloOASIS supports SELECT * with optional Id equality or IN, LIMIT and OFFSET. Use IDs supplied by the user. Do not invent IDs or use other filters, COUNT, or ORDER BY with HoloOASIS.'],
    [['SolanaOASIS'], 'SolanaOASIS supports SELECT * with optional metadata.SolanaAccountAddress equality or IN, LIMIT and OFFSET. Use addresses supplied by the user. Do not invent addresses or use other filters, COUNT, or ORDER BY with SolanaOASIS.'],
    [['EthereumOASIS', 'PolygonOASIS', 'ArbitrumOASIS'], 'EthereumOASIS, PolygonOASIS, and ArbitrumOASIS support SELECT * with LIMIT and OFFSET and an optional equality or IN filter on their own metadata key: EthereumAccountAddress, PolygonAccountAddress, or ArbitrumAccountAddress. Use only addresses supplied by the user. Do not use COUNT, ORDER BY, or mix account networks.'],
    [['BNBChainOASIS', 'AvalancheOASIS', 'BaseOASIS', 'OptimismOASIS'], 'BNBChainOASIS, AvalancheOASIS, BaseOASIS and OptimismOASIS support SELECT * with one equality or IN filter on their respective metadata key: BNBChainAccountAddress, AvalancheAccountAddress, BaseAccountAddress or OptimismAccountAddress. Use at most eight distinct supplied 0x addresses. Use LIMIT and OFFSET without COUNT or ORDER BY, and query each account network separately.'],
    [['SuiOASIS', 'AptosOASIS', 'StarknetOASIS', 'FantomOASIS'], 'SuiOASIS and AptosOASIS use metadata.SuiObjectId or metadata.AptosAccountAddress with quoted 0x followed by 64 lowercase hexadecimal digits. StarknetOASIS uses metadata.StarknetBlockHash with a quoted canonical lowercase 0x field element. FantomOASIS uses metadata.FantomAccountAddress with a quoted 0x address. Use SELECT * with one equality or IN filter of at most eight distinct supplied keys, and no COUNT or ORDER BY.'],
    [['AlgorandOASIS', 'FilecoinOASIS', 'ZcashOASIS', 'RadixOASIS'], 'AlgorandOASIS, FilecoinOASIS, ZcashOASIS and RadixOASIS support SELECT * with one equality or IN filter on metadata.AlgorandAddress, metadata.FilecoinAccountAddress, metadata.ZcashBlockHash or metadata.RadixResourceAddress respectively. Use up to eight distinct supplied keys and quote each key. Use an uppercase Algorand address, a 0x Filecoin EVM address, a lowercase 64-digit Zcash block hash, or a Mainnet resource_rdx1 Radix address. Do not invent keys or use COUNT or ORDER BY.'],
    [['NEAROASIS', 'EOSIOOASIS'], 'NEAROASIS and EOSIOOASIS support SELECT * with LIMIT and OFFSET and an optional equality or IN filter on metadata.NearAccountId or metadata.EOSIOAccountName respectively. Use only account names supplied by the user. Do not use COUNT or ORDER BY.'],
    [['SOLIDOASIS', 'IPFSOASIS', 'ActivityPubOASIS'], 'SOLIDOASIS, IPFSOASIS, and ActivityPubOASIS support SELECT * with LIMIT and OFFSET and equality or IN on metadata.SolidRecordId, metadata.IPFSRecordCid, or metadata.MastodonStatusId respectively. Use keys supplied by the user. Omit COUNT and ORDER BY.'],
    [['CosmosBlockChainOASIS', 'PolkadotOASIS', 'TONOASIS'], 'CosmosBlockChainOASIS, PolkadotOASIS, and TONOASIS support SELECT * with equality or IN on metadata.CosmosValidatorAddress, metadata.PolkadotBlockHash, or metadata.TonBlockRootHash respectively. Use keys supplied by the user, with LIMIT and OFFSET. Omit COUNT and ORDER BY.'],
    [['TRONOASIS'], 'TRONOASIS supports SELECT * with equality or IN on metadata.TronBlockHeight using quoted positive block heights supplied by the user. Omit COUNT and ORDER BY.'],
    [['AxelarOASIS'], 'AxelarOASIS supports SELECT * with equality or IN on metadata.AxelarBlockHeight. Use only supplied quoted positive block heights, at most eight keys. Omit COUNT and ORDER BY.'],
    [['BitcoinOASIS', 'CardanoOASIS', 'XRPLOASIS', 'StellarOASIS'], 'BitcoinOASIS and CardanoOASIS use metadata.BitcoinBlockHash and metadata.CardanoBlockHash respectively, with quoted lowercase 64-digit hexadecimal hashes. XRPLOASIS uses metadata.XRPLLedgerHash with quoted uppercase 64-digit hexadecimal hashes. StellarOASIS uses metadata.StellarLedgerSequence with quoted positive unsigned 32-bit sequences. Use SELECT * and one equality or IN filter of at most eight distinct supplied keys. Do not invent keys or use COUNT or ORDER BY.'],
    [['HashgraphOASIS'], 'HashgraphOASIS supports SELECT * with optional equality or IN on metadata.HederaAccountId, LIMIT and OFFSET. Use only account IDs supplied by the user. Omit COUNT and ORDER BY.'],
    [['NostrOASIS'], 'NostrOASIS supports SELECT * with equality or IN on metadata.NostrEventId, LIMIT and OFFSET. Use only supplied 64-character lowercase hexadecimal event IDs, at most eight. Omit COUNT and ORDER BY.'],
  ];

  function queryInstructions(providers) {
    const ready = new Set(providers);
    const joinProvider = databaseProviders.find(provider => ready.has(provider));
    return [instructions,
      'Use TIMEOUT followed by milliseconds (1–30000) only for SELECT * row queries when requested.',
      'For a federated row query, list distinct providers separated by commas after FROM. COUNT uses one provider. A federated query shares one filter across providers. Use one key field per query; HoloOASIS and public account providers with different key fields require separate queries.',
      ...(joinProvider ? databaseInstructions.map(rule => rule.replace(
        /\bFROM (MongoDBOASIS|SQLLiteDBOASIS|Neo4jOASIS)\b/g,
        (example, provider) => ready.has(provider) ? example : 'FROM ' + joinProvider
      )) : []),
      ...(joinProvider ? [
        'For an exact same-provider INNER JOIN, LEFT JOIN, RIGHT JOIN or FULL OUTER JOIN on MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS, use two aliased (SELECT * FROM provider ... LIMIT n) inputs with explicit LIMIT 1 through 10 in each. Both inputs must use the same available provider. Each input may use OFFSET 0 through 1000 after LIMIT. Put the offset on its specific input. A join input may use bounded AND/OR/NOT and parentheses, but no relationship predicates inside the input. Select qualified columns with distinct aliases. Combine ON comparisons with AND and OR. AND binds more tightly than OR, so A AND B OR C matches when both A and B match or C matches. Use parentheses when OR must bind before AND. The expanded clause must contain at most eight comparisons. An OR join returns each matching pair once even when multiple conditions match. Use = on compatible scalar fields. For matching signed integer fields, also use <>, <, <=, > or >=. FULL OUTER JOIN includes unmatched rows from both inputs. For a Cartesian product of two bounded inputs, use CROSS JOIN without ON. Use CHILD JOIN without ON for visible children of the admitted left parents. The right LIMIT applies after scoping that relationship. For result paging, append ORDER BY selected aliases or qualified expressions with GUID, STRING, INT32, INT64, UINT64, DATETIME or BOOLEAN types and LIMIT n OFFSET m outside both inputs. For known typed metadata use CAST(l.metadata.path AS INT64) and the same declared type on the other side. For known nullable Boolean fields, use CAST(l.metadata.enabled AS BOOLEAN) IS NOT DISTINCT FROM CAST(r.metadata.enabled AS BOOLEAN). Use the same null-safe operators with matching nullable GUID, UTC timestamp, signed integer, UINT64 or DECIMAL metadata. Text joins can also use l.Description IS NOT DISTINCT FROM r.Description or matching metadata CASTs AS STRING. IS NOT DISTINCT FROM matches null to null. IS DISTINCT FROM matches one null to a value or two differing values.',
        `Example: SELECT l.Id AS LeftId, r.Id AS RightId FROM (SELECT * FROM ${joinProvider} ORDER BY Id LIMIT 2) AS l LEFT JOIN (SELECT * FROM ${joinProvider} ORDER BY Id LIMIT 2) AS r ON l.Id = r.Id AND l.Version = r.Version`
      ] : []),
      ...providerInstructions.filter(([names]) => names.some(provider => ready.has(provider))).map(([, rule]) => rule)
    ].join('\n');
  }

  function init({ playground, isSandbox, getIdentity, ensureAccessToken, isDemoSession = () => false }) {
    const $ = id => document.getElementById(id);
    const status = $('ask-status');
    const generate = $('ask-generate');
    const preview = $('ask-preview');
    let draft, activeExample;
    const questionInput = $('ask-question');
    let questionVersion = 0;
    const selectedMapping = () => $('cql-virtual-mapping')?.value.trim().toLowerCase() ?? '';
    const selectedProvider = () => $('cql-provider')?.value ?? '';
    function mappingScope(mappingId, provider) {
      if (!mappingId) return null;
      if (!/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/.test(mappingId) ||
          /^0{8}(?:-0{4}){3}-0{12}$/.test(mappingId))
        throw new Error('Select a valid saved virtual mapping in the CQL Playground first.');
      if (!databaseProviders.includes(provider))
        throw new Error('Select MongoDBOASIS, SQLLiteDBOASIS or Neo4jOASIS for this mapped draft.');
      return { mappingId, provider };
    }
    function sourceChanged() {
      clearDraft();
      status.textContent = 'The selected mapping changed. Draft a new query.';
    }
    $('cql-virtual-mapping')?.addEventListener('input', sourceChanged);
    $('cql-virtual-mapping')?.addEventListener('change', sourceChanged);
    $('cql-saved-mapping')?.addEventListener('change', sourceChanged);
    $('cql-provider')?.addEventListener('change', () => {
      if (selectedMapping() || draft?.mappingId) sourceChanged();
    });


    const exampleProvider = $('ask-example-provider');
    function refreshExampleProviders() {
      const selected = exampleProvider.value;
      const options = [['', 'Any ready provider'], ...playground.availableQuestionProviders().map(provider => [provider, provider])];
      exampleProvider.replaceChildren(...options.map(([value, label]) => {
        const option = document.createElement('option');
        option.value = value;
        option.textContent = label;
        return option;
      }));
      if (options.some(([value]) => value === selected)) exampleProvider.value = selected;
    }


    function clearDraft() {
      questionVersion++;
      draft = null;
      preview.disabled = true;
      $('ask-draft').textContent = '';
    }
    questionInput.addEventListener('input', () => {
      activeExample = null;
      clearDraft();
      status.textContent = '';
    });
    $('cql-load-parameter-example')?.addEventListener('click', () => {
      const provider = $('cql-provider').value;
      if (!databaseProviders.includes(provider) || !playground.availableTextProviders().includes(provider)) {
        $('cql-note').textContent = 'Select a ready MongoDB, SQLite or Neo4j provider before loading this example.';
        return;
      }
      $('cql-text').value = 'SELECT Id, Name, Version FROM ' + provider +
        ' WHERE Version >= @minimum AND IsActive = @active ORDER BY Name LIMIT 5';
      $('cql-text-parameters').value = JSON.stringify({ minimum: 1, active: true }, null, 2);
      $('cql-text').focus();
      $('cql-note').textContent = 'Review the query and parameter values, then preview its request.';
    });
    function editorParameters() {
      const text = $('cql-text-parameters')?.value?.trim();
      if (!text) return {};
      if (text.length > 16384) throw new Error('Parameter JSON may contain up to 16,384 characters.');
      return JSON.parse(text);
    }
    $('cql-compile').addEventListener('click', () => {
      try { playground.loadText($('cql-text').value, editorParameters()); }
      catch (error) { $('cql-note').textContent = error.message; }
    });
    $('cql-run-text').addEventListener('click', () => {
      try {
        playground.loadText($('cql-text').value, editorParameters());
        $('cql-run').click();
      } catch (error) { $('cql-note').textContent = error.message; }
    });
    $('ask-use-example').addEventListener('click', () => {
      try {
        const example = isSandbox() && playground.publicQuestionExample(exampleProvider.value);
        if (!example) throw new Error('No public query example is ready on this WEB4 host.');
        const { provider, request } = example;
        const filter = request.filters[0];
        const field = filter.field ?? 'metadata.' + filter.metadataPath[0];
        const timeout = request.maximumDurationMilliseconds;
        const predicate = filter.operator === 'Equal'
        ? `${field} exactly equal to ${JSON.stringify(filter.value)}`
        : `${field} matching any of these keys: ${[].concat(filter.value).map(value => JSON.stringify(value)).join(', ')}`;
      const question = `Show up to ${request.take} public records from ${provider} with ${predicate}.` +
          (timeout ? ` Use a query timeout of ${timeout} milliseconds.` : '');
        if (question.length > 1000) throw new Error('This example exceeds the question length limit.');
        playground.clearVirtualMapping();
        clearDraft();
        questionInput.value = question;
        activeExample = { question, provider, request };
        questionInput.focus();
        status.textContent = 'Review the question, then select Draft query.';
      } catch (error) { status.textContent = error.message; }
    });
    generate.addEventListener('click', async () => {
      const question = questionInput.value.trim();
      if (!question || question.length > 1000 || !isSandbox()) {
        status.textContent = 'Select WEB4 Development and enter a question of up to 1,000 characters.';
        return;
      }
      const identity = getIdentity();
      const sameSession = () => isSandbox() && getIdentity() === identity;
      const mappingId = selectedMapping();
      const provider = selectedProvider();
      const sameSource = () => selectedMapping() === mappingId && (!mappingId || selectedProvider() === provider);
      clearDraft();
      const version = questionVersion;
      const current = () => sameSession() && sameSource() && version === questionVersion;
      generate.disabled = true;
      status.textContent = 'Drafting with WEB6…';
      let requestKey;
      try {
        const scope = mappingScope(mappingId, provider);
        const token = await ensureAccessToken();
        if (!current() || !token) throw new Error('Sign in to the Development sandbox first.');
        const example = activeExample?.question === question ? activeExample : null;
        const readyProviders = playground.availableTextProviders();
        const providers = readyProviders.filter(candidate =>
          example ? candidate === example.provider : !scope || candidate === scope.provider);
        if (!providers.length) throw new Error('Wait for WEB4 provider discovery before drafting a query.');
        const providerScope = 'Use only these currently available WEB4 providers: ' + providers.join(', ') + '.';
        const keyProfiles = Object.entries(playground.availableTextProfiles())
          .filter(([provider]) => !example || provider === example.provider)
          .map(([provider, { key, maximumKeys }]) => provider + ':metadata.' + key + '(' + maximumKeys + ')');
        const keyScope = keyProfiles.length
          ? '\nAdditional ready sources (provider:metadata key, maximum IN keys). Use SELECT * and one equality on a supplied quoted key. IN is allowed only when the bound is positive. No ORDER BY or aggregation. ' + keyProfiles.join(', ')
          : '';
        const exampleScope = example
          ? '\nFor this public example, select all fields and use exactly one WHERE comparison on the supplied field. ' +
            'Preserve every supplied key verbatim, using IN for multiple keys. Include the requested LIMIT and, ' +
            'if requested, append TIMEOUT followed by the duration in milliseconds after LIMIT.'
          : '';
        const mappingInstructions = scope
          ? '\nSelected source: the visitor chose saved virtual mapping ' + scope.mappingId +
            ' and target provider ' + scope.provider + '. Preserve both exactly in the statement. ' +
            'Use only logical metadata paths and types supplied in the question; otherwise return UNSUPPORTED.'
          : '\nNo virtual mapping is selected. Do not use WITH VIRTUAL MAPPING.';
        requestKey = crypto.randomUUID();
        const response = await fetch('https://dev.api.web6.oasisomniverse.one/v1/complete', {
          method: 'POST', redirect: 'error', signal: AbortSignal.timeout(30000),
          headers: { Authorization: 'Bearer ' + token, 'Content-Type': 'application/json',
            'Idempotency-Key': requestKey },
          body: JSON.stringify({ messages: [{ role: 'system', content: queryInstructions(providers) + '\n' + providerScope + keyScope + exampleScope + mappingInstructions },
            { role: 'user', content: question }], tools: [], maxTokens: 350, temperature: 0,
            useFAHRN: false, useHolonicBraid: false, injectAvatarContext: false,
            ...(isDemoSession() ? { freeOnly: true } : {}) })
        });
        const result = await response.json().catch(error => {
          if (error.name !== 'SyntaxError') throw error;
          throw new Error('WEB6 returned an invalid response (HTTP ' + response.status + ').');
        });
        if (!current()) return;
        if (!result || typeof result !== 'object' || Array.isArray(result))
          throw new Error('WEB6 returned an invalid response (HTTP ' + response.status + ').');
        if (result.code === 'MEASUREMENT_REQUIRED')
          throw new Error('WEB6 could not confirm usage for this request. Reconciliation reference: ' +
            (result.operationId ?? response.headers.get('X-OASIS-Operation-Id') ?? requestKey));
        if (result.errorCode === 'WEB6_FREE_ROUTE_UNAVAILABLE')
          throw new Error('Free AI drafting is temporarily unavailable. You can still write and run CQL in the Playground.');
        if (result.code === 'DAILY_TOKEN_LIMIT_EXCEEDED')
          throw new Error('Your remaining daily AI token allowance is too low for this draft. You can still write and run CQL in the Playground.');
        if (response.status === 400 && result.isError === true &&
            result.message === 'Error calling AI provider(s). Reason: No LeelaAI model in the catalogue is admitted by your plan.')
          throw new Error('Your Development plan has no available WEB6 model. You can still write and run CQL in the Playground.');
        if (!response.ok || result.isError === true)
          throw new Error('WEB6 could not draft a query (HTTP ' + response.status + ').');
        const content = (result.result ?? result).content;
        if (typeof content !== 'string' || !content.trim()) throw new Error('WEB6 returned no CQL statement.');
        const text = content.trim();
        if (text === 'UNSUPPORTED') throw new Error('This question cannot be expressed in the sandbox query editor.');
        const compiled = playground.compileAvailableText(text);
        const actualMapping = compiled.body.virtualMappingId?.toLowerCase();
        if (scope ? actualMapping !== scope.mappingId || compiled.body.provider !== scope.provider : actualMapping)
          throw new Error(scope
            ? 'The draft did not preserve the selected mapping and provider. Nothing was run.'
            : 'The draft used an unselected virtual mapping. Select it in the CQL Playground and draft again.');
        if (example) {
          const body = compiled.body;
          const actual = body.filters?.[0];
          const expected = example.request.filters[0];
          const key = filter => JSON.stringify(filter.metadataPath ?? [filter.field]);
          const values = filter => [].concat(filter.value).map(value => JSON.stringify(value)).sort();
          if (compiled.path !== 'data/query-cql' || body.provider !== example.provider
              || body.filters?.length !== 1 || body.anyOf?.length || body.anyChildren?.length || body.anyDescendants?.length
              || !['Equal', 'In'].includes(actual?.operator) || key(actual) !== key(expected)
              || JSON.stringify(values(actual)) !== JSON.stringify(values(expected))
              || body.selection || body.projection || (body.skip ?? 0) !== 0
              || (body.maximumDurationMilliseconds ?? null) !== (example.request.maximumDurationMilliseconds ?? null)
              || body.take < values(expected).length
              || body.take > example.request.take)
            throw new Error('The draft did not preserve the reviewed public example. Nothing was run.');
        }
        draft = { text, identity, mappingId, provider };
        $('ask-draft').textContent = text;
        preview.disabled = false;
        status.textContent = 'Review the draft and open it in the Playground to run it.';
      } catch (error) {
        if (current()) status.textContent = error.message;
      } finally {
        if (!sameSession() && version === questionVersion)
          status.textContent = 'The sandbox session changed. Draft a new query.';
        else if (!sameSource() && version === questionVersion)
          status.textContent = 'The selected mapping changed. Draft a new query.';
        generate.disabled = false;
      }
    });
    preview.addEventListener('click', () => {
      try {
        if (!draft || !isSandbox() || getIdentity() !== draft.identity)
          throw new Error('The sandbox session changed. Draft a new query.');
        if (selectedMapping() !== draft.mappingId || (draft.mappingId && selectedProvider() !== draft.provider)) {
          clearDraft();
          throw new Error('The selected mapping changed. Draft a new query.');
        }
        playground.loadText(draft.text);
        $('cql-text').value = draft.text;
        $('cql-text-editor').open = true;
        document.querySelector('[data-tab="cql"]').click();
        $('cql-text').focus();
      } catch (error) { status.textContent = error.message; }
    });

    return {
      refreshExampleProviders,
      resetSession() {
        clearDraft();
        questionInput.value = '';
        activeExample = null;
        status.textContent = '';
        $('cql-text').value = '';
      }
    };
  }
  root.CqlAsk = { init, queryInstructions };
})(globalThis);
