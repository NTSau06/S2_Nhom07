using System;
using System.Collections.Generic;
using System.Data;
using static BankingManagesandServicesApp.DBManages.DBConnectandManage;

namespace BankingManagesandServicesApp.DBManages
{
    /// <summary>
    /// Lưu thông tin chi tiết về thẻ tín dụng: hạn mức, màu sắc, kiểu dáng, trạng thái (bảng Creditcard).
    /// </summary>
    public class CreditCard
    {
        public const string StatusActive = "Hoạt động";

        public string idcard { get; set; } = "";
        public string idacc { get; set; } = "";
        public string cardtype { get; set; } = "";
        public string cardcolor { get; set; } = "";
        public string cardstyle { get; set; } = "";
        public int currentlimit { get; set; }
        public int maxlimit { get; set; }
        public string status { get; set; } = "";

        // Thuộc tính hiển thị (lấy từ AccountBank / tính toán)
        public string fullname { get; set; } = "";
        public double limitpercent => maxlimit > 0 ? Math.Min(100.0, currentlimit * 100.0 / maxlimit) : 0;
        public string maskedcardnumber => $"••••  ••••  ••••  {(idcard.Length >= 4 ? idcard.Substring(idcard.Length - 4) : idcard)}";

        private const string SelectSql = @"SELECT c.*, ab.fullname FROM Creditcard c JOIN AccountBank ab ON ab.ID = c.ID ";

        private static CreditCard FromRow(DataRow r)
        {
            return new CreditCard
            {
                idcard = Str(r, "idcard"),
                idacc = Str(r, "ID"),
                cardtype = Str(r, "cardtype"),
                cardcolor = Str(r, "cardcolor"),
                cardstyle = Str(r, "cardstyle"),
                currentlimit = Int(r, "currentlimit"),
                maxlimit = Int(r, "maxlimit"),
                status = Str(r, "status"),
                fullname = Str(r, "fullname")
            };
        }

        /// <summary>Thêm một thẻ tín dụng vừa được khách hàng đăng ký.</summary>
        public bool addcardregistration(CreditCard newcard)
        {
            const string sql = @"INSERT INTO Creditcard(idcard,ID,cardtype,cardcolor,cardstyle,currentlimit,maxlimit,status)
                                 VALUES(@id,@acc,@type,@color,@style,@cur,@max,@st)";
            return ExecuteNonQuery(sql,
                P("@id", newcard.idcard), P("@acc", newcard.idacc), P("@type", newcard.cardtype),
                P("@color", newcard.cardcolor), P("@style", newcard.cardstyle), P("@cur", newcard.currentlimit),
                P("@max", newcard.maxlimit), P("@st", newcard.status)) == 1;
        }

        /// <summary>Truy xuất hạn mức (thông tin) của một thẻ tín dụng theo mã thẻ.</summary>
        public CreditCard? querycardlimit(string querysqltext)
        {
            DataTable t = ExecuteQuery(SelectSql + "WHERE c.idcard = @id", P("@id", querysqltext.Trim()));
            return t.Rows.Count == 0 ? null : FromRow(t.Rows[0]);
        }

        /// <summary>Cập nhật hạn mức của thẻ; lý do điều chỉnh được lưu vào nhật ký hoạt động (ActivityLog).</summary>
        public bool updatecardlimit(string idcard, int newlimit, string reason)
        {
            bool ok = false;
            ExecuteInTransaction((conn, tran) =>
            {
                int oldlimit;
                using (var get = CreateCommand(conn, tran, "SELECT currentlimit FROM Creditcard WITH (UPDLOCK) WHERE idcard = @id", P("@id", idcard)))
                {
                    object? v = get.ExecuteScalar();
                    if (v == null || v == DBNull.Value) return;
                    oldlimit = Convert.ToInt32(v);
                }

                using (var upd = CreateCommand(conn, tran,
                    "UPDATE Creditcard SET currentlimit = @limit WHERE idcard = @id AND @limit <= maxlimit",
                    P("@limit", newlimit), P("@id", idcard)))
                {
                    ok = upd.ExecuteNonQuery() == 1;
                }
                if (!ok) return;

                const string logSql = @"DECLARE @newId BIGINT = (SELECT ISNULL(MAX(logId), 0) + 1 FROM ActivityLog WITH (UPDLOCK, HOLDLOCK));
                                        INSERT INTO ActivityLog(logId,accountId,action,timestamp,result,details)
                                        VALUES(@newId,NULL,N'Điều chỉnh hạn mức thẻ tín dụng',GETDATE(),N'Thành công',@details);";
                string details = $"Thẻ {idcard}: {oldlimit:N0} -> {newlimit:N0} VNĐ. Lý do: {reason}";
                using var log = CreateCommand(conn, tran, logSql, P("@details", details));
                log.ExecuteNonQuery();
            });
            return ok;
        }

        /// <summary>(Bổ sung) Danh sách thẻ theo từ khóa: mã thẻ, mã tài khoản, CCCD hoặc họ tên chủ thẻ.</summary>
        public List<CreditCard> querycardlist(string querysqltext)
        {
            string sql = SelectSql + @"WHERE @kw = '' OR c.idcard = @kw OR c.ID = @kw OR ab.CCCD = @kw OR ab.fullname LIKE N'%' + @kw + N'%'
                                       ORDER BY c.idcard";
            var list = new List<CreditCard>();
            foreach (DataRow r in ExecuteQuery(sql, P("@kw", querysqltext.Trim())).Rows)
                list.Add(FromRow(r));
            return list;
        }

        /// <summary>Truy vấn lịch sử điều chỉnh hạn mức thẻ tín dụng từ bảng ActivityLog.</summary>
        public List<CardLimitHistoryItem> querylimithistory(string idcard = "")
        {
            const string sql = @"SELECT TOP 30 logId, timestamp, result, details
                                 FROM ActivityLog
                                 WHERE action = N'Điều chỉnh hạn mức thẻ tín dụng'
                                   AND (@id = '' OR details LIKE N'Thẻ ' + @id + N':%')
                                 ORDER BY timestamp DESC, logId DESC";
            var list = new List<CardLimitHistoryItem>();
            foreach (DataRow r in ExecuteQuery(sql, P("@id", idcard.Trim())).Rows)
            {
                list.Add(new CardLimitHistoryItem
                {
                    logId = Convert.ToInt64(r["logId"]),
                    timestamp = Date(r, "timestamp") ?? DateTime.MinValue,
                    result = Str(r, "result"),
                    details = Str(r, "details")
                });
            }
            return list;
        }
    }

    public class CardLimitHistoryItem
    {
        public long logId { get; set; }
        public DateTime timestamp { get; set; }
        public string result { get; set; } = "";
        public string details { get; set; } = "";
    }
}
