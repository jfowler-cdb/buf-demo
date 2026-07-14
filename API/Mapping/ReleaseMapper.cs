using API.Data;
using Cdbaby.Demo.V1beta1;
using Google.Protobuf.WellKnownTypes;
using Riok.Mapperly.Abstractions;

namespace API.Mapping;

[Mapper]
public static partial class ReleaseMapper
{
    [MapProperty(nameof(Release.ReleaseDate), nameof(ReleaseEntity.ReleaseDate))]
    public static partial ReleaseEntity ToEntity(Release proto);

    [MapProperty(nameof(ReleaseEntity.ReleaseDate), nameof(Release.ReleaseDate))]
    public static partial Release ToProto(ReleaseEntity entity);

    private static DateTime TimestampToDateTime(Timestamp? ts)
        => ts?.ToDateTime() ?? DateTime.MinValue;

    private static Timestamp DateTimeToTimestamp(DateTime dt)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
}
