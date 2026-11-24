using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BankingManagesandServicesApp.DBManages;

namespace BankingManagesandServicesApp.Controls
{
    /// <summary>
    /// Lớp xử lý Quản lý tín dụng & cho vay ("Xử lý hệ thống" trong các Sequence Diagram UC1.2.8.x):
    ///  - UC1.2.8.1 Tra cứu lịch sử tín dụng
    ///  - UC1.2.8.2 Quản lý hạn mức thẻ tín dụng
    ///  - UC1.2.8.3 Theo dõi dư nợ khách hàng
    ///  - UC1.2.8.4 Xét duyệt hồ sơ vay vốn
    /// </summary>
    internal class CreditLoanControl
    {
        private readonly AccountBank ab = new AccountBank();
        private readonly LoanProfile lp = new LoanProfile();
        private readonly CreditCard cc = new CreditCard();

        public const string All = "Tất cả";
        public static readonly string[] DebtGroups = { All, "Nhóm 1", "Nhóm 2", "Nhóm 3", "Nhóm 4", "Nhóm 5" };
        public static readonly string[] DebtStates = { All, "Trong hạn", "Quá hạn" };

        // Giới hạn khi phê duyệt khoản vay
        public const int MinLoan = 1_000_000;
        public const int MaxLoan = 2_000_000_000;   // cột loanamount kiểu INT
        public const double MaxRate = 30;           // %/năm
        public const int MaxTerm = 360;             // tháng

        // ================= UC1.2.8.1 Tra cứu lịch sử tín dụng =================

        /// <summary>Tìm khách hàng theo Mã KH / CCCD / Số tài khoản / SĐT / họ tên. Danh sách rỗng = không tìm thấy (2a).</summary>
        public List<AccountBank> searchcustomer(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<AccountBank>();
            return ab.queryaccount(keyword);
        }

        public AccountBank? loadcustomer(string idacc) => ab.detailaccount(idacc);

        public List<LoanProfile> loadcredithistory(string idacc) => lp.querycredithistory(idacc);

        public List<CreditCard> loadcustomercards(string idacc)
        {
            return cc.querycardlist(idacc).Where(c => c.idacc == idacc).ToList();
        }

        /// <summary>Tóm tắt lịch sử tín dụng: điểm tín dụng gần nhất, số khoản vay, tổng dư nợ, số khoản quá hạn.</summary>
        public string summarizecredithistory(List<LoanProfile> loans)
        {
            if (loans.Count == 0) return "Khách hàng chưa có khoản vay nào.";
            // Điểm tín dụng lấy theo hồ sơ gần nhất có chấm điểm
            int score = loans.Where(l => l.creditscore > 0).OrderByDescending(l => l.idloan)
                             .Select(l => l.creditscore).FirstOrDefault();
            int active = loans.Count(l => l.status == LoanProfile.StatusApproved && l.remainamount > 0);
            int settled = loans.Count(l => l.status == LoanProfile.StatusSettled);
            int overdue = loans.Count(l => l.debtstate == "Quá hạn");
            long debt = loans.Where(l => l.status == LoanProfile.StatusApproved).Sum(l => (long)l.remainamount);
            return $"Điểm tín dụng: {score}  ({ratecreditscore(score)})   |   Tổng số hồ sơ vay: {loans.Count}   |   " +
                   $"Đang vay: {active}   |   Đã tất toán: {settled}   |   Quá hạn: {overdue}   |   Tổng dư nợ: {debt:N0} VNĐ";
        }

        public static string ratecreditscore(int score)
        {
            if (score >= 750) return "Rất tốt";
            if (score >= 650) return "Tốt";
            if (score >= 550) return "Trung bình";
            if (score > 0) return "Rủi ro";
            return "Chưa xếp hạng";
        }

        // ================= UC1.2.8.2 Quản lý hạn mức thẻ tín dụng =================

