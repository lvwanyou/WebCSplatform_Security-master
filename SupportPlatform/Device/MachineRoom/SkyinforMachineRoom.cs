using MySql.Data.MySqlClient;
using SupportPlatform.Device.MachineRoom;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SupportPlatform.Device.ComputerRoom
{
    class SkyinforMachineRoom
    {
        MyJsonCtrl jscontrol = new MyJsonCtrl();
        private string currentClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        /// <summary>
        /// 轮询刷新机房相关信息
        /// </summary>
        public void MachineRoom_Get()
        {
            while (true)
            {
                // 获取远端数据库中的机房的相关信息：远端数据是个实时更新数据库，基本只出来两天实时的数据。
                List<MachineRoomBean> machineRoon_infor_list = GetRemoteRoomSqlEvent();
                if (machineRoon_infor_list.Count == 0)
                {
                    //跳出                             
                    goto here;
                }
                insertToDbDeviceComputerRoomData(machineRoon_infor_list);   //将获取到的新数据插入到本地数据库
                updateFormUI(machineRoon_infor_list);    // 更新DataGridView 

                // 获取远端数据库中的机房报错异常信息
                List<string[]> machineRoomInfors = GetRemoteRoomWarningSqlEvent();
                // 判断远端数据是否异常并是否需写入到Local数据库中
                if (machineRoomInfors.Count == 0)
                {
                    //跳出                             
                    goto here;
                }
                judgeWarningToEventDD(machineRoomInfors);
            //StaticData.g_msMachineRoomDBC.Close();
            //StaticData.g_msMachineRoomRemoteDBC.Close();
            here:
                Thread.Sleep(StaticData.g_nSystemPollFreq * 1000);   // StaticData.g_nSystemPollFreq seconds Sleep Time
            }
        }

        /// <summary>
        /// 获取远端数据中的机房的相关信息；远端数据是个实时更新数据库，基本只出来两天实时的数据。
        /// </summary>
        public List<MachineRoomBean> GetRemoteRoomSqlEvent()
        {
            List<MachineRoomBean> machineRoon_infor_list = new List<MachineRoomBean>();

            if (StaticData.g_msMachineRoomRemoteDBC.State == System.Data.ConnectionState.Closed)
            {
                try
                {
                    StaticData.g_msMachineRoomRemoteDBC.Open();
                }
                catch
                {
                    StaticUtils.ShowEventMsg("SkyinforMachineRoom.class-GetRemoteRoomSqlEvent :打开远端数据库失败！\n");
                    return machineRoon_infor_list;
                }

            }
            // 获取所有机房的itemgroup_key
            string strMachinekeySql = "SELECT itemgroup_name,itemgroup_key FROM itemgroup";
            MySqlCommand machine_list_command = new MySqlCommand(strMachinekeySql, StaticData.g_msMachineRoomRemoteDBC);
            MySqlDataReader machine_list_sqlDR = null;
            try
            {
                machine_list_sqlDR = machine_list_command.ExecuteReader();
            }
            catch
            {
                StaticUtils.ShowEventMsg("SkyinforMachineRoom.class-GetRemoteRoomSqlEvent : 执行数据库查询语句失败！\n");
                return machineRoon_infor_list;
            }

            List<int> machine_key_list = new List<int>();
            List<string> machine_name_list = new List<string>();
            while (machine_list_sqlDR.Read())
            {
                machine_name_list.Add(machine_list_sqlDR[0].ToString());
                machine_key_list.Add(Convert.ToInt32(machine_list_sqlDR[1]));
            }
            machine_list_sqlDR.Close();

            // 循环远程数据库中的机房相关更新信息
            for (int i = 0; i < machine_key_list.Count; i++)
            {
                // 获取机房名称
                MachineRoomBean machineRoomBean = new MachineRoomBean();
                machineRoomBean.StrMachineRoomName = machine_name_list[i];

                //获取空调温湿度
                string strAir = "SELECT temp,humidity FROM airbox_data_rt WHERE itemgroup_key = " + machine_key_list[i];
                MySqlCommand command = new MySqlCommand(strAir, StaticData.g_msMachineRoomRemoteDBC);
                MySqlDataReader sqlDR = null;

                try
                {
                    sqlDR = command.ExecuteReader();
                }
                catch
                {
                    StaticUtils.ShowEventMsg("SkyinforMachineRoom.class-GetRemoteRoomSqlEvent : 执行数据库查询语句失败！\n");
                    return machineRoon_infor_list;
                }

                if (sqlDR.Read())
                {
                    machineRoomBean.StrAirsql = new string[] { sqlDR["temp"] + "", sqlDR["humidity"] + "" };
                }
                else
                {
                    machineRoomBean.StrAirsql = new string[] { "", "" };
                };                
                sqlDR.Close();

                //获取UDP数据
                string strUPS = "select ups_type,power_supply_mode ,working_mode,power_status,battery_status,output_status,battery_voltage,surplus_capacity FROM upsbox_data_rt  WHERE  itemgroup_key=" + machine_key_list[i];
                MySqlCommand ups_command = new MySqlCommand(strUPS, StaticData.g_msMachineRoomRemoteDBC);
                MySqlDataReader ups_sqlDR = null;

                try
                {
                    ups_sqlDR = ups_command.ExecuteReader();
                }
                catch
                {
                    StaticUtils.ShowEventMsg("SkyinforMachineRoom.class-GetRemoteRoomSqlEvent : 执行数据库查询语句失败！\n");
                    return machineRoon_infor_list;
                }


                if (ups_sqlDR.Read())
                {
                    machineRoomBean.StrUPSsql = new string[] { ups_sqlDR["ups_type"] + "", ups_sqlDR["power_supply_mode"] + "", ups_sqlDR["working_mode"] + "", ups_sqlDR["power_status"] + "", ups_sqlDR["battery_status"] + "", ups_sqlDR["output_status"] + "", ups_sqlDR["battery_voltage"] + "", ups_sqlDR["surplus_capacity"] + "" };
                }
                else
                {
                    machineRoomBean.StrUPSsql = new string[] { "-1", "-1", "-1", "-1", "-1", "-1", "", "" };
                }
                ups_sqlDR.Close();

                //获取 机房水浸、温湿度、消防
                string strSerRoom = " Select status from  item_view where id  in(1,2,5,6,7)";
                MySqlCommand serRoom_command = new MySqlCommand(strSerRoom, StaticData.g_msMachineRoomRemoteDBC);
                MySqlDataReader serRoom_sqlDR = null;

                try
                {
                    serRoom_sqlDR = serRoom_command.ExecuteReader();
                }
                catch
                {
                    StaticUtils.ShowEventMsg("SkyinforMachineRoom.class-GetRemoteRoomSqlEvent : 执行数据库查询语句失败！\n");
                    return machineRoon_infor_list;
                }


                while (serRoom_sqlDR.Read())
                {
                    machineRoomBean.StrLisSerRoomsql.Add(serRoom_sqlDR["status"] + "");
                }
                serRoom_sqlDR.Close();

                /****************************   解析获取到的参数   ****************************/            
                #region
                //UPS类型
                switch (int.Parse(machineRoomBean.StrUPSsql[0]))
                {
                    case 0:
                        machineRoomBean.StrUPSType = "单进单出";
                        break;
                    case 1:
                        machineRoomBean.StrUPSType = "三进单出";
                        break;
                    case 2:
                        machineRoomBean.StrUPSType = "三进三出";
                        break;
                    default:
                        machineRoomBean.StrUPSType = "未知";
                        break;
                }
                //供电模式
                switch (int.Parse(machineRoomBean.StrUPSsql[1]))
                {
                    case 0:
                        machineRoomBean.StrPowerUpType = "逆变";
                        break;
                    case 1:
                        machineRoomBean.StrPowerUpType = "旁路";
                        break;
                    default:
                        machineRoomBean.StrPowerUpType = "未知";
                        break;
                }
                //工作状态    
                switch (int.Parse(machineRoomBean.StrUPSsql[2]))
                {
                    case 0:
                        machineRoomBean.StrWorkType = "在线式";
                        break;
                    case 1:
                        machineRoomBean.StrWorkType = "离线式";
                        break;
                    default:
                        machineRoomBean.StrWorkType = "未知";
                        break;
                }
                //市电状态
                switch (int.Parse(machineRoomBean.StrUPSsql[3]))
                {

                    case 0:
                        machineRoomBean.StrElectricType = "正常";
                        break;
                    case 1:
                        machineRoomBean.StrElectricType = "失败";
                        break;
                    default:
                        machineRoomBean.StrElectricType = "未知";
                        break;
                }
                //输出状态
                switch (int.Parse(machineRoomBean.StrUPSsql[4]))
                {

                    case 0:
                        machineRoomBean.StrOutputType = "正常";
                        break;
                    case 1:
                        machineRoomBean.StrOutputType = "异常";
                        break;
                    default:
                        machineRoomBean.StrOutputType = "未知";
                        break;
                }
                //电池状态
                switch (int.Parse(machineRoomBean.StrUPSsql[5]))
                {

                    case 0:
                        machineRoomBean.StrCellType = "正常";
                        break;
                    case 1:
                        machineRoomBean.StrCellType = "电量低";
                        break;
                    default:
                        machineRoomBean.StrCellType = "未知";
                        break;
                }
                #endregion

                machineRoon_infor_list.Add(machineRoomBean);
            }
            StaticData.g_msMachineRoomRemoteDBC.Close();
            return machineRoon_infor_list;
        }

        /// <summary>
        /// 根据GET到的数据更新DB device_computer_room_data
        /// </summary>
        public void insertToDbDeviceComputerRoomData(List<MachineRoomBean> machineRoon_infor_list)
        {
            if (StaticData.g_msMachineRoomDBC.State == System.Data.ConnectionState.Open)
            {
                return;
            }

            if (StaticData.g_msMachineRoomDBC.State == System.Data.ConnectionState.Closed)
            {
                try
                {
                    StaticData.g_msMachineRoomDBC.Open();
                }
                catch
                {
                    return;
                }
            }

            foreach (MachineRoomBean machineRoomBean in machineRoon_infor_list)
            {
                string localSql = "INSERT INTO device_computer_room_data (device_name,device_temp,device_humidity,device_ups_type,device_power_supply_model,device_working_model,device_power_status,device_output_status,device_battery_voltage,device_battery_status,device_surplus_capacity,device_TH_status1,device_TH_status2,device_ups_status,device_lsostatus1,device_lsostatus2,device_time) VALUES ( '" + machineRoomBean.StrMachineRoomName + "', '" +
                    machineRoomBean.StrAirsql[0] + "','" + machineRoomBean.StrAirsql[1] + "','" + machineRoomBean.StrUPSType + "','" + machineRoomBean.StrPowerUpType + "','" + machineRoomBean.StrWorkType + "','" + machineRoomBean.StrElectricType + "','" + machineRoomBean.StrOutputType + "','" + machineRoomBean.StrCellType + "','" + machineRoomBean.StrUPSsql[6] + "','" + machineRoomBean.StrUPSsql[7] + "','" + machineRoomBean.StrLisSerRoomsql[0] + "','" +
                    machineRoomBean.StrLisSerRoomsql[1] + "','" + machineRoomBean.StrLisSerRoomsql[2] + "','" + machineRoomBean.StrLisSerRoomsql[3] + "','" + machineRoomBean.StrLisSerRoomsql[4] + "','" + DateTime.Now.ToString() + "')";
                MySqlCommand localcmd = new MySqlCommand(localSql, StaticData.g_msMachineRoomDBC);

                try
                {
                    localcmd.ExecuteNonQuery();
                }
                catch
                {
                    StaticUtils.ShowEventMsg("SkyinforMachineRoom.class-insertToDbDeviceComputerRoomData : 执行数据库插入语句失败！\n");
                    return;
                }

            }
            StaticData.g_msMachineRoomDBC.Close();
        }

        /// <summary>
        /// Update Form中的DataGridView中数据
        /// </summary>
        public void updateFormUI(List<MachineRoomBean> machineRoon_infor_list)
        {
            foreach (MachineRoomBean machineRoomBean in machineRoon_infor_list)
            {
                DataRow[] dr = StaticData.g_dtMachineRoom.Select("机房名称='" + machineRoomBean.StrMachineRoomName + "'");

                if (dr.Length > 0)
                {
                    dr[0]["精密空调温度"] = machineRoomBean.StrAirsql[0];
                    dr[0]["精密空调湿度"] = machineRoomBean.StrAirsql[1];
                    dr[0]["UPS类型"] = machineRoomBean.StrUPSType;
                    dr[0]["UPS供电模式"] = machineRoomBean.StrPowerUpType;
                    dr[0]["UPS工作方法"] = machineRoomBean.StrWorkType;
                    dr[0]["UPS市电状态"] = machineRoomBean.StrElectricType;
                    dr[0]["UPS输出状态"] = machineRoomBean.StrOutputType;
                    dr[0]["UPS电池电压"] = machineRoomBean.StrCellType;
                    dr[0]["UPS电池状态"] = machineRoomBean.StrUPSsql[6];
                    dr[0]["UPS电池剩余容量"] = machineRoomBean.StrUPSsql[7];
                    dr[0]["水浸1"] = machineRoomBean.StrLisSerRoomsql[0];
                    dr[0]["水浸2"] = machineRoomBean.StrLisSerRoomsql[1];
                    dr[0]["消防主机监测"] = machineRoomBean.StrLisSerRoomsql[2];
                    dr[0]["温湿度1"] = machineRoomBean.StrLisSerRoomsql[3];
                    dr[0]["温湿度2"] = machineRoomBean.StrLisSerRoomsql[4];
                    dr[0]["时间"] = DateTime.Now.ToString();
                }
            }
        }


        /// <summary>
        /// 获取远端数据中的机房未获取过的异常信息
        /// </summary>
        public List<string[]> GetRemoteRoomWarningSqlEvent()
        {
            string[] sqlstr = new string[] { }; // 存储最新的异常信息
                                             
            List<string[]> machineRoomInfors = new List<string[]>();   // 将远端DB读取到的数据集存到 STL中

            if (StaticData.g_msMachineRoomRemoteDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msMachineRoomRemoteDBC.Open();
            }
            string strCmd = "SELECT a.name,a.value,a.data_id,a.item_key,a.itemgroup_key,b.collection_time,c.itemgroup_name,d.item_name FROM airbox_alarm_data_rt AS a INNER JOIN airbox_data_rt AS b ON a.data_id = b.id INNER JOIN itemgroup AS c ON a.itemgroup_key = c.itemgroup_key INNER JOIN item AS d ON a.item_key = d.item_key WHERE a.`value` <> '正常'";
            MySqlCommand command = new MySqlCommand(strCmd, StaticData.g_msMachineRoomRemoteDBC);
            MySqlDataReader sqlDR = null;

            try
            {
                sqlDR = command.ExecuteReader();
            }
            catch
            {
                StaticUtils.ShowEventMsg("SkyinforMachineRoom.class-GetRemoteRoomWarningSqlEvent : 执行数据库查询语句失败！\n");
                return machineRoomInfors;
            }


            while (sqlDR.Read())
            {
                if (sqlDR["name"] != null)
                {
                    sqlstr = new string[] { sqlDR["name"] + "", sqlDR["value"] + "", sqlDR["data_id"] + "", sqlDR["item_key"] + "", sqlDR["itemgroup_key"] + "", sqlDR["collection_time"] + "", sqlDR["itemgroup_name"] + "", sqlDR["item_name"] + "" };
                    machineRoomInfors.Add(sqlstr);
                }
            }
            sqlDR.Close();
            StaticData.g_msMachineRoomRemoteDBC.Close();
            return machineRoomInfors;
        }

        /// <summary>
        ///  将异常record作为新的event record插入event_assemble & event_wifi中
        /// 判断数据库是否存在该事件，且时间是否x seconds以上才再次触发，存在且是的话插入新事件
        /// </summary>  
        public void judgeWarningToEventDD(List<string[]> machineRoomInfors)
        {
            foreach(string[] singleMachineRoom  in machineRoomInfors){
                DataRow[] dr = StaticData.g_dtMachineRoom.Select("机房名称='" + singleMachineRoom[6] + "'");
                string theone = dr[0]["theone"].ToString();
                string strCMD = "select *  from event_assemble where target_theone='" + theone + "' ORDER BY id DESC LIMIT 1;";
                if (StaticData.g_msMachineRoomDBC.State == System.Data.ConnectionState.Closed)
                {
                    StaticData.g_msMachineRoomDBC.Open();
                }
                MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msMachineRoomDBC);
                MySqlDataReader sqlReader = null;
                try
                {
                    sqlReader = cmd.ExecuteReader();
                }
                catch
                {
                    StaticUtils.ShowEventMsg("SkyinforMachineRoom.class-judgeWarningToEventDD : 执行数据库查询语句失败！\n");
                    return;
                }
                
                if (sqlReader.Read())
                {
                    // 计算两个dateTime 的时间戳，单位s
                    long timeSpan = StaticUtils.GetCurrentTimeUnix(DateTime.Now) - StaticUtils.GetCurrentTimeUnix(Convert.ToDateTime(sqlReader["end_time"]));                   
                    if (timeSpan < StaticData.g_intMachineRoomWarningIntervalTime)
                    {
                        sqlReader.Close();
                        return;  // 返回\"\"代表无需后续步骤，eg step: web端上传
                    }
                    else if (sqlReader["event_status"].ToString().Equals("已处理"))
                    {
                        sqlReader.Close();
                        return;  // 返回\"\"代表无需后续步骤，eg step: web端上传
                    }
                }               
                sqlReader.Close();
                StaticData.g_msMachineRoomDBC.Close();
                insertWarningToEventDb(singleMachineRoom[6]);
            }
        }

        /// <summary>
        ///  将经过判断后需写入的机房异常数据存入到Event_assemble 和event_computer_room
        ///  strLevel : 重要情况
        /// </summary>
        public void insertWarningToEventDb(string machineRoomName)
        {
            if (StaticData.g_msMachineRoomDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msMachineRoomDBC.Open();
            }
            //设置 情况登记
            string strLevel = "重要情况";
            //生成eventtheon
            string strEventTheOne = Guid.NewGuid().ToString();

            DataRow[] dr = StaticData.g_dtMachineRoom.Select("机房名称='" + machineRoomName + "'");
            if (dr.Length > 0)
            {
                string theone = dr[0]["theone"].ToString();
                string strMachineRoomPosition = dr[0]["坐标"].ToString();

                // 将record作为新的event record进行插入 
                DataRow[] dr_abnormal_super = StaticData.g_dtAbnormalInfor.Select("故障父类名称='event_computer_room'");
                DataRow[] dr_abnormal_sub = StaticData.g_dtAbnormalInfor.Select("父ID='" + dr_abnormal_super[0]["ID"] + "'");
                string event_name = dr_abnormal_sub[0]["故障子类名称"].ToString();

                string strInsert_assemble = "INSERT INTO event_assemble (event_name,event_theone,event_type,start_time,end_time,event_status,position,event_level,target_theone) values" +
     "('" + event_name + "','" + strEventTheOne + "','event_computer_room','" + DateTime.Now.ToString().Replace('/', '-') + "','" + DateTime.Now.ToString().Replace('/', '-') + "','未读','" +
     strMachineRoomPosition + "','" + strLevel + "','" + theone + "');";
                string strInsert_machineroom = "INSERT INTO event_computer_room (event_theone,device_event_type,device_theone) values" +
    "('" + strEventTheOne + "','" + event_name + "','" + theone + "');";

                MySqlCommand insertAssembleCmd = new MySqlCommand(strInsert_assemble, StaticData.g_msMachineRoomDBC);
                MySqlCommand insertMachineCmd = new MySqlCommand(strInsert_machineroom, StaticData.g_msMachineRoomDBC);

                string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                string currentClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
                StaticUtils.DBNonQueryOperationException(ref insertAssembleCmd, currentClassName, currentMethodName);
                StaticUtils.DBNonQueryOperationException(ref insertMachineCmd, currentClassName, currentMethodName);

                //进行Web端报警
                sendMsgToWebSocketClients(strEventTheOne, "报警时间：" + DateTime.Now.ToString() + "  事件类型：机房数据异常告警  机房名称：" + machineRoomName + "\n");
            }
            StaticData.g_msMachineRoomDBC.Close();
        }

        /// <summary>
        /// 向web 上报事件
        /// </summary>
        public void sendMsgToWebSocketClients(string strEventTheOne, string msg)
        {
            //发送数据Json格式
            string strSendMSG = jscontrol.EventJson(strEventTheOne);

            Mainform.form1.SendMsgToWebSocketClients(strSendMSG);
            //更新UI 事件列表界面
            StaticUtils.ShowEventMsg(msg);
            //计数加一
            StaticData.g_inAlarmNum++;
        }

    }
}
