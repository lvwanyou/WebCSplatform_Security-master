using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SupportPlatform
{
    class HikvisionPreviewFun : Hikvision
    {

        private static HikvisionPreviewFun instance;
        private Int32 m_lPort = -1;
        public static Int32 g_lRealHandle = -1;     //预览
        private IntPtr m_ptrRealHandle = Mainform.form1.kPictureBox.Handle;  //预览用控件句柄
        PictureBox m_picturebox; //定义一个本地的PictureBox来存储主窗口中的picturebox
        private delegate void CrossThreadOperationControl();

        private HikvisionPreviewFun()
        {

        }
        public static HikvisionPreviewFun GetInstance()
        {
            if (instance==null)
            {
                instance = new HikvisionPreviewFun();
            }
            return instance;
        }
        private void CrossThreadPictureBox()
        {
            CrossThreadOperationControl CrossDele = delegate ()
            {
                m_picturebox = Mainform.form1.kPictureBox;
            };
            Mainform.form1.kPictureBox.Invoke(CrossDele);
        }
        public override void CameraInit(string DVRIPAddress, short DVRPortNumber, string DVRUserName, string DVRPassword)
        {
            throw new NotImplementedException();
        }

        public override void IPListFun(int userID)
        {
            throw new NotImplementedException();
        }

        public override void PreviewFun(int userID,int channel)
        {
            CrossThreadPictureBox();
            if (userID < 0)
            {
                MessageBox.Show("该设备初始化登录失败!");
            }
            if (g_lRealHandle<0)
            {
               // m_ptrRealHandle = m_picturebox.Handle;
                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();  //定义预览参数
                lpPreviewInfo.hPlayWnd = m_picturebox.Handle;    //预览窗口句柄绑定
                lpPreviewInfo.lChannel = channel+32;   //要预览的设备通道号
                lpPreviewInfo.dwStreamType = 0;//码流类型：0-主码流，1-子码流，2-码流3，3-码流4，以此类推
                lpPreviewInfo.dwLinkMode = 0;//连接方式：0- TCP方式，1- UDP方式，2- 多播方式，3- RTP方式，4-RTP/RTSP，5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = true; //0- 非阻塞取流，1- 阻塞取流
                lpPreviewInfo.dwDisplayBufNum = 15; //播放库显示缓冲区最大帧数
                IntPtr pUser = IntPtr.Zero;//用户数据
                lpPreviewInfo.hPlayWnd = IntPtr.Zero;//预览窗口 live view window
                RealData = new CHCNetSDK.REALDATACALLBACK(RealDataCallBack);//预览实时流回调函数 real-time stream callback function 
                g_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(userID, ref lpPreviewInfo, RealData, pUser);
                if (g_lRealHandle < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    MessageBox.Show("预览失败，输出错误号: " + iLastErr); //预览失败，输出错误号 
                    return;
                }
                else
                {
                    CrossThreadOperationControl CrossDeleBtn = delegate ()
                    {
                        Mainform.form1.kButtonView.Text = "停止预览";
                    };
                    Mainform.form1.kPictureBox.Invoke(CrossDeleBtn);


                }
            }
            else
            {
                //停止预览 Stop live view 
                if (!CHCNetSDK.NET_DVR_StopRealPlay(g_lRealHandle))
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    MessageBox.Show("NET_DVR_StopRealPlay failed, error code= " + iLastErr);
                    return;
                }
                m_picturebox.Invalidate();//刷新窗口
                m_picturebox.Refresh();
                g_lRealHandle = -1;
                StaticData.g_CameraSelectChannel = -1;
                CrossThreadOperationControl CrossDeleBtn = delegate ()
                {
                    Mainform.form1.kButtonView.Text = "预览";
                };
                Mainform.form1.kPictureBox.Invoke(CrossDeleBtn);
            }
        }

        /// <summary>
        /// 实时预览的回调函数
        /// </summary>
        /// <param name="lRealHandle"></param>
        /// <param name="dwDataType"></param>
        /// <param name="pBuffer"></param>
        /// <param name="dwBufSize"></param>
        /// <param name="pUser"></param>
        public void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            //下面数据处理建议使用委托的方式
            switch (dwDataType)
            {
                case CHCNetSDK.NET_DVR_SYSHEAD:     // sys head
                    if (dwBufSize > 0)
                    {
                        if (m_lPort >= 0)
                        {
                            return; //同一路码流不需要多次调用开流接口
                        }

                        //获取播放句柄 Get the port to play
                        if (!PlayCtrl.PlayM4_GetPort(ref m_lPort))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            MessageBox.Show("PlayM4_GetPort failed, error code= " + iLastErr);
                            break;
                        }

                        //设置流播放模式 Set the stream mode: real-time stream mode
                        if (!PlayCtrl.PlayM4_SetStreamOpenMode(m_lPort, PlayCtrl.STREAME_REALTIME))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            MessageBox.Show("Set STREAME_REALTIME mode failed, error code= " + iLastErr);
                        }

                        //打开码流，送入头数据 Open stream
                        if (!PlayCtrl.PlayM4_OpenStream(m_lPort, pBuffer, dwBufSize, 2 * 1024 * 1024))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            MessageBox.Show("PlayM4_OpenStream failed, error code= " + iLastErr);
                            break;
                        }


                        //设置显示缓冲区个数 Set the display buffer number
                        if (!PlayCtrl.PlayM4_SetDisplayBuf(m_lPort, 15))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            MessageBox.Show("PlayM4_SetDisplayBuf failed, error code= " + iLastErr);
                        }

                        //设置显示模式 Set the display mode
                        if (!PlayCtrl.PlayM4_SetOverlayMode(m_lPort, 0, 0/* COLORREF(0)*/)) //play off screen 
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            MessageBox.Show("PlayM4_SetOverlayMode failed, error code= " + iLastErr);
                        }

                        //设置解码回调函数，获取解码后音视频原始数据 Set callback function of decoded data
                        m_fDisplayFun = new PlayCtrl.DECCBFUN(DecCallbackFUN);
                        if (!PlayCtrl.PlayM4_SetDecCallBackEx(m_lPort, m_fDisplayFun, IntPtr.Zero, 0))
                        {
                            MessageBox.Show("PlayM4_SetDisplayCallBack fail");
                        }

                        //开始解码 Start to play                       
                        if (!PlayCtrl.PlayM4_Play(m_lPort, m_ptrRealHandle))
                        {
                            iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                            MessageBox.Show("PlayM4_Play failed, error code= " + iLastErr);
                            break;
                        }
                    }
                    break;
                case CHCNetSDK.NET_DVR_STREAMDATA:     // video stream data
                    if (dwBufSize > 0 && m_lPort != -1)
                    {
                        for (int i = 0; i < 999; i++)
                        {
                            //送入码流数据进行解码 Input the stream data to decode
                            if (!PlayCtrl.PlayM4_InputData(m_lPort, pBuffer, dwBufSize))
                            {
                                iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                                MessageBox.Show("PlayM4_InputData failed, error code= " + iLastErr);
                                Thread.Sleep(2);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    break;
                default:
                    if (dwBufSize > 0 && m_lPort != -1)
                    {
                        //送入其他数据 Input the other data
                        for (int i = 0; i < 999; i++)
                        {
                            if (!PlayCtrl.PlayM4_InputData(m_lPort, pBuffer, dwBufSize))
                            {
                                iLastErr = PlayCtrl.PlayM4_GetLastError(m_lPort);
                                MessageBox.Show("PlayM4_InputData failed, error code= " + iLastErr);
                                Thread.Sleep(2);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    break;
            }
        }
        /// <summary>
        /// 解码回调函数
        /// </summary>
        /// <param name="nPort"></param>
        /// <param name="pBuf"></param>
        /// <param name="nSize"></param>
        /// <param name="pFrameInfo"></param>
        /// <param name="nReserved1"></param>
        /// <param name="nReserved2"></param>
        private static void DecCallbackFUN(int nPort, IntPtr pBuf, int nSize, ref PlayCtrl.FRAME_INFO pFrameInfo, int nReserved1, int nReserved2)
        {
            // 将pBuf解码后视频输入写入文件中（解码后YUV数据量极大，尤其是高清码流，不建议在回调函数中处理）
            if (pFrameInfo.nType == 3) //#define T_YV12	3
            {
                //    FileStream fs = null;
                //    BinaryWriter bw = null;
                //    try
                //    {
                //        fs = new FileStream("DecodedVideo.yuv", FileMode.Append);
                //        bw = new BinaryWriter(fs);
                //        byte[] byteBuf = new byte[nSize];
                //        Marshal.Copy(pBuf, byteBuf, 0, nSize);
                //        bw.Write(byteBuf);
                //        bw.Flush();
                //    }
                //    catch (System.Exception ex)
                //    {
                //        MessageBox.Show(ex.ToString());
                //    }
                //    finally
                //    {
                //        bw.Close();
                //        fs.Close();
                //    }
            }
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
