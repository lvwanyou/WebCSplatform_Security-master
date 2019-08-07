using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportPlatform
{
    abstract class Hikvision:CameraNVR
    {
        public static int m_iUserID = -1;     
        public  uint iLastErr = 0; //错误号
        public  uint dwAChanTotalNum = 0;//模拟通道个数
        public  uint dwDChanTotalNum = 0;//数字通道个数
        public  int[] iIPDevID = new int[96];//设备ID号
        public  int[] iChannelNum = new int[96];//通道号
        public  string str1;//channel通道
        public  string str2;//状态
        public  int m_lTree = 0;
        public Dictionary<string, string> dicIPList = new Dictionary<string, string>();  //IP列表字典
        public bool m_bInitSDK = false;//初始化SDK
        public  static CHCNetSDK.NET_DVR_DEVICEINFO_V30 DeviceInfo; //设备信息
        public  static CHCNetSDK.NET_DVR_IPPARACFG_V40 m_struIpParaCfgV40;//IP资源信息 模拟通道是否禁用，IP通道个数等
        public  static CHCNetSDK.NET_DVR_IPCHANINFO m_struChanInfo;//IP通道信息
        public  static CHCNetSDK.NET_DVR_PU_STREAM_URL m_struStreamURL;//URL取流配置
        public  static CHCNetSDK.NET_DVR_IPCHANINFO_V40 m_struChanInfoV40;//IP通道信息扩展
        public  static CHCNetSDK.REALDATACALLBACK RealData = null;//实时预览的回调函数
        public  static PlayCtrl.DECCBFUN m_fDisplayFun = null;    //解码的回调函数

        public abstract void CameraInit(string DVRIPAddress, short DVRPortNumber, string DVRUserName, string DVRPassword);//初始化设备
        public abstract void IPListFun(int userID);//获取设备列表
        public abstract void PreviewFun(int userID, int channel);//实时预览
        public abstract void PTZControl(int userID, int channel);//云台控制
        public abstract void AlarmCallBackFun(int userID, int channel);//报警回调函数方法
         
    }
}
