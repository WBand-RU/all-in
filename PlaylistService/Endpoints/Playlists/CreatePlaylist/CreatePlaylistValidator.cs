using FluentValidation;

namespace PlaylistService.Endpoints.Playlists.CreatePlaylist;

public sealed class CreatePlaylistValidator : AbstractValidator<CreatePlaylistRequest>
{
    public CreatePlaylistValidator()
    {
        RuleFor(x => x.BandId).NotEmpty().NotNull().WithMessage("Band ID is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .NotNull()
            .MaximumLength(200)
            .WithMessage("Playlist name is required and must not exceed 200 characters");

        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);

        RuleFor(x => x.Items)
            .Must(items =>
                items == null
                || items.All(item =>
                    (
                        item.Type == Domain.PlaylistItemType.Song
                        && !string.IsNullOrWhiteSpace(item.SongId)
                    )
                    || (
                        item.Type == Domain.PlaylistItemType.Block
                        && !string.IsNullOrWhiteSpace(item.BlockTitle)
                    )
                    || (item.Type == Domain.PlaylistItemType.Pause)
                )
            )
            .WithMessage("Songs must have SongId, Blocks must have BlockTitle");
    }
}
