using System.IO;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;
namespace BankingManagesandServicesApp.DBManages;
public static class DBConnectandManage
{
    public static string ConnectionString => XDocument.Load(Path.Combine(AppContext.BaseDirectory, "database.config"))
        .Root?.Attribute("connectionString")?.Value ?? throw new InvalidOperationException("Thiếu connectionString trong database.config.");
    public static async Task<SqlConnection> OpenAsync()
    {
        var connection = new SqlConnection(ConnectionString);
        try { await connection.OpenAsync(); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
}
