namespace NextGenSoftware.OASIS.API.Contracts.Interfaces
{
    /// <summary>
    /// Interface for batch API requests
    /// </summary>
    public interface IBatchRequest : IRequest
    {
        string[] GetRequestMultipleURLParameters();
    }
}

