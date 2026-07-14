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

public class ReleaseServiceImplTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ReleasesDbContext _db;
    private readonly ReleaseServiceImpl _sut;
    private readonly ServerCallContext _ctx = Substitute.For<ServerCallContext>();

    public ReleaseServiceImplTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ReleasesDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ReleasesDbContext(options);
        _db.Database.EnsureCreated();
        _sut = new ReleaseServiceImpl(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static Release MakeRelease(string id = "r1", string title = "Album", string artist = "Artist") =>
        new()
        {
            Id = id,
            Title = title,
            Artist = artist,
            Label = "Label",
            ReleaseDate = Timestamp.FromDateTime(DateTime.UtcNow)
        };

    // ── CreateRelease ──────────────────────────────────────────

    [Fact]
    public async Task CreateRelease_AssignsId_WhenEmpty()
    {
        var req = new CreateReleaseRequest { Release = MakeRelease(id: "") };

        var resp = await _sut.CreateRelease(req, _ctx);

        Assert.NotEmpty(resp.Release.Id);
        Assert.Equal("Album", resp.Release.Title);
    }

    [Fact]
    public async Task CreateRelease_PreservesId_WhenProvided()
    {
        var req = new CreateReleaseRequest { Release = MakeRelease(id: "custom-id") };

        var resp = await _sut.CreateRelease(req, _ctx);

        Assert.Equal("custom-id", resp.Release.Id);
    }

    [Fact]
    public async Task CreateRelease_ThrowsAlreadyExists_OnDuplicate()
    {
        var release = MakeRelease();
        await _sut.CreateRelease(new CreateReleaseRequest { Release = release }, _ctx);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.CreateRelease(new CreateReleaseRequest { Release = release }, _ctx));

        Assert.Equal(StatusCode.AlreadyExists, ex.StatusCode);
    }

    // ── GetRelease ─────────────────────────────────────────────

    [Fact]
    public async Task GetRelease_ReturnsRelease_WhenExists()
    {
        var release = MakeRelease();
        await _sut.CreateRelease(new CreateReleaseRequest { Release = release }, _ctx);

        var resp = await _sut.GetRelease(new GetReleaseRequest { Id = "r1" }, _ctx);

        Assert.Equal("r1", resp.Release.Id);
        Assert.Equal("Album", resp.Release.Title);
    }

    [Fact]
    public async Task GetRelease_ThrowsNotFound_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.GetRelease(new GetReleaseRequest { Id = "missing" }, _ctx));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    // ── ListReleases ───────────────────────────────────────────

    [Fact]
    public async Task ListReleases_ReturnsEmpty_WhenNoReleases()
    {
        var resp = await _sut.ListReleases(new ListReleasesRequest(), _ctx);

        Assert.Empty(resp.Releases);
        Assert.Empty(resp.NextPageToken);
    }

    [Fact]
    public async Task ListReleases_ReturnsAll_WhenWithinPageSize()
    {
        await _sut.CreateRelease(new CreateReleaseRequest { Release = MakeRelease("1", "B") }, _ctx);
        await _sut.CreateRelease(new CreateReleaseRequest { Release = MakeRelease("2", "A") }, _ctx);

        var resp = await _sut.ListReleases(new ListReleasesRequest { PageSize = 10 }, _ctx);

        Assert.Equal(2, resp.Releases.Count);
        Assert.Equal("A", resp.Releases[0].Title); // ordered by title
        Assert.Empty(resp.NextPageToken);
    }

    [Fact]
    public async Task ListReleases_Paginates_WhenMoreThanPageSize()
    {
        for (int i = 0; i < 3; i++)
            await _sut.CreateRelease(new CreateReleaseRequest { Release = MakeRelease($"r{i}", $"T{i}") }, _ctx);

        var page1 = await _sut.ListReleases(new ListReleasesRequest { PageSize = 2 }, _ctx);
        Assert.Equal(2, page1.Releases.Count);
        Assert.NotEmpty(page1.NextPageToken);

        var page2 = await _sut.ListReleases(new ListReleasesRequest { PageSize = 2, PageToken = page1.NextPageToken }, _ctx);
        Assert.Single(page2.Releases);
        Assert.Empty(page2.NextPageToken);
    }

    // ── UpdateRelease ──────────────────────────────────────────

    [Fact]
    public async Task UpdateRelease_UpdatesFields()
    {
        await _sut.CreateRelease(new CreateReleaseRequest { Release = MakeRelease() }, _ctx);

        var updated = MakeRelease();
        updated.Title = "Updated Title";
        var resp = await _sut.UpdateRelease(new UpdateReleaseRequest { Release = updated }, _ctx);

        Assert.Equal("Updated Title", resp.Release.Title);

        var fetched = await _sut.GetRelease(new GetReleaseRequest { Id = "r1" }, _ctx);
        Assert.Equal("Updated Title", fetched.Release.Title);
    }

    [Fact]
    public async Task UpdateRelease_ThrowsNotFound_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.UpdateRelease(new UpdateReleaseRequest { Release = MakeRelease("missing") }, _ctx));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    // ── DeleteRelease ──────────────────────────────────────────

    [Fact]
    public async Task DeleteRelease_RemovesAndReturnsRelease()
    {
        await _sut.CreateRelease(new CreateReleaseRequest { Release = MakeRelease() }, _ctx);

        var resp = await _sut.DeleteRelease(new DeleteReleaseRequest { Id = "r1" }, _ctx);
        Assert.Equal("r1", resp.Release.Id);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.GetRelease(new GetReleaseRequest { Id = "r1" }, _ctx));
        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteRelease_ThrowsNotFound_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.DeleteRelease(new DeleteReleaseRequest { Id = "missing" }, _ctx));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
