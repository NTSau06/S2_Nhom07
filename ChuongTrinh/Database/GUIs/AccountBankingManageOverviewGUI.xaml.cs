using System.Data;
using System.Windows;
using BankingManagesandServicesApp.DBManages;
using Microsoft.Data.SqlClient;
namespace BankingManagesandServicesApp.GUIs;
public partial class AccountBankingManageOverviewGUI : AuthenticatedWindow
{
    private bool loading;
    public AccountBankingManageOverviewGUI() { InitializeComponent(); Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync()
    {
        if (loading) return;
        loading = true;
        try
        {
            await using var db = await DBConnectandManage.OpenAsync();
            await using var cmd = new SqlCommand("SELECT idacc,fullname,phone,typeacc,money FROM dbo.AccountBank WHERE @q=N'' OR CHARINDEX(@q,idacc)>0 OR CHARINDEX(@q,fullname)>0 ORDER BY idacc", db);
            cmd.Parameters.Add("@q", SqlDbType.NVarChar, 100).Value = SearchBox.Text.Trim();
            await using var reader = await cmd.ExecuteReaderAsync();
            var table = new DataTable(); table.Load(reader);
            AccountsGrid.ItemsSource = table.DefaultView;
            MessageText.Text = $"Có {table.Rows.Count} tài khoản.";
        }
        catch (Exception ex) { MessageText.Text = "Không tải được tài khoản: " + ex.Message; }
        finally { loading = false; }
    }
    private async void Search_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private void Back_Click(object sender, RoutedEventArgs e) => Close();
}
