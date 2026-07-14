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

    private static Release MakeRelease(string title = "Album", string artist = "Artist") =>
        new()
        {
            Title = title,
            Artist = artist,
            Label = "Label",
            ReleaseDate = Timestamp.FromDateTime(DateTime.UtcNow)
        };

    private async Task<Release> CreateAndReturn(string title = "Album", string artist = "Artist")
    {
        var resp = await _sut.CreateRelease(
            new CreateReleaseRequest { Release = MakeRelease(title, artist) }, _ctx);
        return resp.Release;
    }

    // ── CreateRelease ──────────────────────────────────────────

    [Fact]
    public async Task CreateRelease_AssignsUuid()
    {
        var created = await CreateAndReturn();

        Assert.True(Guid.TryParse(created.Id, out _));
        Assert.Equal("Album", created.Title);
        Assert.NotNull(created.CreateTime);
        Assert.NotNull(created.UpdateTime);
    }

    [Fact]
    public async Task CreateRelease_GeneratesUniqueIds()
    {
        var a = await CreateAndReturn("A");
        var b = await CreateAndReturn("B");

        Assert.NotEqual(a.Id, b.Id);
    }

    // ── GetRelease ─────────────────────────────────────────────

    [Fact]
    public async Task GetRelease_ReturnsRelease_WhenExists()
    {
        var created = await CreateAndReturn();

        var resp = await _sut.GetRelease(new GetReleaseRequest { Id = created.Id }, _ctx);

        Assert.Equal(created.Id, resp.Release.Id);
        Assert.Equal("Album", resp.Release.Title);
    }

    [Fact]
    public async Task GetRelease_ThrowsNotFound_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.GetRelease(new GetReleaseRequest { Id = Guid.NewGuid().ToString() }, _ctx));

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
        await CreateAndReturn("B");
        await CreateAndReturn("A");

        var resp = await _sut.ListReleases(new ListReleasesRequest { PageSize = 10 }, _ctx);

        Assert.Equal(2, resp.Releases.Count);
        Assert.Equal("A", resp.Releases[0].Title); // ordered by title
        Assert.Empty(resp.NextPageToken);
    }

    [Fact]
    public async Task ListReleases_Paginates_WhenMoreThanPageSize()
    {
        for (int i = 0; i < 3; i++)
            await CreateAndReturn($"T{i}");

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
        var created = await CreateAndReturn();

        var updated = MakeRelease();
        updated.Id = created.Id;
        updated.Title = "Updated Title";
        var resp = await _sut.UpdateRelease(new UpdateReleaseRequest { Release = updated }, _ctx);

        Assert.Equal("Updated Title", resp.Release.Title);

        var fetched = await _sut.GetRelease(new GetReleaseRequest { Id = created.Id }, _ctx);
        Assert.Equal("Updated Title", fetched.Release.Title);
    }

    [Fact]
    public async Task UpdateRelease_ThrowsNotFound_WhenMissing()
    {
        var release = MakeRelease();
        release.Id = Guid.NewGuid().ToString();
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.UpdateRelease(new UpdateReleaseRequest { Release = release }, _ctx));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    // ── DeleteRelease ──────────────────────────────────────────

    [Fact]
    public async Task DeleteRelease_RemovesAndReturnsRelease()
    {
        var created = await CreateAndReturn();

        var resp = await _sut.DeleteRelease(new DeleteReleaseRequest { Id = created.Id }, _ctx);
        Assert.Equal(created.Id, resp.Release.Id);

        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.GetRelease(new GetReleaseRequest { Id = created.Id }, _ctx));
        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteRelease_ThrowsNotFound_WhenMissing()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() =>
            _sut.DeleteRelease(new DeleteReleaseRequest { Id = Guid.NewGuid().ToString() }, _ctx));

        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }
}
