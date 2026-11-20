using System.Windows;
using BankingManagesandServicesApp.Services;
namespace BankingManagesandServicesApp.GUIs;
public partial class LoginAppGUI : Window
{
    public LoginAppGUI() { InitializeComponent(); Loaded += (_, _) => UsernameBox.Focus(); }
    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        MessageText.Text = "";
        if (string.IsNullOrWhiteSpace(UsernameBox.Text) || string.IsNullOrEmpty(PasswordBox.Password))
        { MessageText.Text = "Vui lòng nhập tên đăng nhập và mật khẩu."; return; }
        LoginForm.IsEnabled = false;
        try
        {
            var account = await new BankRepository().AuthenticateAsync(UsernameBox.Text.Trim(), PasswordBox.Password);
            PasswordBox.Clear();
            if (account == null) { MessageText.Text = "Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản đã bị khóa."; return; }
            UserSession.SignIn(account);
            var main = new TransactionLogGUI();
            Application.Current.MainWindow = main;
            main.Show();
            Close();
        }
        catch (Exception ex) { MessageText.Text = "Không thể đăng nhập. Kiểm tra SQL Server và database.config. " + ex.Message; }
        finally { LoginForm.IsEnabled = true; }
    }
    private async void Demo_Click(object sender, RoutedEventArgs e)
    {
        LoginForm.IsEnabled = false;
        MessageText.Text = "Đang chuẩn bị dữ liệu demo…";
        try
        {
            await DemoData.EnsureCreatedAsync();
            UsernameBox.Text = DemoData.Username;
            PasswordBox.Password = DemoData.Password;
            MessageText.Text = "Demo đã sẵn sàng. Bấm Đăng nhập để tiếp tục.";
        }
        catch (Exception ex) { MessageText.Text = "Chưa tạo được demo. Kiểm tra database.config và chạy file SQL tạo bảng trước. " + ex.Message; }
        finally { LoginForm.IsEnabled = true; }
    }
}
