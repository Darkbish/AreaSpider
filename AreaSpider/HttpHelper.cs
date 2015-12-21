#region FileDesc

// ----------------------------------------------------------------------------------------------------------
//  Author : 向春林
//  CreateDate : 2015-10-29
//  ModifyDate: 2015-10-29
//  <copyright file="RequestHelper.cs" company="HTLH">
//  </copyright>
//  <summary>
//  
//  </summary>
// ----------------------------------------------------------------------------------------------------------

#endregion

#region using

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

#endregion

namespace AreaSpider
{
    public class HttpHelper
    {
        /// <summary>
        ///     使用Get方法开始异步请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="content">请求参数</param>
        /// <param name="headers">请求头信息</param>
        /// <returns>请求响应的结果</returns>
        public static Task<string> GetAsync(string url, string content = null,
            IEnumerable<KeyValuePair<string, string>> headers = null)
            => StartAsync(url, HttpMethod.Get, content, headers);


        /// <summary>
        ///     使用Post方法开始异步请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="content">请求参数</param>
        /// <param name="headers">请求头信息</param>
        /// <returns>请求响应的结果</returns>
        public static Task<string> PostAsync(string url, string content,
            IEnumerable<KeyValuePair<string, string>> headers = null)
            => StartAsync(url, HttpMethod.Post, content, headers);


        /// <summary>
        ///     开始异步请求
        /// </summary>
        /// <param name="url">请求的url</param>
        /// <param name="method">请求方法</param>
        /// <param name="content">请求参数</param>
        /// <param name="headers">请求头信息</param>
        /// <returns>请求响应的结果</returns>
        public static async Task<string> StartAsync(string url, HttpMethod method, string content,
            IEnumerable<KeyValuePair<string, string>> headers = null)
        {
            var request = WebRequest.CreateHttp(url);
            request.Method = method.ToString();
            //request.ContentType = "text/html;charset=gb2312";
            //request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            if (!content.IsNullOrWhiteSpace())
            {
                request.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
                request.Accept = "text/html,application/xml";
                var reqStream = await request.GetRequestStreamAsync();
                var buffer = Encoding.UTF8.GetBytes(content);
                await reqStream.WriteAsync(buffer, 0, buffer.Length);
            }

            if (headers != null && headers.Any())
                foreach (var item in headers)
                    request.Headers.Add(item.Key, item.Value);
            var response = await request.GetResponseAsync();
            var resStream = response.GetResponseStream();
            if (resStream == null || resStream == Stream.Null)
                return null;
            using (var reader = new StreamReader(resStream,Encoding.GetEncoding("gb2312")))
                return await reader.ReadToEndAsync();
        }

        /// <summary>
        ///     使用Get方法开始同步请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="content">请求参数</param>
        /// <param name="headers">请求头信息</param>
        /// <returns>请求响应的结果</returns>
        public static string Get(string url, string content = null,
            IEnumerable<KeyValuePair<string, string>> headers = null)
            => Start(url, HttpMethod.Get, content, headers);


        /// <summary>
        ///     使用Post方法开始同步请求
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="content">请求参数</param>
        /// <param name="headers">请求头信息</param>
        /// <returns>请求响应的结果</returns>
        public static string Post(string url, string content,
            IEnumerable<KeyValuePair<string, string>> headers = null)
            => Start(url, HttpMethod.Post, content, headers);


        /// <summary>
        ///     开始同步请求
        /// </summary>
        /// <param name="url">请求的url</param>
        /// <param name="method">请求方法</param>
        /// <param name="content">请求参数</param>
        /// <param name="headers">请求头</param>
        /// <returns>请求响应的结果</returns>
        public static string Start(string url, HttpMethod method, string content,
            IEnumerable<KeyValuePair<string, string>> headers)
        {
            var request = WebRequest.CreateHttp(url);
            request.Method = method.ToString();
            //request.ContentType = "text/html;charset=gb2312";
            //request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            //request.Headers.Add("Accept-Encoding", "gzip, deflate");
            //request.Headers.Add("Accept-Language", "zh-CN,zh;q=0.8,en-US;q=0.5,en;q=0.3");
            //request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:43.0) Gecko/20100101 Firefox/43.0";
            if (!content.IsNullOrWhiteSpace())
            {
                request.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
                request.Accept = "text/html,application/xml";
                var reqStream = request.GetRequestStream();
                var buffer = Encoding.UTF8.GetBytes(content);
                reqStream.Write(buffer, 0, buffer.Length);
            }
            if (headers != null && headers.Any())
                foreach (var item in headers)
                    request.Headers.Add(item.Key, item.Value);

            var response = request.GetResponse();
            var resStream = response.GetResponseStream();
            if (resStream == null || resStream == Stream.Null)
                return null;
            using (var reader = new StreamReader(resStream,Encoding.GetEncoding("gb2312")))
                return reader.ReadToEnd();
        }
    }
}