        public List<CreditCard> loadcardlist(string keyword) => cc.querycardlist(keyword);

        public CreditCard? loadcardlimit(string idcard) => cc.querycardlimit(idcard);

        /// <summary>
        /// Kiểm tra hạn mức mới. Mã trả về: 0 hợp lệ | 1 không phải số | 2 phải lớn hơn 0 |
        /// 3 vượt hạn mức tối đa của hạng thẻ (4a) | 4 chưa nhập lý do | 5 trùng hạn mức hiện tại | 6 thẻ chưa kích hoạt
        /// </summary>
        public int checkformatlimit(CreditCard card, string newlimitText, string reason, out int newlimit)
        {
            newlimit = 0;
            if (card.status != CreditCard.StatusActive) return 6;
            if (!MoneyHelper.TryParseMoney(newlimitText, out newlimit)) return 1;
            if (newlimit <= 0) return 2;
            if (newlimit > card.maxlimit) return 3;
            if (string.IsNullOrWhiteSpace(reason)) return 4;
            if (newlimit == card.currentlimit) return 5;
            return 0;
        }

        public static string limitmessage(int code, CreditCard card)
        {
            return code switch
            {
                1 => "Hạn mức mới không hợp lệ (chỉ nhập số).",
                2 => "Hạn mức mới phải lớn hơn 0.",
                3 => $"Hạn mức vượt quy định! Hạng thẻ \"{card.cardtype}\" chỉ cho phép tối đa {card.maxlimit:N0} VNĐ. Vui lòng nhập lại.",
                4 => "Vui lòng nhập lý do điều chỉnh.",
                5 => "Hạn mức mới trùng với hạn mức hiện tại.",
                6 => $"Thẻ đang ở trạng thái \"{card.status}\", chỉ điều chỉnh được thẻ đang hoạt động.",
                _ => ""
            };
        }

        public bool updatelimit(string idcard, int newlimit, string reason)
        {
            return cc.updatecardlimit(idcard, newlimit, reason.Trim());
        }

        // ================= UC1.2.8.3 Theo dõi dư nợ khách hàng =================

        /// <summary>Danh sách các khoản đang có dư nợ (idacc rỗng = tất cả khách hàng).</summary>
        public List<LoanProfile> loaddebtlist(string idacc = "") => lp.querydebtlist(idacc);

        /// <summary>Lọc danh sách dư nợ theo nhóm nợ, tình trạng hạn, thời gian đến hạn và số tiền còn lại.</summary>
        public List<LoanProfile> filterdebt(List<LoanProfile> source, string group, string state,
                                            DateTime? dueFrom, DateTime? dueTo, string minText, string maxText, out string error)
        {
            error = "";
            int min = 0, max = int.MaxValue;
            if (!string.IsNullOrWhiteSpace(minText) && (!MoneyHelper.TryParseMoney(minText, out min) || min < 0))
            {
                error = "Số tiền tối thiểu không hợp lệ.";
                return new List<LoanProfile>();
            }
            if (!string.IsNullOrWhiteSpace(maxText) && (!MoneyHelper.TryParseMoney(maxText, out max) || max < 0))
            {
                error = "Số tiền tối đa không hợp lệ.";
                return new List<LoanProfile>();
            }
            if (min > max || (dueFrom.HasValue && dueTo.HasValue && dueFrom > dueTo))
            {
                error = "Khoảng lọc không hợp lệ (giá trị 'từ' lớn hơn 'đến').";
                return new List<LoanProfile>();
            }

            return source.Where(d =>
                    (group == All || d.debtgroup == group) &&
                    (state == All || d.debtstate == state) &&
                    (!dueFrom.HasValue || (d.duedate.HasValue && d.duedate.Value.Date >= dueFrom.Value.Date)) &&
                    (!dueTo.HasValue || (d.duedate.HasValue && d.duedate.Value.Date <= dueTo.Value.Date)) &&
                    d.remainamount >= min && d.remainamount <= max)
                .ToList();
        }

