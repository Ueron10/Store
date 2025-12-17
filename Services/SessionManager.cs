using StoreProgram.Pages.Popups;

namespace StoreProgram.Services;

public static class SessionManager
{
    private static bool _isLoggingOut;

    public static async Task LogoutAsync()
    {
        if (_isLoggingOut) return;

        _isLoggingOut = true;
        try
        {
            if (DataStore.CurrentUser == null)
            {
                await Shell.Current.GoToAsync("//login");
                return;
            }

            bool confirm = await ConfirmPopupPage.ShowAsync(
                title: "Logout",
                message: "Yakin ingin logout?",
                confirmText: "Logout",
                cancelText: "Batal");

            if (!confirm) return;

            DataStore.ClearCurrentUser();
            await Shell.Current.GoToAsync("//login");
        }
        finally
        {
            _isLoggingOut = false;
        }
    }
}