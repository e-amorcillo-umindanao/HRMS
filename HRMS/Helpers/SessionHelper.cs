using HRMS.Services;
using Microsoft.Maui.ApplicationModel;
using MudBlazor;

namespace HRMS.Helpers;

public class SessionHelper : IDisposable
{
    private static readonly TimeSpan WarningDelay = TimeSpan.FromMinutes(13);
    private static readonly TimeSpan LogoutDelay = TimeSpan.FromMinutes(15);

    private readonly AuthService _authService;
    private readonly ISnackbar _snackbar;
    private CancellationTokenSource? _timerCancellationTokenSource;

    public SessionHelper(AuthService authService, ISnackbar snackbar)
    {
        _authService = authService;
        _snackbar = snackbar;
    }

    public void Start()
    {
        ResetTimer();
    }

    public void RecordActivity()
    {
        ResetTimer();
    }

    public void Stop()
    {
        CancelTimer();
    }

    public void Dispose()
    {
        CancelTimer();
    }

    private void ResetTimer()
    {
        if (!_authService.IsAuthenticated)
        {
            return;
        }

        CancelTimer();

        var cancellationTokenSource = new CancellationTokenSource();
        _timerCancellationTokenSource = cancellationTokenSource;

        _ = RunWarningTimerAsync(cancellationTokenSource.Token);
        _ = RunLogoutTimerAsync(cancellationTokenSource.Token);
    }

    private void CancelTimer()
    {
        if (_timerCancellationTokenSource is null)
        {
            return;
        }

        _timerCancellationTokenSource.Cancel();
        _timerCancellationTokenSource.Dispose();
        _timerCancellationTokenSource = null;
    }

    private async Task RunWarningTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(WarningDelay, cancellationToken);

            if (cancellationToken.IsCancellationRequested || !_authService.IsAuthenticated)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                _snackbar.Add(
                    "Session will lock in 2 minutes due to inactivity.",
                    Severity.Warning,
                    configuration =>
                    {
                        configuration.VisibleStateDuration = 5000;
                        configuration.SnackbarVariant = Variant.Filled;
                    });
            });
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task RunLogoutTimerAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(LogoutDelay, cancellationToken);

            if (cancellationToken.IsCancellationRequested || !_authService.IsAuthenticated)
            {
                return;
            }

            await MainThread.InvokeOnMainThreadAsync(() => _authService.LogoutAsync());
        }
        catch (TaskCanceledException)
        {
        }
    }
}
