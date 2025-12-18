using Microsoft.Maui.ApplicationModel;

namespace StoreProgram.Pages.Popups;

public partial class InfoPopupPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    private readonly TimeSpan? _autoCloseAfter;
    private INavigation? _hostNav;
    private bool _closing;
    private CancellationTokenSource? _autoCloseCts;

    public InfoPopupPage(string title, string message, string okText, TimeSpan? autoCloseAfter = null)
    {
        InitializeComponent();

        _autoCloseAfter = autoCloseAfter;

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        OkButton.Text = string.IsNullOrWhiteSpace(okText) ? "OK" : okText;

        // Kalau auto-close, tombol OK tetap bisa dipakai tapi tidak wajib.
        if (_autoCloseAfter != null)
            OkButton.IsVisible = false;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_autoCloseAfter is not { } delay)
            return;

        _autoCloseCts?.Cancel();
        _autoCloseCts = new CancellationTokenSource();

        _ = AutoCloseAsync(delay, _autoCloseCts.Token);
    }

    protected override void OnDisappearing()
    {
        _autoCloseCts?.Cancel();
        base.OnDisappearing();
    }

    private async Task AutoCloseAsync(TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
            if (ct.IsCancellationRequested)
                return;

            await MainThread.InvokeOnMainThreadAsync(SafeCloseAsync);
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }

    public Task<bool> Result => _tcs.Task;

    private async Task SafeCloseAsync()
    {
        if (_closing)
            return;

        _closing = true;
        _autoCloseCts?.Cancel();
        _tcs.TrySetResult(true);

        try
        {
            var nav = _hostNav ?? Navigation;
            await nav.PopModalAsync(animated: false);
        }
        catch
        {
            // ignore
        }
    }

    private async void OnOkClicked(object sender, EventArgs e)
        => await SafeCloseAsync();

    public static async Task ShowAsync(string title, string message, string okText = "OK", TimeSpan? autoCloseAfter = null)
    {
        var page = new InfoPopupPage(title, message, okText, autoCloseAfter);

        var hostPage = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        var nav = hostPage?.Navigation;
        if (nav == null) return;

        page._hostNav = nav;

        await nav.PushModalAsync(page, animated: false);
        await page.Result;
    }
}
