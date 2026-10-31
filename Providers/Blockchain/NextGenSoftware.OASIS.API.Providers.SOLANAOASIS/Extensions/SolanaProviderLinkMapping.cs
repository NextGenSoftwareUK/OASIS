namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Extensions;

internal static class SolanaProviderLinkMapping
{
    private static Dictionary<ProviderType,string> Copy(Dictionary<ProviderType,string> value)
        => value==null?new():new(value);
    private static Dictionary<ProviderType,Dictionary<string,string>> Copy(Dictionary<ProviderType,Dictionary<string,string>> value)
        => value==null?new():value.ToDictionary(pair=>pair.Key,pair=>pair.Value==null?null:new Dictionary<string,string>(pair.Value));

    internal static void Capture(SolanaBaseDto record,IHolonBase source)
    {
        record.ProviderUniqueStorageKey=Copy(source.ProviderUniqueStorageKey);
        record.PreviousVersionProviderUniqueStorageKey=Copy(source.PreviousVersionProviderUniqueStorageKey);
        record.ProviderMetaData=Copy(source.ProviderMetaData);
    }

    internal static T Restore<T>(T target,SolanaBaseDto record) where T:IHolonBase
    {
        target.ProviderUniqueStorageKey=Copy(record.ProviderUniqueStorageKey);
        target.PreviousVersionProviderUniqueStorageKey=Copy(record.PreviousVersionProviderUniqueStorageKey);
        target.ProviderMetaData=Copy(record.ProviderMetaData);
        return target;
    }
}
