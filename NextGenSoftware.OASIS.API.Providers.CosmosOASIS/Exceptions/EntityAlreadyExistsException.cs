using System;

namespace NextGenSoftware.OASIS.API.Providers.CosmosOASIS.Exceptions
{
    public class EntityAlreadyExistsException : Exception
    {
        public EntityAlreadyExistsException() { }

        public EntityAlreadyExistsException(string message): base(message) { }
    }
}
