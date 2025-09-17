using System;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Exceptions
{
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException() { }

        public EntityNotFoundException(string message): base(message) { }
    }
}
