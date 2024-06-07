# Simple async http server
## Http Example
```
    private class ExampleExtension : IHttpServerExtension
    {
        private Random m_Random = new Random();
        public bool Execute(HttpListenerContext Context)
        {   
            //This will allow the page to load
            //return true;
            //It is not 
            //return false;
            return m_Random.Next(2) == 1;
        }
    }
    private class Program
    {
        static void Main(string[] args)
        {
            HttpServer Server = new HttpServer(new string[] { "http://*:80/"});
            //Adds route
            Server.AddRoute("/", (req, res) =>
            {
                //Send text
                res.SendAndClose("Hello world!");
                //Send embed resource
                //res.SendEmbedResourceAndClose("Namespace.Folder.file.type");
                //Send file in relative path
                //res.SendFile("/resources/index.html");
            });
            //Example of extension
            Server.AddRoute("/extension", (req, res) => {
                res.SendAndClose("You are lucky!");
            }, new IHttpServerExtension[] {
                new ExampleExtension();
            });
            //Delete route. After that, the page will be unavailable
            //Server.RemoveRoute("/");
            
            while (true);
        }
    }
```
## WebSocket Example
```
    //inheritance from the SocketEvent class is required
    private class MessageEvent : SocketEvent 
    {
        public string Message = string.Empty;
        public MessageEvent(string Message) : base("chat-message")
        {
            this.Message = Message;
        }
    }
    private class Program
    {
        static void Main(string[] args)
        {
            WebSocketServer Server = new WebSocketServer(new string[] { "http://*:80/ws"});
            
            //This is called when the correct SocketEvent was received and the Type of the given SocketEvent was equal to 1 argument. Second argument is a callback. Message is a json that received from client. Client is client
            WebSocketServer.On("chat-message", (Message, Client) =>
            {
                //Convert received json from client
                MessageEvent Event = JsonConvert.DeserializeObject(Message);
                if(Event != null)
                {
                    //Send message 
                    WebSocketServer.EmitAll(Event);
                    //Also you can emit to one client
                    //WebSocketServer.Emit(Event, Client);
                }
            });
            
            while (true);
        }
    }
```
## Also this package have class Logger that helps log with queque
### Info - Logger.LogInfo("Hello world!");
### Warning - Logger.LogWarning("Hellow world!");
### Error - Logger.LogError("Hello world!");
