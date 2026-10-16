using WBand.Modules.MixerModule.Application;
using WBand.Modules.MixerModule.Domain;
using Xunit;

namespace WBand.Architecture.Tests;

public sealed class MixerModuleTests
{
    [Fact]
    public void MixPlan_CreatesFocusAndMinusForEachInstrument()
    {
        var tracks = new[]
        {
            new AudioTrack(Guid.NewGuid(), "click.wav", "Click", "Клик", "Click"),
            new AudioTrack(Guid.NewGuid(), "guitar.mp3", "Guitar", "Гитары"),
            new AudioTrack(Guid.NewGuid(), "bass.ogg", "Bass", "Бас"),
        };

        var plans = MixPlanFactory.Create(tracks);

        Assert.Equal(4, plans.Count);
        Assert.DoesNotContain(plans, plan => plan.Kind == MixPlanKind.Full);
        Assert.Equal(2, plans.Count(plan => plan.Kind == MixPlanKind.Focus));
        Assert.Equal(2, plans.Count(plan => plan.Kind == MixPlanKind.Minus));
    }

    [Fact]
    public void FocusMix_KeepsTargetAndClickAtForegroundLevel()
    {
        var click = new AudioTrack(Guid.NewGuid(), "click.wav", "Click", "Клик", "Click");
        var guitar = new AudioTrack(Guid.NewGuid(), "guitar.wav", "Guitar", "Гитары");
        var bass = new AudioTrack(Guid.NewGuid(), "bass.wav", "Bass", "Бас");

        var focus = MixPlanFactory.Create([click, guitar, bass]).Single(plan =>
            plan.Kind == MixPlanKind.Focus && plan.Target == guitar);

        Assert.Equal(0, focus.Inputs.Single(input => input.Track == click).GainDb);
        Assert.Equal(0, focus.Inputs.Single(input => input.Track == guitar).GainDb);
        Assert.Equal(-40, focus.Inputs.Single(input => input.Track == bass).GainDb);
    }

    [Fact]
    public void LeasePolicy_ClaimsProcessingBatchWithExpiredLease()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = new MixBatch { Status = MixBatchStatus.Processing,
            LeaseExpiresAt = now.AddMinutes(-1), CreatedAt = now.AddHours(-1) };

