using System.IO;
using System.Windows;
using System.Windows.Controls;
using BankingManagesandServicesApp.Models;
using BankingManagesandServicesApp.Services;
using Microsoft.Win32;
namespace BankingManagesandServicesApp.GUIs;
public partial class ReceiptPreviewGUI : AuthenticatedWindow
{
    private readonly TransactionBank transaction;
    public ReceiptPreviewGUI(TransactionBank item)
    {
        InitializeComponent(); transaction = item;
        var receipt = new Receipt(item, UserSession.RequireUser(), DateTime.Now);
        FieldsList.ItemsSource = ReceiptExporter.Fields(receipt).Select(f => new { f.Label, f.Value });
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        string format = ((ComboBoxItem)FormatBox.SelectedItem).Content.ToString()!;
        string safeId = string.Concat(transaction.Id.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var dialog = new SaveFileDialog { Title = "Lưu chứng từ giao dịch", FileName = $"ChungTu_{safeId}_{DateTime.Now:yyyyMMdd_HHmmss}",
            Filter = format == "PDF" ? "Chứng từ PDF (*.pdf)|*.pdf" : "Chứng từ HTML (*.html)|*.html", DefaultExt = format.ToLowerInvariant(), AddExtension = true };
        if (dialog.ShowDialog(this) != true) return;
        ExportActions.IsEnabled = false;
        try
        {
            var warning = await new ReceiptService().ExportAsync(transaction.Id, dialog.FileName, format);
            MessageText.Text = warning ?? $"Đã lưu {dialog.FileName} và ghi nhật ký xuất chứng từ.";
        }
        catch (Exception ex) { MessageText.Text = "Chưa xuất được chứng từ: " + ex.Message; }
        finally { ExportActions.IsEnabled = true; }
    }
}
