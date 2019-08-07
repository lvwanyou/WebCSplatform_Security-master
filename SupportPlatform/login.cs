using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using System.IO;
namespace SupportPlatform
{
    public partial class login : Form
    {
        public login()
        {
            InitializeComponent();
        }
        private string m_strUser;
        private string m_strPassword;
        private void button1_Click(object sender, EventArgs e)
        {
            string strMD5 = "";//记录加密后的数值
            MD5 _md5 = new MD5CryptoServiceProvider();//创建MD5对象
            byte[] data = System.Text.Encoding.Default.GetBytes(textBox_PWD.Text);//将文本框输入的数据转化为字节
            byte[] md5Data = _md5.ComputeHash(data);//计算data字节数组的哈希值（加密）
            _md5.Clear();//释放类资源
            foreach (byte a in md5Data)
            {
                if (a < 16)
                {
                    strMD5 += "0" + a.ToString("x");
                }
                else
                {
                    strMD5 += a.ToString("x");
                }
            }

            string userName = textBox_UserName.Text;
            string pwd = strMD5;
            //校验用户名密码

            if (m_strUser.Equals(userName)&& m_strPassword.Equals(pwd))
            {
                this.Hide();
                Mainform form1 = new Mainform();
                form1.Show();
            }
            else
            {
                MessageBox.Show("密码错误!");
                return;
            }

        }

        private void login_Load(object sender, EventArgs e)
        {
            string strConfigPath = AppDomain.CurrentDomain.BaseDirectory + "config.ini";
            if (!File.Exists(strConfigPath))
            {
                FileStream fs = new FileStream(strConfigPath, FileMode.OpenOrCreate);
            }
            StaticData.g_strServerIP = OperateIniFile.ReadIniData("Login", "server", null, strConfigPath);
            StaticData.g_strDBname = OperateIniFile.ReadIniData("Login", "database", null, strConfigPath);
            StaticData.g_strDBport = OperateIniFile.ReadIniData("Login", "port", null, strConfigPath);
            StaticData.g_strDBUserName = OperateIniFile.ReadIniData("Login", "uid", null, strConfigPath);
            StaticData.g_strDBUserPassword = OperateIniFile.ReadIniData("Login", "password", null, strConfigPath);


            //从自己的数据库中读取用户登录信息
            DBConnect getSystemArgvDBC = new DBConnect();
            try
            {

                if (getSystemArgvDBC.msConn.State == System.Data.ConnectionState.Closed)
                {
                    getSystemArgvDBC.msConn.Open();
                }
                string strCMD = "select *  from config_support_platform;";
                MySqlCommand cmd = new MySqlCommand(strCMD, getSystemArgvDBC.msConn);
                MySqlDataReader sqlReader = cmd.ExecuteReader();
                while (sqlReader.Read())
                {
                    //根据配置名称，将数据库中取到的数据存入全局变量。
                    switch (sqlReader["config_type"].ToString())
                    {
                        case "user":
                            m_strUser= sqlReader["username"].ToString();
                            m_strPassword = sqlReader["password"].ToString();
                            break;                     
                    }
                }
                sqlReader.Close();
                getSystemArgvDBC.msConn.Close();
                getSystemArgvDBC.msConn = null;
            }
            catch (Exception eexx)
            {
                MessageBox.Show(DateTime.Now.ToString() + "  login.class-login_Load :数据库检索失败，请检查配置文件");
            }



        }
    }
}
