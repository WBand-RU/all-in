using FluentValidation;

namespace SongService.Endpoints.Songs.CreateSong;

public sealed class CreateSongValidator : AbstractValidator<CreateSongRequest>
{
    public CreateSongValidator()
    {
        RuleFor(x => x.BandId).NotEmpty().NotNull().WithMessage("Band ID is required");

        RuleFor(x => x.Title)
            .NotEmpty()
            .NotNull()
            .MaximumLength(200)
            .WithMessage("Song title is required and must not exceed 200 characters");

        RuleFor(x => x.Author).MaximumLength(200).When(x => x.Author != null);

        RuleFor(x => x.Key).MaximumLength(10).When(x => x.Key != null);

        RuleFor(x => x.Bpm)
            .InclusiveBetween(20, 300)
            .When(x => x.Bpm.HasValue)
            .WithMessage("BPM must be between 20 and 300");
    }
}
