using System.Windows;
using BankingManagesandServicesApp.GUIs;
using BankingManagesandServicesApp.Models;
namespace BankingManagesandServicesApp.Services;

public static class UserSession
{
    public static Account? Current { get; private set; }
    public static bool IsLoggingOut { get; private set; }
    public static Account RequireUser() => Current ?? throw new InvalidOperationException("Vui lòng đăng nhập.");
    public static void SignIn(Account account) => Current = account;

    public static async Task SignOutAsync()
    {
        if (IsLoggingOut || Current == null) return;
        IsLoggingOut = true;
        var account = Current;
        foreach (Window window in Application.Current.Windows) window.IsEnabled = false;
        string? warning = null;
        try { await new BankRepository().WriteActivityAsync(account, "Đăng xuất", "Thành công", "Kết thúc phiên đăng nhập."); }
        catch (Exception ex) { warning = "Đã đăng xuất nhưng chưa ghi được nhật ký: " + ex.Message; }
        finally
        {
            Current = null;
            var oldWindows = Application.Current.Windows.Cast<Window>().ToArray();
            var login = new LoginAppGUI();
            Application.Current.MainWindow = login;
            login.Show();
            foreach (var window in oldWindows) window.Close();
            IsLoggingOut = false;
            if (warning != null) MessageBox.Show(login, warning, "Nhật ký hệ thống");
        }
    }
}
