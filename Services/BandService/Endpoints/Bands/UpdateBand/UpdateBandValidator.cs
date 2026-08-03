using FluentValidation;

namespace BandService.Endpoints.Bands.UpdateBand;

public sealed class UpdateBandValidator : AbstractValidator<UpdateBandRequest>
{
    public UpdateBandValidator()
    {
        _ = this.RuleFor(x => x.Name).NotEmpty().NotNull();
    }
}
