using Wolverine;

namespace WBand.WebAPI.Tests;

internal sealed class RejectingOutgoingRule(Func<bool> reject) : IEnvelopeRule
{
    public void Modify(Envelope envelope)
    {
        if (reject())
            throw new InvalidOperationException("Outgoing publication is unavailable.");
    }

    public void ApplyCorrelation(IMessageContext originator, Envelope outgoing) => Modify(outgoing);
}
