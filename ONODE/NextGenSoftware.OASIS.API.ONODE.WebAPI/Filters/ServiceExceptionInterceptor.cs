using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Filters
{
    public class ServiceExceptionInterceptor: ExceptionFilterAttribute
    {
        private readonly bool _enableGenericExceptionHandling;

        public ServiceExceptionInterceptor(IConfiguration configuration = null)
        {
            _enableGenericExceptionHandling = configuration?.GetValue<bool>("EnableGenericExceptionHandling", 
                bool.Parse(Environment.GetEnvironmentVariable("ENABLE_GENERIC_EXCEPTION_HANDLING") ?? "true")) ?? true;
        }

        public override Task OnExceptionAsync(ExceptionContext context)
        {
            var ex = context.Exception;
            
            // Always log the error first
            OASISErrorHandling.HandleError($"Error in {context.ActionDescriptor.DisplayName}: {ex.Message}", ex, includeStackTrace: true);
            
            // Check if this is a validation error (400) or real exception (500)
            bool isValidationError = ex is System.Text.Json.JsonException ||
                                    ex is OASISException ||
                                    ex.Message.Contains("required", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("missing", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("cannot be null", StringComparison.OrdinalIgnoreCase) ||
                                    ex.Message.Contains("AvatarId is required", StringComparison.OrdinalIgnoreCase);
            
            var errorResult = new OASISResult<object>
            {
                IsError = true,
                Exception = ex,
                Result = null,
                IsSaved = false,
                IsWarning = false,
                InnerMessages = new List<string>()
            };
            
            if (ex.InnerException != null)
            {
                errorResult.InnerMessages.Add(ex.InnerException.Message);
            }
            
            if (isValidationError)
            {
                // Validation error - return 400
                if (_enableGenericExceptionHandling)
                {
                    errorResult.Message = $"Invalid args were passed to {context.ActionDescriptor.DisplayName}. {ex.Message}";
                }
                else
                {
                    errorResult.Message = ex.Message;
                    errorResult.DetailedMessage = ex.ToString();
                }
                context.Result = new BadRequestObjectResult(errorResult);
            }
            else
            {
                // Real exception - return 500
                if (_enableGenericExceptionHandling)
                {
                    errorResult.Message = "Oooops. Sorry something broke, it has been logged and we are looking into it!";
                }
                else
                {
                    errorResult.Message = $"Unexpected error in {context.ActionDescriptor.DisplayName}: {ex.Message}";
                    errorResult.DetailedMessage = ex.ToString();
                }
                context.Result = new ObjectResult(errorResult)
                {
                    StatusCode = 500
                };
            }
            
            context.ExceptionHandled = true;
            return Task.CompletedTask;
        }
    }
}