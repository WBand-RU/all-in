using MassTransit;
using MongoDB.Driver;
using Shared.Messages;
using SongService.Services;

namespace SongService.Handlers;

public class GetSongHandle()
{
    public async Task<GetSongResponse> Consume(GetSongRequest request, Repository repository)
    {
        var song = await repository.Songs.Find(s => s.Id == request.SongId).FirstOrDefaultAsync();

        if (song is null)
        {
            return new GetSongResponse(null, false);
        }

        return new GetSongResponse(song.BandId, true);
    }
}
