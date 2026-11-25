using BankingManagesandServicesApp.Controls;
using BankingManagesandServicesApp.DBConnectandManages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BankingManagesandServicesApp.GUIs
{
    /// <summary>
    /// Interaction logic for AccountBankSearchGUI.xaml
    /// </summary>
    public partial class AccountBankSearchGUI : Window
    {
        AccountBankingControl abc;
        public List<AccountBank> SearchResults { get; private set; } = new List<AccountBank>();
        public bool IsSearchConfirmed { get; private set; } = false;
        private readonly AccountBankingManageOverviewGUI? _ownerOverview;
        public AccountBankSearchGUI(AccountBankingManageOverviewGUI? ownerOverview = null)
        {
            InitializeComponent();
            abc = new AccountBankingControl();
            _ownerOverview = ownerOverview;
        }
        public void showABSearchG()
        {
            base.Show();
        }
        private void SukienTimKiem(object sender, RoutedEventArgs e)
        {
            string selectedType = CboSearchType.SelectedItem is ComboBoxItem item
                ? item.Content?.ToString() ?? "Tất cả"
                : "Tất cả";

            int isValid = abc.searchaccountscontrol(
                TxtSearchId.Text,
                TxtSearchCCCD.Text,
                selectedType,
                TxtMinMoney.Text,
                TxtMaxMoney.Text,
                out List<AccountBank> results
            );

            if(isValid==1)
            {
                MessageBox.Show("Tài khoản đã nhập không đúng định dạng", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if(isValid==3)
            {
                MessageBox.Show("Tài khoản cần tìm không tồn tại", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            else if(isValid==4)
            {
                SearchResults = results;
                IsSearchConfirmed = true;
                if (_ownerOverview != null)
                {
                    _ownerOverview.AccountDataGrid.ItemsSource = results;
                }
                MessageBox.Show("Đã tìm thấy", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void SukienLamMoi(object sender, RoutedEventArgs e)
        {
            TxtSearchId.Text = "";
            TxtSearchCCCD.Text = "";
            TxtMinMoney.Text = "";
            TxtMaxMoney.Text = "";
        }
    }
}
