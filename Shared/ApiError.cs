namespace Shared;

public sealed record ApiResponse(ApiCodes Code)
{
    public static ApiResponse Success() => new(ApiCodes.Success);

    public static ApiResponse Error(ApiCodes code) => new(code);
};

public sealed record ApiResponse<TValue>(ApiCodes Code, TValue Value)
{
    public static ApiResponse<TValue> Success(TValue value) => new(ApiCodes.Success, value);

    public static ApiResponse<TValue> Error(ApiCodes code) => new(code, default!);
};
