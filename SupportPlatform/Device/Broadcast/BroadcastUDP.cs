using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

using MySql.Data.MySqlClient;
using System.Data;

namespace SupportPlatform
{
    //UDP服务器，用于与终端交互
    //UDP客户端，用于与主机交互

    //主机定期向我发动在线信息，我发送应答命令

    //中心发送控制命令到终端，主机向我发送应答

    public class BroadcastUDP
    {
        MyJsonCtrl jscontrol = new MyJsonCtrl();
        private string currentClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        static IPEndPoint ipEndPoint;
        // key: broadcastDevice_bianhao; value: broadcastDevice_name 
        private Dictionary<Int16, string> m_broadcastDeviceList = new Dictionary<Int16, string>();
       
        // key: broadcastDevice_bianhao; value: broadcastDevice_onlineLastTime 
        private Dictionary<Int16, DateTime> m_broadcastDeviceOnlineList = new Dictionary<Int16, DateTime>();
        /// <summary>
        /// 用于接收从终端发送过来的数据
        /// </summary>
        public void BroadServerReceive()
        {
            // 获取本地DB 广播列表
            getDbBroadcastDeviceList();

            //构造函数直接监听本地UDP端口，此端口从数据库获取。。。zzt
            StaticData.broadcastUDPClient = new UdpClient(Convert.ToInt32(StaticData.g_strBroadcastListenPort));
            ipEndPoint = new IPEndPoint(new IPAddress(0), Convert.ToInt32(StaticData.g_strBroadcastListenPort));
            StaticData.broadcastUDPClient.Client.Blocking = false;//设置为非阻塞模式

            /*
            * 如果从未给远程广播server发过数据包，即使设置过本地IP，但仍然无法收到返回包
            * 因此本地向server发送获取广播列表的请求,将本地IP加入到远程server的广播列表中
            */
            getBroadcastDeviceList();

            Int16 nDeviceTotalRecord;
            Int16 nDeviceNowRecord;
            Int16 nDeviceId;
            byte byDeviceType;
            string strIp;
            string strDeviceName;
            string strStatus = "";
            byte byVolume;
            byte byChannel;
            int revType = 0;   // 接收数据的类型
            while (true)
            {

                int buffSizeCurrent;
                buffSizeCurrent = StaticData.broadcastUDPClient.Client.Available;//取得缓冲区当前的数据的个数
                if (buffSizeCurrent > 0)
                {
                    //收到的消息
                    byte[] data = StaticData.broadcastUDPClient.Receive(ref ipEndPoint);
                    //对收到的消息做处理
                    switch (data[1])
                    {
                        //1是设备状态信息
                        case 1:
                            revType = 1;

                            nDeviceId = Convert.ToInt16((data[3].ToString("X2") + data[2].ToString("X2")), 16);

                            // 针对非db中广播进行剔除
                            if (!m_broadcastDeviceList.ContainsKey(nDeviceId))
                            {
                                continue;
                            }

                            byDeviceType = data[4]; // 设备类型，暂时不做处理

                            //0：空闲；1：消防广播；2：SD定时；3：PC定时；4：寻呼；5：电话；6：对讲；7：频道；8：下载；9：点播；10：网络录音；11：SD播放；12：本地收音；13：本地线路；14：空闲；15：功放保护；16：即时广播；
                            switch (data[6])
                            {
                                case 0:
                                    strStatus = "空闲";
                                    break;
                                case 1:
                                    strStatus = "消防广播";
                                    break;
                                case 2:
                                    strStatus = "SD定时";
                                    break;
                                case 3:
                                    strStatus = "PC定时";
                                    break;
                                case 4:
                                    strStatus = "寻呼";
                                    break;
                                case 5:
                                    strStatus = "电话";
                                    break;
                                case 6:
                                    strStatus = "对讲";
                                    break;
                                case 7:
                                    strStatus = "频道";
                                    break;
                                case 8:
                                    strStatus = "下载";
                                    break;
                                case 9:
                                    strStatus = "点播";
                                    break;
                                case 10:
                                    strStatus = "网络录音";
                                    break;
                                case 11:
                                    strStatus = "SD播放";
                                    break;
                                case 12:
                                    strStatus = "本地收音";
                                    break;
                                case 13:
                                    strStatus = "本地线路";
                                    break;
                                case 14:
                                    strStatus = "空闲";
                                    break;
                                case 15:
                                    strStatus = "功放保护";
                                    break;
                                case 16:
                                    strStatus = "即时广播";
                                    break;
                                    //  0：空闲；1：消防广播；2：SD定时；3：PC定时；4：寻呼；5：电话；6：对讲；7：频道；8：下载；9：点播；10：网络录音；11：SD播放；12：本地收音；13：本地线路；14：空闲；15：功放保护；16：即时广播
                            }

                            byVolume = data[19];
                            //软件显示实际频道号 = 状态数据中频道号 + 1
                            byChannel = byte.Parse((Convert.ToInt16(data[20])+1).ToString());
                            
                            //此信息是定期广播的，收到后根据里面的DeviceId进行搜索datatable，然后更新状态和音量   
                            DataRow[] dr = StaticData.g_dtBroadcastDevice.Select("编号='" + (Int32)nDeviceId + "'");


                            //有趣的现象， 当 dr中的值发生动态变化时，界面的值也动态变化-> select后的dataRow存在绑定的关系
                            if (dr.Length > 0)
                            {
                                dr[0]["连通"] = "连通";
                                dr[0]["状态"] = strStatus;
                                dr[0]["频道"] = byChannel;
                                dr[0]["音量"] = byVolume;
                                dr[0]["时间"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                                // 进行设备breakdown 判断
                                judgeBroadcastDeviceBreakdown(nDeviceId);
                            }

                            // 将得到的新状态更新到数据库中去 
                            if (StaticData.g_msBroadcastgDBC.State == System.Data.ConnectionState.Closed)
                            {
                                StaticData.g_msBroadcastgDBC.Open();
                            }
                            string strSql = "UPDATE device_broadcast SET device_liantong='" + dr[0]["连通"] + "' , device_status='" + dr[0]["状态"] + "'  , device_channel='" + dr[0]["频道"] + "'  , device_volume='" + dr[0]["音量"] + "', device_time='" + dr[0]["时间"] + "' WHERE device_bianhao='" + nDeviceId + "';";
                            MySqlCommand cmd = new MySqlCommand(strSql, StaticData.g_msBroadcastgDBC);

                            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;                  
                            StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);
   
                            StaticData.g_msBroadcastgDBC.Close();
                            break;
                        //2是获取系统设备列表 ；设备连通或者不连通都在设备列表中显示，目前来看没什么用。
                        case 2:
                            revType = 2;

                            nDeviceTotalRecord = Convert.ToInt16((data[3].ToString("X2") + data[2].ToString("X2")), 16);
                            nDeviceNowRecord = Convert.ToInt16((data[5].ToString("X2") + data[4].ToString("X2")), 16);
                            nDeviceId = Convert.ToInt16((data[7].ToString("X2") + data[6].ToString("X2")), 16);          
                            byDeviceType = data[8];
                            strIp = data[9].ToString() + "." + data[10].ToString() + "." + data[11].ToString() + "." + data[12].ToString();
                            strDeviceName = Encoding.Unicode.GetString(data, 15, 6);//这里有大小端问题，算了，整体不动了。
                            break;
                    }

                }
                else   // 没有拿到广播包
                {
                   // 针对没有数据包情况， 将所有广播报警
                   judgeBroadcastDeviceBreakdown(0);
                }
             
                // 设置通过list获取到的包，线程不睡， 方便更快响应
                if (revType == 1)
                {
                    Thread.Sleep(5 * 1000);   // 此处不根据输入Sleep阈值， 因为包interval 为30s
                }

            }

        }

