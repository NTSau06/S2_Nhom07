using System.Globalization;
namespace BankingManagesandServicesApp.Models;
public sealed record Account(int AccountId, string Username, string FullName, string RoleName, bool CanExport)
{
    public string DisplayName => $"{FullName} ({Username})";
}
public sealed record TransactionBank(string Id, string AccountNumber, string CustomerName, decimal Amount,
    string TransactionType, string Status, DateTime? TransactionTime, string Description, bool IsFlagged, string FlagReason)
{
    public string AmountText => Amount.ToString("N2", CultureInfo.GetCultureInfo("vi-VN")) + " VND";
    public string TimeText => TransactionTime?.ToString("dd/MM/yyyy HH:mm:ss") ?? "Chưa xác định";
    public string FlagText => IsFlagged ? "Có cảnh báo" : "Không";
    public bool CanIssueReceipt => Status.Equals("Thành công", StringComparison.OrdinalIgnoreCase)
        || Status.Equals("Success", StringComparison.OrdinalIgnoreCase) || Status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
}
public sealed record TransactionLog(long LogId, DateTime? Time, string Action, string Result, string Details);
public sealed record Receipt(TransactionBank Transaction, Account Issuer, DateTime IssuedAt)
{
    public string Number => $"CT-{Transaction.Id}";
    public string IssuedAtText => IssuedAt.ToString("dd/MM/yyyy HH:mm:ss");
}
