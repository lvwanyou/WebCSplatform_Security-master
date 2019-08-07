using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Json;
using System.Web;
using System.Net;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace SupportPlatform
{
    /// <summary>
    /// 封装 Park 请求的相关属性
    /// </summary>
    /// return response array
    class ParkingWebRequest
    {
        public string[] Post(string t)
        {
            string Url = StaticData.g_strParkingUrl;
            HttpWebRequest request;
            request = (HttpWebRequest)WebRequest.Create(Url);
            //Post请求方式  
            request.Method = "POST";
            // 内容类型  
            request.ContentType = "application/x-www-form-urlencoded";//application/x-www-form-urlencoded
            // 参数经过URL编码  
            string paraUrlCoded = HttpUtility.UrlEncode("groupId");
            paraUrlCoded += "=" + HttpUtility.UrlEncode(t);
            byte[] payload;
            //将URL编码后的字符串转化为字节  
            payload = Encoding.UTF8.GetBytes(paraUrlCoded);
            //设置请求的 ContentLength   
            request.ContentLength = payload.Length;
            //获得请求流  
            Stream writer = request.GetRequestStream();
            //将请求参数写入流  
            writer.Write(payload, 0, payload.Length);
            // 关闭请求流  
            writer.Close();
            HttpWebResponse response;
            // 获得响应流  
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                StreamReader myreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string responseText = myreader.ReadToEnd();
                myreader.Close();
                JObject jo = (JObject)JsonConvert.DeserializeObject(responseText);
                string num = jo["data"][0]["groupId"].ToString();
                string remain = jo["data"][0]["number"].ToString();
                string name = jo["data"][0]["parkName"].ToString();
                string totalNum = jo["data"][0]["totalNum"].ToString();
                string[] parkNum = { remain, num, name, totalNum };
                return parkNum;
            }
            catch (Exception)
            {

                
            }
            return null ;
            
        }
    }   
}