        /// <summary>
        /// 向终端发送数据
        /// </summary>
        /// <param name="data"></param>
        ///<param name="_ipendpoint">终端的ip端口</param>
        public void broadudpSend(byte[] data, IPEndPoint _ipendpoint)
        {
            StaticData.broadcastUDPClient.Send(data, data.Length, _ipendpoint);
        }

        /// <summary>
        /// 发送请求，获取广播列表，响应在receive中 
        /// 可用于广播设备在线离线判断
        /// </summary>
        public void getBroadcastDeviceList()
        {
            IPEndPoint epBroadcastServer = new IPEndPoint(IPAddress.Parse(StaticData.g_strBroadcastServerIP), Convert.ToInt32(StaticData.g_strBroadcastServerPort));
            byte[] byTitle = { 240, 2 };
            string[] strRecvIp = StaticData.g_strLocalIP.Split('.');
            byte[] byRecvIP = { byte.Parse(strRecvIp[0]), byte.Parse(strRecvIp[1]), byte.Parse(strRecvIp[2]), byte.Parse(strRecvIp[3]) };
            byte[] byRecvPort = BitConverter.GetBytes(Convert.ToInt16(StaticData.g_strBroadcastListenPort));
            byte[] byEnd = { 0, 0, 0, 0, 0, 255 };
            byte[] byGetSystemListCmd = new byte[byTitle.Length + byRecvIP.Length + byRecvPort.Length + byEnd.Length];
            byTitle.CopyTo(byGetSystemListCmd, 0);
            byRecvIP.CopyTo(byGetSystemListCmd, byTitle.Length);
            byRecvPort.CopyTo(byGetSystemListCmd, byTitle.Length + byRecvIP.Length);
            byEnd.CopyTo(byGetSystemListCmd, byTitle.Length + byRecvIP.Length + byRecvPort.Length);
            broadudpSend(byGetSystemListCmd, epBroadcastServer);
        }

