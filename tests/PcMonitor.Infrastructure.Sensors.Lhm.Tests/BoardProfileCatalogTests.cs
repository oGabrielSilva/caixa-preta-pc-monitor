using PcMonitor.Domain.Sensors;
using PcMonitor.Infrastructure.Sensors.Lhm.Profiles;

namespace PcMonitor.Infrastructure.Sensors.Lhm.Tests;

public sealed class BoardProfileCatalogTests
{
    [Fact]
    public void Profile_is_found_by_board_product_ignoring_case_and_padding() =>
        BoardProfileCatalog.FindFor(" tuf gaming a620m-plus wifi ").ShouldNotBeNull()
            .BoardProduct.ShouldBe("TUF GAMING A620M-PLUS WIFI");

    [Theory]
    [InlineData("PRIME B650-PLUS")]
    [InlineData("")]
    [InlineData(null)]
    public void Unknown_board_has_no_profile(string? product) => BoardProfileCatalog.FindFor(product).ShouldBeNull();

    [Fact]
    public void Every_embedded_profile_parses_and_has_unique_keys_and_roles()
    {
        BoardProfileCatalog.All.ShouldNotBeEmpty();
        foreach (var profile in BoardProfileCatalog.All)
        {
            profile.Sensors.Select(s => s.Key).ShouldBeUnique();
            profile.Sensors.Where(s => s.Role != SensorRole.None).Select(s => s.Role).ShouldBeUnique();
        }
    }

    [Fact]
    public void A620M_profile_maps_the_rails_with_the_researched_multipliers()
    {
        var sensors = BoardProfileCatalog.FindFor("TUF GAMING A620M-PLUS WIFI")!.Sensors.ToDictionary(s => s.Role);

        sensors[SensorRole.Rail12V].ShouldBe(new ProfileSensor("/lpc/nct6799d/0/voltage/4", "+12V", SensorRole.Rail12V, 12));
        sensors[SensorRole.Rail5V].ShouldBe(new ProfileSensor("/lpc/nct6799d/0/voltage/1", "+5V", SensorRole.Rail5V, 5));
        sensors[SensorRole.Rail3V3].Multiplier.ShouldBe(1);
    }
}
