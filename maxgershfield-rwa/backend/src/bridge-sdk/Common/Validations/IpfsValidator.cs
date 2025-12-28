namespace Common.Validations;

public static partial class IpfsValidator
{
    [GeneratedRegex("^Qm[1-9A-HJ-NP-Za-km-z]{44}$")]
    private static partial Regex CidV0Regex();

    public static bool IsValidCidV0(string cid)
    {
        return CidV0Regex().IsMatch(cid);
    }
}