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
using BankingManagesandServicesApp.Controls;

namespace BankingManagesandServicesApp.GUIs
{
    /// <summary>
    /// Interaction logic for AccountBankingManageOverviewGUI.xaml
    /// </summary>
    public partial class AccountBankingManageOverviewGUI : Window
    {
        Controls.AccountBankingControl abc;
        public AccountBankingManageOverviewGUI()
        {
            Show();
        }
        private void Show()
        {
            InitializeComponent();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
