using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SupportPlatform
{
    class SkyinforVideoTransform
    {
        private SkyinforVideoTransform()
        {

        }
        private static SkyinforVideoTransform instance = null;
        public static SkyinforVideoTransform getInstance()
        {
            if (instance==null)
            {
                instance = new SkyinforVideoTransform();
            }
            return instance;
        }
        private delegate void CrossThreadOperationControl();
        /// <summary>
        /// 天覆视频服务器，牛逼的服务器！  //打广告，死全家！对，说的就是你！
        /// </summary>
        /// <param name="rtspAdd"></param>
        /// <param name="id"></param>
        /// <param name="nVideoNum"></param>
        /// <returns></returns>
        [DllImport("skyinforVideoServer.dll", EntryPoint = "skyinforVideoShare", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int skyinforVideoShare(string[] rtspAdd, string[] id, int nVideoNum);
        public void SkyinforVideoServer()
        {

            
            List<string> rtspList = new List<string>();
            List<string> idList = new List<string>();
            int k = 0;
            //遍历一下所有的摄像头rtsp流，传入rtsp流地址，通道号，就能实现转换了，不是判断摄像头online，判断device_transcoding_flag，然后传入rtsp地址
            for (int i = 0; i < StaticData.g_dtCamera.Rows.Count; i++)
            {
                //  if (StaticData.g_dtCamera.Rows[i]["摄像头状态"].ToString()=="online")
                if (Convert.ToInt32(StaticData.g_dtCamera.Rows[i]["转码标识"]) == 1)
                {
                    rtspList.Add(StaticData.g_dtCamera.Rows[i]["RTSP地址"].ToString());
                    idList.Add(k.ToString());
                    k++;
                }                
            }


            //显示视频转码路数
            CrossThreadOperationControl CrossDele_VideoCount = delegate ()
            {
                Mainform.form1.textBoxVideo.Text = rtspList.Count + "路";
            };
            Mainform.form1.textBoxVideo.Invoke(CrossDele_VideoCount);
            try
            {
                skyinforVideoShare(rtspList.ToArray(), idList.ToArray(), rtspList.Count);//该处暂时写死了------------
            }
            catch (Exception)
            {
                
                //自己写的代码，肯定有异常，先不管了。。。测试能用了再说
                StaticUtils.ShowEventMsg("SkyinforVideoTransform.class-SkyinforVideoServer : 出现异常！！\n");
                return;
            }

        }
    }
}
