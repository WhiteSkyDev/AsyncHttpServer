using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

internal class HttpServer
{
    public delegate void HttpRequestDelegate(HttpListenerRequest Reques, HttpListenerResponse Response);
    private HttpListener Listener;
    private Dictionary<string, HttpRequestDelegate> RequestHandlers;
    private void Init()
    {
        Listener = new HttpListener();
        RequestHandlers = new Dictionary<string, HttpRequestDelegate>();
    }
    public HttpServer(string[] Prefixes)
    {
        Init();
        StartServer(Prefixes);
    }
    private void StartServer(string[] Prefixes)
    {
        foreach (string Prefix in Prefixes)
        {
            Listener.Prefixes.Add(Prefix);
        }
        Listener.Start();
        new Thread(() =>
        {
            while (true)
            {
                IAsyncResult Result = Listener.BeginGetContext(HandleRequest, Listener);
                Result.AsyncWaitHandle.WaitOne();
            }
        }).Start();
    }
    private void HandleRequest(IAsyncResult res)
    {
        HttpListenerContext Context = Listener.EndGetContext(res);
        Logger.LogInfo($"Handling request. {Context.Request.HttpMethod} {Context.Request.Url.AbsolutePath} User Agent: {Context.Request.UserAgent} Content Type: {Context.Request.ContentType}");
        if (RequestHandlers.ContainsKey(Context.Request.Url.AbsolutePath))
        {
            RequestHandlers[Context.Request.Url.AbsolutePath](Context.Request, Context.Response);
        }
        else
        {
            Context.Response.StatusCode = 404;
            Context.Response.Close();
        }
    }
    public void AddRoute(string AbsolutePath, HttpRequestDelegate Callback)
    {
        RequestHandlers.Add(AbsolutePath, Callback);
    }
}