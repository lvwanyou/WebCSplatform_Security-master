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
    class MachineRoomRemoteDBConnect
    {
        private string m_strServer;
        private string m_strDatabase;
        private string m_strPort;
        private string m_strUid;
        private string m_strPassword;
        public MySqlConnectionStringBuilder m_msBuilder;
        public MySqlConnection msConn;
       
        public MachineRoomRemoteDBConnect()
        {
            m_msBuilder = new MySqlConnectionStringBuilder();
            m_msBuilder.Server = StaticData.g_strMachineRommIP;
            m_msBuilder.Port = uint.Parse(StaticData.g_strMachineRoomPort);
            m_msBuilder.Database = StaticData.g_strMachineRommDBname;
            m_msBuilder.UserID = StaticData.g_strMachineRoomUser;
            m_msBuilder.Password = StaticData.g_strMachineRoomPassword;
            m_msBuilder.CharacterSet = "utf8";
            m_msBuilder.SslMode = MySqlSslMode.None;
            msConn = new MySqlConnection(m_msBuilder.ConnectionString);
        }
    }
}
