using BankingManagesandServicesApp.Controls;
using BankingManagesandServicesApp.DBConnectandManages;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Principal;
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
    /// Interaction logic for AccountBankFixGUI.xaml
    /// </summary>
    public partial class AccountBankFixGUI : Window
    {
        AccountBankingControl abc;
        private readonly string _idacc;
        private readonly string _cccd;
        private readonly string _originalPhone;
        private readonly string _originalAddress;
        private readonly string _originalEmail;

        private bool _isInitialized = false;
        public AccountBankFixGUI(AccountBank account)
        {
            InitializeComponent();
            _idacc = account.idacc;
            _cccd = account.CCCD;
            _originalPhone = account.phone;
            _originalAddress = account.address;
            _originalEmail = account.email ?? string.Empty;

            IDText.Text = _idacc;
            CCCDText.Text = _cccd;
            PhoneText.Text = _originalPhone;
            AddressText.Text = _originalAddress;
            EmailText.Text = _originalEmail;

            _isInitialized = true;
            UpdateSaveButtonState();
            abc = new AccountBankingControl();
        }
        private void InputField_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized)
            {
                return;
            }

            UpdateSaveButtonState();
        }

        private void UpdateSaveButtonState()
        {
            bool hasChanged =
                PhoneText.Text != _originalPhone ||
                AddressText.Text != _originalAddress ||
                EmailText.Text != _originalEmail;

            HoanThanhButton.IsEnabled = hasChanged;
        }
        public void showABFG()
        {
            base.Show();
        }
        private void SukienHoanThanh(object sender, RoutedEventArgs e)
        {
            int updatestatus = abc.updateaccountcontrol(
                _idacc,
                _cccd,
                PhoneText.Text,
                AddressText.Text,
                EmailText.Text
            );

            if (updatestatus==1)
            {
                MessageBox.Show("Tài khoản đã nhập không đúng định dạng", "Lỗi cập nhật", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else if(updatestatus == 2)
            {
                MessageBox.Show("Tài khoản đã được cập nhật thành công", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void SukienHoanTac(object sender, RoutedEventArgs e)
        {
            PhoneText.Text = "";
            AddressText.Text = "";
            EmailText.Text = "";
        }
    }
}
