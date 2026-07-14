using API.Data;
using API.Interceptors;
using API.Seeding;
using API.Services;
using Cdbaby.Demo.V1beta1;
using Microsoft.EntityFrameworkCore;
using ProtoValidate;

var builder = WebApplication.CreateBuilder(args);
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
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Grpc-Status", "Grpc-Message", "Grpc-Encoding", "Grpc-Accept-Encoding");
    });
});

var app = builder.Build();

// Handle `dotnet run -- seed` command
if (args.Length > 0 && args[0] == "seed")
{
    var seedPath = args.Length > 1 ? args[1] : Path.Combine("..", "seed", "releases.yaml");
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ReleasesDbContext>();
    await Seeder.RunAsync(db, seedPath);
    return;
}

app.UseCors();
app.UseGrpcWeb();
app.MapGrpcService<ReleaseServiceImpl>().EnableGrpcWeb();

app.Run();
