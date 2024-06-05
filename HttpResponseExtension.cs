using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

public static class HttpResponseExtension
{
    public static void SendFile(this HttpListenerResponse instance, string FilePath)
    {
        byte[] FileContent = Encoding.UTF8.GetBytes(File.ReadAllText(Environment.CurrentDirectory + FilePath));
        instance.OutputStream.Write(FileContent, 0, FileContent.Length);
    }
    public static void SendFileAndClose(this HttpListenerResponse instance, string FilePath)
    {
        SendFile(instance, FilePath);
        instance.Close();
    }
    public static void Send(this HttpListenerResponse instance, string Content)
    {
        byte[] FileContent = Encoding.UTF8.GetBytes(Content);
        instance.OutputStream.Write(FileContent, 0, FileContent.Length);
    }
    public static void SendAndClose(this HttpListenerResponse instance, string Content)
    {
        Send(instance, Content);
        instance.Close();
    }
    public static void SendEmbedResource(this HttpListenerResponse instance, string Path)
    {
        byte[] FileContent = Encoding.UTF8.GetBytes(new StreamReader(Assembly.GetCallingAssembly().GetManifestResourceStream(Path)).ReadToEnd());
        instance.OutputStream.Write(FileContent, 0, FileContent.Length);
    }
    public static void SendEmbedResource(this HttpListenerResponse instance, string Path, Assembly CurrentAssembly)
    {
        byte[] FileContent = Encoding.UTF8.GetBytes(new StreamReader(CurrentAssembly.GetManifestResourceStream(Path)).ReadToEnd());
        instance.OutputStream.Write(FileContent, 0, FileContent.Length);
    }
    public static void SendEmbedResourceAndClose(this HttpListenerResponse instance, string Path)
    {
        SendEmbedResource(instance, Path, Assembly.GetCallingAssembly());
        instance.Close();
    }
}
