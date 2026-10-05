namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public interface ICosmosDbClientFactory
    {
        public ICosmosDbClient GetClient(string collectionName);        
    }
}