        /// <summary>
        /// 调整设备总音量
        /// </summary>
        /// <param name="strDeviceIp"></param>//设备ip地址
        /// <param name="nDeviceId"></param>设备ID
        /// <param name="nVolume"></param>设备音量
        public void setBroadcastDeviceVolume(string strDeviceIp, Int16 nDeviceId, byte nVolume)
        {
            IPEndPoint epBroadcastServer = new IPEndPoint(IPAddress.Parse(StaticData.g_strBroadcastServerIP), Convert.ToInt32(StaticData.g_strBroadcastServerPort));

            byte[] byTitle = { 240, 3 };//3代表调节音量
            string[] strdIp = strDeviceIp.Split('.');
            byte[] byDeviceIP = { byte.Parse(strdIp[0]), byte.Parse(strdIp[1]), byte.Parse(strdIp[2]), byte.Parse(strdIp[3]) };//设备ip地址
            byte[] byDeviceId = { Convert.ToByte(nDeviceId & 0x00ff), Convert.ToByte(nDeviceId & (0xff << 8)) };
            byte[] byEnd = { nVolume, 0, 0, 0, 0, 255 };
            byte[] byGetSystemListCmd = new byte[byTitle.Length + byDeviceIP.Length + byDeviceId.Length + byEnd.Length];
            byTitle.CopyTo(byGetSystemListCmd, 0);
            byDeviceIP.CopyTo(byGetSystemListCmd, byTitle.Length);
            byDeviceId.CopyTo(byGetSystemListCmd, byTitle.Length + byDeviceIP.Length);
            byEnd.CopyTo(byGetSystemListCmd, byTitle.Length + byDeviceIP.Length + byDeviceId.Length);
            broadudpSend(byGetSystemListCmd, epBroadcastServer);
        }

        /// <summary>
        /// 强制关闭设备
        /// </summary>
        /// <param name="strDeviceIp"></param>//设备ip地址
        /// <param name="nDeviceId"></param>//设备id
        /// <param name="nMode"></param>//此参数为模式参数，1表示强制关闭，2表示设备复位，取值范围1-2；
        public void closeBroadcastDevice(string strDeviceIp, Int16 nDeviceId, byte nMode)
        {
            IPEndPoint epBroadcastServer = new IPEndPoint(IPAddress.Parse(StaticData.g_strBroadcastServerIP), Convert.ToInt32(StaticData.g_strBroadcastServerPort));

            byte[] byTitle = { 240, 4 };//4代表关闭设备
            string[] strdIp = strDeviceIp.Split('.');
            byte[] byDeviceIP = { byte.Parse(strdIp[0]), byte.Parse(strdIp[1]), byte.Parse(strdIp[2]), byte.Parse(strdIp[3]) };//设备ip地址
            byte[] byDeviceId = { Convert.ToByte(nDeviceId & 0x00ff), Convert.ToByte(nDeviceId & (0xff << 8)) };
            byte[] byEnd = { nMode, 0, 0, 0, 0, 255 };

            byte[] byGetSystemListCmd = new byte[byTitle.Length + byDeviceIP.Length + byDeviceId.Length + byEnd.Length];
            byTitle.CopyTo(byGetSystemListCmd, 0);
            byDeviceIP.CopyTo(byGetSystemListCmd, byTitle.Length);
            byDeviceId.CopyTo(byGetSystemListCmd, byTitle.Length + byDeviceIP.Length);
            byEnd.CopyTo(byGetSystemListCmd, byTitle.Length + byDeviceIP.Length + byDeviceId.Length);
            broadudpSend(byGetSystemListCmd, epBroadcastServer);
        }

