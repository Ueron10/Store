namespace StoreProgram.Pages.Popups;

public partial class ConfirmPopupPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _tcs = new();

    public ConfirmPopupPage(string title, string message, string confirmText, string cancelText)
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;

        ConfirmButton.Text = string.IsNullOrWhiteSpace(confirmText) ? "Konfirmasi" : confirmText;
        CancelButton.Text = string.IsNullOrWhiteSpace(cancelText) ? "Cancel" : cancelText;
    }

    public Task<bool> Result => _tcs.Task;

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(false);
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(true);
        await Navigation.PopModalAsync(animated: false);
    }

    public static async Task<bool> ShowAsync(string title, string message, string confirmText = "Ya", string cancelText = "Batal")
    {
        var page = new ConfirmPopupPage(title, message, confirmText, cancelText);

        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null)
            return false;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
