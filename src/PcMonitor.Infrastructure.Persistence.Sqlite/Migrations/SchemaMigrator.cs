using Dapper;

namespace PcMonitor.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Applies the embedded <c>Migrations/NNN_name.sql</c> scripts in order, each in its own transaction,
/// and records the version in <c>PRAGMA user_version</c>.
/// </summary>
public sealed class SchemaMigrator(SqliteDatabase database)
{
    private const string ResourcePrefix = "PcMonitor.Infrastructure.Persistence.Sqlite.Migrations.";

    /// <summary>The schema version this build creates and reads.</summary>
    public static int LatestVersion { get; } = Scripts().Max(s => s.Version);

    /// <summary>Brings the schema up to <paramref name="upTo"/> (default: the latest version) and returns the version.</summary>
    public int Migrate(int upTo = int.MaxValue)
    {
        using var connection = database.OpenWriter();
        var current = connection.ExecuteScalar<int>("PRAGMA user_version");

        foreach (var (version, script) in Scripts().Where(s => s.Version > current && s.Version <= upTo))
        {
            using var transaction = connection.BeginTransaction();
            connection.Execute(script, transaction: transaction);
            connection.Execute($"PRAGMA user_version = {version}", transaction: transaction);
            transaction.Commit();
            current = version;
        }

        return current;
    }

    private static IEnumerable<(int Version, string Script)> Scripts()
    {
        var assembly = typeof(SchemaMigrator).Assembly;
        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix, StringComparison.Ordinal) && name.EndsWith(".sql", StringComparison.Ordinal))
            .Select(name =>
            {
                var fileName = name[ResourcePrefix.Length..];
                var version = int.Parse(fileName[..fileName.IndexOf('_', StringComparison.Ordinal)], System.Globalization.CultureInfo.InvariantCulture);
                using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
                return (version, reader.ReadToEnd());
            })
            .OrderBy(script => script.version);
    }
}
