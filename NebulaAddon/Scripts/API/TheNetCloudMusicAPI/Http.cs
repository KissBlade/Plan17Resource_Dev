using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

namespace Plana.Core.CloudPlayer
{
    public static class Http
    {
        private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        private static readonly HttpClient client = Create();

        private static HttpClient Create()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                UseCookies = true,
                CookieContainer = new CookieContainer()
            };

            var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(20) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            //http.DefaultRequestHeaders.Referrer = new System.Uri("https://music.163.com/");
            http.DefaultRequestHeaders.Add("Referer", "https://music.163.com/");
            http.DefaultRequestHeaders.Add("Cookie", "os=pc; appver=2.9.7;");
            //http.DefaultRequestHeaders.Add("Referrer", "https://music.163.com/");
            return http;
        }
        static MethodInfo GetStringAsync;
        static MethodInfo GetByteArrayAsync;
        public static IEnumerator CoGetString(string url, Action<string, string> onDone)
        {
            System.Threading.Tasks.Task<string> task;
            try
            {
                if (GetStringAsync == null)
                {
                    GetStringAsync = typeof(HttpClient).GetMethods().First(m => m.Name == "GetStringAsync" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
                }
                task = (System.Threading.Tasks.Task<string>)GetStringAsync.Invoke(client,[url]);
            }
            catch (Exception ex)
            {
                onDone?.Invoke(null, ex.Message);
                yield break;
            }

            while (!task.IsCompleted) yield return new WaitForEndOfFrame();
            if (task.IsFaulted) onDone?.Invoke(null, Describe(task));
            else onDone?.Invoke(task.Result, null);
        }

        public static IEnumerator CoGetBytes(string url, Action<byte[], string> onDone)
        {
            System.Threading.Tasks.Task<byte[]> task;
            try
            {
                if (GetByteArrayAsync == null)
                {
                    GetByteArrayAsync = typeof(HttpClient).GetMethods().First(m => m.Name == "GetByteArrayAsync" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
                }
                task = (System.Threading.Tasks.Task<byte[]>)GetByteArrayAsync.Invoke(client, [url]);
            }
            catch (Exception ex)
            {
                onDone?.Invoke(null, ex.Message);
                yield break;
            }

            while (!task.IsCompleted) yield return new WaitForEndOfFrame();
            if (task.IsFaulted) onDone?.Invoke(null, Describe(task));
            else onDone?.Invoke(task.Result, null);
        }

        private static string Describe(System.Threading.Tasks.Task task)
        {
            var error = task.Exception?.GetBaseException();
            return error?.Message ?? "request failed";
        }
    }
}
