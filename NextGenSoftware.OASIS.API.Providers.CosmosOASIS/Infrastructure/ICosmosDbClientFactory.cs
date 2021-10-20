namespace NextGenSoftware.OASIS.API.Providers.CosmosOASIS.Infrastructure
{
    public interface ICosmosDbClientFactory
    {
        ICosmosDbClient GetClient(string collectionName);
    }
}
