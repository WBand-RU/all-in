namespace BandService.Domain;

public struct BandName
{
    public const int MaxLength = 100;
    public const int MinLenght = 3;

    public readonly string Value;

    public static BandName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (value.Length < 3)
        {
            throw new ArgumentException("Band name must be at least 3 characters long.");
        }

        if (value.Length > 100)
        {
            throw new ArgumentException("Band name must be at most 100 characters long.");
        }

        return new BandName(value);
    }

    private BandName(string value)
    {
        Value = value;
    }
}
