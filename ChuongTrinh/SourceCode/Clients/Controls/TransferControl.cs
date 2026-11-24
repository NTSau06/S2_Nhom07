using System;
using System.Collections.Generic;
using System.Linq;
using BankingManagesandServicesApp.DBManages;

namespace BankingClientApp.Controls
{
    /// <summary>
    /// Lớp xử lý chuyển khoản dành riêng cho Khách hàng (UC1.2.7).
    /// </summary>
    public class TransferControl
    {
        private readonly AccountBank ab = new AccountBank();
        private readonly TransactionBank tb = new TransactionBank();
        private readonly BeneficiaryContact bc = new BeneficiaryContact();

        // ----- Quy định nghiệp vụ -----
        public const string InternalBank = "Ngân hàng Nhóm 07 (nội bộ)";
        public const int MinAmount = 10_000;            // số tiền chuyển tối thiểu
        public const int MaxAmount = 500_000_000;       // số tiền chuyển tối đa / giao dịch
        public const int MinRemain = 50_000;            // số dư tối thiểu phải còn lại (ràng buộc money >= 50000)
        public const int InterbankFee = 5_500;          // phí chuyển liên ngân hàng
        public const int MaxOtpAttempts = 3;            // sai OTP tối đa
        public static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan LockDuration = TimeSpan.FromMinutes(15);

        public static readonly string[] BankList =
        {
            InternalBank, "Vietcombank", "VietinBank", "BIDV", "Techcombank", "MB Bank", "ACB"
        };

        /// <summary>
        /// Danh bạ mô phỏng tra cứu tài khoản ngân hàng khác (thay cho cổng liên ngân hàng NAPAS thật).
        /// Khóa: "Ngân hàng|Số tài khoản".
        /// </summary>
        private static readonly Dictionary<string, string> ExternalDirectory = new Dictionary<string, string>
        {
            { "Vietcombank|0071001234567", "NGUYEN THI HOA" },
            { "Vietcombank|0071009876543", "TRAN MINH KHOA" },
            { "VietinBank|101867543210", "LE THANH TUNG" },
            { "BIDV|31410001234567", "PHAM QUOC VIET" },
            { "Techcombank|19033456789012", "DO THU TRANG" },
            { "MB Bank|0987654321", "VU DUC ANH" },
            { "ACB|123456789", "NGO BAO NGOC" }
        };

        // ----- Trạng thái OTP (mô phỏng gửi SMS) -----
        private string? currentOtp;
        private DateTime otpExpire;
        private int wrongOtpCount;
        private static readonly Dictionary<string, DateTime> lockedUntil = new Dictionary<string, DateTime>();

        public int RemainingOtpAttempts => MaxOtpAttempts - wrongOtpCount;
        public TimeSpan RemainingOtpTime => currentOtp == null ? TimeSpan.Zero : (otpExpire > DateTime.Now ? otpExpire - DateTime.Now : TimeSpan.Zero);
        public bool HasActiveOtp => currentOtp != null && DateTime.Now <= otpExpire;

        // ============== Đăng nhập khách hàng & tài khoản nguồn ==============

        public AccountBank? checkcustomerlogin(string idacc, string password)
        {
            AccountBank? acc = ab.detailaccount(idacc);
            return acc != null && acc.passwordacc == password ? acc : null;
        }

        public List<AccountBank> loadallaccounts() => ab.queryaccount("");

        public AccountBank? loadsender(string idacc) => ab.detailaccount(idacc);

        public List<BeneficiaryContact> loadbeneficiary(string idacc) => bc.querybeneficiary(idacc);

        // ============== Bước 1-2: kiểm tra thông tin ==============

        public int checkformattransfer(string senderId, string receiver, string bank, string amountText, string description, out int amount)
        {
            amount = 0;
            receiver = receiver.Trim();
            if (receiver.Length < 6 || receiver.Length > 20 || !receiver.All(char.IsDigit)) return 1;
            if (string.IsNullOrWhiteSpace(bank)) return 2;
            if (!TryParseMoney(amountText, out amount)) return 3;
            if (amount < MinAmount) return 4;
            if (amount > MaxAmount) return 5;
            if (description.Length > 150) return 6;
            if (bank == InternalBank && receiver == senderId) return 7;
            return 0;
        }

        public static string formatmessage(int code)
        {
            return code switch
            {
                1 => "Số tài khoản nhận phải gồm 6-20 chữ số.",
                2 => "Vui lòng chọn ngân hàng nhận.",
                3 => "Số tiền không hợp lệ.",
                4 => $"Số tiền chuyển tối thiểu là {MinAmount:N0} VNĐ.",
                5 => $"Số tiền chuyển tối đa mỗi giao dịch là {MaxAmount:N0} VNĐ.",
                6 => "Nội dung chuyển khoản tối đa 150 ký tự.",
                7 => "Không thể chuyển khoản cho chính tài khoản nguồn.",
                _ => ""
            };
        }

