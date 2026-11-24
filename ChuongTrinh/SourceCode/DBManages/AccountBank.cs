using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using static BankingManagesandServicesApp.DBManages.DBConnectandManage;

namespace BankingManagesandServicesApp.DBManages
{
    /// <summary>
    /// Lớp kết nối, truy vấn và tương tác qua lại với cơ sở dữ liệu tài khoản giao dịch (bảng AccountBank).
    /// Thuộc tính/phương thức đặt tên theo Class Diagram.
    /// </summary>
    public class AccountBank
    {
        public string idacc { get; set; } = "";
        public string passwordacc { get; set; } = "";
        public string fullname { get; set; } = "";
        public string gender { get; set; } = "";
        public DateTime birthday { get; set; }
        public string CCCD { get; set; } = "";
        public string phone { get; set; } = "";
        public string address { get; set; } = "";
        public string email { get; set; } = "";
        public string typeacc { get; set; } = "";
        public DateTime provideaccdate { get; set; }
        public int money { get; set; }

        private static AccountBank FromRow(DataRow r)
        {
            return new AccountBank
            {
                idacc = Str(r, "ID"),
                passwordacc = Str(r, "password"),
                fullname = Str(r, "fullname"),
                gender = Str(r, "gender"),
                birthday = Date(r, "birthday") ?? DateTime.MinValue,
                CCCD = Str(r, "CCCD"),
                phone = Str(r, "phone"),
                address = Str(r, "address"),
                email = Str(r, "email"),
                typeacc = Str(r, "type"),
                provideaccdate = Date(r, "providedate") ?? DateTime.MinValue,
                money = Int(r, "money")
            };
        }

        /// <summary>Thêm một tài khoản mới vào CSDL.</summary>
        public bool addaccount(AccountBank newaccount)
        {
            const string sql = @"INSERT INTO AccountBank(ID,password,fullname,gender,birthday,CCCD,phone,address,email,type,providedate,money)
                                 VALUES(@id,@pw,@name,@gender,@birthday,@cccd,@phone,@address,@email,@type,@date,@money)";
            return ExecuteNonQuery(sql,
                P("@id", newaccount.idacc), P("@pw", newaccount.passwordacc), P("@name", newaccount.fullname),
                P("@gender", newaccount.gender), P("@birthday", newaccount.birthday), P("@cccd", newaccount.CCCD),
                P("@phone", newaccount.phone), P("@address", newaccount.address), P("@email", newaccount.email),
                P("@type", newaccount.typeacc), P("@date", newaccount.provideaccdate), P("@money", newaccount.money)) == 1;
        }

        /// <summary>Truy vấn chi tiết một tài khoản theo mã tài khoản.</summary>
        public AccountBank? detailaccount(string querysqlidtext)
        {
            DataTable t = ExecuteQuery("SELECT * FROM AccountBank WHERE ID = @id", P("@id", querysqlidtext.Trim()));
            return t.Rows.Count == 0 ? null : FromRow(t.Rows[0]);
        }

        /// <summary>
        /// Truy vấn một hoặc nhiều tài khoản theo từ khóa (mã tài khoản, CCCD, số điện thoại, họ tên).
        /// Từ khóa rỗng: trả về toàn bộ.
        /// </summary>
        public List<AccountBank> queryaccount(string querysqltext)
        {
            const string sql = @"SELECT * FROM AccountBank
                                 WHERE @kw = '' OR ID = @kw OR CCCD = @kw OR phone = @kw OR fullname LIKE N'%' + @kw + N'%'
                                 ORDER BY ID";
            var list = new List<AccountBank>();
            foreach (DataRow r in ExecuteQuery(sql, P("@kw", querysqltext.Trim())).Rows)
                list.Add(FromRow(r));
            return list;
        }

        /// <summary>Cập nhật thông tin một tài khoản.</summary>
        public bool updateaccount(AccountBank newvalueaccount)
        {
            const string sql = @"UPDATE AccountBank SET password=@pw, fullname=@name, gender=@gender, birthday=@birthday,
                                 CCCD=@cccd, phone=@phone, address=@address, email=@email, type=@type, money=@money
                                 WHERE ID=@id";
            return ExecuteNonQuery(sql,
                P("@id", newvalueaccount.idacc), P("@pw", newvalueaccount.passwordacc), P("@name", newvalueaccount.fullname),
                P("@gender", newvalueaccount.gender), P("@birthday", newvalueaccount.birthday), P("@cccd", newvalueaccount.CCCD),
                P("@phone", newvalueaccount.phone), P("@address", newvalueaccount.address), P("@email", newvalueaccount.email),
                P("@type", newvalueaccount.typeacc), P("@money", newvalueaccount.money)) == 1;
        }

        /// <summary>Truy vấn số tiền hiện có của một tài khoản. Trả về -1 nếu không tồn tại.</summary>
        public int querymoney(string querysqlmoneytext)
        {
            object? v = ExecuteScalar("SELECT money FROM AccountBank WHERE ID = @id", P("@id", querysqlmoneytext.Trim()));
            return v == null ? -1 : Convert.ToInt32(v);
        }

        /// <summary>
        /// (Bổ sung cho Chuyển khoản) Cộng/trừ số dư trong một transaction đang mở.
        /// Chỉ cập nhật khi số dư sau giao dịch vẫn >= minremain. Trả về false nếu không đủ số dư.
        /// </summary>
        public bool updatemoney(string idacc, int delta, int minremain, SqlConnection conn, SqlTransaction tran)
        {
            const string sql = @"UPDATE AccountBank SET money = money + @delta
                                 WHERE ID = @id AND money + @delta >= @min";
            using var cmd = CreateCommand(conn, tran, sql, P("@delta", delta), P("@id", idacc), P("@min", minremain));
            return cmd.ExecuteNonQuery() == 1;
        }
    }
}
