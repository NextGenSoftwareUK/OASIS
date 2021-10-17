using System;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Interfaces
{
    public interface IModel
    {
        DateTime AddedDate { get; set; }
        DateTime ModifiedDate { get; set; }
        string IPAddress { get; set; }
    }
}