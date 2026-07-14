using API.Data;
using API.Services;
using Cdbaby.Demo.V1beta1;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace API.Tests;

public class TrackServiceImplTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ReleasesDbContext _db;
    private readonly TrackServiceImpl _sut;
    private readonly ReleaseServiceImpl _releaseSvc;
    private readonly ServerCallContext _ctx = Substitute.For<ServerCallContext>();

    public TrackServiceImplTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ReleasesDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ReleasesDbContext(options);
        _db.Database.EnsureCreated();
        _sut = new TrackServiceImpl(_db);
        _releaseSvc = new ReleaseServiceImpl(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static Track MakeTrack(string title = "Track One", string artist = "Artist") =>
        new()
        {
            Title = title,
            Artist = artist,
            Duration = Duration.FromTimeSpan(TimeSpan.FromMinutes(3).Add(TimeSpan.FromSeconds(45))),
            TrackNumber = 1,
            Isrc = "USRC17600001",
        };

    private async Task<Track> CreateAndReturn(string title = "Track One", string artist = "Artist")
    {
        var resp = await _sut.CreateTrack(
            new CreateTrackRequest { Track = MakeTrack(title, artist) }, _ctx);
        return resp.Track;
    }

    private async Task<string> CreateRelease(string title = "Album")
    {
        var resp = await _releaseSvc.CreateRelease(new CreateReleaseRequest
        {
            Release = new Release
            {
                Title = title,
                Artist = "Artist",
                ReleaseDate = Timestamp.FromDateTime(DateTime.UtcNow),
            }
        }, _ctx);
        return resp.Release.Id;
    }

    // ── CreateTrack ────────────────────────────────────────────

    [Fact]
    public async Task CreateTrack_AssignsUuidAndTimestamps()
    {
        var created = await CreateAndReturn();

        Assert.True(Guid.TryParse(created.Id, out _));
        Assert.Equal("Track One", created.Title);
        Assert.Equal(225, (int)created.Duration.Seconds); // 3m45s
        Assert.Equal(1, created.TrackNumber);
        Assert.NotNull(created.CreateTime);
        Assert.NotNull(created.UpdateTime);
    }

    [Fact]
    public async Task CreateTrack_WithReleaseIds_LinksToReleases()
    {
        var releaseId = await CreateRelease();

        var track = MakeTrack();
        track.ReleaseIds.Add(releaseId);
        var resp = await _sut.CreateTrack(new CreateTrackRequest { Track = track }, _ctx);

        Assert.Single(resp.Track.ReleaseIds);
        Assert.Equal(releaseId, resp.Track.ReleaseIds[0]);
    }

    [Fact]
    public async Task CreateTrack_WithInvalidReleaseId_ThrowsInvalidArgument()
    {
        var track = MakeTrack();
        track.ReleaseIds.Add(Guid.NewGuid().ToString());

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.CreateTrack(new CreateTrackRequest { Track = track }, _ctx));

        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    // ── GetTrack ───────────────────────────────────────────────

    [Fact]
    public async Task GetTrack_ReturnsTrack_WhenExists()
    {
        var created = await CreateAndReturn();

        var resp = await _sut.GetTrack(new GetTrackRequest { Id = created.Id }, _ctx);

        Assert.Equal(created.Id, resp.Track.Id);
        Assert.Equal("Track One", resp.Track.Title);
    }

    [Fact]
    public async Task GetTrack_ThrowsNotFound_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.GetTrack(new GetTrackRequest { Id = Guid.NewGuid().ToString() }, _ctx));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    // ── ListTracks ─────────────────────────────────────────────

    [Fact]
    public async Task ListTracks_ReturnsEmpty_WhenNoTracks()
    {
        var resp = await _sut.ListTracks(new ListTracksRequest(), _ctx);

        Assert.Empty(resp.Tracks);
    }

    [Fact]
    public async Task ListTracks_FiltersByReleaseId()
    {
        var releaseId = await CreateRelease();

        var track1 = MakeTrack("Linked");
        track1.ReleaseIds.Add(releaseId);
        await _sut.CreateTrack(new CreateTrackRequest { Track = track1 }, _ctx);
        await CreateAndReturn("Unlinked");

        var resp = await _sut.ListTracks(new ListTracksRequest { ReleaseId = releaseId }, _ctx);

        Assert.Single(resp.Tracks);
        Assert.Equal("Linked", resp.Tracks[0].Title);
    }

    [Fact]
    public async Task ListTracks_Paginates()
    {
        for (int i = 0; i < 3; i++)
            await CreateAndReturn($"T{i}");

        var page1 = await _sut.ListTracks(new ListTracksRequest { PageSize = 2 }, _ctx);
        Assert.Equal(2, page1.Tracks.Count);
        Assert.NotEmpty(page1.NextPageToken);

        var page2 = await _sut.ListTracks(new ListTracksRequest { PageSize = 2, PageToken = page1.NextPageToken }, _ctx);
        Assert.Single(page2.Tracks);
        Assert.Empty(page2.NextPageToken);
    }

    // ── UpdateTrack ────────────────────────────────────────────

    [Fact]
    public async Task UpdateTrack_UpdatesFieldsAndReleases()
    {
        var releaseId = await CreateRelease();
        var created = await CreateAndReturn();

        var updated = MakeTrack();
        updated.Id = created.Id;
        updated.Title = "Updated";
        updated.ReleaseIds.Add(releaseId);
        var resp = await _sut.UpdateTrack(new UpdateTrackRequest { Track = updated }, _ctx);

        Assert.Equal("Updated", resp.Track.Title);
        Assert.Single(resp.Track.ReleaseIds);
    }

    [Fact]
    public async Task UpdateTrack_ThrowsNotFound_WhenMissing()
    {
        var track = MakeTrack();
        track.Id = Guid.NewGuid().ToString();
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.UpdateTrack(new UpdateTrackRequest { Track = track }, _ctx));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    // ── DeleteTrack ────────────────────────────────────────────

    [Fact]
    public async Task DeleteTrack_RemovesAndReturnsTrack()
    {
        var created = await CreateAndReturn();

        var resp = await _sut.DeleteTrack(new DeleteTrackRequest { Id = created.Id }, _ctx);
        Assert.Equal(created.Id, resp.Track.Id);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.GetTrack(new GetTrackRequest { Id = created.Id }, _ctx));
        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteTrack_ThrowsNotFound_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.DeleteTrack(new DeleteTrackRequest { Id = Guid.NewGuid().ToString() }, _ctx));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
