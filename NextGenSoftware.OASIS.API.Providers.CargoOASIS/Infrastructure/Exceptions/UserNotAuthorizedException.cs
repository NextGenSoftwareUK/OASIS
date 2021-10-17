using System;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Exceptions
{
    public class UserNotAuthorizedException : Exception
    {
        public UserNotAuthorizedException() : base("User not authorized, please retry authentication process, and try again")
        {
        }
    }
}