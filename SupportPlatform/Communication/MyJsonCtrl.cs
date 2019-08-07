using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Json;
using System.Data;
using MySql.Data.MySqlClient;

namespace SupportPlatform
{
    class MyJsonCtrl
    {
        public string m_lCameraIP;
        public string m_lChannel;
        private delegate void CrossThreadOperationControl();
        #region 轮询设备状态时的Json拼接（暂未使用）


        /// <summary>
        /// 方法用于轮询时的状态拼接Json处理
        /// </summary>
        /// <param name="strDeviceName">设备类型</param>
        /// <param name="strIP">IP数组</param>
        /// <param name="strState">状态数组</param>
        /// <returns></returns>
        public string QueryJson(string strDeviceName,string[] strIP,string[] strState)
        {
            JsonObject js = new JsonObject();
            js.Add("responseType", strDeviceName);
            JsonObject jsArr = new JsonObject();
            try
            {
                for (int i = 0; i < strIP.Length; i++)
                {
                    jsArr.Add(strIP[i], strState[i]);
                }
                jsArr.Add("type", "Query");
                jsArr.Add("status", "1");
            }
            catch (Exception e)
            {
                jsArr.Add("type", "Query");
                jsArr.Add("status", "0");
                StaticUtils.ShowEventMsg("MyJsonCtrl.class-QueryJson : 轮询时的状态拼接Json处理出现异常！！\n");
            }
            js.Add("responDetail", jsArr);
            string strReturnResult = js.ToString();
            strReturnResult = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(strReturnResult));
            return strReturnResult;
        }
        #endregion
        /// <summary>
        /// 组装Json的函数
        /// </summary>
        /// <param name="strType">设备类型</param>
        /// <param name="strStatusCode">是否成功编码</param>
        /// <param name="strIP">IP</param>
        /// <param name="strLine">通道号</param>
        /// <param name="strCmdType">命令类型</param>
        /// <param name="strResult">返回结果</param>
        public JsonObject SendJson(string strType,string strStatusCode,string strIP,string strLine,string strCmdType,string strResult,string strResult2)
        {
            JsonObject js = new JsonObject();
            
            js.Add("responseType", strType);

            JsonObject jsArr = new JsonObject();
            jsArr.Add("statuscode", strStatusCode);
            jsArr.Add("ip", strIP);
            jsArr.Add("line", strLine);
            jsArr.Add("type", strCmdType);
            jsArr.Add("returnResult", strResult);
            jsArr.Add("resturnResult2", strResult2);

            js.Add("responseDetail", jsArr);
            return js;
        }
        //public string ResponseJson(string statues,string strType)
        //{
        //    JsonObject js = new JsonObject();

        //    js.Add("responseType", strType);
        //    JsonObject jsArr = new JsonObject();
        //    jsArr.Add("code", statues);
        //    js.Add("responseDetail", jsArr);
        //    string strReturnResult = js.ToString();
        //    strReturnResult = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(strReturnResult));
        //    return strReturnResult;
        //}
        /// <summary>
        /// 读取DATa table中指定的流数据
        /// </summary>
        /// <param name="dtable">DATa table表名</param>
        /// <param name="strStreamType">流类型</param>
        /// <param name="strChannel">通道号</param>
        /// <param name="strIP">IP</param>
        /// <returns></returns>
        private string GetStrStream(DataTable dtable,string strStreamType,string strChannel,string strIP)
        {
           DataRow[] dr = dtable.Select("IP地址='" + strIP + "'AND 通道号='" + strChannel + "'");
            if (dr.Length>=1)
            {
                string strStreamAdd = dr[0][strStreamType].ToString();
                return strStreamAdd;
            }
            else
            {
                return "获取失败";
            }
        }



