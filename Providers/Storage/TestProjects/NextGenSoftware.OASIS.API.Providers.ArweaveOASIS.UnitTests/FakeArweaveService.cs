namespace NextGenSoftware.OASIS.API.Providers.ArweaveOASIS.UnitTests;

/// <summary>In-memory owner-pinned Arweave log: every post appends a transaction; queries match all tag filters.</summary>
internal sealed class FakeArweaveService : IArweaveService
{
    public readonly List<(string Id, byte[] Data, Dictionary<string, string> Tags)> Transactions = new();
    public Func<int, Task<string>>? PostAcknowledgement;
    public Func<string, Exception?>? ReadFailure;
    public int Posts, Reads;

    public static string Wallet { get; } = "{\"kty\":\"RSA\",\"d\":\"" + Convert.ToBase64String(Enumerable.Range(1, 64).Select(i => (byte)i).ToArray()).TrimEnd('=').Replace('+', '-').Replace('/', '_') + "\"}";

    public Task ProbeAsync() => Task.CompletedTask;

    public async Task<string> PostTransactionAsync(byte[] data, string contentType, Dictionary<string, string>? tags = null)
    {
        var posted = ++Posts;
        var id = PostAcknowledgement == null ? posted.ToString("D43") : await PostAcknowledgement(posted);
        if (!string.IsNullOrEmpty(id) && System.Text.RegularExpressions.Regex.IsMatch(id, "^[A-Za-z0-9_-]{43}$"))
            Transactions.Add((id, data, new Dictionary<string, string>(tags!)));
        return id;
    }

    public Task<byte[]> GetTransactionDataAsync(string txId)
    {
        Reads++;
        var failure = ReadFailure?.Invoke(txId);
        return failure != null ? Task.FromException<byte[]>(failure) : Task.FromResult(Transactions.Single(t => t.Id == txId).Data);
    }

    public Task<List<ArweaveTaggedTransaction>> QueryOwnedTaggedAsync(Dictionary<string, string> tags)
        => Task.FromResult(Transactions.Where(t => tags.All(f => t.Tags.TryGetValue(f.Key, out var v) && v == f.Value))
            .Select(t => new ArweaveTaggedTransaction(t.Id, t.Tags)).ToList());

    public Task<List<string>> QueryByTagsAsync(Dictionary<string, string> tags)
        => throw new InvalidOperationException("Storage must never discover unpinned public records.");

    public Task<List<string>> QueryOwnedByTagsAsync(Dictionary<string, string> tags)
        => throw new InvalidOperationException("Storage uses tagged owner-pinned discovery.");
}
