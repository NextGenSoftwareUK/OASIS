using System;

namespace NextGenSoftware.OASIS.API.Providers.CosmosOASIS.Exceptions
{
    public class EntityNotFoundException : Exception
    {
        public EntityNotFoundException() { }

        public EntityNotFoundException(string message): base(message) { }
    }
}
