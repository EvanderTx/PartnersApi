using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Partners.Application.Common.Exceptions;
using Partners.Domain.Common;

namespace Partners.Api.ErrorHandling;

// Converts exceptions into consistent ProblemDetails responses, without exposing stack traces.
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            DomainValidationException ex => ValidationProblem(ex.Errors),
            InvalidRequestException ex => ValidationProblem(ex.Errors),
            NotFoundException ex => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Resource not found.",
                Detail = ex.Message
            },
            ConflictException ex => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict.",
                Detail = ex.Message
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            }
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });
    }

    private static ValidationProblemDetails ValidationProblem(IReadOnlyDictionary<string, string[]> errors) =>
        new(errors.ToDictionary())
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred."
        };
}
