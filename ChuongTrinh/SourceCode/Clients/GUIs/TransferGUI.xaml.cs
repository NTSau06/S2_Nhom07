using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BankingClientApp.Controls;
using BankingManagesandServicesApp.DBManages;

namespace BankingClientApp.GUIs
{
    /// <summary>
    /// Giao diện chuyển khoản dành cho Khách hàng (UC1.2.7).
    /// </summary>
    public partial class TransferGUI : Window
    {
        private readonly TransferControl tc = new TransferControl();
        private readonly string senderId;
        private readonly DispatcherTimer otpTimer;
        private readonly DispatcherTimer receiverLookupTimer;
        private readonly DispatcherTimer newBnfLookupTimer;

        private int senderBalance;
        private string senderName = "";
        private string senderPhone = "";
        private string DefaultDescription => string.IsNullOrWhiteSpace(senderName) ? "Chuyển khoản" : $"{senderName} chuyển khoản";
        private string checkedReceiver = "", checkedBank = "", checkedName = "", checkedDescription = "";
        private string lastCompletedIdtrs = "";
        private int checkedAmount, checkedFee;
        private bool loadingBeneficiary;
        private List<TransactionBank> allHistory = new();

        public TransferGUI(string idacc)
        {
            InitializeComponent();
            senderId = idacc;
            otpTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            otpTimer.Tick += OtpTimer_Tick;

            receiverLookupTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
            receiverLookupTimer.Tick += (s, e) => { receiverLookupTimer.Stop(); LookupReceiverAccount(); };

            newBnfLookupTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
            newBnfLookupTimer.Tick += (s, e) => { newBnfLookupTimer.Stop(); LookupNewBeneficiaryAccount(); };

            Loaded += (s, e) => showTransferG();
            Closed += (s, e) => { otpTimer.Stop(); receiverLookupTimer.Stop(); newBnfLookupTimer.Stop(); };
        }

        /// <summary>Hiển thị giao diện chuyển khoản.</summary>
        public void showTransferG()
        {
            cbBank.ItemsSource = TransferControl.BankList;
            cbBank.SelectedIndex = 0;

            cbNewBnfBank.ItemsSource = TransferControl.BankList;
            cbNewBnfBank.SelectedIndex = 0;

            cbHistoryType.ItemsSource = new[] { "Tất cả loại", "Chuyển khoản", "Nhận chuyển khoản", "Phí giao dịch" };
            cbHistoryType.SelectedIndex = 0;

            LoadSender();
            LoadBeneficiary();
            LoadHistory();
            RefreshLockBanner();
        }

