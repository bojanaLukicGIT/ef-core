using System.ComponentModel.DataAnnotations;

namespace EfCoreMiniApp.Api.Validation;

public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var input = context.Arguments.OfType<T>().FirstOrDefault();
        if (input is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["body"] = ["A request body is required."]
            });
        }

        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true))
        {
            return await next(context);
        }

        var errors = results
            .SelectMany(
                result => result.MemberNames.DefaultIfEmpty(string.Empty),
                (result, member) => (Member: member, Message: result.ErrorMessage ?? "The value is invalid."))
            .GroupBy(error => error.Member)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray());

        return TypedResults.ValidationProblem(errors);
    }
}

public static class ValidationExtensions
{
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder) where T : class =>
        builder.AddEndpointFilter<ValidationFilter<T>>().ProducesValidationProblem();

    public static IEnumerable<ValidationResult> ValidateNested(object? value, string memberName)
    {
        if (value is null)
        {
            return [];
        }

        var results = new List<ValidationResult>();
        Validator.TryValidateObject(value, new ValidationContext(value), results, validateAllProperties: true);
        return results.Select(result => new ValidationResult(
            result.ErrorMessage,
            result.MemberNames.Select(member => $"{memberName}.{member}").ToArray()));
    }
}
