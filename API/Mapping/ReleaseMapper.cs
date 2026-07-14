using API.Data;
using Cdbaby.Demo.V1beta1;
using Google.Protobuf.WellKnownTypes;
using Riok.Mapperly.Abstractions;

namespace API.Mapping;

[Mapper]
public static partial class ReleaseMapper
{
    [MapProperty(nameof(Release.ReleaseDate), nameof(ReleaseEntity.ReleaseDate))]
    [MapProperty(nameof(Release.CreateTime), nameof(ReleaseEntity.CreatedAt))]
    [MapProperty(nameof(Release.UpdateTime), nameof(ReleaseEntity.UpdatedAt))]
    public static partial ReleaseEntity ToEntity(Release proto);

    [MapProperty(nameof(ReleaseEntity.ReleaseDate), nameof(Release.ReleaseDate))]
    [MapProperty(nameof(ReleaseEntity.CreatedAt), nameof(Release.CreateTime))]
    [MapProperty(nameof(ReleaseEntity.UpdatedAt), nameof(Release.UpdateTime))]
    public static partial Release ToProto(ReleaseEntity entity);

    private static DateTime TimestampToDateTime(Timestamp? ts)
        => ts?.ToDateTime() ?? DateTime.MinValue;

    private static Timestamp DateTimeToTimestamp(DateTime dt)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
}
