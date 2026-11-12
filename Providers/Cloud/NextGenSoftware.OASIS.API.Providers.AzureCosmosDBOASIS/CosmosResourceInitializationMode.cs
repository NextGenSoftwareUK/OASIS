namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;

/// <summary>Explicit infrastructure ownership during provider activation.</summary>
public enum CosmosResourceInitializationMode
{
    ProvisionMissingResources = 0,
    ExistingResourcesOnly = 1
}
