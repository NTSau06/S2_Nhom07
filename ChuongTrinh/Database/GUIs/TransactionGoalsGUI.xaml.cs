using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace BankingManagesandServicesApp.GUIs
{
    // Dữ liệu cho từng bảng (dữ liệu mẫu, không dùng database)
    public class Kpi
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public decimal Target { get; set; }
        public decimal Actual { get; set; }
        public string Completion => Target == 0 ? "0%" : (Actual / Target * 100).ToString("0.#") + "%";
    }

    public class VolumeRow
    {
        public string Type { get; set; } = "";
        public int Count { get; set; }
        public decimal Amount { get; set; }
        public double Share { get; set; }
    }

    public class StrategyRow
    {
        public string Service { get; set; } = "";
        public decimal Previous { get; set; }
        public decimal Current { get; set; }
        public string Growth => ((Current - Previous) / Previous * 100).ToString("+0.#;-0.#") + "%";
        public string Assessment => Current >= Previous ? "Tăng trưởng tốt" : "Cần cải thiện";
    }

    public partial class TransactionGoalsGUI : Window
    {
        private readonly ObservableCollection<Kpi> kpis = new()
        {
            new Kpi { Code = "KPI01", Name = "Chỉ tiêu chuyển khoản tháng 11", Type = "Chuyển khoản",       Target = 1_000_000_000, Actual = 1_120_000_000 },
            new Kpi { Code = "KPI02", Name = "Chỉ tiêu nạp tiền tháng 11",     Type = "Nạp tiền",           Target =   500_000_000, Actual =   410_000_000 },
            new Kpi { Code = "KPI03", Name = "Chỉ tiêu rút tiền tháng 11",     Type = "Rút tiền",           Target =   300_000_000, Actual =   210_000_000 },
            new Kpi { Code = "KPI04", Name = "Chỉ tiêu thanh toán hóa đơn",    Type = "Thanh toán hóa đơn", Target =   200_000_000, Actual =   205_000_000 }
        };

        private readonly List<VolumeRow> volumes = new()
        {
            new VolumeRow { Type = "Chuyển khoản",       Count = 183, Amount = 1_481_000_000 },
            new VolumeRow { Type = "Nạp tiền",           Count =  79, Amount =   344_700_000 },
            new VolumeRow { Type = "Rút tiền",           Count =  37, Amount =   123_600_000 },
            new VolumeRow { Type = "Thanh toán hóa đơn", Count = 120, Amount =   200_900_000 }
        };

        private readonly List<StrategyRow> strategies = new()
        {
            new StrategyRow { Service = "Chuyển khoản",       Previous = 1_300_000_000, Current = 1_481_000_000 },
            new StrategyRow { Service = "Nạp tiền",           Previous =   435_900_000, Current =   344_700_000 },
            new StrategyRow { Service = "Rút tiền",           Previous =   236_600_000, Current =   123_600_000 },
            new StrategyRow { Service = "Thanh toán hóa đơn", Previous =   163_300_000, Current =   200_900_000 }
        };

        private Kpi? editingKpi;   // null = đang thêm KPI mới

        public TransactionGoalsGUI()
        {
            InitializeComponent();

            // UC1.2.5.1: hiển thị danh sách KPI
            KpiGrid.ItemsSource = kpis;
            TypeBox.ItemsSource = new[] { "Chuyển khoản", "Nạp tiền", "Rút tiền", "Thanh toán hóa đơn" };
            TypeBox.SelectedIndex = 0;

            // UC1.2.5.2: tổng hợp quy mô giao dịch
            decimal total = volumes.Sum(x => x.Amount);
            foreach (var v in volumes) v.Share = (double)(v.Amount / total * 100);
            VolumeGrid.ItemsSource = volumes;
            TotalCountText.Text = $"Tổng số giao dịch: {volumes.Sum(x => x.Count):N0}";
            TotalAmountText.Text = $"Tổng giá trị: {total:N0} VND";

            // UC1.2.5.3: hiệu quả từng dịch vụ
            StrategyGrid.ItemsSource = strategies;

            // UC1.2.5.4: mặc định 30 ngày gần nhất
            FromDate.SelectedDate = DateTime.Today.AddDays(-30);
            ToDate.SelectedDate = DateTime.Today;
        }

        // ---------- UC1.2.5.1 Quản lý KPI ----------
        private void KpiGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (KpiGrid.SelectedItem is not Kpi k) return;
            editingKpi = k;
            NameBox.Text = k.Name;
            TypeBox.SelectedItem = k.Type;
            TargetBox.Text = k.Target.ToString("0");
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            editingKpi = null;
            KpiGrid.SelectedItem = null;
            NameBox.Text = "";
            TargetBox.Text = "";
            NameBox.Focus();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // 3a. Dữ liệu không hợp lệ -> báo lỗi, yêu cầu nhập lại
            if (string.IsNullOrWhiteSpace(NameBox.Text) ||
                !decimal.TryParse(TargetBox.Text.Replace(".", "").Replace(",", ""), out decimal target) || target <= 0)
            {
                MessageBox.Show("Dữ liệu không hợp lệ. Hãy nhập tên chỉ tiêu và mục tiêu là số lớn hơn 0.",
                    "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (editingKpi == null)
            {
                kpis.Add(new Kpi { Code = $"KPI{kpis.Count + 1:00}", Name = NameBox.Text.Trim(), Type = (string)TypeBox.SelectedItem, Target = target });
            }
            else
            {
                editingKpi.Name = NameBox.Text.Trim();
                editingKpi.Type = (string)TypeBox.SelectedItem;
                editingKpi.Target = target;
            }
            KpiGrid.Items.Refresh();
            StatusText.Text = "Cập nhật chỉ tiêu KPI thành công.";
            New_Click(sender, e);
        }

        // ---------- UC1.2.5.4 Xuất báo cáo ----------
        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (FromDate.SelectedDate == null || ToDate.SelectedDate == null || FromDate.SelectedDate > ToDate.SelectedDate)
            {
                MessageBox.Show("Hãy chọn khoảng thời gian hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "File Excel (*.csv)|*.csv",
                FileName = "BaoCao_" + DateTime.Now.ToString("yyyyMMdd_HHmm")
            };
            if (dialog.ShowDialog() != true) return;

            var sb = new StringBuilder();
            string reportName = ((ComboBoxItem)ReportTypeBox.SelectedItem).Content.ToString()!;
            sb.AppendLine(reportName);
            sb.AppendLine($"Từ {FromDate.SelectedDate:dd/MM/yyyy}\tđến {ToDate.SelectedDate:dd/MM/yyyy}");
            sb.AppendLine();
            switch (ReportTypeBox.SelectedIndex)
            {
                case 0:
                    sb.AppendLine("Mã\tTên chỉ tiêu\tLoại giao dịch\tMục tiêu (VND)\tThực hiện (VND)\tHoàn thành");
                    foreach (var k in kpis) sb.AppendLine($"{k.Code}\t{k.Name}\t{k.Type}\t{k.Target}\t{k.Actual}\t{k.Completion}");
                    break;
                case 1:
                    sb.AppendLine("Loại giao dịch\tSố giao dịch\tTổng giá trị (VND)");
                    foreach (var v in volumes) sb.AppendLine($"{v.Type}\t{v.Count}\t{v.Amount}");
                    break;
                default:
                    sb.AppendLine("Dịch vụ\tKỳ trước (VND)\tKỳ này (VND)\tTăng trưởng\tĐánh giá");
                    foreach (var s in strategies) sb.AppendLine($"{s.Service}\t{s.Previous}\t{s.Current}\t{s.Growth}\t{s.Assessment}");
                    break;
            }

            // Unicode + phân cách bằng Tab: Excel mở đúng tiếng Việt và tự chia cột
            File.WriteAllText(dialog.FileName, sb.ToString(), Encoding.Unicode);
            StatusText.Text = "Đã xuất báo cáo: " + dialog.FileName;
        }
    }
}