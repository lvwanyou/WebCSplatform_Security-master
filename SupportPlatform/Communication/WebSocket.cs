using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SupportPlatform
{
    public class WebSocket
    {
        public static Dictionary<string, Socket> m_listClient = null; //保存了服务器端所有负责和客户端通信的套接字
        public static Dictionary<string, Thread> dictThread = null;//保存了服务器端所有负责调用通信套接字Receive方法的线程
        MyJsonCtrl myJson = null;
        public Thread theStartListne;//开启监听的线程
        static int port = Int32.Parse(StaticData.g_strWebSocketPort);
        static IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);
        Socket m_listener = new Socket(localEP.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        private delegate void CrossThreadOperationControl();

        /// <summary>
        /// 得到本地Ip地址 || 在本地有两个ipv4 首选地址时，getIp得到第一个IP
        /// </summary>
        private void GetIP()
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "ipconfig.exe";//设置程序名     
            cmd.StartInfo.Arguments = "/all";  //参数     
                                               //重定向标准输出     
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.CreateNoWindow = true;//不显示窗口（控制台程序是黑屏）     
            //cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;//暂时不明白什么意思     
            /*  
            收集一下 有备无患  
            关于:ProcessWindowStyle.Hidden隐藏后如何再显示？  
            hwndWin32Host = Win32Native.FindWindow(null, win32Exinfo.windowsName);  
            Win32Native.ShowWindow(hwndWin32Host, 1);     //先FindWindow找到窗口后再ShowWindow  
            */
            cmd.Start();
            string info = cmd.StandardOutput.ReadToEnd();
            string[] strArray = info.Split(new string[] { "IPv4 地址 . . . . . . . . . . . . : ", "(首选)" }, StringSplitOptions.RemoveEmptyEntries);

            //StaticUtils.showLog(info);
            cmd.WaitForExit();
            cmd.Close();
            StaticData.g_strLocalIP = strArray[2];
            //StaticUtils.showLog(StaticData.g_strLocalIP);
        }


        public WebSocket()
        {
            try
            {
                m_listClient = new Dictionary<string, Socket>();
                dictThread = new Dictionary<string, Thread>();
                GetIP();
                m_listener.Bind(localEP);
                m_listener.Listen(50);
                //   Console.WriteLine("等待客户端连接");
                //开启客户端监听线程
                theStartListne = new Thread(StartListen);
                theStartListne.IsBackground = true;
                theStartListne.Start();
            }
            catch (Exception)
            {
                StaticUtils.ShowEventMsg("MgJsonCtrl.class-ReceiceJson : 数据格式错误，请输入标准Json格式或产生异常！\n");
                //  throw;
            }
        }

        private void StartListen()
        {
            Socket clientSocket = default(Socket);
            string strClientIP;
            while (true)
            {
                try
                {
                    clientSocket = m_listener.Accept();//这个方法返回一个通信套接字，并用这个套接字进行通信，错误时返回-1并设置全局错误变量
                }
                catch (Exception e)
                {
                    StaticUtils.ShowEventMsg("MgJsonCtrl.class-StartListen : 监听Json出现异常！");
                    Console.WriteLine(e);
                }
                Byte[] bytesForm = new byte[1024 * 1024];
                if (clientSocket != null && clientSocket.Connected)
                {
                    try
                    {
                        int length = clientSocket.Receive(bytesForm);
                        clientSocket.Send(PackHandShakeData(GetSecKeyAccept(bytesForm, length)));
                        strClientIP = clientSocket.RemoteEndPoint.ToString();//得到客户端IP                                             
                                                                             //      Console.WriteLine(strClientIP );
                        if (m_listClient.ContainsKey(strClientIP))
                        {
                            m_listClient[strClientIP] = clientSocket;
                        }
                        else
                        {
                            m_listClient.Add(strClientIP, clientSocket);
                            CrossThreadOperationControl CrossDele = delegate ()
                            {
                                Mainform.form1.listViewIPClient.Items.Add(strClientIP);
                            };
                            Mainform.form1.listViewIPClient.Invoke(CrossDele);
                        }
                        //创建一个通信线程
                        ParameterizedThreadStart pts = new ParameterizedThreadStart(recv);
                        Thread thread = new Thread(pts);
                        thread.IsBackground = true;
                        thread.Start(clientSocket);
                        dictThread.Add(clientSocket.RemoteEndPoint.ToString(), thread);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        StaticUtils.ShowEventMsg("MgJsonCtrl.class-StartListen : 监听Json出现异常！\n");
                    }
                }
            }
        }
        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="socketclientpara"></param>
        public void recv(object socketclientpara)
        {
            Socket socketClient = socketclientpara as Socket;
            myJson = new MyJsonCtrl();
            while (true)
            {
                //创建一个内存缓冲区，其大小为1024*1024字节  即1M 
                byte[] arrServerRecMsg = new byte[1024 * 1024];
                //将接收到的信息存入到内存缓冲区，并返回其字节数组的长度             
                try
                {
                    int length = socketClient.Receive(arrServerRecMsg);
                    //处理收到的数据
                    string strSRecMsg = AnalyticData(arrServerRecMsg, length);
                    //   Console.WriteLine(strSRecMsg);

                    ///
                    ///zzt重要，这里是解析每个web客户端发过来命令的地方
                    ///
                    string strReturn = myJson.ReceiceJson(strSRecMsg);
                    SendMessage(socketClient, strReturn);
                    //  Console.WriteLine(strReturn);

                }
                catch (SocketException ex)
                {
                    StaticUtils.ShowEventMsg("MgJsonCtrl.class-recv : 接收数据出现异常！\n");
                    Console.WriteLine("异常抛出" + ex + ",RemoteEnd=" + socketClient.RemoteEndPoint.ToString());
                    //从通信套接字集合中删除被中断连接的通信套接字  
                    m_listClient.Remove(socketClient.RemoteEndPoint.ToString());//删除异常的端口和IP
                    Thread stopThread = dictThread[socketClient.RemoteEndPoint.ToString()];
                    //从通信线程中删除被中断的连接通信线程对象                       
                    dictThread.Remove(socketClient.RemoteEndPoint.ToString());//删除异常的线程
                    CrossThreadOperationControl CrossDele = delegate ()
                    {
                        for (int j = 0; j < Mainform.form1.listViewIPClient.Items.Count; j++)
                        {
                            if (Mainform.form1.listViewIPClient.Items[j].Text.Equals(socketClient.RemoteEndPoint.ToString()))
                            {
                                Mainform.form1.listViewIPClient.Items.Remove(Mainform.form1.listViewIPClient.Items[j]);
                                j--;
                            }
                        }
                    };
                    Mainform.form1.listViewIPClient.Invoke(CrossDele);
                    stopThread.Abort();
                    return;
                }
            }
        }
        /// <summary>
        /// 发送数据到客户端
        /// </summary>
        /// <param name="sc"></param>
        /// <param name="str"></param>
        public void SendMessage(Socket sc, string str)
        {
            try
            {
                sc.Send(PackData(str));
            }
            catch (Exception e)
            {
                StaticUtils.ShowEventMsg("MgJsonCtrl.class-SendMessage : 发送数据到客户端出现异常！\n");
                Console.WriteLine(e);

            }
        }

        /// <summary>
        /// 解析客户端数据包
        /// </summary>
        /// <param name="recBytes">服务器接收的数据包</param>
        /// <param name="recByteLength">有效数据长度</param>
        /// <returns></returns>
        private static string AnalyticData(byte[] recBytes, int recByteLength)
        {
            if (recByteLength < 2) { return string.Empty; }

            bool fin = (recBytes[0] & 0x80) == 0x80; // 1bit，1表示最后一帧  
            if (!fin)
            {
                return string.Empty;// 超过一帧暂不处理 
            }

            bool mask_flag = (recBytes[1] & 0x80) == 0x80; // 是否包含掩码  
            if (!mask_flag)
            {
                return string.Empty;// 不包含掩码的暂不处理
            }

            int payload_len = recBytes[1] & 0x7F; // 数据长度  

            byte[] masks = new byte[4];
            byte[] payload_data;

            if (payload_len == 126)
            {
                Array.Copy(recBytes, 4, masks, 0, 4);
                payload_len = (UInt16)(recBytes[2] << 8 | recBytes[3]);
                payload_data = new byte[payload_len];
                Array.Copy(recBytes, 8, payload_data, 0, payload_len);

            }
            else if (payload_len == 127)
            {
                Array.Copy(recBytes, 10, masks, 0, 4);
                byte[] uInt64Bytes = new byte[8];
                for (int i = 0; i < 8; i++)
                {
                    uInt64Bytes[i] = recBytes[9 - i];
                }
                UInt64 len = BitConverter.ToUInt64(uInt64Bytes, 0);

                payload_data = new byte[len];
                for (UInt64 i = 0; i < len; i++)
                {
                    payload_data[i] = recBytes[i + 14];
                }
            }
            else
            {
                Array.Copy(recBytes, 2, masks, 0, 4);
                payload_data = new byte[payload_len];
                Array.Copy(recBytes, 6, payload_data, 0, payload_len);

            }

            for (var i = 0; i < payload_len; i++)
            {
                payload_data[i] = (byte)(payload_data[i] ^ masks[i % 4]);
            }

            return Encoding.UTF8.GetString(payload_data);
        }

        /// <summary>
        /// 打包服务器数据
        /// </summary>
        /// <param name="message">数据</param>
        /// <returns>数据包</returns>
        private static byte[] PackData(string message)
        {
            byte[] contentBytes = null;
            byte[] temp = Encoding.UTF8.GetBytes(message);

            if (temp.Length < 126)
            {
                contentBytes = new byte[temp.Length + 2];
                contentBytes[0] = 0x81;
                contentBytes[1] = (byte)temp.Length;
                Array.Copy(temp, 0, contentBytes, 2, temp.Length);
            }
            else if (temp.Length < 0xFFFF)
            {
                contentBytes = new byte[temp.Length + 4];
                contentBytes[0] = 0x81;
                contentBytes[1] = 126;
                contentBytes[2] = (byte)(temp.Length >> 8 & 0xFF);
                contentBytes[3] = (byte)(temp.Length & 0xFF);
                Array.Copy(temp, 0, contentBytes, 4, temp.Length);
            }
            else
            {
                // 暂不处理超长内容  
                Console.WriteLine(66666666);
            }

            return contentBytes;
        }
        /// <summary>
        /// 打包握手信息
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private byte[] PackHandShakeData(string secKeyAccept)
        {
            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + Environment.NewLine);
            responseBuilder.Append("Upgrade:websocket" + Environment.NewLine);
            responseBuilder.Append("Connection:Upgrade" + Environment.NewLine);
            responseBuilder.Append("Sec-WebSocket-Accept:" + secKeyAccept + Environment.NewLine + Environment.NewLine);
            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }
        /// <summary>
        /// 生成Sec-web socket-accept
        /// </summary>
        /// <param name="handShankeBytes">客户端握手信息</param>
        /// <param name="bytesLength"></param>
        /// <returns></returns>
        private string GetSecKeyAccept(byte[] handShankeBytes, int bytesLength)
        {
            string handShakeText = Encoding.UTF8.GetString(handShankeBytes, 0, bytesLength);
            string key = string.Empty;
            Regex r = new Regex(@"Sec\-WebSocket\-Key:(.*?)\r\n");
            Match m = r.Match(handShakeText);
            if (m.Groups.Count != 0)
            {
                key = Regex.Replace(m.Value, @"Sec\-WebSocket\-Key:(.*?)\r\n", "$1").Trim();
            }
            byte[] encryptionString = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            return Convert.ToBase64String(encryptionString);
        }
    }
}
