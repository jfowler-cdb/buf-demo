using API.Data;
using API.Mapping;
using Cdbaby.Demo.V1beta1;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class TrackServiceImpl : TrackService.TrackServiceBase
{
    private readonly ReleasesDbContext _db;

    public TrackServiceImpl(ReleasesDbContext db)
    {
        _db = db;
    }

    public override async Task<GetTrackResponse> GetTrack(GetTrackRequest request, ServerCallContext context)
    {
        var entity = await _db.Tracks.Include(t => t.ReleaseTracks).FirstOrDefaultAsync(t => t.Id == request.Id);
        if (entity is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Track '{request.Id}' not found."));

        return new GetTrackResponse { Track = ToProtoWithReleaseIds(entity) };
    }

    public override async Task<ListTracksResponse> ListTracks(ListTracksRequest request, ServerCallContext context)
    {
        var pageSize = request.PageSize > 0 ? request.PageSize : 50;

        int startIndex = 0;
        if (!string.IsNullOrEmpty(request.PageToken) && int.TryParse(request.PageToken, out var offset))
            startIndex = offset;

        IQueryable<TrackEntity> query = _db.Tracks.Include(t => t.ReleaseTracks);

        if (!string.IsNullOrEmpty(request.ReleaseId))
            query = query.Where(t => t.ReleaseTracks.Any(rt => rt.ReleaseId == request.ReleaseId));

        var totalCount = await query.CountAsync();
        var entities = await query
            .OrderBy(t => t.Title)
            .Skip(startIndex)
            .Take(pageSize)
            .ToListAsync();

        var response = new ListTracksResponse();
        response.Tracks.AddRange(entities.Select(ToProtoWithReleaseIds));

        if (startIndex + pageSize < totalCount)
            response.NextPageToken = (startIndex + pageSize).ToString();

        return response;
    }

    public override async Task<CreateTrackResponse> CreateTrack(CreateTrackRequest request, ServerCallContext context)
    {
        var entity = TrackMapper.ToEntity(request.Track);
        entity.Id = Guid.NewGuid().ToString();
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await SyncReleaseTracks(entity, request.Track.ReleaseIds);

        _db.Tracks.Add(entity);
        await _db.SaveChangesAsync();

        return new CreateTrackResponse { Track = ToProtoWithReleaseIds(entity) };
    }

    public override async Task<UpdateTrackResponse> UpdateTrack(UpdateTrackRequest request, ServerCallContext context)
    {
        var incoming = TrackMapper.ToEntity(request.Track);

        var existing = await _db.Tracks.Include(t => t.ReleaseTracks).FirstOrDefaultAsync(t => t.Id == incoming.Id);
        if (existing is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Track '{incoming.Id}' not found."));

        existing.Title = incoming.Title;
        existing.Artist = incoming.Artist;
        existing.Duration = incoming.Duration;
        existing.TrackNumber = incoming.TrackNumber;
        existing.Isrc = incoming.Isrc;
        existing.UpdatedAt = DateTime.UtcNow;

        await SyncReleaseTracks(existing, request.Track.ReleaseIds);
        await _db.SaveChangesAsync();

        return new UpdateTrackResponse { Track = ToProtoWithReleaseIds(existing) };
    }

    public override async Task<DeleteTrackResponse> DeleteTrack(DeleteTrackRequest request, ServerCallContext context)
    {
        var entity = await _db.Tracks.Include(t => t.ReleaseTracks).FirstOrDefaultAsync(t => t.Id == request.Id);
        if (entity is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Track '{request.Id}' not found."));

        _db.Tracks.Remove(entity);
        await _db.SaveChangesAsync();

        return new DeleteTrackResponse { Track = ToProtoWithReleaseIds(entity) };
    }

    private static Track ToProtoWithReleaseIds(TrackEntity entity)
    {
        var proto = TrackMapper.ToProto(entity);
        proto.ReleaseIds.AddRange(entity.ReleaseTracks.Select(rt => rt.ReleaseId));
        return proto;
    }

    private async Task SyncReleaseTracks(TrackEntity entity, IEnumerable<string> releaseIds)
    {
        var ids = releaseIds.ToHashSet();

        // Validate that all referenced releases exist.
        if (ids.Count > 0)
        {
            var existingCount = await _db.Releases.CountAsync(r => ids.Contains(r.Id));
            if (existingCount != ids.Count)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "One or more release IDs do not exist."));
        }

        entity.ReleaseTracks.Clear();
        foreach (var releaseId in ids)
            entity.ReleaseTracks.Add(new ReleaseTrackEntity { ReleaseId = releaseId, TrackId = entity.Id });
    }
}
