using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

public interface IHttpServerExtension
{
    bool Execute(HttpListenerContext Context);
}
public class HttpServer : IDisposable
{
    public delegate void HttpRequestDelegate(HttpListenerRequest Request, HttpListenerResponse Response);
    private HttpListener Listener;
    private Dictionary<string, HttpRequestDelegate> RequestCallbacks;
    private Dictionary<string, List<IHttpServerExtension>> Extensions;
    private bool Disposed = false;
    private string NotFoundedErrorPage = string.Empty;

    private void Init()
    {
        Listener = new HttpListener();
        RequestCallbacks = new Dictionary<string, HttpRequestDelegate>();
        Extensions = new Dictionary<string, List<IHttpServerExtension>>();
    }
    public HttpServer(string[] Prefixes)
    {
        Init();
        StartServer(Prefixes);
    }
    public HttpServer(string[] Prefixes, string NotFoundedErrorRedirect)
    {
        NotFoundedErrorPage = NotFoundedErrorRedirect;
        Init();
        StartServer(Prefixes);
    }
    private void StartServer(string[] Prefixes)
    {
        foreach (string Prefix in Prefixes)
        {
            Listener.Prefixes.Add(Prefix);
            Logger.LogInfo("Added prefix: " + Prefix);
        }
        Listener.Start();
        Logger.LogInfo("Http server started.");

        new Task(() =>
        {
            while (!Disposed)
            {
                
                IAsyncResult Result = Listener.BeginGetContext(HandleRequest, Listener);
                Result.AsyncWaitHandle.WaitOne();
            }
        }).Start();
    }
    private void HandleRequest(IAsyncResult res)
    {
        HttpListenerContext HttpContext = Listener.EndGetContext(res);

        string AbsoluteUrl = HttpContext.Request.Url.AbsolutePath;
        if (RequestCallbacks.ContainsKey(AbsoluteUrl) && !HttpContext.Request.IsWebSocketRequest)
        {
            Logger.LogInfo($"Handling http request. {HttpContext.Request.HttpMethod} {AbsoluteUrl} User Agent: {HttpContext.Request.UserAgent} Content Type: {HttpContext.Request.ContentType}");

            if(Extensions.ContainsKey(AbsoluteUrl))
            {
                List<IHttpServerExtension> CurrentRequestExtensions = Extensions[AbsoluteUrl];
                if (CurrentRequestExtensions != null && CurrentRequestExtensions.Count > 0)
                {
                    foreach(IHttpServerExtension Extension in CurrentRequestExtensions)
                    {
                        if(!Extension.Execute(HttpContext))
                        {
                            HttpContext.Response.Close();
                            return;
                        }
                    }
                }
            }

            RequestCallbacks[AbsoluteUrl](HttpContext.Request, HttpContext.Response);
        }
        else if (NotFoundedErrorPage == string.Empty)
        {
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.Close();
        }
        else if (NotFoundedErrorPage != string.Empty && RequestCallbacks.ContainsKey(NotFoundedErrorPage))
        {
            HttpContext.Response.Redirect(NotFoundedErrorPage);
            HttpContext.Response.Close();
        }
    }
    public void AddRoute(string AbsolutePath, HttpRequestDelegate Callback, IHttpServerExtension[] Extensions = null)
    {
        if (!RequestCallbacks.ContainsKey(AbsolutePath))
        {
            RequestCallbacks.Add(AbsolutePath, Callback);
        }
        else
        {
            Logger.LogError("This route already exist!");
        }

        if(Extensions != null)
        {
            if (Extensions.Length > 0 && !this.Extensions.ContainsKey(AbsolutePath))
            {
                this.Extensions.Add(AbsolutePath, Extensions.ToList());
            }
            else
            {
                Logger.LogError("This extensions already exist!");
            }
        }
    }
    public void RemoveRoute(string AbsolutePath)
    {
        if (RequestCallbacks.ContainsKey(AbsolutePath))
        {
            RequestCallbacks.Remove(AbsolutePath);
        }
        else
        {
            Logger.LogError("This route doesn`t exist!");
        }

        if (Extensions.ContainsKey(AbsolutePath))
        {
            Extensions.Remove(AbsolutePath);
        }
        else
        {
            Logger.LogError("This extensions doesn`t exist!");
        }
       
    }
    public void Dispose()
    {
        Disposed = true;
        Listener.Abort();
        GC.SuppressFinalize(this);
    }
    ~HttpServer()
    {
        Dispose();
    }
}
