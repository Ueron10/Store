using Microsoft.Maui.Storage;

namespace StoreProgram.Services;

public static class ExportPathService
{
    public static string GetDefaultExportDirectory()
    {
#if WINDOWS
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var downloads = Path.Combine(userProfile, "Downloads");
        var dir = Path.Combine(downloads, "StoreProgram");
#else
        var dir = Path.Combine(FileSystem.Current.AppDataDirectory, "Exports");
#endif

        Directory.CreateDirectory(dir);
        return dir;
    }
}
