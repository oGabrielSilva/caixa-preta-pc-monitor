using Dapper;
using Microsoft.Data.Sqlite;
using PcMonitor.Domain.Monitoring;

namespace PcMonitor.Infrastructure.Persistence.Sqlite.Frames;

/// <summary>A stored frame: Unix milliseconds and the encoded values.</summary>
internal sealed record FrameRow(long TimestampMs, byte[] Values)
{
    public Frame ToFrame() => new(DateTimeOffset.FromUnixTimeMilliseconds(TimestampMs), FrameCodec.Decode(Values));

    /// <summary>Runs a query selecting <c>TimestampMs</c> and <c>Values</c> and decodes every frame.</summary>
    public static IReadOnlyList<Frame> Read(SqliteConnection connection, string sql, object parameters) =>
        [.. connection.Query<FrameRow>(sql, parameters).Select(row => row.ToFrame())];
}
