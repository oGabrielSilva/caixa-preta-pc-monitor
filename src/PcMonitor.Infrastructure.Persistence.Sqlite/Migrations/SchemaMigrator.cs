using Dapper;

namespace PcMonitor.Infrastructure.Persistence.Sqlite;

/// <summary>
/// Applies the embedded <c>Migrations/NNN_name.sql</c> scripts in order, each in its own transaction,
/// and records the version in <c>PRAGMA user_version</c>.
/// </summary>
public sealed class SchemaMigrator(SqliteDatabase database)
{
    private const string ResourcePrefix = "PcMonitor.Infrastructure.Persistence.Sqlite.Migrations.";

    /// <summary>Brings the schema to the latest version and returns it.</summary>
    public int Migrate()
    {
        using var connection = database.OpenWriter();
        var current = connection.ExecuteScalar<int>("PRAGMA user_version");

        foreach (var (version, script) in Scripts().Where(s => s.Version > current))
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
