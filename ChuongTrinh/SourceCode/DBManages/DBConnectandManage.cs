using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Data.SqlClient;

namespace BankingManagesandServicesApp.DBManages
{
    /// <summary>
    /// Lớp kết nối và thực thi câu lệnh với SQL Server (database QuanLyGiaoDichNganHang).
    /// Hỗ trợ cả cấu hình từ database.config (OpenAsync) và tự dò SQL Server cho các lớp thực thể.
    /// </summary>
    public static class DBConnectandManage
    {
        public const string DatabaseName = "QuanLyGiaoDichNganHang";

        /// <summary>
        /// Tên SQL Server tùy chỉnh. Để null thì ưu tiên đọc database.config rồi tự dò các server thông dụng.
        /// </summary>
        public static string? ServerName = null;
        public static string? ConnectedServer { get; private set; }

        private static readonly string[] CandidateServers =
        {
            @".\SQLEXPRESS", ".", "localhost", @"(localdb)\MSSQLLocalDB"
        };

        private static string? connectionString;

        public static void SetServer(string? server)
        {
            ServerName = string.IsNullOrWhiteSpace(server) ? null : server.Trim();
            connectionString = null;
            ConnectedServer = null;
        }

        public static string BuildConnectionString(string server)
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = DatabaseName,
                IntegratedSecurity = true,
                TrustServerCertificate = true,
                ConnectTimeout = 3
            };
            return builder.ConnectionString;
        }

        private static string? ReadConfigConnectionString()
        {
            try
            {
                string cfgPath = Path.Combine(AppContext.BaseDirectory, "database.config");
                if (!File.Exists(cfgPath)) return null;
                return XDocument.Load(cfgPath).Root?.Attribute("connectionString")?.Value;
            }
            catch
            {
                return null;
            }
        }

        public static string ConnectionString
        {
            get
            {
                if (connectionString == null)
                    connectionString = FindConnectionString();
                return connectionString;
            }
        }

        private static string FindConnectionString()
        {
            Exception? lastError = null;

            if (ServerName == null)
            {
                string? cfg = ReadConfigConnectionString();
                if (!string.IsNullOrWhiteSpace(cfg))
                {
                    try
                    {
                        using var conn = new SqlConnection(cfg);
                        conn.Open();
                        ConnectedServer = new SqlConnectionStringBuilder(cfg).DataSource;
                        return cfg;
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                    }
                }
            }

            string[] servers = ServerName != null ? new[] { ServerName } : CandidateServers;
            foreach (string server in servers)
            {
                string cs = BuildConnectionString(server);
                try
                {
                    using var conn = new SqlConnection(cs);
                    conn.Open();
                    ConnectedServer = server;
                    return cs;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }

            throw new InvalidOperationException(
                "Không kết nối được SQL Server tới database " + DatabaseName +
                ". Hãy chạy SQLQuery3.sql và SQL_ChuyenKhoan_TinDung.sql, kiểm tra database.config hoặc sửa DBConnectandManage.ServerName.",
                lastError);
        }

        /// <summary>Mở kết nối bất đồng bộ (dùng cho BankRepository, DemoData, AccountBankingManageOverviewGUI).</summary>
        public static async Task<SqlConnection> OpenAsync()
        {
            var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        /// <summary>Kiểm tra kết nối, trả về thông báo lỗi (null nếu kết nối được).</summary>
        public static string? TestConnection()
        {
            try
            {
                using var conn = OpenConnection();
                return null;
            }
            catch (Exception ex)
            {
                connectionString = null;
                return ex.Message;
            }
        }

        public static SqlConnection OpenConnection()
        {
            var conn = new SqlConnection(ConnectionString);
            conn.Open();
            return conn;
        }

        public static SqlParameter P(string name, object? value)
        {
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        public static SqlCommand CreateCommand(SqlConnection conn, SqlTransaction? tran, string sql, params SqlParameter[] parameters)
        {
            var cmd = new SqlCommand(sql, conn, tran);
            cmd.Parameters.AddRange(parameters);
            return cmd;
        }

        public static DataTable ExecuteQuery(string sql, params SqlParameter[] parameters)
        {
            using var conn = OpenConnection();
            using var cmd = CreateCommand(conn, null, sql, parameters);
            using var adapter = new SqlDataAdapter(cmd);
            var table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        public static int ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using var conn = OpenConnection();
            using var cmd = CreateCommand(conn, null, sql, parameters);
            return cmd.ExecuteNonQuery();
        }

        public static object? ExecuteScalar(string sql, params SqlParameter[] parameters)
        {
            using var conn = OpenConnection();
            using var cmd = CreateCommand(conn, null, sql, parameters);
            object? result = cmd.ExecuteScalar();
            return result == DBNull.Value ? null : result;
        }

        /// <summary>
        /// Thực hiện nhiều câu lệnh trong cùng một transaction: lỗi ở bất kỳ bước nào sẽ rollback toàn bộ.
        /// </summary>
        public static void ExecuteInTransaction(Action<SqlConnection, SqlTransaction> work)
        {
            using var conn = OpenConnection();
            using var tran = conn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                work(conn, tran);
                tran.Commit();
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        // ---------- Hàm đọc dữ liệu an toàn từ DataRow ----------
        public static string Str(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToString(row[col]) ?? "" : "";
        }

        public static int Int(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToInt32(row[col]) : 0;
        }

        public static double Dbl(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToDouble(row[col]) : 0;
        }

        public static decimal Dec(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToDecimal(row[col]) : 0m;
        }

        public static bool Bool(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) && row[col] != DBNull.Value && Convert.ToBoolean(row[col]);
        }

        public static DateTime? Date(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) && row[col] != DBNull.Value ? Convert.ToDateTime(row[col]) : null;
        }
    }
}
