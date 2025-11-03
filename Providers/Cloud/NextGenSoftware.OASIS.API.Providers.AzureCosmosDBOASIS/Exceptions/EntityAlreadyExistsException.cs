using System;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Exceptions
{
    public class EntityAlreadyExistsException : Exception
    {
        public EntityAlreadyExistsException() { }

        public EntityAlreadyExistsException(string message): base(message) { }
    }
}
