namespace StoreProgram.Pages.Popups;

public partial class InfoPopupPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    private INavigation? _hostNav;
    private bool _closing;

    public InfoPopupPage(string title, string message, string okText)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        OkButton.Text = string.IsNullOrWhiteSpace(okText) ? "OK" : okText;
    }

    public Task<bool> Result => _tcs.Task;

    private async Task SafeCloseAsync()
    {
        if (_closing)
            return;

        _closing = true;
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

    public static async Task ShowAsync(string title, string message, string okText = "OK")
    {
        var page = new InfoPopupPage(title, message, okText);

        var hostPage = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        var nav = hostPage?.Navigation;
        if (nav == null) return;

        page._hostNav = nav;

        await nav.PushModalAsync(page, animated: false);
        await page.Result;
    }
}
