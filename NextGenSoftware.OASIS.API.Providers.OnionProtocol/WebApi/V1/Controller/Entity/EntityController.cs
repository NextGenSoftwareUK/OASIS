using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Interfaces;

namespace OASIS_Onion.WebApi.V1.Controller.Entity
{
    public abstract class EntityController<TEntity, TService> : Microsoft.AspNetCore.Mvc.Controller
        where TService : IEntityService<TEntity>
        where TEntity : IEntity
    {
        protected readonly TService _service;
        protected IHttpContextAccessor _accessor;

        protected EntityController(TService service)
        {
            _service = service;
        }

        protected EntityController(TService service, IHttpContextAccessor accessor)
            : this(service)
        {
            _accessor = accessor;
        }

        protected void SetClientIp(TEntity entityObject)
        {
            entityObject.IPAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
        }
    }
}