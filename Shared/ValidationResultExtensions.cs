using FluentValidation.Results;

namespace Shared;

public static class ValidationResultExtensions
{
    public static string ToMessage(this ValidationResult validationResult)
    {
        return validationResult
            .Errors.Select(e => e.ErrorMessage)
            .Aggregate((a, b) => a + "\n" + b);
    }
}
