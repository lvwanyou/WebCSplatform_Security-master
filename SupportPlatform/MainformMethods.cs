using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
namespace SupportPlatform
{
    partial class Mainform
    {
        #region        轮询获取 DB 中App端上报的event
        private DateTime last_PollForAppEvent_time = DateTime.Now;
        public string currentClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        /// <summary>
        /// 轮询获取 DB 中App端上报的event
        /// </summary>
        public void PollForAppEvent()
        {
            /// Design:服务器维护一个关于Event_assemble的DataTable 
            /// 每次轮询更新DataTable ，find a new record and then send a message to clients by websocket;
            /// Point : if there are many new records found, consist them and the split is signal ','.
            InitEventTable();
            MyJsonCtrl jscontrol = new MyJsonCtrl();
            while (true)
            {  
                refreshEventTable();  
                string theones = "";
                List<string> warning_msg = new List<string>();
                //StaticUtils.ShowEventMsg(Convert.ToString(last_PollForAppEvent_time)  +"    "+ StaticData.g_dtEventAssemble.Rows.Count+"\n");
                if (StaticData.g_dtEventAssemble.Rows.Count > 0)
                {
                    for (int i = 0; i < StaticData.g_dtEventAssemble.Rows.Count; i++)
                    {
                        if (i == StaticData.g_dtEventAssemble.Rows.Count - 1)
                        {
                            theones += StaticData.g_dtEventAssemble.Rows[i]["事件theone"];
                        }
                        else
                        {
                            theones += StaticData.g_dtEventAssemble.Rows[i]["事件theone"] + ",";
                        }
                        warning_msg.Add("报警时间：" + StaticData.g_dtEventAssemble.Rows[i]["时间"] + "  事件类型：App故障事件上报  事件名称：" + StaticData.g_dtEventAssemble.Rows[i]["事件名称"] + "\n");
                    }
                    if (!"".Equals(theones))
                    {
                        //发送数据Json格式
                        string strSendMSG = jscontrol.EventJson(theones);
                        Mainform.form1.SendMsgToWebSocketClients(strSendMSG);
                        foreach (var item in warning_msg)
                        {
                            //更新UI界面
                            StaticUtils.ShowEventMsg(item);
                            //计数加一
                            StaticData.g_inAlarmNum++;
                        }
                    }
                }
                Thread.Sleep(5 * 1000);    //   APP轮询
            }
        }

        /// <summary>
        /// 初始化EventAssemble DataTable
        /// </summary>
        public void InitEventTable()
        {
            #region
            StaticData.g_dtEventAssemble.Columns.Add("事件名称", System.Type.GetType("System.String"));
            StaticData.g_dtEventAssemble.Columns.Add("事件theone", System.Type.GetType("System.String"));
            StaticData.g_dtEventAssemble.Columns.Add("事件类型", System.Type.GetType("System.String"));
            StaticData.g_dtEventAssemble.Columns.Add("事件状态", System.Type.GetType("System.String"));
            StaticData.g_dtEventAssemble.Columns.Add("时间", System.Type.GetType("System.String"));
            #endregion
        }

        /// <summary>
        /// 读取数据库刷新 EventAssemble DataTable
        /// </summary>
        public void refreshEventTable()
        {
            if (StaticData.g_msEventAssembleDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msEventAssembleDBC.Open();
            }
            else
            {
                return;
            }
            // 设置降序
            string strCMD = "select * from event_assemble where event_type='event_app' and event_status='未读' and start_time> '" + Convert.ToString(last_PollForAppEvent_time) + "'ORDER BY id DESC;";
            MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msEventAssembleDBC);
            MySqlDataReader sqlReader = null;
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            if (StaticUtils.DBReaderOperationException(ref cmd, ref sqlReader, currentClassName, currentMethodName))
            {
                StaticData.g_dtEventAssemble.Clear();
                while (sqlReader.Read())
                {
                    DataRow dr = StaticData.g_dtEventAssemble.NewRow();//表格的行
                    dr["事件名称"] = sqlReader["event_name"].ToString();
                    dr["事件theone"] = sqlReader["event_theone"].ToString();
                    dr["事件类型"] = sqlReader["event_type"].ToString();
                    dr["事件状态"] = sqlReader["event_status"].ToString();
                    dr["时间"] = sqlReader["end_time"].ToString();
                    StaticData.g_dtEventAssemble.Rows.Add(dr);//添加                   
                }
                if (StaticData.g_dtEventAssemble.Rows.Count > 0)
                {
                    last_PollForAppEvent_time = Convert.ToDateTime(StaticData.g_dtEventAssemble.Rows[0]["时间"]);
                }

                sqlReader.Close();
            }
            StaticData.g_msEventAssembleDBC.Close();
        }
        #endregion
    }
}
