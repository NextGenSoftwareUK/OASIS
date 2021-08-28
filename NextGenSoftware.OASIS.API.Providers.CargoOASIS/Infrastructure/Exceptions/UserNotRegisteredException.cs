using System;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Exceptions
{
    public class UserNotRegisteredException : Exception
    {
        public UserNotRegisteredException() : base("User Not Registered")
        {
        }
    }
}