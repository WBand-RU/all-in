namespace BandService.Endpoints.Bands.UpdateBand;

using FluentValidation;

public sealed class UpdateBandValidator : AbstractValidator<UpdateBandRequest>
{
    public UpdateBandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().NotNull();
    }
}