        Assert.True(MixBatchLeasePolicy.CanClaim(batch, now));
    }

    [Fact]
    public void LeasePolicy_PermitsImmediateRecoveryOnceDatabaseLockIsReleased()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = new MixBatch { Status = MixBatchStatus.Processing,
            LeaseExpiresAt = now.AddMinutes(5), CreatedAt = now.AddHours(-1) };

        Assert.True(MixBatchLeasePolicy.CanClaim(batch, now));
    }

    [Fact]
    public void MixPlan_WhenThereAre21StemsIncludingClickAndGuide_Creates38Mixes()
    {
        var instruments = Enumerable.Range(0, 19).Select(index => new AudioTrack(Guid.NewGuid(),
            $"{index}.wav", $"Instrument {index}", "Other", "Other"));
        var click = new AudioTrack(Guid.NewGuid(), "c.wav", "Metronome", "Клик", "Click");
        var guide = new AudioTrack(Guid.NewGuid(), "g.wav", "Directions", "Гайд", "Guide");
        var plans = MixPlanFactory.Create([.. instruments, click, guide]);
        Assert.Equal(38, plans.Count);
        Assert.All(plans, plan => Assert.Contains(plan.Inputs, input => input.Track == click));
        Assert.DoesNotContain(plans, plan => plan.Target == click || plan.Target == guide);
    }

    [Fact]
    public void MixPlan_UsesStemKindInsteadOfGuessingSupportTracksByName()
    {
        var instrument = new AudioTrack(Guid.NewGuid(), "guitar.wav", "Click Guitar", "Гитары", "Guitar");
        var click1 = new AudioTrack(Guid.NewGuid(), "c1.wav", "Metronome 1", "Клик", "Click");
        var click2 = new AudioTrack(Guid.NewGuid(), "c2.wav", "Metronome 2", "Клик", "Click");
        var plans = MixPlanFactory.Create([instrument, click1, click2]);
        Assert.Equal(2, plans.Count);
        var focus = plans.Single(plan => plan.Kind == MixPlanKind.Focus);
        Assert.All(focus.Inputs, input => Assert.Equal(0, input.GainDb));
    }

    [Fact]
    public void MixPlan_WhenStemNamesMatch_OutputPathsRemainDistinct()
    {
        var plans = MixPlanFactory.Create([
            new AudioTrack(Guid.NewGuid(), "one.wav", "Guitar", "Гитары"),
            new AudioTrack(Guid.NewGuid(), "two.wav", "Guitar", "Гитары")]);
        Assert.Equal(plans.Count, plans.Select(plan => plan.Name).Distinct().Count());
    }

    [Fact]
    public void Progress_CountsOneMixOnlyAfterBothFormatsAreCommitted()
    {
        var target = new AudioTrack(Guid.NewGuid(), "guitar.wav", "Guitar", "Гитары");
        var plans = MixPlanFactory.Create([target]);
        var outputs = new HashSet<string> {
            MixProgress.Key(MixArtifactKind.Full, null, "mp3"),
            MixProgress.Key(MixArtifactKind.Full, null, "wav"),
            MixProgress.Key(MixArtifactKind.Focus, target.StemId, "wav"),
        };
        Assert.Equal(0, MixProgress.CountCompleted(plans, outputs));
        outputs.Add(MixProgress.Key(MixArtifactKind.Focus, target.StemId, "mp3"));
        Assert.Equal(1, MixProgress.CountCompleted(plans, outputs));
        outputs.Add(MixProgress.Key(MixArtifactKind.Minus, target.StemId, "wav"));
        outputs.Add(MixProgress.Key(MixArtifactKind.Minus, target.StemId, "mp3"));
        Assert.Equal(2, MixProgress.CountCompleted(plans, outputs));
    }

    [Fact]
    public void ArtifactIdentity_IsStableAcrossRetriesAndDistinctAcrossFormats()
    {
        var batch = Guid.NewGuid(); var stem = Guid.NewGuid();
        var id = MixProgress.ArtifactId(batch, MixArtifactKind.Focus, stem, "mp3");
        Assert.Equal(id, MixProgress.ArtifactId(batch, MixArtifactKind.Focus, stem, "MP3"));
        Assert.NotEqual(id, MixProgress.ArtifactId(batch, MixArtifactKind.Focus, stem, "wav"));
        Assert.NotEqual(id, MixProgress.ArtifactId(batch, MixArtifactKind.Minus, stem, "mp3"));
        Assert.NotEqual(id, MixProgress.ArtifactId(Guid.NewGuid(), MixArtifactKind.Focus, stem, "mp3"));
    }

    [Fact]
    public void LeasePolicy_DelaysTransientRetryUntilScheduledTime()
    {
        var now = DateTimeOffset.UtcNow;
        var batch = new MixBatch { Status = MixBatchStatus.Queued, NextAttemptAt = now.AddSeconds(20) };
        Assert.False(MixBatchLeasePolicy.CanClaim(batch, now));
        Assert.True(MixBatchLeasePolicy.CanClaim(batch, now.AddSeconds(20)));
    }

    [Fact]
    public void RetryPolicy_WhenRollbackWrapsConnectionFailure_RetriesOriginalFault()
    {
        var exception = new ObjectDisposedException("NpgsqlTransaction",
            new Npgsql.NpgsqlException("Exception while reading from stream", new EndOfStreamException()));
        Assert.True(MixRetryPolicy.IsTransient(exception));
        Assert.False(MixRetryPolicy.IsTransient(new InvalidOperationException("No instrument stems found")));
    }
}
