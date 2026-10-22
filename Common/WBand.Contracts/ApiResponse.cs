namespace WBand.Contracts;

/// <summary>Common status envelope for an HTTP operation.</summary>
public sealed record ApiResponse(ApiCodes Code)
{
    /// <summary>Creates a successful response.</summary>
    public static ApiResponse Success() => new(ApiCodes.Success);

    /// <summary>Creates a response with the specified application status.</summary>
    public static ApiResponse Error(ApiCodes code) => new(code);
};

/// <summary>Common HTTP status envelope containing a typed value.</summary>
public sealed record ApiResponse<TValue>(ApiCodes Code, TValue Value)
{
    /// <summary>Creates a successful response containing the value.</summary>
    public static ApiResponse<TValue> Success(TValue value) => new(ApiCodes.Success, value);

    /// <summary>Creates a failed response without a value.</summary>
    public static ApiResponse<TValue> Error(ApiCodes code) => new(code, default!);
};
