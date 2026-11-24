using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;
using static BankingManagesandServicesApp.DBManages.DBConnectandManage;

namespace BankingManagesandServicesApp.DBManages
{
    /// <summary>
    /// Lưu trữ và truy xuất thông tin giao dịch ngân hàng (bảng TransactionBank).
    /// </summary>
    public class TransactionBank
    {
        public string idtrs { get; set; } = "";
        public string idacc { get; set; } = "";
        public decimal amount { get; set; }
        public string transactionType { get; set; } = "";
        public string status { get; set; } = "";
        public DateTime transactionTime { get; set; }
        public string description { get; set; } = "";
        public bool isFlagged { get; set; }
        public string flagReason { get; set; } = "";
        public DateTime? flaggedAt { get; set; }

        private static TransactionBank FromRow(DataRow r)
        {
            return new TransactionBank
            {
                idtrs = Str(r, "idtrs"),
                idacc = Str(r, "ID"),
                amount = Dec(r, "amount"),
                transactionType = Str(r, "transactionType"),
                status = Str(r, "status"),
                transactionTime = Date(r, "transactionTime") ?? DateTime.MinValue,
                description = Str(r, "description"),
                isFlagged = Bool(r, "isFlagged"),
                flagReason = Str(r, "flagReason"),
                flaggedAt = Date(r, "flaggedAt")
            };
        }

        /// <summary>Sinh mã giao dịch duy nhất: TRS + thời gian + số ngẫu nhiên.</summary>
        public static string NewId()
        {
            return "TRS" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + Random.Shared.Next(100, 1000);
        }

        /// <summary>Lấy chi tiết một giao dịch theo mã.</summary>
        public TransactionBank? getDetail(string idtrs)
        {
            DataTable t = ExecuteQuery("SELECT * FROM TransactionBank WHERE idtrs = @id", P("@id", idtrs));
            return t.Rows.Count == 0 ? null : FromRow(t.Rows[0]);
        }

        /// <summary>Truy vấn danh sách giao dịch theo điều kiện (mã tài khoản, mã giao dịch hoặc loại giao dịch).</summary>
        public List<TransactionBank> queryTransactions(string condition)
        {
            const string sql = @"SELECT * FROM TransactionBank
                                 WHERE @c = '' OR ID = @c OR idtrs = @c OR transactionType = @c
                                 ORDER BY transactionTime DESC";
            var list = new List<TransactionBank>();
            foreach (DataRow r in ExecuteQuery(sql, P("@c", condition.Trim())).Rows)
                list.Add(FromRow(r));
            return list;
        }

        /// <summary>Đánh dấu giao dịch hiện tại là nghi vấn.</summary>
        public bool markTransaction(string reason)
        {
            const string sql = "UPDATE TransactionBank SET isFlagged = 1, flagReason = @r, flaggedAt = GETDATE() WHERE idtrs = @id";
            return ExecuteNonQuery(sql, P("@r", reason), P("@id", idtrs)) == 1;
        }

        /// <summary>Xuất chứng từ giao dịch ra file (format: "txt"). Trả về đường dẫn file.</summary>
        public string exportReceipt(string format)
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== CHỨNG TỪ GIAO DỊCH =====");
            sb.AppendLine("Mã giao dịch : " + idtrs);
            sb.AppendLine("Tài khoản    : " + idacc);
            sb.AppendLine("Loại         : " + transactionType);
            sb.AppendLine("Số tiền      : " + amount.ToString("N0") + " VNĐ");
            sb.AppendLine("Thời gian    : " + transactionTime.ToString("dd/MM/yyyy HH:mm:ss"));
            sb.AppendLine("Nội dung     : " + description);
            sb.AppendLine("Trạng thái   : " + status);
            string path = Path.Combine(Path.GetTempPath(), idtrs + "." + (string.IsNullOrWhiteSpace(format) ? "txt" : format));
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            return path;
        }

        /// <summary>(Bổ sung cho Chuyển khoản) Ghi giao dịch hiện tại vào CSDL trong một transaction đang mở.</summary>
        public bool addTransaction(SqlConnection conn, SqlTransaction tran)
        {
            const string sql = @"INSERT INTO TransactionBank(idtrs,ID,amount,transactionType,status,transactionTime,description,isFlagged,flagReason,flaggedAt)
                                 VALUES(@idtrs,@id,@amount,@type,@status,@time,@desc,0,NULL,NULL)";
            using var cmd = CreateCommand(conn, tran, sql,
                P("@idtrs", idtrs), P("@id", idacc), P("@amount", amount), P("@type", transactionType),
                P("@status", status), P("@time", transactionTime), P("@desc", description));
            return cmd.ExecuteNonQuery() == 1;
        }
    }
}
