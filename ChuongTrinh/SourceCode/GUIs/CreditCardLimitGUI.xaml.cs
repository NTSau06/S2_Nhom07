using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BankingManagesandServicesApp.Controls;
using BankingManagesandServicesApp.DBManages;

namespace BankingManagesandServicesApp.GUIs
{
    /// <summary>
    /// Giao diện quản lý hạn mức thẻ tín dụng (UC1.2.8.2) - "Giao diện quản lý thẻ" trong Sequence Diagram.
    /// </summary>
    public partial class CreditCardLimitGUI : Window
    {
        private readonly CreditLoanControl clc = new CreditLoanControl();
        private CreditCard? selectedCard;

        public CreditCardLimitGUI()
        {
            InitializeComponent();
            Loaded += (s, e) => showCardLimitG();
        }

        /// <summary>Hiển thị giao diện, nạp toàn bộ danh sách thẻ.</summary>
        public void showCardLimitG()
        {
            LoadCards("");
            LoadHistory();
        }

        private void LoadCards(string keyword, string? selectId = null)
        {
            try
            {
                var cards = clc.loadcardlist(keyword);
                dgCards.ItemsSource = cards;
                tabCards.Header = $"Danh sách thẻ tín dụng ({cards.Count})";

                if (selectId != null)
                    dgCards.SelectedItem = cards.FirstOrDefault(c => c.idcard == selectId);
                else if (cards.Count > 0 && dgCards.SelectedItem == null)
                    dgCards.SelectedIndex = 0;

                if (cards.Count == 0 && keyword != "")
                    MessageBox.Show("Không tìm thấy thẻ tín dụng phù hợp.", "Không tìm thấy", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi truy cập cơ sở dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadHistory()
        {
            try
            {
                var logs = clc.loadcardlimithistory();
                dgLimitHistory.ItemsSource = logs;
                tabHistory.Header = $"Nhật ký điều chỉnh ({logs.Count})";
            }
            catch
            {
                // ActivityLog optional display
            }
        }

        private void btnSearch_Click(object sender, RoutedEventArgs e) => LoadCards(txtKeyword.Text.Trim());

        private void btnResetSearch_Click(object sender, RoutedEventArgs e)
        {
            txtKeyword.Text = "";
            LoadCards("");
        }

        private void txtKeyword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) LoadCards(txtKeyword.Text.Trim());
        }

        // Bước 2-7: chọn thẻ -> hệ thống truy xuất và hiển thị hạn mức hiện tại + form nhập hạn mức mới
        private void dgCards_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedCard = null;
            if (dgCards.SelectedItem is CreditCard row)
            {
                try
                {
                    selectedCard = clc.loadcardlimit(row.idcard);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi truy cập cơ sở dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            bool has = selectedCard != null;
            txtNewLimit.IsEnabled = txtReason.IsEnabled = btnSave.IsEnabled = pnlPresets.IsEnabled = has;
            txtNewLimit.Text = "";
            txtReason.Text = "";
            lblNewLimitHint.Text = "";

            if (selectedCard == null)
            {
                lblVisualStatus.Text = "Chưa chọn";
                lblVisualTier.Text = "CREDIT CARD";
                lblVisualStyle.Text = "";
                lblVisualNumber.Text = "••••  ••••  ••••  ----";
                lblVisualHolder.Text = "CHỌN THẺ BÊN TRÁI";
                lblVisualAcc.Text = "—";
                lblCurrentLimit.Text = "—";
                lblMaxLimit.Text = "—";
                pbLimit.Value = 0;
                pnlLockedBanner.Visibility = Visibility.Collapsed;
                return;
            }

            lblVisualStatus.Text = selectedCard.status;
            lblVisualTier.Text = $"HẠNG {selectedCard.cardtype.ToUpper()}  •  {selectedCard.idcard}";
            lblVisualStyle.Text = $"{selectedCard.cardstyle} ({selectedCard.cardcolor})";
            lblVisualNumber.Text = selectedCard.maskedcardnumber;
            lblVisualHolder.Text = selectedCard.fullname.ToUpper();
            lblVisualAcc.Text = selectedCard.idacc;

            lblCurrentLimit.Text = $"{selectedCard.currentlimit:N0} VNĐ";
            lblMaxLimit.Text = $"{selectedCard.maxlimit:N0} VNĐ";
            pbLimit.Value = selectedCard.limitpercent;

            bool isLocked = selectedCard.status != CreditCard.StatusActive;
            pnlLockedBanner.Visibility = isLocked ? Visibility.Visible : Visibility.Collapsed;
            ApplyCardTheme(selectedCard);
        }

        private void ApplyCardTheme(CreditCard c)
        {
            string c1, c2, c3;
            if (c.status != CreditCard.StatusActive || c.cardcolor.Contains("Đen", StringComparison.OrdinalIgnoreCase))
            {
                c1 = "#1E293B"; c2 = "#334155"; c3 = "#0F172A";
            }
            else if (c.cardtype.Contains("Bạch kim", StringComparison.OrdinalIgnoreCase) || c.cardcolor.Contains("Bạc", StringComparison.OrdinalIgnoreCase))
            {
                c1 = "#334155"; c2 = "#64748B"; c3 = "#1E293B";
            }
            else if (c.cardtype.Contains("Vàng", StringComparison.OrdinalIgnoreCase) || c.cardcolor.Contains("Vàng", StringComparison.OrdinalIgnoreCase))
            {
                c1 = "#92400E"; c2 = "#B45309"; c3 = "#78350F";
            }
            else
            {
                c1 = "#0F3460"; c2 = "#1D6FA5"; c3 = "#0E7490";
            }

            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(c1), 0));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(c2), 0.55));
            brush.GradientStops.Add(new GradientStop((Color)ColorConverter.ConvertFromString(c3), 1));
            cardVisual.Background = brush;
        }

        private void PresetLimit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCard == null) return;
            if (sender is Button btn && btn.Tag is string pctStr && int.TryParse(pctStr, out int pct))
            {
                int val = (int)Math.Round(selectedCard.maxlimit * (pct / 100.0) / 1_000_000.0) * 1_000_000;
                if (val <= 0) val = selectedCard.maxlimit;
                txtNewLimit.Text = val.ToString("N0");
            }
        }

        private void txtNewLimit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (selectedCard == null || string.IsNullOrWhiteSpace(txtNewLimit.Text))
            {
                lblNewLimitHint.Text = "";
                return;
            }
            if (MoneyHelper.TryParseMoney(txtNewLimit.Text, out int val) && val > 0)
            {
                if (val > selectedCard.maxlimit)
                {
                    lblNewLimitHint.Text = $"⚠ Vượt hạn mức tối đa của hạng {selectedCard.cardtype} ({selectedCard.maxlimit:N0} VNĐ)";
                    lblNewLimitHint.Foreground = (SolidColorBrush)FindResource("DangerBrush");
                }
                else
                {
                    lblNewLimitHint.Text = $"{val:N0} VNĐ — {MoneyHelper.MoneyToVietnameseWords(val)}";
                    lblNewLimitHint.Foreground = (SolidColorBrush)FindResource("AccentBrush");
                }
            }
            else
            {
                lblNewLimitHint.Text = "Chỉ nhập số nguyên dương.";
                lblNewLimitHint.Foreground = (SolidColorBrush)FindResource("DangerBrush");
            }
        }

        // Bước 8-10: nhập hạn mức mới + lý do, nhấn "Lưu"
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (selectedCard == null) return;
            int code = clc.checkformatlimit(selectedCard, txtNewLimit.Text, txtReason.Text, out int newlimit);
            if (code != 0)
            {
                // 4a: hạn mức vượt quy định -> cảnh báo, yêu cầu nhập lại
                MessageBox.Show(CreditLoanControl.limitmessage(code, selectedCard), "Không hợp lệ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                if (code == 4) txtReason.Focus(); else { txtNewLimit.SelectAll(); txtNewLimit.Focus(); }
                return;
            }

            if (MessageBox.Show($"Điều chỉnh hạn mức thẻ {selectedCard.idcard} từ {selectedCard.currentlimit:N0} thành {newlimit:N0} VNĐ?",
                    "Xác nhận điều chỉnh", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                if (clc.updatelimit(selectedCard.idcard, newlimit, txtReason.Text))
                {
                    MessageBox.Show("Cập nhật hạn mức thành công và đã ghi vào nhật ký hoạt động.", "Thành công",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadCards(txtKeyword.Text.Trim(), selectedCard.idcard);
                    LoadHistory();
                }
                else
                {
                    MessageBox.Show("Cập nhật thất bại (thẻ không tồn tại hoặc hạn mức vượt quy định).", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi truy cập cơ sở dữ liệu:\n" + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
