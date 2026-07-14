using API.Data;
using API.Mapping;
using Cdbaby.Demo.V1beta1;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace API.Services;

public class ReleaseServiceImpl : ReleaseService.ReleaseServiceBase
{
    private readonly ReleasesDbContext _db;

    public ReleaseServiceImpl(ReleasesDbContext db)
    {
        _db = db;
    }

    public override async Task<GetReleaseResponse> GetRelease(GetReleaseRequest request, ServerCallContext context)
    {
        var entity = await _db.Releases.FindAsync(request.Id);
        if (entity is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Release '{request.Id}' not found."));

        return new GetReleaseResponse { Release = ReleaseMapper.ToProto(entity) };
    }

    public override async Task<ListReleasesResponse> ListReleases(ListReleasesRequest request, ServerCallContext context)
    {
        var pageSize = request.PageSize > 0 ? request.PageSize : 50;

        int startIndex = 0;
        if (!string.IsNullOrEmpty(request.PageToken) && int.TryParse(request.PageToken, out var offset))
            startIndex = offset;

        var totalCount = await _db.Releases.CountAsync();
        var entities = await _db.Releases
            .OrderBy(r => r.Title)
            .Skip(startIndex)
            .Take(pageSize)
            .ToListAsync();

        var response = new ListReleasesResponse();
        response.Releases.AddRange(entities.Select(ReleaseMapper.ToProto));

        if (startIndex + pageSize < totalCount)
            response.NextPageToken = (startIndex + pageSize).ToString();

        return response;
    }

    public override async Task<CreateReleaseResponse> CreateRelease(CreateReleaseRequest request, ServerCallContext context)
    {
        var entity = ReleaseMapper.ToEntity(request.Release);
        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = Guid.NewGuid().ToString();

        if (await _db.Releases.AnyAsync(r => r.Id == entity.Id))
            throw new RpcException(new Status(StatusCode.AlreadyExists, $"Release '{entity.Id}' already exists."));

        _db.Releases.Add(entity);
        await _db.SaveChangesAsync();

        return new CreateReleaseResponse { Release = ReleaseMapper.ToProto(entity) };
    }

    public override async Task<UpdateReleaseResponse> UpdateRelease(UpdateReleaseRequest request, ServerCallContext context)
    {
        var entity = ReleaseMapper.ToEntity(request.Release);

        var existing = await _db.Releases.FindAsync(entity.Id);
        if (existing is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Release '{entity.Id}' not found."));

        existing.Title = entity.Title;
        existing.Artist = entity.Artist;
        existing.Label = entity.Label;
        existing.ReleaseDate = entity.ReleaseDate;
        await _db.SaveChangesAsync();

        return new UpdateReleaseResponse { Release = ReleaseMapper.ToProto(existing) };
    }

    public override async Task<DeleteReleaseResponse> DeleteRelease(DeleteReleaseRequest request, ServerCallContext context)
    {
        var entity = await _db.Releases.FindAsync(request.Id);
        if (entity is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Release '{request.Id}' not found."));

        _db.Releases.Remove(entity);
        await _db.SaveChangesAsync();

        return new DeleteReleaseResponse { Release = ReleaseMapper.ToProto(entity) };
    }
}
