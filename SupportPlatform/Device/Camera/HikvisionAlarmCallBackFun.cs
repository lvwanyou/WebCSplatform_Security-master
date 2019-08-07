using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Json;
using System.Data;
using System.IO;

namespace SupportPlatform
{
    class HikvisionAlarmCallBackFun : Hikvision
    {
        private string currentClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        //数据库连接
        MyJsonCtrl jscontrol = new MyJsonCtrl();
        public static CHCNetSDK.MSGCallBack m_falarmData = null;
        private delegate void CrossThreadOperationControl();
        private string strTemporaryMessage = "";

        /// <summary>
        /// 报警回调函数
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="channel"></param>
        public override void AlarmCallBackFun(int userID, int channel)
        {
            try
            {
                IntPtr pUser = IntPtr.Zero;
                m_falarmData = new CHCNetSDK.MSGCallBack(MsgCallback);   //注册回调函数
                bool test = CHCNetSDK.NET_DVR_SetDVRMessageCallBack_V30(m_falarmData, pUser);  //海康回调SDK
                GC.KeepAlive(m_falarmData);
                GC.KeepAlive(test);
                if (!test)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    MessageBox.Show("Error" + iLastErr);
                }
                else
                {
                 //Console.WriteLine("启动报警成功");
                }
                CHCNetSDK.NET_DVR_SETUPALARM_PARAM struAlarmParam = new CHCNetSDK.NET_DVR_SETUPALARM_PARAM();  //设置布防SDK
                struAlarmParam.dwSize = (uint)Marshal.SizeOf(struAlarmParam);
                struAlarmParam.byLevel = 1; //0- 一级布防,1- 二级布防
                CHCNetSDK.NET_DVR_SetupAlarmChan_V41(userID, ref struAlarmParam);
            }
            catch (Exception e)
            {
                StaticUtils.ShowEventMsg("HikvisionAlarmCallBackFun.class-AlarmCallBackFun : 报警回调函数出现异常！\n");
                Console.WriteLine(e);
            }
        }

