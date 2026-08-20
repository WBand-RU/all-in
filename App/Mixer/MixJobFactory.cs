namespace MixerApp;

public static class MixJobFactory
{
    public static IReadOnlyList<MixJob> Create(TrackAssignments assignments, double foregroundDb, double backgroundDb)
    {
        var jobs = new List<MixJob>
        {
            new("full-mix", MixKind.Full, null,
                assignments.Tracks.Select(track => new TrackMixInput(track, foregroundDb)).ToArray())
        };

        foreach (var instrument in assignments.Instruments)
        {
            var focusInputs = assignments.Tracks.Select(track =>
            {
                var isForeground = track == instrument || track == assignments.ClickTrack || track == assignments.GuideTrack;
                return new TrackMixInput(track, isForeground ? foregroundDb : backgroundDb);
            }).ToArray();
            jobs.Add(new MixJob($"{instrument.OutputStem}__focus", MixKind.Focus, instrument, focusInputs));

            // Muting rather than dropping the stream preserves the common session duration.
            var minusInputs = assignments.Tracks.Select(track => track == instrument
                ? new TrackMixInput(track, foregroundDb, Muted: true)
                : new TrackMixInput(track, foregroundDb)).ToArray();
            jobs.Add(new MixJob($"{instrument.OutputStem}__minus", MixKind.Minus, instrument, minusInputs));
        }
        return jobs;
    }
}
