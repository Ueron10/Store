using StoreProgram.Models;

namespace StoreProgram.Services;

public static class AuthService
{
    // identifier -> otp
    private static readonly Dictionary<string, string> _otpStore = new();

    public static AppUser? ValidateLogin(string username, string password)
    {
        var user = DataStore.Users.FirstOrDefault(u =>
            string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)
            && u.Password == password);

        if (user != null)
        {
            DataStore.SetCurrentUser(user);
        }

        return user;
    }

    public static string? GenerateOtp(string identifier)
    {
        var user = FindUserByIdentifier(identifier);
        if (user == null) return null;

        var rnd = new Random();
        var otp = rnd.Next(100000, 999999).ToString();

        _otpStore[identifier.ToLowerInvariant()] = otp;
        return otp;
    }

    public static bool ResetPassword(string identifier, string otp, string newPassword)
    {
        identifier = identifier.ToLowerInvariant();
        if (!_otpStore.TryGetValue(identifier, out var stored) || stored != otp)
        {
            return false;
        }

        var user = FindUserByIdentifier(identifier);
        if (user == null) return false;

        user.Password = newPassword;
        _otpStore.Remove(identifier);
        return true;
    }

    private static AppUser? FindUserByIdentifier(string identifier)
    {
        identifier = identifier.ToLowerInvariant();
        return DataStore.Users.FirstOrDefault(u =>
            u.Username.Equals(identifier, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(u.Email) && u.Email.Equals(identifier, StringComparison.OrdinalIgnoreCase)) ||
            (!string.IsNullOrEmpty(u.Phone) && u.Phone.Equals(identifier, StringComparison.OrdinalIgnoreCase)));
    }
}
