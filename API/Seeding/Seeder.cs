using API.Data;
using Microsoft.EntityFrameworkCore;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace API.Seeding;

public static class Seeder
{
    public static async Task RunAsync(ReleasesDbContext db, string seedDir)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        await SeedReleases(db, deserializer, Path.Combine(seedDir, "releases.yaml"));
        await SeedTracks(db, deserializer, Path.Combine(seedDir, "tracks.yaml"));
    }

    private static async Task SeedReleases(ReleasesDbContext db, IDeserializer deserializer, string path)
    {
        if (!File.Exists(path)) return;

        var data = deserializer.Deserialize<ReleaseSeedData>(await File.ReadAllTextAsync(path));
        if (data.Releases is null || data.Releases.Count == 0) return;

        var existing = await db.Releases
            .Select(r => r.Title + "|" + r.Artist)
            .ToHashSetAsync();
        var toInsert = new List<ReleaseEntity>();

        foreach (var r in data.Releases)
        {
            var naturalKey = $"{r.Title}|{r.Artist}";
            if (existing.Contains(naturalKey))
                continue;

            var now = DateTime.UtcNow;
            toInsert.Add(new ReleaseEntity
            {
                Id = Guid.NewGuid().ToString(),
                Title = r.Title,
                Artist = r.Artist,
                Label = r.Label ?? string.Empty,
                ReleaseDate = DateTime.SpecifyKind(DateTime.Parse(r.ReleaseDate), DateTimeKind.Utc),
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        if (toInsert.Count > 0)
        {
            db.Releases.AddRange(toInsert);
            await db.SaveChangesAsync();
        }
        Console.WriteLine($"Releases: seeded {toInsert.Count}, skipped {data.Releases.Count - toInsert.Count}.");
    }

    private static async Task SeedTracks(ReleasesDbContext db, IDeserializer deserializer, string path)
    {
        if (!File.Exists(path)) return;

        var data = deserializer.Deserialize<TrackSeedData>(await File.ReadAllTextAsync(path));
        if (data.Tracks is null || data.Tracks.Count == 0) return;

        // Build release title -> ID lookup
        var releaseLookup = await db.Releases
            .ToDictionaryAsync(r => r.Title, r => r.Id);

        var existing = await db.Tracks
            .Select(t => t.Title + "|" + t.Artist)
            .ToHashSetAsync();
        var toInsert = new List<TrackEntity>();

        foreach (var t in data.Tracks)
        {
            var naturalKey = $"{t.Title}|{t.Artist}";
            if (existing.Contains(naturalKey))
                continue;

            var now = DateTime.UtcNow;
            var entity = new TrackEntity
            {
                Id = Guid.NewGuid().ToString(),
                Title = t.Title,
                Artist = t.Artist,
                Duration = TimeSpan.FromSeconds(t.DurationSeconds),
                TrackNumber = t.TrackNumber,
                Isrc = t.Isrc ?? string.Empty,
                CreatedAt = now,
                UpdatedAt = now,
            };

            foreach (var releaseTitle in t.Releases ?? [])
            {
                if (releaseLookup.TryGetValue(releaseTitle, out var releaseId))
                    entity.ReleaseTracks.Add(new ReleaseTrackEntity { ReleaseId = releaseId, TrackId = entity.Id });
                else
                    Console.WriteLine($"  Warning: release '{releaseTitle}' not found for track '{t.Title}'");
            }

            toInsert.Add(entity);
        }

        if (toInsert.Count > 0)
        {
            db.Tracks.AddRange(toInsert);
            await db.SaveChangesAsync();
        }
        Console.WriteLine($"Tracks: seeded {toInsert.Count}, skipped {data.Tracks.Count - toInsert.Count}.");
    }

    private class ReleaseSeedData
    {
        public List<SeedRelease> Releases { get; set; } = [];
    }

    private class TrackSeedData
    {
        public List<SeedTrack> Tracks { get; set; } = [];
    }

    private class SeedRelease
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string ReleaseDate { get; set; } = string.Empty;
    }

    private class SeedTrack
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public int TrackNumber { get; set; }
        public string? Isrc { get; set; }
        public List<string>? Releases { get; set; }
    }
}
