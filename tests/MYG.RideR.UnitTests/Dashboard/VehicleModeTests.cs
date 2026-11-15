using MYG.RideR.Application.Dashboard;

namespace MYG.RideR.UnitTests.Dashboard;

public sealed class VehicleModeTests
{
    [Theory]
    [InlineData(VehicleMode.Walk, 20)]
    [InlineData(VehicleMode.Bicycle, 60)]
    [InlineData(VehicleMode.Motorcycle, 240)]
    [InlineData(VehicleMode.Car, 240)]
    public void MaxGaugeKmh_ReturnsScale(VehicleMode mode, int expected) =>
        Assert.Equal(expected, VehicleModes.MaxGaugeKmh(mode));

    [Fact]
    public void Default_IsMotorcycle() => Assert.Equal(VehicleMode.Motorcycle, VehicleModes.Default);
}
