# Simple async http server
## Example
```
    private class Program
    {
        static void Main(string[] args)
        {
            HttpServer Server = new HttpServer(new string[] { "http://*:80/"});
            Server.AddRoute("/", (req, res) =>
            {
                res.SendAndClose("Hello world!");
            });
            
            while (true);
        }
    }
```