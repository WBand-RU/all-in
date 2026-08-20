using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WBand.Modules.MixerModule.Application;
using WBand.Modules.MixerModule.Domain;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class MixerModuleTests
{
    [Fact]
    public void RecoveryService_CanBeConstructedAsSingletonWithScopeValidation()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostedService, MixBatchRecoveryService>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        Assert.IsType<MixBatchRecoveryService>(provider.GetRequiredService<IHostedService>());
    }

    [Fact]
    public void MixPlan_CreatesFullFocusAndMinusForEachInstrument()
    {
        var tracks = new[]
        {
            new AudioTrack(Guid.NewGuid(), "click.wav", "Click", "Клик"),
            new AudioTrack(Guid.NewGuid(), "guitar.mp3", "Guitar", "Гитары"),
            new AudioTrack(Guid.NewGuid(), "bass.ogg", "Bass", "Бас"),
        };

        var plans = MixPlanFactory.Create(tracks);

        Assert.Equal(5, plans.Count);
        Assert.Single(plans, plan => plan.Kind == MixPlanKind.Full);
        Assert.Equal(2, plans.Count(plan => plan.Kind == MixPlanKind.Focus));
        Assert.Equal(2, plans.Count(plan => plan.Kind == MixPlanKind.Minus));
    }

    [Fact]
    public void FocusMix_KeepsTargetAndClickAtForegroundLevel()
    {
        var click = new AudioTrack(Guid.NewGuid(), "click.wav", "Click", "Клик");
        var guitar = new AudioTrack(Guid.NewGuid(), "guitar.wav", "Guitar", "Гитары");
        var bass = new AudioTrack(Guid.NewGuid(), "bass.wav", "Bass", "Бас");

        var focus = MixPlanFactory.Create([click, guitar, bass]).Single(plan =>
            plan.Kind == MixPlanKind.Focus && plan.Target == guitar);

        Assert.Equal(0, focus.Inputs.Single(input => input.Track == click).GainDb);
        Assert.Equal(0, focus.Inputs.Single(input => input.Track == guitar).GainDb);
        Assert.Equal(-40, focus.Inputs.Single(input => input.Track == bass).GainDb);
    }

    [Fact]
    public void RecoveryPolicy_RecoversProcessingBatchWithoutRecentHeartbeat()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = new MixBatch { Status = MixBatchStatus.Processing,
            StartedAt = now.AddHours(-1), CreatedAt = now.AddHours(-1) };

        Assert.True(MixBatchRecoveryPolicy.IsStale(batch, now));
    }

    [Fact]
    public void RecoveryPolicy_DoesNotDuplicateActiveBatch()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = new MixBatch { Status = MixBatchStatus.Processing,
            StartedAt = now.AddHours(-1), HeartbeatAt = now.AddMinutes(-5),
            CreatedAt = now.AddHours(-1) };

        Assert.False(MixBatchRecoveryPolicy.IsStale(batch, now));
    }
}
