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
    /// Interaction logic for AccountBankSeeDetailGUI.xaml
    /// </summary>
    public partial class AccountBankSeeDetailGUI : Window
    {
        public AccountBankSeeDetailGUI(AccountBank account)
        {
            InitializeComponent();
            BindAccountData(account);
        }
        public void showABSDG()
        {
            base.Show();
        }
        private void BindAccountData(AccountBank acc)
        {
            IDText.Text = acc.idacc;
            PasswordText.Text = acc.passwordacc;
            FullNameText.Text = acc.fullname;
            GenderText.Text = acc.gender;
            BirthdayText.Text = acc.birthday.ToString("yyyy-MM-dd");
            CCCDText.Text = acc.CCCD;
            PhoneText.Text = acc.phone;
            AddressText.Text = acc.address;
            EmailText.Text = acc.email;
            TypeText.Text = acc.typeacc;
            ProvidedateText.Text = acc.provideaccdate.ToString("yyyy-MM-dd HH:mm:ss");
            MoneyText.Text = acc.money.ToString();
        }
    }
}
