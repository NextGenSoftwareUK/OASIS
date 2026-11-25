// Complete a native tag query or reject it. Never publish partial success.
export async function queryTransactionIds(api, tags = {}, owner = null) {
  return (await queryTransactions(api, tags, owner, false)).map(row => row.id);
}

// Same complete traversal, also returning each transaction's native tags (name -> value).
// A repeated tag name is rejected: storage records never carry one, and picking either value would be a guess.
export async function queryTaggedTransactions(api, tags = {}, owner = null) {
  return queryTransactions(api, tags, owner, true);
}

async function queryTransactions(api, tags, owner, withTags) {
  if (owner !== null && (typeof owner !== 'string' || !/^[A-Za-z0-9_-]{43}$/.test(owner))) throw new Error('Invalid Arweave owner.');
  const filters = Object.entries(tags).map(([name, value]) => {
    if (!name.trim() || typeof value !== 'string') throw new Error('Invalid Arweave tag filter.');
    return { name, values: [value] };
  });
  const rows = [], seen = new Set(), cursors = new Set();
  let after = null;
  // Bounded protocol traversal. Exceeding the limit is an error, not truncation.
  for (let page = 0; page < 100; page++) {
    const response = await api.post('graphql', {
      query: `query($tags: [TagFilter!], $after: String, $owners: [String!]) {
        transactions(owners: $owners, tags: $tags, first: 100, after: $after, sort: HEIGHT_DESC) {
          pageInfo { hasNextPage } edges { cursor node { id${withTags ? ' tags { name value }' : ''} } }
        }
      }`,
      variables: { tags: filters, after, owners: owner === null ? null : [owner] }
    });
    if (response?.status !== 200) throw new Error('Arweave GraphQL HTTP failure.');
    const body = response.data;
    if (body?.errors !== undefined && (!Array.isArray(body.errors) || body.errors.length))
      throw new Error('Arweave GraphQL returned errors.');
    const connection = body?.data?.transactions;
    if (!Array.isArray(connection?.edges) || connection.edges.length > 100 ||
        typeof connection?.pageInfo?.hasNextPage !== 'boolean')
      throw new Error('Malformed Arweave GraphQL connection.');
    for (const edge of connection.edges) {
      const id = edge?.node?.id;
      if (typeof id !== 'string' || !/^[A-Za-z0-9_-]{43}$/.test(id) || seen.has(id))
        throw new Error('Invalid or duplicate Arweave transaction identity.');
      seen.add(id);
      if (!withTags) { rows.push({ id }); continue; }
      const nodeTags = edge.node.tags;
      if (!Array.isArray(nodeTags)) throw new Error('Arweave GraphQL transaction tags are malformed.');
      const map = {};
      for (const tag of nodeTags) {
        if (typeof tag?.name !== 'string' || typeof tag?.value !== 'string' || Object.hasOwn(map, tag.name))
          throw new Error('Arweave GraphQL transaction tag is malformed or repeated.');
        map[tag.name] = tag.value;
      }
      rows.push({ id, tags: map });
    }
    if (!connection.pageInfo.hasNextPage) return rows;
    const cursor = connection.edges.at(-1)?.cursor;
    if (typeof cursor !== 'string' || !cursor.trim() || cursors.has(cursor))
      throw new Error('Arweave GraphQL cursor did not advance.');
    cursors.add(cursor); after = cursor;
  }
  throw new Error('Arweave GraphQL traversal exceeds 100 pages; no partial result returned.');
}
