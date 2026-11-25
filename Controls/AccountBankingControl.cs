using BankingManagesandServicesApp.DBConnectandManages;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BankingManagesandServicesApp.Controls
{
    internal class AccountBankingControl
    {
        AccountBank ab;
        public AccountBankingControl()
        {
            ab = new AccountBank();
        }
        public bool IsValidGmail(string Email) // kiểm tra định dạng của một Email
        {
            // 1. Kiểm tra rỗng / toàn khoảng trắng
            if (string.IsNullOrWhiteSpace(Email))
                return false;

            string emailClean = Email.Trim();

            // 2. Viết liền nhau: không chứa bất kỳ khoảng trắng nào ở giữa
            if (emailClean.Any(char.IsWhiteSpace))
                return false;

            // 3. Chuỗi con cuối luôn là "@gmail.com" (phân biệt hay không phân biệt hoa thường tùy bạn)
            if (!emailClean.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
                return false;

            // 4. Phải có phần tên người dùng trước "@gmail.com" (không thể chỉ có "@gmail.com")
            string localPart = emailClean.Substring(0, emailClean.Length - "@gmail.com".Length);
            if (string.IsNullOrEmpty(localPart))
                return false;

            // 5. Chỉ có đúng 1 ký tự '@' trong toàn bộ chuỗi
            if (emailClean.Count(c => c == '@') != 1)
                return false;

            // 6. Không có dấu (tiếng Việt có dấu / dấu câu) và không chứa ký tự đặc biệt lạ
            // Chỉ cho phép chữ cái ASCII (a-z, A-Z), chữ số (0-9), dấu chấm (.), gạch dưới (_), dấu cộng (+), gạch ngang (-)
            if (!Regex.IsMatch(localPart, @"^[a-zA-Z0-9._+-]+$"))
                return false;

            // Không cho phép dấu chấm đứng đầu, đứng cuối hoặc hai dấu chấm liền nhau (..)
            if (localPart.StartsWith(".") || localPart.EndsWith(".") || localPart.Contains(".."))
                return false;

            return true;
        }
        public string GenerateRandomString(int minLength, int maxLength) // tạo ngẫu nhiên password 
        {
            string LowerChars = "abcdefghijklmnopqrstuvwxyz";
            string UpperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string DigitChars = "0123456789";
            string SpecialChars = "@";
            string AllChars = LowerChars + UpperChars + DigitChars + SpecialChars;
            if (minLength < 4)
                throw new ArgumentException("Độ dài tối thiểu phải >= 4 để chứa đủ 4 loại ký tự.", nameof(minLength));
            if (maxLength < minLength)
                throw new ArgumentException("Độ dài tối đa phải >= độ dài tối thiểu.", nameof(maxLength));

            // Chọn độ dài ngẫu nhiên trong khoảng [minLength, maxLength]
            int targetLength = RandomNumberGenerator.GetInt32(minLength, maxLength + 1);
            List<char> chars = new List<char>(targetLength);

            // 1. Đảm bảo mỗi loại ký tự xuất hiện ít nhất 1 lần
            chars.Add(LowerChars[RandomNumberGenerator.GetInt32(LowerChars.Length)]);
            chars.Add(UpperChars[RandomNumberGenerator.GetInt32(UpperChars.Length)]);
            chars.Add(DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)]);
            chars.Add(SpecialChars[RandomNumberGenerator.GetInt32(SpecialChars.Length)]);

            // 2. Bổ sung ngẫu nhiên các ký tự còn lại
            while (chars.Count < targetLength)
            {
                chars.Add(AllChars[RandomNumberGenerator.GetInt32(AllChars.Length)]);
            }

            // 3. Trộn ngẫu nhiên vị trí (Fisher-Yates shuffle)
            for (int i = chars.Count - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }
        public int addaccountcontrol(string CCCD, string FullName, string Gender, string Email, string Birthday, string Phone, string Address, string Type)
        {
            if(CCCD == "" || FullName == "" || Birthday == "" || Phone=="" || Address=="") // không đúng định dạng lần thứ 1
            {
                return 1;
            }
            if(CCCD.Length!=12 || Phone.Length!=10) // không đúng định dạng lần thứ 2
            {
                return 1;
            }
            if (long.TryParse(CCCD,out long cccd)==false || DateOnly.TryParseExact(Birthday, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateOnly birthday)==false || long.TryParse(Phone,out long phone)==false) // không đúng định dạng lần thứ 3
            {
                return 1;
            }
            if(Email!="" && IsValidGmail(Email)==false) // không đúng định dạng lần thứ 4
            {
                return 1;
            }
            DateOnly.TryParseExact(Birthday, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateOnly birthday1);
            AccountBank checksame = ab.queryaccountbyCCCD(CCCD);
            if ((CCCD == checksame.CCCD && Type == checksame.typeacc) && (birthday1 != checksame.birthday || FullName != checksame.fullname || Phone != checksame.phone || Address != checksame.address || Email != checksame.email))
            {
                return 3; // thông tin nhập bị sai so với thông tin tài khoản hiện tại
            }    
            if (ab.checkexistaccount_IDbyCCCDandtypeacc(CCCD, Type) != "") // đã tồn tại
            {
                return 4;
            }
            if(ab.checkexistaccount_IDbyPhoneandEmail(Phone,Email) != "") // đã tồn tại
            {
                return 5;
            }    
            string ID = "";
            string password = "";
            if (Type=="Tài khoản thanh toán")
            {
                ID = CCCD + "1";
            }
            else if (Type == "Tài khoản tiết kiệm")
            {
                ID = CCCD + "2";
            }
            else if (Type == "Tài khoản vay")
            {
                ID = CCCD + "3";
            }
            password = GenerateRandomString(8,15);
            if (DateTime.TryParseExact(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),"yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture,DateTimeStyles.None,out DateTime ketQuaDateTime)==true)
            {
                ab.addaccount(new AccountBank(ID, password, FullName, Gender, birthday, CCCD, Phone, Address, Email, Type, ketQuaDateTime, 50000));
            }
            return 6;
        }
        public List<AccountBank> showallaccountbank() // show tất cả tài khoản không cần điều kiện gì cả
        {
            return ab.queryallaccount();
        }
        public AccountBank showaccountbankByCCCD(string CCCDvalue) // trả về tất cả thông tin của 1 tài khoản từ CCCDvalue
        {
            return ab.queryaccountbyCCCD(CCCDvalue);
        }
        public List<string> getallcccdcontrol()  // lấy tất cả CCCD hiện tại và đưa ( nhét ) vào ComboBox
        {
            return ab.getalldistinctcccd();
        }
        public int searchaccountscontrol(
            string? idInput,
            string? cccdInput,
            string? typeInput,
            string? minMoneyInput,
            string? maxMoneyInput,
            out List<AccountBank> results)
        {
            results = new List<AccountBank>();

            string idClean = idInput?.Trim() ?? string.Empty;
            string cccdClean = cccdInput?.Trim() ?? string.Empty;
            string typeClean = typeInput?.Trim() ?? string.Empty;
            string minClean = minMoneyInput?.Trim() ?? string.Empty;
            string maxClean = maxMoneyInput?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(idClean) && (!long.TryParse(idClean, out _) || idClean.Length != 13))
            {
                return 1; // không đúng định dạng 
            }

            if (!string.IsNullOrEmpty(cccdClean) && (!long.TryParse(cccdClean, out _) || cccdClean.Length != 12))
            {
                return 1;
            }

            int? minMoney = null;
            if (!string.IsNullOrEmpty(minClean))
            {
                if (!int.TryParse(minClean, out int parsedMin) || parsedMin < 50000)
                {
                    return 1;
                }
                minMoney = parsedMin;
            }

            int? maxMoney = null;
            if (!string.IsNullOrEmpty(maxClean))
            {
                if (!int.TryParse(maxClean, out int parsedMax) || parsedMax <= 50000)
                {
                    return 1;
                }
                maxMoney = parsedMax;
            }

            if (minMoney.HasValue && maxMoney.HasValue && minMoney.Value > maxMoney.Value)
            {
                return 1;
            }

            results = ab.searchaccounts(idClean, cccdClean, typeClean, minMoney, maxMoney);
            if (results.Count==0) // không tồn tại
            {
                return 3;
            }
            return 4;
        }
        public AccountBank showDetailAccountbyID(string idvalue)
        {
            return ab.queryaccountbyID(idvalue);
        }
        public int updateaccountcontrol(
            string idacc,
            string cccd,
            string phone,
            string address,
            string email)
        {

            string phoneClean = phone;
            string addressClean = address;
            string emailClean = email;

            if (string.IsNullOrEmpty(phoneClean) || string.IsNullOrEmpty(addressClean))
            {
                return 1; // không đúng định dạng
            }

            if (phoneClean.Length != 10 || !long.TryParse(phoneClean, out _))
            {
                return 1;
            }

            if (addressClean.Length > 150)
            {
                return 1;
            }

            if (!string.IsNullOrEmpty(emailClean) && !IsValidGmail(emailClean))
            {
                return 1;
            }

            bool updated = ab.updateaccount(idacc, cccd, phoneClean, addressClean, emailClean);
            return 2;
        }
    }
}