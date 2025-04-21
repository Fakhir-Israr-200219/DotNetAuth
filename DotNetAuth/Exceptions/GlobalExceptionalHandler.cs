using DotNetAuth.Domain.Constracts;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace DotNetAuth.Exceptions
{
    public class GlobalExceptionalHandler :IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionalHandler> _logger;

        public GlobalExceptionalHandler(ILogger<GlobalExceptionalHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            _logger.LogError(exception , exception.Message);
            var responce = new ErrorResponse
            {
                StatusCode = httpContext.Response.StatusCode,
            };
            switch (exception) 
            {
                case BadHttpRequestException :
                    responce.StatusCode = (int)HttpStatusCode.BadRequest;
                    responce.Title = exception.GetType().Name;
                    break;
                default:
                    responce.StatusCode = (int)HttpStatusCode.InternalServerError;
                    responce.Title = "Internal Server Error";
                    break;
            }

            httpContext.Response.StatusCode = responce.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(responce, cancellationToken);
            return true;
                
        }
    }
}
