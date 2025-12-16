using System;
using System.Linq;
using Microsoft.Maui.Controls;

namespace StoreProgram.Services;

public static class ShellNav
{
    /// <summary>
    /// Pindah tab (ShellContent) di dalam TabBar tertentu.
    /// Ini lebih stabil dibanding GoToAsync("//ownerdashboard/reports") yang sering gagal resolve.
    /// </summary>
    public static bool TrySelectTab(string tabBarRoute, string shellContentRoute)
    {
        if (Shell.Current == null)
            return false;

        var shell = Shell.Current;

        // TabBar adalah ShellItem. Di AppShell.xaml kamu: <TabBar Route="ownerdashboard"> ...
        var item = shell.Items.FirstOrDefault(i => string.Equals(i.Route, tabBarRoute, StringComparison.Ordinal));
        if (item == null)
            return false;

        shell.CurrentItem = item;

        // Tiap <Tab> jadi ShellSection.
        // Setelah AppShell.xaml diberi Route pada Tab, kita bisa pilih langsung via sec.Route.
        var targetSection = item.Items.FirstOrDefault(sec =>
            string.Equals(sec.Route, shellContentRoute, StringComparison.Ordinal))
            ?? item.Items.FirstOrDefault(sec =>
                sec.Items.Any(c => string.Equals(c.Route, shellContentRoute, StringComparison.Ordinal)));

        if (targetSection == null)
            return false;

        item.CurrentItem = targetSection;

        // Pastikan ShellContent di dalam tab juga sinkron (kalau ada lebih dari 1)
        var targetContent = targetSection.Items.FirstOrDefault(c =>
            string.Equals(c.Route, shellContentRoute, StringComparison.Ordinal));

        if (targetContent != null)
            targetSection.CurrentItem = targetContent;

        return true;
    }
}
