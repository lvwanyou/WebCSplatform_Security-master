using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SupportPlatform
{
    class HikIPListFun : Hikvision
    {   
        private delegate void CrossThreadOperationControl();   

        /// <summary>
        /// 重写了父类listFun
        /// 此函数主要实现，
        /// </summary>
        /// <param name="userID"></param>
        public override void IPListFun(int userID )
        {
            dwAChanTotalNum = DeviceInfo.byChanNum; //设备信息中的模拟通道个数
            dwDChanTotalNum = DeviceInfo.byIPChanNum + 256 * (uint)DeviceInfo.byHighDChanNum;//最大数字通道个数（地位）+（高位）
            //新的摄像头，有通道
            if (dwDChanTotalNum > 0)
            {
                InfoIPChannel(userID);
            }
            else
            {
                for (int i = 0; i < dwAChanTotalNum; i++)
                {
                    ListAnalogChannel(i + 1, 1);
                    iChannelNum[i] = i + DeviceInfo.byStartChan;//起始通道号
                }
            }
        }
  
        private void ListAnalogChannel(int iChanNo, int byEnable)
        {
            str1 = String.Format("Camera{0}", iChanNo);
            m_lTree++;
            if (byEnable==0)
            {
                str2 = "Disable";
            }
            else
            {
                str2 = "enable";
            }
            //添加到列表中    
            dicIPList.Add(iChanNo.ToString(), str2);//通道号 ；对应状态（Disable，enable）；
        }


        /// <summary>
        /// 根据传入NVR id，获取通道信息，并且拿到ipc通道
        /// </summary>
        /// <param name="m_iUserID"></param>
        private void InfoIPChannel(int m_iUserID)
        {
            uint dwSize = (uint)Marshal.SizeOf(m_struIpParaCfgV40);
            IntPtr ptrIpParaCfgV40 = Marshal.AllocHGlobal((int)dwSize);
            Marshal.StructureToPtr(m_struIpParaCfgV40, ptrIpParaCfgV40, false);
            uint dwReturn = 0;
            int iGroupNo = 0;
            if (!CHCNetSDK.NET_DVR_GetDVRConfig(m_iUserID,CHCNetSDK.NET_DVR_GET_IPPARACFG_V40,iGroupNo,ptrIpParaCfgV40,dwSize,ref dwReturn))
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                Console.WriteLine("获取资源信息配置失败"+iLastErr);
            }
            else
            {
             //   Console.WriteLine("获取资源信息配置成功!");
                m_struIpParaCfgV40 = (CHCNetSDK.NET_DVR_IPPARACFG_V40)Marshal.PtrToStructure(ptrIpParaCfgV40, typeof(CHCNetSDK.NET_DVR_IPPARACFG_V40));
                //遍历所有模拟通道
                for (int i = 0; i < dwAChanTotalNum; i++)
                {
                    ListAnalogChannel(i + 1, m_struIpParaCfgV40.byAnalogChanEnable[i]);
                    iChannelNum[i] = i + (int)DeviceInfo.byStartChan;
                }
                byte byStreamType = 0;
                uint iDChanNum = 64;
                if (dwDChanTotalNum<64)
                {
                    iDChanNum = dwDChanTotalNum;
                }
                //遍历所有数字通道
                for (int i = 0; i < iDChanNum; i++)
                {
                    iChannelNum[i + dwAChanTotalNum] = i + (int)m_struIpParaCfgV40.dwStartDChan;
                    byStreamType = m_struIpParaCfgV40.struStreamMode[i].byGetStreamType;
                    dwSize = (uint)Marshal.SizeOf(m_struIpParaCfgV40.struStreamMode[i].uGetStream);
                    switch (byStreamType)
                    {

                        case 0:
                            IntPtr ptrChanInfo = Marshal.AllocHGlobal((int)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrChanInfo, false);
                            m_struChanInfo = (CHCNetSDK.NET_DVR_IPCHANINFO)Marshal.PtrToStructure(ptrChanInfo, typeof(CHCNetSDK.NET_DVR_IPCHANINFO));
                            //列出IP通道
                            ListIPChannel(i + 1, m_struChanInfo.byEnable, m_struChanInfo.byIPID, m_iUserID);
                            iIPDevID[i] = m_struChanInfo.byIPID + m_struChanInfo.byIPIDHigh * 256 - iGroupNo * 64 - 1;
                            Marshal.FreeHGlobal(ptrChanInfo);
                            break;
                        case 4:
                            IntPtr ptrStreamURL = Marshal.AllocHGlobal((int)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrStreamURL, false);
                            m_struStreamURL = (CHCNetSDK.NET_DVR_PU_STREAM_URL)Marshal.PtrToStructure(ptrStreamURL, typeof(CHCNetSDK.NET_DVR_PU_STREAM_URL));
                            //列出IP通道
                            ListIPChannel(i + 1, m_struStreamURL.byEnable, m_struStreamURL.wIPID, m_iUserID);
                            iIPDevID[i] = m_struStreamURL.wIPID - iGroupNo * 64 - 1;
                            Marshal.FreeHGlobal(ptrStreamURL);
                            break;
                        case 6:
                            IntPtr ptrChanInfoV40 = Marshal.AllocHGlobal((int)dwSize);
                            Marshal.StructureToPtr(m_struIpParaCfgV40.struStreamMode[i].uGetStream, ptrChanInfoV40, false);
                            m_struChanInfoV40 = (CHCNetSDK.NET_DVR_IPCHANINFO_V40)Marshal.PtrToStructure(ptrChanInfoV40, typeof(CHCNetSDK.NET_DVR_IPCHANINFO_V40));
                            //列出IP通道
                            ListIPChannel(i + 1, m_struChanInfoV40.byEnable, m_struChanInfoV40.wIPID, m_iUserID);
                            iIPDevID[i] = m_struChanInfoV40.wIPID - iGroupNo * 64 - 1;
                            Marshal.FreeHGlobal(ptrChanInfoV40);
                            break;
                        default:
                            break;
                    }
                }
            }
            Marshal.FreeHGlobal(ptrIpParaCfgV40);
        }


        /// <summary>
        /// 根据传入通道信息，
        /// </summary>
        /// <param name="iChanNo"></param>
        /// <param name="byOnline"></param>
        /// <param name="byIPID"></param>
        private void ListIPChannel(int iChanNo, byte byOnline, int byIPID,int nNvrid)
        {
            str1 = String.Format("IPCamera{0}", iChanNo);
            m_lTree++;
            if (byIPID==0)
            {
                str2 = "通道空闲";//通道空闲，没有添加前端设备
            }
            else
            {
                if (byOnline==0)
                {
                    str2 = "掉线";//通道不在线
                }
                else
                {
                    str2 = "正常";
                }
            }
            //根据NVR里面的通道，获取到了所有通道在线信息，然后根据nvr id 跟nvr datatable进行关联，将nvr id 与 nvr ip 地址与 nvr通道号一起跟摄像头关联
            for (int i = 0; i < StaticData.g_dtNVR.Rows.Count; i++)
            {
                if (Convert.ToInt32(StaticData.g_dtNVR.Rows[i]["userid"]) == nNvrid)
                {
                    //找到了目前这个函数通道所在的NVR，
                    //然后根据通道号跟NVRip地址，找到摄像头
                    string srtSql = "NVRIP地址= '" + StaticData.g_dtNVR.Rows[i]["IP地址"].ToString() + "'" + " and  通道号 ='" + iChanNo.ToString() + "'";
                    DataRow[] dr = StaticData.g_dtCamera.Select(srtSql);
                    //这一行更新下状态，str2

                    if (dr.Length>0)
                    {
                        dr[0]["摄像头状态"] = str2;
                    } 
                }
            }
            

            //维护摄像头状态字典，需要通过nvr ip ,端口号共同维护    
            dicIPList.Add(iChanNo.ToString(), str2);
        }

        public override void CameraInit(string DVRIPAddress, short DVRPortNumber, string DVRUserName, string DVRPassword)
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

        public override void AlarmCallBackFun(int userID, int channel)
        {
            throw new NotImplementedException();
        }
    }
}
