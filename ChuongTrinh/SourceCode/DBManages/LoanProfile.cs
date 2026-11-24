using System;
using System.Collections.Generic;
using System.Data;
using static BankingManagesandServicesApp.DBManages.DBConnectandManage;

namespace BankingManagesandServicesApp.DBManages
{
    /// <summary>
    /// Lớp kết nối, truy vấn và tương tác qua lại với cơ sở dữ liệu hồ sơ vay và dư nợ (bảng LoanProfile).
    /// </summary>
    public class LoanProfile
    {
        // Trạng thái hồ sơ vay
        public const string StatusPending = "Chờ duyệt";
        public const string StatusSupplement = "Chờ bổ sung";
        public const string StatusApproved = "Đã duyệt";
        public const string StatusRejected = "Từ chối";
        public const string StatusSettled = "Đã tất toán";

        // ----- Thuộc tính theo Class Diagram -----
        public string idloan { get; set; } = "";
        public string idacc { get; set; } = "";
        public int loanamount { get; set; }
        public double interestrate { get; set; }   // %/năm
        public int loanterm { get; set; }          // tháng
        public int paidamount { get; set; }
        public int remainamount { get; set; }
        public string debtgroup { get; set; } = "";
        public int creditscore { get; set; }
        public string attachdocs { get; set; } = "";
        public string status { get; set; } = "";

        // ----- Thuộc tính bổ sung (cột note, approveddate thêm trong SQL_ChuyenKhoan_TinDung.sql) -----
        public string note { get; set; } = "";
        public DateTime? approveddate { get; set; }

        // ----- Thuộc tính hiển thị (lấy từ bảng liên quan / tính toán, không lưu CSDL) -----
        public string fullname { get; set; } = "";
        public string CCCD { get; set; } = "";
        public DateTime? submissiondate { get; set; }

        /// <summary>Ngày đến hạn = ngày duyệt + kỳ hạn (tháng).</summary>
        public DateTime? duedate => approveddate?.AddMonths(loanterm);

        /// <summary>Phân loại nợ: trong hạn / quá hạn.</summary>
        public string debtstate
        {
            get
            {
                if (status != StatusApproved || remainamount <= 0) return "";
                return duedate.HasValue && duedate.Value.Date < DateTime.Today ? "Quá hạn" : "Trong hạn";
            }
        }

        /// <summary>Tình trạng trả nợ dùng cho màn hình tra cứu lịch sử tín dụng.</summary>
        public string repaymentstate
        {
            get
            {
                if (status == StatusSettled) return "Đã tất toán";
                if (status == StatusApproved)
                {
                    double percent = loanamount > 0 ? paidamount * 100.0 / loanamount : 0;
                    return $"Đang trả nợ ({percent:0.#}%) - {debtstate}";
                }
                return "Chưa giải ngân (" + status + ")";
            }
        }

        public string attachdocsdisplay => attachdocs.Replace(";", Environment.NewLine);
        public double paidpercent => loanamount > 0 ? Math.Min(100.0, Math.Max(0.0, paidamount * 100.0 / loanamount)) : 0;
        public string paidpercenttext => $"{paidpercent:0.#}%";

        private const string SelectSql = @"SELECT lp.*, ab.fullname, ab.CCCD, lar.submissionDate
                                           FROM LoanProfile lp
                                           JOIN AccountBank ab ON ab.ID = lp.ID
                                           LEFT JOIN LoanApplicationRecord lar ON lar.idLoan = lp.idloan ";

        private static LoanProfile FromRow(DataRow r)
        {
            return new LoanProfile
            {
                idloan = Str(r, "idloan"),
                idacc = Str(r, "ID"),
                loanamount = Int(r, "loanamount"),
                interestrate = Dbl(r, "interestrate"),
                loanterm = Int(r, "loanterm"),
                paidamount = Int(r, "paidamount"),
                remainamount = Int(r, "remainamount"),
                debtgroup = Str(r, "debtgroup"),
                creditscore = Int(r, "creditscore"),
                attachdocs = Str(r, "attachdocs"),
                status = Str(r, "status"),
                note = Str(r, "note"),
                approveddate = Date(r, "approveddate"),
                fullname = Str(r, "fullname"),
                CCCD = Str(r, "CCCD"),
                submissiondate = Date(r, "submissionDate")
            };
        }

        private static List<LoanProfile> ToList(DataTable t)
        {
            var list = new List<LoanProfile>();
            foreach (DataRow r in t.Rows) list.Add(FromRow(r));
            return list;
        }

