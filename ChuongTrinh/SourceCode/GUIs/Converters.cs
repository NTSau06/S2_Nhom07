using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BankingManagesandServicesApp.GUIs
{
    /// <summary>
    /// Chuyển đổi trạng thái (Hồ sơ vay, Dư nợ, Nhóm nợ, Thẻ tín dụng) thành màu nền / màu chữ cho Badge.
    /// ConverterParameter = "bg" (mặc định) hoặc "fg".
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        private static SolidColorBrush Hex(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string s = (value?.ToString() ?? "").Trim();
            bool fg = string.Equals(parameter?.ToString(), "fg", StringComparison.OrdinalIgnoreCase);

            return s switch
            {
                "Đã duyệt" or "Hoạt động" or "Trong hạn" or "Thành công" or "Nhóm 1"
                    => fg ? Hex("#166534") : Hex("#DCFCE7"),
                "Chờ duyệt" or "Nhóm 2"
                    => fg ? Hex("#92400E") : Hex("#FEF3C7"),
                "Chờ bổ sung"
                    => fg ? Hex("#9A3412") : Hex("#FFEDD5"),
                "Từ chối" or "Đã khóa" or "Quá hạn" or "Thất bại" or "Nhóm 3" or "Nhóm 4" or "Nhóm 5"
                    => fg ? Hex("#991B1B") : Hex("#FEE2E2"),
                "Đã tất toán"
                    => fg ? Hex("#1E40AF") : Hex("#DBEAFE"),
                "Vàng"
                    => fg ? Hex("#854D0E") : Hex("#FEF9C3"),
                "Bạch kim"
                    => fg ? Hex("#334155") : Hex("#E2E8F0"),
                "Chuẩn"
                    => fg ? Hex("#075985") : Hex("#E0F2FE"),
                "Nhận chuyển khoản"
                    => fg ? Hex("#166534") : Hex("#DCFCE7"),
                "Chuyển khoản"
                    => fg ? Hex("#1E3A8A") : Hex("#DBEAFE"),
                "Phí giao dịch"
                    => fg ? Hex("#6B21A8") : Hex("#F3E8FF"),
                _ => fg ? Hex("#475569") : Hex("#F1F5F9")
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Ẩn badge nếu chuỗi trạng thái rỗng.
    /// </summary>
    public class NonEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => string.IsNullOrWhiteSpace(value?.ToString()) ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
