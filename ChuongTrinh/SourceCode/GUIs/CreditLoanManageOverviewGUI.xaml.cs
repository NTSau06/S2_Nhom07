using System;
using System.Windows;
using System.Windows.Input;
using BankingManagesandServicesApp.Controls;

namespace BankingManagesandServicesApp.GUIs
{
    /// <summary>
    /// Giao diện tổng quan chức năng Quản lý tín dụng &amp; cho vay (UC1.2.8) dành cho Quản lý giao dịch.
    /// </summary>
    public partial class CreditLoanManageOverviewGUI : Window
    {
        private readonly CreditLoanControl clc = new CreditLoanControl();

        public CreditLoanManageOverviewGUI()
        {
            InitializeComponent();
            Loaded += (s, e) => LoadDashboard();
        }

        private void LoadDashboard()
        {
            try
            {
                CreditDashboardSummary summary = clc.loaddashboardsummary();
                lblKpiPending.Text = $"{summary.PendingLoansCount} hồ sơ";
                lblKpiPendingSub.Text = $"Chờ duyệt: {summary.WaitingApprovalCount}  •  Chờ bổ sung: {summary.SupplementCount}";

                lblKpiDebt.Text = $"{summary.TotalRemainingDebt:N0} VNĐ";
                lblKpiDebtSub.Text = $"{summary.ActiveLoansCount} khoản vay đang có dư nợ";

                lblKpiOverdue.Text = $"{summary.OverdueLoansCount} khoản";
                lblKpiOverdueSub.Text = $"Dư nợ quá hạn: {summary.OverdueRemainingDebt:N0} VNĐ";

                lblKpiCards.Text = $"{summary.ActiveCardsCount} / {summary.TotalCardsCount} thẻ";
                lblKpiCardsSub.Text = $"Tổng hạn mức hoạt động: {summary.TotalCurrentCardLimit:N0} VNĐ";

                dgPendingQuick.ItemsSource = summary.PendingProfiles;
                dgOverdueQuick.ItemsSource = summary.OverdueDebts;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải số liệu tổng quan tín dụng:\n" + ex.Message, "Lỗi CSDL",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenChild(Window w)
        {
            w.Owner = this;
            w.Closed += (s, e) => LoadDashboard();
            w.Show();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e) => LoadDashboard();

        private void btnCreditHistory_Click(object sender, RoutedEventArgs e) => OpenChild(new CreditHistorySearchGUI());

        private void btnCardLimit_Click(object sender, RoutedEventArgs e) => OpenChild(new CreditCardLimitGUI());

        private void btnDebt_Click(object sender, RoutedEventArgs e) => OpenChild(new DebtTrackingGUI());

        private void btnApprove_Click(object sender, RoutedEventArgs e) => OpenChild(new LoanProfileApproveGUI());

        private void dgPendingQuick_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgPendingQuick.SelectedItem != null) OpenChild(new LoanProfileApproveGUI());
        }

        private void dgOverdueQuick_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (dgOverdueQuick.SelectedItem != null) OpenChild(new DebtTrackingGUI());
        }
    }
}
