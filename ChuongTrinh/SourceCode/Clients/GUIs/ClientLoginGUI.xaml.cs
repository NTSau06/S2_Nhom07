using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BankingClientApp.Controls;
using BankingManagesandServicesApp.DBManages;
using BankingManagesandServicesApp.GUIs;

namespace BankingClientApp.GUIs
{
    /// <summary>
    /// Màn hình đăng nhập &amp; cổng dịch vụ dành riêng cho Khách hàng (Clients).
    /// </summary>
    public partial class ClientLoginGUI : Window
    {
        private readonly TransferControl tc = new TransferControl();
        private AccountBank? customer;

        public ClientLoginGUI()
        {
            InitializeComponent();
            Loaded += (s, e) => CheckDatabase();
        }

        private void CheckDatabase()
        {
            string? error = DBConnectandManage.TestConnection();
            if (error == null)
            {
                string srv = DBConnectandManage.ConnectedServer ?? DBConnectandManage.ServerName ?? "SQL Server";
                lblDbStatus.Text = $"✔ Đã kết nối: {srv} / {DBConnectandManage.DatabaseName}";
                badgeDb.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#14532D"));
                badgeDb.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4ADE80"));
                LoadDemoAccounts();
            }
            else
            {
                lblDbStatus.Text = "✘ Chưa kết nối được CSDL — bấm 'Đổi Server'";
                badgeDb.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"));
                badgeDb.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
                MessageBox.Show(error, "Lỗi kết nối CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDemoAccounts()
        {
            try
            {
                List<AccountBank> accounts = tc.loadallaccounts();
                var items = new List<DemoAccountItem>();
                foreach (var a in accounts)
                    items.Add(new DemoAccountItem { Id = a.idacc, Display = $"{a.idacc} — {a.fullname} ({a.money:N0}đ)" });
                cbDemoAccounts.ItemsSource = items;
                cbDemoAccounts.DisplayMemberPath = "Display";
                if (items.Count > 0 && cbDemoAccounts.SelectedIndex < 0)
                    cbDemoAccounts.SelectedIndex = 0;

                RefreshCustomerInfo();
            }
            catch
            {
            }
        }

        private void RefreshCustomerInfo()
        {
            if (customer == null) return;
            try
            {
                AccountBank? fresh = tc.loadsender(customer.idacc);
                if (fresh != null) customer = fresh;
                lblCustomerName.Text = customer.fullname;
                lblCustomerMeta.Text = $"STK: {customer.idacc}  •  {customer.typeacc}";
                lblCustomerBalance.Text = $"{customer.money:N0} VNĐ";
            }
            catch
            {
            }
        }

        private void btnTestDb_Click(object sender, RoutedEventArgs e) => CheckDatabase();

        private void btnConfigDb_Click(object sender, RoutedEventArgs e)
        {
            pnlServerConfig.Visibility = pnlServerConfig.Visibility == Visibility.Visible
                ? Visibility.Collapsed : Visibility.Visible;
            txtServerName.Text = DBConnectandManage.ServerName ?? DBConnectandManage.ConnectedServer ?? "";
        }

        private void btnApplyServer_Click(object sender, RoutedEventArgs e)
        {
            DBConnectandManage.SetServer(txtServerName.Text);
            pnlServerConfig.Visibility = Visibility.Collapsed;
            CheckDatabase();
        }

        private void cbDemoAccounts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbDemoAccounts.SelectedItem is DemoAccountItem item)
            {
                txtCustomerId.Text = item.Id;
                txtPassword.Password = "123456";
            }
        }

        private void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) btnLogin_Click(sender, e);
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                customer = tc.checkcustomerlogin(txtCustomerId.Text.Trim(), txtPassword.Password);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (customer == null)
            {
                MessageBox.Show("Sai mã tài khoản hoặc mật khẩu.", "Đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            pnlLoginForm.Visibility = Visibility.Collapsed;
            pnlCustomerDashboard.Visibility = Visibility.Visible;
            RefreshCustomerInfo();
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            customer = null;
            pnlCustomerDashboard.Visibility = Visibility.Collapsed;
            pnlLoginForm.Visibility = Visibility.Visible;
            LoadDemoAccounts();
        }

        private void OpenFeature(Window w)
        {
            w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            w.Closed += (s, args) =>
            {
                Show();
                Activate();
                RefreshCustomerInfo();
            };
            Hide();
            w.Show();
        }

        private void btnTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (customer != null) OpenFeature(new TransferGUI(customer.idacc));
        }

        private void btnMyCredit_Click(object sender, RoutedEventArgs e)
        {
            if (customer != null) OpenFeature(new CreditHistorySearchGUI(customer.idacc));
        }

        private void btnMyDebt_Click(object sender, RoutedEventArgs e)
        {
            if (customer != null) OpenFeature(new DebtTrackingGUI(customer.idacc));
        }

        private void btnApplyLoan_Click(object sender, RoutedEventArgs e)
        {
            OpenFeature(new LoanApplicationGUI());
        }

        private class DemoAccountItem
        {
            public string Id { get; set; } = "";
            public string Display { get; set; } = "";
        }
    }
}