        private void MsgCallback(int lCommand, ref CHCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {

            //通过lCommand来判断接收到的报警信息类型，不同的lCommand对应不同的pAlarmInfo内容
            switch (lCommand)
            {
                case CHCNetSDK.COMM_ALARM: //(DS-8000老设备)移动侦测、视频丢失、遮挡、IO信号量等报警信息
                    ProcessCommAlarm(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK.COMM_ALARM_V30://移动侦测、视频丢失、遮挡、IO信号量等报警信息
                    ProcessCommAlarm_V30(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                case CHCNetSDK.COMM_ALARM_RULE://进出区域、入侵、徘徊、人员聚集等行为分析报警信息
                    ProcessCommAlarm_RULE(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                    break;
                //case CHCNetSDK.COMM_UPLOAD_PLATE_RESULT://交通抓拍结果上传(老报警信息类型)
                //    ProcessCommAlarm_Plate(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                //case CHCNetSDK.COMM_ITS_PLATE_RESULT://交通抓拍结果上传(新报警信息类型)
                //    ProcessCommAlarm_ITSPlate(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                //case CHCNetSDK.COMM_ALARM_PDC://客流量统计报警信息
                //    ProcessCommAlarm_PDC(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                //case CHCNetSDK.COMM_ITS_PARK_VEHICLE://客流量统计报警信息
                //    ProcessCommAlarm_PARK(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                //case CHCNetSDK.COMM_DIAGNOSIS_UPLOAD://VQD报警信息
                //    ProcessCommAlarm_VQD(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                //case CHCNetSDK.COMM_UPLOAD_FACESNAP_RESULT://人脸抓拍结果信息
                //    ProcessCommAlarm_FaceSnap(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                //case CHCNetSDK.COMM_SNAP_MATCH_ALARM://人脸比对结果信息
                //    ProcessCommAlarm_FaceMatch(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                //case CHCNetSDK.COMM_ALARM_FACE_DETECTION://人脸侦测报警信息
                //    ProcessCommAlarm_FaceDetect(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                //case CHCNetSDK.COMM_ALARMHOST_CID_ALARM://报警主机CID报警上传
                //    ProcessCommAlarm_CIDAlarm(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                //case CHCNetSDK.COMM_ALARM_ACS://门禁主机报警上传
                //    ProcessCommAlarm_AcsAlarm(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                //case CHCNetSDK.COMM_ID_INFO_ALARM://身份证刷卡信息上传
                //    ProcessCommAlarm_IDInfoAlarm(ref pAlarmer, pAlarmInfo, dwBufLen, pUser);
                //    break;
                default:
                    {
                        //报警设备IP地址
                        string strIP = pAlarmer.sDeviceIP;

                        //报警信息类型
                        string stringAlarm = "报警上传，信息类型：" + lCommand;

                        if (true)
                        {
                            string[] paras = new string[3];
                            paras[0] = DateTime.Now.ToString(); //当前PC系统时间
                            paras[1] = strIP;
                            paras[2] = stringAlarm;
                //            Console.WriteLine (paras[0] + " " + paras[1] + " " + paras[2]);
                        }
                        else
                        {
                            //创建该控件的主线程直接更新信息列表 
                            //UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
                        }
                    }
                    break;
            }
        }
        private void ProcessCommAlarm(ref CHCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK.NET_DVR_ALARMINFO struAlarmInfo = new CHCNetSDK.NET_DVR_ALARMINFO();

            struAlarmInfo = (CHCNetSDK.NET_DVR_ALARMINFO)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK.NET_DVR_ALARMINFO));

            string strIP = pAlarmer.sDeviceIP;
            string stringAlarm = "";
            int i = 0;

            switch (struAlarmInfo.dwAlarmType)
            {
                case 0:
                    stringAlarm = "信号量报警，报警报警输入口：" + struAlarmInfo.dwAlarmInputNumber + "，触发录像通道：";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM; i++)
                    {
                        if (struAlarmInfo.dwAlarmRelateChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 1:
                    stringAlarm = "硬盘满，报警硬盘号：";
                    for (i = 0; i < CHCNetSDK.MAX_DISKNUM; i++)
                    {
                        if (struAlarmInfo.dwDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 2:
                    stringAlarm = "信号丢失，报警通道：";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM; i++)
                    {
                        if (struAlarmInfo.dwChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 3:
                    stringAlarm = "移动侦测，报警通道：";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM; i++)
                    {
                        if (struAlarmInfo.dwChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 4:
                    stringAlarm = "硬盘未格式化，报警硬盘号：";
                    for (i = 0; i < CHCNetSDK.MAX_DISKNUM; i++)
                    {
                        if (struAlarmInfo.dwDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 5:
                    stringAlarm = "读写硬盘出错，报警硬盘号：";
                    for (i = 0; i < CHCNetSDK.MAX_DISKNUM; i++)
                    {
                        if (struAlarmInfo.dwDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 6:
                    stringAlarm = "遮挡报警，报警通道：";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM; i++)
                    {
                        if (struAlarmInfo.dwChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 7:
                    stringAlarm = "制式不匹配，报警通道";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM; i++)
                    {
                        if (struAlarmInfo.dwChannel[i] == 1)
                        {
                            stringAlarm += (i + 1) + " \\ ";
                        }
                    }
                    break;
                case 8:
                    stringAlarm = "非法访问";
                    break;
                default:
                    stringAlarm = "其他未知报警信息";
                    break;
            }

            if (true)
            {
                string[] paras = new string[3];
                paras[0] = DateTime.Now.ToString(); //当前PC系统时间
                paras[1] = strIP;
                paras[2] = stringAlarm;
    //            Console.WriteLine(paras[0] + " " + paras[1] + " " + paras[2]);
            }
            else
            {
                //创建该控件的主线程直接更新信息列表 
                // UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            }
        }
        private void ProcessCommAlarm_V30(ref CHCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            CHCNetSDK.NET_DVR_ALARMINFO_V30 struAlarmInfoV30 = new CHCNetSDK.NET_DVR_ALARMINFO_V30();

            struAlarmInfoV30 = (CHCNetSDK.NET_DVR_ALARMINFO_V30)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK.NET_DVR_ALARMINFO_V30));
            
            string strIP = pAlarmer.sDeviceIP;
            string stringAlarm = "";
            int i;

            switch (struAlarmInfoV30.dwAlarmType)
            {
                case 0:
                    stringAlarm = "信号量报警，报警报警输入口：" + struAlarmInfoV30.dwAlarmInputNumber + "，触发录像通道：";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byAlarmRelateChannel[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 1:
                    stringAlarm = "硬盘满,";
                    for (i = 0; i < CHCNetSDK.MAX_DISKNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 2:
                    stringAlarm = "信号丢失,";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 3:
                    stringAlarm = "移动侦测,";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM_V30; i++)   //MAX_CHANNUM_V30
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 4:
                    stringAlarm = "硬盘未格式化,";
                    for (i = 0; i < CHCNetSDK.MAX_DISKNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 5:
                    stringAlarm = "读写硬盘出错,";
                    for (i = 0; i < CHCNetSDK.MAX_DISKNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byDiskNumber[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 6:
                    stringAlarm = "遮挡报警,";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 7:
                    stringAlarm = "制式不匹配,";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 8:
                    stringAlarm = "非法访问,";
                    break;
                case 9:
                    stringAlarm = "视频信号异常,";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 10:
                    stringAlarm = "录像/抓图异常,";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 11:
                    stringAlarm = "智能场景变化,";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 12:
                    stringAlarm = "阵列异常,";
                    break;
                case 13:
                    stringAlarm = "前端/录像分辨率不匹配,";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                case 15:
                    stringAlarm = "智能侦测,";
                    for (i = 0; i < CHCNetSDK.MAX_CHANNUM_V30; i++)
                    {
                        if (struAlarmInfoV30.byChannel[i] == 1)
                        {
                            stringAlarm += (i + 1 - 32) + ",";
                        }
                    }
                    break;
                default:
                    stringAlarm = "其他未知报警信息,";
                    break;
            }
            //在这里为报警信息
            if (true)
            {
                string[] paras = new string[3];
                paras[0] = DateTime.Now.ToString(); //当前PC系统时间
                paras[1] = strIP;//nvrip地址
                paras[2] = stringAlarm;
                string[] alarmMsg = paras[2].Split(',');//报警类型和通道号
    //            Console.WriteLine(paras[0] + "," + paras[1] + "," + paras[2] + "\n");               
                //根据数据库找到theone、摄像头名称等内容
                string theone = "0";
                string name = "";
                string position = "";
                try
                {
                    //根据数据库找到theone、摄像头名称等内容
                    //找到了目前这个函数通道所在的NVR，
                    //然后根据通道号跟NVRip地址，找到摄像头
                    string srtSql = "NVRIP地址= '" + paras[1] + "'" + " and  通道号 ='" + alarmMsg[1] + "'";
                    DataRow[] dr = StaticData.g_dtCamera.Select(srtSql);
                    if (dr.Length > 0)
                    {
                        theone = dr[0]["theone"].ToString();
                        name = dr[0]["摄像头名称"].ToString();
                        position= dr[0]["坐标"].ToString();
                    }else{
                        // 处理获取非指定camaras错误回调后所产生的数据为空的异常情况
                        // 异常eg:  发现了非列表中的未知nvrip = “10.1.12.51”的设备
                        return;
                    }


                }            
                catch (Exception)
                {
                    StaticUtils.ShowEventMsg("HikvisionAlarmCallBackFun.class-ProcessCommAlarm_V30 : 报警回调函数出现异常！\n");
                    //throw;
                }

                //读取事件数据库，判断数据库是否存在该事件，且时间是否2分钟以上差别，存在且是的话插入新事件
                //不是的话更新endtime字段为当前时间
                //不存在的话直接插入数据，还需要插入个摄像头的位置,名称

                CheckEvent(name, theone, alarmMsg[0], DateTime.Now.ToString().Replace('/', '-'), position, name,paras, alarmMsg[1]);
            }
            else
            {
                //创建该控件的主线程直接更新信息列表 
                //  UpdateClientList(DateTime.Now.ToString(), strIP, stringAlarm);
            }
        }

        /// <summary>
        /// 越界侦测用的报警
        /// </summary>
        /// <param name="pAlarmer"></param>
        /// <param name="pAlarmInfo"></param>
        /// <param name="dwBufLen"></param>
        /// <param name="pUser"></param>
        private void ProcessCommAlarm_RULE(ref CHCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {

            string strNvrIp = pAlarmer.sDeviceIP;//报警nvrIp地址
            CHCNetSDK.NET_VCA_RULE_ALARM struRuleAlarmInfo = new CHCNetSDK.NET_VCA_RULE_ALARM();
            //看下报警信息里面有没有通道号，
            struRuleAlarmInfo = (CHCNetSDK.NET_VCA_RULE_ALARM)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK.NET_VCA_RULE_ALARM));
            byte channel= struRuleAlarmInfo.struDevInfo.byChannel;
            //报警设备IP地址
            string strIP = struRuleAlarmInfo.struDevInfo.struDevIP.sIpV4;
            //报警信息
            string stringAlarm = "";
            uint dwSize = (uint)Marshal.SizeOf(struRuleAlarmInfo.struRuleInfo.uEventParam);

            switch (struRuleAlarmInfo.struRuleInfo.wEventTypeEx)
            {
                case (ushort)CHCNetSDK.VCA_RULE_EVENT_TYPE_EX.ENUM_VCA_EVENT_TRAVERSE_PLANE:
                    IntPtr ptrTraverseInfo = Marshal.AllocHGlobal((Int32)dwSize);
                    Marshal.StructureToPtr(struRuleAlarmInfo.struRuleInfo.uEventParam, ptrTraverseInfo, false);
                    CHCNetSDK.NET_VCA_TRAVERSE_PLANE m_struTraversePlane = (CHCNetSDK.NET_VCA_TRAVERSE_PLANE)Marshal.PtrToStructure(ptrTraverseInfo, typeof(CHCNetSDK.NET_VCA_TRAVERSE_PLANE));
                    stringAlarm = "穿越警戒面，目标ID：" + struRuleAlarmInfo.struTargetInfo.dwID;
                    //警戒面边线起点坐标: (m_struTraversePlane.struPlaneBottom.struStart.fX, m_struTraversePlane.struPlaneBottom.struStart.fY)
                    //警戒面边线终点坐标: (m_struTraversePlane.struPlaneBottom.struEnd.fX, m_struTraversePlane.struPlaneBottom.struEnd.fY)
                    break;
                case (ushort)CHCNetSDK.VCA_RULE_EVENT_TYPE_EX.ENUM_VCA_EVENT_ENTER_AREA:
                    IntPtr ptrEnterInfo = Marshal.AllocHGlobal((Int32)dwSize);
                    Marshal.StructureToPtr(struRuleAlarmInfo.struRuleInfo.uEventParam, ptrEnterInfo, false);
                    CHCNetSDK.NET_VCA_AREA m_struVcaArea = (CHCNetSDK.NET_VCA_AREA)Marshal.PtrToStructure(ptrEnterInfo, typeof(CHCNetSDK.NET_VCA_AREA));
                    stringAlarm = "目标进入区域，目标ID：" + struRuleAlarmInfo.struTargetInfo.dwID;
                    //m_struVcaArea.struRegion 多边形区域坐标
                    break;
                case (ushort)CHCNetSDK.VCA_RULE_EVENT_TYPE_EX.ENUM_VCA_EVENT_EXIT_AREA:
                    IntPtr ptrExitInfo = Marshal.AllocHGlobal((Int32)dwSize);
                    Marshal.StructureToPtr(struRuleAlarmInfo.struRuleInfo.uEventParam, ptrExitInfo, false);
                    m_struVcaArea = (CHCNetSDK.NET_VCA_AREA)Marshal.PtrToStructure(ptrExitInfo, typeof(CHCNetSDK.NET_VCA_AREA));
                    stringAlarm = "目标离开区域，目标ID：" + struRuleAlarmInfo.struTargetInfo.dwID;
                    //m_struVcaArea.struRegion 多边形区域坐标
                    break;
                case (ushort)CHCNetSDK.VCA_RULE_EVENT_TYPE_EX.ENUM_VCA_EVENT_INTRUSION:
                    IntPtr ptrIntrusionInfo = Marshal.AllocHGlobal((Int32)dwSize);
                    Marshal.StructureToPtr(struRuleAlarmInfo.struRuleInfo.uEventParam, ptrIntrusionInfo, false);
                    CHCNetSDK.NET_VCA_INTRUSION m_struIntrusion = (CHCNetSDK.NET_VCA_INTRUSION)Marshal.PtrToStructure(ptrIntrusionInfo, typeof(CHCNetSDK.NET_VCA_INTRUSION));
                    stringAlarm = "周界入侵，目标ID：" + struRuleAlarmInfo.struTargetInfo.dwID;
                    //m_struIntrusion.struRegion 多边形区域坐标
                    break;
                default:
                    stringAlarm = "其他行为分析报警，目标ID：" + struRuleAlarmInfo.struTargetInfo.dwID;
                    break;
            }


            //报警图片保存
            if (struRuleAlarmInfo.dwPicDataLen > 0)
            {
                FileStream fs = new FileStream("行为分析报警抓图.jpg", FileMode.Create);
                int iLen = (int)struRuleAlarmInfo.dwPicDataLen;
                byte[] by = new byte[iLen];
                Marshal.Copy(struRuleAlarmInfo.pImage, by, 0, iLen);
                fs.Write(by, 0, iLen);
                fs.Close();
            }

            //报警时间：年月日时分秒
            string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            string theone = "0";
            string name = "";
            string position = "";

            try
            {
                //根据数据库找到theone、摄像头名称等内容
                //找到了目前这个函数通道所在的NVR，
                //然后根据通道号跟NVRip地址，找到摄像头
                string srtSql = "IP地址 ='" + strIP + "'";
                DataRow[] dr = StaticData.g_dtCamera.Select(srtSql);
                if (dr.Length > 0)
                {
                    theone = dr[0]["theone"].ToString();
                    name = dr[0]["摄像头名称"].ToString();
                    position = dr[0]["坐标"].ToString();

                }
                else
                {
                    // 处理获取非指定camaras错误回调后所产生的数据为空的异常情况
                    // 异常eg:  发现了非列表中的未知nvrip = “10.1.12.51”的设备
                    return;
                }


            }
            catch (Exception)
            {
                StaticUtils.ShowEventMsg("HikvisionAlarmCallBackFun.class-ProcessCommAlarm_RULE : 报警回调函数出现异常！\n");
                //throw;
            }

            //读取事件数据库，判断数据库是否存在该事件，且时间是否2分钟以上差别，存在且是的话插入新事件
            //不是的话更新endtime字段为当前时间
            //不存在的话直接插入数据，还需要插入个摄像头的位置,名称
            string[] paras = new string[2]
            {
                time,strNvrIp
            };
            string str_channel = Convert.ToString(channel);
            CheckEvent(name, theone, stringAlarm , DateTime.Now.ToString().Replace('/', '-'), position, name, paras, str_channel);

        }

        public override void CameraInit(string DVRIPAddress, short DVRPortNumber, string DVRUserName, string DVRPassword)
        {
            throw new NotImplementedException();
        }

        public override void IPListFun(int userID)
        {
            throw new NotImplementedException();
        }

        public override void PreviewFun(int userID, int channel)
        {
            throw new NotImplementedException();
        }

        public override void PTZControl(int userID, int channel)
        {
            throw new NotImplementedException();

        }    

        //事件信息增加
        public void ShowEventMsg(string msg)
        {
            CrossThreadOperationControl CrossDele = delegate ()
            {
                Mainform.form1.kText_EventInfo.AppendText(msg);
            };
            Mainform.form1.kText_EventInfo.Invoke(CrossDele);
        }
        //读取事件数据库，判断数据库是否存在该事件，且时间是否2分钟以上才再次触发，存在且是的话插入新事件
        //不是的话更新endtime字段为当前时间
        //不存在的话直接插入数据
        private void CheckEvent(string strDevice,string strTarget_theone,string strEvent,string strTime,string strPosition,string srtCameraName,string[] paras,string channelName)
        {
            //事件比较特殊，可有可能同时都会调用，所以这里每次调用的时候，都新建一个数据库的conn
            DBConnect dbCheck = new DBConnect();
            DBConnect dbInsORUp = new DBConnect();

            string[] strReadDB = new string[] { };
            try
            {
                //判断数据库状态并打开数据库连接
                if (dbCheck.msConn.State == System.Data.ConnectionState.Closed)
                {
                    dbCheck.msConn.Open();
                }
                if (dbInsORUp.msConn.State == System.Data.ConnectionState.Closed)
                {
                    dbInsORUp.msConn.Open();
                }
                // string strCMD = "select theone,event,endtime  from event;";
                string strCMD = "select end_time from event_assemble where target_theone='" + strTarget_theone + "' ORDER BY id DESC LIMIT 1;";

                MySqlCommand cmd = new MySqlCommand(strCMD, dbCheck.msConn);
                MySqlDataReader sqlReader = null;
                string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                if (StaticUtils.DBReaderOperationException(ref cmd, ref sqlReader, currentClassName, currentMethodName))
                {
                    while (sqlReader.Read())
                    {
                        strReadDB = new string[] { sqlReader.GetString(0) };
                        //逐条数据判断
                        //判断数据库中是否存在数据
                        if (strReadDB != null)
                        {
                            //比较endtime的时间和当前时间
                            //传入时间
                            DateTime data_create = Convert.ToDateTime(strTime);
                            DateTime data_db = Convert.ToDateTime(strReadDB[0]);

                            // 相同报警间隔time   unit: s
                            long time_interval = StaticUtils.GetCurrentTimeUnix(data_create) - StaticUtils.GetCurrentTimeUnix(data_db);

                            if (time_interval < 120)
                            {
                                string strUpdate = "UPDATE event_assemble SET end_time='" + strTime + "'  WHERE target_theone='" + strTarget_theone + "' AND end_time='" + strReadDB[0] + "';";
                                MySqlCommand cmdUpdate = new MySqlCommand(strUpdate, dbInsORUp.msConn);
                                StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);
                                //跳出                             
                                goto here;
                            }
                        }

                    }
                }
                string event_name = "";   //故障二级名称

                //事件等级判断
                string strLevel = "";
                if (strEvent.Contains("信号丢失"))
                {
                    strLevel = "一般情况";
                    event_name = "监控设备掉线";
                }
                else if(strEvent.Contains("移动侦测"))
                {
                    strLevel = "一般情况";
                    event_name = "监控移动侦测";
                }
                else if (strEvent.Contains("穿越警戒面"))
                {
                    strLevel = "重要情况";
                    event_name = "监控越界侦测";
                }

                
                //生成eventtheon
                string strEventTheOne = Guid.NewGuid().ToString();

                //插入数据
                //往总表跟字表里面都插入数据
                DataRow[] dr_abnormal_super = StaticData.g_dtAbnormalInfor.Select("故障父类名称='event_camera'");
                DataRow[] dr_abnormal_sub = StaticData.g_dtAbnormalInfor.Select("父ID='" + dr_abnormal_super[0]["ID"] + "'");
               
                string strInsert1 = "INSERT INTO event_assemble (event_name,event_theone,event_type,start_time,end_time,event_status,position,event_level,target_theone) values"+
                    "('"+ event_name + "','"+ strEventTheOne+ "','event_camera','"+ DateTime.Now.ToString().Replace('/', '-') +"','" + DateTime.Now.ToString().Replace('/', '-') + "','未读','"+
                    strPosition + "','"+ strLevel+"','"+ strTarget_theone+"');";
                string strInsert2 = "INSERT INTO event_camera (event_theone,device_event_type,device_theone) values" +
                    "('" + strEventTheOne + "','" + event_name + "','" + strTarget_theone + "');";
                MySqlCommand cmdInsert = new MySqlCommand(strInsert1, dbInsORUp.msConn);
                StaticUtils.DBNonQueryOperationException(ref cmdInsert, currentClassName, currentMethodName);
                MySqlCommand cmdInsert2 = new MySqlCommand(strInsert2, dbInsORUp.msConn);
                StaticUtils.DBNonQueryOperationException(ref cmdInsert2, currentClassName, currentMethodName);

                //向web上报事件
                string strSendMSG = jscontrol.EventJson(strEventTheOne);
                Mainform.form1.SendMsgToWebSocketClients(strSendMSG);
                //更新UI界面
                StaticUtils.ShowEventMsg("报警时间：" + paras[0] + "  NVRIP：" + paras[1] + "  事件类型：" + event_name + "  通道号：" + channelName + "  摄像机名称：" + strDevice + "\n");
                //计数加一
                StaticData.g_inAlarmNum++;

                here:
                dbInsORUp.msConn.Close();
                sqlReader.Close();
                dbCheck.msConn.Close();
            }
            catch (Exception e)
            {                
                StaticUtils.ShowEventMsg("HikvisionAlarmCallBackFun.class-CheckEvent : 插入摄像头报警事件出现异常！！\n");
            }
        }
        
    }


}
