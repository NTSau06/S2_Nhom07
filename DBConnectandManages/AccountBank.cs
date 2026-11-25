using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace BankingManagesandServicesApp.DBConnectandManages
{
    public class AccountBank
    {
        public string idacc { get; set; }
        public string passwordacc { get; set; }
        public string fullname { get; set; }
        public string gender { get; set; }
        public DateOnly birthday { get; set; }
        public string CCCD { get; set; }
        public string phone { get; set; }
        public string address { get; set; }
        public string email { get; set; }
        public string typeacc { get; set; }
        public DateTime provideaccdate { get; set; }
        public int money { get; set; }
        public AccountBank() {} // giữ chỗ khởi tạo đối tượng để gọi phương thức và giá trị thuộc tính thay đổi liên tục
        public AccountBank(string idaccvalue, string passwordaccvalue, string fullnamevalue, string gendervalue, DateOnly birthdayvalue, string CCCDvalue, string phonevalue, string addressvalue, string emailvalue, string typeaccvalue, DateTime provideaccdatevalue, int moneyvalue)
        {
            idacc = idaccvalue;
            passwordacc = passwordaccvalue;
            fullname = fullnamevalue;
            gender = gendervalue;
            birthday = birthdayvalue;
            CCCD = CCCDvalue;
            phone = phonevalue;
            address = addressvalue;
            email = emailvalue;
            typeacc = typeaccvalue;
            provideaccdate = provideaccdatevalue;
            money = moneyvalue;
        }
        public string checkexistaccount_IDbyCCCDandtypeacc(string checkCCCD, string checktypeacc)
        {
            DBConnect dbconnect = new DBConnect();
            SqlConnection checkconnect = dbconnect.GetConnection();
            try
            {
                checkconnect.Open();
                string cautruyvan = @"Select ID from AccountBank where CCCD = @CCCD and type = @type";
                SqlCommand cmd = new SqlCommand(cautruyvan,checkconnect);
                cmd.Parameters.AddWithValue("@CCCD",checkCCCD);
                cmd.Parameters.AddWithValue("@type",checktypeacc);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read()==true)
                {
                    return "Có tồn tại";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return "";
        }
        public string checkexistaccount_IDbyPhoneandEmail(string checkPhone, string checkEmail)
        {
            DBConnect dbconnect = new DBConnect();
            SqlConnection checkconnect = dbconnect.GetConnection();
            try
            {
                checkconnect.Open();
                string cautruyvan = @"Select ID from AccountBank where phone = @phone and email = @email";
                SqlCommand cmd = new SqlCommand(cautruyvan, checkconnect);
                cmd.Parameters.AddWithValue("@phone", checkPhone);
                cmd.Parameters.AddWithValue("@email", checkEmail);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read() == true)
                {
                    return "Có tồn tại";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return "";
        }
        public string checkexistaccount_IDbyCCCD(string checkCCCD)
        {
            DBConnect dbconnect = new DBConnect();
            SqlConnection checkconnect = dbconnect.GetConnection();
            try
            {
                checkconnect.Open();
                string cautruyvan = @"Select ID from AccountBank where CCCD = @CCCD";
                SqlCommand cmd = new SqlCommand(cautruyvan, checkconnect);
                cmd.Parameters.AddWithValue("@CCCD", checkCCCD);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read() == true)
                {
                    return "Có tồn tại";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return "";
        }
        public bool addaccount(AccountBank newaccount) // thêm dữ liệu tài khoản mới vào cơ sở dữ liệu bên sql server
        {
            DBConnect dbconnect = new DBConnect();
            SqlConnection checkconnect = dbconnect.GetConnection();
            try
            {
                checkconnect.Open();
                string cautruyvan = @"INSERT INTO AccountBank (ID, password, fullname, gender, birthday, CCCD, phone, address, email, type, providedate, money) VALUES (@ID, @password, @fullname, @gender, @birthday, @CCCD, @phone, @address, @email, @type, @providedate, @money)";
                SqlCommand cmd = new SqlCommand(cautruyvan, checkconnect);
                cmd.Parameters.AddWithValue("@ID",newaccount.idacc);
                cmd.Parameters.AddWithValue("@password",newaccount.passwordacc);
                cmd.Parameters.AddWithValue("@fullname",newaccount.fullname);
                cmd.Parameters.AddWithValue("@gender",newaccount.gender);
                cmd.Parameters.AddWithValue("@birthday", newaccount.birthday);
                cmd.Parameters.AddWithValue("@CCCD",newaccount.CCCD);
                cmd.Parameters.AddWithValue("@phone",newaccount.phone);
                cmd.Parameters.AddWithValue("@address",newaccount.address);
                cmd.Parameters.AddWithValue("@email",newaccount.email);
                cmd.Parameters.AddWithValue("@type", newaccount.typeacc);
                cmd.Parameters.AddWithValue("@providedate",newaccount.provideaccdate);
                cmd.Parameters.AddWithValue("@money",newaccount.money);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return false;
        }
        public List<AccountBank> queryallaccount() // truy vấn tất cả dòng dữ liệu bên sql server ( không cần bất kỳ điều kiện nào )
        {
            DBConnect dbconnect = new DBConnect();
            SqlConnection checkconnect = dbconnect.GetConnection();
            List<AccountBank> list = new List<AccountBank>();
            try
            {

                checkconnect.Open();
                string cautruyvan = @"SELECT ID, password, fullname, gender, birthday, CCCD, phone, address, email, type, providedate, money FROM AccountBank";
                SqlCommand cmd = new SqlCommand(cautruyvan,checkconnect);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string idaccvalue = reader["ID"]?.ToString() ?? "";
                    string passwordaccvalue = reader["password"]?.ToString() ?? "";
                    string fullnamevalue = reader["fullname"]?.ToString() ?? "";
                    string gendervalue = reader["gender"]?.ToString() ?? "";

                    DateOnly birthdayvalue = reader["birthday"] != DBNull.Value
                        ? DateOnly.FromDateTime(Convert.ToDateTime(reader["birthday"]))
                        : DateOnly.MinValue;

                    string CCCDvalue = reader["CCCD"]?.ToString() ?? "";
                    string phonevalue = reader["phone"]?.ToString() ?? "";
                    string addressvalue = reader["address"]?.ToString() ?? "";
                    string emailvalue = reader["email"] != DBNull.Value
                        ? (reader["email"]?.ToString() ?? "")
                        : "";
                    string typeaccvalue = reader["type"]?.ToString() ?? "";

                    DateTime provideaccdatevalue = reader["providedate"] != DBNull.Value
                        ? Convert.ToDateTime(reader["providedate"])
                        : DateTime.MinValue;

                    int moneyvalue = reader["money"] != DBNull.Value
                        ? Convert.ToInt32(reader["money"])
                        : 0;

                    AccountBank item = new AccountBank(
                        idaccvalue,
                        passwordaccvalue,
                        fullnamevalue,
                        gendervalue,
                        birthdayvalue,
                        CCCDvalue,
                        phonevalue,
                        addressvalue,
                        emailvalue,
                        typeaccvalue,
                        provideaccdatevalue,
                        moneyvalue
                    );

                    list.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi queryallaccount: " + ex.Message);
            }

            return list;
        }
        public AccountBank queryaccountbyID(string checkID) // truy vấn 1 dòng dữ liệu có ID bằng checkID ( dùng cho chức năng Xem chi tiết tài khoản , Chỉnh sửa tài khoản )
        {
            DBConnect dbconnect = new DBConnect();
            SqlConnection checkconnect = dbconnect.GetConnection();
            List<AccountBank> list = new List<AccountBank>();
            try
            {

                checkconnect.Open();
                string cautruyvan = @"SELECT ID, password, fullname, gender, birthday, CCCD, phone, address, email, type, providedate, money FROM AccountBank where ID = @ID";
                SqlCommand cmd = new SqlCommand(cautruyvan, checkconnect);
                cmd.Parameters.AddWithValue("@ID", checkID);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string idaccvalue = reader["ID"]?.ToString() ?? "";
                    string passwordaccvalue = reader["password"]?.ToString() ?? "";
                    string fullnamevalue = reader["fullname"]?.ToString() ?? "";
                    string gendervalue = reader["gender"]?.ToString() ?? "";

                    DateOnly birthdayvalue = reader["birthday"] != DBNull.Value
                        ? DateOnly.FromDateTime(Convert.ToDateTime(reader["birthday"]))
                        : DateOnly.MinValue;

                    string CCCDvalue = reader["CCCD"]?.ToString() ?? "";
                    string phonevalue = reader["phone"]?.ToString() ?? "";
                    string addressvalue = reader["address"]?.ToString() ?? "";
                    string emailvalue = reader["email"] != DBNull.Value
                        ? (reader["email"]?.ToString() ?? "")
                        : "";
                    string typeaccvalue = reader["type"]?.ToString() ?? "";

                    DateTime provideaccdatevalue = reader["providedate"] != DBNull.Value
                        ? Convert.ToDateTime(reader["providedate"])
                        : DateTime.MinValue;

                    int moneyvalue = reader["money"] != DBNull.Value
                        ? Convert.ToInt32(reader["money"])
                        : 0;

                    return new AccountBank(
                        idaccvalue,
                        passwordaccvalue,
                        fullnamevalue,
                        gendervalue,
                        birthdayvalue,
                        CCCDvalue,
                        phonevalue,
                        addressvalue,
                        emailvalue,
                        typeaccvalue,
                        provideaccdatevalue,
                        moneyvalue
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi queryallaccount: " + ex.Message);
            }

            return null;
        }
        public AccountBank queryaccountbyCCCD(string checkCCCD) // truy vấn 1 dòng dữ liệu có CCCD bằng checkCCCD
        {
            DBConnect dbconnect = new DBConnect();
            SqlConnection checkconnect = dbconnect.GetConnection();
            List<AccountBank> list = new List<AccountBank>();
            try
            {

                checkconnect.Open();
                string cautruyvan = @"SELECT ID, password, fullname, gender, birthday, CCCD, phone, address, email, type, providedate, money FROM AccountBank where CCCD = @CCCD";
                SqlCommand cmd = new SqlCommand(cautruyvan, checkconnect);
                cmd.Parameters.AddWithValue("@CCCD", checkCCCD);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    string idaccvalue = reader["ID"]?.ToString() ?? "";
                    string passwordaccvalue = reader["password"]?.ToString() ?? "";
                    string fullnamevalue = reader["fullname"]?.ToString() ?? "";
                    string gendervalue = reader["gender"]?.ToString() ?? "";

                    DateOnly birthdayvalue = reader["birthday"] != DBNull.Value
                        ? DateOnly.FromDateTime(Convert.ToDateTime(reader["birthday"]))
                        : DateOnly.MinValue;

                    string CCCDvalue = reader["CCCD"]?.ToString() ?? "";
                    string phonevalue = reader["phone"]?.ToString() ?? "";
                    string addressvalue = reader["address"]?.ToString() ?? "";
                    string emailvalue = reader["email"] != DBNull.Value
                        ? (reader["email"]?.ToString() ?? "")
                        : "";
                    string typeaccvalue = reader["type"]?.ToString() ?? "";

                    DateTime provideaccdatevalue = reader["providedate"] != DBNull.Value
                        ? Convert.ToDateTime(reader["providedate"])
                        : DateTime.MinValue;

                    int moneyvalue = reader["money"] != DBNull.Value
                        ? Convert.ToInt32(reader["money"])
                        : 0;

                    return new AccountBank(
                        idaccvalue,
                        passwordaccvalue,
                        fullnamevalue,
                        gendervalue,
                        birthdayvalue,
                        CCCDvalue,
                        phonevalue,
                        addressvalue,
                        emailvalue,
                        typeaccvalue,
                        provideaccdatevalue,
                        moneyvalue
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi queryallaccount: " + ex.Message);
            }

            return null;
        }
        public List<string> getalldistinctcccd() // lấy các CCCD hiện có
        {
            List<string> cccdList = new List<string>();
            string query = @"SELECT DISTINCT CCCD FROM AccountBank WHERE CCCD IS NOT NULL AND CCCD <> '' ORDER BY CCCD";

            DBConnect dbconnect = new DBConnect();
            try
            {
                using SqlConnection conn = dbconnect.GetConnection();
                using SqlCommand cmd = new SqlCommand(query, conn);

                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    string cccd = reader["CCCD"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(cccd))
                    {
                        cccdList.Add(cccd);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi getalldistinctcccd: " + ex.Message);
            }

            return cccdList;
        }
        public List<AccountBank> searchaccounts( // Tìm kiếm 1 hoặc nhiều dòng dữ liệu theo ID , CCCD , Type, MinnimunMoney và MaximunMoney
            string? id = null,
            string? cccd = null,
            string? typeacc = null,
            int? minMoney = null,
            int? maxMoney = null)
        {
            List<AccountBank> resultList = new List<AccountBank>();

            StringBuilder queryBuilder = new StringBuilder(@"
        SELECT ID, password, fullname, gender, birthday, CCCD, phone, address, email, type, providedate, money
        FROM dbo.AccountBank
        WHERE 1 = 1");

            List<SqlParameter> parameters = new List<SqlParameter>();

            if (!string.IsNullOrWhiteSpace(id))
            {
                queryBuilder.Append(" AND ID = @ID");
                parameters.Add(new SqlParameter("@ID", SqlDbType.VarChar, 13) { Value = id.Trim() });
            }

            if (!string.IsNullOrWhiteSpace(cccd))
            {
                queryBuilder.Append(" AND CCCD = @CCCD");
                parameters.Add(new SqlParameter("@CCCD", SqlDbType.VarChar, 13) { Value = cccd.Trim() });
            }

            if (!string.IsNullOrWhiteSpace(typeacc) && typeacc != "Tất cả")
            {
                queryBuilder.Append(" AND type = @type");
                parameters.Add(new SqlParameter("@type", SqlDbType.NVarChar, 20) { Value = typeacc.Trim() });
            }

            if (minMoney.HasValue)
            {
                queryBuilder.Append(" AND money >= @MinMoney");
                parameters.Add(new SqlParameter("@MinMoney", SqlDbType.Int) { Value = minMoney.Value });
            }

            if (maxMoney.HasValue)
            {
                queryBuilder.Append(" AND money <= @MaxMoney");
                parameters.Add(new SqlParameter("@MaxMoney", SqlDbType.Int) { Value = maxMoney.Value });
            }

            DBConnect dbconnect = new DBConnect();
            try
            {
                using SqlConnection conn = dbconnect.GetConnection();
                using SqlCommand cmd = new SqlCommand(queryBuilder.ToString(), conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                conn.Open();
                using SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime bdayDt = reader["birthday"] != DBNull.Value
                        ? Convert.ToDateTime(reader["birthday"])
                        : DateTime.MinValue;

                    DateTime provideDt = reader["providedate"] != DBNull.Value
                        ? Convert.ToDateTime(reader["providedate"])
                        : DateTime.MinValue;

                    AccountBank item = new AccountBank(
                        reader["ID"]?.ToString() ?? string.Empty,
                        reader["password"]?.ToString() ?? string.Empty,
                        reader["fullname"]?.ToString() ?? string.Empty,
                        reader["gender"]?.ToString() ?? string.Empty,
                        DateOnly.FromDateTime(bdayDt),
                        reader["CCCD"]?.ToString() ?? string.Empty,
                        reader["phone"]?.ToString() ?? string.Empty,
                        reader["address"]?.ToString() ?? string.Empty,
                        reader["email"] != DBNull.Value ? (reader["email"]?.ToString() ?? string.Empty) : string.Empty,
                        reader["type"]?.ToString() ?? string.Empty,
                        provideDt,
                        reader["money"] != DBNull.Value ? Convert.ToInt32(reader["money"]) : 0
                    );

                    resultList.Add(item);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi searchaccounts: " + ex.Message);
            }

            return resultList;
        }
        public bool updateaccount(string idacc, string cccd, string newPhone, string newAddress, string newEmail) // cập nhật 1 dòng dữ liệu bên sql server
        {
            string query = @"
        UPDATE dbo.AccountBank
        SET phone   = @phone,
            address = @address,
            email   = @email
        WHERE ID = @ID AND CCCD = @CCCD";

            DBConnect dbconnect = new DBConnect();
            try
            {
                using SqlConnection conn = dbconnect.GetConnection();
                using SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.Add("@ID", SqlDbType.VarChar, 13).Value = idacc.Trim();
                cmd.Parameters.Add("@CCCD", SqlDbType.VarChar, 12).Value = cccd.Trim();
                cmd.Parameters.Add("@phone", SqlDbType.VarChar, 10).Value = newPhone.Trim();
                cmd.Parameters.Add("@address", SqlDbType.NVarChar, 150).Value = newAddress.Trim();
                cmd.Parameters.Add("@email", SqlDbType.VarChar, 256).Value =
                    string.IsNullOrWhiteSpace(newEmail) ? DBNull.Value : newEmail.Trim();

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi updatecontactinfo: " + ex.Message);
                return false;
            }
        }
    }
}