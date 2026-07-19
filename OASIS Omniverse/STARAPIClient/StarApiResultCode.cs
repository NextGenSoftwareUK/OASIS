namespace NextGenSoftware.OASIS.STARAPI.Client;

public enum StarApiResultCode
{
    Success = 0,
    InitFailed = -1,
    NotInitialized = -2,
    Network = -3,
    InvalidParam = -4,
    ApiError = -5
}
