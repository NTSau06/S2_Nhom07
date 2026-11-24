using System.Collections.Generic;

namespace BankingManagesandServicesApp.DBManages
{
    /// <summary>
    /// Tiện ích đọc và chuyển đổi số tiền VNĐ dùng chung giữa Quản trị viên và Khách hàng (Clients).
    /// </summary>
    public static class MoneyHelper
    {
        /// <summary>Đọc số tiền, chấp nhận dạng "1000000", "1.000.000", "1,000,000".</summary>
        public static bool TryParseMoney(string text, out int value)
        {
            string digits = (text ?? "").Replace(".", "").Replace(",", "").Replace(" ", "").Trim();
            return int.TryParse(digits, out value);
        }

        /// <summary>Đọc số tiền bằng chữ tiếng Việt.</summary>
        public static string MoneyToVietnameseWords(long number)
        {
            if (number <= 0) return "";
            string[] units = { "", "nghìn", "triệu", "tỷ" };
            string[] digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };

            string ReadThree(int n, bool full)
            {
                int h = n / 100, t = (n % 100) / 10, u = n % 10;
                var parts = new List<string>();
                if (h > 0 || full)
                {
                    parts.Add(digits[h] + " trăm");
                    if (t == 0 && u > 0) parts.Add("lẻ");
                }
                if (t > 1)
                {
                    parts.Add(digits[t] + " mươi");
                    if (u == 1) parts.Add("mốt");
                    else if (u == 5) parts.Add("lăm");
                    else if (u > 0) parts.Add(digits[u]);
                }
                else if (t == 1)
                {
                    parts.Add("mười");
                    if (u == 5) parts.Add("lăm");
                    else if (u > 0) parts.Add(digits[u]);
                }
                else if (u > 0)
                {
                    parts.Add(digits[u]);
                }
                return string.Join(" ", parts);
            }

            var groups = new List<int>();
            long tmp = number;
            while (tmp > 0)
            {
                groups.Add((int)(tmp % 1000));
                tmp /= 1000;
            }
            var result = new List<string>();
            for (int i = groups.Count - 1; i >= 0; i--)
            {
                if (groups[i] == 0) continue;
                bool full = i < groups.Count - 1;
                string txt = ReadThree(groups[i], full);
                if (!string.IsNullOrEmpty(units[i])) txt += " " + units[i];
                result.Add(txt);
            }
            string sentence = string.Join(" ", result).Trim() + " đồng";
            return char.ToUpper(sentence[0]) + sentence.Substring(1);
        }
    }
}
