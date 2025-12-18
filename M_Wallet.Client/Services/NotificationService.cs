namespace M_Wallet.Client.Services;

/// <summary>
/// Lightweight notification service for POS-style feedback.
/// Shows brief, non-intrusive notifications that auto-dismiss.
/// </summary>
public class NotificationService
{
    public event Action? OnChange;
    
    public string? CurrentMessage { get; private set; }
    public NotificationType CurrentType { get; private set; }
    public bool IsVisible { get; private set; }

    private CancellationTokenSource? _cts;

    /// <summary>
    /// Shows a brief notification that auto-dismisses.
    /// </summary>
    public async Task ShowAsync(string message, NotificationType type = NotificationType.Info, int durationMs = 1500)
    {
        // Cancel any pending hide
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        CurrentMessage = message;
        CurrentType = type;
        IsVisible = true;
        OnChange?.Invoke();

        try
        {
            await Task.Delay(durationMs, _cts.Token);
            Hide();
        }
        catch (TaskCanceledException)
        {
            // New notification came in, ignore
        }
    }

    public void Hide()
    {
        IsVisible = false;
        OnChange?.Invoke();
    }

    // Convenience methods (fire-and-forget, no await needed)
    public void Success(string message) => _ = ShowAsync(message, NotificationType.Success, 1200);
    public void Error(string message) => _ = ShowAsync(message, NotificationType.Error, 3000);
    public void Warning(string message) => _ = ShowAsync(message, NotificationType.Warning, 2000);
    public void Info(string message) => _ = ShowAsync(message, NotificationType.Info, 1500);
}

public enum NotificationType
{
    Success,
    Warning,
    Error,
    Info
}