        /// <summary>
        /// 切换设备频道
        /// </summary>
        /// <param name="strDeviceIp"></param>//设备ip地址
        /// <param name="nDeviceId"></param>//设备ID
        /// <param name="nChannel"></param>//长度为1个字节,需要切换的频道，取值范围0-30，0表示关闭当前设备频道播放；
        public void changeBroadcastDeviceChannel(string strDeviceIp, Int16 nDeviceId, byte nChannel)
        {
            IPEndPoint epBroadcastServer = new IPEndPoint(IPAddress.Parse(StaticData.g_strBroadcastServerIP), Convert.ToInt32(StaticData.g_strBroadcastServerPort));
            byte[] byTitle = { 240, 5 };//5代表切换频道
            string[] strdIp = strDeviceIp.Split('.');
            byte[] byDeviceIP = { byte.Parse(strdIp[0]), byte.Parse(strdIp[1]), byte.Parse(strdIp[2]), byte.Parse(strdIp[3]) };//设备ip地址
            byte[] byDeviceId = { Convert.ToByte(nDeviceId & 0x00ff), Convert.ToByte(nDeviceId & (0xff << 8)) };
            byte[] byEnd = { nChannel, 0, 0, 0, 0, 255 };
            byte[] byGetSystemListCmd = new byte[byTitle.Length + byDeviceIP.Length + byDeviceId.Length + byEnd.Length];
            byTitle.CopyTo(byGetSystemListCmd, 0);
            byDeviceIP.CopyTo(byGetSystemListCmd, byTitle.Length);
            byDeviceId.CopyTo(byGetSystemListCmd, byTitle.Length + byDeviceIP.Length);
            byEnd.CopyTo(byGetSystemListCmd, byTitle.Length + byDeviceIP.Length + byDeviceId.Length);
            broadudpSend(byGetSystemListCmd, epBroadcastServer);
        }

        /// <summary>
        ///  获取本地DB 广播列表
        ///  用于剔除广播包中郊野等无效包
        /// </summary>
        public void getDbBroadcastDeviceList()
        {
            if (StaticData.g_msBroadcastgDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msBroadcastgDBC.Open();
            }
            string strCMD = "select device_bianhao,device_name from device_broadcast;";
            MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msBroadcastgDBC);
            MySqlDataReader sqlReader = null;
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;

            if (StaticUtils.DBReaderOperationException(ref cmd, ref sqlReader, currentClassName, currentMethodName))
            {
            
                while (sqlReader.Read())
                {
                    //name，password
                    string[] str = { sqlReader.GetString(0), sqlReader.GetString(1) };
                    //添加进字典
                    m_broadcastDeviceList.Add(Int16.Parse(str[0]), str[1]);

                    // 初始化用于判断设备是否故障的dict
                    m_broadcastDeviceOnlineList.Add(Int16.Parse(str[0]), DateTime.Now);
                }
                sqlReader.Close();
            }

            StaticData.g_msBroadcastgDBC.Close();
        }

