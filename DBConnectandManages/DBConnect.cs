using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BankingManagesandServicesApp.DBConnectandManages
{
    internal class DBConnect
    {
        public SqlConnection GetConnection()
        {
            string conectionstring = "Server=DESKTOP-CND6QBF;Database=QuanLyGiaoDichNganHang;Trusted_Connection=True;TrustServerCertificate=True;";
            SqlConnection connect = new SqlConnection(conectionstring);
            return connect;
        }
    }
}