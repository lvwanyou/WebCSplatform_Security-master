using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using System.IO;
using System.Runtime.InteropServices;

namespace SupportPlatform
{
    class DBConnect
    {
        private string m_strServer;
        private string m_strDatabase;
        private string m_strPort;
        private string m_strUid;
        private string m_strPassword;
        public MySqlConnectionStringBuilder m_msBuilder;
        public MySqlConnection msConn;
      //  private MySqlConnectionStringBuilder s = new MySqlConnectionStringBuilder();
       
        public  DBConnect()
        {
            m_msBuilder = new MySqlConnectionStringBuilder();
            m_msBuilder.Server = StaticData.g_strServerIP;
            m_msBuilder.Port = uint.Parse(StaticData.g_strDBport);
            m_msBuilder.Database = StaticData.g_strDBname;
            m_msBuilder.UserID = StaticData.g_strDBUserName;
            m_msBuilder.Password = StaticData.g_strDBUserPassword;
            m_msBuilder.CharacterSet = "utf8";
            m_msBuilder.SslMode = MySqlSslMode.None;
            msConn = new MySqlConnection(m_msBuilder.ConnectionString);
        }
    }
}
