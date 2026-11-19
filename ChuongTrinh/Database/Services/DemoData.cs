using System.Data;
using BankingManagesandServicesApp.DBManages;
using Microsoft.Data.SqlClient;
namespace BankingManagesandServicesApp.Services;

public static class DemoData
{
    public const string Username = "admin.demo";
    public const string Password = "Demo@123456";
    // Explicitly invoked demo setup. No clearing tables and no resetting existing passwords.
    public static async Task EnsureCreatedAsync()
    {
        await using var db = await DBConnectandManage.OpenAsync();
        await using var tx = (SqlTransaction)await db.BeginTransactionAsync(IsolationLevel.Serializable);
        await using var cmd = new SqlCommand("""
            IF NOT EXISTS (SELECT 1 FROM dbo.Account WITH (UPDLOCK,HOLDLOCK) WHERE username=N'admin.demo')
            BEGIN
                IF EXISTS (SELECT 1 FROM dbo.Account WHERE accountId=900001)
                    OR EXISTS (SELECT 1 FROM dbo.Role WHERE roleId=900001)
                    OR EXISTS (SELECT 1 FROM dbo.Permission WHERE permissionId=900001)
                    OR EXISTS (SELECT 1 FROM dbo.AccountBank WHERE idacc LIKE N'DEMO-%')
                    OR EXISTS (SELECT 1 FROM dbo.TransactionBank WHERE idtrs LIKE N'DEMO-%')
                    THROW 50001,N'Mã demo đã được sử dụng. Không thay đổi dữ liệu hiện có.',1;
                INSERT dbo.Role(roleId,roleName,description) VALUES(900001,N'Nhân viên demo',N'Vai trò demo xuất chứng từ');
                INSERT dbo.Permission(permissionId,permissionName,description) VALUES(900001,N'ExportReceipt',N'Xuất chứng từ giao dịch');
                INSERT dbo.RolePermission VALUES(900001,900001);
                INSERT dbo.Account(accountId,roleId,fullName,username,password,status,createdDate)
                    VALUES(900001,900001,N'Nguyễn Minh Anh',N'admin.demo',@hash,N'Active',GETDATE());
                INSERT dbo.AccountBank(idacc,fullname,typeacc,provideaccdate,money) VALUES
                    (N'DEMO-TK001',N'Trần Hoàng Nam',N'Thanh toán',GETDATE(),15000000),
                    (N'DEMO-TK002',N'Lê Thị Mai',N'Thanh toán',GETDATE(),8500000);
                INSERT dbo.TransactionBank(idtrs,idacc,amount,transactionType,status,transactionTime,description,isFlagged,flagReason) VALUES
                    (N'DEMO-GD001',N'DEMO-TK001',2500000,N'Chuyển khoản',N'Thành công',DATEADD(minute,-15,GETDATE()),N'Thanh toán hóa đơn dịch vụ',0,NULL),
                    (N'DEMO-GD002',N'DEMO-TK002',10000000,N'Nộp tiền',N'Thành công',DATEADD(day,-1,GETDATE()),N'Nộp tiền tại quầy giao dịch',0,NULL),
                    (N'DEMO-GD003',N'DEMO-TK001',500000,N'Rút tiền',N'Đang xử lý',GETDATE(),N'Chờ xác nhận giao dịch',0,NULL),
                    (N'DEMO-GD004',N'DEMO-TK002',20000000,N'Chuyển khoản',N'Thất bại',DATEADD(day,-2,GETDATE()),N'Số dư không đủ',1,N'Giao dịch bị từ chối do số dư không đủ');
                DECLARE @base BIGINT=(SELECT COALESCE(MAX(logId),0) FROM dbo.TransactionLog WITH (UPDLOCK,HOLDLOCK));
                INSERT dbo.TransactionLog(logId,idtrs,action,timestamp,result,details)
                    SELECT @base+ROW_NUMBER() OVER(ORDER BY idtrs),idtrs,N'Tạo giao dịch',transactionTime,status,N'Dữ liệu demo cho đồ án'
                    FROM dbo.TransactionBank WHERE idtrs IN(N'DEMO-GD001',N'DEMO-GD002',N'DEMO-GD003',N'DEMO-GD004');
            END
            """, db, tx);
        cmd.Parameters.Add("@hash", SqlDbType.NVarChar, 255).Value = PasswordHasher.Hash(Password);
        await cmd.ExecuteNonQueryAsync();
        await tx.CommitAsync();
    }
}
