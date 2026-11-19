using System.Windows;
using System.Windows.Controls;
using BankingManagesandServicesApp.Services;
namespace BankingManagesandServicesApp.GUIs;
public partial class SessionHeader : UserControl
{
    public SessionHeader()
    {
        InitializeComponent();
        UserNameText.Text = UserSession.Current?.DisplayName ?? "Chưa đăng nhập";
        RoleText.Text = UserSession.Current?.RoleName ?? "";
        Loaded += (_, _) =>
        {
            UserNameText.Text = UserSession.Current?.DisplayName ?? "Chưa đăng nhập";
            RoleText.Text = UserSession.Current?.RoleName ?? "";
        };
    }
    private async void Logout_Click(object sender, RoutedEventArgs e) => await UserSession.SignOutAsync();
}
