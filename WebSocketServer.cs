using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Security.Permissions;
using Newtonsoft.Json;
using System.Reflection.Emit;


public class SocketEvent
{
    public string Type = string.Empty;

    public SocketEvent(string type)
    {
        Type = type;
    }
}
public class WebSocketServer : IDisposable
{
    public delegate void SocketEventCallback(string Message, WebSocket Client);
    private Dictionary<string, SocketEventCallback> Callbacks = new Dictionary<string, SocketEventCallback>();
    private HttpListener Listener;
    private bool Disposed = false;
    private List<WebSocket> Clients = new List<WebSocket>();

    public WebSocketServer(string[] Prefixes)
    {
        Init();
        StartServer(Prefixes);
    }
    private void Init()
    {
        Listener = new HttpListener();
    }
    private void StartServer(string[] Prefixes)
    {
        foreach(string Prefix in Prefixes)
        {
            Listener.Prefixes.Add(Prefix);
        }
        Listener.Start();
        Logger.LogInfo("WebSocket server started.");

        new Thread(() =>
        {
            while (!Disposed)
            {
                IAsyncResult Result = Listener.BeginGetContext(ProcessHttpRequest, Listener);
                Result.AsyncWaitHandle.WaitOne();
            }
        }).Start();
    }

    private void ProcessHttpRequest(IAsyncResult Result)
    {
        HttpListenerContext context = Listener.EndGetContext(Result);
        if (context.Request.IsWebSocketRequest)
        {
            ProcessWebSocketRequest(context);
        }
        else
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
        }
    }
    private async void ProcessWebSocketRequest(HttpListenerContext Context)
    {
        HttpListenerWebSocketContext WebSocketContext = null;
        try
        {
            WebSocketContext = await Context.AcceptWebSocketAsync(null);
            Logger.LogInfo("WebSocket connection established.");
            if (Callbacks.ContainsKey("connection"))
            {
                Callbacks["connection"](string.Empty, WebSocketContext.WebSocket);
            }
            if (!Clients.Contains(WebSocketContext.WebSocket))
            {
                Clients.Add(WebSocketContext.WebSocket);
            }

            await HandleWebSocketConnection(WebSocketContext.WebSocket);
        }
        catch (Exception ex)
        {
            Logger.LogError("WebSocket closed by: " + ex.Message);
            if (WebSocketContext != null)
            {
                if (Callbacks.ContainsKey("disconnection"))
                {
                    Callbacks["disconnection"](string.Empty, WebSocketContext.WebSocket);
                }
                if (Clients.Contains(WebSocketContext.WebSocket))
                {
                    Clients.Remove(WebSocketContext.WebSocket);
                }
                if (WebSocketContext.WebSocket.State != WebSocketState.Closed)
                {
                    await WebSocketContext.WebSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Internal Server Error", CancellationToken.None);
                }
            }
        }
    }

    private async Task HandleWebSocketConnection(WebSocket Client)
    {
        byte[] Buffer = new byte[8192];
        WebSocketReceiveResult ReceiveResult = await Client.ReceiveAsync(new ArraySegment<byte>(Buffer), CancellationToken.None);
                                      
        while (!ReceiveResult.CloseStatus.HasValue)
        {
            string ReceivedMessage = Encoding.UTF8.GetString(Buffer, 0, ReceiveResult.Count);
            SocketEvent Event = JsonConvert.DeserializeObject<SocketEvent>(ReceivedMessage);
            if(Event != null && Callbacks.ContainsKey(Event.Type))
            {
                Callbacks[Event.Type](ReceivedMessage, Client);
            } 

            ReceiveResult = await Client.ReceiveAsync(new ArraySegment<byte>(Buffer), CancellationToken.None);
        }

        if (Callbacks.ContainsKey("disconnection")) 
        { 
            Callbacks["disconnection"](string.Empty, Client); 
        }
        if (Clients.Contains(Client))
        {
            Clients.Remove(Client);
        }
        await Client.CloseAsync(ReceiveResult.CloseStatus.Value, ReceiveResult.CloseStatusDescription, CancellationToken.None);
        Logger.LogInfo("WebSocket connection closed.");
    }
    public void On(string Type, SocketEventCallback Callback)
    {
        if(!Callbacks.ContainsKey(Type))
        {
            Callbacks.Add(Type, Callback);
        }
        else
        {
            Logger.LogError("That callback already registred!");
        }
    }
    public async void EmitAll(object Event)
    {
        foreach(WebSocket Client in Clients)
        {
            Emit(Event, Client);
        }
    }
    public async void Emit(object Event, WebSocket Client)
    {
        if (Client.State != WebSocketState.Open && Event.GetType().IsSubclassOf(typeof(SocketEvent))) return; 

        byte[] Buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Event));
        await Client.SendAsync(new ArraySegment<byte>(Buffer, 0, Buffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
    }
    
    public void Dispose()
    {
        Disposed = true;
        Listener.Abort();
        GC.SuppressFinalize(this);
    }
}

