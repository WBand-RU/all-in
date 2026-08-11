namespace WBand.Modules.BandModule.Contracts;

public enum BandPermission
{
    View,
    EditContent,
    ManageBand,
}

public sealed record CheckBandPermission(Guid BandId, Guid UserId, BandPermission Permission);
