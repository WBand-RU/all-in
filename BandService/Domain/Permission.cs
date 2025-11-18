namespace BandService.Domain;

/// <summary>
/// Defines the permissions available in a band
/// </summary>
[Flags]
public enum Permission
{
    None = 0,

    /// <summary>
    /// Permission to manage band members (invite, remove, change roles)
    /// </summary>
    ManageMembers = 1 << 0,

    /// <summary>
    /// Permission to edit songs
    /// </summary>
    EditSongs = 1 << 1,

    /// <summary>
    /// Permission to edit playlists
    /// </summary>
    EditPlaylists = 1 << 2,

    /// <summary>
    /// Permission to manage playbacks
    /// </summary>
    ManagePlaybacks = 1 << 3,

    /// <summary>
    /// Permission to manage agent players
    /// </summary>
    ManageAgents = 1 << 4,

    /// <summary>
    /// Permission to send messages in chat
    /// </summary>
    SendMessages = 1 << 5,

    /// <summary>
    /// All permissions combined
    /// </summary>
    All = ManageMembers | EditSongs | EditPlaylists | ManagePlaybacks | ManageAgents | SendMessages,
}
