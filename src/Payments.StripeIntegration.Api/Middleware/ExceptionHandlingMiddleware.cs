namespace Payments.StripeIntegration.Api.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                //Log exception type and message here, you can also log the stack trace if needed, you can also log the request path and query string if needed, you can also log the user identity if needed, you can also log the correlation id if needed, you can also log the request body if needed, you can also log the response body if needed, you can also log the request headers if needed, you can also log the response headers if needed, you can also log the request method if needed, you can also log the response status code if needed, you can also log the request duration if needed, you can also log the request id if needed, you can also log the trace id if needed, you can also log the span id if needed, you can also log the parent span id if needed, you can also log the baggage items if needed, you can also log the activity tags if needed, you can also log the activity events if needed, you can also log the activity links if needed, you can also log the activity status if needed, you can also log the activity duration if needed, you can also log the activity start time if needed, you can also log the activity end time if needed, you can also log the activity trace state if needed, you can also log the activity trace flags if needed
                _logger.LogError($"An unhandled exception occurred: {ex.GetType().FullName} - {ex.Message}");

                if (ex.InnerException != null)
                {
                    //Log innerexception type and message here
                    _logger.LogError($"An unhandled inner exception occurred: {ex.InnerException.GetType().FullName} - {ex.InnerException.Message}");
                }

                httpContext.Response.StatusCode = 500; // Internal Server Error
                await httpContext.Response.WriteAsJsonAsync(new
                {
                    Message = ex.Message,
                    Type = ex.GetType().FullName,
                });
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    // Example app.UseExceptionHandlingMiddleware();
    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
}
