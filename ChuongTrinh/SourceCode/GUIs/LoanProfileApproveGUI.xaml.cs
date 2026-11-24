using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BankingManagesandServicesApp.Controls;
using BankingManagesandServicesApp.DBManages;

namespace BankingManagesandServicesApp.GUIs
{
    /// <summary>
    /// Giao diện duyệt hồ sơ vay vốn (UC1.2.8.4) - "Giao diện duyệt hồ sơ" trong Sequence Diagram.
    /// </summary>
    public partial class LoanProfileApproveGUI : Window
    {
        private readonly CreditLoanControl clc = new CreditLoanControl();
        private LoanProfile? selected;
        private bool showAllMode;

        public LoanProfileApproveGUI()
        {
            InitializeComponent();
            Loaded += (s, e) => showLoanApproveG();
        }

        /// <summary>Hiển thị danh sách hồ sơ đang chờ xử lý.</summary>
        public void showLoanApproveG()
        {
            try
            {
                List<LoanProfile> list = showAllMode ? clc.loadallprofiles() : clc.loadpendingprofiles();
                dgProfiles.ItemsSource = list;
                lblQueueTitle.Text = showAllMode
                    ? "Toàn bộ hồ sơ vay vốn (Chờ xử lý & Đã xử lý)"
                    : "Hồ sơ đang chờ xử lý (Chờ duyệt / Chờ bổ sung)";
                lblQueueCount.Text = $"{list.Count} hồ sơ";

                bool empty = list.Count == 0;
                pnlEmpty.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
                lblEmpty.Text = empty ? "Không có hồ sơ nào trong danh sách hiện tại." : "";

                if (list.Count > 0)
                    dgProfiles.SelectedIndex = 0;
                else
                    ShowDetail(null);
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
                ShowDetail(null);
            }
        }

        private void btnModePending_Click(object sender, RoutedEventArgs e)
        {
            showAllMode = false;
            btnModePending.Style = (Style)FindResource("AccentButton");
            btnModeAll.Style = (Style)FindResource("SecondaryButton");
            btnModeAll.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24FFFFFF"));
            btnModeAll.Foreground = Brushes.White;
            showLoanApproveG();
        }

        private void btnModeAll_Click(object sender, RoutedEventArgs e)
        {
            showAllMode = true;
            btnModeAll.Style = (Style)FindResource("AccentButton");
            btnModePending.Style = (Style)FindResource("SecondaryButton");
            btnModePending.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24FFFFFF"));
            btnModePending.Foreground = Brushes.White;
            showLoanApproveG();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e) => showLoanApproveG();

