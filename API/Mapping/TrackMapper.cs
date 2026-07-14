using API.Data;
using Cdbaby.Demo.V1beta1;
using Google.Protobuf.WellKnownTypes;
using Riok.Mapperly.Abstractions;

namespace API.Mapping;

[Mapper]
public static partial class TrackMapper
{
    [MapperIgnoreSource(nameof(Track.ReleaseIds))]
    [MapperIgnoreTarget(nameof(TrackEntity.ReleaseTracks))]
    [MapProperty(nameof(Track.CreateTime), nameof(TrackEntity.CreatedAt))]
    [MapProperty(nameof(Track.UpdateTime), nameof(TrackEntity.UpdatedAt))]
    public static partial TrackEntity ToEntity(Track proto);

    [MapperIgnoreTarget(nameof(Track.ReleaseIds))]
    [MapperIgnoreSource(nameof(TrackEntity.ReleaseTracks))]
    [MapProperty(nameof(TrackEntity.CreatedAt), nameof(Track.CreateTime))]
    [MapProperty(nameof(TrackEntity.UpdatedAt), nameof(Track.UpdateTime))]
    public static partial Track ToProto(TrackEntity entity);

    private static TimeSpan DurationToTimeSpan(Duration? d)
        => d is not null ? d.ToTimeSpan() : TimeSpan.Zero;

    private static Duration TimeSpanToDuration(TimeSpan ts)
        => Duration.FromTimeSpan(ts);

    private static DateTime TimestampToDateTime(Timestamp? ts)
        => ts?.ToDateTime() ?? DateTime.MinValue;

    private static Timestamp DateTimeToTimestamp(DateTime dt)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
}
