using System.Globalization;

namespace PcMonitor.Infrastructure.Windows.EventLog;

/// <summary>XPath filters for the System log.</summary>
internal static class EventLogQueries
{
    public const string SystemLog = "System";

    private const string KernelPower = "Microsoft-Windows-Kernel-Power";

    /// <summary>Providers and event ids that help explain a failure.</summary>
    private const string RelatedEvents =
        "(Provider[@Name='EventLog'] and (EventID=6005 or EventID=6006 or EventID=6008))" +
        " or (Provider[@Name='User32'] and (EventID=1074 or EventID=1076))" +
        " or (Provider[@Name='Microsoft-Windows-WER-SystemErrorReporting'] and EventID=1001)" +
        $" or (Provider[@Name='{KernelPower}'] and (EventID=41 or EventID=42 or EventID=107 or EventID=109))" +
        " or Provider[@Name='Microsoft-Windows-WHEA-Logger']";

    public static string UnexpectedShutdowns(DateTimeOffset? after) =>
        $"*[System[Provider[@Name='{KernelPower}'] and EventID=41{After(after)}]]";

    public static string ShutdownReports(DateTimeOffset? after) =>
        $"*[System[Provider[@Name='EventLog'] and EventID=6008{After(after)}]]";

    public static string Related(DateTimeOffset since, DateTimeOffset until) =>
        $"*[System[TimeCreated[@SystemTime>='{Format(since)}' and @SystemTime<='{Format(until)}'] and ({RelatedEvents})]]";

    private static string After(DateTimeOffset? after) =>
        after is { } time ? $" and TimeCreated[@SystemTime>'{Format(time)}']" : string.Empty;

    private static string Format(DateTimeOffset time) =>
        time.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
}