        public LoanProfile? loaddebtdetail(string idloan) => lp.detaildebt(idloan);

        public string describedebt(LoanProfile d)
        {
            double monthlyRate = d.interestrate / 100 / 12;
            int monthsLeft = d.duedate.HasValue
                ? Math.Max(0, (d.duedate.Value.Year - DateTime.Today.Year) * 12 + d.duedate.Value.Month - DateTime.Today.Month)
                : 0;
            long interestThisMonth = (long)Math.Round(d.remainamount * monthlyRate);
            long principalPerMonth = monthsLeft > 0 ? d.remainamount / monthsLeft : d.remainamount;
            return
                $"Mã khoản vay: {d.idloan}\n" +
                $"Khách hàng: {d.fullname} - TK {d.idacc} - CCCD {d.CCCD}\n" +
                $"Số tiền vay: {d.loanamount:N0} VNĐ   |   Lãi suất: {d.interestrate:0.##}%/năm   |   Kỳ hạn: {d.loanterm} tháng\n" +
                $"Đã trả: {d.paidamount:N0} VNĐ   |   Còn lại: {d.remainamount:N0} VNĐ\n" +
                $"Ngày giải ngân: {d.approveddate:dd/MM/yyyy}   |   Ngày đến hạn: {d.duedate:dd/MM/yyyy}   |   {d.debtgroup} - {d.debtstate}\n" +
                $"Kỳ thanh toán tháng này (ước tính): gốc {principalPerMonth:N0} + lãi {interestThisMonth:N0} = {principalPerMonth + interestThisMonth:N0} VNĐ" +
                (string.IsNullOrEmpty(d.note) ? "" : $"\nGhi chú: {d.note}");
        }

        // ================= UC1.2.8.4 Xét duyệt hồ sơ vay vốn =================

        public List<LoanProfile> loadpendingprofiles() => lp.querypendingprofiles();

        public LoanProfile? loadprofiledetail(string idloan) => lp.detailloanprofile(idloan);

        /// <summary>
        /// Kiểm tra thông số phê duyệt. Mã trả về: 0 hợp lệ | 1 số tiền sai | 2 số tiền ngoài giới hạn |
        /// 3 lãi suất sai | 4 lãi suất ngoài giới hạn | 5 thời hạn sai | 6 thời hạn ngoài giới hạn
        /// </summary>
        public int checkformatapproval(string amountText, string rateText, string termText,
                                       out int amount, out double rate, out int term)
        {
            rate = 0; term = 0;
            if (!MoneyHelper.TryParseMoney(amountText, out amount)) return 1;
            if (amount < MinLoan || amount > MaxLoan) return 2;
            if (!double.TryParse((rateText ?? "").Replace(',', '.').Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out rate)) return 3;
            if (rate <= 0 || rate > MaxRate) return 4;
            if (!int.TryParse((termText ?? "").Trim(), out term)) return 5;
            if (term < 1 || term > MaxTerm) return 6;
            return 0;
        }

        public static string approvalmessage(int code)
        {
            return code switch
            {
                1 => "Hạn mức (số tiền vay) không hợp lệ.",
                2 => $"Hạn mức phải từ {MinLoan:N0} đến {MaxLoan:N0} VNĐ.",
                3 => "Lãi suất không hợp lệ.",
                4 => $"Lãi suất phải lớn hơn 0 và không quá {MaxRate}%/năm.",
                5 => "Thời hạn vay không hợp lệ.",
                6 => $"Thời hạn vay từ 1 đến {MaxTerm} tháng.",
                _ => ""
            };
        }

        /// <summary>Phê duyệt hồ sơ: lưu "Đã duyệt" và thông số vay. Trả về false nếu lưu thất bại (6a).</summary>
        public bool approveprofile(string idloan, int amount, double rate, int term, string note)
        {
            return lp.updateloanterms(idloan, amount, rate, term,
                string.IsNullOrWhiteSpace(note) ? "Đã phê duyệt" : note.Trim());
        }

