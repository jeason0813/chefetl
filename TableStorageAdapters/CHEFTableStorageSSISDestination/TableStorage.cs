using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Security.Policy;
using System.Security.Cryptography;

namespace CHEFTableStorageSSISDestination
{
    static public class TableStorage
    {
        static private string contentType = "application/atom+xml";
        static private string KeyType = "SharedKey";
        static public String Account = String.Empty;
        static private String SharedKey = String.Empty;

        static public void UploadData(String EntityData, DateTime RequestDate, string AccountName, string AccountKey, string EndPoint, string TableName)
        {
            Account = AccountName;
            SharedKey = AccountKey;
            string urlMask = EndPoint;
            string endPoint = string.Format(urlMask, Account);
            string ContentMD5 = string.Empty;
            string AuthorizationValue = string.Empty;
            string SignedValue = string.Empty;
            string AuthorizationHeader = string.Empty;
            string HttpMethod = "POST";
            string ReturnBody = string.Empty;
            string RequestBody = string.Empty;
            string canonicalResource = string.Format("/{0}/{1}", Account, TableName);
            string requestUrl = string.Format("{0}/{1}?", endPoint, TableName);
            DateTime requestDate = RequestDate;// DateTime.UtcNow;
            RequestBody = EntityData.ToString();
            ContentMD5 = MD5(RequestBody);
            AuthorizationHeader = CreateSharedKeyAuth(HttpMethod, canonicalResource, ContentMD5, RequestDate);
            WebRequest req = null;
            WebResponse resp = null;
            try
            {
                req = WebRequest.Create(requestUrl);
                req.Headers.Add("content-md5", ContentMD5);
                req.Headers.Add("x-ms-date", string.Format("{0:R}", RequestDate));
                req.Headers.Add("authorization", AuthorizationHeader);
                req.ContentType = contentType;
                req.ContentLength = Encoding.UTF8.GetBytes(RequestBody).Length;
                req.Method = HttpMethod;
                using (StreamWriter sw = new StreamWriter(req.GetRequestStream()))
                {
                    sw.Write(RequestBody);
                    sw.Close();
                }

                resp = req.GetResponse();
            }
            catch (WebException ex)
            {
                throw ex;
            }
            finally
            {
                req = null;
                if (resp != null) { resp.Close(); }
                resp = null;
            }
        }
        static private string CreateSharedKeyAuth(string method, string resource, string contentMD5, DateTime requestDate)
        {
            try
            {
                string rtn = string.Empty;
                string fmtHeader = "{0} {1}:{2}";
                string fmtStringToSign = "{0}\n{1}\n{2}\n{3:R}\n{4}";

                string authValue = string.Format(fmtStringToSign, method, contentMD5, contentType, requestDate, resource);
                string sigValue = MacSha(authValue, Convert.FromBase64String(SharedKey));
                rtn = string.Format(fmtHeader, KeyType, Account, sigValue);
                return rtn;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        static string MacSha(string canonicalizedString, byte[] key)
        {
            byte[] dataToMAC = System.Text.Encoding.UTF8.GetBytes(canonicalizedString);

            using (HMACSHA256 hmacsha1 = new HMACSHA256(key))
            {
                return System.Convert.ToBase64String(hmacsha1.ComputeHash(dataToMAC));
            }
        }
        static string MD5(string data)
        {
            return MD5(data, false);
        }
        static string MD5(string data, bool removeTail)
        {
            string rtn = Convert.ToBase64String(new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(System.Text.Encoding.Default.GetBytes(data)));
            if (removeTail)
                return rtn.Replace("=", "");
            else
                return rtn;
        }
    }
}

