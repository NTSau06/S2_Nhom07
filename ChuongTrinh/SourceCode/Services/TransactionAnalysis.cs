using System.Globalization;
using BankingManagesandServicesApp.Models;

namespace BankingManagesandServicesApp.Services;

public sealed record TransactionStatistics(string Group, int Count, decimal TotalAmount, int SuccessfulCount, int FlaggedCount)
{
    public string TotalAmountText => TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("vi-VN")) + " VND";
    public string SuccessRateText => (Count == 0 ? 0m : 100m * SuccessfulCount / Count)
        .ToString("N1", CultureInfo.GetCultureInfo("vi-VN")) + "%";
}

public static class TransactionAnalysis
{
    // Apply extra filters to the complete SQL result, before calculating any totals.
    public static List<TransactionBank> Filter(IEnumerable<TransactionBank> items, string transactionType,
        decimal? minimumAmount, decimal? maximumAmount, bool? flagged)
    {
        if (minimumAmount < 0 || maximumAmount < 0 || minimumAmount > maximumAmount)
            throw new ArgumentException("Khoảng số tiền không hợp lệ.");
        return items.Where(t => (string.IsNullOrEmpty(transactionType) || t.TransactionType == transactionType)
            && (!minimumAmount.HasValue || t.Amount >= minimumAmount.Value)
            && (!maximumAmount.HasValue || t.Amount <= maximumAmount.Value)
            && (!flagged.HasValue || t.IsFlagged == flagged.Value)).ToList();
    }

    public static TransactionStatistics Summarize(string group, IEnumerable<TransactionBank> items)
    {
        var rows = items.ToList();
        return new(group, rows.Count, rows.Sum(t => t.Amount), rows.Count(t => t.CanIssueReceipt), rows.Count(t => t.IsFlagged));
    }

    public static List<TransactionStatistics> GroupBy(IEnumerable<TransactionBank> items, Func<TransactionBank, string> key)
        => items.GroupBy(key).OrderBy(g => g.Key)
            .Select(g => Summarize(string.IsNullOrWhiteSpace(g.Key) ? "Chưa xác định" : g.Key, g)).ToList();
}
