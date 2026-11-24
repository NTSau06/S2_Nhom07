using System;
using Microsoft.Data.SqlClient;
using static BankingManagesandServicesApp.DBManages.DBConnectandManage;

namespace BankingManagesandServicesApp.DBManages
{
    /// <summary>
    /// Lưu thông tin chi tiết về các hồ sơ đăng ký vay và trạng thái duyệt vay (bảng LoanApplicationRecord).
    /// </summary>
    public class LoanApplicationRecord
    {
        public string idLoan { get; set; } = "";
        public string idacc { get; set; } = "";
        public double loanAmount { get; set; }
        public int loanTerm { get; set; }
        public DateTime submissionDate { get; set; }
        public string status { get; set; } = "";

        /// <summary>Tạo mới hồ sơ đăng ký vay (và hồ sơ LoanProfile "Chờ duyệt" tương ứng), trả về mã hồ sơ.</summary>
        public string createApplication()
        {
            if (string.IsNullOrEmpty(idLoan))
                idLoan = "LN" + DateTime.Now.ToString("yyMMddHHmmss");
            if (submissionDate == default) submissionDate = DateTime.Today;
            status = LoanProfile.StatusPending;
            ExecuteInTransaction((conn, tran) =>
            {
                using var cmd = CreateCommand(conn, tran,
                    @"INSERT INTO LoanApplicationRecord(idLoan,ID,loanAmount,loanTerm,submissionDate,status)
                      VALUES(@id,@acc,@amount,@term,@date,@st);
                      INSERT INTO LoanProfile(idloan,ID,loanamount,interestrate,loanterm,paidamount,remainamount,creditscore,status)
                      VALUES(@id,@acc,@amount,0,@term,0,0,0,@st);",
                    P("@id", idLoan), P("@acc", idacc), P("@amount", loanAmount), P("@term", loanTerm),
                    P("@date", submissionDate), P("@st", status));
                cmd.ExecuteNonQuery();
            });
            return idLoan;
        }

        /// <summary>Cập nhật trạng thái xử lý của hồ sơ vay.</summary>
        public bool updateStatus(string status)
        {
            bool ok = false;
            ExecuteInTransaction((conn, tran) => ok = updateStatus(status, conn, tran));
            return ok;
        }

        /// <summary>Cập nhật trạng thái trong một transaction đang mở (hồ sơ cũ có thể không có bản ghi đăng ký).</summary>
        public bool updateStatus(string status, SqlConnection conn, SqlTransaction tran)
        {
            using var cmd = CreateCommand(conn, tran,
                "UPDATE LoanApplicationRecord SET status = @st WHERE idLoan = @id",
                P("@st", status), P("@id", idLoan));
            this.status = status;
            return cmd.ExecuteNonQuery() == 1;
        }
    }
}
