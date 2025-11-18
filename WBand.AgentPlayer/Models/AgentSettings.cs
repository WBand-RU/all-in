namespace WBand.AgentPlayer.Models;

/// <summary>
/// Settings for the agent player
/// </summary>
public sealed class AgentSettings
{
    public required string ServerUrl { get; set; }
    public required string BandId { get; set; }
    public required string AgentToken { get; set; }
    public required string AgentName { get; set; }
}
