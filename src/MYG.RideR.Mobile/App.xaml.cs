using MYG.RideR.Mobile.Interfaces;

namespace MYG.RideR.Mobile;

public partial class App : Microsoft.Maui.Controls.Application
{
	private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(6);

	private readonly AppShell _appShell;
	private readonly IAppUpdater _appUpdater;
	private DateTimeOffset _lastUpdateCheck = DateTimeOffset.MinValue;

	public App(AppShell appShell, IAppUpdater appUpdater)
	{
		_appShell = appShell;
		_appUpdater = appUpdater;
		InitializeComponent();
		UserAppTheme = AppTheme.Dark;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_appShell);
	}

	protected override void OnStart()
	{
		base.OnStart();
		CheckForUpdate();
	}

	protected override void OnResume()
	{
		base.OnResume();
		if (DateTimeOffset.UtcNow - _lastUpdateCheck >= UpdateCheckInterval)
			CheckForUpdate();
	}

	private void CheckForUpdate()
	{
		_lastUpdateCheck = DateTimeOffset.UtcNow;
		_ = _appUpdater.CheckAndInstallAsync();
	}
}
