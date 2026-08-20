using MixerApp;

namespace Mixer.Tests;

public sealed class MixJobFactoryTests
{
    [Fact]
    public void Create_AssignsExpectedTracksAndGains()
    {
        var click = Track("click");
        var guide = Track("guide");
        var guitar = Track("guitar");
        var bass = Track("bass");
        var jobs = MixJobFactory.Create(new TrackAssignments([click, guide, guitar, bass], click, guide), 0, -40);

        Assert.Equal(5, jobs.Count);
        Assert.All(jobs[0].Inputs, input => Assert.Equal(0, input.GainDb));

        var guitarFocus = Assert.Single(jobs, job => job.Name == "guitar__focus");
        Assert.Equal(-40, guitarFocus.Inputs.Single(input => input.Track == bass).GainDb);
        Assert.All(guitarFocus.Inputs.Where(input => input.Track != bass), input => Assert.Equal(0, input.GainDb));

        var guitarMinus = Assert.Single(jobs, job => job.Name == "guitar__minus");
        Assert.True(guitarMinus.Inputs.Single(input => input.Track == guitar).Muted);
        Assert.All(guitarMinus.Inputs.Where(input => input.Track != guitar), input => Assert.False(input.Muted));
    }

    [Fact]
    public void Create_DoesNotCreateIndividualMixesForClickAndGuide()
    {
        var click = Track("click");
        var guide = Track("guide");
        var instrument = Track("piano");
        var jobs = MixJobFactory.Create(new TrackAssignments([click, guide, instrument], click, guide), 0, -40);

        Assert.Equal(["full-mix", "piano__focus", "piano__minus"], jobs.Select(job => job.Name));
    }

    private static AudioTrack Track(string name) => new($"{name}.wav", $"{name}.wav", name);
}
