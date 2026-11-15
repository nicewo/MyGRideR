using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MYG.RideR.Application.Dashboard;
using MYG.RideR.Application.Diagnostics;
using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ICrashLog _crashLog;
    private readonly IErrorReporter _errorReporter;

    [ObservableProperty]
    private string _crashLogStatus = CrashLogStatusText.From(null);

    [ObservableProperty]
    private string _reportEmail = string.Empty;

    public SettingsViewModel(ICrashLog crashLog, IErrorReporter errorReporter)
    {
        _crashLog = crashLog;
        _errorReporter = errorReporter;
        _reportEmail = errorReporter.ReportEmail;
    }

    private const string VehicleModeKey = "vehicle_mode";

    [ObservableProperty]
    private int _vehicleModeIndex = Preferences.Default.Get(VehicleModeKey, (int)VehicleModes.Default);

    public bool IsWalk => VehicleModeIndex == (int)VehicleMode.Walk;
    public bool IsBicycle => VehicleModeIndex == (int)VehicleMode.Bicycle;
    public bool IsMotorcycle => VehicleModeIndex == (int)VehicleMode.Motorcycle;
    public bool IsCar => VehicleModeIndex == (int)VehicleMode.Car;

    partial void OnVehicleModeIndexChanged(int value)
    {
        Preferences.Default.Set(VehicleModeKey, value);
        OnPropertyChanged(nameof(IsWalk));
        OnPropertyChanged(nameof(IsBicycle));
        OnPropertyChanged(nameof(IsMotorcycle));
        OnPropertyChanged(nameof(IsCar));
    }

    [RelayCommand]
    private void SetVehicleMode(string index) => VehicleModeIndex = int.Parse(index);

    partial void OnReportEmailChanged(string value) => _errorReporter.ReportEmail = value;

    [RelayCommand]
    private async Task SendReportAsync()
    {
        if (!await _errorReporter.SendAsync())
            CrashLogStatus = "Gönderilecek hata kaydı yok (ya da e-posta uygulaması bulunamadı)";
    }

    public string Title => "Ayarlar";

    public string AppVersionText { get; } = $"Sürüm {AppInfo.VersionString} (build {AppInfo.BuildString})";

    [RelayCommand]
    public async Task LoadAsync()
    {
        var text = await _crashLog.ReadAsync();
        CrashLogStatus = CrashLogStatusText.From(text is null ? null : Encoding.UTF8.GetByteCount(text));
    }

    [RelayCommand]
    private async Task ShareCrashLogAsync()
    {
        if (await _crashLog.ReadAsync() is null)
        {
            return;
        }

        await Share.Default.RequestAsync(new ShareFileRequest(
            "MYG RideR hata kaydı",
            new ShareFile(_crashLog.FilePath)));
    }

    [RelayCommand]
    private async Task ClearCrashLogAsync()
    {
        await _crashLog.ClearAsync();
        await LoadAsync();
    }
}
