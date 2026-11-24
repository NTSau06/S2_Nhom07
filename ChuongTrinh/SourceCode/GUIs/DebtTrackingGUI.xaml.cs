using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BankingManagesandServicesApp.Controls;
using BankingManagesandServicesApp.DBManages;

namespace BankingManagesandServicesApp.GUIs
{
    /// <summary>
    /// Giao diện theo dõi dư nợ khách hàng (UC1.2.8.3).
    /// Quản lý giao dịch: xem toàn bộ; Khách hàng: truyền mã tài khoản để chỉ xem dư nợ của mình.
    /// </summary>
    public partial class DebtTrackingGUI : Window
    {
        private readonly CreditLoanControl clc = new CreditLoanControl();
        private readonly string? customerId;
        private List<LoanProfile> allDebts = new List<LoanProfile>();

        public DebtTrackingGUI(string? customerId = null)
        {
            InitializeComponent();
            this.customerId = customerId;
            Loaded += (s, e) => showDebtTrackingG();
        }

        /// <summary>Hiển thị danh sách tổng hợp các khách hàng đang có dư nợ.</summary>
        public void showDebtTrackingG()
        {
            cbGroup.ItemsSource = CreditLoanControl.DebtGroups;
            cbGroup.SelectedIndex = 0;
            cbState.ItemsSource = CreditLoanControl.DebtStates;
            cbState.SelectedIndex = 0;
            if (customerId != null)
            {
                Title = "Dư nợ của tôi";
                lblHeader.Text = "DƯ NỢ CỦA TÔI";
                lblSubHeader.Text = "Theo dõi các khoản vay đang hoạt động, tiến độ thanh toán và ngày đến hạn của bạn";
                btnBackHome.Content = "← Trang chủ";
            }

            try
            {
                allDebts = clc.loaddebtlist(customerId ?? "");
                ShowList(allDebts);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi truy cập cơ sở dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowList(List<LoanProfile> list)
        {
            dgDebts.ItemsSource = list;
            bool isEmpty = list.Count == 0;
            pnlEmpty.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
            lblEmpty.Text = isEmpty ? "Danh sách trống — không có khoản dư nợ nào phù hợp với điều kiện lọc." : "";

            int overdueCount = list.Count(d => d.debtstate == "Quá hạn");
            long overdueRemain = list.Where(d => d.debtstate == "Quá hạn").Sum(d => (long)d.remainamount);

            lblStatCount.Text = $"{list.Count} khoản";
            lblStatLoanTotal.Text = $"{list.Sum(d => (long)d.loanamount):N0} VNĐ";
            lblStatRemainTotal.Text = $"{list.Sum(d => (long)d.remainamount):N0} VNĐ";
            lblStatOverdue.Text = overdueCount > 0 ? $"{overdueCount} khoản ({overdueRemain:N0}đ)" : "0 khoản";

            ResetDetail();
            if (list.Count > 0) dgDebts.SelectedIndex = 0;
        }

        private void ResetDetail()
        {
            lblDetailTitle.Text = "Chi tiết khoản dư nợ";
            lblDetailInfo.Text = "Chọn một khoản dư nợ trong danh sách để xem thông tin chi tiết.";
            badgeDetailGroup.Visibility = Visibility.Collapsed;
            badgeDetailState.Visibility = Visibility.Collapsed;
            pnlProgress.Visibility = Visibility.Collapsed;
            lblEstPrincipal.Text = lblEstInterest.Text = lblEstTotal.Text = "—";
        }

        private void btnBackHome_Click(object sender, RoutedEventArgs e) => Close();

        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            List<LoanProfile> result = clc.filterdebt(allDebts,
                cbGroup.SelectedItem as string ?? CreditLoanControl.All,
                cbState.SelectedItem as string ?? CreditLoanControl.All,
                dpFrom.SelectedDate, dpTo.SelectedDate, txtMin.Text, txtMax.Text, out string error);
            if (error != "")
            {
                MessageBox.Show(error, "Bộ lọc không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ShowList(result);
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            cbGroup.SelectedIndex = 0;
            cbState.SelectedIndex = 0;
            dpFrom.SelectedDate = null;
            dpTo.SelectedDate = null;
            txtMin.Text = "";
            txtMax.Text = "";
            showDebtTrackingG();
        }

        // Chọn một khách hàng để xem chi tiết khoản dư nợ, số tiền đã trả và còn lại
        private void dgDebts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDebts.SelectedItem is not LoanProfile row)
            {
                ResetDetail();
                return;
            }
            try
            {
                LoanProfile? d = clc.loaddebtdetail(row.idloan);
                if (d == null)
                {
                    ResetDetail();
                    lblDetailInfo.Text = "Không tìm thấy khoản dư nợ.";
                    return;
                }

                lblDetailTitle.Text = $"Khoản vay {d.idloan} — {d.fullname}";
                lblDetailInfo.Text =
                    $"Tài khoản: {d.idacc}  •  CCCD: {d.CCCD}\n" +
                    $"Lãi suất: {d.interestrate:0.##}%/năm  •  Kỳ hạn: {d.loanterm} tháng  •  Ngày giải ngân: {d.approveddate:dd/MM/yyyy}  •  Ngày đến hạn: {d.duedate:dd/MM/yyyy}" +
                    (string.IsNullOrWhiteSpace(d.note) ? "" : $"\nGhi chú: {d.note}");

                var conv = (StatusToBrushConverter)FindResource("StatusBrush");
                if (!string.IsNullOrEmpty(d.debtgroup))
                {
                    lblDetailGroup.Text = d.debtgroup;
                    badgeDetailGroup.Background = (Brush)conv.Convert(d.debtgroup, typeof(Brush), "bg", System.Globalization.CultureInfo.CurrentCulture);
                    lblDetailGroup.Foreground = (Brush)conv.Convert(d.debtgroup, typeof(Brush), "fg", System.Globalization.CultureInfo.CurrentCulture);
                    badgeDetailGroup.Visibility = Visibility.Visible;
                }
                if (!string.IsNullOrEmpty(d.debtstate))
                {
                    lblDetailState.Text = d.debtstate;
                    badgeDetailState.Background = (Brush)conv.Convert(d.debtstate, typeof(Brush), "bg", System.Globalization.CultureInfo.CurrentCulture);
                    lblDetailState.Foreground = (Brush)conv.Convert(d.debtstate, typeof(Brush), "fg", System.Globalization.CultureInfo.CurrentCulture);
                    badgeDetailState.Visibility = Visibility.Visible;
                }

                pnlProgress.Visibility = Visibility.Visible;
                lblProgressLeft.Text = $"Đã trả: {d.paidamount:N0} VNĐ / {d.loanamount:N0} VNĐ ({d.paidpercenttext})";
                lblProgressRight.Text = $"Còn lại: {d.remainamount:N0} VNĐ";
                pbDebtProgress.Value = d.paidpercent;

                int monthsLeft = d.duedate.HasValue
                    ? Math.Max(1, (d.duedate.Value.Year - DateTime.Today.Year) * 12 + d.duedate.Value.Month - DateTime.Today.Month)
                    : 1;
                if (d.debtstate == "Quá hạn") monthsLeft = 1;
                var (principal, interest, total) = CreditLoanControl.estimateMonthlyPayment(d.remainamount, d.interestrate, monthsLeft);
                lblEstPrincipal.Text = $"{principal:N0} VNĐ";
                lblEstInterest.Text = $"{interest:N0} VNĐ";
                lblEstTotal.Text = $"{total:N0} VNĐ";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi truy cập cơ sở dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
