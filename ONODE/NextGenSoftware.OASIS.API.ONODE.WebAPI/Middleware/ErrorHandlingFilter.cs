//using System.Net;
//using System;
//using System.Web.Http.ExceptionHandling;
//using System.Web.Http.Results;
//using System.Web.Http.Filters;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
//{
//    public class ErrorHandlingFilter : ExceptionFilterAttribute
//    {
//        public override void OnException(ExceptionContext context)
//        {
//            HandleExceptionAsync(context);
//            context.ExceptionHandled = true;
//        }

//        private static void HandleExceptionAsync(ExceptionContext context)
//        {
//            var exception = context.Exception;

//            if (exception is MyNotFoundException)
//                SetExceptionResult(context, exception, HttpStatusCode.NotFound);
//            else if (exception is MyUnauthorizedException)
//                SetExceptionResult(context, exception, HttpStatusCode.Unauthorized);
//            else if (exception is MyException)
//                SetExceptionResult(context, exception, HttpStatusCode.BadRequest);
//            else
//                SetExceptionResult(context, exception, HttpStatusCode.InternalServerError);
//        }

//        private static void SetExceptionResult(
//            ExceptionContext context,
//            Exception exception,
//            HttpStatusCode code)
//        {
//            context.Result = new JsonResult(new ApiResponse(exception))
//            {
//                StatusCode = (int)code
//            };
//        }
//    }
//}