        private void LoadSender()
        {
            try
            {
                AccountBank? acc = tc.loadsender(senderId);
                if (acc == null)
                {
                    MessageBox.Show("Không tìm thấy tài khoản nguồn.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                    return;
                }
                senderBalance = acc.money;
                senderName = acc.fullname;
                senderPhone = acc.phone;
                lblSenderName.Text = acc.fullname;
                lblSenderAcc.Text = $"STK: {acc.idacc}  •  {acc.typeacc}";
                lblBalance.Text = $"{acc.money:N0} VNĐ";
                if (string.IsNullOrWhiteSpace(txtDescription.Text))
                {
                    txtDescription.Text = DefaultDescription;
                    lblDescCount.Text = $"{txtDescription.Text.Length}/150";
                }
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void LoadBeneficiary()
        {
            try
            {
                List<BeneficiaryContact> list = tc.loadbeneficiary(senderId);
                cbBeneficiary.ItemsSource = list;
                dgBeneficiaries.ItemsSource = list;
                lblBeneficiarySummary.Text = list.Count == 0
                    ? "Chưa có người thụ hưởng nào được lưu."
                    : $"Đang lưu {list.Count} người thụ hưởng.";
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void LoadHistory()
        {
            try
            {
                allHistory = tc.loadtransferhistory(senderId);
                ApplyHistoryFilter();
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void ApplyHistoryFilter()
        {
            if (dgHistory == null) return;
            string selected = cbHistoryType.SelectedItem as string ?? "Tất cả loại";
            var filtered = selected == "Tất cả loại"
                ? allHistory
                : allHistory.Where(t => t.transactionType == selected).ToList();
            dgHistory.ItemsSource = filtered;
            lblHistorySummary.Text = $"Hiển thị {filtered.Count} / {allHistory.Count} giao dịch gần nhất";
        }

        private void RefreshLockBanner()
        {
            if (tc.islocked(senderId, out DateTime until))
            {
                pnlLockWarning.Visibility = Visibility.Visible;
                lblLockWarning.Text = $"Chức năng chuyển khoản đang bị tạm khóa do nhập sai OTP quá {TransferControl.MaxOtpAttempts} lần. Vui lòng thử lại sau {until:HH:mm:ss}.";
            }
            else
            {
                pnlLockWarning.Visibility = Visibility.Collapsed;
            }
        }

        private void cbBeneficiary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbBeneficiary.SelectedItem is BeneficiaryContact b)
            {
                loadingBeneficiary = true;
                cbBank.SelectedItem = b.bankname;
                txtReceiver.Text = b.receiveraccount;
                loadingBeneficiary = false;
                ResetConfirm();
                LookupReceiverAccount();
            }
        }

        private void QuickAmount_Click(object sender, RoutedEventArgs e)
        {
            if (!txtAmount.IsEnabled) return;
            if (sender is Button btn && btn.Tag is string raw && int.TryParse(raw, out int val))
            {
                txtAmount.Text = val.ToString("N0");
                txtAmount.CaretIndex = txtAmount.Text.Length;
            }
        }

        private void txtAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (TransferControl.TryParseMoney(txtAmount.Text, out int val) && val > 0)
                lblAmountWords.Text = $"{val:N0} VNĐ — {TransferControl.MoneyToVietnameseWords(val)}";
            else
                lblAmountWords.Text = "";
            ResetConfirm();
        }

        private void txtDescription_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;
            lblDescCount.Text = $"{txtDescription.Text.Length}/150";
            ResetConfirm();
        }

        private void Input_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (!loadingBeneficiary && (sender == txtReceiver || sender == cbBank))
                cbBeneficiary.SelectedItem = null;
            ResetConfirm();

            if (sender == cbBank)
            {
                receiverLookupTimer.Stop();
                LookupReceiverAccount();
            }
            else if (sender == txtReceiver)
            {
                receiverLookupTimer.Stop();
                receiverLookupTimer.Start();
            }
        }

        private void txtReceiver_LostFocus(object sender, RoutedEventArgs e)
        {
            receiverLookupTimer.Stop();
            LookupReceiverAccount();
        }

        /// <summary>
        /// Tra cứu tự động tên chủ tài khoản theo Ngân hàng + Số tài khoản.
        /// Đúng cả 2 -> hiện tên chủ tài khoản; sai 1 trong 2 -> hiện "Tài khoản không tồn tại".
        /// </summary>
        private void LookupReceiverAccount()
        {
            if (pnlReceiverLookup == null) return;
            string acc = txtReceiver.Text.Trim();
            string bank = cbBank.SelectedItem as string ?? "";

            if (string.IsNullOrEmpty(acc))
            {
                pnlReceiverLookup.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
                pnlReceiverLookup.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));
                lblReceiverLookup.Foreground = (SolidColorBrush)FindResource("TextMutedBrush");
                lblReceiverLookup.Text = "Chọn ngân hàng và nhập số tài khoản để tự động hiện tên chủ tài khoản.";
                return;
            }

            if (bank == TransferControl.InternalBank && acc == senderId)
            {
                pnlReceiverLookup.Background = (SolidColorBrush)FindResource("WarningBgBrush");
                pnlReceiverLookup.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCD34D"));
                lblReceiverLookup.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#92400E"));
                lblReceiverLookup.Text = "⚠ Không thể chuyển khoản cho chính tài khoản nguồn.";
                return;
            }

            if (acc.Length < 6 || acc.Length > 20 || !acc.All(char.IsDigit) || string.IsNullOrWhiteSpace(bank))
            {
                ShowReceiverNotFound();
                return;
            }

            try
            {
                string? name = tc.checkreceiver(acc, bank);
                if (name != null)
                {
                    pnlReceiverLookup.Background = (SolidColorBrush)FindResource("SuccessBgBrush");
                    pnlReceiverLookup.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC"));
                    lblReceiverLookup.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
                    lblReceiverLookup.Text = $"✔ Chủ tài khoản: {name}";
                }
                else
                {
                    ShowReceiverNotFound();
                }
            }
            catch
            {
                ShowReceiverNotFound();
            }
        }

