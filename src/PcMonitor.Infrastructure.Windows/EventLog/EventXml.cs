using System.Globalization;
using System.Xml.Linq;
using PcMonitor.Domain.Incidents;

namespace PcMonitor.Infrastructure.Windows.EventLog;

/// <summary>Converts the XML of a Windows event log record into a <see cref="SystemEvent"/>.</summary>
public static class EventXml
{
    private static readonly XNamespace _ns = "http://schemas.microsoft.com/win/2004/08/events/event";

    /// <summary>
    /// Named <c>Data</c> elements are keyed by name, unnamed ones by position (<c>"0"</c>, <c>"1"</c>…), and the
    /// <c>Binary</c> element under <c>"Binary"</c>. Events that use <c>UserData</c> are keyed by element name.
    /// </summary>
    public static SystemEvent Parse(string xml)
    {
        var root = XDocument.Parse(xml).Root ?? throw new FormatException("Empty event XML.");
        var system = root.Element(_ns + "System") ?? throw new FormatException("Event XML without <System>.");

        return new SystemEvent(
            Provider: system.Element(_ns + "Provider")?.Attribute("Name")?.Value ?? string.Empty,
            EventId: int.Parse(system.Element(_ns + "EventID")!.Value, CultureInfo.InvariantCulture),
            RecordId: long.Parse(system.Element(_ns + "EventRecordID")!.Value, CultureInfo.InvariantCulture),
            LoggedAt: DateTimeOffset.Parse(
                system.Element(_ns + "TimeCreated")!.Attribute("SystemTime")!.Value, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal),
            Level: int.TryParse(system.Element(_ns + "Level")?.Value, CultureInfo.InvariantCulture, out var level) ? level : 0,
            Data: ReadData(root));
    }

    private static Dictionary<string, string> ReadData(XElement root)
    {
        var data = new Dictionary<string, string>(StringComparer.Ordinal);
        if (root.Element(_ns + "EventData") is { } eventData)
        {
            var position = 0;
            foreach (var element in eventData.Elements(_ns + "Data"))
            {
                data[element.Attribute("Name")?.Value ?? position.ToString(CultureInfo.InvariantCulture)] = element.Value;
                position++;
            }

            if (eventData.Element(_ns + "Binary") is { } binary)
            {
                data["Binary"] = binary.Value;
            }
        }
        else if (root.Element(_ns + "UserData") is { } userData)
        {
            foreach (var leaf in userData.Descendants().Where(e => !e.HasElements))
            {
                data[leaf.Name.LocalName] = leaf.Value;
            }
        }

        return data;
    }
}
