using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SupportPlatform
{
    class SkyinforHikvision
    {
        private string currentClassName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
        MySqlConnection Msconn = StaticData.g_msConn;
        private static string m_hlsPort;
        private delegate void CrossThreadOperationControl();
        public SkyinforHikvision()
        {

        }
        /// <summary>
        /// 根据数据库对海康NVR进行初始化
        /// </summary>
        public void InitNVR()
        {
            try
            {
                /* 
                * 读取nvr表，并根据反射进行摄像头SDK注册
                */
                StaticData.g_dtNVR.Columns.Add("序号", typeof(int));
                StaticData.g_dtNVR.Columns.Add("IP地址", typeof(string));
                StaticData.g_dtNVR.Columns.Add("用户名", typeof(string));
                StaticData.g_dtNVR.Columns.Add("密码", typeof(string));
                StaticData.g_dtNVR.Columns.Add("端口", typeof(int));
                StaticData.g_dtNVR.Columns.Add("userid", typeof(int));

                DBConnect getNvrDBC = new DBConnect();
                try
                {

                    if (getNvrDBC.msConn.State == System.Data.ConnectionState.Closed)
                    {
                        getNvrDBC.msConn.Open();
                    }
                    string strCMD = "select username,password,nvrip  from config_support_platform where config_type='nvr';";
                    MySqlCommand cmd = new MySqlCommand(strCMD, getNvrDBC.msConn);
                    MySqlDataReader sqlReader = null;
                    string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                    if (StaticUtils.DBReaderOperationException(ref cmd, ref sqlReader, currentClassName, currentMethodName))
                    {
                        int i = 0;
                        while (sqlReader.Read())
                        {
                            //得到了NVR数据库
                            //IP，name，password
                            string[] str = { sqlReader["nvrip"].ToString(), sqlReader["username"].ToString(), sqlReader["password"].ToString() };
                            Hikvision hikCamera = CameraNVR.CreateCamera("Hikvision");
                            hikCamera.CameraInit(str[0], 8000, str[1], str[2]); //海康SDK注册 
                            DataRow dr = StaticData.g_dtNVR.NewRow();//表格的行
                            dr["序号"] = i;
                            dr["IP地址"] = str[0];
                            dr["用户名"] = str[1];
                            dr["密码"] = str[2];
                            dr["端口"] = 8000;//以后也可以做成可以配置的

                            dr["userid"] = Hikvision.m_iUserID;
                            StaticData.g_dtNVR.Rows.Add(dr);//添加     
                            i++;
                        }
                        sqlReader.Close();
                    }
                    getNvrDBC.msConn.Close();
                    getNvrDBC.msConn = null;
                }
                catch
                {
                    throw;
                }
            }
            catch (Exception)
            {
                StaticUtils.ShowEventMsg("SkyinforHikvision.class-InitNVR : 海康NVR进行初始化出现异常！！\n");
                //throw;
            }
        }

        /// <summary>
        /// 根据数据库对海康摄像头进行遍历
        /// </summary>
        public void InitHKCamera()
        {
            StaticData.g_dtCamera.Columns.Add("序号", typeof(int));
            StaticData.g_dtCamera.Columns.Add("编号", typeof(string));
            StaticData.g_dtCamera.Columns.Add("摄像头名称", typeof(string));
            StaticData.g_dtCamera.Columns.Add("摄像头状态", typeof(string));
            StaticData.g_dtCamera.Columns.Add("IP地址", typeof(string));
            StaticData.g_dtCamera.Columns.Add("用户名", typeof(string));
            StaticData.g_dtCamera.Columns.Add("密码", typeof(string));
            StaticData.g_dtCamera.Columns.Add("端口", typeof(int));
            StaticData.g_dtCamera.Columns.Add("NVRIP地址", typeof(string));
            StaticData.g_dtCamera.Columns.Add("通道号", typeof(string));
            StaticData.g_dtCamera.Columns.Add("RTSP地址", typeof(string));
            StaticData.g_dtCamera.Columns.Add("转码标识", typeof(int));
            StaticData.g_dtCamera.Columns.Add("theone", typeof(string));
            StaticData.g_dtCamera.Columns.Add("坐标", typeof(string));
            StaticData.g_dtCamera.Columns.Add("RTMP地址", typeof(string));
            StaticData.g_dtCamera.Columns.Add("HLS地址", typeof(string));
            StaticData.g_dtCamera.Columns.Add("摄像头型号", typeof(string));
            StaticData.g_dtCamera.Columns.Add("摄像头型类型", typeof(string));
            StaticData.g_dtCamera.Columns.Add("摄像头型区域", typeof(string));
            StaticData.g_dtCamera.Columns.Add("流ID", typeof(string));
            //下面是转码配置，先不管
            string strConfigPath = AppDomain.CurrentDomain.BaseDirectory + "config.ini";
            m_hlsPort = OperateIniFile.ReadIniData("Login", "hlsPort", null, strConfigPath);//读取hls流使用的端口号
                                                                                            /*--------------------------------------------------------------------------------
                                                                                                读取camera表，获取IP、名称、channel、line等
                                                                                                进行摄像头状态获取及流转换处理
                                                                                                之后更新Form中的数据
                                                                                                ---------------------------------------------------------------------------------*/
            DBConnect getCameraDBC = new DBConnect();
            try
            {

                if (getCameraDBC.msConn.State == System.Data.ConnectionState.Closed)
                {
                    getCameraDBC.msConn.Open();
                }
                string strCMD = "select device_paper_number,device_name,device_ip,device_type,device_area,device_nvrip,device_channel,device_theone,concat_ws(',',ST_X(device_point),ST_Y(device_point),device_height) as device_position,device_rtsp,device_transcoding_flag from device_camera;";
                MySqlCommand cmd = new MySqlCommand(strCMD, getCameraDBC.msConn);
                MySqlDataReader sqlReaderCamera = null;
                string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                if (StaticUtils.DBReaderOperationException(ref cmd, ref sqlReaderCamera, currentClassName, currentMethodName))
                {
                    int i = 0;
                    while (sqlReaderCamera.Read())
                    {

                        DataRow dr = StaticData.g_dtCamera.NewRow();//表格的行
                        dr["序号"] = i;
                        dr["编号"] = sqlReaderCamera["device_paper_number"].ToString();
                        dr["摄像头名称"] = sqlReaderCamera["device_name"].ToString();
                        dr["IP地址"] = sqlReaderCamera["device_ip"].ToString();
                        dr["摄像头型类型"] = sqlReaderCamera["device_type"].ToString();
                        dr["摄像头型区域"] = sqlReaderCamera["device_area"].ToString();
                        dr["NVRIP地址"] = sqlReaderCamera["device_nvrip"].ToString();
                        dr["通道号"] = sqlReaderCamera["device_channel"].ToString();
                        dr["theone"] = sqlReaderCamera["device_theone"].ToString();
                        dr["坐标"] = sqlReaderCamera["device_position"].ToString();
                        dr["RTSP地址"] = sqlReaderCamera["device_rtsp"].ToString();

                        // 针对转码标识为空无法直转int 作条件判断
                        if (sqlReaderCamera["device_transcoding_flag"] is System.DBNull || "".Equals(sqlReaderCamera["device_transcoding_flag"]))
                        {
                            dr["转码标识"] = 0;
                        }
                        else
                        {
                            dr["转码标识"] = Convert.ToInt32(sqlReaderCamera["device_transcoding_flag"]);
                        }

                        dr["IP地址"] = sqlReaderCamera["device_ip"].ToString();
                        dr["用户名"] = "admin";
                        dr["密码"] = "admin123456";
                        dr["端口"] = 8000;
                        StaticData.g_dtCamera.Rows.Add(dr);//添加     
                        i++;
                    }


                    //读取完所有摄像头后，遍历一遍状态
                    //遍历一遍NVR，将NVR中各个通道的状态与摄像头列表进行管线，进行整体状态维护，其中根据NVRIP 与通道号进行关联
                    for (int k = 0; k < StaticData.g_dtNVR.Rows.Count; k++)
                    {
                        Hikvision hikivisionList = new HikIPListFun();
                        hikivisionList.IPListFun(Convert.ToInt32(StaticData.g_dtNVR.Rows[k]["userid"]));  //调用IPList方法，将状态写入到dicIPList字典中
                    }

                    sqlReaderCamera.Close();
                }
                getCameraDBC.msConn.Close();
                getCameraDBC.msConn = null;
            }
            catch
            {
                StaticUtils.ShowEventMsg("HikvisionAlarmCallBackFun.class-InitHKCamera : 利用DB初始化摄像头出现异常！！\n");
            }
        }

        /// <summary>
        /// 用于轮询时刷新摄像头状态,轮询状态意义不大。不用轮询
        /// </summary>
        public void QueryCameras()
        {
            /*------------------------------------------------------------------------
             读取camera表，获取IP、名称、channel、line等
             进行摄像头状态获取及流转换处理
              之后更新Form中的数据
             ------------------------------------------------------------------------*/
            string strCMD = "select * from device_camera;";
            if (StaticData.g_msVideQueryConn.State == System.Data.ConnectionState.Closed)
            {
                StaticData.g_msVideQueryConn.Open();
            }
            MySqlCommand cmd = new MySqlCommand(strCMD, StaticData.g_msVideQueryConn);
            MySqlDataReader sqlReader = null;
            string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
            if (StaticUtils.DBReaderOperationException(ref cmd, ref sqlReader, currentClassName, currentMethodName))
            {

                int i = 0;
                while (sqlReader.Read())
                {
                    /*----------------------------------------------------------------------------------------------------------------------------------------                
                     下面读取到camera表的所有字段，依次为                   
                      id(0)   name(1)    position(2)   ip(3)   type(4) username(5) password(6) theon(7)    time(8) line(9) nvrip(10)   rtspstreamid(11)                                   
                    ------------------------------------------------------------------------------------------------------------------------------------------*/
                    //string[] strCamera = { sqlReader.GetString(0), sqlReader.GetString(1), sqlReader.GetString(2), sqlReader.GetString(3), sqlReader.GetString(4), sqlReader.GetString(5), sqlReader.GetString(6)
                    //, sqlReader.GetString(7), sqlReader.GetString(8), sqlReader.GetString(9), sqlReader.GetString(10), sqlReader.GetString(11)};
                    DataRow dt = StaticData.g_dtCamera.NewRow();//表格的行
                    dt["序号"] = i;
                    dt["编号"] = sqlReader["device_paper_number"].ToString();

                    dt["摄像头名称"] = sqlReader["device_name"].ToString();
                    dt["IP地址"] = sqlReader["device_ip"].ToString();
                    dt["摄像头型类型"] = sqlReader["device_type"].ToString();
                    dt["摄像头型区域"] = sqlReader["device_area"].ToString();
                    dt["NVRIP地址"] = sqlReader["device_nvrip"].ToString();
                    dt["通道号"] = sqlReader["device_channel"].ToString();
                    dt["用户名"] = "admin";
                    dt["密码"] = "admin123456";

                    dt["RTSP地址"] = "rtsp://";

                    dt["端口"] = 8000;
                    string strCameraState = "";
                    try
                    {
                        Hikvision hikivisionList = new HikIPListFun();
                        hikivisionList.IPListFun(Hikvision.m_iUserID);  //调用IPList方法，将状态写入到dicIPList字典中
                        strCameraState = hikivisionList.dicIPList[sqlReader["device_channel"].ToString()]; //从字典中读取到该摄像头的状态
                    }
                    catch (Exception)
                    {
                        strCameraState = "ERROR";
                        StaticUtils.ShowEventMsg("SkyinforHikvision.class-QueryCameras : 轮询刷新摄像头状态出现异常！！\n");
                    }

                    //dt.Rows[i]["摄像头状态"] = strCameraState;
                    //dt.Rows[i]["用户名"] = strCamera[5];//这个要查询数据库的，我先放着了，你继续完善
                    //dt.Rows[i]["密码"] = strCamera[6];//这个要查询数据库的，我先放着了，你继续完善
                    //dt.Rows[i]["NVR IP地址"] = strCamera[10];//这个要查询数据库的，我先放着了，你继续完善

                    //dt.Rows[i]["RTSP地址"] = "rtsp://" + dt.Rows[i]["用户名"] + ":" + dt.Rows[i]["密码"] + "@" + dt.Rows[i]["NVR IP地址"] + ":554/Streaming/Channels/" + dt.Rows[i]["流ID"];
                    //dt.Rows[i]["RTMP地址"] = "rtmp://" + StaticData.g_strLocalIP + "/live/" + dt.Rows[i]["通道号"];
                    //dt.Rows[i]["HLS地址"] = "http://" + StaticData.g_strLocalIP +":"+m_hlsPort+ "/live/" + dt.Rows[i]["通道号"] + "/hls.m3u8";           
                    i++;
                    if (i > StaticData.g_dtCamera.Rows.Count - 1)
                    {
                        i = StaticData.g_dtCamera.Rows.Count - 1;
                    }
                }
                sqlReader.Close();
            }
        }

        #region 转码函数，已经独立出来,这里不在调用
        /// <summary>
        /// 天覆视频服务器，牛逼的服务器！
        /// </summary>
        /// <param name="rtspAdd"></param>
        /// <param name="id"></param>
        /// <param name="nVideoNum"></param>
        /// <returns></returns>
        [DllImport("skyinforVideoServer.dll", EntryPoint = "skyinforVideoShare", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int skyinforVideoShare(string[] rtspAdd, string[] id, int nVideoNum);



        public void SkyinforVideoServer()
        {
            //显示视频转码路数
            CrossThreadOperationControl CrossDele_VideoCount = delegate ()
            {
                Mainform.form1.textBoxVideo.Text = StaticData.g_dtCamera.Rows.Count + "路";
            };
            Mainform.form1.textBoxVideo.Invoke(CrossDele_VideoCount);
            List<string> rtspList = new List<string>();
            List<string> idList = new List<string>();
            //遍历一下所有的摄像头rtsp流，传入rtsp流地址，通道号，就能实现转换了
            for (int i = 0; i < StaticData.g_dtCamera.Rows.Count; i++)
            {
                rtspList.Add(StaticData.g_dtCamera.Rows[i]["RTSP地址"].ToString());
                idList.Add(StaticData.g_dtCamera.Rows[i]["通道号"].ToString());
            }
            try
            {
                skyinforVideoShare(rtspList.ToArray(), idList.ToArray(), StaticData.g_dtCamera.Rows.Count);
            }
            catch (Exception)
            {
                //自己写的代码，肯定有异常，先不管了。。。测试能用了再说
                StaticUtils.ShowEventMsg("SkyinforHikvision.class-SkyinforVideoServer : SkyinforVideoServer出现异常！！\n");
                return;
            }

        }
        #endregion
        /// <summary>
        /// 遍历CameraData table的数据，并写入数据库   /*  UpdateDB耗时操作应放到子线程中 */
        /// 
        /// </summary>
        public void UpdateDB()
        {
            //遍历datatable
            DBConnect upCameraDBC = new DBConnect();
            DataTable dt = StaticData.g_dtCamera;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                string strNVVRIP = dt.Rows[i]["NVRIP地址"].ToString();
                string strChannel = dt.Rows[i]["通道号"].ToString();
                string strStatus = dt.Rows[i]["摄像头状态"].ToString();
                string strCMD = "UPDATE device_camera SET device_status='" + strStatus + "'  WHERE device_nvrip='" + strNVVRIP + "' AND device_channel='" + strChannel + "';";

                if (upCameraDBC.msConn.State == System.Data.ConnectionState.Closed)
                {
                    upCameraDBC.msConn.Open();
                }

                try
                {
                    MySqlCommand cmd = new MySqlCommand(strCMD, upCameraDBC.msConn);
                    string currentMethodName = System.Reflection.MethodBase.GetCurrentMethod().Name;
                    StaticUtils.DBNonQueryOperationException(ref cmd, currentClassName, currentMethodName);

                }
                catch (Exception)
                {
                    StaticUtils.ShowEventMsg("SkyinforHikvision.class-UpdateDB : 遍历CameraData table数据出现异常！！\n");
                    //throw;
                }
            }
        }
    }
}