        public static bool TryParseMoney(string text, out int value) => MoneyHelper.TryParseMoney(text, out value);

        public static string MoneyToVietnameseWords(long number) => MoneyHelper.MoneyToVietnameseWords(number);

        public string? checkreceiver(string receiver, string bank)
        {
            receiver = receiver.Trim();
            if (bank == InternalBank)
                return ab.detailaccount(receiver)?.fullname;
            return ExternalDirectory.TryGetValue(bank + "|" + receiver, out string? name) ? name : null;
        }

        public int calculatefee(string bank, int amount)
        {
            return bank == InternalBank ? 0 : InterbankFee;
        }

        public bool checkbalance(string senderId, int amount, int fee)
        {
            int money = ab.querymoney(senderId);
            return money >= 0 && (long)money - amount - fee >= MinRemain;
        }

        // ============== Bước 4-5: OTP ==============

        public bool islocked(string senderId, out DateTime until)
        {
            if (lockedUntil.TryGetValue(senderId, out until))
            {
                if (until > DateTime.Now) return true;
                lockedUntil.Remove(senderId);
            }
            return false;
        }

        public string generateOTP()
        {
            currentOtp = Random.Shared.Next(0, 1_000_000).ToString("D6");
            otpExpire = DateTime.Now.Add(OtpLifetime);
            wrongOtpCount = 0;
            return currentOtp;
        }

        public int verifyOTP(string senderId, string otp)
        {
            if (currentOtp == null || DateTime.Now > otpExpire) return 3;
            if (otp.Trim() == currentOtp)
            {
                currentOtp = null;
                return 0;
            }
            wrongOtpCount++;
            if (wrongOtpCount >= MaxOtpAttempts)
            {
                currentOtp = null;
                lockedUntil[senderId] = DateTime.Now.Add(LockDuration);
                return 2;
            }
            return 1;
        }

        // ============== Bước 6: thực hiện chuyển tiền ==============

        public string executetransfer(string senderId, string receiver, string bank, string receivername, int amount, int fee, string description)
        {
            string idtrs = TransactionBank.NewId();
            DateTime now = DateTime.Now;
            string content = string.IsNullOrWhiteSpace(description) ? "Chuyen khoan" : description.Trim();

            DBConnectandManage.ExecuteInTransaction((conn, tran) =>
            {
                if (!ab.updatemoney(senderId, -(amount + fee), MinRemain, conn, tran))
                    throw new InvalidOperationException("Số dư không đủ để thực hiện giao dịch.");

                if (bank == InternalBank && !ab.updatemoney(receiver, amount, 0, conn, tran))
                    throw new InvalidOperationException("Không tìm thấy tài khoản nhận.");

                new TransactionBank
                {
                    idtrs = idtrs, idacc = senderId, amount = amount, transactionType = "Chuyển khoản",
                    status = "Thành công", transactionTime = now,
                    description = Cut($"CK đến {receiver} - {receivername} ({bank}): {content}", 255)
                }.addTransaction(conn, tran);

                if (fee > 0)
                {
                    new TransactionBank
                    {
                        idtrs = idtrs + "F", idacc = senderId, amount = fee, transactionType = "Phí giao dịch",
                        status = "Thành công", transactionTime = now,
                        description = "Phí chuyển khoản liên ngân hàng cho giao dịch " + idtrs
                    }.addTransaction(conn, tran);
                }

                if (bank == InternalBank)
                {
                    new TransactionBank
                    {
                        idtrs = idtrs + "R", idacc = receiver, amount = amount, transactionType = "Nhận chuyển khoản",
                        status = "Thành công", transactionTime = now,
                        description = Cut($"Nhận từ {senderId}: {content}", 255)
                    }.addTransaction(conn, tran);
                }

                new TransactionLog
                {
                    idtrs = idtrs, action = "Chuyển khoản", timestamp = now, result = "Thành công",
                    details = $"{senderId} chuyển {amount:N0} VNĐ (phí {fee:N0}) đến {receiver} - {receivername} - {bank}"
                }.writeLog(conn, tran);
            });
            return idtrs;
        }

        // ============== Bước 7: danh bạ thụ hưởng & lịch sử ==============

        public bool checkbeneficiaryexists(string senderId, string receiver, string bank)
        {
            return bc.checkexists(senderId, receiver.Trim(), bank);
        }

        public bool savebeneficiary(string senderId, string receiver, string receivername, string bank)
        {
            return new BeneficiaryContact
            {
                idacc = senderId, receiveraccount = receiver.Trim(), receivername = receivername, bankname = bank
            }.addbeneficiary();
        }

        public string? exportreceipt(string idtrs) => tb.getDetail(idtrs)?.exportReceipt("txt");

        public List<TransactionBank> loadtransferhistory(string senderId) => tb.queryTransactions(senderId);

        public bool deletebeneficiary(int idbnf) => bc.deletebeneficiary(idbnf);

        private static string Cut(string s, int max) => s.Length <= max ? s : s.Substring(0, max);
    }
}
