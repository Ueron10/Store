namespace StoreProgram.Pages.Popups;

public partial class SelectOptionPopupPage : ContentPage
{
    private readonly TaskCompletionSource<string?> _tcs = new();

    public SelectOptionPopupPage(string title, IEnumerable<string> options, string? message)
    {
        InitializeComponent();

        TitleLabel.Text = title;

        if (!string.IsNullOrWhiteSpace(message))
        {
            MessageLabel.IsVisible = true;
            MessageLabel.Text = message;
        }

        OptionsView.ItemsSource = options.ToList();
    }

    public Task<string?> Result => _tcs.Task;

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(null);
        await Navigation.PopModalAsync(animated: false);
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        var selected = OptionsView.SelectedItem as string;
        _tcs.TrySetResult(selected);
        await Navigation.PopModalAsync(animated: false);
    }

    public static async Task<string?> ShowAsync(string title, IEnumerable<string> options, string? message = null)
    {
        var page = new SelectOptionPopupPage(title, options, message);

        var nav = Shell.Current?.Navigation ?? Application.Current?.MainPage?.Navigation;
        if (nav == null) return null;

        await nav.PushModalAsync(page, animated: false);
        return await page.Result;
    }
}
