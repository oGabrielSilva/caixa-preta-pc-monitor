#:project ../../../tests/PcMonitor.Tests.Support/PcMonitor.Tests.Support.csproj
// File-based apps default to Native AOT; Dapper (used by the stores) needs dynamic code.
#:property PublishAot=false

// Writes the demo database used only for the landing page's incident screenshot (see README.md in this folder):
// a recording that ends in an unexpected shutdown while the +12 V rail sags under a game load, diagnosed by the real
// engine. Synthetic data: the screenshot taken from it is labelled "sample data". Nothing here names a real machine
// or user (no 1074 event).
//
//   dotnet run src/PcMonitor.Site/screenshots/DemoDatabase.cs -- <path-to-new.db>
using PcMonitor.Domain.Incidents;
using PcMonitor.Infrastructure.Persistence.Sqlite;
using PcMonitor.Tests.Support;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: dotnet run DemoDatabase.cs -- <path-to-new.db>");
    return 1;
}

var path = Path.GetFullPath(args[0]);
foreach (var file in new[] { path, $"{path}-wal", $"{path}-shm" })
{
    File.Delete(file);
}

var database = new SqliteDatabase(path);
new SchemaMigrator(database).Migrate();

// A Tuesday evening: 10 minutes at the desktop, then 5 minutes of a game until the PC goes dark.
var start = new DateTimeOffset(2026, 11, 24, 21, 30, 0, TimeSpan.FromHours(-3));
const int IdleSeconds = 600;
const int GameSeconds = 300;

// One frame: CPU °C, load %, package W, +12 V, +5 V, +3.3 V, fan RPM (the order of DatabaseSeed.Catalog).
static float[] Reading(int second)
{
    // Deterministic jitter in [-1, 1] (same database on every run), over a slow drift.
    static float Noise(int n, float salt)
    {
        var fraction = ((MathF.Sin((n * 12.9898f) + salt) * 43758.547f % 1f) + 1f) % 1f;
        return (2f * fraction) - 1f;
    }

    var wobble = (0.4f * MathF.Sin(second / 23f)) + (0.6f * Noise(second, 0f));
    if (second < IdleSeconds)
    {
        return [44f + (2f * wobble), 7f + (3f * MathF.Abs(wobble)), 21f + (4f * wobble), 12.19f, 5.00f, 3.296f, 980f];
    }

    var inGame = second - IdleSeconds;
    var warmUp = MathF.Min(1f, inGame / 90f);
    var toEnd = (IdleSeconds + GameSeconds) - second;

    // The rail holds at about 12.1 V, then sags in the last 40 s and drops below the ATX minimum (11.40 V) at the end.
    var rail12 = toEnd > 40 ? 12.10f - (0.02f * MathF.Abs(wobble)) : 12.10f - ((40 - toEnd) * 0.022f);
    return [
        46f + (24f * warmUp) + (1.5f * wobble),
        84f + (9f * Noise(second, 3.1f)) + (4f * MathF.Sin(second / 11f)),
        24f + (64f * warmUp) + (5f * wobble),
        MathF.Round(rail12, 2),
        4.98f - (toEnd <= 40 ? 0.02f : 0f),
        3.29f,
        980f + (900f * warmUp),
    ];
}

using (var seed = new DatabaseSeed(database))
{
    var session = seed.StartSession(start);
    var segment = seed.StartSegment(session, start);
    seed.Frames(segment, start, IdleSeconds + GameSeconds, Reading);

    var lastFrame = start.AddSeconds(IdleSeconds + GameSeconds - 1);
    var loggedAt = lastFrame.AddMinutes(2);
    var incident = seed.Incident(
        41, loggedAt, FailureTimeSource.LastFrame, lastFrame, session, segment, lastFrame, 0,
        new SystemEvent("EventLog", 6008, 50, loggedAt.AddSeconds(5), 2, new Dictionary<string, string>()),
        new SystemEvent("EventLog", 6005, 52, loggedAt.AddSeconds(6), 4, new Dictionary<string, string>()));
    var diagnosis = seed.Diagnose(incident, TimeProvider.System);
    Console.WriteLine($"Demo database written to {path}");
    Console.WriteLine($"Incident {incident}: {diagnosis.Confidence}, ranking {string.Join(", ", diagnosis.Ranking.Select(r => $"{r.Hypothesis} {r.Score}"))}");
}

Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
return 0;
