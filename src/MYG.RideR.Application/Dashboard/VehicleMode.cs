namespace MYG.RideR.Application.Dashboard;

public enum VehicleMode
{
    Walk,
    Bicycle,
    Motorcycle,
    Car,
}

public static class VehicleModes
{
    public const VehicleMode Default = VehicleMode.Motorcycle;

    /// <summary>Göstergenin tam ölçek değeri (km/sa).</summary>
    public static int MaxGaugeKmh(VehicleMode mode) => mode switch
    {
        VehicleMode.Walk => 20,
        VehicleMode.Bicycle => 60,
        VehicleMode.Motorcycle => 240,
        VehicleMode.Car => 240,
        _ => 240,
    };
}
