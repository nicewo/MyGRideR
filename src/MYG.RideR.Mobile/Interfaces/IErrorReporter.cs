namespace MYG.RideR.Mobile.Interfaces;

public interface IErrorReporter
{
    /// <summary>Recipient saved on the device (never compiled into the public APK).</summary>
    string ReportEmail { get; set; }

    /// <summary>True when crash.log has grown since the last report.</summary>
    Task<bool> HasUnreportedAsync();

    /// <summary>Opens the mail app with the log in the body and as attachment. False if there is nothing to send or no mail app.</summary>
    Task<bool> SendAsync();
}
