using System.Windows;
using System.Windows.Controls;
using BankingManagesandServicesApp.Models;
using BankingManagesandServicesApp.Services;
namespace BankingManagesandServicesApp.GUIs;
public partial class TransactionLogGUI : AuthenticatedWindow
{
    private readonly BankRepository repository = new();
    private int selectionVersion;
    private bool searching;
    public TransactionLogGUI() { InitializeComponent(); Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync()
    {
        if (searching || UserSession.Current == null) return;
        if (FromDate.SelectedDate > ToDate.SelectedDate) { MessageText.Text = "Từ ngày không được lớn hơn đến ngày."; return; }
        searching = true;
        try
        {
            MessageText.Text = "Đang tải giao dịch…";
            var status = StatusBox.SelectedIndex == 0 ? "" : ((ComboBoxItem)StatusBox.SelectedItem).Content.ToString()!;
            var items = await repository.SearchTransactionsAsync(KeywordBox.Text.Trim(), status, FromDate.SelectedDate, ToDate.SelectedDate);
            if (UserSession.Current == null) return;
            TransactionsGrid.ItemsSource = items;
            MessageText.Text = items.Count == 0 ? "Không tìm thấy giao dịch phù hợp." : $"Tìm thấy {items.Count} giao dịch. Chứng từ chỉ được lập cho giao dịch thành công.";
        }
        catch (Exception ex) { MessageText.Text = "Không tải được dữ liệu: " + ex.Message; }
        finally { searching = false; }
    }
    private async void Search_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private async void Reset_Click(object sender, RoutedEventArgs e)
    { KeywordBox.Clear(); StatusBox.SelectedIndex = 0; FromDate.SelectedDate = null; ToDate.SelectedDate = null; await LoadAsync(); }
    private async void Transaction_Selected(object sender, SelectionChangedEventArgs e)
        => await RefreshSelectedLogsAsync();
    private async Task RefreshSelectedLogsAsync()
    {
        int version = ++selectionVersion;
        LogsGrid.ItemsSource = null;
        var transaction = TransactionsGrid.SelectedItem as TransactionBank;
        ReceiptButton.IsEnabled = transaction?.CanIssueReceipt == true && UserSession.Current?.CanExport == true;
        SelectionText.Text = transaction == null ? "Chưa chọn giao dịch" : $"{transaction.Id} • {transaction.Description} • Cảnh báo: {transaction.FlagText}";
        if (transaction == null) return;
        try
        {
            var logs = await repository.GetLogsAsync(transaction.Id);
            if (version == selectionVersion) LogsGrid.ItemsSource = logs;
        }
        catch (Exception ex) { if (version == selectionVersion) MessageText.Text = "Không tải được nhật ký: " + ex.Message; }
    }
    private async void Receipt_Click(object sender, RoutedEventArgs e)
    {
        if (TransactionsGrid.SelectedItem is not TransactionBank item) return;
        try
        {
            await repository.EnsureExportAllowedAsync(UserSession.RequireUser(), item.Id);
            var fresh = (await repository.SearchTransactionsAsync(item.Id, "", null, null)).Single(t => t.Id == item.Id);
            var preview = new ReceiptPreviewGUI(fresh) { Owner = this };
            preview.ShowDialog();
            if (UserSession.Current != null) await RefreshSelectedLogsAsync();
        }
        catch (Exception ex) { if (UserSession.Current != null) MessageText.Text = ex.Message; }
    }
    private void Accounts_Click(object sender, RoutedEventArgs e)
    { new AccountBankingManageOverviewGUI { Owner = this }.ShowDialog(); }
}
