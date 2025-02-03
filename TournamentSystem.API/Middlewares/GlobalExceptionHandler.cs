using Microsoft.AspNetCore.Mvc;
using TournamentSystem.Domain.Exceptions;

namespace TournamentSystem.API.Middlewares
{
    public class GlobalExceptionHandler : IMiddleware
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (ValidationException validationEx)
            {
                _logger.LogWarning(validationEx, validationEx.Message);

                context.Response.StatusCode = StatusCodes.Status400BadRequest;

                var problemDetails = new ProblemDetails()
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Validation Error",
                    Detail = validationEx.Message
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
            catch (NotFoundException notFoundEx)
            {
                _logger.LogWarning(notFoundEx, notFoundEx.Message);

                context.Response.StatusCode = StatusCodes.Status404NotFound;

                var problemDetails = new ProblemDetails()
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource Not Found",
                    Detail = notFoundEx.Message
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
            catch (UnauthorizedException unauthorizedEx)
            {
                _logger.LogWarning(unauthorizedEx, unauthorizedEx.Message);

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                var problemDetails = new ProblemDetails()
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = unauthorizedEx.Message
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
            catch (ForbiddenException forbiddenEx)
            {
                _logger.LogWarning(forbiddenEx, forbiddenEx.Message);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                var problemDetails = new ProblemDetails()
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = forbiddenEx.Message
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
            catch (ConflictException conflictEx)
            {
                _logger.LogWarning(conflictEx, conflictEx.Message);

                context.Response.StatusCode = StatusCodes.Status409Conflict;

                var problemDetails = new ProblemDetails()
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = conflictEx.Message
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
            catch (UnexpectedException unexpectedEx)
            {
                _logger.LogError(unexpectedEx, unexpectedEx.Message);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var problemDetails = new ProblemDetails()
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Unexpected Error",
                    Detail = unexpectedEx.Message
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var problemDetails = new ProblemDetails()
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Server Error",
                    Detail = "An unexpected server error occurred."
                };

                await context.Response.WriteAsJsonAsync(problemDetails);
            }
        }
    }
}
