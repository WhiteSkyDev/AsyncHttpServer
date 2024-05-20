internal class Example
{
    private static HttpServer Server = new HttpServer(new string[] { "http://*:85/" });
    static void Main()
    {
        Server.AddRoute("/", (req, res) =>
        {
            res.SendAndClose("Hello world");
        });
    }
}
