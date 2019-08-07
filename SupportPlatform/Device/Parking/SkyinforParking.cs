using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Threading;
namespace SupportPlatform
{
    class SkyinforParking
    {
        private string currentClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        MyJsonCtrl jscontrol = new MyJsonCtrl();
        public void ParkingWebRequest_Get()
        {
            ParkingWebRequest parks = new ParkingWebRequest();
            while (true)
            {
                if (StaticData.g_msParkingDBC.State == System.Data.ConnectionState.Closed)
                {
                    try
                    {
                        StaticData.g_msParkingDBC.Open();
                    }
                    catch
                    {
                        StaticUtils.ShowEventMsg("时间：" + DateTime.Now.ToString() + "SkyinforParking.class- ParkingWebRequest_Get  ： 开启数据库查询失败！\n");
                        //sleep,间隔事件轮询
                        Thread.Sleep(StaticData.g_nSystemPollFreq * 1000);
                        continue;
                    }

                }

                //获取停车场数据                
                string[] num1 = parks.Post("T1");
                string[] num2 = parks.Post("T2");
                string[] num4= parks.Post("T4");
                string time = DateTime.Now.ToString();

                if(num1==null|| num2==null|| num4 == null || num1.Length==0|| num2.Length == 0 || num4.Length == 0)
                {
                    Thread.Sleep(StaticData.g_nSystemPollFreq * 1000);
                    continue;
                }

                // 更新界面UI
                DataRow[] dr1 = StaticData.g_dtParking.Select("停车场名称='" + num1[2] + "'");//表格的行
                DataRow[] dr2 = StaticData.g_dtParking.Select("停车场名称='" + num2[2] + "'");//表格的行
                DataRow[] dr4 = StaticData.g_dtParking.Select("停车场名称='" + num4[2] + "'");//表格的行
                if (dr1.Length > 0 && dr2.Length > 0 && dr4.Length > 0)
                {
                    dr1[0]["总车位数"] = num1[3];
                    dr1[0]["已使用车位"] = int.Parse(num1[3]) - int.Parse(num1[0]);
                    dr1[0]["未使用车位"] = num1[0];
                    dr1[0]["时间"] = time;

                    dr2[0]["总车位数"] = num2[3];
                    dr2[0]["已使用车位"] = int.Parse(num2[3]) - int.Parse(num2[0]);
                    dr2[0]["未使用车位"] = num2[0];
                    dr2[0]["时间"] = time;

                    dr4[0]["总车位数"] = num4[3];
                    dr4[0]["已使用车位"] = int.Parse(num4[3]) - int.Parse(num4[0]);
                    dr4[0]["未使用车位"] = num4[0];
                    dr4[0]["时间"] = time;

                    //获取到的数据插入到DB：park_number中进行记录
                    insertToDbParkNumber(num1, num2, num4, time);
                    //根据GET到的数据更新DB park
                    updateDbPark(num1[2], int.Parse(num1[3]), int.Parse(num1[0]), num2[2], int.Parse(num2[3]), int.Parse(num2[0]), num4[2], int.Parse(num4[3]), int.Parse(num4[0]));
                    // 判断车辆已使用车位是否过多报警
                    string[] park_name = new string[3] { num1[2], num2[2], num4[2] };
                    int[] park_num = new int[3] {Convert.ToInt32(dr1[0]["已使用车位"]), Convert.ToInt32(dr2[0]["已使用车位"]),Convert.ToInt32(dr4[0]["已使用车位"]) };
                    int[] park_total_num = new int[3] { Convert.ToInt32(dr1[0]["总车位数"]), Convert.ToInt32(dr2[0]["总车位数"]), Convert.ToInt32(dr4[0]["总车位数"]) };
                    judgeWarning(park_name, park_num, park_total_num);

                }                
;
                StaticData.g_msParkingDBC.Close();
                //sleep,间隔事件轮询
                Thread.Sleep(StaticData.g_nSystemPollFreq * 1000);
            }

        }


        /// <summary>
        /// 获取到的数据插入到DB：park_number中进行记录
        /// </summary>
        public void insertToDbParkNumber(string[] record1, string[] record2, string[] record4, string time)
        {

            //写入数据库，并更新,更新总车位
            string str = "INSERT INTO device_park_number (device_name, device_rest_park, time) VALUES('" + record1[2] + "', " + record1[0] + ", '" + time + "'),('" + record2[2] + "'," + record2[0] + ", '" + time + "'),('" + record4[2] + "'," + record4[0] + ", '" + time + "')";
            MySqlCommand cmd = new MySqlCommand(str, StaticData.g_msParkingDBC);
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);
        }