        /// <summary>Truy vấn lịch sử tín dụng (tất cả khoản vay) của một khách hàng theo mã tài khoản.</summary>
        public List<LoanProfile> querycredithistory(string querysqltext)
        {
            return ToList(ExecuteQuery(SelectSql + "WHERE lp.ID = @id ORDER BY lp.idloan DESC", P("@id", querysqltext.Trim())));
        }

        /// <summary>
        /// Truy vấn danh sách các khoản nợ đang còn dư nợ (đã duyệt, còn tiền phải trả).
        /// querysqltext: mã tài khoản để lọc theo khách hàng; rỗng = tất cả khách hàng.
        /// </summary>
        public List<LoanProfile> querydebtlist(string querysqltext)
        {
            string sql = SelectSql + "WHERE lp.status = @st AND lp.remainamount > 0 AND (@id = '' OR lp.ID = @id) ORDER BY lp.remainamount DESC";
            return ToList(ExecuteQuery(sql, P("@st", StatusApproved), P("@id", querysqltext.Trim())));
        }

        /// <summary>Lấy thông tin chi tiết một khoản nợ.</summary>
        public LoanProfile? detaildebt(string querysqlidtext)
        {
            return detailloanprofile(querysqlidtext);
        }

        /// <summary>Lấy thông tin chi tiết một hồ sơ vay.</summary>
        public LoanProfile? detailloanprofile(string querysqlidtext)
        {
            List<LoanProfile> list = ToList(ExecuteQuery(SelectSql + "WHERE lp.idloan = @id", P("@id", querysqlidtext.Trim())));
            return list.Count == 0 ? null : list[0];
        }

        /// <summary>Cập nhật trạng thái hồ sơ vay kèm ghi chú (đồng bộ trạng thái sang LoanApplicationRecord).</summary>
        public bool updateloanstatus(string idloan, string status, string note)
        {
            bool ok = false;
            ExecuteInTransaction((conn, tran) =>
            {
                using var cmd = CreateCommand(conn, tran,
                    "UPDATE LoanProfile SET status = @st, note = @note WHERE idloan = @id",
                    P("@st", status), P("@note", note), P("@id", idloan));
                ok = cmd.ExecuteNonQuery() == 1;
                new LoanApplicationRecord { idLoan = idloan }.updateStatus(status, conn, tran);
            });
            return ok;
        }

        // ================= Phương thức bổ sung =================

        /// <summary>(Bổ sung) Danh sách hồ sơ đang chờ xử lý: "Chờ duyệt" và "Chờ bổ sung".</summary>
        public List<LoanProfile> querypendingprofiles()
        {
            string sql = SelectSql + "WHERE lp.status IN (@p1, @p2) ORDER BY lar.submissionDate, lp.idloan";
            return ToList(ExecuteQuery(sql, P("@p1", StatusPending), P("@p2", StatusSupplement)));
        }

        /// <summary>Truy vấn toàn bộ hồ sơ vay (phục vụ dashboard và lịch sử xét duyệt).</summary>
        public List<LoanProfile> queryallprofiles()
        {
            string sql = SelectSql + "ORDER BY CASE WHEN lp.status IN (N'Chờ duyệt', N'Chờ bổ sung') THEN 0 ELSE 1 END, lp.idloan DESC";
            return ToList(ExecuteQuery(sql));
        }

        /// <summary>
        /// (Bổ sung) Phê duyệt hồ sơ: lưu hạn mức (số tiền vay), lãi suất, thời hạn, trạng thái "Đã duyệt".
        /// Dư nợ ban đầu = số tiền vay, nhóm nợ = Nhóm 1. Chỉ duyệt được hồ sơ đang chờ.
        /// </summary>
        public bool updateloanterms(string idloan, int loanamount, double interestrate, int loanterm, string note)
        {
            bool ok = false;
            ExecuteInTransaction((conn, tran) =>
            {
                const string sql = @"UPDATE LoanProfile
                                     SET loanamount = @amount, interestrate = @rate, loanterm = @term,
                                         paidamount = 0, remainamount = @amount, debtgroup = N'Nhóm 1',
                                         status = @st, note = @note, approveddate = CAST(GETDATE() AS DATE)
                                     WHERE idloan = @id AND status IN (@p1, @p2)";
                using var cmd = CreateCommand(conn, tran, sql,
                    P("@amount", loanamount), P("@rate", interestrate), P("@term", loanterm),
                    P("@st", StatusApproved), P("@note", note), P("@id", idloan),
                    P("@p1", StatusPending), P("@p2", StatusSupplement));
                ok = cmd.ExecuteNonQuery() == 1;
                if (ok) new LoanApplicationRecord { idLoan = idloan }.updateStatus(StatusApproved, conn, tran);
            });
            return ok;
        }
    }
}
