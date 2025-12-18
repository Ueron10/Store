namespace StoreProgram.Pages.Popups;

public partial class ConfirmPopupPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    private INavigation? _hostNav;
    private bool _closing;

    public ConfirmPopupPage(string title, string message, string confirmText, string cancelText)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;

        ConfirmButton.Text = string.IsNullOrWhiteSpace(confirmText) ? "Konfirmasi" : confirmText;
        CancelButton.Text = string.IsNullOrWhiteSpace(cancelText) ? "Cancel" : cancelText;
    }

    public Task<bool> Result => _tcs.Task;

    private async Task SafeCloseAsync(bool result)
    {
        if (_closing)
            return;

        _closing = true;
        _tcs.TrySetResult(result);

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

    private async void OnCancelClicked(object sender, EventArgs e)
        => await SafeCloseAsync(false);

    private async void OnConfirmClicked(object sender, EventArgs e)
        => await SafeCloseAsync(true);

    public static async Task<bool> ShowAsync(string title, string message, string confirmText = "Ya", string cancelText = "Batal")
    {
        var page = new ConfirmPopupPage(title, message, confirmText, cancelText);

        var hostPage = Shell.Current?.CurrentPage ?? Application.Current?.MainPage;
        var nav = hostPage?.Navigation;
        if (nav == null)
            return false;

        page._hostNav = nav;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
