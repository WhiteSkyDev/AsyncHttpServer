﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class Logger
{
    public enum LogSeverity
    {
        Info, Warning, Error
    }
    public struct Log
    {
        public string Content;
        public LogSeverity Level;
    }
    private static Queue<Log> LogQueue = new Queue<Log>();
    private static TimeSpan LogDelay = TimeSpan.FromMilliseconds(10);
    public static void LogInfo(string message)
    {
        LogQueue.Enqueue(new Log { Content = message, Level = LogSeverity.Info });
    }
    public static void LogWarning(string message)
    {
        LogQueue.Enqueue(new Log { Content = message, Level = LogSeverity.Warning });
    }
    public static void LogError(string message)
    {
        LogQueue.Enqueue(new Log { Content = message, Level = LogSeverity.Error });
    }
    public static void ChangeLogDelay(TimeSpan Delay)
    {
        LogDelay = Delay;
    }
    static Logger()
    {
        new Task(async () =>
        {
            while (true)
            {
                if (LogQueue.Count < 1) continue;
                Log CurrentLog = LogQueue.Dequeue();
                switch (CurrentLog.Level)
                {
                    case LogSeverity.Info:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"[{DateTime.Now}/INFO]");
                        break;
                    case LogSeverity.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.Write($"[{DateTime.Now}/WARNING]");
                        break;
                    case LogSeverity.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"[{DateTime.Now}/ERROR]");
                        break;
                }
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine($" {CurrentLog.Content}");
                await Task.Delay(LogDelay);
            }
        }).Start();
    }
}