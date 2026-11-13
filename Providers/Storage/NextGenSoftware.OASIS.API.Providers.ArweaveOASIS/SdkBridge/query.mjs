// Complete a native tag query or reject it. Never publish partial success.
export async function queryTransactionIds(api, tags = {}, owner = null) {
  if (owner !== null && (typeof owner !== 'string' || !/^[A-Za-z0-9_-]{43}$/.test(owner))) throw new Error('Invalid Arweave owner.');
  const filters = Object.entries(tags).map(([name, value]) => {
    if (!name.trim() || typeof value !== 'string') throw new Error('Invalid Arweave tag filter.');
    return { name, values: [value] };
  });
  const ids = [], seen = new Set(), cursors = new Set();
  let after = null;
  // Bounded protocol traversal. Exceeding the limit is an error, not truncation.
  for (let page = 0; page < 100; page++) {
    const response = await api.post('graphql', {
      query: `query($tags: [TagFilter!], $after: String, $owners: [String!]) {
        transactions(owners: $owners, tags: $tags, first: 100, after: $after, sort: HEIGHT_DESC) {
          pageInfo { hasNextPage } edges { cursor node { id } }
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
      seen.add(id); ids.push(id);
    }
    if (!connection.pageInfo.hasNextPage) return ids;
    const cursor = connection.edges.at(-1)?.cursor;
    if (typeof cursor !== 'string' || !cursor.trim() || cursors.has(cursor))
      throw new Error('Arweave GraphQL cursor did not advance.');
    cursors.add(cursor); after = cursor;
  }
  throw new Error('Arweave GraphQL traversal exceeds 100 pages; no partial result returned.');
}
