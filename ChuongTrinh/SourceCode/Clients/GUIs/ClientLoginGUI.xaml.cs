using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using BankingClientApp.Controls;
using BankingManagesandServicesApp.DBManages;
using BankingManagesandServicesApp.GUIs;

namespace BankingClientApp.GUIs
{
    /// <summary>
    /// Màn hình chính &amp; cổng dịch vụ dành riêng cho Khách hàng (tự động đăng nhập sẵn).
    /// </summary>
    public partial class ClientLoginGUI : Window
    {
        private const string DefaultCustomerId = "1000000000001";
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
                AutoSignInDefaultCustomer();
            }
            else
            {
                lblDbStatus.Text = "✘ Chưa kết nối được CSDL — bấm 'Đổi Server'";
                badgeDb.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7F1D1D"));
                badgeDb.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F87171"));
                lblCustomerName.Text = "Chưa kết nối CSDL";
                lblCustomerMeta.Text = "Kiểm tra lại SQL Server";
                MessageBox.Show(error, "Lỗi kết nối CSDL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AutoSignInDefaultCustomer()
        {
            try
            {
                if (customer == null)
                {
                    customer = tc.loadsender(DefaultCustomerId);
                    if (customer == null)
                    {
                        List<AccountBank> accounts = tc.loadallaccounts();
                        customer = accounts.FirstOrDefault();
                    }
                }
                RefreshCustomerInfo();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi tải tài khoản", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshCustomerInfo()
        {
            if (customer == null)
            {
                lblCustomerName.Text = "Không tìm thấy tài khoản";
                lblCustomerMeta.Text = "STK: ---";
                lblCustomerBalance.Text = "0 VNĐ";
                return;
            }
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

        private bool EnsureCustomerLoaded()
        {
            if (customer != null) return true;
            AutoSignInDefaultCustomer();
            if (customer != null) return true;
            MessageBox.Show("Chưa tải được thông tin tài khoản khách hàng. Vui lòng kiểm tra kết nối SQL Server.",
                "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
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
            if (EnsureCustomerLoaded() && customer != null) OpenFeature(new TransferGUI(customer.idacc));
        }

        private void btnMyCredit_Click(object sender, RoutedEventArgs e)
        {
            if (EnsureCustomerLoaded() && customer != null) OpenFeature(new CreditHistorySearchGUI(customer.idacc));
        }

        private void btnMyDebt_Click(object sender, RoutedEventArgs e)
        {
            if (EnsureCustomerLoaded() && customer != null) OpenFeature(new DebtTrackingGUI(customer.idacc));
        }

        private void btnApplyLoan_Click(object sender, RoutedEventArgs e)
        {
            OpenFeature(new LoanApplicationGUI());
        }
    }
}
