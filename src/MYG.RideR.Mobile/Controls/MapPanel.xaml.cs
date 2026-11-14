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
}
