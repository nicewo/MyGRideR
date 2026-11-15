using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Nts;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using NetTopologySuite.Geometries;
using Brush = Mapsui.Styles.Brush;
using Color = Mapsui.Styles.Color;
using Pen = Mapsui.Styles.Pen;

namespace MYG.RideR.Mobile.Controls;

public partial class MapPanel : ContentView
{
    private const double InitialResolution = 4;

    private readonly MemoryLayer _routeLayer;
    private readonly MemoryLayer _positionLayer;
    private bool _follow = true;
    private bool _programmatic;
    private bool _hasFix;
    private double _lastLat;
    private double _lastLon;

    public MapPanel()
    {
        InitializeComponent();

        // CARTO now requires an API key ("API KEY REQUIRED" tiles). Use OSM (identifying User-Agent per its
        // tile policy) and dim it over the dark map background so it fits the dark UI. Attribution stays visible.
        var osm = Mapsui.Tiling.OpenStreetMap.CreateTileLayer("MYG-RideR/1.0");
        osm.Opacity = 0.55;
        Map.Map.BackColor = Color.FromArgb(255, 0x0D, 0x13, 0x1A); // MapBackground; shows through the dimmed tiles
        Map.Map.Layers.Add(osm);

        _routeLayer = new MemoryLayer { Name = "Route", Style = null };
        _positionLayer = new MemoryLayer { Name = "Position", Style = null };
        Map.Map.Layers.Add(_routeLayer);
        Map.Map.Layers.Add(_positionLayer);

        // North-up map: two-finger rotation turned labels upside down on the dashboard.
        Map.Map.Navigator.RotationLock = true;
        Map.Map.Navigator.ViewportChanged += (_, _) =>
        {
            if (!_programmatic && _hasFix)
            {
                _follow = false;
            }
        };
    }

    public void UpdatePosition(double lat, double lon, double? courseDeg)
    {
        CoordinateLabel.Text = MYG.RideR.Application.Dashboard.LiveReadout.CoordinateText(lat, lon);
        _lastLat = lat;
        _lastLon = lon;
        var (x, y) = SphericalMercator.FromLonLat(lon, lat);
        var point = new MPoint(x, y);

        var feature = new PointFeature(point);
        feature.Styles.Add(new SymbolStyle
        {
            SymbolType = SymbolType.Ellipse,
            SymbolScale = 10 / 32.0, // ~10px
            Fill = new Brush(Color.FromArgb(255, 0, 191, 255)), // NeonBlue #00BFFF
            Outline = new Pen(Color.White, 2)
        });
        _positionLayer.Features = [feature];
        _positionLayer.DataHasChanged();

        if (!_hasFix)
        {
            _hasFix = true;
            Center(point, InitialResolution, zoom: true);
        }
        else if (_follow)
        {
            Center(point, null, zoom: false);
        }
    }

    public void SetRoute(IReadOnlyList<(double Lat, double Lon)> points)
    {
        if (points.Count < 2)
        {
            _routeLayer.Features = [];
            _routeLayer.DataHasChanged();
            return;
        }

        var coordinates = points
            .Select(p =>
            {
                var (x, y) = SphericalMercator.FromLonLat(p.Lon, p.Lat);
                return new Coordinate(x, y);
            })
            .ToArray();

        var feature = new GeometryFeature(new LineString(coordinates));
        feature.Styles.Add(new VectorStyle
        {
            Line = new Pen(Color.FromArgb(255, 255, 138, 0), 4) // #FF8A00
        });
        _routeLayer.Features = [feature];
        _routeLayer.DataHasChanged();
    }

    private void Center(MPoint point, double? resolution, bool zoom)
    {
        _programmatic = true;
        try
        {
            if (zoom && resolution is { } r)
                Map.Map.Navigator.CenterOnAndZoomTo(point, r);
            else
                Map.Map.Navigator.CenterOn(point);
        }
        finally
        {
            _programmatic = false;
        }
    }

    private void LocateClicked(object? sender, EventArgs e)
    {
        // Uses the last live sample pushed by the dashboard; no separate Geolocation request.
        if (!_hasFix)
        {
            return;
        }

        _follow = true;
        var (x, y) = SphericalMercator.FromLonLat(_lastLon, _lastLat);
        Center(new MPoint(x, y), null, zoom: false);
    }

    private async void LayersClicked(object? sender, EventArgs e) =>
        await Shell.Current.CurrentPage.DisplayAlertAsync("Katmanlar", "Harita: CARTO Dark (© OpenStreetMap contributors © CARTO). Ek katmanlar yakında.", "Tamam");
}
