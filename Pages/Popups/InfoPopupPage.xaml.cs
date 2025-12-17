namespace StoreProgram.Pages.Popups;

public partial class InfoPopupPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _tcs = new();

    public InfoPopupPage(string title, string message, string okText)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        OkButton.Text = string.IsNullOrWhiteSpace(okText) ? "OK" : okText;
    }

    public Task<bool> Result => _tcs.Task;

    private async void OnOkClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(true);
        await Navigation.PopModalAsync(animated: false);
    }

    public static async Task ShowAsync(string title, string message, string okText = "OK")
    {
        var page = new InfoPopupPage(title, message, okText);

        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return;

        await nav.PushModalAsync(page, animated: false);
        await page.Result;
    }
}
