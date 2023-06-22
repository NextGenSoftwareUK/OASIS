using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core.Helpers;

namespace NextGenSoftware.OASIS.API.ONode.WebAPI.Filters
{
    public class ServiceExceptionInterceptor: ExceptionFilterAttribute
    {
        public override Task OnExceptionAsync(ExceptionContext context)
        {
            if (context.Exception.InnerException != null)
            {
                var exceptionResponse = new OASISResult<object>()
                {
                    Exception = context.Exception,
                    Message = context.Exception.Message,
                    Result = null,
                    IsError = true,
                    IsSaved = false,
                    IsWarning = false,
                    InnerMessages = new List<string>()
                    {
                        context.Exception.InnerException.Message
                    },
                    MetaData = null
                };
                context.Result = new JsonResult(exceptionResponse);
                ErrorHandling.HandleError(ref exceptionResponse, context.Exception.Message);
            }

            context.HttpContext.Response.StatusCode = 500;
            return Task.CompletedTask;
        }
    }
}