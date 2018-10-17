using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Configuration;
using System.Web.Configuration;
using System.Net.Http;
using System.Text;

namespace BoomiAPIGateway
{
    public class RequestHandler : IHttpHandler
    {
        /*read settings from "Application settings" of Web App*/
        static string server = WebConfigurationManager.AppSettings["server"];
        static string user = WebConfigurationManager.AppSettings["user"];
        static string pwd = WebConfigurationManager.AppSettings["pwd"];

        public bool IsReusable
        {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var path = context.Request.Path;
            var verb = context.Request.HttpMethod.ToUpper();
            if (path == "/") return;
            var target = new Uri(new Uri(server), path);

            switch (verb)
            { 
                case "PUT":
                    ProcessRequestInternal(context, target.ToString(), PutRequestDelegate);
                    break;
                case "DELETE":
                    ProcessRequestInternal(context, target.ToString(), DeleteRequestDelegate);
                    break;
                case "POST":
                    ProcessRequestInternal(context, target.ToString(), PostRequestDelegate);
                    break;
                default: //"GET"
                    ProcessRequestInternal(context, target.ToString(), GetRequestDelegate);
                    break;
            }            
        }

        /// <summary>
        /// Get or default request implementation
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Client"></param>
        /// <param name="Target"></param>
        /// <returns></returns>
        private HttpResponseMessage GetRequestDelegate(HttpContext Context, HttpClient Client, string Target)
        {
            return Client.GetAsync(Target.ToString()).Result;
        }

        /// <summary>
        /// Put request implementation
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Client"></param>
        /// <param name="Target"></param>
        /// <returns></returns>
        private HttpResponseMessage PutRequestDelegate(HttpContext Context, HttpClient Client, string Target)
        {
            var putContent = new StreamReader(Context.Request.InputStream).ReadToEnd();
            var content = new StringContent(putContent, Encoding.UTF8, "application/json");
            return Client.PutAsync(Target.ToString(), content).Result;
        }

        /// <summary>
        /// Delete request implementation
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Client"></param>
        /// <param name="Target"></param>
        /// <returns></returns>
        private HttpResponseMessage DeleteRequestDelegate(HttpContext Context, HttpClient Client, string Target)
        {
            return Client.DeleteAsync(Target.ToString()).Result;
        }

        /// <summary>
        /// POST request implementation
        /// </summary>
        /// <param name="Context"></param>
        /// <param name="Client"></param>
        /// <param name="Target"></param>
        /// <returns></returns>
        private HttpResponseMessage PostRequestDelegate(HttpContext Context, HttpClient Client, string Target)
        {
            var putContent = new StreamReader(Context.Request.InputStream).ReadToEnd();
            var content = new StringContent(putContent, Encoding.UTF8, "application/json");
            return Client.PostAsync(Target.ToString(), content).Result;
        }


        /// <summary>
        /// Internal processing request as proxy
        /// </summary>
        /// <param name="context"></param>
        /// <param name="target"></param>
        /// <param name="metodDelegate"></param>
        private void ProcessRequestInternal(HttpContext context, string target, Func<HttpContext, HttpClient, String, HttpResponseMessage> metodDelegate)
        {
            using (HttpClient client = new HttpClient())
            {
                var basicauthheader = Encoding.ASCII.GetBytes(user + ":" + pwd);
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(basicauthheader));

                //get server response
                using (HttpResponseMessage responseServer = metodDelegate.Invoke(context, client, target)) 
                {
                    using (HttpContent contentServer = responseServer.Content)
                    {
                        //if server return 404 and etc, return that to requester.
                        if (!responseServer.IsSuccessStatusCode)
                        {
                            context.Response.StatusCode = (int)responseServer.StatusCode;
                            return;
                        }

                        string result = contentServer.ReadAsStringAsync().Result;
                        context.Response.ClearHeaders();
                        context.Response.ContentType = (contentServer.Headers.ContentType !=null) ? contentServer.Headers.ContentType.ToString() : "application/json";
                        context.Response.Write(result);
                    }

                }
            }
        }

        public void Dispose() { }
    }
}