        /// <summary>
        /// 命令解析函数
        /// </summary>
        /// <param name="strJson"></param>
        /// <returns></returns>
        public string ReceiceJson(string strJson)
        {
            
            try
            {
                JsonObject joWebCmd = (JsonObject)JsonValue.Parse(strJson);
                string strResult = "null";
                string strResult2 = "null";
                string strStatusCode = "0";
                string strCmd="";
                #region 摄像头云台控制命令
                if (joWebCmd["requestType"] == "Camera")
                {
                    string  strCameraNVRip = joWebCmd["requestDetail"]["nvrip"];
                    string strCameraChannel = joWebCmd["requestDetail"]["channel"];
                    string strCameraCmd = joWebCmd["requestDetail"]["command"];
                    //执行命令
                    HikvisionPTZControl hkPtzCtl = new HikvisionPTZControl();
                    //根据选中的摄像头通道号跟IP地址进行预览
                    //zzt补充，这里加个界面日志，告诉别人收到了什么命令，时间跟
                    DataRow[] dr;
                    switch (strCameraCmd)
                    {                        
                        case "left":
                            dr = StaticData.g_dtNVR.Select("IP地址= '" + strCameraNVRip + "'");
                            if (dr.Length > 0)
                            {
                                hkPtzCtl.PTZleft(Convert.ToInt32(dr[0]["userid"]), Convert.ToInt32( strCameraChannel));
                            }                           
                            break;
                        case "right":
                            dr = StaticData.g_dtNVR.Select("IP地址= '" + strCameraNVRip + "'");
                            if (dr.Length > 0)
                            {
                                hkPtzCtl.PTZright(Convert.ToInt32(dr[0]["userid"]), Convert.ToInt32(strCameraChannel));
                            }
                            break;
                        case "up":
                            dr = StaticData.g_dtNVR.Select("IP地址= '" + strCameraNVRip + "'");
                            if (dr.Length > 0)
                            {
                                hkPtzCtl.PTZup(Convert.ToInt32(dr[0]["userid"]), Convert.ToInt32(strCameraChannel));
                            }
                            break;
                        case "down":
                            dr = StaticData.g_dtNVR.Select("IP地址= '" + strCameraNVRip + "'");
                            if (dr.Length > 0)
                            {
                                hkPtzCtl.PTZdown(Convert.ToInt32(dr[0]["userid"]), Convert.ToInt32(strCameraChannel));
                            }
                            break;
                        case "upleft":
                            dr = StaticData.g_dtNVR.Select("IP地址= '" + strCameraNVRip + "'");
                            if (dr.Length > 0)
                            {
                                hkPtzCtl.PTZupleft(Convert.ToInt32(dr[0]["userid"]), Convert.ToInt32(strCameraChannel));
                            }
                            break;
                        case "upright":
                            dr = StaticData.g_dtNVR.Select("IP地址= '" + strCameraNVRip + "'");
                            if (dr.Length > 0)
                            {
                                hkPtzCtl.PTZright(Convert.ToInt32(dr[0]["userid"]), Convert.ToInt32(strCameraChannel));
                            }
                            break;
                        case "downleft":
                            dr = StaticData.g_dtNVR.Select("IP地址= '" + strCameraNVRip + "'");
                            if (dr.Length > 0)
                            {
                                hkPtzCtl.PTZdownleft(Convert.ToInt32(dr[0]["userid"]), Convert.ToInt32(strCameraChannel));
                            }
                            break;
                        case "downright":
                            dr = StaticData.g_dtNVR.Select("IP地址= '" + strCameraNVRip + "'");
                            if (dr.Length > 0)
                            {
                                hkPtzCtl.PTZdownright(Convert.ToInt32(dr[0]["userid"]), Convert.ToInt32(strCameraChannel));
                            }
                            break;
                        case "zoom_out":
                            dr = StaticData.g_dtNVR.Select("IP地址= '" + strCameraNVRip + "'");
                            if (dr.Length > 0)
                            {
                                hkPtzCtl.PTZzoomout(Convert.ToInt32(dr[0]["userid"]), Convert.ToInt32(strCameraChannel));
                            }
                            break;
                        case "zoom_in":
                            dr = StaticData.g_dtNVR.Select("IP地址= '" + strCameraNVRip + "'");
                            if (dr.Length > 0)
                            {
                                hkPtzCtl.PTZzoomin(Convert.ToInt32(dr[0]["userid"]), Convert.ToInt32(strCameraChannel));
                            }
                            break;
                    }

                    //更新下界面              
                    StaticUtils.ShowCMDMsg("接收时间：" + DateTime.Now.ToString() + "  摄像头云台控制命令!\n" + "控制nvrip为：" + strCameraNVRip + "   通道号为：" + strCameraChannel + "   命令为：" + strCameraCmd+"\n");

                    //得到状态，下一步封装成json传回去
                    JsonObject returnJS=  SendJson("Camera", strStatusCode, m_lCameraIP, m_lChannel,strCmd, strResult,strResult2);
                    string strReturnResult =returnJS.ToString();
                    strReturnResult = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(strReturnResult));
                    return strReturnResult;

                }
                #endregion


                #region 广播控制命令
                if (joWebCmd["requestType"] == "Broadcast")
                {
                    string strBroadcastDeviceId = joWebCmd["requestDetail"]["deviceid"];
                    string strBroadcastChannel = joWebCmd["requestDetail"]["channel"];
                    string strBroadcastDeviceIp = joWebCmd["requestDetail"]["deviceip"];
                    string strBroadcastVolumeValue = joWebCmd["requestDetail"]["volumeValue"];
                    string strBroadcastCmd = joWebCmd["requestDetail"]["command"];
                  
                    //执行命令
                    BroadcastUDP broadcastUDP = new BroadcastUDP();
                    //根据选中的摄像头通道号跟IP地址进行预览 
                    //zzt补充，这里加个界面日志，告诉别人收到了什么命令，时间跟
                    switch (strBroadcastCmd)
                    {
                        case "volume":
                            //此命令为修改设备音量，需要设备id，设备ip地址，音量值。
                            Byte volumeArray = (Byte)Convert.ToInt32(strBroadcastVolumeValue); // 将获取到的string转化为 byte                        
                            broadcastUDP.setBroadcastDeviceVolume(strBroadcastDeviceIp, short.Parse(strBroadcastDeviceId), volumeArray);
                            break;
                        case "stop":
                            //此命令为 强制关闭设备，需要设备id，设备ip地址，音量值。
                            Byte nMode = (Byte)Convert.ToInt32("1"); // 输入默认值 ;强制关闭类型: 1 表示强制关闭
                            broadcastUDP.closeBroadcastDevice(strBroadcastDeviceIp, short.Parse(strBroadcastDeviceId), nMode);
                            break;
                        case "channel":
                            //此命令为 切换设备频道，需要设备id，设备ip地址，频道。
                            Byte nChannel = (Byte)Convert.ToInt32(strBroadcastChannel);
                            broadcastUDP.changeBroadcastDeviceChannel(strBroadcastDeviceIp, short.Parse(strBroadcastDeviceId), nChannel);
                            break;
                        
                    }

                    //更新下界面              
                    StaticUtils.ShowCMDMsg("接收时间：" + DateTime.Now.ToString()+"  广播云台控制命令!\n" + "控制广播设备编号为：" + strBroadcastDeviceId + "   命令为：" + strBroadcastCmd + "   广播设备IP为：" + strBroadcastDeviceIp + "\n");
                }
                #endregion
              
                return strResult;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                StaticUtils.ShowEventMsg("MgJsonCtrl.class-ReceiceJson : 数据格式错误，请输入标准Json格式或产生异常\n");
                return "数据格式错误，请输入标准Json格式或产生异常";                     
            }
        }
        /// <summary>
        /// 触发事件时调用，向前端发送Json数据，前端去查询数据库
        /// </summary>
        /// <param name="strDeviceType">设备类型</param>
        /// <param name="strStatus">状态</param>
        /// <param name="theon">唯一标识符</param>
        /// <param name="vol">音量</param>
        /// <returns>返回拼接好的Json字符串</returns>
        public string EventJson(string strEventTheone)
        {
            JsonObject js = new JsonObject();
            js.Add("responseType", "event");
            js.Add("eventTheone", strEventTheone);
            //JsonObject jsArr = new JsonObject();
            //if (vol==null)
            //{
            //    jsArr.Add("status", strStatus);
            //    jsArr.Add("theone", theon);
            //}
            //else
            //{
            //    jsArr.Add("status",strStatus);
            //    jsArr.Add("theone", theon);
            //    jsArr.Add("vol", vol);
            //}
            //joWebCmd.Add("responDetail", jsArr);
            string strReturnResult = js.ToString();
            strReturnResult = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(strReturnResult));
            return strReturnResult;
        }
    }
}
