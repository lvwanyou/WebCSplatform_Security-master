using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SupportPlatform
{
    class HikvisionInit : Hikvision
    {
        public override void AlarmCallBackFun(int userID, int channel)
        {
            throw new NotImplementedException();
        }

        public override void CameraInit(string DVRIPAddress, short DVRPortNumber, string DVRUserName, string DVRPassword)
        {
            m_iUserID = -1;
            m_bInitSDK = CHCNetSDK.NET_DVR_Init();
            if (!m_bInitSDK)
            {
                Console.WriteLine("初始化SDK失败。。。");

            }
            else
            {
                for (int i = 0; i < 64; i++)   //将64路全部置为-1
                {
                    iIPDevID[i] = -1;
                    iChannelNum[i] = -1;
                }
            }

            m_iUserID = CHCNetSDK.NET_DVR_Login_V30(DVRIPAddress, DVRPortNumber, DVRUserName, DVRPassword, ref DeviceInfo);
            if (m_iUserID < 0)
            {
                iLastErr = CHCNetSDK.NET_DVR_GetLastError();  //返回最后操作的错误码
                string str = "登录失败，错误号为" + iLastErr;
                Console.WriteLine(str);
                return;
            }
            else
            {
        //        Console.WriteLine("登陆成功！");
            }
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
    }
}
