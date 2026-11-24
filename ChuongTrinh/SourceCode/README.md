# Demo quản lý nhật ký và xuất chứng từ giao dịch

Ứng dụng C# WPF, .NET 9, SQL Server. Khởi động tại màn hình đăng nhập.

## Chạy trên máy hiện tại

1. Dịch vụ SQL Server `SQLEXPRESS` cần đang chạy.
2. Database `QuanLyGiaoDichNganHang` cần có 18 bảng từ file `SQLQuery3.sql` của nhóm. Máy hiện tại đã tạo database này; không chạy lại `CREATE DATABASE`.
3. Mở `BankingManagesandServicesApp.csproj` trong Visual Studio và chạy F5, hoặc dùng terminal trong thư mục dự án:

```powershell
dotnet run --project .\BankingManagesandServicesApp.csproj
```

4. Tại màn hình đăng nhập, bấm **Tạo dữ liệu demo**. Thao tác này thêm dữ liệu có mã `DEMO-` và tài khoản riêng, không xóa bảng, không đặt lại mật khẩu hiện có. Bấm nhiều lần không tạo trùng. Nếu mã demo bị trùng dữ liệu khác, chương trình dừng và báo lỗi.
5. Đăng nhập:

| Thông tin | Giá trị |
|---|---|
| Tên đăng nhập | `admin.demo` |
| Mật khẩu | `Demo@123456` |
| Họ tên hiển thị | Nguyễn Minh Anh |

## Kịch bản trình bày demo

1. Nhập mật khẩu sai: hiện thông báo, không mở giao diện chức năng.
2. Đăng nhập đúng: mở **Quản lý nhật ký giao dịch**; góc trên hiện `Nguyễn Minh Anh (admin.demo)` và vai trò.
3. Nhập `DEMO-`, bấm **Sàng lọc**: bộ demo có 4 giao dịch, tổng 33.000.000 VND, 2 thành công với số tiền 12.500.000 VND, tỷ lệ thành công 50%, 1 giao dịch có cảnh báo. Có thể lọc theo từ khóa, trạng thái, khoảng ngày (bao gồm cả ngày kết thúc), loại giao dịch, khoảng số tiền và cảnh báo. Số tiền lọc nhập số nguyên VND không có dấu phân cách, ví dụ `2500000`; để trống để không giới hạn. Loại giao dịch để trống nghĩa là tất cả; có thể chọn gợi ý hoặc nhập loại khác.
   - Chọn loại `Chuyển khoản`, số tiền từ `2500000` đến `20000000`, cảnh báo **Không có cảnh báo**, bấm **Sàng lọc**: chỉ còn `DEMO-GD001`.
   - Mở tab **Thống kê giao dịch** để xem số lượng và tổng tiền theo trạng thái/loại giao dịch. Thống kê chỉ tính các giao dịch trong kết quả lọc đã áp dụng; thay đổi tiêu chí cần bấm **Sàng lọc**. Tổng số tiền gồm mọi trạng thái; đây là giá trị giao dịch, không phải số dư hoặc doanh thu.
   - Bấm **Đặt lại** để bỏ toàn bộ điều kiện. Tìm `DEMO-GD001`, mở tab **Giao dịch và nhật ký**, chọn dòng để đọc lịch sử.
4. Bấm **Xem / Xuất chứng từ** để xem thông tin giao dịch, khách hàng, số tiền, người xuất và thời điểm xuất.
5. Chọn **PDF** hoặc **HTML**, bấm **Lưu chứng từ**, chọn nơi lưu. Tệp PDF hỗ trợ tiếng Việt; HTML mở bằng trình duyệt và có nút In.
6. Đóng cửa sổ chứng từ: nhật ký có dòng **Xuất chứng từ**. Mỗi lần lưu thành công tạo một dòng `TransactionLog` và một dòng `ActivityLog`. Hủy hộp thoại lưu không ghi log xuất.
7. Thử `DEMO-GD003` (đang xử lý) và `DEMO-GD004` (thất bại): nút chứng từ bị khóa. Đây là quy tắc nghiệp vụ của bản demo.
8. Mở **Tài khoản khách hàng**: thanh tên tài khoản/đăng xuất dùng chung vẫn có mặt. Màn hình này hỗ trợ xem và tìm kiếm tài khoản; nghiệp vụ thêm/sửa tài khoản chưa thuộc chức năng lần này.
9. Bấm **Đăng xuất** trên bất kỳ màn hình chức năng nào: đóng các màn hình của phiên, xóa tài khoản đang đăng nhập, trở về đăng nhập. Thử đăng nhập lại để tiếp tục.

