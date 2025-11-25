namespace BandService.Endpoints.Bands.CreateBand;

using FluentValidation;

public sealed class CreateBandValidator : AbstractValidator<CreateBandRequest>
{
    public CreateBandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().NotNull();
    }
}
