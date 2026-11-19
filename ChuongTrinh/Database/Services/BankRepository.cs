using System.Data;
using BankingManagesandServicesApp.DBManages;
using BankingManagesandServicesApp.Models;
using Microsoft.Data.SqlClient;
namespace BankingManagesandServicesApp.Services;

public sealed class BankRepository
{
    public async Task<Account?> AuthenticateAsync(string username, string password)
    {
        await using var db = await DBConnectandManage.OpenAsync();
        await using var cmd = new SqlCommand("""
            SELECT a.accountId,a.username,a.fullName,a.password,a.status,COALESCE(r.roleName,N'Chưa phân vai trò'),
                CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.RolePermission rp JOIN dbo.Permission p
                    ON p.permissionId=rp.permissionId WHERE rp.roleId=a.roleId AND p.permissionName=N'ExportReceipt')
                    THEN 1 ELSE 0 END AS bit)
            FROM dbo.Account a LEFT JOIN dbo.Role r ON r.roleId=a.roleId WHERE a.username=@username
            """, db);
        cmd.Parameters.Add("@username", SqlDbType.NVarChar, 50).Value = username;
        Account account;
        await using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync()) return null;
            string status = reader.IsDBNull(4) ? "" : reader.GetString(4);
            if (status != "Active" && status != "Hoạt động") return null;
            if (reader.IsDBNull(3) || !PasswordHasher.Verify(password, reader.GetString(3))) return null;
            account = new Account(reader.GetInt32(0), reader.GetString(1), reader.IsDBNull(2) ? reader.GetString(1) : reader.GetString(2),
                reader.GetString(5), reader.GetBoolean(6));
            if (await reader.ReadAsync()) return null;
        }
        await WriteActivityAsync(account, "Đăng nhập", "Thành công", "Đăng nhập bằng tài khoản ứng dụng.");
        return account;
    }

    public async Task<List<TransactionBank>> SearchTransactionsAsync(string keyword, string status, DateTime? from, DateTime? to)
    {
        await using var db = await DBConnectandManage.OpenAsync();
        await using var cmd = new SqlCommand("""
            SELECT t.idtrs,COALESCE(t.idacc,''),COALESCE(a.fullname,''),COALESCE(t.amount,0),
                COALESCE(t.transactionType,''),COALESCE(t.status,''),t.transactionTime,
                COALESCE(t.description,''),ISNULL(t.isFlagged,CAST(0 AS bit)),COALESCE(t.flagReason,'')
            FROM dbo.TransactionBank t LEFT JOIN dbo.AccountBank a ON a.idacc=t.idacc
            WHERE (@keyword=N'' OR CHARINDEX(@keyword,t.idtrs)>0 OR CHARINDEX(@keyword,t.idacc)>0
                OR CHARINDEX(@keyword,a.fullname)>0)
                AND (@status=N'' OR t.status=@status)
                AND (@from IS NULL OR t.transactionTime>=@from) AND (@to IS NULL OR t.transactionTime<@to)
            ORDER BY t.transactionTime DESC,t.idtrs
            """, db);
        cmd.Parameters.Add("@keyword", SqlDbType.NVarChar, 100).Value = keyword;
        cmd.Parameters.Add("@status", SqlDbType.NVarChar, 50).Value = status;
        cmd.Parameters.Add("@from", SqlDbType.DateTime).Value = (object?)from?.Date ?? DBNull.Value;
        cmd.Parameters.Add("@to", SqlDbType.DateTime).Value = (object?)to?.Date.AddDays(1) ?? DBNull.Value;
        var items = new List<TransactionBank>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) items.Add(new TransactionBank(reader.GetString(0), reader.GetString(1), reader.GetString(2),
            reader.GetDecimal(3), reader.GetString(4), reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            reader.GetString(7), reader.GetBoolean(8), reader.GetString(9)));
        return items;
    }

    public async Task<List<TransactionLog>> GetLogsAsync(string transactionId)
    {
        await using var db = await DBConnectandManage.OpenAsync();
        await using var cmd = new SqlCommand("""
            SELECT logId,timestamp,COALESCE(action,''),COALESCE(result,''),COALESCE(details,'')
            FROM dbo.TransactionLog WHERE idtrs=@id ORDER BY timestamp DESC,logId DESC
            """, db);
        cmd.Parameters.Add("@id", SqlDbType.NVarChar, 50).Value = transactionId;
        var logs = new List<TransactionLog>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) logs.Add(new TransactionLog(reader.GetInt64(0), reader.IsDBNull(1) ? null : reader.GetDateTime(1),
            reader.GetString(2), reader.GetString(3), reader.GetString(4)));
        return logs;
    }

    public async Task WriteActivityAsync(Account account, string action, string result, string details)
    {
        await using var db = await DBConnectandManage.OpenAsync();
        await using var tx = (SqlTransaction)await db.BeginTransactionAsync(IsolationLevel.Serializable);
        await InsertActivityAsync(db, tx, account, action, result, details);
        await tx.CommitAsync();
    }

    private static async Task InsertActivityAsync(SqlConnection db, SqlTransaction tx, Account account, string action, string result, string details)
    {
        await using var cmd = new SqlCommand("""
            INSERT dbo.ActivityLog(logId,accountId,action,timestamp,result,details)
            SELECT COALESCE(MAX(logId),0)+1,@account,@action,GETDATE(),@result,@details
            FROM dbo.ActivityLog WITH (UPDLOCK,HOLDLOCK)
            """, db, tx);
        cmd.Parameters.Add("@account", SqlDbType.Int).Value = account.AccountId;
        cmd.Parameters.Add("@action", SqlDbType.NVarChar, 100).Value = action;
        cmd.Parameters.Add("@result", SqlDbType.NVarChar, 50).Value = result;
        cmd.Parameters.Add("@details", SqlDbType.NVarChar, -1).Value = details;
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task EnsureExportAllowedAsync(Account account, string id)
    {
        await using var db = await DBConnectandManage.OpenAsync();
        await using var cmd = new SqlCommand("""
            SELECT COUNT(*) FROM dbo.Account a
            WHERE a.accountId=@account AND a.status IN (N'Active',N'Hoạt động')
                AND EXISTS (SELECT 1 FROM dbo.RolePermission rp JOIN dbo.Permission p ON p.permissionId=rp.permissionId
                    WHERE rp.roleId=a.roleId AND p.permissionName=N'ExportReceipt')
                AND EXISTS (SELECT 1 FROM dbo.TransactionBank WHERE idtrs=@id AND status IN (N'Thành công',N'Success',N'Completed'))
            """, db);
        cmd.Parameters.Add("@account", SqlDbType.Int).Value = account.AccountId;
        cmd.Parameters.Add("@id", SqlDbType.NVarChar, 50).Value = id;
        if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) != 1)
            throw new InvalidOperationException("Không có quyền xuất hoặc giao dịch chưa thành công. Hãy tải lại dữ liệu.");
    }

    public async Task LogExportAsync(Receipt receipt, string format, string fileName)
    {
        await using var db = await DBConnectandManage.OpenAsync();
        await using var tx = (SqlTransaction)await db.BeginTransactionAsync(IsolationLevel.Serializable);
        string details = $"Chứng từ {receipt.Number}; định dạng {format}; người xuất {receipt.Issuer.DisplayName}; tệp {fileName}";
        await using var cmd = new SqlCommand("""
            INSERT dbo.TransactionLog(logId,idtrs,action,timestamp,result,details)
            SELECT COALESCE(MAX(logId),0)+1,@id,N'Xuất chứng từ',GETDATE(),N'Thành công',@details
            FROM dbo.TransactionLog WITH (UPDLOCK,HOLDLOCK)
            """, db, tx);
        cmd.Parameters.Add("@id", SqlDbType.NVarChar, 50).Value = receipt.Transaction.Id;
        cmd.Parameters.Add("@details", SqlDbType.NVarChar, -1).Value = details;
        await cmd.ExecuteNonQueryAsync();
        await InsertActivityAsync(db, tx, receipt.Issuer, "Xuất chứng từ", "Thành công", details);
        await tx.CommitAsync();
    }
}
