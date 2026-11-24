using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BankingManagesandServicesApp.Controls;
using BankingManagesandServicesApp.DBManages;

namespace BankingManagesandServicesApp.GUIs
{
    /// <summary>
    /// Giao diện tra cứu lịch sử tín dụng (UC1.2.8.1).
    /// - Quản lý giao dịch: tìm khách hàng rồi xem chi tiết.
    /// - Khách hàng: truyền mã tài khoản vào constructor, chỉ xem lịch sử của chính mình.
    /// </summary>
    public partial class CreditHistorySearchGUI : Window
    {
        private readonly CreditLoanControl clc = new CreditLoanControl();
        private readonly string? customerId;

        public CreditHistorySearchGUI(string? customerId = null)
        {
            InitializeComponent();
            this.customerId = customerId;
            Loaded += (s, e) => showCreditHistoryG();
        }

        /// <summary>Hiển thị giao diện tra cứu lịch sử tín dụng.</summary>
        public void showCreditHistoryG()
        {
            if (customerId != null)
            {
                pnlSearch.Visibility = Visibility.Collapsed;
                Title = "Lịch sử tín dụng của tôi";
                lblTitle.Text = "LỊCH SỬ TÍN DỤNG CỦA TÔI";
                lblSubtitle.Text = "Thông tin điểm tín dụng, các khoản vay và thẻ tín dụng cá nhân";
                btnBackHome.Content = "← Trang chủ";
                ShowHistory(customerId);
            }
            else
            {
                try
                {
                    var all = clc.loadallcustomers();
                    dgCustomers.ItemsSource = all;
                    if (all.Count > 0) dgCustomers.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    ShowDbError(ex);
                }
                txtKeyword.Focus();
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtKeyword.Text))
            {
                MessageBox.Show("Vui lòng nhập Mã KH, CCCD hoặc Số tài khoản để tìm kiếm.", "Thiếu thông tin",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                List<AccountBank> result = clc.searchcustomer(txtKeyword.Text);
                dgCustomers.ItemsSource = result;
                if (result.Count == 0)
                {
                    // Dòng sự kiện phụ 2a
                    MessageBox.Show("Không tìm thấy dữ liệu khách hàng. Vui lòng nhập lại thông tin.", "Không tìm thấy",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    txtKeyword.SelectAll();
                    txtKeyword.Focus();
                }
                else
                {
                    dgCustomers.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void btnShowAll_Click(object sender, RoutedEventArgs e)
        {
            txtKeyword.Text = "";
            try
            {
                var all = clc.loadallcustomers();
                dgCustomers.ItemsSource = all;
                if (all.Count > 0) dgCustomers.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void txtKeyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnSearch_Click(sender, e);
        }

        private void dgCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgCustomers.SelectedItem is AccountBank acc)
                ShowHistory(acc.idacc);
        }

        private void btnDetail_Click(object sender, RoutedEventArgs e)
        {
            if (dgCustomers.SelectedItem is AccountBank acc)
                ShowHistory(acc.idacc);
            else
                MessageBox.Show("Vui lòng chọn một khách hàng trong danh sách.", "Chưa chọn khách hàng",
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void dgCustomers_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgCustomers.SelectedItem is AccountBank acc) ShowHistory(acc.idacc);
        }

        private void ShowHistory(string idacc)
        {
            try
            {
                AccountBank? acc = clc.loadcustomer(idacc);
                if (acc == null)
                {
                    MessageBox.Show("Không tìm thấy dữ liệu khách hàng.", "Không tìm thấy", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                List<LoanProfile> loans = clc.loadcredithistory(idacc);
                List<CreditCard> cards = clc.loadcustomercards(idacc);

                lblCustomer.Text = $"{acc.fullname}  •  STK: {acc.idacc}  •  CCCD: {acc.CCCD}  •  SĐT: {acc.phone}";
                lblSummary.Text = clc.summarizecredithistory(loans);

                // Cập nhật 4 thẻ KPI
                int score = loans.Where(l => l.creditscore > 0).OrderByDescending(l => l.idloan).Select(l => l.creditscore).FirstOrDefault();
                string rating = CreditLoanControl.ratecreditscore(score);
                lblScore.Text = score > 0 ? score.ToString() : "—";
                lblScoreRating.Text = rating;
                pbScore.Value = Math.Max(300, Math.Min(850, score));
                ApplyScoreStyle(score);

                int active = loans.Count(l => l.status == LoanProfile.StatusApproved && l.remainamount > 0);
                int settled = loans.Count(l => l.status == LoanProfile.StatusSettled);
                int overdue = loans.Count(l => l.debtstate == "Quá hạn");
                long totalDebt = loans.Where(l => l.status == LoanProfile.StatusApproved).Sum(l => (long)l.remainamount);

                lblTotalDebt.Text = $"{totalDebt:N0} VNĐ";
                lblOverdueInfo.Text = overdue > 0 ? $"Cảnh báo: {overdue} khoản quá hạn" : "Không có khoản quá hạn";
                lblOverdueInfo.Foreground = overdue > 0
                    ? (SolidColorBrush)FindResource("DangerBrush")
                    : (SolidColorBrush)FindResource("SuccessBrush");

                lblLoanCounts.Text = $"{loans.Count} hồ sơ";
                lblLoanCountsSub.Text = $"Đang vay: {active}  •  Đã tất toán: {settled}";

                long totalCardLimit = cards.Sum(c => (long)c.currentlimit);
                lblCardCount.Text = $"{cards.Count} thẻ";
                lblCardLimitTotal.Text = $"Tổng hạn mức: {totalCardLimit:N0} VNĐ";

                tabLoans.Header = $"Các khoản vay ({loans.Count})";
                tabCards.Header = $"Thẻ tín dụng ({cards.Count})";
                dgLoans.ItemsSource = loans;
                dgCards.ItemsSource = cards;
            }
            catch (Exception ex)
            {
                ShowDbError(ex);
            }
        }

        private void ApplyScoreStyle(int score)
        {
            Color bg, fg;
            if (score >= 750) { bg = (Color)ColorConverter.ConvertFromString("#DCFCE7"); fg = (Color)ColorConverter.ConvertFromString("#166534"); }
            else if (score >= 650) { bg = (Color)ColorConverter.ConvertFromString("#DBEAFE"); fg = (Color)ColorConverter.ConvertFromString("#1E40AF"); }
            else if (score >= 550) { bg = (Color)ColorConverter.ConvertFromString("#FEF3C7"); fg = (Color)ColorConverter.ConvertFromString("#92400E"); }
            else if (score > 0) { bg = (Color)ColorConverter.ConvertFromString("#FEE2E2"); fg = (Color)ColorConverter.ConvertFromString("#991B1B"); }
            else { bg = (Color)ColorConverter.ConvertFromString("#F1F5F9"); fg = (Color)ColorConverter.ConvertFromString("#64748B"); }

            badgeScoreRating.Background = new SolidColorBrush(bg);
            lblScoreRating.Foreground = new SolidColorBrush(fg);
            pbScore.Foreground = new SolidColorBrush(fg);
        }

        private void btnBackHome_Click(object sender, RoutedEventArgs e) => Close();

        private static void ShowDbError(Exception ex)
        {
            MessageBox.Show("Lỗi truy cập cơ sở dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
