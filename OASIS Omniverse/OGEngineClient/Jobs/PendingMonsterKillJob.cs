namespace NextGenSoftware.OASIS.STARAPI.Client;

internal sealed class PendingMonsterKillJob
{
    public PendingMonsterKillJob(string engineName, string displayName, int xp, bool isBoss, bool doMint, string? provider, string gameSource)
    {
        EngineName = engineName;
        DisplayName = displayName;
        Xp = xp;
        IsBoss = isBoss;
        DoMint = doMint;
        Provider = provider ?? "SolanaOASIS";
        GameSource = gameSource ?? "ODOOM";
    }

    public string EngineName { get; }
    public string DisplayName { get; }
    public int Xp { get; }
    public bool IsBoss { get; }
    public bool DoMint { get; }
    public string Provider { get; }
    public string GameSource { get; }
}
