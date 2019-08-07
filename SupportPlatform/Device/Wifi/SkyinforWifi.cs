/*
 * 时间：2018/07/06
 * 作者:吕万友
 * 功能: WIFI轮询
 * */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Data;
using System.Threading;
using System.Collections;
using MySql.Data.MySqlClient;
using SupportPlatform.Device.Wifi;

namespace SupportPlatform
{

    /// <summary>
    /// WiFi事件类 , class 中为什么所
    /// </summary>
    class SkyinforWifi
    {
        MyJsonCtrl jscontrol = new MyJsonCtrl();
        private string currentClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        /// <summary>
        /// 根据GET到的数据更新DB device_wifi  
        /// </summary>
        public void updateDbWifi(string serialId, string onlinestatus, string status, string person_online, string time)
        {
            // 将得到的新状态更新到数据库中去 
            if (StaticData.g_msWifiDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msWifiDBC.Open();
            }

            string strSql = "UPDATE device_wifi SET device_onlinestatus='" + onlinestatus + "',device_status='" + status + "' ,device_person_online='" + person_online + "' ,device_time='" + time + "'WHERE device_ap_sn='" + serialId + "';";
            MySqlCommand cmd = new MySqlCommand(strSql, StaticData.g_msWifiDBC);
       
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);
            StaticData.g_msWifiDBC.Close();
        }


        public void WifiList_Get()
        {
            while (true)
            {
                string msg = "";
                // out  格式类似于C 中的 pointer
                string responseString = DigestAuthClient.Request(StaticData.g_strWifiUrl, "GET", out msg);
                if ("".Equals(responseString))
                {
                    StaticUtils.ShowEventMsg("Wifi数据获取失败： " + msg+"\n");
                }
                else
                {
                    JObject jo = (JObject)JsonConvert.DeserializeObject(responseString);
                    JArray jar = JArray.Parse(jo["apBasicInfo"].ToString());
                    for (var i = 0; i < jar.Count; i++)
                    {
                        JObject j = JObject.Parse(jar[i].ToString());

                        //发现问题， 管理中心中设备的名称会不定期发生变化；故此处查询条件选用设备唯一 SN
                        string serialId = j["serialId"].ToString();
                        DataRow[] dr = StaticData.g_dtWifi.Select("SN='" + serialId + "'");//表格的行

                        // 根据get得到的json 刷新界面
                        //  onlineStatus	:对于Fit AP，0表示不在线，1表示在线。整数（Integer）类型。
                        //  status:设备状态 -1：未管理，0：未知，1：正常，2：警告，3：次要，4：重要，5：严重 。  整数（Integer）类型。
                        if (dr.Length > 0)
                        {
                            switch (Convert.ToInt32(j["onlineStatus"].ToString()))
                            {
                                case 0:
                                    dr[0]["在线状态"] = "掉线";
                                    break;
                                case 1:
                                    dr[0]["在线状态"] = "在线";
                                    break;
                                default:
                                    dr[0]["在线状态"] = "掉线";
                                    break;
                            }
                            switch (Convert.ToInt32(j["status"].ToString()))
                            {
                                case -1:
                                    dr[0]["wifi设备状态"] = "未管理";
                                    break;
                                case 0:
                                    dr[0]["wifi设备状态"] = "未知";
                                    break;
                                case 1:
                                    dr[0]["wifi设备状态"] = "正常";
                                    break;
                                case 2:
                                    dr[0]["wifi设备状态"] = "警告";
                                    break;
                                case 3:
                                    dr[0]["wifi设备状态"] = "次要";
                                    break;
                                case 4:
                                    dr[0]["wifi设备状态"] = "重要";
                                    break;
                                case 5:
                                    dr[0]["wifi设备状态"] = "严重";
                                    break;
                                default:
                                    dr[0]["wifi设备状态"] = "未管理";
                                    break;
                            }
                            dr[0]["接入人数"] = j["onlineClientCount"].ToString();
                            dr[0]["时间"] = DateTime.Now.ToString();

                            //根据GET到的数据更新DB device_wifi
                            updateDbWifi(serialId, dr[0]["在线状态"].ToString(), dr[0]["wifi设备状态"].ToString(), dr[0]["接入人数"].ToString(), dr[0]["时间"].ToString());
                            
                            // 判断wifi 设备是否故障 报警
                            judgeWifiDeviceBreakdown(dr[0]["theone"].ToString());
                        }


                    }

                }
                Thread.Sleep(20 * 1000); // 30s Sleep Time
            }
        }

        /// <summary>
        /// 向web 上报事件
        /// </summary>
        public void sendMsgToWebSocketClients(string strEventTheOne,string msg)
        {
            //发送数据Json格式
            string strSendMSG = jscontrol.EventJson(strEventTheOne);

            Mainform.form1.SendMsgToWebSocketClients(strSendMSG);
            //更新UI界面
            StaticUtils.ShowEventMsg(msg);
            //计数加一
            StaticData.g_inAlarmNum++;
        }