        // Bước 1-6: chọn hồ sơ "Chờ duyệt" -> hiển thị thông tin cá nhân và chứng từ đính kèm
        private void dgProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgProfiles.SelectedItem is not LoanProfile row) return;
            try
            {
                ShowDetail(clc.loadprofiledetail(row.idloan));
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void ShowDetail(LoanProfile? p)
        {
            selected = p;
            txtNote.Text = "";
            if (p == null)
            {
                lblInfoTitle.Text = "Thông tin hồ sơ khách hàng";
                lblInfo.Text = "Chọn một hồ sơ ở danh sách phía trên để xem thông tin cá nhân và chứng từ.";
                badgeScore.Visibility = Visibility.Collapsed;
                badgeReadOnly.Visibility = Visibility.Collapsed;
                icDocs.ItemsSource = null;
                lblNoDocs.Visibility = Visibility.Collapsed;
                txtAmount.Text = txtRate.Text = txtTerm.Text = "";
                SetActionEnabled(false);
                UpdateMonthlyPreview();
                return;
            }

            bool actionable = p.status == LoanProfile.StatusPending || p.status == LoanProfile.StatusSupplement;
            SetActionEnabled(actionable);
            badgeReadOnly.Visibility = actionable ? Visibility.Collapsed : Visibility.Visible;

            AccountBank? acc = clc.loadcustomer(p.idacc);
            lblInfoTitle.Text = $"Hồ sơ {p.idloan} — {p.status}";
            lblInfo.Text =
                $"Khách hàng: {acc?.fullname}  •  Giới tính: {acc?.gender}  •  Ngày sinh: {acc?.birthday:dd/MM/yyyy}\n" +
                $"CCCD: {acc?.CCCD}  •  SĐT: {acc?.phone}  •  Email: {acc?.email}\n" +
                $"Địa chỉ: {acc?.address}\n" +
                $"Tài khoản: {p.idacc} ({acc?.typeacc})  •  Số dư hiện tại: {acc?.money:N0} VNĐ\n" +
                $"Đề nghị vay: {p.loanamount:N0} VNĐ  •  Kỳ hạn: {p.loanterm} tháng  •  Ngày nộp: {p.submissiondate:dd/MM/yyyy}" +
                (string.IsNullOrEmpty(p.note) ? "" : $"\nGhi chú trước đó: {p.note}");

            string rating = CreditLoanControl.ratecreditscore(p.creditscore);
            lblScoreBadge.Text = $"Điểm TD: {p.creditscore} ({rating})";
            ApplyScoreBadge(p.creditscore);
            badgeScore.Visibility = Visibility.Visible;

            var docs = p.attachdocs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            icDocs.ItemsSource = docs;
            lblNoDocs.Visibility = docs.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            // Gợi ý thông số phê duyệt từ đề nghị của khách hàng
            txtAmount.Text = p.loanamount.ToString("N0");
            txtRate.Text = p.interestrate > 0 ? p.interestrate.ToString(CultureInfo.InvariantCulture) : "9";
            txtTerm.Text = p.loanterm.ToString();
            UpdateMonthlyPreview();
        }

        private void SetActionEnabled(bool enabled)
        {
            btnApprove.IsEnabled = btnSupplement.IsEnabled = btnReject.IsEnabled = enabled;
            txtAmount.IsEnabled = txtRate.IsEnabled = txtTerm.IsEnabled = txtNote.IsEnabled = enabled;
        }

        private void ApplyScoreBadge(int score)
        {
            string bg, fg;
            if (score >= 750) { bg = "#DCFCE7"; fg = "#166534"; }
            else if (score >= 650) { bg = "#DBEAFE"; fg = "#1E40AF"; }
            else if (score >= 550) { bg = "#FEF3C7"; fg = "#92400E"; }
            else { bg = "#FEE2E2"; fg = "#991B1B"; }
            badgeScore.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg));
            lblScoreBadge.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg));
        }

        private void TermInput_Changed(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) UpdateMonthlyPreview();
        }

        private void UpdateMonthlyPreview()
        {
            if (clc.checkformatapproval(txtAmount.Text, txtRate.Text, txtTerm.Text, out int amount, out double rate, out int term) == 0)
            {
                var (principal, interest, total) = CreditLoanControl.estimateMonthlyPayment(amount, rate, term);
                lblMonthlyPreview.Text = $"Ước tính kỳ đầu: Gốc {principal:N0}đ + Lãi {interest:N0}đ = {total:N0} VNĐ/tháng  ({MoneyHelper.MoneyToVietnameseWords(amount)})";
            }
            else
            {
                lblMonthlyPreview.Text = "Nhập hạn mức (1tr–2tỷ), lãi suất (0–30%/năm) và kỳ hạn (1–360 tháng) để tính lịch trả dự kiến.";
            }
        }

        // Bước 7.2: thẩm định, nhập hạn mức - lãi suất - thời hạn, xác nhận phê duyệt
        private void btnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (selected == null) return;
            int code = clc.checkformatapproval(txtAmount.Text, txtRate.Text, txtTerm.Text, out int amount, out double rate, out int term);
            if (code != 0)
            {
                MessageBox.Show(CreditLoanControl.approvalmessage(code), "Thông số chưa hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (MessageBox.Show($"Phê duyệt hồ sơ {selected.idloan}?\nHạn mức: {amount:N0} VNĐ - Lãi suất: {rate:0.##}%/năm - Thời hạn: {term} tháng",
                    "Xác nhận phê duyệt", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                if (!clc.approveprofile(selected.idloan, amount, rate, term, txtNote.Text))
                {
                    // 6a: lưu thất bại -> giữ nguyên trạng thái cũ
                    MessageBox.Show("Lưu thất bại: hồ sơ không còn ở trạng thái chờ xử lý. Trạng thái hồ sơ được giữ nguyên.",
                        "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string sent = clc.notifycustomer(selected.idacc,
                    $"Hồ sơ vay {selected.idloan} đã được phê duyệt: {amount:N0} VNĐ, lãi suất {rate:0.##}%/năm, thời hạn {term} tháng.");
                MessageBox.Show("Phê duyệt hồ sơ thành công.\n\n" + sent, "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                showLoanApproveG();
            }
            catch (Exception ex)
            {
                // 6a: lỗi CSDL - transaction đã rollback, hồ sơ giữ nguyên trạng thái
                MessageBox.Show("Lưu thất bại do lỗi hệ thống, hồ sơ giữ nguyên trạng thái cũ.\n" + ex.Message,
                    "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 3a: thiếu chứng từ -> yêu cầu bổ sung hồ sơ
        private void btnSupplement_Click(object sender, RoutedEventArgs e) => UpdateStatus(true);

        private void btnReject_Click(object sender, RoutedEventArgs e) => UpdateStatus(false);

        private void UpdateStatus(bool supplement)
        {
            if (selected == null) return;
            if (string.IsNullOrWhiteSpace(txtNote.Text))
            {
                MessageBox.Show("Vui lòng nhập lý do vào ô \"Ghi chú / Lý do\".", "Thiếu lý do", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNote.Focus();
                return;
            }
            string action = supplement ? "yêu cầu bổ sung hồ sơ" : "từ chối hồ sơ";
            if (MessageBox.Show($"Xác nhận {action} {selected.idloan}?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            try
            {
                bool ok = supplement
                    ? clc.requestsupplement(selected.idloan, txtNote.Text)
                    : clc.rejectprofile(selected.idloan, txtNote.Text);
                if (!ok)
                {
                    MessageBox.Show("Cập nhật thất bại, hồ sơ giữ nguyên trạng thái cũ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string msg = supplement
                    ? $"Hồ sơ vay {selected.idloan} cần bổ sung chứng từ. Lý do: {txtNote.Text.Trim()}"
                    : $"Hồ sơ vay {selected.idloan} không được phê duyệt. Lý do: {txtNote.Text.Trim()}";
                MessageBox.Show((supplement ? "Đã gửi yêu cầu bổ sung.\n\n" : "Đã từ chối hồ sơ.\n\n") + clc.notifycustomer(selected.idacc, msg),
                    "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                showLoanApproveG();
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private static void ShowDbError(Exception ex)
        {
            MessageBox.Show("Lỗi truy cập cơ sở dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
