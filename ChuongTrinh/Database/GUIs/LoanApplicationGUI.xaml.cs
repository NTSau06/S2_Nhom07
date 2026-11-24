using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Win32;

namespace BankingManagesandServicesApp.GUIs
{
    // Một hồ sơ vay (dữ liệu mẫu, không dùng database)
    public class LoanApplication
    {
        public string Code { get; set; } = "";
        public decimal Amount { get; set; }
        public string Term { get; set; } = "";
        public string Purpose { get; set; } = "";
        public string Status { get; set; } = "Chờ duyệt";
    }

    public partial class LoanApplicationGUI : Window
    {
        private readonly ObservableCollection<LoanApplication> loans = new();

        public LoanApplicationGUI()
        {
            InitializeComponent();
            // Bước 2: hiển thị biểu mẫu đăng ký khoản vay
            TermBox.ItemsSource = new[] { "6 tháng", "12 tháng", "24 tháng", "36 tháng", "60 tháng" };
            TermBox.SelectedIndex = 1;
            LoanGrid.ItemsSource = loans;
        }

        // Bước 3: tải lên chứng từ
        private void ChooseIdCard_Click(object sender, RoutedEventArgs e) => ChooseFile(IdCardBox);
        private void ChooseIncome_Click(object sender, RoutedEventArgs e) => ChooseFile(IncomeBox);

        private void ChooseFile(System.Windows.Controls.TextBox box)
        {
            var dialog = new OpenFileDialog { Filter = "Ảnh hoặc PDF (*.jpg;*.png;*.pdf)|*.jpg;*.jpeg;*.png;*.pdf" };
            if (dialog.ShowDialog() == true) box.Text = dialog.FileName;
        }

        // Bước 4–5: kiểm tra lại, xác nhận gửi
        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            // 3a. Thiếu thông tin hoặc sai định dạng -> báo lỗi, yêu cầu chỉnh sửa
            if (!decimal.TryParse(AmountBox.Text.Replace(".", "").Replace(",", ""), out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Số tiền vay phải là số lớn hơn 0.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(PurposeBox.Text) || IdCardBox.Text == "" || IncomeBox.Text == "")
            {
                MessageBox.Show("Vui lòng nhập mục đích vay và tải lên đủ CCCD/CMND, chứng minh thu nhập.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Bước 4: khách hàng kiểm tra lại thông tin
            string term = (string)TermBox.SelectedItem;
            var confirm = MessageBox.Show(
                $"Số tiền: {amount:N0} VND\nThời hạn: {term}\nMục đích: {PurposeBox.Text.Trim()}\n\nXác nhận gửi hồ sơ?",
                "Kiểm tra thông tin", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            // Bước 5: tạo mã hồ sơ, trạng thái "Chờ duyệt"
            var loan = new LoanApplication
            {
                Code = $"HSV{DateTime.Now:yyMMdd}{loans.Count + 1:000}",
                Amount = amount,
                Term = term,
                Purpose = PurposeBox.Text.Trim()
            };
            loans.Add(loan);
            ClearForm();
            StatusText.Text = $"Gửi hồ sơ thành công. Mã hồ sơ: {loan.Code} – trạng thái: Chờ duyệt.";
        }

        // 4a. Hủy thao tác -> xóa thông tin đã nhập, quay về trang chủ
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            Close();
        }

        private void ClearForm()
        {
            AmountBox.Text = "";
            PurposeBox.Text = "";
            IdCardBox.Text = "";
            IncomeBox.Text = "";
            TermBox.SelectedIndex = 1;
        }
    }
}