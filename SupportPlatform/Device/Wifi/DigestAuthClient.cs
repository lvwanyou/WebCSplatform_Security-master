using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SupportPlatform.Device.Wifi
{

    /// <summary>
    ///  利用WebClient进行摘要认证
    /// </summary>
    class DigestAuthClient
    {
        public static string Request(string sUrl, string sMethod, out string sMessage)
        {
            try
            {
                sMessage = "";
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                    client.Credentials = CreateAuthenticateValue(sUrl);
                    skyinforHttpAPI.SetHeaderValue(client.Headers, "accept", "application/json");

                    Uri url = new Uri(sUrl);
                    byte[] buffer=new byte[] { };         
                    if (sMethod.ToUpper().Equals("GET"))
                    {
                        buffer = client.DownloadData(url);
                    }
                    return Encoding.UTF8.GetString(buffer);
                }
            }
            catch (WebException ex)
            {
                sMessage = ex.Message;
                var rsp = ex.Response as HttpWebResponse;
                var httpStatusCode = rsp.StatusCode;
                var authenticate = rsp.Headers.Get("WWW-Authenticate");
                return "";
            }
            catch (Exception ex)
            {
                sMessage = ex.Message;
                return "";
            }
        }

        /// <summary>
        /// 设置摘要认证
        /// </summary>
        private static CredentialCache CreateAuthenticateValue(string sUrl)
        {
            CredentialCache credentialCache = new CredentialCache();
            credentialCache.Add(new Uri(sUrl), "Digest", new NetworkCredential("admin", "admin"));
            return credentialCache;
        }

    }
}
