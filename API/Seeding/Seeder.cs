using API.Data;
using Microsoft.EntityFrameworkCore;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace API.Seeding;

public static class Seeder
{
    public static async Task RunAsync(ReleasesDbContext db, string yamlPath)
    {
        var yaml = await File.ReadAllTextAsync(yamlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var data = deserializer.Deserialize<SeedData>(yaml);
        if (data.Releases is null || data.Releases.Count == 0)
        {
            Console.WriteLine("No releases found in seed file.");
            return;
        }

        var existing = await db.Releases
            .Select(r => r.Title + "|" + r.Artist)
            .ToHashSetAsync();
        var toInsert = new List<ReleaseEntity>();

        foreach (var r in data.Releases)
        {
            var naturalKey = $"{r.Title}|{r.Artist}";
            if (existing.Contains(naturalKey))
                continue;

            toInsert.Add(new ReleaseEntity
            {
                Id = Guid.NewGuid().ToString(),
                Title = r.Title,
                Artist = r.Artist,
                Label = r.Label ?? string.Empty,
                ReleaseDate = DateTime.SpecifyKind(DateTime.Parse(r.ReleaseDate), DateTimeKind.Utc),
            });
        }

        if (toInsert.Count == 0)
        {
            Console.WriteLine("All releases already seeded. Nothing to do.");
            return;
        }

        db.Releases.AddRange(toInsert);
        await db.SaveChangesAsync();
        Console.WriteLine($"Seeded {toInsert.Count} releases ({data.Releases.Count - toInsert.Count} already existed).");
    }

    private class SeedData
    {
        public List<SeedRelease> Releases { get; set; } = [];
    }

    private class SeedRelease
    {
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string ReleaseDate { get; set; } = string.Empty;
    }
}
