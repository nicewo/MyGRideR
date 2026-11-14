using Mapsui.Tiling;

namespace MYG.RideR.Mobile.Controls;

public partial class MapPanel : ContentView
{
    public MapPanel()
    {
        InitializeComponent();
        // OSM tile policy: identifying User-Agent (set by CreateTileLayer) and visible attribution.
        Map.Map.Layers.Add(OpenStreetMap.CreateTileLayer("MYG-RideR/1.0"));
    }

    private async void LocateClicked(object? sender, EventArgs e)
    {
        try
        {
            var location = await Geolocation.Default.GetLocationAsync(
                new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10)));
            if (location is null)
            {
                return;
            }

            var (x, y) = Mapsui.Projections.SphericalMercator.FromLonLat(location.Longitude, location.Latitude);
            Map.Map.Navigator.CenterOnAndZoomTo(new Mapsui.MPoint(x, y), 10);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Konum", $"Konum alınamadı: {ex.Message}", "Tamam");
        }
    }

    private async void LayersClicked(object? sender, EventArgs e) =>
        await Shell.Current.DisplayAlert("Katmanlar", "Harita: OpenStreetMap. Ek katmanlar yakında.", "Tamam");
}
