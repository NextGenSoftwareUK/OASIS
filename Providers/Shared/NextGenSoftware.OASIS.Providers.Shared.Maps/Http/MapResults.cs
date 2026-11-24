using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.Maps.Http
{
    public static class MapResults
    {
        public static OASISResult<T> Error<T>(string message) => new OASISResult<T> { IsError = true, Message = message };

        public static OASISResult<T> Ok<T>(T value) => new OASISResult<T> { Result = value };
    }
}