## Cấu hình trên máy khác

Sửa `database.config` tại thư mục dự án rồi build lại. Nếu chạy trực tiếp bản build, sửa file cạnh `.exe`.

```xml
<database connectionString="Server=.\SQLEXPRESS;Database=QuanLyGiaoDichNganHang;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10" />
```

Kết nối dùng tài khoản Windows của người chạy ứng dụng. Tài khoản này cần quyền đọc các bảng và thêm dữ liệu/log cho demo. `TrustServerCertificate=True` phục vụ SQL Server local của nhóm.

## Ánh xạ sơ đồ RDM

| Thành phần | Chức năng |
|---|---|
| `Account`, `Role` | Xác thực tài khoản đang hoạt động, đọc họ tên/vai trò |
| `Permission`, `RolePermission` | Quyền `ExportReceipt`; kiểm tra lại từ SQL trước khi xuất |
| `TransactionBank`, `AccountBank` | Dữ liệu giao dịch và khách hàng trên chứng từ |
| `TransactionLog` | Lịch sử từng giao dịch, bao gồm thao tác xuất chứng từ |
| `ActivityLog` | Đăng nhập, đăng xuất, xuất chứng từ theo nhân viên |

Không đổi cấu trúc 18 bảng gốc. Mật khẩu demo được băm PBKDF2-SHA256 với salt ngẫu nhiên; mật khẩu dạng chữ thường trong database không được chấp nhận. Chưa triển khai đăng ký, đổi mật khẩu hoặc đặt lại mật khẩu.

`SessionHeader` được dùng trong tất cả cửa sổ chức năng hiện có. Màn hình chức năng mới cần kế thừa `AuthenticatedWindow` và đặt `SessionHeader` trên cùng. `UserSession` giữ tài khoản hiện tại; không truyền mật khẩu giữa các màn hình.

Tệp chỉ được thay thế sau khi tạo đủ dữ liệu thành công. Nếu đã lưu tệp nhưng ghi log SQL thất bại, ứng dụng cảnh báo rõ ràng; hệ thống tệp và SQL Server không nằm trong cùng một giao dịch phân tán. Chứng từ có nhãn **DEMO ĐỒ ÁN**, không có chữ ký số và không dùng làm chứng từ ngân hàng thực tế.

## Kiểm tra trong SSMS

```sql
USE QuanLyGiaoDichNganHang;
SELECT * FROM dbo.TransactionLog WHERE idtrs = N'DEMO-GD001' ORDER BY timestamp DESC, logId DESC;
SELECT * FROM dbo.ActivityLog WHERE accountId = 900001 ORDER BY timestamp DESC, logId DESC;
```

## Kiểm thử tích hợp

```powershell
dotnet run --project .\Tests\Banking.SmokeTests.csproj
```

Kiểm thử dùng SQL Server trong `database.config`, chuẩn bị bộ demo và ghi log kiểm thử. Kết quả, ảnh giao diện và chứng từ mẫu nằm ở `output/verification/`. Chỉ chạy trên database demo của nhóm.

Kiểm tra riêng logic sàng lọc và thống kê (không mở giao diện, không cần SQL Server):

```powershell
dotnet run --project .\Tests\Banking.SmokeTests.csproj -- --analysis-only
```

Bộ kiểm thử tích hợp còn kiểm tra bộ lọc nâng cao trên giao diện, thống kê sau lọc, nhập số tiền sai, đặt lại và kết quả rỗng. Ảnh tab thống kê: `output/verification/05-statistics.png`. Chứng từ demo: `output/verification/receipt-demo.pdf` và `receipt-demo.html`.

Thư viện: [Microsoft.Data.SqlClient](https://www.nuget.org/packages/Microsoft.Data.SqlClient/6.1.8), [PDFsharp-WPF](https://www.nuget.org/packages/PDFsharp-WPF/6.2.4). Hướng dẫn phông chữ từ [PDFsharp](https://docs.pdfsharp.net/PDFsharp/Topics/Fonts/Font-Resolving.html).
