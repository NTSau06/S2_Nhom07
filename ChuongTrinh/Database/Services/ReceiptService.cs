using System.IO;
using BankingManagesandServicesApp.Models;
namespace BankingManagesandServicesApp.Services;

public sealed class ReceiptService
{
    public async Task<string?> ExportAsync(string transactionId, string path, string format)
    {
        var user = UserSession.RequireUser();
        var repo = new BankRepository();
        await repo.EnsureExportAllowedAsync(user, transactionId);
        var transaction = (await repo.SearchTransactionsAsync(transactionId, "", null, null)).Single(t => t.Id == transactionId);
        if (!transaction.CanIssueReceipt) throw new InvalidOperationException("Giao dịch chưa thành công.");
        var receipt = new Receipt(transaction, user, DateTime.Now);
        var bytes = format switch { "PDF" => ReceiptExporter.CreatePdf(receipt), "HTML" => ReceiptExporter.CreateHtml(receipt),
            _ => throw new ArgumentException("Định dạng không được hỗ trợ.") };
        // Generate before touching the target; replace only after the full temporary file is written.
        string target = Path.GetFullPath(path);
        string temp = target + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temp, bytes);
            if (UserSession.Current != user || UserSession.IsLoggingOut) throw new InvalidOperationException("Phiên đăng nhập đã kết thúc.");
            File.Move(temp, target, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
        try { await repo.LogExportAsync(receipt, format, Path.GetFileName(target)); }
        catch (Exception ex) { return "Tệp đã lưu thành công, nhưng CHƯA ghi được nhật ký xuất. " + ex.Message; }
        return null;
    }
}
