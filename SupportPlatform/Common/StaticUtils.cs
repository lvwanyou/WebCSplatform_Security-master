using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportPlatform
{
    /// <summary>
    /// 全局方法集合
    /// </summary>
    public static class StaticUtils
    {
        private delegate void CrossThreadOperationControl();
   

        //数据库读取异常判断
        public static bool DBReaderOperationException(ref MySqlCommand cmd, ref MySqlDataReader sqlReader, string currentClassName, string currentMethodName)
        {
            try
            {
                sqlReader = cmd.ExecuteReader();
                return true;
            }
            catch
            {
                StaticUtils.ShowEventMsg("时间：" + DateTime.Now.ToString()+"  "+  currentClassName+".class-"+ currentMethodName + " : 执行数据库查询语句失败！\n");
                if (!sqlReader.IsClosed)
                {
                    sqlReader.Close();
                }
                return false;
            }
        }

        //数据库更新插入异常判断
        public static bool DBNonQueryOperationException(ref MySqlCommand cmd, string currentClassName, string currentMethodName)
        {
            try
            {
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                StaticUtils.ShowEventMsg("时间："+DateTime.Now.ToString() + "  " + currentClassName + ".class-" + currentMethodName + " : 执行数据库nonQuery语句失败！\n");
                return false;
            }
        }

        //log信息 高亮输出
        public static void showLog(string msg)
        {
            Console.WriteLine("\n");
            Console.WriteLine("*****************************    "+msg);
            Console.WriteLine("\n");
        }

        //事件信息增加   控件：Mainform.form1.kText_EventInfo
        public static void ShowEventMsg(string msg)
        {
            CrossThreadOperationControl CrossDele = delegate ()
            {
                Mainform.form1.kText_EventInfo.AppendText(msg);
                Mainform.form1.kText_EventInfo.AppendText(Environment.NewLine);
            };
            Mainform.form1.kText_EventInfo.Invoke(CrossDele);
        }

        //命令信息 高亮输出
        public static void ShowCMDMsg(string msg)
        {
            CrossThreadOperationControl CrossDele = delegate ()
            {
                Mainform.form1.kText_Cmd.AppendText(msg);
                Mainform.form1.kText_Cmd.AppendText(Environment.NewLine);
            };
            Mainform.form1.kText_Cmd.Invoke(CrossDele);
        }

        /// <summary>
        /// 获取指定dateTime时间戳;unit : s
        /// </summary>
        /// <returns></returns>      
        public static long GetCurrentTimeUnix(DateTime date)
        {
            TimeSpan cha = (date - TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)));
            long t = (long)cha.TotalSeconds;
            return t;
        }

    }
}
