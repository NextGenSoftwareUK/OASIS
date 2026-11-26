/* SQL-like sandbox syntax compiled to the existing typed WEB4 request. */
(function (root) {
  'use strict';

  const providers = ['MongoDBOASIS', 'SQLLiteDBOASIS', 'Neo4jOASIS', 'HoloOASIS', 'SolanaOASIS', 'EthereumOASIS', 'PolygonOASIS', 'ArbitrumOASIS', 'NEAROASIS', 'EOSIOOASIS', 'SOLIDOASIS', 'IPFSOASIS', 'ActivityPubOASIS', 'CosmosBlockChainOASIS', 'PolkadotOASIS', 'TONOASIS', 'HashgraphOASIS', 'TRONOASIS', 'NostrOASIS', 'AxelarOASIS', 'BitcoinOASIS', 'XRPLOASIS', 'StellarOASIS', 'CardanoOASIS', 'BNBChainOASIS', 'AvalancheOASIS', 'BaseOASIS', 'OptimismOASIS', 'SuiOASIS', 'AptosOASIS', 'StarknetOASIS', 'FantomOASIS', 'AlgorandOASIS', 'FilecoinOASIS', 'ZcashOASIS', 'RadixOASIS'];
  const utcDateParts = Object.freeze({
    YEAR:'UtcYear', MONTH:'UtcMonth', DAY:'UtcDay', DAYOFYEAR:'UtcDayOfYear',
    HOUR:'UtcHour', MINUTE:'UtcMinute', SECOND:'UtcSecond'
  });
  const utcDatePartNames = new Set(Object.values(utcDateParts));
  const evmAddress = /^0x[0-9a-fA-F]{40}$/;
  const lowercaseHex32 = /^0x[0-9a-f]{64}$/;
  const starknetFieldPrime = (1n << 251n) + 17n * (1n << 192n) + 1n;
  const accountProfiles = {
    AlgorandOASIS: { key: 'AlgorandAddress', requiresSingleKeyFilter: true, accepts: value => /^[A-Z2-7]{58}$/.test(value) },
    FilecoinOASIS: { key: 'FilecoinAccountAddress', requiresSingleKeyFilter: true, accepts: value => evmAddress.test(value) },
    ZcashOASIS: { key: 'ZcashBlockHash', requiresSingleKeyFilter: true, accepts: value => /^[0-9a-f]{64}$/.test(value) },
    RadixOASIS: { key: 'RadixResourceAddress', requiresSingleKeyFilter: true, accepts: value =>
      /^resource_rdx1[qpzry9x8gf2tvdw0s3jn54khce6mua7l]{54}$/.test(value) },
    SuiOASIS: { key: 'SuiObjectId', requiresSingleKeyFilter: true, accepts: value => lowercaseHex32.test(value) },
    AptosOASIS: { key: 'AptosAccountAddress', requiresSingleKeyFilter: true, accepts: value => lowercaseHex32.test(value) },
    StarknetOASIS: { key: 'StarknetBlockHash', requiresSingleKeyFilter: true, accepts: value =>
      /^0x(?:0|[1-9a-f][0-9a-f]{0,62})$/.test(value) && BigInt(value) < starknetFieldPrime },
    FantomOASIS: { key: 'FantomAccountAddress', requiresSingleKeyFilter: true, accepts: value => evmAddress.test(value) },
    BitcoinOASIS: { key: 'BitcoinBlockHash', requiresSingleKeyFilter: true, accepts: value => /^[0-9a-f]{64}$/.test(value) },
    XRPLOASIS: { key: 'XRPLLedgerHash', requiresSingleKeyFilter: true, accepts: value => /^[0-9A-F]{64}$/.test(value) },
    StellarOASIS: { key: 'StellarLedgerSequence', requiresSingleKeyFilter: true, accepts: value =>
      /^[1-9][0-9]{0,9}$/.test(value) && BigInt(value) <= 4294967295n },
    CardanoOASIS: { key: 'CardanoBlockHash', requiresSingleKeyFilter: true, accepts: value => /^[0-9a-f]{64}$/.test(value) },
    BNBChainOASIS: { key: 'BNBChainAccountAddress', requiresSingleKeyFilter: true, accepts: value => evmAddress.test(value) },
    AvalancheOASIS: { key: 'AvalancheAccountAddress', requiresSingleKeyFilter: true, accepts: value => evmAddress.test(value) },
    BaseOASIS: { key: 'BaseAccountAddress', requiresSingleKeyFilter: true, accepts: value => evmAddress.test(value) },
    OptimismOASIS: { key: 'OptimismAccountAddress', requiresSingleKeyFilter: true, accepts: value => evmAddress.test(value) },
    NostrOASIS: { key: 'NostrEventId', accepts: value => /^[0-9a-f]{64}$/.test(value) },
    TRONOASIS: { key: 'TronBlockHeight', accepts: value =>
      /^[1-9][0-9]{0,18}$/.test(value) && BigInt(value) <= 9223372036854775807n },
    AxelarOASIS: { key: 'AxelarBlockHeight', requiresSingleKeyFilter: true, accepts: value =>
      /^[1-9][0-9]{0,18}$/.test(value) && BigInt(value) <= 9223372036854775807n },
    HashgraphOASIS: { key: 'HederaAccountId', accepts: value =>
      /^(?:0|[1-9][0-9]{0,19})\.(?:0|[1-9][0-9]{0,19})\.[1-9][0-9]{0,19}$/.test(value) &&
      value.split('.').every(part => BigInt(part) <= 18446744073709551615n) },
    CosmosBlockChainOASIS: { key: 'CosmosValidatorAddress', accepts: value => /^[a-z0-9]{8,90}$/.test(value) },
    PolkadotOASIS: { key: 'PolkadotBlockHash', accepts: value => /^0x[0-9a-fA-F]{64}$/.test(value) },
    TONOASIS: { key: 'TonBlockRootHash', accepts: value => /^[a-zA-Z0-9+/_-]{43}=$/.test(value) },
    SOLIDOASIS: { key: 'SolidRecordId', accepts: value => /^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(value) },
    IPFSOASIS: { key: 'IPFSRecordCid', accepts: value => /^[a-zA-Z0-9]{1,128}$/.test(value) },
    ActivityPubOASIS: { key: 'MastodonStatusId', maximumKeys: 40, accepts: value => /^[0-9]{1,128}$/.test(value) },
    SolanaOASIS: { key: 'SolanaAccountAddress', accepts: value => /^[1-9A-HJ-NP-Za-km-z]{32,44}$/.test(value) },
    EthereumOASIS: { key: 'EthereumAccountAddress', accepts: value => evmAddress.test(value) },
    PolygonOASIS: { key: 'PolygonAccountAddress', accepts: value => evmAddress.test(value) },
    ArbitrumOASIS: { key: 'ArbitrumAccountAddress', accepts: value => evmAddress.test(value) },
    NEAROASIS: { key: 'NearAccountId', accepts: value => value.length >= 2 && value.length <= 64 &&
      /^[a-z0-9]+(?:[._-][a-z0-9]+)*$/.test(value) },
    EOSIOOASIS: { key: 'EOSIOAccountName', accepts: value =>
      /^[a-z1-5.]{1,12}(?:[a-j1-5.])?$/.test(value) && !value.endsWith('.') }
  };
  const fields = { id: ['Id', 'Guid'], name: ['Name', 'String'],
    parentholonid: ['ParentHolonId', 'Guid'], description: ['Description', 'String'], version: ['Version', 'Integer'],
    isactive: ['IsActive', 'Boolean'] };
  const projectionFields = { ...fields, holontype: ['HolonType', 'HolonType'],
    ispublic: ['IsPublic', 'Boolean'], versionid: ['VersionId', 'Guid'],
    previousversionid: ['PreviousVersionId', 'Guid'], createdbyavatarid: ['CreatedByAvatarId', 'Guid'],
    modifiedbyavatarid: ['ModifiedByAvatarId', 'Guid'] };
  const comparisons = { '=': 'Equal', '<>': 'NotEqual', '!=': 'NotEqual',
    '<': 'LessThan', '<=': 'LessThanOrEqual', '>': 'GreaterThan',
    '>=': 'GreaterThanOrEqual', CONTAINS: 'Contains',
    STARTSWITH: 'StartsWith', ENDSWITH: 'EndsWith' };

  function discoverKeyProfiles(sources) {
    const profiles = Object.create(null);
    for (const source of sources) {
      const { provider, textKeyProfile } = source;
      if (!source.available || !source.queryOnly || !textKeyProfile || providers.includes(provider) ||
          !/^[A-Za-z][A-Za-z0-9]{0,39}$/.test(provider) || ['Default', 'None'].includes(provider)) continue;
      const { metadataKey, maximumKeyCharacters, maximumSetKeys } = textKeyProfile;
      if (!/^[A-Za-z_][A-Za-z0-9_]{0,63}$/.test(metadataKey ?? '') ||
          !Number.isInteger(maximumKeyCharacters) || maximumKeyCharacters < 1 || maximumKeyCharacters > 512 ||
          !Number.isInteger(maximumSetKeys) || maximumSetKeys < 0 || maximumSetKeys > 40) continue;
      profiles[provider] = {
        key: metadataKey, maximumKeys: maximumSetKeys, requiresSingleKeyFilter: true,
        accepts: value => value.length <= maximumKeyCharacters && /^[!-~]+$/.test(value)
      };
    }
    return profiles;
  }

  const completeSelectedBounds = new WeakSet();

  function compile(text, discoveredProfiles = {}, parameters = {}) {
    return compileQuery(text,discoveredProfiles,parameters,Object.create(null),0);
  }

  function compileQuery(text, discoveredProfiles = {}, parameters = {}, cteScope = Object.create(null), compileDepth = 0) {
    if (compileDepth > 8) throw new Error('Nested selected queries exceed eight levels.');
    const keyProfiles = { ...discoveredProfiles, ...accountProfiles };
    const selectableProviders = [...providers, ...Object.keys(discoveredProfiles)];
    if (typeof text !== 'string' || text.length > 4096) throw new Error('Enter a query of up to 4,096 characters.');
    const lexer = /\s+|--[^\r\n]*|\/\*[\s\S]*?\*\/|'(?:[^']|'')*'|"(?:[^"]|"")*"|@[A-Za-z_][A-Za-z_0-9]*|[A-Za-z_][A-Za-z_0-9]*|-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?|<=|>=|<>|!=|[(),.*=<>;]/gy;
    if (!parameters || typeof parameters !== 'object' || Array.isArray(parameters))
      throw new Error('Parameters must be a JSON object keyed by parameter name.');
    const parameterNames = Object.keys(parameters);
    if (parameterNames.length > 64 || parameterNames.some(name => !/^[A-Za-z_][A-Za-z_0-9]{0,63}$/.test(name)))
      throw new Error('Use up to 64 named parameters, each with at most 64 letters, digits or underscores.');
    let boundCharacters = 0;
    function scalarParameterToken(value) {
      let literal;
      if (value === null) literal = 'NULL';
      else if (typeof value === 'boolean') literal = value ? 'TRUE' : 'FALSE';
      else if (typeof value === 'string') {
        if (value.length > 512) throw new Error('String parameter values may contain up to 512 characters.');
        literal = "'" + value.replace(/'/g, "''") + "'";
      } else if (typeof value === 'number') {
        if (!Number.isFinite(value) || Number.isInteger(value) && !Number.isSafeInteger(value))
          throw new Error('Use a finite safe JSON number or an exact numeric parameter object.');
        literal = Object.is(value, -0) ? '-0' : String(value);
      } else if (value && typeof value === 'object' && !Array.isArray(value)) {
        const type = Object.getOwnPropertyDescriptor(value, 'type');
        const exact = Object.getOwnPropertyDescriptor(value, 'value');
        if (Object.keys(value).length !== 2 || !type || !exact ||
            !Object.hasOwn(type, 'value') || !Object.hasOwn(exact, 'value') ||
            typeof type.value !== 'string' || typeof exact.value !== 'string')
          throw new Error('Exact numeric parameters require only type and string value.');
        const domain = type.value.toUpperCase(), digits = exact.value;
        if (domain === 'INT64') {
          if (!/^(?:0|-?[1-9][0-9]{0,18})$/.test(digits) ||
              BigInt(digits) < -9223372036854775808n || BigInt(digits) > 9223372036854775807n)
            throw new Error('Expected an exact INT64 parameter.');
        } else if (domain === 'UINT64') {
          if (!/^(?:0|[1-9][0-9]{0,19})$/.test(digits) || BigInt(digits) > 18446744073709551615n)
            throw new Error('Expected an exact UINT64 parameter.');
        } else if (domain === 'DECIMAL') exactDecimal(digits);
        else if (domain === 'SINGLE' || domain === 'DOUBLE') {
          if (digits.length > 32 || !/^-?[0-9]+(?:[.][0-9]+)?(?:[eE][+-]?[0-9]+)?$/.test(digits) ||
              !Number.isFinite(Number(digits)))
            throw new Error('Expected a finite exact floating parameter spelling.');
        } else throw new Error('Use INT64, UINT64, DECIMAL, SINGLE or DOUBLE for exact numeric parameters.');
        literal = digits;
      } else throw new Error('Parameters accept scalar JSON values or exact numeric objects.');
      boundCharacters += literal.length;
      if (boundCharacters > 16384) throw new Error('Bound parameter values exceed the query value budget.');
      return literal;
    }
    function parameterTokens(token, arrayAllowed) {
      const name = token.slice(1);
      const property = Object.getOwnPropertyDescriptor(parameters, name);
      if (!property || !Object.hasOwn(property, 'value'))
        throw new Error('Missing data value for parameter @' + name + '.');
      const value = property.value;
      if (!Array.isArray(value)) return [scalarParameterToken(value)];
      if (!arrayAllowed) throw new Error('A list parameter must be the complete value list in IN (@name).');
      if (value.length < 1 || value.length > 40)
        throw new Error('List parameters require between one and forty non-null scalar values.');
      const result = [];
      for (let position = 0; position < value.length; position++) {
        const item = Object.getOwnPropertyDescriptor(value, String(position));
        if (!item || !Object.hasOwn(item, 'value') || item.value === null || Array.isArray(item.value))
          throw new Error('List parameters require present non-null scalar values.');
        if (position) result.push(',');
        result.push(scalarParameterToken(item.value));
      }
      return result;
    }

    const tokens = [];
    while (lexer.lastIndex < text.length) {
      const position = lexer.lastIndex;
      const match = lexer.exec(text);
      if (!match) throw new Error('Unexpected character at position ' + (position + 1) + '.');
      const token = match[0];
      if (token.trim() && !token.startsWith('--') && !token.startsWith('/*')) tokens.push(token);
    }
    const rawTokens = tokens.slice();

    function cteIdentifier(token, description) {
      const value = token?.startsWith('"') ? token.slice(1,-1).replace(/""/g, '"') : token;
      if (!/^[A-Za-z][A-Za-z0-9_]{0,63}$/.test(value ?? ''))
        throw new Error('Expected a bounded ' + description + '.');
      return value;
    }
    function topFrom(parts) {
      let depth = 0;
      for (let position = 0; position < parts.length; position++) {
        if (parts[position] === '(') depth++;
        else if (parts[position] === ')') depth--;
        else if (!depth && parts[position].toUpperCase() === 'FROM') return position;
      }
      return -1;
    }
    function explicitPage(parts) {
      let depth = 0;
      return parts.some((token, position) => {
        if (token === '(') depth++;
        else if (token === ')') depth--;
        return !depth && parts[position-1]!=='.' && (['LIMIT','FETCH'].includes(token.toUpperCase()) ||
          token.toUpperCase() === 'TOP' && [1,2].includes(position));
      });
    }
    function frame(compiled) {
      if (compiled.path === 'data/query-cql/set' &&
          !compiled.body.left.columns && !compiled.body.right.columns &&
          !compiled.body.outputColumns) {
        return {entitySet:true, provider:compiled.body.provider, columns:[], stages:[]};
      }
      if(compiled.path==='data/query-cql/set' && compiled.body.outputColumns &&
          !compiled.body.left.columns && !compiled.body.right.columns) {
        if(!['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS'].includes(compiled.body.provider) ||
            !Array.isArray(compiled.body.outputColumns) || compiled.body.outputColumns.length<1 ||
            compiled.body.outputColumns.length>32)
          throw new Error('Late entity-set selected stages require bounded native database columns.');
        const stages=(compiled.body.selectedStages ?? []).slice();
        return {provider:compiled.body.provider,setBody:compiled.body,stages,
          columns:visibleSelectedColumns(compiled.body.outputColumns,stages)};
      }
      if (!['data/query-cql/projection','data/query-cql/composition'].includes(compiled.path))
        throw new Error('A WITH source requires explicit selected columns from one native database.');
      const provider = compiled.body.provider;
      if (!['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS'].includes(provider))
        throw new Error('WITH selected stages require MongoDB, SQLite or Neo4j.');
      const {provider: ignored, includeMetadata, ...source} = compiled.path.endsWith('/composition')
        ? compiled.body.source : compiled.body;
      const allowed = new Set(['filters','anyOf','anyChildren','anyDescendants','where','orderBy',
        'descending','selectedOrder','skip','take','columns']);
      if (Object.keys(source).some(key => !allowed.has(key)) || !Array.isArray(source.columns) ||
          !Number.isInteger(source.take) || source.take < 0 || source.take > 100)
        throw new Error('WITH requires a bounded native column source without mapping, graph or deadline overrides.');
      const stages = compiled.path.endsWith('/composition') ? compiled.body.stages.slice() : [];
      return {provider, source, stages, columns:visibleSelectedColumns(source.columns,stages)};
    }
    function visibleSelectedColumns(source,stages) {
      let columns=source.slice();
      for(const stage of stages) if(stage.operation==='Select') {
        columns=stage.columns.map(binding=>{
          const input=columns.find(column=>column.alias===binding.sourceAlias);
          if(!input)throw new Error('A selected stage references an unavailable alias.');
          return {...input,alias:binding.alias};
        });
      }
      return columns;
    }
    function bindSelectedFrame(input,stages) {
      return input.setBody
        ?{path:'data/query-cql/set',body:{...input.setBody,selectedStages:stages}}
        :{path:'data/query-cql/composition',body:{provider:input.provider,source:input.source,stages}};
    }
    function readCteDefinitions() {
      let cursor = 1;
      if (rawTokens[cursor]?.toUpperCase() === 'RECURSIVE')
        throw new Error('Recursive WITH queries require a separate native recursive contract.');
      const scope = Object.assign(Object.create(null), cteScope);
      do {
        const name = cteIdentifier(rawTokens[cursor++], 'WITH name');
        const key = name.toLowerCase();
        if (Object.hasOwn(scope,key) || selectableProviders.some(provider => provider.toLowerCase() === key) ||
            Object.keys(scope).length >= 8)
          throw new Error('Use up to eight distinct WITH names without replacing a provider.');
        const declaredAliases = [];
        if (rawTokens[cursor] === '(') {
          cursor++;
          do {
            const alias = cteIdentifier(rawTokens[cursor++], 'WITH column alias');
            if (declaredAliases.length >= 32 ||
                declaredAliases.some(item => item.toLowerCase() === alias.toLowerCase()))
              throw new Error('Use up to32 distinct WITH column aliases.');
            declaredAliases.push(alias);
            if (rawTokens[cursor] !== ',') break;
            cursor++;
          } while (cursor < rawTokens.length);
          if (rawTokens[cursor++] !== ')') throw new Error('Close the WITH column alias list.');
        }
        if (rawTokens[cursor++]?.toUpperCase() !== 'AS' || rawTokens[cursor++] !== '(')
          throw new Error('Use WITH name AS (SELECT explicit columns ... LIMIT n).');
        const begin = cursor;
        let depth = 1;
        while (cursor < rawTokens.length && depth) {
          if (rawTokens[cursor] === '(') depth++;
          else if (rawTokens[cursor] === ')') depth--;
          cursor++;
        }
        if (depth) throw new Error('Unbalanced WITH source parentheses.');
        const sourceTokens = rawTokens.slice(begin,cursor-1);
        if (sourceTokens[0]?.toUpperCase() !== 'SELECT')
          throw new Error('Declare selected WITH sources together, in dependency order.');
        const from = topFrom(sourceTokens);
        const sourceName = sourceTokens[from+1]?.replace(/^"|"$/g,'');
        const inherited = from >= 0 && (sourceTokens[from+1] === '(' || Object.hasOwn(scope, sourceName?.toLowerCase()));
        if (!inherited && !explicitPage(sourceTokens))
          throw new Error('Give the initial WITH source an explicit TOP, LIMIT or FETCH bound; an editor default page is not a complete source.');
        let compiled = compileQuery(sourceTokens.join(' '), discoveredProfiles, parameters, scope, compileDepth+1);
        const captured = frame(compiled);
        if (declaredAliases.length) {
          if (captured.entitySet) throw new Error('Select explicit columns after the entity set before declaring WITH column aliases.');
          if (declaredAliases.length !== captured.columns.length)
            throw new Error('WITH column alias count must match its selected columns.');
          const columns = declaredAliases.map((alias, position) =>
            ({alias, sourceAlias:captured.columns[position].alias}));
          const identity = columns.every(binding => binding.alias === binding.sourceAlias);
          if (!identity) {
            if (captured.stages.length >= 16)
              throw new Error('WITH composition exceeds16 stages.');
            compiled=bindSelectedFrame(captured,[...captured.stages,{operation:'Select',columns}]);
          }
        }
        completeSelectedBounds.add(compiled);
        scope[key] = compiled;
        if (rawTokens[cursor] !== ',') break;
        cursor++;
      } while (cursor < rawTokens.length);
      if (cursor === rawTokens.length) throw new Error('A WITH query requires its final SELECT.');
      return compileQuery(rawTokens.slice(cursor).join(' '), discoveredProfiles, parameters, scope, compileDepth+1);
    }
    function readCteSelection(compiled, from) {
      const input = frame(compiled);
      if (input.entitySet) {
        const sourceName=cteIdentifier(tokens[from+1], 'entity-set source');
        let cursor=from+2, tableAlias=sourceName;
        if (tokens[cursor]?.toUpperCase() === 'AS') {
          tableAlias=cteIdentifier(tokens[cursor+1], 'table alias');cursor+=2;
        } else if (/^[A-Za-z][A-Za-z0-9_]*$/.test(tokens[cursor] ?? '') &&
            !['WHERE','ORDER','LIMIT','OFFSET','FETCH','GROUP','HAVING'].includes(tokens[cursor].toUpperCase())) {
          tableAlias=cteIdentifier(tokens[cursor++], 'table alias');
        }
        const distinct=rawTokens[1]?.toUpperCase()==='DISTINCT';
        const outer=[...rawTokens.slice(0,from),'FROM',input.provider,...rawTokens.slice(cursor)];
        if(distinct)outer.splice(1,1);
        const qualified=[];
        for(let position=0;position<outer.length;position++) {
          if(outer[position-1]!=='.' && outer[position].toLowerCase()===tableAlias.toLowerCase() && outer[position+1]==='.') {
            position++;continue;
          }
          qualified.push(outer[position]);
        }
        const selection=compileQuery(qualified.join(' '),discoveredProfiles,parameters,
          Object.create(null),compileDepth+1);
        const reduction=selection.path==='data/query-cql/scalar';
        if(reduction && (distinct || !['LongCount','Any'].includes(selection.body.operation) || selection.body.selection))
          throw new Error('Combined-record aggregates currently support COUNT(*)/COUNT(1) and ANY(*); selected-value and numeric aggregates need independent native reduction.');
        if(!reduction && (!['data/query-cql','data/query-cql/projection'].includes(selection.path) ||
            selection.path.endsWith('/projection')&&!Array.isArray(selection.body.columns)))
          throw new Error('Select native entity columns or * after the set; DISTINCT and aggregates require their own result stage.');
        const body=selection.body;
        const stages=(compiled.body.entityStages ?? []).slice();
        function conjunction(parts) {
          return parts.length===0 ? undefined : parts.length===1 ? parts[0] : {kind:'And',terms:parts};
        }
        function predicate(part) {
          const parts=part.where?[part.where]:[];
          for(const filter of part.filters ?? [])parts.push({kind:'Filter',filter});
          if(part.anyOf?.length)parts.push(part.anyOf.length===1
            ?{kind:'Filter',filter:part.anyOf[0]}
            :{kind:'Or',terms:part.anyOf.map(filter=>({kind:'Filter',filter}))});
          for(const relation of part.anyChildren ?? []) {
            const match=predicate(relation);
            parts.push({kind:'Child',...(match?{match}:{})});
          }
          for(const relation of part.anyDescendants ?? []) {
            const match=predicate(relation);
            parts.push({kind:'Descendant',...(match?{match}:{}),
              ...(relation.maximumDepth===undefined?{}:{maximumDepth:relation.maximumDepth}),
              ...(relation.maximumNodes===undefined?{}:{maximumNodes:relation.maximumNodes})});
          }
          return conjunction(parts);
        }
        const where=predicate(body);
        if(reduction) {
          if(explicitPage(qualified))
            throw new Error('Apply the input LIMIT/OFFSET inside the combined-record source before counting.');
          return {path:compiled.path,body:{...compiled.body,includeMetadata:false,
            reduction:body.operation,...(where?{reductionWhere:where}:{})},
            ...(selection.scalarAlias?{scalarAlias:selection.scalarAlias}:{})};
        }
        if(where)stages.push({operation:'Where',where});
        const hasOrdering=rawTokens.slice(cursor).some((token,position,tail)=>
          token.toUpperCase()==='ORDER'&&tail[position+1]?.toUpperCase()==='BY');
        if(distinct) {
          if(!Array.isArray(body.columns)||!body.columns.length)
            throw new Error('DISTINCT after an entity set requires explicit selected columns.');
          const selected=[{operation:'Distinct'}];
          if(hasOrdering) {
            const order=body.selectedOrder?.map(term=>({...term})) ?? [{
              alias:body.columns.find(column=>column.field===body.orderBy&&!column.function)?.alias,
              descending:body.descending??false}];
            if(order.length>8||order.some(term=>!body.columns.some(column=>column.alias===term.alias)))
              throw new Error('DISTINCT orders only the explicitly selected visible columns.');
            selected.push({operation:'OrderBy',order});
          }
          if(body.skip)selected.push({operation:'Skip',count:body.skip});
          if(explicitPage(qualified))selected.push({operation:'Take',count:body.take});
          if(stages.length>16)throw new Error('Entity-set result composition exceeds16 stages.');
          return {path:compiled.path,body:{...compiled.body,
            ...(stages.length?{entityStages:stages}:{}),includeMetadata:false,
            outputColumns:body.columns,selectedStages:selected}};
        }
        if(hasOrdering) {
          const ordering=body.selectedOrder?.map(term=>{
            const column=body.columns.find(item=>item.alias===term.alias);
            if(!column?.field || column.function || !['Name','Description','Version','Id'].includes(column.field))
              throw new Error('Entity-set result ordering requires a direct Name, Description, Version or Id column.');
            return {field:column.field,descending:term.descending};
          }) ?? [{field:body.orderBy,descending:body.descending ?? false}];
          if(ordering.length>4)throw new Error('Use up to four native entity ordering terms.');
          stages.push({operation:'OrderBy',ordering});
        }
        if(body.skip)stages.push({operation:'Skip',count:body.skip});
        if(explicitPage(qualified))stages.push({operation:'Take',count:body.take});
        if(stages.length>16)throw new Error('Entity-set result composition exceeds16 stages.');
        return {path:compiled.path,body:{...compiled.body,
          ...(stages.length?{entityStages:stages}:{}),
          ...(body.columns?{includeMetadata:false,outputColumns:body.columns}:{})}};
      }
      const sourceName = cteIdentifier(tokens[from+1], 'WITH source name');
      let tableAlias = sourceName;
      let afterFrom = from+2;
      if (tokens[afterFrom]?.toUpperCase() === 'AS') {
        tableAlias = cteIdentifier(tokens[afterFrom+1], 'table alias'); afterFrom += 2;
      } else if (/^[A-Za-z][A-Za-z0-9_]*$/.test(tokens[afterFrom] ?? '') &&
          !['WHERE','ORDER','LIMIT','OFFSET','FETCH','GROUP','HAVING','JOIN','INNER','LEFT','RIGHT','FULL','CROSS'].includes(tokens[afterFrom].toUpperCase())) {
        tableAlias = cteIdentifier(tokens[afterFrom++], 'table alias');
      }
      const byName = new Map(input.columns.map(column => [column.alias.toLowerCase(),column]));
      if (byName.size !== input.columns.length) throw new Error('WITH aliases must be unambiguous without case.');
      function column() {
        let name = cteIdentifier(tokens[index++], 'visible selected alias');
        if (accept('.')) {
          if (name.toLowerCase() !== tableAlias.toLowerCase())
            throw new Error('Use the current WITH table alias.');
          name = cteIdentifier(tokens[index++], 'visible selected alias');
        }
        const selected = byName.get(name.toLowerCase());
        if (!selected) throw new Error('The WITH source does not expose column ' + name + '.');
        return selected;
      }
      function domain(selected) {
        return selected.function === 'Utf16Length' || selected.function === 'Int32Coalesce' || utcDatePartNames.has(selected.function) ? 'Int32'
          : ['TextCoalesce','BooleanTextCase'].includes(selected.function) ? 'String'
          : selected.valueType ?? projectionFields[selected.field?.toLowerCase()]?.[1];
      }
      function nullable(selected) {
        return domain(selected) === 'String' || selected.nullable === true;
      }
      const leaf = (selected, operator, value) => ({kind:'leaf', selected,
        filter:{alias:selected.alias, operator,...(value === undefined ? {} : {value})}});
      const logical = (kind,left,right) => ({kind,left,right});
      function condition(depth) {
        if (depth > 8) throw new Error('WITH conditions allow at most eight nested levels.');
        if (accept('NOT')) return {kind:'not',value:condition(depth+1)};
        if (accept('(')) {const node=alternatives(depth+1);expect(')');return node;}
        const selected = column(), type = domain(selected);
        const literal = () => readLiteral(type === 'Int64' ? 'Integer' : type, type === 'Int64');
        if (accept('IS')) {
          const not = accept('NOT');expect('NULL');
          if (!nullable(selected)) throw new Error('This selected alias cannot be null.');
          return leaf(selected,not ? 'NotEqual' : 'IsNull',not ? null : undefined);
        }
        const not = accept('NOT');
        let node;
        if (accept('BETWEEN')) {
          const low=literal();expect('AND');const high=literal();
          if (['String','Boolean','Guid','HolonType'].includes(type))
            throw new Error('BETWEEN requires an ordered numeric or timestamp alias.');
          node=logical('and',leaf(selected,'GreaterThanOrEqual',low),leaf(selected,'LessThanOrEqual',high));
        } else if (accept('IN')) {
          expect('(');let count=0;
          do {
            if (++count > 32) throw new Error('Use up to32 WITH membership values.');
            const item=leaf(selected,'Equal',literal());
            node=node ? logical('or',node,item) : item;
          } while (accept(','));
          expect(')');
        } else if (accept('LIKE')) {
          const match=readLike(type,true);
          node=match.operator === 'IsNotNull' ? {...leaf(selected,'NotEqual',null),matchAllLike:true}
            : leaf(selected,match.operator,match.value);
        } else {
          if (not) throw new Error('Use NOT IN, NOT BETWEEN or NOT LIKE for a selected alias.');
          const comparison=tokens[index++];
          if (!['=','<>','!=','<','<=','>','>='].includes(comparison) ||
              ['String','Boolean','Guid','HolonType'].includes(type) && !['=','<>','!='].includes(comparison))
            throw new Error('Use a supported exact selected-alias comparison.');
          node=leaf(selected,comparisons[comparison],literal());
        }
        return not ? {kind:'not',value:node} : node;
      }
      function conjunction(depth) {
        let node=condition(depth);
        while (accept('AND')) node=logical('and',node,condition(depth));
        return node;
      }
      function alternatives(depth) {
        let node=conjunction(depth);
        while (accept('OR')) node=logical('or',node,conjunction(depth));
        return node;
      }
      const inverse={Equal:'NotEqual',NotEqual:'Equal',LessThan:'GreaterThanOrEqual',
        LessThanOrEqual:'GreaterThan',GreaterThan:'LessThanOrEqual',GreaterThanOrEqual:'LessThan',
        Contains:'NotContains',NotContains:'Contains',StartsWith:'NotStartsWith',NotStartsWith:'StartsWith',
        EndsWith:'NotEndsWith',NotEndsWith:'EndsWith'};
      function normalize(node,negative=false) {
        if (node.kind === 'not') return normalize(node.value,!negative);
        if (node.kind !== 'leaf') return {kind:negative ? node.kind === 'and' ? 'or' : 'and' : node.kind,
          left:normalize(node.left,negative),right:normalize(node.right,negative)};
        if (negative && node.matchAllLike) return logical('and',
          {kind:'leaf',filter:{alias:node.filter.alias,operator:'IsNull'}},
          {kind:'leaf',filter:{alias:node.filter.alias,operator:'NotEqual',value:null}});
        let filter={...node.filter};
        if (negative) {
          if (filter.operator === 'IsNull') filter={alias:filter.alias,operator:'NotEqual',value:null};
          else if (filter.operator === 'NotEqual' && filter.value === null) filter={alias:filter.alias,operator:'IsNull'};
          else {
            if (!inverse[filter.operator]) throw new Error('Unsupported negated selected comparison.');
            filter.operator=inverse[filter.operator];
          }
        }
        const result={kind:'leaf',filter};
        if (nullable(node.selected) && filter.value !== null &&
            ['NotEqual','NotContains','NotStartsWith','NotEndsWith'].includes(filter.operator))
          return logical('and',{kind:'leaf',filter:{alias:filter.alias,operator:'NotEqual',value:null}},result);
        return result;
      }
      function clauses(node) {
        if (node.kind === 'leaf') return [[node.filter]];
        const left=clauses(node.left),right=clauses(node.right);
        const result=node.kind === 'and' ? [...left,...right] : left.flatMap(a=>right.map(b=>[...a,...b]));
        if (result.length > 16 || result.reduce((sum,part)=>sum+part.length,0) > 32)
          throw new Error('The selected Boolean expression exceeds32 native comparisons.');
        return result;
      }
      expect('SELECT');
      const distinct = accept('DISTINCT');
      const leadingTake=readTop();
      const output=[];
      if (accept('*')) input.columns.forEach(item=>output.push({alias:item.alias,sourceAlias:item.alias}));
      else do {
        const selected=column();
        const alias=readColumnAlias(selected.alias);
        output.push({alias,sourceAlias:selected.alias});
      } while (accept(','));
      if (!output.length || output.length > 32 || new Set(output.map(item=>item.alias.toLowerCase())).size !== output.length)
        throw new Error('Choose up to32 distinct visible output aliases.');
      expect('FROM');index=afterFrom;
      const stages=input.stages.slice();
      const identity=output.length === input.columns.length &&
        output.every((item,position)=>item.alias === input.columns[position].alias && item.sourceAlias === item.alias);
      if (accept('WHERE')) {
        const parts=clauses(normalize(alternatives(1)));
        for (const part of parts) stages.push({operation:'Where',...(part.length === 1 ? {filters:part} : {anyOf:part})});
      }
      // SQL DISTINCT compares only this SELECT's visible tuple, before its order/page.
      // Earlier native source and result windows remain separate stages.
      if (distinct) {
        if (!identity) stages.push({operation:'Select',columns:output});
        stages.push({operation:'Distinct'});
      }
      if (accept('ORDER')) {
        expect('BY');const order=[];
        do {
          const token=tokens[index];
          let alias;
          if (/^[1-9][0-9]*$/.test(token ?? '')) {
            const position=integer(tokens[index++],1,output.length);alias=output[position-1].sourceAlias;
          } else if (tokens[index+1] !== '.') {
            const selected=output.find(item=>item.alias.toLowerCase() === token?.toLowerCase());
            if (selected) {index++;alias=selected.sourceAlias;}
          }
          if (!alias) alias=column().alias;
          if (distinct) {
            const visible=output.find(item=>item.sourceAlias === alias);
            if (!visible) throw new Error('DISTINCT orders only columns visible in this SELECT.');
            alias=visible.alias;
          }
          if (order.length >= 8 || order.some(term=>term.alias === alias)) throw new Error('Use up to eight distinct ordering aliases.');
          const descending=accept('DESC');if(!descending)accept('ASC');
          order.push({alias,descending});
        } while (accept(','));
        stages.push({operation:'OrderBy',order});
      }
      const window=readWindow(100,10000,0,leadingTake);
      if (window.skip !== undefined) stages.push({operation:'Skip',count:window.skip});
      if (window.take !== undefined) stages.push({operation:'Take',count:window.take});
      accept(';');
      if (index !== tokens.length) throw new Error('Unsupported WITH result clause.');
      if (!identity && !distinct) stages.push({operation:'Select',columns:output});
      if (stages.length > 16 || stages.reduce((sum,stage)=>sum+(stage.filters?.length ?? 0)+(stage.anyOf?.length ?? 0),0) > 32)
        throw new Error('WITH composition exceeds16 stages or32 comparisons.');
      if (!stages.length) return compiled;
      const selectedResult=bindSelectedFrame(input,stages);
      if (completeSelectedBounds.has(compiled)) completeSelectedBounds.add(selectedResult);
      return selectedResult;
    }

    if (rawTokens[0]?.toUpperCase() === 'WITH') return readCteDefinitions();
    function readDerivedTable(from) {
      let cursor = from + 2;
      const begin = cursor;
      let depth = 1;
      while (cursor < rawTokens.length && depth) {
        if (rawTokens[cursor] === '(') depth++;
        else if (rawTokens[cursor] === ')') depth--;
        cursor++;
      }
      if (depth) throw new Error('Unbalanced selected subquery parentheses.');
      const sourceTokens = rawTokens.slice(begin,cursor-1);
      if (sourceTokens[0]?.toUpperCase() !== 'SELECT')
        throw new Error('A derived table requires one selected query.');
      if (rawTokens[cursor]?.toUpperCase() === 'AS') cursor++;
      const alias = cteIdentifier(rawTokens[cursor++], 'derived table alias');
      if (['WHERE','GROUP','HAVING','ORDER','LIMIT','OFFSET','FETCH','JOIN','INNER','LEFT',
          'RIGHT','FULL','CROSS','UNION','INTERSECT','EXCEPT'].includes(alias.toUpperCase()))
        throw new Error('Give the selected subquery an explicit table alias.');
      const innerFrom = topFrom(sourceTokens);
      const innerName = sourceTokens[innerFrom+1]?.replace(/^"|"$/g,'').toLowerCase();
      const inherited = innerFrom >= 0 && (sourceTokens[innerFrom+1] === '(' ||
        Object.hasOwn(cteScope,innerName));
      if (!inherited && !explicitPage(sourceTokens))
        throw new Error('Give the initial derived source an explicit TOP, LIMIT or FETCH bound.');
      const captured = compileQuery(sourceTokens.join(' '),discoveredProfiles,parameters,cteScope,compileDepth+1);
      frame(captured);
      completeSelectedBounds.add(captured);
      const scope = Object.assign(Object.create(null),cteScope);
      scope[alias.toLowerCase()] = captured;
      const outer = [...rawTokens.slice(0,from+1),alias,...rawTokens.slice(cursor)];
      return compileQuery(outer.join(' '),discoveredProfiles,parameters,scope,compileDepth+1);
    }
    // A joined derived source belongs to the native join grammar, not the
    // selected-result composition grammar. Choose its owner before parsing.
    let joinDepth=0,hasTopLevelJoin=false;
    for(const token of rawTokens) {
      if(token==='(')joinDepth++;
      else if(token===')')joinDepth--;
      else if(joinDepth===0 && token.toUpperCase()==='JOIN')hasTopLevelJoin=true;
    }
    const derivedFrom = topFrom(rawTokens);
    if (!hasTopLevelJoin && rawTokens[0]?.toUpperCase() === 'SELECT' && derivedFrom >= 0 &&
        rawTokens[derivedFrom+1] === '(')
      return readDerivedTable(derivedFrom);


    // Parenthesized operands make each source's ORDER/LIMIT/OFFSET scope explicit.
    let setDepth = 0;
    const setOperators = [];
    for (let position = 0; position < rawTokens.length; position++) {
      const token = rawTokens[position];
      if (token === '(') setDepth++;
      else if (token === ')') setDepth--;
      else if (setDepth === 0 && ['UNION','INTERSECT','EXCEPT'].includes(token.toUpperCase()))
        setOperators.push(position);
      if (setDepth < 0) throw new Error('Unbalanced set-query parentheses.');
    }
    if (setOperators.length) {
      if (setDepth || setOperators.length !== 1)
        throw new Error('Use two parenthesized SELECT operands for a native set query.');
      const position = setOperators[0];
      let kind = rawTokens[position].toUpperCase(), rightStart = position + 1;
      if (kind === 'UNION' && rawTokens[rightStart]?.toUpperCase() === 'ALL') {
        kind = 'Concat'; rightStart++;
      } else kind = {UNION:'Union',INTERSECT:'Intersect',EXCEPT:'Except'}[kind];
      const rightTokens = rawTokens.slice(rightStart);
      if (rightTokens.at(-1) === ';') rightTokens.pop();
      let rightEnd = -1, rightDepth = 0;
      if (rightTokens[0] === '(') {
        for (let position = 0; position < rightTokens.length; position++) {
          if (rightTokens[position] === '(') rightDepth++;
          else if (rightTokens[position] === ')' && --rightDepth === 0) { rightEnd = position; break; }
        }
      }
      function topWindow(parts) {
        let depth = 0;
        for (let position = 0; position < parts.length; position++) {
          if (parts[position] === '(') depth++;
          else if (parts[position] === ')') depth--;
          else if (!depth && ['ORDER','LIMIT','OFFSET','FETCH'].includes(parts[position].toUpperCase())) return position;
        }
        return -1;
      }
      const trailingWindow = rightEnd >= 0 ? rightEnd + 1 : topWindow(rightTokens);
      const outputWindow = trailingWindow >= 0 ? rightTokens.splice(trailingWindow) : [];
      function operand(parts) {
        const parenthesized = parts[0] === '(' && parts.at(-1) === ')';
        if (!parenthesized && (parts[0]?.toUpperCase() !== 'SELECT' || topWindow(parts) >= 0))
          throw new Error('Wrap an independently ordered or paged SELECT operand in parentheses.');
        let compiled = compileQuery((parenthesized ? parts.slice(1,-1) : parts).join(' '),
          discoveredProfiles,parameters,cteScope,compileDepth+1);
        const inheritedBound = completeSelectedBounds.has(compiled);
        let visibleColumns;
        if (compiled.path === 'data/query-cql/composition') {
          const input = frame(compiled);
          visibleColumns = input.columns;
          // Preserve ordered native stages when they affect membership/order/windows.
          // Pure reselection may retain the existing direct-projection representation.
          const staged = input.stages.some(stage=>stage.operation !== 'Select');
          compiled = {path:'data/query-cql/projection',body:staged
            ? {provider:input.provider,...input.source,stages:input.stages}
            : {provider:input.provider,...input.source,columns:input.columns}};
        }
        if (!['data/query-cql','data/query-cql/projection'].includes(compiled.path) ||
            !['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS'].includes(compiled.body.provider))
          throw new Error('Native sets require SELECT * or explicit columns from one MongoDB, SQLite or Neo4j provider.');
        const {provider,includeMetadata,maximumDurationMilliseconds,...source} = compiled.body;
        const allowed = new Set(['filters','anyOf','anyChildren','anyDescendants','where','orderBy','descending','skip','take','columns','stages']);
        if (Object.keys(source).some(key => !allowed.has(key)))
          throw new Error('This set operand contains an independently unsupported source option.');
        if (maximumDurationMilliseconds !== undefined)
          throw new Error('Native set queries currently use the server-owned execution deadline.');
        // Only an explicit operand window can bound a complete set input.
        // Parentheses and ORDER BY alone must not inherit the editor's ten-row page.
        const operandTokens = parenthesized ? parts.slice(1,-1) : parts;
        const topIndex = operandTokens[1]?.toUpperCase() === 'DISTINCT' ? 2 : 1;
        let depth = 0;
        const explicitPage = operandTokens[topIndex]?.toUpperCase() === 'TOP' ||
          operandTokens.some(token => {
            if (token === '(') depth++;
            else if (token === ')') depth--;
            return depth === 0 && ['LIMIT','FETCH'].includes(token.toUpperCase());
          });
        if (!explicitPage && !inheritedBound) delete source.take;
        return {provider,includeMetadata,source,columns:visibleColumns ?? source.columns};
      }
      const left = operand(rawTokens.slice(0,position)), right = operand(rightTokens);
      if (left.provider !== right.provider)
        throw new Error('Native set operands must use the same provider.');
      const selected = Array.isArray(left.source.columns);
      if (selected !== Array.isArray(right.source.columns))
        throw new Error('Use SELECT * on both inputs or explicit columns on both inputs.');
      if (selected && left.columns.length !== right.columns.length)
        throw new Error('Selected set inputs must have the same column count.');
      let window = {};
      if (!selected && outputWindow.some(token => !/^(?:ORDER|BY|ASC|DESC|Name|Description|Version|Id|,|LIMIT|OFFSET|FETCH|FIRST|NEXT|ROWS?|ONLY|[0-9]+|@[A-Za-z_][A-Za-z_0-9]*)$/i.test(token)))
        throw new Error('A set result accepts declared entity ORDER BY terms and bounded LIMIT/OFFSET/FETCH paging.');
      let pageTokens = outputWindow.slice();
      if (selected && pageTokens[0]?.toUpperCase() === 'ORDER') {
        let cursor = 1;
        if (pageTokens[cursor++]?.toUpperCase() !== 'BY') throw new Error('Expected BY after ORDER.');
        const ordering = [], used = new Set();
        do {
          const token = pageTokens[cursor++];
          const column = /^[1-9][0-9]*$/.test(token ?? '')
            ? left.columns[Number(token) - 1]
            : left.columns.find(column => column.alias.toLowerCase() === token?.toLowerCase());
          if (!column || used.has(column.alias) || ordering.length >= 4)
            throw new Error('Order a selected set by up to four distinct visible left-column aliases or positions.');
          used.add(column.alias);
          const direction = pageTokens[cursor]?.toUpperCase();
          if (direction === 'ASC' || direction === 'DESC') cursor++;
          ordering.push({field:column.alias,descending:direction === 'DESC'});
          if (pageTokens[cursor] !== ',') break;
          cursor++;
          if (cursor >= pageTokens.length) throw new Error('Expected a visible selected alias after the comma.');
        } while (cursor < pageTokens.length);
        window.ordering = ordering;
        pageTokens = pageTokens.slice(cursor);
      }
      if (selected && pageTokens.length && !['LIMIT','OFFSET','FETCH'].includes(pageTokens[0].toUpperCase()))
        throw new Error('A selected set accepts visible ORDER BY aliases and bounded LIMIT/OFFSET/FETCH paging.');
      if (pageTokens.length) {
        const page = compile('SELECT * FROM ' + left.provider + ' ' + pageTokens.join(' '), discoveredProfiles, parameters);
        if (page.path !== 'data/query-cql') throw new Error('Invalid set output window.');
        const ordering = pageTokens.some(token => token.toUpperCase() === 'ORDER');
        const paging = pageTokens.some(token => ['LIMIT','OFFSET','FETCH'].includes(token.toUpperCase()));
        window = {
          ...window,
          ...(paging ? {skip:page.body.skip,take:page.body.take} : {}),
          ...(ordering ? {ordering:page.body.ordering ?? [{field:page.body.orderBy,descending:!!page.body.descending}]} : {})
        };
      }
      return {path:'data/query-cql/set',body:{
        provider:left.provider,kind,left:left.source,right:right.source,...window,
        maximumLeftRows:100,maximumRightRows:100,maximumResults:100,
        includeMetadata:!selected && !!(left.includeMetadata || right.includeMetadata)
      }};
    }
    tokens.length = 0;
    for (let position = 0; position < rawTokens.length; position++) {
      const token = rawTokens[position];
      if (!token.startsWith('@')) tokens.push(token);
      else tokens.push(...parameterTokens(token,
        rawTokens[position - 2]?.toUpperCase() === 'IN' &&
        rawTokens[position - 1] === '(' && rawTokens[position + 1] === ')'));
    }
    let index = 0;
    const peek = () => tokens[index]?.toUpperCase();
    const aliasBoundaries = new Set(['FROM','WHERE','GROUP','HAVING','ORDER','LIMIT','OFFSET','TIMEOUT',
      'JOIN','INNER','LEFT','RIGHT','FULL','CROSS','CHILD','ON','UNION','INTERSECT','EXCEPT']);
    if (Object.keys(cteScope).length) {
      const from = topFrom(tokens);
      const sourceName = tokens[from+1]?.replace(/^"|"$/g,'').toLowerCase();
      if (from >= 0 && Object.hasOwn(cteScope,sourceName))
        return readCteSelection(cteScope[sourceName],from);
    }

    function readColumnAlias(defaultAlias, description = 'column alias') {
      const explicit = accept('AS');
      if (!explicit && (!/^[A-Za-z][A-Za-z0-9_]{0,63}$/.test(tokens[index] ?? '') || aliasBoundaries.has(peek())))
        return defaultAlias;
      const alias = tokens[index++];
      if (!/^[A-Za-z][A-Za-z0-9_]{0,63}$/.test(alias ?? ''))
        throw new Error('Expected a ' + description + ' of up to 64 letters, digits or underscores, starting with a letter.');
      return alias;
    }

    function readSelectPosition(columns, clause) {
      if (!/^-?\d+$/.test(tokens[index] ?? '')) return undefined;
      const position = Number(tokens[index++]);
      if (!Number.isSafeInteger(position) || position < 1 || position > columns.length)
        throw new Error(clause + ' position must refer to an explicit SELECT column.');
      return columns[position - 1];
    }
    function readOrderColumn(columns) {
      return readSelectPosition(columns, 'ORDER BY')?.alias ?? tokens[index++];
    }
    function accept(token) {
      if (peek() !== token) return false;
      index++;
      return true;
    }
    function expect(token) {
      if (!accept(token)) throw new Error('Expected ' + token + ' near token ' + (index + 1) + '.');
    }
    function integer(token, minimum, maximum) {
      const value = Number(token);
      if (!/^-?\d+$/.test(token) || !Number.isInteger(value) || value < minimum || value > maximum)
        throw new Error('Expected an integer from ' + minimum + ' to ' + maximum + '.');
      return value;
    }
    function readTop() {
      if (!accept('TOP')) return undefined;
      const parenthesized = accept('(');
      const take = integer(tokens[index++], 1, 100);
      if (parenthesized) expect(')');
      if (peek() === 'PERCENT' || peek() === 'WITH')
        throw new Error('Use a bounded TOP row count without PERCENT or WITH TIES.');
      return take;
    }
    function readWindow(maximumTake, maximumSkip, minimumTake = 1, leadingTake) {
      if (leadingTake !== undefined) {
        if (['LIMIT', 'OFFSET', 'FETCH'].includes(peek()))
          throw new Error('Use either TOP or LIMIT/OFFSET/FETCH in one query window.');
        return { take: integer(String(leadingTake), minimumTake, maximumTake), skip: undefined };
      }
      let take, skip;
      if (accept('LIMIT')) {
        take = integer(tokens[index++], minimumTake, maximumTake);
        if (accept('OFFSET')) skip = integer(tokens[index++], 0, maximumSkip);
      } else {
        if (accept('OFFSET')) {
          skip = integer(tokens[index++], 0, maximumSkip);
          if (!accept('ROW') && !accept('ROWS') && peek() === 'FETCH')
            throw new Error('Use OFFSET n ROWS before FETCH.');
        }
        if (accept('FETCH')) {
          if (!accept('FIRST')) expect('NEXT');
          take = peek() === 'ROW' || peek() === 'ROWS' ? 1 : integer(tokens[index++], minimumTake, maximumTake);
          if (!accept('ROW')) expect('ROWS');
          expect('ONLY');
        }
      }
      return { take, skip };
    }

    function exactDecimal(token) {
      const match = /^(-?)(0|[1-9]\d*)(?:\.(\d{1,28}))?$/.exec(token ?? '');
      if (!match || token.length > 31) throw new Error('Expected an exact decimal literal.');
      const [, sign, whole, fraction = ''] = match;
      const coefficient = BigInt(whole + fraction);
      if (coefficient > 79228162514264337593543950335n || (sign && coefficient === 0n))
        throw new Error('Expected an exact decimal literal.');
      return token;
    }
    function readMetadataPath() {
      const path = [];
      do {
        expect('.');
        const token = tokens[index++] ?? '';
        const quoted = token.startsWith('"');
        const key = quoted ? token.slice(1, -1).replace(/""/g, '"') : token;
        if (!key.trim() || key.length > 64 || /[\x00-\x1f\x7f-\x9f]/.test(key) ||
            (!quoted && !/^[A-Za-z_][A-Za-z_0-9]*$/.test(key)))
          throw new Error('Expected a metadata path segment of up to 64 characters.');
        path.push(key);
        if (path.length > 4) throw new Error('Use at most four metadata path segments.');
      } while (peek() === '.');
      return path;
    }
    function readLiteral(scalar, wideInteger = false) {
      const token = tokens[index++];
      if (scalar === 'Int32') return integer(token, -2147483648, 2147483647);
      if (scalar === 'Integer') {
          if (!wideInteger) return integer(token, -2147483648, 2147483647);
          if (!/^(?:0|-?[1-9][0-9]{0,18})$/.test(token ?? '') ||
              BigInt(token) < -9223372036854775808n || BigInt(token) > 9223372036854775807n)
            throw new Error('Expected a signed 64-bit integer.');
          return token;
        }
      if (scalar === 'DateTime') {
        // Date stores milliseconds; keep fractional ticks outside its conversion.
        const literal = token?.toUpperCase() === 'TIMESTAMP' ? tokens[index++] : token;
        const match = /^'(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(\.\d{1,7})?(Z|([+-])(\d{2}):(\d{2}))'$/.exec(literal ?? '');
        const localSeconds = match && new Date(match[1] + 'Z');
        if (!localSeconds || match[1].startsWith('0000') || !Number.isFinite(localSeconds.getTime())
            || localSeconds.toISOString().slice(0, 19) !== match[1])
          throw new Error('Expected a quoted UTC timestamp with Z, or an explicit timezone offset, and up to seven fractional digits.');
        const hours = Number(match[5] ?? 0), minutes = Number(match[6] ?? 0);
        if (hours > 14 || minutes > 59 || hours === 14 && minutes !== 0)
          throw new Error('Timestamp timezone offsets must be within minus or plus 14 hours.');
        const offsetMinutes = (hours * 60 + minutes) * (match[4] === '-' ? -1 : 1);
        const utc = new Date(localSeconds.getTime() - offsetMinutes * 60000);
        if (utc.getUTCFullYear() < 1 || utc.getUTCFullYear() > 9999)
          throw new Error('The UTC timestamp must remain within years 0001 through 9999.');
        return utc.toISOString().slice(0, 19) + (match[2] ?? '') + 'Z';
      }
      if (scalar === 'Decimal') {
        return exactDecimal(token);
      }
      if (scalar === 'Double' || scalar === 'Single') {
        const match = /^(-?)(\d+)(?:\.(\d+))?(?:[eE]([+-]?\d+))?$/.exec(token ?? '');
        if (!match) throw new Error('Expected a finite floating-point literal.');
        const [, sign, whole, fraction = '', scale = '0'] = match;
        const digits = whole + fraction;
        const firstNonzero = digits.search(/[1-9]/);
        if (firstNonzero < 0) return '0';
        const magnitude = Number(token);
        if (!Number.isFinite(magnitude) || magnitude === 0)
          throw new Error('Expected a finite floating-point literal.');

        // Reformat the input digits without rounding. WEB4 validates that the result
        // is the shortest exact round-trip spelling for the declared width.
        const significant = digits.slice(firstNonzero).replace(/0+$/, '');
        const exponent = Number(scale) + whole.length - firstNonzero - 1;
        const exponentThreshold = scalar === 'Double' ? 17 : 9;
        let canonical;
        if (exponent < -4 || exponent >= exponentThreshold) {
          const mantissa = significant.length === 1 ? significant : significant[0] + '.' + significant.slice(1);
          canonical = mantissa + 'E' + (exponent < 0 ? '-' : '+') + String(Math.abs(exponent)).padStart(2, '0');
        } else if (exponent < 0) {
          canonical = '0.' + '0'.repeat(-exponent - 1) + significant;
        } else {
          const point = exponent + 1;
          canonical = significant.length > point
            ? significant.slice(0, point) + '.' + significant.slice(point)
            : significant.padEnd(point, '0');
        }
        if (sign.length + canonical.length > 24)
          throw new Error('Floating-point literals may use up to 24 canonical characters.');
        return sign + canonical;
      }
      if (scalar === 'UInt64') {
        if (!/^[0-9]{1,20}$/.test(token ?? '') || BigInt(token) > 18446744073709551615n)
          throw new Error('Expected an unsigned 64-bit integer.');
        return token;
      }
      if (scalar === 'Boolean' && /^(true|false)$/i.test(token)) return token.toLowerCase() === 'true';
      if (scalar === 'Guid') {
        const value = token?.slice(1, -1);
        if (!token?.startsWith("'") || !/^[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12}$/i.test(value))
          throw new Error('Expected a quoted GUID for the selected field.');
        return value;
      }
      if (scalar === 'HolonType') {
        if (!/^'[A-Za-z][A-Za-z0-9_]{0,39}'$/.test(token ?? ''))
          throw new Error('Expected a quoted HolonType member.');
        return token.slice(1, -1);
      }
      if (scalar === 'String' && token?.startsWith("'")) {
        const value = token.slice(1, -1).replace(/''/g, "'");
        if (value.length > 512) throw new Error('String literals may contain up to 512 characters.');
        return value;
      }
      throw new Error('Expected a ' + scalar + ' literal.');
    }

    function readLike(scalar, allowMatchAll = false) {
      if (scalar !== 'String') throw new Error('LIKE requires a string field.');
      const pattern = readLiteral(scalar);
      const escape = accept('ESCAPE') ? readLiteral(scalar) : null;
      if (escape !== null && ([...escape].length !== 1 || '%_'.includes(escape)))
        throw new Error('LIKE ESCAPE requires one character other than % or _.');
      const units = [];
      const characters = [...pattern];
      for (let offset = 0; offset < characters.length; offset++) {
        const character = characters[offset];
        if (character === escape) {
          const escaped = characters[++offset];
          if (escaped !== escape && escaped !== '%' && escaped !== '_')
            throw new Error('LIKE ESCAPE must precede %, _ or the escape character.');
          units.push(escaped);
        } else if (character === '%') {
          if (units[units.length - 1] !== null) units.push(null);
        } else if (character === '_') {
          throw new Error('LIKE requires escaping a literal underscore.');
        } else {
          units.push(character);
        }
      }
      if (allowMatchAll && units.length === 1 && units[0] === null) return { operator: 'IsNotNull' };
      const leading = units[0] === null;
      const trailing = units[units.length - 1] === null;
      const inner = units.slice(leading ? 1 : 0, trailing ? -1 : undefined);
      if (!inner.length || inner.includes(null))
        throw new Error('LIKE supports nonempty text with optional % at either end.');
      const value = inner.join('');
      let operator = 'Equal';
      if (leading && trailing) operator = 'Contains';
      else if (leading) operator = 'EndsWith';
      else if (trailing) operator = 'StartsWith';
      return { operator, value };
    }

    function filter(allowRange = false) {
      const length = accept('LENGTH');
      if (length) expect('(');
      const cast = accept('CAST');
      if (cast) expect('(');
      const name = tokens[index++]?.toLowerCase();
      let selector, scalar;
      if (name === 'metadata') {
        selector = { metadataPath: readMetadataPath() };
        if (accept('IS')) {
          if (length || cast) throw new Error('Close the typed expression before its comparison.');
          if (accept('NULL')) return { ...selector, operator: 'IsNull' };
          if (accept('MISSING')) return { ...selector, operator: 'IsMissing' };
          if (accept('NOT')) {
            expect('NULL');
            return { ...selector, operator: 'IsNotNull' };
          }
          expect('PRESENT');
          return { ...selector, operator: 'Exists' };
        }
        scalar = 'String';
        const typed = accept('AS');
        if (cast && !typed) throw new Error('CAST metadata requires an explicit type.');
        if (typed) {
          if (cast && accept('STRING')) scalar = 'String';
          else if (accept('INT32')) scalar = 'Int32';
            else if (accept('INT64')) scalar = 'Integer';
          else if (accept('BOOLEAN')) scalar = 'Boolean';
            else if (accept('GUID')) scalar = 'Guid';
          else if (accept('DECIMAL')) scalar = 'Decimal';
          else if (accept('DOUBLE')) scalar = 'Double';
          else if (accept('SINGLE')) scalar = 'Single';
          else if (accept('DATETIME')) scalar = 'DateTime';
          else {
            expect('UINT64');
            scalar = 'UInt64';
          }
        }
        if (cast) expect(')');
      } else {
        if (cast) throw new Error('Typed CAST filters require a metadata field.');
        if (!Object.hasOwn(fields, name)) throw new Error('Unknown filter field.');
        const [field, type] = fields[name];
        selector = { field };
        scalar = type;
        if (accept('IS')) {
          if (length) throw new Error('Close LENGTH before its numeric comparison.');
          const negated = accept('NOT');
          if (scalar === 'Boolean') {
            let value;
            if (accept('TRUE')) value = true;
            else if (accept('FALSE')) value = false;
            else throw new Error('Canonical Boolean IS tests require TRUE or FALSE.');
            return { ...selector, scalar, operator: 'Equal', value: negated ? !value : value };
          }
          if (scalar !== 'String') throw new Error('Canonical null tests require a text field.');
          expect('NULL');
          return { ...selector, operator: negated ? 'IsNotNull' : 'IsNull' };
        }
      }
      if (length) {
        if (scalar !== 'String') throw new Error('LENGTH requires a string field.');
        expect(')');
        const operator = comparisons[tokens[index++]?.toUpperCase()];
        if (!['Equal', 'NotEqual', 'GreaterThan', 'GreaterThanOrEqual', 'LessThan', 'LessThanOrEqual'].includes(operator))
          throw new Error('Use an integer comparison after LENGTH.');
        return { ...selector, scalar: 'String', function: 'Utf16Length', operator,
          value: readLiteral('Integer', true) };
      }
      const literal = () => readLiteral(scalar, Boolean(selector.metadataPath));
      const negated = accept('NOT');
      if (accept('IN')) {
        expect('(');
        const values = [literal()];
        while (accept(',')) values.push(literal());
        expect(')');
        if (values.length > 100) throw new Error('IN accepts up to 100 values.');
        if (!negated) return { ...selector, scalar, operator: 'In', value: values };
        if (!allowRange) throw new Error('NOT IN needs an AND clause outside OR alternatives.');
        if (!selector.metadataPath && !['Id', 'Version', 'Name', 'Description'].includes(selector.field))
          throw new Error('NOT IN requires Id, Version, Name, Description, or a metadata path.');
        const predicates = [...new Set(values)].map(value => ({ ...selector, scalar, operator: 'NotEqual', value }));
        if (selector.metadataPath || scalar === 'String') predicates.unshift({ ...selector, operator: 'IsNotNull' });
        if (predicates.length > 12) throw new Error('NOT IN exceeds the twelve-filter budget.');
        return predicates;
      }
      if (accept('LIKE')) {
        const match = readLike(scalar, negated || Boolean(selector.field));
        if (!negated) return match.operator === 'IsNotNull' ? { ...selector, operator: match.operator }
          : { ...selector, scalar, ...match };
        if (match.operator === 'IsNotNull') return { ...selector, scalar, operator: 'NotContains', value: '' };
        if (match.operator !== 'Equal') return { ...selector, scalar, operator: 'Not' + match.operator, value: match.value };
        if (!allowRange) throw new Error('Exact NOT LIKE needs an AND clause outside OR alternatives.');
        return [{ ...selector, operator: 'IsNotNull' }, { ...selector, scalar, operator: 'NotEqual', value: match.value }];
      }
      if (negated) expect('BETWEEN');
      if (negated || accept('BETWEEN')) {
        if (!allowRange && !negated) throw new Error('BETWEEN needs an AND clause outside OR alternatives.');
        if (!['Int32', 'Integer', 'Decimal', 'UInt64', 'Double', 'Single', 'DateTime'].includes(scalar))
          throw new Error('BETWEEN requires a numeric or UTC timestamp field.');
        const lower = literal();
        expect('AND');
        const upper = literal();
        if (negated) return { anyOf: [
          { ...selector, scalar, operator: 'LessThan', value: lower },
          { ...selector, scalar, operator: 'GreaterThan', value: upper }
        ] };
        return [
          { ...selector, scalar, operator: 'GreaterThanOrEqual', value: lower },
          { ...selector, scalar, operator: 'LessThanOrEqual', value: upper }
        ];
      }
      const comparison = tokens[index++]?.toUpperCase();
      const operator = Object.hasOwn(comparisons, comparison) ? comparisons[comparison] : null;
      if (!operator) throw new Error('Expected a supported comparison.');
      if (!['Int32', 'Integer', 'UInt64', 'Decimal', 'Double', 'Single', 'DateTime'].includes(scalar) && ['<', '<=', '>', '>='].includes(comparison))
        throw new Error('Ordered comparisons require Version or metadata typed AS INT32, AS INT64, AS UINT64, AS DECIMAL, AS DOUBLE, AS SINGLE, or AS DATETIME.');
      if (['Contains', 'StartsWith', 'EndsWith', 'NotContains', 'NotStartsWith', 'NotEndsWith'].includes(operator) && scalar !== 'String')
        throw new Error('Text matching requires a string field.');
      return { ...selector, scalar, operator, value: literal() };
    }

    function readJoin(leadingTake) {
      const identifier = label => {
        const value = tokens[index++];
        if (!/^[A-Za-z][A-Za-z0-9_]{0,63}$/.test(value ?? ''))
          throw new Error('Expected a ' + label + ' alias.');
        return value;
      };
      const qualifiedField = () => {
        const cast = accept('CAST');
        if (cast) expect('(');
        const source = identifier('source').toLowerCase();
        expect('.');
        if (cast) return { source, selection: readMetadataCast() };
        const name = tokens[index++]?.toLowerCase();
        if (!Object.hasOwn(fields, name)) throw new Error('Unknown qualified join field.');
        return { source, selection: { field: fields[name][0] } };
      };
      const source = () => {
        expect('(');
        expect('SELECT');
        const sourceTop = readTop();
        expect('*');
        expect('FROM');
        const provider = ['MongoDBOASIS', 'SQLLiteDBOASIS', 'Neo4jOASIS'].find(name => name.toUpperCase() === peek());
        if (!provider) throw new Error('Join sources require MongoDBOASIS, SQLLiteDBOASIS, or Neo4jOASIS.');
        index++;
        const body = { filters: [], orderBy: 'Id' };
        if (accept('WHERE')) predicates(body, false);
        if (accept('ORDER')) {
          expect('BY');
          const field = fields[tokens[index++]?.toLowerCase()]?.[0];
          if (!['Id', 'Name', 'Version'].includes(field)) throw new Error('Order join inputs by Id, Name, or Version.');
          body.orderBy = field;
          if (accept('DESC')) body.descending = true;
          else accept('ASC');
        }
        const window = readWindow(10, 1000, 1, sourceTop);
        if (window.take === undefined) throw new Error('Join inputs require a LIMIT or FETCH row bound.');
        body.take = window.take;
        if (window.skip !== undefined) body.skip = window.skip;
        expect(')');
        accept('AS');
        return { alias: identifier('source').toLowerCase(), provider, body };
      };

      const fieldsToSelect = [];
      do {
        const field = qualifiedField();
        const alias = readColumnAlias(field.selection.field);
        if (!alias) throw new Error('Use AS to name a joined metadata column.');
        fieldsToSelect.push({ ...field, alias });
      } while (accept(','));
      if (fieldsToSelect.length > 32 || new Set(fieldsToSelect.map(field => field.alias.toLowerCase())).size !== fieldsToSelect.length)
        throw new Error('Use up to 32 distinct joined column aliases.');
      expect('FROM');
      const firstSource = source();
      const leftJoin = accept('LEFT');
      const rightJoin = !leftJoin && accept('RIGHT');
      const fullJoin = !leftJoin && !rightJoin && accept('FULL');
      const outerJoin = leftJoin || rightJoin || fullJoin;
      const crossJoin = !outerJoin && accept('CROSS');
      const childJoin = !outerJoin && !crossJoin && accept('CHILD');
      if (outerJoin) accept('OUTER');
      else if (!crossJoin && !childJoin) accept('INNER');
      expect('JOIN');
      const secondSource = source();
      const [left, right] = rightJoin ? [secondSource, firstSource] : [firstSource, secondSource];
      if (left.alias === right.alias || left.provider !== right.provider)
        throw new Error('Use two different aliases for one join provider.');
      const sourceSide = alias => {
        if (alias === left.alias) return 'Left';
        if (alias === right.alias) return 'Right';
        throw new Error('Unknown join source alias.');
      };
      function joinKey() {
        const first = qualifiedField();
        let nullComparison;
        if (accept('IS')) {
          const negate = accept('NOT');
          expect('DISTINCT');
          expect('FROM');
          nullComparison = negate ? 'IsNotDistinctFrom' : 'IsDistinctFrom';
        }
        const symbol = nullComparison ? undefined : tokens[index++];
        const reversed = { '=': '=', '<>': '<>', '!=': '!=', '<': '>', '<=': '>=', '>': '<', '>=': '<=' };
        if (!nullComparison && !Object.hasOwn(reversed, symbol)) throw new Error('Use an exact join comparison operator.');
        const second = qualifiedField();
        const one = sourceSide(first.source);
        if (one === sourceSide(second.source)) throw new Error('Join keys must compare both sources.');
        const [l, r] = one === 'Left' ? [first, second] : [second, first];
        const ordered = symbol !== '=';
        const operator = nullComparison ?? (ordered ? comparisons[one === 'Left' ? symbol : reversed[symbol]] : undefined);
        if (ordered) {
          const keyDomain = selection => selection.field === 'Version' ? 'Int32'
            : selection.field ? fields[selection.field.toLowerCase()][1] : selection.valueType;
          const domain = keyDomain(l.selection);
          if (nullComparison) {
            if (domain !== keyDomain(r.selection) || (domain !== 'String' &&
                (!['Boolean', 'Guid', 'DateTime', 'Int32', 'Int64', 'UInt64', 'Decimal'].includes(domain) ||
                 !l.selection.nullable || !r.selection.nullable)))
              throw new Error('Use IS DISTINCT FROM with matching nullable Boolean, GUID, UTC timestamp or supported exact numeric columns, or matching text columns.');
          } else if (!['Int32', 'Int64'].includes(domain) || domain !== keyDomain(r.selection)) {
            throw new Error('Join comparisons require matching signed integer columns.');
          }
        }
        return { left: l.selection, right: r.selection, ...(ordered ? { operator } : {}) };
      }

      const keyCount = groups => groups.reduce((count, group) => count + group.length, 0);
      const tooManyKeys = () => new Error('Use up to eight join keys after expanding the ON conditions.');
      function alternatives(depth) {
        const groups = conjunction(depth);
        while (accept('OR')) {
          const next = conjunction(depth);
          if (keyCount(groups) + keyCount(next) > 8) throw tooManyKeys();
          groups.push(...next);
        }
        return groups;
      }
      function conjunction(depth) {
        let groups = term(depth);
        while (accept('AND')) {
          const next = term(depth);
          if (keyCount(groups) * next.length + keyCount(next) * groups.length > 8) throw tooManyKeys();
          groups = groups.flatMap(group => next.map(branch => {
            if (branch.some(key => group.some(previous =>
                selectionKey(previous.left) === selectionKey(key.left) &&
                selectionKey(previous.right) === selectionKey(key.right) && previous.operator === key.operator)))
              throw new Error('Use each join key pair once in a conjunction.');
            return group.concat(branch);
          }));
        }
        return groups;
      }
      function term(depth) {
        if (!accept('(')) return [[joinKey()]];
        if (depth >= 8) throw new Error('JOIN ON conditions are nested too deeply.');
        const groups = alternatives(depth + 1);
        expect(')');
        return groups;
      }

      const keys = [], keyGroupStarts = [];
      if (!crossJoin && !childJoin) {
        expect('ON');
        for (const group of alternatives(0)) {
          keyGroupStarts.push(keys.length);
          for (const key of group) keys.push({ alias: 'Key' + (keys.length + 1), ...key });
        }
      }
      let outputOrder;
      if (accept('ORDER')) {
        expect('BY');
        const terms = [];
        do {
          let selected;
          if (tokens[index + 1] === '.' || (peek() === 'CAST' && tokens[index + 1] === '(')) {
            const field = qualifiedField();
            const expression = selectionKey(field.selection);
            selected = fieldsToSelect.find(column => column.source === field.source &&
              selectionKey(column.selection) === expression);
          } else {
            const name = readOrderColumn(fieldsToSelect)?.toLowerCase();
            selected = fieldsToSelect.find(field => field.alias.toLowerCase() === name);
          }
          if (!selected) throw new Error('Order joined results by a selected column alias or qualified expression.');
          const type = selected.selection.valueType ?? fields[selected.selection.field.toLowerCase()][1];
          if (!['Guid', 'String', 'Integer', 'Int32', 'Int64', 'UInt64', 'DateTime', 'Boolean'].includes(type))
            throw new Error('Joined ordering supports GUID, STRING, INT32, INT64, UINT64, DATETIME, and BOOLEAN outputs.');
          const descending = accept('DESC');
          if (!descending) accept('ASC');
          terms.push({ alias: selected.alias, descending });
          if (terms.length > 8) throw new Error('Use up to eight joined ordering terms.');
        } while (accept(','));
        outputOrder = terms.length === 1
          ? { outputOrderBy: terms[0].alias, outputDescending: terms[0].descending }
          : { outputOrdering: terms };
      }
      const { take, skip } = readWindow(100, 1000, 0, leadingTake);
      if (take === undefined && skip !== undefined)
        throw new Error('A joined output offset requires LIMIT or FETCH.');
      accept(';');
      if (index !== tokens.length) throw new Error('Unexpected join clause.');
      const filterCount = left.body.filters.length + right.body.filters.length +
        (left.body.anyOf?.length ?? 0) + (right.body.anyOf?.length ?? 0) +
        (left.body.where ? whereStats(left.body.where).filters : 0) +
        (right.body.where ? whereStats(right.body.where).filters : 0);
      if (keys.length > 8 || filterCount > 12)
        throw new Error('Use up to eight join keys and twelve input filters.');
      const columns = fieldsToSelect.map(field => {
        const side = sourceSide(field.source);
        const column = { alias: field.alias, source: side, selection: field.selection };
        if (fullJoin || (outerJoin && side === 'Right')) column.allowMissing = true;
        return column;
      });
      const product = left.body.take * right.body.take;
      const maximumPairs = childJoin ? right.body.take :
        fullJoin ? Math.max(product, left.body.take + right.body.take) : product;
      const outputLimit = take === undefined ? undefined : Math.min(take, maximumPairs);
      const window = outputLimit === undefined ? {} : { outputLimit, outputOffset: skip ?? 0 };
      return { path: 'data/query-cql/join', body: {
        provider: left.provider, kind: childJoin ? 'Child' : crossJoin ? 'Cross' : fullJoin ? 'Full' : outerJoin ? 'Left' : 'Inner',
        left: left.body, right: right.body, keys, columns, ...outputOrder, ...window,
        ...(keyGroupStarts.length <= 1 ? {} : keyGroupStarts.length === keys.length
          ? { matchAnyKey: true } : { keyGroupStarts }),
        maximumLeftRows: left.body.take, maximumRightRows: right.body.take, maximumPairs,
        maximumResults: outputLimit === undefined ? maximumPairs : Math.max(1, outputLimit)
      } };
    }

    expect('SELECT');
    const distinct = accept('DISTINCT');
    const selectTop = readTop();
    if (tokens.some((token, position) => token.toUpperCase() === 'FROM' && tokens[position + 1] === '(')) {
      if (distinct) throw new Error('DISTINCT is not admitted for joined output.');
      return readJoin(selectTop);
    }
    const aggregates = { COUNT: 'LongCount', ANY: 'Any', MIN: 'Min', MAX: 'Max', SUM: 'Sum', AVG: 'Average' };
    const hasGrouping = tokens.some((token, position) =>
      token.toUpperCase() === 'GROUP' && tokens[position + 1]?.toUpperCase() === 'BY');
    const operation = !hasGrouping && Object.hasOwn(aggregates, peek())
      ? aggregates[tokens[index++].toUpperCase()] : null;
    if (selectTop !== undefined && operation)
      throw new Error('TOP applies to row or grouped results; scalar queries already return one value.');
    if (operation) expect('(');
    const countDistinct = operation === 'LongCount' && accept('DISTINCT');
    const countValues = operation === 'LongCount' && !countDistinct && peek() !== '*' && peek() !== '1';
    const selectedCount = countDistinct || countValues;
    const hasSelection = selectedCount || operation && operation !== 'LongCount' && operation !== 'Any';
    const scalarSelection = selectedCount ? readSelection(true) : hasSelection ? readAggregateSelection(true, operation === 'Min' || operation === 'Max') : null;
    if (utcDatePartNames.has(scalarSelection?.function) || ['TextCoalesce','BooleanTextCase','Int32Coalesce'].includes(scalarSelection?.function)) throw new Error('Computed scalar expressions currently support selected row columns, not scalar aggregates.');
    if (operation && !hasSelection && !(operation === 'LongCount' && accept('1'))) expect('*');
    function readSelection(allowComputed = false) {
      if (allowComputed && Object.hasOwn(utcDateParts,peek())) {
        const functionName=utcDateParts[peek()];index++;
        expect('(');const input=readSelection();expect(')');
        if(input.field || input.valueType!=='DateTime')
          throw new Error('UTC date parts require CAST of metadata AS DATETIME.');
        return {...input,function:functionName,nullable:true};
      }
      if (allowComputed && accept('CASE')) {
        expect('WHEN');
        const input = readSelection();
        if (input.field ? !['IsActive','IsPublic'].includes(input.field) : input.valueType !== 'Boolean')
          throw new Error('CASE requires a Boolean field or exact CAST of metadata AS BOOLEAN.');
        function branch() {
          const token = tokens[index++];
          if (!/^'(?:[^']|'')*'$/.test(token ?? ''))
            throw new Error('CASE requires quoted text branches or string parameters.');
          const value = token.slice(1,-1).replace(/''/g, "'");
          if (value.length > 512) throw new Error('CASE branch exceeds512 characters.');
          return value;
        }
        expect('THEN'); const whenTrue = branch();
        expect('ELSE'); const whenFalse = branch();
        expect('END');
        return {...input,...(input.metadataPath ? {nullable:false} : {}),function:'BooleanTextCase',whenTrue,whenFalse};
      }
      if (allowComputed && accept('COALESCE')) {
        expect('(');
        const input = readSelection();
        expect(',');
        if (!input.field && input.valueType === 'Int32') {
          const int32Replacement = integer(tokens[index++], -2147483648, 2147483647);
          expect(')');
          return {...input,function:'Int32Coalesce',int32Replacement};
        }
        const token = tokens[index++];
        if (!/^'(?:[^']|'')*'$/.test(token ?? ''))
          throw new Error('COALESCE requires a non-null quoted text replacement or string parameter.');
        const nullReplacement = token.slice(1,-1).replace(/''/g, "'");
        if (nullReplacement.length > 512) throw new Error('COALESCE replacement exceeds512 characters.');
        expect(')');
        if (input.field ? !['Name','Description'].includes(input.field) : input.valueType !== 'String')
          throw new Error('COALESCE requires a string field or CAST of metadata AS STRING.');
        return {...input,function:'TextCoalesce',nullReplacement};
      }
      if (allowComputed && accept('LENGTH')) {
        expect('(');
        const input = readSelection();
        expect(')');
        if (input.field ? !['Name', 'Description'].includes(input.field) : input.valueType !== 'String')
          throw new Error('Selected LENGTH requires a string field or CAST of metadata AS STRING.');
        return { ...input, function: 'Utf16Length', nullable: true };
      }
      if (!accept('CAST')) {
        const name = tokens[index++]?.toLowerCase();
        if (!Object.hasOwn(projectionFields, name)) throw new Error('Unknown selected field.');
        return { field: projectionFields[name][0] };
      }
      expect('(');
      return readMetadataCast();
    }

    function readMetadataCast() {
      expect('METADATA');
      const metadataPath = readMetadataPath();
      expect('AS');
      const types = { STRING: 'String', BOOLEAN: 'Boolean', INT32: 'Int32', INT64: 'Int64',
        UINT64: 'UInt64', DECIMAL: 'Decimal', DOUBLE: 'Double', SINGLE: 'Single', GUID: 'Guid', DATETIME: 'DateTime' };
      const type = tokens[index++]?.toUpperCase();
      if (!Object.hasOwn(types, type)) throw new Error('Choose an exact metadata projection type.');
      expect(')');
      return { metadataPath, valueType: types[type], nullable: true };
    }

    function readAggregateSelection(allowComputed = false, allowText = false) {
      const selection = readSelection(allowComputed);
      if (!selection.function && selection.field !== 'Version' && !['Int32', 'Int64', 'UInt64', 'Decimal', 'Single', 'Double'].includes(selection.valueType) && !(allowText && selection.valueType === 'String'))
        throw new Error('Aggregate Version or exact numeric metadata using CAST.');
      return selection;
    }
    function selectionKey(column) {
      const input = column.field ?? JSON.stringify([column.valueType, ...column.metadataPath]);
      return column.function === 'BooleanTextCase' ? JSON.stringify([column.function,input,column.whenTrue,column.whenFalse])
        : column.function === 'Int32Coalesce' ? JSON.stringify([column.function,input,column.int32Replacement])
        : column.function === 'TextCoalesce' ? JSON.stringify([column.function,input,column.nullReplacement])
        : column.function ? JSON.stringify([column.function, input]) : input;
    }
    function readGroupedAggregate(name) {
      const operation = aggregates[name];
      expect('(');
      const distinct = operation === 'LongCount' && accept('DISTINCT');
      const selection = operation === 'LongCount'
        ? (!distinct && (accept('*') || accept('1')) ? null : readSelection())
        : readAggregateSelection();
      expect(')');
      if (distinct) {
        const type = selection.field === 'Version' ? 'Int32'
          : selection.field ? projectionFields[selection.field.toLowerCase()][1] : selection.valueType;
        if (!['String', 'Boolean', 'Guid', 'Int32', 'Int64', 'Single', 'Double'].includes(type))
          throw new Error('Grouped COUNT DISTINCT requires an admitted direct text, GUID, boolean or signed integer/floating value.');
      }
      return selection ? { operation, selection, ...(distinct ? { distinct: true } : {}) } : { operation };
    }
    const columns = [];
    const groupedAggregates = [];
    if (!operation && !accept('*')) {
      do {
        if (peek() !== 'ANY' && Object.hasOwn(aggregates, peek())) {
          const name = tokens[index++].toUpperCase();
          const binding = readGroupedAggregate(name);
          const alias = readColumnAlias(name[0] + name.slice(1).toLowerCase());
          if (!/^[A-Za-z][A-Za-z0-9_]{0,63}$/.test(alias ?? ''))
            throw new Error('Expected a column alias for the grouped aggregate.');
          binding.alias = alias;
          groupedAggregates.push({ index: columns.length + groupedAggregates.length, binding });
          continue;
        }
        const selection = readSelection(true);
        const requiresAlias = selection.metadataPath || selection.function;
        const alias = readColumnAlias(requiresAlias ? undefined : selection.field);
        if (requiresAlias && alias === undefined) throw new Error('Expected AS or a column alias.');
        if (!/^[A-Za-z][A-Za-z0-9_]{0,63}$/.test(alias ?? ''))
          throw new Error('Expected a column alias of up to 64 letters, digits or underscores, starting with a letter.');
        if (columns.some(column => column.alias.toLowerCase() === alias.toLowerCase()))
          throw new Error('Use a distinct alias for each selected column.');
        columns.push({ ...selection, alias });
      } while (accept(','));
    }
    let scalarAlias;
    if (operation) {
      expect(')');
      const resultAlias = readColumnAlias(undefined, 'scalar result alias');
      if (resultAlias !== undefined) {
        scalarAlias = resultAlias;
        if (!/^[A-Za-z][A-Za-z0-9_]{0,63}$/.test(scalarAlias ?? ''))
          throw new Error('Expected a scalar result alias of up to 64 letters, digits or underscores.');
      }
    }
    if (distinct && (!columns.length || groupedAggregates.length || new Set(columns.map(selectionKey)).size !== columns.length))
      throw new Error('DISTINCT requires one or more different selected fields without aggregates.');
    expect('FROM');
    const selected = [];
    do {
      const provider = selectableProviders.find(value => value.toUpperCase() === peek());
      if (!provider || selected.includes(provider)) throw new Error('Choose distinct available providers.');
      selected.push(provider);
      index++;
    } while (accept(','));
    if ((columns.length || groupedAggregates.length) && (selected.length !== 1 || !['MongoDBOASIS', 'SQLLiteDBOASIS', 'Neo4jOASIS'].includes(selected[0])))
      throw new Error('Selected columns require one of MongoDBOASIS, SQLLiteDBOASIS, or Neo4jOASIS.');
    if (operation && selected.length > 1) throw new Error('Scalar queries require a single provider.');
    const [provider] = selected;
    const holo = selected.includes('HoloOASIS');
    const accountProviders = selected.filter(provider => Object.hasOwn(keyProfiles, provider));
    const nativeKeys = new Set(accountProviders.map(name => keyProfiles[name].key));
    if (holo) nativeKeys.add('Id');
    if (nativeKeys.size > 1)
      throw new Error('These providers require different key fields. Run separate queries for each key field.');

    let mixedScalar = false;
    const body = { provider, filters: [], orderBy: holo || accountProviders.length ? 'None' : 'Name', skip: 0 };
    if (!operation) Object.assign(body, { take: 10, includeMetadata: true });
    function mappedValue(column, allowDateTime = false, allowComputedRows = false) {
      return column && !column.field && column.metadataPath?.length === 1 &&
        (['String', 'Boolean', 'Guid', 'Int32', 'Int64', 'Double'].includes(column.valueType) ||
          allowDateTime && column.valueType === 'DateTime') &&
        (!column.function || column.function === 'Utf16Length' && column.valueType === 'String' ||
            allowComputedRows && (column.function === 'TextCoalesce' && column.valueType === 'String' ||
              column.function === 'BooleanTextCase' && column.valueType === 'Boolean' ||
              column.function === 'Int32Coalesce' && column.valueType === 'Int32' ||
              utcDatePartNames.has(column.function) && column.valueType === 'DateTime'));
    }
    if (accept('WITH')) {
      expect('VIRTUAL');
      expect('MAPPING');
      const id = readLiteral('Guid');
      if (/^0{8}(?:-0{4}){3}-0{12}$/.test(id))
        throw new Error('Use a nonempty Virtual Holon mapping ID.');
      if (operation) {
        if (!['LongCount','Min','Max','Sum','Average'].includes(operation) ||
            !['MongoDBOASIS', 'SQLLiteDBOASIS', 'Neo4jOASIS'].includes(provider))
          throw new Error('Choose a supported mixed count or numeric aggregate.');
        if (operation !== 'LongCount' && (!scalarSelection || scalarSelection.metadataPath?.length !== 1 ||
            (scalarSelection.function
              ? scalarSelection.function !== 'Utf16Length' || scalarSelection.valueType !== 'String'
              : !['Int32','Int64','Double'].includes(scalarSelection.valueType) && !(['Min','Max'].includes(operation) && scalarSelection.valueType === 'String'))))
          throw new Error('Mixed numeric aggregates require direct INT32, INT64, DOUBLE metadata or LENGTH of STRING metadata.');
        mixedScalar = true;
        body.resultOperation = countDistinct ? 'CountDistinct' : countValues ? 'CountValues' : operation; body.orderBy = 'Id'; body.take = 1;
        columns.push({field: 'Id', alias: 'Id'});
        if (operation !== 'LongCount' || selectedCount) columns.push({...scalarSelection,alias:'Value'});
      }
      if (operation && !mixedScalar)
        throw new Error('This scalar cannot use a Virtual Holon mapping.');
      body.virtualMappingId = id;
    }
    if (!body.virtualMappingId && scalarSelection?.valueType === 'String' && ['Min','Max'].includes(operation))
      throw new Error('Text MIN/MAX currently requires a selected Virtual Holon mapping.');


    function whereStats(node, depth = 1) {
      if (depth > 8) throw new Error('Use at most eight WHERE levels.');
      let filters = node.kind === 'Filter' ? 1 : 0;
      let relationships = ['Child', 'Descendant'].includes(node.kind) ? 1 : 0;
      let nodes = 1;
      for (const term of [...(node.terms ?? []), ...(node.match ? [node.match] : [])]) {
        const child = whereStats(term, depth + 1);
        filters += child.filters; relationships += child.relationships; nodes += child.nodes;
      }
      if (filters > 12 || relationships > 4 || nodes > 31)
        throw new Error('WHERE supports twelve filters, four relationships and 31 nodes.');
      return { filters, relationships, nodes };
    }
    function combineWhere(kind, terms) {
      const flat = terms.flatMap(term => term.kind === kind ? term.terms : [term]);
      return flat.length === 1 ? flat[0] : { kind, terms: flat };
    }
    function flatWhere(node) {
      const target = { filters: [] };
      function add(current) {
        if (current.kind === 'Filter') { target.filters.push(current.filter); return true; }
        if (current.kind === 'And') return current.terms.every(add);
        if (current.kind === 'Or' && !target.anyOf && current.terms.every(term => term.kind === 'Filter')) {
          target.anyOf = current.terms.map(term => term.filter); return true;
        }
        const negated = current.kind === 'Not';
        const related = negated ? current.terms[0] : current;
        if (!['Child', 'Descendant'].includes(related.kind)) return false;
        const match = related.match ? flatWhere(related.match) : { filters: [] };
        if (!match || match.anyChildren || match.anyDescendants) return false;
        if (negated) match.negated = true;
        if (related.kind === 'Descendant') {
          Object.assign(match, { maximumDepth: 8, maximumNodes: 1000 });
          (target.anyDescendants ??= []).push(match);
        } else (target.anyChildren ??= []).push(match);
        return true;
      }
      return add(node) ? target : null;
    }
    function predicates(target, allowChildren) {
      function term(depth) {
        if (depth > 16) throw new Error('WHERE parentheses are nested too deeply.');
        if (accept('NOT')) return { kind: 'Not', terms: [term(depth + 1)] };
        if (accept('(')) {
          const node = alternatives(depth + 1); expect(')'); return node;
        }
        if (allowChildren && accept('EXISTS')) {
          const descendant = accept('DESCENDANT');
          if (!descendant) expect('CHILD');
          let match;
          if (accept('(')) { match = alternatives(depth + 1); expect(')'); }
          return { kind: descendant ? 'Descendant' : 'Child', ...(match ? { match } : {}) };
        }
        const value = filter(true);
        const leaf = filter => ({ kind: 'Filter', filter });
        return Array.isArray(value) ? combineWhere('And', value.map(leaf))
          : value.anyOf ? combineWhere('Or', value.anyOf.map(leaf)) : leaf(value);
      }
      function conjunction(depth) {
        const terms = [term(depth)];
        while (accept('AND')) terms.push(term(depth));
        return combineWhere('And', terms);
      }
      function alternatives(depth) {
        const terms = [conjunction(depth)];
        while (accept('OR')) terms.push(conjunction(depth));
        return combineWhere('Or', terms);
      }
      const tree = alternatives(1);
      whereStats(tree);
      const flat = flatWhere(tree);
      if (flat) Object.assign(target, flat);
      else target.where = tree;
    }
    if (accept('WHERE')) predicates(body, true);
    if (body.where && selected.some(provider =>
        !['MongoDBOASIS','SQLLiteDBOASIS','Neo4jOASIS'].includes(provider)))
      throw new Error('Nested Boolean WHERE requires admitted MongoDB, SQLite or Neo4j sources.');

    if (body.virtualMappingId && (body.where || body.filters.length > 8 ||
        body.filters.some(filter => filter.metadataPath?.length !== 1 || filter.value === null ||
            !['Equal', 'NotEqual', 'GreaterThan', 'GreaterThanOrEqual', 'LessThan', 'LessThanOrEqual',
              'Contains', 'StartsWith', 'EndsWith', 'NotContains', 'NotStartsWith', 'NotEndsWith'].includes(filter.operator) ||
            !['String', 'Boolean', 'Guid', 'DateTime', 'Int32', 'Integer', 'Double'].includes(filter.scalar) ||
            filter.function != null && (filter.function !== 'Utf16Length' || filter.scalar !== 'String' ||
              ['Contains', 'StartsWith', 'EndsWith', 'NotContains', 'NotStartsWith', 'NotEndsWith'].includes(filter.operator)) ||
            filter.function == null && !['Int32', 'Integer', 'Double', 'DateTime'].includes(filter.scalar) &&
              !(filter.scalar === 'String' ? ['Equal', 'NotEqual', 'Contains', 'StartsWith', 'EndsWith', 'NotContains', 'NotStartsWith', 'NotEndsWith'] :
                ['Equal', 'NotEqual']).includes(filter.operator) ||
            ['Contains', 'StartsWith', 'EndsWith', 'NotContains', 'NotStartsWith', 'NotEndsWith'].includes(filter.operator) && filter.scalar !== 'String') ||
        body.anyOf || body.anyChildren || body.anyDescendants))
      throw new Error('Use zero to eight supported typed metadata comparison filters with a bounded Virtual Holon mapping.');
    if (distinct || groupedAggregates.length || peek() === 'GROUP') {
      if (columns.some(column => column.function))
        throw new Error('Computed SELECT grouping and DISTINCT require native grouped-function support.');
      let keys = columns;
      if (!distinct) {
        expect('GROUP');
        expect('BY');
        const selectedFields = columns.slice();
        for (const { index } of groupedAggregates) selectedFields.splice(index, 0, null);
        const groupFields = [];
        do {
          const selected = readSelectPosition(selectedFields, 'GROUP BY');
          if (selected === null) throw new Error('GROUP BY cannot refer to an aggregate position.');
          const alias = selected === undefined && !(peek() === 'CAST' && tokens[index + 1] === '(')
            && !Object.hasOwn(projectionFields, peek()?.toLowerCase())
            ? columns.find(column => column.alias.toUpperCase() === peek())
            : null;
          if (alias) index++;
          groupFields.push(selected ?? alias ?? readSelection());
        } while (accept(','));
        const groupKeys = groupFields.map(selectionKey);
        if (groupFields.length > 8 || new Set(groupKeys).size !== groupFields.length)
          throw new Error('Use up to eight different GROUP BY fields.');
        const selectedKeys = columns.map(selectionKey);
        if ((!columns.length && !groupedAggregates.length) ||
            selectedKeys.some(key => !groupKeys.includes(key)) ||
            new Set(selectedKeys).size !== columns.length)
          throw new Error('Each selected field must match GROUP BY once.');
        const keyAliases = new Set(columns.map(column => column.alias.toLowerCase()));
        keys = groupFields.map((selection, position) => {
          const selected = columns[selectedKeys.indexOf(groupKeys[position])];
          if (selected) return selected;
          let alias = selection.field ?? 'GroupKey' + (position + 1);
          while (keyAliases.has(alias.toLowerCase())) alias += '_';
          keyAliases.add(alias.toLowerCase());
          return { ...selection, alias };
        });
      }
      delete body.includeMetadata;
      delete body.take;
      const resultColumns = columns.map(({ alias }) => ({ alias, keyAlias: alias }));
      for (const { index, binding } of groupedAggregates) resultColumns.splice(index, 0, binding);
      if (new Set(resultColumns.map(column => column.alias.toLowerCase())).size !== resultColumns.length)
        throw new Error('Use a distinct alias for each selected column.');
      if (resultColumns.length > 32) throw new Error('Use at most 32 grouped result columns.');
      const grouped = {
        ...body,
        keys,
        columns: resultColumns,
        maximumGroups: 100
      };
      function readGroupedResultColumn() {
        const name = peek();
        let aggregate, key;
        if (tokens[index + 1] === '(' && name !== 'ANY' && Object.hasOwn(aggregates, name)) {
          index++;
          const expression = readGroupedAggregate(name);
          const input = expression.selection ? selectionKey(expression.selection) : null;
          aggregate = groupedAggregates.find(({ binding }) => binding.operation === expression.operation &&
            Boolean(binding.distinct) === Boolean(expression.distinct) &&
            (binding.selection ? selectionKey(binding.selection) : null) === input)?.binding;
        } else if (name === 'CAST' && tokens[index + 1] === '(') {
          const expression = selectionKey(readSelection());
          key = columns.find(column => selectionKey(column) === expression);
        } else {
          aggregate = groupedAggregates.find(({ binding }) => binding.alias.toUpperCase() === name)?.binding;
          key = columns.find(column => column.alias.toUpperCase() === name) ??
            columns.find(column => column.field?.toUpperCase() === name);
          index++;
        }
        return { aggregate, key };
      }
      if (accept('HAVING')) {
        if (distinct) throw new Error('DISTINCT does not accept a HAVING clause.');
        grouped.resultFilters = [];
        function addHavingFilter(filter) {
          grouped.resultFilters.push(filter);
          if (grouped.resultFilters.length > 32) throw new Error('Use at most 32 HAVING comparisons.');
        }
        function addHavingComparison(alias, type, comparison, nullable = false) {
          if (!['=', '<>', '!=', '<', '<=', '>', '>='].includes(comparison))
            throw new Error('HAVING requires a scalar comparison.');
          if (['String', 'Boolean', 'Guid', 'HolonType'].includes(type) && !['=', '<>', '!='].includes(comparison))
            throw new Error('This grouped key supports equality and inequality in HAVING.');
          let value;
          if (type === 'Count') {
            value = tokens[index++];
            if (!/^(0|[1-9][0-9]{0,18})$/.test(value ?? '') || BigInt(value) > 9223372036854775807n)
              throw new Error('HAVING requires a comparison to a nonnegative 64-bit count.');
          } else if (type === 'UInt64') {
            value = tokens[index++];
            if (!/^(0|[1-9][0-9]{0,19})$/.test(value ?? '') || BigInt(value) > 18446744073709551615n)
              throw new Error('HAVING requires an exact UInt64 threshold.');
          } else {
            const scalar = type === 'Int32' || type === 'Int64' ? 'Integer' : type;
            try { value = readLiteral(scalar, type === 'Int64'); }
            catch (error) {
              if (type === 'Int64') throw new Error('HAVING requires an exact Int64 threshold.');
              throw error;
            }
          }
          if (nullable && ['<>', '!='].includes(comparison))
            addHavingFilter({ alias, operator: 'NotEqual', value: null });
          addHavingFilter({ alias, operator: comparisons[comparison], value });
        }
        do {
          const { aggregate, key } = readGroupedResultColumn();
          if (!aggregate && !key) throw new Error('HAVING must name a selected aggregate or grouping key by alias or expression.');
          const alias = (aggregate ?? key).alias;
          let type = key?.valueType ?? (key && projectionFields[key.field.toLowerCase()][1]);
          if (aggregate) {
            const inputType = aggregate.selection?.valueType ?? 'Int32';
            if (aggregate.operation === 'LongCount') type = 'Count';
            else if (aggregate.operation === 'Average' && !['Single', 'Double'].includes(inputType)) type = 'Decimal';
            else type = inputType;
          }
          const nullable = aggregate ? aggregate.operation !== 'LongCount' && aggregate.selection?.nullable === true : key.nullable || type === 'String';
          if (key && accept('NOT')) {
            expect('LIKE');
            const match = readLike(type, true);
            if (match.operator === 'IsNotNull') {
              addHavingFilter({ alias, operator: 'NotContains', value: '' });
            } else if (match.operator === 'Equal') {
              addHavingFilter({ alias, operator: 'NotEqual', value: null });
              addHavingFilter({ alias, operator: 'NotEqual', value: match.value });
            } else {
              addHavingFilter({ alias, operator: 'Not' + match.operator, value: match.value });
            }
          } else if (key && accept('LIKE')) {
            addHavingFilter({ alias, ...readLike(type) });
          } else if (accept('IS')) {
            const not = accept('NOT');
            expect('NULL');
            if (!nullable) throw new Error('This selected alias cannot be null.');
            addHavingFilter(not ? { alias, operator: 'NotEqual', value: null } : { alias, operator: 'IsNull' });
          } else if (accept('BETWEEN')) {
            addHavingComparison(alias, type, '>=');
            expect('AND');
            addHavingComparison(alias, type, '<=');
          } else {
            addHavingComparison(alias, type, tokens[index++], nullable);
          }
        } while (accept('AND'));
      }
      if (accept('ORDER')) {
        expect('BY');
        grouped.resultOrder = [];
        do {
          const position = readSelectPosition(resultColumns, 'ORDER BY');
          const { aggregate, key } = position ? {} : readGroupedResultColumn();
          const alias = (position ?? aggregate ?? key)?.alias;
          if (!alias) throw new Error('Order grouped results by a selected column alias or expression.');
          if (grouped.resultOrder.some(item => item.alias === alias))
            throw new Error('Use each grouped ordering alias only once.');
          const descending = accept('DESC');
          if (!descending) accept('ASC');
          grouped.resultOrder.push({ alias, descending });
        } while (accept(','));
      }
      const window = readWindow(100, 100, 1, selectTop);
      if (window.take !== undefined) grouped.resultTake = window.take;
      if (window.skip !== undefined) {
        if (!grouped.resultOrder) throw new Error('Use ORDER BY before paging grouped results.');
        grouped.resultSkip = window.skip;
      }
      accept(';');
      if (index !== tokens.length) throw new Error('Unexpected grouped query clause.');
      const filterCount = body.filters.length + (body.anyOf?.length ?? 0) +
        (body.where ? whereStats(body.where).filters : 0);
      if (body.anyChildren || body.anyDescendants || body.where && whereStats(body.where).relationships || filterCount > 12)
        throw new Error('Grouped queries support up to twelve filters on the source records.');
      if (body.virtualMappingId) {
        const inputColumns = [{ field: 'Id', alias: 'Id' }];
        const inputKeys = new Set();
        function addMappedInput(selection, groupingKey = false) {
          if (!mappedValue(selection, groupingKey))
            throw new Error('Mapped grouping requires direct STRING, BOOLEAN, GUID, INT32, INT64 or DOUBLE metadata fields; DATETIME is also admitted for grouping keys.');
          const key = selectionKey(selection);
          if (!inputKeys.has(key)) {
            inputKeys.add(key);
            inputColumns.push({ ...selection, alias: 'MixedInput' + inputKeys.size });
          }
        }
        keys.forEach(key => addMappedInput(key, true));
        resultColumns.forEach(column => {
          if (column.selection) {
            if (column.selection.function)
              throw new Error('Computed grouped selections require native function enrollment.');
            addMappedInput(column.selection);
          }
        });
        if (inputColumns.length > 32) throw new Error('Use at most 31 different mapped grouping inputs.');
        const grouping = { keys, columns: resultColumns, maximumGroups: grouped.maximumGroups };
        for (const name of ['resultFilters', 'resultOrder', 'resultSkip', 'resultTake'])
          if (Object.hasOwn(grouped, name)) grouping[name] = grouped[name];
        return { path: 'data/query-cql/projection/mixed', body: {
          ...body, orderBy: 'Id', skip: 0, take: 1, resultOperation: 'Group',
          columns: inputColumns, grouping
        } };
      }
      return { path: 'data/query-cql/grouped', body: grouped };
    }
    const bindRowOrder = order => {
      if (selected.length !== 1 || !['MongoDBOASIS', 'SQLLiteDBOASIS', 'Neo4jOASIS'].includes(selected[0]))
        throw new Error('Multi-column row ordering requires one MongoDB, SQLite or Neo4j source.');
      const terms = order.map(({ name, descending }) => {
        const field = fields[name?.toLowerCase()]?.[0];
        if (!['Name', 'Description', 'Version', 'Id'].includes(field))
          throw new Error('Order full rows by Name, Description, Version or Id.');
        return { field, descending };
      });
      if (new Set(terms.map(term => term.field)).size !== terms.length)
        throw new Error('Use each row ordering field only once.');
      body.orderBy = 'None';
      body.ordering = terms;
    };
    let selectedOrder;
    if (!operation && accept('ORDER')) {
      expect('BY');
      const order = [];
      do {
        let name;
        if (columns.length && (peek() === 'CASE' || ['CAST', 'LENGTH', 'COALESCE'].includes(peek()) && tokens[index + 1] === '(')) {
          const expression = selectionKey(readSelection(true));
          const selectedColumn = columns.find(column => selectionKey(column) === expression);
          if (!selectedColumn) throw new Error('Order by selected column aliases or expressions.');
          name = selectedColumn.alias;
        } else {
          name = readOrderColumn(columns);
        }
        const descending = accept('DESC');
        if (!descending) accept('ASC');
        order.push({ name, descending });
      } while (accept(','));
      if (order.length > 1 && !columns.length) {
        bindRowOrder(order);
      } else if (order.length > 1 || order.some(term =>
          columns.some(column => column.function && column.alias.toLowerCase() === term.name?.toLowerCase()))) {
        if (order.length > 8) throw new Error('Use up to eight selected ordering aliases.');
        selectedOrder = order.map(({ name, descending }) => {
          const column = columns.find(item => item.alias.toLowerCase() === name?.toLowerCase());
          if (!column) throw new Error('Order by selected column aliases.');
          return { alias: column.alias, descending };
        });
        if (new Set(selectedOrder.map(item => item.alias)).size !== selectedOrder.length)
          throw new Error('Use each selected ordering alias only once.');
      } else {
        const name = order[0].name?.toLowerCase();
        const selectedColumn = columns.find(column => column.alias.toLowerCase() === name);
        const field = selectedColumn?.field ?? fields[name]?.[0];
        if (['Name', 'Version', 'Id'].includes(field)) {
          body.orderBy = field;
          if (order[0].descending) body.descending = true;
        } else if (!columns.length && field === 'Description') {
          bindRowOrder(order);
        } else if (selectedColumn) {
          selectedOrder = [{ alias: selectedColumn.alias, descending: order[0].descending }];
        } else {
          throw new Error('Order by Name, Version, Id, or a selected column alias.');
        }
      }
    }
    if (mixedScalar) {
      if (scalarSelection?.valueType === 'DateTime') throw new Error('Mapped DateTime selected-value aggregates are not admitted.');
      const window = readWindow(1, 0);
      if (window.skip !== undefined && window.skip !== 0)
        throw new Error('A mixed count returns one scalar without a result offset.');
    }
    if (!operation) {
      const window = readWindow(100, 1000, 1, selectTop);
      if (window.take !== undefined) body.take = window.take;
      if (window.skip !== undefined) body.skip = window.skip;
    }
    if (accept('TIMEOUT')) {
      if (columns.length) throw new Error('Selected columns use the server timeout.');
      if (operation) throw new Error('TIMEOUT applies to row queries. Scalar queries use the server timeout.');
      body.maximumDurationMilliseconds = integer(tokens[index++], 1, 30000);
    }
    accept(';');
    if (index !== tokens.length) throw new Error('Unexpected clause near token ' + (index + 1) + '.');
    const relationships = [...(body.anyChildren ?? []), ...(body.anyDescendants ?? [])];
    if (relationships.length > 4) throw new Error('Use at most four relationship predicates.');
    const filterCount = [body, ...relationships].reduce(
      (total, scope) => total + scope.filters.length + (scope.anyOf?.length ?? 0) +
        (scope.where ? whereStats(scope.where).filters : 0), 0);
    if (filterCount > 12) throw new Error('Use at most twelve filters across the query.');
    if (holo && (operation || body.orderBy !== 'None' || relationships.length ||
        [...body.filters, ...(body.anyOf ?? [])].some(item =>
          item.field !== 'Id' || !['Equal', 'In'].includes(item.operator)))) {
      throw new Error('HoloOASIS supports row queries with Id equality or IN filters, LIMIT, and OFFSET.');
    }
    for (const accountProvider of accountProviders) {
      const { key, accepts, maximumKeys = 8, requiresSingleKeyFilter } = keyProfiles[accountProvider];
      if (requiresSingleKeyFilter && (body.filters.length !== 1 || body.anyOf))
        throw new Error(accountProvider + ' requires one ' + key + ' equality or IN filter.');
      const invalidFilter = [...body.filters, ...(body.anyOf ?? [])].some(filter => {
        if (filter.scalar !== 'String' || filter.metadataPath?.length !== 1 || filter.metadataPath[0] !== key ||
            !['Equal', 'In'].includes(filter.operator)) return true;
        const values = Array.isArray(filter.value) ? filter.value : [filter.value];
        return (filter.operator === 'In' && values.length > maximumKeys) || values.some(value => !accepts(value)) ||
          (requiresSingleKeyFilter && new Set(values).size !== values.length);
      });
      if (operation || body.orderBy !== 'None' || relationships.length || invalidFilter) {
        throw new Error(accountProvider + ' supports row queries with ' + key +
          ' equality or IN (up to ' + maximumKeys + ' keys), LIMIT, and OFFSET.');
      }
    }
    if (columns.length) {
      delete body.includeMetadata;
      if (body.virtualMappingId) {
        if (columns.length < 1 || columns[0].field !== 'Id' || columns[0].function ||
            columns.slice(1).some(column => column.metadataPath?.length !== 1))
          throw new Error('Virtual mapping queries must select Id first, followed only by direct mapped metadata fields.');
        if (columns.slice(1).some(column => !mappedValue(column, true, !mixedScalar)))
          throw new Error('Mapped columns support STRING, BOOLEAN, GUID, DATETIME, INT32, INT64 and finite DOUBLE; LENGTH/text COALESCE require STRING, CASE requires BOOLEAN integer COALESCE requires INT32 input; UTC date parts require DATETIME input.');
        if ((!selectedOrder && body.orderBy !== 'Id') || body.skip >= 200 || body.take < 1)
          throw new Error('Order by Id or selected aliases, with a positive LIMIT and OFFSET below 200 for a Virtual Holon mapping.');
        return { path: 'data/query-cql/projection/mixed', body: { ...body,
          ...(selectedOrder ? { orderBy: 'None', selectedOrder } : {}), columns },
          ...(mixedScalar && scalarAlias ? {scalarAlias} : {}) };
      }
      if (selectedOrder) {
        return { path: 'data/query-cql/projection',
          body: { ...body, orderBy: 'None', columns, selectedOrder } };
      }
      return { path: 'data/query-cql/projection', body: { ...body, columns } };
    }
    if (operation) body.operation = countDistinct ? 'CountDistinct' : countValues ? 'CountValues' : operation;
    if (scalarSelection) body.selection = scalarSelection;
    if (selected.length > 1) {
      const { provider: _, skip, take, maximumDurationMilliseconds, ...source } = body;
      return { path: 'data/query-cql/federation', body: {
        scope: 'Named', providers: selected, source: { ...source, skip: 0, take: 100 },
        identityMode: 'PreserveCopies', skip, take, maximumInputRows: 300, maximumRowsPerProvider: 100,
        maximumInputBytes: 4194304, maximumOutputBytes: 1048576, maximumWork: 100000,
        maximumProviderAttempts: selected.length, maximumDurationMilliseconds: maximumDurationMilliseconds ?? 5000
      } };
    }
    return { path: operation ? 'data/query-cql/scalar' : 'data/query-cql', body, ...(scalarAlias ? { scalarAlias } : {}) };
  }

  function publicExample(provider, request, discoveredProfiles = {}) {
    const expectedKey = (accountProfiles[provider] ?? discoveredProfiles[provider])?.key;
    const [filter] = request?.filters ?? [];
    const values = filter?.operator === 'Equal' ? [filter.value] : filter?.operator === 'In' ? filter.value : null;
    if (request?.provider !== provider || request.filters?.length !== 1 || !values ||
        !Array.isArray(values) || !values.length || request.orderBy !== 'None' ||
        request.descending || request.includeMetadata !== true ||
        request.anyOf?.length || request.anyChildren?.length || request.anyDescendants?.length || request.graph)
      return null;
    const field = provider === 'HoloOASIS' && filter.field === 'Id' && filter.scalar === 'Guid' ? 'Id'
      : expectedKey && filter.metadataPath?.length === 1 && filter.metadataPath[0] === expectedKey &&
        filter.scalar === 'String' ? 'metadata.' + expectedKey : null;
    if (!field || values.some(key => typeof key !== 'string') ||
        !Number.isInteger(request.take) || request.take < 1 || request.take > 100 || request.skip)
      return null;
    const keys = values.map(key => "'" + key.replace(/'/g, "''") + "'").join(', ');
    const text = `SELECT * FROM ${provider} WHERE ${field} ${filter.operator === 'Equal' ? '= ' + keys : 'IN (' + keys + ')'} LIMIT ${request.take}${request.maximumDurationMilliseconds ? ' TIMEOUT ' + request.maximumDurationMilliseconds : ''}`;
    compile(text, discoveredProfiles);
    return text;
  }

  const api = { providers: Object.freeze([...providers]), compile, publicExample, discoverKeyProfiles };
  if (typeof module !== 'undefined') module.exports = api;
  else root.CqlText = api;
})(globalThis);
