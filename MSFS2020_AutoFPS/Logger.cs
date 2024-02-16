using System.Collections;

namespace MSFS2020_AutoFPS
{
    public enum LogLevel
    {
        Critical = 5,
        Error = 4,
        Warning = 3,
        Information = 2,
        Debug = 1,
        Verbose = 0,
    }

    public static class Logger
    {
        public static readonly Queue MessageQueue = new();

        public static void Log(LogLevel level, string context, string message)
        {
            string entry = string.Format("[ {0,-32} ] {1}", (context.Length <= 32 ? context : context[0..32]), message.Replace("\n", "").Replace("\r", "").Replace("\t", ""));
            switch (level)
            {
                case LogLevel.Critical:
                    Serilog.Log.Logger.Fatal(entry);
                    break;
                case LogLevel.Error:
                    Serilog.Log.Logger.Error(entry);
                    break;
                case LogLevel.Warning:
                    Serilog.Log.Logger.Warning(entry);
                    break;
                case LogLevel.Information:
                    Serilog.Log.Logger.Information(entry);
                    break;
                case LogLevel.Debug:
                    Serilog.Log.Logger.Debug(entry);
                    break;
                case LogLevel.Verbose:
                    Serilog.Log.Logger.Verbose(entry);
                    break;
                default:
                    Serilog.Log.Logger.Debug(entry);
                    break;
            }
            if (level > LogLevel.Debug)
            {
                if (message.Length > 80)
                    MessageQueue.Enqueue(message[1..80]);
                else
                    MessageQueue.Enqueue(message);
            }
        }
    }
}
