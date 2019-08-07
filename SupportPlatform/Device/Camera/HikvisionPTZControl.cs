using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace SupportPlatform
{
    /// <summary>
    /// 云台控制，还未完善，具体可参考官方demo
    /// </summary>
    class HikvisionPTZControl : Hikvision
    {
        private bool bAuto = false;
        private static HikvisionPTZControl instance;
        private delegate void CrossThreadOperationControl();
        public HikvisionPTZControl()
        {
            CrossThreadOperationControl CrossDeleBtn = delegate ()
            {
                Mainform.form1.kcomboBoxPTZ.SelectedIndex = 3;
                if ( HikvisionPreviewFun.g_lRealHandle>-1)
                {
                    Mainform.form1.kcheckBox_isPreviewON.Checked = true;
                }
                else
                {
                    Mainform.form1.kcheckBox_isPreviewON.Checked = false;
                }
            };
            Mainform.form1.kPictureBox.Invoke(CrossDeleBtn);
        }

        public static HikvisionPTZControl GetInstance()
        {
            if (instance == null)
            {
                instance = new HikvisionPTZControl();
            }
            return instance;
        }

        public void PTZup(int userID, int channel)
        {
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            else
            {
               bool ret= CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.TILT_UP, 0);
                Thread.Sleep(50);
                bool ret2 = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.TILT_UP, 1);
            }

        }
        public void PTZdown(int userID, int channel)
        {
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            else
            {
                bool ret = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.TILT_DOWN, 0);
                Thread.Sleep(50);
                bool ret2 = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.TILT_DOWN, 1);
            }

        }
        public void PTZleft(int userID, int channel)
        {
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            else
            {
                bool ret = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.PAN_LEFT, 0);
                Thread.Sleep(50);
                bool ret2 = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.PAN_LEFT, 1);
            }

        }
        public void PTZright(int userID, int channel)
        {
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            else
            {
                bool ret = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.PAN_RIGHT, 0);
                Thread.Sleep(50);
                bool ret2 = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.PAN_RIGHT, 1);
            }

        }
        public void PTZupleft(int userID, int channel)
        {
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            else
            {
                bool ret = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.UP_LEFT, 0);
                Thread.Sleep(50);
                bool ret2 = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.UP_LEFT, 1);
            }

        }
        public void PTZupright(int userID, int channel)
        {
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            else
            {
                bool ret = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.UP_RIGHT, 0);
                Thread.Sleep(50);
                bool ret2 = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.UP_RIGHT, 1);
            }

        }
        public void PTZdownleft(int userID, int channel)
        {
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            else
            {
                bool ret = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.DOWN_LEFT, 0);
                Thread.Sleep(50);
                bool ret2 = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.DOWN_LEFT, 1);
            }

        }
        public void PTZdownright(int userID, int channel)
        {
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            else
            {
                bool ret = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.DOWN_RIGHT, 0);
                Thread.Sleep(50);
                bool ret2 = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.DOWN_RIGHT, 1);
            }

        }
        public void PTZzoomin(int userID, int channel)
        {
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            else
            {
                bool ret = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.ZOOM_IN, 0);
                Thread.Sleep(100);
                bool ret2 = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.ZOOM_IN, 1);
            }

        }
        public void PTZzoomout(int userID, int channel)
        {
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            else
            {
                bool ret = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.ZOOM_OUT, 0);
                Thread.Sleep(100);
                bool ret2 = CHCNetSDK.NET_DVR_PTZControl_Other(userID, channel + 32, CHCNetSDK.ZOOM_OUT, 1);
            }

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

        /// <summary>
        /// 重写下云台控制
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="channel"></param>
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
