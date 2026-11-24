using System.Windows;
using System.Windows.Controls;
using System.Globalization;
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
        if (!TryReadAmount(MinimumAmountBox.Text, out var minimum) || !TryReadAmount(MaximumAmountBox.Text, out var maximum))
        { MessageText.Text = "Số tiền phải là số nguyên VND không âm, không dùng dấu phân cách (ví dụ: 2500000)."; return; }
        if (minimum > maximum) { MessageText.Text = "Số tiền từ không được lớn hơn số tiền đến."; return; }
        string transactionType = TypeBox.Text.Trim();
        bool? flagged = FlagBox.SelectedIndex switch { 1 => true, 2 => false, _ => null };
        searching = true;
        SearchButton.IsEnabled = ResetButton.IsEnabled = false;
        try
        {
            MessageText.Text = "Đang tải giao dịch…";
            var status = StatusBox.SelectedIndex == 0 ? "" : ((ComboBoxItem)StatusBox.SelectedItem).Content.ToString()!;
            var items = await repository.SearchTransactionsAsync(KeywordBox.Text.Trim(), status, FromDate.SelectedDate, ToDate.SelectedDate);
            if (UserSession.Current == null) return;
            var filtered = TransactionAnalysis.Filter(items, transactionType, minimum, maximum, flagged);
            // Keep the selected type even when the SQL filters produce no matching types.
            TypeBox.ItemsSource = items.Select(t => t.TransactionType).Append(transactionType)
                .Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().OrderBy(t => t).ToList();
            TypeBox.Text = transactionType;
            TransactionsGrid.ItemsSource = filtered;
            UpdateStatistics(filtered);
            MessageText.Text = filtered.Count == 0 ? "Không tìm thấy giao dịch phù hợp." : $"Tìm thấy {filtered.Count} giao dịch. Chứng từ chỉ được lập cho giao dịch thành công.";
        }
        catch (Exception ex) { MessageText.Text = "Không tải được dữ liệu; kết quả cũ (nếu có) vẫn được giữ lại. " + ex.Message; }
        finally { searching = false; SearchButton.IsEnabled = ResetButton.IsEnabled = true; }
    }
    private static bool TryReadAmount(string text, out decimal? amount)
    {
        amount = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!decimal.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value)) return false;
        amount = value;
        return true;
    }
    private void UpdateStatistics(List<TransactionBank> items)
    {
        var total = TransactionAnalysis.Summarize("Tất cả", items);
        var successful = TransactionAnalysis.Summarize("Thành công", items.Where(t => t.CanIssueReceipt));
        SummaryText.Text = $"Tổng: {total.Count} giao dịch   •   Thành công: {total.SuccessfulCount}   •   Tỷ lệ thành công: {total.SuccessRateText}   •   Có cảnh báo: {total.FlaggedCount}";
        AmountSummaryText.Text = $"Tổng số tiền: {total.TotalAmountText}   •   Số tiền thành công: {successful.TotalAmountText}";
        StatusStatisticsGrid.ItemsSource = TransactionAnalysis.GroupBy(items, t => t.Status);
        TypeStatisticsGrid.ItemsSource = TransactionAnalysis.GroupBy(items, t => t.TransactionType);
    }
    private async void Search_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        KeywordBox.Clear(); StatusBox.SelectedIndex = 0; FromDate.SelectedDate = null; ToDate.SelectedDate = null;
        TypeBox.SelectedIndex = -1; TypeBox.Text = ""; MinimumAmountBox.Clear(); MaximumAmountBox.Clear(); FlagBox.SelectedIndex = 0;
        await LoadAsync();
    }
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
