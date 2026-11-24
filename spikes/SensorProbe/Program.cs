// Throwaway spike: lists every sensor LibreHardwareMonitor can read on this machine.
// Run from an elevated terminal; output is Markdown.
using System.Diagnostics;
using System.Globalization;
using System.Security.Principal;
using LibreHardwareMonitor.Hardware;

var inv = CultureInfo.InvariantCulture;
var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

var computer = new Computer
{
    IsCpuEnabled = true,
    IsMotherboardEnabled = true,
    IsMemoryEnabled = true,
};

var open = Stopwatch.StartNew();
computer.Open();
open.Stop();

var visitor = new UpdateVisitor();
var cycles = new List<double>();
for (var i = 0; i < 5; i++)
{
    var sw = Stopwatch.StartNew();
    computer.Accept(visitor);
    sw.Stop();
    cycles.Add(sw.Elapsed.TotalMilliseconds);
    Thread.Sleep(1000);
}

Console.WriteLine($"# Resultado do spike de sensores ({DateTime.Now:yyyy-MM-dd HH:mm})");
Console.WriteLine();
Console.WriteLine($"- Administrador: {(isAdmin ? "sim" : "não")}");
Console.WriteLine($"- Open(): {open.Elapsed.TotalMilliseconds.ToString("F0", inv)} ms");
Console.WriteLine($"- Ciclos de leitura (ms): {string.Join(", ", cycles.Select(c => c.ToString("F1", inv)))}");
Console.WriteLine();
Console.WriteLine("| Hardware | Identifier | Name | SensorType | Value | Min | Max |");
Console.WriteLine("|---|---|---|---|---|---|---|");

var nulls = new List<string>();
foreach (var hardware in computer.Hardware)
{
    Print(hardware);
}

Console.WriteLine();
Console.WriteLine("## Sensores sem valor");
Console.WriteLine();
Console.WriteLine(nulls.Count == 0 ? "- nenhum" : string.Join(Environment.NewLine, nulls.Select(n => $"- `{n}`")));

computer.Close();

void Print(IHardware hardware)
{
    foreach (var sensor in hardware.Sensors.OrderBy(s => s.SensorType).ThenBy(s => s.Index))
    {
        if (sensor.Value is null)
        {
            nulls.Add(sensor.Identifier.ToString());
        }

        Console.WriteLine(
            $"| {hardware.Name} | `{sensor.Identifier}` | {sensor.Name} | {sensor.SensorType} | {F(sensor.Value)} | {F(sensor.Min)} | {F(sensor.Max)} |");
    }

    foreach (var sub in hardware.SubHardware)
    {
        Print(sub);
    }
}

string F(float? v) => v?.ToString("F3", inv) ?? "—";

sealed class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer) => computer.Traverse(this);

    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var sub in hardware.SubHardware)
        {
            sub.Accept(this);
        }
    }

    public void VisitSensor(ISensor sensor) { }

    public void VisitParameter(IParameter parameter) { }
}
