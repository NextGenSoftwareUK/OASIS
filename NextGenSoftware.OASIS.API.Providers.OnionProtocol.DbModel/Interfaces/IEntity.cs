using System;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Interfaces
{
    public interface IEntity : IModel
    {
        Guid Id { get; set; }
    }
}