        /// <summary>
        /// 掉线进行插入表逻辑判断->
        /// 判断如果某个设备不进行广播，则在十分钟后进行进一步逻辑判断->
        /// 1. 如果这个设备record 已经在db_event中& status 为‘未处理’，则不做处理;
        /// 2. 如果这个设备record 已经在db_event中& status 为‘已处理’，则insert;
        /// 3. 如果这个设备record不在db_event中， 则insert.
        /// </summary>
        public void judgeBroadcastDeviceBreakdown(Int16 BroadcastDeviceId)
        {
            if (BroadcastDeviceId != 0) {
                m_broadcastDeviceOnlineList[BroadcastDeviceId] = DateTime.Now;
            }       

            foreach (var item in m_broadcastDeviceOnlineList)
            {
                DataRow[] dr = StaticData.g_dtBroadcastDevice.Select("编号='" + (Int32)item.Key + "'");
                string theone = dr[0]["theone"].ToString();

                // 计算两个dateTime 的时间戳，单位s
                long timeSpan= StaticUtils.GetCurrentTimeUnix(DateTime.Now) - StaticUtils.GetCurrentTimeUnix(item.Value);

                // 未上报时长 > 阈值
                if (timeSpan > StaticData.g_intBroadcastBreakdownTime)
                {
                    if (StaticData.g_msBroadcastgDBC.State == System.Data.ConnectionState.Closed)
                    {
                        StaticData.g_msBroadcastgDBC.Open();
                    }
                    string strCMD = "select * from event_assemble where target_theone='" + theone + "';";
                    MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msBroadcastgDBC);
                    MySqlDataReader sqlReader =null;
                    string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                    if (StaticUtils.DBReaderOperationException(ref cmd, ref sqlReader, currentClassName, currentMethodName))
                    {
                        if (sqlReader.Read())
                        {  // record 在设备表中
                            if (sqlReader["event_status"].ToString().Contains("已读"))
                            {
                                sqlReader.Close();
                                StaticData.g_msBroadcastgDBC.Close();
                                insertBrokenRecordToEventDb(item.Key, theone); // 将record作为新的event record进行插入 
                            }
                            else
                            {
                                sqlReader.Close();
                                StaticData.g_msBroadcastgDBC.Close();
                                // record 状态未处理 ，更新DB device_broadcast表 和 界面
                                updateBreakdownInfor(item.Key);
                            }
                        }
                        else
                        {
                            sqlReader.Close();
                            StaticData.g_msBroadcastgDBC.Close();
                            insertBrokenRecordToEventDb(item.Key, theone);
                        }
                    }
                }

                // 未上报时长 <= 阈值时，就算event_assemble中显示未处理，仍更新状态为连接

            }
        }

        /// <summary>
        /// 将record作为新的event record插入event_assemble & event_broadcast 
        /// 关于 广播故障类型: 掉线
        /// strLevel  ： 重要情况
        /// </summary>
        public void insertBrokenRecordToEventDb(Int16 BroadcastDeviceId, String theone)
        {
            if (StaticData.g_msBroadcastgDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msBroadcastgDBC.Open();
            }
            //生成eventtheon
            string strEventTheOne = Guid.NewGuid().ToString();
            //事件等级判断
            string strLevel = "重要情况";
            DataRow[] dr = StaticData.g_dtBroadcastDevice.Select("编号='" + (Int32)BroadcastDeviceId + "'");
            string strBroadcastPosition = dr[0]["坐标"].ToString();
            // 将record作为新的event record进行插入 
            DataRow[] dr_abnormal_super = StaticData.g_dtAbnormalInfor.Select("故障父类名称='event_broadcast'");
            DataRow[] dr_abnormal_sub=StaticData.g_dtAbnormalInfor.Select("父ID='" + dr_abnormal_super[0]["ID"] + "'");
            string event_name = dr_abnormal_sub[0]["故障子类名称"].ToString();
            string strInsert_assemble = "INSERT INTO event_assemble (event_name,event_theone,event_type,start_time,end_time,event_status,position,event_level,target_theone) values" +
                "('" + event_name + "','" + strEventTheOne + "','event_broadcast','" + DateTime.Now.ToString().Replace('/', '-') + "','" + DateTime.Now.ToString().Replace('/', '-') + "','未读','" +
                strBroadcastPosition + "','" + strLevel + "','" + theone + "');";

            string strInsert_broadcast = "INSERT INTO event_broadcast (event_theone,device_event_type,device_theone) values" +
    "('" + strEventTheOne + "','" + event_name + "','" + theone + "');";
            MySqlCommand insertAssembleCmd = new MySqlCommand(strInsert_assemble, StaticData.g_msBroadcastgDBC);
            MySqlCommand insertBroadcastCmd = new MySqlCommand(strInsert_broadcast, StaticData.g_msBroadcastgDBC);
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;           
            StaticUtils.DBNonQueryOperationException(ref insertAssembleCmd, currentClassName, currentMethodName);
            StaticUtils.DBNonQueryOperationException(ref insertBroadcastCmd, currentClassName, currentMethodName);

            // 向web 上报事件
            sendMsgToWebSocketClients(strEventTheOne, "报警时间：" + DateTime.Now.ToString("yyyy/M/d HH:mm:ss") + "   报警内容：编号" + BroadcastDeviceId + "-广播未连通报警" + "   报警类型：广播未连通报警\n");

            updateBreakdownInfor(BroadcastDeviceId);
        }

        /// <summary>
        /// 更新故障信息到DB device_broadcast 中；刷新界面
        /// </summary>
        public void updateBreakdownInfor(Int16 BroadcastDeviceId)
        {
            // 将得到的新状态更新到数据库中去 
            if (StaticData.g_msBroadcastgDBC.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msBroadcastgDBC.Open();
            }
            string strSql = "UPDATE device_broadcast SET device_liantong='未连通',device_time='" + DateTime.Now.ToString().Replace('/', '-') + "' WHERE device_bianhao='" + BroadcastDeviceId + "';";
            MySqlCommand cmd = new MySqlCommand(strSql, StaticData.g_msBroadcastgDBC);
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;           
            StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);

            StaticData.g_msBroadcastgDBC.Close();
            //刷新 form1界面上的 GridListView
            DataRow[] dr = StaticData.g_dtBroadcastDevice.Select("编号='" + (Int32)BroadcastDeviceId + "'");
            dr[0]["连通"] = "未连通";
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

        private delegate void CrossThreadOperationControl();

    }
 }