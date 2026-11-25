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
    public partial class AccountBankAddGUI : Window
    {
        AccountBankingControl abc;
        string genderselected = "";
        string typeselected = "";
        private readonly AccountBankingManageOverviewGUI? _ownerOverview;
        private bool _isLoadingCccd = false;
        public AccountBankAddGUI(AccountBankingManageOverviewGUI? ownerOverview = null)
        {
            InitializeComponent();
            abc = new AccountBankingControl();
            _ownerOverview = ownerOverview;
            LoadExistingCccdToComboBox();
        }
        public void showABAddG()
        {
            base.Show();
        }
        private void SukienHoanThanh(object sender, RoutedEventArgs e)
        {
            if (GenderText.SelectedItem is ComboBoxItem itemgender)
            {
                genderselected = itemgender.Content.ToString();
            }
            if (TypeText.SelectedItem is ComboBoxItem itemtype)
            {
                typeselected = itemtype.Content.ToString();
            }
            string cccdInput = CCCDComboBox.Text?.Trim() ?? string.Empty;
            int addaccountstatus = abc.addaccountcontrol(cccdInput, FullNameText.Text, genderselected, EmailText.Text, BirthdayText.Text, PhoneText.Text, AddressText.Text, typeselected);
            if (addaccountstatus == 1)
            {
                MessageBox.Show("Tài khoản đã nhập không đúng định dạng", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (addaccountstatus == 3)
            {
                MessageBox.Show("Tài khoản vừa nhập không khớp với tài khoản đã lưu", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (addaccountstatus == 4)
            {
                MessageBox.Show("Tài khoản đã tồn tại", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (addaccountstatus == 5)
            {
                MessageBox.Show("Email hoặc Số điện thoại bị trùng", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (addaccountstatus == 6)
            {
                MessageBox.Show("Thêm tài khoản thành công", "Thông Báo", MessageBoxButton.OK, MessageBoxImage.Information);
                _ownerOverview?.loadAccountDataGrid();
            }
        }
        private void SukienLamMoi(object sender, RoutedEventArgs e)
        {
            FullNameText.Text = "";
            EmailText.Text = "";
            BirthdayText.Text = "";
            PhoneText.Text = "";
            AddressText.Text = "";
        }
        private void LoadExistingCccdToComboBox()
        {
            _isLoadingCccd = true;
            List<string> cccdList = abc.getallcccdcontrol();
            CCCDComboBox.ItemsSource = cccdList;
            CCCDComboBox.SelectedIndex = -1;
            _isLoadingCccd = false;
        }
        private void CCCDComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoadingCccd) return;

            string? selectedCCCD = CCCDComboBox.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedCCCD)) return;

            AccountBank? info = abc.showaccountbankByCCCD(selectedCCCD);
            if (info != null)
            {
                FullNameText.Text = info.fullname;
                PhoneText.Text = info.phone;
                AddressText.Text = info.address;
                EmailText.Text = info.email;
                BirthdayText.Text = info.birthday == DateOnly.MinValue
                    ? string.Empty
                    : info.birthday.ToString("yyyy-MM-dd");

                // Chọn đúng giới tính trên ComboBox GenderText
                foreach (var item in GenderText.Items)
                {
                    if (item is ComboBoxItem cbi && cbi.Content?.ToString() == info.gender)
                    {
                        GenderText.SelectedItem = cbi;
                        break;
                    }
                }
            }
        }
    }
}