        /// <summary>
        /// 根据GET到的数据更新DB park  
        /// </summary>
        public void updateDbPark(string parkName1, int all_park_num1,int rest_park_num1, string parkName2, int all_park_num2, int rest_park_num2, string parkName4, int all_park_num4, int rest_park_num4)
        {
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            if (StaticData.g_msParkingDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msParkingDBC.Open();
            }
            // update T1 park
            string strSql1 = "UPDATE device_park SET device_time='" + DateTime.Now.ToString().Replace('/', '-') + "',device_all_park='" + all_park_num1 + "',device_rest_park='" + rest_park_num1 + "' WHERE device_name='" + parkName1 + "';";
            MySqlCommand cmd = new MySqlCommand(strSql1, StaticData.g_msParkingDBC);
            StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);

            // update T2 park
            string strSql2 = "UPDATE device_park SET device_time='" + DateTime.Now.ToString().Replace('/', '-') + "',device_all_park='" + all_park_num2 + "',device_rest_park='" + rest_park_num2 + "' WHERE device_name='" + parkName2 + "';";
            cmd = new MySqlCommand(strSql2, StaticData.g_msParkingDBC);
            StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);

            // update T4 park
            string strSql4 = "UPDATE device_park SET device_time='" + DateTime.Now.ToString().Replace('/', '-') + "',device_all_park='" + all_park_num4 + "',device_rest_park='" + rest_park_num4 + "' WHERE device_name='" + parkName4 + "';";
            cmd = new MySqlCommand(strSql4, StaticData.g_msParkingDBC);
            StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);
        }

        /// <summary>
        /// 将异常record作为新的event record插入event_assemble & event_park
        /// 判断数据库是否存在该事件，且时间是否x seconds以上才再次触发，存在且是的话插入新事件
        /// x为StaticData中的全局变量
        /// </summary>
        /// <param name="parkName"> 闸机名称 </param>
        /// <param name="strLevel"> 事件等级判断 </param>
        public string insertRecordToEventDb(string parkName, string strLevel)
        {
            if (StaticData.g_msParkingDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msParkingDBC.Open();
            }

            //生成event theone
            string strEventTheOne = Guid.NewGuid().ToString();

            DataRow[] dr = StaticData.g_dtParking.Select("停车场名称='" + parkName + "'");
            string theone = dr[0]["theone"].ToString();
            string strParkPosition = dr[0]["坐标"].ToString();

            string strCMD = "select *  from event_assemble where target_theone='" + theone + "' ORDER BY id DESC LIMIT 1;";
            if (StaticData.g_msEventAssembleDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msEventAssembleDBC.Open();
            }
            MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msEventAssembleDBC);
            MySqlDataReader sqlReader = null;
            try
            {
                sqlReader = cmd.ExecuteReader();
            }
            catch
            {
                StaticUtils.ShowEventMsg("SkyinforParking.class-insertRecordToEventDb : 执行数据库查询语句失败！\n");
                return "";
            }
            if (sqlReader.Read())
            {
                // 计算两个dateTime 的时间戳，单位s
                long timeSpan = StaticUtils.GetCurrentTimeUnix(DateTime.Now) - StaticUtils.GetCurrentTimeUnix(Convert.ToDateTime(sqlReader["end_time"]));
                if (timeSpan < StaticData.g_intParkWarningIntervalTime)
                {
                    sqlReader.Close();
                    // 返回\"\"代表无需后续步骤，eg step: web端上传
                    return "";
                }else if (sqlReader["event_status"].ToString().Equals("已读"))
                {
                    sqlReader.Close();
                    // 返回\"\"代表无需后续步骤，eg step: web端上传
                    return "";
                }
            }

            sqlReader.Close();
            // 将record作为新的event record进行插入 
            DataRow[] dr_abnormal_super = StaticData.g_dtAbnormalInfor.Select("故障父类名称='event_park'");
            DataRow[] dr_abnormal_sub = StaticData.g_dtAbnormalInfor.Select("父ID='" + dr_abnormal_super[0]["ID"] + "'");
            string event_name = ""; //故障二级名称

            string strInsert_assemble = "";
            string strInsert_park = "";
            if (strLevel.Equals("一般情况"))
            {

                // 数据库存储按照等级进行存储， 级别越低，id越低
                event_name = dr_abnormal_sub[0]["故障子类名称"].ToString();

                strInsert_assemble = "INSERT INTO event_assemble (event_name,event_theone,event_type,start_time,end_time,event_status,position,event_level,target_theone) values" +
    "('" + event_name + "','" + strEventTheOne + "','event_park','" + DateTime.Now.ToString().Replace('/', '-') + "','" + DateTime.Now.ToString().Replace('/', '-') + "','未读','" +
    strParkPosition + "','" + strLevel + "','" + theone + "');";
                strInsert_park = "INSERT INTO event_park (event_theone,device_event_type,device_theone) values" +
    "('" + strEventTheOne + "','" + event_name + "','" + theone + "');";
            }
            else
            {
                // 数据库存储按照等级进行存储， 级别越低，id越低
                event_name = dr_abnormal_sub[1]["故障子类名称"].ToString();

                strInsert_assemble = "INSERT INTO event_assemble (event_name,event_theone,event_type,start_time,end_time,event_status,position,event_level,target_theone) values" +
    "('" + event_name + "','" + strEventTheOne + "','event_park','" + DateTime.Now.ToString().Replace('/', '-') + "','" + DateTime.Now.ToString().Replace('/', '-') + "','未读','" +
    strParkPosition + "','" + strLevel + "','" + theone + "');";
                strInsert_park = "INSERT INTO event_park (event_theone,device_event_type,device_theone) values" +
"('" + strEventTheOne + "','" + event_name + "','" + theone + "');";
            }
            MySqlCommand insertAssembleCmd = new MySqlCommand(strInsert_assemble, StaticData.g_msParkingDBC);
            MySqlCommand insertParkCmd = new MySqlCommand(strInsert_park, StaticData.g_msParkingDBC);
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            StaticUtils.DBNonQueryOperationException(ref insertAssembleCmd, currentClassName, currentMethodName);
            StaticUtils.DBNonQueryOperationException(ref insertParkCmd, currentClassName, currentMethodName);
            return strEventTheOne;
        }

        /// <summary>
        /// 向web 上报事件
        /// </summary>
        public void sendMsgToWebSocketClients(string strEventTheOne, string msg)
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
        /// 停车场车位使用过多进行插入表逻辑判断->（分为预警、告警两种情况）
        /// 判断如果发现车位使用超过阈值，则进行进一步逻辑判断->
        /// 判断数据库是否存在该事件，且时间是否  x seconds以上才再次触发，存在且是的话插入新事件
        /// </summary>
        /// <param name="park_name">已使用停车位名称集合：park_name[0],park_name[1] ;park_name[0]: T1 park名称</param>
        /// <param name="park_num">已使用停车位数量集合：park_num[0],park_num[1] ;park_num[0]: T1 park中已使用车位数量</param>
        /// <param name="park_total_num">停车位总数量集合：park_total_num[0],park_total_num[1] ;park_total_num[0]: T1 park中总车位数量</param>
        public void judgeWarning(string[] park_name,int[] park_num,int[] park_total_num)
        {
            string strEventTheOne = "";
            // 告警： strLevel-重要情况
            // 预警： strLevel-一般情况
            for(int i=0;i<park_num.Length;i++)
            {
                //采用百分比告警
                if (park_num[i] >= Convert.ToInt32(Math.Round(StaticData.g_intParkEarlyWarning * park_total_num[i])))// 告警
                {
                    // 插入到event_assembel 中
                    // 插入到event_park 中
                    strEventTheOne = insertRecordToEventDb(park_name[i], "重要情况");
                    // 向web 上报事件
                    if (!"".Equals(strEventTheOne))
                    {
                        sendMsgToWebSocketClients(strEventTheOne, "报警时间：" + DateTime.Now.ToString() + "  事件类型：车位使用过多告警  已使用车位数为：" + park_num[i] + "  停车场名称：" + park_name[i] + "\n");
                    }
                }
                else if (park_num[i] >= Convert.ToInt32(Math.Round(StaticData.g_intParkEarlyWarning* park_total_num[i])))// 预警
                {
                    // 插入到event_assembel 中
                    // 插入到event_park 中
                    strEventTheOne = insertRecordToEventDb(park_name[i], "一般情况");
                    // 向web 上报事件
                    if (!"".Equals(strEventTheOne))
                    {
                        sendMsgToWebSocketClients(strEventTheOne, "报警时间：" + DateTime.Now.ToString() + "  事件类型：车位使用过多预警  人数为：" + park_num[i] + "  停车场名称：" + park_name[i] + "\n");
                    }
                }
            }
        }

    }
}
