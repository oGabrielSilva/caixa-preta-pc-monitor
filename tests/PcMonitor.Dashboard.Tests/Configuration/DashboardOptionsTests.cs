using System.ComponentModel.DataAnnotations;
using PcMonitor.Dashboard.Configuration;

namespace PcMonitor.Dashboard.Tests.Configuration;

public sealed class DashboardOptionsTests
{
    [Theory]
    [InlineData(80)]
    [InlineData(70000)]
    public void Port_outside_range_fails_validation(int port)
    {
        var options = new DashboardOptions { Port = port };

        Validator.TryValidateObject(options, new ValidationContext(options), null, validateAllProperties: true)
            .ShouldBeFalse();
    }

    [Fact]
    public void Default_port_is_5280() => new DashboardOptions().Port.ShouldBe(5280);
}