        /// <summary>
        /// 掉线进行插入表逻辑判断->
        /// 判断如果某个设备不进行广播，则在x分钟后进行进一步逻辑判断->
        /// 1. 如果这个设备record 已经在db_event中& status 为‘未处理’，则不做处理;
        /// 2. 如果这个设备record 已经在db_event中& status 为‘已处理’，则insert;
        /// 3. 如果这个设备record不在db_event中， 则insert.
        /// </summary>
        public void judgeWifiDeviceBreakdown(string wifiTheone)
        {               
            DataRow[] dr = StaticData.g_dtWifi.Select("theone='" + wifiTheone + "'");

            // wifi设备状态 || 在线状态 发生异常时，进行报错并插入数据库
            if (dr[0]["wifi设备状态"].Equals("警告") || dr[0]["wifi设备状态"].Equals("次要") || dr[0]["wifi设备状态"].Equals("重要") || dr[0]["wifi设备状态"].Equals("严重") || dr[0]["在线状态"].Equals("掉线") )
            {
                if (StaticData.g_msWifiDBC.State == System.Data.ConnectionState.Closed)
                {
                    StaticData.g_msWifiDBC.Open();
                }

                // 获取错误详情
                string status = "";
                if (dr[0]["wifi设备状态"].Equals("警告") || dr[0]["wifi设备状态"].Equals("次要") || dr[0]["wifi设备状态"].Equals("重要") || dr[0]["wifi设备状态"].Equals("严重"))
                {
                    status = dr[0]["wifi设备状态"].ToString();
                }
                else
                {
                    status = "";
                }
                if (dr[0]["在线状态"].Equals("掉线"))
                {
                    status += dr[0]["在线状态"];
                }

                string strCMD = "select * from event_assemble where target_theone='" + wifiTheone + "';";
                MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msWifiDBC);
                MySqlDataReader sqlReader = null;
                string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                if (StaticUtils.DBReaderOperationException(ref cmd, ref sqlReader, currentClassName, currentMethodName))
                {
                    if (sqlReader.Read())
                    {  // record 在设备表中
                        if (sqlReader["event_status"].ToString().Contains("已处理"))
                        {
                            sqlReader.Close();
                            StaticData.g_msWifiDBC.Close();
                            insertBrokenRecordToEventDb(wifiTheone, status); // 将record作为新的event record进行插入 
                        }
                        else
                        {
                            sqlReader.Close();
                            StaticData.g_msWifiDBC.Close();
                        }
                    }
                    else
                    {
                        sqlReader.Close();
                        StaticData.g_msWifiDBC.Close();
                        insertBrokenRecordToEventDb(wifiTheone, status);
                    }
                }
            }           

        }


        /// <summary>
        /// 将record作为新的event record插入event_assemble & event_wifi
        /// 关于 Wifi故障类型: (设备状态：掉线) &&  (设备状态警告、次要、重要、严重)
        /// strLevel  ： 重要情况{掉线、重要、严重}； 一般情况{警告、次要}
        /// </summary>
        public void insertBrokenRecordToEventDb(String theone,String status)
        {
            if (StaticData.g_msWifiDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msWifiDBC.Open();
            }
            //生成eventtheon
            string strEventTheOne = Guid.NewGuid().ToString();
            //事件等级判断
            string strLevel = "";
                        

            if(status.Contains("重要")|| status.Contains("严重") || status.Contains("掉线"))
            {
                strLevel = "重要情况";
            }
            else
            {
                strLevel = "一般情况";
            }

            int event_name_index = 0;
            if (status.Contains("掉线"))
            {
                event_name_index = 1;
            }

            DataRow[] dr = StaticData.g_dtWifi.Select("theone='" + theone + "'");
            string strWifiPosition = dr[0]["WIFI坐标"].ToString();
            // 将record作为新的event record进行插入 
            DataRow[] dr_abnormal_super = StaticData.g_dtAbnormalInfor.Select("故障父类名称='event_wifi'");
            DataRow[] dr_abnormal_sub = StaticData.g_dtAbnormalInfor.Select("父ID='" + dr_abnormal_super[0]["ID"] + "'");
            string event_name = dr_abnormal_sub[event_name_index]["故障子类名称"].ToString();

            string strInsert_assemble = "INSERT INTO event_assemble (event_name,event_theone,event_type,start_time,end_time,event_status,position,event_level,target_theone) values" +
                "('"+ event_name + "','" + strEventTheOne + "','event_wifi','" + DateTime.Now.ToString().Replace('/', '-') + "','" + DateTime.Now.ToString().Replace('/', '-') + "','未读','" +
                strWifiPosition + "','" + strLevel + "','" + theone + "');";

            string strInsert_wifi = "INSERT INTO event_wifi (event_theone,device_event_type,device_theone) values" +
    "('" + strEventTheOne + "','"+ event_name + "','" + theone + "');";
            MySqlCommand insertAssembleCmd = new MySqlCommand(strInsert_assemble, StaticData.g_msWifiDBC);
            MySqlCommand insertWifiCmd = new MySqlCommand(strInsert_wifi, StaticData.g_msWifiDBC);
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            StaticUtils.DBNonQueryOperationException(ref insertAssembleCmd, currentClassName, currentMethodName);            
            StaticUtils.DBNonQueryOperationException(ref insertWifiCmd, currentClassName, currentMethodName);
            //增加 form1界面上的事件信息,向web端发送故障消息
            sendMsgToWebSocketClients(strEventTheOne, "报警时间：" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "   报警内容：名称" + dr[0]["Ap名称"] + "-故障报警" + "   报警类型：" + strLevel + "报警\n");
        }
    }
}
