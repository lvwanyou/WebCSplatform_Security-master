using MySql.Data.MySqlClient;
using SupportPlatform.Device.ComputerRoom;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;

namespace SupportPlatform
{
    public partial class Mainform : Form
    {
  
        public static WebSocket websocket;
        public static Mainform form1;
        private delegate void CrossThreadOperationControl();//跨线程调用控件委托   
        public Mainform()
        {
            InitializeComponent();
            form1 = this;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //从自己的数据库中读取所有的连接第三方系统的参数配置信息
            DBConnect getSystemArgvDBC = new DBConnect();
            try
            {
                if (getSystemArgvDBC.msConn.State == System.Data.ConnectionState.Closed)
                {
                    getSystemArgvDBC.msConn.Open();
                }
                string strCMD = "select *  from config_support_platform;";
                MySqlCommand cmd = new MySqlCommand(strCMD, getSystemArgvDBC.msConn);
                MySqlDataReader sqlReader = null;
                string currentClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
                string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                if (StaticUtils.DBReaderOperationException(ref cmd, ref sqlReader, currentClassName, currentMethodName))
                {
                    while (sqlReader.Read())
                    {
                        //根据配置名称，将数据库公共配置表中取到的数据存入全局变量。
                        switch (sqlReader["config_type"].ToString())
                        {
                            case "webSocketIP":
                                StaticData.g_strWebSocketPort = sqlReader["port"].ToString();
                                break;
                            //下面是其他第三方系统

                            //广播相关配置
                            case "broadcast":
                                StaticData.g_strLocalIP = sqlReader["local_ip"].ToString();
                                StaticData.g_strBroadcastServerIP = sqlReader["online_ip"].ToString();
                                StaticData.g_strBroadcastServerPort = sqlReader["online_port"].ToString();
                                StaticData.g_strBroadcastListenPort = sqlReader["local_port"].ToString();
                                break;
                            //机房相关配置
                            case "machineRoomConfig":
                                StaticData.g_strMachineRoomUser = sqlReader["username"].ToString();
                                StaticData.g_strMachineRoomPassword = sqlReader["password"].ToString();
                                StaticData.g_strMachineRoomPort = sqlReader["port"].ToString();
                                StaticData.g_strMachineRommIP = sqlReader["nvrip"].ToString();
                                StaticData.g_strMachineRommDBname = sqlReader["dbname"].ToString();
                                break;
                            //系统轮询时间
                            case "system_poll_freq":
                                StaticData.g_nSystemPollFreq = Convert.ToInt32(sqlReader["value"]);
                                break;
                            //闸机相关配置
                            case "gate":

                                break;
                            //停车场相关配置
                            case "park":

                                break;
                        }
                    }
                    sqlReader.Close();
                }
                /// 根据不同硬件， 读取本地数据库各硬件参数表中各自的参数
                #region
                //机房相关独立配置
                string strConfigCMD_machineRoom = "select *  from config_event_computer_room;";
                MySqlCommand configCmd_machineRoom = new MySqlCommand(strConfigCMD_machineRoom, getSystemArgvDBC.msConn);
                MySqlDataReader configSqlReader_machineRoom = null;       
                if(StaticUtils.DBReaderOperationException(ref configCmd_machineRoom, ref configSqlReader_machineRoom, currentClassName, currentMethodName))
                {
                    StaticData.g_dictMachineRoomConfig = new Dictionary<string, int>();
                    while (configSqlReader_machineRoom.Read())
                    {
                        StaticData.g_dictMachineRoomConfig.Add(configSqlReader_machineRoom["event_definition"].ToString(), Convert.ToInt32(configSqlReader_machineRoom["event_value"]));
                    }
                    configSqlReader_machineRoom.Close();
                    StaticData.g_intMachineRoomWarningIntervalTime = Convert.ToInt32(StaticData.g_dictMachineRoomConfig["warning"]);
                }

                //闸机相关独立配置
                string strConfigCMD_gate = "select *  from config_event_gate;";
                MySqlCommand configCmd_gate = new MySqlCommand(strConfigCMD_gate, getSystemArgvDBC.msConn);
                MySqlDataReader configSqlReader_gate = null;
                if (StaticUtils.DBReaderOperationException(ref configCmd_gate, ref configSqlReader_gate, currentClassName, currentMethodName))
                {
                    StaticData.g_dictGateConfig = new Dictionary<string, int>();
                    while (configSqlReader_gate.Read())
                    {
                        StaticData.g_dictGateConfig.Add(configSqlReader_gate["event_definition"].ToString(), Convert.ToInt32(configSqlReader_gate["event_value"]));
                    }
                    configSqlReader_gate.Close();
                    StaticData.g_intGateEarlyWarning = Convert.ToInt32(StaticData.g_dictGateConfig["warning"]);  //闸机人数过多预警
                    StaticData.g_intGateWarning = Convert.ToInt32(StaticData.g_dictGateConfig["alarm"]);  //闸机人数过多告警
                    StaticData.g_intGateWarningIntervalTime = Convert.ToInt32(StaticData.g_dictGateConfig["warning_interval"]);    ////同一闸机相同报警Interval   unit : s
                }
                //停车场相关独立配置
                string strConfigCMD_park = "select *  from config_event_park;";
                MySqlCommand configCmd_park = new MySqlCommand(strConfigCMD_park, getSystemArgvDBC.msConn);
                MySqlDataReader configSqlReader_park = null;
                if (StaticUtils.DBReaderOperationException(ref configCmd_park, ref configSqlReader_park, currentClassName, currentMethodName))
                {
                    StaticData.g_dictParkConfig = new Dictionary<string, double>();
                    while (configSqlReader_park.Read())
                    {
                        StaticData.g_dictParkConfig.Add(configSqlReader_park["event_definition"].ToString(), Convert.ToDouble(configSqlReader_park["event_value"]));
                    }
                    configSqlReader_park.Close();
                    StaticData.g_intParkWarningIntervalTime = Convert.ToInt32(StaticData.g_dictParkConfig["warning_interval"]);  //同一停车场相同报警Interval   unit : s
                    StaticData.g_intParkEarlyWarning = Convert.ToDouble(StaticData.g_dictParkConfig["warning_rate"]);  //车位使用过多预警  考虑到不同车位的容量不同，此处使用百分比
                    StaticData.g_intParkWarning = Convert.ToDouble(StaticData.g_dictParkConfig["alarm_rate"]);   //车位使用过多告警
                }
                #endregion

                getSystemArgvDBC.msConn.Close();
                getSystemArgvDBC.msConn = null;
            }
            catch(Exception aaa)
            {
                Console.WriteLine(aaa.ToString());
                MessageBox.Show(DateTime.Now.ToString()+ "  Mainform.class-Form1_Load : 数据库检索失败，请检查配置文件");
            }  
        }


