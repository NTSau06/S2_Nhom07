using System;
using System.Collections.Generic;
using System.Data;
using static BankingManagesandServicesApp.DBManages.DBConnectandManage;

namespace BankingManagesandServicesApp.DBManages
{
    /// <summary>
    /// (Lớp bổ sung) Danh bạ thụ hưởng của khách hàng - bảng BeneficiaryContact.
    /// Phục vụ bước "gợi ý lưu danh bạ thụ hưởng [Extend]" của UC Chuyển khoản.
    /// </summary>
    public class BeneficiaryContact
    {
        public int idbnf { get; set; }
        public string idacc { get; set; } = "";
        public string receiveraccount { get; set; } = "";
        public string receivername { get; set; } = "";
        public string bankname { get; set; } = "";
        public DateTime createddate { get; set; }

        public string display => $"{receivername} - {receiveraccount} ({bankname})";

        /// <summary>Lấy danh bạ thụ hưởng của một tài khoản.</summary>
        public List<BeneficiaryContact> querybeneficiary(string idacc)
        {
            var list = new List<BeneficiaryContact>();
            DataTable t = ExecuteQuery("SELECT * FROM BeneficiaryContact WHERE ID = @id ORDER BY receivername", P("@id", idacc));
            foreach (DataRow r in t.Rows)
            {
                list.Add(new BeneficiaryContact
                {
                    idbnf = Int(r, "idbnf"),
                    idacc = Str(r, "ID"),
                    receiveraccount = Str(r, "receiveraccount"),
                    receivername = Str(r, "receivername"),
                    bankname = Str(r, "bankname"),
                    createddate = Date(r, "createddate") ?? DateTime.MinValue
                });
            }
            return list;
        }

        /// <summary>Kiểm tra người nhận đã có trong danh bạ chưa.</summary>
        public bool checkexists(string idacc, string receiveraccount, string bankname)
        {
            object? v = ExecuteScalar(
                "SELECT COUNT(*) FROM BeneficiaryContact WHERE ID = @id AND receiveraccount = @acc AND bankname = @bank",
                P("@id", idacc), P("@acc", receiveraccount), P("@bank", bankname));
            return v != null && Convert.ToInt32(v) > 0;
        }

        /// <summary>Lưu người thụ hưởng hiện tại vào danh bạ.</summary>
        public bool addbeneficiary()
        {
            if (checkexists(idacc, receiveraccount, bankname)) return true;
            return ExecuteNonQuery(
                @"INSERT INTO BeneficiaryContact(ID,receiveraccount,receivername,bankname,createddate)
                  VALUES(@id,@acc,@name,@bank,GETDATE())",
                P("@id", idacc), P("@acc", receiveraccount), P("@name", receivername), P("@bank", bankname)) == 1;
        }

        /// <summary>Xóa một liên hệ khỏi danh bạ thụ hưởng.</summary>
        public bool deletebeneficiary(int idbnf)
        {
            return ExecuteNonQuery("DELETE FROM BeneficiaryContact WHERE idbnf = @id", P("@id", idbnf)) == 1;
        }
    }
}
