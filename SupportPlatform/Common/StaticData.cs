using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Net.Sockets;

namespace SupportPlatform
{
 public static  class StaticData
    {

        /************************************    下面是系统及第三方平台相关配置    ************************************/
        #region

        public static DataTable g_dtComputerRoomDevice = new DataTable("aaaa");

        /// <summary>
        /// 整体配置
        /// </summary>
        //第三方系统轮询时间
        public static int g_nSystemPollFreq ;
        //本机Ip；测试期间，将值赋值为vpn地址   
        public static string g_strLocalIP ;
        //保存报警数量
        public static int g_inAlarmNum = 0;
        //WebSocket监听端口
        public static string g_strWebSocketPort;

        /// <summary>
        /// 广播相关配置
        /// </summary>
        //广播远程ip地址
        public static string g_strBroadcastServerIP;
        //广播远程ip地址监听端口
        public static string g_strBroadcastServerPort;
        //广播本地ip地址监听端口
        public static string g_strBroadcastListenPort;
        //广播故障Time阈值   unit : s
        public static int g_intBroadcastBreakdownTime=300;
        //保存广播的udp对象，因为多线程可能都需要控制，这里暂时高程全局的，虽然会冲突，但是如果不冲突，那同时对设备下命令，设备那边就会冲突
        public static UdpClient broadcastUDPClient;


        /// <summary>
        /// 机房相关配置
        /// </summary>
        public static string g_strMachineRoomUser;//远端数据库用户名
        public static string g_strMachineRoomPassword;//远端数据库用户密码
        public static string g_strMachineRoomPort;//远端数据库端口
        public static string g_strMachineRommIP;//远端数据库IP
        public static string g_strMachineRommDBname;//远端数据库名称
        //同一机房相同报警Interval   unit : s
        public static int g_intMachineRoomWarningIntervalTime;
        //机房相关独立配置数据库
        public static Dictionary<string, int> g_dictMachineRoomConfig = null;

        /// <summary>
        /// 摄像头相关配置
        /// </summary>       
        //音乐文件列表数据库
        public static DataTable g_dtMusic = new DataTable("MusicList");
        //存储点击的摄像头通道号
        public static int g_CameraSelectChannel = -1;
        //存储点击的摄像头NVRIP地址
        public static string g_CameraSelectNVRIP = "";
        //保存开始报警信息
        public static Dictionary<string, string> CameraAlarmInfo = new Dictionary<string, string>();
        //摄像头类型
        public static string g_strCameraType;

        /// <summary>
        /// 闸机相关配置
        /// </summary>
        //闸机人数过多预警
        public static int g_intGateEarlyWarning;
        //闸机人数过多告警
        public static int g_intGateWarning ;
        //同一闸机相同报警Interval   unit : s
        public static int g_intGateWarningIntervalTime ;
        //闸机请求的url
        public static string g_strGateUrl="http://172.16.18.1/open/TicketData/CountTerminalDetail?startTime=8:00&endTime=23:00&CheckDate=";
        //闸机相关独立配置数据库
        public static Dictionary<string, int> g_dictGateConfig = null;

        /// <summary>
        /// 停车场相关配置
        /// </summary>
        //停车场请求的url
        public static string g_strParkingUrl = "http://www.gflpark.com/webInterface/screen/sendNumScreen";
        //同一停车场相同报警Interval   unit : s
        public static int g_intParkWarningIntervalTime;
        //车位使用过多预警  考虑到不同车位的容量不同，此处使用百分比
        public static double g_intParkEarlyWarning ;
        //车位使用过多告警
        public static double g_intParkWarning;
        //停车场相关独立配置数据库
        public static Dictionary<string, double> g_dictParkConfig = null;

        /// <summary>
        /// Wifi相关配置
        /// </summary>
        public static string g_strWifiUrl = "http://10.1.90.252:8080/imcrs/wlan/apInfo/queryApBasicInfo";
        #endregion


        /************************************    下面是数据库相关配置    ************************************/
        #region
        /*   
         *
         * 下面是整体数据库配置信息 
         * 
         * 
         */
        public static string g_strServerIP;//数据库ip地址
        public static string g_strDBname;//数据库名称
        public static string g_strDBport;//数据库端口
        public static string g_strDBUserName;//数据库用户名
        public static string g_strDBUserPassword;//数据库用户密码


        /*
         * 
         * 
         * 静态数据库连接，用于特定数据库连接
         * 
         * 
         */
        public static  MySqlConnection g_msVideCallBackConn;//摄像头报警时间回调数据库连接

        public static MySqlConnection g_msVideQueryConn;//摄像头轮询连接

        public static MySqlConnection g_msParkingDBC;// 停车场数据库连接

        public static MySqlConnection g_msGateDBC; //闸机数据库连接

        public static MySqlConnection g_msMachineRoomDBC; //机房数据库连接

        public static MySqlConnection g_msMachineRoomRemoteDBC; //机房Remote数据库连接

        public static MySqlConnection g_msBroadcastgDBC;//广播数据库连接

        public static MySqlConnection g_msWifiDBC;//Wifi数据库连接

        public static MySqlConnection g_msEventAssembleDBC;//异常事件数据库连接


        /// <summary>
        ///保存所有的数据库连接
        /// </summary> 
        //数据库连接
        public static MySqlConnection g_msConn;
        //数据库连接字段
        public static MySqlConnectionStringBuilder g_msBuilder;
        //异常信息数据库
        public static DataTable g_dtAbnormalInfor = new DataTable("AbnormalInforList");
        //nvr数据库
        public static DataTable g_dtNVR = new DataTable("NVRList");
        //摄像头数据库
        public static DataTable g_dtCamera = new DataTable("CameraList");
        //停车场数据库
        public static DataTable g_dtParking = new DataTable("ParkingList");
        //闸机数据库
        public static DataTable g_dtGate = new DataTable("GateList");
        //广播设备数据库
        public static DataTable g_dtBroadcastDevice = new DataTable("g_dtBroadcastDevice");
        //机房数据库
        public static DataTable g_dtMachineRoom = new DataTable("g_dtMachineRoom");
        //Event_assemble数据库
        public static DataTable g_dtEventAssemble = new DataTable("g_dtEventAssemble");
        //Wifi数据库
        public static DataTable g_dtWifi = new DataTable("g_dtWifi");
        #endregion
    }
}