        /// <summary>
        /// 用于更新UI中的在线数、掉线数、报警数等信息
        /// </summary>
        public void UpdateForm()
        {
            DataRow[] dtrCamera_Online = null;    //获取摄像头在线的行数
            DataRow[] dtrCamera_Offline = null;//获取摄像头掉线的行数  
            DataRow[] dtrBroadcast_Online = null;//获取广播在线的行数 
            DataRow[] dtrBroadcast_Offline = null;//获取广播离线的行数
            while (true)
            {
                Thread.Sleep(5000);
                try
                {
                    dtrCamera_Online = StaticData.g_dtCamera.Select("摄像头状态='正常'");    //获取摄像头在线的行数
                    dtrCamera_Offline = StaticData.g_dtCamera.Select("摄像头状态='掉线'");//获取摄像头掉线的行数   
                }
                catch (Exception)
                {
                    StaticUtils.ShowEventMsg("Mainform.class-UpdateForm : 获取摄像头在线数据出现异常！！\n");

                }
                try
                {
                    dtrBroadcast_Online = StaticData.g_dtBroadcastDevice.Select("连通='连通'");//获取广播在线的行数
                    dtrBroadcast_Offline = StaticData.g_dtBroadcastDevice.Select("连通 <>'连通'");//获取广播离线的行数
                }
                catch (Exception)
                {
                    StaticUtils.ShowEventMsg("Mainform.class-UpdateForm : 获取广播在线数据出现异常！！\n");
                }

                #region 跨线程更新UI界面

                if (dtrCamera_Online != null)
                {
                    CrossThreadOperationControl CrossDele_CamOnline = delegate ()
                    {
                        Mainform.form1.textBoxCameraOnline.Text = dtrCamera_Online.Length.ToString();
                    };
                    Mainform.form1.textBoxCameraOnline.Invoke(CrossDele_CamOnline);
                    CrossThreadOperationControl CrossDele_CamOffline = delegate ()
                    {
                        Mainform.form1.textBoxCameraOffline.Text = dtrCamera_Offline.Length.ToString();
                    };
                    Mainform.form1.textBoxCameraOffline.Invoke(CrossDele_CamOffline);
                    CrossThreadOperationControl CrossDele_AlarmNum = delegate ()
                    {
                        Mainform.form1.textBoxCameraAlarm.Text = StaticData.g_inAlarmNum.ToString();
                    };
                    Mainform.form1.textBoxCameraAlarm.Invoke(CrossDele_AlarmNum);
                }

                if (dtrBroadcast_Online != null)
                {
                    CrossThreadOperationControl CrossDele_BroadOnline = delegate ()
                    {
                        Mainform.form1.textBoxBroadcastOnline.Text = dtrBroadcast_Online.Length.ToString();
                    };
                    Mainform.form1.textBoxBroadcastOnline.Invoke(CrossDele_BroadOnline);
                    CrossThreadOperationControl CrossDele_BroadOffline = delegate ()
                    {
                        Mainform.form1.textBoxBroadcastOffline.Text = dtrBroadcast_Offline.Length.ToString();
                    };
                    Mainform.form1.textBoxBroadcastOffline.Invoke(CrossDele_BroadOffline);
                }
                #endregion
            }
        }

