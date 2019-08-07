/*
 * 时间
 * 作者：LWY
 * 功能: 闸机接口数据获取分析  
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
namespace SupportPlatform
{
    class SkyinforGate
    {
        private string currentClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        MyJsonCtrl jscontrol = new MyJsonCtrl();
        public void Gate_Get()
        {
            while (true)
            {
                //获取个闸机信息
                //获取当前入园数量
                string strNowDay = DateTime.Now.ToString("yyyy-MM-dd");

                //闸机数据请求url
                string URL = StaticData.g_strGateUrl + strNowDay;//zzt备注，此参数后台配置
                HttpWebRequest rq = (HttpWebRequest)WebRequest.Create(URL);
                rq.Method = "GET";

                //设置请求头
                string code = "WHGY";
                TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 8, 0, 0, 0);
                string ticks = Convert.ToInt64(ts.TotalSeconds).ToString();

                string sign = "jyzy.code=" + code + "&jyzy.ticks=" + ticks + "&Md5Salt=JYZY@TICKET2017";

                string signMD5 = skyinforHttpAPI.UserMd5(sign);
                // sign =  System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(sign, "MD5");

                // 上面明明有动态数据，这里选择写死？？？ 暂时不理解。  但是不写死报授权错误
                skyinforHttpAPI.SetHeaderValue(rq.Headers, "jyzy.code", "WHGY");
                skyinforHttpAPI.SetHeaderValue(rq.Headers, "jyzy.ticks", "1529992314");
                skyinforHttpAPI.SetHeaderValue(rq.Headers, "jyzy.sign", "5c45b419839043f9b18ff819edd0a026");

                HttpWebResponse resp = (HttpWebResponse)rq.GetResponse();


                if (StaticData.g_msGateDBC.State == System.Data.ConnectionState.Closed)
                {
                    StaticData.g_msGateDBC.Open();
                }

                using (Stream stream = resp.GetResponseStream())
                {
                    int total_person_number = 0;    // 根据所有闸机的人数总和与阈值比较判断人数是否过多
                    StreamReader reader = new StreamReader(stream, Encoding.GetEncoding("utf-8"));
                    string responseString = reader.ReadToEnd();
                    //return responseString;
                    JObject jo = (JObject)JsonConvert.DeserializeObject(responseString);
                    JArray jar = JArray.Parse(jo["Data"].ToString());
                    for (var i = 0; i < jar.Count; i++)
                    {
                        
                            JObject j = JObject.Parse(jar[i].ToString());
                            string gateName = j["SpotName"].ToString(); 
                            DataRow[] dr = StaticData.g_dtGate.Select("闸机名称='" + gateName + "'");//表格的行

                        // 根据get得到的json 刷新界面
                        if (dr.Length > 0) { 
                            dr[0]["此闸机入园人数"] = Convert.ToInt32(j["InPersonCount"].ToString());
                            dr[0]["此闸机出园人数"] = Convert.ToInt32(j["OutPersonCount"].ToString());
                            dr[0]["时间"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else {
                            DataRow temp_dr = StaticData.g_dtGate.NewRow();                            
                            temp_dr["闸机名称"] = j["SpotName"].ToString();
                            temp_dr["此闸机入园人数"] = Convert.ToInt32(j["InPersonCount"].ToString());
                            temp_dr["此闸机出园人数"] = Convert.ToInt32(j["OutPersonCount"].ToString());
                            temp_dr["时间"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            // 新闸机其他参数设置为空
                            StaticData.g_dtGate.Rows.Add(dr);
                        }

                        // 获取到的数据存入到 gate_number 中进行记录
                        insertToDbGateNumber(j["SpotName"].ToString(), Convert.ToInt32(j["InPersonCount"].ToString()), Convert.ToInt32(j["OutPersonCount"].ToString()), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        //根据GET到的数据更新DB gate
                        updateDbGate(j["SpotName"].ToString(), Convert.ToInt32(j["InPersonCount"].ToString()), Convert.ToInt32(j["OutPersonCount"].ToString()),Convert.ToString(dr[0]["坐标"]));
                        // 判断人数是否过多报警
                        total_person_number += Convert.ToInt32(j["InPersonCount"].ToString());
                        judgeWarning(j["SpotName"].ToString(), total_person_number);
                    }
                }

                StaticData.g_msGateDBC.Close();            
                                 

                Thread.Sleep(StaticData.g_nSystemPollFreq * 1000);   // StaticData.g_nSystemPollFreq seconds Sleep Time
            }
        }
        /// <summary>
        /// 获取到的数据插入到DB：gate_number中进行记录
        /// </summary>
        public void insertToDbGateNumber(string name,int in_number,int out_number,string time)
        {
            string str = "INSERT INTO device_gate_number (device_name,device_in_number,device_out_number,time) VALUES ('" + name + "'," + in_number + "," + out_number + ",'" + time + "')";
            MySqlCommand cmd = new MySqlCommand(str, StaticData.g_msGateDBC);
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);
        }

        /// <summary>
        /// 根据GET到的数据更新DB gate  
        /// </summary>
        public void updateDbGate(string gateName,int in_person_num, int out_person_num,string device_position)
        {
            if (StaticData.g_msGateDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msGateDBC.Open();
            }
            string strSql = "UPDATE device_gate SET device_time='" + DateTime.Now.ToString().Replace('/', '-') + "',device_in_number='" + in_person_num + "',device_out_number='" + out_person_num + "',device_position='" + device_position + "' WHERE device_name='" + gateName + "';";
            MySqlCommand cmd = new MySqlCommand(strSql, StaticData.g_msGateDBC);
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);
        }

        /// <summary>
        /// 将异常record作为新的event record插入event_assemble & event_gate 
        /// 判断数据库是否存在该事件，且时间是否x seconds以上才再次触发，存在且是的话插入新事件
        /// x为StaticData中的全局变量
        /// </summary>
        /// <param name="gateName"> 闸机名称 </param>
        /// <param name="strLevel"> 事件等级判断 </param>
        /// 
        public string insertPersonTooMuchRecordToEventDb(string gateName,string strLevel)
        {
            if (StaticData.g_msGateDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msGateDBC.Open();
            }

            //生成eventtheon
            string strEventTheOne = Guid.NewGuid().ToString();
   
            DataRow[] dr = StaticData.g_dtGate.Select("闸机名称='" + gateName + "'");
            string theone = dr[0]["theone"].ToString();
            string strGatePosition = dr[0]["坐标"].ToString();


            string strCMD = "select *  from event_assemble where target_theone='" + theone + "' ORDER BY id DESC LIMIT 1;";
            if (StaticData.g_msEventAssembleDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msEventAssembleDBC.Open();
            }
            MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msEventAssembleDBC);
            MySqlDataReader sqlReader = null;
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            if (StaticUtils.DBReaderOperationException(ref cmd, ref sqlReader, currentClassName, currentMethodName))
            {
                if (sqlReader.Read())
                {
                    // 计算两个dateTime 的时间戳，单位s
                    long timeSpan = StaticUtils.GetCurrentTimeUnix(DateTime.Now) - StaticUtils.GetCurrentTimeUnix(Convert.ToDateTime(sqlReader["end_time"]));
                    if (timeSpan < StaticData.g_intGateWarningIntervalTime)
                    {
                        sqlReader.Close();
                        return "";  // 返回\"\"代表无需后续步骤，eg step: web端上传
                    }
                    else if (sqlReader["event_status"].ToString().Equals("已读"))
                    {
                        sqlReader.Close();
                        return "";  // 返回\"\"代表无需后续步骤，eg step: web端上传
                    }
                }

                sqlReader.Close();
            }
            else
            {
                return "";
            }

            // 将record作为新的event record进行插入 
            DataRow[] dr_abnormal_super = StaticData.g_dtAbnormalInfor.Select("故障父类名称='event_gate'");
            DataRow[] dr_abnormal_sub = StaticData.g_dtAbnormalInfor.Select("父ID='" + dr_abnormal_super[0]["ID"] + "'");
            string event_name = "";   //故障二级名称

            string strInsert_assemble = "";
            string strInsert_gate = "";
            if (strLevel.Equals("一般情况"))
            {
                // 数据库存储按照等级进行存储， 级别越低，id越低
                event_name = dr_abnormal_sub[0]["故障子类名称"].ToString();

                strInsert_assemble = "INSERT INTO event_assemble (event_name,event_theone,event_type,start_time,end_time,event_status,position,event_level,target_theone) values" +
    "('" + event_name + "','" + strEventTheOne + "','event_gate','" + DateTime.Now.ToString().Replace('/', '-') + "','" + DateTime.Now.ToString().Replace('/', '-') + "','未读','" +
    strGatePosition + "','" + strLevel + "','" + theone + "');";
                strInsert_gate = "INSERT INTO event_gate (event_theone,device_event_type,device_theone) values" +
    "('" + strEventTheOne + "','" + event_name + "','" + theone + "');";
            }
            else
            {

                event_name = dr_abnormal_sub[1]["故障子类名称"].ToString();

                strInsert_assemble = "INSERT INTO event_assemble (event_name,event_theone,event_type,start_time,end_time,event_status,position,event_level,target_theone) values" +
    "('" + event_name + "','" + strEventTheOne + "','event_gate','" + DateTime.Now.ToString().Replace('/', '-') + "','" + DateTime.Now.ToString().Replace('/', '-') + "','未读','" +
    strGatePosition + "','" + strLevel + "','" + theone + "');";
                strInsert_gate = "INSERT INTO event_gate (event_theone,device_event_type,device_theone) values" +
"('" + strEventTheOne + "','" + event_name + "','" + theone + "');";
            }
            MySqlCommand insertAssembleCmd = new MySqlCommand(strInsert_assemble, StaticData.g_msGateDBC);
            MySqlCommand insertGateCmd = new MySqlCommand(strInsert_gate, StaticData.g_msGateDBC);
           
            StaticUtils.DBNonQueryOperationException(ref insertAssembleCmd, currentClassName, currentMethodName);
            StaticUtils.DBNonQueryOperationException(ref insertGateCmd, currentClassName, currentMethodName);
            return strEventTheOne;
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
        /// 闸机游客数量过多进行插入表逻辑判断->（分为预警、告警两种情况）
        /// 判断如果发现闸机游客超过阈值，则进行插入逻辑判断
        /// </summary>
        public void judgeWarning(string gateName,int person_num)
        {
            string strEventTheOne = "";
            // 告警： strLevel-重要情况
            // 预警： strLevel-一般情况
            if (person_num >= StaticData.g_intGateWarning)// 告警
            {
                // 插入到event_assembel 中
                // 插入到event_gate 中
                strEventTheOne = insertPersonTooMuchRecordToEventDb(gateName, "重要情况");
                // 向web 上报事件
                if (!"".Equals(strEventTheOne)) { 
                sendMsgToWebSocketClients(strEventTheOne, "报警时间："+DateTime.Now.ToString() + "  事件类型：人数过多告警  人数为：" + person_num + "  闸机名称：" + gateName + "\n");
                }
            }
            else if (person_num>= StaticData.g_intGateEarlyWarning)// 预警
            {
                // 插入到event_assembel 中
                // 插入到event_gate 中
                strEventTheOne=insertPersonTooMuchRecordToEventDb(gateName, "一般情况");
                // 向web 上报事件
                if (!"".Equals(strEventTheOne))
                {
                    sendMsgToWebSocketClients(strEventTheOne, "报警时间："+DateTime.Now.ToString() + "  事件类型：人数过多预警  人数为：" + person_num + "  闸机名称：" + gateName + "\n");
                }
            }
          
        }
    }
}
