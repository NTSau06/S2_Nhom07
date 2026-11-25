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
    /// Interaction logic for AccountBankingManageOverviewGUI.xaml
    /// </summary>
    public partial class AccountBankingManageOverviewGUI : Window
    {
        AccountBankingControl abc;
        string selectedAccountID = "";
        private string selectedAccountCCCD = "";
        private AccountBankSearchGUI? _searchWindow;
        public AccountBankingManageOverviewGUI()
        {
            Show();
        }
        public void Show()
        {
            InitializeComponent();
            abc = new AccountBankingControl();
            AccountDataGrid.ItemsSource = abc.showallaccountbank();
        }
        private void SukienThemTaiKhoan(object sender, RoutedEventArgs e)
        {
            AccountBankAddGUI accaddGUI = new AccountBankAddGUI(this)
            {
                Owner = this
            };
            accaddGUI.showABAddG();
        }
        private void SukienTimKiemTaiKhoan(object sender, RoutedEventArgs e)
        {
            if (_searchWindow == null || !_searchWindow.IsLoaded)
            {
                _searchWindow = new AccountBankSearchGUI(this)
                {
                    Owner = this
                };
                _searchWindow.Closed += (_, _) => _searchWindow = null;
            }

            _searchWindow.showABSearchG();
            _searchWindow.Focus();
        }
        private void SukienXemChiTietTaiKhoan(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string idacc && !string.IsNullOrWhiteSpace(idacc))
            {
                selectedAccountID = idacc;

                AccountBank detailAccount = abc.showDetailAccountbyID(selectedAccountID);
                AccountBankSeeDetailGUI accseedetailGUI = new AccountBankSeeDetailGUI(detailAccount);
                accseedetailGUI.showABSDG();
            }
        }
        private void SukienChinhSuaTaiKhoan(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is AccountBank rowAccount)
            {
                selectedAccountID = rowAccount.idacc;
                selectedAccountCCCD = rowAccount.CCCD;

                // Lấy bản mới nhất từ DB theo ID để đảm bảo dữ liệu gốc chính xác
                AccountBank? freshAccount = abc.showDetailAccountbyID(selectedAccountID) ?? rowAccount;

                AccountBankFixGUI editWindow = new AccountBankFixGUI(freshAccount)
                {
                    Owner = this
                };

                if (editWindow.ShowDialog() == true)
                {
                    // Tải lại danh sách trên DataGrid sau khi cập nhật thành công
                    AccountDataGrid.ItemsSource = abc.showallaccountbank();
                }
            }
        }
        public void loadAccountDataGrid()
        {
            AccountDataGrid.ItemsSource = abc.showallaccountbank();
        }
    }
}