        /// <summary>
        /// 初始化界面相关
        /// </summary>
        private void InitUI()
        {
            #region 初始化异常信息
            //初始化异常信息DataTable
            StaticData.g_dtAbnormalInfor.Columns.Add("ID", System.Type.GetType("System.Int32"));
            StaticData.g_dtAbnormalInfor.Columns.Add("故障父类名称", System.Type.GetType("System.String"));
            StaticData.g_dtAbnormalInfor.Columns.Add("故障子类名称", System.Type.GetType("System.String"));
            StaticData.g_dtAbnormalInfor.Columns.Add("父ID", System.Type.GetType("System.Int32"));
            #endregion

            #region 初始化停车场相关
            //初始化停车场相关
            StaticData.g_dtParking.Columns.Add("停车场名称", System.Type.GetType("System.String"));
            StaticData.g_dtParking.Columns.Add("总车位数", System.Type.GetType("System.Int32"));
            StaticData.g_dtParking.Columns.Add("已使用车位", System.Type.GetType("System.Int32"));
            StaticData.g_dtParking.Columns.Add("未使用车位", System.Type.GetType("System.Int32"));
            StaticData.g_dtParking.Columns.Add("时间", System.Type.GetType("System.String"));
            StaticData.g_dtParking.Columns.Add("地址", System.Type.GetType("System.String"));
            StaticData.g_dtParking.Columns.Add("类别", System.Type.GetType("System.String"));
            StaticData.g_dtParking.Columns.Add("坐标", System.Type.GetType("System.String"));
            StaticData.g_dtParking.Columns.Add("theone", System.Type.GetType("System.String"));

            kDataGV_Parking.DataSource = StaticData.g_dtParking;
            kDataGV_Parking.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            #endregion

            #region 初始化闸机界面显示g_dtGate
            //初始化闸机界面显示g_dtGate
            StaticData.g_dtGate.Columns.Add("闸机名称", System.Type.GetType("System.String"));
            StaticData.g_dtGate.Columns.Add("此闸机入园人数", System.Type.GetType("System.Int32"));
            StaticData.g_dtGate.Columns.Add("此闸机出园人数", System.Type.GetType("System.Int32"));
            StaticData.g_dtGate.Columns.Add("时间", System.Type.GetType("System.String"));
            StaticData.g_dtGate.Columns.Add("坐标", System.Type.GetType("System.String"));
            StaticData.g_dtGate.Columns.Add("地址", System.Type.GetType("System.String"));
            StaticData.g_dtGate.Columns.Add("theone", System.Type.GetType("System.String"));
            KDataGV_Gate.DataSource = StaticData.g_dtGate;
            KDataGV_Gate.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            #endregion

            #region 初始化机房环控界面
            //初始化机房环控界面
            StaticData.g_dtMachineRoom.Columns.Add("机房名称", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("精密空调温度", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("精密空调湿度", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("UPS类型", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("UPS供电模式", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("UPS工作方法", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("UPS市电状态", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("UPS输出状态", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("UPS电池电压", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("UPS电池状态", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("UPS电池剩余容量", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("水浸1", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("水浸2", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("消防主机监测", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("温湿度1", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("温湿度2", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("时间", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("坐标", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("theone", System.Type.GetType("System.String"));
            StaticData.g_dtMachineRoom.Columns.Add("地址", System.Type.GetType("System.String"));
            kDataGV_Room.DataSource = StaticData.g_dtMachineRoom;
            kDataGV_Room.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            #endregion

            #region 初始化广播界面
            //初始化广播界面
            StaticData.g_dtBroadcastDevice.Columns.Add("编号", System.Type.GetType("System.Int32"));
            StaticData.g_dtBroadcastDevice.Columns.Add("名称", System.Type.GetType("System.String"));
            StaticData.g_dtBroadcastDevice.Columns.Add("类型", System.Type.GetType("System.String"));
            StaticData.g_dtBroadcastDevice.Columns.Add("IP", System.Type.GetType("System.String"));
            StaticData.g_dtBroadcastDevice.Columns.Add("连通", System.Type.GetType("System.String"));
            StaticData.g_dtBroadcastDevice.Columns.Add("状态", System.Type.GetType("System.String"));
            StaticData.g_dtBroadcastDevice.Columns.Add("频道", System.Type.GetType("System.String"));
            StaticData.g_dtBroadcastDevice.Columns.Add("音量", System.Type.GetType("System.String"));
            StaticData.g_dtBroadcastDevice.Columns.Add("theone", System.Type.GetType("System.String"));
            StaticData.g_dtBroadcastDevice.Columns.Add("时间", System.Type.GetType("System.String"));
            StaticData.g_dtBroadcastDevice.Columns.Add("坐标", System.Type.GetType("System.String"));
            kGridView_Broadcast.DataSource = StaticData.g_dtBroadcastDevice;
            kGridView_Broadcast.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            #endregion

            #region 初始化Wifi界面
            //初始化Wifi界面
            StaticData.g_dtWifi.Columns.Add("Ap名称", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("Ap中文名称", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("Ap型号", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("wifi设备状态", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("在线状态", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("接入人数", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("Ap安装位置", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("时间", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("安装方式", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("Ap标志", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("WIFI坐标", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("theone", System.Type.GetType("System.String"));
            StaticData.g_dtWifi.Columns.Add("SN", System.Type.GetType("System.String"));
            kDataGV_Wifi.DataSource = StaticData.g_dtWifi;
            kDataGV_Wifi.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            #endregion

        }


        /// <summary>
        /// 第三方系统初始化函数
        /// </summary>
        private void InitDevices()
        {
            ///
            ///初始化摄像头
            ///
            #region
            //初始化安防NVR与IPC  
            //首先初始化数据库连接
            DBConnect getVideoCBDBC = new DBConnect();
            try
            {
                if (getVideoCBDBC.msConn.State == System.Data.ConnectionState.Closed)
                {
                    getVideoCBDBC.msConn.Open();
                }
                StaticData.g_msVideCallBackConn = getVideoCBDBC.msConn;//将数据库连接指向静态全局，保持开放为事件回调写入提供
            }
            catch
            {
                StaticUtils.ShowEventMsg("Mainform.class-InitDevices : 初始化摄像头数据库连接出现异常 ！！\n");
            }

            DBConnect dbcVideoQueryDBC = new DBConnect();
            try
            {
                if (dbcVideoQueryDBC.msConn.State == System.Data.ConnectionState.Closed)
                {
                    dbcVideoQueryDBC.msConn.Open();
                }
                StaticData.g_msVideQueryConn = dbcVideoQueryDBC.msConn;//将数据库连接指向静态全局，保持开放为事件回调写入提供
            }
            catch
            {
                StaticUtils.ShowEventMsg("Mainform.class-InitDevices : 初始化摄像头数据库连接出现异常 ！！\n");
            }
            #endregion

            ///
            ///初始化广播,建立广播相关的数据库连接   
            ///   
            #region
            //将获取到的数据库信息存入到datatable
            DBConnect getBroadcastDBC = new DBConnect();
            try
            {
                if (getBroadcastDBC.msConn.State == System.Data.ConnectionState.Closed)
                {
                    getBroadcastDBC.msConn.Open();
                }
                StaticData.g_msBroadcastgDBC = getBroadcastDBC.msConn;//将数据库连接指向静态全局，保持开放为事件回调写入提供
                                                                      //然后把远程数据库的数据存入到datatable
                string strCMD = "select * from device_broadcast;";
                MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msBroadcastgDBC);
                MySqlDataReader sqlReaderBroadcast = cmd.ExecuteReader();
                while (sqlReaderBroadcast.Read())
                {
                    DataRow dr = StaticData.g_dtBroadcastDevice.NewRow();//表格的行
                    dr["编号"] = Convert.ToInt32(sqlReaderBroadcast["device_bianhao"]);
                    dr["名称"] = sqlReaderBroadcast["device_name"].ToString();
                    dr["类型"] = sqlReaderBroadcast["device_type"].ToString();
                    dr["IP"] = sqlReaderBroadcast["device_ip"].ToString();
                    dr["连通"] = sqlReaderBroadcast["device_liantong"].ToString();
                    dr["状态"] = sqlReaderBroadcast["device_status"].ToString();
                    dr["频道"] = sqlReaderBroadcast["device_channel"].ToString();
                    dr["theone"] = sqlReaderBroadcast["device_theone"].ToString();
                    dr["音量"] = sqlReaderBroadcast["device_volume"].ToString();
                    if (sqlReaderBroadcast["device_time"].ToString() == null || "".Equals(sqlReaderBroadcast["device_time"].ToString()))
                    {
                        dr["时间"] = "";
                    }
                    else
                    {
                        dr["时间"] = Convert.ToDateTime(sqlReaderBroadcast["device_time"].ToString());
                    }   
                    dr["坐标"]= sqlReaderBroadcast["device_position"].ToString();
                    StaticData.g_dtBroadcastDevice.Rows.Add(dr);//添加                   
                }
                sqlReaderBroadcast.Close(); // 写入操作关闭， 数据库连接保持开放
                StaticData.g_msBroadcastgDBC.Close();
            }
            catch
            {
                //  throw;
                StaticUtils.ShowEventMsg("Mainform.class-InitDevices : 初始化广播,建立广播相关的数据库连接出现异常 ！！\n");
            }
            #endregion 

            ///
            ///初始化停车场
            ///
            #region
            DBConnect getParkingDBC = new DBConnect();
            try
            {
                if (getParkingDBC.msConn.State == System.Data.ConnectionState.Closed)
                {
                    getParkingDBC.msConn.Open();
                }
                StaticData.g_msParkingDBC = getParkingDBC.msConn;//将数据库连接指向静态全局，保持开放为事件回调写入提供

                string strCMD = "select * from device_park;";
                MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msParkingDBC);
                MySqlDataReader sqlReaderParking = cmd.ExecuteReader();
                while (sqlReaderParking.Read())
                {
                    DataRow dr = StaticData.g_dtParking.NewRow();//表格的行
                    dr["停车场名称"] = sqlReaderParking["device_name"].ToString();
                    dr["总车位数"] = Convert.ToInt32(sqlReaderParking["device_all_park"]);
                    dr["未使用车位"] = Convert.ToInt32(sqlReaderParking["device_rest_park"]);
                    dr["已使用车位"] = Convert.ToInt32(sqlReaderParking["device_all_park"]) - Convert.ToInt32(sqlReaderParking["device_rest_park"]);
                    dr["时间"] = sqlReaderParking["device_time"].ToString();
                    dr["地址"] = sqlReaderParking["device_address"].ToString();
                    dr["类别"] = sqlReaderParking["device_park_id"].ToString();
                    dr["theone"] = sqlReaderParking["device_theone"].ToString();
                    dr["坐标"] = sqlReaderParking["device_position"].ToString();
                    StaticData.g_dtParking.Rows.Add(dr);//添加    
                }
                sqlReaderParking.Close(); // 写入操作关闭， 数据库连接保持开放
                StaticData.g_msParkingDBC.Close();
            }
            catch
            {
                //  throw;
                StaticUtils.ShowEventMsg("Mainform.class-InitDevices : 初始化停车场出现异常 ！！\n");
            }
            #endregion

            ///
            ///初始化闸机系统
            ///
            #region
            DBConnect getGateDBC = new DBConnect();
            try
            {
                if (getGateDBC.msConn.State == System.Data.ConnectionState.Closed)
                {

                    getGateDBC.msConn.Open();

                }
                StaticData.g_msGateDBC = getGateDBC.msConn;

                string strCMD = "select device_name,device_time,concat_ws(',',ST_X(device_point),ST_Y(device_point),device_height) as device_position,device_address,device_theone  from device_gate;";
                MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msGateDBC);
                MySqlDataReader sqlReaderGate = cmd.ExecuteReader();
                while (sqlReaderGate.Read())
                {
                    DataRow dr = StaticData.g_dtGate.NewRow();//表格的行
                    dr["闸机名称"] = sqlReaderGate["device_name"].ToString();
                    dr["此闸机入园人数"] = 0;
                    dr["此闸机出园人数"] = 0;
                    dr["时间"] = sqlReaderGate["device_time"].ToString();
                    dr["坐标"] = sqlReaderGate["device_position"].ToString();
                    dr["地址"] = sqlReaderGate["device_address"].ToString();
                    dr["theone"] = sqlReaderGate["device_theone"].ToString();
                    StaticData.g_dtGate.Rows.Add(dr);//添加                   
                }
                sqlReaderGate.Close(); // 写入操作关闭， 数据库连接保持开放
                StaticData.g_msGateDBC.Close();
            }
            catch
            {

                StaticUtils.ShowEventMsg("Mainform.class-InitDevices : 初始化停车场 ！！\n");

            }
            #endregion

            ///
            ///初始化机房环控
            ///
            #region
            DBConnect getMachineRoom = new DBConnect();
            //机房设备数据实时写入到远端数据库中， 故需轮询远端数据库
            MachineRoomRemoteDBConnect getRemoteMachineRoom = new MachineRoomRemoteDBConnect();  // 开启机房远程数据库连接
            try
            {
                if (getMachineRoom.msConn.State == System.Data.ConnectionState.Closed)
                {
                    getMachineRoom.msConn.Open();
                }
                StaticData.g_msMachineRoomDBC = getMachineRoom.msConn;

                if (getRemoteMachineRoom.msConn.State == System.Data.ConnectionState.Closed)
                {
                    getRemoteMachineRoom.msConn.Open();
                }
                StaticData.g_msMachineRoomRemoteDBC = getRemoteMachineRoom.msConn;

                string strCMD = "select * from device_computer_room;";
                MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msMachineRoomDBC);
                MySqlDataReader sqlReaderMachineRoom = cmd.ExecuteReader();
                while (sqlReaderMachineRoom.Read())
                {
                    DataRow dr = StaticData.g_dtMachineRoom.NewRow();//表格的行
                    dr["机房名称"] = sqlReaderMachineRoom["device_name"].ToString();
                    dr["坐标"] = sqlReaderMachineRoom["device_position"].ToString();
                    dr["theone"] = sqlReaderMachineRoom["device_theone"].ToString();
                    dr["地址"] = sqlReaderMachineRoom["device_address"].ToString();
                    StaticData.g_dtMachineRoom.Rows.Add(dr);//添加                   
                }
                sqlReaderMachineRoom.Close();
                StaticData.g_msMachineRoomDBC.Close();
            }
            catch
            {
                StaticUtils.ShowEventMsg("Mainform.class-InitDevices : 初始化机房环控出现异常 ！！\n");

            }
            #endregion
            ///
            ///初始化Wifi数据库连接
            ///
            #region
            DBConnect getDeviceWifi = new DBConnect();
            try
            {
                if (getDeviceWifi.msConn.State == System.Data.ConnectionState.Closed)
                {
                    getDeviceWifi.msConn.Open();
                }
                StaticData.g_msWifiDBC = getDeviceWifi.msConn;

                string strCMD = "select * from device_wifi;";
                MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msWifiDBC);
                MySqlDataReader sqlReaderGate = cmd.ExecuteReader();
                while (sqlReaderGate.Read())
                {
                    DataRow dr = StaticData.g_dtWifi.NewRow();//表格的行
                    dr["Ap名称"] = sqlReaderGate["device_name"].ToString();
                    dr["Ap中文名称"] = sqlReaderGate["device_ap_chinesename"].ToString();
                    dr["Ap型号"] = sqlReaderGate["device_ap_model"].ToString();
                    dr["wifi设备状态"] = sqlReaderGate["device_status"].ToString();
                    dr["在线状态"] = sqlReaderGate["device_onlinestatus"].ToString();
                    dr["接入人数"] = sqlReaderGate["device_person_online"].ToString();
                    dr["Ap安装位置"] = sqlReaderGate["device_address"].ToString();
                    dr["时间"] = sqlReaderGate["device_time"].ToString();
                    dr["安装方式"] = sqlReaderGate["device_install_type"].ToString();
                    dr["Ap标志"] = sqlReaderGate["device_ap_tag"].ToString();
                    dr["WIFI坐标"] = sqlReaderGate["device_position"].ToString();
                    dr["theone"] = sqlReaderGate["device_theone"].ToString();
                    dr["SN"] = sqlReaderGate["device_ap_sn"].ToString();
                    StaticData.g_dtWifi.Rows.Add(dr);//添加                   
                }

                sqlReaderGate.Close(); // 写入操作关闭
                StaticData.g_msWifiDBC.Close();
            }
            catch
            {
                StaticUtils.ShowEventMsg("Mainform.class-InitDevices : 初始化Wifi数据库连接出现异常 ！！\n");
            }
            #endregion

            ///
            ///初始化异常事件数据库连接  &&  异常信息DataTable 初始化
            ///
            #region
            DBConnect getEventAssemble = new DBConnect();
            try
            {
                if (getEventAssemble.msConn.State == System.Data.ConnectionState.Closed)
                {
                    getEventAssemble.msConn.Open();
                }
                StaticData.g_msEventAssembleDBC = getEventAssemble.msConn;

                string strCMD = "select * from event_type;";
                MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msEventAssembleDBC);
                MySqlDataReader sqlReaderAbnormalInfor = cmd.ExecuteReader();
                while (sqlReaderAbnormalInfor.Read())
                {
                    DataRow dr = StaticData.g_dtAbnormalInfor.NewRow();//表格的行
                    dr["ID"] = sqlReaderAbnormalInfor["id"].ToString();
                    dr["故障父类名称"] = sqlReaderAbnormalInfor["event_type"].ToString();
                    dr["故障子类名称"] = sqlReaderAbnormalInfor["event_type_name"].ToString();
                    dr["父ID"] = sqlReaderAbnormalInfor["parent_id"].ToString();  
                    StaticData.g_dtAbnormalInfor.Rows.Add(dr);//添加                   
                }

                sqlReaderAbnormalInfor.Close(); // 写入操作关闭  
                StaticData.g_msEventAssembleDBC.Close();
            }
            catch
            {
                StaticUtils.ShowEventMsg("Mainform.class-InitDevices : 初始化异常事件数据库连接出现异常 ！！\n");
            }
            StaticData.g_msEventAssembleDBC.Close();
            #endregion

        }

        /// <summary>
        /// 用于设置websocket端口同时开启所有功能
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_StartWebSocket_Click(object sender, EventArgs e)
        {
            ///
            ///初始化界面相关数据
            ///
            InitUI();

            ///
            ///初始化第三方设备
            ///
            //注册设备线程
            InitDevices();


            ///
            ///开启websocket线程
            ///
            Thread thrWebSocket = new Thread(new ThreadStart(WebSocketInit));
            thrWebSocket.IsBackground = true;
            thrWebSocket.Start();


            ///
            ///开启摄像头初始化线程 
            ////* 摄像头数量较多，初始化过程牵扯到多步操作（eg:数据库摄像头相关状态更新）。因此开启子线程执行耗时操作，避免主线程卡死 */
            ///
            SkyinforHikvision skyinforhk = new SkyinforHikvision();
            InitCamera(ref skyinforhk);
            Thread thrCamerainit = new Thread(new ThreadStart(skyinforhk.UpdateDB));
            thrCamerainit.IsBackground = true;
            thrCamerainit.Start();
            //轮询完所有摄像头，开启摄像头转码线程,
            //读取dt表中flag字段跟rtsp留，然后开线程转码
            //SkyinforVideoTransform videoTransform = SkyinforVideoTransform.getInstance();
            //Thread thrCamerainit = new Thread(new ThreadStart(videoTransform.SkyinforVideoServer));
            //thrCamerainit.IsBackground = true;
            //thrCamerainit.Start();

            ///
            ///轮询第三方设备
            ///
            //轮询机房环控
            SkyinforMachineRoom environment = new SkyinforMachineRoom();
            Thread thrEnvironment = new Thread(new ThreadStart(environment.MachineRoom_Get));
            thrEnvironment.IsBackground = true;
            thrEnvironment.Start();


            //轮询广播 : 一直监听broadcast硬件的广播，没隔30秒左右会返回一遍状态
            BroadcastUDP serverReceive = new BroadcastUDP();
            Thread thrUDP = new Thread(new ThreadStart(serverReceive.BroadServerReceive));
            thrUDP.IsBackground = true;
            thrUDP.Start();


            //轮询停车场
            SkyinforParking skinforParing = new SkyinforParking();
            Thread Parking = new Thread(new ThreadStart(skinforParing.ParkingWebRequest_Get));
            Parking.IsBackground = true;
            Parking.Start();

            //轮询闸机
            SkyinforGate skyinforGate = new SkyinforGate();
            Thread Gate = new Thread(new ThreadStart(skyinforGate.Gate_Get));
            Gate.IsBackground = true;
            Gate.Start();

            //轮询Wifi列表
            SkyinforWifi skyinforWifi = new SkyinforWifi();
            Thread Wifi = new Thread(new ThreadStart(skyinforWifi.WifiList_Get));   // 获取所有AP的基本信息的列表
            Wifi.IsBackground = true;
            Wifi.Start();


            //App上报故障事件监听线程
            Thread AppEventListener = new Thread(new ThreadStart(PollForAppEvent));
            AppEventListener.IsBackground = true;
            AppEventListener.Start();

            ///
            ///界面刷新及状态监控
            ///
            //软件占用资源信息线程
            Thread ProgramInfo = new Thread(new ThreadStart(GetProgramInfo));
            ProgramInfo.IsBackground = true;
            ProgramInfo.Start();

            //设备状态统计信息线程
            Thread DeviceInfo = new Thread(new ThreadStart(UpdateForm));
            DeviceInfo.IsBackground = true;
            DeviceInfo.Start();


            kBut_StartService.Text = "服务运行中...";
            kBut_StartService.BackColor = ColorTranslator.FromHtml("#4679FF");

            kBut_StartService.FlatAppearance.BorderSize = 0;
            kBut_StartService.Enabled = false;

        }


        /// <summary>
        /// 初始化websocket
        /// </summary>
        private void WebSocketInit()
        {
            websocket = new WebSocket();
        }

        /// <summary>
        /// 轮询摄像头
        /// </summary>
        private void InitCamera(ref SkyinforHikvision skyinforhk)
        {
            skyinforhk.InitNVR();
            skyinforhk.InitHKCamera();
            //初始化报警回调函数。zzt  这里有问题，可能有多个NVR，这里要对所有的NVR跟所有的通道整体注册
            Hikvision hikAlarmCallBack = new HikvisionAlarmCallBackFun();
            //对所有NVR进行注册报警
            //根据NVR里面的通道，获取到了所有通道在线信息，然后根据nvr id 跟nvr datatable进行关联，将nvr id 与 nvr ip 地址与 nvr通道号一起跟摄像头关联
            for (int i = 0; i < StaticData.g_dtNVR.Rows.Count; i++)
            {
                hikAlarmCallBack.AlarmCallBackFun(Convert.ToInt32(StaticData.g_dtNVR.Rows[i]["userid"]), 0);
            }
            //更新下列表界面
            CameraListUpdate listUpdate = new CameraListUpdate(StaticData.g_dtCamera);
        }


        /// <summary>
        /// 轮询摄像头
        /// </summary>
        private void QueryCamera()
        {
            while (true)
            {
                Thread.Sleep(10000);    //线程挂起10s
                                        //更新一遍摄像头状态
                SkyinforHikvision skyinforhk = new SkyinforHikvision();
                skyinforhk.QueryCameras();
            }
        }


        /// <summary>
        /// 该方法用于发送消息给客户端
        /// webSocket发送消息
        /// </summary>
        public void SendMsgToWebSocketClients(string strMsg)
        {
            //发送消息给各个客户端
            if (WebSocket.m_listClient != null)
            {
                foreach (Socket clientSocket in WebSocket.m_listClient.Values)
                {
                    try
                    {
                        //发送摄像头状态信息给每一个客户端
                        // string msg = ProcessMsgQuery();
                        if (strMsg != null)
                        {
                            websocket.SendMessage(clientSocket, strMsg);
                        }
                    }
                    catch (Exception ex)
                    {
                        StaticUtils.ShowEventMsg("Mainform.class-SendMsgToWebSocketClients : 轮询发送数据出现异常！！\n");
                        Console.WriteLine("轮询发送数据出错" + ex);
                    }
                }
            }
        }

        /// <summary>
        /// 用于摄像头列表的选取
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kdtGrid_CameraList_CellContentClick(object sender, DataGridViewCellEventArgs e)  //单击DataView中的单元格内容时发生
        {
            //string strCameraName= (kdtGrid_CameraList.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString());
            //DataRow[] dr = StaticData.g_dtCamera.Select("摄像头名称='" + strCameraName + "'");           
            //if (dr.Length>=1)
            //{
            //    //StaticData.g_CameraSelectChannel = Int32.Parse(dr[e.RowIndex]["通道号"].ToString());//将获取到的通道号存入g_CameraChannel;         
            //}
            //else
            //{
            //    return;
            //}
            try
            {
                //得到选中行的索引
                int intRow = kdtGrid_CameraList.SelectedCells[0].RowIndex;

                //得到列的索引
                int intColumn = kdtGrid_CameraList.SelectedCells[0].ColumnIndex;
                string strLine = kdtGrid_CameraList.Rows[intRow].Cells["通道号"].Value.ToString();

                StaticData.g_CameraSelectChannel = Int32.Parse(strLine);//将获取到的通道号存入g_CameraChannel;
                StaticData.g_CameraSelectNVRIP = kdtGrid_CameraList.Rows[intRow].Cells["NVRIP地址"].Value.ToString();//将获取到的通道号存入g_CameraChannel;
            }
            catch (Exception ex)
            {
                StaticUtils.ShowEventMsg("Mainform.class-kdtGrid_CameraList_CellContentClick : 摄像头列表的选取出现异常！！\n");
                return;
            }
        }
        /// <summary>
        /// 摄像头预览方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kButtonView_Click(object sender, EventArgs e)
        {

            if (StaticData.g_CameraSelectChannel > 0)
            {
                Hikvision previewFun = HikvisionPreviewFun.GetInstance();
                //根据选中的摄像头通道号跟IP地址进行预览
                string srtSql = "IP地址= '" + StaticData.g_CameraSelectNVRIP + "'";
                DataRow[] dr = StaticData.g_dtNVR.Select(srtSql);
                if (dr.Length > 0)
                {
                    previewFun.PreviewFun(Convert.ToInt32(dr[0]["userid"]), StaticData.g_CameraSelectChannel);
                }

            }
            else
            {
                MessageBox.Show("请点击摄像头名称选择要预览的摄像头画面");
            }
        }

        /// <summary>
        /// 获取软件占用资源信息--I/O--CPU--Memory
        /// </summary>
        public void GetProgramInfo()
        {
            //获取当前进程对象
            Process cur = Process.GetCurrentProcess();
            PerformanceCounter curpcp = new PerformanceCounter("Process", "Working Set - Private", cur.ProcessName);
            PerformanceCounter curpc = new PerformanceCounter("Process", "Working Set", cur.ProcessName);
            PerformanceCounter curtime = new PerformanceCounter("Process", "% Processor Time", cur.ProcessName);
            PerformanceCounter ioReadcounter = new PerformanceCounter("Process", "IO Read Bytes/sec", cur.ProcessName);
            PerformanceCounter ioWritecounter = new PerformanceCounter("Process", "IO Write Bytes/sec", cur.ProcessName);

            //上次记录CPU的时间
            TimeSpan prevCpuTime = TimeSpan.Zero;
            //Sleep的时间间隔
            int interval = 1000;
            PerformanceCounter totalcpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            //  SystemInfo sys = new SystemInfo();
            const int KB_DIV = 1024;
            const int MB_DIV = 1024 * 1024;
            const int GB_DIV = 1024 * 1024 * 1024;
            while (true)
            {
                // Console.WriteLine("{0}:{1}  {2:N}MB CPU使用率：{3}%", cur.ProcessName, "私有工作集    ", curpcp.NextValue() / MB_DIV, curtime.NextValue() / Environment.ProcessorCount);
                //Console.WriteLine("读取硬盘：{0}----------写入硬盘：{1}", ioReadcounter.NextValue() , ioWritecounter.NextValue() / KB_DIV);

                Thread.Sleep(interval);
                CrossThreadOperationControl CrossDele_cpu = delegate ()
                {
                    Mainform.form1.textBoxCPU.Text = Math.Round(curtime.NextValue() / Environment.ProcessorCount, 2) + "%";
                };
                Mainform.form1.textBoxCPU.Invoke(CrossDele_cpu);
                CrossThreadOperationControl CrossDele_memory = delegate ()
                {
                    Mainform.form1.textBoxMemory.Text = Math.Round(curpcp.NextValue() / MB_DIV, 2) + "MB";
                };
                Mainform.form1.textBoxMemory.Invoke(CrossDele_memory);
                CrossThreadOperationControl CrossDele_io = delegate ()
                {
                    Mainform.form1.textBoxIO.Text = "读取:" + Math.Round(ioReadcounter.NextValue(), 2) + "B/s  " + "写入:" + Math.Round(ioWritecounter.NextValue() / KB_DIV, 2) + "KB/s";
                };
                Mainform.form1.textBoxIO.Invoke(CrossDele_io);
            }
        }




        private void Mainform_FormClosed(object sender, FormClosedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        //  string combox;



        /// <summary>
        /// 云台向上
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kPTZBtn_up_Click(object sender, EventArgs e)
        {
            HikvisionPTZControl hkPtzCtl = new HikvisionPTZControl();
            //根据选中的摄像头通道号跟IP地址进行预览
            string srtSql = "IP地址= '" + StaticData.g_CameraSelectNVRIP + "'";
            DataRow[] dr = StaticData.g_dtNVR.Select(srtSql);
            if (dr.Length > 0)
            {
                hkPtzCtl.PTZup(Convert.ToInt32(dr[0]["userid"]), StaticData.g_CameraSelectChannel);
            }
        }

        private void kPTZBtn_down_Click(object sender, EventArgs e)
        {
            HikvisionPTZControl hkPtzCtl = new HikvisionPTZControl();
            //根据选中的摄像头通道号跟IP地址进行预览
            string srtSql = "IP地址= '" + StaticData.g_CameraSelectNVRIP + "'";
            DataRow[] dr = StaticData.g_dtNVR.Select(srtSql);
            if (dr.Length > 0)
            {
                hkPtzCtl.PTZdown(Convert.ToInt32(dr[0]["userid"]), StaticData.g_CameraSelectChannel);
            }
        }

        private void kPTZBtn_left_Click(object sender, EventArgs e)
        {
            HikvisionPTZControl hkPtzCtl = new HikvisionPTZControl();
            //根据选中的摄像头通道号跟IP地址进行预览
            string srtSql = "IP地址= '" + StaticData.g_CameraSelectNVRIP + "'";
            DataRow[] dr = StaticData.g_dtNVR.Select(srtSql);
            if (dr.Length > 0)
            {
                hkPtzCtl.PTZleft(Convert.ToInt32(dr[0]["userid"]), StaticData.g_CameraSelectChannel);
            }
        }

        private void kPTZBtn_right_Click(object sender, EventArgs e)
        {
            HikvisionPTZControl hkPtzCtl = new HikvisionPTZControl();
            //根据选中的摄像头通道号跟IP地址进行预览
            string srtSql = "IP地址= '" + StaticData.g_CameraSelectNVRIP + "'";
            DataRow[] dr = StaticData.g_dtNVR.Select(srtSql);
            if (dr.Length > 0)
            {
                hkPtzCtl.PTZright(Convert.ToInt32(dr[0]["userid"]), StaticData.g_CameraSelectChannel);
            }
        }

        private void kBut_Zoomin_Click(object sender, EventArgs e)
        {
            HikvisionPTZControl hkPtzCtl = new HikvisionPTZControl();
            //根据选中的摄像头通道号跟IP地址进行预览
            string srtSql = "IP地址= '" + StaticData.g_CameraSelectNVRIP + "'";
            DataRow[] dr = StaticData.g_dtNVR.Select(srtSql);
            if (dr.Length > 0)
            {
                hkPtzCtl.PTZzoomin(Convert.ToInt32(dr[0]["userid"]), StaticData.g_CameraSelectChannel);
            }
        }

        private void kBut_ZoomOut_Click(object sender, EventArgs e)
        {
            HikvisionPTZControl hkPtzCtl = new HikvisionPTZControl();
            //根据选中的摄像头通道号跟IP地址进行预览
            string srtSql = "IP地址= '" + StaticData.g_CameraSelectNVRIP + "'";
            DataRow[] dr = StaticData.g_dtNVR.Select(srtSql);
            if (dr.Length > 0)
            {
                hkPtzCtl.PTZzoomout(Convert.ToInt32(dr[0]["userid"]), StaticData.g_CameraSelectChannel);
            }
        }

        /// <summary>
        /// 调整音量测试
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kBut_BroadcastTest_Click(object sender, EventArgs e)
        {
            //设置一个音量
            BroadcastUDP a = new BroadcastUDP();
            a.setBroadcastDeviceVolume("172.16.17.20", 20, 20);
        }

        private void kBut_EventClear_Click(object sender, EventArgs e)
        {
            kText_EventInfo.Clear();
        }

        private void kBut_OrderClear_Click(object sender, EventArgs e)
        {
            kText_Cmd.Clear();
        }
    }
}