        /// <summary>Yêu cầu bổ sung chứng từ (3a): trạng thái "Chờ bổ sung" + lý do.</summary>
        public bool requestsupplement(string idloan, string reason)
        {
            return lp.updateloanstatus(idloan, LoanProfile.StatusSupplement, "Yêu cầu bổ sung: " + reason.Trim());
        }

        public bool rejectprofile(string idloan, string reason)
        {
            return lp.updateloanstatus(idloan, LoanProfile.StatusRejected, "Từ chối: " + reason.Trim());
        }

        /// <summary>Mô phỏng gửi thông báo đến khách hàng (SMS/Email), trả về nội dung đã gửi.</summary>
        public string notifycustomer(string idacc, string message)
        {
            AccountBank? acc = ab.detailaccount(idacc);
            if (acc == null) return "";
            return $"Đã gửi thông báo tới {acc.fullname} (SĐT {acc.phone}, email {acc.email}):\n\"{message}\"";
        }

        // ================= Tổng hợp số liệu Dashboard & Tiện ích UI =================

        public List<AccountBank> loadallcustomers() => ab.queryaccount("");

        public List<LoanProfile> loadallprofiles() => lp.queryallprofiles();

        public List<CardLimitHistoryItem> loadcardlimithistory(string idcard = "") => cc.querylimithistory(idcard);

        public static (long principal, long interest, long total) estimateMonthlyPayment(int remainOrAmount, double annualRate, int months)
        {
            if (remainOrAmount <= 0 || months <= 0) return (0, 0, 0);
            double monthlyRate = annualRate / 100.0 / 12.0;
            long interest = (long)Math.Round(remainOrAmount * monthlyRate);
            long principal = remainOrAmount / months;
            return (principal, interest, principal + interest);
        }

        public CreditDashboardSummary loaddashboardsummary()
        {
            var pending = lp.querypendingprofiles();
            var debts = lp.querydebtlist("");
            var cards = cc.querycardlist("");
            var customers = ab.queryaccount("");
            var overdueList = debts.Where(d => d.debtstate == "Quá hạn").ToList();
            var activeCards = cards.Where(c => c.status == CreditCard.StatusActive).ToList();

            return new CreditDashboardSummary
            {
                TotalCustomers = customers.Count,
                PendingLoansCount = pending.Count,
                WaitingApprovalCount = pending.Count(p => p.status == LoanProfile.StatusPending),
                SupplementCount = pending.Count(p => p.status == LoanProfile.StatusSupplement),
                ActiveLoansCount = debts.Count,
                OverdueLoansCount = overdueList.Count,
                TotalRemainingDebt = debts.Sum(d => (long)d.remainamount),
                OverdueRemainingDebt = overdueList.Sum(d => (long)d.remainamount),
                ActiveCardsCount = activeCards.Count,
                TotalCardsCount = cards.Count,
                TotalCurrentCardLimit = activeCards.Sum(c => (long)c.currentlimit),
                PendingProfiles = pending,
                OverdueDebts = overdueList
            };
        }
    }

    internal class CreditDashboardSummary
    {
        public int TotalCustomers { get; set; }
        public int PendingLoansCount { get; set; }
        public int WaitingApprovalCount { get; set; }
        public int SupplementCount { get; set; }
        public int ActiveLoansCount { get; set; }
        public int OverdueLoansCount { get; set; }
        public long TotalRemainingDebt { get; set; }
        public long OverdueRemainingDebt { get; set; }
        public int ActiveCardsCount { get; set; }
        public int TotalCardsCount { get; set; }
        public long TotalCurrentCardLimit { get; set; }
        public List<LoanProfile> PendingProfiles { get; set; } = new();
        public List<LoanProfile> OverdueDebts { get; set; } = new();
    }
}
