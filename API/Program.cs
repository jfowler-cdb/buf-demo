using API.Data;
using API.Interceptors;
using API.Seeding;
using API.Services;
using Cdbaby.Demo.V1beta1;
using Microsoft.EntityFrameworkCore;
using ProtoValidate;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    // HTTP/2 only — browser access goes through Go gateway
    options.ListenLocalhost(5000, o =>
    {
        o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
    });
});
builder.Services.AddDbContext<ReleasesDbContext>(options =>
    options.UseSqlite("Data Source=releases.db"));
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<ProtoValidateInterceptor>();
});
builder.Services.AddProtoValidate(options =>
{
    options.FileDescriptors = [ReleasesReflection.Descriptor];
});
var app = builder.Build();

// Handle `dotnet run -- seed` command
if (args.Length > 0 && args[0] == "seed")
{
    var seedDir = args.Length > 1 ? args[1] : Path.Combine("..", "seed");
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ReleasesDbContext>();
    await Seeder.RunAsync(db, seedDir);
    return;
}

app.MapGrpcService<ReleaseServiceImpl>();
app.MapGrpcService<TrackServiceImpl>();

app.Run();
