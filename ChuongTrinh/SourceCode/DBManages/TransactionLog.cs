using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using static BankingManagesandServicesApp.DBManages.DBConnectandManage;

namespace BankingManagesandServicesApp.DBManages
{
    /// <summary>
    /// Lớp ghi nhận nhật ký các sự kiện phát sinh đối với giao dịch (bảng TransactionLog).
    /// </summary>
    public class TransactionLog
    {
        public long logId { get; set; }
        public string idtrs { get; set; } = "";
        public string action { get; set; } = "";
        public DateTime timestamp { get; set; }
        public string result { get; set; } = "";
        public string details { get; set; } = "";

        private static TransactionLog FromRow(DataRow r)
        {
            return new TransactionLog
            {
                logId = Convert.ToInt64(r["logId"]),
                idtrs = Str(r, "idtrs"),
                action = Str(r, "action"),
                timestamp = Date(r, "timestamp") ?? DateTime.MinValue,
                result = Str(r, "result"),
                details = Str(r, "details")
            };
        }

        /// <summary>Lấy danh sách tất cả nhật ký giao dịch.</summary>
        public List<TransactionLog> getLogList()
        {
            return searchLogs("");
        }

        /// <summary>Tìm kiếm nhật ký theo từ khóa (mã giao dịch, hành động, chi tiết).</summary>
        public List<TransactionLog> searchLogs(string keyword)
        {
            const string sql = @"SELECT * FROM TransactionLog
                                 WHERE @kw = '' OR idtrs = @kw OR action LIKE N'%' + @kw + N'%' OR details LIKE N'%' + @kw + N'%'
                                 ORDER BY timestamp DESC";
            var list = new List<TransactionLog>();
            foreach (DataRow r in ExecuteQuery(sql, P("@kw", keyword.Trim())).Rows)
                list.Add(FromRow(r));
            return list;
        }

        /// <summary>Ghi nhật ký hiện tại vào CSDL (tự mở kết nối).</summary>
        public bool writeLog()
        {
            bool ok = false;
            ExecuteInTransaction((conn, tran) => ok = writeLog(conn, tran));
            return ok;
        }

        /// <summary>Ghi nhật ký trong một transaction đang mở (dùng khi chuyển khoản).</summary>
        public bool writeLog(SqlConnection conn, SqlTransaction tran)
        {
            const string sql = @"DECLARE @newId BIGINT = (SELECT ISNULL(MAX(logId), 0) + 1 FROM TransactionLog WITH (UPDLOCK, HOLDLOCK));
                                 INSERT INTO TransactionLog(logId,idtrs,action,timestamp,result,details)
                                 VALUES(@newId,@idtrs,@action,@time,@result,@details);
                                 SELECT @newId;";
            using var cmd = CreateCommand(conn, tran, sql,
                P("@idtrs", idtrs), P("@action", action), P("@time", timestamp == default ? DateTime.Now : timestamp),
                P("@result", result), P("@details", details));
            object? id = cmd.ExecuteScalar();
            if (id == null || id == DBNull.Value) return false;
            logId = Convert.ToInt64(id);
            return true;
        }
    }
}