        private void ShowReceiverNotFound()
        {
            pnlReceiverLookup.Background = (SolidColorBrush)FindResource("DangerBgBrush");
            pnlReceiverLookup.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCA5A5"));
            lblReceiverLookup.Foreground = (SolidColorBrush)FindResource("DangerBrush");
            lblReceiverLookup.Text = "✘ Tài khoản không tồn tại";
        }

        private void ResetConfirm()
        {
            if (pnlConfirm == null) return;
            otpTimer.Stop();
            pnlConfirm.Visibility = Visibility.Collapsed;
            pnlOtp.Visibility = Visibility.Collapsed;
            pnlSuccess.Visibility = Visibility.Collapsed;
            pnlRules.Visibility = Visibility.Visible;
            btnCheck.IsEnabled = true;
            SetInputEnabled(true);
            RefreshLockBanner();
        }

        private void SetInputEnabled(bool enabled)
        {
            cbBeneficiary.IsEnabled = cbBank.IsEnabled = txtReceiver.IsEnabled = enabled;
            txtAmount.IsEnabled = txtDescription.IsEnabled = enabled;
        }

        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            RefreshLockBanner();
            if (tc.islocked(senderId, out DateTime until))
            {
                MessageBox.Show($"Chức năng chuyển khoản đang tạm khóa do nhập sai OTP quá {TransferControl.MaxOtpAttempts} lần.\nVui lòng thử lại sau {until:HH:mm:ss}.",
                    "Tạm khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string bank = cbBank.SelectedItem as string ?? "";
            int code = tc.checkformattransfer(senderId, txtReceiver.Text, bank, txtAmount.Text, txtDescription.Text, out int amount);
            if (code != 0)
            {
                MessageBox.Show(TransferControl.formatmessage(code), "Thông tin chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string? name = tc.checkreceiver(txtReceiver.Text, bank);
                if (name == null)
                {
                    ShowReceiverNotFound();
                    MessageBox.Show("Tài khoản không tồn tại tại ngân hàng đã chọn. Vui lòng kiểm tra lại.",
                        "Tài khoản không tồn tại", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtReceiver.Focus();
                    return;
                }

                int fee = tc.calculatefee(bank, amount);
                if (!tc.checkbalance(senderId, amount, fee))
                {
                    MessageBox.Show($"Số dư không đủ. Sau giao dịch tài khoản phải còn tối thiểu {TransferControl.MinRemain:N0} VNĐ.\nVui lòng sửa lại số tiền.",
                        "Số dư không đủ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtAmount.Focus();
                    return;
                }

                checkedReceiver = txtReceiver.Text.Trim();
                checkedBank = bank;
                checkedName = name;
                checkedAmount = amount;
                checkedFee = fee;
                checkedDescription = string.IsNullOrWhiteSpace(txtDescription.Text) ? DefaultDescription : txtDescription.Text.Trim();

                lblReceiverName.Text = name;
                lblReceiverAcc.Text = checkedReceiver;
                lblReceiverBank.Text = bank;
                lblConfirmAmount.Text = $"{amount:N0} VNĐ";
                lblFee.Text = fee == 0 ? "Miễn phí (0 VNĐ)" : $"{fee:N0} VNĐ";
                lblFee.Foreground = fee == 0
                    ? (SolidColorBrush)FindResource("SuccessBrush")
                    : (SolidColorBrush)FindResource("TextPrimaryBrush");
                lblTotal.Text = $"{amount + fee:N0} VNĐ";
                lblAfterBalance.Text = $"Số dư dự kiến sau giao dịch: {(long)senderBalance - amount - fee:N0} VNĐ";

                pnlRules.Visibility = Visibility.Collapsed;
                pnlConfirm.Visibility = Visibility.Visible;
                btnConfirm.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void btnConfirm_Click(object sender, RoutedEventArgs e)
        {
            SetInputEnabled(false);
            btnCheck.IsEnabled = false;
            btnConfirm.IsEnabled = false;
            SendOtp();
        }

        private void SendOtp()
        {
            string otp = tc.generateOTP();
            string masked = senderPhone.Length >= 4 ? "******" + senderPhone.Substring(senderPhone.Length - 4) : senderPhone;
            lblOtpInfo.Text = $"Mã OTP 6 chữ số đã gửi tới SĐT {masked} (hiệu lực {TransferControl.OtpLifetime.TotalMinutes:0} phút, tối đa {TransferControl.MaxOtpAttempts} lần nhập).";
            lblOtpError.Text = "";
            txtOtp.Text = "";
            pnlOtp.Visibility = Visibility.Visible;
            btnVerifyOtp.IsEnabled = btnResendOtp.IsEnabled = true;

            UpdateOtpTimerDisplay();
            otpTimer.Start();

            MessageBox.Show($"[SMS mô phỏng]\nMã OTP xác thực giao dịch chuyển khoản của bạn là:\n\n{otp}\n\nHiệu lực trong {TransferControl.OtpLifetime.TotalMinutes:0} phút.",
                "Tin nhắn OTP", MessageBoxButton.OK, MessageBoxImage.Information);
            txtOtp.Focus();
        }

        private void OtpTimer_Tick(object? sender, EventArgs e)
        {
            UpdateOtpTimerDisplay();
            if (!tc.HasActiveOtp)
            {
                otpTimer.Stop();
                lblOtpError.Text = "Mã OTP đã hết hạn. Vui lòng bấm \"Gửi lại\".";
            }
        }

        private void UpdateOtpTimerDisplay()
        {
            TimeSpan left = tc.RemainingOtpTime;
            if (left <= TimeSpan.Zero)
            {
                lblOtpTimer.Text = "Hết hạn";
                lblOtpTimer.Foreground = (SolidColorBrush)FindResource("DangerBrush");
                badgeOtpTimer.Background = (SolidColorBrush)FindResource("DangerBgBrush");
            }
            else
            {
                lblOtpTimer.Text = $"{(int)left.TotalMinutes:00}:{left.Seconds:00}";
                bool low = left.TotalSeconds <= 30;
                lblOtpTimer.Foreground = low
                    ? (SolidColorBrush)FindResource("DangerBrush")
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B45309"));
                badgeOtpTimer.Background = low
                    ? (SolidColorBrush)FindResource("DangerBgBrush")
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FEF3C7"));
            }
        }

        private void btnResendOtp_Click(object sender, RoutedEventArgs e) => SendOtp();

        private void txtOtp_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnVerifyOtp_Click(sender, e);
        }

        private void btnVerifyOtp_Click(object sender, RoutedEventArgs e)
        {
            int result = tc.verifyOTP(senderId, txtOtp.Text);
            switch (result)
            {
                case 1:
                    lblOtpError.Text = $"Mã OTP không đúng. Còn {tc.RemainingOtpAttempts} lần nhập.";
                    txtOtp.SelectAll();
                    txtOtp.Focus();
                    return;
                case 2:
                    otpTimer.Stop();
                    MessageBox.Show($"Bạn đã nhập sai OTP quá {TransferControl.MaxOtpAttempts} lần. Chức năng chuyển khoản bị tạm khóa {TransferControl.LockDuration.TotalMinutes:0} phút.",
                        "Tạm khóa chức năng", MessageBoxButton.OK, MessageBoxImage.Error);
                    ResetConfirm();
                    return;
                case 3:
                    otpTimer.Stop();
                    lblOtpError.Text = "Mã OTP đã hết hạn. Vui lòng bấm \"Gửi lại\".";
                    return;
            }

            otpTimer.Stop();
            btnVerifyOtp.IsEnabled = btnResendOtp.IsEnabled = false;
            string idtrs;
            try
            {
                idtrs = tc.executetransfer(senderId, checkedReceiver, checkedBank, checkedName, checkedAmount, checkedFee, checkedDescription);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message, "Giao dịch thất bại", MessageBoxButton.OK, MessageBoxImage.Warning);
                ResetConfirm();
                LoadSender();
                return;
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
                ResetConfirm();
                return;
            }

            lastCompletedIdtrs = idtrs;
            LoadSender();
            LoadHistory();

            // Hiển thị thẻ Hoàn tất ngay trên giao diện kèm nút Xuất chứng từ, Thêm vào danh bạ và Chuyển tiếp
            pnlConfirm.Visibility = Visibility.Collapsed;
            pnlOtp.Visibility = Visibility.Collapsed;
            pnlRules.Visibility = Visibility.Collapsed;

            lblSuccessId.Text = idtrs;
            lblSuccessReceiver.Text = $"{checkedName}\n{checkedReceiver} ({checkedBank})";
            lblSuccessAmount.Text = $"{checkedAmount:N0} VNĐ";
            lblSuccessFee.Text = checkedFee == 0 ? "Miễn phí (0 VNĐ)" : $"{checkedFee:N0} VNĐ";
            lblSuccessDesc.Text = checkedDescription;
            lblSuccessTime.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            try
            {
                bool exists = tc.checkbeneficiaryexists(senderId, checkedReceiver, checkedBank);
                btnAddToBeneficiary.IsEnabled = !exists;
                btnAddToBeneficiary.Content = exists ? "✔ Đã có trong danh bạ" : "+ Thêm vào danh bạ";
            }
            catch
            {
                btnAddToBeneficiary.IsEnabled = true;
                btnAddToBeneficiary.Content = "+ Thêm vào danh bạ";
            }

            pnlSuccess.Visibility = Visibility.Visible;
        }

        private void btnSuccessReceipt_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(lastCompletedIdtrs))
                ExportReceipt(lastCompletedIdtrs);
        }

        private void btnAddToBeneficiary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (tc.checkbeneficiaryexists(senderId, checkedReceiver, checkedBank))
                {
                    btnAddToBeneficiary.IsEnabled = false;
                    btnAddToBeneficiary.Content = "✔ Đã có trong danh bạ";
                    return;
                }
                if (tc.savebeneficiary(senderId, checkedReceiver, checkedName, checkedBank))
                {
                    btnAddToBeneficiary.IsEnabled = false;
                    btnAddToBeneficiary.Content = "✔ Đã lưu danh bạ";
                    LoadBeneficiary();
                    MessageBox.Show($"Đã thêm {checkedName} ({checkedReceiver} - {checkedBank}) vào danh bạ thụ hưởng.",
                        "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void btnTransferNext_Click(object sender, RoutedEventArgs e)
        {
            lastCompletedIdtrs = "";
            ClearForm();
            LoadSender();
            LoadBeneficiary();
            LoadHistory();
            txtReceiver.Focus();
        }

        private void ExportReceipt(string idtrs)
        {
            try
            {
                string? path = tc.exportreceipt(idtrs);
                if (path != null)
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không xuất được chứng từ: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void cbHistoryType_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyHistoryFilter();

        private void btnRefreshHistory_Click(object sender, RoutedEventArgs e)
        {
            LoadSender();
            LoadHistory();
        }

        private void btnExportSelected_Click(object sender, RoutedEventArgs e)
        {
            if (dgHistory.SelectedItem is TransactionBank trs)
                ExportReceipt(trs.idtrs);
            else
                MessageBox.Show("Vui lòng chọn một giao dịch để xuất chứng từ.", "Chưa chọn giao dịch", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void dgHistory_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgHistory.SelectedItem is TransactionBank trs) ExportReceipt(trs.idtrs);
        }

        // ---------- Tab 3: Tự thêm, sử dụng & xóa Danh bạ thụ hưởng ----------
        private void btnToggleAddBnf_Click(object sender, RoutedEventArgs e)
        {
            bool show = pnlAddBnfForm.Visibility != Visibility.Visible;
            pnlAddBnfForm.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show)
            {
                ResetNewBnfForm();
                txtNewBnfAcc.Focus();
            }
        }

        private void btnCancelNewBnf_Click(object sender, RoutedEventArgs e)
        {
            ResetNewBnfForm();
            pnlAddBnfForm.Visibility = Visibility.Collapsed;
        }

        private void ResetNewBnfForm()
        {
            newBnfLookupTimer.Stop();
            cbNewBnfBank.SelectedIndex = 0;
            txtNewBnfAcc.Text = "";
            txtNewBnfName.Text = "";
            lblNewBnfStatus.Text = "Chọn ngân hàng và nhập số tài khoản để tự động hiện tên chủ tài khoản.";
            lblNewBnfStatus.Foreground = (SolidColorBrush)FindResource("TextMutedBrush");
        }

        private void NewBnfInput_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            if (sender == cbNewBnfBank)
            {
                newBnfLookupTimer.Stop();
                LookupNewBeneficiaryAccount();
            }
            else if (sender == txtNewBnfAcc)
            {
                newBnfLookupTimer.Stop();
                newBnfLookupTimer.Start();
            }
        }

        private void txtNewBnfAcc_LostFocus(object sender, RoutedEventArgs e)
        {
            newBnfLookupTimer.Stop();
            LookupNewBeneficiaryAccount();
        }

        private void LookupNewBeneficiaryAccount()
        {
            string bank = cbNewBnfBank.SelectedItem as string ?? "";
            string acc = txtNewBnfAcc.Text.Trim();
            if (string.IsNullOrEmpty(acc))
            {
                txtNewBnfName.Text = "";
                lblNewBnfStatus.Text = "Chọn ngân hàng và nhập số tài khoản để tự động hiện tên chủ tài khoản.";
                lblNewBnfStatus.Foreground = (SolidColorBrush)FindResource("TextMutedBrush");
                return;
            }
            if (bank == TransferControl.InternalBank && acc == senderId)
            {
                txtNewBnfName.Text = "";
                lblNewBnfStatus.Text = "⚠ Không thể thêm chính tài khoản của bạn vào danh bạ thụ hưởng.";
                lblNewBnfStatus.Foreground = (SolidColorBrush)FindResource("DangerBrush");
                return;
            }
            if (acc.Length < 6 || acc.Length > 20 || !acc.All(char.IsDigit) || string.IsNullOrWhiteSpace(bank))
            {
                txtNewBnfName.Text = "";
                lblNewBnfStatus.Text = "✘ Tài khoản không tồn tại";
                lblNewBnfStatus.Foreground = (SolidColorBrush)FindResource("DangerBrush");
                return;
            }
            try
            {
                string? found = tc.checkreceiver(acc, bank);
                if (found != null)
                {
                    txtNewBnfName.Text = found;
                    lblNewBnfStatus.Text = $"✔ Chủ tài khoản: {found}";
                    lblNewBnfStatus.Foreground = (SolidColorBrush)FindResource("SuccessBrush");
                }
                else
                {
                    txtNewBnfName.Text = "";
                    lblNewBnfStatus.Text = "✘ Tài khoản không tồn tại";
                    lblNewBnfStatus.Foreground = (SolidColorBrush)FindResource("DangerBrush");
                }
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void btnAddNewBnf_Click(object sender, RoutedEventArgs e)
        {
            string bank = cbNewBnfBank.SelectedItem as string ?? "";
            string acc = txtNewBnfAcc.Text.Trim();
            string name = txtNewBnfName.Text.Trim();

            if (string.IsNullOrWhiteSpace(bank))
            {
                MessageBox.Show("Vui lòng chọn ngân hàng.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (acc.Length < 6 || acc.Length > 20 || !acc.All(char.IsDigit))
            {
                MessageBox.Show("Tài khoản không tồn tại. Số tài khoản phải gồm 6–20 chữ số.", "Tài khoản không tồn tại", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNewBnfAcc.Focus();
                return;
            }
            if (bank == TransferControl.InternalBank && acc == senderId)
            {
                MessageBox.Show("Không thể thêm chính tài khoản nguồn vào danh bạ thụ hưởng.", "Không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string? found = tc.checkreceiver(acc, bank);
                if (found == null)
                {
                    lblNewBnfStatus.Text = "✘ Tài khoản không tồn tại";
                    lblNewBnfStatus.Foreground = (SolidColorBrush)FindResource("DangerBrush");
                    MessageBox.Show("Tài khoản không tồn tại tại ngân hàng đã chọn.", "Tài khoản không tồn tại", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNewBnfAcc.Focus();
                    return;
                }
                if (string.IsNullOrWhiteSpace(name))
                    name = found;

                if (tc.checkbeneficiaryexists(senderId, acc, bank))
                {
                    MessageBox.Show("Người thụ hưởng này đã tồn tại trong danh bạ.", "Đã tồn tại", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (tc.savebeneficiary(senderId, acc, name, bank))
                {
                    LoadBeneficiary();
                    ResetNewBnfForm();
                    pnlAddBnfForm.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Đã thêm {name} ({acc} - {bank}) vào danh bạ thụ hưởng.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void UseBeneficiary(BeneficiaryContact b)
        {
            tabMain.SelectedIndex = 0;
            ClearForm();
            loadingBeneficiary = true;
            cbBank.SelectedItem = b.bankname;
            txtReceiver.Text = b.receiveraccount;
            cbBeneficiary.SelectedItem = b;
            loadingBeneficiary = false;
            LookupReceiverAccount();
            txtAmount.Focus();
        }

        private void btnUseBeneficiary_Click(object sender, RoutedEventArgs e)
        {
            if (dgBeneficiaries.SelectedItem is BeneficiaryContact b)
                UseBeneficiary(b);
            else
                MessageBox.Show("Vui lòng chọn một người thụ hưởng trong danh sách.", "Chưa chọn liên hệ", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void dgBeneficiaries_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgBeneficiaries.SelectedItem is BeneficiaryContact b) UseBeneficiary(b);
        }

        private void btnDeleteBeneficiary_Click(object sender, RoutedEventArgs e)
        {
            if (dgBeneficiaries.SelectedItem is not BeneficiaryContact b)
            {
                MessageBox.Show("Vui lòng chọn liên hệ cần xóa.", "Chưa chọn liên hệ", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (MessageBox.Show($"Xóa {b.receivername} ({b.receiveraccount} - {b.bankname}) khỏi danh bạ thụ hưởng?",
                    "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                if (tc.deletebeneficiary(b.idbnf))
                    LoadBeneficiary();
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void ClearForm()
        {
            loadingBeneficiary = true;
            cbBeneficiary.SelectedItem = null;
            txtReceiver.Text = "";
            txtAmount.Text = "";
            txtDescription.Text = DefaultDescription;
            lblAmountWords.Text = "";
            lblDescCount.Text = $"{txtDescription.Text.Length}/150";
            cbBank.SelectedIndex = 0;
            loadingBeneficiary = false;
            ResetConfirm();
            LookupReceiverAccount();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e) => ClearForm();

        private void btnHome_Click(object sender, RoutedEventArgs e) => Close();

        private static void ShowDbError(Exception ex)
        {
            MessageBox.Show("Lỗi truy cập cơ sở dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
